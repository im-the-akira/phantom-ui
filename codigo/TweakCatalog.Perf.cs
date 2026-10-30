using Microsoft.Win32;

namespace PhantomUI;

public static partial class TweakCatalog
{
    private const string KGpuMem = @"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers";
    private const string KSvcHost = @"HKLM\SYSTEM\CurrentControlSet\Control";
    private const string KHdSvc = @"HKLM\SYSTEM\CurrentControlSet\Services";
    private const string KNetIface = @"HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";
    private const string KMsTcp = @"HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters";
    private const string KNsi = @"HKLM\SYSTEM\CurrentControlSet\Services\Nsi";
    private const string KPsched = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Psched";
    private const string KMouseClass = @"HKLM\SYSTEM\CurrentControlSet\Services\mouclass\Parameters";
    private const string KKeyboardClass = @"HKLM\SYSTEM\CurrentControlSet\Services\kbdclass\Parameters";
    private const string KUsbPower = @"HKLM\SYSTEM\CurrentControlSet\Enum\USB";
    private const string KPowerCpl = @"HKLM\SYSTEM\CurrentControlSet\Control\Power";
    private const string KMmcssProfile = @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games";

    // =====================================================================
    // 5) MEMÓRIA RAM
    // =====================================================================
    private static IEnumerable<TweakEntry> BuildRam()
    {
        yield return Dword("ram.clear-pagefile", TweakSection.Ram, TweakRisk.Medium, TweakTags.None,
            "Limpar pagefile no desligamento (ClearPageFileAtShutdown = 0)",
            "Do not clear pagefile at shutdown (ClearPageFileAtShutdown = 0)",
            "Deixar em 0 evita reescrever gigabytes de pagefile a cada desligamento — desligar e iniciar ficam muito mais rápidos. Trade-off: quem tem dados sensíveis na swap prefere 1.",
            "Leaving it 0 avoids rewriting gigabytes of pagefile on every shutdown — shutdown and boot get much faster. Trade-off: users with sensitive data in swap prefer 1.",
            KMemMgmt, "ClearPageFileAtShutdown", 0);

        yield return Dword("ram.paging-executive", TweakSection.Ram, TweakRisk.Medium, TweakTags.HighRam | TweakTags.RequiresReboot,
            "Núcleo do sistema sempre na RAM",
            "System kernel always in RAM",
            "DisablePagingExecutive=1 mantém código do kernel e drivers em memória física, cortando page faults em jogos com muitos threads.",
            "DisablePagingExecutive=1 keeps kernel and driver code in physical memory, cutting page faults in heavily threaded games.",
            KMemMgmt, "DisablePagingExecutive", 1);

        yield return T("ram.compression-on", TweakSection.Ram, TweakRisk.Low, TweakTags.LowRam,
            "Compactação de memória ativa (8-12 GB)",
            "Memory compression active (8-12 GB)",
            "Em máquinas com 8 ou 12 GB, desligar a compactação (mito de tweaker) faz o Windows ir para o disco e travar. Este card garante ENABLE via MMAgent.",
            "On 8 or 12 GB machines, disabling compression (a tweaker myth) pushes Windows to disk and freezes it. This card guarantees ENABLE through MMAgent.",
            () => new TweakStateInfo(
                TweakJournal.RunProcess("powershell.exe", "-NoProfile -Command \"(Get-MMAgent).MemoryCompression\"").Output.Trim().StartsWith("True", StringComparison.OrdinalIgnoreCase),
                Loc.Pick("Compactação de memória", "Memory compression")),
            "powershell -Command \"Enable-MMAgent -MemoryCompression\"",
            TweakOp.Cmd("Enable-MMAgent -MemoryCompression", "Disable-MMAgent -MemoryCompression"));

        yield return Dword("ram.superfetch-hdd", TweakSection.Ram, TweakRisk.Low, TweakTags.Hdd,
            "Superfetch obrigatório em HDD",
            "Superfetch required on HDD",
            "EnableSuperfetch=3 em disco mecânico é obrigatório: sem ele cada abertura de jogo vira espera de 30 s no disco. Nunca desative em HDD.",
            "EnableSuperfetch=3 on a mechanical disk is mandatory: without it every game launch becomes a 30 s disk wait. Never disable it on HDD.",
            KPrefetch, "EnableSuperfetch", 3);

        yield return Dword("ram.prefetch-params", TweakSection.Ram, TweakRisk.Low, TweakTags.None,
            "Prefetch completo (apps + boot)",
            "Full Prefetch (apps + boot)",
            "EnablePrefetcher=3 e EnableBootTrace=1 deixam o Windows pré-carregar apps e o próprio boot com base no histórico de uso.",
            "EnablePrefetcher=3 and EnableBootTrace=1 let Windows preload apps and the boot itself based on usage history.",
            KPrefetch, "EnablePrefetcher", 3);

        yield return TweakEntry_RamPurge();

        yield return TweakEntry_StandbyList();

        yield return Dword("ram.nonpaged-pool", TweakSection.Ram, TweakRisk.High, TweakTags.HighRam | TweakTags.NoAuto,
            "Aumentar NonPagedPoolSize (64 MB)",
            "Increase NonPagedPoolSize (64 MB)",
            "NonPagedPoolSize só deve ser tocado em servidores com muitos drivers de rede. Em desktop pode causar instabilidade — alto risco, fora do automático.",
            "NonPagedPoolSize should only be touched on servers with many network drivers. On desktops it may cause instability — high risk, excluded from auto.",
            KMemMgmt, "NonPagedPoolSize", 0x04000000);

        yield return Dword("ram.session-pool", TweakSection.Ram, TweakRisk.Medium, TweakTags.HighRam,
            "Pool de sessão maior (pool de 64 bits)",
            "Larger session pool (64-bit pool)",
            "SessionPoolSize maior ajuda em sessões com muitos handles abertos (streaming + jogo + navegador com 50 abas).",
            "A larger SessionPoolSize helps sessions with many open handles (streaming + game + browser with 50 tabs).",
            KMemMgmt, "SessionPoolSize", 0);

        yield return Dword("ram.dwm-memory", TweakSection.Ram, TweakRisk.Low, TweakTags.None,
            "DWM com cache de memória controlado",
            "DWM with controlled memory cache",
            "OverrideMonitorForSystemGraphics=0 impede o DWM de reservar superfícies extras de memória em monitores 4K, liberando VRAM e RAM.",
            "OverrideMonitorForSystemGraphics=0 stops DWM from reserving extra memory surfaces on 4K monitors, freeing VRAM and RAM.",
            KDwm, "OverrideMonitorForSystemGraphics", 0);

        yield return TweakEntry_MemoryIntegrity();

        yield return Dword("ram.large-system-cache-off", TweakSection.Ram, TweakRisk.Medium, TweakTags.None,
            "LargeSystemCache desligado (RAM para jogos)",
            "LargeSystemCache off (RAM for games)",
            "Com LargeSystemCache=0 o Windows não rouba 40% da RAM para cache de arquivos, deixando mais memória disponível para texturas do jogo.",
            "With LargeSystemCache=0 Windows does not take 40% of RAM for file cache, leaving more memory available for game textures.",
            KMemMgmt, "LargeSystemCache", 0);

        yield return Dword("ram.paged-pool", TweakSection.Ram, TweakRisk.High, TweakTags.NoAuto,
            "Tamanho explícito do PagedPool",
            "Explicit PagedPool size",
            "PagedPoolSize explícito raramente ajuda e pode limitar o sistema. Presente no catálogo apenas para transparência completa do acervo — não entra no automático.",
            "An explicit PagedPoolSize rarely helps and can throttle the system. Listed only for full transparency of the collection — never applied automatically.",
            KMemMgmt, "PagedPoolSize", 0);

        yield return TweakEntry_AutoPurgeToggle();
    }

    private static TweakEntry TweakEntry_RamPurge() => T(
        "ram.purge-now", TweakSection.Ram, TweakRisk.Low, TweakTags.NoAuto,
        "Limpar memória em espera agora (standby list)",
        "Purge standby memory now",
        "Esvazia a lista de espera (cached) e a working set dos processos escolhidos, liberando RAM na hora. O PHANTOM UI já faz isso automaticamente quando a RAM livre cai abaixo do limite configurado.",
        "Empties the standby list and the working set of chosen processes, freeing RAM right away. PHANTOM UI already does this automatically when free RAM drops below your configured threshold.",
        () => new TweakStateInfo(false, Loc.Pick("Ação imediata (sem estado)", "Immediate action (no state)")),
        "powershell -Command \"Clear-RecycleBin -Force\"  # + EmptyWorkingSet via API nativa",
        TweakOp.Cmd("(void)0", "(void)0"));

    private static TweakEntry TweakEntry_StandbyList() => T(
        "ram.standby-flush", TweakSection.Ram, TweakRisk.Medium, TweakTags.NoAuto | TweakTags.HighRam,
        "Liberar lista de espera antes do jogo",
        "Flush standby list before gaming",
        "Antes de abrir um jogo pesado, o standby cache pode estar com 8 GB de arquivos antigos. O PHANTOM UI oferece o botão de limpeza; o acervo Kairo executa isso no início de sessão.",
        "Before launching a heavy game the standby cache may hold 8 GB of stale files. PHANTOM UI offers the purge button; the Kairo collection runs it at session start.",
        () => new TweakStateInfo(false, Loc.Pick("Ação imediata (sem estado)", "Immediate action (no state)")),
        "NtSetSystemInformation(SystemMemoryListInformation, MemoryPurgeStandbyList)",
        TweakOp.Cmd("(void)0", "(void)0"));

    private static TweakEntry TweakEntry_AutoPurgeToggle() => T(
        "ram.auto-purge", TweakSection.Ram, TweakRisk.Low, TweakTags.None,
        "Limpeza automática de RAM em segundo plano",
        "Automatic background RAM purge",
        "Quando a RAM livre cai abaixo do limite definido nas configurações, o PHANTOM UI libera a standby list e o working set de processos não-críticos. Nunca toca em lsass, csrss, wininit ou antich eat.",
        "When free RAM drops below the configured threshold, PHANTOM UI frees the standby list and the working set of non-critical processes. It never touches lsass, csrss, wininit or anti-cheats.",
        () => new TweakStateInfo(SettingsInstance.AutoRamPurge,
            SettingsInstance.AutoRamPurge ? Loc.Pick($"Ativo (limite {SettingsInstance.AutoRamFreeThresholdMb} MB)", $"Active (threshold {SettingsInstance.AutoRamFreeThresholdMb} MB)")
                                          : Loc.Pick("Desativado", "Disabled")),
        "Interno: loop de 5 s monitorando RAM livre (sem comando externo)",
        TweakOp.Cmd("", ""));

    private static TweakEntry TweakEntry_MemoryIntegrity() => T(
        "ram.integrity-policy", TweakSection.Ram, TweakRisk.Medium, TweakTags.NoAuto,
        "Modo de integridade de memória (HVCI)",
        "Memory integrity mode (HVCI)",
        "Desligar HVCI (Memory Integrity) dá de 1 a 5% de FPS em jogos CPU-bound, mas abre o kernel para drivers maliciosos e QUEBRA o Valorant/Vanguard. Mantido como opção manual com aviso.",
        "Turning HVCI (Memory Integrity) off gives 1-5% FPS in CPU-bound games, but exposes the kernel to malicious drivers and BREAKS Valorant/Vanguard. Kept as a manual option with a warning.",
        () => CheckReg(@"HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", 0),
        "reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity\" /v Enabled /t REG_DWORD /d 0 /f",
        TweakOp.RegDword(@"HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", 0));

    // =====================================================================
    // 6) ARMAZENAMENTO & SSD
    // =====================================================================
    private static IEnumerable<TweakEntry> BuildDisk()
    {
        yield return T("disk.trim", TweakSection.Disk, TweakRisk.Low, TweakTags.Ssd,
            "Garantir TRIM habilitado no SSD",
            "Ensure TRIM is enabled on SSD",
            "DisableDeleteNotify=0 mantém o TRIM ativo, que é o que mantém a velocidade de escrita do SSD ao longo do tempo. Desativar é dano permanente de desempenho.",
            "DisableDeleteNotify=0 keeps TRIM active, which is what preserves SSD write speed over time. Disabling it does permanent performance damage.",
            () => new TweakStateInfo(
                !TweakJournal.RunProcess("fsutil.exe", "behavior query DisableDeleteNotify").Output.Contains("= 1"),
                Loc.Pick("Estado do TRIM", "TRIM state")),
            "fsutil behavior set DisableDeleteNotify 0",
            TweakOp.Cmd("fsutil behavior set DisableDeleteNotify 0", "fsutil behavior set DisableDeleteNotify 1"));

        yield return T("disk.optimize-ssd", TweakSection.Disk, TweakRisk.Low, TweakTags.Ssd,
            "Executar Retrim / otimização de SSD agora",
            "Run Retrim / SSD optimization now",
            "Executa o retrim do volume do sistema, devolvendo blocos não usados ao controlador. Rode uma vez por mês e depois de apagar muitos jogos.",
            "Runs a volume retrim, returning unused blocks to the controller. Run it once a month and after deleting many games.",
            () => new TweakStateInfo(false, Loc.Pick("Ação manual", "Manual action")),
            "defrag C: /L",
            TweakOp.Cmd("defrag C: /L", ""));

        yield return Dword("disk.no-last-access", TweakSection.Disk, TweakRisk.Low, TweakTags.None,
            "Sem atualização de último acesso",
            "No last access update",
            "NtfsDisableLastAccessUpdate=1 remove uma operação de escrita de metadados a cada arquivo lido — ganho direto em tempo de carregamento de mapas.",
            "NtfsDisableLastAccessUpdate=1 removes a metadata write on every file read — a direct win in map load times.",
            KFilesystem, "NtfsDisableLastAccessUpdate", 1);

        yield return Dword("disk.ntfs-memory-2", TweakSection.Disk, TweakRisk.Low, TweakTags.HighRam,
            "Cache de metadados NTFS alto",
            "High NTFS metadata cache",
            "NtfsMemoryUsage=2 amplia o cache de metadados; com 16 GB+ evita reabrir o índice de pastas grandes de jogos.",
            "NtfsMemoryUsage=2 expands the metadata cache; with 16 GB+ it avoids reopening the index of large game folders.",
            KFilesystem, "NtfsMemoryUsage", 2);

        yield return T("disk.device-write-cache", TweakSection.Disk, TweakRisk.Medium, TweakTags.Ssd,
            "Cache de escrita do dispositivo ativo",
            "Device write cache enabled",
            "Habilita o cache de escrita do disco (padrão em SSD NVMe). Ganho de desempenho; em queda de energia há risco de perda dos últimos dados não confirmados.",
            "Enables the disk write cache (default on NVMe). Performance gain; on power loss there is a risk of losing the last unacknowledged data.",
            () => new TweakStateInfo(true, Loc.Pick("Padrão do dispositivo", "Device default")),
            "Get-Disk | Set-Disk -IsWriteCacheEnabled $true",
            TweakOp.Cmd("Get-Disk | Set-Disk -IsWriteCacheEnabled $true", "Get-Disk | Set-Disk -IsWriteCacheEnabled $false"));

        yield return T("disk.shader-cache-purge", TweakSection.Disk, TweakRisk.Low, TweakTags.None,
            "Limpar cache de shader corrompido",
            "Clear corrupted shader cache",
            "Apaga DirectX Shader Cache, NVIDIA DXCache/GLCache e o cache AMD. É a correção padrão para texturas pretas, crash de shader e stutter que não passa.",
            "Deletes DirectX Shader Cache, NVIDIA DXCache/GLCache and the AMD cache. The standard fix for black textures, shader crashes and stutter that will not go away.",
            () => new TweakStateInfo(false, Loc.Pick("Ação manual", "Manual action")),
            "del /f /q \"%LOCALAPPDATA%\\D3DSCache\\*\" \"%LOCALAPPDATA%\\NVIDIA\\DXCache\\*\" \"%LOCALAPPDATA%\\AMD\\DxCache\\*\"",
            TweakOp.Cmd("Remove-Item -Recurse -Force \"$env:LOCALAPPDATA\\D3DSCache\\*\",\"$env:LOCALAPPDATA\\NVIDIA\\DXCache\\*\",\"$env:LOCALAPPDATA\\AMD\\DxCache\\*\" -ErrorAction SilentlyContinue", ""));

        yield return T("disk.temp-clean-prefetch-safe", TweakSection.Disk, TweakRisk.Low, TweakTags.None,
            "Limpeza de temporários PRESERVANDO Prefetch",
            "Temp cleanup PRESERVING Prefetch",
            "Remove %TEMP%, Windows\\Temp, logs de CBS e relatórios de erro antigos, mas mantém C:\\Windows\\Prefetch intacto (apagar Prefetch é mito que piora o boot).",
            "Removes %TEMP%, Windows\\Temp, CBS logs and old error reports, but keeps C:\\Windows\\Prefetch intact (deleting Prefetch is a myth that worsens boot).",
            () => new TweakStateInfo(false, Loc.Pick("Ação manual", "Manual action")),
            "Remove-Item \"$env:TEMP\\*\",\"C:\\Windows\\Temp\\*\",\"C:\\Windows\\Logs\\CBS\\*\" -Recurse -Force -ErrorAction SilentlyContinue",
            TweakOp.Cmd("Remove-Item \"$env:TEMP\\*\",\"C:\\Windows\\Temp\\*\",\"C:\\Windows\\Logs\\CBS\\*\" -Recurse -Force -ErrorAction SilentlyContinue", ""));

        yield return T("disk.hibernate-off", TweakSection.Disk, TweakRisk.Medium, TweakTags.Ssd,
            "Desativar hibernação (libera hiberfil.sys)",
            "Disable hibernation (frees hiberfil.sys)",
            "powercfg /h off libera de 9 a 48 GB do SSD ocupados pelo hiberfil.sys. Trade-off: perde hibernação e Inicialização Rápida (que aliás causa boot sujo).",
            "powercfg /h off frees 9 to 48 GB of SSD taken by hiberfil.sys. Trade-off: you lose hibernation and Fast Startup (which actually causes dirty boots).",
            () => new TweakStateInfo(
                !System.IO.File.Exists(@"C:\hiberfil.sys"),
                Loc.Pick("hiberfil.sys existe?", "hiberfil.sys present?")),
            "powercfg /h off",
            TweakOp.Cmd("powercfg /h off", "powercfg /h on"));

        yield return T("disk.fast-startup-off", TweakSection.Disk, TweakRisk.Low, TweakTags.Ssd,
            "Desativar Inicialização Rápida",
            "Disable Fast Startup",
            "HiberbootEnabled=0 força desligamento real. Corrige drivers presos, boot sujo, perda de configuração de RGB e problemas de dual boot.",
            "HiberbootEnabled=0 forces a real shutdown. Fixes stuck drivers, dirty boot, lost RGB settings and dual boot problems.",
            () => CheckReg(@"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", 0),
            "reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Power\" /v HiberbootEnabled /t REG_DWORD /d 0 /f",
            TweakOp.RegDword(@"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", 0));

        yield return Dword("disk.superfetch-hdd", TweakSection.Disk, TweakRisk.Low, TweakTags.Hdd,
            "Superfetch ligado em disco mecânico",
            "Superfetch on for mechanical disks",
            "Em HDD, EnableSuperfetch=3 é o que evita espera de 30 s ao abrir jogos grandes. Desativar em HDD é um dos erros mais comuns dos pacotes de tweak.",
            "On HDD, EnableSuperfetch=3 is what avoids 30 s waits when opening big games. Disabling it on HDD is one of the most common tweak-pack mistakes.",
            KPrefetch, "EnableSuperfetch", 3);

        yield return T("disk.chkdsk-dirty", TweakSection.Disk, TweakRisk.Medium, TweakTags.NoAuto,
            "Verificar e limpar bit sujo do volume",
            "Check and clear volume dirty bit",
            "Consulta o bit sujo (fsutil dirty query) e agenda um chkdsk /spotfix no próximo boot quando necessário. Use o Centro de Reparos para a versão completa.",
            "Queries the dirty bit (fsutil dirty query) and schedules chkdsk /spotfix on next boot when needed. Use the Repair Center for the full version.",
            () => new TweakStateInfo(false, Loc.Pick("Ação manual", "Manual action")),
            "fsutil dirty query C:  /  chkdsk C: /spotfix",
            TweakOp.Cmd("chkdsk C: /spotfix", ""));

        yield return Dword("disk.no-8dot3", TweakSection.Disk, TweakRisk.Medium, TweakTags.None,
            "Desativar nomes curtos 8.3",
            "Disable 8.3 short names",
            "Acelera criação de milhares de arquivos pequenos (compilação, instalação de jogos). Pode afetar instaladores legados.",
            "Speeds up creating thousands of small files (compiling, game installs). May affect legacy installers.",
            KFilesystem, "NtfsDisable8dot3NameCreation", 1);

        yield return T("disk.recycle-clean", TweakSection.Disk, TweakRisk.Low, TweakTags.None,
            "Esvaziar Lixeira de todos os discos",
            "Empty Recycle Bin on all drives",
            "Libera espaço em todos os volumes de uma vez, inclusive os que o Windows esquece por não estarem fixos.",
            "Frees space across all volumes at once, including ones Windows forgets because they are not fixed.",
            () => new TweakStateInfo(false, Loc.Pick("Ação manual", "Manual action")),
            "Clear-RecycleBin -Force -ErrorAction SilentlyContinue",
            TweakOp.Cmd("Clear-RecycleBin -Force -ErrorAction SilentlyContinue", ""));
    }

    // =====================================================================
    // 7) PERIFÉRICOS & INPUT LAG
    // =====================================================================
    private static IEnumerable<TweakEntry> BuildInput()
    {
        yield return T("input.mouse-accel-off", TweakSection.Input, TweakRisk.Low, TweakTags.None,
            "Aceleração do mouse desligada (1:1)",
            "Mouse acceleration off (1:1)",
            "MouseSpeed=0, MouseThreshold1=0 e MouseThreshold2=0 dão movimento linear 1:1 — obrigatório para mira em FPS competitivo.",
            "MouseSpeed=0, MouseThreshold1=0 and MouseThreshold2=0 give linear 1:1 movement — mandatory for aiming in competitive FPS.",
            () => CheckReg(KMouseKey, "MouseSpeed", "0"),
            "reg add \"HKCU\\Control Panel\\Mouse\" /v MouseSpeed /t REG_SZ /d 0 /f",
            TweakOp.RegString(KMouseKey, "MouseSpeed", "0"),
            TweakOp.RegString(KMouseKey, "MouseThreshold1", "0"),
            TweakOp.RegString(KMouseKey, "MouseThreshold2", "0"));

        yield return T("input.keyboard-repeat-fast", TweakSection.Input, TweakRisk.Low, TweakTags.None,
            "Repetição de teclado rápida (500 ms / 31)",
            "Fast keyboard repeat (500 ms / 31)",
            "KeyboardDelay=0 e KeyboardSpeed=31 respondem ao menor tempo permitido pelo Windows para repetição de tecla — útil em jogos com hold.",
            "KeyboardDelay=0 and KeyboardSpeed=31 give the fastest repeat Windows allows — useful in games with hold inputs.",
            () => CheckReg(KKeyboard, "KeyboardSpeed", "31"),
            "reg add \"HKCU\\Control Panel\\Keyboard\" /v KeyboardSpeed /t REG_SZ /d 31 /f",
            TweakOp.RegString(KKeyboard, "KeyboardSpeed", "31"),
            TweakOp.RegString(KKeyboard, "KeyboardDelay", "0"));

        yield return T("input.filter-keys-off", TweakSection.Input, TweakRisk.Low, TweakTags.None,
            "Teclas de filtro desativadas",
            "Filter keys disabled",
            "FilterKeys (acessibilidade) segura a repetição de teclas quando você aperta rápido — mata completamente o strafe. Este tweak zera o filtro na API do Windows.",
            "FilterKeys (accessibility) holds back key repeat when you press fast — it completely kills strafing. This tweak zeroes the filter through the Windows API.",
            () => CheckFilterKeys(),
            "SystemParametersInfo(SPI_SETFILTERKEYS, fFilterKeys=0)",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("input.mouse-pointer-precision", TweakSection.Input, TweakRisk.Low, TweakTags.None,
            "Aprimorar precisão do ponteiro DESLIGADO",
            "Enhance pointer precision OFF",
            "MouseSensitivity com 'Enhance pointer precision' desligado remove a curva de aceleração escondida do Windows 11 que atrapalha flicks.",
            "MouseSensitivity with 'Enhance pointer precision' off removes the hidden Windows 11 acceleration curve that ruins flicks.",
            () => CheckReg(KMouseKey, "MouseSensitivity", "10"),
            "reg add \"HKCU\\Control Panel\\Mouse\" /v MouseSensitivity /t REG_SZ /d 10 /f",
            TweakOp.RegString(KMouseKey, "MouseSensitivity", "10"));

        yield return Dword("input.mouse-data-queue", TweakSection.Input, TweakRisk.Medium, TweakTags.None,
            "Fila de dados do mouse em 16 (USB polling)",
            "Mouse data queue at 16",
            "Aumentar o buffer do driver mouclass reduz perda de pacotes com mouse de 1000 Hz ou 8000 Hz em CPUs ocupadas.",
            "Raising the mouclass driver buffer reduces packet loss with 1000 Hz or 8000 Hz mice on busy CPUs.",
            KMouseClass, "MouseDataQueueSize", 16);

        yield return T("input.kbd-data-queue", TweakSection.Input, TweakRisk.Low, TweakTags.None,
            "Fila de dados do teclado em 32",
            "Keyboard data queue at 32",
            "Buffer maior do kbdclass evita teclas 'engolidas' em teclados mecânicos com macros rápidas.",
            "A larger kbdclass buffer prevents swallowed keys on mechanical keyboards with fast macros.",
            () => CheckReg(KKeyboardClass, "KeyboardDataQueueSize", 32), "",
            TweakOp.RegDword(KKeyboardClass, "KeyboardDataQueueSize", 32));

        yield return T("input.usb-selective-suspend", TweakSection.Input, TweakRisk.Medium, TweakTags.None,
            "Suspensão seletiva USB desligada",
            "USB selective suspend off",
            "Impede o Windows de dormir portas USB, que causa o famoso 'mouse desconectou por 2 s' no meio da partida.",
            "Stops Windows from sleeping USB ports, which causes the famous 'mouse disconnected for 2 seconds' mid-match.",
            () => new TweakStateInfo(false, Loc.Pick("Aplicado por plano de energia", "Applied per power plan")),
            "powercfg /setacvalueindex SCHEME_CURRENT 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0",
            TweakOp.Cmd("powercfg /setacvalueindex SCHEME_CURRENT 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0; powercfg /setactive SCHEME_CURRENT",
                        "powercfg /setacvalueindex SCHEME_CURRENT 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 1; powercfg /setactive SCHEME_CURRENT"));

        yield return T("input.pcie-msi-mode", TweakSection.Input, TweakRisk.Medium, TweakTags.None,
            "PCIe MSI Mode na GPU (interrupções sem linha)",
            "PCIe MSI Mode on GPU (no line interrupts)",
            "Habilita Message Signaled Interrupts na GPU e no controlador USB: menos disputa de IRQ e frame time mais estável. A aba de Input Lag lista cada dispositivo com botão próprio.",
            "Enables Message Signaled Interrupts on the GPU and USB controller: less IRQ contention and steadier frame times. The Input Lag page lists every device with its own button.",
            () => new TweakStateInfo(false, Loc.Pick("Aplicado por dispositivo", "Applied per device")),
            "reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Enum\\PCI\\VEN_10DE&DEV_XXXX\\...\\MessageSignaledInterruptProperties\" /v MSISupported /t REG_DWORD /d 1 /f",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("input.timer-05ms", TweakSection.Input, TweakRisk.Medium, TweakTags.NoAuto,
            "Resolver timer em 0,5 ms durante o jogo",
            "Timer resolution at 0.5 ms during gaming",
            "NtSetTimerResolution(5000) deixa o agendador responder em 0,5 ms em vez de 15,6 ms. O PHANTOM UI só mantém isso ativo enquanto um jogo está aberto (opção GameTimer05Ms).",
            "NtSetTimerResolution(5000) makes the scheduler respond in 0.5 ms instead of 15.6 ms. PHANTOM UI only keeps it active while a game is running (GameTimer05Ms option).",
            () => new TweakStateInfo(false,
                Loc.Pick("Timer atual: ", "Current timer: ") + InputLagTools.GetLiveMemoryAndTimer().TimerMs),
            "NtSetTimerResolution(5000, TRUE, &cur)",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return Dword("input.global-timer-off", TweakSection.Input, TweakRisk.Medium, TweakTags.None,
            "Sem pedido global de timer",
            "No global timer request",
            "GlobalTimerResolutionRequests=0 evita que apps de RGB e launchers forcem timer alto o tempo todo, o que eleva temperatura e reduz o boost.",
            "GlobalTimerResolutionRequests=0 stops RGB apps and launchers from forcing a high timer all the time, raising temperatures and cutting boost.",
            KKernel, "GlobalTimerResolutionRequests", 0);

        yield return T("input.raw-input", TweakSection.Input, TweakRisk.Low, TweakTags.None,
            "Entrada bruta (Raw Input) preferida",
            "Raw Input preferred",
            "Windows usa Raw Input quando o jogo pede, ignorando a aceleração do cursor. Este card garante que o filtro de acessibilidade não interfira.",
            "Windows uses Raw Input when the game asks, bypassing cursor acceleration. This card ensures the accessibility filter does not interfere.",
            () => CheckFilterKeys(),
            "SystemParametersInfo(SPI_GETMOUSE / SPI_SETMOUSE)",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("input.win-keys-off-gaming", TweakSection.Input, TweakRisk.Low, TweakTags.NoAuto,
            "Bloquear tecla Windows durante o jogo",
            "Block the Windows key while gaming",
            "Evita minimizar o jogo sem querer. Aplicado pelo modo Game Focus da aba de Jogos, que também silencia notificações.",
            "Prevents accidentally minimizing the game. Applied by Game Focus in the Games page, which also silences notifications.",
            () => new TweakStateInfo(false, Loc.Pick("Aplicado por sessão de jogo", "Applied per game session")),
            "Hook de teclado (WH_KEYBOARD_LL) ativo apenas com o jogo em foco",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return Dword("input.hid-power-off", TweakSection.Input, TweakRisk.Low, TweakTags.None,
            "HID: desativar economia de energia do dispositivo",
            "HID: disable device power saving",
            "EnhancedPowerManagementEnabled=0 impede o Windows de reduzir o polling do mouse e do teclado para economizar energia — causa micro-travadas na mira.",
            "EnhancedPowerManagementEnabled=0 stops Windows from lowering mouse and keyboard polling to save power — it causes micro-stutters in aim.",
            KMouseClass, "EnhancedPowerManagementEnabled", 0);
    }

    // =====================================================================
    // 8) REDE & LATÊNCIA
    // =====================================================================
    private static IEnumerable<TweakEntry> BuildNet()
    {
        yield return Dword("net.tcp-nagle-off", TweakSection.Net, TweakRisk.Medium, TweakTags.None,
            "Desativar algoritmo de Nagle (TcpAckFrequency)",
            "Disable Nagle algorithm (TcpAckFrequency)",
            "TcpAckFrequency=1 e TCPNoDelay=1 enviam ACKs imediatamente em vez de agrupar pacotes. Ganho de latência em jogos de tiro; pode reduzir throughput em downloads.",
            "TcpAckFrequency=1 and TCPNoDelay=1 send ACKs immediately instead of batching. A latency win for shooters; may reduce download throughput.",
            KMsTcp, "TcpAckFrequency", 1);

        yield return Dword("net.tcp-nodelay", TweakSection.Net, TweakRisk.Medium, TweakTags.None,
            "TCPNoDelay = 1",
            "TCPNoDelay = 1",
            "Complemento do card acima: desativa o buffer de transmissão por socket, devolvendo o pacote ao driver imediatamente.",
            "Complements the card above: disables per-socket transmission buffering, handing the packet to the driver immediately.",
            KMsTcp, "TCPNoDelay", 1);

        yield return T("net.task-offload", TweakSection.Net, TweakRisk.Low, TweakTags.None,
            "Task Offload habilitado (regra Kairo)",
            "Task Offload enabled (Kairo rule)",
            "netsh int tcp set global taskoffload=enabled devolve o processamento de checksum/timestamp para a placa de rede. Desativar (mito antigo) sobrecarrega a CPU.",
            "netsh int tcp set global taskoffload=enabled gives checksum/timestamp processing back to the NIC. Disabling it (old myth) overloads the CPU.",
            () => new TweakStateInfo(
                TweakJournal.RunProcess("netsh.exe", "int tcp show global").Output.Contains("enabled", StringComparison.OrdinalIgnoreCase),
                Loc.Pick("Estado global do TCP", "TCP global state")),
            "netsh int tcp set global taskoffload=enabled",
            TweakOp.Cmd("netsh int tcp set global taskoffload=enabled", "netsh int tcp set global taskoffload=disabled"));

        yield return T("net.rss-on", TweakSection.Net, TweakRisk.Low, TweakTags.None,
            "RSS (Receive Side Scaling) ativo",
            "RSS (Receive Side Scaling) enabled",
            "Distribui o processamento de rede entre núcleos. Desativar coloca todo o tráfego em um núcleo e engasga jogos com voz e download simultâneos.",
            "Spreads network processing across cores. Disabling it puts all traffic on one core and stutters games with voice and download at the same time.",
            () => new TweakStateInfo(false, Loc.Pick("Por adaptador", "Per adapter")),
            "netsh int tcp set global rss=enabled",
            TweakOp.Cmd("netsh int tcp set global rss=enabled", "netsh int tcp set global rss=disabled"));

        yield return T("net.autotuning-normal", TweakSection.Net, TweakRisk.Low, TweakTags.None,
            "Janela TCP em normal (não restricted)",
            "TCP window at normal (not restricted)",
            "AutotuningLevel=normal evita o 'restricted' que muitos pacotes aplicam e que reduz drasticamente a banda em conexões com latência.",
            "AutotuningLevel=normal avoids the 'restricted' setting many packs apply, which sharply cuts throughput on high-latency connections.",
            () => new TweakStateInfo(false, Loc.Pick("Por adaptador", "Per adapter")),
            "netsh int tcp set global autotuninglevel=normal",
            TweakOp.Cmd("netsh int tcp set global autotuninglevel=normal", "netsh int tcp set global autotuninglevel=restricted"));

        yield return T("net.ecn-off", TweakSection.Net, TweakRisk.Low, TweakTags.None,
            "ECN desativado (compatibilidade com roteadores)",
            "ECN disabled (router compatibility)",
            "Muitos roteadores domésticos tratam ECN mal e geram retransmissões. O padrão do Windows 11 já é disabled; este card garante o valor.",
            "Many home routers mishandle ECN and cause retransmissions. Windows 11 already defaults to disabled; this card enforces the value.",
            () => new TweakStateInfo(false, Loc.Pick("Estado global", "Global state")),
            "netsh int tcp set global ecncapability=disabled",
            TweakOp.Cmd("netsh int tcp set global ecncapability=disabled", "netsh int tcp set global ecncapability=enabled"));

        yield return T("net.qos-dscp46", TweakSection.Net, TweakRisk.Low, TweakTags.None,
            "QoS DSCP 46 para o executável do jogo",
            "QoS DSCP 46 for the game executable",
            "Marca os pacotes do jogo com prioridade EF (DSCP 46), que roteadores com QoS respeitam. A aba de Jogos aplica isso por jogo com Group Policy nativa.",
            "Marks game packets with EF priority (DSCP 46), which QoS-aware routers honor. The Games page applies this per game using native Group Policy.",
            () => new TweakStateInfo(false, Loc.Pick("Aplicado por jogo", "Applied per game")),
            "New-NetQosPolicy -Name \"PhantomUI_<jogo>\" -AppPathNameMatchCondition \"jogo.exe\" -DSCPAction 46 -NetworkProfile All",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("net.dns-cloudflare", TweakSection.Net, TweakRisk.Low, TweakTags.None,
            "DNS rápido: Cloudflare 1.1.1.1",
            "Fast DNS: Cloudflare 1.1.1.1",
            "Define 1.1.1.1 e 1.0.0.1 nos adaptadores ativos. Use o benchmark de DNS da aba Rede antes: o melhor DNS depende da sua operadora.",
            "Sets 1.1.1.1 and 1.0.0.1 on active adapters. Run the DNS benchmark on the Network page first: the best DNS depends on your ISP.",
            () => new TweakStateInfo(false, Loc.Pick("Por adaptador", "Per adapter")),
            "netsh interface ip set dns name=\"Ethernet\" static 1.1.1.1 primary",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return Dword("net.dns-cache-boost", TweakSection.Net, TweakRisk.Low, TweakTags.None,
            "Cache DNS maior (menos consultas)",
            "Larger DNS cache (fewer lookups)",
            "MaxCacheTtl e MaxNegativeCacheTtl controlam por quanto tempo o Windows guarda resoluções. Aumentar reduz consultas repetidas e o pico de latência ao entrar em partida.",
            "MaxCacheTtl and MaxNegativeCacheTtl control how long Windows caches resolutions. Raising them reduces repeated lookups and the latency spike when joining a match.",
            KNetDns, "MaxCacheTtl", 86400);

        yield return T("net.netbios-off", TweakSection.Net, TweakRisk.Medium, TweakTags.None,
            "Desativar NetBIOS sobre TCP/IP",
            "Disable NetBIOS over TCP/IP",
            "NetbiosOptions=2 remove broadcast de descoberta de nome na rede. Menos ruído de broadcast, mais estabilidade de ping em rede doméstica.",
            "NetbiosOptions=2 removes name discovery broadcasts on the network. Less broadcast noise, steadier ping on a home network.",
            () => new TweakStateInfo(false, Loc.Pick("Por adaptador", "Per adapter")),
            "reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\NetBT\\Parameters\\Interfaces\\Tcpip_<id>\" /v NetbiosOptions /t REG_DWORD /d 2 /f",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("net.lso-on", TweakSection.Net, TweakRisk.Low, TweakTags.None,
            "Large Send Offload ativo",
            "Large Send Offload enabled",
            "LSO entrega segmentação de pacotes grandes à placa, reduzindo uso de CPU em downloads — ajuda a manter o FPS estável com update rodando.",
            "LSO hands large packet segmentation to the NIC, cutting CPU use during downloads — helps keep FPS stable while an update runs.",
            () => new TweakStateInfo(false, Loc.Pick("Por adaptador", "Per adapter")),
            "Set-NetAdapterAdvancedProperty -Name * -DisplayName \"Large Send Offload V2 (IPv4)\" -DisplayValue Enabled",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("net.power-saving-off", TweakSection.Net, TweakRisk.Low, TweakTags.None,
            "Sem economia de energia na placa de rede",
            "No power saving on the network card",
            "Desliga 'Permitir que o computador desligue este dispositivo' na NIC: evita o pico de ping nos primeiros segundos depois de um período ocioso.",
            "Turns off 'Allow the computer to turn off this device' on the NIC: avoids the ping spike in the first seconds after idle.",
            () => new TweakStateInfo(false, Loc.Pick("Por adaptador", "Per adapter")),
            "Set-NetAdapterPowerManagement -Name * -AllowComputerToTurnOffDevice Disabled",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("net.mtu-1500", TweakSection.Net, TweakRisk.Medium, TweakTags.None,
            "MTU explícito de 1500",
            "Explicit MTU of 1500",
            "Fixa MTU 1500 (ou 1492 em PPPoE) no adaptador ativo. Evita fragmentação causada por drivers que configuram MTU errado.",
            "Pins MTU 1500 (or 1492 on PPPoE) on the active adapter. Prevents fragmentation caused by drivers that set the wrong MTU.",
            () => new TweakStateInfo(false, Loc.Pick("Por adaptador", "Per adapter")),
            "netsh interface ipv4 set subinterface \"Ethernet\" mtu=1500 store=persistent",
            TweakOp.Cmd("(void)0", "(void)0"));

        yield return T("net.winsock-reset", TweakSection.Net, TweakRisk.Medium, TweakTags.NoAuto,
            "Redefinir Winsock e pilha TCP/IP",
            "Reset Winsock and the TCP/IP stack",
            "Corrige 'sem conexão' depois de antivírus ou VPN mal removidos. Exige reinício e derruba todas as conexões abertas na hora.",
            "Fixes 'no connection' after antivirus or VPN were badly removed. Requires a reboot and drops all open connections immediately.",
            () => new TweakStateInfo(false, Loc.Pick("Ação manual", "Manual action")),
            "netsh winsock reset\nnetsh int ip reset",
            TweakOp.Cmd("netsh winsock reset; netsh int ip reset", ""));
    }

    // =====================================================================
    // 9) ENERGIA & PLANOS
    // =====================================================================
    private static IEnumerable<TweakEntry> BuildPower()
    {
        yield return T("power.ultimate-plan", TweakSection.Power, TweakRisk.Medium, TweakTags.Desktop,
            "Ativar plano Desempenho Máximo (Ultimate)",
            "Enable Ultimate Performance plan",
            "Duplica o plano Ultimate (e849...) e o ativa. Remove micro-latências de gerenciamento de energia em desktops; em notebook aumenta muito o consumo.",
            "Duplicates and activates the Ultimate plan (e849...). Removes power-management micro-latency on desktops; on laptops it drains the battery heavily.",
            () => new TweakStateInfo(
                TweakJournal.RunProcess("powercfg.exe", "/getactivescheme").Output.Contains("e9a42b02", StringComparison.OrdinalIgnoreCase),
                Loc.Pick("Plano ativo", "Active plan")),
            "powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61\npowercfg /setactive e9a42b02-d5df-448d-aa00-03f14749eb61",
            TweakOp.Cmd("powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 | Out-Null; powercfg /setactive e9a42b02-d5df-448d-aa00-03f14749eb61",
                        "powercfg /setactive SCHEME_BALANCED"));

        yield return T("power.high-perf-plan", TweakSection.Power, TweakRisk.Low, TweakTags.None,
            "Plano Alto Desempenho",
            "High Performance plan",
            "Ativa o plano Alto Desempenho clássico (8c5e...), bom meio-termo para notebooks na tomada.",
            "Activates the classic High Performance plan (8c5e...), a good middle ground for laptops plugged in.",
            () => new TweakStateInfo(
                TweakJournal.RunProcess("powercfg.exe", "/getactivescheme").Output.Contains("8c5e7fda", StringComparison.OrdinalIgnoreCase),
                Loc.Pick("Plano ativo", "Active plan")),
            "powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c",
            TweakOp.Cmd("powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", "powercfg /setactive SCHEME_BALANCED"));

        yield return T("power.balanced-plan", TweakSection.Power, TweakRisk.Low, TweakTags.None,
            "Voltar para Equilibrado",
            "Back to Balanced",
            "Restaura o plano Equilibrado do Windows, recomendado para notebooks e uso de escritório.",
            "Restores the Windows Balanced plan, recommended for laptops and office use.",
            () => new TweakStateInfo(
                TweakJournal.RunProcess("powercfg.exe", "/getactivescheme").Output.Contains("381b4222", StringComparison.OrdinalIgnoreCase),
                Loc.Pick("Plano ativo", "Active plan")),
            "powercfg /setactive SCHEME_BALANCED",
            TweakOp.Cmd("powercfg /setactive SCHEME_BALANCED", "powercfg /setactive SCHEME_BALANCED"));

        yield return T("power.usb-suspend-off", TweakSection.Power, TweakRisk.Medium, TweakTags.None,
            "Suspensão seletiva USB desativada",
            "USB selective suspend disabled",
            "Evita desconexão de mouse, teclado e headset USB durante o jogo.",
            "Prevents mouse, keyboard and USB headset disconnects during a game.",
            () => new TweakStateInfo(false, Loc.Pick("Plano ativo", "Active plan")),
            "powercfg /setacvalueindex SCHEME_CURRENT 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0",
            TweakOp.Cmd("powercfg /setacvalueindex SCHEME_CURRENT 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0; powercfg /setactive SCHEME_CURRENT",
                        "powercfg /setacvalueindex SCHEME_CURRENT 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 1; powercfg /setactive SCHEME_CURRENT"));

        yield return T("power.pcie-link-aspm-off", TweakSection.Power, TweakRisk.Medium, TweakTags.Desktop,
            "Desativar ASPM do link PCIe",
            "Disable PCIe link ASPM",
            "Impede a GPU de negociar estados de baixo consumo do barramento PCIe, que causam stutter de 1 quadro ao voltar de idle.",
            "Stops the GPU from negotiating low-power PCIe link states, which cause one-frame stutter when returning from idle.",
            () => new TweakStateInfo(false, Loc.Pick("Plano ativo", "Active plan")),
            "powercfg /setacvalueindex SCHEME_CURRENT SUB_PCIEXPRESS ASPM 0",
            TweakOp.Cmd("powercfg /setacvalueindex SCHEME_CURRENT SUB_PCIEXPRESS ASPM 0; powercfg /setactive SCHEME_CURRENT",
                        "powercfg /setacvalueindex SCHEME_CURRENT SUB_PCIEXPRESS ASPM 2; powercfg /setactive SCHEME_CURRENT"));

        yield return T("power.disk-timeout-never", TweakSection.Power, TweakRisk.Low, TweakTags.Desktop,
            "Disco nunca desliga",
            "Disk never sleeps",
            "DISKIDLE=0 mantém o disco ativo. Evita a espera de 2-3 s quando o jogo acessa o disco depois de um tempo parado em um menu.",
            "DISKIDLE=0 keeps the disk active. Avoids the 2-3 s wait when a game hits the disk after sitting in a menu.",
            () => new TweakStateInfo(false, Loc.Pick("Plano ativo", "Active plan")),
            "powercfg /setacvalueindex SCHEME_CURRENT SUB_DISK DISKIDLE 0",
            TweakOp.Cmd("powercfg /setacvalueindex SCHEME_CURRENT SUB_DISK DISKIDLE 0; powercfg /setactive SCHEME_CURRENT",
                        "powercfg /setacvalueindex SCHEME_CURRENT SUB_DISK DISKIDLE 1200; powercfg /setactive SCHEME_CURRENT"));

        yield return T("power.lid-do-nothing", TweakSection.Power, TweakRisk.Low, TweakTags.Laptop,
            "Fechar a tampa não suspende (na tomada)",
            "Closing the lid does not sleep (plugged in)",
            "LIDACTION=0 na energia AC permite usar o notebook como desktop com a tampa fechada, sem throttling por suspensão.",
            "LIDACTION=0 on AC power lets you use the laptop as a desktop with the lid closed, with no sleep-related throttling.",
            () => new TweakStateInfo(false, Loc.Pick("Plano ativo", "Active plan")),
            "powercfg /setacvalueindex SCHEME_CURRENT SUB_BUTTONS LIDACTION 0",
            TweakOp.Cmd("powercfg /setacvalueindex SCHEME_CURRENT SUB_BUTTONS LIDACTION 0; powercfg /setactive SCHEME_CURRENT",
                        "powercfg /setacvalueindex SCHEME_CURRENT SUB_BUTTONS LIDACTION 1; powercfg /setactive SCHEME_CURRENT"));

        yield return T("power.sleep-never-ac", TweakSection.Power, TweakRisk.Low, TweakTags.Desktop,
            "Sem suspensão na tomada",
            "No sleep on AC power",
            "STANDBYIDLE=0 impede a máquina de dormir durante partidas longas ou downloads grandes.",
            "STANDBYIDLE=0 stops the machine from sleeping during long matches or big downloads.",
            () => new TweakStateInfo(false, Loc.Pick("Plano ativo", "Active plan")),
            "powercfg /setacvalueindex SCHEME_CURRENT SUB_SLEEP STANDBYIDLE 0",
            TweakOp.Cmd("powercfg /setacvalueindex SCHEME_CURRENT SUB_SLEEP STANDBYIDLE 0; powercfg /setactive SCHEME_CURRENT",
                        "powercfg /setacvalueindex SCHEME_CURRENT SUB_SLEEP STANDBYIDLE 1200; powercfg /setactive SCHEME_CURRENT"));

        yield return T("power.throttling-off", TweakSection.Power, TweakRisk.High, TweakTags.Laptop | TweakTags.NoAuto,
            "Desativar throttling térmico do Windows",
            "Disable Windows thermal throttling",
            "PERFTHROTTLE... = 100 mantém a CPU no máximo mesmo aquecendo. Em notebook isso acelera o desgaste e pode causar desligamento por proteção.",
            "Keeping the CPU at maximum even when hot. On laptops this accelerates wear and can cause a protection shutdown.",
            () => new TweakStateInfo(false, Loc.Pick("Plano ativo", "Active plan")),
            "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMAX 100",
            TweakOp.Cmd("powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMAX 100; powercfg /setactive SCHEME_CURRENT",
                        "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMAX 100; powercfg /setactive SCHEME_CURRENT"));

        yield return T("power.battery-saver-list", TweakSection.Power, TweakRisk.Low, TweakTags.Laptop,
            "Desativar Economia de Bateria em jogos",
            "Disable Battery Saver in games",
            "EnergySaverPolicy desligada evita que o Windows 11 corte clocks da CPU quando a bateria cai abaixo de 20%.",
            "EnergySaverPolicy off stops Windows 11 from cutting CPU clocks when the battery drops below 20%.",
            () => new TweakStateInfo(false, Loc.Pick("Política de energia", "Power policy")),
            "powercfg /setdcvalueindex SCHEME_CURRENT SUB_ENERGYSAVER ESBATTTHRESHOLD 0",
            TweakOp.Cmd("powercfg /setdcvalueindex SCHEME_CURRENT SUB_ENERGYSAVER ESBATTTHRESHOLD 0; powercfg /setactive SCHEME_CURRENT",
                        "powercfg /setdcvalueindex SCHEME_CURRENT SUB_ENERGYSAVER ESBATTTHRESHOLD 20; powercfg /setactive SCHEME_CURRENT"));

        yield return T("power.wake-timers", TweakSection.Power, TweakRisk.Low, TweakTags.None,
            "Bloquear timers que acordam o PC",
            "Block wake timers",
            "RTCWAKE=0 impede atualizações e telemetria de acordarem a máquina no meio da noite e deixarem o PC lento no dia seguinte.",
            "RTCWAKE=0 stops updates and telemetry from waking the machine at night and leaving it slow the next day.",
            () => new TweakStateInfo(false, Loc.Pick("Plano ativo", "Active plan")),
            "powercfg /setacvalueindex SCHEME_CURRENT SUB_SLEEP RTCWAKE 0",
            TweakOp.Cmd("powercfg /setacvalueindex SCHEME_CURRENT SUB_SLEEP RTCWAKE 0; powercfg /setactive SCHEME_CURRENT",
                        "powercfg /setacvalueindex SCHEME_CURRENT SUB_SLEEP RTCWAKE 2; powercfg /setactive SCHEME_CURRENT"));

        yield return T("power.plan-import", TweakSection.Power, TweakRisk.Low, TweakTags.None,
            "Importar planos .pow embutidos (34 planos)",
            "Import bundled .pow plans (34 plans)",
            "O PHANTOM UI carrega 34 planos de energia curados do acervo Kairo. A aba Energia lista cada um com descrição e botão de aplicar.",
            "PHANTOM UI ships 34 curated power plans from the Kairo collection. The Power page lists each one with a description and an apply button.",
            () => new TweakStateInfo(PowerPlanVault.GetEmbeddedCatalog().Count > 0,
                Loc.Pick($"{PowerPlanVault.GetEmbeddedCatalog().Count} planos disponíveis", $"{PowerPlanVault.GetEmbeddedCatalog().Count} plans available")),
            "powercfg /import \"<plano>.pow\"",
            TweakOp.Cmd("(void)0", "(void)0"));
    }
}
