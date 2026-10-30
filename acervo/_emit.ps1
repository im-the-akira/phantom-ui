# =====================================================================
#  _emit.ps1 - emissao do MEU-TWEAKER.cmd
#  Bilingue (pt/en), 100% ASCII (sem acentos nem simbolos fora do CP1252
#  basico), menu redesenhado com telas de ajuda e detalhe por ajuste.
#  Chamado por Gerar-MeuTweaker.ps1 (mesmo escopo).
# =====================================================================

$catsPt = @{
    '01' = 'Otimizacoes Gerais do Sistema'; '02' = 'Energia e Desempenho'
    '03' = 'Teclado e Mouse';               '04' = 'Placa de Video e Jogos'
    '04N' = 'Placa de Video NVIDIA';        '04A' = 'Placa de Video AMD'
    '04I' = 'Placa de Video Intel';         '05' = 'Processador (todos)'
    '05A' = 'Processador AMD';              '05I' = 'Processador Intel'
    '06' = 'Limpeza do PC';                 '07' = 'Privacidade e Bloatware'
    '08' = 'Armazenamento e Disco';         '09' = 'Memoria RAM'
    '10' = 'Extras e Qualidade de Vida';    '11' = 'Desinstalar Programas'
    '12' = 'Rede e Internet';               'F'  = 'Corretivos e Reversao'
}
$catsEn = @{
    '01' = 'General System Optimizations';  '02' = 'Power and Energy'
    '03' = 'Keyboard and Mouse';            '04' = 'GPU and Games'
    '04N' = 'GPU NVIDIA';                   '04A' = 'GPU AMD'
    '04I' = 'GPU Intel';                    '05' = 'CPU (universal)'
    '05A' = 'CPU AMD';                      '05I' = 'CPU Intel'
    '06' = 'PC Clean';                      '07' = 'Debloat and Privacy'
    '08' = 'Storage and Disk';              '09' = 'Memory and RAM'
    '10' = 'Additional and QOL';            '11' = 'Uninstall Apps'
    '12' = 'Network Tweaking';              'F'  = 'Fixes and Reverts'
}
$EmtLines = New-Object System.Collections.Generic.List[string]
function Ln { param([string]$s = '') $EmtLines.Add($s) }
# translitera acentos (a -> a, c -> c...) em vez de apagar a letra
$mapAc = @{}
$__de = [char]0xE0, [char]0xE1, [char]0xE2, [char]0xE3, [char]0xE4, [char]0xE5, [char]0xE8, [char]0xE9, [char]0xEA, [char]0xEB, [char]0xEC, [char]0xED, [char]0xEE, [char]0xEF, [char]0xF2, [char]0xF3, [char]0xF4, [char]0xF5, [char]0xF6, [char]0xF9, [char]0xFA, [char]0xFB, [char]0xFC, [char]0xE7, [char]0xF1, [char]0xC0, [char]0xC1, [char]0xC2, [char]0xC3, [char]0xC4, [char]0xC5, [char]0xC8, [char]0xC9, [char]0xCA, [char]0xCB, [char]0xCC, [char]0xCD, [char]0xCE, [char]0xCF, [char]0xD2, [char]0xD3, [char]0xD4, [char]0xD5, [char]0xD6, [char]0xD9, [char]0xDA, [char]0xDB, [char]0xDC, [char]0xC7, [char]0xD1
$__pa = 'aaaaaaeeeeiiiiooooouuuucnAAAAAAEEEEIIIIOOOOOUUUUCN'
for ($__i = 0; $__i -lt $__de.Count; $__i++) { $mapAc[$__de[$__i]] = $__pa[$__i] }
function Translit { param([string]$s)
  if (-not $s) { return '' }
  $ch = $s.ToCharArray()
  for ($i = 0; $i -lt $ch.Length; $i++) { if ($mapAc.ContainsKey($ch[$i])) { $ch[$i] = $mapAc[$ch[$i]] } }
  return (-join $ch)
}
function Limpa { param([string]$s, [int]$n)
  if ($null -eq $s) { return '' }
  $s = Translit $s
  $s = ($s -replace '[\r\n\t]', ' ' -replace '\s+', ' ').Trim()
  if ($n -gt 0 -and $s.Length -gt $n) { return $s.Substring(0, $n - 3) + '...' }
  return $s
}
function Et { param([string]$s, [int]$n = 0)
  $s = Limpa $s $n
  if ($s -match '%[A-Za-z0-9_]%|%[0-9]') { return $s }
  $s = $s -replace '%', '%%'
  $s = $s -replace '(?<!\^)&', '^&'
  $s = $s -replace '(?<!\^)\|', '^|'
  $s = $s -replace '(?<!\^)<', '^<'
  $s = $s -replace '(?<!\^)>', '^>'
  $s = $s -replace '[^\x20-\x7E]', ''
  return $s
}
function Dc { param([string]$c)
  if ($null -eq $c) { return '' }
  return Et (($c -replace '\s*>nul\s+2>&1', '') -replace '\s*>nul', '') 94
}
function Get-TotalOps { param($Gs) $s = 0; foreach ($g in $Gs) { $s += @($g.Ops).Count }; return $s }

$agora = Get-Date -Format 'dd/MM/yyyy HH:mm'
$nOpt  = $grupos.Count
$total = Get-TotalOps $grupos

# --- dados dos agentes: nomes bilingues e reversoes ---------------------
# leitura robusta: o PS 5.1 tem duas armadilhas com JSON grande: o pipeline
# Get-Content -Raw | ConvertFrom-Json e o @() em volta de -InputObject ambos
# aninham o array dentro de 1 elemento. Converte solto e normaliza depois.
function Parse-JsonArr { param([string]$Path) ConvertFrom-Json -InputObject ([IO.File]::ReadAllText($Path)) }
function Normalize-JsonArr { param($J) $a = @($J); if ($a.Count -eq 1 -and $a[0] -is [System.Array]) { return @($a[0]) }; return $a }
$nmPt = @{}; $nmEn = @{}
$curPt = @{}; $curEn = @{}
$pN = Join-Path $tools 'nomes.json'
if (Test-Path $pN) {
    $jn = Parse-JsonArr $pN
    foreach ($x in (Normalize-JsonArr $jn.tweaks)) { if ($x.id) { $nmPt[$x.id] = $x.pt; $nmEn[$x.id] = $x.en } }
}
$catPt = @{}; $catEn = @{}
if (Test-Path $pN) { foreach ($x in (Normalize-JsonArr $jn.categorias)) { $catPt[$x.chave] = $x.pt; $catEn[$x.chave] = $x.en } }
foreach ($ck in @($catsPt.Keys)) { if ($catPt[$ck]) { $catsPt[$ck] = $catPt[$ck] } }
foreach ($ck in @($catsEn.Keys)) { if ($catEn[$ck]) { $catsEn[$ck] = $catEn[$ck] } }

# --- textos extras do Kairo (Explicacao / Ganho) por tweak ---------------
$twTodos = @()
$pTw = Join-Path $tools 'tweaks.json'
if (Test-Path $pTw) { $twTodos = Normalize-JsonArr (Parse-JsonArr $pTw) }
$twExp = @{}; $twGanho = @{}; $twReinicio = @{}
foreach ($twk in $twTodos) { $twExp[$twk.Id] = $twk.Explicacao; $twGanho[$twk.Id] = $twk.Ganho; $twReinicio[$twk.Id] = $twk.Reinicio }

$rvExtra = @{}
foreach ($fn in @('reverts-kairo.json', 'reverts-exm.json')) {
    $pR = Join-Path $tools $fn
    if (-not (Test-Path -LiteralPath $pR)) { continue }
    foreach ($r in (Normalize-JsonArr (Parse-JsonArr $pR))) {
        if ($r.reversivel -and @($r.revert).Count -gt 0) { $rvExtra[$r.id] = @($r.revert) }
    }
}

# nome pt/en de um grupo
function NmDe { param($g)
    $id = $null
    if ($g.Chave -like 'kt:*') { $id = $g.Chave.Substring(3) }
    if ($g.EsoCuradoria) { return @($g.Nome, $g.NomeEn) }
    if ($id -and $nmPt.ContainsKey($id)) { return @($nmPt[$id], $nmEn[$id]) }
    return @($g.Nome, $g.Nome)
}
# texto seguro para dentro de  set "X=..."
function Sn { param([string]$s, [int]$n = 0)
    if ($null -eq $s) { return '' }
    $s = Limpa $s $n
    $s = $s -replace '"', "'"
    $s = $s -replace '%', '%%'
    $s = $s -replace '[^\x20-\x7E]', ''
    $s -replace '[&|<>]', ''
}
# descricao curta pt/en por ajuste (Explicacao do Kairo, quando existir)
function DescDe { param($g)
    $id = $null
    if ($g.Chave -like 'kt:*') { $id = $g.Chave.Substring(3) }
    if ($id -and $twExp.ContainsKey($id) -and $twExp[$id]) { return @($twExp[$id], $twExp[$id]) }
    return @('', '')
}
# benefício pt/en por ajuste
function GanhoDe { param($g)
    $id = $null
    if ($g.Chave -like 'kt:*') { $id = $g.Chave.Substring(3) }
    if ($id -and $twGanho.ContainsKey($id) -and $twGanho[$id]) { return @($twGanho[$id], $twGanho[$id]) }
    return @('', '')
}
function PrecisaReiniciar { param($g)
    $id = $null
    if ($g.Chave -like 'kt:*') { $id = $g.Chave.Substring(3) }
    if ($id -and $twReinicio.ContainsKey($id)) { return ("$($twReinicio[$id])" -match 'true') }
    return $false
}
# reversao de um grupo: usa o catalogo do agente quando existir
function Get-Rev { param($g)
    if ($g.RevDir) { return @($g.RevDir) }
    if ($g.EsoRevert) { return @() }
    if ($g.Chave -like 'kt:*') { $id = $g.Chave.Substring(3); if ($rvExtra.ContainsKey($id)) { return @($rvExtra[$id]) } }
    $r = @()
    foreach ($o in $g.Ops) { $x = Get-Desfazer $o; if ($x) { $r += $x } }
    return $r
}
# --- CATEGORIA F: revisoes prontas ---------------------------------------
$pRX = Join-Path $tools 'reverts-exm.json'
$rvEXM = @()
if (Test-Path $pRX) { $rvEXM = Normalize-JsonArr (Parse-JsonArr $pRX) }

function NovoPacote { param([string]$Nome, [string]$Fonte, [string[]]$Cmds, [string]$Resumo)
    $ops = @()
    foreach ($c in $Cmds) {
        $ops += [pscustomobject]@{ Kind = 'Raw'; Raw = $c; Path = ''; Name = ''; Type = ''; Data = ''; Action = ''; File = ''; Line = 0 }
    }
    return [pscustomobject]@{
        Cat = 'F'; Chave = ('pac:' + $Nome); Fonte = $Fonte; Nome = $Nome
        RiscoAutor = ''; Ops = $ops; EsoRevert = $true; Nivel = 'Baixo'; Resumo = $Resumo
    }
}

$porCat['F'] = @()
foreach ($ck in @($cats.Keys)) {
    if ($ck -eq 'F') { continue }
    $cmds = @()
    foreach ($g in $porCat[$ck]) {
        foreach ($r in (Get-Rev $g)) { if ($r) { $cmds += $r } }
    }
    if ($cmds.Count -eq 0) { continue }
    $porCat['F'] += NovoPacote ('Desfazer tudo: ' + $catsPt[$ck]) 'reversao automatica' $cmds ('Remove os ' + $cmds.Count + ' ajustes desta categoria')
}
foreach ($r in $rvEXM) {
    if (-not $r.reversivel) { continue }
    $cmds = @($r.revert) | Where-Object { $_ }
    if ($cmds.Count -eq 0) { continue }
    if ($r.id -notmatch '(?i)^(revert|restore|undo|reset)') { continue }
    $nm = $r.rotulo; if (-not $nm) { $nm = $r.id }
    $porCat['F'] += NovoPacote ('Pacote EXM: ' + $nm) 'EXM' $cmds ('Reverte ' + $cmds.Count + ' operacao(oes) de uma vez')
}
# --- CURADORIA: itens documentados, um por linha, com reversao propria ---
$rl = @{ 'Baixo' = 'Baixo'; 'Medio' = 'Medio'; 'Alto' = 'Alto'; 'Critico' = 'Critico' }
$pC = Join-Path $tools 'curadoria.json'
if (Test-Path $pC) {
    $cur = Get-Content -LiteralPath $pC -Raw | ConvertFrom-Json
    foreach ($prop in $cur.PSObject.Properties) {
        $set = $prop.Value
        $n = 0
        foreach ($it in @($set.itens)) {
            $n++
            $nmPt2 = $it.pt; $nmEn2 = $it.en
            $ch = 'cur:' + $set.id + ':' + $n
            $grp = [pscustomobject]@{
                Cat = $set.cat; Chave = $ch; Fonte = 'curadoria'
                Nome = $nmPt2; NomeEn = $nmEn2; RiscoAutor = ''
                Ops = @([pscustomobject]@{ Kind = 'Raw'; Raw = $it.cmd; Path = ''; Name = ''; Type = ''; Data = ''; Action = ''; File = ''; Line = 0 })
                RevDir = $it.rev; EsoCuradoria = $true
                Nivel = $set.insta; Resumo = $it.nota_pt; ResumoEn = $it.nota_en
            }
            $porCat[$set.cat] += $grp
            $curPt[$ch] = $nmPt2; $curEn[$ch] = $nmEn2
        }
    }
}

# reconstroi a ordem global agora que a categoria F existe
# categorias de fabricante (04N/04A/04I/05A/05I) so entram no menu quando tem conteudo
$vendCats = @('04N', '04A', '04I', '05A', '05I')
# --- REMOVE ativacao/pirataria: KMS, chaves genericas, marca d'agua ---------
$EXCLUIR = @('ativ.kms.win10pro', 'ativ.iot.ltsc', 'ativ.marca')
$removidos = 0
foreach ($ck in @($porCat.Keys)) {
    $antes = @($porCat[$ck])
    $depois = @($antes | Where-Object {
        $idd = $null
        if ($_.Chave -like 'kt:*') { $idd = $_.Chave.Substring(3) }
        if ($idd -and $EXCLUIR -contains $idd) { $removidos++; return $false }
        foreach ($o in $_.Ops) {
            if ($o.Raw -match '(?i)slmgr.*(/ipk|/skms|/ato|/rilc)|kms8\.msguides|DigitalActivation|Universal ?Watermark|HWID') { $removidos++; return $false }
        }
        return $true
    })
    if ($depois.Count -eq 0) { $porCat[$ck] = @() } else { $porCat[$ck] = $depois }
}
Write-Host ("  Removidos {0} ajuste(s) de ativacao/pirataria" -f $removidos)

$nomesCat = @()
foreach ($ck in $cats.Keys) {
    if (@($porCat[$ck]).Count -gt 0 -or ($vendCats -notcontains $ck)) { $nomesCat += $ck }
}
$ordem = @(); $seq = @{}; $i = 0
foreach ($ck in $nomesCat) { foreach ($g in $porCat[$ck]) { $i++; $seq[$g.Chave] = $i; $ordem += $g } }
# =====================================================================
#  TABELA DE TEXTOS   chave = @(portugues, ingles)
# =====================================================================
$TEXTOS = [ordered]@{
    'lang'    = @('Escolha seu idioma',                   'Choose your language')
    'pick'    = @('Idioma / Language',                    'Language / Idioma')
    'rp'      = @('PONTO DE RESTAURACAO',                 'RESTORE POINT')
    'rp_ask'  = @('Criar um ponto de restauracao agora?', 'Create a restore point now?')
    'rp_work' = @('Criando o ponto de restauracao, aguarde...', 'Creating the restore point, please wait...')
    'rp_ok'   = @('Ponto de restauracao criado.',         'Restore point created.')
    'rp_fail' = @('Nao deu. Rode o script como Administrador.', 'Failed. Run the script as Administrator.')
    'rp_skip' = @('Ok. Voce pode criar depois pela opcao R.', 'Ok. You can create one later with option R.')
    'main'    = @('MENU PRINCIPAL',                       'MAIN MENU')
    'ajuste'  = @('ajustes',                              'adjustments')
    'marc'    = @('marcados',                             'selected')
    'apply'   = @('Aplicar marcadas',                     'Apply selected')
    'undo'    = @('Desfazer marcadas',                    'Undo selected')
    'rp2'     = @('Novo ponto de restauracao',            'New restore point')
    'quit'    = @('Sair',                                 'Exit')
    'back'    = @('Voltar',                               'Back')
    'markall' = @('Marcar todas',                         'Select all')
    'unmark'  = @('Desmarcar todas',                      'Deselect all')
    'view'    = @('Ver detalhes e comandos',              'View details and commands')
    'help'    = @('Ajuda: como usar e riscos',            'Help: how to use and risks')
    'page'    = @('Pagina',                               'Page')
    'next'    = @('Proxima',                              'Next page')
    'prev'    = @('Anterior',                             'Previous page')
    'appl'    = @('APLICANDO OS MARCADOS',               'APPLYING THE SELECTED ONES')
    'undy'    = @('DESFAZENDO OS MARCADOS',              'UNDOING THE SELECTED ONES')
    'done'    = @('CONCLUIDO',                            'ALL DONE')
    'none'    = @('Nada marcado. Marque algo primeiro.',   'Nothing selected. Select something first.')
    'choose'  = @('Escolha',                              'Choice')
    'yn'      = @('[S]im / [N]ao',                        '[Y]es / [N]o')
    'exall'   = @('Executar todos',                       'Run all of them')
    'exone'   = @('Executar este comando',                'Run this command')
    'revit'   = @('Reverter este ajuste',                 'Undo this adjustment')
    'norev'   = @('Este ajuste nao tem reversao automatica.', 'This one has no automatic undo.')
    'cmds'    = @('comandos',                             'commands')
    'clear'   = @('Limpar marcacoes',                     'Clear the selection')
    'lbl'     = @('AJUSTE',                               'ADJUSTMENT')
    'risco'   = @('RISCO',                                'RISK')
    'voltal'  = @('Voltar para a lista de ajustes',       'Back to the adjustment list')
    'executados' = @('executados',                        'done')
    'desfeitos'  = @('desfeitos',                         'undone')
    'recom'   = @('recomendado',                          'recommended')
    'numdet'  = @('Digite o numero para ver o DETALHE (o que faz, ganho, risco)', 'Type the number to see DETAILS (what it does, gain, risk)')
    'markit'  = @('[M] marcar/desmarcar este ajuste', '[M] mark/unmark this adjustment')
    'cuidado' = @('use com cuidado',                      'use with care')
    'evite'   = @('so se souber o que faz',               'only if you know what it does')
    'nunca'   = @('evite: pode quebrar o sistema',        'avoid: may break the system')
    'oqfaz'   = @('O QUE FAZ',                            'WHAT IT DOES')
    'ganho'   = @('GANHO ESPERADO',                       'EXPECTED GAIN')
    'origem'  = @('ORIGEM',                               'SOURCE')
    'rein'    = @('Precisa reiniciar o PC para valer',    'Needs a restart to take effect')
    'dver'    = @('DETALHE DO AJUSTE',                    'ADJUSTMENT DETAILS')
    'hd1'     = @('COMO USAR',                            'HOW TO USE')
    'hd2'     = @('DIGITE O NUMERO E ENTER PARA LIGAR/DESLIGAR A MARCACAO [x]', 'TYPE THE NUMBER AND ENTER TO MARK/UNMARK [x]')
    'hd3'     = @('O RISCO MOSTRA O QUE PODE DAR ERRADO:', 'RISK SHOWS WHAT CAN GO WRONG:')
    'hd4'     = @('BAIXO  = recomendado, quase sem perigo', 'LOW     = recommended, almost no risk')
    'hd5'     = @('MEDIO  = sensivel, leia o detalhe antes', 'MEDIUM  = sensitive, read details first')
    'hd6'     = @('ALTO   = mexe em driver/rede/servico', 'HIGH    = touches driver/network/service')
    'hd7'     = @('CRITICO= pode impedir o Windows de ligar', 'CRITICAL= may stop Windows from booting')
    'hd8'     = @('MARQUE VARIAS E USE [A] NO MENU PARA APLICAR DE UMA VEZ', 'MARK SEVERAL AND USE [A] IN THE MENU TO APPLY AT ONCE')
    'hd9'     = @('SEMPRE CRIE O PONTO DE RESTAURACAO ANTES (TECLA R)', 'ALWAYS CREATE A RESTORE POINT FIRST (KEY R)')
    'catgpu'  = @('Separadas por fabricante. Use so a da SUA placa.', 'Split by brand. Use only YOUR card''s section.')
    'semaj'   = @('(nenhum ajuste aqui - sua seguradora de reversao)', '(no tweaks here - your safety net)')
}
# INICIO REAL: cabecalho + desvio. TEM que vir antes das tabelas, porque o cmd
# executa o arquivo de cima pra baixo e um 'exit /b' em nivel superior
# encerraria o script antes do menu.
Ln '@echo off'
Ln 'setlocal EnableExtensions EnableDelayedExpansion'
Ln 'title MEU-TWEAKER  ::  all-in-one windows tweaker'
Ln 'color 0F'
Ln 'mode con: cols=104 lines=48 >nul 2>&1'
Ln 'set "CNT=0"'
Ln 'rem ============================================================================'
Ln ('rem  MEU-TWEAKER.cmd   -   gerado em ' + $agora)
Ln ('rem  ' + $nOpt + ' ajustes - ' + $total + ' comandos - arquivo autocontido')
Ln 'rem ============================================================================'
Ln 'goto idioma'
Ln ':txt'
Ln 'if "%L%"=="en" goto txt_en'
foreach ($k in $TEXTOS.Keys) { Ln ('set "T_' + $k + '=' + (Et $TEXTOS[$k][0]) + '"') }
foreach ($ck in $cats.Keys) { Ln ('set "C_' + $ck + '=' + (Et $catsPt[$ck] 40) + '"') }
Ln 'goto txt_fim'
Ln ':txt_en'
foreach ($k in $TEXTOS.Keys) { Ln ('set "T_' + $k + '=' + (Et $TEXTOS[$k][1]) + '"') }
foreach ($ck in $cats.Keys) { Ln ('set "C_' + $ck + '=' + (Et $catsEn[$ck] 40) + '"') }
Ln ':txt_fim'
Ln 'exit /b'
Ln ''
Ln ':nomes'
$gi = 0
foreach ($g in $ordem) {
    $gi++
    $sq = '{0:00000}' -f $gi
    $nm = NmDe $g
    Ln ('set "NP_' + $sq + '=' + (Sn $nm[0] 60) + '"')
    Ln ('set "NE_' + $sq + '=' + (Sn $nm[1] 60) + '"')
}
Ln 'exit /b'
Ln ''
Ln ':idioma'
Ln 'call :txt'
Ln 'call :nomes'
Ln 'cls'
Ln 'echo.'
Ln 'echo   ##############################'
Ln 'echo   #  M E U   T W E A K E R    #'
Ln 'echo   #  windows all-in-one tweak  #'
Ln 'echo   ##############################'
Ln 'echo.'
Ln 'echo  =========================================================================='
Ln 'echo    %T_lang%'
Ln 'echo  =========================================================================='
Ln 'echo.'
Ln 'echo      [1]   Portugues (Brasil)'
Ln 'echo      [2]   English'
Ln 'echo.'
Ln 'choice /C 12 /N /M "      > "'
Ln 'if errorlevel 2 goto idi_en'
Ln 'set "L=pt"'
Ln 'goto idi_fim'
Ln ':idi_en'
Ln 'set "L=en"'
Ln ':idi_fim'
Ln 'call :txt'
Ln 'goto rpoint'
Ln ''
# =====================================================================
#  PONTO DE RESTAURACAO
# =====================================================================
Ln ':rpoint'
Ln 'cls'
Ln 'echo.'
Ln 'echo  =========================================================================='
Ln 'echo    %T_rp%'
Ln 'echo  =========================================================================='
Ln 'echo.'
Ln 'echo    Antes de mudar qualquer coisa, o Windows pode gravar um retrato do'
Ln 'echo    sistema para voltar atras se algo ficar estranho.'
Ln 'echo    En / Before changing anything, Windows can snapshot the system so'
Ln 'echo    you can roll back if anything looks off.'
Ln 'echo.'
Ln 'choice /C SN /N /M "   %T_rp_ask%   %T_yn% : "'
Ln 'if errorlevel 2 goto rpoint_skip'
Ln 'echo   %T_rp_work%'
Ln 'powershell -NoProfile -ExecutionPolicy Bypass -Command "Checkpoint-Computer -Description ''MEU-TWEAKER'' -RestorePointType ''MODIFY_SETTINGS''" >nul 2>&1'
Ln 'if errorlevel 1 goto rpoint_fail'
Ln 'echo   %T_rp_ok%'
Ln 'timeout /t 2 /nobreak >nul'
Ln 'goto menu'
Ln ':rpoint_fail'
Ln 'echo   %T_rp_fail%'
Ln 'timeout /t 3 /nobreak >nul'
Ln 'goto menu'
Ln ':rpoint_skip'
Ln 'echo   %T_rp_skip%'
Ln 'timeout /t 2 /nobreak >nul'
Ln 'goto menu'
Ln ''
# =====================================================================
#  MENU PRINCIPAL
# =====================================================================
Ln ':menu'
Ln 'cls'
Ln 'echo.'
Ln 'echo  =========================================================================='
Ln 'echo    %T_main%   -   %CNT% %T_marc%'
Ln 'echo  =========================================================================='
Ln 'echo.'
Ln 'if "%L%"=="en" (echo    TYPE THE CATEGORY NUMBER) else (echo    DIGITE O NUMERO DA CATEGORIA)'
Ln 'echo.'
$ci = 0
# descricao curta por categoria, na mesma linha (pt / en)
$descCat = @{
    '01' = @('base segura para qualquer PC',   'safe base for any PC')
    '02' = @('planos de energia',              'power plans')
    '03' = @('resposta de teclado e mouse',    'keyboard and mouse response')
    '04' = @('jogos - qualquer placa',         'games - any card')
    '04N' = @('SO PARA NVIDIA',                'NVIDIA ONLY')
    '04A' = @('SO PARA AMD / RADEON',          'AMD RADEON ONLY')
    '04I' = @('SO PARA INTEL',                 'INTEL ONLY')
    '05' = @('CPU - qualquer marca',           'CPU - any brand')
    '05A' = @('SO PARA AMD RYZEN',             'AMD RYZEN ONLY')
    '05I' = @('SO PARA INTEL',                 'INTEL ONLY')
    '06' = @('limpeza segura de lixo',         'safe junk cleanup')
    '07' = @('telemetria e apps de fabrica',   'telemetry and bundled apps')
    '08' = @('SSD e disco',                    'SSD and disk')
    '09' = @('memoria RAM e pagefile',         'RAM and pagefile')
    '10' = @('melhorias do dia a dia',         'daily-use improvements')
    '11' = @('desinstalar apps inuteis',       'uninstall useless apps')
    '12' = @('latencia e estabilidade de rede', 'network latency and stability')
    'F'  = @('desfazer o que foi aplicado',    'undo what was applied')
}
foreach ($ck in $nomesCat) {
    $ci++
    $d = $descCat[$ck]; if (-not $d) { $d = @('', '') }
    $lnPt = 'echo    [' + ('{0,2}' -f $ci) + '] %C_' + $ck + '%  -  ' + (Et $d[0] 44)
    $lnEn = 'echo    [' + ('{0,2}' -f $ci) + '] %C_' + $ck + '%  -  ' + (Et $d[1] 44)
    Ln ('if "%L%"=="en" (' + $lnEn + ') else (' + $lnPt + ')')
}
Ln 'echo    -------------------------------------------------------------------------'
Ln 'echo    [A] %T_apply%  (%CNT%)        [D] %T_undo%  (%CNT%)'
Ln 'echo    [C] %T_clear%     [R] %T_rp2%     [H] %T_help%     [Q] %T_quit%'
Ln 'echo  =========================================================================='
Ln 'set "Op="'
Ln 'set /p "Op=%T_choose%: "'
$mc = 0
foreach ($ck in $nomesCat) {
    $mc++
    Ln ('if "%Op%"=="' + $mc + '" goto c_' + $ck + '_p1')
}
Ln 'if /i "%Op%"=="A" goto aplicar'
Ln 'if /i "%Op%"=="D" goto desfazer'
Ln 'if /i "%Op%"=="C" goto limpar'
Ln 'if /i "%Op%"=="R" goto rpoint'
Ln 'if /i "%Op%"=="H" goto ajuda'
Ln 'if /i "%Op%"=="Q" goto fim'
Ln 'goto menu'
Ln ''
# =====================================================================
#  AJUDA
# =====================================================================
Ln ':ajuda'
Ln 'cls'
Ln 'echo.'
Ln 'echo  =========================================================================='
Ln 'echo    %T_help%'
Ln 'echo  =========================================================================='
Ln 'echo.'
Ln 'echo    %T_hd1%'
Ln 'echo    ------------------------------------------------------------------------'
Ln 'echo    1. %T_hd9%'
Ln 'echo    2. %T_hd2%'
Ln 'echo    3. %T_hd8%'
Ln 'if "%L%"=="en" (echo    4. On the detail screen: [T] runs all now, [R] undoes it) else (echo    4. Na tela do ajuste: [T] roda tudo agora, [R] desfaz so ele)'
Ln 'echo.'
Ln 'echo    %T_hd3%'
Ln 'echo    ------------------------------------------------------------------------'
Ln 'echo    %T_hd4%'
Ln 'echo    %T_hd5%'
Ln 'echo    %T_hd6%'
Ln 'echo    %T_hd7%'
Ln 'echo.'
Ln 'echo    %T_catgpu%'
Ln 'echo.'
Ln 'echo  -------------------------------------------------------------------------'
Ln 'echo    [0] %T_back%     [Q] %T_quit%'
Ln 'echo  =========================================================================='
Ln 'set "Op="'
Ln 'set /p "Op=%T_choose%: "'
Ln 'if "%Op%"=="0" goto menu'
Ln 'if /i "%Op%"=="Q" goto fim'
Ln 'goto ajuda'
Ln ''
Ln ':limpar'
Ln 'cls'
Ln 'set "CNT=0"'
foreach ($g in $ordem) { Ln ('set "M_' + ('{0:00000}' -f $seq[$g.Chave]) + '="') }
Ln 'timeout /t 1 /nobreak >nul'
Ln 'goto menu'
Ln ''
Ln ':barra'
Ln 'set /a _p=%1*40/100'
Ln 'set "_bar="'
Ln 'for /l %%k in (1,1,40) do ('
Ln '  if %%k leq !_p! (set "_bar=!_bar!#") else (set "_bar=!_bar!-")'
Ln ')'
Ln 'echo    [!_bar!]  %1%%'
Ln 'exit /b'
Ln ''
# =====================================================================
#  TELAS DE CATEGORIA  (paginadas, com caixa de marcacao)
# =====================================================================
$PP = 16
$rc = @{ 'Baixo' = 'BAIXO  '; 'Medio' = 'MEDIO  '; 'Alto' = 'ALTO   '; 'Critico' = 'CRITICO' }
$rec = @{ 'Baixo' = 'rec'; 'Medio' = 'cui'; 'Alto' = 'evi'; 'Critico' = 'nun' }

foreach ($ck in $nomesCat) {
    $gs  = @($porCat[$ck])
    $n   = $gs.Count
    $np  = [math]::Ceiling($n / $PP)

    for ($pg = 1; $pg -le $np; $pg++) {
        $ini = ($pg - 1) * $PP
        $fim = [math]::Min($ini + $PP, $n)
        Ln (':c_' + $ck + '_p' + $pg)
        Ln 'cls'
        Ln 'echo.'
        Ln 'echo  =========================================================================='
        Ln ('echo    %C_' + $ck + '%' + ('{0,6}' -f $n) + ' %T_ajuste%' + $(if ($np -gt 1) { '     %T_page% ' + $pg + '/' + $np } else { '' }))
        Ln 'echo  =========================================================================='
        Ln 'echo     #  MARCA  RISCO      AJUSTE'
        for ($j = $ini; $j -lt $fim; $j++) {
            $g  = $gs[$j]
            $sq = '{0:00000}' -f $seq[$g.Chave]
            $mm = 'M_' + $sq
            $rl = $rc[$g.Nivel]; if (-not $rl) { $rl = '------ ' }
            $nm = NmDe $g
            Ln ('set "Bx=[  ]"')
            Ln ('if defined ' + $mm + ' set "Bx=[OK]"')
            Ln ('set "Nm=!NP_' + $sq + '!"')
            Ln ('if "%L%"=="en" set "Nm=!NE_' + $sq + '!"')
            Ln ('echo    %Bx%  ' + ('{0,3}' -f ($j + 1)) + '  ' + $rl + '  !Nm!')
        }
        Ln 'echo  -------------------------------------------------------------------------'
        Ln 'echo    [0] %T_back%   [T] %T_markall%   [U] %T_unmark%   [H] %T_help%'
        Ln 'echo    %T_numdet%'
        Ln 'echo    [M] %T_apply% (%CNT%)   [D] %T_undo% (%CNT%)'
        if ($np -gt 1) { Ln 'echo    [P] %T_next%   [Z] %T_prev%' }
        Ln 'echo  =========================================================================='
        Ln 'set "Op="'
        Ln 'set /p "Op=%T_choose%: "'
        Ln 'if "%Op%"=="0" goto menu'
        Ln ('if /i "%Op%"=="T" goto mkall_' + $ck)
        Ln ('if /i "%Op%"=="U" goto umall_' + $ck)
        Ln ('if /i "%Op%"=="H" goto ajuda')
        Ln 'if /i "%Op%"=="M" goto aplicar'
        Ln 'if /i "%Op%"=="D" goto desfazer'
        if ($pg -lt $np) { Ln ('if /i "%Op%"=="P" goto c_' + $ck + '_p' + ($pg + 1)) }
        if ($pg -gt 1) { Ln ('if /i "%Op%"=="Z" goto c_' + $ck + '_p' + ($pg - 1)) }
        for ($j = $ini; $j -lt $fim; $j++) {
            Ln ('if "%Op%"=="' + ($j + 1) + '" goto dt_' + ('{0:00000}' -f $seq[$gs[$j].Chave]))
        }
        Ln ('goto c_' + $ck + '_p' + $pg)
        Ln ''
    }

    Ln (':mkall_' + $ck)
    Ln 'cls'
    Ln 'echo    %T_markall%...'
    foreach ($g in $gs) { Ln ('set "M_' + ('{0:00000}' -f $seq[$g.Chave]) + '=1"') }
    Ln ('set /a CNT+=' + $n)
    Ln 'timeout /t 1 /nobreak >nul'
    Ln ('goto c_' + $ck + '_p1')
    Ln ''

    Ln (':umall_' + $ck)
    Ln 'cls'
    Ln 'echo    %T_unmark%...'
    foreach ($g in $gs) { Ln ('set "M_' + ('{0:00000}' -f $seq[$g.Chave]) + '="') }
    Ln ('set /a CNT-=' + $n)
    Ln 'timeout /t 1 /nobreak >nul'
    Ln ('goto c_' + $ck + '_p1')
    Ln ''

    for ($j = 0; $j -lt $n; $j++) {
        $sq  = '{0:00000}' -f $seq[$gs[$j].Chave]
        $ret = 'c_' + $ck + '_p' + ([math]::Floor($j / $PP) + 1)
        Ln (':tg_' + $sq)
        Ln ('if defined M_' + $sq + ' goto tgu_' + $sq)
        Ln ('set "M_' + $sq + '=1"')
        Ln 'set /a CNT+=1'
        Ln ('goto ' + $ret)
        Ln (':tgu_' + $sq)
        Ln ('set "M_' + $sq + '="')
        Ln 'set /a CNT-=1'
        Ln ('goto ' + $ret)
        Ln ''
    }
}
# =====================================================================
#  TELA DE DETALHE DO AJUSTE (o que faz, ganho, risco, comandos)
# =====================================================================
foreach ($ck in $nomesCat) {
    $gs = @($porCat[$ck])
    for ($j = 0; $j -lt $gs.Count; $j++) {
        $g  = $gs[$j]
        $sq = '{0:00000}' -f $seq[$g.Chave]
        $cm = @($g.Ops | ForEach-Object { Get-Cmd $_ })
        $n  = $cm.Count
        $rv = Get-Rev $g
        $nm = NmDe $g
        $dd = DescDe $g
        $gg = GanhoDe $g
        $ret = 'c_' + $ck + '_p' + ([math]::Floor($j / $PP) + 1)
        $rl  = $rc[$g.Nivel]; if (-not $rl) { $rl = '------ ' }
        $tag = $rec[$g.Nivel]; if (-not $tag) { $tag = 'cui' }

        # -------- tela de detalhe (visto ao digitar o numero) ----------
        Ln (':dt_' + $sq)
        Ln 'cls'
        Ln 'echo.'
        Ln 'echo  =========================================================================='
        Ln ('echo    %T_dver%  -  ' + (Et $g.Nivel) + ' = %T_' + $tag + '%')
        Ln 'echo  =========================================================================='
        Ln ('set "NmO=!NP_' + $sq + '!"')
        Ln ('if "%L%"=="en" set "NmO=!NE_' + $sq + '!"')
        Ln 'echo    !NmO!'
        Ln 'echo  -------------------------------------------------------------------------'
        Ln ('echo    %T_oqfaz%:')
        if ($dd[0]) {
            # quebra a explicacao em linhas de ~92 colunas, ate 6 linhas
            $exp = (Limpa $dd[0] 560)
            $pal = $exp -split '\s+'
            $linhas = @(); $cur = ''
            foreach ($p in $pal) {
                if (($cur.Length + $p.Length + 1) -gt 92) { $linhas += $cur; $cur = $p }
                else { if ($cur) { $cur = $cur + ' ' + $p } else { $cur = $p } }
            }
            if ($cur) { $linhas += $cur }
            $linhas = @($linhas | Select-Object -First 6)
            foreach ($lnX in $linhas) { Ln ('echo    ' + (Et $lnX 92)) }
        } else {
            Ln 'echo    Sem descricao para este ajuste. / No description for this one.'
            Ln ('echo    Origem/Source: ' + (Et $g.Fonte 40) + '   Comandos/Commands: ' + $n)
            Ln 'echo    Leia os comandos com [E] antes de aplicar. / Read them with [E] first.'
        }
        Ln 'echo.'
        if ($gg[0]) {
            Ln ('echo    %T_ganho%:')
            $gn = (Limpa $gg[0] 280)
            $pal2 = $gn -split '\s+'
            $linhas2 = @(); $cur2 = ''
            foreach ($p in $pal2) {
                if (($cur2.Length + $p.Length + 1) -gt 92) { $linhas2 += $cur2; $cur2 = $p }
                else { if ($cur2) { $cur2 = $cur2 + ' ' + $p } else { $cur2 = $p } }
            }
            if ($cur2) { $linhas2 += $cur2 }
            $linhas2 = @($linhas2 | Select-Object -First 3)
            foreach ($lnX in $linhas2) { Ln ('echo    ' + (Et $lnX 92)) }
            Ln 'echo.'
        }
        if (PrecisaReiniciar $g) { Ln ('echo    ** %T_rein% **'); Ln 'echo.' }
        Ln ('echo    %T_risco% ' + $rl + ' - %T_origem%: ' + (Et $g.Fonte 36))
        Ln ('echo    ' + $n + ' %T_cmds%')
        Ln 'echo.'
        Ln ('echo    [M] %T_markit%')
        Ln ('echo    [E] %T_view%   [T] %T_exall%   [R] %T_revit%   [0] %T_voltal%')
        Ln 'echo  =========================================================================='
        Ln 'set "Op="'
        Ln 'set /p "Op=%T_choose%: "'
        Ln ('if /i "%Op%"=="M" goto tg_' + $sq)
        Ln ('if /i "%Op%"=="E" goto o_' + $ck + '_' + ($j + 1))
        Ln ('if /i "%Op%"=="T" goto ot_' + $sq)
        Ln ('if /i "%Op%"=="R" goto rv_' + $sq)
        Ln ('if "%Op%"=="0" goto ' + $ret)
        Ln ('goto dt_' + $sq)
        Ln ''

        # -------- tela de comandos ----------
        Ln (':o_' + $ck + '_' + ($j + 1))
        Ln 'cls'
        Ln 'echo.'
        Ln 'echo  =========================================================================='
        Ln 'echo    !NmO!'
        Ln ('echo    %T_risco%: ' + (Et $g.Nivel 10) + '   ' + (Et $g.Resumo 52))
        Ln ('echo    %T_origem%: ' + (Et $g.Fonte 40) + '     ' + $n + ' %T_cmds%')
        Ln 'echo  =========================================================================='
        Ln 'echo.'
        for ($q = 0; $q -lt $n; $q++) {
            Ln ('echo    ' + ('{0,3}' -f ($q + 1)) + ')  ' + (Dc $cm[$q]))
            Ln ('if "%Op%"=="' + ($q + 1) + '" goto x_' + $sq + '_' + ($q + 1))
        }
        Ln 'echo.'
        Ln 'echo    [0] %T_voltal%     [T] %T_exall%     [R] %T_revit%     [D] %T_back%'
        Ln 'echo  =========================================================================='
        Ln 'set "Op="'
        Ln 'set /p "Op=%T_choose%: "'
        Ln ('if "%Op%"=="0" goto dt_' + $sq)
        Ln ('if /i "%Op%"=="T" goto ot_' + $sq)
        Ln ('if /i "%Op%"=="R" goto rv_' + $sq)
        Ln ('if /i "%Op%"=="D" goto dt_' + $sq)
        Ln ('goto o_' + $ck + '_' + ($j + 1))
        Ln ''

        # -------- executar todos de uma vez ----------
        Ln (':ot_' + $sq)
        Ln 'cls'
        Ln 'echo.'
        Ln 'echo  -------------------------------------------------------------------------'
        Ln 'echo    %T_exall%: !NmO!'
        Ln 'echo  -------------------------------------------------------------------------'
        Ln ('echo    %T_risco%: ' + (Et $g.Nivel 10) + '   (' + $n + ' %T_cmds%)')
        Ln 'echo.'
        Ln 'choice /C SN /N /M "    %T_choose%  %T_yn% : "'
        Ln ('if errorlevel 2 goto dt_' + $sq)
        foreach ($c in $cm) { Ln $c }
        Ln 'echo.'
        Ln ('echo    [%T_done%]  ' + $n + ' %T_executados%')
        Ln 'pause'
        Ln ('goto dt_' + $sq)
        Ln ''

        # -------- reverter este ajuste ----------
        Ln (':rv_' + $sq)
        Ln 'cls'
        Ln 'echo.'
        Ln 'echo  -------------------------------------------------------------------------'
        Ln 'echo    %T_revit%: !NmO!'
        Ln 'echo  -------------------------------------------------------------------------'
        if ($rv.Count -eq 0) {
            Ln 'echo    %T_norev%'
        } else {
            Ln ('echo    ' + $rv.Count + ' %T_cmds% de reversao. %T_hd9%.')
            Ln 'choice /C SN /N /M "    %T_choose%  %T_yn% : "'
            Ln ('if errorlevel 2 goto dt_' + $sq)
            foreach ($c in $rv) { Ln $c }
            Ln 'echo.'
            Ln 'echo    [%T_done%]'
        }
        Ln 'pause'
        Ln ('goto dt_' + $sq)
        Ln ''

        # -------- comando individual ----------
        for ($q = 0; $q -lt $n; $q++) {
            Ln (':x_' + $sq + '_' + ($q + 1))
            Ln 'cls'
            Ln 'echo.'
            Ln 'echo  -------------------------------------------------------------------------'
            Ln 'echo    !NmO!'
            Ln ('echo    ' + ($q + 1) + '/' + $n + '   %T_exone%')
            Ln 'echo  -------------------------------------------------------------------------'
            Ln 'echo.'
            Ln ('echo    ' + (Dc $cm[$q]))
            Ln 'echo.'
            Ln 'choice /C SNV /N /M "    %T_choose%  %T_yn%  [V]oltar : "'
            Ln ('if errorlevel 3 goto o_' + $ck + '_' + ($j + 1))
            Ln ('if errorlevel 2 goto o_' + $ck + '_' + ($j + 1))
            Ln $cm[$q]
            Ln 'echo.'
            Ln 'echo    [%T_done%]'
            Ln 'pause'
            Ln ('goto o_' + $ck + '_' + ($j + 1))
            Ln ''
        }
    }
}
# =====================================================================
#  CORRENTE DE APLICAR
# =====================================================================
Ln ':aplicar'
Ln 'if "%CNT%"=="0" goto semmarca'
Ln 'cls'
Ln 'echo.'
Ln 'echo  =========================================================================='
Ln 'echo    %T_appl%'
Ln 'echo  =========================================================================='
Ln 'set "_i=0"'
Ln 'set "_ok=0"'
Ln 'set "_er=0"'
Ln 'set "_tot=%CNT%"'
Ln 'echo.'
Ln 'call :barra 0'
$id = 0
foreach ($g in $ordem) {
    $id++
    $sq  = '{0:00000}' -f $id
    $cm  = @($g.Ops | ForEach-Object { Get-Cmd $_ })
    Ln (':ap_' + $sq)
    Ln ('if not defined M_' + $sq + ' goto ' + $(if ($id -ge $ordem.Count) { 'ap_fim' } else { 'ap_' + ('{0:00000}' -f ($id + 1)) }))
    Ln 'set /a _i+=1'
    Ln 'set /a _pc=(_i*100)/_tot'
    Ln 'call :barra %_pc%'
    Ln ('set "NmA=!NP_' + $sq + '!"')
    Ln ('if "%L%"=="en" set "NmA=!NE_' + $sq + '!"')
    Ln 'echo     [%_i%/%_tot%]  !NmA!'
    foreach ($c in $cm) { Ln ('echo        * ' + (Dc $c)) }
    foreach ($c in $cm) { Ln $c }
    Ln 'set /a _ok+=1'
    Ln ('goto ' + $(if ($id -ge $ordem.Count) { 'ap_fim' } else { 'ap_' + ('{0:00000}' -f ($id + 1)) }))
    Ln ''
}
Ln ':ap_fim'
Ln 'echo.'
Ln 'echo  =========================================================================='
Ln 'echo    %T_done%   %_ok%'
Ln 'echo  =========================================================================='
Ln 'echo.'
Ln 'pause'
Ln 'goto menu'
Ln ''
# =====================================================================
#  CORRENTE DE DESFAZER
# =====================================================================
Ln ':desfazer'
Ln 'if "%CNT%"=="0" goto semmarca'
Ln 'cls'
Ln 'echo.'
Ln 'echo  =========================================================================='
Ln 'echo    %T_undy%'
Ln 'echo  =========================================================================='
Ln 'set "_i=0"'
Ln 'set "_ok=0"'
Ln 'set "_sk=0"'
Ln 'set "_tot=%CNT%"'
Ln 'echo.'
Ln 'call :barra 0'
$id = 0
foreach ($g in $ordem) {
    $id++
    $sq = '{0:00000}' -f $id
    $rv = Get-Rev $g
    Ln (':dr_' + $sq)
    Ln ('if not defined M_' + $sq + ' goto ' + $(if ($id -ge $ordem.Count) { 'dr_fim' } else { 'dr_' + ('{0:00000}' -f ($id + 1)) }))
    Ln 'set /a _i+=1'
    Ln 'set /a _pc=(_i*100)/_tot'
    Ln 'call :barra %_pc%'
    Ln ('set "NmA=!NP_' + $sq + '!"')
    Ln ('if "%L%"=="en" set "NmA=!NE_' + $sq + '!"')
    Ln 'echo     [%_i%/%_tot%]  !NmA!'
    if ($rv.Count -eq 0) {
        Ln 'echo        * %T_norev%'
        Ln 'set /a _sk+=1'
    } else {
        foreach ($c in $rv) { Ln ('echo        * ' + (Dc $c)) }
        foreach ($c in $rv) { Ln $c }
        Ln 'set /a _ok+=1'
    }
    Ln ('goto ' + $(if ($id -ge $ordem.Count) { 'dr_fim' } else { 'dr_' + ('{0:00000}' -f ($id + 1)) }))
    Ln ''
}
Ln ':dr_fim'
Ln 'echo.'
Ln 'echo  =========================================================================='
Ln 'echo    %T_done%   %_ok%'
Ln 'echo  =========================================================================='
Ln 'echo.'
Ln 'pause'
Ln 'goto menu'
Ln ''
Ln ':semmarca'
Ln 'cls'
Ln 'echo.'
Ln 'echo    %T_none%'
Ln 'echo.'
Ln 'pause'
Ln 'goto menu'
Ln ''
Ln ':fim'
Ln 'cls'
Ln 'echo.'
Ln 'echo  =========================================================================='
Ln 'echo.'
Ln 'echo        ####   ####   ##  ##  ##   ##  ####   ####   ##  ##   ####'
Ln 'echo.'
Ln 'echo    ate mais  /  see you'
Ln 'echo.'
Ln 'echo  =========================================================================='
Ln 'echo.'
Ln 'pause'
Ln 'endlocal'
Ln ''
# =====================================================================
#  GRAVAR  (ANSI/CP1252: e assim que o cmd.exe le acento)
# =====================================================================
$texto = ($EmtLines -join "`r`n")
$texto = $texto -replace '[^\x09\x0A\x0D\x20-\x7E\u00A0-\u00FF]', ''
[IO.File]::WriteAllText($OutFile, $texto + "`r`n", [Text.Encoding]::GetEncoding(1252))
$cmdsTot = 0
foreach ($g in $ordem) { $cmdsTot += @($g.Ops).Count }
Write-Host ''
Write-Host ('Script gerado: {0}' -f $OutFile) -ForegroundColor Green
Write-Host ('  {0} ajustes - {1} comandos - {2:N0} linhas - {3:N0} KB' -f $ordem.Count, $cmdsTot, $EmtLines.Count, ((Get-Item $OutFile).Length / 1KB)) -ForegroundColor Green
