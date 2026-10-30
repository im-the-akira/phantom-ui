using System.Windows;
using System.Windows.Controls;

namespace PhantomUI;

public partial class MainWindow
{
    private List<DetectedGame> _detectedGames = new();
    private bool _gameOptInit;

    private void InitGameOptPage()
    {
        if (_gameOptInit) return;
        _gameOptInit = true;
        GameDetect_Click(this, new RoutedEventArgs());
    }

    private async void GameDetect_Click(object sender, RoutedEventArgs e)
    {
        GameOptStatus.Text = Loc.Pick("Procurando jogos em Steam, Epic, Riot, Battle.net e pastas de config…",
                                       "Searching games in Steam, Epic, Riot, Battle.net and config folders…");
        if (BtnGameDetect != null) BtnGameDetect.IsEnabled = false;
        try
        {
            _detectedGames = await Task.Run(DirectGameOptimizer.Detect);
            GameOptGrid.ItemsSource = _detectedGames;
            int found = _detectedGames.Count(g => g.Detected);
            GameOptStatus.Text = Loc.Pick($"{found} de {_detectedGames.Count} jogos detectados nesta máquina.",
                                          $"{found} of {_detectedGames.Count} games detected on this machine.");
        }
        catch (Exception ex)
        {
            GameOptStatus.Text = ex.Message;
        }
        finally
        {
            if (BtnGameDetect != null) BtnGameDetect.IsEnabled = true;
        }
    }

    private async void GameOptApply_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: DetectedGame g }) return;
        if (!RequireAdmin()) return;
        GameOptStatus.Text = Loc.Pick("Otimizando ", "Optimizing ") + g.Name + "…";
        var res = await Task.Run(() => DirectGameOptimizer.Optimize(g));
        GameOptStatus.Text = res.Message.Split('\n')[0];
        if (HwSummary != null) { }
        Toast(res.Ok ? Loc.Pick("Jogo otimizado", "Game optimized") : Loc.Pick("Falha", "Failed"),
              g.Name + Environment.NewLine + res.Message, res.Ok ? "\uE73E" : "\uE7BA");
    }

    private async void GameOptRevert_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: DetectedGame g }) return;
        if (!RequireAdmin()) return;
        var res = await Task.Run(() => DirectGameOptimizer.RevertConfig(g));
        GameOptStatus.Text = res.Message;
        Toast(res.Ok ? Loc.Pick("Config revertida", "Config reverted") : Loc.Pick("Aviso", "Warning"), res.Message, res.Ok ? "\uE7A7" : "\uE7BA");
    }

    private async void GameOptAll_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireAdmin()) return;
        var list = _detectedGames.Where(g => g.Detected && g.Kind != GameConfigKind.LauncherOnly).ToList();
        if (list.Count == 0)
        {
            GameOptStatus.Text = Loc.Pick("Nenhum jogo detectado — clique em DETECTAR JOGOS.", "No game detected — click DETECT GAMES.");
            return;
        }
        var c = MessageBox.Show(Loc.Pick(
            $"Otimizar {list.Count} jogos detectados?\n\nCada um recebe GPU dedicada, QoS DSCP 46, prioridade High e a config nativa com backup (.phantomui.bak).",
            $"Optimize {list.Count} detected games?\n\nEach gets the dedicated GPU, QoS DSCP 46, High priority and the native config with a backup (.phantomui.bak)."),
            "PHANTOM UI", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (c != MessageBoxResult.Yes) return;

        if (BtnGameAll != null) BtnGameAll.IsEnabled = false;
        int ok = 0;
        foreach (var g in list)
        {
            GameOptStatus.Text = Loc.Pick("Otimizando ", "Optimizing ") + g.Name + "…";
            var r = await Task.Run(() => DirectGameOptimizer.Optimize(g));
            if (r.Ok) ok++;
        }
        GameOptStatus.Text = Loc.Pick($"{ok} jogos otimizados.", $"{ok} games optimized.");
        Toast(Loc.Pick("Jogos otimizados", "Games optimized"), GameOptStatus.Text, "\uE73E");
        if (BtnGameAll != null) BtnGameAll.IsEnabled = true;
    }
}
