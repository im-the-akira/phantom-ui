using System.IO;
using System.Text;
using Microsoft.Win32;

namespace PhantomUI;

public sealed class DriverAuditRow
{
    public string Device { get; init; } = "";
    public string Class { get; init; } = "";
    public string Provider { get; init; } = "";
    public string Version { get; init; } = "";
    public string Date { get; init; } = "";
    public string Source { get; init; } = "";          // WHQL (Windows Update) / Fabricante / Genérico
    public bool IsGeneric { get; init; }
    public bool IsOld { get; init; }
    public bool Signed { get; init; }
    public string Status => IsGeneric ? Loc.Pick("GENÉRICO", "GENERIC") : IsOld ? Loc.Pick("ANTIGO", "OLD") : Loc.Pick("OK", "OK");
    public string Color => IsGeneric ? "#FF4757" : IsOld ? "#FFA502" : "#2ED573";
}

public sealed class GpuDriverTarget
{
    public string Vendor { get; init; } = "";      // NVIDIA / AMD / Intel
    public string GpuName { get; init; } = "";
    public string InstalledVersion { get; init; } = "";
    public string OfficialPage { get; init; } = "";
    public string IntelProduct { get; init; } = ""; // código do produto Intel (DSA)
    public bool SupportsDsa { get; init; }
}

/// <summary>
/// Auditoria e atualização de drivers SEMPRE por fonte oficial:
/// NVIDIA (API oficial GeForce), AMD (página oficial do modelo), Intel (Driver & Support Assistant / página oficial),
/// Microsoft WHQL (Windows Update COM, com política de WU liberada temporariamente e restaurada no fim).
/// </summary>
public static class DriverAudit
{
    /// <summary>Inventário assinado de drivers de terceiros, marcando genéricos e antigos.</summary>
    public static List<DriverAuditRow> Audit()
    {
        var rows = new List<DriverAuditRow>();
        try
        {
            string root = @"SOFTWARE\Microsoft\Windows\CurrentVersion\DriverDatabase";
            using var k = Registry.LocalMachine.OpenSubKey(root);
            // Inventário rápido via pnputil (cobre todos os drivers publicados)
            var r = TweakJournal.RunProcess("pnputil.exe", "/enum-drivers", 60_000);
            if (r.ExitCode == 0)
            {
                string name = "", provider = "", ver = "", date = "", cls = "";
                foreach (var raw in r.Output.Split('\n'))
                {
                    string line = raw.Trim();
                    if (line.StartsWith("Published Name", StringComparison.OrdinalIgnoreCase)) { Flush(); name = After(line); }
                    else if (line.StartsWith("Original Name", StringComparison.OrdinalIgnoreCase)) name = name.Length > 0 ? name : After(line);
                    else if (line.StartsWith("Provider Name", StringComparison.OrdinalIgnoreCase)) provider = After(line);
                    else if (line.StartsWith("Class Name", StringComparison.OrdinalIgnoreCase)) cls = After(line);
                    else if (line.StartsWith("Driver Version", StringComparison.OrdinalIgnoreCase)) ver = After(line);
                    else if (line.StartsWith("Driver Date", StringComparison.OrdinalIgnoreCase)) date = After(line);
                }
                Flush();

                void Flush()
                {
                    if (provider.Length == 0 && ver.Length == 0) { name = provider = ver = date = cls = ""; return; }
                    bool generic = provider.Equals("Microsoft", StringComparison.OrdinalIgnoreCase);
                    bool old = DateTime.TryParse(ver.Split(' ')[0], out var d) && (DateTime.Today - d).TotalDays > 1500 && date.Length == 0;
                    rows.Add(new DriverAuditRow
                    {
                        Device = name, Class = cls, Provider = provider, Version = ver, Date = date,
                        Source = generic ? "WHQL / Microsoft" : provider,
                        IsGeneric = generic && !cls.Contains("USB", StringComparison.OrdinalIgnoreCase),
                        IsOld = old, Signed = true
                    });
                    name = provider = ver = date = cls = "";
                }
            }
        }
        catch { }

        if (rows.Count == 0)
            rows.Add(new DriverAuditRow { Device = Loc.Pick("Execute como administrador para listar", "Run as administrator to list"),
                Class = "—", Provider = "—", Version = "—", Date = "—", Source = "—", Signed = false });
        return rows.OrderBy(x => x.IsGeneric ? 0 : x.IsOld ? 1 : 2).ThenBy(x => x.Class).ToList();
    }

    private static string After(string line)
    {
        int idx = line.IndexOf(':');
        return idx < 0 ? line : line[(idx + 1)..].Trim();
    }

    /// <summary>Alvos de driver de GPU detectados na máquina.</summary>
    public static List<GpuDriverTarget> GpuTargets()
    {
        var hw = HardwareAutoOptimizer.Detect();
        var list = new List<GpuDriverTarget>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, vendor, version) in EnumerateGpus())
        {
            if (!seen.Add(name)) continue;
            string page = vendor switch
            {
                "NVIDIA" => "https://www.nvidia.com/pt-br/drivers/",
                "AMD" => "https://www.amd.com/pt/support/download/drivers.html",
                "Intel" => "https://www.intel.com.br/content/www/br/pt/support/detect.html",
                _ => ""
            };
            list.Add(new GpuDriverTarget
            {
                Vendor = vendor, GpuName = name, InstalledVersion = version, OfficialPage = page,
                IntelProduct = vendor == "Intel" ? IntelProductId(name) : "",
                SupportsDsa = vendor == "Intel"
            });
        }
        if (list.Count == 0 && hw.GpuName.Length > 0)
            list.Add(new GpuDriverTarget { Vendor = hw.GpuVendor.ToString(), GpuName = hw.GpuName, OfficialPage = "https://www.nvidia.com/pt-br/drivers/" });
        return list;
    }

    private static IEnumerable<(string Name, string Vendor, string Version)> EnumerateGpus()
    {
        var result = new List<(string, string, string)>();
        try
        {
            using var s = new System.Management.ManagementObjectSearcher(
                "SELECT Name, DriverVersion, DriverDate, PNPDeviceID FROM Win32_VideoController");
            foreach (var mo in s.Get().Cast<System.Management.ManagementObject>())
            {
                string name = (mo["Name"] as string ?? "").Trim();
                if (name.Length == 0) continue;
                string version = FormatVersion(mo["DriverVersion"] as string ?? "");
                string vendor = name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ? "NVIDIA"
                              : name.Contains("AMD", StringComparison.OrdinalIgnoreCase) || name.Contains("Radeon", StringComparison.OrdinalIgnoreCase) ? "AMD"
                              : name.Contains("Intel", StringComparison.OrdinalIgnoreCase) ? "Intel" : "";
                result.Add((name, vendor, version));
            }
        }
        catch { }
        return result;
    }

    private static string FormatVersion(string raw)
    {
        // NVIDIA expõe 32.0.15.6094 → 560.94
        var parts = raw.Split('.');
        if (parts.Length == 4 && parts[0] == "32" && int.TryParse(parts[2], out int major))
            return $"{major}.{parts[3]}";
        if (parts.Length == 4 && parts[0] == "31" && int.TryParse(parts[2], out int m2))
            return $"{m2}.{parts[3]}";
        return raw;
    }

    private static string IntelProductId(string gpuName)
    {
        // Intel usa IDs de produto na página oficial de download
        var m = System.Text.RegularExpressions.Regex.Match(gpuName, @"Arc\s+(A\d{3}|B\d{3})|Iris\s+Xe|UHD", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (gpuName.Contains("Arc B", StringComparison.OrdinalIgnoreCase)) return "arc-b-series";
        if (gpuName.Contains("Arc A", StringComparison.OrdinalIgnoreCase)) return "arc-a-series";
        if (m.Success) return "integrated";
        return "";
    }

    /// <summary>
    /// Instala/atualiza o driver de GPU pela fonte oficial do fabricante.
    /// NVIDIA/AMD: baixa o instalador oficial. Intel: abre o Driver &amp; Support Assistant oficial ou a página do produto.
    /// </summary>
    public static async Task<(bool Ok, string Message)> UpdateGpuDriverAsync(
        GpuDriverTarget target, Action<string, double> progress, CancellationToken ct)
    {
        var status = new GpuDriverStatus
        {
            Vendor = target.Vendor,
            GpuName = target.GpuName,
            Installed = target.InstalledVersion,
            OfficialPage = target.OfficialPage
        };

        switch (target.Vendor)
        {
            case "NVIDIA":
                await DriverManager.CheckNvidiaAsync(status, false, ct);
                if (status.DownloadUrl.Length == 0)
                    return (false, Loc.Pick("NVIDIA não devolveu link oficial. Abrindo a página do fabricante: ", "NVIDIA returned no official link. Opening the vendor page: ") + target.OfficialPage);
                break;

            case "AMD":
                await DriverManager.CheckAmdAsync(status, ct);
                if (status.DownloadUrl.Length == 0)
                    return (false, Loc.Pick("AMD não devolveu link oficial. Abrindo a página do fabricante: ", "AMD returned no official link. Opening the vendor page: ") + target.OfficialPage);
                break;

            case "Intel":
            {
                // Intel: única via 100% oficial é o DSA (Driver & Support Assistant) ou a página do produto.
                string dsa = @"https://dsadata.intel.com/installer";
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dsa) { UseShellExecute = true });
                }
                catch { }
                return (true, Loc.Pick(
                    "Intel: abrimos o instalador oficial do Driver & Support Assistant (dsadata.intel.com). Ele identifica o modelo exato (Arc/Iris/UHD) e instala somente o driver WHQL publicado pela Intel. Nada de pacote de terceiros.",
                    "Intel: we opened the official Driver & Support Assistant installer (dsadata.intel.com). It identifies the exact model (Arc/Iris/UHD) and installs only the WHQL driver Intel publishes. No third-party packs."));
            }

            default:
                return (false, Loc.Pick("Fabricante não identificado. Use a aba Windows Update (WHQL).", "Vendor not identified. Use the Windows Update (WHQL) tab."));
        }

        var msg = await DriverManager.DownloadAndLaunchAsync(status, progress, ct);
        return (true, msg + Environment.NewLine + Loc.Pick(
            "Instalador oficial aberto. Marque 'instalação limpa' quando oferecido e reinicie no fim.",
            "Official installer opened. Check 'clean install' when offered and reboot at the end."));
    }

    // =====================================================================
    // MICROSOFT WHQL — Windows Update COM com política temporária
    // =====================================================================
    /// <summary>Consulta drivers WHQL disponíveis, levantando a política de WU por alguns segundos.</summary>
    public static async Task<(bool Ok, List<WuDriver> Drivers, string Message)> SearchWhqlAsync()
    {
        var (lifted, backup) = LiftPolicy();
        try
        {
            await Task.Delay(400);
            var list = await DriverManager.SearchWindowsUpdateAsync();
            return (true, list, list.Count == 0
                ? Loc.Pick("Nenhum driver WHQL pendente para este hardware.", "No pending WHQL driver for this hardware.")
                : Loc.Pick($"{list.Count} driver(s) WHQL oficial(is) disponível(is).", $"{list.Count} official WHQL driver(s) available."));
        }
        catch (Exception ex)
        {
            return (false, new List<WuDriver>(), ex.Message);
        }
        finally
        {
            if (lifted) RestorePolicy(backup);
        }
    }

    /// <summary>Instala os drivers WHQL selecionados e restaura a política de WU ao final (sempre).</summary>
    public static async Task<(bool Ok, string Message)> InstallWhqlAsync(List<WuDriver> drivers, Action<string> progress)
    {
        if (drivers.Count == 0) return (false, Loc.Pick("Selecione ao menos um driver.", "Select at least one driver."));
        var (lifted, backup) = LiftPolicy();
        try
        {
            string result = await DriverManager.InstallWindowsUpdateAsync(drivers, progress);
            return (true, result + Environment.NewLine + Loc.Pick(
                "Política de Windows Update restaurada ao estado original.|Opcional: rode 'pnputil /add-driver' com o backup salvo para reverter um driver específico.",
                "Windows Update policy restored to its original state.|Optional: run 'pnputil /add-driver' with the saved backup to roll a specific driver back."));
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
        finally
        {
            if (lifted) RestorePolicy(backup);
        }
    }

    private static (bool, int?) LiftPolicy()
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", true);
            if (k?.GetValue("DisableWindowsUpdateAccess") is int v)
            {
                k.DeleteValue("DisableWindowsUpdateAccess", false);
                return (true, v);
            }
        }
        catch { }
        return (false, null);
    }

    private static void RestorePolicy(int? backup)
    {
        try
        {
            using var k = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", true);
            if (backup.HasValue) k?.SetValue("DisableWindowsUpdateAccess", backup.Value, RegistryValueKind.DWord);
        }
        catch { }
    }

    /// <summary>Exporta os drivers atuais com pnputil (backup restaurável).</summary>
    public static async Task<string> BackupAsync(Action<string> progress, CancellationToken ct)
        => await DriverManager.BackupAsync(progress, ct);

    public static string BuildReport(List<DriverAuditRow> rows, List<GpuDriverTarget> gpus)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Loc.Pick("=== AUDITORIA DE DRIVERS ===", "=== DRIVER AUDIT ==="));
        foreach (var g in gpus)
            sb.AppendLine($"{g.Vendor,-7} {g.GpuName,-40} {g.InstalledVersion,-12} {g.OfficialPage}");
        sb.AppendLine();
        int generic = rows.Count(r => r.IsGeneric);
        int old = rows.Count(r => r.IsOld);
        sb.AppendLine(Loc.Pick($"Drivers listados: {rows.Count} · genéricos: {generic} · antigos: {old}",
                               $"Drivers listed: {rows.Count} · generic: {generic} · old: {old}"));
        foreach (var r in rows)
            sb.AppendLine($"[{r.Status,-8}] {r.Class,-20} {r.Device,-45} {r.Provider} {r.Version}");
        sb.AppendLine();
        sb.AppendLine(Loc.Pick("Regra do PHANTOM UI: só fontes oficiais — NVIDIA/AMD/Intel (site do fabricante) e Microsoft WHQL.",
                               "PHANTOM UI rule: official sources only — NVIDIA/AMD/Intel (vendor site) and Microsoft WHQL."));
        return sb.ToString();
    }
}
