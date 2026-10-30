# =====================================================================
#  Kairo Tweaker :: 45.WindowsUpdate.ps1
#  ABA 7 - WINDOWS UPDATE
#
#  Tudo aqui e REVERSIVEL: cada chave de registro e cada alteracao de
#  tarefa agendada e gravada no journal antes de ser modificada, e a
#  aba "Desfazer" restaura exatamente o estado anterior.
#
#  Tecnicas absorvidas de:
#   - 3 - Regedits\Disable Automatic Driver Installation.reg (AtlasOS)
#   - 6 - Otimizacao de jogos\Pack Fivem\.bats\*.bat
#   - 4 - Scripts\EXM Free Tweaking Utility (secoes de Windows Update)
#   - 4 - Scripts\atlasOS\Disable Automatic Driver Installation.reg
# =====================================================================

# --- Helpers de tarefa agendada -----------------------------------------

function Set-KTTarefa {
    <#  Habilita (1) ou desabilita (2) uma tarefa agendada, com reversao.  #>
    param(
        [Parameter(Mandatory)][string]$Nome,
        [ValidateSet('Habilitar','Desabilitar')][string]$Acao
    )
    $existe = (& schtasks.exe /query /tn $Nome 2>&1) -notmatch 'ERROR|Não existe'
    if (-not $existe) {
        Write-KTLog "Tarefa nao existe neste Windows: $Nome" 'AVISO' -SemTela
        return $false
    }
    $antes = $null
    $saida = & schtasks.exe /query /tn $Nome /fo LIST /v 2>$null
    if ($saida -match '(?m)^\s*Status\s*:\s*(.+?)\s*$') { $antes = $Matches[1].Trim() }

    Add-KTJournal -Entry @{
        Tipo = 'Tarefa'; Path = $Nome; Nome = $Nome
        Existia = $true; TipoValor = ''; Valor = $(if ($antes -eq 'Disabled') { 'Desabilitada' } else { 'Habilitada' })
    }

    $null = & schtasks.exe /change /tn $Nome $(if ($Acao -eq 'Desabilitar') { '/Disable' } else { '/Enable' }) 2>&1
    return $true
}

# =====================================================================
#  CATALOGO
#  Risco: "Baixo" = so desliga atualizacoes opcionais.
#         "Medio" = impede atualizacoes de seguranca (use com consciencia).
# =====================================================================
Add-KTModule -Modulo 'WindowsUpdate' -Tweaks @(

    @{ Id='wu.bloquear.drivers'; Categoria='Windows Update'; Risco='Baixo'; Reinicio=$false
       Nome='Impedir que drivers venham do Windows Update'
       Explicacao='O Windows Update tambem busca drivers de hardware. Em um PC montado, ele costuma oferecer um driver generico inferior ao do fabricante, o que derruba desempenho (principalmente video). Esta opcao diz ao Windows para nunca baixar driver do driverstore, forcando uso do driver que voce ja instalou.'
       Ganho='Nao ha ganho direto. Evita que um driver generico seja instalado e fique por meses. O driver continua sendo o que voce ja tem.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Microsoft\PolicyManager\current\device\Update';    Nome='ExcludeWUDriversInQualityUpdate'; Tipo='DWord'; Valor='1' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\PolicyManager\default\Update';          Nome='ExcludeWUDriversInQualityUpdate'; Tipo='DWord'; Valor='1' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings';             Nome='ExcludeWUDriversInQualityUpdate'; Tipo='DWord'; Valor='1' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate';       Nome='ExcludeWUDriversInQualityUpdate'; Tipo='DWord'; Valor='1' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Device Metadata'; Nome='PreventDeviceMetadataFromNetwork'; Tipo='DWord'; Valor='1' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching'; Nome='SearchOrderConfig';              Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching'; Nome='DontSearchWindowsUpdate';        Tipo='DWord'; Valor='1' }
       )
    }

    @{ Id='wu.bloquear.reinicio'; Categoria='Windows Update'; Risco='Baixo'; Reinicio=$false
       Nome='Impedir o reinício automático do Windows'
       Explicacao='Depois de instalar uma atualizacao, o Windows pode marcar uma hora e REINICIAR o PC sozinho, mesmo com voce jogando ou trabalhando. NoAutoRebootWithLoggedOnUsers impede o reinicio automatico quando ha usuario logado. As atualizacoes ainda sao instaladas; apenas param de interromper voce.'
       Ganho='Nenhum ganho de desempenho. Evita perder a partida ou o trabalho por causa de um reinicio forcado.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU';  Nome='NoAutoRebootWithLoggedOnUsers'; Tipo='DWord'; Valor='1' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings';           Nome='UxOption';                       Tipo='DWord'; Valor='1' }
       )
    }

    @{ Id='wu.bloquear.mesmo'; Categoria='Windows Update'; Risco='Baixo'; Reinicio=$false
       Nome='Parar de avisar sobre atualizações (só notificação)'
       Explicacao='Desliga apenas os avisos de "ha atualizacao disponivel" nas Configuracoes e na notificacao do sistema. As atualizacoes continuam acontecendo normalmente em segundo plano. Diferente de bloquear a atualizacao: aqui so o aviso some.'
       Ganho='Nenhum ganho de desempenho. Some o aviso de atualizacao. As atualizacoes continuam.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU';      Nome='NoAutoUpdate';          Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings';               Nome='ShowPersistentUXToast';  Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings';               Nome='UIBranch';               Tipo='String'; Valor='4' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update'; Nome='ShowUIEnabledAtStartup'; Tipo='DWord'; Valor='0' }
       )
    }

    @{ Id='wu.bloquear.insider'; Categoria='Windows Update'; Risco='Baixo'; Reinicio=$false
       Nome='Impedir inscrição no Windows Insider'
       Explicacao='Se voce clicou por engano em "Insider Preview" e comecou a receber builds de teste, esta opcao volta para o canal estavel e trava o canal em Release. Sem isso, a build de teste pode ficar travada no sistema por meses, sem direito a suporte.'
       Ganho='Nenhum ganho. Volta a build estavel e impede inscricao acidental em builds de teste.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsSelfHost\Applicability';     Nome='BranchName';  Tipo='String'; Valor='Release' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsSelfHost\Applicability';     Nome='ContentType'; Tipo='String'; Valor='Release' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsSelfHost\Applicability';     Nome='Ring';        Tipo='String'; Valor='Retail' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsSelfHost\UI\Selection';       Nome='UIBranch';    Tipo='String'; Valor='Release' }
       )
    }

    @{ Id='wu.bloquear.delivery'; Categoria='Windows Update'; Risco='Baixo'; Reinicio=$false
       Nome='Desligar a Otimização de Entrega (Delivery Optimization)'
       Explicacao='O Delivery Optimization usa a sua banda de internet para entregar atualizacoes de outros usuarios da Microsoft, como torrent. Se voce tem internet limitada (dados moveis), isso consome seus dados sem voce usar. DODownloadMode=100 faz o Windows baixar so o que voce precisa.'
       Ganho='Para de usar seus dados moveis para baixar atualizacao de outros. Nao afeta a velocidade do PC.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization';                 Nome='DODownloadMode';       Tipo='DWord'; Valor='100' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization';                 Nome='DODownloadModePolicy'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config';    Nome='DODownloadMode';       Tipo='DWord'; Valor='100' }
       )
    }

    @{ Id='wu.bloquear.servicos'; Categoria='Windows Update'; Risco='Medio'; Reinicio=$true
       Nome='Desligar os serviços que aplicam atualizações'
       Explicacao='Desliga o Windows Update (wuauserv), o Orchestrator que agenda as tarefas, o WaaSMedic que forca a reavaliacao, e o DoSvc de entrega. Com isso o Windows para de PROCURAR e BAIXAR atualizacoes. ATENCAO: o sistema deixa de receber correcoes de seguranca, o que deixa o PC mais vulneravel. Use apenas se voce sabe o que esta fazendo e tem um plano de atualizacao manual.'
       Ganho='Nenhum ganho de velocidade. O PC para de buscar atualizacao em segundo plano (some o uso de rede e disco do Windows Update). Em troca, voce perde as correcoes de seguranca.'
       Servicos=@(
           @{ Nome='wuauserv'; Inicio='Disabled' }
           @{ Nome='UsoSvc';   Inicio='Disabled' }
           @{ Nome='WaaSMedicSvc'; Inicio='Disabled' }
           @{ Nome='DoSvc';    Inicio='Disabled' }
       )
    }

    @{ Id='wu.bloquear.tarefas'; Categoria='Windows Update'; Risco='Medio'; Reinicio=$false
       Nome='Desativar as tarefas agendadas do Windows Update'
       Explicacao='Desliga as tarefas agendadas que o Windows usa para varrer e instalar atualizacoes em horario ocioso. Estas tarefas acordam a maquina e usam disco mesmo com voce parado. ATENCAO: sem atualizacao automatica, o sistema nao corrige vulnerabilidades sozinho.'
       Ganho='Nenhum ganho de velocidade. O PC para de varrer por atualizacoes em horario ocioso, o que reduz o uso de disco.'
       Executar='wu-tarefas'
    }
)

# =====================================================================
#  ACOES (executadas pelo motor, com journal)
# =====================================================================

# Tarefas agendadas do Windows Update. Reversao: cada tarefa volta ao
# estado que o journal registrou (Habilitada ou Desabilitada).
$global:KT_WU_Tarefas = @(
    '\Microsoft\Windows\WindowsUpdate\Scheduled Start'
    '\Microsoft\Windows\WindowsUpdate\Refresh Group Policy Cache'
    '\Microsoft\Windows\Update\Orchestrator\Schedule Scan'
    '\Microsoft\Windows\Update\Orchestrator\Schedule Scan Static Task'
    '\Microsoft\Windows\Update\Orchestrator\UScheduler_Oobe\COveTask'
    '\Microsoft\Windows\Update\Orchestrator\MaintenanceInstall'
    '\Microsoft\Windows\Update\Orchestrator\MaintenanceInstallTask'
    '\Microsoft\Windows\Update\Orchestrator\Reboot'
    '\Microsoft\Windows\Update\Orchestrator\UScheduler_UxDaily'
    '\Microsoft\Windows\InstallService\ScanForUpdates'
)

function Invoke-KTTarefasUpdate {
    param([ValidateSet('Desabilitar','Habilitar')][string]$Acao = 'Desabilitar')
    $ok = 0; $falha = 0
    foreach ($t in $global:KT_WU_Tarefas) {
        if (Set-KTTarefa -Nome $t -Acao $Acao) { $ok++ } else { $falha++ }
    }
    Write-KTLog "Tarefas do Windows Update: $Acao - aplicadas: $ok | indisponiveis: $falha" 'OK'
    return $ok
}

$global:KT_Acoes['wu-tarefas'] = {
    param($Silencioso)
    if ($env:KT_CMD_REVERSA -eq '1') { Invoke-KTTarefasUpdate -Acao 'Habilitar' }
    else { Invoke-KTTarefasUpdate -Acao 'Desabilitar' }
}

# ---------------------------------------------------------------------
#  DIAGNOSTICO DO WINDOWS UPDATE
# ---------------------------------------------------------------------
function Show-KTStatusUpdate {
    Write-Host ''
    Write-Host '  WINDOWS UPDATE - SITUACAO ATUAL' -ForegroundColor Cyan
    Write-Host ('  ' + ('=' * 66)) -ForegroundColor DarkGray
    Write-Host ('  {0,-24} {1}' -f 'Servicos', '') -NoNewline
    $svc = @('wuauserv','UsoSvc','WaaSMedicSvc','DoSvc','BITS')
    $lista = @()
    foreach ($s in $svc) {
        $o = Get-Service $s -ErrorAction SilentlyContinue
        if ($o) { $lista += "$s=$($o.StartType)" }
    }
    Write-Host ($lista -join '  ') -ForegroundColor Gray

    Write-Host ('  {0,-24} {1}' -f 'Politicas ativas', '') -NoNewline
    $p = @()
    foreach ($k in @(
        @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate'; Nome='ExcludeWUDriversInQualityUpdate' },
        @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU';   Nome='NoAutoRebootWithLoggedOnUsers' },
        @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching'; Nome='DontSearchWindowsUpdate' })) {
        $e = Get-KTRegValue -Path $k.Path -Nome $k.Nome
        if ($e.Existia -and $e.Valor -eq 1) { $p += $k.Nome }
    }
    if ($p.Count) { Write-Host ($p -join ', ') -ForegroundColor Yellow }
    else { Write-Host 'nenhuma' -ForegroundColor DarkGray }

    Write-Host ('  {0,-24} {1}' -f 'Tarefas agendadas', '') -NoNewline
    $desab = 0
    foreach ($t in $global:KT_WU_Tarefas) {
        $o = & schtasks.exe /query /tn $t 2>&1 | Out-String
        if ($o -notmatch 'ERROR|Não existe' -and $o -match '(?m)Status\s*:\s*Disabled') { $desab++ }
    }
    Write-Host ("$desab de $($global:KT_WU_Tarefas.Count) desabilitadas") -ForegroundColor Gray
    Write-Host ('  ' + ('=' * 66)) -ForegroundColor DarkGray
    Write-Host ''
}
