using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PhantomUI;

public partial class MainWindow
{
    /// <summary>Atualiza os textos fixos da janela quando o idioma muda (PT-BR ⇄ EN).</summary>
    private void UpdateLanguageUi()
    {
        if (BtnLang != null)
        {
            BtnLang.Content = Loc.Instance.Code;
            BtnLang.ToolTip = Loc.Instance.LangTooltip;
        }
        if (TitlePage != null && string.IsNullOrWhiteSpace(TitlePage.Text))
            TitlePage.Text = Loc.Pick("Visão geral", "Overview");
        if (SideVersion != null)
            SideVersion.Text = $"v1.0.0 · {Loc.Pick("PT-BR", "EN")}";
    }

    private void Lang_Click(object sender, RoutedEventArgs e)
    {
        Loc.Toggle();
        _settings.Language = Loc.IsEn ? "en" : "pt";
        _settings.Save();
        UpdateLanguageUi();

        // Re-renderiza o que é gerado por código (bindings estáticos não trocam de idioma sozinhos)
        try { RenderSection(); } catch { }
        try { if (_hw != null) HwSummary.Text = HardwareAutoOptimizer.BuildDiagnostics(_hw); } catch { }
        try { if (_biosReport != null) { BiosSummary.Text = _biosReport.Summary; BiosItems.ItemsSource = null; BiosItems.ItemsSource = _biosReport.Items; BiosChecklist.ItemsSource = _biosReport.Checklist; } } catch { }
        try { if (_detectedGames.Count > 0) GameOptGrid.ItemsSource = null; GameOptGrid.ItemsSource = _detectedGames; } catch { }
        try { RepairList.ItemsSource = null; RepairList.ItemsSource = SystemDoctor.Actions(); } catch { }
        Toast(Loc.Pick("Idioma: Português (BR)", "Language: English"), Loc.Pick("Textos atualizados em tempo real.", "Texts updated live."), "\uE774");
    }

    // =====================================================================
    // Loop de segundo plano: limpeza automática de RAM + timer 0,5 ms em jogo
    // =====================================================================
    private DispatcherTimer? _watchTimer;
    private readonly HashSet<string> _gameProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "cod", "cs2", "valorant", "fortniteclient-win64-shipping", "r5apex", "bf2042", "bf6", "marvelrivals",
        "escapefromtarkov", "tslgame", "pioneergame", "deltaforceclient-win64-shipping", "discovery",
        "fivem", "gta5", "javaw", "rustclient", "leagueclient", "dota2", "overwatch", "rainbowsix",
        "destiny2", "aces", "robloxplayerbeta", "genshinimpact", "wutheringwaves"
    };
    private bool _timerBoosted;
    private DateTime _lastPurge = DateTime.MinValue;

    private void StartBackgroundWatchers()
    {
        _watchTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _watchTimer.Tick += (_, _) => BackgroundWatch();
        _watchTimer.Start();
    }

    private void BackgroundWatch()
    {
        try
        {
            // 1) Timer 0,5 ms apenas enquanto há jogo em primeiro plano
            if (_settings.GameTimer05Ms)
            {
                bool gaming = GameRunning();
                if (gaming && !_timerBoosted) { InputLagTools.SetHighPrecisionTimer05Ms(true); _timerBoosted = true; }
                else if (!gaming && _timerBoosted) { InputLagTools.SetHighPrecisionTimer05Ms(false); _timerBoosted = false; }
            }
            else if (_timerBoosted)
            {
                InputLagTools.SetHighPrecisionTimer05Ms(false);
                _timerBoosted = false;
            }

            // 2) Limpeza automática de RAM quando abaixo do limite configurado
            if (_settings.AutoRamPurge && (DateTime.Now - _lastPurge).TotalSeconds >= 30)
            {
                var (total, free, _) = InputLagTools.GetLiveMemoryAndTimer();
                if (total > 0 && free < _settings.AutoRamFreeThresholdMb)
                {
                    _lastPurge = DateTime.Now;
                    var (freed, count) = MemoryPurge.PurgeStandbyAndWorkingSets();
                    if (freed > 0)
                        Dispatcher.Invoke(() => Toast(
                            Loc.Pick("RAM liberada automaticamente", "RAM freed automatically"),
                            Loc.Pick($"{freed:0} MB liberados ({count} processos ajustados).", $"{freed:0} MB freed ({count} processes trimmed)."),
                            "\uE945"));
                }
            }
        }
        catch { }
    }

    private static bool GameRunning()
    {
        try
        {
            foreach (var p in System.Diagnostics.Process.GetProcesses())
            {
                try
                {
                    if (p.MainWindowHandle == IntPtr.Zero) continue;
                    string name = p.ProcessName.ToLowerInvariant();
                    foreach (var g in new[]
                    {
                        "cod", "cs2", "valorant", "fortnite", "r5apex", "bf2042", "bf6", "marvel", "tarkov",
                        "tslgame", "pioneer", "deltaforce", "discovery", "fivem", "gta5", "javaw", "rust", "league",
                        "dota2", "overwatch", "rainbowsix", "destiny2", "aces", "roblox", "genshin"
                    })
                        if (name.Contains(g, StringComparison.Ordinal)) return true;
                }
                catch { }
                finally { p.Dispose(); }
            }
        }
        catch { }
        return false;
    }
}
