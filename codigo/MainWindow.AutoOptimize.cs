using System.Text;
using System.Windows;

namespace PhantomUI;

public partial class MainWindow
{
    private bool _hwInit;
    private HardwareProfile? _hw;
    private string? _lastProfileId;

    private void InitAutoPage()
    {
        if (_hwInit) return;
        _hwInit = true;
        ProfileList.ItemsSource = HardwareAutoOptimizer.Profiles;
        ProfileList.DisplayMemberPath = nameof(HardwareAutoOptimizer.OptimizationProfile.Name);
        ProfileList.SelectedIndex = 0;
        _lastProfileId = HardwareAutoOptimizer.Profiles[0].Id;
        ProfileList.SelectionChanged += (_, _) =>
        {
            if (ProfileList.SelectedItem is HardwareAutoOptimizer.OptimizationProfile p)
            {
                _lastProfileId = p.Id;
                ShowAutoTweaks(p.Id);
            }
        };
    }

    private async void HwScan_Click(object sender, RoutedEventArgs e)
    {
        if (BtnHwScan != null) BtnHwScan.IsEnabled = false;
        HwSummary.Text = Loc.Pick("Lendo CPU, GPU, RAM, discos, chassi e anti-cheats…", "Reading CPU, GPU, RAM, disks, chassis and anti-cheats…");
        try
        {
            _hw = await Task.Run(() => HardwareAutoOptimizer.Detect(true));
            var sb = new StringBuilder();
            sb.AppendLine(HardwareAutoOptimizer.BuildDiagnostics(_hw));
            HwSummary.Text = sb.ToString();
            ShowAutoTweaks(_lastProfileId ?? HardwareAutoOptimizer.Profiles[0].Id);
            TitlePage.Text = Loc.Pick("AUTO-OTIMIZAÇÃO · ", "SMART OPTIMIZATION · ") + _hw.FormFactorLabel;
        }
        catch (Exception ex)
        {
            HwSummary.Text = "Erro: " + ex.Message;
        }
        finally
        {
            if (BtnHwScan != null) BtnHwScan.IsEnabled = true;
        }
    }

    private void ShowAutoTweaks(string profileId)
    {
        if (AutoTweaksText == null) return;
        var hw = _hw ?? HardwareAutoOptimizer.Detect();
        _hw = hw;
        var list = HardwareAutoOptimizer.AutoEligible(hw, profileId);
        var sb = new StringBuilder();
        sb.AppendLine(Loc.Pick(
            $"{list.Count} tweaks compatíveis com ESTE hardware e seguros para o modo automático:",
            $"{list.Count} tweaks compatible with THIS hardware and safe for automatic mode:"));
        sb.AppendLine($"{hw.FormFactorLabel} · {hw.CpuVendor} {hw.PhysicalCores}C/{hw.LogicalCores}T · {hw.GpuVendor} · {hw.RamGb:0} GB · {hw.StorageLabel}");
        if (hw.AntiCheats.Count > 0)
            sb.AppendLine(Loc.Pick("Anti-cheat: ", "Anti-cheat: ") + string.Join(", ", hw.AntiCheats) +
                          Loc.Pick(" → HVCI/hipervisor/integridade bloqueados automaticamente.", " → HVCI/hypervisor/integrity automatically locked."));
        sb.AppendLine();
        foreach (var t in list.OrderBy(t => t.Section).ThenBy(t => t.Risk))
            sb.AppendLine($"• [{t.RiskLabel,-8}] {t.Section,-8} {t.Title}");
        AutoTweaksText.Text = sb.ToString();
    }

    private async void ApplyProfile_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireAdmin()) return;
        if (ProfileList.SelectedItem is not HardwareAutoOptimizer.OptimizationProfile p)
        {
            MessageBox.Show(Loc.Pick("Escolha um perfil.", "Choose a profile."), "PHANTOM UI");
            return;
        }
        var hw = _hw ?? HardwareAutoOptimizer.Detect();
        int count = HardwareAutoOptimizer.AutoEligible(hw, p.Id).Count;
        var c = MessageBox.Show(Loc.Pick(
            $"Aplicar o perfil \"{p.Name}\"?\n\n{count} tweaks compatíveis com o seu hardware.\nSó entram itens de risco baixo/médio compatíveis: nada de Core Parking em notebook, nada de SysMain off em HDD, nada que quebre anti-cheat.\n\nUm ponto de restauração será criado antes.",
            $"Apply the \"{p.Name}\" profile?\n\n{count} tweaks compatible with your hardware.\nOnly compatible low/medium risk items: no Core Parking on laptops, no SysMain off on HDD, nothing that breaks anti-cheat.\n\nA restore point will be created first."),
            "PHANTOM UI", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (c != MessageBoxResult.Yes) return;

        if (BtnApplyProfile != null) BtnApplyProfile.IsEnabled = false;
        try
        {
            AutoTweaksText.Text = Loc.Pick("Criando ponto de restauração…", "Creating restore point…");
            await Task.Run(() => TweakJournal.CreateRestorePoint($"PhantomUI · {p.Name}"));
            var (ok, fail, msg) = await HardwareAutoOptimizer.ApplyProfileAsync(p.Id, false,
                step => Dispatcher.Invoke(() => AutoTweaksText.Text = Loc.Pick("Aplicando: ", "Applying: ") + step));
            AutoTweaksText.Text = msg + Environment.NewLine + Environment.NewLine + AutoTweaksText.Text;
            Toast(Loc.Pick("Perfil aplicado", "Profile applied"), msg, "\uE73E");
        }
        finally
        {
            if (BtnApplyProfile != null) BtnApplyProfile.IsEnabled = true;
        }
    }

    private async void RevertProfile_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireAdmin()) return;
        if (ProfileList.SelectedItem is not HardwareAutoOptimizer.OptimizationProfile p) return;
        var c = MessageBox.Show(Loc.Pick(
            $"Reverter o perfil \"{p.Name}\"? Todos os tweaks dele voltam ao padrão do Windows.",
            $"Revert the \"{p.Name}\" profile? All of its tweaks go back to the Windows default."),
            "PHANTOM UI", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (c != MessageBoxResult.Yes) return;
        var (ok, fail, msg) = await HardwareAutoOptimizer.ApplyProfileAsync(p.Id, true,
            step => Dispatcher.Invoke(() => AutoTweaksText.Text = Loc.Pick("Revertendo: ", "Reverting: ") + step));
        AutoTweaksText.Text = msg;
        Toast(Loc.Pick("Perfil revertido", "Profile reverted"), msg, "\uE7A7");
    }

    private async void RestorePoint_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireAdmin()) return;
        TweakStatus.Text = Loc.Pick("Criando ponto de restauração…", "Creating restore point…");
        var (ok, msg) = await Task.Run(() => TweakJournal.CreateRestorePoint("PhantomUI Manual Checkpoint"));
        Toast(ok ? Loc.Pick("Checkpoint criado", "Checkpoint created") : Loc.Pick("Aviso", "Warning"), msg, ok ? "\uE73E" : "\uE7BA");
        if (HwSummary != null) HwSummary.Text = msg;
    }
}
