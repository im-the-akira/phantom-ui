using Microsoft.Win32;

namespace PhantomUI;

public static partial class TweakCatalog
{
    internal const string KClassGpu = @"HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000";
    internal const string KGameBar = @"HKCU\Software\Microsoft\GameBar";
    internal const string KGameConfigStore = @"HKCU\System\GameConfigStore";
    internal const string KDirectXUser = @"HKCU\Software\Microsoft\DirectX\UserGpuPreferences";
    internal const string KDesktop = @"HKCU\Control Panel\Desktop";
    internal const string KMouseKey = @"HKCU\Control Panel\Mouse";
    internal const string KKeyboard = @"HKCU\Control Panel\Keyboard";
    internal const string KShaderCache = @"HKCU\Software\Microsoft\DirectX\ShaderCache";
    internal const string KKernel = @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel";
    internal const string KPrefetch = @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters";
    internal const string KContentDelivery = @"HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";
    internal const string KSearchPolicy = @"HKCU\Software\Policies\Microsoft\Windows\Explorer";
    internal const string KDoConfig = @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config";
    internal const string KSerialize = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize";
    internal const string KExplorerPolicy = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer";
    internal const string KSearchBox = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Search";
    internal const string KVisualFx = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects";
    internal const string KPowerUser = @"HKLM\SYSTEM\CurrentControlSet\Control\Power\User\Default\PowerSchemes";
    internal const string KGraphicsDrivers2 = @"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers";

    private static List<TweakEntry> BuildAll()
    {
        var l = new List<TweakEntry>();
        l.AddRange(BuildOs());
        l.AddRange(BuildMenu());
        l.AddRange(BuildCpu());
        l.AddRange(BuildGpu());
        l.AddRange(BuildRam());
        l.AddRange(BuildDisk());
        l.AddRange(BuildInput());
        l.AddRange(BuildNet());
        l.AddRange(BuildPower());
        l.AddRange(BuildGames());
        l.AddRange(BuildPrivacy());
        l.AddRange(BuildAi());
        l.AddRange(BuildWu());
        l.AddRange(BuildFixes());
        l.AddRange(BuildBlocked());
        return l;
    }

    // =====================================================================
    // 1) SISTEMA & WINDOWS
    // =====================================================================
    private static IEnumerable<TweakEntry> BuildOs()
    {
        yield return Dword("os.priority-separation", TweakSection.Os, TweakRisk.Medium, TweakTags.None,
            "Separação de prioridade 0x26 (38) — CPU para o jogo",
            "Priority separation 0x26 (38) — CPU favors the game",
            "Win32PrioritySeparation=38 entrega quantum curto e variável ao processo em primeiro plano. É o valor validado do acervo Kairo: resposta mais rápida ao jogo aberto sem matar o resto do sistema.",
            "Win32PrioritySeparation=38 gives the foreground process a short, variable quantum. Kairo-validated value: snappier in-game response without starving the rest of the system.",
            KPriority, "Win32PrioritySeparation", 38);

        yield return Dword("os.large-system-cache", TweakSection.Os, TweakRisk.Medium, TweakTags.None,
            "Manter LargeSystemCache desligado (0)",
            "Keep LargeSystemCache off (0)",
            "LargeSystemCache=1 reserva um cache enorme para arquivos e engasga jogos com muita textura. O acervo Kairo exige manter 0 em máquinas gamer.",
            "LargeSystemCache=1 reserves a huge file cache and stutters texture-heavy games. Kairo requires keeping it 0 on gaming machines.",
            KMemMgmt, "LargeSystemCache", 0);

        yield return Dword("os.paging-executive", TweakSection.Os, TweakRisk.Medium, TweakTags.HighRam | TweakTags.RequiresReboot,
            "Manter o kernel na RAM (DisablePagingExecutive)",
            "Keep the kernel in RAM (DisablePagingExecutive)",
            "Impede o Windows de paginar o núcleo e os drivers para o disco. Ganho de latência real, mas só faz sentido com 16 GB ou mais. Requer reinício.",
            "Stops Windows from paging the kernel and drivers to disk. Real latency win, but only makes sense with 16 GB or more. Requires reboot.",
            KMemMgmt, "DisablePagingExecutive", 1);

        yield return Dword("os.svchost-split", TweakSection.Os, TweakRisk.Medium, TweakTags.RequiresReboot,
            "Limite de divisão do svchost em 4 GB",
            "svchost split threshold at 4 GB",
            "SvcHostSplitThresholdInKB=0x400000 agrupa serviços em menos processos: menos troca de contexto e menos RAM gasta em hosts ociosos.",
            "SvcHostSplitThresholdInKB=0x400000 groups services into fewer processes: less context switching and less RAM spent on idle hosts.",
            KSessionManager, "SvcHostSplitThresholdInKB", 0x400000);

        yield return Dword("os.ntfs-last-access", TweakSection.Os, TweakRisk.Low, TweakTags.None,
            "Desativar registro de último acesso a arquivos",
            "Disable NTFS last access timestamps",
            "NtfsDisableLastAccessUpdate=1 evita uma escrita de metadado a cada leitura de arquivo — menos I/O inútil em SSD e HDD.",
            "NtfsDisableLastAccessUpdate=1 avoids a metadata write on every file read — less wasted I/O on SSD and HDD.",
            KFilesystem, "NtfsDisableLastAccessUpdate", 1);

        yield return Dword("os.ntfs-8dot3", TweakSection.Os, TweakRisk.Medium, TweakTags.None,
            "Desativar nomes curtos 8.3 do NTFS",
            "Disable NTFS 8.3 short names",
            "NtfsDisable8dot3NameCreation=1 acelera a criação de arquivos em pastas grandes. Trade-off: instaladores muito antigos que dependem de PROGRA~1 podem falhar.",
            "NtfsDisable8dot3NameCreation=1 speeds up file creation in large folders. Trade-off: very old installers relying on PROGRA~1 may fail.",
            KFilesystem, "NtfsDisable8dot3NameCreation", 1);

        yield return Dword("os.ntfs-memory", TweakSection.Os, TweakRisk.Low, TweakTags.HighRam,
            "NTFS com uso de memória alto",
            "NTFS at high memory usage",
            "NtfsMemoryUsage=2 aumenta o cache de metadados do NTFS. Em máquinas com 16 GB+ reduz acessos ao disco durante o carregamento dos jogos.",
            "NtfsMemoryUsage=2 raises the NTFS metadata cache. On 16 GB+ machines it cuts disk access while games load.",
            KFilesystem, "NtfsMemoryUsage", 2);

        yield return Dword("os.prefetch-keep", TweakSection.Os, TweakRisk.Low, TweakTags.None,
            "Manter Prefetch e Superfetch ativos",
            "Keep Prefetch and Superfetch enabled",
            "EnablePrefetcher=3 mantém o cache de pré-carregamento de programas. Desativar é mito: em HDD e SSD moderno o Prefetch só ajuda.",
            "EnablePrefetcher=3 keeps the program preload cache. Disabling it is a myth: on modern HDD and SSD Prefetch only helps.",
            KPrefetch, "EnablePrefetcher", 3);

        yield return Bcd("os.boot-menu-timeout", TweakSection.Os, TweakRisk.Low, TweakTags.None,
            "Menu de boot em 3 segundos",
            "Boot menu timeout at 3 seconds",
            "Reduz o tempo parado no menu de inicialização de 30 para 3 segundos. Se você usa dual boot, use as setas na hora do boot.",
            "Cuts idle time in the boot menu from 30 to 3 seconds. If you dual boot, use the arrow keys right at boot.",
            "timeout", "3");

        yield return Bcd("os.no-gui-boot", TweakSection.Os, TweakRisk.Low, TweakTags.RequiresReboot,
            "Boot sem animação do logo (NoGuiBoot)",
            "Boot without the logo animation (NoGuiBoot)",
            "bootux=disabled remove a animação do Windows na inicialização: ganho pequeno, mas perceptível em notebooks com vídeo integrado.",
            "bootux=disabled removes the Windows boot animation: small but noticeable gain on laptops with integrated graphics.",
            "bootux", "disabled");

        yield return Dword("os.dwm-buffered-paint", TweakSection.Os, TweakRisk.Low, TweakTags.None,
            "DWM com baixa latência de composição",
            "DWM low composition latency",
            "Desliga o buffered composition do DWM em tela cheia, reduzindo cerca de um quadro de atraso na exibição.",
            "Turns off DWM buffered composition in fullscreen, removing roughly one frame of display delay.",
            KDwm, "UseBufferedPaintComposition", 0);

        yield return Dword("os.no-tips", TweakSection.Os, TweakRisk.Low, TweakTags.None,
            "Remover sugestões e dicas do Windows",
            "Remove Windows tips and suggestions",
            "SoftLandingEnabled=0 desliga as sugestões patrocinadas que consomem CPU e rede em segundo plano.",
            "SoftLandingEnabled=0 turns off sponsored suggestions that burn CPU and network in the background.",
            KContentDelivery, "SoftLandingEnabled", 0);

        yield return Dword("os.search-no-web", TweakSection.Os, TweakRisk.Low, TweakTags.None,
            "Pesquisa do Windows sem busca na web",
            "Windows Search without web results",
            "DisableSearchBoxSuggestions=1 impede que o menu Iniciar consulte a Bing a cada tecla digitada: menos rede e menos atraso ao pesquisar.",
            "DisableSearchBoxSuggestions=1 stops the Start menu from querying Bing on every keystroke: less network and less lag while searching.",
            KSearchPolicy, "DisableSearchBoxSuggestions", 1);

        yield return Dword("os.do-lan-only", TweakSection.Os, TweakRisk.Low, TweakTags.None,
            "Delivery Optimization só na LAN",
            "Delivery Optimization LAN only",
            "DODownloadMode=1 evita que sua banda de upload seja usada para semear atualizações do Windows na internet.",
            "DODownloadMode=1 stops your upload bandwidth from seeding Windows updates across the internet.",
            KDoConfig, "DODownloadMode", 1);

        yield return Dword("os.startup-delay", TweakSection.Os, TweakRisk.Low, TweakTags.None,
            "Sem atraso artificial de apps de inicialização",
            "No artificial startup app delay",
            "StartupDelayInMSec=0 remove o atraso de 10 segundos que o Explorer impõe aos programas de inicialização.",
            "StartupDelayInMSec=0 removes the 10-second delay Explorer imposes on startup programs.",
            KSerialize, "StartupDelayInMSec", 0);

        yield return Dword("os.foreground-lock", TweakSection.Os, TweakRisk.Low, TweakTags.None,
            "Janela em primeiro plano priorizada",
            "Foreground window prioritized",
            "ForegroundLockTimeout=0 reduz a espera para a janela do jogo receber foco do teclado e do mouse.",
            "ForegroundLockTimeout=0 reduces the wait for the game window to grab keyboard and mouse focus.",
            KDesktop, "ForegroundLockTimeout", 0);

        yield return Dword("os.auto-end-tasks", TweakSection.Os, TweakRisk.Low, TweakTags.None,
            "Fechar apps travados automaticamente",
            "Auto-close hung apps",
            "AutoEndTasks=1 evita ficar preso no diálogo 'o programa parou de responder' e volta mais rápido para a área de trabalho.",
            "AutoEndTasks=1 avoids getting stuck on the 'program not responding' dialog and returns to the desktop faster.",
            KDesktop, "AutoEndTasks", 1);

        yield return Dword("os.no-autoplay", TweakSection.Os, TweakRisk.Low, TweakTags.None,
            "Desativar AutoPlay de pendrives",
            "Disable removable drive AutoPlay",
            "NoDriveTypeAutoRun=0xFF bloqueia a execução automática de mídia removível: menos interrupções e proteção contra autorun.",
            "NoDriveTypeAutoRun=0xFF blocks automatic execution of removable media: fewer interruptions and Autorun protection.",
            KExplorerPolicy, "NoDriveTypeAutoRun", 255);

        yield return T("os.memory-compression-on", TweakSection.Os, TweakRisk.Low, TweakTags.LowRam,
            "Manter Compactação de Memória ligada (RAM <= 12 GB)",
            "Keep Memory Compression on (RAM <= 12 GB)",
            "Enable-MMAgent -MemoryCompression ligada reduz o uso de swap em máquinas com pouca RAM. O acervo Kairo marca desligar isso como erro em 8/12 GB.",
            "Enable-MMAgent -MemoryCompression on reduces swap usage on low-RAM machines. The Kairo collection flags disabling it as a mistake on 8/12 GB.",
            () => new TweakStateInfo(
                TweakJournal.RunProcess("powershell.exe", "-NoProfile -Command \"(Get-MMAgent).MemoryCompression\"").Output.Trim().StartsWith("True", StringComparison.OrdinalIgnoreCase),
                Loc.Pick("estado do MMAgent", "MMAgent state")),
            "powershell -Command \"Enable-MMAgent -MemoryCompression\"",
            TweakOp.Cmd("powershell -Command \"Enable-MMAgent -MemoryCompression\"", "powershell -Command \"Disable-MMAgent -MemoryCompression\""));
    }

    // =====================================================================
    // 2) MENU & INTERFACE
    // =====================================================================
    private static IEnumerable<TweakEntry> BuildMenu()
    {
        yield return Str("menu.show-delay", TweakSection.Menu, TweakRisk.Low, TweakTags.None,
            "Menu instantâneo (MenuShowDelay 0)",
            "Instant menus (MenuShowDelay 0)",
            "MenuShowDelay=0 remove os 400 ms de espera ao abrir menus e submenus do Windows e do Explorer.",
            "MenuShowDelay=0 removes the 400 ms wait when opening Windows and Explorer menus and submenus.",
            KDesktop, "MenuShowDelay", "0");

        yield return Str("menu.hung-timeout", TweakSection.Menu, TweakRisk.Low, TweakTags.None,
            "Timeout de app travado em 1 segundo",
            "Hung app timeout at 1 second",
            "HungAppTimeout=1000 detecta travamento em 1 s em vez de 5 s, liberando a interface mais rápido.",
            "HungAppTimeout=1000 detects a hang in 1 s instead of 5 s, freeing the UI faster.",
            KDesktop, "HungAppTimeout", "1000");

        yield return Str("menu.kill-service-timeout", TweakSection.Menu, TweakRisk.Medium, TweakTags.None,
            "Encerrar serviços em 2 s no desligamento",
            "Kill services in 2 s at shutdown",
            "WaitToKillServiceTimeout=2000 faz o Windows desligar rápido em vez de esperar 5 s por serviço. Médio: um serviço com escrita pendente pode ser cortado.",
            "WaitToKillServiceTimeout=2000 makes Windows shut down fast instead of waiting 5 s per service. Medium: a service with pending writes may be cut off.",
            KSessionManager, "WaitToKillServiceTimeout", "2000");

        yield return Str("menu.drag-full-windows", TweakSection.Menu, TweakRisk.Low, TweakTags.None,
            "Arrastar janelas mostrando conteúdo",
            "Drag windows with full content",
            "DragFullWindows=1 mostra o conteúdo durante o arraste — mais fluido em GPU moderna.",
            "DragFullWindows=1 shows content while dragging — smoother on a modern GPU.",
            KDesktop, "DragFullWindows", "1");

        yield return T("menu.disable-animations", TweakSection.Menu, TweakRisk.Low, TweakTags.None,
            "Desativar animações de janela",
            "Disable window animations",
            "MinAnimate=0 e TaskbarAnimations=0 eliminam a animação de minimizar/maximizar, deixando a troca de janelas imediata.",
            "MinAnimate=0 and TaskbarAnimations=0 remove the minimize/maximize animation, making window switching immediate.",
            () => CheckReg(KDwm, "MinAnimate", "0"), "",
            TweakOp.RegString(KDwm, "MinAnimate", "0"), TweakOp.RegDword(KExplorerAdv, "TaskbarAnimations", 0));

        yield return T("menu.visual-fx-performance", TweakSection.Menu, TweakRisk.Low, TweakTags.None,
            "Efeitos visuais em modo desempenho",
            "Visual effects at performance mode",
            "UserPreferencesMask desliga sombras e feedback visual animado: menos trabalho de composição por quadro para o DWM. Reverter volta à máscara padrão do Windows 11.",
            "UserPreferencesMask turns off shadows and animated visual feedback: less composition work per frame for DWM. Revert restores the Windows 11 default mask.",
            () => CheckReg(KDesktop, "UserPreferencesMask", Convert.ToHexString(new byte[] { 0x90, 0x12, 0x03, 0x80, 0x10, 0x00, 0x00, 0x00 })), "",
            TweakOp.RegBinary(KDesktop, "UserPreferencesMask", new byte[] { 0x90, 0x12, 0x03, 0x80, 0x10, 0x00, 0x00, 0x00 }));

        yield return Dword("menu.no-widgets", TweakSection.Menu, TweakRisk.Low, TweakTags.None,
            "Remover widgets da barra de tarefas",
            "Remove taskbar widgets",
            "TaskbarDa=0 remove o painel de widgets, que mantém processos e rede ativos mesmo fechado.",
            "TaskbarDa=0 removes the widgets panel, which keeps processes and network active even when closed.",
            KExplorerAdv, "TaskbarDa", 0);

        yield return Dword("menu.no-chat", TweakSection.Menu, TweakRisk.Low, TweakTags.None,
            "Remover ícone de chat/Teams",
            "Remove chat/Teams icon",
            "TaskbarMn=0 tira o botão de chat embutido que carrega o Teams em memória.",
            "TaskbarMn=0 removes the embedded chat button that loads Teams into memory.",
            KExplorerAdv, "TaskbarMn", 0);

        yield return Dword("menu.search-icon-only", TweakSection.Menu, TweakRisk.Low, TweakTags.None,
            "Barra de tarefas com busca discreta",
            "Minimal search on taskbar",
            "SearchboxTaskbarMode=1 deixa apenas o ícone de busca: a caixa completa renderiza conteúdo web a cada foco.",
            "SearchboxTaskbarMode=1 keeps only the search icon: the full box renders web content on every focus.",
            KSearchBox, "SearchboxTaskbarMode", 1);

        yield return Dword("menu.no-recent-docs", TweakSection.Menu, TweakRisk.Low, TweakTags.None,
            "Não guardar documentos recentes",
            "Do not track recent documents",
            "Start_TrackDocs=0 impede o registro de cada arquivo aberto: menos escrita em disco e mais privacidade.",
            "Start_TrackDocs=0 stops logging every file you open: less disk writes and more privacy.",
            KExplorerAdv, "Start_TrackDocs", 0);

        yield return Dword("menu.no-recent-programs", TweakSection.Menu, TweakRisk.Low, TweakTags.None,
            "Não registrar programas usados",
            "Do not track used programs",
            "Start_TrackProgs=0 desliga o monitor de programas do menu Iniciar, que indexa uso a cada execução.",
            "Start_TrackProgs=0 disables the Start menu program tracker, which indexes usage on every launch.",
            KExplorerAdv, "Start_TrackProgs", 0);

        yield return Dword("menu.explorer-this-pc", TweakSection.Menu, TweakRisk.Low, TweakTags.None,
            "Explorer abrindo em Este Computador",
            "Explorer opens in This PC",
            "LaunchTo=1 abre o Explorer direto em Este Computador em vez de 'Acesso rápido', que carrega histórico e recentes.",
            "LaunchTo=1 opens Explorer in This PC instead of 'Quick access', which loads history and recents.",
            KExplorerAdv, "LaunchTo", 1);

        yield return Dword("menu.visual-fx-custom", TweakSection.Menu, TweakRisk.Low, TweakTags.None,
            "Efeitos visuais definidos manualmente",
            "Visual effects defined manually",
            "VisualFXSetting=3 permite que as escolhas feitas em Propriedades do Sistema > Desempenho sejam respeitadas, em vez de 'deixar o Windows escolher'.",
            "VisualFXSetting=3 lets the choices made in System Properties > Performance be honored, instead of 'let Windows choose'.",
            KVisualFx, "VisualFXSetting", 3);

        yield return Dword("menu.no-app-suggestions", TweakSection.Menu, TweakRisk.Low, TweakTags.None,
            "Sem sugestões de apps no Iniciar",
            "No app suggestions in Start",
            "SilentInstalledAppsEnabled=0 e SystemPaneSuggestionsEnabled=0 bloqueiam instalações silenciosas de apps sugeridos.",
            "SilentInstalledAppsEnabled=0 and SystemPaneSuggestionsEnabled=0 block silent installs of suggested apps.",
            KContentDelivery, "SilentInstalledAppsEnabled", 0);

        yield return Dword("menu.no-lockscreen-spotlight", TweakSection.Menu, TweakRisk.Low, TweakTags.None,
            "Tela de bloqueio sem anúncios",
            "Lock screen without ads",
            "RotatingLockScreenOverlayEnabled=0 remove o texto e os links promocionais da tela de bloqueio do Windows 11.",
            "RotatingLockScreenOverlayEnabled=0 removes promo text and links from the Windows 11 lock screen.",
            KContentDelivery, "RotatingLockScreenOverlayEnabled", 0);

        yield return Dword("menu.taskbar-align-left", TweakSection.Menu, TweakRisk.Low, TweakTags.None,
            "Barra de tarefas alinhada à esquerda",
            "Taskbar aligned to the left",
            "TaskbarAl=0 devolve o menu Iniciar para a esquerda — preferência estética que também reduz a área de renderização da barra central.",
            "TaskbarAl=0 moves the Start menu back to the left — a visual preference that also shrinks the centered taskbar render area.",
            KExplorerAdv, "TaskbarAl", 0);
    }

    // =====================================================================
    // 3) PROCESSADOR (CPU)
    // =====================================================================
    private static IEnumerable<TweakEntry> BuildCpu()
    {
        yield return Dword("cpu.mmcss-gpu-priority", TweakSection.Cpu, TweakRisk.Medium, TweakTags.None,
            "MMCSS Games — GPU Priority = 2 (valor seguro)",
            "MMCSS Games — GPU Priority = 2 (safe value)",
            "Muitos guias mandam GPU Priority=8, o que quebra a alocação de filas e causa stutter. O valor validado do Kairo é 2, com Priority=2 e Scheduling Category=Medium.",
            "Many guides push GPU Priority=8, which breaks queue allocation and causes stutter. The validated Kairo value is 2, with Priority=2 and Scheduling Category=Medium.",
            KGameTask, "GPU Priority", 2);

        yield return Dword("cpu.mmcss-priority", TweakSection.Cpu, TweakRisk.Medium, TweakTags.None,
            "MMCSS Games — Priority = 2",
            "MMCSS Games — Priority = 2",
            "Priority=2 no perfil Games do MMCSS mantém a prioridade equilibrada; valores altos (6-8) monopolizam a CPU e travam áudio e input.",
            "Priority=2 in the MMCSS Games profile keeps priority balanced; high values (6-8) monopolize the CPU and stall audio and input.",
            KGameTask, "Priority", 2);

        yield return Str("cpu.mmcss-scheduling", TweakSection.Cpu, TweakRisk.Low, TweakTags.None,
            "MMCSS — Scheduling Category = Medium",
            "MMCSS — Scheduling Category = Medium",
            "SchedulingCategory='Medium' é a categoria recomendada para jogos. 'High' cria disputa com o driver de áudio e piora o frame time.",
            "SchedulingCategory='Medium' is the recommended category for games. 'High' fights the audio driver and worsens frame time.",
            KGameTask, "Scheduling Category", "Medium");

        yield return Del("cpu.mmcss-affinity-free", TweakSection.Cpu, TweakRisk.Low, TweakTags.None,
            "Liberar afinidade fixa da MMCSS (nunca fixar 0x0f)",
            "Free MMCSS CPU affinity (never lock 0x0f)",
            "Fixar Affinity=0x0f prende áudio e agendador de jogos em 4 núcleos fixos — erro clássico de tweaker que gera stutter em CPUs híbridas. Este tweak REMOVE a afinidade e deixa o Windows escolher.",
            "Locking Affinity=0x0f pins audio and the game scheduler to 4 fixed cores — a classic tweaker mistake that stutters hybrid CPUs. This tweak REMOVES the affinity and lets Windows choose.",
            KGameTask, "Affinity");

        yield return Dword("cpu.global-timer-resolution", TweakSection.Cpu, TweakRisk.Medium, TweakTags.None,
            "GlobalTimerResolutionRequests = 0",
            "GlobalTimerResolutionRequests = 0",
            "Deixar em 0 impede que qualquer processo force o timer global em 0,5 ms 24 h por dia, o que aumenta consumo e temperatura sem ganho em jogo.",
            "Leaving it at 0 prevents any process from forcing the global timer to 0.5 ms 24/7, which raises power and temperature with no in-game gain.",
            KKernel, "GlobalTimerResolutionRequests", 0);

        yield return Dword("cpu.system-responsiveness", TweakSection.Cpu, TweakRisk.Low, TweakTags.None,
            "SystemResponsiveness = 10 (padrão de jogos)",
            "SystemResponsiveness = 10 (gaming default)",
            "SystemResponsiveness=10 reserva 10% da CPU para tarefas de sistema em vez dos 20% padrão, liberando mais CPU para o jogo.",
            "SystemResponsiveness=10 reserves 10% of the CPU for system tasks instead of the default 20%, freeing more CPU for the game.",
            KSystemProfile, "SystemResponsiveness", 10);

        yield return Dword("cpu.network-throttling", TweakSection.Cpu, TweakRisk.Low, TweakTags.None,
            "NetworkThrottlingIndex liberado",
            "NetworkThrottlingIndex released",
            "NetworkThrottlingIndex=0xFFFFFFFF remove o limite de 10 pacotes/ms do receptor multimídia, que engasga jogos com muito tráfego UDP.",
            "NetworkThrottlingIndex=0xFFFFFFFF removes the 10 packets/ms multimedia receive limit, which chokes games with heavy UDP traffic.",
            KSystemProfile, "NetworkThrottlingIndex", -1);

        yield return T("cpu.core-parking-off", TweakSection.Cpu, TweakRisk.Medium,
            TweakTags.Laptop | TweakTags.Desktop | TweakTags.NoAuto,
            "Desativar Core Parking (núcleos sempre acordados)",
            "Disable Core Parking (cores always awake)",
            "CPMINCORES=100 impede que o Windows 'estacione' núcleos. Ganho de consistência em jogos, porém mais consumo e calor — em notebook só use ligado na tomada.",
            "CPMINCORES=100 stops Windows from parking cores. More consistent frame times, but higher power and heat — on laptops only use it plugged in.",
            () => new TweakStateInfo(
                TweakJournal.RunProcess("powercfg.exe", "/query SCHEME_CURRENT SUB_PROCESSOR CPMINCORES").Output.Contains("0x00000064"),
                Loc.Pick("CPMINCORES atual", "current CPMINCORES")),
            "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR CPMINCORES 100\npowercfg /setdcvalueindex SCHEME_CURRENT SUB_PROCESSOR CPMINCORES 100\npowercfg /setactive SCHEME_CURRENT",
            TweakOp.Cmd("powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR CPMINCORES 100; powercfg /setdcvalueindex SCHEME_CURRENT SUB_PROCESSOR CPMINCORES 100; powercfg /setactive SCHEME_CURRENT",
                        "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR CPMINCORES 0; powercfg /setdcvalueindex SCHEME_CURRENT SUB_PROCESSOR CPMINCORES 0; powercfg /setactive SCHEME_CURRENT"));

        yield return T("cpu.perf-boost-aggressive", TweakSection.Cpu, TweakRisk.Medium, TweakTags.None,
            "Performance Boost Mode = Agressivo",
            "Performance Boost Mode = Aggressive",
            "PERFBOOSTMODE=2 libera o boost máximo do processador na política de energia ativa, evitando quedas de clock no meio da partida.",
            "PERFBOOSTMODE=2 unlocks maximum CPU boost in the active power policy, avoiding mid-match clock drops.",
            () => new TweakStateInfo(
                TweakJournal.RunProcess("powercfg.exe", "/query SCHEME_CURRENT SUB_PROCESSOR PERFBOOSTMODE").Output.Contains("0x00000002"),
                Loc.Pick("PERFBOOSTMODE atual", "current PERFBOOSTMODE")),
            "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PERFBOOSTMODE 2\npowercfg /setactive SCHEME_CURRENT",
            TweakOp.Cmd("powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PERFBOOSTMODE 2; powercfg /setactive SCHEME_CURRENT",
                        "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PERFBOOSTMODE 0; powercfg /setactive SCHEME_CURRENT"));

        yield return T("cpu.min-state-5", TweakSection.Cpu, TweakRisk.Medium, TweakTags.NoAuto,
            "Estado mínimo do processador em 5%",
            "Minimum processor state at 5%",
            "PROCTHROTTLEMIN=5 evita o degrau de 100% constante (que esquenta e reduz o boost sustentado) mantendo pouca margem de reação.",
            "PROCTHROTTLEMIN=5 avoids a constant 100% floor (which heats up and lowers sustained boost) while keeping a small reaction margin.",
            () => new TweakStateInfo(
                TweakJournal.RunProcess("powercfg.exe", "/query SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMIN").Output.Contains("0x00000005"),
                Loc.Pick("PROCTHROTTLEMIN atual", "current PROCTHROTTLEMIN")),
            "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMIN 5\npowercfg /setactive SCHEME_CURRENT",
            TweakOp.Cmd("powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMIN 5; powercfg /setactive SCHEME_CURRENT",
                        "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMIN 100; powercfg /setactive SCHEME_CURRENT"));

        yield return T("cpu.sysmain-off", TweakSection.Cpu, TweakRisk.High, TweakTags.Ssd | TweakTags.NoAuto | TweakTags.RequiresReboot,
            "Desativar SysMain/Superfetch (SOMENTE SSD)",
            "Disable SysMain/Superfetch (SSD ONLY)",
            "Em SSD o SysMain às vezes disputa I/O em máquinas com pouca RAM. NUNCA desative em HDD: o acervo Kairo marca isso como erro grave que deixa o sistema lento.",
            "On SSD SysMain sometimes fights for I/O on low-RAM machines. NEVER disable it on HDD: the Kairo collection flags that as a serious mistake that slows the system down.",
            () => CheckService("SysMain", 4), "sc config SysMain start= disabled",
            TweakOp.Service("SysMain", 4));

        yield return T("cpu.search-index-off", TweakSection.Cpu, TweakRisk.High, TweakTags.Ssd | TweakTags.NoAuto,
            "Desativar indexação do Windows Search",
            "Disable Windows Search indexing",
            "WSearch.exe indexa arquivos constantemente e disputa disco e CPU. Só desative se você não usa a busca do menu Iniciar para arquivos.",
            "WSearch.exe constantly indexes files and competes for disk and CPU. Only disable it if you never use Start menu file search.",
            () => CheckService("WSearch", 4), "sc config WSearch start= disabled",
            TweakOp.Service("WSearch", 4));

        yield return T("cpu.spectre-mitigations", TweakSection.Cpu, TweakRisk.Critical, TweakTags.NoAuto | TweakTags.RequiresReboot,
            "Desativar mitigações Spectre/Meltdown",
            "Disable Spectre/Meltdown mitigations",
            "Ganho real de 5-15% em CPUs antigas, mas abre a máquina para vulnerabilidades de execução especulativa. CRÍTICO: só para bancada de teste, nunca para uso diário.",
            "Real 5-15% gain on older CPUs, but exposes the machine to speculative execution vulnerabilities. CRITICAL: test bench only, never daily use.",
            () => CheckReg(KMemMgmt, "FeatureSettingsOverride", 3),
            "reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\" /v FeatureSettingsOverride /t REG_DWORD /d 3 /f\nreg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\" /v FeatureSettingsOverrideMask /t REG_DWORD /d 3 /f",
            TweakOp.RegDword(KMemMgmt, "FeatureSettingsOverride", 3),
            TweakOp.RegDword(KMemMgmt, "FeatureSettingsOverrideMask", 3));

        yield return Bcd("cpu.dynamictick", TweakSection.Cpu, TweakRisk.High, TweakTags.NoAuto | TweakTags.RequiresReboot,
            "Desativar tick dinâmico do kernel",
            "Disable kernel dynamic tick",
            "disabledynamictick=yes dá timer 100% estável, mas aumenta consumo em idle e pode causar drift em notebooks. Alto risco: desmarcado no modo automático.",
            "disabledynamictick=yes gives a rock-stable timer, but raises idle power and may cause drift on laptops. High risk: unchecked in auto mode.",
            "disabledynamictick", "yes");

        yield return Bcd("cpu.tsc-sync", TweakSection.Cpu, TweakRisk.Medium, TweakTags.HighRam,
            "Sincronização de TSC aprimorada",
            "Enhanced TSC sync policy",
            "tscsyncpolicy=Enhanced melhora a consistência de timestamps entre núcleos — ajuda em jogos sensíveis a timing e ferramentas de captura.",
            "tscsyncpolicy=Enhanced improves cross-core timestamp consistency — helps timing-sensitive games and capture tools.",
            "tscsyncpolicy", "Enhanced");

        yield return Bcd("cpu.hpet-forced", TweakSection.Cpu, TweakRisk.High, TweakTags.NoAuto | TweakTags.RequiresReboot,
            "Forçar useplatformclock (HPET)",
            "Force useplatformclock (HPET)",
            "Forçar HPET no boot AUMENTA a latência de timer em CPUs modernas e piora o input lag. Está no catálogo por ser mito popular — alto risco, nunca automático.",
            "Forcing HPET at boot INCREASES timer latency on modern CPUs and worsens input lag. Listed because it is a popular myth — high risk, never automatic.",
            "useplatformclock", "true");

        yield return Dword("cpu.priority-games-high", TweakSection.Cpu, TweakRisk.Low, TweakTags.None,
            "Prioridade de CPU: jogos em High (nunca RealTime)",
            "CPU priority: games at High (never RealTime)",
            "CpuPriorityClass=3 dá prioridade alta ao executável do jogo. RealTime (4) é bloqueado: congela mouse e áudio. Nunca aplique prioridade em lsass/svchost — isso é bug do iGust.",
            "CpuPriorityClass=3 gives high priority to the game executable. RealTime (4) is blocked: it freezes mouse and audio. Never set priority on lsass/svchost — that is an iGust bug.",
            @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\<jogo>.exe\PerfOptions", "CpuPriorityClass", 3);

        yield return T("cpu.win32-quantum", TweakSection.Cpu, TweakRisk.Medium, TweakTags.None,
            "Quantum de CPU curto para primeiro plano",
            "Short CPU quantum for foreground",
            "Win32PrioritySeparation=38 já aplicado como padrão do app. Este card reforça o valor em HKLM\\Control\\PriorityControl para todos os usuários da máquina.",
            "The app default is already 38. This card enforces the value machine-wide under HKLM\\Control\\PriorityControl for every user.",
            () => CheckReg(@"HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 38), "",
            TweakOp.RegDword(@"HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 38));
    }

    // =====================================================================
    // 4) PLACA DE VÍDEO (GPU)
    // =====================================================================
    private static IEnumerable<TweakEntry> BuildGpu()
    {
        yield return Dword("gpu.hags", TweakSection.Gpu, TweakRisk.Medium, TweakTags.RequiresReboot,
            "Agendamento de GPU acelerado por hardware (HAGS)",
            "Hardware-accelerated GPU scheduling (HAGS)",
            "HwSchMode=2 ativa o HAGS, que encurta a fila de submissão da GPU. Ganho em jogos com baixo overhead de driver; em GPUs antigas pode custar FPS.",
            "HwSchMode=2 enables HAGS, which shortens the GPU submission queue. Gains in low-overhead games; on old GPUs it may cost FPS.",
            KGraphicsDrivers, "HwSchMode", 2);

        yield return Dword("gpu.swap-effect-upgrade", TweakSection.Gpu, TweakRisk.Medium, TweakTags.None,
            "SwapEffectUpgradeEnable = 1",
            "SwapEffectUpgradeEnable = 1",
            "Permite que o Windows promova a swap chain do jogo para flip model quando o jogo pede, reduzindo cópias de framebuffer. Valor validado do acervo Kairo.",
            "Lets Windows promote the game swap chain to flip model when the game asks, cutting framebuffer copies. Validated Kairo value.",
            KDwm, "SwapEffectUpgradeEnable", 1);

        yield return Dword("gpu.gamedvr-off", TweakSection.Gpu, TweakRisk.Low, TweakTags.None,
            "Desativar Game DVR (gravação em segundo plano)",
            "Disable Game DVR (background recording)",
            "GameDVR_Enabled=0 desliga a gravação automática do Xbox Game Bar, que mantém um encoder ativo consumindo GPU e disco.",
            "GameDVR_Enabled=0 turns off Xbox Game Bar automatic recording, which keeps an encoder active burning GPU and disk.",
            KGameConfigStore, "GameDVR_Enabled", 0);

        yield return T("gpu.fse-behavior", TweakSection.Gpu, TweakRisk.Low, TweakTags.None,
            "Tela cheia exclusiva otimizada",
            "Optimized exclusive fullscreen mode",
            "GameDVR_FSEBehaviorMode=2 e FSEBehavior=1 evitam que o Windows force a otimização de tela cheia em cima do jogo, reduzindo o stutter de alt-tab.",
            "GameDVR_FSEBehaviorMode=2 and FSEBehavior=1 stop Windows from forcing fullscreen optimizations over the game, reducing alt-tab stutter.",
            () => CheckReg(KGameConfigStore, "GameDVR_FSEBehaviorMode", 2), "",
            TweakOp.RegDword(KGameConfigStore, "GameDVR_FSEBehaviorMode", 2),
            TweakOp.RegDword(KGameConfigStore, "GameDVR_FSEBehavior", 1));

        yield return T("gpu.game-mode", TweakSection.Gpu, TweakRisk.Low, TweakTags.None,
            "Modo de Jogo do Windows ativado",
            "Windows Game Mode enabled",
            "AutoGameModeEnabled=1 prioriza a thread do jogo em foco e suspende tarefas de manutenção do Windows durante a partida.",
            "AutoGameModeEnabled=1 prioritizes the focused game thread and suspends Windows maintenance tasks during a match.",
            () => CheckReg(KGameBar, "AutoGameModeEnabled", 1), "",
            TweakOp.RegDword(KGameBar, "AutoGameModeEnabled", 1),
            TweakOp.RegDword(KGameBar, "AllowAutoGameMode", 1));

        yield return Dword("gpu.disable-mpo", TweakSection.Gpu, TweakRisk.Medium, TweakTags.None,
            "Desativar MPO (Multiplane Overlay)",
            "Disable MPO (Multiplane Overlay)",
            "OverlayTestMode=5 é a correção documentada pela Microsoft para flicker, tela piscando e queda de FPS em janela com dois monitores.",
            "OverlayTestMode=5 is Microsoft's documented workaround for flicker, blinking and FPS drops with two monitors in windowed mode.",
            KDwm, "OverlayTestMode", 5);

        yield return Dword("gpu.tdr-delay", TweakSection.Gpu, TweakRisk.Medium, TweakTags.None,
            "Aumentar TdrDelay para 10 s (evita TDR falso)",
            "Raise TdrDelay to 10 s (avoids false TDR)",
            "TdrDelay=10 dá à GPU 10 segundos para responder em vez de 2. Evita o erro 'o driver parou de responder' em overclock leve e compilação pesada de shader. NÃO confunda com TdrLevel=0 (bloqueado).",
            "TdrDelay=10 gives the GPU 10 seconds to respond instead of 2. Avoids 'display driver stopped responding' on mild overclocks and heavy shader compiles. Do NOT confuse with TdrLevel=0 (blocked).",
            KGraphicsDrivers, "TdrDelay", 10);

        yield return Dword("gpu.shader-cache", TweakSection.Gpu, TweakRisk.Low, TweakTags.None,
            "Cache de shader do DirectX habilitado",
            "DirectX shader cache enabled",
            "ShaderCacheEnabled=1 permite que drivers guardem shaders compilados: elimina o engasgo de primeira partida em jogos como Warzone e Fortnite.",
            "ShaderCacheEnabled=1 lets drivers keep compiled shaders: removes first-match stutter in games like Warzone and Fortnite.",
            KShaderCache, "ShaderCacheEnabled", 1);

        yield return Dword("gpu.preemption", TweakSection.Gpu, TweakRisk.Medium, TweakTags.None,
            "Preempção de GPU em modo compute",
            "GPU preemption at compute mode",
            "EnablePreemption=1 ajusta o tempo de preempção entre processos de GPU, melhorando a estabilidade do frame time quando há overlay ativo.",
            "EnablePreemption=1 tunes GPU preemption timing between processes, improving frame time stability when an overlay is active.",
            KGraphicsDrivers, "EnablePreemption", 1);

        yield return T("gpu.nvidia-telemetry", TweakSection.Gpu, TweakRisk.Low, TweakTags.Nvidia,
            "Desativar telemetria NVIDIA (NvTelemetryContainer)",
            "Disable NVIDIA telemetry (NvTelemetryContainer)",
            "O serviço NvTelemetryContainer envia estatísticas de uso. Desativar reduz processos em segundo plano sem mexer em driver nem desempenho.",
            "The NvTelemetryContainer service sends usage stats. Disabling cuts background processes without touching the driver or performance.",
            () => CheckService("NvTelemetryContainer", 4), "sc config NvTelemetryContainer start= disabled",
            TweakOp.Service("NvTelemetryContainer", 4));

        yield return Dword("gpu.nvidia-pstate", TweakSection.Gpu, TweakRisk.Medium, TweakTags.Nvidia | TweakTags.Laptop,
            "NVIDIA: desativar Dynamic P-State em notebooks",
            "NVIDIA: disable Dynamic P-State on laptops",
            "DisableDynamicPstate=1 mantém a GPU em P0 sob carga, evitando a oscilação de clock que vira stutter em notebooks gamer.",
            "DisableDynamicPstate=1 keeps the GPU at P0 under load, avoiding the clock oscillation that becomes stutter on gaming laptops.",
            KClassGpu, "DisableDynamicPstate", 1);

        yield return Dword("gpu.amd-ulps", TweakSection.Gpu, TweakRisk.Medium, TweakTags.AmdGpu,
            "AMD: desativar ULPS (Ultra Low Power State)",
            "AMD: disable ULPS (Ultra Low Power State)",
            "EnableUlps=0 impede a GPU AMD de entrar em sono profundo, que em multi-monitor causa flicker e clocks presos em idle.",
            "EnableUlps=0 stops the AMD GPU from deep sleep, which on multi-monitor causes flicker and stuck idle clocks.",
            KClassGpu, "EnableUlps", 0);

        yield return T("gpu.vrr", TweakSection.Gpu, TweakRisk.Low, TweakTags.None,
            "Variable Refresh Rate do Windows",
            "Windows Variable Refresh Rate",
            "VRROptimizeEnable=1 permite que o Windows respeite FreeSync/G-Sync em jogos flip model, evitando tearing sem o custo do VSync.",
            "VRROptimizeEnable=1 lets Windows honor FreeSync/G-Sync in flip-model games, avoiding tearing without the VSync cost.",
            () => new TweakStateInfo(
                (TweakJournal.ReadReg(KDirectXUser, "VRROptimizeEnable")?.ToString() ?? "").Contains("1"),
                Loc.Pick("VRR global", "Global VRR")),
            "reg add \"HKCU\\Software\\Microsoft\\DirectX\\UserGpuPreferences\" /v VRROptimizeEnable /t REG_SZ /d \"1;\" /f",
            TweakOp.RegString(KDirectXUser, "VRROptimizeEnable", "1;"));

        yield return T("gpu.global-high-perf", TweakSection.Gpu, TweakRisk.Low, TweakTags.Laptop,
            "Preferência global de GPU: alto desempenho",
            "Global GPU preference: high performance",
            "GpuPreference=2 força o Windows a escolher a GPU dedicada em vez da integrada em qualquer app, inclusive nos jogos que não declaram preferência.",
            "GpuPreference=2 forces Windows to pick the dedicated GPU over the integrated one in every app, including games that declare no preference.",
            () => new TweakStateInfo(
                (TweakJournal.ReadReg(KDirectXUser, "DirectXUserGlobalSettings")?.ToString() ?? "").Contains("GpuPreference=2"),
                Loc.Pick("Preferência global", "Global preference")),
            "reg add \"HKCU\\Software\\Microsoft\\DirectX\\UserGpuPreferences\" /v DirectXUserGlobalSettings /t REG_SZ /d GpuPreference=2; /f",
            TweakOp.RegString(KDirectXUser, "DirectXUserGlobalSettings", "GpuPreference=2;"));

        yield return Dword("gpu.no-color-override", TweakSection.Gpu, TweakRisk.Low, TweakTags.None,
            "Sem overlay de cor forçado",
            "No forced color overlay",
            "Desliga o overlay de cor automático do DWM, que em monitores SDR causa imagem lavada e custo extra de composição.",
            "Disables the DWM automatic color overlay, which on SDR monitors causes washed-out images and extra composition cost.",
            KDwm, "ColorProfileOverride", 0);

        yield return Dword("gpu.intel-gpu-policy", TweakSection.Gpu, TweakRisk.Low, TweakTags.IntelGpu,
            "Intel: política de energia do driver em desempenho",
            "Intel: driver power policy at performance",
            "PowerMizerEnable mantém o driver Intel Arc/Iris em modo desempenho quando há jogo em primeiro plano. A instalação do driver oficial fica na aba Drivers.",
            "PowerMizerEnable keeps the Intel Arc/Iris driver in performance mode while a game is in the foreground. Official driver install lives in the Drivers page.",
            KClassGpu, "PowerMizerEnable", 1);
    }
}
