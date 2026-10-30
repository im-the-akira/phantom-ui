using System.Windows;
using System.Windows.Controls;

namespace PhantomUI;

public partial class MainWindow
{
    // =====================================================================
    // PLANOS DE ENERGIA EMBUTIDOS (34 .pow)
    // =====================================================================
    private bool _planInit;
    private List<EmbeddedPowerPlan> _plans = new();

    private void InitPlansPage()
    {
        if (_planInit) return;
        _planInit = true;
        _plans = PowerPlanVault.GetEmbeddedCatalog();
        PlanCombo.ItemsSource = _plans;
        PlanCombo.DisplayMemberPath = nameof(EmbeddedPowerPlan.Name);
        if (_plans.Count > 0) PlanCombo.SelectedIndex = 0;
        PlanStatus.Text = $"{_plans.Count} " + Loc.Pick("planos disponíveis", "plans available");
        RefreshInstalledPlans();
    }

    private void PlanCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PlanCombo?.SelectedItem is not EmbeddedPowerPlan plan) return;
        PlanDesc.Text = $"{plan.Category} · {plan.Description}" +
                        (plan.RecommendedFor.Length > 0 ? $"  ·  {Loc.Pick("indicado para", "recommended for")}: {plan.RecommendedFor}" : "");
    }

    private void RefreshInstalledPlans()
    {
        try
        {
            var installed = PowerPlanVault.ListInstalled();
            if (PlanInstalled == null) return;
            PlanInstalled.ItemsSource = installed
                .Select(s => $"{s.Name}  ·  {s.Guid}" + (s.IsActive ? "   ← " + Loc.Pick("ATIVO", "ACTIVE") : ""))
                .ToList();
        }
        catch (Exception ex) { PlanStatus.Text = ex.Message; }
    }

    private void BtnPlanRefresh_Click(object sender, RoutedEventArgs e) => RefreshInstalledPlans();

    private async void BtnPlanApply_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireAdmin()) return;
        if (PlanCombo?.SelectedItem is not EmbeddedPowerPlan plan) return;
        var c = MessageBox.Show(Loc.Pick(
            $"Importar e ativar o plano \"{plan.Name}\"?\n\nO Windows cria um GUID novo; depois dá para voltar ao Equilibrado com um clique.\n\n{plan.Description}",
            $"Import and activate the \"{plan.Name}\" plan?\n\nWindows creates a new GUID; you can go back to Balanced with one click.\n\n{plan.Description}"),
            "PHANTOM UI", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (c != MessageBoxResult.Yes) return;

        PlanStatus.Text = Loc.Pick("Importando…", "Importing…");
        var res = await Task.Run(() => PowerPlanVault.ImportAndActivate(plan));
        PlanStatus.Text = res.Message;
        Toast(res.Ok ? Loc.Pick("Plano aplicado", "Plan applied") : Loc.Pick("Falha", "Failed"), res.Message, res.Ok ? "\uE73E" : "\uE7BA");
        RefreshInstalledPlans();
    }

    private async void BtnPlanBalanced_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireAdmin()) return;
        var res = await Task.Run(PowerPlanVault.RestoreBalancedDefault);
        PlanStatus.Text = res.Message;
        RefreshInstalledPlans();
    }

    // =====================================================================
    // BENCHMARK DE DNS
    // =====================================================================
    private bool _dnsInit;
    private List<DnsProviderResult> _dns = new();
    private CancellationTokenSource? _dnsCts;

    private void InitDnsPage()
    {
        if (_dnsInit) return;
        _dnsInit = true;
        _dns = DnsBenchmark.DefaultProviders();
        DnsResults.ItemsSource = _dns;
        DnsStatus.Text = Loc.Pick("Pronto. O teste leva ~15 s.", "Ready. The test takes ~15 s.");
    }

    private async void BtnDnsBench_Click(object sender, RoutedEventArgs e)
    {
        if (_dnsInit == false) InitDnsPage();
        if (BtnDnsBench != null) BtnDnsBench.IsEnabled = false;
        _dnsCts?.Cancel();
        _dnsCts = new CancellationTokenSource();
        DnsStatus.Text = Loc.Pick("Testando em paralelo (UDP + ICMP)…", "Testing in parallel (UDP + ICMP)…");
        try
        {
            await DnsBenchmark.RunBenchmarkAsync(_dns, _dnsCts.Token);
            var best = _dns.Where(d => d.AvgMs > 0).OrderBy(d => d.AvgMs).FirstOrDefault();
            DnsStatus.Text = best == null
                ? Loc.Pick("Nenhum provedor respondeu (firewall/antivírus bloqueando UDP 53?).", "No provider answered (firewall/antivirus blocking UDP 53?).")
                : Loc.Pick($"Mais rápido: {best.Name} ({best.AvgMs:0.0} ms)", $"Fastest: {best.Name} ({best.AvgMs:0.0} ms)");
            DnsResults.ItemsSource = null;
            DnsResults.ItemsSource = _dns;
        }
        catch (OperationCanceledException) { DnsStatus.Text = Loc.Pick("Cancelado.", "Cancelled."); }
        catch (Exception ex) { DnsStatus.Text = "Falha: " + ex.Message; }
        finally { if (BtnDnsBench != null) BtnDnsBench.IsEnabled = true; }
    }

    private void BtnDnsApply_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireAdmin()) return;
        if (DnsResults?.SelectedItem is not DnsProviderResult sel)
        {
            DnsStatus.Text = Loc.Pick("Selecione um provedor na lista.", "Select a provider in the list.");
            return;
        }
        var res = DnsBenchmark.ApplyDnsToActiveNic(sel.Primary, sel.Secondary);
        DnsStatus.Text = res.Message;
        Toast(res.Ok ? Loc.Pick("DNS aplicado", "DNS applied") : Loc.Pick("Falha", "Failed"), res.Message, res.Ok ? "\uE73E" : "\uE7BA");
    }

    private void BtnDnsAuto_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireAdmin()) return;
        var res = DnsBenchmark.RestoreDhcpDns();
        DnsStatus.Text = res.Message;
    }

    // =====================================================================
    // INPUT LAG · PCIe MSI MODE + FILTERKEYS
    // =====================================================================
    private bool _msiInit;
    private List<PciMsiDevice> _msiDevices = new();

    private void InitMsiPage()
    {
        if (_msiInit) return;
        _msiInit = true;
        ChkFilterKeys.IsChecked = TweakCatalog.CheckFilterKeys().Applied;
        BtnMsiScan_Click(this, new RoutedEventArgs());
    }

    private async void BtnMsiScan_Click(object sender, RoutedEventArgs e)
    {
        if (BtnMsiScan != null) BtnMsiScan.IsEnabled = false;
        MsiStatus.Text = Loc.Pick("Lendo dispositivos PCI/USB…", "Reading PCI/USB devices…");
        try
        {
            _msiDevices = await Task.Run(InputLagTools.ScanPciMsiDevices);
            MsiDevices.ItemsSource = _msiDevices;
            var (total, free, timer) = InputLagTools.GetLiveMemoryAndTimer();
            MsiStatus.Text = Loc.Pick(
                $"{_msiDevices.Count} dispositivos · RAM {free:0}/{total:0} MB · timer {timer}",
                $"{_msiDevices.Count} devices · RAM {free:0}/{total:0} MB · timer {timer}");
        }
        catch (Exception ex) { MsiStatus.Text = ex.Message; }
        finally { if (BtnMsiScan != null) BtnMsiScan.IsEnabled = true; }
    }

    private async void MsiApply(bool enableMsi, int priority)
    {
        if (!RequireAdmin()) return;
        if (MsiDevices?.SelectedItem is not PciMsiDevice dev)
        {
            MsiStatus.Text = Loc.Pick("Selecione um dispositivo na lista.", "Select a device in the list.");
            return;
        }
        var res = await Task.Run(() => InputLagTools.SetDeviceMsiAndPriority(dev, enableMsi, priority));
        MsiStatus.Text = res.Message;
        Toast(res.Ok ? "MSI / IRQ" : Loc.Pick("Falha", "Failed"), res.Message, res.Ok ? "\uE73E" : "\uE7BA");
        MsiDevices.Items.Refresh();
    }

    private void MsiDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MsiDevices?.SelectedItem is not PciMsiDevice dev) return;
        MsiStatus.Text = $"{dev.Name} · {dev.StateBadge} · {dev.PriorityLabel}";
    }

    private void BtnMsiOn_Click(object sender, RoutedEventArgs e) => MsiApply(true, 3);
    private void BtnMsiOff_Click(object sender, RoutedEventArgs e) => MsiApply(false, 0);

    private void ChkFilterKeys_Changed(object sender, RoutedEventArgs e)
    {
        if (ChkFilterKeys == null) return;
        bool block = ChkFilterKeys.IsChecked == true;
        var res = InputLagTools.ApplyFilterKeys(!block, 5000, 1000, 500, 0);
        MsiStatus.Text = res.Message;
        if (!res.Ok) ChkFilterKeys.IsChecked = TweakCatalog.CheckFilterKeys().Applied;
    }
}
