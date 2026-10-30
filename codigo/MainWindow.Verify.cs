using System.IO;
using System.Text;
using System.Windows;

namespace PhantomUI;

/// <summary>
/// Página "Verificação &amp; Relatório": confere a integridade do próprio catálogo de tweaks
/// e gera o relatório da máquina para suporte.
/// </summary>
public partial class MainWindow
{
    private CatalogIntegrity.Result? _verifyResult;

    private void InitVerifyPage()
    {
        if (_verifyResult != null) return;
        VerifyRun_Click(this, new RoutedEventArgs());
    }

    private async void VerifyRun_Click(object sender, RoutedEventArgs e)
    {
        BtnVerifyRun.IsEnabled = false;
        VerifyHeadline.Text = Loc.Pick("Verificando catálogo…", "Checking catalog…");
        VerifyStats.Text = "";
        try
        {
            _verifyResult = await Task.Run(CatalogIntegrity.Verify);
            var r = _verifyResult;
            VerifyHeadline.Text = r.Headline;
            VerifyHeadline.Foreground = r.Ok
                ? (System.Windows.Media.Brush)FindResource("RiskLow")
                : (System.Windows.Media.Brush)FindResource("RiskHigh");
            VerifyStats.Text = Loc.Pick(
                $"{r.Tweaks} tweaks em {r.Sections} seções · {r.Profiles} perfis · {r.Blocked} bloqueados por segurança · {r.Commands} comandos renderizados",
                $"{r.Tweaks} tweaks in {r.Sections} sections · {r.Profiles} profiles · {r.Blocked} blocked for safety · {r.Commands} rendered commands");
            VerifyIssues.ItemsSource = r.Issues;
            if (r.Issues.Count == 0) VerifyIssues.ItemsSource = new List<CatalogIntegrity.Issue>
            {
                new() { Kind = "OK", Detail = Loc.Pick("Nenhum id duplicado, nenhum perfil quebrado, nenhum comando perigoso fora da seção de bloqueio.", "No duplicated id, no broken profile, no dangerous command outside the blocked section.") }
            };
        }
        catch (Exception ex)
        {
            VerifyHeadline.Text = Loc.Pick("A verificação falhou: ", "Verification failed: ") + ex.Message;
        }
        finally { BtnVerifyRun.IsEnabled = true; }
    }

    private void VerifyCopy_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var r = _verifyResult ?? CatalogIntegrity.Verify();
            var sb = new StringBuilder();
            sb.AppendLine(Loc.Pick("PHANTOM UI · verificação do catálogo", "PHANTOM UI · catalog verification"));
            sb.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            sb.AppendLine($"tweaks: {r.Tweaks} · seções: {r.Sections} · perfis: {r.Profiles} · bloqueados: {r.Blocked} · comandos: {r.Commands}");
            sb.AppendLine(r.Headline);
            foreach (var i in r.Issues) sb.AppendLine($"[{i.Kind}] {i.Detail}");
            Clipboard.SetText(sb.ToString());
            VerifyStats.Text = Loc.Pick("Resultado copiado para a área de transferência.", "Result copied to the clipboard.");
        }
        catch (Exception ex) { VerifyStats.Text = ex.Message; }
    }

    private async void SupportGen_Click(object sender, RoutedEventArgs e)
    {
        BtnSupportGen.IsEnabled = false;
        SupportStatus.Text = Loc.Pick("Gerando relatório…", "Building report…");
        try
        {
            string text = await Task.Run(() => SupportReport.Build(_settings, m => Dispatcher.Invoke(() => SupportStatus.Text = m)));
            string dir = Path.Combine(AppSettings.Folder, "Reports");
            Directory.CreateDirectory(dir);
            string file = Path.Combine(dir, $"support_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            await File.WriteAllTextAsync(file, text, Encoding.UTF8);
            SupportStatus.Text = Loc.Pick($"Relatório salvo em {file}", $"Report saved to {file}");
            Toast(Loc.Pick("Relatório de suporte gerado", "Support report generated"), file, "\uE9D9");
        }
        catch (Exception ex)
        {
            SupportStatus.Text = Loc.Pick("Falhou ao gerar: ", "Failed to build: ") + ex.Message;
        }
        finally { BtnSupportGen.IsEnabled = true; }
    }

    private void SupportOpen_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string dir = Path.Combine(AppSettings.Folder, "Reports");
            Directory.CreateDirectory(dir);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = dir,
                UseShellExecute = true
            });
            SupportStatus.Text = dir;
        }
        catch (Exception ex) { SupportStatus.Text = ex.Message; }
    }
}
