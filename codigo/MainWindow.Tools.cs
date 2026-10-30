using System.Diagnostics;
using System.Text;
using System.Windows;

namespace PhantomUI;

public partial class MainWindow
{
    // =====================================================================
    // BIOS & PLACA-MÃE (somente leitura)
    // =====================================================================
    private BiosReport? _biosReport;
    private bool _biosInit;

    private void InitBiosPage()
    {
        if (_biosInit) return;
        _biosInit = true;
        BiosAnalyze_Click(this, new RoutedEventArgs());
    }

    private async void BiosAnalyze_Click(object sender, RoutedEventArgs e)
    {
        if (BtnBiosAnalyze != null) BtnBiosAnalyze.IsEnabled = false;
        BiosStatus.Text = Loc.Pick("Lendo UEFI, Secure Boot, TPM, XMP/EXPO, ReBAR e virtualização…",
                                   "Reading UEFI, Secure Boot, TPM, XMP/EXPO, ReBAR and virtualization…");
        try
        {
            _biosReport = await Task.Run(() => BiosAdvisor.Analyze(_hw ?? HardwareAutoOptimizer.Detect()));
            BiosSummary.Text = _biosReport.Summary;
            BiosItems.ItemsSource = _biosReport.Items;
            BiosChecklist.ItemsSource = _biosReport.Checklist;
            BiosStatus.Text = Loc.Pick(
                "Diagnóstico concluído. Nenhuma escrita foi feita no firmware.",
                "Diagnostics complete. Nothing was written to the firmware.");
            await BiosAdvisor.SaveReportAsync(BiosAdvisor.ToText(_biosReport));
        }
        catch (Exception ex)
        {
            BiosStatus.Text = ex.Message;
        }
        finally
        {
            if (BtnBiosAnalyze != null) BtnBiosAnalyze.IsEnabled = true;
        }
    }

    private void BiosOpen_Click(object sender, RoutedEventArgs e)
    {
        string url = _biosReport?.SupportUrl ?? "https://www.asus.com/support/";
        // SupportUrl pode conter texto descritivo depois do link
        var match = System.Text.RegularExpressions.Regex.Match(url, @"https?://[^\s]+");
        if (match.Success) url = match.Value;
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (Exception ex) { BiosStatus.Text = ex.Message; }
    }

    private void BiosReboot_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireAdmin()) return;
        var c = MessageBox.Show(Loc.Pick(
            "Reiniciar AGORA direto no setup da BIOS?\n\nA máquina reinicia em 5 segundos. Salve o que estiver aberto.",
            "Reboot NOW straight into BIOS setup?\n\nThe machine restarts in 5 seconds. Save anything open."),
            "PHANTOM UI", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (c != MessageBoxResult.Yes) return;
        var (ok, msg) = BiosAdvisor.RebootToFirmware(true);
        BiosStatus.Text = msg;
        if (!ok) MessageBox.Show(msg, "PHANTOM UI", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // =====================================================================
    // DIAGNÓSTICO & REPAROS
    // =====================================================================
    private bool _repairInit;

    private void InitRepairPage()
    {
        if (_repairInit) return;
        _repairInit = true;
        RepairList.ItemsSource = SystemDoctor.Actions();
        RepairList.SelectedIndex = 0;
        DoctorScan_Click(this, new RoutedEventArgs());
    }

    private async void DoctorScan_Click(object sender, RoutedEventArgs e)
    {
        if (BtnDoctorScan != null) BtnDoctorScan.IsEnabled = false;
        RepairLog.Text += Environment.NewLine + Loc.Pick("=== ESCANEANDO ===", "=== SCANNING ===") + Environment.NewLine;
        try
        {
            var findings = await SystemDoctor.ScanAsync(msg => Dispatcher.Invoke(() => AppendRepairLog(msg)));
            FindingsList.ItemsSource = findings;
            int crit = findings.Count(f => f.Severity == FindingSeverity.Critical);
            int warn = findings.Count(f => f.Severity == FindingSeverity.Warning);
            AppendRepairLog(Loc.Pick($"Concluído: {crit} crítico(s) e {warn} aviso(s).", $"Done: {crit} critical and {warn} warning(s)."));
        }
        catch (Exception ex)
        {
            AppendRepairLog("Erro: " + ex.Message);
        }
        finally
        {
            if (BtnDoctorScan != null) BtnDoctorScan.IsEnabled = true;
        }
    }

    private void AppendRepairLog(string line)
    {
        if (RepairLog == null) return;
        RepairLog.Text += line + Environment.NewLine;
        RepairLog.ScrollToEnd();
    }

    private async void RunRepair_Click(object sender, RoutedEventArgs e)
    {
        if (RepairList.SelectedItem is not RepairAction action)
        {
            AppendRepairLog(Loc.Pick("Selecione uma ação de reparo.", "Select a repair action."));
            return;
        }
        if (!RequireAdmin()) return;

        string warn = action.NeedsReboot
            ? Loc.Pick("\n\n⚠ Esta ação pode exigir reinício.", "\n\n⚠ This action may require a reboot.")
            : "";
        if (action.Long)
            warn += Loc.Pick("\n\n⚠ Pode levar de 5 a 20 minutos. Não feche o programa.", "\n\n⚠ May take 5 to 20 minutes. Do not close the program.");
        if (action.Risk == "Médio")
            warn += Loc.Pick("\n\nRisco médio: leia a descrição antes de continuar.", "\n\nMedium risk: read the description before continuing.");

        var c = MessageBox.Show($"{action.Title}\n\n{action.Desc}\n\n{action.Command}{warn}\n\n" +
                                Loc.Pick("Continuar?", "Continue?"),
                                "PHANTOM UI", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (c != MessageBoxResult.Yes) return;

        if (BtnRunRepair != null) BtnRunRepair.IsEnabled = false;
        AppendRepairLog($"> {action.Command}");
        try
        {
            var (ok, output) = await SystemDoctor.RunActionAsync(action,
                line => Dispatcher.Invoke(() => AppendRepairLog(line)));
            AppendRepairLog(ok ? "✔ " + action.Title : "✖ " + Loc.Pick("Falhou ou pediu atenção", "Failed or needs attention"));
            Toast(action.Title, ok ? Loc.Pick("Concluído", "Completed") : Loc.Pick("Verifique o log", "Check the log"), ok ? "\uE73E" : "\uE7BA");
        }
        catch (Exception ex)
        {
            AppendRepairLog("Erro: " + ex.Message);
        }
        finally
        {
            if (BtnRunRepair != null) BtnRunRepair.IsEnabled = true;
        }
        DoctorScan_Click(this, new RoutedEventArgs());
    }

    // =====================================================================
    // WINDOWS PRÉ-OTIMIZADO (ISO / USB / instalação limpa)
    // =====================================================================
    private bool _isoInit;

    private void InitIsoPage()
    {
        if (_isoInit) return;
        _isoInit = true;
        WorkDirBox.Text = WindowsIsoBuilder.DefaultWorkDir;
        RefreshUsb_Click(this, new RoutedEventArgs());
    }

    private void AppendIsoLog(string line)
    {
        if (IsoLog == null) return;
        IsoLog.Text += line + Environment.NewLine;
        IsoLog.ScrollToEnd();
    }

    private void RefreshUsb_Click(object sender, RoutedEventArgs e)
    {
        var drives = WindowsIsoBuilder.RemovableDrives();
        UsbBox.Items.Clear();
        UsbBox.Items.Add(Loc.Pick("— não usar pendrive —", "— do not use USB —"));
        foreach (var d in drives)
            UsbBox.Items.Add($"{d.Letter}: · {d.Label} · {d.SizeGb:0.#} GB");
        UsbBox.SelectedIndex = 0;
    }

    private void PickIso_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = Loc.Pick("Selecione a ISO oficial do Windows", "Select the official Windows ISO"),
            Filter = "Imagem ISO (*.iso)|*.iso|Todos os arquivos (*.*)|*.*"
        };
        if (dlg.ShowDialog() == true) IsoPathBox.Text = dlg.FileName;
    }

    private void PickWorkDir_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog { Title = Loc.Pick("Pasta de trabalho", "Work folder") };
        if (dlg.ShowDialog() == true) WorkDirBox.Text = dlg.FolderName;
    }

    private IsoBuildOptions CollectIsoOptions()
    {
        string usb = "";
        if (UsbBox.SelectedIndex > 0 && UsbBox.SelectedItem is string s && s.Length > 0)
            usb = s[..1];

        return new IsoBuildOptions
        {
            SourceIso = IsoPathBox.Text.Trim(),
            WorkDir = WorkDirBox.Text.Trim(),
            TargetUsb = usb,
            OptimizeOs = ChkIsoOptimize.IsChecked == true,
            RemoveBloat = ChkIsoDebloat.IsChecked == true,
            InjectDrivers = ChkIsoDrivers.IsChecked == true,
            SkipOobe = ChkIsoOobe.IsChecked == true,
            DisableTelemetry = ChkIsoTelemetry.IsChecked == true,
            DisableCopilotRecall = ChkIsoAi.IsChecked == true,
            DisableBitLocker = ChkIsoBitLocker.IsChecked == true,
            DisableReservedStorage = ChkIsoReserved.IsChecked == true,
            CreateIso = ChkIsoCreateIso.IsChecked == true,
            ComputerName = IsoPcName.Text.Trim(),
            UserName = IsoUserName.Text.Trim(),
            Password = IsoPassword.Text,
            OutputIso = WindowsIsoBuilder.DefaultOutputIso
        };
    }

    private async void IsoBuild_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireAdmin()) return;
        var o = CollectIsoOptions();
        if (!System.IO.File.Exists(o.SourceIso))
        {
            MessageBox.Show(Loc.Pick("Selecione a ISO oficial do Windows primeiro.", "Select the official Windows ISO first."),
                            "PHANTOM UI", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var c = MessageBox.Show(Loc.Pick(
            "Gerar a mídia do Windows pré-otimizado?\n\nA pasta de trabalho é limpa e recriada. Se você escolheu um pendrive, ele é FORMATADO.\n\nDuração típica: 15 a 50 minutos (DISM offline).",
            "Generate the pre-optimized Windows media?\n\nThe work folder is wiped and recreated. If you picked a USB drive, it is FORMATTED.\n\nTypical time: 15 to 50 minutes (offline DISM)."),
            "PHANTOM UI", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (c != MessageBoxResult.Yes) return;

        if (BtnIsoBuild != null) BtnIsoBuild.IsEnabled = false;
        IsoLog.Text = "";
        AppendIsoLog(Loc.Pick("=== GERANDO MÍDIA PRÉ-OTIMIZADA ===", "=== BUILDING PRE-OPTIMIZED MEDIA ==="));
        try
        {
            var result = await WindowsIsoBuilder.BuildAsync(o, line => Dispatcher.Invoke(() => AppendIsoLog(line)));
            AppendIsoLog(result.Message);
            if (result.AnswerFile != null) AppendIsoLog("autounattend.xml: " + result.AnswerFile);
            if (result.OutIso != null) AppendIsoLog("ISO: " + result.OutIso);
            Toast(Loc.Pick("Mídia gerada", "Media generated"), result.Message, result.Ok ? "\uE73E" : "\uE7BA");
        }
        catch (Exception ex)
        {
            AppendIsoLog("Erro: " + ex.Message);
        }
        finally
        {
            if (BtnIsoBuild != null) BtnIsoBuild.IsEnabled = true;
        }
    }

    private async void IsoDirectInstall_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireAdmin()) return;
        var o = CollectIsoOptions();
        var c = MessageBox.Show(Loc.Pick(
            "INSTALAÇÃO LIMPA SEM PENDRIVE\n\nIsto monta a ISO e inicia o setup.exe /auto clean: o disco do sistema é APAGADO e o Windows é reinstalado já otimizado.\n\nSalve seus arquivos antes. Continuar?",
            "CLEAN INSTALL WITHOUT USB\n\nThis mounts the ISO and starts setup.exe /auto clean: the system disk is WIPED and Windows is reinstalled already optimized.\n\nBack up your files first. Continue?"),
            "PHANTOM UI", MessageBoxButton.YesNo, MessageBoxImage.Stop);
        if (c != MessageBoxResult.Yes) return;

        var (ok, msg) = await WindowsIsoBuilder.InstallWithoutUsbAsync(o, true);
        AppendIsoLog(msg);
        if (!ok) MessageBox.Show(msg, "PHANTOM UI", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void IsoResetPc_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireAdmin()) return;
        var c = MessageBox.Show(Loc.Pick(
            "Reinstalação limpa com a imagem do próprio Windows (systemreset -cleanpc).\n\nO Windows mostra as opções na tela (manter ou remover arquivos). Continuar?",
            "Clean reinstall using the Windows image itself (systemreset -cleanpc).\n\nWindows shows the options on screen (keep or remove files). Continue?"),
            "PHANTOM UI", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (c != MessageBoxResult.Yes) return;
        var (_, msg) = WindowsIsoBuilder.ResetPcClean(true);
        AppendIsoLog(msg);
    }
}
