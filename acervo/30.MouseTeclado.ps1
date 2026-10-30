# =====================================================================
#  Kairo Tweaker :: 30.MouseTeclado.ps1
#  ABA 3 - MOUSE E TECLADO
#  Tudo que afeta a resposta e o comportamento do mouse e do teclado.
# =====================================================================

Add-KTModule -Modulo 'MouseTeclado' -Tweaks @(

    @{ Id='mouse.precisao'; Categoria='Mouse e Teclado'; Risco='Baixo'; Reinicio=$false
       Nome='Remover a aceleração do mouse do Windows'
       Explicacao='O Windows aplica uma curva de aceleracao no movimento do ponteiro, que "puxa" o cursor mais rapido quando voce move o mouse devagar. Isso foi feito para trackballs antigos e incomoda em jogos, onde voce quer resposta 1:1. MouseThreshold1/2=0 e MouseSpeed=0 desativam essa correcao.'
       Ganho='Mira mais precisa e sem "puxao" no movimento. Nao afeta o mouse de jogos com driver proprio.'
       Spec=@(
           @{ Path='HKCU:\Control Panel\Mouse'; Nome='MouseThreshold1'; Tipo='String'; Valor='0' }
           @{ Path='HKCU:\Control Panel\Mouse'; Nome='MouseThreshold2'; Tipo='String'; Valor='0' }
           @{ Path='HKCU:\Control Panel\Mouse'; Nome='MouseSpeed';      Tipo='String'; Valor='0' }
       )
    }

    @{ Id='mouse.hover'; Categoria='Mouse e Teclado'; Risco='Baixo'; Reinicio=$false
       Nome='Reduzir o atraso de hover do mouse'
       Explicacao='Quando o cursor passa sobre um item (botao, link, icone), o Windows espera um tempo antes de acender a realce. Esse tempo e o MouseHoverTime, que vem em 500 ms por padrao e deixa a interface "morta". Aqui cai para 10 ms, deixando os menus e botoes reagirem quase imediatamente.'
       Ganho='Menus e submenus reagem mais rapido ao passar o mouse. Ajuda tambem em planilhas com muitas celulas.'
       Spec=@(
           @{ Path='HKCU:\Control Panel\Mouse'; Nome='MouseHoverTime'; Tipo='String'; Valor='10' }
       )
    }

    @{ Id='teclado.resposta'; Categoria='Mouse e Teclado'; Risco='Baixo'; Reinicio=$false
       Nome='Resposta imediata do teclado'
       Explicacao='O Windows pode aplicar um pequeno atraso de filtragem ao teclado para evitar teclas fantasma em circuitos electricos antigos. Em teclados modernos esse filtro e desnecessario e adiciona alguns milissegundos. Desligar faz cada tecla responder no instante em que e pressionada.'
       Ganho='Poucos milissegundos a menos de atraso na digitacao. Preocupacao apenas em teclados USB muito antigos.'
       Spec=@(
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Keyboard Response'; Nome='KeyboardResponseOn'; Tipo='DWord'; Valor='0' }
       )
    }

    @{ Id='teclado.tcp'; Categoria='Mouse e Teclado'; Risco='Baixo'; Reinicio=$true
       Nome='TCP sem atraso (afeta a resposta em jogos online)'
       Explicacao='O algoritmo de Nagle agrupa pequenos pacotes TCP para nao "encher" a rede, o que adiciona ate 200 ms de atraso a comandos pequenos e interativos. TcpNoDelay=1 faz o SO enviar cada comando assim que ele nasce, sem esperar. Como mouse e teclado enviam comandos constantemente pela rede em jogos online, desativar o Nagle deixa a resposta mais imediata.'
       Ganho='Resposta mais rapida em jogos online (menor latencia percebida).'
       Spec=@(
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces'; Nome='TcpNoDelay'; Tipo='DWord'; Valor='1' }
       )
    }

    @{ Id='mouse.bluetooth'; Categoria='Mouse e Teclado'; Risco='Baixo'; Reinicio=$false
       Nome='Reparar o Bluetooth (mouse e teclado sem fio)'
       Explicacao='Depois de otimizacoes mal feitas, o Bluetooth costuma parar de funcionar porque os servicos bthserv, BTAGService, BthAudioHF e BthHFSrv ficam comecando em "Manual" ou "Desativado". Esta opcao restaura os quatro servicos para iniciar automaticamente. E um reparo, nao uma otimizacao.'
       Ganho='Nenhum ganho de desempenho. Conserta mouse e teclado Bluetooth que sumiram do sistema.'
       Servicos=@(
           @{ Nome='bthserv';     Inicio='Automatic' }
           @{ Nome='BTAGService'; Inicio='Automatic' }
           @{ Nome='BthAudioHF';  Inicio='Automatic' }
           @{ Nome='BthHFSrv';    Inicio='Automatic' }
       )
    }
)
