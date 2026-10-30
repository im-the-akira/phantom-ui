using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace PhantomUI;

public enum FindingSeverity { Info, Warning, Critical }

public sealed class DoctorFinding
{
    public string Area { get; init; } = "";
    public string Title { get; init; } = "";
    public string Detail { get; init; } = "";
    public FindingSeverity Severity { get; init; }
    public string? FixId { get; init; }
    public string SeverityLabel => Severity switch
    {
        FindingSeverity.Critical => Loc.Pick("CRÍTICO", "CRITICAL"),
        FindingSeverity.Warning => Loc.Pick("ATENÇÃO", "WARNING"),
        _ => "OK"
    };
    public string Color => Severity switch
    {
        FindingSeverity.Critical => "#FF4757",
        FindingSeverity.Warning => "#FFA502",
        _ => "#2ED573"
    };
}

public sealed class RepairAction
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string Desc { get; init; } = "";
    public string Command { get; init; } = "";
    public string Risk { get; init; } = "Baixo";
    public bool NeedsReboot { get; init; }
    public bool Long { get; init; }
}

/// <summary>
/// Detector de problemas de hardware/SO + Centro de Reparos.
/// S.M.A.R.T., WHEA/TDR, bit sujo, WU/DNS, caches, SFC, DISM inteligente, rede e Bluetooth.
/// </summary>
public static class SystemDoctor
{
    // =====================================================================
    // DETECÇÃO
    // =====================================================================
    public static async Task<List<DoctorFinding>> ScanAsync(Action<string>? progress = null)
    {
        return await Task.Run(() =>
        {
            var f = new List<DoctorFinding>();
            progress?.Invoke(Loc.Pick("Lendo S.M.A.R.T. dos discos…", "Reading disk S.M.A.R.T.…"));
            try
            {
                var cards = DiskHealth.Read(null);
                foreach (var c in cards)
                {
                    if (c.Verdict == DiskVerdict.Critical)
                        f.Add(new DoctorFinding { Area = Loc.Pick("Disco", "Disk"), Title = c.Title, Detail = c.Reasons,
                            Severity = FindingSeverity.Critical, FixId = "disk-health" });
                    else if (c.Verdict == DiskVerdict.Warning)
                        f.Add(new DoctorFinding { Area = Loc.Pick("Disco", "Disk"), Title = c.Title, Detail = c.Reasons,
                            Severity = FindingSeverity.Warning, FixId = "disk-health" });
                }
                if (cards.Count == 0)
                    f.Add(new DoctorFinding { Area = Loc.Pick("Disco", "Disk"), Title = Loc.Pick("S.M.A.R.T. indisponível", "S.M.A.R.T. unavailable"),
                        Detail = Loc.Pick("Execute como administrador para ler os atributos dos discos.", "Run as administrator to read disk attributes."),
                        Severity = FindingSeverity.Warning });
            }
            catch (Exception ex)
            {
                f.Add(new DoctorFinding { Area = Loc.Pick("Disco", "Disk"), Title = Loc.Pick("Falha ao ler discos", "Failed reading disks"),
                    Detail = ex.Message, Severity = FindingSeverity.Warning });
            }

            progress?.Invoke(Loc.Pick("Analisando WHEA / TDR / BSOD…", "Analyzing WHEA / TDR / BSOD…"));
            var whea = CountEvents("Microsoft-Windows-WHEA-Logger", null, 7);
            if (whea > 0)
                f.Add(new DoctorFinding
                {
                    Area = "Hardware", Title = Loc.Pick($"{whea} erro(s) WHEA em 7 dias", $"{whea} WHEA error(s) in 7 days"),
                    Detail = Loc.Pick("WHEA indica erro de hardware: memória instável (XMP/EXPO), PCIe ou CPU. Nenhum tweak corrige — reduza o XMP ou teste as memórias.",
                                       "WHEA means hardware error: unstable memory (XMP/EXPO), PCIe or CPU. No tweak fixes it — lower XMP or test the RAM."),
                    Severity = FindingSeverity.Critical, FixId = "whea"
                });

            var tdr = CountEvents("Display", 4101, 30);
            if (tdr > 2)
                f.Add(new DoctorFinding
                {
                    Area = "GPU", Title = Loc.Pick($"{tdr} TDR em 30 dias (driver reiniciou)", $"{tdr} TDR in 30 days (driver restarted)"),
                    Detail = Loc.Pick("Driver de vídeo reiniciou. Use DDU para reinstalar o driver oficial, remova overclock e verifique a fonte/cabo de energia da GPU.",
                                       "Video driver restarted. Use DDU to reinstall the official driver, remove overclock and check the PSU/GPU power cable."),
                    Severity = tdr > 5 ? FindingSeverity.Critical : FindingSeverity.Warning, FixId = "tdr"
                });

            var bsod = CountEvents("System", 1001, 30);
            if (bsod > 0)
                f.Add(new DoctorFinding
                {
                    Area = "Kernel", Title = Loc.Pick($"{bsod} BugCheck (tela azul) em 30 dias", $"{bsod} BugCheck (blue screen) in 30 days"),
                    Detail = Loc.Pick("Veja o código de parada no Centro de Reparos. Causa comum: driver de áudio/rede antigo ou overclock instável.",
                                       "Check the stop code in the Repair Center. Common cause: old audio/network driver or unstable overclock."),
                    Severity = FindingSeverity.Warning, FixId = "bsod"
                });

            progress?.Invoke(Loc.Pick("Checando bit sujo e volumes…", "Checking dirty bit and volumes…"));
            foreach (var drive in DriveLetters())
            {
                var dirty = TweakJournal.RunProcess("fsutil.exe", $"dirty query {drive}:", 8_000);
                if (dirty.Output.Contains("is Dirty", StringComparison.OrdinalIgnoreCase))
                    f.Add(new DoctorFinding
                    {
                        Area = Loc.Pick("Disco", "Disk"), Title = Loc.Pick($"Volume {drive}: marcado como sujo", $"Volume {drive}: flagged dirty"),
                        Detail = Loc.Pick("O Windows vai rodar chkdsk no próximo boot. Corrija com 'Reparar volume' para não repetir toda inicialização.",
                                           "Windows will run chkdsk on next boot. Fix it with 'Repair volume' so it does not repeat every startup."),
                        Severity = FindingSeverity.Warning, FixId = "dirty" });
            }

            progress?.Invoke(Loc.Pick("Auditando serviços de atualização e DNS…", "Auditing update services and DNS…"));
            int? wu = TweakJournal.ReadServiceStart("wuauserv");
            if (wu is 4)
                f.Add(new DoctorFinding
                {
                    Area = "Windows Update", Title = Loc.Pick("wuauserv desativado", "wuauserv disabled"),
                    Detail = Loc.Pick("Pacotes de tweak desativam o Windows Update e o sistema fica sem patch de segurança. Reative no Centro de Reparos.",
                                       "Tweak packs disable Windows Update and the system goes unpatched. Re-enable it in the Repair Center."),
                    Severity = FindingSeverity.Critical, FixId = "wu"
                });
            int? bits = TweakJournal.ReadServiceStart("bits");
            if (bits is 4)
                f.Add(new DoctorFinding
                {
                    Area = "Windows Update", Title = Loc.Pick("BITS desativado", "BITS disabled"),
                    Detail = Loc.Pick("Sem BITS, downloads de update e da Store falham com erro 0x80246007.", "Without BITS, updates and Store downloads fail with 0x80246007."),
                    Severity = FindingSeverity.Warning, FixId = "wu" });

            var hosts = ReadHosts();
            if (hosts > 0)
                f.Add(new DoctorFinding
                {
                    Area = "Rede", Title = Loc.Pick($"{hosts} bloqueio(s) no arquivo hosts", $"{hosts} entries blocked in the hosts file"),
                    Detail = Loc.Pick("Pacotes de 'debloat' colocam dezenas de domínios no hosts. Isso quebra login de jogos, Store e Windows Update.",
                                       "'Debloat' packs add dozens of domains to hosts. That breaks game logins, Store and Windows Update."),
                    Severity = FindingSeverity.Warning, FixId = "hosts" });

            try
            {
                var dns = TweakJournal.RunProcess("nslookup.exe", "www.microsoft.com 8.8.8.8", 8_000);
                if (!dns.Output.Contains("Address", StringComparison.OrdinalIgnoreCase))
                    f.Add(new DoctorFinding
                    {
                        Area = "Rede", Title = Loc.Pick("Resolução DNS externa falhou", "External DNS resolution failed"),
                        Detail = Loc.Pick("Rode o benchmark de DNS na aba Rede e aplique o servidor mais rápido, ou repare a pilha TCP/IP.",
                                           "Run the DNS benchmark on the Network page and apply the fastest server, or repair the TCP/IP stack."),
                        Severity = FindingSeverity.Warning, FixId = "dns" });
            }
            catch { }

            progress?.Invoke(Loc.Pick("Checando espaço, pagefile e memória…", "Checking space, pagefile and memory…"));
            try
            {
                var sys = new System.IO.DriveInfo(System.IO.Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\");
                double freeGb = sys.AvailableFreeSpace / 1073741824.0;
                if (freeGb < 15)
                    f.Add(new DoctorFinding
                    {
                        Area = Loc.Pick("Disco", "Disk"), Title = Loc.Pick($"Só {freeGb:0.#} GB livres no {sys.Name}", $"Only {freeGb:0.#} GB free on {sys.Name}"),
                        Detail = Loc.Pick("Abaixo de 15 GB o Windows não consegue atualizar nem criar pagefile adequado. Limpe a pasta de temporários e jogos não usados.",
                                           "Below 15 GB Windows cannot update or create an adequate pagefile. Clean temp files and unused games."),
                        Severity = freeGb < 8 ? FindingSeverity.Critical : FindingSeverity.Warning, FixId = "space" });
            }
            catch { }

            var (totalMb, freeMb, _) = InputLagTools.GetLiveMemoryAndTimer();
            if (totalMb > 0 && freeMb / totalMb < 0.08)
                f.Add(new DoctorFinding
                {
                    Area = "Memória", Title = Loc.Pick($"RAM livre em {freeMb:0} MB ({freeMb / totalMb:P0})", $"Free RAM at {freeMb:0} MB ({freeMb / totalMb:P0})"),
                    Detail = Loc.Pick("Memória praticamente esgotada: feche abas/launchers, ative a limpeza automática de RAM ou aumente a RAM física.",
                                       "Memory nearly exhausted: close tabs/launchers, enable automatic RAM purge or add physical RAM."),
                    Severity = FindingSeverity.Warning, FixId = "ram" });

            bool pfOff = PagefileDisabled();
            if (pfOff)
                f.Add(new DoctorFinding
                {
                    Area = "Memória", Title = Loc.Pick("Pagefile desativado (iGust hazard)", "Pagefile disabled (iGust hazard)"),
                    Detail = Loc.Pick("Sem pagefile, jogos pesados recebem 'Out of memory' e o sistema congela. Restaure para 'Gerenciado pelo sistema'.",
                                       "Without a pagefile, heavy games hit 'Out of memory' and the system freezes. Restore it to 'System managed'."),
                    Severity = FindingSeverity.Critical, FixId = "pagefile" });

            progress?.Invoke(Loc.Pick("Procurando tweaks perigosos aplicados…", "Scanning for applied dangerous tweaks…"));
            if (TweakJournal.ReadReg(@"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "TdrLevel") is int tdrLevel && tdrLevel == 0)
                f.Add(new DoctorFinding
                {
                    Area = "GPU", Title = "TdrLevel = 0", Detail = Loc.Pick("Desativa a recuperação do driver de vídeo: em vez de um TDR recuperável, o jogo trava e a tela fica preta.",
                                                                        "Disables video driver recovery: instead of a recoverable TDR, the game freezes and the screen goes black."),
                    Severity = FindingSeverity.Critical, FixId = "tdrlevel" });

            if (TweakJournal.ReadReg(KGameTask(), "Affinity") is int aff && aff != 0)
                f.Add(new DoctorFinding
                {
                    Area = "CPU", Title = Loc.Pick($"MMCSS Affinity fixa (0x{aff:X})", $"MMCSS Affinity pinned (0x{aff:X})"),
                    Detail = Loc.Pick("Afinidade fixa no MMCSS prende áudio e agendador de jogos em poucos núcleos: stutter e cortes de som.",
                                       "Pinned MMCSS affinity traps audio and the game scheduler on few cores: stutter and sound cutouts."),
                    Severity = FindingSeverity.Warning, FixId = "mmcss" });

            int? hypervisor = null;
            var bcd = TweakJournal.RunProcess("bcdedit.exe", "/enum {current}", 8_000).Output;
            if (bcd.Contains("hypervisorlaunchtype", StringComparison.OrdinalIgnoreCase) && bcd.Contains("Off", StringComparison.OrdinalIgnoreCase))
                hypervisor = 0;
            var antiCheats = HardwareAutoOptimizer.DetectedAntiCheats();
            if (hypervisor == 0 && antiCheats.Any(a => a.Contains("Vanguard") || a.Contains("FACEIT")))
                f.Add(new DoctorFinding
                {
                    Area = "Anti-Cheat", Title = Loc.Pick("Hipervisor desligado com Vanguard/FACEIT instalado", "Hypervisor off with Vanguard/FACEIT installed"),
                    Detail = Loc.Pick("Esses anti-cheats exigem virtualização. O jogo vai recusar abrir até restaurar 'hypervisorlaunchtype auto'.",
                                       "These anti-cheats require virtualization. The game refuses to launch until 'hypervisorlaunchtype auto' is restored."),
                    Severity = FindingSeverity.Critical, FixId = "hypervisor" });

            progress?.Invoke(Loc.Pick("Verificando integridade do sistema…", "Checking system integrity…"));
            int? sysmain = TweakJournal.ReadServiceStart("SysMain");
            bool hasHdd = HardwareAutoOptimizer.Detect().HasHdd;
            if (sysmain is 4 && hasHdd)
                f.Add(new DoctorFinding
                {
                    Area = "Disco", Title = Loc.Pick("SysMain desativado com HDD presente", "SysMain disabled with an HDD present"),
                    Detail = Loc.Pick("Erro grave do acervo Kairo: sem SysMain em HDD, cada abertura de jogo espera o disco. Reative.",
                                       "Serious Kairo mistake: without SysMain on HDD, every game launch waits on the disk. Re-enable it."),
                    Severity = FindingSeverity.Warning, FixId = "sysmain" });

            if (f.Count == 0 || f.All(x => x.Severity == FindingSeverity.Info))
                f.Add(new DoctorFinding
                {
                    Area = Loc.Pick("Geral", "General"), Title = Loc.Pick("Nenhum problema crítico encontrado", "No critical problem found"),
                    Detail = Loc.Pick("Discos, eventos de hardware, serviços, rede e memória estão saudáveis. Faça um ponto de restauração antes de aplicar tweaks.",
                                       "Disks, hardware events, services, network and memory are healthy. Make a restore point before applying tweaks."),
                    Severity = FindingSeverity.Info
                });

            return f.OrderBy(x => x.Severity == FindingSeverity.Info ? 2 : x.Severity == FindingSeverity.Warning ? 1 : 0).ToList();
        });
    }

    private static string KGameTask() => @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games";

    private static int CountEvents(string provider, int? id, int days)
    {
        try
        {
            string query = id.HasValue
                ? $"*[System[(EventID={id}) and TimeCreated[timediff(@SystemTime) <= {days * 86400000L}]]]"
                : $"*[System[Provider[@Name='{provider}'] and TimeCreated[timediff(@SystemTime) <= {days * 86400000L}]]]";
            var r = TweakJournal.RunProcess("wevtutil.exe", $"qe System /q:\"{query}\" /c:50 /rd:true /f:text", 20_000);
            if (r.ExitCode != 0) return 0;
            // cada evento no formato texto começa com "Event[0]:" — conta ocorrências de "Event["
            int count = Regex.Matches(r.Output, @"Event\[\d+\]").Count;
            if (count == 0 && r.Output.Trim().Length > 50) count = 1;
            return Math.Min(count, 50);
        }
        catch { return 0; }
    }

    private static IEnumerable<string> DriveLetters()
    {
        var list = new List<string>();
        try
        {
            foreach (var d in System.IO.DriveInfo.GetDrives())
            {
                if (d.DriveType != System.IO.DriveType.Fixed || !d.IsReady) continue;
                if (d.Name.StartsWith("C", StringComparison.OrdinalIgnoreCase)) list.Add("C");
            }
        }
        catch { }
        return list.Count == 0 ? new[] { "C" } : list;
    }

    private static int ReadHosts()
    {
        try
        {
            string path = System.IO.Path.Combine(Environment.SystemDirectory,
                @"drivers\etc\hosts");
            if (!System.IO.File.Exists(path)) return 0;
            return System.IO.File.ReadAllLines(path)
                .Count(l => l.Trim().Length > 0 && !l.TrimStart().StartsWith("#") &&
                            (l.Contains("microsoft", StringComparison.OrdinalIgnoreCase) ||
                             l.Contains("windowsupdate", StringComparison.OrdinalIgnoreCase) ||
                             l.Contains("xboxlive", StringComparison.OrdinalIgnoreCase) ||
                             l.Contains("riot", StringComparison.OrdinalIgnoreCase) ||
                             l.Contains("steam", StringComparison.OrdinalIgnoreCase)));
        }
        catch { return 0; }
    }

    public static bool PagefileDisabled()
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management");
            if (k?.GetValue("PagingFiles") is string[] arr)
                return arr.Length == 0 || arr.All(a => a.Contains(" 0 0") || a.Trim().Length == 0);
        }
        catch { }
        return false;
    }

    // =====================================================================
    // AÇÕES DE REPARO
    // =====================================================================
    public static List<RepairAction> Actions() => new()
    {
        new RepairAction
        {
            Id = "sfc", Title = "SFC /scannow", Command = "sfc /scannow", Long = true,
            Desc = Loc.Pick("Verifica e restaura arquivos protegidos do Windows corrompidos.", "Verifies and restores corrupted protected Windows files.")
        },
        new RepairAction
        {
            Id = "dism", Title = "DISM RestoreHealth (inteligente)", Command = "DISM /Online /Cleanup-Image /RestoreHealth", Long = true,
            Desc = Loc.Pick("Repara a imagem do Windows usando fonte oficial. O app libera temporariamente a política de Windows Update e a restaura no fim.",
                            "Repairs the Windows image from the official source. The app temporarily lifts the Windows Update policy and restores it afterwards.")
        },
        new RepairAction
        {
            Id = "chkdsk", Title = "Reparar volume (chkdsk /spotfix)", Command = "chkdsk C: /spotfix", NeedsReboot = true,
            Desc = Loc.Pick("Corrige o bit sujo e inconsistências de metadados sem varrer o disco inteiro.", "Fixes the dirty bit and metadata inconsistencies without a full scan.")
        },
        new RepairAction
        {
            Id = "chkdskfull", Title = "Agendar chkdsk completo no boot", Command = "chkdsk C: /f /r", NeedsReboot = true, Risk = "Médio",
            Desc = Loc.Pick("Varre setores defeituosos e recupera dados legíveis. Pode levar horas.", "Scans for bad sectors and recovers readable data. Can take hours.")
        },
        new RepairAction
        {
            Id = "retrim", Title = "ReTrim do SSD", Command = "defrag C: /L",
            Desc = Loc.Pick("Devolve blocos não usados ao controlador do SSD e recupera a velocidade de escrita.",
                            "Returns unused blocks to the SSD controller and restores write speed.")
        },
        new RepairAction
        {
            Id = "wu", Title = "Reparar Windows Update", Command = "net stop wuauserv && net stop bits && ren SoftwareDistribution ...", Risk = "Médio",
            Desc = Loc.Pick("Reativa wuauserv/BITS, renomeia SoftwareDistribution e catroot2 e reinicia os serviços.",
                            "Re-enables wuauserv/BITS, renames SoftwareDistribution and catroot2 and restarts the services.")
        },
        new RepairAction
        {
            Id = "net", Title = "Reparar rede (Winsock + TCP/IP)", Command = "netsh winsock reset && netsh int ip reset", NeedsReboot = true, Risk = "Médio",
            Desc = Loc.Pick("Reseta a pilha de rede. Resolve 'sem internet' fantasma e erro de conexão em jogos.",
                            "Resets the network stack. Fixes phantom 'no internet' and in-game connection errors.")
        },
        new RepairAction
        {
            Id = "hosts", Title = "Limpar bloqueios do arquivo hosts", Command = "backup + limpeza de entradas de debloat",
            Desc = Loc.Pick("Mantém o hosts original em backup e remove entradas que quebram login, Store e Update.", "Backs up the original hosts and removes entries that break logins, Store and Update.")
        },
        new RepairAction
        {
            Id = "shader", Title = "Resetar cache de shader", Command = "Remove-Item D3DSCache, NVIDIA\\DXCache, AMD\\DxCache",
            Desc = Loc.Pick("Corrige textura preta, crash de shader e stutter que não passa.", "Fixes black textures, shader crashes and persistent stutter.")
        },
        new RepairAction
        {
            Id = "fontcache", Title = "Reconstruir cache de fontes", Command = "net stop FontCache && del FontCache\\*", NeedsReboot = true,
            Desc = Loc.Pick("Corrige textos borrados e ícones quebrados no Explorer.", "Fixes blurry text and broken Explorer icons.")
        },
        new RepairAction
        {
            Id = "iconcache", Title = "Reconstruir cache de ícones", Command = "del iconcache_*.db && restart explorer",
            Desc = Loc.Pick("Resolve ícones brancos ou errados depois de instalar/remover jogos.", "Fixes white or wrong icons after installing/removing games.")
        },
        new RepairAction
        {
            Id = "temp", Title = "Limpeza de temporários (preserva Prefetch)", Command = "Remove-Item TEMP, Windows\\Temp, Logs\\CBS",
            Desc = Loc.Pick("Libera espaço sem tocar em C:\\Windows\\Prefetch nem em pontos de restauração.", "Frees space without touching C:\\Windows\\Prefetch or restore points.")
        },
        new RepairAction
        {
            Id = "bt", Title = "Reparar Bluetooth e áudio", Command = "Restart-Service bthserv, Audiosrv",
            Desc = Loc.Pick("Reinicia serviços e resolve headset que cai no meio da partida.", "Restarts services and fixes headsets dropping mid-match.")
        },
        new RepairAction
        {
            Id = "defender", Title = "Restaurar políticas de segurança", Command = "Remove-MpPreference / reabilitar serviços",
            Desc = Loc.Pick("Devolve ao padrão o que pacotes de tweak desativaram (Defender, ELAM, Update).", "Restores what tweak packs disabled (Defender, ELAM, Update).")
        },
        new RepairAction
        {
            Id = "store", Title = "Re-registrar Microsoft Store", Command = "wsreset.exe -i",
            Desc = Loc.Pick("Corrige Store que não abre ou apps que somem depois do debloat.", "Fixes a Store that will not open or apps that vanish after debloat.")
        },
        new RepairAction
        {
            Id = "time", Title = "Sincronizar horário (NTP)", Command = "w32tm /resync /force",
            Desc = Loc.Pick("Relógio errado quebra autenticação de jogos e TLS.", "A wrong clock breaks game authentication and TLS.")
        },
        new RepairAction
        {
            Id = "restorepoint", Title = "Criar ponto de restauração", Command = "Checkpoint-Computer",
            Desc = Loc.Pick("Cria um checkpoint do sistema antes de qualquer otimização.", "Creates a system checkpoint before any optimization.")
        }
    };

    /// <summary>Executa uma ação de reparo. Retorna (sucesso, saída).</summary>
    public static async Task<(bool Ok, string Output)> RunActionAsync(RepairAction action, Action<string>? progress = null)
    {
        return await Task.Run(() =>
        {
            var log = new StringBuilder();
            void Add(string s) { log.AppendLine(s); progress?.Invoke(s); }

            switch (action.Id)
            {
                case "sfc":
                {
                    Add(Loc.Pick("Executando SFC (pode levar de 5 a 20 minutos)…", "Running SFC (may take 5 to 20 minutes)…"));
                    var r = TweakJournal.RunProcess("sfc.exe", "/scannow", 1_800_000);
                    Add(r.Output.Trim());
                    if (!string.IsNullOrWhiteSpace(r.Error)) Add(r.Error.Trim());
                    bool ok = r.Output.Contains("did not find any integrity violations", StringComparison.OrdinalIgnoreCase)
                           || r.Output.Contains("successfully repaired", StringComparison.OrdinalIgnoreCase)
                           || r.Output.Contains("não encontrou nenhuma violação", StringComparison.OrdinalIgnoreCase)
                           || r.Output.Contains("reparou com êxito", StringComparison.OrdinalIgnoreCase);
                    return (r.ExitCode == 0 && ok, log.ToString());
                }

                case "dism":
                {
                    Add(Loc.Pick("DISM: liberando temporariamente a política de Windows Update…", "DISM: temporarily lifting the Windows Update policy…"));
                    var (lifted, policyBackup) = LiftWuPolicy();
                    try
                    {
                        Add(Loc.Pick("Executando DISM /RestoreHealth…", "Running DISM /RestoreHealth…"));
                        var r = TweakJournal.RunProcess("dism.exe", "/Online /Cleanup-Image /RestoreHealth", 1_800_000);
                        Add(r.Output.Trim());
                        if (!string.IsNullOrWhiteSpace(r.Error)) Add(r.Error.Trim());
                        bool ok = r.ExitCode == 0 || r.Output.Contains("operation completed successfully", StringComparison.OrdinalIgnoreCase)
                                  || r.Output.Contains("concluída com êxito", StringComparison.OrdinalIgnoreCase);
                        return (ok, log.ToString());
                    }
                    finally
                    {
                        if (lifted) RestoreWuPolicy(policyBackup);
                        Add(Loc.Pick("Política de Windows Update restaurada.", "Windows Update policy restored."));
                    }
                }

                case "chkdsk":
                {
                    var r = TweakJournal.RunProcess("chkdsk.exe", "C: /spotfix", 900_000);
                    Add(r.Output.Trim() + r.Error.Trim());
                    return (r.ExitCode is 0 or 1, log.ToString());
                }

                case "chkdskfull":
                {
                    var r = TweakJournal.RunProcess("chkdsk.exe", "C: /f /r", 30_000);
                    Add(Loc.Pick("O Windows vai executar o chkdsk completo no próximo reinício.", "Windows will run the full chkdsk on the next restart."));
                    Add(r.Output.Trim());
                    return (true, log.ToString());
                }

                case "retrim":
                {
                    var r = TweakJournal.RunProcess("defrag.exe", "C: /L", 300_000);
                    Add(r.Output.Trim() + r.Error.Trim());
                    return (r.ExitCode == 0, log.ToString());
                }

                case "wu":
                {
                    TweakJournal.RunProcess("net.exe", "stop wuauserv", 30_000);
                    TweakJournal.RunProcess("net.exe", "stop bits", 30_000);
                    TweakJournal.RunProcess("net.exe", "stop cryptsvc", 30_000);
                    try
                    {
                        string sd = @"C:\Windows\SoftwareDistribution";
                        string cat = @"C:\Windows\System32\catroot2";
                        if (System.IO.Directory.Exists(sd))
                            System.IO.Directory.Move(sd, sd + ".phantomui.old");
                        if (System.IO.Directory.Exists(cat))
                            System.IO.Directory.Move(cat, cat + ".phantomui.old");
                        Add(Loc.Pick("SoftwareDistribution e catroot2 renomeados.", "SoftwareDistribution and catroot2 renamed."));
                    }
                    catch (Exception ex) { Add("⚠ " + ex.Message); }
                    RestoreService("wuauserv", 3);
                    RestoreService("bits", 3);
                    RestoreService("cryptsvc", 2);
                    TweakJournal.RunProcess("net.exe", "start wuauserv", 30_000);
                    TweakJournal.RunProcess("net.exe", "start bits", 30_000);
                    TweakJournal.RunProcess("net.exe", "start cryptsvc", 30_000);
                    Add(Loc.Pick("Serviços de atualização reiniciados.", "Update services restarted."));
                    return (true, log.ToString());
                }

                case "net":
                {
                    var a = TweakJournal.RunProcess("netsh.exe", "winsock reset", 60_000);
                    Add(a.Output.Trim() + a.Error.Trim());
                    var b = TweakJournal.RunProcess("netsh.exe", "int ip reset", 60_000);
                    Add(b.Output.Trim() + b.Error.Trim());
                    var c = TweakJournal.RunProcess("ipconfig.exe", "/flushdns", 30_000);
                    Add(c.Output.Trim());
                    Add(Loc.Pick("Reinicie para concluir o reset da pilha de rede.", "Reboot to finish the network stack reset."));
                    return (true, log.ToString());
                }

                case "hosts":
                {
                    string path = @"C:\Windows\System32\drivers\etc\hosts";
                    if (!System.IO.File.Exists(path)) return (false, Loc.Pick("Arquivo hosts não encontrado.", "Hosts file not found."));
                    string backup = path + $".phantomui.{DateTime.Now:yyyyMMddHHmmss}.bak";
                    System.IO.File.Copy(path, backup, true);
                    var kept = new List<string>();
                    int removed = 0;
                    foreach (var line in System.IO.File.ReadAllLines(path))
                    {
                        string t = line.Trim();
                        if (t.Length == 0 || t.StartsWith("#")) { kept.Add(line); continue; }
                        if (t.Contains("microsoft", StringComparison.OrdinalIgnoreCase) ||
                            t.Contains("windowsupdate", StringComparison.OrdinalIgnoreCase) ||
                            t.Contains("xboxlive", StringComparison.OrdinalIgnoreCase) ||
                            t.Contains("riot", StringComparison.OrdinalIgnoreCase) ||
                            t.Contains("steam", StringComparison.OrdinalIgnoreCase) ||
                            t.Contains("epicgames", StringComparison.OrdinalIgnoreCase) ||
                            t.Contains("battle.net", StringComparison.OrdinalIgnoreCase))
                        { removed++; continue; }
                        kept.Add(line);
                    }
                    System.IO.File.WriteAllLines(path, kept);
                    TweakJournal.RunProcess("ipconfig.exe", "/flushdns", 20_000);
                    Add(Loc.Pick($"{removed} entrada(s) removidas. Backup: {backup}", $"{removed} entries removed. Backup: {backup}"));
                    return (true, log.ToString());
                }

                case "shader":
                {
                    var r = TweakJournal.RunPowerShell(
                        "Remove-Item -Recurse -Force \"$env:LOCALAPPDATA\\D3DSCache\\*\",\"$env:LOCALAPPDATA\\NVIDIA\\DXCache\\*\",\"$env:LOCALAPPDATA\\NVIDIA\\GLCache\\*\",\"$env:LOCALAPPDATA\\AMD\\DxCache\\*\",\"$env:LOCALAPPDATA%\\AMD\\DxcCache\\*\" -ErrorAction SilentlyContinue", 120_000);
                    Add(Loc.Pick("Cache de shader limpo (DirectX, NVIDIA e AMD).", "Shader cache cleared (DirectX, NVIDIA and AMD)."));
                    if (!string.IsNullOrWhiteSpace(r.Error)) Add(r.Error.Trim());
                    return (true, log.ToString());
                }

                case "fontcache":
                {
                    TweakJournal.RunProcess("net.exe", "stop FontCache", 30_000);
                    TweakJournal.RunPowerShell("Remove-Item -Force \"$env:WINDIR\\ServiceProfiles\\LocalService\\AppData\\Local\\FontCache\\*\" -Recurse -ErrorAction SilentlyContinue", 60_000);
                    TweakJournal.RunProcess("net.exe", "start FontCache", 30_000);
                    Add(Loc.Pick("Cache de fontes reconstruído.", "Font cache rebuilt."));
                    return (true, log.ToString());
                }

                case "iconcache":
                {
                    TweakJournal.RunProcess("taskkill.exe", "/f /im explorer.exe", 20_000);
                    TweakJournal.RunPowerShell(
                        "Remove-Item -Force \"$env:LOCALAPPDATA\\IconCache.db\",\"$env:LOCALAPPDATA\\Microsoft\\Windows\\Explorer\\iconcache*\",\"$env:LOCALAPPDATA\\Microsoft\\Windows\\Explorer\\thumbcache*\" -ErrorAction SilentlyContinue; Start-Process explorer.exe", 60_000);
                    Add(Loc.Pick("Cache de ícones reconstruído e Explorer reiniciado.", "Icon cache rebuilt and Explorer restarted."));
                    return (true, log.ToString());
                }

                case "temp":
                {
                    var r = TweakJournal.RunPowerShell(
                        "$before=(Get-PSDrive C).Free; Remove-Item \"$env:TEMP\\*\",\"$env:WINDIR\\Temp\\*\",\"$env:WINDIR\\Logs\\CBS\\*\",\"$env:LOCALAPPDATA\\CrashDumps\\*\" -Recurse -Force -ErrorAction SilentlyContinue; $after=(Get-PSDrive C).Free; '{0:N0} MB liberados' -f (($after-$before)/1MB)", 300_000);
                    Add(r.Output.Trim());
                    Add(Loc.Pick("Prefetch preservado (C:\\Windows\\Prefetch intacto).", "Prefetch preserved (C:\\Windows\\Prefetch untouched)."));
                    return (true, log.ToString());
                }

                case "bt":
                {
                    foreach (var svc in new[] { "bthserv", "BTAGService", "Audiosrv", "AudioEndpointBuilder" })
                    {
                        var r = TweakJournal.RunProcess("powershell.exe", $"-NoProfile -Command \"Restart-Service {svc} -Force -ErrorAction SilentlyContinue\"", 40_000);
                        Add($"{svc}: {(r.ExitCode == 0 ? "OK" : r.Error.Trim())}");
                    }
                    return (true, log.ToString());
                }

                case "defender":
                {
                    var a = TweakJournal.RunPowerShell("Set-MpPreference -DisableRealtimeMonitoring $false -ErrorAction SilentlyContinue; Add-MpPreference -EnableControlledFolderAccess Disabled -ErrorAction SilentlyContinue", 60_000);
                    RestoreService("WinDefend", 2);
                    RestoreService("WdNisSvc", 3);
                    RestoreService("SecurityHealthService", 3);
                    TweakJournal.RunProcess("bcdedit.exe", "/deletevalue {current} disableelamdrivers", 10_000);
                    Add(Loc.Pick("Defender, ELAM e serviços de segurança restaurados ao padrão.", "Defender, ELAM and security services restored to default."));
                    if (!string.IsNullOrWhiteSpace(a.Error)) Add(a.Error.Trim());
                    return (true, log.ToString());
                }

                case "store":
                {
                    var r = TweakJournal.RunProcess("wsreset.exe", "-i", 120_000);
                    Add(Loc.Pick("Store re-registrada.", "Store re-registered."));
                    return (true, log.ToString());
                }

                case "time":
                {
                    var r = TweakJournal.RunProcess("w32tm.exe", "/resync /force", 40_000);
                    Add(r.Output.Trim() + r.Error.Trim());
                    return (r.ExitCode == 0, log.ToString());
                }

                case "restorepoint":
                {
                    var (ok, msg) = TweakJournal.CreateRestorePoint();
                    Add(msg);
                    return (ok, log.ToString());
                }

                default:
                    return (false, Loc.Pick("Ação desconhecida.", "Unknown action."));
            }
        });
    }

    private static void RestoreService(string name, int start)
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{name}", true);
            k?.SetValue("Start", start, RegistryValueKind.DWord);
        }
        catch { }
    }

    /// <summary>Remove temporariamente a política que bloqueia a fonte do DISM (e devolve o backup).</summary>
    private static (bool Lifted, string? Backup) LiftWuPolicy()
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", true);
            if (k == null) return (false, null);
            object? disable = k.GetValue("DisableWindowsUpdateAccess");
            if (disable == null) return (false, null);
            k.DeleteValue("DisableWindowsUpdateAccess", false);
            return (true, disable.ToString());
        }
        catch { return (false, null); }
    }

    private static void RestoreWuPolicy(string? backup)
    {
        if (backup == null) return;
        try
        {
            using var k = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", true);
            if (int.TryParse(backup, out int v)) k?.SetValue("DisableWindowsUpdateAccess", v, RegistryValueKind.DWord);
        }
        catch { }
    }

    public static string Summary(List<DoctorFinding> findings)
    {
        int crit = findings.Count(f => f.Severity == FindingSeverity.Critical);
        int warn = findings.Count(f => f.Severity == FindingSeverity.Warning);
        var sb = new StringBuilder();
        sb.AppendLine(Loc.Pick($"Detector de problemas: {crit} crítico(s), {warn} aviso(s).", $"Problem detector: {crit} critical, {warn} warning(s)."));
        foreach (var f in findings)
            sb.AppendLine($"[{f.SeverityLabel}] {f.Area} — {f.Title}: {f.Detail}");
        return sb.ToString();
    }
}
