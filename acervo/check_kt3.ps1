$ErrorActionPreference = 'Continue'
$root = 'Kairo Tweaker'

Write-Host '=== 1. PARSE ===' -ForegroundColor Cyan
$falhas = 0
Get-ChildItem $root -Recurse -Filter '*.ps1' | ForEach-Object {
  $errs = $null
  [void][System.Management.Automation.Language.Parser]::ParseFile($_.FullName, [ref]$null, [ref]$errs)
  if ($errs) { $falhas++; Write-Host ('  ERRO ' + $_.Name + ' linha ' + $errs[0].Extent.StartLineNumber + ': ' + $errs[0].Message) -ForegroundColor Red }
}
if ($falhas) { Write-Host 'PARSE FALHOU' -ForegroundColor Red; exit 1 }
Write-Host '  parse OK em todos os .ps1' -ForegroundColor Green

Write-Host '=== 2. CARREGAMENTO DOS MODULOS ===' -ForegroundColor Cyan
$global:KT_Registry = New-Object System.Collections.ArrayList
$global:KT_Acoes = @{}
$global:KT_Jogos = @()
$global:KT_ConfigsJogos = @{}
. (Join-Path $root 'modules\00.Core.ps1') -ErrorAction SilentlyContinue
Get-ChildItem (Join-Path $root 'modules') -Filter '*.ps1' | Where-Object { $_.Name -ne '00.Core.ps1' } | Sort-Object Name | ForEach-Object {
  try { . $_.FullName } catch { Write-Host ('  FALHOU ' + $_.Name + ': ' + $_.Exception.Message) -ForegroundColor Red }
}
Write-Host ('  TOTAL DE TWEAKS: ' + $global:KT_Registry.Count)

Write-Host '=== 3. ABAS ===' -ForegroundColor Cyan
$global:KT_Registry | Group-Object Categoria | Sort-Object Name | ForEach-Object {
  Write-Host ('  ' + $_.Name.PadRight(34) + $_.Count)
}
Write-Host ('  total de abas: ' + @($global:KT_Registry | Select-Object -ExpandProperty Categoria -Unique).Count)

Write-Host '=== 4. DUPLICADOS ===' -ForegroundColor Cyan
$dup = @($global:KT_Registry | Group-Object Id | Where-Object { $_.Count -gt 1 })
if ($dup) { $dup | ForEach-Object { Write-Host ('  ID DUPLICADO: ' + $_.Name + ' x' + $_.Count) -ForegroundColor Red } }
else { Write-Host '  nenhum Id duplicado' -ForegroundColor Green }

Write-Host '=== 5. EXECUTAR -> ACAO ===' -ForegroundColor Cyan
$faltando = @($global:KT_Registry | Where-Object { $_.Executar -and -not $global:KT_Acoes[$_.Executar] })
if ($faltando) { $faltando | ForEach-Object { Write-Host ('  EXECUTAR SEM ACAO: ' + $_.Id + ' -> ' + $_.Executar) -ForegroundColor Red } }
else { Write-Host ('  OK: todos os Executar resolvem (' + @($global:KT_Registry | Where-Object Executar).Count + ' tweaks com acao)') -ForegroundColor Green }

Write-Host '=== 6. PERFIS (regiao de linhas do KairoTweaker.ps1) ===' -ForegroundColor Cyan
$linhas = Get-Content (Join-Path $root 'KairoTweaker.ps1')
$iIni = 0
for ($i = 0; $i -lt $linhas.Count; $i++) { if ($linhas[$i] -match 'KT_Perfis\s*=\s*\[ordered\]') { $iIni = $i; break } }
$iFim = $linhas.Count - 1
for ($i = $iIni; $i -lt $linhas.Count; $i++) { if ($linhas[$i] -match 'function\s+Resolve-KTSelecao') { $iFim = $i - 1; break } }
$bloco = ($linhas[$iIni..$iFim] -join "`n")
try { Invoke-Expression $bloco } catch { Write-Host ('  ERRO ao avaliar perfis: ' + $_.Exception.Message) -ForegroundColor Red }
foreach ($nome in $global:KT_Perfis.Keys) {
  $ids = @($global:KT_Perfis[$nome])
  $bad = @($ids | Where-Object { @($global:KT_Registry | Where-Object Id -eq $_).Count -eq 0 })
  $dupids = @($ids | Group-Object | Where-Object { $_.Count -gt 1 } | Select-Object -ExpandProperty Name)
  $msg = '  ' + $nome.PadRight(20) + $ids.Count + ' itens'
  if ($bad)   { $msg += '  FALTAM: ' + ($bad -join ',') }
  else        { $msg += '  OK' }
  if ($dupids) { $msg += '  REPETIDOS: ' + ($dupids -join ',') }
  if ($bad) { Write-Host $msg -ForegroundColor Red } else { Write-Host $msg -ForegroundColor Green }
}

Write-Host '=== 7. ACOES ===' -ForegroundColor Cyan
Write-Host ('  acoes registradas: ' + $global:KT_Acoes.Count)
$powAcoes = @($global:KT_Acoes.Keys | Where-Object { $_ -like 'pow.*' })
Write-Host ('  acoes pow.* (planos de energia): ' + $powAcoes.Count)
if ($global:KT_Acoes['wu-tarefas']) { Write-Host '  acao wu-tarefas: OK' } else { Write-Host '  FALTA acao wu-tarefas' -ForegroundColor Red }

Write-Host '=== 8. JOGOS ===' -ForegroundColor Cyan
Write-Host ('  titulos: ' + $global:KT_Jogos.Count + ' | configs: ' + $global:KT_ConfigsJogos.Count)

Write-Host '=== 9. RESUMO ===' -ForegroundColor Cyan
$semExplic = @($global:KT_Registry | Where-Object { -not $_.Explicacao })
$semProb   = @($global:KT_Registry | Where-Object { -not $_.Problema -and -not $_.Explicacao })
Write-Host ('  sem Explicacao: ' + $semExplic.Count + ' | sem Problema nem Explicacao: ' + $semProb.Count)
Write-Host 'FIM'
