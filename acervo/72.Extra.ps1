# =====================================================================
#  Kairo Tweaker :: 72.Extra.ps1
#  2a MINERACAO DO EXM (V10 completo): o que faltou na 1a passada.
#  Tarefas agendadas (40+), rede extrema, remocao do Edge, loggers.
#  So entra aqui o que passou no filtro de seguranca (nada de Nagle por
#  interface, heuristics, chimney/DCA — esses causaram stutter no PC real).
# =====================================================================

Add-KTModule -Modulo 'Extra' -Tweaks @(

    @{ Id='x2.tarefas.exm'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Desativar TODAS as tarefas de telemetria/diagnostico (EXM completo)'
       Explicacao='Origem: EXM V10 (blocos schtasks /end + /change). Desativa mais de 30 tarefas: CEIP (Consolidator, BthSQM, KernelCeipTask, UsbCeip, Uploader), Application Experience (Compatibility Appraiser, ProgramDataUpdater, StartupAppTask, AitAgent), DiskDiagnostic (Collector/Resolver), Power Efficiency Diagnostics, FamilySafety (3), Autochk Proxy, WinSAT, WER QueueReporting, CloudExperienceHost, DiskFootprint, PI Sqm-Tasks, NetTrace, SmartScreenSpecific, Office Telemetry (5), Time Synchronization (2), Automatic App Update, Device Information.'
       Ganho='Zero scans programados de telemetria; PC ocioso de verdade.'
       Comandos=@(
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Customer Experience Improvement Program\Consolidator" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Customer Experience Improvement Program\Consolidator" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Customer Experience Improvement Program\BthSQM" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Customer Experience Improvement Program\BthSQM" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Customer Experience Improvement Program\KernelCeipTask" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Customer Experience Improvement Program\KernelCeipTask" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Customer Experience Improvement Program\Uploader" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Customer Experience Improvement Program\Uploader" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Application Experience\ProgramDataUpdater" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Application Experience\ProgramDataUpdater" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Application Experience\StartupAppTask" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Application Experience\StartupAppTask" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Application Experience\AitAgent" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Application Experience\AitAgent" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticResolver" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticResolver" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Power Efficiency Diagnostics\AnalyzeSystem" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Power Efficiency Diagnostics\AnalyzeSystem" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Shell\FamilySafetyMonitor" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Shell\FamilySafetyMonitor" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Shell\FamilySafetyRefresh" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Shell\FamilySafetyRefresh" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Shell\FamilySafetyUpload" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Shell\FamilySafetyUpload" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Autochk\Proxy" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Autochk\Proxy" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Maintenance\WinSAT" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Maintenance\WinSAT" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Windows Error Reporting\QueueReporting" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Windows Error Reporting\QueueReporting" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\CloudExperienceHost\CreateObjectTask" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\CloudExperienceHost\CreateObjectTask" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\DiskFootprint\Diagnostics" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\DiskFootprint\Diagnostics" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\PI\Sqm-Tasks" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\PI\Sqm-Tasks" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\NetTrace\GatherNetworkInfo" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\NetTrace\GatherNetworkInfo" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\AppID\SmartScreenSpecific" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\AppID\SmartScreenSpecific" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Device Information\Device" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Device Information\Device" /Enable' }
       )
    }

    @{ Id='x2.rede.extrema'; Categoria='Rede e Internet'; Risco='Medio'; Reinicio=$true
       Nome='Rede EXTREMA (EXM): heuristics off, RSC off, timestamps off, taskoffload off'
       Explicacao='Origem: EXM V10 (bloco netsh agressivo). AVISO HONESTO: foi o unico bloco do EXM que causou sintomas reais no seu PC (ping instavel) e foi desfeito. Fica disponivel SO no perfil FPS Extreme. Desliga: tcp heuristics (window scaling automatico), RSC (receive segment coalescing), timestamps, ECN, taskoffload de IP, e seta CTCP. Autotuning permanece normal (desligar quebra throughput).'
       Ganho='Latencia teoricamente menor por pacote; teste em jogo. Se ping piorar, desmarque.'
       Comandos=@(
           @{ Cmd='netsh int tcp set heuristics disabled'; Rev='netsh int tcp set heuristics enabled' }
           @{ Cmd='netsh int tcp set global rsc=disabled'; Rev='netsh int tcp set global rsc=enabled' }
           @{ Cmd='netsh int tcp set global timestamps=disabled'; Rev='netsh int tcp set global timestamps=enabled' }
           @{ Cmd='netsh int tcp set global ecncapability=disabled'; Rev='netsh int tcp set global ecncapability=enabled' }
           @{ Cmd='netsh int ip set global taskoffload=disabled'; Rev='netsh int ip set global taskoffload=enabled' }
           @{ Cmd='netsh int tcp set supplemental internet congestionprovider=ctcp'; Rev='netsh int tcp set supplemental internet congestionprovider=default' }
           @{ Cmd='netsh int ip set global icmpredirects=disabled'; Rev='netsh int ip set global icmpredirects=enabled' }
       )
    }

    @{ Id='x2.edge.nuke'; Categoria='Otimização do Windows'; Risco='Alto'; Reinicio=$false
       Nome='Remover Microsoft Edge do disco (EXM: rd Edge*)'
       Explicacao='Origem: EXM V10 (rd /s /q Edge/EdgeCore/EdgeUpdate/EdgeWebView + taskkill). APAGA as pastas do Edge. Mais radical que o item abs.priv.edge (que so o torna desinstalavel). Risco: apps que usam WebView2 (Widgets, alguns launchers) deixam de renderizar ate reinstalar o Runtime. Nao marque se usa WebView2.'
       Ganho='Libera ~500 MB e mata processos residuais do Edge de vez.'
       Comandos=@(
           @{ Cmd='taskkill /f /im msedge.exe'; Rev='echo Reinstale o Edge em microsoft.com/edge' }
           @{ Cmd='cmd /c rd /s /q "C:\Program Files (x86)\Microsoft\Edge"'; Rev='echo Reinstale o Edge em microsoft.com/edge' }
           @{ Cmd='cmd /c rd /s /q "C:\Program Files (x86)\Microsoft\EdgeCore"'; Rev='echo idem' }
           @{ Cmd='cmd /c rd /s /q "C:\Program Files (x86)\Microsoft\EdgeUpdate"'; Rev='echo idem' }
           @{ Cmd='cmd /c rd /s /q "C:\Program Files (x86)\Microsoft\EdgeWebView"'; Rev='echo idem (WebView2)' }
       )
    }

    @{ Id='x2.wu.offload'; Categoria='Windows Update'; Risco='Baixo'; Reinicio=$true
       Nome='Task offload do TCP OFF (EXM) — variante antiga da rede extrema'
       Explicacao='Origem: EXM V6.1 (netsh int ip set global taskoffload=Disabled). Desativa a descarga de checksum/segmentacao para a placa de rede. Em NICs Realtek de consumo, o offload as vezes implementa mal e causa ping oscilante; em outras, desligar aumenta uso de CPU. Teste alternando este item se o ping no jogo oscilar.'
       Ganho='Alternativa de teste para problemas de latencia de rede.'
       Comandos=@(
           @{ Cmd='netsh int ip set global taskoffload=disabled'; Rev='netsh int ip set global taskoffload=enabled' }
       )
    }
)
