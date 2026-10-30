# =====================================================================
#  Kairo Tweaker :: 00.Core.ps1
#  Nucleo: log, privilegios, catalogo de tweaks, journal/backup, helpers
#  Requer PowerShell 5.1+  |  Sem dependencias externas
# =====================================================================

$global:KT_Version  = '2.0.0'
$global:KT_Title   = 'KAIRO OPTIMIZER'
# Esta pasta (o app). O Core fica em modules\, entao sobe um nivel.
# Usa $MyInvocation.MyCommand.Path como fallback, pois $PSCommandPath
# pode vir vazio quando o script e carregado de outra forma.
$corePath = if ($PSCommandPath) { $PSCommandPath } else { $MyInvocation.MyCommand.Path }
$global:KT_Root     = Split-Path -Parent (Split-Path -Parent $corePath)
# Pasta do acervo, onde ficam os .pow e as ferramentas de terceiros
$global:KT_PastaAcervo = Split-Path -Parent $global:KT_Root
$global:KT_Modules  = Join-Path $global:KT_Root 'modules'
$global:KT_DataDir  = Join-Path $env:ProgramData 'KairoTweaker'
$global:KT_LogFile  = Join-Path $global:KT_DataDir 'kairo-tweaker.log'
$global:KT_Backups  = Join-Path $global:KT_DataDir 'Backups'
$global:KT_Registry = New-Object System.Collections.ArrayList
# Mapa de acoes nomeadas -> scriptblock. Os modulos ADICIONAM chaves aqui;
# nunca reatribuem a variavel inteira (senao um modulo apaga as do outro).
if (-not $global:KT_Acoes) { $global:KT_Acoes = @{} }
$global:KT_Quiet    = $false

# ---------------------------------------------------------------- Log
function Write-KTLog {
    param(
        [string]$Message,
        [ValidateSet('INFO','OK','AVISO','ERRO','TITULO')][string]$Level = 'INFO',
        [switch]$SemTela
    )
    if (-not (Test-Path $global:KT_DataDir)) {
        New-Item -ItemType Directory -Force -Path $global:KT_DataDir | Out-Null
    }
    $stamp = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
    Add-Content -Path $global:KT_LogFile -Value "[$stamp] [$Level] $Message" -Encoding UTF8 -ErrorAction SilentlyContinue
    if ($SemTela) { return }
    switch ($Level) {
        'OK'     { Write-Host $Message -ForegroundColor Green }
        'AVISO'  { Write-Host $Message -ForegroundColor Yellow }
        'ERRO'   { Write-Host $Message -ForegroundColor Red }
        'TITULO' { Write-Host $Message -ForegroundColor Cyan }
        default  { Write-Host $Message -ForegroundColor Gray }
    }
}

# ------------------------------------------------------- Privilegios
function Test-KTAdmin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    (New-Object Security.Principal.WindowsPrincipal($id)).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Assert-KTAdmin {
    if (Test-KTAdmin) { return $true }
    Write-KTLog 'Privilegios de administrador necessarios. Elevando...' 'AVISO'
    try {
        $argList = @('-NoProfile','-ExecutionPolicy','Bypass','-File',"`"$($global:KT_EntryPoint)`"",$global:KT_Args)
        Start-Process -FilePath 'powershell.exe' -ArgumentList $argList -Verb RunAs
        exit 0
    } catch {
        Write-KTLog 'Falha ao elevar. Execute manualmente como Administrador.' 'ERRO'
        if (-not $global:KT_Quiet) { Read-Host 'Pressione ENTER para sair' }
        exit 1
    }
}

# ------------------------------------------------------------ Cores
function Show-KTTitulo {
    Write-Host ''
    Write-Host '  ============================================================' -ForegroundColor DarkCyan
    Write-Host "   KAIRO OPTIMIZER  v$($global:KT_Version)" -ForegroundColor Cyan
    Write-Host '   Tudo do acervo, absorvido, granular e reversivel' -ForegroundColor DarkGray
    Write-Host '  ============================================================' -ForegroundColor DarkCyan
    Write-Host ''
}

# =====================================================================
#  CATALOGO DE TWEAKS
#  Campos por tweak:
#    Id         - identificador unico (usado na CLI)
#    Categoria  - aba onde a tweak aparece no menu
#    Nome       - titulo curto exibido na lista
#    Explicacao - o que a funcao faz, em linguagem simples e honesta
#    Ganho      - o que o usuario deve esperar de melhora (ou "nenhum")
#    Risco      - Baixo | Medio | Alto
#    Reinicio   - $true se precisa reiniciar o Windows
#    Spec/Bcd/Servicos/Comandos - o que sera alterado (tudo reversivel)
# =====================================================================
function Add-KTModule {
    param(
        [Parameter(Mandatory)][string]$Modulo,
        [Parameter(Mandatory)][array]$Tweaks
    )
    foreach ($t in $Tweaks) {
        # Converte o Hashtable em PSCustomObject: assim Group-Object, Where-Object
        # e Add-Member funcionam sobre as propriedades (Id, Nome, Categoria...).
        if ($t -is [hashtable]) {
            $obj = [pscustomobject]@{}
            foreach ($k in $t.Keys) { $obj | Add-Member -NotePropertyName $k -NotePropertyValue $t[$k] }
        } else {
            $obj = $t
        }
        $obj | Add-Member -NotePropertyName Modulo -NotePropertyValue $Modulo -Force
        if ($null -eq $obj.Risco)      { $obj | Add-Member -NotePropertyName Risco -NotePropertyValue 'Baixo' -Force }
        if ($null -eq $obj.Reinicio)   { $obj | Add-Member -NotePropertyName Reinicio -NotePropertyValue $false -Force }
        if ($null -eq $obj.Ativo)      { $obj | Add-Member -NotePropertyName Ativo -NotePropertyValue $false -Force }
        if ($null -eq $obj.Explicacao) { $obj | Add-Member -NotePropertyName Explicacao -NotePropertyValue '' -Force }
        if ($null -eq $obj.Ganho)      { $obj | Add-Member -NotePropertyName Ganho -NotePropertyValue 'Nao ha ganho mensuravel. Feito por seguranca ou organizacao.' -Force }
        [void]$global:KT_Registry.Add($obj)
    }
}

function Get-KTTweak      { param([string]$Id) $global:KT_Registry | Where-Object { $_.Id -eq $Id } }
function Get-KTCategorias { $global:KT_Registry | ForEach-Object { $_.Categoria } | Sort-Object -Unique }

# =====================================================================
#  BACKUP / JOURNAL  ->  todo tweak aplicado fica reversivel
# =====================================================================
function New-KTBackup {
    param([string]$Motivo = 'manual')
    $stamp = (Get-Date).ToString('yyyyMMdd-HHmmss')
    $dir = Join-Path $global:KT_Backups $stamp
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
    [pscustomobject]@{
        Data    = (Get-Date).ToString('o')
        Motivo  = $Motivo
        usuário = "$env:USERDOMAIN\$env:USERNAME"
        SO      = (Get-CimInstance Win32_OperatingSystem).Caption
        Build   = (Get-CimInstance Win32_OperatingSystem).BuildNumber
    } | ConvertTo-Json | Set-Content -Path (Join-Path $dir 'meta.json') -Encoding UTF8
    Set-Content -Path (Join-Path $dir 'journal.jsonl') -Value '' -Encoding UTF8
    return $dir
}

function Add-KTJournal {
    param([Parameter(Mandatory)][hashtable]$Entry)
    if (-not $global:KT_BackupDir) { return }
    $Entry.Data = (Get-Date).ToString('o')
    $Entry | ConvertTo-Json -Compress -Depth 6 |
        Add-Content -Path (Join-Path $global:KT_BackupDir 'journal.jsonl') -Encoding UTF8
}
# =====================================================================
#  REGISTRO
# =====================================================================
function ConvertTo-KTRegPath {
    param([string]$Path)
    if ($Path -match '^(HKLM|HKCU|HKCR):\\')  { return $Path }
    if ($Path -match '^HKU\\(.+?)\\')        { return "HKCU:\$1" }
    if ($Path -match '^HKLM\\')              { return "HKLM:\$(($Path -replace '^HKLM\\',''))" }
    if ($Path -match '^HKCU\\')              { return "HKCU:\$(($Path -replace '^HKCU\\',''))" }
    if ($Path -match '^HKCR\\')              { return "Registry::HKEY_CLASSES_ROOT\$($Path -replace '^HKCR\\','')" }
    return $Path
}

function Get-KTRegValue {
    param([string]$Path, [string]$Nome)
    $p = ConvertTo-KTRegPath $Path
    if (-not (Test-Path $p)) { return @{ Existia = $false } }
    try {
        $k = Get-ItemProperty -Path $p -Name $Nome -ErrorAction Stop
        return @{ Existia = $true; Valor = $k.$Nome; Tipo = "$( (Get-Item $p).GetValueKind($Nome) )" }
    } catch { return @{ Existia = $false } }
}

function Set-KTRegValue {
    param([string]$Path, [string]$Nome, [string]$Tipo, $Valor)
    $p = ConvertTo-KTRegPath $Path
    if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
    switch ($Tipo.ToUpper()) {
        'DWORD'        { New-ItemProperty -Path $p -Name $Nome -Value ([int]$Valor)    -PropertyType DWord        -Force | Out-Null }
        'QWORD'        { New-ItemProperty -Path $p -Name $Nome -Value ([long]$Valor)   -PropertyType QWord        -Force | Out-Null }
        'STRING'       { New-ItemProperty -Path $p -Name $Nome -Value ([string]$Valor) -PropertyType String       -Force | Out-Null }
        'EXPANDSTRING' { New-ItemProperty -Path $p -Name $Nome -Value ([string]$Valor) -PropertyType ExpandString -Force | Out-Null }
        'MULTISTRING'  { New-ItemProperty -Path $p -Name $Nome -Value ([string[]]$Valor) -PropertyType MultiString -Force | Out-Null }
        'BINARY'       { New-ItemProperty -Path $p -Name $Nome -Value ([byte[]]$Valor)  -PropertyType Binary       -Force | Out-Null }
        default        { throw "Tipo de registro desconhecido: $Tipo" }
    }
}

function Remove-KTRegValue {
    param([string]$Path, [string]$Nome)
    $p = ConvertTo-KTRegPath $Path
    if (Test-Path $p) { try { Remove-ItemProperty -Path $p -Name $Nome -ErrorAction Stop } catch { } }
}

# Registra o valor atual antes de sobrescrever (base da reversao)
function Backup-KTReg {
    param([string]$Path, [string]$Nome)
    $e = Get-KTRegValue -Path $Path -Nome $Nome
    Add-KTJournal -Entry @{
        Tipo = 'Reg'; Path = $Path; Nome = $Nome
        Existia = [bool]$e.Existia; TipoValor = [string]$e.Tipo; Valor = $e.Valor
    }
}

# Aplica (ou reverte) uma especificacao declarativa de registro
function Invoke-KTRegSpec {
    param([array]$Spec, [switch]$Reverso)
    foreach ($s in $Spec) {
        if ($Reverso) {
            if ($s.Remover) { Remove-KTRegValue -Path $s.Path -Nome $s.Nome }
        } else {
            Backup-KTReg -Path $s.Path -Nome $s.Nome
            Set-KTRegValue  -Path $s.Path -Nome $s.Nome -Tipo $s.Tipo -Valor $s.Valor
        }
    }
}

# Confere se a tweak ja esta aplicada
function Test-KTRegSpec {
    param([array]$Spec)
    foreach ($s in $Spec) {
        $e = Get-KTRegValue -Path $s.Path -Nome $s.Nome
        if ($s.Remover) { if ($e.Existia) { return $false }; continue }
        if (-not $e.Existia) { return $false }
        if ([string]$e.Valor -ne [string]$s.Valor) { return $false }
    }
    return $true
}
# =====================================================================
#  SERVICOS
# =====================================================================
function Set-KTServiceStart {
    param([string]$Nome, [ValidateSet('Automatic','Manual','Disabled')][string]$Start)
    $svc = Get-Service -Name $Nome -ErrorAction SilentlyContinue
    if (-not $svc) { return $false }
    Add-KTJournal -Entry @{
        Tipo = 'Servico'; Path = $Nome; Nome = $Nome
        Existia = $true; TipoValor = ''; Valor = "$($svc.StartType)"
    }
    try {
        if ($Start -eq 'Disabled' -and $svc.Status -ne 'Stopped') {
            Stop-Service -Name $Nome -Force -ErrorAction SilentlyContinue
        }
        Set-Service -Name $Nome -StartupType $Start -ErrorAction Stop
        return $true
    } catch {
        Write-KTLog "Nao foi possivel alterar o servico '$Nome': $($_.Exception.Message)" 'AVISO' -SemTela
        return $false
    }
}

# =====================================================================
#  BCDEDIT  (somente o que tem reversao confiavel)
# =====================================================================
function Get-KTBcdValue {
    param([string]$Nome)
    $out = & bcdedit.exe /enum '{current}' 2>$null
    $linha = $out | Where-Object { $_ -match "^\s*$([regex]::Escape($Nome))\s+" } | Select-Object -First 1
    if (-not $linha) { return @{ Existia = $false } }
    if ($linha -match '\s(0x[0-9a-fA-F]+)\s*$') { return @{ Existia=$true; Valor=$Matches[1]; Tipo='Hex' } }
    if ($linha -match '\s(Yes|No)\s*$')          { return @{ Existia=$true; Valor=$Matches[1]; Tipo='Bool' } }
    if ($linha -match '\s(\S+)\s*$')             { return @{ Existia=$true; Valor=$Matches[1]; Tipo='Texto' } }
    return @{ Existia = $false }
}

function Set-KTBcdValue {
    param([string]$Nome, [Parameter(Mandatory)][string]$Valor)
    $atual = Get-KTBcdValue -Nome $Nome
    Add-KTJournal -Entry @{
        Tipo = 'BcdEdit'; Path = '{current}'; Nome = $Nome
        Existia = [bool]$atual.Existia; TipoValor = [string]$atual.Tipo; Valor = $atual.Valor
    }
    & bcdedit.exe /set '{current}' $Nome $Valor 2>&1 | Out-Null
}

# =====================================================================
#  COMANDOS  (executa e registra o reverso)
# =====================================================================
function Invoke-KTCommand {
    param([Parameter(Mandatory)][string]$Comando, [Parameter(Mandatory)][string]$Reverso)
    Add-KTJournal -Entry @{ Tipo='Comando'; Path=$Reverso; Nome=$Comando; Existia=$true; TipoValor=''; Valor='' }
    return (& cmd.exe /c $Comando 2>&1)
}

# =====================================================================
#  PONTO DE RESTAURACAO DO SISTEMA
# =====================================================================
function New-KTRestorePoint {
    param([string]$Descricao = 'Antes do Kairo Tweaker')
    try {
        Enable-ComputerRestore -Drive "$env:SystemDrive\" -ErrorAction Stop
        Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore' `
            -Name 'SystemRestorePointCreationFrequency' -Value 0 -ErrorAction SilentlyContinue
        Checkpoint-Computer -Description $Descricao -RestorePointType 'MODIFY_SETTINGS' -ErrorAction Stop
        Write-KTLog "Ponto de restauracao criado: $Descricao" 'OK'
        return $true
    } catch {
        Write-KTLog "Nao foi possivel criar ponto de restauracao: $($_.Exception.Message)" 'AVISO'
        return $false
    }
}

# =====================================================================
#  REVERSAO
# =====================================================================
function Get-KTBackups {
    if (-not (Test-Path $global:KT_Backups)) { return }
    Get-ChildItem $global:KT_Backups -Directory | Sort-Object Name -Descending
}

function Revert-KTBackup {
    param([Parameter(Mandatory)][string]$Diretorio, [switch]$Silencioso)
    $j = Join-Path $Diretorio 'journal.jsonl'
    if (-not (Test-Path $j)) { Write-KTLog "Journal nao encontrado em $Diretorio" 'ERRO'; return $false }
    $entradas = @(Get-Content $j -Encoding UTF8 | Where-Object { $_.Trim() } | ForEach-Object { $_ | ConvertFrom-Json })
    [array]::Reverse($entradas)

    $ok = 0; $falha = 0
    foreach ($e in $entradas) {
        try {
            switch ($e.Tipo) {
                'Reg' {
                    if ($e.Existia) {
                        Set-KTRegValue -Path $e.Path -Nome $e.Nome -Tipo $e.TipoValor -Valor $e.Valor
                    } else {
                        Remove-KTRegValue -Path $e.Path -Nome $e.Nome
                    }
                }
                'Servico' {
                    $map = @{ Automatic='Automatic'; Auto='Automatic'; Manual='Manual';
                              Demand='Manual'; Disabled='Disabled' }
                    $novo = $map[[string]$e.Valor]
                    if ($novo) { try { Set-Service -Name $e.Path -StartupType $novo -ErrorAction Stop } catch { $falha++ } }
                }
                'BcdEdit' {
                    if ($e.Existia) { & bcdedit.exe /set '{current}' $e.Nome $e.Valor 2>&1 | Out-Null }
                    else            { & bcdedit.exe /deletevalue '{current}' $e.Nome 2>&1 | Out-Null }
                }
                'Tarefa' {
                    # $e.Valor guarda o estado original: Habilitada ou Desabilitada
                    $flag = if ($e.Valor -eq 'Desabilitada') { '/Disable' } else { '/Enable' }
                    $null = & schtasks.exe /change /tn $e.Path $flag 2>&1
                }
                'ArquivoJogo' {
                    # Config gerada pelo Kairo: se o arquivo nao existia antes, remove.
                    if ($e.Existia) {
                        Write-KTLog "Arquivo de jogo mantido (existia antes): $($e.Nome)" 'AVISO' -SemTela
                    } else {
                        Remove-Item -LiteralPath $e.Path -Force -ErrorAction SilentlyContinue
                    }
                }
                'Comando' { $null = & cmd.exe /c ([string]$e.Path) 2>&1 }
            }
            $ok++
        } catch {
            $falha++
            Write-KTLog "Falha ao reverter $($e.Tipo) $($e.Nome): $($_.Exception.Message)" 'AVISO' -SemTela
        }
    }
    Write-KTLog "Reversao concluida - entradas processadas: $ok / falhas: $falha" 'OK'
    if (-not $Silencioso) { Write-KTLog 'Reinicie o Windows para que todas as mudancas tenham efeito.' 'TITULO' }
    return $true
}

