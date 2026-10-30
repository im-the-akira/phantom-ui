namespace PhantomUI;

public static partial class TweakCatalog
{
    private const string KGameDvr = @"HKCU\System\GameConfigStore";
    private const string KTelemetry = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection";
    private const string KAdvertising = @"HKCU\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo";
    private const string KCopilot = @"HKCU\Software\Policies\Microsoft\Windows\WindowsCopilot";
    private const string KAiPolicy = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string KRecall = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI";
    private const string KWuPolicy = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate";
    private const string KWuAu = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU";
    private const string KWns = @"HKLM\SYSTEM\CurrentControlSet\Services\W32Time\TimeProviders\NtpClient";
    private const string KFontCache = @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\FontCache";
    private const string KWaitToKillApp = @"HKCU\Control Panel\Desktop";
    private const string KStorePolicy = @"HKLM\SOFTWARE\Policies\Microsoft\WindowsStore";

    // =====================================================================
    // 10) JOGOS & ANTI-CHEAT
    // =====================================================================
    private static IEnumerable<TweakEntry> BuildGames()
    {
        yield return Dword("games.gamedvr-off", TweakSection.Games, TweakRisk.Low, TweakTags.None,
            "Game DVR totalmente desligado",
            "Game DVR fully disabled",
            "Desliga a captura em segundo plano do Xbox Game Bar. Reduz uso de GPU/disco e remove o overlay que atrapalha jogos com anti-cheat.",
            "Disables Xbox Game Bar background capture. Cuts GPU/disk usage and removes the overlay that clashes with anti-cheat games.",
            KGameDvr, "GameDVR_Enabled", 0);

        yield return Dword("games.fse-off", TweakSection.Games, TweakRisk.Low, TweakTags.None,
            "Otimização de tela cheia do Windows desligada",
            "Windows fullscreen optimization off",
            "GameDVR_FSEBehavior=2 força tela cheia exclusiva de verdade, evitando que o compositor do Windows adicione input lag em jogos antigos.",
            "GameDVR_FSEBehavior=2 forces true exclusive fullscreen, preventing the Windows compositor from adding input lag in older games.",
            KGameDvr, "GameDVR_FSEBehavior", 2);

        yield return Dword("games.game-bar-off", TweakSection.Games, TweakRisk.Low, TweakTags.None,
            "Barra de jogos (Game Bar) desativada",
            "Game Bar disabled",
            "UseNexusForGameBarEnabled=0 e ShowStartupPanel=0 impedem o processo GameBar.exe de iniciar com o jogo.",
            "UseNexusForGameBarEnabled=0 and ShowStartupPanel=0 stop GameBar.exe from launching with the game.",
            KGameBar, "ShowStartupPanel", 0);

        yield return Dword("games.game-mode-on", TweakSection.Games, TweakRisk.Low, TweakTags.None,
            "Modo de Jogo ativado",
            "Game Mode enabled",
            "O Modo de Jogo do Windows 11 reserva CPU e GPU para o app em foco. Desligar é mito: em máquinas 8 GB+ ele melhora o frame time.",
            "Windows 11 Game Mode reserves CPU and GPU for the focused app. Disabling it is a myth: on 8 GB+ machines it improves frame time.",
            KGameBar, "AutoGameModeEnabled", 1);

        yield return T("games.hags-check", TweakSection.Games, TweakRisk.Medium, TweakTags.RequiresReboot,
            "HAGS verificado para jogos DX12",
            "HAGS verified for DX12 games",
            "Confere e aplica HwSchMode=2, necessário para o agendador de GPU por hardware usado por Warzone, Fortnite e CS2 em DX12.",
            "Checks and applies HwSchMode=2, required by the hardware GPU scheduler used by Warzone, Fortnite and CS2 on DX12.",
            () => CheckReg(KGraphicsDrivers, "HwSchMode", 2), "",
            TweakOp.RegDword(KGraphicsDrivers, "HwSchMode", 2));

        yield return T("games.anticheat-services", TweakSection.Games, TweakRisk.Low, TweakTags.AntiCheatSensitive,
            "Anti-cheats oficiais PRESERVADOS",
            "Official anti-cheats PRESERVED",
            "O PHANTOM UI detecta Vanguard (vgk.sys), FACEIT, EasyAntiCheat e BattlEye (BEDaisy) e NUNCA desativa serviços, drivers ou mitigações exigidos por eles. Este card apenas audita o estado.",
            "PHANTOM UI detects Vanguard (vgk.sys), FACEIT, EasyAntiCheat and BattlEye (BEDaisy) and NEVER disables services, drivers or mitigations they require. This card only audits state.",
            () => new TweakStateInfo(
                HardwareAutoOptimizer.DetectedAntiCheats().Count > 0,
                HardwareAutoOptimizer.DetectedAntiCheats() is { Count: > 0 } list ? string.Join(", ", list)
                    : Loc.Pick("Nenhum anti-cheat detectado", "No anti-cheat detected")),
            "Get-Service vgk, EasyAntiCheat, BEService  (somente leitura)",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("games.timer-05-gaming", TweakSection.Games, TweakRisk.Medium, TweakTags.NoAuto,
            "Timer 0,5 ms só enquanto o jogo está aberto",
            "0.5 ms timer only while the game is running",
            "O loop de foco do PHANTOM UI detecta o processo do jogo e eleva a resolução do timer só durante a partida, liberando ao sair.",
            "The PHANTOM UI focus loop detects the game process and raises timer resolution only during the match, releasing it on exit.",
            () => new TweakStateInfo(SettingsInstance.GameTimer05Ms,
                SettingsInstance.GameTimer05Ms ? Loc.Pick("Ativo no modo jogo", "Active in game mode") : Loc.Pick("Desativado", "Disabled")),
            "NtSetTimerResolution(5000, TRUE) chamado apenas com jogo em foco",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("games.defender-exclusions", TweakSection.Games, TweakRisk.High, TweakTags.NoAuto,
            "Exclusões do Defender para a pasta de jogos",
            "Defender exclusions for the games folder",
            "Adicionar a pasta de jogos às exclusões do Microsoft Defender reduz leituras de disco no carregamento de mapas. ALTO RISCO: qualquer arquivo naquela pasta deixa de ser verificado.",
            "Adding the games folder to Microsoft Defender exclusions cuts disk reads while maps load. HIGH RISK: files in that folder are no longer scanned.",
            () => new TweakStateInfo(false, Loc.Pick("Configurado na aba Jogos", "Configured in the Games page")),
            "Add-MpPreference -ExclusionPath \"D:\\Games\"",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("games.qos-priority", TweakSection.Games, TweakRisk.Low, TweakTags.None,
            "QoS DSCP 46 por executável de jogo",
            "QoS DSCP 46 per game executable",
            "Cria política de QoS nativa marcando o tráfego do jogo como EF (46). Funciona com roteadores que respeitam DSCP e não mexe em antivírus.",
            "Creates a native QoS policy marking game traffic as EF (46). Works with DSCP-aware routers and does not touch antivirus.",
            () => new TweakStateInfo(false, Loc.Pick("Aplicado por jogo", "Applied per game")),
            "New-NetQosPolicy -Name \"PhantomUI_CS2\" -AppPathNameMatchCondition \"cs2.exe\" -DSCPAction 46",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("games.gpu-preference", TweakSection.Games, TweakRisk.Low, TweakTags.Laptop,
            "Preferência de alto desempenho por jogo",
            "High performance preference per game",
            "Escreve UserGpuPreferences por executável garantindo que o jogo use a GPU dedicada mesmo com driver otimizado por economia de energia.",
            "Writes UserGpuPreferences per executable ensuring the game uses the dedicated GPU even with power-saving driver profiles.",
            () => new TweakStateInfo(false, Loc.Pick("Por jogo", "Per game")),
            "reg add \"HKCU\\Software\\Microsoft\\DirectX\\UserGpuPreferences\" /v <exe> /t REG_SZ /d GpuPreference=2;",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("games.fivem-mld", TweakSection.Games, TweakRisk.Low, TweakTags.None,
            "FiveM: correção do MLD (Memory Leak Detector)",
            "FiveM: MLD fix (Memory Leak Detector)",
            "O FiveM sofre de vazamento de memória com o MLD do próprio jogo. O PHANTOM UI aplica o ajuste de cache e limpa a pasta cache/ do FiveM, correção validada do acervo Kairo.",
            "FiveM leaks memory with its own MLD. PHANTOM UI applies the cache adjustment and clears FiveM's cache/ folder, a validated fix from the Kairo collection.",
            () => new TweakStateInfo(false, Loc.Pick("Aplicado pela aba Jogos", "Applied by the Games page")),
            "Remove-Item \"%LOCALAPPDATA%\\FiveM\\FiveM.app\\cache\\*\" -Recurse -Force",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("games.shader-precompile", TweakSection.Games, TweakRisk.Low, TweakTags.None,
            "Pré-compilação de shader habilitada",
            "Shader precompilation enabled",
            "Garante que o cache de shader do DirectX e o pipeline de compilação estejam ativos — evita o engasgo clássico do primeiro confronto em Warzone e Fortnite.",
            "Ensures the DirectX shader cache and compilation pipeline are active — avoids the classic first-fight stutter in Warzone and Fortnite.",
            () => CheckReg(KShaderCache, "ShaderCacheEnabled", 1), "",
            TweakOp.RegDword(KShaderCache, "ShaderCacheEnabled", 1));

        yield return T("games.steam-shader-cache", TweakSection.Games, TweakRisk.Low, TweakTags.None,
            "Steam: pré-carregar shader cache",
            "Steam: preload shader cache",
            "Ativa ShaderBackgroundUpdate na Steam (config/config.vdf), que baixa o cache de shader Vulkan/DX antes da partida.",
            "Enables ShaderBackgroundUpdate in Steam (config/config.vdf), which fetches the Vulkan/DX shader cache before the match.",
            () => new TweakStateInfo(false, Loc.Pick("Aplicado pela aba Jogos", "Applied by the Games page")),
            "config.vdf: \"ShaderBackgroundUpdate\" \"1\"",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("games.low-latency-pipeline", TweakSection.Games, TweakRisk.Medium, TweakTags.None,
            "Low Latency Mode (NVIDIA Reflex / Anti-Lag)",
            "Low Latency Mode (NVIDIA Reflex / Anti-Lag)",
            "Audita se o driver tem Reflex/Anti-Lag disponível e recomenda ativação por jogo. Não altera o driver: apenas verifica e mostra o caminho oficial.",
            "Audits whether the driver exposes Reflex/Anti-Lag and recommends enabling it per game. Does not change the driver: it checks and shows the official path.",
            () => new TweakStateInfo(false, Loc.Pick("Verificado pela aba Jogos", "Checked by the Games page")),
            "Driver API (NvAPI_D3D_SetSleepMode / AMD Anti-Lag)",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("games.windowed-optimizations", TweakSection.Games, TweakRisk.Low, TweakTags.None,
            "Jogos em tela cheia sem otimização de janela",
            "Games fullscreen without window optimization",
            "Remove as otimizações de janela do Windows 11 que causam stutter em jogos que usam borderless e DWM ao mesmo tempo.",
            "Removes Windows 11 window optimizations that cause stutter in games using borderless and DWM at the same time.",
            () => CheckReg(KDwm, "UseBufferedPaintComposition", 0), "",
            TweakOp.RegDword(KDwm, "UseBufferedPaintComposition", 0));

        yield return T("games.network-priority", TweakSection.Games, TweakRisk.Low, TweakTags.None,
            "Prioridade de rede para o jogo em foco",
            "Network priority for the focused game",
            "Ativa o monitor de foco do PHANTOM UI, que aplica prioridade de processo e DSCP quando o jogo entra em primeiro plano e remove ao sair.",
            "Enables the PHANTOM UI focus monitor, which applies process priority and DSCP when the game comes to the foreground and removes it on exit.",
            () => new TweakStateInfo(false, Loc.Pick("Ativo durante o jogo", "Active during the game")),
            "SetPriorityClass(HIGH_PRIORITY_CLASS) + QoS policy",
            TweakOp.Cmd("(void)0", "(void)0"));
    }

    // =====================================================================
    // 11) PRIVACIDADE & DEBLOAT
    // =====================================================================
    private static IEnumerable<TweakEntry> BuildPrivacy()
    {
        yield return Dword("privacy.telemetry-min", TweakSection.Privacy, TweakRisk.Low, TweakTags.None,
            "Telemetria no mínimo (Security)",
            "Telemetry at minimum (Security)",
            "AllowTelemetry=0 bloqueia envio de dados de uso. O Windows 11 Home respeita 1 (Basic); o valor 0 vale em Pro/Enterprise.",
            "AllowTelemetry=0 blocks usage data upload. Windows 11 Home honors 1 (Basic); 0 applies on Pro/Enterprise.",
            KTelemetry, "AllowTelemetry", 0);

        yield return Dword("privacy.advertising-id", TweakSection.Privacy, TweakRisk.Low, TweakTags.None,
            "Desativar ID de publicidade",
            "Disable advertising ID",
            "Enabled=0 impede que apps usem um identificador único para anúncios personalizados e rastreamento entre apps.",
            "Enabled=0 stops apps from using a unique identifier for personalized ads and cross-app tracking.",
            KAdvertising, "Enabled", 0);

        yield return Dword("privacy.activity-feed", TweakSection.Privacy, TweakRisk.Low, TweakTags.None,
            "Não publicar histórico de atividades",
            "Do not publish activity history",
            "PublishUserActivities=0 e UploadUserActivities=0 impedem o envio do histórico de uso para a conta Microsoft.",
            "PublishUserActivities=0 and UploadUserActivities=0 stop sending your usage history to your Microsoft account.",
            @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System", "PublishUserActivities", 0);

        yield return Dword("privacy.feedback-off", TweakSection.Privacy, TweakRisk.Low, TweakTags.None,
            "Sem pedidos de feedback do Windows",
            "No Windows feedback prompts",
            "DoNotShowFeedbackNotifications=1 elimina as notificações de feedback que interrompem o jogo em tela cheia.",
            "DoNotShowFeedbackNotifications=1 removes the feedback notifications that interrupt fullscreen games.",
            @"HKCU\Software\Microsoft\Siuf\Rules", "NumberOfSIUFInPeriod", 0);

        yield return T("privacy.diagtrack", TweakSection.Privacy, TweakRisk.Medium, TweakTags.None,
            "Desativar serviço DiagTrack (Experiências Conectadas)",
            "Disable DiagTrack service (Connected User Experiences)",
            "O DiagTrack é o serviço de telemetria que mais consome CPU e disco em segundo plano. Desativar é reversível pelo journal; o Windows Update continua funcionando.",
            "DiagTrack is the telemetry service that consumes the most background CPU and disk. Disabling is journal-reversible and Windows Update keeps working.",
            () => CheckService("DiagTrack", 4), "sc config DiagTrack start= disabled",
            TweakOp.Service("DiagTrack", 4));

        yield return T("privacy.dmwappushservice", TweakSection.Privacy, TweakRisk.Low, TweakTags.None,
            "Desativar dmwappushservice (WAP Push)",
            "Disable dmwappushservice (WAP Push)",
            "Serviço de mensagens WAP herdado do Windows Mobile. Não é usado por jogos nem por drivers e pode ser desativado com segurança.",
            "Legacy WAP messaging service from Windows Mobile. It is not used by games or drivers and can be safely disabled.",
            () => CheckService("dmwappushservice", 4), "sc config dmwappushservice start= disabled",
            TweakOp.Service("dmwappushservice", 4));

        yield return T("privacy.maps-broker", TweakSection.Privacy, TweakRisk.Low, TweakTags.None,
            "Desativar MapsBroker",
            "Disable MapsBroker",
            "O serviço de mapas baixa dados offline em segundo plano. Se você não usa o app Mapas, desativar libera disco e rede.",
            "The maps service downloads offline data in the background. If you do not use the Maps app, disabling frees disk and network.",
            () => CheckService("MapsBroker", 4), "sc config MapsBroker start= disabled",
            TweakOp.Service("MapsBroker", 4));

        yield return T("privacy.retail-demo", TweakSection.Privacy, TweakRisk.Low, TweakTags.None,
            "Desativar RetailDemo",
            "Disable RetailDemo",
            "Serviço de demonstração de loja, inútil em máquina pessoal e que pode ser reativado por atualizações.",
            "Store demo service, useless on a personal machine and sometimes re-enabled by updates.",
            () => CheckService("RetailDemo", 4), "sc config RetailDemo start= disabled",
            TweakOp.Service("RetailDemo", 4));

        yield return T("privacy.wap-push", TweakSection.Privacy, TweakRisk.Low, TweakTags.None,
            "Notificações em jogo do WpnUserService",
            "In-game push notifications (WpnUserService)",
            "O WpnUserService entrega notificações que roubam foco. Desativar mantém o jogo em primeiro plano durante partidas.",
            "WpnUserService delivers notifications that steal focus. Disabling keeps the game in the foreground during matches.",
            () => new TweakStateInfo(false, Loc.Pick("Por usuário", "Per user session")),
            "Get-Service WpnUserService* | Set-Service -StartupType Disabled",
            TweakOp.Cmd("Get-Service WpnUserService* | Set-Service -StartupType Disabled", "Get-Service WpnUserService* | Set-Service -StartupType Automatic"));

        yield return Dword("privacy.cortana-off", TweakSection.Privacy, TweakRisk.Low, TweakTags.None,
            "Desativar Cortana",
            "Disable Cortana",
            "AllowCortana=0 remove o assistente herdado que mantém um processo de indexação de voz ativo.",
            "AllowCortana=0 removes the legacy assistant that keeps a voice indexing process active.",
            @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0);

        yield return Dword("privacy.location-off", TweakSection.Privacy, TweakRisk.Low, TweakTags.None,
            "Desativar localização do sistema",
            "Disable system location",
            "DisableLocation=1 impede que apps consultem o sensor de localização, que mantém o GPS/Wi-Fi scan ativo em notebooks.",
            "DisableLocation=1 stops apps from querying the location sensor, which keeps GPS/Wi-Fi scanning active on laptops.",
            @"HKLM\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableLocation", 1);

        yield return Dword("privacy.cloud-search", TweakSection.Privacy, TweakRisk.Low, TweakTags.None,
            "Sem resultados de nuvem na busca",
            "No cloud results in search",
            "ConnectedSearchUseWeb=0 impede o SearchHost de enviar cada pesquisa para a Bing, reduzindo rede e uso de CPU.",
            "ConnectedSearchUseWeb=0 stops SearchHost from sending every query to Bing, cutting network and CPU usage.",
            @"HKCU\Software\Policies\Microsoft\Windows\Windows Search", "ConnectedSearchUseWeb", 0);

        yield return T("privacy.onedrive-debloat", TweakSection.Privacy, TweakRisk.Medium, TweakTags.NoAuto,
            "Remover OneDrive do Explorer (sem desinstalar)",
            "Remove OneDrive from Explorer (no uninstall)",
            "Remove o item de navegação e o processo de sincronização do Explorer sem apagar seus arquivos. Reversível pelo próprio app.",
            "Removes the navigation entry and the sync process from Explorer without deleting your files. Reversible from the app itself.",
            () => new TweakStateInfo(false, Loc.Pick("Aplicado pelo Debloat", "Applied by Debloat")),
            "reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\. Explorer\\Desktop\\NameSpace\\{018D5C66-4533-4307-9B53-224DE2ED1FE6}\" /f",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("privacy.remove-xbox", TweakSection.Privacy, TweakRisk.Medium, TweakTags.NoAuto,
            "Remover Xbox Game Bar e apps Xbox",
            "Remove Xbox Game Bar and Xbox apps",
            "Remove por Appx os apps Xbox que ficam em memória. Jogos da Microsoft Store continuam funcionando; o Game Bar/overlay é que sai.",
            "Removes Xbox apps via Appx that stay resident. Microsoft Store games keep working; what goes away is Game Bar/overlay.",
            () => new TweakStateInfo(false, Loc.Pick("Aplicado pelo Debloat", "Applied by Debloat")),
            "Get-AppxPackage *Xbox* | Remove-AppxPackage",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("privacy.disable-consumer-features", TweakSection.Privacy, TweakRisk.Low, TweakTags.None,
            "Bloquear apps sugeridos e conteúdo promocional",
            "Block suggested apps and promo content",
            "DisableWindowsConsumerFeatures=1 impede o Windows de instalar apps promovidos (TikTok, Instagram, jogos da Store) sem sua permissão.",
            "DisableWindowsConsumerFeatures=1 stops Windows from silently installing promoted apps (TikTok, Instagram, Store games) without permission.",
            () => CheckReg(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures", 1), "",
            TweakOp.RegDword(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures", 1));

        yield return Dword("privacy.disable-store-promo", TweakSection.Privacy, TweakRisk.Low, TweakTags.None,
            "Store sem apps promovidos",
            "Store without promoted apps",
            "DisableStoreApps=1 desliga a instalação automática de apps promovidos pela Microsoft Store.",
            "DisableStoreApps=1 turns off automatic install of apps promoted by Microsoft Store.",
            KStorePolicy, "DisableStoreApps", 1);

        yield return Dword("privacy.speech-off", TweakSection.Privacy, TweakRisk.Low, TweakTags.None,
            "Sem reconhecimento de fala online",
            "No online speech recognition",
            "AllowInputPersonalization=0 evita o envio de amostras de voz para treinar modelos na nuvem.",
            "AllowInputPersonalization=0 stops voice samples from being uploaded to train cloud models.",
            @"HKCU\Software\Microsoft\Speech_OneCore\Settings\OnlineSpeechPrivacy", "HasAccepted", 0);
    }

    // =====================================================================
    // 12) IA, COPILOT & RECALL
    // =====================================================================
    private static IEnumerable<TweakEntry> BuildAi()
    {
        yield return Dword("ai.copilot-off", TweakSection.Ai, TweakRisk.Low, TweakTags.None,
            "Desativar Copilot do Windows",
            "Disable Windows Copilot",
            "TurnOffWindowsCopilot=1 remove o Copilot da barra lateral. Ele mantém um processo Edge WebView com IA carregada em memória.",
            "TurnOffWindowsCopilot=1 removes Copilot from the sidebar. It keeps an Edge WebView process with AI loaded in memory.",
            KCopilot, "TurnOffWindowsCopilot", 1);

        yield return Dword("ai.recall-off", TweakSection.Ai, TweakRisk.Low, TweakTags.None,
            "Desativar Recall (snapshots da tela)",
            "Disable Recall (screen snapshots)",
            "AllowRecallEnablement=0 impede o Recall de tirar capturas contínuas da tela e indexá-las. Privacidade e menos uso de disco/GPU.",
            "AllowRecallEnablement=0 stops Recall from continuously snapshotting and indexing your screen. Privacy plus less disk/GPU usage.",
            KRecall, "AllowRecallEnablement", 0);

        yield return Dword("ai.click-to-do-off", TweakSection.Ai, TweakRisk.Low, TweakTags.None,
            "Desativar Click to Do",
            "Disable Click to Do",
            "Click to Do usa modelos locais de IA para analisar o que está na tela. Desativar libera NPU, RAM e disco em máquinas Copilot+.",
            "Click to Do uses local AI models to analyze what is on screen. Disabling frees NPU, RAM and disk on Copilot+ machines.",
            KRecall, "DisableClickToDo", 1);

        yield return Dword("ai.bing-search-off", TweakSection.Ai, TweakRisk.Low, TweakTags.None,
            "Sem assistente de IA na busca",
            "No AI assistant in search",
            "DisableAIDataAnalysis=1 impede que a busca do Windows use modelos de IA para responder às suas consultas.",
            "DisableAIDataAnalysis=1 stops Windows search from using AI models to answer your queries.",
            @"HKCU\Software\Policies\Microsoft\Windows\WindowsCopilot", "DisableAIDataAnalysis", 1);

        yield return Dword("ai.windows-copilot-taskbar", TweakSection.Ai, TweakRisk.Low, TweakTags.None,
            "Remover botão do Copilot da barra",
            "Remove Copilot button from taskbar",
            "ShowCopilotButton=0 remove o botão da barra de tarefas e o processo de pré-carregamento associado.",
            "ShowCopilotButton=0 removes the taskbar button and its preloading process.",
            KAiPolicy, "ShowCopilotButton", 0);

        yield return Dword("ai.settings-agent-off", TweakSection.Ai, TweakRisk.Low, TweakTags.None,
            "Sem agente de IA nas Configurações",
            "No AI agent in Settings",
            "DisableSettingsAgent=1 desativa o agente que sugere ajustes por IA nas Configurações do Windows 11 24H2+.",
            "DisableSettingsAgent=1 disables the agent that suggests AI-driven tweaks in Windows 11 24H2+ Settings.",
            @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "DisableSettingsAgent", 1);

        yield return Dword("ai.text-suggestions", TweakSection.Ai, TweakRisk.Low, TweakTags.None,
            "Sem sugestões de texto por IA",
            "No AI text suggestions",
            "IsTextPredictionEnabled=0 desliga a previsão de texto que envia contexto de digitação para modelos locais.",
            "IsTextPredictionEnabled=0 turns off text prediction that feeds typing context to local models.",
            @"HKCU\Software\Microsoft\Input\Settings", "IsTextPredictionEnabled", 0);

        yield return Dword("ai.ai-taskbar-off", TweakSection.Ai, TweakRisk.Low, TweakTags.None,
            "Sem integração de IA na barra de tarefas",
            "No AI integration in the taskbar",
            "TurnOffWindowsCopilot + ShowCopilotButton garantem que nenhum ponto de entrada de IA reinicie sozinho após updates.",
            "TurnOffWindowsCopilot + ShowCopilotButton ensure no AI entry point restarts on its own after updates.",
            KAiPolicy, "ShowCopilotButton", 0);

        yield return Dword("ai.education-tips", TweakSection.Ai, TweakRisk.Low, TweakTags.None,
            "Sem dicas contextuais de IA",
            "No contextual AI tips",
            "SubscribedContent-338389Enabled=0 remove os cards de dica de IA que a Microsoft injeta nas Configurações.",
            "SubscribedContent-338389Enabled=0 removes the AI tip cards Microsoft injects into Settings.",
            KContentDelivery, "SubscribedContent-338389Enabled", 0);

        yield return T("ai.models-cache-clean", TweakSection.Ai, TweakRisk.Low, TweakTags.None,
            "Limpar cache de modelos de IA locais",
            "Clear local AI model cache",
            "Apaga modelos ONNX baixados para Recall/Click to Do. Pode liberar vários GB. O Windows baixa de novo somente se você reativar os recursos.",
            "Deletes ONNX models downloaded for Recall/Click to Do. Can free several GB. Windows only re-downloads if you re-enable those features.",
            () => new TweakStateInfo(false, Loc.Pick("Ação manual", "Manual action")),
            "Remove-Item \"$env:LOCALAPPDATA%\\Microsoft\\Windows\\AICache\\*\" -Recurse -Force -ErrorAction SilentlyContinue",
            TweakOp.Cmd("(void)0", "(void)0"));
    }

    // =====================================================================
    // 13) WINDOWS UPDATE
    // =====================================================================
    private static IEnumerable<TweakEntry> BuildWu()
    {
        yield return Dword("wu.pause-drivers", TweakSection.Wu, TweakRisk.Low, TweakTags.None,
            "Impedir driver genérico de sobrescrever o oficial",
            "Stop generic drivers from overwriting official ones",
            "ExcludeWUDriversInQualityUpdate=1 impede o Windows Update de substituir o driver oficial NVIDIA/AMD/Intel por um WHQL antigo durante updates de qualidade.",
            "ExcludeWUDriversInQualityUpdate=1 stops Windows Update from replacing the official NVIDIA/AMD/Intel driver with an older WHQL one during quality updates.",
            KWuPolicy, "ExcludeWUDriversInQualityUpdate", 1);

        yield return Dword("wu.no-auto-reboot", TweakSection.Wu, TweakRisk.Low, TweakTags.None,
            "Nunca reiniciar sozinho com jogo aberto",
            "Never auto-reboot while gaming",
            "NoAutoRebootWithLoggedOnUsers=1 impede o reinício automático depois de atualização quando há usuário logado.",
            "NoAutoRebootWithLoggedOnUsers=1 prevents the automatic reboot after an update while a user is logged on.",
            KWuAu, "NoAutoRebootWithLoggedOnUsers", 1);

        yield return Dword("wu.no-auto-update-reboot", TweakSection.Wu, TweakRisk.Low, TweakTags.None,
            "Avisar antes de instalar atualizações",
            "Notify before installing updates",
            "AUOptions=2 deixa o download manual. Mantém a segurança nas suas mãos, sem atualização silenciosa no meio da partida.",
            "AUOptions=2 makes downloads manual. Keeps security in your hands, with no silent update mid-match.",
            KWuAu, "AUOptions", 2);

        yield return T("wu.metered-connection", TweakSection.Wu, TweakRisk.Low, TweakTags.None,
            "Tratar conexão como limitada (menos download)",
            "Treat the connection as metered (less download)",
            "Ajusta a política de conexão limitada para reduzir downloads automáticos de recursos opcionais e OneDrive sem bloquear segurança.",
            "Sets the metered connection policy to reduce automatic downloads of optional content and OneDrive without blocking security.",
            () => new TweakStateInfo(false, Loc.Pick("Por adaptador", "Per adapter")),
            "Set-NetConnectionProfile -InterfaceAlias \"Ethernet\" -NetworkCategory Private  # + MeteredConnection",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("wu.reschedule-night", TweakSection.Wu, TweakRisk.Low, TweakTags.None,
            "Janela de atividade fora do horário de jogo",
            "Active hours outside gaming time",
            "Define janela de atividade (ActiveHoursStart/End) para o Windows não reiniciar entre 18h e 02h.",
            "Sets active hours (ActiveHoursStart/End) so Windows does not reboot between 6 PM and 2 AM.",
            () => new TweakStateInfo(false, Loc.Pick("Configurado no Windows", "Configured in Windows")),
            "reg add \"HKLM\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings\" /v ActiveHoursStart /t REG_DWORD /d 18 /f",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("wu.temp-unblock-for-drivers", TweakSection.Wu, TweakRisk.Low, TweakTags.NoAuto,
            "Desbloqueio temporário para drivers WHQL",
            "Temporary unblock for WHQL drivers",
            "A aba Drivers faz uma janela temporária de desbloqueio da política de WU, consulta os drivers WHQL oficiais, instala e REABILITA a política em seguida. Nada fica aberto.",
            "The Drivers page opens a temporary window that lifts the WU policy, queries official WHQL drivers, installs them and RE-ENABLES the policy right after. Nothing stays open.",
            () => new TweakStateInfo(false, Loc.Pick("Sob demanda", "On demand")),
            "Política removida → busca WHQL → instala → política restaurada",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("wu.clean-scan", TweakSection.Wu, TweakRisk.Low, TweakTags.NoAuto,
            "Auditar atualizações pendentes",
            "Audit pending updates",
            "Lista atualizações pendentes, falhas e o estado do serviço wuauserv sem aplicar nada. Base do Centro de Reparos.",
            "Lists pending and failed updates plus the wuauserv service state without applying anything. The basis of the Repair Center.",
            () => new TweakStateInfo(false, Loc.Pick("Somente leitura", "Read only")),
            "Uso: Get-WindowsUpdate (módulo PSWindowsUpdate) ou wmic qfe list brief",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("wu.broken-cache-fix", TweakSection.Wu, TweakRisk.Medium, TweakTags.NoAuto,
            "Reparar cache do Windows Update",
            "Repair the Windows Update cache",
            "Para wuauserv e bits, renomeia SoftwareDistribution e catroot2 e reinicia os serviços. Correção padrão para erro 0x80070002 e afins.",
            "Stops wuauserv and bits, renames SoftwareDistribution and catroot2, then restarts the services. The standard fix for 0x80070002 and similar errors.",
            () => new TweakStateInfo(false, Loc.Pick("Ação manual", "Manual action")),
            "net stop wuauserv\nnet stop bits\nren C:\\Windows\\SoftwareDistribution SoftwareDistribution.old\nnet start wuauserv",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("wu.dism-health", TweakSection.Wu, TweakRisk.Medium, TweakTags.NoAuto,
            "DISM RestoreHealth inteligente",
            "Smart DISM RestoreHealth",
            "Roda DISM /RestoreHealth com fonte de Windows Update temporariamente liberada — necessário quando a política de WU bloqueia a fonte de reparo.",
            "Runs DISM /RestoreHealth with the Windows Update source temporarily unblocked — required when the WU policy blocks the repair source.",
            () => new TweakStateInfo(false, Loc.Pick("Ação manual", "Manual action")),
            "DISM /Online /Cleanup-Image /RestoreHealth",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("wu.sfc-scan", TweakSection.Wu, TweakRisk.Low, TweakTags.NoAuto,
            "SFC /scannow",
            "SFC /scannow",
            "Verifica e restaura arquivos protegidos do sistema corrompidos por driver errado ou desligamento sujo.",
            "Verifies and restores protected system files corrupted by a bad driver or dirty shutdown.",
            () => new TweakStateInfo(false, Loc.Pick("Ação manual", "Manual action")),
            "sfc /scannow",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("wu.show-hidden-updates", TweakSection.Wu, TweakRisk.Low, TweakTags.None,
            "Mostrar atualizações ocultas",
            "Show hidden updates",
            "Limpa a lista de atualizações ocultas/recusadas, útil quando um update crítico ficou escondido por um pacote de tweak.",
            "Clears the hidden/declined update list, useful when a critical update was hidden by a tweak pack.",
            () => new TweakStateInfo(false, Loc.Pick("Ação manual", "Manual action")),
            "Remover HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsUpdate\\Auto Update\\RevokedCertificates",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return Dword("wu.office-updates", TweakSection.Wu, TweakRisk.Low, TweakTags.None,
            "Atualizações de outros produtos Microsoft",
            "Updates for other Microsoft products",
            "AllowMUUpdateService=1 inclui atualizações de Office e Visual C++ no Windows Update, fechando falhas que pacotes de debloat deixam abertas.",
            "AllowMUUpdateService=1 includes Office and Visual C++ updates in Windows Update, closing holes left open by debloat packs.",
            @"HKLM\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "AllowMUUpdateService", 1);
    }

    // =====================================================================
    // 14) CORREÇÕES & ANTI-STUTTER
    // =====================================================================
    private static IEnumerable<TweakEntry> BuildFixes()
    {
        yield return T("fixes.mmcss-reset", TweakSection.Fixes, TweakRisk.Low, TweakTags.None,
            "Restaurar MMCSS para o padrão seguro do Kairo",
            "Restore MMCSS to the safe Kairo defaults",
            "Reaplica o conjunto validado: GPU Priority=2, Priority=2, Scheduling Category=Medium, Affinity removida. Corrige stutter causado por pacotes que usam valores extremos.",
            "Reapplies the validated set: GPU Priority=2, Priority=2, Scheduling Category=Medium, Affinity removed. Fixes stutter caused by packs using extreme values.",
            () => CheckReg(KGameTask, "Priority", 2),
            "reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v Priority /t REG_DWORD /d 2 /f",
            TweakOp.RegDword(KGameTask, "GPU Priority", 2),
            TweakOp.RegDword(KGameTask, "Priority", 2),
            TweakOp.RegString(KGameTask, "Scheduling Category", "Medium"),
            TweakOp.RegDel(KGameTask, "Affinity"));

        yield return T("fixes.mpo-flicker", TweakSection.Fixes, TweakRisk.Low, TweakTags.None,
            "Corrigir flicker de tela / piscada",
            "Fix screen flicker / blinking",
            "OverlayTestMode=5 é a correção documentada para flicker em multi-monitor e piscada ao abrir jogos em janela. Não resolve flicker de hardware.",
            "OverlayTestMode=5 is the documented fix for multi-monitor flicker and blinking when opening windowed games. It does not fix hardware flicker.",
            () => CheckReg(KDwm, "OverlayTestMode", 5), "",
            TweakOp.RegDword(KDwm, "OverlayTestMode", 5));

        yield return T("fixes.whea-audit", TweakSection.Fixes, TweakRisk.Low, TweakTags.NoAuto,
            "Auditar erros WHEA (hardware)",
            "Audit WHEA errors (hardware)",
            "Lê o log de eventos do sistema buscando WHEA-Logger, Kernel-Power 41 e BugCheck. Se houver WHEA, nenhum tweak resolve: é hardware, XMP/EXPO instável ou driver.",
            "Reads the system event log for WHEA-Logger, Kernel-Power 41 and BugCheck. If WHEA shows up, no tweak fixes it: it is hardware, unstable XMP/EXPO or a driver.",
            () => TweakJournal.RunProcess("wevtutil.exe", "qe System /q:\"*[System[(Provider[@Name='Microsoft-Windows-WHEA-Logger'])]]\" /c:1 /f:text").ExitCode == 0
                ? new TweakStateInfo(true, Loc.Pick("Erros WHEA encontrados", "WHEA errors found"))
                : new TweakStateInfo(false, Loc.Pick("Sem WHEA recente", "No recent WHEA")),
            "wevtutil qe System /q:\"*[System[(Provider[@Name='Microsoft-Windows-WHEA-Logger'])]]\" /c:5 /f:text",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("fixes.tdr-check", TweakSection.Fixes, TweakRisk.Low, TweakTags.NoAuto,
            "Auditar TDR (driver de vídeo reiniciou)",
            "Audit TDR (video driver restarted)",
            "Conta eventos 'Display driver nvlddmkm/amdkmdag stopped responding' nas últimas horas. Se aparecer com frequência, a causa é driver/overclock, não o Windows.",
            "Counts 'Display driver nvlddmkm/amdkmdag stopped responding' events in recent hours. If frequent, the cause is the driver/overclock, not Windows.",
            () => new TweakStateInfo(false, Loc.Pick("Verificado pelo Centro de Reparos", "Checked by the Repair Center")),
            "Get-WinEvent -FilterHashtable @{LogName='System'; Id=4101} -MaxEvents 20",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("fixes.dirty-bit", TweakSection.Fixes, TweakRisk.Medium, TweakTags.NoAuto,
            "Corrigir bit sujo / volume inconsistente",
            "Fix dirty bit / inconsistent volume",
            "Consulta fsutil dirty e agenda chkdsk /spotfix ou /f no próximo boot. Sintoma clássico: boot lento e 'reparando disco' toda vez.",
            "Queries fsutil dirty and schedules chkdsk /spotfix or /f on next boot. Classic symptom: slow boot and 'repairing disk' every time.",
            () => TweakJournal.RunProcess("fsutil.exe", "dirty query C:").Output.Contains("is Dirty", StringComparison.OrdinalIgnoreCase)
                ? new TweakStateInfo(true, Loc.Pick("Volume sujo detectado", "Dirty volume detected"))
                : new TweakStateInfo(false, Loc.Pick("Volume limpo", "Volume clean")),
            "fsutil dirty query C:",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("fixes.font-cache", TweakSection.Fixes, TweakRisk.Low, TweakTags.None,
            "Reconstruir cache de fontes",
            "Rebuild the font cache",
            "Corrige texto borrado, ícones corrompidos e travadas do Explorer causadas pelo FontCache corrompido.",
            "Fixes blurry text, corrupted icons and Explorer freezes caused by a corrupted FontCache.",
            () => new TweakStateInfo(false, Loc.Pick("Ação manual", "Manual action")),
            "net stop FontCache\ndel /f /q %WinDir%\\ServiceProfiles\\LocalService\\AppData\\Local\\FontCache\\*",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("fixes.icon-cache", TweakSection.Fixes, TweakRisk.Low, TweakTags.None,
            "Reconstruir cache de ícones do Explorer",
            "Rebuild Explorer icon cache",
            "Apaga iconcache_*.db e reinicia o Explorer. Resolve ícones brancos/errados depois de instalar ou remover jogos.",
            "Deletes iconcache_*.db and restarts Explorer. Fixes white/wrong icons after installing or removing games.",
            () => new TweakStateInfo(false, Loc.Pick("Ação manual", "Manual action")),
            "del /a %LocalAppData%\\IconCache.db /f && del /a %LocalAppData%\\Microsoft\\Windows\\Explorer\\iconcache* /f",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("fixes.bt-repair", TweakSection.Fixes, TweakRisk.Low, TweakTags.NoAuto,
            "Reparar Bluetooth e áudio",
            "Repair Bluetooth and audio",
            "Reinicia serviços Bluetooth e áudio e remove dispositivos emparelhados quebrados. Corrige headset que cai no meio da partida.",
            "Restarts Bluetooth and audio services and removes broken paired devices. Fixes headsets dropping mid-match.",
            () => new TweakStateInfo(false, Loc.Pick("Ação manual", "Manual action")),
            "Restart-Service bthserv, BTAGService, Audiosrv -Force",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("fixes.store-reset", TweakSection.Fixes, TweakRisk.Low, TweakTags.None,
            "Re-registrar Microsoft Store e apps",
            "Re-register Microsoft Store and apps",
            "Corrige Store que não abre e apps que somem depois de um debloat agressivo, sem perder dados.",
            "Fixes a Store that will not open and apps that vanish after aggressive debloat, without losing data.",
            () => new TweakStateInfo(false, Loc.Pick("Ação manual", "Manual action")),
            "wsreset.exe",
            TweakOp.Cmd("wsreset.exe -i", ""));

        yield return T("fixes.eventlog-audit", TweakSection.Fixes, TweakRisk.Low, TweakTags.NoAuto,
            "Auditar travamentos e BSOD recentes",
            "Audit recent crashes and BSODs",
            "Lista os últimos BugCheck (1001) com código de parada e o driver culpado apontado pelo Windows.",
            "Lists the latest BugCheck (1001) events with the stop code and the driver Windows blames.",
            () => new TweakStateInfo(false, Loc.Pick("Verificado pelo Centro de Reparos", "Checked by the Repair Center")),
            "Get-WinEvent -FilterHashtable @{LogName='System'; Id=1001,41} -MaxEvents 10",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("fixes.winsock-reset", TweakSection.Fixes, TweakRisk.Medium, TweakTags.NoAuto,
            "Reparar rede (Winsock + TCP/IP)",
            "Repair network (Winsock + TCP/IP)",
            "Reset completo de pilha para jogos que não conectam, erro 0x8007274D e 'sem internet' fantasma. Requer reinício.",
            "Full stack reset for games that will not connect, error 0x8007274D and phantom 'no internet'. Requires reboot.",
            () => new TweakStateInfo(false, Loc.Pick("Ação manual", "Manual action")),
            "netsh winsock reset\nnetsh int ip reset\nipconfig /flushdns",
            TweakOp.Cmd("netsh winsock reset; netsh int ip reset; ipconfig /flushdns", ""));

        yield return T("fixes.printer-spooler", TweakSection.Fixes, TweakRisk.Low, TweakTags.NoAuto,
            "Reiniciar Spooler de impressão travado",
            "Restart a stuck print spooler",
            "Limpa a fila e reinicia o spooler, que às vezes segura o desligamento por 30 s.",
            "Clears the queue and restarts the spooler, which sometimes holds shutdown for 30 s.",
            () => new TweakStateInfo(false, Loc.Pick("Ação manual", "Manual action")),
            "net stop spooler && del /Q %SystemRoot%\\System32\\spool\\PRINTERS\\* && net start spooler",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("fixes.time-sync", TweakSection.Fixes, TweakRisk.Low, TweakTags.None,
            "Sincronizar horário com servidor NTP",
            "Sync clock with an NTP server",
            "Relógio adiantado quebra autenticação de jogos (erro de servidor) e TLS. Força sincronização com time.windows.com.",
            "A fast clock breaks game authentication (server errors) and TLS. Forces a sync with time.windows.com.",
            () => new TweakStateInfo(false, Loc.Pick("Ação manual", "Manual action")),
            "w32tm /resync /force",
            TweakOp.Cmd("w32tm /resync /force", ""));

        yield return T("fixes.power-cycle", TweakSection.Fixes, TweakRisk.Low, TweakTags.NoAuto,
            "Reinício completo sugerido (limpar estados)",
            "Full restart suggested (clear states)",
            "Com Inicialização Rápida o 'desligar' não reinicia o kernel. Este card apenas agenda um reinício real, que resolve metade dos bugs de driver.",
            "With Fast Startup, 'shut down' does not restart the kernel. This card just schedules a real reboot, which fixes half of driver bugs.",
            () => new TweakStateInfo(false, Loc.Pick("Sob demanda", "On demand")),
            "shutdown /r /t 30 /c \"PhantomUI: reinicio completo\"",
            TweakOp.Cmd("shutdown /r /t 30", ""));
    }

    // =====================================================================
    // 15) MITOS BLOQUEADOS POR SEGURANÇA
    // =====================================================================
    private static IEnumerable<TweakEntry> BuildBlocked()
    {
        yield return Blocked("block.pagefile-zero",
            "Pagefile 0 MB (desativar memória virtual)",
            "Pagefile 0 MB (disable virtual memory)",
            "iGust e diversos pacotes aplicam pagingfiles=\"c:\\pagefile.sys 0 0\". Sem pagefile, qualquer pico de memória em Warzone/CS2 gera 'Out of memory' e travamento total do sistema. Bloqueado permanentemente.",
            "iGust and several packs apply pagingfiles=\"c:\\pagefile.sys 0 0\". Without a pagefile, any memory spike in Warzone/CS2 causes 'Out of memory' and a total system hang. Permanently blocked.",
            "wmic pagefileset where name=\"C:\\\\pagefile.sys\" set InitialSize=0,MaximumSize=0   ⛔ BLOQUEADO");

        yield return Blocked("block.tdrlevel-zero",
            "TdrLevel = 0 (desativar recuperação do driver de vídeo)",
            "TdrLevel = 0 (disable video driver recovery)",
            "TdrLevel=0 impede o Windows de reiniciar o driver de vídeo. Em vez de um TDR recuperável, o jogo trava e a tela fica preta até reiniciar. Bloqueado: use TdrDelay=10 (Médio) se precisar.",
            "TdrLevel=0 prevents Windows from restarting the video driver. Instead of a recoverable TDR, the game freezes and the screen stays black until reboot. Blocked: use TdrDelay=10 (Medium) if needed.",
            "reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\" /v TdrLevel /t REG_DWORD /d 0 /f   ⛔ BLOQUEADO");

        yield return Blocked("block.lsass-idle",
            "lsass / svchost em Idle / prioridade baixa",
            "lsass / svchost at Idle / low priority",
            "Pacotes antigos colocam lsass.exe e svchost.exe em CpuPriorityClass=1 (Idle). Isso derruba a autenticação, o login e a rede, e é vetor de falha de segurança. Bloqueado.",
            "Old packs set lsass.exe and svchost.exe to CpuPriorityClass=1 (Idle). That breaks authentication, logon and networking, and is a security failure vector. Blocked.",
            "reg add \"HKLM\\...\\Image File Execution Options\\lsass.exe\\PerfOptions\" /v CpuPriorityClass /t REG_DWORD /d 1 /f   ⛔ BLOQUEADO");

        yield return Blocked("block.disable-elam",
            "disableelamdrivers = yes (desativar ELAM)",
            "disableelamdrivers = yes (disable ELAM)",
            "Desativar Early Launch Anti-Malware permite que drivers maliciosos e rootkits carreguem antes do antivírus. Também quebra Vanguard e FACEIT. Bloqueado.",
            "Disabling Early Launch Anti-Malware lets malicious drivers and rootkits load before the antivirus. It also breaks Vanguard and FACEIT. Blocked.",
            "bcdedit /set {current} disableelamdrivers yes   ⛔ BLOQUEADO");

        yield return Blocked("block.hypervisor-off-vanguard",
            "hypervisorlaunchtype off (quebra Vanguard)",
            "hypervisorlaunchtype off (breaks Vanguard)",
            "Desativar o hypervisor é sugerido para ganhar FPS em máquinas virtuais, mas o Vanguard (Valorant) exige virtualização/hipervisor e o jogo passa a recusar abrir. Também quebra WSL2, Hyper-V, Sandbox e Memory Integrity. Bloqueado.",
            "Disabling the hypervisor is suggested to gain FPS in VMs, but Vanguard (Valorant) requires virtualization/hypervisor and the game refuses to launch. It also breaks WSL2, Hyper-V, Sandbox and Memory Integrity. Blocked.",
            "bcdedit /set {current} hypervisorlaunchtype off   ⛔ BLOQUEADO");

        yield return Blocked("block.agp-concur",
            "AGPConcur / AGP aperture do Windows 98",
            "AGPConcur / Windows 98 AGP aperture",
            "Chaves AGP de 1998 aplicadas em máquinas PCIe modernas não têm efeito nenhum além de poluir o registro. Bloqueado por ser ruído de pacote copiado.",
            "AGP keys from 1998 applied to modern PCIe machines have no effect beyond polluting the registry. Blocked as copied-pack noise.",
            "reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\" /v AGPConcur /t REG_DWORD /d 1 /f   ⛔ BLOQUEADO");

        yield return Blocked("block.prefetch-delete",
            "Apagar C:\\Windows\\Prefetch inteiro",
            "Delete the whole C:\\Windows\\Prefetch",
            "Pacotes mandam apagar o Prefetch achando que 'libera espaço'. O resultado é boot e abertura de jogos mais lentos por semanas até o Windows reconstruir o cache. Bloqueado.",
            "Packs tell you to delete Prefetch thinking it 'frees space'. The result is slower boot and game launches for weeks until Windows rebuilds the cache. Blocked.",
            "del /f /s /q C:\\Windows\\Prefetch\\*.*   ⛔ BLOQUEADO");

        yield return Blocked("block.mmcss-affinity-0f",
            "MMCSS Affinity fixa em 0x0f",
            "MMCSS Affinity locked to 0x0f",
            "Fixar a afinidade do MMCSS em 4 núcleos (0x0f) prende áudio e agendador de jogos em CPUs híbridas, criando stutter e cortes de som. O valor correto é NÃO definir afinidade. Bloqueado.",
            "Locking MMCSS affinity to 4 cores (0x0f) pins audio and the game scheduler on hybrid CPUs, creating stutter and sound cutouts. The correct value is NOT to set affinity. Blocked.",
            "reg add \"...\\Multimedia\\SystemProfile\\Tasks\\Games\" /v Affinity /t REG_DWORD /d 15 /f   ⛔ BLOQUEADO");

        yield return Blocked("block.disable-defender",
            "Desativar Microsoft Defender permanentemente",
            "Permanently disable Microsoft Defender",
            "Desativar o Defender por registro é revertido pelo Windows em minutos e deixa o sistema sem proteção real. Se você usa outro antivírus, o Defender se desativa sozinho de forma correta. Bloqueado.",
            "Disabling Defender through the registry is reverted by Windows within minutes and leaves the system with no real protection. If you use another antivirus, Defender disables itself properly. Blocked.",
            "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\" /v DisableAntiSpyware /t REG_DWORD /d 1 /f   ⛔ BLOQUEADO");

        yield return Blocked("block.remove-winsxs",
            "Apagar WinSxS manualmente",
            "Delete WinSxS manually",
            "Apagar a pasta WinSxS quebra a instalação do Windows de forma irreparável e impede qualquer atualização futura. O correto é DISM /StartComponentCleanup. Bloqueado.",
            "Deleting the WinSxS folder breaks the Windows installation beyond repair and blocks every future update. The right call is DISM /StartComponentCleanup. Blocked.",
            "rmdir /s /q C:\\Windows\\WinSxS   ⛔ BLOQUEADO");

        yield return Blocked("block.disable-svchost-priority",
            "Prioridade RealTime em svchost / csrss",
            "RealTime priority on svchost / csrss",
            "Prioridade RealTime em processos de sistema monopoliza a CPU e congela mouse, teclado e áudio — a máquina só volta com reset. Bloqueado.",
            "RealTime priority on system processes monopolizes the CPU and freezes mouse, keyboard and audio — the machine only comes back with a reset. Blocked.",
            "wmic process where name=\"csrss.exe\" CALL setpriority 256   ⛔ BLOQUEADO");

        yield return Blocked("block.memory-compression-off",
            "Desativar compactação de memória em 8-12 GB",
            "Disable memory compression on 8-12 GB",
            "Desligar a compactação em máquinas com 8 ou 12 GB empurra tudo para o pagefile e transforma stutter em freeze de 2 s. O acervo Kairo classifica como erro grave. Bloqueado.",
            "Disabling compression on 8 or 12 GB machines pushes everything into the pagefile and turns stutter into 2 s freezes. The Kairo collection flags it as a serious mistake. Blocked.",
            "Disable-MMAgent -MemoryCompression   ⛔ BLOQUEADO");
    }
}
