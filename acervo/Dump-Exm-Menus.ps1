# =====================================================================
#  Dump-Exm-Menus.ps1
#  Para cada utilitario EXM, lista ABA por ABA: as opcoes do menu, o que
#  cada uma faz (derivado dos comandos reais) e o risco de instabilidade.
#  Saida: EXM-MENUS.md
# =====================================================================
[CmdletBinding()]
param(
    [string]$Root = '',
    [string]$OutFile = '',
    [string]$InDir = ''
)
$ErrorActionPreference = 'Continue'

$__here = if ($PSCommandPath) { Split-Path -Parent $PSCommandPath } else { (Get-Location).Path }
if ([string]::IsNullOrWhiteSpace($Root))    { $Root = Split-Path -Parent $__here }
if ([string]::IsNullOrWhiteSpace($InDir))   { $InDir = $__here }
if ([string]::IsNullOrWhiteSpace($OutFile)) { $OutFile = Join-Path $Root 'EXM-MENUS.md' }
$Root = (Resolve-Path -LiteralPath $Root).Path.TrimEnd('\')
$toolsFilter = Join-Path $Root '_tools\*'
$quarentena  = @((Join-Path $Root 'old\*'), (Join-Path $Root 'OLD\*'),
                 (Join-Path $Root '_old\*'), (Join-Path $Root 'arquivados\*'))

. (Join-Path $__here 'Exm-Mapa.ps1')
. (Join-Path $__here 'Risco.ps1')

$opsPath = Join-Path $InDir 'ops.json'
if (-not (Test-Path -LiteralPath $opsPath)) { throw "ops.json nao encontrado em $InDir" }
Write-Host 'Lendo ops.json ...' -ForegroundColor Cyan
$opsRaw = Get-Content -LiteralPath $opsPath -Raw | ConvertFrom-Json
$ops = @($opsRaw)
Write-Host ("  {0} operacoes" -f $ops.Count)

# ------------------------------------------------------------- descricao
function Curto { param([string]$S, [int]$N) if ($null -eq $S) { return '' }; $S = ($S -replace '\s+', ' ').Trim(); if ($S.Length -le $N) { return $S }; return $S.Substring(0, $N - 1) + '…' }
function Esc { param([string]$S) if ($null -eq $S) { return '' }; ($S -replace '\|', '\|') }

function Get-Descricao {
    param($Ops)
    if (-not $Ops -or $Ops.Count -eq 0) { return @{ Texto = 'corpo da opcao nao localizado no trecho mapeado (ver `Linhas`)'; Alvos = @() } }

    $partes = @()
    $alvos  = @()
    $n = $Ops.Count

    $reg = @($Ops | Where-Object { $_.Kind -eq 'Registry' })
    if ($reg.Count -gt 0) {
        $escrita = @($reg | Where-Object { $_.Action -eq 'SetValue' }).Count
        $apaga   = @($reg | Where-Object { $_.Action -match '^Delete' }).Count
        $t = @()
        if ($escrita -gt 0) { $t += "grava $escrita valor(es) no registro" }
        if ($apaga -gt 0)   { $t += "apaga $apaga chave/valor" }
        $partes += ($t -join ' e ')
        foreach ($g in ($reg | Group-Object Path | Sort-Object Count -Descending | Select-Object -First 3)) {
            $alvos += ('`' + (Curto $g.Name 78) + '` (' + $g.Count + ')')
        }
    }
    $svc = @($Ops | Where-Object { $_.Kind -eq 'Service' })
    if ($svc.Count -gt 0) {
        $nomes = ($svc | ForEach-Object { $_.Name } | Select-Object -Unique | Select-Object -First 6) -join ', '
        $partes += "altera $($svc.Count) servico(s)"
        $alvos += ('servicos: ' + $nomes)
    }
    $tsk = @($Ops | Where-Object { $_.Kind -eq 'Task' })
    if ($tsk.Count -gt 0) {
        $partes += "$($tsk.Count) tarefa(s) agendada(s)"
        foreach ($g in ($tsk | Group-Object Name | Sort-Object Count -Descending | Select-Object -First 3)) { $alvos += ('tarefa: `' + (Curto $g.Name 60) + '`') }
    }
    $apx = @($Ops | Where-Object { $_.Kind -eq 'Appx' })
    if ($apx.Count -gt 0) {
        $partes += "$($apx.Count) aplicativo(s)"
        $pk = ($apx | ForEach-Object { $_.Name } | Select-Object -Unique | Select-Object -First 4) -join ', '
        if ($pk) { $alvos += ('pacotes: ' + $pk) }
    }
    $pw = @($Ops | Where-Object { $_.Kind -eq 'Power' })
    if ($pw.Count -gt 0) { $partes += "$($pw.Count) ajuste(s) de energia" }
    $bd = @($Ops | Where-Object { $_.Kind -eq 'BcdEdit' })
    if ($bd.Count -gt 0) { $partes += "$($bd.Count) ajuste(s) de boot (bcdedit)" }
    $ns = @($Ops | Where-Object { $_.Kind -eq 'NetShell' })
    if ($ns.Count -gt 0) { $partes += "$($ns.Count) ajuste(s) de rede (netsh)" }
    $cl = @($Ops | Where-Object { $_.Kind -eq 'Clean' })
    if ($cl.Count -gt 0) { $partes += "$($cl.Count) operacao(oes) de limpeza" }
    $wi = @($Ops | Where-Object { $_.Kind -eq 'Wmi' })
    if ($wi.Count -gt 0) { $partes += "$($wi.Count) ajuste(s) via WMI" }
    $ds = @($Ops | Where-Object { $_.Kind -eq 'Dism' })
    if ($ds.Count -gt 0) { $partes += "$($ds.Count) manutencao de imagem (DISM)" }
    $de = @($Ops | Where-Object { $_.Kind -eq 'Defender' })
    if ($de.Count -gt 0) { $partes += "ajuste do Windows Defender" }

    if ($partes.Count -eq 0) { $partes += "$n operacao(oes) de sistema" }
    $texto = "$n operacao(oes): " + (($partes | Select-Object -Unique) -join '; ') + '.'
    return @{ Texto = $texto; Alvos = $alvos }
}


# =====================================================================
#  MAIN
# =====================================================================
$exmFiles = @($ops | Select-Object -ExpandProperty File -Unique |
              Where-Object { $_ -match '(?i)exm.*\.cmd$' } | Sort-Object)
Write-Host ("Utilitarios EXM: {0}" -f $exmFiles.Count) -ForegroundColor Cyan

$itens = @()
$abasTotais = 0

foreach ($rel in $exmFiles) {
    $full = Join-Path $Root $rel
    if (-not (Test-Path -LiteralPath $full)) { continue }
    $abas = Get-ExmMenu -Path $full
    if (-not $abas -or $abas.Count -eq 0) { continue }
    $abasTotais += $abas.Count
    Write-Host ("  {0}: {1} aba(s)" -f (Split-Path $rel -Leaf), $abas.Count) -ForegroundColor DarkGray

    foreach ($aba in $abas) {
        foreach ($it in $aba.Items) {
            $meus = @($ops | Where-Object { $_.File -eq $rel -and [int]$_.Line -ge $it.From -and [int]$_.Line -le $it.To })
            $cmds = @($meus | ForEach-Object { $_.Raw } | Where-Object { $_ } | Select-Object -Unique)
            $chaves = @($meus | Where-Object { $_.Path } | ForEach-Object { $_.Path } | Select-Object -Unique)
            $risco = Get-Risco -Comandos $cmds -Chaves $chaves
            $desc  = Get-Descricao $meus
            # Opcao sem operacao no trecho mapeado: NAO e seguro chamar de
            # "baixo risco" — e ausencia de dado, nao seguranca.
            if ($meus.Count -eq 0) {
                $niv = 'n/d'; $res = 'Nao avaliado: o corpo desta opcao esta em outro trecho do arquivo (o EXM repete a estrutura de menus). Veja COMANDOS-DO-ACERVO.md.'
                $pts = @()
            }
            else { $niv = $risco.Nivel; $res = $risco.Resumo; $pts = $risco.Pontos }
            $itens += [pscustomobject]@{
                Script = $rel; AbaCat = $aba.Cat; AbaNome = $aba.CatName; AbaOrdem = $aba.Order
                N = $it.N; Rotulo = $it.Label; LabelSrc = $it.LabelSrc
                De = $it.From; Ate = $it.To
                Ops = $meus.Count; Cmds = $cmds.Count
                Desc = $desc.Texto; Alvos = $desc.Alvos
                Nivel = $niv; Resumo = $res; Pontos = $pts
            }
        }
    }
}
Write-Host ("Opcoes mapeadas: {0}" -f $itens.Count) -ForegroundColor Cyan

$porNivel = @{}
foreach ($i in $itens) { $porNivel[$i.Nivel] = 1 + $porNivel[$i.Nivel] }
$ordem = 'Critico', 'Alto', 'Medio', 'Baixo', 'n/d'


# =====================================================================
#  MARKDOWN
# =====================================================================
$emoji = @{ 'Critico' = '🔴'; 'Alto' = '🟠'; 'Medio' = '🟡'; 'Baixo' = '🟢'; 'n/d' = '⚪' }
$L = @()
$L += '# MENUS DO EXM - abas, opcoes e risco'
$L += ''
$L += ('> Desenhado dos **4 utilitarios EXM** do acervo: **{0} abas** e **{1} opcoes** de menu.' -f $abasTotais, $itens.Count)
$L += ('> Para cada opcao: o que ela faz (derivado dos comandos reais) e o risco de instabilidade.')
$L += ('> Gerado em {0}.' -f (Get-Date -Format 'dd/MM/yyyy HH:mm'))
$L += ''
$L += '## Como ler o risco'
$L += ''
$L += 'O risco e uma **inferencia sobre o que os comandos fazem** — nao e uma garantia do autor do utilitario.'
$L += 'Um nivel alto significa "pode dar problema", nao "vai dar problema".'
$L += ''
$L += '| Nivel | Quando Happens | O que fazer |'
$L += '|:---:|---|---|'
$L += '| 🔴 Crítico | Toca boot, bootloader, sessão ou disco. | **Não aplique** sem ponto de restauracao validado. Confira o hardware. |'
$L += '| 🟠 Alto | Toca driver, serviço, rede ou proteção. | Aplique com reversivel pronto; teste rede e audio depois. |'
$L += '| 🟡 Médio | Ajuste sensível mas normalmente reversível. | Revise a lista de alvos antes de aceitar. |'
$L += '| 🟢 Baixo | Interface, telemetria, preferência, energia. | Pode aplicar sem precaução especial. |'
$L += ''
$L += '## Resumo de risco'
$L += ''
$L += '| Nivel | Opcoes | % |'
$L += '|:---:|---:|---:|'
foreach ($n in $ordem) {
    $q = $porNivel[$n]; if (-not $q) { $q = 0 }
    $L += ('| {0} {1} | {2} | {3:N1}% |' -f $emoji[$n], $n, $q, (100 * $q / [Math]::Max(1, $itens.Count)))
}
$L += ''

# ---- ranking de risco ------------------------------------------------
$L += '## Onde o risco se concentra'
$L += ''
$L += 'As opcoes marcadas com 🔴 ou 🟠, ordenadas pelo nivel e depois pelo numero de operacoes.'
$L += ''
$L += '| Script | Aba | # | Opcao | Risco | Ops |'
$L += '|---|---|---:|---|:---:|---:|'
$rank = @($itens | Where-Object { $_.Nivel -in @('Critico', 'Alto') } |
          Sort-Object @{ e = { if ($_.Nivel -eq 'Critico') { 0 } else { 1 } } }, @{ e = 'Ops'; Descending = $true })
foreach ($i in $rank) {
    $ab = if ($i.AbaNome) { $i.AbaNome } else { "Aba $($i.AbaCat)" }
    $L += ('| `{0}` | {1} | {2} | **{3}** | {4} {5} | {6} |' -f `
        (Split-Path $i.Script -Leaf), (Esc (Curto $ab 34)), $i.N, (Esc $i.Rotulo), $emoji[$i.Nivel], $i.Nivel, $i.Ops)
}
$L += ''

# ---- por utilitario --------------------------------------------------
$k = 0
foreach ($g in ($itens | Group-Object Script | Sort-Object Name)) {
    $k++
    $L += ('## {0} · `{1}`' -f $k, (Split-Path $g.Name -Leaf))
    $L += ''
    $L += ('_{0} opcao(oes) mapeada(s) · {1:N0} operacoes no total do utilitario._' -f `
        $g.Count, (@($ops | Where-Object { $_.File -eq $g.Name }).Count))
    $L += ''
    foreach ($ab in ($g.Group | Group-Object AbaNome, AbaCat | Sort-Object { $_.Group[0].AbaOrdem })) {
        $a0 = $ab.Group[0]
        $nome = if ($a0.AbaNome) { $a0.AbaNome } else { "(aba sem rotulo: $($a0.AbaCat))" }
        $L += ('### Aba · {0}  ·  categoria `{1}`' -f $nome, $a0.AbaCat)
        $L += ''
        $L += '| # | Opcao | O que faz | Risco | Linhas |'
        $L += '|---:|---|---|:---:|---|'
        foreach ($i in ($ab.Group | Sort-Object N)) {
            $d = (Curto $i.Desc 150)
            $L += ('| {0} | **{1}** | {2} | {3} {4} | {5}-{6} |' -f `
                $i.N, (Esc $i.Rotulo), (Esc $d), $emoji[$i.Nivel], $i.Nivel, $i.De, $i.Ate)
        }
        $L += ''
        $det = @($ab.Group | Where-Object { $_.Nivel -in @('Critico', 'Alto') })
        if ($det.Count -gt 0) {
            $L += ('<details><summary><b>{0} opcao(oes) de risco alto/critico nesta aba</b></summary>' -f $det.Count)
            $L += ''
            foreach ($i in ($det | Sort-Object { if ($_.Nivel -eq 'Critico') { 0 } else { 1 } }, N)) {
                $L += ('**[{0}] {1}** - {2} {3}' -f $i.N, (Esc $i.Rotulo), $emoji[$i.Nivel], $i.Nivel)
                $L += ''
                $L += ('- _O que faz:_ ' + (Esc $i.Desc))
                foreach ($a in $i.Alvos) { $L += ('- _Alvo:_ ' + $a) }
                $L += ('- _Consequencia:_ ' + (Esc $i.Resumo))
                foreach ($p in $i.Pontos) { $L += ('- _Sinal:_ ' + (Esc $p)) }
                $L += ('- _No arquivo:_ linhas {0}-{1} · {2} operacao(oes) · {3} comando(s) distintos' -f $i.De, $i.Ate, $i.Ops, $i.Cmds)
                $L += ''
            }
            $L += '</details>'
            $L += ''
        }
    }
}

[IO.File]::WriteAllLines($OutFile, $L, (New-Object System.Text.UTF8Encoding $true))
Write-Host ("Opcoes: {0} -> {1} ({2:N0} KB, {3:N0} linhas)" -f $itens.Count, $OutFile, ((Get-Item $OutFile).Length / 1KB), $L.Count) -ForegroundColor Green
