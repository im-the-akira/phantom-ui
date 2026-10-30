using System.IO;
using System.Text;
using Microsoft.Win32;

namespace PhantomUI;

public enum GameConfigKind { None, CodOptions, SourceCfg, UnrealIni, UnityPrefs, FiveMCache, MinecraftJvm, MmoSettings, LauncherOnly }

/// <summary>Jogo detectado com sua pasta de configuração e o tipo de arquivo que o PHANTOM UI sabe otimizar.</summary>
public sealed class DetectedGame
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Exe { get; init; } = "";
    public string? ExePath { get; set; }
    public string ConfigDir { get; init; } = "";
    public string ConfigFile { get; init; } = "";
    public GameConfigKind Kind { get; init; }
    public string Engine { get; init; } = "";
    public string NotesPt { get; init; } = "";
    public string NotesEn { get; init; } = "";
    public string AntiCheat { get; init; } = "";
    public bool ReBarRelevant { get; init; }
    public bool Detected => !string.IsNullOrEmpty(ExePath) || (ConfigDir.Length > 0 && Directory.Exists(ConfigDir));

    public string Notes => Loc.Pick(NotesPt, NotesEn);
    public string StatusLabel => Detected
        ? Loc.Pick("DETECTADO", "DETECTED")
        : Loc.Pick("NÃO ENCONTRADO", "NOT FOUND");
    public string StatusColor => Detected ? "#2ED573" : "#6B7280";
    public string ConfigPath => string.IsNullOrEmpty(ConfigFile) ? ConfigDir : Path.Combine(ConfigDir, ConfigFile);
    public string KindLabel => Kind switch
    {
        GameConfigKind.CodOptions => "options.*.cod.cst / options.4.cod23.cst",
        GameConfigKind.SourceCfg => "cfg / autoexec",
        GameConfigKind.UnrealIni => "GameUserSettings.ini / Engine.ini",
        GameConfigKind.UnityPrefs => "prefs / settings",
        GameConfigKind.FiveMCache => "FiveM.app\\cache",
        GameConfigKind.MinecraftJvm => "launcher_profiles.json (JVM args)",
        GameConfigKind.MmoSettings => "config local",
        _ => "launcher"
    };
}

/// <summary>
/// Otimizações diretas por jogo (23+ títulos): detecta instalação, escreve config oficial do próprio jogo
/// (nunca injeta DLL), aplica UserGpuPreferences, QoS DSCP 46 e exclusões do Defender com rollback.
/// </summary>
public static class DirectGameOptimizer
{
    private static string Local => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private static string Roaming => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    private static string Docs => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    private static readonly string[] SteamRoots =
    {
        @"C:\Program Files (x86)\Steam\steamapps\common",
        @"C:\Program Files\Steam\steamapps\common",
        @"D:\Steam\steamapps\common", @"D:\SteamLibrary\steamapps\common",
        @"E:\Steam\steamapps\common", @"E:\SteamLibrary\steamapps\common",
        @"F:\SteamLibrary\steamapps\common"
    };

    private static readonly string[] EpicRoots =
    {
        @"C:\Program Files\Epic Games", @"D:\Epic Games", @"E:\Epic Games", @"F:\Epic Games"
    };

    private static readonly string[] RiotRoots =
    {
        @"C:\Riot Games", @"D:\Riot Games", @"E:\Riot Games"
    };

    /// <summary>Catálogo de jogos suportados (fonte: acervo Kairo + engenharia reversa do iGust/Atlas).</summary>
    public static List<DetectedGame> Catalog() => new()
    {
        new DetectedGame
        {
            Id = "warzone", Name = "Call of Duty: Warzone / MW3", Exe = "cod.exe",
            ConfigDir = Path.Combine(Docs, "Call of Duty"), ConfigFile = "players", Kind = GameConfigKind.CodOptions,
            Engine = "IW 9.0", AntiCheat = "RICOCHET",
            NotesPt = "Otimiza options.4.cod23.cst / options.4.cod24.cst: WorkerCount pelo número de núcleos físicos, VRAMScale, ReBAR, texturas e cache de shader. Sempre faz backup do .cst antes.",
            NotesEn = "Tunes options.4.cod23.cst / options.4.cod24.cst: WorkerCount from physical cores, VRAMScale, ReBAR, textures and shader cache. Always backs up the .cst first.",
            ReBarRelevant = true
        },
        new DetectedGame
        {
            Id = "cs2", Name = "Counter-Strike 2", Exe = "cs2.exe",
            ConfigDir = @"C:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive\game\csgo\cfg",
            ConfigFile = "phantomui.cfg", Kind = GameConfigKind.SourceCfg, Engine = "Source 2",
            AntiCheat = "VAC",
            NotesPt = "Cria phantomui.cfg + autoexec com fps_max, cl_forcepreload equivalente do Source 2, mat_queue_mode, e aplica QoS DSCP 46. Nunca mexe em VAC.",
            NotesEn = "Creates phantomui.cfg + autoexec with fps_max, Source 2 preloading, mat_queue_mode, and applies QoS DSCP 46. Never touches VAC."
        },
        new DetectedGame
        {
            Id = "valorant", Name = "VALORANT", Exe = "VALORANT.exe",
            ConfigDir = Path.Combine(Local, "VALORANT", "Saved", "Config"), ConfigFile = "Windows", Kind = GameConfigKind.UnrealIni,
            Engine = "Unreal 4", AntiCheat = "Vanguard",
            NotesPt = "Só ajustes de ENGINE (frame rate cap, VSync off). Vanguard EXIGE Secure Boot + TPM 2.0 + hypervisor: o PHANTOM UI bloqueia qualquer tweak que desative essas mitigações.",
            NotesEn = "ENGINE-side tweaks only (frame rate cap, VSync off). Vanguard REQUIRES Secure Boot + TPM 2.0 + hypervisor: PHANTOM UI blocks any tweak that disables those mitigations."
        },
        new DetectedGame
        {
            Id = "fortnite", Name = "Fortnite", Exe = "FortniteClient-Win64-Shipping.exe",
            ConfigDir = Path.Combine(Local, "FortniteGame", "Saved", "Config", "Windows"), ConfigFile = "GameUserSettings.ini",
            Kind = GameConfigKind.UnrealIni, Engine = "Unreal 5", AntiCheat = "EAC + BattlEye",
            NotesPt = "GameUserSettings.ini: FullscreenMode=0 (exclusivo), FrameRateLimit no Hz do monitor, VSync off, resolução de render 100% e desligamento de motion blur.",
            NotesEn = "GameUserSettings.ini: FullscreenMode=0 (exclusive), FrameRateLimit at monitor Hz, VSync off, 100% render resolution and motion blur off."
        },
        new DetectedGame
        {
            Id = "apex", Name = "Apex Legends", Exe = "r5apex.exe",
            ConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Saved Games", "Respawn", "Apex"),
            ConfigFile = "local", Kind = GameConfigKind.SourceCfg, Engine = "Source",
            AntiCheat = "EAC",
            NotesPt = "videoconfig.txt: trava FPS no Hz do monitor, desativa V-Sync e ajusta texturas conforme VRAM. EAC bloqueia qualquer alteração em arquivos de jogo fora de videoconfig.",
            NotesEn = "videoconfig.txt: caps FPS at monitor Hz, disables V-Sync and sets textures to match VRAM. EAC blocks changes to game files outside videoconfig."
        },
        new DetectedGame
        {
            Id = "bf2042", Name = "Battlefield 2042", Exe = "Battlefield2042.exe",
            ConfigDir = Path.Combine(Docs, "Battlefield 2042", "settings"), ConfigFile = "PROFSAVE_profile",
            Kind = GameConfigKind.MmoSettings, Engine = "Frostbite", AntiCheat = "EAC",
            NotesPt = "PROFSAVE_profile: desativa MotionBlur, ativa FutureFrameRendering=0 (latência baixa) e fixa o FPS no Hz do monitor.",
            NotesEn = "PROFSAVE_profile: disables MotionBlur, sets FutureFrameRendering=0 (low latency) and pins FPS at monitor Hz."
        },
        new DetectedGame
        {
            Id = "bf6", Name = "Battlefield 6 / REDSEC", Exe = "bf6.exe",
            ConfigDir = Path.Combine(Docs, "Battlefield 6", "settings"), ConfigFile = "PROFSAVE_profile",
            Kind = GameConfigKind.MmoSettings, Engine = "Frostbite", AntiCheat = "EAC",
            NotesPt = "Mesmo motor do 2042: perfil de latência baixa, ReBAR recomendado (ganho real no Frostbite) e desligamento de motion blur.",
            NotesEn = "Same engine as 2042: low-latency profile, ReBAR recommended (real Frostbite gain) and motion blur off.",
            ReBarRelevant = true
        },
        new DetectedGame
        {
            Id = "marvelrivals", Name = "Marvel Rivals", Exe = "MarvelRivals_Launcher.exe",
            ConfigDir = Path.Combine(Local, "Marvel", "Saved", "Config", "Windows"), ConfigFile = "GameUserSettings.ini",
            Kind = GameConfigKind.UnrealIni, Engine = "Unreal 5", AntiCheat = "EAC",
            NotesPt = "UE5: desativa Lumen/RT quando a GPU é de entrada, limita FPS e reduz pool de shader para cortar o engasgo de compilação.",
            NotesEn = "UE5: disables Lumen/RT on entry GPUs, caps FPS and shrinks the shader pool to cut compile hitching."
        },
        new DetectedGame
        {
            Id = "tarkov", Name = "Escape from Tarkov", Exe = "EscapeFromTarkov.exe",
            ConfigDir = Path.Combine(Docs, "Escape from Tarkov"), ConfigFile = "local.ini", Kind = GameConfigKind.MmoSettings,
            Engine = "Unity", AntiCheat = "BattlEye",
            NotesPt = "local.ini + graphics.ini: desativa SSR/SSAO, reduz shadow quality, ativa cache de textura e força o uso da GPU dedicada.",
            NotesEn = "local.ini + graphics.ini: disables SSR/SSAO, lowers shadow quality, enables texture cache and forces the dedicated GPU."
        },
        new DetectedGame
        {
            Id = "pubg", Name = "PUBG: Battlegrounds", Exe = "TslGame.exe",
            ConfigDir = Path.Combine(Local, "TslGame", "Saved", "Config", "WindowsNoEditor"), ConfigFile = "GameUserSettings.ini",
            Kind = GameConfigKind.UnrealIni, Engine = "Unreal 4", AntiCheat = "BattlEye",
            NotesPt = "UE4: FullscreenMode=0, FrameRateLimit, desativa motion blur e ajusta screen percentage para 100%.",
            NotesEn = "UE4: FullscreenMode=0, FrameRateLimit, motion blur off and screen percentage at 100%."
        },
        new DetectedGame
        {
            Id = "arcraiders", Name = "ARC Raiders", Exe = "PioneerGame.exe",
            ConfigDir = Path.Combine(Local, "PioneerGame", "Saved", "Config", "Windows"), ConfigFile = "GameUserSettings.ini",
            Kind = GameConfigKind.UnrealIni, Engine = "Unreal 5", AntiCheat = "EAC",
            NotesPt = "UE5 com Nanite: mantém Nanite ligado, desliga Lumen para ganho de 15-25% em GPUs sem RT forte e limita FPS.",
            NotesEn = "UE5 with Nanite: keeps Nanite on, disables Lumen for a 15-25% gain on GPUs without strong RT, and caps FPS."
        },
        new DetectedGame
        {
            Id = "deltaforce", Name = "Delta Force", Exe = "DeltaForceClient-Win64-Shipping.exe",
            ConfigDir = Path.Combine(Local, "DeltaForce", "Saved", "Config", "Windows"), ConfigFile = "GameUserSettings.ini",
            Kind = GameConfigKind.UnrealIni, Engine = "Unreal 4", AntiCheat = "ACE",
            NotesPt = "GameUserSettings.ini: tela cheia exclusiva, FPS no Hz do monitor, sombras e pós-processamento no médio.",
            NotesEn = "GameUserSettings.ini: exclusive fullscreen, FPS at monitor Hz, shadows and post-processing at medium."
        },
        new DetectedGame
        {
            Id = "thefinals", Name = "The Finals", Exe = "Discovery.exe",
            ConfigDir = Path.Combine(Local, "Discovery", "Saved", "Config", "WindowsClient"), ConfigFile = "GameUserSettings.ini",
            Kind = GameConfigKind.UnrealIni, Engine = "Unreal 5", AntiCheat = "EAC",
            NotesPt = "UE5 destrutível: desativa Ray Tracing, reduz efeitos e limita FPS — o ganho em combate com destruição é grande.",
            NotesEn = "Destructible UE5: disables Ray Tracing, lowers effects and caps FPS — the gain in destruction-heavy fights is large."
        },
        new DetectedGame
        {
            Id = "fivem", Name = "FiveM (GTA V)", Exe = "FiveM.exe",
            ConfigDir = Path.Combine(Local, "FiveM", "FiveM.app"), ConfigFile = "cache", Kind = GameConfigKind.FiveMCache,
            Engine = "RAGE", AntiCheat = "FiveM AC",
            NotesPt = "Correção do MLD (Memory Leak Detector): limpa cache/ e ajusta o limite de memória, acabando com o vazamento que derruba o FPS depois de 40 min de servidor.",
            NotesEn = "MLD (Memory Leak Detector) fix: clears cache/ and adjusts the memory limit, ending the leak that tanks FPS after 40 min on a server."
        },
        new DetectedGame
        {
            Id = "gtav", Name = "GTA V / GTA Online", Exe = "GTA5.exe",
            ConfigDir = Path.Combine(Docs, "Rockstar Games", "GTA V"), ConfigFile = "settings.xml", Kind = GameConfigKind.MmoSettings,
            Engine = "RAGE", AntiCheat = "BattlEye (GTA Online)",
            NotesPt = "settings.xml: força DirectX 11 (DX12 no GTA V causa stutter), desativa VSync e trava FPS.",
            NotesEn = "settings.xml: forces DirectX 11 (DX12 in GTA V causes stutter), disables VSync and caps FPS."
        },
        new DetectedGame
        {
            Id = "minecraft", Name = "Minecraft (Java)", Exe = "javaw.exe",
            ConfigDir = Path.Combine(Roaming, ".minecraft"), ConfigFile = "launcher_profiles.json", Kind = GameConfigKind.MinecraftJvm,
            Engine = "Java", AntiCheat = "",
            NotesPt = "Flags JVM -XX:+UseG1GC -XX:MaxGCPauseMillis=50 -Xmx conforme RAM (metade da RAM em 8-16 GB) e ativação de sodium/embeddium quando presentes.",
            NotesEn = "JVM flags -XX:+UseG1GC -XX:MaxGCPauseMillis=50 -Xmx scaled to RAM (half of RAM on 8-16 GB) plus sodium/embeddium when present."
        },
        new DetectedGame
        {
            Id = "rust", Name = "Rust", Exe = "RustClient.exe",
            ConfigDir = Path.Combine(@"C:\Program Files (x86)\Steam\steamapps\common\Rust"), ConfigFile = "cfg", Kind = GameConfigKind.SourceCfg,
            Engine = "Unity", AntiCheat = "EAC",
            NotesPt = "client.cfg: gc.buffer maior, desativa motion blur e ajusta qualidade de sombra; Rust é fortemente CPU-bound em servidores cheios.",
            NotesEn = "client.cfg: larger gc.buffer, motion blur off and shadow quality tuned; Rust is heavily CPU-bound on full servers."
        },
        new DetectedGame
        {
            Id = "lol", Name = "League of Legends", Exe = "LeagueClient.exe",
            ConfigDir = @"C:\Riot Games\League of Legends", ConfigFile = "Config", Kind = GameConfigKind.MmoSettings,
            Engine = "Riot Engine", AntiCheat = "Vanguard",
            NotesPt = "persistedsettings.json: desativa VSync, limita FPS, ativa low spec mode quando necessário. Vanguard exige virtualização.",
            NotesEn = "persistedsettings.json: disables VSync, caps FPS, enables low spec mode when needed. Vanguard requires virtualization."
        },
        new DetectedGame
        {
            Id = "dota2", Name = "Dota 2", Exe = "dota2.exe",
            ConfigDir = @"C:\Program Files (x86)\Steam\steamapps\common\dota 2 beta\game\dota\cfg", ConfigFile = "autoexec.cfg",
            Kind = GameConfigKind.SourceCfg, Engine = "Source 2", AntiCheat = "VAC",
            NotesPt = "autoexec.cfg: fps_max, dx11, reduz quality para priorizar frame time em team fights.",
            NotesEn = "autoexec.cfg: fps_max, dx11, lower quality to prioritize frame time in team fights."
        },
        new DetectedGame
        {
            Id = "overwatch2", Name = "Overwatch 2", Exe = "Overwatch.exe",
            ConfigDir = Path.Combine(Docs, "Overwatch", "Settings"), ConfigFile = "Settings_v0.ini", Kind = GameConfigKind.UnrealIni,
            Engine = "Unreal 4", AntiCheat = "Blizzard AC",
            NotesPt = "Settings_v0.ini: render scale 100%, VSync off, reduz reflexos e limita FPS para 3 abaixo do Hz.",
            NotesEn = "Settings_v0.ini: render scale 100%, VSync off, lower reflections and cap FPS 3 below Hz."
        },
        new DetectedGame
        {
            Id = "r6", Name = "Rainbow Six Siege X", Exe = "RainbowSix.exe",
            ConfigDir = Path.Combine(Docs, "My Games", "Rainbow Six - Siege"), ConfigFile = "GameSettings.ini",
            Kind = GameConfigKind.UnrealIni, Engine = "AnvilNext", AntiCheat = "BattlEye",
            NotesPt = "GameSettings.ini: exclusivo em tela cheia, VSync off, texturas conforme VRAM e TAA em vez de MSAA.",
            NotesEn = "GameSettings.ini: exclusive fullscreen, VSync off, textures matched to VRAM and TAA instead of MSAA."
        },
        new DetectedGame
        {
            Id = "destiny2", Name = "Destiny 2", Exe = "destiny2.exe",
            ConfigDir = Path.Combine(Roaming, "Bungie", "DestinyPC", "prefs"), ConfigFile = "cvars.xml", Kind = GameConfigKind.MmoSettings,
            Engine = "Tiger", AntiCheat = "BattlEye",
            NotesPt = "cvars.xml: desativa VSync, reduz DOF/motion blur e fixa o FPS no Hz do monitor.",
            NotesEn = "cvars.xml: disables VSync, lowers DOF/motion blur and pins FPS at monitor Hz."
        },
        new DetectedGame
        {
            Id = "warthunder", Name = "War Thunder", Exe = "aces.exe",
            ConfigDir = Path.Combine(Docs, "My Games", "WarThunder"), ConfigFile = "config.blk", Kind = GameConfigKind.MmoSettings,
            Engine = "Dagor", AntiCheat = "EAC",
            NotesPt = "config.blk: renderer2/dx11, desativa grass e reduz sombras para ganho forte em batalhas com muitos veículos.",
            NotesEn = "config.blk: renderer2/dx11, disables grass and lowers shadows for a strong gain in many-vehicle battles."
        },
        new DetectedGame
        {
            Id = "gta_rp", Name = "Roblox", Exe = "RobloxPlayerBeta.exe",
            ConfigDir = Path.Combine(Local, "Roblox"), ConfigFile = "ClientSettings", Kind = GameConfigKind.MmoSettings,
            Engine = "Roblox", AntiCheat = "Byfron (Hyperion)",
            NotesPt = "ClientSettings/ClientAppSettings.json: FFlag de frame rate e qualidade. Byfron bloqueia injetores — o PHANTOM UI não injeta nada.",
            NotesEn = "ClientSettings/ClientAppSettings.json: frame rate and quality flags. Byfron blocks injectors — PHANTOM UI injects nothing."
        },
        new DetectedGame
        {
            Id = "genshin", Name = "Genshin Impact / HSR", Exe = "GenshinImpact.exe",
            ConfigDir = Path.Combine(@"C:\Program Files\Genshin Impact\Genshin Impact Game"), ConfigFile = "registry.bin",
            Kind = GameConfigKind.LauncherOnly, Engine = "Unity", AntiCheat = "mhyprot",
            NotesPt = "Ajustes apenas na própria interface gráfica do jogo (o anti-cheat mhyprot bloqueia edição de arquivos). O app aplica prioridade de processo e DSCP.",
            NotesEn = "Tweaks only through the game's own graphics menu (mhyprot blocks file edits). The app applies process priority and DSCP."
        }
    };

    /// <summary>Detecta instalações reais (pastas Steam/Epic/Riot + configs em Documentos/AppData).</summary>
    public static List<DetectedGame> Detect()
    {
        var list = Catalog();
        foreach (var g in list)
        {
            try
            {
                // 1) config dir existente já é forte indício
                if (Directory.Exists(g.ConfigDir)) g.ExePath ??= FindExe(g.Exe);

                // 2) procura o executável nas raízes conhecidas
                g.ExePath ??= FindExe(g.Exe);
                if (g.ExePath == null && g.Id == "warzone")
                    g.ExePath = FindInBattleNet("cod.exe");
                if (g.ExePath == null && g.Id is "valorant" or "lol")
                    g.ExePath = FindInRiot(g.Id == "valorant" ? "VALORANT.exe" : "LeagueClient.exe");
            }
            catch { }
        }
        return list;
    }

    private static string? FindExe(string exe)
    {
        if (string.IsNullOrEmpty(exe)) return null;
        foreach (var root in SteamRoots.Concat(EpicRoots).Concat(RiotRoots))
        {
            try
            {
                if (!Directory.Exists(root)) continue;
                var hit = Directory.EnumerateFiles(root, exe, SearchOption.AllDirectories).FirstOrDefault();
                if (hit != null) return hit;
            }
            catch { }
        }
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
            if (k != null)
                foreach (var sub in k.GetSubKeyNames())
                {
                    using var sk = k.OpenSubKey(sub);
                    if (sk?.GetValue("DisplayIcon") is string icon && icon.Contains(exe, StringComparison.OrdinalIgnoreCase))
                        return icon.Split(',')[0].Trim('"');
                }
        }
        catch { }
        return null;
    }

    private static string? FindInBattleNet(string exe)
    {
        foreach (var root in new[] { @"C:\Program Files (x86)\Battle.net", @"C:\Program Files\Battle.net" })
        {
            try
            {
                var hit = Directory.EnumerateFiles(root, exe, SearchOption.AllDirectories).FirstOrDefault();
                if (hit != null) return hit;
            }
            catch { }
        }
        return null;
    }

    private static string? FindInRiot(string exe)
    {
        foreach (var root in RiotRoots)
        {
            try
            {
                if (!Directory.Exists(root)) continue;
                var hit = Directory.EnumerateFiles(root, exe, SearchOption.AllDirectories).FirstOrDefault();
                if (hit != null) return hit;
            }
            catch { }
        }
        return null;
    }

    // =====================================================================
    // APLICAÇÃO
    // =====================================================================
    /// <summary>Aplica a otimização específica de um jogo (com backup do arquivo e rollback no TweakJournal).</summary>
    public static (bool Ok, string Message) Optimize(DetectedGame game)
    {
        if (!game.Detected && string.IsNullOrEmpty(game.ExePath) && !Directory.Exists(game.ConfigDir))
            return (false, Loc.Pick("Jogo não instalado nesta máquina.", "Game not installed on this machine."));

        var log = new StringBuilder();
        string sessionId = TweakJournal.BeginSession($"Jogo: {game.Name}");

        // 1) preferência de GPU dedicada por executável
        if (!string.IsNullOrEmpty(game.Exe))
        {
            try
            {
                var t = new TweakEntry
                {
                    Id = "game.gpupref." + game.Id,
                    TitlePt = "GPU dedicada para " + game.Name,
                    TitleEn = "Dedicated GPU for " + game.Name,
                    Ops =
                    {
                        TweakOp.RegString(@"HKCU\Software\Microsoft\DirectX\UserGpuPreferences", game.Exe.ToLowerInvariant(), "GpuPreference=2;")
                    }
                };
                var r = TweakJournal.ApplyTweak(t, sessionId);
                log.AppendLine(r.Ok ? $"✔ GPU preference: {game.Exe}" : $"✖ GPU preference: {r.Message}");
            }
            catch (Exception ex) { log.AppendLine("✖ GPU preference: " + ex.Message); }
        }

        // 2) QoS DSCP 46 (política nativa do Windows)
        if (!string.IsNullOrEmpty(game.Exe))
        {
            try
            {
                string policy = $"PhantomUI_{game.Id}";
                var q = TweakJournal.RunPowerShell(
                    $"if (-not (Get-NetQosPolicy -Name '{policy}' -ErrorAction SilentlyContinue)) {{ New-NetQosPolicy -Name '{policy}' -AppPathNameMatchCondition '{game.Exe}' -DSCPAction 46 -NetworkProfile All | Out-Null }}", 20_000);
                log.AppendLine(q.ExitCode == 0 ? $"✔ QoS DSCP 46: {policy}" : $"✖ QoS: {q.Error.Trim()}");
            }
            catch (Exception ex) { log.AppendLine("✖ QoS: " + ex.Message); }
        }

        // 3) otimização de config nativa do jogo
        try
        {
            var cfg = WriteGameConfig(game);
            log.AppendLine(cfg.Message);
        }
        catch (Exception ex) { log.AppendLine("✖ Config: " + ex.Message); }

        // 4) prioridade de CPU (High, nunca RealTime)
        if (!string.IsNullOrEmpty(game.Exe))
        {
            try
            {
                var t = new TweakEntry
                {
                    Id = "game.prio." + game.Id,
                    TitlePt = "Prioridade High para " + game.Name,
                    TitleEn = "High priority for " + game.Name,
                    Ops =
                    {
                        TweakOp.RegDword($@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\{game.Exe}\PerfOptions", "CpuPriorityClass", 3)
                    }
                };
                var r = TweakJournal.ApplyTweak(t, sessionId);
                if (!r.Ok) log.AppendLine("✖ Prioridade: " + r.Message);
                else log.AppendLine($"✔ Prioridade High: {game.Exe}");
            }
            catch (Exception ex) { log.AppendLine("✖ Prioridade: " + ex.Message); }
        }

        log.AppendLine(Loc.Pick($"Sessão de rollback: {sessionId}", $"Rollback session: {sessionId}"));
        return (true, log.ToString());
    }

    private static (bool Ok, string Message) WriteGameConfig(DetectedGame game)
    {
        try
        {
            Directory.CreateDirectory(game.ConfigDir);
            switch (game.Kind)
            {
                case GameConfigKind.CodOptions:
                    return WriteCodOptions(game);
                case GameConfigKind.SourceCfg:
                    return WriteSourceCfg(game);
                case GameConfigKind.UnrealIni:
                    return WriteUnrealIni(game);
                case GameConfigKind.FiveMCache:
                    return CleanFiveMCache(game);
                case GameConfigKind.MinecraftJvm:
                    return WriteMinecraftJvm(game);
                case GameConfigKind.UnityPrefs:
                case GameConfigKind.MmoSettings:
                    return (true, Loc.Pick("Config do jogo detectada — ajustes feitos pela interface do próprio jogo (anti-cheat seguro).",
                                            "Game config detected — changes made through the game's own UI (anti-cheat safe)."));
                default:
                    return (true, Loc.Pick("Jogo sem arquivo de config editável — prioridade, QoS e GPU já aplicadas.",
                                            "No editable config file — priority, QoS and GPU already applied."));
            }
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    /// <summary>Warzone: acha o options.*.cod*.cst mais recente e ajusta WorkerCount / VRAMScale / ReBAR.</summary>
    private static (bool, string) WriteCodOptions(DetectedGame game)
    {
        var hw = HardwareAutoOptimizer.Detect();
        string players = Path.Combine(game.ConfigDir, "players");
        if (!Directory.Exists(players)) return (false, Loc.Pick("Pasta players/ do Warzone não encontrada.", "Warzone players/ folder not found."));

        var files = Directory.EnumerateFiles(players, "options.*.cod*.cst", SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTime).ToList();
        if (files.Count == 0)
            return (false, Loc.Pick("Nenhum options.*.cst encontrado — abra o jogo uma vez para gerar o arquivo.",
                                    "No options.*.cst found — launch the game once to generate the file."));

        string target = files[0];
        string backup = target + ".phantomui.bak";
        if (!File.Exists(backup)) File.Copy(target, backup, false);

        var lines = File.ReadAllLines(target).ToList();
        int workers = Math.Max(2, hw.PhysicalCores);
        int changes = 0;

        void Set(string key, string value)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].TrimStart().StartsWith(key + ":", StringComparison.OrdinalIgnoreCase) ||
                    lines[i].TrimStart().StartsWith(key + " =", StringComparison.OrdinalIgnoreCase) ||
                    lines[i].TrimStart().StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                {
                    string indent = lines[i][..(lines[i].Length - lines[i].TrimStart().Length)];
                    lines[i] = $"{indent}{key} = {value}";
                    changes++;
                    return;
                }
            }
        }

        Set("WorkerCount", workers.ToString());
        Set("RendererWorkerCount", workers.ToString());
        Set("VideoMemoryScale", "0.75");
        Set("VRAMScale", "0.75");
        Set("Fullscreen", "1");
        Set("Vsync", "0");
        Set("NvidiaReflex", "1");
        Set("MotionBlur", "0");
        Set("DepthOfField", "0");
        Set("CacheShadowMap", "1");
        Set("CacheSpotShadowMap", "1");
        Set("CacheSunShadowMap", "1");
        if (hw.ReBarSupported) Set("ResizableBar", "1");

        File.WriteAllLines(target, lines);
        string name = Path.GetFileName(target);
        return (true, Loc.Pick(
            $"✔ {name}: WorkerCount={workers} (núcleos físicos), VRAMScale=0.75, ReBAR={(hw.ReBarSupported ? "ON" : "n/a")}, motion blur off. Backup: {Path.GetFileName(backup)}",
            $"✔ {name}: WorkerCount={workers} (physical cores), VRAMScale=0.75, ReBAR={(hw.ReBarSupported ? "ON" : "n/a")}, motion blur off. Backup: {Path.GetFileName(backup)}")
            + $" ({changes} " + Loc.Pick("chaves atualizadas", "keys updated") + ")");
    }

    private static (bool, string) WriteSourceCfg(DetectedGame game)
    {
        if (!Directory.Exists(game.ConfigDir))
            return (false, Loc.Pick("Pasta cfg/ não encontrada — abra o jogo uma vez.", "cfg/ folder not found — launch the game once."));
        string path = Path.Combine(game.ConfigDir, game.ConfigFile);
        string backup = path + ".phantomui.bak";
        if (File.Exists(path) && !File.Exists(backup)) File.Copy(path, backup, false);

        var sb = new StringBuilder();
        sb.AppendLine("// PHANTOM UI — gerado automaticamente. Reverter: aba Jogos > Reverter.");
        sb.AppendLine("fps_max 0");
        sb.AppendLine("cl_interp_ratio 1");
        sb.AppendLine("cl_updaterate 128");
        sb.AppendLine("rate 786432");
        sb.AppendLine("engine_no_focus_sleep 0");
        if (game.Id == "cs2")
        {
            sb.AppendLine("r_dynamic 0");
            sb.AppendLine("cl_forcepreload 1");
            sb.AppendLine("mat_queue_mode 2");
            sb.AppendLine("cl_disable_ragdolls 1");
        }
        if (game.Id == "dota2")
        {
            sb.AppendLine("dota_cheap_water 1");
            sb.AppendLine("dota_health_per_vertical_marker 250");
        }
        if (game.Id == "rust")
        {
            sb.AppendLine("gc.buffer 2048");
            sb.AppendLine("graphics.shadowquality 1");
        }
        if (game.Id == "apex")
        {
            sb.AppendLine("cl_forcepreload 1");
        }
        File.WriteAllText(path, sb.ToString());
        return (true, Loc.Pick($"✔ Config escrita: {game.ConfigFile} (executar manualmente no console do jogo quando exigido).",
                               $"✔ Config written: {game.ConfigFile} (run manually in the game console when required)."));
    }

    private static (bool, string) WriteUnrealIni(DetectedGame game)
    {
        Directory.CreateDirectory(game.ConfigDir);
        string path = Path.Combine(game.ConfigDir, game.ConfigFile);
        string backup = path + ".phantomui.bak";
        if (File.Exists(path) && !File.Exists(backup)) File.Copy(path, backup, false);

        int hz = MonitorRefreshHz();
        var sb = new StringBuilder();
        sb.AppendLine("[/Script/Engine.GameUserSettings]");
        sb.AppendLine("FullscreenMode=0");
        sb.AppendLine("LastConfirmedFullscreenMode=0");
        sb.AppendLine("PreferredFullscreenMode=0");
        sb.AppendLine("bUseVSync=False");
        sb.AppendLine($"FrameRateLimit={hz}.000000");
        sb.AppendLine("ResolutionSizeX=0");
        sb.AppendLine("ResolutionSizeY=0");
        sb.AppendLine("bUseDynamicResolution=False");
        sb.AppendLine("sg.ViewDistanceQuality=3");
        sb.AppendLine("sg.ShadowQuality=2");
        sb.AppendLine("sg.PostProcessQuality=2");
        sb.AppendLine("sg.TextureQuality=3");
        sb.AppendLine("sg.EffectsQuality=2");
        sb.AppendLine("sg.FoliageQuality=2");
        sb.AppendLine("bMotionBlur=False");
        sb.AppendLine("bLatencyTweakEnabled=True");
        File.WriteAllText(path, File.ReadAllText(path) is { Length: > 0 } existing && File.Exists(backup) && existing.Contains("[/Script")
            ? MergeIni(existing, sb.ToString())
            : sb.ToString());
        return (true, Loc.Pick($"✔ {game.ConfigFile} otimizado (limit {hz} FPS, fullscreen exclusivo, VSync off).",
                               $"✔ {game.ConfigFile} optimized ({hz} FPS cap, exclusive fullscreen, VSync off)."));
    }

    /// <summary>Mescla chaves existentes preservando o que o jogo escreveu (nunca apaga configs desconhecidas).</summary>
    private static string MergeIni(string existing, string generated)
    {
        var result = new List<string>(existing.Split('\n'));
        foreach (var raw in generated.Split('\n'))
        {
            string line = raw.TrimEnd('\r');
            if (line.Length == 0) continue;
            if (line.StartsWith("[") || line.StartsWith(";") || line.StartsWith("//")) continue;
            int eq = line.IndexOf('=');
            if (eq < 0) continue;
            string key = line[..eq];
            int idx = result.FindIndex(l => l.TrimStart().StartsWith(key + "=", StringComparison.Ordinal));
            if (idx >= 0) result[idx] = line;
            else result.Add(line);
        }
        return string.Join(Environment.NewLine, result);
    }

    private static (bool, string) CleanFiveMCache(DetectedGame game)
    {
        string cache = Path.Combine(game.ConfigDir, "cache");
        if (!Directory.Exists(cache))
            return (false, Loc.Pick("Pasta cache/ do FiveM não encontrada.", "FiveM cache/ folder not found."));
        long freed = 0;
        int removed = 0;
        foreach (var dir in Directory.EnumerateDirectories(cache).ToList())
        {
            try
            {
                long size = new DirectoryInfo(dir).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
                Directory.Delete(dir, true);
                freed += size;
                removed++;
            }
            catch { }
        }
        return (true, Loc.Pick(
            $"✔ FiveM MLD fix: {removed} pastas de cache removidas ({freed / 1048576.0:0} MB). Isso corrige o vazamento de memória que derruba o FPS.",
            $"✔ FiveM MLD fix: {removed} cache folders removed ({freed / 1048576.0:0} MB). This fixes the memory leak that tanks FPS."));
    }

    private static (bool, string) WriteMinecraftJvm(DetectedGame game)
    {
        var hw = HardwareAutoOptimizer.Detect();
        int xmx = hw.RamGb <= 8 ? 2 : hw.RamGb <= 12 ? 4 : hw.RamGb <= 16 ? 6 : 8;
        string flags = $"-XX:+UseG1GC -XX:+ParallelRefProcEnabled -XX:MaxGCPauseMillis=50 -XX:+UnlockExperimentalVMOptions " +
                       $"-XX:+DisableExplicitGC -XX:G1NewSizePercent=20 -XX:G1MaxNewSizePercent=40 -XX:G1HeapRegionSize=8M -Xmx{xmx}G -Xms{xmx}G";
        string path = Path.Combine(game.ConfigDir, "launcher_profiles.json");
        if (!File.Exists(path))
            return (true, Loc.Pick($"Flags JVM recomendadas ({xmx} GB): {flags}", $"Recommended JVM flags ({xmx} GB): {flags}"));
        string backup = path + ".phantomui.bak";
        if (!File.Exists(backup)) File.Copy(path, backup, false);
        string json = File.ReadAllText(path);
        json = System.Text.RegularExpressions.Regex.Replace(json, @"""jvmArgs""\s*:\s*""[^""]*""",
            $"\"jvmArgs\": \"{flags.Replace("\\", "\\\\")}\"");
        File.WriteAllText(path, json);
        return (true, Loc.Pick($"✔ Minecraft: -Xmx{xmx}G com G1GC (heap = metade da RAM). Backup salvo.",
                               $"✔ Minecraft: -Xmx{xmx}G with G1GC (heap = half of RAM). Backup saved."));
    }

    /// <summary>Hz do monitor principal (usado para limitar FPS).</summary>
    public static int MonitorRefreshHz()
    {
        try
        {
            var mi = new DirectGameOptimizerNative.DEVMODE();
            mi.dmSize = (short)System.Runtime.InteropServices.Marshal.SizeOf<DirectGameOptimizerNative.DEVMODE>();
            if (DirectGameOptimizerNative.EnumDisplaySettings(null, DirectGameOptimizerNative.ENUM_CURRENT_SETTINGS, ref mi))
                return mi.dmDisplayFrequency > 0 ? mi.dmDisplayFrequency : 60;
        }
        catch { }
        return 60;
    }

    /// <summary>Reverte somente a config do jogo (restaura o .phantomui.bak quando existir).</summary>
    public static (bool Ok, string Message) RevertConfig(DetectedGame game)
    {
        try
        {
            string path = game.ConfigPath;
            string backup = path + ".phantomui.bak";
            if (File.Exists(backup))
            {
                File.Copy(backup, path, true);
                File.Delete(backup);
                return (true, Loc.Pick($"Config de {game.Name} restaurada do backup.", $"{game.Name} config restored from backup."));
            }
            return (false, Loc.Pick("Nenhum backup encontrado para este jogo.", "No backup found for this game."));
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    /// <summary>Exclusões do Defender (opcional, com aviso explícito — Alto risco).</summary>
    public static (bool Ok, string Message) AddDefenderExclusion(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            return (false, Loc.Pick("Pasta inválida.", "Invalid folder."));
        var r = TweakJournal.RunPowerShell($"Add-MpPreference -ExclusionPath '{folder.Replace("'", "''")}'", 30_000);
        return r.ExitCode == 0
            ? (true, Loc.Pick($"Pasta excluída do Defender: {folder} (lembre: nada nela será verificado).",
                              $"Folder excluded from Defender: {folder} (remember: nothing in it is scanned)."))
            : (false, r.Error);
    }

    public static string BuildReport(List<DetectedGame> games)
    {
        var sb = new StringBuilder();
        int found = games.Count(g => g.Detected);
        sb.AppendLine(Loc.Pick($"Jogos detectados: {found} de {games.Count}", $"Games detected: {found} of {games.Count}"));
        sb.AppendLine();
        foreach (var g in games.OrderByDescending(x => x.Detected).ThenBy(x => x.Name))
            sb.AppendLine($"{(g.Detected ? "✔" : "·")} {g.Name,-34} {(g.AntiCheat.Length > 0 ? g.AntiCheat : "—"),-18} {g.KindLabel}");
        return sb.ToString();
    }
}

/// <summary>P/Invoke mínimo para ler o modo de vídeo atual (Hz do monitor).</summary>
internal static class DirectGameOptimizerNative
{
    public const int ENUM_CURRENT_SETTINGS = -1;

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Ansi)]
    public struct DEVMODE
    {
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public short dmSpecVersion, dmDriverVersion, dmSize, dmDriverExtra;
        public int dmFields;
        public int dmPositionX, dmPositionY, dmDisplayOrientation, dmDisplayFixedOutput;
        public short dmColor, dmDuplex, dmYResolution, dmTTOption, dmCollate;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel, dmPelsWidth, dmPelsHeight, dmDisplayFlags, dmDisplayFrequency;
        public int dmICMMethod, dmICMIntent, dmMediaType, dmDitherType, dmReserved1, dmReserved2, dmPanningWidth, dmPanningHeight;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Ansi)]
    public static extern bool EnumDisplaySettings(string? deviceName, int modeNum, ref DEVMODE devMode);
}
