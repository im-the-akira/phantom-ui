using System.Windows;

namespace PhantomUI;

public partial class MainWindow
{
    private List<DriverAuditRow> _auditRows = new();
    private List<GpuDriverTarget> _auditGpus = new();

    private void DrvAuditBusy(bool busy)
    {
        if (BtnDrvAudit != null) BtnDrvAudit.IsEnabled = !busy;
        if (BtnDrvGpuOfficial != null) BtnDrvGpuOfficial.IsEnabled = !busy;
        if (BtnDrvWhql != null) BtnDrvWhql.IsEnabled = !busy;
    }

    private async void BtnDrvAudit_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireAdmin()) return;
        DrvAuditBusy(true);
        DrvAuditStatus.Text = Loc.Pick("Lendo drivers publicados (pnputil /enum-drivers)…", "Reading published drivers (pnputil /enum-drivers)…");
        try
        {
            _auditRows = await Task.Run(DriverAudit.Audit);
            _auditGpus = await Task.Run(DriverAudit.GpuTargets);
            DrvAuditList.ItemsSource = _auditRows;

            int generic = _auditRows.Count(r => r.IsGeneric);
            int old = _auditRows.Count(r => r.IsOld);
            string gpus = string.Join(", ", _auditGpus.Select(g => $"{g.Vendor} {g.GpuName} ({g.InstalledVersion})"));
            DrvAuditStatus.Text = Loc.Pick(
                $"{_auditRows.Count} drivers · {generic} genéricos · {old} antigos · GPU: {gpus}",
                $"{_auditRows.Count} drivers · {generic} generic · {old} old · GPU: {gpus}");

            // Relatório persistente para o usuário anexar em suporte
            try
            {
                string dir = System.IO.Path.Combine(AppSettings.Folder, "Reports");
                System.IO.Directory.CreateDirectory(dir);
                await System.IO.File.WriteAllTextAsync(
                    System.IO.Path.Combine(dir, $"drivers_{DateTime.Now:yyyyMMdd_HHmmss}.txt"),
                    DriverAudit.BuildReport(_auditRows, _auditGpus));
            }
            catch { }
        }
        catch (Exception ex) { DrvAuditStatus.Text = "Falha: " + ex.Message; }
        finally { DrvAuditBusy(false); }
    }

    private async void BtnDrvGpuOfficial_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireAdmin()) return;
        if (_auditGpus.Count == 0)
        {
            DrvAuditStatus.Text = Loc.Pick("Audite primeiro para detectar a GPU.", "Audit first to detect the GPU.");
            return;
        }
        var withVendor = _auditGpus.Where(g => g.Vendor.Length > 0).ToList();
        if (withVendor.Count == 0)
        {
            DrvAuditStatus.Text = Loc.Pick("Fabricante de GPU não reconhecido — use o WHQL (Windows Update).", "GPU vendor not recognized — use WHQL (Windows Update).");
            return;
        }
        var target = withVendor[0];
        if (withVendor.Count > 1)
        {
            // deixa o usuário escolher (notebook com iGPU + dGPU)
            var names = withVendor.Select(g => $"{g.Vendor} — {g.GpuName}").ToArray();
            var pick = PickFromList(Loc.Pick("Escolha a GPU", "Choose the GPU"), names);
            if (pick < 0) return;
            target = withVendor[pick];
        }

        var c = MessageBox.Show(Loc.Pick(
            $"Atualizar o driver de {target.Vendor} — {target.GpuName}?\n\nInstalado: {target.InstalledVersion}\nFonte: site oficial do fabricante (assinatura conferida).\n\nRecomendado: feche jogos e overlays antes de instalar.",
            $"Update the {target.Vendor} driver for {target.GpuName}?\n\nInstalled: {target.InstalledVersion}\nSource: the vendor's official site (signature checked).\n\nRecommended: close games and overlays before installing."),
            "PHANTOM UI", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (c != MessageBoxResult.Yes) return;

        DrvAuditBusy(true);
        using var cts = new CancellationTokenSource();
        try
        {
            await Task.Run(() => TweakJournal.CreateRestorePoint("PhantomUI · antes do driver de GPU"));
            var (ok, msg) = await DriverAudit.UpdateGpuDriverAsync(target,
                (text, frac) => Dispatcher.Invoke(() => DrvAuditStatus.Text = $"{text} {(frac > 0 ? $"({frac:P0})" : "")}"), cts.Token);
            DrvAuditStatus.Text = msg;
            Toast(ok ? Loc.Pick("Driver oficial", "Official driver") : Loc.Pick("Atenção", "Attention"), msg, ok ? "\uE73E" : "\uE7BA");
        }
        catch (Exception ex) { DrvAuditStatus.Text = "Falha: " + ex.Message; }
        finally { DrvAuditBusy(false); }
    }

    private async void BtnDrvWhql_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireAdmin()) return;
        DrvAuditBusy(true);
        DrvAuditStatus.Text = Loc.Pick("Consultando drivers WHQL (política de Windows Update liberada só durante a consulta)…",
                                       "Querying WHQL drivers (Windows Update policy lifted only during the query)…");
        try
        {
            var (ok, drivers, msg) = await DriverAudit.SearchWhqlAsync();
            DrvAuditStatus.Text = msg + " " + Loc.Pick("Política de WU restaurada.", "WU policy restored.");
            if (!ok)
            {
                MessageBox.Show(msg, "PHANTOM UI", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (drivers.Count == 0) return;

            var c = MessageBox.Show(Loc.Pick(
                $"{drivers.Count} driver(s) WHQL oficial(is):\n\n{string.Join("\n", drivers.Take(12).Select(d => "  • " + d.Title))}\n\nInstalar todos agora?",
                $"{drivers.Count} official WHQL driver(s):\n\n{string.Join("\n", drivers.Take(12).Select(d => "  • " + d.Title))}\n\nInstall all now?"),
                "PHANTOM UI", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (c != MessageBoxResult.Yes) return;

            var (installed, result) = await DriverAudit.InstallWhqlAsync(drivers,
                line => Dispatcher.Invoke(() => DrvAuditStatus.Text = line));
            DrvAuditStatus.Text = result;
            Toast(installed ? Loc.Pick("WHQL instalado", "WHQL installed") : Loc.Pick("Falha", "Failed"), result, installed ? "\uE73E" : "\uE7BA");
        }
        catch (Exception ex) { DrvAuditStatus.Text = "Falha: " + ex.Message; }
        finally { DrvAuditBusy(false); }
    }

    /// <summary>Diálogo simples de escolha (usado quando há mais de uma GPU).</summary>
    private int PickFromList(string title, string[] items)
    {
        var dlg = new Window
        {
            Title = "PHANTOM UI",
            Width = 520, Height = 300, Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = (System.Windows.Media.Brush)FindResource("Bg")
        };
        var list = new System.Windows.Controls.ListBox
        {
            Margin = new Thickness(14), ItemsSource = items,
            Background = System.Windows.Media.Brushes.Transparent, BorderThickness = new Thickness(0)
        };
        list.SelectedIndex = 0;
        var ok = new System.Windows.Controls.Button
        {
            Content = Loc.Pick("SELECIONAR", "SELECT"),
            Margin = new Thickness(14, 0, 14, 14), Padding = new Thickness(12, 8, 12, 8),
            Style = (Style)FindResource("PrimaryButton")
        };
        var dock = new System.Windows.Controls.DockPanel();
        System.Windows.Controls.DockPanel.SetDock(ok, System.Windows.Controls.Dock.Bottom);
        dock.Children.Add(ok);
        dock.Children.Add(list);
        dlg.Content = dock;
        dlg.Title = title;
        int result = -1;
        ok.Click += (_, _) => { result = list.SelectedIndex; dlg.Close(); };
        dlg.ShowDialog();
        return result;
    }
}
