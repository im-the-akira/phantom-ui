param([string]$Out = '')
$ErrorActionPreference = 'Stop'
$tools = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($Out)) { $Out = Join-Path (Split-Path -Parent $tools) 'MEU-TWEAKER.cmd' }
$enc = [Text.Encoding]::GetEncoding(1252)
$lines = [IO.File]::ReadAllLines($Out, $enc)
$fail = 0
function Bad { param($m) Write-Host ('  [!!] ' + $m) -ForegroundColor Red; $script:fail++ }
function Ok  { param($m) Write-Host ('  [OK] ' + $m) -ForegroundColor Green }

# 1) fora de ASCII puro? (caret notation / mojibake / acento solto)
$bad = New-Object System.Collections.Generic.List[string]
for ($i = 0; $i -lt $lines.Count; $i++) {
    foreach ($ch in $lines[$i].ToCharArray()) {
        if ([int]$ch -gt 126) { $bad.Add(('L{0}: {1}' -f ($i + 1), $lines[$i].Trim())); break }
    }
}
if ($bad.Count -eq 0) { Ok ('100% ASCII: ' + $lines.Count + ' linhas') }
else { Bad ('linhas fora de ASCII: ' + $bad.Count); $bad | Select-Object -First 8 | ForEach-Object { Write-Host ('       ' + $_) } }

# 2) gotos sem label / labels duplicados
$labels = @{}; $dups = New-Object System.Collections.Generic.List[string]
foreach ($l in $lines) { if ($l -match '^:([A-Za-z0-9_]+)$') { if ($labels.ContainsKey($Matches[1])) { $dups.Add($Matches[1]) } ; $labels[$Matches[1]] = $true } }
$gotos = New-Object System.Collections.Generic.List[string]
foreach ($l in $lines) { foreach ($m in [regex]::Matches($l, '(?i)\bgoto\s+([A-Za-z0-9_]+)')) { $gotos.Add($m.Groups[1].Value) } }
$orf = @($gotos | Where-Object { -not $labels.ContainsKey($_) } | Select-Object -Unique)
if ($orf.Count -eq 0 -and $dups.Count -eq 0) { Ok ('labels: ' + $labels.Count + ' · nenhum goto orfao · nenhum duplicado') }
else { if ($orf.Count) { Bad ('gotos sem label: ' + ($orf -join ', ')) }; if ($dups.Count) { Bad ('labels duplicados: ' + ($dups -join ', ')) } }

# 3) categorias no menu principal (dispatch em if "%Op%"=="N" goto c_XX_p1)
#    categorias de fabricante (04N/04A/04I/05A/05I) sao opcionais: so aparecem
#    quando o acervo tem tweaks daquele fabricante.
$nCat = @()
foreach ($l in $lines) {
    if ($l -match '^if "%Op%"=="\d+" goto c_([0-9A-F]+[NI]?)_p1') { $nCat += $Matches[1] }
}
$nCat = @($nCat | Select-Object -Unique)
Ok ('categorias roteadas no menu: ' + $nCat.Count + ' -> ' + ($nCat -join ' '))
$espera = @('01','02','03','04','05','06','07','08','09','10','11','12','F')
$faltam = @($espera | Where-Object { $nCat -notcontains $_ })
if ($faltam.Count -gt 0) { Bad ('categorias obrigatorias ausentes: ' + ($faltam -join ', ')) } else { Ok ('todas as categorias obrigatorias presentes') }
$opc = @('04N','04A','04I','05A','05I')
Ok ('fabricantes presentes no menu: ' + ((@($opc | Where-Object { $nCat -contains $_ })) -join ', '))

# 4) telas de detalhe e de comandos por ajuste
$nDt  = @($lines | Select-String -Pattern '^:dt_').Count
$nO   = @($lines | Select-String -Pattern '^:o_').Count
$nRv  = @($lines | Select-String -Pattern '^:rv_').Count
Ok ('telas: dt(detalhe)=' + $nDt + '  o(comandos)=' + $nO + '  rv(reverter)=' + $nRv)
if ($nDt -ne 387) { Bad ('esperava 387 telas de detalhe, achei ' + $nDt) }

# 5) help e simbolos novos
foreach ($alvo in @(':ajuda', 'T_hd4', 'T_numdet', 'T_markit', 'T_oqfaz', 'T_ganho')) {
    $achou = @($lines | Select-String -SimpleMatch $alvo).Count
    if ($achou -gt 0) { Ok ('presente: ' + $alvo) } else { Bad ('ausente: ' + $alvo) }
}

Write-Host ''
if ($fail -eq 0) { Write-Host 'VERIFICACAO: TUDO OK' -ForegroundColor Green } else { Write-Host ("VERIFICACAO: $fail problema(s)") -ForegroundColor Red }
exit $fail
