using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using Microsoft.Win32;

namespace PhantomUI;

/// <summary>Nível de risco exibido em cada card da aba Otimização Manual.</summary>
public enum TweakRisk
{
    Low,        // Baixo   — reversível, sem efeito colateral conhecido
    Medium,     // Médio   — ganho real, exige entender o trade-off
    High,       // Alto    — desmarca por padrão no Auto, aviso explícito
    Critical,   // Crítico — só para usuários avançados, pode quebrar funções do Windows
    BlockedHazard // BLOQUEADO POR SEGURANÇA — mito perigoso, nunca aplicável
}

public enum TweakSection
{
    Os, Menu, Cpu, Gpu, Ram, Disk, Input, Net, Power, Games, Privacy, Ai, Wu, Fixes, Blocked
}

/// <summary>Restrições de hardware usadas pelo Auto-Optimizer para escolher apenas tweaks seguros.</summary>
[Flags]
public enum TweakTags
{
    None = 0,
    Intel = 1 << 0,
    Amd = 1 << 1,
    Nvidia = 1 << 2,
    AmdGpu = 1 << 3,
    IntelGpu = 1 << 4,
    AmdCpu = 1 << 5,
    Ssd = 1 << 6,
    Hdd = 1 << 7,
    Laptop = 1 << 8,
    Desktop = 1 << 9,
    LowRam = 1 << 10,   // <= 12 GB
    HighRam = 1 << 11,  // >= 16 GB
    AntiCheatSensitive = 1 << 12,
    RequiresReboot = 1 << 13,
    NoAuto = 1 << 14    // nunca entra no modo automático
}

public sealed record TweakStateInfo(bool Applied, string Detail);

/// <summary>Seção da aba de otimização manual (título bilíngue + ícone).</summary>
public sealed class TweakSectionInfo
{
    public TweakSection Section { get; init; }
    public string TitlePt { get; init; } = "";
    public string TitleEn { get; init; } = "";
    public string Icon { get; init; } = "";
    public string Title => Loc.Pick(TitlePt, TitleEn);
    public ObservableCollection<TweakEntry> Tweaks { get; } = new();
    public int Total => Tweaks.Count;
    public int Applied => Tweaks.Count(t => t.IsApplied);
    public string CountLabel => $"{Applied}/{Total}";
}

/// <summary>
/// Um tweak individual: descrição, comando exato, risco, estado ao vivo e rollback transacional.
/// </summary>
public sealed class TweakEntry : INotifyPropertyChanged
{
    public string Id { get; init; } = "";
    public TweakSection Section { get; init; } = TweakSection.Os;
    public TweakRisk Risk { get; init; } = TweakRisk.Low;
    public TweakTags Tags { get; init; } = TweakTags.None;

    public string TitlePt { get; init; } = "";
    public string TitleEn { get; init; } = "";
    public string DescPt { get; init; } = "";
    public string DescEn { get; init; } = "";

    /// <summary>Texto fixo opcional; se vazio, é gerado a partir das operações reais.</summary>
    public string CommandOverride { get; init; } = "";

    public List<TweakOp> Ops { get; init; } = new();
    public List<TweakOp> FallbackRevertOps { get; init; } = new();

    public Action? CustomApply { get; init; }
    public Action? CustomRevert { get; init; }
    public Func<TweakStateInfo>? DetectFunc { get; init; }

    // ---------- Estado ao vivo ----------
    private bool _isApplied;
    private string _liveState = "—";
    private bool _isSelected;

    public bool IsApplied
    {
        get => _isApplied;
        private set { if (_isApplied == value) return; _isApplied = value; Raise(); Raise(nameof(StateLabel)); }
    }

    public string LiveState
    {
        get => _liveState;
        private set { if (_liveState == value) return; _liveState = value; Raise(); }
    }

    // ---------- Texto para bindings ----------
    public string Title => Loc.Pick(TitlePt, TitleEn);
    public string Desc => Loc.Pick(DescPt, DescEn);

    public string CommandPreview => string.IsNullOrWhiteSpace(CommandOverride)
        ? TweakCatalog.RenderCommands(Ops)
        : CommandOverride;

    public string RiskLabel => Risk switch
    {
        TweakRisk.Low => Loc.Pick("BAIXO", "LOW"),
        TweakRisk.Medium => Loc.Pick("MÉDIO", "MEDIUM"),
        TweakRisk.High => Loc.Pick("ALTO", "HIGH"),
        TweakRisk.Critical => Loc.Pick("CRÍTICO", "CRITICAL"),
        _ => Loc.Pick("BLOQUEADO", "BLOCKED")
    };

    public string RiskColor => Risk switch
    {
        TweakRisk.Low => "#2ED573",
        TweakRisk.Medium => "#FFA502",
        TweakRisk.High => "#FF6B35",
        TweakRisk.Critical => "#FF4757",
        _ => "#7A5CFF"
    };

    /// <summary>Marcação usada pela aba de Otimização Manual (aplicação em lote).</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set { if (_isSelected == value) return; _isSelected = value; Raise(); }
    }

    private static readonly Dictionary<string, System.Windows.Media.Brush> _brushCache = new();
    private static System.Windows.Media.Brush BrushOf(string hex)
    {
        lock (_brushCache)
        {
            if (_brushCache.TryGetValue(hex, out var b)) return b;
            b = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString(hex)!;
            b.Freeze();
            _brushCache[hex] = b;
            return b;
        }
    }

    public System.Windows.Media.Brush RiskBrush => BrushOf(RiskColor);
    public System.Windows.Media.Brush StateBrush => BrushOf(StateColor);

    public string StateLabel => IsApplied
        ? Loc.Pick("ATIVO", "ACTIVE")
        : Loc.Pick("PADRÃO", "DEFAULT");

    public string StateColor => IsApplied ? "#00E5FF" : "#6B7280";

    public bool IsBlocked => Risk == TweakRisk.BlockedHazard;
    public bool RequiresReboot => Tags.HasFlag(TweakTags.RequiresReboot);
    public bool AutoEligible => !IsBlocked && (Risk == TweakRisk.Low || Risk == TweakRisk.Medium) && !Tags.HasFlag(TweakTags.NoAuto);

    public string HardwareHint
    {
        get
        {
            var parts = new List<string>();
            if (Tags.HasFlag(TweakTags.Intel)) parts.Add("Intel");
            if (Tags.HasFlag(TweakTags.AmdCpu)) parts.Add("AMD CPU");
            if (Tags.HasFlag(TweakTags.Nvidia)) parts.Add("NVIDIA");
            if (Tags.HasFlag(TweakTags.AmdGpu)) parts.Add("AMD GPU");
            if (Tags.HasFlag(TweakTags.IntelGpu)) parts.Add("Intel GPU");
            if (Tags.HasFlag(TweakTags.Ssd)) parts.Add("SSD");
            if (Tags.HasFlag(TweakTags.Hdd)) parts.Add("HDD");
            if (Tags.HasFlag(TweakTags.Laptop)) parts.Add("Laptop");
            if (Tags.HasFlag(TweakTags.Desktop)) parts.Add("Desktop");
            if (Tags.HasFlag(TweakTags.LowRam)) parts.Add("RAM <= 12 GB");
            if (Tags.HasFlag(TweakTags.HighRam)) parts.Add("RAM >= 16 GB");
            if (Tags.HasFlag(TweakTags.AntiCheatSensitive)) parts.Add(Loc.Pick("Anti-Cheat", "Anti-Cheat"));
            return string.Join(" · ", parts);
        }
    }

    public void RefreshState()
    {
        try
        {
            if (DetectFunc == null) { LiveState = Loc.Pick("Sem leitura automática", "No automatic reading"); return; }
            var s = DetectFunc();
            IsApplied = s.Applied;
            LiveState = s.Detail;
        }
        catch (Exception ex)
        {
            LiveState = ex.Message;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Raise([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Catálogo completo de otimizações do PHANTOM UI (PT-BR / EN, 170+ tweaks).
/// Cada entrada tem comando visível, risco, leitura de estado e rollback individual.
/// </summary>
public static partial class TweakCatalog
{
    private static readonly Lazy<List<TweakEntry>> _all = new(BuildAll);
    public static List<TweakEntry> All => _all.Value;

    private static readonly Lazy<List<TweakSectionInfo>> _sections = new(() =>
    {
        var defs = new (TweakSection Sec, string Pt, string En, string Icon)[]
        {
            (TweakSection.Os,      "Sistema & Windows",           "OS & Windows",            "🪟"),
            (TweakSection.Menu,    "Menu & Interface",            "Menu & Shell",            "🧭"),
            (TweakSection.Cpu,     "Processador (CPU)",           "Processor (CPU)",         "⚙"),
            (TweakSection.Gpu,     "Placa de Vídeo (GPU)",        "Graphics Card (GPU)",     "🎮"),
            (TweakSection.Ram,     "Memória RAM",                 "Memory (RAM)",            "🧠"),
            (TweakSection.Disk,    "Armazenamento & SSD",         "Storage & SSD",           "💾"),
            (TweakSection.Input,   "Periféricos & Input Lag",     "Peripherals & Input Lag", "🖱"),
            (TweakSection.Net,     "Rede & Latência",             "Network & Latency",       "🌐"),
            (TweakSection.Power,   "Energia & Planos",            "Power & Plans",           "⚡"),
            (TweakSection.Games,   "Jogos & Anti-Cheat",          "Games & Anti-Cheat",      "🕹"),
            (TweakSection.Privacy, "Privacidade & Debloat",       "Privacy & Debloat",       "🛡"),
            (TweakSection.Ai,      "IA, Copilot & Recall",        "AI, Copilot & Recall",    "🤖"),
            (TweakSection.Wu,      "Windows Update",              "Windows Update",          "🔄"),
            (TweakSection.Fixes,   "Correções & Anti-Stutter",    "Fixes & Anti-Stutter",    "🔧"),
            (TweakSection.Blocked, "Mitos Bloqueados por Segurança", "Blocked Destructive Myths", "⛔")
        };
        var list = new List<TweakSectionInfo>();
        foreach (var d in defs)
        {
            var si = new TweakSectionInfo { Section = d.Sec, TitlePt = d.Pt, TitleEn = d.En, Icon = d.Icon };
            foreach (var t in All.Where(x => x.Section == d.Sec)) si.Tweaks.Add(t);
            list.Add(si);
        }
        return list;
    });

    public static List<TweakSectionInfo> Sections => _sections.Value;

    public static void RefreshAllStates()
    {
        foreach (var t in All) t.RefreshState();
    }

    public static IEnumerable<TweakEntry> ForSection(TweakSection s) => All.Where(t => t.Section == s);

    // ==================== Renderização de comandos exatos ====================
    public static string RenderCommands(IReadOnlyList<TweakOp> ops)
    {
        if (ops.Count == 0) return Loc.Pick("(nenhum comando — ajuste informativo)", "(no command — informational)");
        var lines = new List<string>();
        foreach (var op in ops)
        {
            lines.Add(op.Kind switch
            {
                TweakOpKind.RegSet => op.RegKind switch
                {
                    RegistryValueKind.DWord => $"reg add \"{op.Target}\" /v {op.ValueName} /t REG_DWORD /d {op.Value} /f",
                    RegistryValueKind.QWord => $"reg add \"{op.Target}\" /v {op.ValueName} /t REG_QWORD /d {op.Value} /f",
                    RegistryValueKind.Binary => $"reg add \"{op.Target}\" /v {op.ValueName} /t REG_BINARY /d {Convert.ToHexString(op.Value as byte[] ?? Array.Empty<byte>())} /f",
                    _ => $"reg add \"{op.Target}\" /v {op.ValueName} /t {(op.RegKind == RegistryValueKind.ExpandString ? "REG_EXPAND_SZ" : "REG_SZ")} /d \"{op.Value}\" /f"
                },
                TweakOpKind.RegDelete => $"reg delete \"{op.Target}\" /v {op.ValueName} /f",
                TweakOpKind.ServiceStartMode => $"sc config \"{op.Target}\" start= {StartLabel(op)}",
                TweakOpKind.ScheduledTask => $"schtasks /Change /TN \"{op.Target}\" {(Convert.ToBoolean(op.Value ?? false) ? "/ENABLE" : "/DISABLE")}",
                TweakOpKind.BcdSet => $"bcdedit /set {{current}} {op.Target} {op.ValueName}",
                TweakOpKind.BcdDelete => $"bcdedit /deletevalue {{current}} {op.Target}",
                _ => op.ApplyCmd
            });
        }
        return string.Join(Environment.NewLine, lines);
    }

    private static string StartLabel(TweakOp op) => Convert.ToInt32(op.Value ?? 3) switch
    {
        0 => "boot",
        1 => "system",
        2 => "auto",
        3 => "demand",
        _ => "disabled"
    };

    // ==================== Detectores auxiliares ====================
    internal static TweakStateInfo CheckReg(string key, string name, object expected)
    {
        object? cur = TweakJournal.ReadReg(key, name);
        if (cur == null)
            return new TweakStateInfo(false, Loc.Pick("Valor ausente (padrão do Windows)", "Value absent (Windows default)"));
        bool applied = Equals(Convert.ToString(cur, CultureInfo.InvariantCulture),
                              Convert.ToString(expected, CultureInfo.InvariantCulture));
        return new TweakStateInfo(applied, $"{(applied ? "= " : "≠ ")}{Fmt(cur)}");
    }

    internal static TweakStateInfo CheckRegMissing(string key, string name)
    {
        object? cur = TweakJournal.ReadReg(key, name);
        return cur == null
            ? new TweakStateInfo(true, Loc.Pick("Removido (padrão)", "Removed (default)"))
            : new TweakStateInfo(false, Fmt(cur));
    }

    internal static TweakStateInfo CheckService(string name, int expectedStart)
    {
        int? cur = TweakJournal.ReadServiceStart(name);
        if (cur == null) return new TweakStateInfo(false, Loc.Pick("Serviço inexistente", "Service not found"));
        return new TweakStateInfo(cur == expectedStart, $"Start={cur}");
    }

    internal static TweakStateInfo CheckKeyExists(string key)
    {
        try
        {
            var (root, sub) = TweakJournal.SplitRegistryPath(key);
            using var k = root.OpenSubKey(sub, false);
            return k != null ? new TweakStateInfo(true, Loc.Pick("Chave criada", "Key present"))
                             : new TweakStateInfo(false, Loc.Pick("Chave ausente", "Key absent"));
        }
        catch { return new TweakStateInfo(false, "—"); }
    }

    private static string Fmt(object v) => v is byte[] b
        ? Convert.ToHexString(b)
        : Convert.ToString(v, CultureInfo.InvariantCulture) ?? "";

    // ==================== Fábricas compactas ====================
    internal static TweakEntry T(string id, TweakSection sec, TweakRisk risk, TweakTags tags,
        string pt, string en, string dPt, string dEn,
        Func<TweakStateInfo>? detect, string cmdOverride = "", params TweakOp[] ops)
        => new()
        {
            Id = id, Section = sec, Risk = risk, Tags = tags,
            TitlePt = pt, TitleEn = en, DescPt = dPt, DescEn = dEn,
            Ops = ops.ToList(),
            FallbackRevertOps = new List<TweakOp>(),
            DetectFunc = detect,
            CommandOverride = cmdOverride
        };

    internal static TweakEntry Dword(string id, TweakSection sec, TweakRisk risk, TweakTags tags,
        string pt, string en, string dPt, string dEn, string key, string name, int value)
        => T(id, sec, risk, tags, pt, en, dPt, dEn,
             () => CheckReg(key, name, value), "", TweakOp.RegDword(key, name, value));

    internal static TweakEntry Str(string id, TweakSection sec, TweakRisk risk, TweakTags tags,
        string pt, string en, string dPt, string dEn, string key, string name, string value)
        => T(id, sec, risk, tags, pt, en, dPt, dEn,
             () => CheckReg(key, name, value), "", TweakOp.RegString(key, name, value));

    internal static TweakEntry Del(string id, TweakSection sec, TweakRisk risk, TweakTags tags,
        string pt, string en, string dPt, string dEn, string key, string name)
        => T(id, sec, risk, tags, pt, en, dPt, dEn,
             () => CheckRegMissing(key, name), "", TweakOp.RegDel(key, name));

    internal static TweakEntry Svc(string id, TweakRisk risk, TweakTags tags,
        string pt, string en, string dPt, string dEn, string service, int start = 4)
        => T(id, TweakSection.Privacy, risk, tags, pt, en, dPt, dEn,
             () => CheckService(service, start), "",
             TweakOp.Service(service, start));

    internal static TweakEntry Bcd(string id, TweakSection sec, TweakRisk risk, TweakTags tags,
        string pt, string en, string dPt, string dEn, string bcdKey, string? bcdValue)
    {
        TweakStateInfo Detect()
        {
            var o = TweakJournal.RunProcess("bcdedit.exe", "/enum {current}").Output;
            bool present = o.Contains(bcdKey, StringComparison.OrdinalIgnoreCase);
            return new TweakStateInfo(bcdValue == null ? !present : present,
                present ? $"{bcdKey} " + Loc.Pick("definido", "set") : Loc.Pick("padrão do Windows", "Windows default"));
        }
        var t = T(id, sec, risk, tags, pt, en, dPt, dEn, Detect,
            bcdValue == null ? $"bcdedit /deletevalue {{current}} {bcdKey}" : $"bcdedit /set {{current}} {bcdKey} {bcdValue}",
            bcdValue == null ? TweakOp.BcdDel(bcdKey) : TweakOp.Bcd(bcdKey, bcdValue));
        return t;
    }

    /// <summary>Acesso às configurações vivas do app (preenchido pelo MainWindow ao iniciar).</summary>
    public static AppSettings SettingsInstance { get; internal set; } = AppSettings.Load();

    /// <summary>Leitura nativa do FilterKeys (acessibilidade que engole o strafe).</summary>
    internal static TweakStateInfo CheckFilterKeys()
    {
        uint flags = InputLagTools.GetFilterKeysFlags();
        if (flags == 0) return new TweakStateInfo(true, Loc.Pick("Filtro desligado", "Filter off"));
        if (flags == 0xFFFFFFFF) return new TweakStateInfo(false, "—");
        return new TweakStateInfo(false, $"SPI flags = 0x{flags:X}");
    }

    internal static TweakEntry Blocked(string id, string pt, string en, string dPt, string dEn, string fakeCmd)
        => new()
        {
            Id = id, Section = TweakSection.Blocked, Risk = TweakRisk.BlockedHazard,
            Tags = TweakTags.NoAuto,
            TitlePt = pt, TitleEn = en, DescPt = dPt, DescEn = dEn,
            CommandOverride = fakeCmd,
            DetectFunc = () => new TweakStateInfo(false, Loc.Pick("Bloqueado permanentemente", "Permanently blocked"))
        };

    // ==================== Chaves de registro reutilizadas ====================
    internal const string KGameTask = @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games";
    internal const string KSystemProfile = @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile";
    internal const string KPriority = @"HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl";
    internal const string KSessionManager = @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager";
    internal const string KMemMgmt = @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management";
    internal const string KGraphicsDrivers = @"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers";
    internal const string KFilesystem = @"HKLM\SYSTEM\CurrentControlSet\Control\FileSystem";
    internal const string KDwm = @"HKCU\Software\Microsoft\Windows\DWM";
    internal const string KExplorerAdv = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    internal const string KEcoQos = @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive";
    internal const string KNetTcpip = @"HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters";
    internal const string KNetDns = @"HKLM\SYSTEM\CurrentControlSet\Services\Dnscache\Parameters";

    public static bool IsElevated
    {
        get
        {
            try
            {
                using var id = WindowsIdentity.GetCurrent();
                return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }
    }
}
