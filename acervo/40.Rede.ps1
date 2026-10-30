# =====================================================================
#  Kairo Tweaker :: 40.Rede.ps1
#  ABA 4 - REDE E INTERNET
# =====================================================================

Add-KTModule -Modulo 'Rede' -Tweaks @(

    @{ Id='rede.dns'; Categoria='Rede e Internet'; Risco='Baixo'; Reinicio=$false
       Nome='Limpar o cache DNS (corrige "sem internet" na VPN)'
       Explicacao='Quando voce conecta em uma VPN, o Windows continua tentando resolver os enderecos pelos servidores DNS antigos, o que causa o erro "sem internet". O cache DNS guarda esses resultados por ate 24 horas. Limpar o cache e a solucao classica e mais rapida do que o gerenciador de rede do Windows.'
       Ganho='Corrige imediatamente o erro "sem internet" ao conectar em VPN. Ganho de velocidade: apenas instantaneo.'
       Comandos=@(
           @{ Cmd='ipconfig /flushdns'; Rev='echo nada a reverter: limpar o cache e sempre seguro' }
       )
    }

    @{ Id='rede.timewait'; Categoria='Rede e Internet'; Risco='Baixo'; Reinicio=$true
       Nome='Liberar portas de conexão mais rápido'
       Explicacao='Depois que uma conexao TCP fecha, o Windows coloca a porta em estado TIME_WAIT por 120 segundos, para evitar confusao de pacotes atrasados. Em servidores de jogos com limite de portas, sair e voltar a conectar em 2 minutos falha por falta de portas livres. Reduzir para 30 segundos libera as portas 4x mais rapido.'
       Ganho='Reconexao em servidores com limite de portas bem mais rapida apos sair de um jogo.'
       Spec=@(
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters'; Nome='TcpTimedWaitDelay'; Tipo='DWord'; Valor='30' }
       )
    }

    @{ Id='rede.descoberta'; Categoria='Rede e Internet'; Risco='Baixo'; Reinicio=$false
       Nome='Detectar mudanças de rede mais rápido'
       Explicacao='Por padrao, o Windows espera alguns segundos para detectar se o cabo foi desconectado, se o Wi-Fi trocou de rede ou se o roteador voltou. Esses ajustes reduzem essa espera, fazendo o sistema reencontrar a rede mais rapido quando ela oscila. Ajuda em chamadas de voz e transmissao ao vivo, que travam quando a rede demora para voltar.'
       Ganho='Reconexao de rede mais rapida quando o Wi-Fi ou o cabo oscila. Estabilidade em chamada e streaming.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\NetworkConnectivityConfigIndicator'; Nome='NoAutoRelease'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters';               Nome='AutoShareWks'; Tipo='DWord'; Valor='0' }
       )
    }
)
