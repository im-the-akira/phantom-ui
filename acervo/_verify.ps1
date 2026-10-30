param(
    [string]$Root = '',
    [string]$Doc = ''
)
$ErrorActionPreference = 'Continue'
$__here = if ($PSCommandPath) { Split-Path -Parent $PSCommandPath } else { (Get-Location).Path }
if ([string]::IsNullOrWhiteSpace($Root)) { $Root = Split-Path -Parent $__here }
$tools = $__here

# O documento pode ser o unico consolidado ou o catalogo gerado sozinho
if ([string]::IsNullOrWhiteSpace($Doc)) {
    $cands = @('ACERVO-KAIRO-TWEAKS.md', 'ACERVO-DE-TWEAKS_EXM-V10.md')
    $Doc = ($cands | Where-Object { Test-Path -LiteralPath (Join-Path $Root $_) } | Select-Object -First 1)
    if (-not $Doc) { $Doc = $cands[0] }
}
$md = Join-Path $Root $Doc

$fail = 0
function Ok  { param($m) Write-Host ("  [ OK ] " + $m) -ForegroundColor Green }
function Bad { param($m) Write-Host ("  [!! ] " + $m) -ForegroundColor Red; $script:fail++ }
function Hdr { param($m) Write-Host ("`n== " + $m + " ==") -ForegroundColor Cyan }

# ------------------------------------------------------------------ ops.json
Hdr 'ops.json'
$opsPath = Join-Path $tools 'ops.json'
if (-not (Test-Path -LiteralPath $opsPath)) { Bad 'ops.json ausente'; }
else {
    $o = Get-Content -LiteralPath $opsPath -Raw | ConvertFrom-Json
    $o = @($o)
    Ok ("operacoes carregadas: " + $o.Count)
    if ($o.Count -lt 5000) { Bad ('volume inesperadamente baixo: ' + $o.Count) }
    foreach ($p in @('File','Line','Kind','Action','Path','Name','Type','Data','Raw','Note')) {
        if (-not ($o[0].PSObject.Properties.Name -contains $p)) { Bad ("campo ausente no JSON: " + $p) }
    }
    $pw = @($o | Where-Object { $_.Kind -eq 'Power' -and $_.Name -and $_.Path -like 'Power:SUB_*' })
    $pwNoVal = @($pw | Where-Object { [string]::IsNullOrWhiteSpace($_.Data) })
    if ($pwNoVal.Count -eq 0) { Ok ('powercfg com alias sempre tem valor (' + $pw.Count + ' ops)') }
    else { Bad ('powercfg sem valor: ' + $pwNoVal.Count + ' de ' + $pw.Count) }
    $kinds = $o | Group-Object Kind | Sort-Object Count -Descending
    foreach ($k in $kinds) { Write-Host ("       {0,-10} {1,6}" -f $k.Name, $k.Count) }
    $kt = @($o | Where-Object { $_.Type -match '(?i)^KT_' })
    if ($kt.Count -eq 0) { Ok 'tipos KT_* normalizados' } else { Bad ('tipos KT_* restantes: ' + $kt.Count) }
    $dyn = @($o | Where-Object { $_.Path -match '%%' })
    Write-Host ("       caminhos %%residuais: " + $dyn.Count + " de " + $o.Count + " ops")
    # Variaveis de lote (%PROFILENAME%, %%i de FOR /L) sao intrinsecamente
    # impossiveis de resolver por analise estatica. O extrator ja marca essas
    # operacoes com Note='alvo dinamico' e o catalogo as documenta no Apendice C.3.
    # So conta como falha se sobrar variavel em arquivo que NAO e .cmd/.bat.
    $un = @($o | Where-Object { $_.Path -match '%%' -or $_.Path -match '%[A-Za-z]+%' })
    $unHard = @($un | Where-Object { $_.File -notmatch '(?i)\.(cmd|bat)$' })
    $unSoft = @($un | Where-Object { $_.File -match '(?i)\.(cmd|bat)$' })
    if ($unHard.Count -gt 0) { Bad ('variavel de lote nao resolvida fora de .cmd/.bat: ' + $unHard.Count) }
    else { Ok ('nenhuma variavel de lote fora de .cmd/.bat (' + $unSoft.Count + ' em .cmd/.bat, esperado: FOR/L dinamico)') }
}

# ---------------------------------------------------------------- markdown
Hdr 'Markdown'
if (-not (Test-Path -LiteralPath $md)) { Bad 'Markdown ausente' }
else {
    $lines = Get-Content -LiteralPath $md
    Ok ('linhas: ' + $lines.Count + '  (' + [Math]::Round((Get-Item $md).Length / 1KB) + ' KB)')

    $badTable = New-Object System.Collections.Generic.List[string]
    $expect  = 0
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $l = $lines[$i]
        if ($l -match '^\|\s*-{2,}') { continue }
        if ($l -match '^\|') {
            $pipes = ([regex]::Matches($l, '(?<!\\)\|')).Count
            if ($expect -eq 0) { $expect = $pipes }
            elseif ($pipes -ne $expect) { $badTable.Add(("L{0}: {1} pipes, esperado {2}" -f ($i + 1), $pipes, $expect)) }
        }
        else { $expect = 0 }
    }
    if ($badTable.Count -eq 0) { Ok 'nenhuma tabela quebrada' }
    else { Bad ('tabelas quebradas: ' + $badTable.Count); $badTable | Select-Object -First 8 | ForEach-Object { Write-Host ("       " + $_) } }

    $moji = New-Object System.Collections.Generic.List[string]
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -cmatch 'Ã.|â€|Â.|ï¿½|\uFFFD') { $moji.Add(('L' + ($i + 1))) }
    }
    if ($moji.Count -eq 0) { Ok 'sem mojibake' } else { Bad ('mojibake em ' + $moji.Count + ' linhas'); $moji | Select-Object -First 5 | ForEach-Object { Write-Host ("       " + $_) } }

    $junk = @($lines | Select-String -Pattern '^\| \*\*[a-z]\d{1,2}\*\* \|' )
    if ($junk.Count -eq 0) { Ok 'sem rotulos de menu sem sentido' } else { Bad ('rotulos sem sentido: ' + $junk.Count) }

    $secs = @('## Como ler este catalogo','## Indice de categorias','## Apendice A','## Apendice B','## Apendice C')
    foreach ($s in $secs) {
        if (@($lines | Select-String -SimpleMatch $s).Count -gt 0) { Ok ('presente: ' + $s) }
        else { Bad ('ausente: ' + $s) }
    }
    $cats = @('01','02','03','04','05','06','07','08','09','10','11','12')
    foreach ($c in $cats) {
        # no documento unico os titulos sao rebaixados um nivel (### NN · ...)
        if (@($lines | Select-String -Pattern ('^#{2,4} ' + $c + ' ')).Count -ge 1) { }
        else { Bad ("categoria sem secao propria: " + $c) }
    }
    $total = 0
    foreach ($m in ($lines | Select-String -Pattern '^\| \*\*(\d\d|F)\*\* \| .*? \| (\d+) \|')) { $total += [int]$m.Matches[0].Groups[2].Value }
    if ($o.Count -gt 0 -and $total -ne $o.Count) { Bad ("soma do indice ($total) != ops.json ($($o.Count))") }
    elseif ($o.Count -gt 0) { Ok ("soma do indice confere: $total operacoes") }
}

Hdr 'RESULTADO'
if ($fail -eq 0) { Write-Host 'TUDO OK' -ForegroundColor Green } else { Write-Host ("$fail PROBLEMA(S)") -ForegroundColor Red }
exit $fail
