using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace PhantomUI;

public sealed class EmbeddedPowerPlan
{
    public string ResourceName { get; init; } = "";
    public string Category { get; init; } = "";
    public string Name { get; init; } = "";
    public string DescriptionPt { get; init; } = "";
    public string DescriptionEn { get; init; } = "";
    public string RecommendedFor { get; init; } = "";

    public string Description => Loc.Pick(DescriptionPt, DescriptionEn);
}

public sealed class InstalledPowerScheme
{
    public string Guid { get; init; } = "";
    public string Name { get; init; } = "";
    public bool IsActive { get; init; }
    public string Badge => IsActive ? Loc.Pick("ATIVO AGORA", "ACTIVE NOW") : "";
}

/// <summary>
/// Cofre embutido com os 34 planos de energia .pow curados (AMD Ryzen, Intel, AtlasOS, ReviOS, EXM, Amit Idle-Enabled, Windows)
/// + gerenciamento oficial via powercfg.exe (absorvido do 55.PlanosEnergia.ps1).
/// </summary>
public static class PowerPlanVault
{
    public static List<EmbeddedPowerPlan> GetEmbeddedCatalog()
    {
        var asm = Assembly.GetExecutingAssembly();
        var list = new List<EmbeddedPowerPlan>();
        foreach (var res in asm.GetManifestResourceNames().Where(r => r.StartsWith("PowerPlans/", StringComparison.OrdinalIgnoreCase) && r.EndsWith(".pow", StringComparison.OrdinalIgnoreCase)))
        {
            // ex: PowerPlans/AMD/Custom - 1usmus' Ryzen Universal Power Plan.pow
            var parts = res.Split('/');
            string folder = parts.Length >= 3 ? parts[1] : "Geral";
            string file = Path.GetFileNameWithoutExtension(parts[^1]);

            var (cat, pt, en, rec) = DescribePlan(folder, file);
            list.Add(new EmbeddedPowerPlan
            {
                ResourceName = res,
                Category = cat,
                Name = file,
                DescriptionPt = pt,
                DescriptionEn = en,
                RecommendedFor = rec
            });
        }
        return list.OrderBy(p => p.Category).ThenBy(p => p.Name).ToList();
    }

    private static (string Cat, string Pt, string En, string Rec) DescribePlan(string folder, string name)
    {
        string low = name.ToLowerInvariant();
        if (folder.Equals("AMD", StringComparison.OrdinalIgnoreCase))
        {
            if (low.Contains("oficial"))
                return ("AMD Ryzen", "Plano oficial AMD Ryzen High Performance para máxima resposta de boost.", "Official AMD Ryzen High Performance plan for fast core boost.", "AMD Ryzen Desktop");
            if (low.Contains("1usmus"))
                return ("AMD Ryzen", "Plano universal do 1usmus: prioriza núcleos preferenciais (CPPC) sem travar todos os cores.", "1usmus Universal Ryzen plan: favors CPPC preferred cores cleanly.", "Ryzen 3000/5000");
            if (low.Contains("lowpower") || low.Contains("saver"))
                return ("AMD Ryzen", "Plano Ryzen otimizado para temperaturas baixas, silêncio e eficiência.", "Ryzen plan tuned for low thermals, quiet operation, and efficiency.", "Ryzen Notebook / Frio");
            if (low.Contains("snappy"))
                return ("AMD Ryzen", "Equilíbrio entre resposta instantânea de desktop e C-States ativos em repouso.", "Snappy desktop responsiveness while keeping idle C-states active.", "AMD Ryzen Geral");
            return ("AMD Ryzen", "Plano customizado de baixa latência ajustado para arquiteturas AMD Zen.", "Low-latency custom power profile tuned for AMD Zen CPUs.", "AMD Ryzen");
        }
        if (folder.Equals("Intel", StringComparison.OrdinalIgnoreCase))
            return ("Intel Core", "Plano calibrado para Intel SpeedShift (EPP) e resposta rápida sem aquecimento ocioso desnecessário.", "Tuned for Intel SpeedShift (EPP) and fast clock ramp-up.", "Intel Core");
        if (folder.Equals("Windows", StringComparison.OrdinalIgnoreCase))
            return ("Windows Oficial", "Esquema original de fábrica da Microsoft para restauração imediata.", "Original Microsoft factory power scheme for clean restoration.", "Qualquer PC");
        if (low.Contains("amit") || low.Contains("idle"))
            return ("Gaming (Idle Seguro)", "Plano gamer competitivo COM estados ociosos (C-States/Idle) preservados — zero aquecimento em idle!", "Competitive gaming plan WITH CPU idle states preserved — runs cool at idle!", "Recomendado Gamer");
        if (low.Contains("atlas") || low.Contains("revi") || low.Contains("kernel") || low.Contains("forged") || low.Contains("rek"))
            return ("Custom OS (Atlas/Revi)", "Extraído dos playbooks de baixa latência (USB Suspend Off, PCIe ASPM Off, Boost responsivo).", "Extracted from low-latency OS playbooks (USB Suspend Off, ASPM Off).", "Desktop Gamer");

        return ("Gaming Performance", "Plano focado em latência mínima de escalonamento e estabilidade de frametime em jogos.", "Focused on minimum scheduling latency and consistent gaming frametimes.", "Desktop AC");
    }

    public static List<InstalledPowerScheme> ListInstalled()
    {
        var list = new List<InstalledPowerScheme>();
        var r = TweakJournal.RunProcess("powercfg.exe", "/list", 8000);
        var rx = new Regex(@"([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\s+\((.+?)\)(\s*\*)?");
        foreach (var line in r.Output.Split('\n'))
        {
            var m = rx.Match(line);
            if (!m.Success) continue;
            list.Add(new InstalledPowerScheme
            {
                Guid = m.Groups[1].Value,
                Name = m.Groups[2].Value.Trim(),
                IsActive = !string.IsNullOrWhiteSpace(m.Groups[3].Value)
            });
        }
        return list;
    }

    public static (bool Ok, string Message) ImportAndActivate(EmbeddedPowerPlan plan)
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream(plan.ResourceName);
            if (stream == null) return (false, "Recurso .pow não encontrado no executável.");

            string tempPow = Path.Combine(Path.GetTempPath(), $"PhantomUI_{Guid.NewGuid():N}.pow");
            try
            {
                using (var fs = File.Create(tempPow))
                    stream.CopyTo(fs);

                string newGuid = Guid.NewGuid().ToString();
                var imp = TweakJournal.RunProcess("powercfg.exe", $"-import \"{tempPow}\" {newGuid}", 10_000);
                if (imp.ExitCode != 0)
                    return (false, "Falha no powercfg -import: " + imp.Error + " " + imp.Output);

                TweakJournal.RunProcess("powercfg.exe", $"-changename {newGuid} \"PhantomUI - {plan.Name}\"", 6000);
                var act = TweakJournal.RunProcess("powercfg.exe", $"-setactive {newGuid}", 6000);
                return act.ExitCode == 0
                    ? (true, Loc.Pick($"Plano '{plan.Name}' importado e ativado com sucesso!", $"Power plan '{plan.Name}' imported and activated!"))
                    : (false, "Importado, mas falhou ao ativar: " + act.Error);
            }
            finally
            {
                try { if (File.Exists(tempPow)) File.Delete(tempPow); } catch { }
            }
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public static (bool Ok, string Message) ActivateGuid(string guid)
    {
        var r = TweakJournal.RunProcess("powercfg.exe", $"-setactive {guid}", 6000);
        return r.ExitCode == 0
            ? (true, Loc.Pick("Plano de energia ativado.", "Power plan activated."))
            : (false, r.Error);
    }

    public static (bool Ok, string Message) DeleteScheme(string guid)
    {
        var r = TweakJournal.RunProcess("powercfg.exe", $"-delete {guid}", 6000);
        return r.ExitCode == 0
            ? (true, Loc.Pick("Plano removido.", "Power plan deleted."))
            : (false, Loc.Pick("Não é possível remover o plano ativo ou padrão do Windows.", "Cannot delete the currently active or default Windows plan."));
    }

    public static (bool Ok, string Message) RestoreBalancedDefault()
    {
        TweakJournal.RunProcess("powercfg.exe", "-setactive 381b4222-f694-41f0-9685-ff5bb260df2e", 6000);
        return (true, Loc.Pick("Plano Equilibrado padrão do Windows ativado.", "Windows Balanced default plan activated."));
    }
}
