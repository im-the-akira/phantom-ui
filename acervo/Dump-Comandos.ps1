# =====================================================================
#  Dump-Comandos.ps1
#  Le os arquivos do acervo UM A UM e copia, VERBATIM, todos os comandos
#  que cada um executa -> COMANDOS-DO-ACERVO.md
#
#  Para .reg converte a sintaxe de arquivo em "reg add"/"reg delete".
#  Para .ps1/.cmd/.bat copia a linha original.
#  A coluna "Categoria" reaproveita as mesmas regras do catalogo.
# =====================================================================
[CmdletBinding()]
param(
    [string]$Root = '',
    [string]$OutFile = '',
    [string]$InCat = ''
)
$ErrorActionPreference = 'Continue'

$__here = if ($PSCommandPath) { Split-Path -Parent $PSCommandPath } else { (Get-Location).Path }
if ([string]::IsNullOrWhiteSpace($Root))    { $Root = Split-Path -Parent $__here }
if ([string]::IsNullOrWhiteSpace($OutFile)) { $OutFile = Join-Path $Root 'COMANDOS-DO-ACERVO.md' }
$Root = (Resolve-Path -LiteralPath $Root).Path.TrimEnd('\')
$toolsFilter = Join-Path $Root '_tools\*'
$quarentena  = @((Join-Path $Root 'old\*'), (Join-Path $Root 'OLD\*'),
                 (Join-Path $Root '_old\*'), (Join-Path $Root 'arquivados\*'))

# --------------------------------------------------------------- encoding
function Read-TextSmart {
    param([string]$Path)
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -eq 0) { return '' }
    if ($bytes.Length -ge 2 -and $bytes[0] -eq 0xFF -and $bytes[1] -eq 0xFE) {
        return [System.Text.Encoding]::Unicode.GetString($bytes, 2, $bytes.Length - 2)
    }
    if ($bytes.Length -ge 2 -and $bytes[0] -eq 0xFE -and $bytes[1] -eq 0xFF) {
        return [System.Text.Encoding]::BigEndianUnicode.GetString($bytes, 2, $bytes.Length - 2)
    }
    $zeros = 0; $sample = [Math]::Min($bytes.Length, 65536)
    for ($i = 0; $i -lt $sample; $i++) { if ($bytes[$i] -eq 0) { $zeros++ } }
    if ($zeros -gt ($sample / 4)) { return [System.Text.Encoding]::Unicode.GetString($bytes) }
    $t = [System.Text.Encoding]::UTF8.GetString($bytes)
    if ($t -notmatch [char]0xFFFD) { return $t }
    return [System.Text.Encoding]::GetEncoding(1252).GetString($bytes)
}

# ------------------------------------------------- regras do catalogo (reuso)
$script:Regras = New-Object System.Collections.Generic.List[object]
function Add-Rule { param([string]$C, [string]$Re) $script:Regras.Add([pscustomobject]@{ C = $C; Re = $Re.ToLowerInvariant() }) }
if ([string]::IsNullOrWhiteSpace($InCat)) { $InCat = Join-Path $__here 'Build-Catalog.ps1' }
if (-not [string]::IsNullOrWhiteSpace($InCat)) {
    $InCat = if (Test-Path -LiteralPath $InCat) { $InCat } else { Join-Path $__here $InCat }
}
if ($InCat -and (Test-Path -LiteralPath $InCat)) {
    $bc = [IO.File]::ReadAllText($InCat)
    foreach ($m in [regex]::Matches($bc, "Add-Rule\s+'(\w+)'\s+'((?:[^']|'')*)'")) {
        $script:Regras.Add([pscustomobject]@{ C = $m.Groups[1].Value; Re = $m.Groups[2].Value.ToLowerInvariant() })
    }
    Write-Host ("Regras de categoria carregadas: {0} (de {1})" -f $script:Regras.Count, (Split-Path -Leaf $InCat)) -ForegroundColor DarkGray
}
function Get-Cat {
    param([string]$Cmd)
    $t = $Cmd.ToLowerInvariant()
    foreach ($r in $script:Regras) { if ($t.Contains($r.Re) -or $t -match $r.Re) { return $r.C } }
    return '01'
}


# ------------------------------------------------------------- deteccao
# Verbos de linha de comando que alteram o sistema
$reCmd = '(?i)^(?:@+|start\s+""\s+|start\s+/[a-z]\s+|call\s+)?(' +
         'reg|sc|bcdedit|powercfg|netsh|wmic|schtasks|taskkill|icacls|takeown|' +
         'del|erase|rd|rmdir|attrib|cipher|dism|sfc|chkdsk|compact|vssadmin|wevtutil|' +
         'cleanmgr|msiexec|regini|bitsadmin|fsutil|bcdboot|bootcfg|winget|choco|scoop|' +
         'manage-bde|manage-ntfs|net\s+(start|stop)|robocopy|mklink|cipherw)\b'
# Cmdlets do PowerShell que alteram o sistema
$rePS  = '(?i)^(?:@+|&\s*)?(' +
         'set-itemproperty|set-item|set-service|new-itemproperty|new-item|new-service|' +
         'remove-itemproperty|remove-item|remove-service|remove-appxpackage|' +
         'remove-appxprovisionedpackage|stop-service|start-service|restart-service|' +
         'set-content|add-content|clear-eventlog|clear-recyclebin|set-mppreference|' +
         'get-appxpackage|get-appxprovisionedpackage|get-scheduledtask|get-netadapter|' +
         'register-scheduledtask|new-scheduledtaskaction|enable-scheduledtask|' +
         'disable-scheduledtask|set-nettcpip|set-dnsclientserveraddress|' +
         'disable-netadapter|enable-netadapter|import-|export-|invoke-webrequest|' +
         'set-executionpolicy|optimize-volume|set-netfirewallprofile)\b'
# Tipos do catalogo Kairo -> tipo de registro (aceita o prefixo KT_ ou nao)
$ktTipo = @{ 'KT_DWORD'='REG_DWORD'; 'KT_QWORD'='REG_QWORD'; 'KT_STRING'='REG_SZ'
             'KT_MULTISZ'='REG_MULTI_SZ'; 'KT_BINARY'='REG_BINARY'; 'KT_EXPANDSTRING'='REG_SZ'
             'DWORD'='REG_DWORD'; 'QWORD'='REG_QWORD'; 'STRING'='REG_SZ'; 'SZ'='REG_SZ'
             'MULTISZ'='REG_MULTI_SZ'; 'BINARY'='REG_BINARY'; 'EXPANDSTRING'='REG_SZ'
             'DWORD32'='REG_DWORD'; 'DWORD64'='REG_QWORD' }

# reg.exe nao aceita "HKLM:\..." (com dois-pontos) nem "HKCU:\...". Normaliza.
function Fix-RaizCmd {
    param([string]$C)
    return ($C -replace '(?i)\b(HKLM|HKCU|HKCR|HKU|HKCC):\\', '$1\')
}

function Test-LinhaComando {
    param([string]$L)
    $t = ($L -replace '\s+', ' ').Trim()
    if ($t -eq '') { return $false }
    $t2 = ($t -replace '^[@:]{1,2}\s*', '').TrimStart('@').Trim()
    if ($t2 -eq '') { return $false }
    if ($t2 -match '^(rem\b|::|#)(?![a-fA-F0-9]{4}\b)') { return $false }
    if ($t2 -match '^(echo[\s\.]|echo$|\.|title\s|color\s|cls$|pause$|setlocal|endlocal)') { return $false }
    if ($t2 -match '^(set\s|exit|goto|shift|timeout|chcp|mode|cd\s|cd$|pushd|popd)') { return $false }
    if ($t2 -match '^[)\(]+$') { return $false }
    if ($t2 -match '^(if|for|do|else|elseif)\b') { return $false }
    if ($t2 -match '^[{\]]$') { return $false }
    return $true
}

function Esc { param([string]$S) if ($null -eq $S) { return '' }; ($S -replace '\|', '\|').Trim() }


# --------------------------------------------- helpers de linha / extracao
function Get-LineOf {
    param([string]$Text, [int]$Index)
    if ($Index -le 0) { return 0 }
    if ($Index -gt $Text.Length) { $Index = $Text.Length }
    return ([regex]::Matches($Text.Substring(0, $Index), "`n").Count + 1)
}

function Get-Campo {
    param([string]$Blk, [string]$Nome, [string]$Padrao = '')
    $m = [regex]::Match($Blk, ("\b" + $Nome + "\s*=\s*'([^']*)'"))
    if ($m.Success) { return $m.Groups[1].Value }
    return $Padrao
}

function Get-RegTipo {
    param([string]$Data)
    $d = $Data.Trim()
    if ($d -match '^(?i)dword:')            { return 'REG_DWORD' }
    if ($d -match '^(?i)qword:')            { return 'REG_QWORD' }
    if ($d -match '^(?i)hex\(\d+\):')       { return 'REG_EXPAND_SZ' }
    if ($d -match '^(?i)hex(,[0-9a-f]+)?:') { return 'REG_BINARY' }
    if ($d -match '^(?i)expand')            { return 'REG_SZ' }
    if ($d -match '^(?i)multi_sz')          { return 'REG_MULTI_SZ' }
    return 'REG_SZ'
}

# Corta dados enormos e marca: alguns .reg do acervo estao "fundidos" (varios
# cabecalhos de versao no mesmo arquivo) e a associacao chave/valor fica torta.
function Curta-Dado {
    param([string]$V)
    $v = $V -replace '^(?i)dword:','' -replace '^(?i)qword:','' `
            -replace '^(?i)hex\(\d+\):','' -replace '^(?i)hex,[0-9a-f]+:','' -replace '^(?i)expand:',''
    # Dados corrompidos: alguns .reg do acervo tem varias cabecalhos de versao
    # fundidos e o valor vira lixo em CJK. Melhor dizer isso do que mostrar lixo.
    if ($v -match '[^\x20-\x7E\u00A0-\u00FF]') {
        $limpo = ($v -replace '[^\x20-\x7E]', '.')
        if ($limpo.Length -gt 150) { $limpo = $limpo.Substring(0, 147) + '...' }
        return '<hex corrompido: ' + $limpo + '>'
    }
    if ($v.Length -gt 220) { $v = $v.Substring(0, 217) + '...' }
    return $v
}
function Norm-Raiz {
    param([string]$K)
    return ($K -replace '^HKEY_LOCAL_MACHINE','HKLM' -replace '^HKEY_CURRENT_USER','HKCU' `
            -replace '^HKEY_CLASSES_ROOT','HKCR'  -replace '^HKEY_USERS','HKU' `
            -replace '^HKEY_CURRENT_CONFIG','HKCC')
}

# Converte a sintaxe de .reg em comando reg.exe equivalente
function Get-RegComandos {
    param([string]$Text)
    $out = New-Object System.Collections.Generic.List[object]
    $key = ''
    foreach ($mm in [regex]::Matches($Text, '(?m)^[ \t]*(-?)\[([^\]\r\n]+)\][ \t]*$')) {
        $key = $mm.Groups[2].Value.Trim()
        if ($mm.Groups[1].Value -eq '-') {
            $out.Add([pscustomobject]@{ L = (Get-LineOf $Text $mm.Index); C = ('reg delete "' + (Norm-Raiz $key) + '" /f'); T = 'reg' })
        }
    }
    foreach ($mm in [regex]::Matches($Text, '(?m)^[ \t]*"([^"]+)"\s*=\s*(.*)$')) {
        if ($key -eq '') { continue }
        $k2 = Norm-Raiz $key
        $nome = $mm.Groups[1].Value
        $val  = $mm.Groups[2].Value.Trim()
        $ln   = Get-LineOf $Text $mm.Index
        if ($val -eq '-') {
            $out.Add([pscustomobject]@{ L = $ln; C = ('reg delete "' + $k2 + '" /v "' + $nome + '" /f'); T = 'reg' })
        }
        else {
            $tipo = Get-RegTipo $val
            $d = Curta-Dado $val
            $out.Add([pscustomobject]@{ L = $ln; C = ('reg add "' + $k2 + '" /v "' + $nome + '" /t ' + $tipo + ' /d "' + $d + '" /f'); T = 'reg' })
        }
    }
    return $out
}

# Converte os blocos declarativos do Kairo Tweaker (Spec/Servicos/Bcd/Comandos)
function Get-KtComandos {
    param([string]$Text)
    $out = New-Object System.Collections.Generic.List[object]
    foreach ($mSpec in [regex]::Matches($Text, "(?s)\bSpec\s*=\s*@\((.*?)\r?\n\s*\)")) {
        $base = Get-LineOf $Text $mSpec.Groups[1].Index; $i = 0
        foreach ($mm in [regex]::Matches($mSpec.Groups[1].Value, '(?s)@\{.*?\}')) {
            $p = Get-Campo $mm.Value 'Path'
            if ($p) {
                $n = Get-Campo $mm.Value 'Nome'; $t = Get-Campo $mm.Value 'Tipo' 'KT_DWord'; $v = Get-Campo $mm.Value 'Valor' '0'
                if ($ktTipo.ContainsKey($t.ToUpperInvariant())) { $t = $ktTipo[$t.ToUpperInvariant()] }
                $out.Add([pscustomobject]@{ L = ($base + $i); T = 'reg'
                    C = (Fix-RaizCmd ('reg add "' + $p + '" /v "' + $n + '" /t ' + $t + ' /d "' + $v + '" /f')) })
            }
            $i++
        }
    }
    foreach ($mSvc in [regex]::Matches($Text, "(?s)\bServicos\s*=\s*@\((.*?)\r?\n\s*\)")) {
        $base = Get-LineOf $Text $mSvc.Groups[1].Index; $i = 0
        foreach ($mm in [regex]::Matches($mSvc.Groups[1].Value, '(?s)@\{.*?\}')) {
            $n = Get-Campo $mm.Value 'Nome'
            if ($n) {
                $s = Get-Campo $mm.Value 'Inicio' 'Disabled'
                $out.Add([pscustomobject]@{ L = ($base + $i); T = 'sc'; C = ('sc config "' + $n + '" start= ' + $s.ToLower()) })
            }
            $i++
        }
    }
    foreach ($mBcd in [regex]::Matches($Text, "(?s)\bBcd\s*=\s*@\((.*?)\r?\n\s*\)")) {
        $base = Get-LineOf $Text $mBcd.Groups[1].Index; $i = 0
        foreach ($mm in [regex]::Matches($mBcd.Groups[1].Value, '(?s)@\{.*?\}')) {
            $n = Get-Campo $mm.Value 'Nome'
            if ($n) {
                $v = Get-Campo $mm.Value 'Valor'
                $out.Add([pscustomobject]@{ L = ($base + $i); T = 'bcdedit'; C = ('bcdedit /set {current} ' + $n + ' ' + $v) })
            }
            $i++
        }
    }
    foreach ($mCmd in [regex]::Matches($Text, "(?s)\bComandos\s*=\s*@\((.*?)\r?\n\s*\)")) {
        $base = Get-LineOf $Text $mCmd.Groups[1].Index; $i = 0
        foreach ($mm in [regex]::Matches($mCmd.Groups[1].Value, '(?s)@\{.*?\}')) {
            $c = Get-Campo $mm.Value 'Cmd'
            if ($c) { $out.Add([pscustomobject]@{ L = ($base + $i); T = 'cmd'; C = $c }) }
            $i++
        }
    }
    return $out
}


# =====================================================================
#  MAIN
# =====================================================================
$exts = @('.reg', '.ps1', '.cmd', '.bat')
$files = Get-ChildItem -LiteralPath $Root -Recurse -Force -File -ErrorAction SilentlyContinue |
         Where-Object { $exts -contains $_.Extension.ToLower() } |
         Where-Object {
             $f = $_.FullName
             ($f -notlike $toolsFilter) -and ($f -notlike '*\.git\*') -and
             (-not ($quarentena | Where-Object { $f -like $_ }))
         } | Sort-Object FullName

Write-Host ("Varrendo {0} arquivos em {1}" -f $files.Count, $Root) -ForegroundColor Cyan

$L = New-Object System.Collections.Generic.List[string]
$resumo = New-Object System.Collections.Generic.List[object]
$catCount = @{}
$totalCmd = 0
$idx = 0

foreach ($f in $files) {
    $rel = $f.FullName.Substring($Root.Length).TrimStart('\')
    try { $text = Read-TextSmart $f.FullName } catch { continue }
    if ([string]::IsNullOrWhiteSpace($text)) { continue }
    $lines = $text -split "`r?`n"
    $ext   = $f.Extension.ToLower()

    $cmds = New-Object System.Collections.Generic.List[object]

    if ($ext -eq '.reg') {
        foreach ($c in (Get-RegComandos $text)) { $cmds.Add($c) }
    }
    else {
        foreach ($c in (Get-KtComandos $text)) { $cmds.Add($c) }
        # linhas literais: junta continuacao com ^ antes de testar
        $buf = ''; $bufL = 0
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $raw = $lines[$i]
            if ($buf -eq '') { $bufL = $i + 1 }
            $t = $raw.Trim()
            if ($t -match '\^$') { $buf = $buf + ' ' + ($t -replace '\^$',''); continue }
            $t = (($buf + ' ' + $t) -replace '\s+', ' ').Trim()
            $buf = ''
            if (-not (Test-LinhaComando $t)) { continue }
            if ($t -match $reCmd -or $t -match $rePS) {
                $tipo = 'cmd'
                if ($t -match $reCmd) { $tipo = ([regex]::Match($t, $reCmd).Groups[1].Value).ToLower() }
                else { $tipo = 'ps' }
                $cmds.Add([pscustomobject]@{ L = $bufL; T = $tipo; C = $t })
            }
        }
    }

    # dedupe por linha+comando
    $vistos = @{}
    $limpos = New-Object System.Collections.Generic.List[object]
    foreach ($c in $cmds) {
        $chave = "$($c.L)|$($c.C)"
        if ($vistos.ContainsKey($chave)) { continue }
        $vistos[$chave] = $true
        $limpos.Add($c)
    }
    $ord = @($limpos | Sort-Object L, C)
    if ($ord.Count -eq 0) { continue }

    $idx++
    $totalCmd += $ord.Count
    $resumo.Add([pscustomobject]@{ N = $idx; Rel = $rel; Ext = $ext; Linhas = $lines.Count; Cmd = $ord.Count })

    $L.Add('')
    $L.Add(('### {0:00} · `{1}`' -f $idx, (Split-Path $rel -Leaf)))
    $L.Add('')
    $L.Add(('_{0} · {1:N0} linhas · **{2:N0} comandos**_' -f $rel, $lines.Count, $ord.Count))
    $hdrs = ([regex]::Matches($text, '(?i)windows\s+registry\s+editor')).Count
    if ($hdrs -gt 1) {
        $L.Add('')
        $L.Add(('> **Atencao:** este arquivo tem **{0} cabecalhos de versao** (`.reg` fundido). A associacao' -f $hdrs))
        $L.Add('> chave/valor fica torta — confira o comando no arquivo original antes de usar.')
    }
    $L.Add('')
    $L.Add('| Linha | Tipo | Cat | Comando |')
    $L.Add('|---:|:---:|:---:|---|')
    foreach ($c in $ord) {
        $cat = Get-Cat $c.C
        if ($catCount.ContainsKey($cat)) { $catCount[$cat]++ } else { $catCount[$cat] = 1 }
        $L.Add(('| {0} | {1} | {2} | `{3}` |' -f $c.L, (Esc $c.T), $cat, (Esc $c.C)))
    }
}

# =====================================================================
#  CABECALHO + INDICE
# =====================================================================
$head = New-Object System.Collections.Generic.List[string]
$head.Add('# COMANDOS DO ACERVO')
$head.Add('')
$head.Add(('> Todos os comandos executados por **{0} arquivos** de tweak do acervo, copiados' -f $resumo.Count))
$head.Add(('> linha a linha e na ordem original do arquivo. **{0:N0} comandos** no total.' -f $totalCmd))
$head.Add(('> Gerado em {0} a partir de `{1}`.' -f (Get-Date -Format 'dd/MM/yyyy HH:mm'), $Root))
$head.Add('')
$head.Add('| Coluna | Significado |')
$head.Add('|---|---|')
$head.Add('| **Linha** | Número da linha no arquivo de origem (no `.reg`, a linha do valor) |')
$head.Add('| **Tipo** | `reg` `sc` `bcdedit` `cmd` `ps` — verbo executado |')
$head.Add('| **Cat** | Categoria EXM V10 inferida (mesmas regras do catalogo) |')
$head.Add('| **Comando** | Comando **verbatim**, pronto para copiar |')
$head.Add('')
$head.Add('Arquivos `.reg` teve a sintaxe de arquivo convertida para `reg add` / `reg delete`.')
$head.Add('Blocos declarativos do Kairo Tweaker (`Spec`/`Servicos`/`Bcd`/`Comandos`) foram')
$head.Add('também convertidos para os comandos que eles executam de fato.')
$head.Add('')
$head.Add('## Resumo por categoria')
$head.Add('')
$head.Add('| Cat | Categoria | Comandos |')
$head.Add('|:---:|---|---:|')
foreach ($g in ($catCount.GetEnumerator() | Sort-Object Name)) {
    $nomeCat = switch ($g.Name) {
        '01' { 'General System Optimizations' } '02' { 'Power / Energia' }
        '03' { 'Teclado e mouse' }            '04' { 'GPU e jogos' }
        '05' { 'CPU' }                        '06' { 'Limpeza do PC' }
        '07' { 'Debloat e privacidade' }      '08' { 'Armazenamento' }
        '09' { 'Memoria RAM' }                '10' { 'Extras / QOL' }
        '11' { 'Remover aplicativos' }        '12' { 'Rede e internet' }
        'F'  { 'Correcoes e reversoes' }      default { '—' }
    }
    $head.Add(('| {0} | {1} | {2:N0} |' -f $g.Name, $nomeCat, $g.Value))
}
$head.Add('')
$head.Add('## Indice de arquivos')
$head.Add('')
$head.Add('| # | Arquivo | Tipo | Linhas | Comandos |')
$head.Add('|---:|---|:---:|---:|---:|')
foreach ($x in $resumo) {
    $head.Add(('| {0:00} | `{1}` | {2} | {3:N0} | {4:N0} |' -f $x.N, $x.Rel, $x.Ext, $x.Linhas, $x.Cmd))
}

$saida = New-Object System.Collections.Generic.List[string]
foreach ($h in $head) { $saida.Add($h) }
$saida.Add('')
$saida.Add('---')
$saida.Add('')
$saida.Add('## Comandos por arquivo')
$saida.Add('')
foreach ($l in $L) { $saida.Add($l) }

[IO.File]::WriteAllLines($OutFile, $saida, (New-Object System.Text.UTF8Encoding $true))
Write-Host ("Comandos: {0} em {1} arquivos -> {2} ({3:N1} KB)" -f $totalCmd, $resumo.Count, $OutFile, ((Get-Item $OutFile).Length / 1KB)) -ForegroundColor Green
Write-Host ("Linhas do documento: {0}" -f $saida.Count) -ForegroundColor Green
