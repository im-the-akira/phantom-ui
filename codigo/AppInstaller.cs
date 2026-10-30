using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Win32;

namespace PhantomUI;

/// <summary>App do catálogo "Instalar apps" (pós-formatação), instalado pelo winget.</summary>
public sealed class InstallableApp : INotifyPropertyChanged
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string WingetId { get; init; } = "";
    /// <summary>Começo do nome em "Aplicativos instalados" (Painel de Controle), para saber se já está instalado.</summary>
    public string DisplayPrefix { get; init; } = "";
    public bool PreSelect { get; init; } = true;
    /// <summary>Categoria exibida na aba Apps (Runtimes, Launchers, Jogos, Streaming, Ferramentas).</summary>
    public string Category { get; init; } = "Ferramentas";
    /// <summary>Página oficial do fabricante (sempre a fonte primária do winget).</summary>
    public string OfficialUrl { get; init; } = "";

    private bool _selected, _installed;
    private string _status = "";

    public bool Selected { get => _selected; set => Set(ref _selected, value); }
    public bool Installed
    {
        get => _installed;
        set { Set(ref _installed, value); OnChanged(nameof(CanSelect)); }
    }
    public bool CanSelect => !Installed;
    public string Status { get => _status; set => Set(ref _status, value); }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChanged([CallerMemberName] string? p = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    private void Set<T>(ref T field, T value, [CallerMemberName] string? p = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        OnChanged(p);
    }
}

/// <summary>
/// Catálogo de apps essenciais e instalação silenciosa pelo winget (Gerenciador de Pacotes do Windows), que baixa
/// sempre a versão oficial mais recente do site do fabricante e confere o hash do instalador.
/// </summary>
public static class AppInstaller
{
    public static List<InstallableApp> Catalog() => new()
    {
        // ---------------- RUNTIMES OFICIAIS (Microsoft / fabricante) ----------------
        new() { Name = "Visual C++ 2015-2022 (x64)", Description = "Runtime obrigatório de milhares de jogos. Versão oficial da Microsoft.", WingetId = "Microsoft.VCRedist.2015+.x64", DisplayPrefix = "Microsoft Visual C++ 2015", Category = "Runtimes", OfficialUrl = "https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist" },
        new() { Name = "Visual C++ 2015-2022 (x86)", Description = "Versão 32 bits — muitos jogos antigos ainda precisam dela.", WingetId = "Microsoft.VCRedist.2015+.x86", DisplayPrefix = "Microsoft Visual C++ 2015", Category = "Runtimes", OfficialUrl = "https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist" },
        new() { Name = ".NET Desktop Runtime 8", Description = "Necessário para apps modernos .NET 8 (incluindo este app).", WingetId = "Microsoft.DotNet.DesktopRuntime.8", DisplayPrefix = "Microsoft Windows Desktop Runtime", Category = "Runtimes", OfficialUrl = "https://dotnet.microsoft.com/download/dotnet/8.0" },
        new() { Name = ".NET Desktop Runtime 6", Description = "Compatibilidade com apps .NET 6 antigos.", WingetId = "Microsoft.DotNet.DesktopRuntime.6", DisplayPrefix = "Microsoft Windows Desktop Runtime", Category = "Runtimes", OfficialUrl = "https://dotnet.microsoft.com/download/dotnet/6.0", PreSelect = false },
        new() { Name = ".NET Framework 4.8.1", Description = "Base de apps e launchers clássicos.", WingetId = "Microsoft.DotNet.Framework.DeveloperPack_4", DisplayPrefix = ".NET Framework 4", Category = "Runtimes", OfficialUrl = "https://dotnet.microsoft.com/download/dotnet-framework", PreSelect = false },
        new() { Name = "DirectX End-User Runtime", Description = "D3DX/DSound usados por jogos antigos (o DirectX 12 já vem no Windows).", WingetId = "Microsoft.DirectX", DisplayPrefix = "Microsoft DirectX", Category = "Runtimes", OfficialUrl = "https://www.microsoft.com/download/details.aspx?id=35" },
        new() { Name = "DirectX Shader Compiler", Description = "Compilador de shader oficial da Microsoft usado por ferramentas de mod.", WingetId = "Microsoft.DirectXShaderCompiler", DisplayPrefix = "DirectX Shader Compiler", Category = "Runtimes", OfficialUrl = "https://github.com/microsoft/DirectXShaderCompiler", PreSelect = false },
        new() { Name = "Visual C++ 2013 (x64)", Description = "Runtime de 2013 exigido por jogos e anticheats antigos.", WingetId = "Microsoft.VCRedist.2013.x64", DisplayPrefix = "Microsoft Visual C++ 2013", Category = "Runtimes", OfficialUrl = "https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist", PreSelect = false },
        new() { Name = "Java Runtime 21 (Microsoft Build)", Description = "Necessário para Minecraft Java e launchers alternativos.", WingetId = "Microsoft.OpenJDK.21", DisplayPrefix = "Microsoft OpenJDK", Category = "Runtimes", OfficialUrl = "https://learn.microsoft.com/java/openjdk/download", PreSelect = false },
        new() { Name = "App Installer (winget)", Description = "Gerenciador de pacotes oficial do Windows.", WingetId = "Microsoft.AppInstaller", DisplayPrefix = "App Installer", Category = "Runtimes", OfficialUrl = "https://learn.microsoft.com/windows/package-manager/", PreSelect = false },
        new() { Name = "Windows PC Health Check", Description = "Ferramenta oficial de diagnóstico de compatibilidade do Windows 11.", WingetId = "Microsoft.WindowsPCHealthCheck", DisplayPrefix = "Windows PC Health Check", Category = "Runtimes", OfficialUrl = "https://aka.ms/GetPCHealthCheckApp", PreSelect = false },

        // ---------------- LAUNCHERS & JOGOS ----------------
        new() { Name = "Steam", Description = "Loja e biblioteca de jogos da Valve.", WingetId = "Valve.Steam", DisplayPrefix = "Steam", Category = "Launchers", OfficialUrl = "https://store.steampowered.com/about/" },
        new() { Name = "Epic Games Launcher", Description = "Fortnite, Rocket League e jogos grátis semanais.", WingetId = "EpicGames.EpicGamesLauncher", DisplayPrefix = "Epic Games Launcher", Category = "Launchers", OfficialUrl = "https://store.epicgames.com/download" },
        new() { Name = "GOG Galaxy", Description = "Launcher da CD Projekt com jogos sem DRM.", WingetId = "GOG.Galaxy", DisplayPrefix = "GOG Galaxy", Category = "Launchers", OfficialUrl = "https://www.gog.com/galaxy", PreSelect = false },
        new() { Name = "Battle.net", Description = "Launcher da Blizzard (Warzone, Overwatch, Diablo).", WingetId = "Blizzard.BattleNet", DisplayPrefix = "Battle.net", Category = "Launchers", OfficialUrl = "https://www.blizzard.com/download", PreSelect = false },
        new() { Name = "Riot Client / VALORANT", Description = "Cliente oficial da Riot (Valorant, LoL). Exige Vanguard.", WingetId = "RiotGames.RiotClient", DisplayPrefix = "Riot", Category = "Launchers", OfficialUrl = "https://www.riotgames.com/pt-br/download", PreSelect = false },
        new() { Name = "EA app", Description = "Substitui o Origin para jogos EA.", WingetId = "ElectronicArts.EADesktop", DisplayPrefix = "EA app", Category = "Launchers", OfficialUrl = "https://www.ea.com/ea-app", PreSelect = false },
        new() { Name = "Ubisoft Connect", Description = "Launcher da Ubisoft.", WingetId = "Ubisoft.Connect", DisplayPrefix = "Ubisoft Connect", Category = "Launchers", OfficialUrl = "https://ubisoftconnect.com/", PreSelect = false },
        new() { Name = "Rockstar Games Launcher", Description = "GTA V, RDR2 e GTA Online.", WingetId = "RockstarGames.Launcher", DisplayPrefix = "Rockstar Games", Category = "Launchers", OfficialUrl = "https://socialclub.rockstargames.com/rockstar-games-launcher", PreSelect = false },
        new() { Name = "Amazon Games", Description = "Launcher da Amazon (New World, Lost Ark).", WingetId = "Amazon.Games", DisplayPrefix = "Amazon Games", Category = "Launchers", OfficialUrl = "https://gaming.amazon.com/", PreSelect = false },
        new() { Name = "Xbox App", Description = "PC Game Pass oficial da Microsoft.", WingetId = "Microsoft.GamingApp", DisplayPrefix = "Xbox", Category = "Launchers", OfficialUrl = "https://www.xbox.com/apps/xbox-app", PreSelect = false },
        new() { Name = "Minecraft Launcher", Description = "Launcher oficial do Minecraft Java/Bedrock.", WingetId = "Mojang.MinecraftLauncher", DisplayPrefix = "Minecraft Launcher", Category = "Launchers", OfficialUrl = "https://www.minecraft.net/download", PreSelect = false },

        // ---------------- VOZ / STREAMING ----------------
        new() { Name = "Discord", Description = "Chat de voz e texto para jogar com os amigos.", WingetId = "Discord.Discord", DisplayPrefix = "Discord", Category = "Comunicação", OfficialUrl = "https://discord.com/download" },
        new() { Name = "TeamSpeak 3 Client", Description = "Voz de baixa latência usada em clãs e competitivo.", WingetId = "TeamSpeak.TeamSpeak3.Client", DisplayPrefix = "TeamSpeak 3", Category = "Comunicação", OfficialUrl = "https://www.teamspeak.com/downloads", PreSelect = false },
        new() { Name = "OBS Studio", Description = "Gravação e transmissão com encoder por hardware (NVENC/AMF/QSV).", WingetId = "OBSProject.OBSStudio", DisplayPrefix = "OBS Studio", Category = "Streaming", OfficialUrl = "https://obsproject.com/download", PreSelect = false },
        new() { Name = "Streamlabs Desktop", Description = "Alternativa ao OBS com alertas integrados.", WingetId = "Streamlabs.Streamlabs", DisplayPrefix = "Streamlabs", Category = "Streaming", OfficialUrl = "https://streamlabs.com/desktop", PreSelect = false },

        // ---------------- FERRAMENTAS ----------------
        new() { Name = "7-Zip", Description = "Compactador gratuito (zip, rar, 7z).", WingetId = "7zip.7zip", DisplayPrefix = "7-Zip", Category = "Ferramentas", OfficialUrl = "https://www.7-zip.org/" },
        new() { Name = "WinRAR", Description = "Compactador clássico (pago após o teste).", WingetId = "RARLab.WinRAR", DisplayPrefix = "WinRAR", Category = "Ferramentas", OfficialUrl = "https://www.win-rar.com/", PreSelect = false },
        new() { Name = "VLC", Description = "Player de vídeo e música que abre qualquer formato.", WingetId = "VideoLAN.VLC", DisplayPrefix = "VLC media player", Category = "Ferramentas", OfficialUrl = "https://www.videolan.org/vlc/" },
        new() { Name = "Google Chrome", Description = "Navegador do Google.", WingetId = "Google.Chrome", DisplayPrefix = "Google Chrome", Category = "Ferramentas", OfficialUrl = "https://www.google.com/chrome/" },
        new() { Name = "Firefox", Description = "Navegador independente da Mozilla.", WingetId = "Mozilla.Firefox", DisplayPrefix = "Mozilla Firefox", Category = "Ferramentas", OfficialUrl = "https://www.mozilla.org/firefox/", PreSelect = false },
        new() { Name = "Lightshot", Description = "Captura de tela rápida com a tecla Print Screen.", WingetId = "Skillbrains.Lightshot", DisplayPrefix = "Lightshot", Category = "Ferramentas", OfficialUrl = "https://app.prntscr.com/" },
        new() { Name = "Everything", Description = "Busca instantânea de arquivos (substitui a indexação lenta).", WingetId = "voidtools.Everything", DisplayPrefix = "Everything", Category = "Ferramentas", OfficialUrl = "https://www.voidtools.com/", PreSelect = false },
        new() { Name = "HWiNFO64", Description = "Monitor de sensores e temperatura (referência de diagnóstico).", WingetId = "REALiX.HWiNFO", DisplayPrefix = "HWiNFO", Category = "Ferramentas", OfficialUrl = "https://www.hwinfo.com/download/", PreSelect = false },
        new() { Name = "CrystalDiskInfo", Description = "Saúde S.M.A.R.T. dos discos, oficial do fabricante.", WingetId = "CrystalDewWorld.CrystalDiskInfo", DisplayPrefix = "CrystalDiskInfo", Category = "Ferramentas", OfficialUrl = "https://crystalmark.info/en/software/crystaldiskinfo/", PreSelect = false },
        new() { Name = "CrystalDiskMark", Description = "Benchmark de leitura/escrita de disco.", WingetId = "CrystalDewWorld.CrystalDiskMark", DisplayPrefix = "CrystalDiskMark", Category = "Ferramentas", OfficialUrl = "https://crystalmark.info/en/software/crystaldiskmark/", PreSelect = false },
        new() { Name = "MSI Afterburner", Description = "Overclock, curva de fan e overlay de monitoramento.", WingetId = "Guru3D.Afterburner", DisplayPrefix = "MSI Afterburner", Category = "Ferramentas", OfficialUrl = "https://www.msi.com/Landing/afterburner", PreSelect = false },
        new() { Name = "GPU-Z", Description = "Informações técnicas da GPU (VRAM real, ReBAR, PCIe link).", WingetId = "TechPowerUp.GPU-Z", DisplayPrefix = "GPU-Z", Category = "Ferramentas", OfficialUrl = "https://www.techpowerup.com/gpuz/", PreSelect = false },
        new() { Name = "Notepad++", Description = "Editor de texto para configs de jogo e scripts.", WingetId = "Notepad++.Notepad++", DisplayPrefix = "Notepad++", Category = "Ferramentas", OfficialUrl = "https://notepad-plus-plus.org/downloads/", PreSelect = false },
        new() { Name = "DDU (Display Driver Uninstaller)", Description = "Remoção limpa de driver de vídeo antes de reinstalar o oficial.", WingetId = "Wagnardsoft.DisplayDriverUninstaller", DisplayPrefix = "Display Driver Uninstaller", Category = "Ferramentas", OfficialUrl = "https://www.wagnardsoft.com/", PreSelect = false },
        new() { Name = "PowerToys", Description = "Utilitários oficiais da Microsoft (FancyZones, PowerRename).", WingetId = "Microsoft.PowerToys", DisplayPrefix = "PowerToys", Category = "Ferramentas", OfficialUrl = "https://learn.microsoft.com/windows/powertoys/", PreSelect = false },
        new() { Name = "qBittorrent", Description = "Cliente torrent open source (sem anúncios).", WingetId = "qBittorrent.qBittorrent", DisplayPrefix = "qBittorrent", Category = "Ferramentas", OfficialUrl = "https://www.qbittorrent.org/download", PreSelect = false },
        new() { Name = "ShareX", Description = "Captura e gravação de tela com upload automático.", WingetId = "ShareX.ShareX", DisplayPrefix = "ShareX", Category = "Ferramentas", OfficialUrl = "https://getsharex.com/", PreSelect = false }
    };

    public static string WingetPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\WindowsApps\winget.exe");

    public static bool WingetAvailable => File.Exists(WingetPath);

    /// <summary>Nomes em "Aplicativos instalados" (máquina 64/32 bits e usuário atual).</summary>
    public static HashSet<string> InstalledNames()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        const string path = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
        foreach (var (hive, view) in new[] { (RegistryHive.LocalMachine, RegistryView.Registry64), (RegistryHive.LocalMachine, RegistryView.Registry32),
                                             (RegistryHive.CurrentUser, RegistryView.Default) })
        {
            try
            {
                using var root = RegistryKey.OpenBaseKey(hive, view).OpenSubKey(path);
                if (root == null) continue;
                foreach (var sub in root.GetSubKeyNames())
                {
                    using var k = root.OpenSubKey(sub);
                    if (k?.GetValue("DisplayName") is string n && !string.IsNullOrWhiteSpace(n)) names.Add(n.Trim());
                }
            }
            catch { }
        }
        return names;
    }

    public static void RefreshInstalled(IEnumerable<InstallableApp> apps)
    {
        var names = InstalledNames();
        foreach (var a in apps)
        {
            a.Installed = names.Any(n => n.StartsWith(a.DisplayPrefix, StringComparison.OrdinalIgnoreCase));
            if (a.Installed) a.Selected = false;
        }
    }

    /// <summary>Instala um app em silêncio. Retorna (ok, mensagem curta).</summary>
    public static async Task<(bool Ok, string Message)> InstallAsync(InstallableApp app, CancellationToken ct, bool upgrade = false)
    {
        var psi = new ProcessStartInfo(WingetPath,
            $"{(upgrade ? "upgrade" : "install")} --id {app.WingetId} -e --silent --accept-package-agreements --accept-source-agreements --disable-interactivity")
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8,
        };
        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Não foi possível iniciar o winget.");
        var outTask = p.StandardOutput.ReadToEndAsync(ct);
        var errTask = p.StandardError.ReadToEndAsync(ct);
        try { await p.WaitForExitAsync(ct); }
        catch (OperationCanceledException) { try { p.Kill(true); } catch { } throw; }
        string output = await outTask + await errTask;

        return p.ExitCode switch
        {
            0 => (true, "Instalado."),
            unchecked((int)0x8A15002B) or unchecked((int)0x8A150061) => (true, "Já estava instalado."),
            unchecked((int)0x8A150014) => (false, "Pacote não encontrado no winget."),
            unchecked((int)0x8A150008) => (false, "Falha no download: confira a internet e tente de novo."),
            _ => (false, $"Falhou (código 0x{p.ExitCode:X8}). {LastLine(output)}".Trim()),
        };
    }

    /// <summary>Última linha útil da saída do winget (sem as barras de progresso).</summary>
    private static string LastLine(string output) =>
        output.Split('\n', '\r').Select(l => l.Trim())
              .LastOrDefault(l => l.Length > 3 && !l.All(c => "-\\|/█▒ ".Contains(c)) && !l.Contains("KB /") && !l.Contains("MB /")) ?? "";
}
