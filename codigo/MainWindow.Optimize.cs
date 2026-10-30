using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PhantomUI;

public partial class MainWindow
{
    private bool _tweakInit;
    private TweakSectionInfo? _currentSection;
    private string _tweakFilter = "";

    // ============ inicialização da aba de Otimização Manual ============
    private void InitOptimizePage()
    {
        if (_tweakInit) return;
        _tweakInit = true;
        TweakCatalog.SettingsInstance = _settings;
        SectionList.ItemsSource = TweakCatalog.Sections;
        SectionList.DisplayMemberPath = nameof(TweakSectionInfo.Title);
        if (TweakCatalog.Sections.Count > 0) SectionList.SelectedIndex = 0;
    }

    private void SectionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _currentSection = SectionList.SelectedItem as TweakSectionInfo;
        RenderSection();
    }

    private void SectionFilter_TextChanged(object sender, TextChangedEventArgs e)
    {
        _tweakFilter = SectionFilter?.Text?.Trim() ?? "";
        RenderSection();
    }

    private void RenderSection()
    {
        if (_currentSection == null || TweakList == null) return;
        IEnumerable<TweakEntry> items = _currentSection.Tweaks;
        if (_tweakFilter.Length > 0)
            items = items.Where(t =>
                t.Title.Contains(_tweakFilter, StringComparison.OrdinalIgnoreCase) ||
                t.Desc.Contains(_tweakFilter, StringComparison.OrdinalIgnoreCase) ||
                t.Id.Contains(_tweakFilter, StringComparison.OrdinalIgnoreCase));
        var list = items.ToList();
        TweakList.ItemsSource = list;
        TweakStatus.Text = Loc.Pick(
            $"{_currentSection.Title}: {list.Count} tweaks · {_currentSection.Applied} já ativos nesta máquina.",
            $"{_currentSection.Title}: {list.Count} tweaks · {_currentSection.Applied} already active on this machine.");
        // Lê o estado ao vivo em segundo plano (registry/serviços) sem travar a UI
        _ = Task.Run(() =>
        {
            foreach (var t in list) t.RefreshState();
            Dispatcher.Invoke(() => TweakStatus.Text = Loc.Pick(
                $"{_currentSection.Title}: {list.Count} tweaks · {_currentSection.Applied} já ativos nesta máquina.",
                $"{_currentSection.Title}: {list.Count} tweaks · {_currentSection.Applied} already active on this machine."));
        });
    }

    private void RefreshStates_Click(object sender, RoutedEventArgs e)
    {
        TweakStatus.Text = Loc.Pick("Lendo estados ao vivo…", "Reading live states…");
        _ = Task.Run(() =>
        {
            TweakCatalog.RefreshAllStates();
            Dispatcher.Invoke(() =>
            {
                RenderSection();
                TweakStatus.Text = Loc.Pick("Estados atualizados.", "States refreshed.");
            });
        });
    }

    private void TweakApply_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: TweakEntry t }) return;
        if (!RequireAdmin()) return;
        if (t.IsBlocked)
        {
            MessageBox.Show(Loc.Pick(
                "Este ajuste está BLOQUEADO POR SEGURANÇA: é um mito destrutivo do acervo original (pagefile 0, TdrLevel 0, lsass em Idle etc.). O PHANTOM UI não aplica.",
                "This tweak is BLOCKED FOR SAFETY: a destructive myth from the original collection (pagefile 0, TdrLevel 0, lsass at Idle etc.). PHANTOM UI will not apply it."),
                "PHANTOM UI", MessageBoxButton.OK, MessageBoxImage.Stop);
            return;
        }
        if (t.Risk >= TweakRisk.High)
        {
            var c = MessageBox.Show(Loc.Pick(
                $"RISCO {t.RiskLabel}.\n\n{t.Title}\n\n{t.Desc}\n\nAplicar mesmo assim?",
                $"RISK {t.RiskLabel}.\n\n{t.Title}\n\n{t.Desc}\n\nApply anyway?"),
                "PHANTOM UI", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (c != MessageBoxResult.Yes) return;
        }
        var res = TweakJournal.ApplyTweak(t);
        TweakStatus.Text = res.Message;
        if (!res.Ok) Toast("Falha ao aplicar", res.Message, "\uE7BA");
        else Toast("Tweak aplicado", t.Title, "\uE73E");
        RenderSection();
    }

    private void TweakRevert_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: TweakEntry t }) return;
        if (!RequireAdmin()) return;
        var res = TweakJournal.RevertTweak(t);
        TweakStatus.Text = res.Message;
        Toast(res.Ok ? "Revertido" : "Falha ao reverter", res.Message, res.Ok ? "\uE7A7" : "\uE7BA");
        RenderSection();
    }

    private async void ApplySelected_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireAdmin()) return;
        var selected = TweakCatalog.All.Where(t => t.IsSelected && !t.IsBlocked).ToList();
        if (selected.Count == 0)
        {
            TweakStatus.Text = Loc.Pick("Marque os tweaks que deseja aplicar.", "Check the tweaks you want to apply.");
            return;
        }
        var risky = selected.Count(t => t.Risk >= TweakRisk.High);
        var c = MessageBox.Show(Loc.Pick(
            $"Aplicar {selected.Count} tweaks?\n\n{risky} de risco ALTO/CRÍTICO.\nTudo é salvo no diário transacional e pode ser desfeito depois (individual ou sessão).",
            $"Apply {selected.Count} tweaks?\n\n{risky} are HIGH/CRITICAL risk.\nEverything is saved in the transactional journal and can be undone later (individually or by session)."),
            "PHANTOM UI", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (c != MessageBoxResult.Yes) return;

        string session = TweakJournal.BeginSession(Loc.Pick($"Lote manual ({selected.Count} tweaks)", $"Manual batch ({selected.Count} tweaks)"));
        int ok = 0, fail = 0;
        foreach (var t in selected)
        {
            TweakStatus.Text = Loc.Pick("Aplicando: ", "Applying: ") + t.Title;
            var r = await Task.Run(() => TweakJournal.ApplyTweak(t, session));
            if (r.Ok) ok++; else fail++;
        }
        TweakStatus.Text = Loc.Pick($"{ok} aplicados, {fail} falhas. Sessão {session}.", $"{ok} applied, {fail} failed. Session {session}.");
        Toast(Loc.Pick("Lote concluído", "Batch finished"), TweakStatus.Text, "\uE73E");
        RenderSection();
    }

    private async void RevertSelected_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireAdmin()) return;
        var selected = TweakCatalog.All.Where(t => t.IsSelected).ToList();
        if (selected.Count == 0) { TweakStatus.Text = Loc.Pick("Marque os tweaks a reverter.", "Check the tweaks to revert."); return; }
        int ok = 0, fail = 0;
        foreach (var t in selected)
        {
            var r = await Task.Run(() => TweakJournal.RevertTweak(t));
            if (r.Ok) ok++; else fail++;
        }
        TweakStatus.Text = Loc.Pick($"{ok} revertidos, {fail} falhas.", $"{ok} reverted, {fail} failed.");
        RenderSection();
    }

    private void SessionRollback_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireAdmin()) return;
        var sessions = TweakJournal.ListSessions();
        if (sessions.Count == 0)
        {
            TweakStatus.Text = Loc.Pick("Nenhuma sessão de backup encontrada.", "No backup session found.");
            return;
        }
        var dlg = new Window
        {
            Title = Loc.Pick("Desfazer sessão", "Undo session"),
            Width = 560, Height = 420, Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = (Brush)FindResource("Bg")
        };
        var list = new ListBox { Margin = new Thickness(14), Background = Brushes.Transparent, BorderThickness = new Thickness(0) };
        list.ItemsSource = sessions;
        list.DisplayMemberPath = nameof(JournalSessionInfo.Summary);
        var btn = new Button
        {
            Content = Loc.Pick("DESFAZER ESTA SESSÃO", "UNDO THIS SESSION"),
            Margin = new Thickness(14, 0, 14, 14), Padding = new Thickness(12, 8, 12, 8),
            Style = (Style)FindResource("PrimaryButton")
        };
        var dock = new DockPanel();
        DockPanel.SetDock(btn, Dock.Bottom);
        dock.Children.Add(btn);
        dock.Children.Add(list);
        dlg.Content = dock;
        list.SelectedIndex = 0;
        btn.Click += (_, _) =>
        {
            if (list.SelectedItem is not JournalSessionInfo s) return;
            var c = MessageBox.Show(Loc.Pick(
                $"Desfazer TODAS as operações de: {s.Summary}?", $"Undo ALL operations from: {s.Summary}?"),
                "PHANTOM UI", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (c != MessageBoxResult.Yes) return;
            var (restored, errors) = TweakJournal.RollbackSession(s.SessionId);
            TweakStatus.Text = Loc.Pick($"{restored} operações restauradas, {errors.Count} erros.", $"{restored} operations restored, {errors.Count} errors.");
            dlg.Close();
            RenderSection();
        };
        dlg.ShowDialog();
    }
}
