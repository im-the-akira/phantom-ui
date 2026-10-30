using System.Management;
using System.Text;
using Microsoft.Win32;

namespace PhantomUI;

public enum HardwareVendor { Unknown, Intel, Amd, Nvidia }

/// <summary>Retrato completo da máquina usado pelo Auto-Optimizer para escolher apenas o que é seguro.</summary>
public sealed class HardwareProfile
{
    public string CpuName { get; set; } = "";
    public HardwareVendor CpuVendor { get; set; } = HardwareVendor.Unknown;
    public int PhysicalCores { get; set; }
    public int LogicalCores { get; set; }
    public bool HybridCpu { get; set; }
    public string CpuCodename { get; set; } = "";

    public string GpuName { get; set; } = "";
    public HardwareVendor GpuVendor { get; set; } = HardwareVendor.Unknown;
    public long VramMb { get; set; }
    public bool ReBarSupported { get; set; }
    public bool ReBarEnabled { get; set; }
    public bool IsIntegratedOnly { get; set; }

    public double RamGb { get; set; }
    public int RamSpeedMhz { get; set; }
    public int RamChannels { get; set; }
    public string RamType { get; set; } = "";

    public bool HasNvme { get; set; }
    public bool HasSsd { get; set; }
    public bool HasHdd { get; set; }
    public string SystemDiskMedia { get; set; } = "";

    public bool IsLaptop { get; set; }
    public bool OnBattery { get; set; }
    public bool SecureBoot { get; set; }
    public bool TpmPresent { get; set; }
    public string OsVersion { get; set; } = "";
    public string BoardVendor { get; set; } = "";
    public string BoardName { get; set; } = "";
    public string BiosVersion { get; set; } = "";
    public DateTime? BiosDate { get; set; }
    public int WindowsBuild { get; set; }
    public List<string> AntiCheats { get; set; } = new();

    public bool IsIntelCpu => CpuVendor == HardwareVendor.Intel;
    public bool IsAmdCpu => CpuVendor == HardwareVendor.Amd;
    public bool IsNvidia => GpuVendor == HardwareVendor.Nvidia;
    public bool IsAmdGpu => GpuVendor == HardwareVendor.Amd;
    public bool IsIntelGpu => GpuVendor == HardwareVendor.Intel;
    public bool LowRam => RamGb <= 12.5;
    public bool HighRam => RamGb >= 15.5;

    public string RamLabel => RamGb <= 0 ? "—" : $"{RamGb:0} GB" +
        (RamSpeedMhz > 0 ? $" @ {RamSpeedMhz} MHz" : "") +
        (RamChannels > 0 ? (RamChannels >= 2 ? $" · Dual/Quad Channel ({RamChannels}x)" : " · Single Channel") : "");

    public string FormFactorLabel => IsLaptop
        ? Loc.Pick("Notebook / Laptop", "Laptop")
        : Loc.Pick("Desktop", "Desktop");

    public string CpuLabel => $"{CpuName} ({PhysicalCores}C/{LogicalCores}T)" + (HybridCpu ? " · Híbrida P+E" : "");

    public string GpuLabel => $"{GpuName}" + (VramMb > 0 ? $" · {VramMb / 1024.0:0.#} GB VRAM" : "");

    public string StorageLabel
    {
        get
        {
            var parts = new List<string>();
            if (HasNvme) parts.Add("NVMe");
            if (HasSsd) parts.Add("SSD SATA");
            if (HasHdd) parts.Add("HDD");
            return parts.Count == 0 ? "—" : string.Join(" + ", parts) + $" · {Loc.Pick("sistema em", "system on")} {SystemDiskMedia}";
        }
    }
}

/// <summary>
/// Analisador de hardware + gerador de perfis seguros.
/// Nada é aplicado sem passar por aqui: cada sugestão é filtrada por fornecedor, RAM, disco e anti-cheat.
/// </summary>
public static class HardwareAutoOptimizer
{
    public static HardwareProfile Current { get; private set; } = new();
    private static readonly object _lock = new();

    public static HardwareProfile Detect(bool force = false)
    {
        lock (_lock)
        {
            if (!force && !string.IsNullOrEmpty(Current.CpuName)) return Current;
            var p = new HardwareProfile();
            try
            {
                // ---------- CPU ----------
                using (var searcher = new ManagementObjectSearcher("SELECT Name, NumberOfCores, NumberOfLogicalProcessors, Description FROM Win32_Processor"))
                {
                    foreach (var mo in searcher.Get().Cast<ManagementObject>())
                    {
                        p.CpuName = (mo["Name"] as string ?? "").Trim();
                        p.PhysicalCores = Convert.ToInt32(mo["NumberOfCores"] ?? 0);
                        p.LogicalCores = Convert.ToInt32(mo["NumberOfLogicalProcessors"] ?? 0);
                        break;
                    }
                }
                p.CpuVendor = p.CpuName.Contains("Intel", StringComparison.OrdinalIgnoreCase) ? HardwareVendor.Intel
                            : p.CpuName.Contains("AMD", StringComparison.OrdinalIgnoreCase) ? HardwareVendor.Amd
                            : HardwareVendor.Unknown;
                p.HybridCpu = p.CpuVendor == HardwareVendor.Intel
                    ? p.CpuName.Contains("Core Ultra", StringComparison.OrdinalIgnoreCase) || p.CpuName.Contains("12th Gen", StringComparison.OrdinalIgnoreCase)
                      || p.CpuName.Contains("13th Gen", StringComparison.OrdinalIgnoreCase) || p.CpuName.Contains("14th Gen", StringComparison.OrdinalIgnoreCase)
                    : p.CpuName.Contains("Ryzen 7 7", StringComparison.OrdinalIgnoreCase) || p.CpuName.Contains("Ryzen 9 7", StringComparison.OrdinalIgnoreCase)
                      || p.CpuName.Contains("Ryzen 5 7", StringComparison.OrdinalIgnoreCase);

                // ---------- GPU ----------
                using (var searcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM, PNPDeviceID FROM Win32_VideoController"))
                {
                    foreach (var mo in searcher.Get().Cast<ManagementObject>())
                    {
                        string name = (mo["Name"] as string ?? "").Trim();
                        if (name.Length == 0) continue;
                        bool dedicated = name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)
                                      || name.Contains("Radeon RX", StringComparison.OrdinalIgnoreCase)
                                      || name.Contains("Arc", StringComparison.OrdinalIgnoreCase);
                        if (p.GpuName.Length == 0 || dedicated)
                        {
                            p.GpuName = name;
                            p.VramMb = mo["AdapterRAM"] != null ? Convert.ToInt64(mo["AdapterRAM"]) / 1048576 : 0;
                            if (!dedicated && p.VramMb <= 0) p.VramMb = 0;
                        }
                        if (dedicated) break;
                    }
                }
                // VRAM real via registro do driver (AdapterRAM do WMI satura em 4 GB)
                try
                {
                    using var cls = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000");
                    if (cls?.GetValue("HardwareInformation.qwMemorySize") is long qw && qw > 0) p.VramMb = qw / 1048576;
                }
                catch { }

                p.GpuVendor = p.GpuName.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ? HardwareVendor.Nvidia
                            : p.GpuName.Contains("AMD", StringComparison.OrdinalIgnoreCase) || p.GpuName.Contains("Radeon", StringComparison.OrdinalIgnoreCase) ? HardwareVendor.Amd
                            : p.GpuName.Contains("Intel", StringComparison.OrdinalIgnoreCase) ? HardwareVendor.Intel
                            : HardwareVendor.Unknown;
                p.IsIntegratedOnly = p.GpuVendor == HardwareVendor.Intel && !p.GpuName.Contains("Arc", StringComparison.OrdinalIgnoreCase);
                p.ReBarSupported = p.GpuVendor is HardwareVendor.Nvidia or HardwareVendor.Amd
                                   && (p.GpuName.Contains("RTX", StringComparison.OrdinalIgnoreCase)
                                       || p.GpuName.Contains("RX 6", StringComparison.OrdinalIgnoreCase)
                                       || p.GpuName.Contains("RX 7", StringComparison.OrdinalIgnoreCase)
                                       || p.GpuName.Contains("Arc", StringComparison.OrdinalIgnoreCase));
                try
                {
                    using var rb = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000");
                    p.ReBarEnabled = rb?.GetValue("EnableResizableBar") is int v && v == 1;
                }
                catch { }

                // ---------- RAM ----------
                using (var searcher = new ManagementObjectSearcher("SELECT Capacity, Speed, ConfiguredClockSpeed, SMBIOSMemoryType, DeviceLocator, BankLabel FROM Win32_PhysicalMemory"))
                {
                    var modules = searcher.Get().Cast<ManagementObject>().ToList();
                    p.RamGb = modules.Sum(m => m["Capacity"] != null ? Convert.ToDouble(m["Capacity"]) / 1073741824.0 : 0);
                    if (p.RamGb <= 0) p.RamGb = GetTotalRamGb();
                    p.RamChannels = modules.Count;
                    var speeds = modules.Select(m => m["ConfiguredClockSpeed"] ?? m["Speed"]).Where(v => v != null).Select(Convert.ToInt32).ToList();
                    p.RamSpeedMhz = speeds.Count > 0 ? speeds.Max() : 0;
                    int type = modules.Count > 0 && modules[0]["SMBIOSMemoryType"] != null ? Convert.ToInt32(modules[0]["SMBIOSMemoryType"]) : 0;
                    p.RamType = type switch { 26 => "DDR4", 34 => "DDR5", 24 => "DDR3", 21 => "DDR2", _ => "" };
                }

                // ---------- Discos ----------
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT Model, MediaType, InterfaceType, Size FROM Win32_DiskDrive");
                    foreach (var mo in searcher.Get().Cast<ManagementObject>())
                    {
                        string model = (mo["Model"] as string ?? "");
                        string iface = (mo["InterfaceType"] as string ?? "");
                        int media = mo["MediaType"] != null ? Convert.ToInt32(mo["MediaType"]) : 0;
                        bool isNvme = model.Contains("NVMe", StringComparison.OrdinalIgnoreCase) || iface.Contains("NVMe", StringComparison.OrdinalIgnoreCase);
                        bool isHdd = media == 3 || model.Contains("HDD", StringComparison.OrdinalIgnoreCase) || iface.Contains("IDE", StringComparison.OrdinalIgnoreCase);
                        if (isNvme) p.HasNvme = true;
                        else if (isHdd) p.HasHdd = true;
                        else p.HasSsd = true;
                    }
                }
                catch { }

                // mídia do disco do sistema
                try
                {
                    string sysDrive = System.IO.Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
                    using var part = new ManagementObjectSearcher($"ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{sysDrive.TrimEnd('\\')}'}} WHERE AssocClass=Win32_LogicalDiskToPartition");
                    foreach (var pm in part.Get().Cast<ManagementObject>())
                    {
                        using var drive = new ManagementObjectSearcher($"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{pm["DeviceID"]}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition");
                        foreach (var dm in drive.Get().Cast<ManagementObject>())
                        {
                            p.SystemDiskMedia = (dm["Model"] as string ?? "").Trim();
                            break;
                        }
                        break;
                    }
                }
                catch { }
                if (string.IsNullOrEmpty(p.SystemDiskMedia))
                    p.SystemDiskMedia = p.HasNvme ? "NVMe" : p.HasSsd ? "SSD" : "HDD";

                // ---------- Chassi / bateria ----------
                try
                {
                    using var cs = new ManagementObjectSearcher("SELECT PCSystemType, Manufacturer, Model FROM Win32_ComputerSystem");
                    foreach (var mo in cs.Get().Cast<ManagementObject>())
                    {
                        int sysType = mo["PCSystemType"] != null ? Convert.ToInt32(mo["PCSystemType"]) : 1;
                        p.IsLaptop = sysType == 2;
                        break;
                    }
                    if (!p.IsLaptop)
                    {
                        using var bq = new ManagementObjectSearcher("SELECT Status FROM Win32_Battery");
                        p.IsLaptop = bq.Get().Count > 0;
                    }
                    using var bat = new ManagementObjectSearcher("SELECT BatteryStatus FROM Win32_Battery");
                    foreach (var mo in bat.Get().Cast<ManagementObject>())
                    {
                        int st = mo["BatteryStatus"] != null ? Convert.ToInt32(mo["BatteryStatus"]) : 2;
                        p.OnBattery = st != 2;
                        break;
                    }
                }
                catch { }

                // ---------- Placa-mãe / BIOS ----------
                try
                {
                    using var bb = new ManagementObjectSearcher("SELECT Manufacturer, Product, Version, ReleaseDate FROM Win32_BaseBoard");
                    foreach (var mo in bb.Get().Cast<ManagementObject>())
                    {
                        p.BoardVendor = (mo["Manufacturer"] as string ?? "").Trim();
                        p.BoardName = (mo["Product"] as string ?? "").Trim();
                        break;
                    }
                    using var bios = new ManagementObjectSearcher("SELECT SMBIOSBIOSVersion, ReleaseDate FROM Win32_BIOS");
                    foreach (var mo in bios.Get().Cast<ManagementObject>())
                    {
                        p.BiosVersion = (mo["SMBIOSBIOSVersion"] as string ?? "").Trim();
                        string raw = mo["ReleaseDate"] as string ?? "";
                        if (raw.Length >= 8 && DateTime.TryParseExact(raw[..8], "yyyyMMdd", null,
                                System.Globalization.DateTimeStyles.None, out var d))
                            p.BiosDate = d;
                        break;
                    }
                }
                catch { }

                // ---------- Segurança / OS ----------
                try
                {
                    using var fw = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State");
                    p.SecureBoot = Convert.ToInt32(fw?.GetValue("UEFISecureBootEnabled") ?? 0) == 1;
                }
                catch { }
                try
                {
                    using var tpm = new ManagementObjectSearcher("SELECT SpecVersion FROM Win32_Tpm");
                    p.TpmPresent = tpm.Get().Count > 0;
                }
                catch { }
                p.OsVersion = Environment.OSVersion.VersionString;
                p.WindowsBuild = Environment.OSVersion.Version.Build;
                p.AntiCheats = DetectedAntiCheats();
            }
            catch (Exception ex)
            {
                p.CpuName = string.IsNullOrEmpty(p.CpuName) ? "?" : p.CpuName;
                System.Diagnostics.Debug.WriteLine("HardwareAutoOptimizer: " + ex.Message);
            }
            if (p.RamGb <= 0) p.RamGb = GetTotalRamGb();
            Current = p;
            return p;
        }
    }

    private static double GetTotalRamGb()
    {
        try { return InputLagTools.GetLiveMemoryAndTimer().TotalMb / 1024.0; }
        catch { return 0; }
    }

    /// <summary>Anti-cheats instalados — qualquer um deles desativa automaticamente o que quebra o jogo.</summary>
    public static List<string> DetectedAntiCheats()
    {
        var found = new List<string>();
        void CheckFile(string path, string label)
        {
            try { if (System.IO.File.Exists(path)) found.Add(label); } catch { }
        }
        CheckFile(@"C:\Program Files\Riot Vanguard\vgk.sys", "Vanguard (Valorant)");
        CheckFile(@"C:\Windows\System32\drivers\vgk.sys", "Vanguard (Valorant)");
        CheckFile(@"C:\Windows\System32\drivers\faceit.sys", "FACEIT AC");
        CheckFile(@"C:\Program Files\FACEIT AC\faceit.sys", "FACEIT AC");
        CheckFile(@"C:\Windows\System32\drivers\EasyAntiCheat.sys", "EasyAntiCheat (EAC)");
        CheckFile(@"C:\Program Files (x86)\EasyAntiCheat\EasyAntiCheat.exe", "EasyAntiCheat (EAC)");
        CheckFile(@"C:\Windows\System32\drivers\BEDaisy.sys", "BattlEye (BEDaisy)");
        CheckFile(@"C:\Program Files (x86)\Common Files\BattlEye\BEDaisy.sys", "BattlEye (BEDaisy)");
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services");
            if (k != null)
            {
                foreach (var n in k.GetSubKeyNames())
                {
                    if (n.Equals("vgk", StringComparison.OrdinalIgnoreCase) && !found.Any(f => f.Contains("Vanguard"))) found.Add("Vanguard (Valorant)");
                    if (n.Equals("FACEIT", StringComparison.OrdinalIgnoreCase) && !found.Any(f => f.Contains("FACEIT"))) found.Add("FACEIT AC");
                    if (n.StartsWith("EasyAntiCheat", StringComparison.OrdinalIgnoreCase) && !found.Any(f => f.Contains("EAC"))) found.Add("EasyAntiCheat (EAC)");
                    if (n.StartsWith("BEService", StringComparison.OrdinalIgnoreCase) && !found.Any(f => f.Contains("BattlEye"))) found.Add("BattlEye (BEDaisy)");
                }
            }
        }
        catch { }
        return found.Distinct().ToList();
    }

    /// <summary>Diagnóstico em texto do hardware + avisos do que está fora do ideal.</summary>
    public static string BuildDiagnostics(HardwareProfile? p = null)
    {
        p ??= Detect();
        var sb = new StringBuilder();
        sb.AppendLine(Loc.Pick("=== DIAGNÓSTICO DE HARDWARE ===", "=== HARDWARE DIAGNOSTICS ==="));
        sb.AppendLine($"{Loc.Pick("CPU", "CPU")}: {p.CpuName}");
        sb.AppendLine($"   {p.PhysicalCores}C / {p.LogicalCores}T · {p.CpuVendor}" + (p.HybridCpu ? " · " + Loc.Pick("híbrida (P+E)", "hybrid (P+E)") : ""));
        sb.AppendLine($"{Loc.Pick("GPU", "GPU")}: {p.GpuName} · {p.VramMb / 1024.0:0.#} GB · {p.GpuVendor}");
        sb.AppendLine($"{Loc.Pick("RAM", "RAM")}: {p.RamLabel} {p.RamType}");
        sb.AppendLine($"{Loc.Pick("Discos", "Disks")}: {p.StorageLabel}");
        sb.AppendLine($"{Loc.Pick("Formato", "Form factor")}: {p.FormFactorLabel}" + (p.IsLaptop ? (p.OnBattery ? Loc.Pick(" · na bateria", " · on battery") : Loc.Pick(" · na tomada", " · plugged in")) : ""));
        sb.AppendLine($"{Loc.Pick("Placa-mãe", "Motherboard")}: {p.BoardVendor} {p.BoardName}");
        sb.AppendLine($"BIOS: {p.BiosVersion}" + (p.BiosDate.HasValue ? $" ({p.BiosDate:MM/yyyy})" : ""));
        sb.AppendLine($"Secure Boot: {(p.SecureBoot ? "ON" : "OFF")} · TPM: {(p.TpmPresent ? "ON" : "OFF")} · ReBAR: {(p.ReBarSupported ? (p.ReBarEnabled ? "ON" : "OFF") : "n/a")}");
        sb.AppendLine($"{Loc.Pick("Anti-cheats", "Anti-cheats")}: {(p.AntiCheats.Count == 0 ? Loc.Pick("nenhum", "none") : string.Join(", ", p.AntiCheats))}");
        sb.AppendLine();
        sb.AppendLine(Loc.Pick("=== AVISOS ===", "=== WARNINGS ==="));

        var warns = new List<string>();
        if (p.RamChannels == 1 && p.RamGb >= 8)
            warns.Add(Loc.Pick("RAM em canal único: perde de 10 a 20% de FPS. Instale um segundo pente igual (dual channel).",
                               "Single-channel RAM: loses 10-20% FPS. Install a matching second stick (dual channel)."));
        if (p.RamSpeedMhz > 0 && p.RamSpeedMhz < 2400)
            warns.Add(Loc.Pick("RAM abaixo de 2400 MHz: ative XMP/EXPO na BIOS (perfil 1) e confirme o clock no Windows.",
                               "RAM below 2400 MHz: enable XMP/EXPO in BIOS (profile 1) and confirm the clock in Windows."));
        if (p.HasHdd && !p.HasNvme && !p.HasSsd)
            warns.Add(Loc.Pick("Windows em HDD: nenhum tweak substitui um SSD. Este é o gargalo real de carregamento.",
                               "Windows on HDD: no tweak replaces an SSD. This is the real loading bottleneck."));
        if (p.HasHdd && p.HasSsd)
            warns.Add(Loc.Pick("HDD presente: mantenha o Superfetch LIGADO para esse disco (o Auto não desativa).",
                               "HDD present: keep Superfetch ON for that disk (Auto never disables it)."));
        if (p.ReBarSupported && !p.ReBarEnabled)
            warns.Add(Loc.Pick("GPU suporta Resizable BAR mas está desligado: ative 'Above 4G Decoding' + 'Re-Size BAR Support' na BIOS.",
                               "GPU supports Resizable BAR but it is off: enable 'Above 4G Decoding' + 'Re-Size BAR Support' in BIOS."));
        if (!p.SecureBoot)
            warns.Add(Loc.Pick("Secure Boot desligado: alguns anti-cheats (Vanguard/FACEIT) exigem Secure Boot + TPM 2.0.",
                               "Secure Boot off: some anti-cheats (Vanguard/FACEIT) require Secure Boot + TPM 2.0."));
        if (!p.TpmPresent)
            warns.Add(Loc.Pick("TPM 2.0 não detectado: ative PTT (Intel) ou fTPM (AMD) na BIOS.",
                               "TPM 2.0 not detected: enable PTT (Intel) or fTPM (AMD) in BIOS."));
        if (p.BiosDate.HasValue && (DateTime.Today - p.BiosDate.Value).TotalDays > 730)
            warns.Add(Loc.Pick($"BIOS de {p.BiosDate:MM/yyyy} — mais de 2 anos. Atualize pela aba BIOS/Placa-mãe (fonte oficial do fabricante).",
                               $"BIOS from {p.BiosDate:MM/yyyy} — over 2 years old. Update from the BIOS/Motherboard page (official vendor source)."));
        if (p.LowRam)
            warns.Add(Loc.Pick("RAM <= 12 GB: mantenha a Compactação de Memória LIGADA e o pagefile no automático.",
                               "RAM <= 12 GB: keep Memory Compression ON and leave the pagefile on automatic."));
        if (p.IsLaptop)
            warns.Add(Loc.Pick("Notebook: Core Parking, Ultimate Performance e ASPM ficam fora do modo automático (só na tomada).",
                               "Laptop: Core Parking, Ultimate Performance and ASPM stay out of auto mode (plugged in only)."));
        if (p.AntiCheats.Count > 0)
            warns.Add(Loc.Pick($"Anti-cheat detectado ({string.Join(", ", p.AntiCheats)}): HVCI, hypervisor e integridade ficam travados — mexer derruba o jogo.",
                               $"Anti-cheat detected ({string.Join(", ", p.AntiCheats)}): HVCI, hypervisor and integrity are locked — changing them breaks the game."));
        if (warns.Count == 0)
            warns.Add(Loc.Pick("Nenhum gargalo crítico detectado. Aplicar o perfil Balanceado é seguro.",
                               "No critical bottleneck detected. Applying the Balanced profile is safe."));
        foreach (var w in warns) sb.AppendLine("• " + w);
        return sb.ToString();
    }

    // =====================================================================
    // PERFIS
    // =====================================================================
    public sealed record OptimizationProfile(string Id, string NamePt, string NameEn, string DescPt, string DescEn)
    {
        public string Name => Loc.Pick(NamePt, NameEn);
        public string Desc => Loc.Pick(DescPt, DescEn);
        public override string ToString() => Name;
    }

    public static List<OptimizationProfile> Profiles { get; } = new()
    {
        new("BalancedRecommended", "Balanceado (Recomendado)", "Balanced (Recommended)",
            "Só o que é seguro e reversível: MMCSS validado, timer global, rede, mouse 1:1, Game DVR off, shader cache, menus rápidos. Nada de risco alto.",
            "Only what is safe and reversible: validated MMCSS, global timer, network, 1:1 mouse, Game DVR off, shader cache, fast menus. No high risk."),
        new("CompetitiveFps", "Competitivo FPS", "Competitive FPS",
            "Foco em latência: perfil acima + HAGS, SwapEffectUpgrade, timer 0,5 ms em jogo, QoS DSCP, prioridade de processo, NVIDIA/AMD ajustes de estado.",
            "Latency focused: the profile above plus HAGS, SwapEffectUpgrade, 0.5 ms in-game timer, QoS DSCP, process priority, NVIDIA/AMD state tweaks."),
        new("PureStability", "Estabilidade Pura", "Pure Stability",
            "Corrige o que pacotes de tweak quebraram: restaura MMCSS, remove afinidades fixas, garante TRIM, desliga Inicialização Rápida e limpa caches corrompidos. Não busca FPS.",
            "Fixes what tweak packs broke: restores MMCSS, removes pinned affinities, ensures TRIM, disables Fast Startup and clears corrupt caches. Not chasing FPS."),
        new("MaxPrivacy", "Privacidade Máxima", "Max Privacy",
            "Telemetria mínima, DiagTrack/WAP/Maps desligados, Copilot e Recall off, ID de publicidade off, busca sem nuvem. Sem impacto em jogos.",
            "Minimum telemetry, DiagTrack/WAP/Maps off, Copilot and Recall off, ad ID off, no cloud search. No gaming impact.")
    };

    private static readonly Dictionary<string, string[]> ProfileTweaks = new()
    {
        ["BalancedRecommended"] = new[]
        {
            "os.priority-separation", "os.large-system-cache", "os.ntfs-last-access", "os.prefetch-keep",
            "os.no-tips", "os.search-no-web", "os.startup-delay", "os.foreground-lock", "os.auto-end-tasks",
            "os.dwm-buffered-paint", "os.do-lan-only",
            "menu.show-delay", "menu.hung-timeout", "menu.no-widgets", "menu.no-chat", "menu.search-icon-only",
            "menu.explorer-this-pc", "menu.disable-animations",
            "cpu.mmcss-gpu-priority", "cpu.mmcss-priority", "cpu.mmcss-scheduling", "cpu.mmcss-affinity-free",
            "cpu.global-timer-resolution", "cpu.system-responsiveness", "cpu.network-throttling",
            "gpu.gamedvr-off", "gpu.game-mode", "gpu.shader-cache", "gpu.swap-effect-upgrade",
            "ram.compression-on", "ram.clear-pagefile",
            "disk.trim", "disk.no-last-access", "disk.fast-startup-off",
            "input.mouse-accel-off", "input.filter-keys-off", "input.keyboard-repeat-fast", "input.kbd-data-queue",
            "net.task-offload", "net.tcp-nodelay",
            "games.gamedvr-off", "games.game-mode-on", "games.shader-precompile",
            "fixes.mmcss-reset", "fixes.mpo-flicker", "fixes.time-sync"
        },
        ["CompetitiveFps"] = new[]
        {
            "gpu.hags", "gpu.preemption", "gpu.global-high-perf", "gpu.vrr",
            "cpu.tsc-sync", "cpu.perf-boost-aggressive",
            "input.mouse-pointer-precision", "input.mouse-data-queue", "input.hid-power-off",
            "net.tcp-nagle-off", "net.qos-dscp46", "net.rss-on",
            "games.fse-off", "games.game-bar-off", "games.hags-check", "games.windowed-optimizations",
            "games.low-latency-pipeline", "games.steam-shader-cache"
        },
        ["PureStability"] = new[]
        {
            "fixes.mmcss-reset", "cpu.mmcss-affinity-free", "disk.trim", "disk.fast-startup-off",
            "ram.compression-on", "os.prefetch-keep", "ram.superfetch-hdd", "disk.superfetch-hdd",
            "gpu.tdr-delay", "fixes.mpo-flicker", "fixes.font-cache", "fixes.time-sync",
            "os.no-autoplay", "menu.hung-timeout"
        },
        ["MaxPrivacy"] = new[]
        {
            "privacy.telemetry-min", "privacy.advertising-id", "privacy.activity-feed", "privacy.feedback-off",
            "privacy.diagtrack", "privacy.dmwappushservice", "privacy.maps-broker", "privacy.retail-demo",
            "privacy.cortana-off", "privacy.location-off", "privacy.cloud-search",
            "privacy.disable-consumer-features", "privacy.disable-store-promo", "privacy.speech-off",
            "ai.copilot-off", "ai.recall-off", "ai.click-to-do-off", "ai.bing-search-off",
            "ai.windows-copilot-taskbar", "ai.settings-agent-off", "ai.text-suggestions", "ai.education-tips",
            "os.no-tips", "menu.no-recent-docs", "menu.no-recent-programs", "os.search-no-web"
        }
    };

    /// <summary>Tweaks que o hardware atual permite aplicar no modo automático.</summary>
    public static List<TweakEntry> AutoEligible(HardwareProfile p, string profileId)
    {
        var tags = HardwareTagsFor(p);
        var wanted = ProfileTweaks.TryGetValue(profileId, out var ids) ? ids : Array.Empty<string>();
        var result = new List<TweakEntry>();

        foreach (var t in TweakCatalog.All)
        {
            bool inProfile = wanted.Contains(t.Id);
            // no modo automático entram os tweaks do perfil + todos os "Low" compatíveis com o hardware
            bool autoLow = t.AutoEligible && t.Risk == TweakRisk.Low && Matches(t.Tags, tags);
            if ((inProfile && Matches(t.Tags, tags)) || (profileId == "BalancedRecommended" && autoLow))
            {
                if (t.IsBlocked) continue;
                if (t.Risk >= TweakRisk.High) continue;
                if (!Matches(t.Tags, tags)) continue;
                result.Add(t);
            }
        }
        return result.Distinct().ToList();
    }

    private static TweakTags HardwareTagsFor(HardwareProfile p)
    {
        var f = TweakTags.None;
        if (p.IsIntelCpu) f |= TweakTags.Intel;
        if (p.IsAmdCpu) f |= TweakTags.Amd;
        if (p.IsNvidia) f |= TweakTags.Nvidia;
        if (p.IsAmdGpu) f |= TweakTags.AmdGpu;
        if (p.IsIntelGpu) f |= TweakTags.IntelGpu;
        if (p.HasSsd || p.HasNvme) f |= TweakTags.Ssd;
        if (p.HasHdd) f |= TweakTags.Hdd;
        if (p.IsLaptop) f |= TweakTags.Laptop; else f |= TweakTags.Desktop;
        if (p.LowRam) f |= TweakTags.LowRam;
        if (p.HighRam) f |= TweakTags.HighRam;
        if (p.AntiCheats.Count > 0) f |= TweakTags.AntiCheatSensitive;
        return f;
    }

    /// <summary>Um tweak é compatível se não exigir hardware que a máquina não tem.</summary>
    private static bool Matches(TweakTags required, TweakTags available)
    {
        if (required == TweakTags.None) return true;
        var gating = required & (TweakTags.Intel | TweakTags.Amd | TweakTags.AmdCpu | TweakTags.Nvidia | TweakTags.AmdGpu | TweakTags.IntelGpu
                                 | TweakTags.Ssd | TweakTags.Hdd | TweakTags.Laptop | TweakTags.Desktop | TweakTags.LowRam | TweakTags.HighRam);
        if (gating == TweakTags.None) return true;

        bool anyMatch = false;
        if (gating.HasFlag(TweakTags.Intel) || gating.HasFlag(TweakTags.AmdCpu))
            anyMatch |= available.HasFlag(TweakTags.Intel) || available.HasFlag(TweakTags.Amd);
        if (gating.HasFlag(TweakTags.Nvidia)) anyMatch |= available.HasFlag(TweakTags.Nvidia);
        if (gating.HasFlag(TweakTags.AmdGpu)) anyMatch |= available.HasFlag(TweakTags.AmdGpu);
        if (gating.HasFlag(TweakTags.IntelGpu)) anyMatch |= available.HasFlag(TweakTags.IntelGpu);
        if (gating.HasFlag(TweakTags.Ssd)) anyMatch |= available.HasFlag(TweakTags.Ssd);
        if (gating.HasFlag(TweakTags.Hdd)) anyMatch |= available.HasFlag(TweakTags.Hdd);
        if (gating.HasFlag(TweakTags.Laptop)) anyMatch |= available.HasFlag(TweakTags.Laptop);
        if (gating.HasFlag(TweakTags.Desktop)) anyMatch |= available.HasFlag(TweakTags.Desktop);
        if (gating.HasFlag(TweakTags.LowRam)) anyMatch |= available.HasFlag(TweakTags.LowRam);
        if (gating.HasFlag(TweakTags.HighRam)) anyMatch |= available.HasFlag(TweakTags.HighRam);
        return anyMatch;
    }

    /// <summary>Aplica (ou reverte) um perfil inteiro numa sessão transacional.</summary>
    public static async Task<(int Applied, int Failed, string Message)> ApplyProfileAsync(
        string profileId, bool revert, Action<string>? progress = null)
    {
        var p = Detect();
        var list = AutoEligible(p, profileId);
        if (list.Count == 0)
            return (0, 0, Loc.Pick("Nenhum tweak compatível com este hardware.", "No tweak compatible with this hardware."));

        var prof = Profiles.FirstOrDefault(x => x.Id == profileId);
        string sessionId = revert ? "" : TweakJournal.BeginSession($"Perfil: {prof?.Name ?? profileId}");
        int ok = 0, fail = 0;

        await Task.Run(() =>
        {
            foreach (var t in list)
            {
                progress?.Invoke(t.Title);
                try
                {
                    bool isApplied = t.IsApplied;
                    if (revert == isApplied)
                    {
                        var res = revert ? TweakJournal.RevertTweak(t) : TweakJournal.ApplyTweak(t, sessionId);
                        if (res.Ok) ok++; else fail++;
                    }
                    else ok++; // já está no estado desejado
                }
                catch { fail++; }
            }
        });

        string msg = revert
            ? Loc.Pick($"Perfil revertido: {ok} tweaks restaurados, {fail} falhas.", $"Profile reverted: {ok} tweaks restored, {fail} failures.")
            : Loc.Pick($"Perfil aplicado: {ok} tweaks com backup transacional, {fail} falhas. Sessão {sessionId}.",
                       $"Profile applied: {ok} tweaks with transactional backup, {fail} failures. Session {sessionId}.");
        return (ok, fail, msg);
    }

    /// <summary>Checklist por fabricante para a aba BIOS.</summary>
    public static List<string> BiosChecklist(HardwareProfile p)
    {
        var vendor = p.BoardVendor.ToUpperInvariant();
        var list = new List<string>();
        string kind = vendor.Contains("ASUS") ? "ASUS"
                    : vendor.Contains("MICRO") || vendor.Contains("MSI") ? "MSI"
                    : vendor.Contains("GIGABYTE") ? "GIGABYTE"
                    : vendor.Contains("ASROCK") ? "ASROCK"
                    : vendor.Contains("DELL") || vendor.Contains("LENOVO") || vendor.Contains("HP") ? "OEM"
                    : "GENERIC";
        switch (kind)
        {
            case "ASUS":
                list.Add("F7 = Advanced Mode. Ai Tweaker > Ai Overclock Tuner = XMP I (Intel) / D.O.C.P (AMD).");
                list.Add("Advanced > PCI Subsystem Settings > Above 4G Decoding = Enabled + Re-Size BAR Support = Auto.");
                list.Add("Advanced > CPU Configuration > SVM Mode = Enabled (só se usar VM/WSL2 — Vanguard exige virtualização).");
                list.Add("Boot > Fast Boot = Disabled (evita skip de periféricos USB); Secure Boot > OS Type = Windows UEFI.");
                break;
            case "MSI":
                list.Add("F7 = Advanced. OC > Extreme Memory Profile (XMP) = Enabled.");
                list.Add("Settings > Advanced > PCIe/PCI Subsystem Settings > Re-Size BAR = Enabled + Above 4G = Enabled.");
                list.Add("Settings > Advanced > Integrated Graphics = Disabled (desktop com GPU dedicada).");
                list.Add("Settings > Boot > Fast Boot = Disabled; Secure Boot = Enabled, Standard.");
                break;
            case "GIGABYTE":
                list.Add("F2 = Advanced. Tweaker > Extreme Memory Profile (X.M.P) = Profile1.");
                list.Add("Settings > IO Ports > Above 4G Decoding = Enabled + Re-Size BAR Support = Auto.");
                list.Add("Settings > Miscellaneous > Trusted Computing > Security Device = Enable (fTPM/PTT).");
                list.Add("Boot > Fast Boot = Disabled; CSM Support = Disabled (UEFI puro).");
                break;
            case "ASROCK":
                list.Add("F6 = Advanced Mode. OC Tweaker > Load XMP Setting = XMP 2.0 Profile 1.");
                list.Add("Advanced > Chipset Configuration > Above 4G Decoding = Enabled + Re-Size BAR Support = Enabled.");
                list.Add("Security > Secure Boot = Enabled; CSM = Disabled.");
                break;
            case "OEM":
                list.Add("Notebook/OEM: as opções de memória e ReBAR costumam estar travadas. Atualize a BIOS pelo utilitário oficial do fabricante.");
                list.Add("Procure 'Performance Mode', 'Ultimate Performance' ou 'Cooling Boost' no setup — é o que libera TDP de verdade.");
                list.Add("Verifique se existe opção de 'Discrete GPU only' / 'MUX Switch' para desligar a GPU integrada.");
                break;
            default:
                list.Add("Entre no setup (Del/F2), ative XMP/EXPO/DOCP no perfil 1 e confirme o clock no Windows.");
                list.Add("Ative Above 4G Decoding + Re-Size BAR se a GPU suportar (RTX 30+, RX 6000+).");
                list.Add("Ative PTT (Intel) ou fTPM (AMD) para TPM 2.0 e Secure Boot em modo UEFI.");
                break;
        }
        list.Add(Loc.Pick("NUNCA atualize a BIOS com overclock ativo, na bateria ou com queda de energia prevista.",
                          "NEVER flash the BIOS with an active overclock, on battery, or with a power outage expected."));
        list.Add(Loc.Pick("Salve o perfil atual em 'Save to Profile' antes de qualquer mudança — é seu rollback.",
                          "Save the current settings to 'Save to Profile' before any change — that is your rollback."));
        return list;
    }
}
