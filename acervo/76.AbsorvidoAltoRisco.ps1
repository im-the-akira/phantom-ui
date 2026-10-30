# =====================================================================
#  Kairo Tweaker :: 76.AbsorvidoAltoRisco.ps1
#  Itens que EXISTEM nos .reg do acervo mas causaram stutter REAL no PC
#  do dono (Ryzen 5700X): ficam disponiveis com RISCO ALTO e aviso forte.
#  Tambem: limpeza de registros de eventos (LimpezaCompletaPC.bat iGust).
# =====================================================================

Add-KTModule -Modulo 'AbsorvidoAlto' -Tweaks @(

    @{ Id='hi.largescache'; Categoria='Otimização do Windows'; Risco='Alto'; Reinicio=$true
       Nome='LargeSystemCache = 1 + SecondLevelDataCache (reg Ryzen/Memphis) - CUIDADO'
       Explicacao='Origem: "Habilitar cache L1 e L2 da CPU (Ryzen).reg" + Ajustes na memoria.reg + EXM. LargeSystemCache=1 prioriza cache de ARQUIVOS (servidor) em vez de processos; SecondLevelDataCache=0x100/0xC00 "declara" o L2 a CPU (o Windows detecta sozinho e o valor e ignorado em CPU moderna).'
       Problema='FOI REMOVIDO DO SEU PC EM 26/09 PORQUE CAUSOU TRAVAMENTOS: com LargeSystemCache=1 o Windows hesitava em descarregar file-cache, e o jogo disputava RAM com ele (stutter). SecondLevelDataCache nao faz nada em Ryzen. So reative se souber exatamente por que.'
       Spec=@(
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management'; Nome='LargeSystemCache';     Tipo='DWord'; Valor='1' }
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management'; Nome='SecondLevelDataCache'; Tipo='DWord'; Valor='0x100' }
       )
    }
    @{ Id='hi.clearpagefile'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='ClearPageFileAtShutdown = 1 (reg Ryzen/Memphis) - desligamento lento'
       Explicacao='Origem: "Habilitar cache L1 e L2.reg" + Ajustes na memoria.reg + EXM (opcao inversa do win.mem.pagefile). Zera o conteudo do pagefile a cada desligamento (seguranca forense: nada fica no disco).'
       Problema='Desligamento 5-30s mais lento (grava GBs no disco ao desligar). Primeiro boot apos desligar o jogo carrega pagefile do zero. Fica de fora do perfil por custo sem beneficio em casa.'
       Spec=@( @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management'; Nome='ClearPageFileAtShutdown'; Tipo='DWord'; Valor='1' } )
    }
    @{ Id='hi.evlog'; Categoria='Correção do Windows'; Risco='Medio'; Reinicio=$false
       Nome='Limpar TODOS os registros de eventos (LimpezaCompletaPC.bat iGust)'
       Explicacao='Origem: LimpezaCompletaPC.bat (iGust) — bloco wevtutil cl. Apaga System, Application, Security e todos os logs operacionais: util para "zerar" diagnosticos ou vender o PC.'
       Problema='VOCE PERDE O HISTORICO: nao podera investigar crash/blue-screen antigo nem quando comecou um problema. Diagnostico do app fica cego. Nao use rotineiramente.'
       Comandos=@(
           @{ Cmd='powershell -NoProfile -Command "wevtutil el | ForEach-Object { wevtutil cl $_ 2>$null }"'; Rev='echo Historico de eventos NAO pode ser restaurado' }
       )
    }
    @{ Id='hi.dllcache'; Categoria='Correção do Windows'; Risco='Medio'; Reinicio=$false
       Nome='Limpar dllcache + spool printers (LimpezaCompletaPC iGust)'
       Explicacao='Origem: LimpezaCompletaPC.bat (del dllcache, spool\printers). Libera ~200-400 MB do Windows\SoftwareDistribution-esque de copias de protecao.'
       Problema='dllcache e copia de protecao de DLLs: se apagar, o Windows repara pelo WinSxS (SFC demora mais). Impressoras travadas: limpar spool resolve fila presa.'
       Comandos=@(
           @{ Cmd='cmd /c del /s /f /q "%SystemRoot%\system32\dllcache\*.*"'; Rev='sfc /scannow' }
           @{ Cmd='cmd /c del /s /f /q "%SystemRoot%\system32\spool\printers\*.*"'; Rev='net stop spooler & net start spooler' }
       )
    }
)
