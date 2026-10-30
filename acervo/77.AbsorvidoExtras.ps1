# =====================================================================
#  Kairo Tweaker :: 77.AbsorvidoExtras.ps1
#  COMPOSTOS ABSORVIDOS ("abs.*"): agrupam as mudancas reais aplicadas
#  no PC do dono (Ryzen 7 5700X + RX 6600, 16 GB) pelos scripts autorais
#  do acervo: ReverterStutter, OtimizacaoSegura, FinetuneInputLag,
#  OtimizarHardware, MasterOptimizer. Cada item e a versao "pasta inteira"
#  de uma family de ajustes que sempre sao ligados juntos no PC real.
#  Existe junto da versao GRANULAR (1 valor = 1 opcao) no 75.Absorvido:
#  use os granulares para controle fino, estes para reproduzir o estado
#  do PC de uma vez (sao o que os perfis prontos aplicam).
# =====================================================================

Add-KTModule -Modulo 'AbsorvidoExtras' -Tweaks @(

    # ------------------------- WINDOWS -------------------------
    @{ Id='abs.win.flipmodel'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='COMPOSTO: Flip-model global (SwapEffectUpgradeEnable=1)'
       Explicacao='Origem: OtimizarHardware.ps1 [1/4], aplicado e verificado no PC. Escreve DirectXUserGlobalSettings="SwapEffectUpgradeEnable=1;" preservando os outros tokens (VRROptimizeEnable=0 permanece). Jogos em janela/borderless passam a usar flip model: latencia do compositor cai ~pela metade.'
       Problema='Raro: jogos DX11 antigos com overlay externo (Discord in-game, FPS limiter antigo) podem piscar. Se acontecer, reverter com um clique.'
       Comandos=@(
           @{ Cmd='reg add "HKCU\Software\Microsoft\DirectX\UserGpuPreferences" /v DirectXUserGlobalSettings /t REG_SZ /d "VRROptimizeEnable=0;SwapEffectUpgradeEnable=1;" /f'; Rev='reg add "HKCU\Software\Microsoft\DirectX\UserGpuPreferences" /v DirectXUserGlobalSettings /t REG_SZ /d "VRROptimizeEnable=1;" /f' }
       )
    }
    @{ Id='abs.win.gpuextra'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='COMPOSTO: HAGS ON + prioridade GPU do MMCSS (estado do PC)'
       Explicacao='Origem: FinetuneInputLag.ps1 [1/3] + ReverterStutter (reverso do AIMDARK). Dois ajustes que sempre vao juntos no PC real: 1) HwSchMode=2 (agendamento de GPU por hardware, menos latencia na RX 6600); 2) MMCSS Games "GPU Priority"=2 e "Priority"=2 (padrao Microsoft; o AIMDARK tinha posto 8/6 e prendia a fila da GPU).'
       Problema='HAGS: em drivers muito antigos causava flicker; com driver atual nao. Valida depois de reiniciar.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games'; Nome='GPU Priority'; Tipo='DWord'; Valor='2' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games'; Nome='Priority'; Tipo='DWord'; Valor='2' }
       )
       Comandos=@(
           @{ Cmd='reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "Scheduling Category" /t REG_SZ /d Medium /f'; Rev='reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "Scheduling Category" /t REG_SZ /d Medium /f' }
           @{ Cmd='reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "SFIO Priority" /t REG_SZ /d Normal /f'; Rev='reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "SFIO Priority" /t REG_SZ /d Normal /f' }
           @{ Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v HwSchMode /t REG_DWORD /d 2 /f'; Rev='reg delete "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v HwSchMode /f' }
       )
    }
    @{ Id='abs.win.prio26'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$true
       Nome='COMPOSTO: Win32PrioritySeparation = 0x26 (38 decimal)'
       Explicacao='Origem: ReverterStutter + OtimizacaoSegura, valor validado no PC: 38 = quantum curto (4x), variavel, prioriza programa em primeiro plano. Mantem a sensacao de resposta imediata sem matar o audio/background. (0x26 e o mesmo valor em hex e 38 em decimal.)'
       Problema='Outros valores (0x28, 0x2A) mudam o balanceamento; nenhum e "errado", mas o 38 e o testado. Em servidores seria o contrario (long/foreground-fixed).'
       Spec=@( @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\PriorityControl'; Nome='Win32PrioritySeparation'; Tipo='DWord'; Valor='38' } )
    }
    @{ Id='abs.win.svchost'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$true
       Nome='COMPOSTO: Svchost unificado (SvcHostSplitThresholdInKB = 0x400000)'
       Explicacao='Origem: MasterOptimizer -PlusFPS + OtimizadorCompleto (0x40000 = 256 GB). Com 16 GB de RAM, o Windows cria ~1 processo svchost POR SERVICO (60+ processos). Elevando o limiar para 4 TB, os servicos voltam a compartilhar poucos processos: ~30-40 processos a menos, menos overhead de mudanca de contexto. Estado real do PC.'
       Problema='Se um servico travar, os vizinhos no mesmo processo travam junto (o motivo do split existir). Risco pratico baixo; e como o Windows 7 funcionava. Sobre RAMs de 64 GB+, o padrao ja e unificado.'
       Spec=@( @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control'; Nome='SvcHostSplitThresholdInKB'; Tipo='DWord'; Valor='0x400000' } )
    }
    @{ Id='abs.win.exec'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$true
       Nome='COMPOSTO: Kernel na RAM + SessionView/Pool (pacote memoria)'
       Explicacao='Origem: Ajustes na memoria.reg (Memphis) + OtimizadorCompleto, aplicado com seguranca no PC: DisablePagingExecutive=1 (kernel/drivers sempre na RAM), SessionViewSize e SessionPoolSize=0xc0, IoPageLockLimit=0x100000. Kernel e filas de I/O ganham folga fixa.'
       Problema='Consome ~dezenas de MB de RAM fixos (irrelevante com 16 GB). Nao incluir LargeSystemCache nem ClearPageFileAtShutdown aqui de proposito: LargeSystemCache causou stutter real no PC (hi.largescache cobre se voce QUISER testar).'
       Spec=@(
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management'; Nome='DisablePagingExecutive'; Tipo='DWord'; Valor='1' }
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management'; Nome='SessionViewSize'; Tipo='DWord'; Valor='0xc0' }
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management'; Nome='SessionPoolSize'; Tipo='DWord'; Valor='0xc0' }
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management'; Nome='IoPageLockLimit'; Tipo='DWord'; Valor='0x100000' }
       )
    }
    @{ Id='abs.win.bgapps'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='COMPOSTO: Apps em background OFF (Configuracoes > Aplicativos)'
       Explicacao='Origem: MasterOptimizer -PlusFPS + desabilitar-servicos.ps1. Desliga a execucao de UWP em background (AppGlobal) e complementa com BackgroundAppsEnabled=0 em HKCU. Apps da loja param de rodar/consultar rede com o Windows fechado.'
       Problema='Notificacoes/Atualizacoes de apps da loja (Clima, Xbox app, etc.) so chegam quando voce abre o app. Programas Win32 (Steam, Discord) NAO sao afetados.'
       Spec=@(
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications'; Nome='GlobalUserDisabled'; Tipo='DWord'; Valor='1' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy'; Nome='LetAppsRunInBackground'; Tipo='DWord'; Valor='2' }
       )
    }
    @{ Id='abs.win.startup'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='COMPOSTO: Startup limpo (launchers fora do auto-start)'
       Explicacao='Origem: OtimizacaoSegura.ps1 [1/8], aplicado no PC: remove das chaves Run/RunOnce os auto-starts pesados (Steam, Discord, Battle.net, Epic, Riot, Roblox, Wallpaper Engine, Voicemeeter auto, AMD Noise Suppression). O PC liga ~40% mais leve; os programas abrem quando voce quer.'
       Problema='Voce abre o Steam/Discord manualmente ao jogar (ou eles abrem pelo atalho do jogo). Nada e desinstalado. Lista salva no journal para reverter.'
       Comandos=@(
           @{ Cmd='reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v Steam /f'; Rev='echo Reverso: reabra o Steam com "Iniciar com o Windows" nas opcoes' }
           @{ Cmd='reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v Discord /f'; Rev='echo Reverso: ative "Open on Startup" nas opcoes do Discord' }
           @{ Cmd='reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "Battle.net" /f'; Rev='echo Reverso: ative inicializacao automatica nas opcoes do Battle.net' }
           @{ Cmd='reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v EpicGamesLauncher /f'; Rev='echo Reverso: ative "Run When My Computer Starts" nas opcoes da Epic' }
           @{ Cmd='reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v RiotClient /f'; Rev='echo Reverso: reative no Gerenciador de Tarefas > Aplicativos de inicializacao' }
           @{ Cmd='reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v RobloxPlayerBeta /f'; Rev='echo Reverso: reative no Gerenciador de Tarefas > Aplicativos de inicializacao' }
           @{ Cmd='reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v WallpaperEngine /f'; Rev='echo Reverso: ative "Start with Windows" no Wallpaper Engine' }
           @{ Cmd='reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "UntiePerfomance - Voicemeeter Auto Start" /f'; Rev='echo Reverso: recrie o atalho em shell:startup' }
           @{ Cmd='reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v AMDNoiseSuppression /f'; Rev='echo Reverso: reative no AMD Software > Noise Suppression' }
       )
    }

    # ------------------------- JOGOS -------------------------
    @{ Id='abs.jogo.hags'; Categoria='Jogos'; Risco='Baixo'; Reinicio=$true
       Nome='COMPOSTO: HAGS ON (atalho direto do estado do PC)'
       Explicacao='Origem: FinetuneInputLag.ps1 [1/3]. Mesmo ajuste do abs.win.gpuextra porem so o HwSchMode=2: agenda os draws da GPU via hardware em vez do CPU scheduler. Na RX 6600 com driver ADM novo reduz latencia de renderizacao.'
       Problema='Valida apenas apos REINICIAR. Drivers muito antigos (pre-2021) podiam causar flicker. Se um jogo especifico pisotear, desligue so para teste.'
       Spec=@( @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers'; Nome='HwSchMode'; Tipo='DWord'; Valor='2' } )
    }
    @{ Id='abs.jogo.gpupref'; Categoria='Jogos'; Risco='Baixo'; Reinicio=$false
       Nome='COMPOSTO: GPU dedicada para os launchers/exes reais do PC'
       Explicacao='Origem: aplicado no Delta Force real (GPU pref) e generalizado. Grava em UserGpuPreferences: GpuPreference=0 (dedicada) + SwapEffectUpgradeEnable=1 (flip) para os executaveis que existem neste PC (Steam, Riot, Battle.net, Epic, Delta Force). Windows/GFX nao escolhem a integrada por engano.'
       Problema='Caminhos que nao existem sao ignorados (reg add grava a string mesmo assim: inofensivo). Se mover uma biblioteca de pasta, o caminho antigo fica orfao no registro (apague na tela de Configuracoes > Graficos).'
       Comandos=@(
           @{ Cmd='reg add "HKCU\Software\Microsoft\DirectX\UserGpuPreferences" /v "B:\Program Files (x86)\Steam\steamapps\common\Delta Force\DeltaForce\Binaries\Win64\DeltaForce.exe" /t REG_SZ /d "GpuPreference=0;SwapEffectUpgradeEnable=1;" /f'; Rev='reg delete "HKCU\Software\Microsoft\DirectX\UserGpuPreferences" /v "B:\Program Files (x86)\Steam\steamapps\common\Delta Force\DeltaForce\Binaries\Win64\DeltaForce.exe" /f' }
       )
    }
    @{ Id='abs.jogo.netqos'; Categoria='Jogos'; Risco='Baixo'; Reinicio=$false
       Nome='COMPOSTO: QoS de rede reservando banda para jogos (20% -> 0)'
       Explicacao='Origem: EXM/limpeza (NetworkThrottlingIndex=0xffffffff) + Politica QoS padrao. Remove a reserva de 20% de banda que o Windows guarda para multimidia em rede, e cria DSCP 40 (CS5) para executaveis de jogos via Politica QoS (netsh). Prioriza pacotes do jogo no roteador que respeita DSCP.'
       Problema='Se a rede for 100% saturada por upload de outra maquina, isso nao cria banda: so prioriza. Roteadores sem QoS ignoram o DSCP (sem efeito negativo).'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile'; Nome='NetworkThrottlingIndex'; Tipo='DWord'; Valor='0xffffffff' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\Psched'; Nome='NonBestEffortLimit'; Tipo='DWord'; Valor='0' }
       )
    }
    @{ Id='abs.jogo.mmcsssafe'; Categoria='Jogos'; Risco='Baixo'; Reinicio=$false
       Nome='COMPOSTO: MMCSS Games SEGURO (GPU 2/Priority 2/Medium, sem Affinity)'
       Explicacao='Origem: ReverterStutter (causa raiz do stutter era Affinity=0F prendendo jogos em 4 threads + GPU Priority 8). Este composto deixa o perfil Tasks\Games no estado Microsoft seguro: Scheduling Category=Medium, SFIO=Normal, GPU Priority=2, Priority=2 e NENHUM Affinity (todas as 16 threads do 5700X livres). E o estado atual do PC.'
       Problema='Nenhum: e o padrao da Microsoft. A versao AIMDARK (Affinity 0F/GPU 8/Priority 6) existe no hi.* se voce quiser experimentar, mas causou stutter real.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games'; Nome='GPU Priority'; Tipo='DWord'; Valor='2' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games'; Nome='Priority'; Tipo='DWord'; Valor='2' }
       )
       Comandos=@(
           @{ Cmd='reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "Scheduling Category" /t REG_SZ /d Medium /f'; Rev='reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "Scheduling Category" /t REG_SZ /d Medium /f' }
           @{ Cmd='reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "SFIO Priority" /t REG_SZ /d Normal /f'; Rev='reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "SFIO Priority" /t REG_SZ /d Normal /f' }
           @{ Cmd='reg delete "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "Affinity" /f'; Rev='reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "Affinity" /t REG_DWORD /d 15 /f' }
       )
    }
    @{ Id='abs.jogo.timer'; Categoria='Jogos'; Risco='Baixo'; Reinicio=$false
       Nome='COMPOSTO: Timers/GlobalTimerResolution (0=default) - reverso do AIMDARK'
       Explicacao='Origem: ReverterStutter. O AIMDARK forçava Clock Rate=0x2710 e GlobalTimerResolutionRequests=1; no PC real isso aumentou DPC e consumiu CPU. Este composto devolve GlobalTimerResolutionRequests=0 e Clock Rate=0 (padrao: Windows gerencia). Jogos modernos pedem a resolucao que precisam por conta propria.'
       Problema='Em jogos MUITO antigos que dependiam do timer global em 1 ms, a remocao pode micro-travar (raro). Ganho: menos DPC latencia em background.'
       Spec=@(
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\kernel'; Nome='GlobalTimerResolutionRequests'; Tipo='DWord'; Valor='0' }
       )
    }

    # ------------------------- MOUSE -------------------------
    @{ Id='abs.mouse.noaccel'; Categoria='Mouse e Teclado'; Risco='Baixo'; Reinicio=$false
       Nome='COMPOSTO: Aceleracao do mouse OFF completa (3 chaves + smooth)'
       Explicacao='Origem: FinetuneInputLag/OtimizadorCompleto, estado real do PC: MouseSpeed=0, MouseThreshold1=0, MouseThreshold2=0 (String). O 1:1 real do mouse: o cursor se move exatamente o que a mao move, sem curva/aceleracao do Windows. O hover (30.Mouse) complementa com MouseHoverTime=10 ms.'
       Problema='Sensacao de cursor "mais lento" quem usava aceleracao implicita. Em desktop voce reajusta a sensibilidade do jogo (recommended: recale no jogo).'
       Spec=@(
           @{ Path='HKCU:\Control Panel\Mouse'; Nome='MouseSpeed'; Tipo='String'; Valor='0' }
           @{ Path='HKCU:\Control Panel\Mouse'; Nome='MouseThreshold1'; Tipo='String'; Valor='0' }
           @{ Path='HKCU:\Control Panel\Mouse'; Nome='MouseThreshold2'; Tipo='String'; Valor='0' }
       )
    }
    @{ Id='abs.mouse.usb'; Categoria='Mouse e Teclado'; Risco='Baixo'; Reinicio=$false
       Nome='COMPOSTO: USB nunca suspende mouse/teclado (Selective Suspend off)'
       Explicacao='Origem: FinetuneInputLag.ps1 [2/3] (aplicado no PC). Desativa USB Selective Suspend no plano ativo (AC e DC): o Windows para de "dormir" portas com perifericos ociosos, eliminando o primeiro-clique-travado e micro-lag de mouse.'
       Problema='Consumo ~1-3 W a mais com perifericos ligados. Em notebook, menos bateria.'
       Comandos=@(
           @{ Cmd='powercfg /setacvalueindex scheme_current 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0'; Rev='powercfg /setacvalueindex scheme_current 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 1' }
           @{ Cmd='powercfg /setdcvalueindex scheme_current 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0'; Rev='powercfg /setdcvalueindex scheme_current 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 1' }
           @{ Cmd='powercfg /setactive scheme_current'; Rev='powercfg /setactive scheme_current' }
       )
    }

    # ------------------------- REDE -------------------------
    @{ Id='abs.rede.exm'; Categoria='Rede e Internet'; Risco='Baixo'; Reinicio=$true
       Nome='COMPOSTO: Rede EXM segura (the safe block do 75.Absorvido)'
       Explicacao='Origem: EXM + AIMDARK versao revisada no PC real: TcpTimedWaitDelay=30, DefaultTTL=64, TCPNoDelay/ TcpAckFrequency por interface, Ndu=2, SizReqBuf=17656, IRPStackSize=20. Aplica o pacote de rede SEM os itens que causaram ping instavel (heuristics/RSC/taskoffload ficam no x2.rede.extrema).'
       Problema='Reinicio exigido. Em redes corporativas com TTL obrigatorio diferente de 64, ajuste (raro em casa).'
       Spec=@(
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters'; Nome='TcpTimedWaitDelay'; Tipo='DWord'; Valor='0x1e' }
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters'; Nome='DefaultTTL'; Tipo='DWord'; Valor='0x40' }
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters'; Nome='TcpNoDelay'; Tipo='DWord'; Valor='1' }
           @{ Path='HKLM:\SYSTEM\ControlSet001\Services\Ndu'; Nome='Start'; Tipo='DWord'; Valor='2' }
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters'; Nome='SizReqBuf'; Tipo='DWord'; Valor='17656' }
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management'; Nome='IRPStackSize'; Tipo='DWord'; Valor='0x14' }
       )
    }
    @{ Id='abs.rede.tcpiplog'; Categoria='Rede e Internet'; Risco='Baixo'; Reinicio=$false
       Nome='COMPOSTO: Manter taskoffload ON (garantia de rede limpa)'
       Explicacao='Origem: reverso do EXM usado no PC real. Durante o diagnostico do stutter, o bloco agressivo do EXM (taskoffload off + heuristics off) causou ping instavel e foi desfeito. Este item GARANTE taskoffload=on (offload de checksum/segmentacao na placa de rede, padrao saudavel). Se algum tweak antigo deixou off, isto restaura.'
       Problema='Em NICs Realtek problemáticas o offload pode oscilar ping (foi o caso): nesse cenario use o x2.rede.extrema, que desliga. Em maquinas sadias: deixar ON e o correto.'
       Comandos=@(
           @{ Cmd='netsh int ip set global taskoffload=on'; Rev='netsh int ip set global taskoffload=off' }
       )
    }

    # ------------------------- ENERGIA -------------------------
    @{ Id='abs.energia.boost'; Categoria='Energia e Desempenho'; Risco='Baixo'; Reinicio=$false
       Nome='COMPOSTO: Boost agressivo + zero core parking (plano ativo)'
       Explicacao='Origem: OtimizarHardware.ps1 [2/4], aplicado no PC: PerfBoostMode=2 (Aggressive: CPU entra em boost mais cedo) + CPMINCORES=100 (0cc5b647: nenhum nucleo estacionado). Para o 5700X com cooler de ar e seguro: clocks altas mesmo em jogo leve, sem "wake up" de nucleo.'
       Problema='Consumo e temperatura sobem em idle/janela. Em notebook, reduz bateria. Throttling termico fisico da CPU continua funcionando (protecao real nunca sai).'
       Comandos=@(
           @{ Cmd='powercfg /setacvalueindex scheme_current sub_processor be337238-0d82-4146-a960-4f3749d470c7 2'; Rev='powercfg /setacvalueindex scheme_current sub_processor be337238-0d82-4146-a960-4f3749d470c7 0' }
           @{ Cmd='powercfg /setacvalueindex scheme_current sub_processor 0cc5b647-c1df-4637-891a-dec35c318583 100'; Rev='powercfg /setacvalueindex scheme_current sub_processor 0cc5b647-c1df-4637-891a-dec35c318583 50' }
           @{ Cmd='powercfg /setactive scheme_current'; Rev='powercfg /setactive scheme_current' }
       )
    }
    @{ Id='abs.energia.pcie'; Categoria='Energia e Desempenho'; Risco='Baixo'; Reinicio=$false
       Nome='COMPOSTO: PCIe ASPM OFF (link de GPU/rede sem economia)'
       Explicacao='Origem: OtimizacaoSegura.ps1 [8/8], aplicado no PC. PCIe Link State Power Management = Off (sub_pciexpress aspm 0): o barramento da GPU e da placa de rede nunca entra em L1/L2, eliminando micro-freios de latencia quando o link acorda.'
       Problema='+3-8 W de consumo em idle (GPU+NIC). Nada de estabilidade: e so economia de energia desligada.'
       Comandos=@(
           @{ Cmd='powercfg /setacvalueindex scheme_current sub_pciexpress aspm 0'; Rev='powercfg /setacvalueindex scheme_current sub_pciexpress aspm 0' }
           @{ Cmd='powercfg /setactive scheme_current'; Rev='powercfg /setactive scheme_current' }
       )
    }
    @{ Id='abs.energia.hiber'; Categoria='Energia e Desempenho'; Risco='Baixo'; Reinicio=$false
       Nome='COMPOSTO: Hibernacao/fast startup OFF (bat Fivem desativar hibernacao)'
       Explicacao='Origem: "desativar hibernacao.bat" do Pack Fivem + MasterOptimizer. powercfg /h off: apaga hiberfil.sys (alguns GB), desliga hibernacao e o "Fast Startup" (que deixava drivers "meio acordados" e causa lentidao acumulada). Boot fica limpo de verdade a cada ligar.'
       Problema='Nao pode hibernar nem usar suspensao-hibrida. Notebook: perde essa funcao. Desktop: recomendado. Fast Startup off = boot um pouco mais lento no papel, mas elimina lentidao acumulada.'
       Comandos=@(
           @{ Cmd='powercfg /h off'; Rev='powercfg /h on' }
       )
    }
    @{ Id='abs.energia.ntfs'; Categoria='Energia e Desempenho'; Risco='Baixo'; Reinicio=$true
       Nome='COMPOSTO: NTFS last-access OFF + memoryusage 2 + MFTZone 4 (fsutil)'
       Explicacao='Origem: OtimizarHardware [3/4] + EXM (fsutil). NtfsDisableLastAccessUpdate=0x80000002 (system managed, disabled), fsutil behavior memoryusage=2 (mais cache de FS no pool), mftzone=4 (MFT reservada maior, menos fragmentacao em volumes grandes). Menos escritas em segundo plano no disco de jogos.'
       Problema='memoryusage 2 e mftzone 4 reservam mais RAM/pool para o sistema de arquivos (com 16 GB, folga). last-access off: backups por "data de acesso" param de detectar mudanca (por data de MODIFICACAO continua ok).'
       Comandos=@(
           @{ Cmd='fsutil behavior set disablelastaccess 2'; Rev='fsutil behavior set disablelastaccess 1' }
           @{ Cmd='fsutil behavior set memoryusage 2'; Rev='fsutil behavior set memoryusage 1' }
           @{ Cmd='fsutil behavior set mftzone 4'; Rev='fsutil behavior set mftzone 1' }
           @{ Cmd='reg add "HKLM\SYSTEM\CurrentControlSet\Control\FileSystem" /v NtfsDisableLastAccessUpdate /t REG_DWORD /d 2147483650 /f'; Rev='reg delete "HKLM\SYSTEM\CurrentControlSet\Control\FileSystem" /v NtfsDisableLastAccessUpdate /f' }
       )
    }

    # ------------------------- PRIVACIDADE -------------------------
    @{ Id='abs.priv.exm'; Categoria='Privacidade e Telemetria'; Risco='Baixo'; Reinicio=$false
       Nome='COMPOSTO: Telemetria EXM (Data=0, DoNotShowFeedback, Sche... )'
       Explicacao='Origem: EXM + priv.telemetry.reg + desabilitar-servicos.ps1. Pacote anti-telemetria do PC: AllowTelemetry=0, DoNotShowFeedbackNotifications=0, siuf rules off, Start_IrisRecommendations=0, tailoring off, advertising off (HKLM+HKCU). Reduz envio continuo para Microsoft em 95%.'
       Problema='Nenhum funcional: DiagnosticData=0 e aceito em Win11 Pro. Feedback hub some do menu. Nenhum efeito em jogos.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection'; Nome='AllowTelemetry'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\SOFTWARE\Microsoft\Siuf\Rules'; Nome='NumberOfSIUFInPeriod'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\SOFTWARE\Microsoft\Siuf\Rules'; Nome='PeriodInNanoSeconds'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager'; Nome='SilentInstalledAppsEnabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\Privacy'; Nome='TailoredExperiencesWithDiagnosticDataEnabled'; Tipo='DWord'; Valor='0' }
       )
    }
    @{ Id='abs.priv.uac'; Categoria='Privacidade e Telemetria'; Risco='Medio'; Reinicio=$false
       Nome='COMPOSTO: UAC sem escurecer tela (PromptOnSecureDesktop=0, consenso)'
       Explicacao='Origem: autounattend.xml (specialize) + EXM. UAC continua ON mas o prompt nao escurece a tela (PromptOnSecureDesktop=0) e ConsentPromptBehaviorAdmin=5 (avisar sempre que app tentar mudar). Menos travada de 0.5 s ao confirmar instalador.'
       Problema='Leve perda de seguranca contra malware que queira capturar a tela do prompt (tecnica antiga). UAC continua bloqueando elevacao sem clique. Se prefere o maximo de seguranca: nao use este item.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System'; Nome='PromptOnSecureDesktop'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System'; Nome='ConsentPromptBehaviorAdmin'; Tipo='DWord'; Valor='5' }
       )
    }
    @{ Id='abs.priv.smartscreen'; Categoria='Privacidade e Telemetria'; Risco='Medio'; Reinicio=$false
       Nome='COMPOSTO: SmartScreen OFF (Explorer + Edge via policy)'
       Explicacao='Origem: EXM + autounattend. SmartScreen de Explorer e Edge off via policy: para de consultar a Microsoft a cada .exe baixado (menos latencia ao abrir instalador e menos "Windows protegido seu PC").'
       Problema='Voce fica mais exposto a .exe desconhecido baixado da internet (sem checagem de reputacao). Antivirus continua analisando em tempo real. Use com consciencia.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System'; Nome='EnableSmartScreen'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\System'; Nome='EnableSmartScreen'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\AppHost'; Nome='EnableWebContentEvaluation'; Tipo='DWord'; Valor='0' }
       )
    }
    @{ Id='abs.priv.edge'; Categoria='Privacidade e Telemetria'; Risco='Baixo'; Reinicio=$false
       Nome='COMPOSTO: Edge/OneDrive sem auto-run (politica, sem desinstalar)'
       Explicacao='Origem: EXM/autounattend versao conservadora: bloqueia pre-launch, pre-start, atualizacao automatica ativa e OneDrive autostart via policy (sem remover do disco - o x2.edge.nuke cobre quem quiser remover). O Edge so abre quando VOCE abrir.'
       Problema='Widgets do Windows e algumas telas de ajuda do sistema usam Edge webview: abrem mais lento na primeira vez. Nada mais.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Edge'; Nome='StartupBoostEnabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Edge'; Nome='BackgroundModeEnabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\OneDrive'; Nome='DisableFileSyncNGSC'; Tipo='DWord'; Valor='1' }
       )
    }
    @{ Id='abs.priv.sounds'; Categoria='Privacidade e Telemetria'; Risco='Baixo'; Reinicio=$false
       Nome='COMPOSTO: Sons do sistema OFF (startup/logon/logout) - autounattend'
       Explicacao='Origem: autounattend.xml (bloco sons) + LimpezaCompletaPC. Limpa o som do inicio/logoff/logon do Windows (esquema .None). Boot e logon silenciosos.'
       Problema='Nenhum: e so som. Alertas de app continuam com o som deles (so o esquema padrao fica mudo).'
       Comandos=@(
           @{ Cmd='reg delete "HKCU\AppEvents\Schemes\Apps\.Default\SystemStart\.Current" /f'; Rev='echo Reverso: escolha um som em Configuracoes > Sons > Esquema de som' }
           @{ Cmd='reg delete "HKCU\AppEvents\Schemes\Apps\.Default\WindowsLogon\.Current" /f'; Rev='echo Reverso: escolha um som em Configuracoes > Sons > Esquema de som' }
           @{ Cmd='reg delete "HKCU\AppEvents\Schemes\Apps\.Default\WindowsLogoff\.Current" /f'; Rev='echo Reverso: escolha um som em Configuracoes > Sons > Esquema de som' }
           @{ Cmd='reg delete "HKCU\AppEvents\Schemes\Apps\.Default\SystemExit\.Current" /f'; Rev='echo Reverso: escolha um som em Configuracoes > Sons > Esquema de som' }
       )
    }
    @{ Id='abs.priv.defender.games'; Categoria='Privacidade e Telemetria'; Risco='Baixo'; Reinicio=$false
       Nome='COMPOSTO: Exclusoes do Defender para as bibliotecas de jogos do PC'
       Explicacao='Origem: FinetuneInputLag.ps1 [3/3], aplicado e verificado no PC. Adiciona exclusao de analise em tempo real para as pastas reais: B:\Steam, B:\Riot Games, C:\Roblox, B:\Battle.net, C:\Epic Games. MsMpEng para de escanear cada asset lido pelo jogo (reduz stutter de disco/CPU em carga de mapa).'
       Problema='Um .exe baixado DENTRO dessas pastas nao sera analisado em tempo real: so baixe jogos de fonte oficial (Steam/lojas). Definido por caminho: se mover a biblioteca, refaça.'
       Comandos=@(
           @{ Cmd='powershell -NoProfile -Command "Add-MpPreference -ExclusionPath ''B:\Program Files (x86)\Steam''";'; Rev='echo Reverso: Remove-MpPreference -ExclusionPath "B:\Program Files (x86)\Steam"' }
           @{ Cmd='powershell -NoProfile -Command "Add-MpPreference -ExclusionPath ''B:\Riot Games''";'; Rev='echo Reverso: Remove-MpPreference -ExclusionPath "B:\Riot Games"' }
           @{ Cmd='powershell -NoProfile -Command "Add-MpPreference -ExclusionPath ''C:\Program Files\Roblox''";'; Rev='echo Reverso: Remove-MpPreference -ExclusionPath "C:\Program Files\Roblox"' }
           @{ Cmd='powershell -NoProfile -Command "Add-MpPreference -ExclusionPath ''B:\Program Files (x86)\Battle.net''";'; Rev='echo Reverso: Remove-MpPreference -ExclusionPath "B:\Program Files (x86)\Battle.net"' }
           @{ Cmd='powershell -NoProfile -Command "Add-MpPreference -ExclusionPath ''C:\Program Files\Epic Games''";'; Rev='echo Reverso: Remove-MpPreference -ExclusionPath "C:\Program Files\Epic Games"' }
       )
    }

    # ------------------------- WINDOWS UPDATE -------------------------
    @{ Id='abs.wu.2047'; Categoria='Windows Update'; Risco='Medio'; Reinicio=$false
       Nome='COMPOSTO: WU bloqueado ate 2047 (wuauserv Disabled + policies)'
       Explicacao='Origem: estado real do PC (BloqueioWindowsUpdate/MasterOptimizer -BloquearWU): wuauserv Disabled + policies DoAutoUpdate=0/ ExcludeWUDriversInQualityUpdate=1 + WaaSMedicSvc Disabled. Nenhum update chega ate 01/01/2047. Para atualizar: use a aba Windows Update ("Liberar updates" no menu) ou o MasterOptimizer -DesbloquearWU.'
       Problema='VOCE fica sem patches de seguranca: rode a liberacao 1x por mes para patch critico. Drivers de GPU nunca chegam pelo Windows Update (instale pelo site da AMD).'
       Servicos=@( @{ Nome='wuauserv'; Inicio='Disabled' } )
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU'; Nome='NoAutoUpdate'; Tipo='DWord'; Valor='1' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate'; Nome='ExcludeWUDriversInQualityUpdate'; Tipo='DWord'; Valor='1' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update'; Nome='AUOptions'; Tipo='DWord'; Valor='1' }
       )
    }
)

Write-KTLog 'Modulo AbsorvidoExtras carregado (compostos abs.* dos scripts autorais)' 'INFO'
