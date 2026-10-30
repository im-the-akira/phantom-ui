# =====================================================================
#  Kairo Tweaker :: 20.Jogos.ps1
#  ABA 2 - JOGOS
#  Tudo relacionado a desempenho em jogos: gravacao em segundo plano,
#  modo de jogo, tela cheia, Game Bar e prioridade de rede para partidas.
# =====================================================================

Add-KTModule -Modulo 'Jogos' -Tweaks @(

    @{ Id='jogo.dvr'; Categoria='Jogos'; Risco='Baixo'; Reinicio=$false
       Nome='Desligar a gravação de tela em segundo plano (Game DVR)'
       Explicacao='Por padrao, o Windows 10/11 grava clipes e screenshots dos seus jogos o tempo todo, mesmo sem voce pedir. Esse processo (Game DVR) consome CPU e espaco em disco durante a partida e pode causar quedas de FPS. Desligar nao afeta o Xbox Game Bar nem a gravacao manual que voce inicia com Win+G.'
       Ganho='Cerca de 2% a 5% de FPS a mais e menos travadas durante a partida.'
       Spec=@(
           @{ Path='HKCU:\System\GameConfigStore';                            Nome='GameDVR_Enabled';               Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR'; Nome='AppCaptureEnabled';         Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR'; Nome='HistoricalCaptureEnabled';  Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\GameDVR';       Nome='AppCaptureEnabled';            Tipo='DWord'; Valor='0' }
       )
    }

    @{ Id='jogo.mode'; Categoria='Jogos'; Risco='Baixo'; Reinicio=$false
       Nome='Ligar o Modo de Jogo do Windows'
       Explicacao='O Game Mode faz o Windows identificar qual programa e um jogo e reduzir as prioridade das tarefas de fundo (atualizacoes, indexacao, antvirus) enquanto o jogo roda. A Microsoft recomenda e o proprio recurso padrao do Windows 10/11. Nao afeta desempenho offline, apenas a prioridade.'
       Ganho='Cerca de 3% a 10% de FPS em maquinas fracas, onde a CPU e disputada por tarefas de fundo.'
       Spec=@(
           @{ Path='HKCU:\SOFTWARE\Microsoft\GameBar'; Nome='AutoGameModeEnabled';    Tipo='DWord'; Valor='1' }
           @{ Path='HKCU:\System\GameConfigStore';      Nome='GameDVR_FSEBehaviorMode'; Tipo='DWord'; Valor='2' }
       )
    }

    @{ Id='jogo.fullscreen'; Categoria='Jogos'; Risco='Baixo'; Reinicio=$false
       Nome='Ajustes de tela cheia (Fullscreen Optimizations)'
       Explicacao='Desde o Windows 10, o Windows "otimiza" o modo tela cheia dos jogos e desativa a troca de pagina de video da GPU. Em alguns jogos isso causa stutter e uso extra de VRAM. HwSchMode=1 desliga o agendamento de hardware da GPU, que e a causa mais comum de queda de FPS na emissao de tela. Desligar o agendamento de hardware tambem resolve travadas em varios GPUs.'
       Ganho='Menos stutter e 1% low mais estavel em alguns jogos. Se a sua GPU roda melhor com o agendamento de hardware ligado, nao aplique esta tweak.'
       Spec=@(
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers'; Nome='HwSchMode'; Tipo='DWord'; Valor='1' }
           @{ Path='HKCU:\SOFTWARE\Microsoft\GameBar';                         Nome='UseNexusForGameBarEnabled'; Tipo='DWord'; Valor='0' }
       )
    }

    @{ Id='jogo.gamebar.popup'; Categoria='Jogos'; Risco='Baixo'; Reinicio=$false
       Nome='Impedir a janela da Game Bar durante o jogo'
       Explicacao='Ao entrar em tela cheia, a Xbox Game Bar pode abrir uma janela de "pressione Win+G" ou uma notificacao por cima do jogo. Desligar ShowStartupPanel e ShowGameModeNotifications remove essas interrupcoes.'
       Ganho='Nenhum ganho de FPS. Apenas remove a janela que atrapalha a partida.'
       Spec=@(
           @{ Path='HKCU:\SOFTWARE\Microsoft\GameBar';               Nome='ShowStartupPanel';           Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR'; Nome='ShowGameModeNotifications'; Tipo='DWord'; Valor='0' }
       )
    }

    @{ Id='jogo.xbox.servicos'; Categoria='Jogos'; Risco='Baixo'; Reinicio=$false
       Nome='Desligar os serviços do Xbox em segundo plano'
       Explicacao='Se voce nao joga no Xbox/Game Pass, os servicos XblAuthManager, XblGameSave, XboxGipSvc e XboxNetApiSvc sobem no boot mesmo assim para verificar assinatura e salvar progresso. Eles ficam de olho na rede e acordam o disco. Desligar libera cerca de 80 MB de RAM.'
       Ganho='Boot mais rapido e cerca de 80 MB de RAM a mais. Se voce usa Xbox/Game Pass, nao aplique.'
       Servicos=@(
           @{ Nome='XblAuthManager'; Inicio='Disabled' }
           @{ Nome='XblGameSave';     Inicio='Disabled' }
           @{ Nome='XboxGipSvc';      Inicio='Disabled' }
           @{ Nome='XboxNetApiSvc';   Inicio='Disabled' }
       )
    }

    @{ Id='jogo.rede.qos'; Categoria='Jogos'; Risco='Baixo'; Reinicio=$false
       Nome='Priorizar o tráfego de jogos na rede'
       Explicacao='O Windows limita por padrao a banda da rede para "trafego de fundo" (tarefas de sincronizacao, Windows Update) para nao atrapalhar a navegacao. Em jogos online, essa limitacao pode aumentar o ping. NetworkThrottlingIndex = FFFFFFFF desliga completamente esse limite de banda.'
       Ganho='Ping mais estavel e menos pacotes perdidos em horario de pico ou com muitos dispositivos na rede.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile'; Nome='NetworkThrottlingIndex'; Tipo='DWord'; Valor='0xFFFFFFFF' }
       )
    }
)
