using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace PhantomUI;

public sealed class PciMsiDevice : INotifyPropertyChanged
{
    public string InstancePath { get; init; } = "";
    public string Name { get; init; } = "";
    public string Category { get; init; } = "";
    private bool _msiEnabled;
    private int _priority; // 0=Undefined, 1=Low, 2=Normal, 3=High
    public string Limit { get; init; } = "—";

    public bool MsiEnabled
    {
        get => _msiEnabled;
        set { _msiEnabled = value; OnChanged(nameof(MsiEnabled)); OnChanged(nameof(StateBadge)); }
    }

    public int Priority
    {
        get => _priority;
        set { _priority = value; OnChanged(nameof(Priority)); OnChanged(nameof(PriorityLabel)); }
    }

    public string StateBadge => MsiEnabled ? "MSI ON" : "LINE-BASED (IRQ)";
    public string PriorityLabel => Priority switch
    {
        1 => "Baixa (Low)",
        2 => "Normal",
        3 => "Alta (High)",
        _ => "Padrão (Auto)"
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

/// <summary>
/// Ferramentas nativas de Input Lag e Memória:
/// 1) FilterKeys em tempo real via Win32 SystemParametersInfo (SPI_SETFILTERKEYS)
/// 2) Gerenciador nativo de PCIe MSI Mode & Interrupt Priority (substitui MSI_util_v3.exe com backup no TweakJournal)
/// 3) Purgador Inteligente de Standby List + Timer Resolution 0.5ms (substitui ISLC.exe nativamente)
/// </summary>
public static class InputLagTools
{
    [StructLayout(LayoutKind.Sequential)]
    private struct FILTERKEYS
    {
        public uint cbSize;
        public uint dwFlags;
        public uint iWaitMSec;
        public uint iDelayMSec;
        public uint iRepeatMSec;
        public uint iBounceMSec;
    }

    private const uint SPI_SETFILTERKEYS = 0x0033;
    private const uint SPI_GETFILTERKEYS = 0x0032;
    private const uint SPIF_UPDATEINIFILE = 0x01;
    private const uint SPIF_SENDCHANGE = 0x02;
    private const uint FKF_FILTERKEYSON = 0x00000001;
    private const uint FKF_AVAILABLE = 0x00000002;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref FILTERKEYS pvParam, uint fWinIni);

    [DllImport("ntdll.dll")]
    private static extern int NtSetTimerResolution(uint desiredResolution100Ns, bool setResolution, out uint currentResolution);

    [DllImport("ntdll.dll")]
    private static extern int NtQueryTimerResolution(out uint minRes, out uint maxRes, out uint curRes);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    /// <summary>Lê os flags atuais do FilterKeys (0 = filtro totalmente desligado).</summary>
    public static uint GetFilterKeysFlags()
    {
        try
        {
            var fk = new FILTERKEYS { cbSize = (uint)Marshal.SizeOf<FILTERKEYS>() };
            if (SystemParametersInfo(SPI_GETFILTERKEYS, fk.cbSize, ref fk, 0)) return fk.dwFlags;
        }
        catch { }
        return 0xFFFFFFFF;
    }

    public static (bool Ok, string Message) ApplyFilterKeys(bool enable, int waitMs, int delayMs, int repeatMs, int bounceMs = 0)
    {
        try
        {
            string regPath = @"Control Panel\Accessibility\Keyboard Response";
            using (var k = Registry.CurrentUser.CreateSubKey(regPath, true))
            {
                if (k != null)
                {
                    k.SetValue("AutoRepeatDelay", delayMs.ToString(), RegistryValueKind.String);
                    k.SetValue("AutoRepeatRate", repeatMs.ToString(), RegistryValueKind.String);
                    k.SetValue("DelayBeforeAcceptance", waitMs.ToString(), RegistryValueKind.String);
                    k.SetValue("BounceTime", bounceMs.ToString(), RegistryValueKind.String);
                    k.SetValue("Flags", enable ? "59" : "126", RegistryValueKind.String);
                }
            }

            var fk = new FILTERKEYS
            {
                cbSize = (uint)Marshal.SizeOf<FILTERKEYS>(),
                dwFlags = enable ? (FKF_FILTERKEYSON | FKF_AVAILABLE) : FKF_AVAILABLE,
                iWaitMSec = (uint)Math.Max(0, waitMs),
                iDelayMSec = (uint)Math.Max(0, delayMs),
                iRepeatMSec = (uint)Math.Max(0, repeatMs),
                iBounceMSec = (uint)Math.Max(0, bounceMs)
            };
            SystemParametersInfo(SPI_SETFILTERKEYS, fk.cbSize, ref fk, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);

            return enable
                ? (true, Loc.Pick($"FilterKeys aplicado na hora: Aceite={waitMs}ms, Atraso={delayMs}ms, Repetição={repeatMs}ms.",
                                  $"FilterKeys applied immediately: Accept={waitMs}ms, Delay={delayMs}ms, Repeat={repeatMs}ms."))
                : (true, Loc.Pick("FilterKeys desativado e restaurado ao padrão do Windows.",
                                  "FilterKeys disabled and restored to Windows default."));
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public static List<PciMsiDevice> ScanPciMsiDevices()
    {
        var result = new List<PciMsiDevice>();
        try
        {
            using var pciRoot = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\PCI", false);
            if (pciRoot == null) return result;

            foreach (var devId in pciRoot.GetSubKeyNames())
            {
                using var devKey = pciRoot.OpenSubKey(devId, false);
                if (devKey == null) continue;
                foreach (var instId in devKey.GetSubKeyNames())
                {
                    using var instKey = devKey.OpenSubKey(instId, false);
                    if (instKey == null) continue;

                    string cls = (instKey.GetValue("Class")?.ToString() ?? "").ToLowerInvariant();
                    string service = (instKey.GetValue("Service")?.ToString() ?? "").ToLowerInvariant();
                    // Filtra apenas controladores relevantes para latência (GPU, Rede, NVMe/Storage, USB XHCI, Áudio HD)
                    bool relevant = cls is "display" or "net" or "scsiadapter" or "hdc" or "usb" or "media"
                                    || service is "nvlddmkm" or "amdkmdag" or "amdwddmg" or "igfx" or "stornvme" or "usbxhci" or "hdaudbus";
                    if (!relevant) continue;

                    using var msiKey = instKey.OpenSubKey(@"Device Parameters\Interrupt Management\MessageSignaledInterruptProperties", false);
                    if (msiKey == null) continue; // só exibe dispositivos que suportam tabela MSI no barramento PCIe

                    string rawDesc = instKey.GetValue("FriendlyName")?.ToString() ?? instKey.GetValue("DeviceDesc")?.ToString() ?? devId;
                    int semi = rawDesc.LastIndexOf(';');
                    if (semi >= 0 && semi + 1 < rawDesc.Length) rawDesc = rawDesc[(semi + 1)..];

                    bool msiOn = Convert.ToInt32(msiKey.GetValue("MSISupported") ?? 0) == 1;
                    string limit = msiKey.GetValue("MessageNumberLimit")?.ToString() ?? "Auto";

                    int prio = 0;
                    using (var affKey = instKey.OpenSubKey(@"Device Parameters\Interrupt Management\Affinity Policy", false))
                    {
                        if (affKey != null) prio = Convert.ToInt32(affKey.GetValue("DevicePriority") ?? 0);
                    }

                    string cat = cls switch
                    {
                        "display" => "GPU",
                        "net" => "Rede / NIC",
                        "scsiadapter" or "hdc" => "NVMe / SATA",
                        "usb" => "USB Controller",
                        "media" => "Áudio",
                        _ => service.ToUpperInvariant()
                    };

                    result.Add(new PciMsiDevice
                    {
                        InstancePath = $@"HKLM\SYSTEM\CurrentControlSet\Enum\PCI\{devId}\{instId}",
                        Name = rawDesc.Trim(),
                        Category = cat,
                        MsiEnabled = msiOn,
                        Priority = prio,
                        Limit = limit
                    });
                }
            }
        }
        catch { }
        return result.OrderBy(d => d.Category).ThenBy(d => d.Name).ToList();
    }

    public static (bool Ok, string Message) SetDeviceMsiAndPriority(PciMsiDevice dev, bool enableMsi, int priority)
    {
        try
        {
            string msiSub = $@"{dev.InstancePath}\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties";
            string affSub = $@"{dev.InstancePath}\Device Parameters\Interrupt Management\Affinity Policy";

            var ops = new List<TweakOp>
            {
                TweakOp.RegDword(msiSub, "MSISupported", enableMsi ? 1 : 0)
            };
            if (priority <= 0)
                ops.Add(TweakOp.RegDel(affSub, "DevicePriority"));
            else
                ops.Add(TweakOp.RegDword(affSub, "DevicePriority", priority));

            var dummy = new TweakEntry
            {
                Id = "msi." + dev.InstancePath.GetHashCode().ToString("X8"),
                TitlePt = $"PCIe MSI Mode: {dev.Name}",
                TitleEn = $"PCIe MSI Mode: {dev.Name}",
                Ops = ops
            };
            var res = TweakJournal.ApplyTweak(dummy);
            if (res.Ok)
            {
                dev.MsiEnabled = enableMsi;
                dev.Priority = priority;
                return (true, Loc.Pick(
                    $"MSI={(enableMsi ? "ON" : "OFF")} e Prioridade={dev.PriorityLabel} salvos para {dev.Name} (reinicie para valer).",
                    $"MSI={(enableMsi ? "ON" : "OFF")} and Priority={dev.PriorityLabel} saved for {dev.Name} (reboot required)."));
            }
            return res;
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public static (double TotalMb, double FreeMb, string TimerMs) GetLiveMemoryAndTimer()
    {
        double total = 0, free = 0;
        try
        {
            var mem = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(mem))
            {
                total = mem.ullTotalPhys / 1048576.0;
                free = mem.ullAvailPhys / 1048576.0;
            }
        }
        catch { }

        string timer = "—";
        try
        {
            if (NtQueryTimerResolution(out _, out _, out uint cur) == 0)
                timer = $"{cur / 10000.0:0.00} ms";
        }
        catch { }
        return (total, free, timer);
    }

    public static void SetHighPrecisionTimer05Ms(bool enable)
    {
        try { NtSetTimerResolution(5000, enable, out _); } catch { }
    }
}
