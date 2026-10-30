using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PhantomUI;

/// <summary>
/// Purgador nativo de memória (substitui ISLC / limpa-memória de terceiros):
/// esvazia a standby list e o working set de processos não-críticos via NtSetSystemInformation + EmptyWorkingSet.
/// NUNCA toca em lsass, csrss, wininit, winlogon, services, smss, System, MemCompression e anti-cheats.
/// </summary>
public static class MemoryPurge
{
    private const int SystemMemoryListInformation = 0x0050;
    private const int MemoryPurgeStandbyList = 4;
    private const int MemoryEmptyWorkingSets = 2;
    private const int MemoryFlushModifiedList = 3;
    private const int SeIncreaseQuotaPrivilege = 4;
    private const int SeProfileSingleProcessPrivilege = 13;

    [DllImport("ntdll.dll")]
    private static extern uint NtSetSystemInformation(int infoClass, IntPtr info, int length);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool LookupPrivilegeValue(string? host, string name, out long luid);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool AdjustTokenPrivileges(IntPtr token, bool disableAll, ref TokenPrivileges newState, int len, IntPtr prev, IntPtr retLen);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("psapi.dll")]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct TokenPrivileges
    {
        public int PrivilegeCount;
        public long Luid;
        public int Attributes;
    }

    private static readonly HashSet<string> Protected = new(StringComparer.OrdinalIgnoreCase)
    {
        "system", "registry", "memory compression", "memcompression", "smss", "csrss", "wininit", "winlogon",
        "services", "lsass", "lsaiso", "fontdrvhost", "dwm", "svchost", "audiodg", "spoolsv", "msmpeng",
        "securityhealthservice", "nissrv", "vgc", "vgtray", "easyanticheat", "easyanticheat_eos", "beservice",
        "bedaisy", "faceit", "faceitclient", "faceitac", "battleye", "phantomui"
    };

    /// <summary>Libera standby list + working sets não-críticos. Retorna (MB liberados, processos ajustados).</summary>
    public static (double MbFreed, int Processes) PurgeStandbyAndWorkingSets()
    {
        var (beforeMb, _) = FreeMb();
        EnablePrivilege(SeProfileSingleProcessPrivilege);
        EnablePrivilege(SeIncreaseQuotaPrivilege);

        // 1) Standby list + modified page list
        PurgeList(MemoryFlushModifiedList);
        PurgeList(MemoryPurgeStandbyList);

        // 2) Working set de processos que não são críticos
        int count = 0;
        try
        {
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    if (p.Id <= 4) continue;
                    if (Protected.Contains(p.ProcessName)) continue;
                    if (p.WorkingSet64 < 40L * 1024 * 1024) continue; // abaixo de 40 MB não compensa
                    if (EmptyWorkingSet(p.Handle)) count++;
                }
                catch { }
                finally { p.Dispose(); }
            }
        }
        catch { }

        var (afterMb, _) = FreeMb();
        double freed = Math.Max(0, afterMb - beforeMb);
        return (freed, count);
    }

    private static void PurgeList(int command)
    {
        try
        {
            IntPtr ptr = Marshal.AllocHGlobal(sizeof(int));
            Marshal.WriteInt32(ptr, command);
            NtSetSystemInformation(SystemMemoryListInformation, ptr, sizeof(int));
            Marshal.FreeHGlobal(ptr);
        }
        catch { }
    }

    private static (double FreeMb, double TotalMb) FreeMb()
    {
        var (total, free, _) = InputLagTools.GetLiveMemoryAndTimer();
        return (free, total);
    }

    private static void EnablePrivilege(int privilege)
    {
        try
        {
            if (!OpenProcessToken(GetCurrentProcess(), 0x0020 | 0x0008, out IntPtr token)) return;
            try
            {
                string name = privilege switch
                {
                    SeProfileSingleProcessPrivilege => "SeProfileSingleProcessPrivilege",
                    SeIncreaseQuotaPrivilege => "SeIncreaseQuotaPrivilege",
                    _ => "SeDebugPrivilege"
                };
                if (LookupPrivilegeValue(null, name, out long luid))
                {
                    var tp = new TokenPrivileges { PrivilegeCount = 1, Luid = luid, Attributes = 0x00000002 }; // ENABLED
                    AdjustTokenPrivileges(token, false, ref tp, Marshal.SizeOf<TokenPrivileges>(), IntPtr.Zero, IntPtr.Zero);
                }
            }
            finally { CloseHandle(token); }
        }
        catch { }
    }
}
