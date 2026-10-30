# =====================================================================
#  Kairo Tweaker :: 10.OtimizacaoWindows.ps1
#  ABA 1 - OTIMIZACAO DO WINDOWS
#  Tudo que mexe no funcionamento do sistema: memoria, CPU, disco,
#  agendamento, interface e servicos desnecessarios.
# =====================================================================

Add-KTModule -Modulo 'OtimizacaoWindows' -Tweaks @(

    @{ Id='win.mem.priority'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$true
       Nome='Reduzir a reserva de CPU do Windows'
       Explicacao='O Windows reserva de 10% a 20% da CPU para "Processamento do Sistema" e para agendar tarefas em primeiro plano. Essa reserva existe para manter o sistema responsivo. Em maquinas fracas ela significa menos ciclos livres para os seus aplicativos. Aqui ela cai para 10% (0x26), o valor recomendado pela propria Microsoft para Balancing.'
       Ganho='Navegacao e abertura de programas mais rapida em maquinas fracas. Ganho pequeno em PC potente.'
       Spec=@(
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\PriorityControl'; Nome='Win32PrioritySeparation'; Tipo='DWord'; Valor='0x26' }
       )
    }

    @{ Id='win.mem.pagefile'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Nao gravar o arquivo de paginacao ao desligar'
       Explicacao='ClearPageFileAtShutdown faz o Windows escrever todo o conteudo do pagefile (memoria virtual) no disco antes de desligar, para liberar os arquivos. Em um PC com 16 GB de RAM isso pode representar centenas de MB e atrasar o desligamento em varios segundos, sem nenhum beneficio real.'
       Ganho='Desligamento e reinicio mais rapidos (tipicamente 5 a 30 segundos).'
       Spec=@(
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management'; Nome='ClearPageFileAtShutdown'; Tipo='DWord'; Valor='0' }
       )
    }

    @{ Id='win.mem.mmcss'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Prioridade de audio e video do sistema (MMCSS)'
       Explicacao='O Windows classifica cada processo em uma "categoria" de QoS (MMCSS) e reserva parte da CPU para tarefas de fundo via SystemResponsiveness. Reduzir esse valor de 20 para 10 diminui a fatia que o Windows guarda para background, dando mais espaco para audio e video em tempo real, o que reduz chiado e cortes em jogos e gravacao de tela.'
       Ganho='Audio sem cortes e menos travadas em jogos, streams e gravacoes.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile'; Nome='SystemResponsiveness'; Tipo='DWord'; Valor='10' }
       )
    }

    @{ Id='win.cpu.multicore'; Categoria='Otimização do Windows'; Risco='Medio'; Reinicio=$true
       Nome='Usar todos os nucleos do processador'
       Explicacao='Em CPUs hibridas (Intel 12a geracao+ e AMD com nucleos economicos), o Windows as vezes escala a thread principal para um nucleo secundario, achando que e "mais rapido" por ser mais novo, quando na verdade ele e mais lento. Esta opcao desliga o escalonamento automatico e instrui o Windows a considerar todos os nucleos igualmente.'
       Ganho='Menos travadas e "1% low" melhorado em jogos em CPUs hibridas. Sem efeito se seu processador nao tem nucleos hibridos.'
       Bcd=@(
           @{ Nome='x2apicpolicy'; Valor='Enable' }
       )
    }

    @{ Id='win.cpu.timers'; Categoria='Otimização do Windows'; Risco='Medio'; Reinicio=$true
       Nome='Ajustar o relogio do sistema (timer)'
       Explicacao='O Windows usa um relogio de 15,625 ms por padrao para agendar tarefas. O ajuste mantem o relogio alinhado ao hardware e desliga o ajuste automatico de frequencia. Em teoria reduz a latencia entre o seu comando e a resposta na tela. Este ajuste e muito citado em guias de otimizacao, mas o ganho real raramente passa de 1%.'
       Ganho='Latencia de resposta teoricamente menor. Ganho real raramente acima de 1%.'
       Bcd=@(
           @{ Nome='useplatformclock';   Valor='yes' }
           @{ Nome='useplatformtick';    Valor='yes' }
           @{ Nome='disabledynamictick'; Valor='yes' }
       )
    }

    @{ Id='win.disk.nvme'; Categoria='Otimização do Windows'; Risco='Medio'; Reinicio=$true
       Nome='Aumentar a fila de comandos de disco (NVMe)'
       Explicacao='IRPStackSize define quantos pedidos de disco podem ficar na fila ao mesmo tempo. O padrao do Windows e 15. Em discos NVMe rapidos, uma fila maior permite que o controlador NVMe processe mais operacoes em paralelo, o que ajuda bastante em cenas com muitas leituras pequenas (abrir nivel no jogo, carregar mapa). Aqui vai para 20 - um aumento moderado, nao o valor radical de 30 que circula pela internet.'
       Ganho='Carregamento de nivel, mapa e texturas um pouco mais rapido em NVMe.'
       Spec=@(
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management'; Nome='IRPStackSize'; Tipo='DWord'; Valor='0x14' }
       )
    }
)

Add-KTModule -Modulo 'OtimizacaoWindows' -Tweaks @(

    @{ Id='win.ui.animations'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Desligar as animações da interface'
       Explicacao='Cada janela, pasta e menu e desenhado com uma animacao de abertura, que exige que a GPU desenhe varios quadros intermediarios. Desligar (MinAnimate=0, VisualFXSetting=Ajustar para o melhor desempenho) faz o Windows pular direto para o desenho final. Tambem reduz o atraso do menu do botao direito de 400 ms para 10 ms.'
       Ganho='Abas, janelas e menus abrem instantaneamente. A interface fica com visual mais "seco".'
       Spec=@(
           @{ Path='HKCU:\Control Panel\Desktop\WindowMetrics';                             Nome='MinAnimate';        Tipo='String'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects'; Nome='VisualFXSetting';  Tipo='DWord';  Valor='2' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced';      Nome='TaskbarAnimations'; Tipo='DWord';  Valor='0' }
           @{ Path='HKCU:\Control Panel\Desktop';                                          Nome='MenuShowDelay';     Tipo='String'; Valor='10' }
       )
    }

    @{ Id='win.ui.fontsmoothing'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$true
       Nome='Desligar o suavizado de fonte do Windows'
       Explicacao='O ClearType suaviza as bordas das letras para a tela do monitor. Custa um pouco de CPU e GPU para desenhar cada texto. Em resolucoes altas em telas pequenas, muita gente prefere o texto sem suavizado porque fica mais nitido. Aqui tambem desliga o ajuste automatico de desempenho da interface (SoftLanding), que reduz efeitos visuais quando o sistema esta sob carga.'
       Ganho='Texto menos suavizado (opcional). Pequena economia de CPU e GPU no desktop.'
       Spec=@(
           @{ Path='HKCU:\Control Panel\Desktop';               Nome='FontSmoothing';      Tipo='String'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Avalon.Graphics'; Nome='SoftLandingEnabled'; Tipo='DWord';  Valor='0' }
       )
    }

    @{ Id='win.search.index'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Desligar a indexação de arquivos do Windows'
       Explicacao='O Windows Search mantem um indice com o nome e o conteudo de todos os seus arquivos para a busca do menu Iniciar. Esse indice e reconstruido continuamente: mesmo com o PC ocioso, o servico WSearch le o disco e usa CPU. Se voce nao usa a busca do Windows para achar arquivos, esse trabalho e puro desperdicio.'
       Ganho='Menos uso de disco e CPU em segundo plano. Menu Iniciar mais rapido.'
       Spec=@(
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Services\WSearch'; Nome='Start'; Tipo='DWord'; Valor='4' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search'; Nome='AllowIndexing'; Tipo='DWord'; Valor='0' }
       )
    }

    @{ Id='win.startup.services'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Desligar serviços do Windows que você não usa'
       Explicacao='Varios servicos sobem no boot mesmo sem uso: Fax, Mapas offline, demonstracao de loja, teclado touch, telefone, Windows Insider e rastreamento de localizacao. Cada um consome um pouco de RAM e, principalmente, acorda o disco. Desligar nao quebra nada em um PC domestico normal.'
       Ganho='Boot mais rapido, menos processos em segundo plano e menos uso de disco ocioso.'
       Servicos=@(
           @{ Nome='Fax';                Inicio='Disabled' }
           @{ Nome='MapsBroker';         Inicio='Disabled' }
           @{ Nome='RetailDemo';         Inicio='Disabled' }
           @{ Nome='TabletInputService'; Inicio='Disabled' }
           @{ Nome='PhoneSvc';           Inicio='Disabled' }
           @{ Nome='wisvc';              Inicio='Disabled' }
           @{ Nome='WaaSMedicSvc';       Inicio='Disabled' }
           @{ Nome='lfsvc';              Inicio='Disabled' }
       )
    }

    @{ Id='win.sysmain'; Categoria='Otimização do Windows'; Risco='Medio'; Reinicio=$true
       Nome='Desligar o SysMain (Superfetch) - apenas em SSD'
       Explicacao='O SysMain foi criado para discos lentos: antecipava a leitura dos arquivos que voce provavelmente abriria, para o disco nao ter tempo de girar. Em um SSD, que entrega dados em menos de 0,1 ms, essa antecipacao nao adianta - e o servico passa a fazer leitura de disco constante sem ganho. Em HD mecanico, DESLIGAR ISSO DEIXA O PC MAIS LENTO.'
       Ganho='Menos I/O constante em SSD. Nao use em HD mecanico - piora o desempenho.'
       Servicos=@(
           @{ Nome='SysMain'; Inicio='Disabled' }
       )
    }

    @{ Id='win.compactacao'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Desligar a compactação de arquivos em segundo plano'
       Explicacao='O PcaSvc observa seus arquivos e, periodicamente, compacta os maiores em background para economizar espaco. Como o proprio NTFS ja comprime, essa tarefa costuma apenas consumir CPU e disco sem liberar espaco util.'
       Ganho='Menos consumo de CPU e disco ao longo do dia. Nao ha ganho de velocidade.'
       Servicos=@(
           @{ Nome='PcaSvc'; Inicio='Disabled' }
       )
    }

    @{ Id='win.shutdown.rapido'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$true
       Nome='Encurtar o encerramento do Windows'
       Explicacao='Por padrao, o Windows espera 1,5 segundo antes de encerrar e a barra "Encerrando" fica visivel por alguns segundos. WaitToKillServiceTimeout reduz a espera dos servicos e AutoEndTasks encerra aplicativos imediatamente.'
       Ganho='Desligamento e reinicio alguns segundos mais rapidos.'
       Spec=@(
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control'; Nome='WaitToKillServiceTimeout'; Tipo='String'; Valor='2000' }
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager'; Nome='AutoEndTasks'; Tipo='String'; Valor='1' }
       )
    }

    @{ Id='win.console.utf8'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Forçar UTF-8 no console do Windows'
       Explicacao='O console (CMD e PowerShell) usa a pagina de codigo 850 por padrao, que nao possui os caracteres de acento corretos. Ao exibir acentos, aparece "a§", "a~". Forcar 65001 (UTF-8) resolve isso e e a correcao correta para scripts em portugues.'
       Ganho='Nenhum ganho de desempenho. Corrige acentos quebrados em scripts e no console.'
       Spec=@(
           @{ Path='HKCU:\Console'; Nome='OutputCodePage'; Tipo='DWord'; Valor='65001' }
       )
    }

    @{ Id='win.menu.iniciar'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$true
       Nome='Restaurar o menu Iniciar e a barra de tarefas'
       Explicacao='Muitos programas de otimizacao e versoes do Windows escondem a barra de tarefas ou trocam o menu Iniciar. Estas chaves devolvem o comportamento padrao do Windows 10/11.'
       Ganho='Nenhum ganho de desempenho. Volta a barra de tarefas e o menu Iniciar ao padrao.'
       Spec=@(
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer'; Nome='NoStart';  Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer'; Nome='NoStart2d'; Tipo='DWord'; Valor='0' }
       )
    }
)
