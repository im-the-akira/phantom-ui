using System.IO;
using System.Text;
using Microsoft.Win32;

namespace PhantomUI;

/// <summary>
/// Verificação de integridade do próprio catálogo, executada em tempo de execução.
/// Existe para garantir que nenhum id se repete, que nenhum perfil aponta para tweak inexistente
/// e, o mais importante, que nenhum comando classificado como mito perigoso ficou executável
/// por qualquer caminho (perfil automático, lote ou botão individual).
/// A tela de Diagnóstico mostra o resultado. O código de saída (Environment) não é usado aqui.
/// </summary>
public static class CatalogIntegrity
{
    public sealed class Issue
    {
        public string Kind { get; init; } = "";      // DUPLICADO / PERFIL / BLOQUEIO / VAZIO
        public string Detail { get; init; } = "";
        public bool Critical { get; init; }
        public string Color => Critical ? "#FF4757" : "#FFA502";
    }

    public sealed class Result
    {
        public List<Issue> Issues { get; } = new();
        public int Tweaks { get; set; }
        public int Sections { get; set; }
        public int Profiles { get; set; }
        public int Blocked { get; set; }
        public int Commands { get; set; }
        public bool Ok => Issues.All(i => !i.Critical);
        public string Headline => Issues.Count == 0
            ? Loc.Pick("Catálogo íntegro: nenhum problema encontrado.", "Catalog intact: no problems found.")
            : Loc.Pick($"{Issues.Count} problema(s) encontrado(s), {Issues.Count(i => i.Critical)} crítico(s).",
                       $"{Issues.Count} problem(s) found, {Issues.Count(i => i.Critical)} critical.");
    }

    // Trechos que NUNCA podem existir em comando executável, herdados do acervo original (iGust/Kairo).
    private static readonly (string Needle, string WhyPt, string WhyEn)[] Forbidden =
    {
        ("pagingfiles", "pagefile em 0 gera falta de memória em jogo grande", "pagefile 0 causes out-of-memory in heavy games"),
        ("PagingFiles", "pagefile em 0 gera falta de memória em jogo grande", "pagefile 0 causes out-of-memory in heavy games"),
        ("TdrLevel", "TdrLevel=0 impede o Windows de recuperar o driver de vídeo", "TdrLevel=0 stops Windows from recovering the display driver"),
        ("hypervisorlaunchtype off", "desliga o hipervisor e derruba Vanguard/WSL2", "turns hypervisor off and breaks Vanguard/WSL2"),
        ("hypervisorlaunchtype  off", "desliga o hipervisor e derruba Vanguard/WSL2", "turns hypervisor off and breaks Vanguard/WSL2"),
        ("disableelamdrivers", "permite rootkit antes do antivírus", "allows a rootkit before the antivirus"),
        ("AGPConcur", "ruído de pacote de Windows 98, sem efeito em PCIe", "Windows 98 packet noise, no effect on PCIe"),
        ("DisableAntiSpyware", "o Windows reverte em minutos e o efeito é nulo", "Windows reverts it in minutes, effect is null"),
        ("WinSxS", "apagar o WinSxS quebra a instalação de forma irreparável", "deleting WinSxS breaks the install beyond repair"),
        ("Prefetch", "apagar o Prefetch deixa o boot lento por semanas", "clearing Prefetch slows boot for weeks")
    };

    public static Result Verify()
    {
        var r = new Result();
        List<TweakEntry> all;
        try { all = TweakCatalog.All; }
        catch (Exception ex)
        {
            r.Issues.Add(new Issue { Kind = "CATÁLOGO", Detail = Loc.Pick($"o catálogo falhou ao carregar: {ex.Message}", $"catalog failed to load: {ex.Message}"), Critical = true });
            return r;
        }

        r.Tweaks = all.Count;
        r.Sections = TweakCatalog.Sections.Count;

        // 1) id vazio ou duplicado
        foreach (var g in all.GroupBy(t => t.Id, StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(g.Key))
            {
                r.Issues.Add(new Issue { Kind = "VAZIO", Detail = Loc.Pick($"tweak sem id na seção {g.First().Section}", $"tweak with no id in section {g.First().Section}"), Critical = true });
                continue;
            }
            if (g.Count() > 1)
                r.Issues.Add(new Issue { Kind = "DUPLICADO", Detail = Loc.Pick($"id '{g.Key}' aparece {g.Count()} vezes", $"id '{g.Key}' appears {g.Count()} times"), Critical = true });
        }

        // 2) perfil apontando para id inexistente, ou puxando risco alto/crítico/bloqueado
        var byId = all.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);
        r.Profiles = HardwareAutoOptimizer.Profiles.Count;
        foreach (var prof in HardwareAutoOptimizer.Profiles)
        {
            List<TweakEntry> eligible;
            try { eligible = HardwareAutoOptimizer.AutoEligible(HardwareAutoOptimizer.Current, prof.Id); }
            catch (Exception ex)
            {
                r.Issues.Add(new Issue { Kind = "PERFIL", Detail = Loc.Pick($"perfil '{prof.Id}' falhou: {ex.Message}", $"profile '{prof.Id}' failed: {ex.Message}"), Critical = true });
                continue;
            }
            foreach (var t in eligible)
            {
                if (!byId.ContainsKey(t.Id))
                    r.Issues.Add(new Issue { Kind = "PERFIL", Detail = Loc.Pick($"perfil '{prof.Id}' aponta para id inexistente '{t.Id}'", $"profile '{prof.Id}' points to unknown id '{t.Id}'"), Critical = true });
                else if (t.Risk is TweakRisk.High or TweakRisk.Critical or TweakRisk.BlockedHazard)
                    r.Issues.Add(new Issue { Kind = "PERFIL", Detail = Loc.Pick($"perfil '{prof.Id}' inclui '{t.Id}' de risco {t.Risk}", $"profile '{prof.Id}' includes '{t.Id}' with risk {t.Risk}"), Critical = true });
            }
        }

        // 3) comando perigoso que não esteja marcado como mito bloqueado
        foreach (var t in all)
        {
            r.Blocked += t.Risk == TweakRisk.BlockedHazard ? 1 : 0;
            var cmds = SafeCommands(t);
            r.Commands += cmds.Count;
            if (t.Risk == TweakRisk.BlockedHazard) continue;
            foreach (var c in cmds)
            {
                foreach (var (needle, whyPt, whyEn) in Forbidden)
                {
                    if (c.Contains(needle, StringComparison.OrdinalIgnoreCase))
                        r.Issues.Add(new Issue
                        {
                            Kind = "BLOQUEIO",
                            Detail = Loc.Pick($"'{t.Id}' ({t.Section}) tem comando com '{needle}' fora da seção de bloqueio: {whyPt}",
                                              $"'{t.Id}' ({t.Section}) has a command with '{needle}' outside the blocked section: {whyEn}"),
                            Critical = true
                        });
                }
            }
        }

        // 4) mito bloqueado que por acidente ganhou operação executável
        foreach (var t in all.Where(t => t.Risk == TweakRisk.BlockedHazard))
        {
            var cmds = SafeCommands(t);
            var runnable = cmds.Count(c => !c.StartsWith("#", StringComparison.Ordinal) && !c.Contains("BLOQUEADO", StringComparison.OrdinalIgnoreCase));
            if (runnable > 0)
                r.Issues.Add(new Issue { Kind = "BLOQUEIO", Detail = Loc.Pick($"'{t.Id}' está marcado como bloqueado mas tem {runnable} comando executável", $"'{t.Id}' is marked blocked but has {runnable} runnable command(s)"), Critical = true });
        }

        return r;
    }

    private static List<string> SafeCommands(TweakEntry t)
    {
        try { return t.Ops.SelectMany(CommandLines).Where(l => !string.IsNullOrWhiteSpace(l)).ToList(); }
        catch { return new List<string>(); }
    }

    private static IEnumerable<string> CommandLines(TweakOp op)
    {
        if (!string.IsNullOrWhiteSpace(op.ApplyCmd)) yield return op.ApplyCmd;
        if (!string.IsNullOrWhiteSpace(op.Target) && op.Kind is TweakOpKind.RegSet or TweakOpKind.RegDelete or TweakOpKind.ServiceStartMode or TweakOpKind.BcdSet)
            yield return op.Target + " " + op.ValueName;
    }
}

/// <summary>
/// Gera um arquivo de texto com o retrato da máquina para suporte: hardware detectado,
/// diagnóstico, achados do doctor, auditoria de drivers e integridade do catálogo.
/// Sem dado pessoal: nenhum nome de usuário, caminho de pasta pessoal, nome de máquina ou chave.
/// </summary>
public static class SupportReport
{
    public static string Build(AppSettings settings, Action<string>? progress = null)
    {
        var sb = new StringBuilder();
        void Line(string s) => sb.AppendLine(s);
        void Head(string s) { sb.AppendLine(); Line(new string('=', 72)); Line(s.ToUpperInvariant()); Line(new string('=', 72)); }

        Head("phantom ui · relatório de suporte");
        Line($"Gerado em      : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Line($"Versão do app  : {typeof(SupportReport).Assembly.GetName().Version}");
        Line($"Windows        : {Environment.OSVersion.VersionString} ({Environment.OSVersion.Platform})");
        Line($"Arquitetura    : {(Environment.Is64BitOperatingSystem ? "x64" : "x86")} · processo {(Environment.Is64BitProcess ? "x64" : "x86")}");
        Line($"CLR            : {Environment.Version}");
        Line($"Elevado        : {TweakCatalog.IsElevated}");

        progress?.Invoke(Loc.Pick("Lendo hardware…", "Reading hardware…"));
        var p = HardwareAutoOptimizer.Detect(force: true);
        Head("hardware");
        Line($"CPU            : {p.CpuLabel}");
        Line($"Código         : {p.CpuCodename}");
        Line($"GPU            : {p.GpuLabel}");
        Line($"ReBAR          : {(p.ReBarEnabled ? "ligado" : p.ReBarSupported ? "suportado e desligado" : "não suportado")}");
        Line($"RAM            : {p.RamLabel} · {p.RamType}");
        Line($"Armazenamento  : {p.StorageLabel}");
        Line($"Placa-mãe      : {p.BoardVendor} {p.BoardName}");
        Line($"Chassi         : {p.FormFactorLabel}");
        Line($"BIOS           : {p.BiosVersion} · {p.BiosDate:yyyy-MM-dd}");
        Line($"SO             : {p.OsVersion} build {p.WindowsBuild}");
        Line($"Segurança      : Secure Boot {(p.SecureBoot ? "ligado" : "desligado")} · TPM {(p.TpmPresent ? "presente" : "ausente")}");
        Line($"Anti-cheats    : {(p.AntiCheats.Count == 0 ? "nenhum" : string.Join(", ", p.AntiCheats))}");

        Head("diagnóstico do hardware");
        Line(HardwareAutoOptimizer.BuildDiagnostics(p));

        progress?.Invoke(Loc.Pick("Integridade do catálogo…", "Catalog integrity…"));
        var c = CatalogIntegrity.Verify();
        Head("integridade do catálogo");
        Line($"Tweaks         : {c.Tweaks} em {c.Sections} seções · {c.Blocked} bloqueados por segurança · {c.Commands} comandos renderizados");
        Line($"Perfis         : {c.Profiles}");
        Line($"Resultado      : {(c.Ok ? "OK" : "PROBLEMAS ENCONTRADOS")} · {c.Headline}");
        foreach (var i in c.Issues) Line($"  [{i.Kind}] {i.Detail}");

        progress?.Invoke(Loc.Pick("Auditoria de drivers…", "Driver audit…"));
        Head("drivers");
        try
        {
            var rows = DriverAudit.Audit();
            Line($"Dispositivos   : {rows.Count} · genéricos {rows.Count(r => r.IsGeneric)} · antigos {rows.Count(r => r.IsOld)}");
            foreach (var r in rows.Take(60))
                Line($"  {r.Status,-9} {r.Class,-22} {r.Version,-18} {r.Device}");
            if (rows.Count > 60) Line($"  ... e mais {rows.Count - 60}");
            foreach (var g in DriverAudit.GpuTargets())
                Line($"  GPU {g.Vendor}: {g.GpuName} · instalado {g.InstalledVersion} · {g.OfficialPage}");
        }
        catch (Exception ex) { Line($"  Falhou: {ex.Message}"); }

        Head("arquivos e preferências");
        Line($"Pasta          : {AppSettings.Folder}");
        Line($"Backups        : {(Directory.Exists(Path.Combine(AppSettings.Folder, "Backups")) ? Directory.GetFiles(Path.Combine(AppSettings.Folder, "Backups")).Length + " arquivo(s)" : "nenhum")}");
        Line($"Relatórios     : {(Directory.Exists(Path.Combine(AppSettings.Folder, "Reports")) ? Directory.GetFiles(Path.Combine(AppSettings.Folder, "Reports")).Length + " arquivo(s)" : "nenhum")}");
        Line($"Idioma/Tema    : {(Loc.IsEn ? "en" : "pt")} · {(ThemeManager.IsLight ? "claro" : "escuro")}");
        Line($"RAM automática : {(settings.AutoRamPurge ? "ligada, limite " + settings.AutoRamFreeThresholdMb + " MB" : "desligada")}");
        Line($"Timer 0,5 ms   : {(settings.GameTimer05Ms ? "ligado no modo jogo" : "desligado")}");

        Head("nota");
        Line(Loc.Pick("Este relatório não contém nome de usuário, chave de API nem nome do computador.",
                      "This report contains no user name, API key or computer name."));

        return sb.ToString();
    }

    /// <summary>Só o resumo de integridade, usado no aviso de abertura.</summary>
    public static string IntegrityOneLiner()
    {
        var c = CatalogIntegrity.Verify();
        return Loc.Pick($"Catálogo: {c.Tweaks} tweaks em {c.Sections} seções, {c.Blocked} bloqueados. {c.Headline}",
                        $"Catalog: {c.Tweaks} tweaks in {c.Sections} sections, {c.Blocked} blocked. {c.Headline}");
    }
}
