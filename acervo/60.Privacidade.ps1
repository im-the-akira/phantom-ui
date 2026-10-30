# =====================================================================
#  Kairo Tweaker :: 60.Privacidade.ps1
#  ABA 6 - PRIVACIDADE E TELEDADOS
# =====================================================================

Add-KTModule -Modulo 'Privacidade' -Tweaks @(

    @{ Id='priv.telemetria'; Categoria='Privacidade e Telemetria'; Risco='Baixo'; Reinicio=$false
       Nome='Reduzir a telemetria do Windows ao mínimo'
       Explicacao='O Windows coleta dados de uso e diagnostico e os envia para a Microsoft. Isso exige um servico rodando em segundo plano (DiagTrack) que acorda o disco e usa rede. AllowTelemetry=0 desliga esse envio. Isso NAO e desbloqueio de recursos, e realmente reducao do que sai da sua maquina.'
       Ganho='Menos uso de rede, disco e CPU em segundo plano. Privacidade real. Nao ha ganho de FPS.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection';                Nome='AllowTelemetry'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection';  Nome='AllowTelemetry'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\PolicyManager\current\device\CloudContent'; Nome='DisableTelemetry'; Tipo='DWord'; Valor='1' }
       )
    }

    @{ Id='priv.servicos.telemetria'; Categoria='Privacidade e Telemetria'; Risco='Baixo'; Reinicio=$false
       Nome='Desligar os serviços que enviam dados do seu PC'
       Explicacao='Desliga o DiagTrack (Connected User Experiences), que envia tudo que o Windows observa do seu uso, e o dmwappushservice, que envia dados de diagnostico. Desligar esses dois servicos corta na raiz a maior parte da coleta automatica.'
       Ganho='Menos processos de segundo plano e nenhuma coleta de uso. Nao ha ganho de FPS.'
       Servicos=@(
           @{ Nome='DiagTrack';        Inicio='Disabled' }
           @{ Nome='dmwappushservice'; Inicio='Disabled' }
           @{ Nome='WerSvc';           Inicio='Disabled' }
       )
    }

    @{ Id='priv.ads'; Categoria='Privacidade e Telemetria'; Risco='Baixo'; Reinicio=$false
       Nome='Desligar os anúncios do menu Iniciar e do Windows'
       Explicacao='O Windows exibe anuncios e sugestoes de aplicativos no menu Iniciar e na area de noticias. Esses anuncios sao de terceiros, nao tem relacao com defender ou com o seu computador estar lento. Desligar so remove os anuncios; nao afeta o Edge instalado separadamente.'
       Ganho='Menu Iniciar mais limpo e sem anuncios. Nao ha ganho de desempenho.'
       Spec=@(
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced';     Nome='ShowSyncNotifications'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager'; Nome='SilentInstalledAppsEnabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager'; Nome='SystemPaneSuggestionsEnabled';  Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo';              Nome='DisabledByGroupPolicy';       Tipo='DWord'; Valor='1' }
       )
    }

    @{ Id='priv.cortana'; Categoria='Privacidade e Telemetria'; Risco='Baixo'; Reinicio=$true
       Nome='Desligar a Cortana, a busca web e as sugestões'
       Explicacao='A Cortana envia sua voz e suas buscas para os servidores da Microsoft. No menu Iniciar, a "busca" tambem consulta a web quando o Windows nao acha o resultado local. Desligar Remove esses envios e o icone da Cortana, deixando o menu Iniciar mais leve. Se voce nao usa a busca do Windows, nao perde nada.'
       Ganho='Menos consultas de rede ao digitar no menu Iniciar. Privacidade real. Nao ha ganho de FPS.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search'; Nome='AllowCortana';   Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search'; Nome='DisableWebSearch'; Tipo='DWord'; Valor='1' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\Explorer';      Nome='DisableSearchBoxSuggestions'; Tipo='DWord'; Valor='1' }
       )
    }

    @{ Id='priv.feedback'; Categoria='Privacidade e Telemetria'; Risco='Baixo'; Reinicio=$false
       Nome='Desligar o programa de avaliação de experiência'
       Explicacao='O Windows periodicamente pergunta "como esta sendo sua experiencia?" e coleta dados de uso do seu computador. Desligar o Windows CEIP (Customer Experience Improvement Program) encerra esse envio e as janelas de pergunta que aparecem sozinhas.'
       Ganho='Nenhuma pergunta de avaliacao aparecendo. Privacidade real. Nao ha ganho de desempenho.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting'; Nome='Disabled';    Tipo='DWord'; Valor='1' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\SQM\Windows';                     Nome='WindowsCEIPEnable'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Siuf\Rules';                               Nome='NumberOfSIUFInPeriod'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System';   Nome='EnableActivityFeed'; Tipo='DWord'; Valor='0' }
       )
    }
)
