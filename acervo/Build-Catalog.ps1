<#
.SYNOPSIS
    Le ops.json + tweaks.json (gerados por Extract-ScriptOps.ps1), classifica as
    operacoes nas 12 categorias + Reverts do menu do EXM V10 e escreve o
    catalogo final em Markdown.

.USAGE
    powershell -NoProfile -ExecutionPolicy Bypass -File Build-Catalog.ps1
#>
[CmdletBinding()]
param(
    [string]$Root    = '',
    [string]$InDir   = '',
    [string]$OutFile = '',
    [string]$Diag    = ''
)

$__here = if ($PSCommandPath) { Split-Path -Parent $PSCommandPath } else { (Get-Location).Path }
if ([string]::IsNullOrWhiteSpace($Root))    { $Root  = Split-Path -Parent $__here }
if ([string]::IsNullOrWhiteSpace($InDir))   { $InDir = $__here }
$Root = (Resolve-Path -LiteralPath $Root).Path.TrimEnd('\')
if ([string]::IsNullOrWhiteSpace($OutFile)) { $OutFile = Join-Path $Root 'ACERVO-DE-TWEAKS_EXM-V10.md' }

$ErrorActionPreference = 'Continue'


# Parser de menus do EXM (abas, opcoes e corpo de cada opcao) - ver Exm-Mapa.ps1
. (Join-Path $PSScriptRoot 'Exm-Mapa.ps1')
# =====================================================================
#  CATEGORIAS (espelho do menu principal do EXM V10)
# =====================================================================
$script:Cats = [ordered]@{
    '01' = @{ N='General System Optimizations'; PT='Otimizacoes gerais do sistema';     E=[char]::ConvertFromUtf32(0x1F527) }
    '02' = @{ N='Power Optimizations';          PT='Energia e suspensao';               E=[char]::ConvertFromUtf32(0x1F50B) }
    '03' = @{ N='Keyboard and Mouse';           PT='Teclado e mouse';                   E=[char]::ConvertFromUtf32(0x1F5B1) }
    '04' = @{ N='GPU Optimizations';            PT='Placa de video e jogos';            E=[char]::ConvertFromUtf32(0x1F3A8) }
    '05' = @{ N='CPU Optimizations';            PT='Processador';                       E=[char]::ConvertFromUtf32(0x1F532) }
    '06' = @{ N='PC clean';                     PT='Limpeza do PC';                     E=[char]::ConvertFromUtf32(0x1F9F9) }
    '07' = @{ N='System Debloat';               PT='Debloat, privacidade e telemetria'; E=[char]::ConvertFromUtf32(0x1F9FC) }
    '08' = @{ N='Storage Optimizations';        PT='Armazenamento e disco';             E=[char]::ConvertFromUtf32(0x1F5C3) }
    '09' = @{ N='Memory Optimizations';         PT='Memoria RAM';                       E=[char]::ConvertFromUtf32(0x1F4BE) }
    '10' = @{ N='Additional / QOL';             PT='Extras e qualidade de vida';        E=[char]::ConvertFromUtf32(0x2795) }
    '11' = @{ N='Uninstall Useless Apps';       PT='Remocao de aplicativos';            E=[char]::ConvertFromUtf32(0x1F5D1) }
    '12' = @{ N='Network Tweaking';             PT='Rede e internet';                   E=[char]::ConvertFromUtf32(0x1F310) }
    'F'  = @{ N='Fixes and Reverts';            PT='Correcoes e reversoes';             E=[char]::ConvertFromUtf32(0x1F6E0) }
}

# Categorias nativas do Kairo Tweaker -> categorias EXM V10
$script:KtMap = @{
    'Otimização do Windows'    = '01'
    'Privacidade e Telemetria' = '07'
    'Rede e Internet'          = '12'
    'Windows Update'           = '10'
    'Energia e Desempenho'     = '02'
    'Jogos'                    = '04'
    'Jogos por Título'         = '04'
    'Correção do Windows'      = 'F'
    'Mouse e Teclado'          = '03'
    'Ativação'                 = '10'
}

# =====================================================================
#  CLASSIFICADOR
#  Cada regra testa o alvo normalizado = "Path\Name | Data | Raw" em minusculas.
#  A primeira regra que casar define a categoria.
# =====================================================================
$script:Rules = New-Object System.Collections.Generic.List[object]
function Add-Rule { param([string]$C, [string]$Re) $script:Rules.Add([pscustomobject]@{ C=$C; Re=$Re.ToLowerInvariant() }) }

# --- 03 Teclado e mouse -------------------------------------------------
Add-Rule '03' 'services\\(kbdclass|mouclass|kbdhid|mouhid|hidusb|hidserv)'
Add-Rule '03' 'control panel\\(mouse|keyboard|accessibility\\(keyboard response|togglekeys|stickykeys|soundssentry|filterkeys))'
Add-Rule '03' 'keyboarddelay|keyboardspeed|keyboardbubbletime|mousehover|mouseclicklock|mousekeys|stickykeys|togglekeys|soundssentry|filterkeys'
Add-Rule '03' 'pointerprecision|enhancepointer|pointercontrolpanel|cursorsettings|cursorschemes|\\cursors\\|\\penworkspace|\\touchpad'
Add-Rule '03' 'mousedataqueuesize|keyboarddataqueuesize|mousebuffer_size|keyboardbuffer_size'
Add-Rule '03' 'foregroundfiltertimeout|keyboardprefetch|mousequeue'

# --- Classes de dispositivo (HKLM\SYSTEM\CurrentControlSet\Control\Class\{GUID}) ---
Add-Rule '04' '\{4d36e968'                 # Display / placa de video
Add-Rule '12' '\{4d36e972'                 # Placas de rede
Add-Rule '08' '\{4d36e967'                 # Discos e controladoras
Add-Rule '05' '\{4d36e97d'                 # Dispositivos de sistema
Add-Rule '05' 'interrupt management|messagesignaledinterrupt|affinity policy'
Add-Rule '12' 'wakeonmagicpacket|wakeonpattern|wakeonping|enablepmcports|advancedeee|powermgmtproperties|pnpcapabilities|dozemode|\*eee'

# --- 04 GPU e jogos -----------------------------------------------------
Add-Rule '04' 'systemprofile\\tasks\\(games|gamesvscpd|gamesvscat)'
Add-Rule '04' 'gameconfigstore|gameconfig\\|\\gamebar|gameinputconfig|gamemode'
Add-Rule '04' 'currentversion\\gamedvr|\\gamedvr|allowgamedvr|game_dvr'
Add-Rule '04' 'graphicsdrivers|hwschmode|gpupreference|usergpupreferences|disable3dacceleratedrendering|disablehwacceleration|hardwareacceleratedgpu'
Add-Rule '04' '\\dwm\\|\\themes\\|flipmodel|flipsdgrm|disableoverlay|msaa|tessellation'
Add-Rule '04' '\\(nvidia|nvstreamingmanager|amd|atibrt|atikmdag|igdkmd|nvlddmkm|amdkmdag|dxgkrnl|dxgmms)'
Add-Rule '04' 'appcaptureenabled|cursorcapture|audiocapture|historicalcapture|allowtooclose|gamefi|capture'
Add-Rule '04' '\\microsoft\\games|\\direct3d\\|\\opengl\\|vulkan'
Add-Rule '04' '\\software\\intel\\gmm'                            # Intel GMM = GPU (+4)

# --- 05 CPU -------------------------------------------------------------
Add-Rule '05' 'prioritycontrol|schedulpriority'
Add-Rule '05' 'systemresponsiveness|cpucycle|cpmincores|cpmaxcores|cpcoreparking|perfboostmode|boostmode'
Add-Rule '05' 'x2apic|useplatformclock|useplatformtick|disabledynamictick|onedynamictick|invariantpmc'
Add-Rule '05' 'bcdedit|disablenx|nxoptin|forcevalidatemmpages|bootux|safeboot|isolatedmemory'
Add-Rule '05' 'hypervisorlaunchtype|virtualizationbasedsecurity|deviceguard|hvci|memoryintegrity|coreisolation|credentialguard'
Add-Rule '05' 'interruptmerg|\\mps\\|\\affinity\\|\\processor\\|\\acpi\\|\\msischeduling|msimode'
Add-Rule '05' 'multimedia\\systemprofile|mmcss|schedulercategory'

# --- 02 Energia ---------------------------------------------------------
Add-Rule '02' 'powercfg|sub_sleep|sub_battery|sub_processor|sub_video'
Add-Rule '02' '\\control\\power|\\control panel\\power|hiberfil|hibernateenabled|standbyidle|hybridsleep|powerthrottling|powersettings|\\schemes\\|setactive'
Add-Rule '02' 'sleepstudy|autosleep|lididentity|allowstandby'
Add-Rule '02' 'session manager\\power'

# --- 09 Memoria ---------------------------------------------------------
Add-Rule '09' 'memory management|largesystemcache|pagedpool|nonpagedpool|poolusage|poolsize|memorycompression|clearpagefile|physicaladdressextension|ioportsize|disableexecpagesoptimization'
Add-Rule '09' 'sysmain|prefetch|superfetch|disablepagingexecutive|pagingfiles|pagefile'
Add-Rule '09' 'islc|emptystandby|emptyworkingset|workingset|setmemory|memreduct|clearmem'

# --- 08 Armazenamento ---------------------------------------------------
Add-Rule '08' 'ntfs|8dot3|lastaccess|disableindexing|\\frv|ftdisk|\\partmgr|storahci|\\disk\\|volsnap|defragsvc|defrag'
Add-Rule '08' 'writesecaching|writecache|\\mountpoints|\\exfat|\\udfs|storagemanagement|storagesense|managedstorage'
Add-Rule '08' 'known folders|shell folders|shell extensions|autoplayhandlers'
Add-Rule '08' 'winsat|diskcleanup|usnjsvc|trim|deletetoc'
Add-Rule '08' 'device parameters\\disk|enablehddparking|idle3|idlepowerdown|pminimizepower'

# --- PRIORIDADE 07/06 (precisa vir ANTES do bloco 12 Rede) -----------------
# A regra 12 abaixo contem 'tcpip' e engole ...\WMI\Autologger\TCPIPLOGGER;
# por isso o grupo de telemetria abaixo e declarado antes dela (e antes do
# bloco 07, que engole via 'policies\microsoft'). Regra: 1a que casa vence.
Add-Rule '07' '\\control\\wmi\\autologger\\'                    # ETW autologger = telemetria (+173 ops)
Add-Rule '07' '\\currentversion\\ext\\settings\\'                 # shell extensions (+104)
Add-Rule '07' '\\currentversion\\settingsync'                    # sincronizacao de conta (+53)
Add-Rule '07' '\\office\\16\.0\\'                                 # telemetria do Office (+15)
Add-Rule '07' 'nt\\currentversion\\sensor\\permissions|nt\\currentversion\\sensor\\overrides'  # (+8)
Add-Rule '06' '\\currentversion\\systemrestore'                   # desativar restauracao (+12)

# --- 12 Rede ------------------------------------------------------------
Add-Rule '12' 'tcpip|\\afd\\|dnsapi|dnscache|dhcpsvc|\\nsi\\|mrxsmb|lanman(server|workstation)|\\netbt|\\winsck|mpsdrv|\\pacer\\|qwave|\\ndis\\|wcmsvc|wlanapi|wlansvc|iphlpsvc|dhcpcservice'
Add-Rule '12' 'netsh|netdom|networklist|networkprovider|netbios|llmnr|multicast|autotuninglevel'
Add-Rule '12' 'tcpmaxconnectretries|tcpnodelay|tcpdelack|tcpclosewait|defaultttl|tosdescriptor|fastroute|initialrto|maxconnectionsper|1323opts|securityprotocols'
Add-Rule '12' '\\http\\parameters|winhttp|\\inetsettings|\\connections\\|zonemap|sharedaccess|mpssvc|firewall'

# --- 11 Aplicativos -----------------------------------------------------
Add-Rule '11' 'appxpackage|appxprovisioned|provisionedpackage|stagedapplications|packagedinstallers'
Add-Rule '11' 'winget|choco |scoop|\\uninstall|silentuninstall|\\oobe\\'
Add-Rule '11' 'onedrive|\\maps\\|yourphone|sway|solitaire|microsoftofficehub|getstarted|feedbackhub|gethelp|stickynotes'

# --- 07 Debloat, privacidade e telemetria -------------------------------
Add-Rule '07' 'telemetry|datacollection|customerexperience|appcompat\\programdata|\\sqm|\\wer\\|windows error reporting|\\wdi\\|diagnostictracing|diagtrack|state roaming|state repository|\\sip\\|\\aif\\|\\siuf|\\applaunch|programdataupdater'
Add-Rule '07' 'contentdeliverymanager|contentsdeliverymanager|\\feedback|tips|suggestion|quethours|pushnotifications|toastenabled|notifications\\settings|cloudcontent|hostedappsrings'
Add-Rule '07' 'cortana|bingsearch|textinput\\(datacollection|reporting)|advertisingid|\\privacy\\|\\edge\\|edgeupdate|smartscreenenabled'
Add-Rule '07' 'policies\\microsoft|policymanager|policies\\explorer|eventviewer|attachmentmanager|fontprovider|windowsdefender|windows defender'
Add-Rule '07' 'schtasks|scheduledtask|consolidator|usbc eip|kernelceip|bthsqm|familysafety|\\maintenance\\|diskdiagnostic|power efficiency diagnostics'
Add-Rule '07' 'lockscreen|\\spotlight|textprediction|inputpersonalization|typing|\\speech\\|\\ink\\|accountinfo|appcompat\\shim'
Add-Rule '07' '\bosm\b|preventedapplications|preventedusage\\|\\office\\.*\\osm'
Add-Rule '07' 'microsoft\\edge|program files \(x86\)\\microsoft\\edge'
Add-Rule '07' 'office\\16\.0\\(common|osm|security|tools|registration)|outlook\\16\.0\\(mail|preferences)|\\cdp\\|crossdevice'

# --- 06 Limpeza ---------------------------------------------------------
Add-Rule '06' 'cleanmgr|vssadmin|\\temp\\|%temp%|recyclebin|softwaredistribution|\\pss\\|memory.dmp|\\logs\\|\\del |del /f|rd /s|rmdir /s|erase /'
Add-Rule '06' 'dism|startcomponentcleanup|restorehealth|analyzecomponentstore|compactos|wevtutil|clr_migration|winsxs'

# --- 10 Extras / QOL ----------------------------------------------------
Add-Rule '10' 'windowsupdate|wu\\|wuaueng|\\windows update\\|usoschedule|\\deliveryoptimization|\\updateorchestrator|\\update\\orchestrator|wusa|noautoupdate|auoptions'
Add-Rule '10' '\\microsoft\\mediaplayer\\preferences'            # Media Player (+4)
Add-Rule '10' 'uac|enablelua|consentpromptbehavior|filteradministrator|promptonsecuredesktop'
Add-Rule '10' 'wallpaper|theme|darkmode|themes\\|\\themes|\\dark|\\light|taskbar|startmenu|start menu|\\explorer\\startpage|controlpanel\\desktop|\\desktop\\'
Add-Rule '10' 'fileext|hidefileext|showinfosec|superhidden|folderoptions|contextmenu|context menus|\\shell\\|\\shellex\\|jumplist|nojumplist|norecent'
Add-Rule '10' 'copilot|widgets|websearch|clips|chat\\|microsoftwindows\\client\\|\\feedsw|taskbarchat|taskbaredgechat|taskbargames'
Add-Rule '10' 'faststartup|hpet|legacy|numlock|bootanimation|quietboot|logo|\\currentversion\\run|runonce|startupreg|\\startup\\'

# --- ultimo recurso antes do geral --------------------------------------
Add-Rule '05' '\\enum\\'

# --- 01 Gerais (fallbacks especificos) ----------------------------------
Add-Rule '01' 'image file execution options|imagefileexecutionoptions'
Add-Rule '01' '\\services\\|sc config|sc stop|sc start|sc delete|sc create|set-service|new-service|remove-service|stop-service|start-service'
Add-Rule '01' 'explorer\\advanced|\\currentversion\\explorer|visualstyles|\\control panel\\desktop\\'

# =====================================================================
#  LEITURA DE TEXTO (mesma logica do extractor)
# =====================================================================
# =====================================================================
#  CLASSIFICACAO DE UMA OPERACAO
# =====================================================================
function Get-TargetString {
    param($Op)
    $s = ''
    if ($Op.Kind -eq 'Registry') {
        $s = $Op.Path + '\' + $Op.Name + ' ' + $Op.Type + ' ' + $Op.Data
    }
    elseif ($Op.Kind -eq 'Service') {
        $s = 'services\\' + $Op.Name + ' ' + $Op.Action + ' ' + $Op.Data
    }
    elseif ($Op.Kind -eq 'Task') {
        $s = 'schtasks ' + $Op.Path + ' ' + $Op.Action
    }
    else {
        $s = $Op.Path + ' ' + $Op.Name + ' ' + $Op.Data + ' ' + $Op.Raw
    }
    return ($s + ' ' + $Op.Raw).ToLowerInvariant()
}

function Test-RevertText {
    param([string]$S)
    if ($S -match '(restore point|restoremepoint|restoredefaultschemes|point de restauracao)') { return $false }
    return ($S -match '(revert|undo|restore|reverter|desfazer|rollback|back to default|set default|restore default|uninstall tweak)')
}

# $KtIndex nao e parametro: o vinculo com o tweak do Kairo e feito depois, na
# montagem, via $ktId (ver $ktIndex mais abaixo). Declarar aqui so criava uma
# dependencia aparente e quebraria se a ordem de classificacao mudasse.
function Get-OpCategory {
    param($Op, [hashtable]$ExmMap)

    $fileLc = $Op.File.ToLowerInvariant()

    # 1) Kairo Tweaker: a linha Raw carrega o contexto do tweak
    if ($Op.Raw -like 'KT|*') {
        $parts = $Op.Raw.Split('|')
        if ($parts.Count -ge 3 -and $script:KtMap.ContainsKey($parts[2])) {
            return @{ Cat = $script:KtMap[$parts[2]]; Label = $parts[3]; Src = 'Kairo' }
        }
    }

    # 2) EXM: usa o menu do proprio utilitario
    if ($ExmMap.Count -gt 0 -and $ExmMap.ContainsKey([int]$Op.Line)) {
        $e = $ExmMap[[int]$Op.Line]
        $lab = $e.Label
        if ($lab -match '(?i)^(revert|undo|restore)' -or $e.Cat -eq 'F') {
            return @{ Cat = 'F'; Label = $lab; Src = 'EXM' }
        }
        return @{ Cat = $e.Cat; Label = $lab; Src = 'EXM' }
    }

    # 3) Arquivos explicitamente de reversao
    if (Test-RevertText $fileLc) { return @{ Cat = 'F'; Label = (Split-Path $Op.File -Leaf); Src = 'Arquivo' } }

    # 4) Regras sobre o alvo
    $t = Get-TargetString $Op
    foreach ($r in $script:Rules) { if ($t.Contains($r.Re) -or $t -match $r.Re) { return @{ Cat = $r.C; Label = ''; Src = 'Regra'; Rule = $r.Re } } }

    # 5) Fallback por Kind
    switch ($Op.Kind) {
        'Clean'     { @{ Cat='06'; Label=''; Src='Kind'; Rule='kind:Clean' } }
        'Appx'      { @{ Cat='11'; Label=''; Src='Kind'; Rule='kind:Appx' } }
        'Power'     { @{ Cat='02'; Label=''; Src='Kind'; Rule='kind:Power' } }
        'NetShell'  { @{ Cat='12'; Label=''; Src='Kind'; Rule='kind:NetShell' } }
        'BcdEdit'   { @{ Cat='05'; Label=''; Src='Kind'; Rule='kind:BcdEdit' } }
        'Dism'      { @{ Cat='06'; Label=''; Src='Kind'; Rule='kind:Dism' } }
        default     { @{ Cat='01'; Label=''; Src='Kind'; Rule='kind:' + $Op.Kind } }
    }
}

# =====================================================================
#  MAIN - carregar, anotar, renderizar
# =====================================================================
$opsPath    = Join-Path $InDir 'ops.json'
$tweaksPath = Join-Path $InDir 'tweaks.json'
if (-not (Test-Path -LiteralPath $opsPath)) { throw "ops.json nao encontrado em $InDir (rode Extract-ScriptOps.ps1)" }

Write-Host 'Lendo ops.json ...' -ForegroundColor Cyan
$opsRaw = Get-Content -LiteralPath $opsPath -Raw | ConvertFrom-Json
$ops = @($opsRaw)
Write-Host ("  {0} operacoes" -f $ops.Count)

$tweaks = @()
if (Test-Path -LiteralPath $tweaksPath) {
    $twRaw = Get-Content -LiteralPath $tweaksPath -Raw | ConvertFrom-Json
    $tweaks = @($twRaw)
    Write-Host ("  {0} tweaks declarativos do Kairo" -f $tweaks.Count)
}

# Indice de tweaks do Kairo por Id
$ktIndex = @{}
foreach ($t in $tweaks) { if (-not $ktIndex.ContainsKey($t.Id)) { $ktIndex[$t.Id] = $t } }

# Mapas de secao do EXM (um por arquivo .cmd do utilitario)
Write-Host 'Mapeando menus do EXM ...' -ForegroundColor Cyan
$exmMaps = @{}
foreach ($f in ($ops | Select-Object -ExpandProperty File -Unique)) {
    if ($f -notmatch '(?i)exm.*\.cmd$') { continue }
    $full = Join-Path $Root $f
    if (-not (Test-Path -LiteralPath $full)) { continue }
    $m = Get-ExmLineMap $full
    $exmMaps[$f] = $m
    Write-Host ("  {0}: {1} linhas rotuladas" -f $f, $m.Count)
}

Write-Host 'Classificando ...' -ForegroundColor Cyan
$annot = New-Object System.Collections.Generic.List[object]
foreach ($o in $ops) {
    $m = if ($exmMaps.ContainsKey($o.File)) { $exmMaps[$o.File] } else { @{} }
    $c = Get-OpCategory $o $m
    $annot.Add([pscustomobject]@{
        File = $o.File; Line = [int]$o.Line; Kind = $o.Kind; Action = $o.Action
        Path = $o.Path; Name = $o.Name; Type = $o.Type; Data = $o.Data; Raw = $o.Raw
        Note = $o.Note; Cat = $c.Cat; Label = $c.Label; Src = $c.Src; Rule = $c.Rule
    })
}

if ($Diag -ne '') {
    $dl = New-Object System.Collections.Generic.List[string]
    $dl.Add("DISTRIBUICAO POR CATEGORIA")
    foreach ($g in ($annot | Group-Object Cat | Sort-Object Name)) {
        $dl.Add(("  {0}  {1,5}  {2}" -f $g.Name, $g.Count, $script:Cats[$g.Name].N))
    }
    foreach ($cat in ($annot | Group-Object Cat | Sort-Object Name)) {
        $dl.Add("")
        $dl.Add("=== CAT $($cat.Name) :: $($cat.Count) ops :: origem ===")
        foreach ($s in ($cat.Group | Group-Object Src | Sort-Object Count -Descending)) { $dl.Add(("    src {0,-8} {1,5}" -f $s.Name, $s.Count)) }
        $dl.Add("  -- top regras --")
        foreach ($r in ($cat.Group | Group-Object Rule | Sort-Object Count -Descending | Select-Object -First 18)) {
            $dl.Add(("    {0,5}  {1}" -f $r.Count, $r.Name))
        }
        $dl.Add("  -- top paths --")
        foreach ($p in ($cat.Group | Group-Object Path | Sort-Object Count -Descending | Select-Object -First 18)) {
            $pp = $p.Name; if ($null -ne $pp -and $pp.Length -gt 100) { $pp = $pp.Substring(0, 97) + '...' }
            $srcs = ($p.Group | Group-Object Src | Sort-Object Count -Descending | ForEach-Object { "$($_.Name):$($_.Count)" }) -join ','
            $rls  = ($p.Group | Group-Object Rule | Sort-Object Count -Descending | Select-Object -First 1 | ForEach-Object { $_.Name })
            $dl.Add(("    {0,5}  [{1}] {2}  << {3}" -f $p.Count, $srcs, $pp, $rls))
        }
    }
    [IO.File]::WriteAllLines($Diag, $dl, (New-Object System.Text.UTF8Encoding $true))
    Write-Host "Diagnostico gravado em $Diag" -ForegroundColor Yellow
}

# =====================================================================
#  HELPERS DE RENDER
# =====================================================================
function Esc { param([string]$S) if ($null -eq $S) { return '' }; ($S -replace '\|', '\|' -replace '\s+', ' ').Trim() }
function Short { param([string]$S, [int]$Max) if ($null -eq $S) { return '' }; $S = $S.Trim(); if ($S.Length -le $Max) { return $S } return $S.Substring(0, $Max - 1) + '…' }

function Get-SourceList {
    param($Rows, [int]$Max = 4)
    # desempate explicito por Name: sem isso a ordem dos empates depende da
    # estabilidade do Sort-Object e muda entre execucoes
    # @() obrigatorio: com UM grupo so, $g.Count seria o TAMANHO do grupo
    # (numero de operacoes), nao a quantidade de grupos
    $g = @($Rows | Group-Object File | Sort-Object -Property @{e='Count';Descending=$true}, Name)
    $parts = @()
    foreach ($x in ($g | Select-Object -First $Max)) {
        $n = Split-Path $x.Name -Leaf
        $parts += ('`' + (Short $n 42) + '`' + $(if ($x.Count -gt 1) { "×$($x.Count)" } else { '' }))
    }
    if ($g.Count -gt $Max) { $parts += "+$($g.Count - $Max) outros" }
    return (Esc ($parts -join ', '))
}

function Get-ActionTag {
    param($Actions)
    $a = @($Actions)
    if ($a -contains 'CreateKey') { return 'chave' }
    if ($a -contains 'DeleteKey' -or $a -contains 'DeleteValue') { return 'del' }
    return 'valor'
}

function Reg-Table {
    param($Rows, [System.Collections.Generic.List[string]]$Out)
    $by = @($Rows | Group-Object { ($_.Path + '\' + $_.Name).ToLowerInvariant() } | Sort-Object Name)
    if ($by.Count -eq 0) { return }
    $Out.Add('| Chave + Valor | Tipo | Valores encontrados | Onde aparece |')
    $Out.Add('|---|---|---|---|')
    $seen = @{}
    $seenTrunc = @{}
    foreach ($g in $by) {
        $r0    = $g.Group[0]
        $acts  = @($g.Group | Select-Object -ExpandProperty Action -Unique | Where-Object { $_ })
        $tag   = Get-ActionTag $acts
        $key   = $r0.Path + '\' + $r0.Name
        # nunca truncar a cauda do caminho: e ali que mora o nome do valor.
        # Cortar a cauda gerava linhas byte-identicas (bug de colisao).
        if ($key.Length -gt 150) {
            # Truncagem segura: cabeca + CAUDA do Path original. Retinencias sozinhas
            # faziam duas chaves distintas (mesmos 99 primeiros chars do Path e mesmo
            # Name) virarem texto byte-identico. Os ultimos 30 caracteres do Path
            # entram no resultado; se ainda assim colidirem (Path longo com miolo
            # diferente e mesma cabeca+cauda), um sufixo curto desempata.
            $head = (Short $r0.Path 90).TrimEnd('\')
            $tail = if ($r0.Path.Length -gt 30) { $r0.Path.Substring($r0.Path.Length - 30).TrimStart('\') } else { $r0.Path }
            $base = $head + '\...\' + $tail + '\' + $r0.Name
            $key  = $base
            $i    = 2
            while ($seenTrunc.ContainsKey($key)) { $key = $base + ' #' + $i; $i++ }
            $seenTrunc[$key] = $true
        }
        if ($tag -eq 'del') { $key = '**[APAGA]** ' + $key }
        $types = (($g.Group | Where-Object { $_.Type -and $_.Type -notmatch '^(KT_|REG_SZ|\?)$' } | Select-Object -ExpandProperty Type -Unique) | Select-Object -First 2) -join '/'
        if (-not $types) { $types = (($g.Group | Select-Object -ExpandProperty Type -Unique) | Where-Object { $_ }) -join '/' }
        if (-not $types) { $types = $tag }
        $vals  = ($g.Group | Where-Object { $_.Data } | ForEach-Object { Short ($_.Data -replace '[\r\n\t]', ' ') 46 } | Select-Object -Unique | Select-Object -First 4) -join ' · '
        if (-not $vals) { $vals = '—' }
        # "Chave + Valor" precisa ser unica dentro da tabela: desempata pelo Action
        $cell = Esc $key
        if ($seen.ContainsKey($cell)) {
            $cell = $cell + $(if ($tag -eq 'valor') { '' } else { ' [' + $tag + ']' })
            $i = 2
            while ($seen.ContainsKey($cell)) { $cell = (Esc $key) + ' [' + $tag + '] #' + $i; $i++ }
        }
        $seen[$cell] = $true
        $Out.Add('| `' + $cell + '` | ' + (Esc $types) + ' | ' + (Esc $vals) + ' | ' + (Get-SourceList $g.Group) + ' |')
    }
    $Out.Add('')
}





# =====================================================================
#  MONTAGEM DO DOCUMENTO
# =====================================================================
function Src-List {
    param($Rows, [int]$Max = 3)
    # desempate explicito por Name: sem isso a ordem dos empates depende da
    # estabilidade do Sort-Object e muda entre execucoes
    # @() obrigatorio: com UM grupo so, $g.Count seria o TAMANHO do grupo
    # (numero de operacoes), nao a quantidade de grupos
    $g = @($Rows | Group-Object File | Sort-Object -Property @{e='Count';Descending=$true}, Name)
    $parts = @()
    foreach ($x in ($g | Select-Object -First $Max)) {
        $n = Split-Path $x.Name -Leaf
        $s = (Short $n 40)
        if ($x.Count -gt 1) { $s += " x$($x.Count)" }
        $parts += $s
    }
    if ($g.Count -gt $Max) { $parts += "+$($g.Count - $Max)" }
    return (Esc ($parts -join ', '))
}

$L = New-Object System.Collections.Generic.List[string]
$stamp = Get-Date -Format 'dd/MM/yyyy HH:mm'
$allFiles = $annot | Select-Object -ExpandProperty File -Unique

$L.Add('# ACERVO DE TWEAKS E MODIFICACOES')
$L.Add('')
$L.Add("> Catalogo consolidado de todos os ajustes encontrados em ``$Root``  ")
$L.Add("> Gerado em $stamp · **$($annot.Count) operacoes unicas** em **$($allFiles.Count) arquivos** · categorias espelhadas do **EXM Free Tweaking Utility V10.0**")
$L.Add('')
$L.Add('## Como ler este catalogo')
$L.Add('')
$L.Add('| Sinal | Significado |')
$L.Add('|---|---|')
$L.Add('| `Chave + Valor` | Alvo exato no registro (caminho normalizado HKLM/HKCU/HKCR/HKU) |')
$L.Add('| **[APAGA]** | O script remove o valor/chave em vez de escrever |')
$L.Add('| `xN` | A mesma operacao se repete N vezes naquele arquivo |')
$L.Add('| `+N` | Existem N outros arquivos alem dos listados |')
$L.Add('| **Kairo** | Operacao pertence a um tweak nomeado do Kairo Tweaker (com descricao) |')
$L.Add('| **EXM** | Operacao esta dentro de um item de menu do EXM (rotulo conhecido) |')
$L.Add('| **Regra** | Categoria inferida por padrao de caminho/servico/comando |')
$L.Add('')
$L.Add('As operacoes foram deduplicadas por (arquivo + tipo + acao + alvo + dados),')
$L.Add('entao o mesmo ajuste aplicado em cinco utilitarios aparece uma vez por arquivo.')
$L.Add('')
$L.Add('---')
$L.Add('')
$L.Add('## Indice de categorias')
$L.Add('')
$L.Add('| # | Categoria EXM V10 | Descricao | Operacoes | Alvos unicos | Arquivos |')
$L.Add('|---|---|---|---|---|---|')

$catRows = [ordered]@{}
foreach ($k in $script:Cats.Keys) {
    $rows = @($annot | Where-Object { $_.Cat -eq $k })
    if ($rows.Count -eq 0) { continue }
    $catRows[$k] = $rows
}
function Get-TargetKey {
    param($R)
    switch ($R.Kind) {
        'Registry' { return 'REG|' + ($R.Path + '\' + $R.Name).ToLowerInvariant() }
        'Service'  { return 'SVC|' + $R.Name.ToLowerInvariant() }
        'Task'     { return 'TSK|' + $R.Path.ToLowerInvariant() }
        'Appx'     { return 'APX|' + $R.Name.ToLowerInvariant() }
        default    { return 'CMD|' + ($R.Kind + '|' + $R.Action + '|' + $R.Path + '|' + $R.Name).ToLowerInvariant() }
    }
}
foreach ($k in $catRows.Keys) {
    $rows = $catRows[$k]
    $c    = $script:Cats[$k]
    $alvos = ($rows | ForEach-Object { Get-TargetKey $_ } | Sort-Object -Unique).Count
    $arqs  = ($rows | Select-Object -ExpandProperty File -Unique).Count
    $L.Add(('| **{0}** | {1} {2} | {3} | {4} | {5} | {6} |' -f $k, $c.E, $c.N, $c.PT, $rows.Count, $alvos, $arqs))
}
$L.Add('')
$L.Add('---')
$L.Add('')

# =====================================================================
#  SECOES POR CATEGORIA
# =====================================================================
foreach ($k in $catRows.Keys) {
    $rows = $catRows[$k]
    $c    = $script:Cats[$k]

    $L.Add("## $k · $($c.E) $($c.N)")
    $L.Add('')
    $L.Add("_$($c.PT) — $($rows.Count) operacoes._")
    $L.Add('')

    # ---- Tweaks nomeados ------------------------------------------------
    $named = @($rows | Where-Object { $_.Src -ne 'Regra' -and $_.Label } | Group-Object Label | Sort-Object Count -Descending)
    if ($named.Count -gt 0) {
        $L.Add("### $k.1 · Tweaks nomeados ($($named.Count))")
        $L.Add('')
        $L.Add('| Twek | Origem | Risco | Registr | Serv | Taref | Comd | Onde |')
        $L.Add('|---|---|---|---|---|---|---|---|')
        foreach ($g in $named) {
            $g0   = $g.Group
            $src  = $g0[0].Src
            $ktId = ''
            if ($src -eq 'Kairo' -and $g0[0].Raw -like 'KT|*') { $ktId = $g0[0].Raw.Split('|')[1] }
            $kt   = if ($ktId -and $ktIndex.ContainsKey($ktId)) { $ktIndex[$ktId] } else { $null }
            $risk = if ($kt) { $kt.Risco } else { '—' }
            $nReg = @($g0 | Where-Object { $_.Kind -eq 'Registry' }).Count
            $nSvc = @($g0 | Where-Object { $_.Kind -eq 'Service'  }).Count
            $nTsk = @($g0 | Where-Object { $_.Kind -eq 'Task'     }).Count
            $nCm  = @($g0 | Where-Object { $_.Kind -notin @('Registry','Service','Task') }).Count
            $badge = if ($src -eq 'Kairo') { 'Kairo' } elseif ($src -eq 'EXM') { 'EXM' } else { 'Arquivo' }
            $nm = Short ($g.Name -replace '^KT\|.*?\|', '') 78
            $L.Add('| **' + (Esc $nm) + '** | ' + $badge + ' | ' + (Esc $risk) + ' | ' + $nReg + ' | ' + $nSvc + ' | ' + $nTsk + ' | ' + $nCm + ' | ' + (Src-List $g0 1) + ' |')
        }
        $L.Add('')
    }

    # ---- Registro -------------------------------------------------------
    $reg = @($rows | Where-Object { $_.Kind -eq 'Registry' })
    if ($reg.Count -gt 0) {
        $nAlvo = ($reg | ForEach-Object { Get-TargetKey $_ } | Sort-Object -Unique).Count
        $L.Add("### $k.2 · Registro do Windows ($nAlvo alvos, $($reg.Count) escritas)")
        $L.Add('')
        Reg-Table $reg $L
    }

    # ---- Servicos -------------------------------------------------------
    $svc = @($rows | Where-Object { $_.Kind -eq 'Service' })
    if ($svc.Count -gt 0) {
        $L.Add("### $k.3 · Servicos ($(@($svc | Group-Object Name).Count))")
        $L.Add('')
        $L.Add('| Servico | Acoes / inicio | Onde aparece |')
        $L.Add('|---|---|---|')
        foreach ($g in ($svc | Group-Object Name | Sort-Object Name)) {
            $acts = (($g.Group | ForEach-Object { if ($_.Data) { "$($_.Action) -> $($_.Data)" } else { $_.Action } }) | Select-Object -Unique) -join ' · '
            $L.Add('| `' + (Esc $g.Name) + '` | ' + (Esc (Short $acts 90)) + ' | ' + (Src-List $g.Group 2) + ' |')
        }
        $L.Add('')
    }

    # ---- Tarefas --------------------------------------------------------
    $tsk = @($rows | Where-Object { $_.Kind -eq 'Task' })
    if ($tsk.Count -gt 0) {
        $L.Add("### $k.4 · Tarefas agendadas ($(@($tsk | Group-Object Path).Count))")
        $L.Add('')
        $L.Add('| Tarefa | Acoes | Onde aparece |')
        $L.Add('|---|---|---|')
        foreach ($g in ($tsk | Group-Object Path | Sort-Object -Property Count,Name -Descending)) {
            $acts = (($g.Group | Select-Object -ExpandProperty Action | Select-Object -Unique)) -join ' · '
            $L.Add('| `' + (Esc (Short $g.Name 100)) + '` | ' + (Esc $acts) + ' | ' + (Src-List $g.Group 2) + ' |')
        }
        $L.Add('')
    }

    # ---- Diversos -------------------------------------------------------
    $oth = @($rows | Where-Object { $_.Kind -notin @('Registry', 'Service', 'Task') })
    if ($oth.Count -gt 0) {
        # declara os DOIS numeros: $oth.Count e o total de operacoes, mas a tabela
        # abaixo agrupa por (Kind,Action,Path,Name) e rende menos linhas
        $nLinhas = @($oth | Group-Object Kind, Action, Path, Name).Count
        $L.Add("### $k.5 · Comandos e outros ($($oth.Count) ops · $nLinhas linhas)")
        $L.Add('')
        $L.Add('| Tipo | Acao | Alvo / dados | Onde aparece |')
        $L.Add('|---|---|---|---|')
        foreach ($g in ($oth | Group-Object Kind, Action, Path, Name | Sort-Object Count -Descending)) {
            $g0 = $g.Group[0]
            if ($g0.Kind -eq 'Appx') {
                $d = if ($g0.Name) { $g0.Name } else { '(todos os pacotes)' }
                if ($g0.Data) { $d = $d + '  ' + $g0.Data }
            }
            # Path\Name = Data (ex.: Power:SUB_PROCESSOR\PERFBOOSTMODE = 1)
            # So usa este formato quando Name E Data existem; se Data vier vazio,
            # cai no tratamento geral abaixo (Raw), que preserva o comando completo.
            elseif ($g0.Kind -eq 'Power' -and $g0.Name -and $g0.Data) { $d = ($g0.Path + '\' + $g0.Name + ' = ' + $g0.Data).Trim() }
            elseif ($g0.Raw)  { $d = $g0.Raw }
            elseif ($g0.Data) { $d = $g0.Data }
            else              { $d = $g0.Name }
            $L.Add('| ' + (Esc $g0.Kind) + ' | ' + (Esc $g0.Action) + ' | `' + (Esc (Short $d 88)) + '` | ' + (Src-List $g.Group 2) + ' |')
        }
        $L.Add('')
    }
    $L.Add('---')
    $L.Add('')
}


# =====================================================================
#  APENDICE A - Tweaks nomeados do Kairo Tweaker (com descricao)
# =====================================================================
if ($tweaks.Count -gt 0) {
    $L.Add('## Apendice A · Tweaks documentados do Kairo Tweaker')
    $L.Add('')
    $L.Add("São $($tweaks.Count) ajustes declarados nos módulos do Kairo Tweaker. Cada um tem descricao,")
    $L.Add('ganho esperado e nivel de risco definidos pelo autor — a fonte mais confiavel deste acervo.')
    $L.Add('')
    foreach ($g in ($tweaks | Group-Object Categoria | Sort-Object Count -Descending)) {
        $ex = if ($script:KtMap.ContainsKey($g.Name)) { $script:KtMap[$g.Name] } else { '01' }
        $L.Add("### A.$ex · $($g.Name) ($($g.Count))")
        $L.Add('')
        foreach ($t in ($g.Group | Sort-Object Id)) {
            $L.Add("#### ``$($t.Id)`` — $((Short $t.Nome 110)))")
            $L.Add('')
            $bits = @()
            if ($t.Risco)    { $bits += "Risco: **$($t.Risco)**" }
            if ($t.Reinicio) { $bits += "Requer reinicio: $($t.Reinicio)" }
            $alvo = @()
            if ($t.Registry) { $alvo += "$($t.Registry) valores de registro" }
            if ($t.Services) { $alvo += "$($t.Services) servicos" }
            if ($t.Bcd)      { $alvo += "$($t.Bcd) entradas BCD" }
            if ($t.Commands) { $alvo += "$($t.Commands) comandos" }
            $L.Add('- ' + (($bits + @("Aplica: $($alvo -join ', ')")) -join ' · '))
            if ($t.Explicacao) { $L.Add('- **Por que:** ' + (Short ($t.Explicacao -replace '\s+', ' ') 600)) }
            if ($t.Ganho)      { $L.Add('- **Ganho:** '     + (Short ($t.Ganho      -replace '\s+', ' ') 400)) }
            $bq = [string][char]0x60
            $L.Add('- **Modulo:** ' + $bq + (Split-Path $t.File -Leaf) + ':' + $t.Line + $bq)
            $L.Add('')
        }
    }
    $L.Add('---')
    $L.Add('')
}

# =====================================================================
#  APENDICE B - Conflitos (mesmo alvo, valores diferentes)
# =====================================================================
$L.Add('## Apendice B · Conflitos e divergencias')
$L.Add('')
$L.Add('Alvos tocados por mais de um script com **valores diferentes**. Aplicar dois scripts diferentes')
$L.Add('em sequencia faz o ultimo vencer — esta lista mostra onde isso importa.')
$L.Add('')
$regAll = @($annot | Where-Object { $_.Kind -eq 'Registry' -and $_.Data })
$conf = foreach ($g in ($regAll | Group-Object { ($_.Path + '\' + $_.Name).ToLowerInvariant() })) {
    # @() obrigatorio: com UM grupo só, $vals.Count seria o tamanho do grupo, nao o de grupos
    $vals = @($g.Group | Group-Object Data)
    if ($vals.Count -gt 1) {
        [pscustomobject]@{
            Key   = $g.Group[0].Path + '\' + $g.Group[0].Name
            N     = $vals.Count
            Vals  = (($vals | Sort-Object Count -Descending | ForEach-Object { "$($_.Name) (x$($_.Count))" }) | Select-Object -First 5) -join ' vs '
            Files = ($g.Group | Select-Object -ExpandProperty File -Unique).Count
        }
    }
}
$conf = @($conf | Sort-Object N, Files -Descending)
if ($conf.Count -gt 0) {
    $L.Add("Foram encontrados **$($conf.Count) alvos divergentes**. Mostrando os $($([Math]::Min(150,$conf.Count))) mais afetados:")
    $L.Add('')
    $L.Add('| Alvo | Valores distintos | Arquivos | Valores |')
    $L.Add('|---|---|---|---|')
    foreach ($x in ($conf | Select-Object -First 150)) {
        $L.Add('| `' + (Esc (Short $x.Key 110)) + '` | ' + $x.N + ' | ' + $x.Files + ' | ' + (Esc (Short $x.Vals 150)) + ' |')
    }
    $L.Add('')
}
else { $L.Add('_Nenhum conflito detectado._'); $L.Add('') }

# Conflito de sentido (ligar x desligar) no mesmo valor
$sw = foreach ($g in ($regAll | Group-Object { ($_.Path + '\' + $_.Name).ToLowerInvariant() })) {
    $on  = @($g.Group | Where-Object { $_.Data -match '^(1|0x1|0xffffffff|4294967295|Enable|High|Yes)$' }).Count
    $off = @($g.Group | Where-Object { $_.Data -match '^(0|0x0|Disable|Low|No)$' }).Count
    if ($on -gt 0 -and $off -gt 0) {
        [pscustomobject]@{ Key = $g.Group[0].Path + '\' + $g.Group[0].Name; On = $on; Off = $off }
    }
}
$sw = @($sw)
if ($sw.Count -gt 0) {
    $L.Add("### B.2 · Mesmo interruptor, decisões opostas ($($sw.Count))")
    $L.Add('')
    $L.Add('| Alvo | Escrito como LIGADO | Escrito como DESLIGADO |')
    $L.Add('|---|---|---|')
    foreach ($x in ($sw | Sort-Object { [Math]::Min($_.On, $_.Off) } -Descending | Select-Object -First 100)) {
        $L.Add('| `' + (Esc (Short $x.Key 120)) + '` | ' + $x.On + ' | ' + $x.Off + ' |')
    }
    $L.Add('')
}
$L.Add('---')
$L.Add('')


# =====================================================================
#  APENDICE C - Inventario de arquivos
# =====================================================================
$L.Add('## Apendice C · Inventario de arquivos')
$L.Add('')
$invFiles = @($annot | Group-Object File | Sort-Object Count -Descending)
$L.Add("### C.1 · Arquivos que geraram operacoes ($($invFiles.Count))")
$L.Add('')
$L.Add('| Arquivo | Tipo | Operacoes | Categorias tocadas |')
$L.Add('|---|---|---|---|')
foreach ($g in $invFiles) {
    $ext  = [IO.Path]::GetExtension($g.Name)
    $cats = (($g.Group | Group-Object Cat | Sort-Object Count -Descending | ForEach-Object { $_.Name }) | Select-Object -First 6) -join ' '
    $L.Add('| `' + (Esc (Short $g.Name 92)) + '` | ' + $ext + ' | ' + $g.Count + ' | ' + $cats + ' |')
}
$L.Add('')

# --- C.2 · cobertura do acervo (requer _tools\scan.json) --------------------
$scanPath = Join-Path $InDir 'scan.json'
if (Test-Path -LiteralPath $scanPath) {
    $scanRaw = Get-Content -LiteralPath $scanPath -Raw | ConvertFrom-Json
    $scan    = @($scanRaw)
    $noOps   = @($scan | Where-Object { [int]$_.Ops -eq 0 })
    $L.Add("### C.2 · Arquivos do acervo sem nenhuma operacao detectada ($($noOps.Count))")
    $L.Add('')
    $L.Add('| Arquivo | Bytes | Provavel motivo |')
    $L.Add('|---|---|---|')
    # Padroes de escrita cobertos pelo teste. Ampliados de proposito: o regex
    # antigo (reg add / Set-ItemProperty / sc config / Remove-Appx / schtasks)
    # deixava passar justamente os arquivos que NAO escrevem registro, e o teste
    # so rodava abaixo de 2048 bytes — 7 dos 11 arquivos ficavam sem diagnostico.
    $writeRe = '(?i)reg\s+add|reg\.exe\s+add|Set-ItemProperty|New-ItemProperty|sc\s+(config|delete)|Remove-Appx|schtasks|netsh|powercfg'
    # Binario auxiliar invocado pelo script. O extrator parseia .ps1/.cmd/.bat,
    # mas nao segue a chamada de um .exe/.com externo — e e exatamente por isso
    # que "Limpar Memoria Ram.bat" (que so chama EmptyStandbyList.exe) fica sem
    # operacao. Interpretadores/launchers ficam de fora: nao sao tweak.
    $binRe = '(?i)(?<![\w\-])(?!cmd\.exe|powershell|pwsh|conhost|mshta|wscript|cscript|rundll32|regsvr32|dllhost)[\w\-]+\.(exe|com)\b'
    foreach ($s in ($noOps | Sort-Object Bytes)) {
        $bytes     = [int]$s.Bytes
        $ehPequeno = $bytes -lt 2048
        $casou     = $false
        $aux       = $false
        try {
            $full = Join-Path $Root $s.File
            if (Test-Path -LiteralPath $full) {
                $txt   = Read-TextSmart $full
                $casou = [regex]::IsMatch($txt, $writeRe)
                $aux   = [regex]::IsMatch($txt, $binRe)
            }
        } catch { }
        if ($casou) {
            # Ha escrita no texto, mas o extrator nao produziu operacao: bug ou
            # formato nao suportado. Nao da para chamar isso de "apenas interface".
            $motivo = 'escrita presente no texto, porem nao virou operacao (formato nao reconhecido pelo extrator; verificar manualmente)'
        }
        elseif ($aux) {
            # Tweak real que delega a um binario externo: o extrator nao parseia
            # a chamada, entao zero operacoes aqui NAO significa "sem efeito".
            $motivo = 'sem escrita nos padroes cobertos, mas invoca executavel auxiliar (.exe/.com) que o extrator nao parseia; zero operacoes aqui nao significa sem efeito (verificar manualmente)'
        }
        elseif ($ehPequeno) {
            $motivo = 'parece apenas interface/validacao'
        }
        else {
            $motivo = 'sem escrita de registro detectada (padroes cobertos: reg add, Set-ItemProperty, sc config, Remove-Appx, schtasks, netsh, powercfg); nao analisado: o extrator nao reconheceu nenhuma escrita (verificar manualmente)'
        }
        $L.Add('| `' + (Esc (Short $s.File 92)) + '` | ' + $bytes + ' | ' + $motivo + ' |')
    }
    $L.Add('')
    if ($scan.Count -gt 0) {
        $pct = [Math]::Round((100.0 * @($scan | Where-Object { [int]$_.Ops -gt 0 }).Count) / $scan.Count, 1)
        $L.Add("Cobertura: $(@($scan | Where-Object { [int]$_.Ops -gt 0 }).Count) de $($scan.Count) arquivos tweak-like ($pct%).")
        $L.Add('')
    }
}
else {
    $L.Add('### C.2 · Arquivos do acervo sem nenhuma operacao detectada')
    $L.Add('')
    $L.Add('> **Indisponivel:** `_tools\scan.json` nao existe, entao nao da para listar os arquivos')
    $L.Add('> do acervo que produziram zero operacoes. Rode o extrator novamente:')
    $L.Add('> `powershell -NoProfile -ExecutionPolicy Bypass -File _tools\Extract-ScriptOps.ps1`')
    $L.Add('')
}

$L.Add('### C.3 · Observacoes de qualidade dos dados')
$L.Add('')
$noted = @($annot | Where-Object { $_.Note } | Group-Object File)
if ($noted.Count -gt 0) {
    foreach ($g in $noted) {
        $L.Add("- ``$($g.Name)`` — $($g.Group[0].Note) ($($g.Count) operacoes afetadas).")
    }
}
else { $L.Add('- Nenhuma observacao.') }
$L.Add('')
$L.Add('- Os 4 utilitarios EXM concentram a maior parte das operacoes; as versoes V6.1 e V10 se')
$L.Add('  sobrepoe bastante, entao muitas chaves aparecem em 3 ou 4 arquivos ao mesmo tempo.')
$L.Add('- ``Desativar Game DVR 2.reg`` contem milhares de linhas de binario hex (cursores/temas)')
$L.Add('  que foram consolidadas em poucas operacoes de tipo ``REG_BINARY``.')
$L.Add('- Reversoes do EXM aparecem na categoria **F** quando o rotulo do menu comeca com')
$L.Add('  Revert/Undo/Restore; reversoes soltas em arquivos proprios tambem caem em **F**.')
$L.Add('')
$L.Add('---')
$L.Add('')
$L.Add('## Como regenerar este catalogo')
$L.Add('')
$L.Add('```powershell')
$L.Add('# 1) extrai as operacoes de todos os .reg/.ps1/.cmd/.bat')
$L.Add('powershell -NoProfile -ExecutionPolicy Bypass -File _tools\Extract-ScriptOps.ps1')
$L.Add('')
$L.Add('# 2) classifica e gera este arquivo')
$L.Add('powershell -NoProfile -ExecutionPolicy Bypass -File _tools\Build-Catalog.ps1')
$L.Add('```')
$L.Add('')
$L.Add('_Os arquivos intermediarios ``_tools\ops.json`` e ``_tools\tweaks.json`` podem ser usados')
$L.Add('para consultas programaticas (por exemplo, gerar um menu interativo no formato do Kairo Tweaker)._')
$L.Add('')

$dir = Split-Path -Parent $OutFile
if ($dir -and -not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
[System.IO.File]::WriteAllLines($OutFile, $L, (New-Object System.Text.UTF8Encoding($true)))
Write-Host ("Catalogo gravado: {0}  ({1:N0} linhas, {2:N0} KB)" -f $OutFile, $L.Count, ((Get-Item -LiteralPath $OutFile).Length / 1KB)) -ForegroundColor Green


