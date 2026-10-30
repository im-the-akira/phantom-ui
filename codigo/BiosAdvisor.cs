using System.Management;
using System.Text;
using Microsoft.Win32;

namespace PhantomUI;

/// <summary>Item de diagnóstico da BIOS/UEFI (só leitura — o PHANTOM UI nunca grava na NVRAM).</summary>
public sealed class BiosCheckItem
{
    public string Title { get; init; } = "";
    public string Value { get; init; } = "";
    public string AdvicePt { get; init; } = "";
    public string AdviceEn { get; init; } = "";
    public bool Good { get; init; }
    public bool Warning { get; init; }
    public string Advice => Loc.Pick(AdvicePt, AdviceEn);
    public string StatusIcon => Good ? "✔" : Warning ? "⚠" : "✖";
    public string Color => Good ? "#2ED573" : Warning ? "#FFA502" : "#FF4757";
}

public sealed class BiosReport
{
    public string FirmwareMode { get; set; } = "";
    public string BoardVendor { get; set; } = "";
    public string BoardName { get; set; } = "";
    public string BiosVersion { get; set; } = "";
    public string BiosDateText { get; set; } = "";
    public string SupportUrl { get; set; } = "";
    public List<BiosCheckItem> Items { get; } = new();
    public List<string> Checklist { get; set; } = new();
    public string Summary { get; set; } = "";
}

/// <summary>
/// Diagnóstico de BIOS/UEFI 100% somente-leitura (WMI + registro) com checklist específico por fabricante.
/// Nenhuma escrita em CMOS/NVRAM: o app no máximo reinicia para o setup com confirmação explícita.
/// </summary>
public static class BiosAdvisor
{
    public static BiosReport Analyze(HardwareProfile? hw = null)
    {
        hw ??= HardwareAutoOptimizer.Detect();
        var r = new BiosReport
        {
            BoardVendor = hw.BoardVendor,
            BoardName = hw.BoardName,
            BiosVersion = hw.BiosVersion,
            BiosDateText = hw.BiosDate.HasValue ? hw.BiosDate.Value.ToString("dd/MM/yyyy") : "—"
        };

        r.FirmwareMode = DetectFirmwareMode();
        r.SupportUrl = SupportUrlFor(hw.BoardVendor, hw.BoardName);

        // ---------- UEFI / Secure Boot ----------
        r.Items.Add(new BiosCheckItem
        {
            Title = Loc.Pick("Modo de firmware", "Firmware mode"),
            Value = r.FirmwareMode,
            Good = r.FirmwareMode.Contains("UEFI", StringComparison.OrdinalIgnoreCase),
            Warning = !r.FirmwareMode.Contains("UEFI", StringComparison.OrdinalIgnoreCase),
            AdvicePt = "UEFI é obrigatório para Secure Boot, TPM 2.0, ReBAR e para os anti-cheats modernos. Se está em Legacy/CSM, ative UEFI + desative CSM no setup.",
            AdviceEn = "UEFI is required for Secure Boot, TPM 2.0, ReBAR and modern anti-cheats. If you are on Legacy/CSM, enable UEFI and disable CSM in setup."
        });

        bool secureBootSupported = FirmwareModeHasSecureBoot();
        r.Items.Add(new BiosCheckItem
        {
            Title = "Secure Boot",
            Value = hw.SecureBoot ? Loc.Pick("Ativado", "Enabled") : Loc.Pick("Desativado", "Disabled"),
            Good = hw.SecureBoot,
            Warning = !hw.SecureBoot && !secureBootSupported,
            AdvicePt = "Vanguard (Valorant) e FACEIT exigem Secure Boot + TPM 2.0 em Windows 11. Ative em Security > Secure Boot > Enabled e reinstale as chaves padrão (Restore Factory Keys).",
            AdviceEn = "Vanguard (Valorant) and FACEIT require Secure Boot + TPM 2.0 on Windows 11. Enable it under Security > Secure Boot > Enabled and restore factory keys."
        });

        r.Items.Add(new BiosCheckItem
        {
            Title = "TPM 2.0",
            Value = hw.TpmPresent ? Loc.Pick("Presente", "Present") : Loc.Pick("Ausente", "Missing"),
            Good = hw.TpmPresent,
            AdvicePt = "Ative PTT (Intel Platform Trust Technology) ou fTPM/AMD CPU fTPM. Sem TPM, Windows 11 perde BitLocker e alguns anti-cheats recusam o jogo.",
            AdviceEn = "Enable PTT (Intel Platform Trust Technology) or fTPM/AMD CPU fTPM. Without TPM, Windows 11 loses BitLocker and some anti-cheats refuse the game."
        });

        // ---------- Memória ----------
        bool xmpLikely = hw.RamSpeedMhz == 0 || hw.RamSpeedMhz <= 2666;
        r.Items.Add(new BiosCheckItem
        {
            Title = Loc.Pick("XMP / EXPO / DOCP", "XMP / EXPO / DOCP"),
            Value = hw.RamSpeedMhz > 0 ? $"{hw.RamSpeedMhz} MHz" : Loc.Pick("não lido", "not read"),
            Good = !xmpLikely,
            Warning = xmpLikely,
            AdvicePt = xmpLikely
                ? "O clock de RAM está no padrão JEDEC. Ative o perfil XMP (Intel) / EXPO ou DOCP (AMD) no setup: é o ganho de BIOS mais seguro e mais forte (5-15% em jogos CPU-bound)."
                : "Perfil de memória ativo. Se houver instabilidade, desça um degrau de frequência em vez de desligar o perfil.",
            AdviceEn = xmpLikely
                ? "RAM clock is at JEDEC default. Enable the XMP (Intel) / EXPO or DOCP profile in setup: the safest and strongest BIOS gain (5-15% in CPU-bound games)."
                : "Memory profile is active. If unstable, step one frequency down instead of disabling the profile."
        });

        r.Items.Add(new BiosCheckItem
        {
            Title = Loc.Pick("Canais de memória", "Memory channels"),
            Value = hw.RamChannels > 0 ? $"{hw.RamChannels}x" : "—",
            Good = hw.RamChannels >= 2,
            Warning = hw.RamChannels == 1,
            AdvicePt = "Canal único custa de 10 a 20% de FPS em jogos. Instale dois pentes iguais nos slots A2/B2 (2º e 4º) para dual channel.",
            AdviceEn = "Single channel costs 10-20% FPS in games. Install two identical sticks in slots A2/B2 (2nd and 4th) for dual channel."
        });

        // ---------- ReBAR ----------
        r.Items.Add(new BiosCheckItem
        {
            Title = "Resizable BAR",
            Value = hw.ReBarSupported ? (hw.ReBarEnabled ? Loc.Pick("Ativo", "Enabled") : Loc.Pick("Suportado, desligado", "Supported, disabled")) : Loc.Pick("Não aplicável", "Not applicable"),
            Good = !hw.ReBarSupported || hw.ReBarEnabled,
            Warning = hw.ReBarSupported && !hw.ReBarEnabled,
            AdvicePt = hw.ReBarSupported && !hw.ReBarEnabled
                ? "Ative 'Above 4G Decoding' + 'Re-Size BAR Support' (ASUS/MSI/Gigabyte/ASRock) e ative 'Resizable BAR' no painel do driver. Ganho real em Warzone, BF6, ARC Raiders."
                : "Se a GPU suporta e o monitor do driver mostra BAR ativo, nada a fazer.",
            AdviceEn = hw.ReBarSupported && !hw.ReBarEnabled
                ? "Enable 'Above 4G Decoding' + 'Re-Size BAR Support' (ASUS/MSI/Gigabyte/ASRock) and enable 'Resizable BAR' in the driver panel. Real gains in Warzone, BF6, ARC Raiders."
                : "If the GPU supports it and the driver panel shows BAR active, nothing to do."
        });

        // ---------- Virtualização ----------
        var (vtEnabled, hypervisorPresent) = VirtualizationState();
        r.Items.Add(new BiosCheckItem
        {
            Title = Loc.Pick("Virtualização (VT-x / SVM)", "Virtualization (VT-x / SVM)"),
            Value = vtEnabled ? (hypervisorPresent ? Loc.Pick("Ativa (hipervisor em uso)", "Enabled (hypervisor in use)") : Loc.Pick("Ativa", "Enabled")) : Loc.Pick("Desativada", "Disabled"),
            Good = vtEnabled,
            Warning = !vtEnabled,
            AdvicePt = "Ative Intel VT-x / AMD SVM. Vanguard e o anti-cheat da FACEIT exigem; WSL2, Hyper-V, Sandbox e Emuladores também.",
            AdviceEn = "Enable Intel VT-x / AMD SVM. Vanguard and the FACEIT anti-cheat require it; so do WSL2, Hyper-V, Sandbox and emulators."
        });

        // ---------- Idade da BIOS ----------
        if (hw.BiosDate.HasValue)
        {
            double days = (DateTime.Today - hw.BiosDate.Value).TotalDays;
            r.Items.Add(new BiosCheckItem
            {
                Title = Loc.Pick("Idade da BIOS", "BIOS age"),
                Value = $"{(int)(days / 365)} " + Loc.Pick("anos", "years") + $" ({hw.BiosDate:MM/yyyy})",
                Good = days < 365,
                Warning = days is >= 365 and < 730,
                AdvicePt = days < 365
                    ? "BIOS recente. Nenhuma ação necessária."
                    : "BIOS com mais de um ano. Atualizações trazem AGESA/microcódigo novo, correção de instabilidade de memória e melhor suporte a ReBAR. Baixe SOMENTE do site oficial do fabricante.",
                AdviceEn = days < 365
                    ? "Recent BIOS. No action needed."
                    : "BIOS older than a year. Updates bring new AGESA/microcode, memory instability fixes and better ReBAR support. Download ONLY from the manufacturer's official site."
            });
        }

        // ---------- Fast Boot / CSM ----------
        r.Items.Add(new BiosCheckItem
        {
            Title = Loc.Pick("Fast Boot da BIOS", "BIOS Fast Boot"),
            Value = "—",
            Good = false,
            Warning = true,
            AdvicePt = "Desative o Fast Boot da BIOS: ele pula a inicialização de periféricos USB e é a causa clássica de teclado/mouse sem resposta no boot e de entrada no setup por tecla falhando.",
            AdviceEn = "Disable BIOS Fast Boot: it skips USB device initialization and is the classic cause of unresponsive keyboard/mouse at boot and of missing the setup key."
        });

        r.Checklist = HardwareAutoOptimizer.BiosChecklist(hw);
        r.Summary = BuildSummary(r, hw);
        return r;
    }

    private static string DetectFirmwareMode()
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control");
            object? v = k?.GetValue("PEFirmwareType");
            if (v != null)
                return Convert.ToInt32(v) switch
                {
                    2 => "UEFI",
                    1 => Loc.Pick("Legacy (BIOS)", "Legacy (BIOS)"),
                    _ => "—"
                };
        }
        catch { }
        return Loc.Pick("Indeterminado", "Unknown");
    }

    private static bool FirmwareModeHasSecureBoot()
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State");
            return k != null;
        }
        catch { return false; }
    }

    private static (bool Enabled, bool Hypervisor) VirtualizationState()
    {
        bool vt = false, hv = false;
        try
        {
            using var s = new ManagementObjectSearcher("SELECT VirtualizationFirmwareEnabled, SecondLevelAddressTranslationExtensions FROM Win32_Processor");
            foreach (var mo in s.Get().Cast<ManagementObject>())
            {
                vt = mo["VirtualizationFirmwareEnabled"] is bool b && b;
                break;
            }
        }
        catch { }
        try
        {
            using var cs = new ManagementObjectSearcher("SELECT HypervisorPresent FROM Win32_ComputerSystem");
            foreach (var mo in cs.Get().Cast<ManagementObject>())
            {
                hv = mo["HypervisorPresent"] is bool h && h;
                break;
            }
        }
        catch { }
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard");
            if (k?.GetValue("EnableVirtualizationBasedSecurity") is int v && v == 1) hv = true;
        }
        catch { }
        return (vt || hv, hv);
    }

    private static string SupportUrlFor(string vendor, string board)
    {
        string v = vendor.ToUpperInvariant();
        string model = Uri.EscapeDataString(board);
        if (v.Contains("ASUS")) return $"https://www.asus.com/support/download-center/ (modelo: {board})";
        if (v.Contains("MICRO") || v.Contains("MSI")) return $"https://www.msi.com/support/download/ (modelo: {board})";
        if (v.Contains("GIGABYTE")) return $"https://www.gigabyte.com/Support (modelo: {board})";
        if (v.Contains("ASROCK")) return $"https://www.asrock.com/support/index.asp (modelo: {board})";
        if (v.Contains("DELL")) return "https://www.dell.com/support/home (Service Tag do equipamento)";
        if (v.Contains("LENOVO")) return "https://support.lenovo.com (serial do equipamento)";
        if (v.Contains("HP")) return "https://support.hp.com (serial do equipamento)";
        if (v.Contains("ACER")) return "https://www.acer.com/support";
        return "https://www.microsoft.com/en-us/windows/windows-11-specifications";
    }

    private static string BuildSummary(BiosReport r, HardwareProfile hw)
    {
        int ok = r.Items.Count(i => i.Good);
        int warn = r.Items.Count(i => i.Warning && !i.Good);
        int bad = r.Items.Count(i => !i.Good && !i.Warning);
        var sb = new StringBuilder();
        sb.AppendLine(Loc.Pick(
            $"Placa: {hw.BoardVendor} {hw.BoardName} · BIOS {hw.BiosVersion} ({r.BiosDateText}) · {r.FirmwareMode}",
            $"Board: {hw.BoardVendor} {hw.BoardName} · BIOS {hw.BiosVersion} ({r.BiosDateText}) · {r.FirmwareMode}"));
        sb.AppendLine(Loc.Pick(
            $"Itens OK: {ok} · Ajustar: {warn} · Corrigir: {bad}",
            $"OK items: {ok} · Tune: {warn} · Fix: {bad}"));
        sb.AppendLine();
        sb.AppendLine(Loc.Pick("Arquivo oficial de BIOS/suporte:", "Official BIOS/support page:") + " " + r.SupportUrl);
        sb.AppendLine();
        sb.AppendLine(Loc.Pick("O PHANTOM UI NÃO grava na NVRAM/CMOS. Todas as mudanças abaixo são feitas por você no setup, com o valor exato indicado.",
                               "PHANTOM UI does NOT write to NVRAM/CMOS. Every change below is made by you in setup, with the exact value given."));
        return sb.ToString();
    }

    /// <summary>Reinicia direto no setup do firmware (a única ação de escrita: um flag de boot do Windows).</summary>
    public static (bool Ok, string Message) RebootToFirmware(bool confirmed)
    {
        if (!confirmed)
            return (false, Loc.Pick("Confirmação necessária: a máquina vai reiniciar imediatamente.",
                                    "Confirmation required: the machine will reboot immediately."));
        var r = TweakJournal.RunProcess("shutdown.exe", "/r /fw /t 5 /c \"PhantomUI: reiniciando no setup da BIOS\"", 15_000);
        return r.ExitCode == 0
            ? (true, Loc.Pick("Reiniciando no setup da BIOS em 5 segundos…", "Rebooting to BIOS setup in 5 seconds…"))
            : (false, Loc.Pick("Falha: o firmware precisa suportar /fw ou a Inicialização Rápida está bloqueando. Reinicie manualmente e pressione Del/F2.",
                               "Failed: the firmware must support /fw or Fast Startup is blocking. Reboot manually and press Del/F2."));
    }

    public static string ToText(BiosReport r)
    {
        var sb = new StringBuilder(r.Summary);
        sb.AppendLine();
        sb.AppendLine("=== " + Loc.Pick("DIAGNÓSTICO", "DIAGNOSTICS") + " ===");
        foreach (var i in r.Items)
            sb.AppendLine($"{i.StatusIcon} {i.Title}: {i.Value} — {i.Advice}");
        sb.AppendLine();
        sb.AppendLine("=== " + Loc.Pick("CHECKLIST DO FABRICANTE", "VENDOR CHECKLIST") + " ===");
        for (int i = 0; i < r.Checklist.Count; i++)
            sb.AppendLine($"{i + 1}. {r.Checklist[i]}");
        return sb.ToString();
    }

    public static async Task SaveReportAsync(string content)
    {
        try
        {
            string dir = System.IO.Path.Combine(AppSettings.Folder, "Reports");
            System.IO.Directory.CreateDirectory(dir);
            await System.IO.File.WriteAllTextAsync(System.IO.Path.Combine(dir, $"bios_{DateTime.Now:yyyyMMdd_HHmmss}.txt"), content);
        }
        catch { }
    }
}
