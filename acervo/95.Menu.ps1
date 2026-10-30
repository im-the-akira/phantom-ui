# =====================================================================
#  Kairo Tweaker :: 95.Menu.ps1
#  Menu em abas: cada categoria e uma aba, com selecao e explicacao.
#
#  - A selecao NAO volta para o menu principal: marca/desmarca e a
#    lista redesenha na hora. So sai da aba com 0 (ou Enter vazio).
#  - T = marcar TODAS da aba atual | L = limpar TODAS da aba
#  - A = aplicar as marcadas sem sair da aba
#  - E = explicacoes da aba | 0/Enter = voltar
#  - As abas "Acervo: ..." listam os .reg/.bat/.cmd/.ps1/.vbs das
#    pastas do acervo, e cada arquivo pode ser marcado e aplicado.
# =====================================================================

$global:KT_Selecionados = @{}

# Ordem e titulo das abas do catalogo (as do acervo entram no fim)
$global:KT_Abas = @(
    @{ Chave='Otimização do Windows';    Titulo='OTIMIZAÇÃO DO WINDOWS' }
    @{ Chave='Jogos por Título';        Titulo='JOGOS POR TÍTULO (configs)' }
    @{ Chave='Jogos';                    Titulo='JOGOS' }
    @{ Chave='Mouse e Teclado';          Titulo='MOUSE E TECLADO' }
    @{ Chave='Rede e Internet';          Titulo='REDE E INTERNET' }
    @{ Chave='Energia e Desempenho';     Titulo='ENERGIA E DESEMPENHO' }
    @{ Chave='Privacidade e Telemetria'; Titulo='PRIVACIDADE E TELEMETRIA' }
    @{ Chave='Windows Update';            Titulo='WINDOWS UPDATE' }
    @{ Chave='Correção do Windows';      Titulo='CORREÇÃO DO WINDOWS (reparo/limpeza)' }
    @{ Chave='Ativação';                  Titulo='ATIVAÇÃO (KMS / IoT LTSC / MSI)' }
)

function Get-KTAbasComAcervo {
    <#  Abas do catalogo + abas geradas a partir das pastas do acervo.  #>
    $abas = @($global:KT_Abas)
    $acervo = $global:KT_Registry | Where-Object { $_.Categoria -like 'Acervo: *' } |
              Select-Object -ExpandProperty Categoria -Unique | Sort-Object
    foreach ($a in $acervo) { $abas += @{ Chave=$a; Titulo=$a.Substring(8).ToUpper(); Acervo=$true } }
    return $abas
}

function Get-KTTweaksDaAba { param([string]$Aba) @($global:KT_Registry | Where-Object { $_.Categoria -eq $Aba }) }

function Get-KTSelecionadas {
    $sel = @()
    foreach ($t in $global:KT_Registry) { if ($global:KT_Selecionados[$t.Id]) { $sel += $t } }
    return $sel
}

function Get-KTContagemAba {
    param([string]$Aba)
    $lista = Get-KTTweaksDaAba -Aba $Aba
    $marc = @($lista | Where-Object { $global:KT_Selecionados[$_.Id] }).Count
    return @{ Total = $lista.Count; Marcadas = $marc }
}

function Set-KTSelecaoAba {
    <#  Marca ou limpa TODAS as tweaks de uma aba.  #>
    param([string]$Aba, [switch]$Limpar)
    foreach ($t in (Get-KTTweaksDaAba -Aba $Aba)) {
        if ($Limpar) { $global:KT_Selecionados.Remove($t.Id) | Out-Null }
        else { $global:KT_Selecionados[$t.Id] = $true }
    }
}

# --------------------------------------------------------------------
#  Aba de listagem + selecao interativa (nao sai da aba ao selecionar)
# --------------------------------------------------------------------
function Show-KTLista {
    param([string]$Filtro = '')
    $todos = @($global:KT_Registry | Where-Object { -not $Filtro -or $_.Categoria -eq $Filtro })
    $i = 0
    foreach ($t in $todos) {
        $i++
        $marca = if ($global:KT_Selecionados[$t.Id]) { 'X' } else { ' ' }
        $atual = if ((Test-KTTweakAtiva -Tweak $t) -eq $true) { ' <- aplicada' } else { '' }
        $cor = switch ($t.Risco) { 'Alto' { 'Red' } 'Medio' { 'Yellow' } default { 'DarkGray' } }
        Write-Host ("   {0,2}. [{1}] {2}" -f $i, $marca, $t.Nome) -ForegroundColor White -NoNewline
        Write-Host ("  [{0}]" -f $t.Risco) -ForegroundColor $cor -NoNewline
        Write-Host $atual -ForegroundColor Green
    }
    return $todos
}

function Show-KTRodapeAba {
    param([string]$Aba)
    $c = Get-KTContagemAba -Aba $Aba
    Write-Host ''
    Write-Host ("   {0} itens nesta aba  |  {1} marcada(s) nesta aba  |  {2} marcada(s) no total" -f $c.Total, $c.Marcadas, (Get-KTSelecionadas).Count) -ForegroundColor Cyan
    Write-Host '   numero = marcar/desmarcar   T = marcar todas   L = limpar todas   A = aplicar marcadas' -ForegroundColor DarkGray
    Write-Host '   E = explicacoes   0 ou Enter = voltar' -ForegroundColor DarkGray
}

# --------------------------------------------------------------------
#  Aba de tweaks: loop de selecao (so sai com 0/Enter)
# --------------------------------------------------------------------
function Invoke-KTAbaTweaks {
    param([Parameter(Mandatory)][string]$Aba)
    $tituloAba = if ($Aba -like 'Acervo: *') { $Aba.Substring(8).ToUpper() } else { $Aba.ToUpper() }
    while ($true) {
        Show-KTTitulo
        Write-Host "   ABA: $tituloAba" -ForegroundColor Magenta
        Write-Host ''
        $todos = Show-KTLista -Filtro $Aba
        Show-KTRodapeAba -Aba $Aba
        Write-Host ''
        $op = Read-Host '   Opcao'
        if ($op -eq '' -or $op -eq '0') { return }

        if ($op -match '^\d+$') {
            $idx = [int]$op
            if ($idx -ge 1 -and $idx -le $todos.Count) {
                $t = $todos[$idx - 1]
                if ($global:KT_Selecionados[$t.Id]) { $global:KT_Selecionados.Remove($t.Id) | Out-Null }
                else { $global:KT_Selecionados[$t.Id] = $true }
            }
            continue   # redesenha a lista, sem voltar ao menu
        }
        switch ($op.ToUpper()) {
            'T' { Set-KTSelecaoAba -Aba $Aba; continue }
            'L' { Set-KTSelecaoAba -Aba $Aba -Limpar; continue }
            'A' {
                $desta = @(Get-KTTweaksDaAba -Aba $Aba | Where-Object { $global:KT_Selecionados[$_.Id] })
                if (-not $desta.Count) { Write-KTLog 'Nenhum item desta aba esta marcado.' 'AVISO'; Start-Sleep 1; continue }
                Show-KTTitulo
                Write-Host "   APLICANDO AS MARCADAS DE: $tituloAba" -ForegroundColor Magenta
                Write-Host ''
                New-KTRestorePoint -Descricao "Kairo Tweaker - $tituloAba (marcadas)" | Out-Null
                Invoke-KTTransacao -Tweaks $desta -Motivo "aba-$tituloAba" | Out-Null
                Write-Host ''
                [void](Read-Host '   Pressione ENTER para continuar')
                continue
            }
            'E' {
                Show-KTTitulo
                Show-KTExplicacoes -Filtro $Aba -Silencioso
                [void](Read-Host '   Pressione ENTER para voltar')
                continue
            }
        }
    }
}

# --------------------------------------------------------------------
#  Explicacoes detalhadas: o que cada funcao faz
# --------------------------------------------------------------------
function Show-KTExplicacao {
    param($Tweak)
    if (-not $Tweak) { return }
    $corRisco = switch ($Tweak.Risco) { 'Alto' { 'Red' } 'Medio' { 'Yellow' } default { 'Green' } }
    Write-Host ''
    Write-Host ('   ' + ('=' * 68)) -ForegroundColor DarkCyan
    Write-Host "   $($Tweak.Nome)" -ForegroundColor Cyan
    Write-Host ('   ' + ('=' * 68)) -ForegroundColor DarkCyan
    Write-Host ''
    Write-Host '   O QUE ESTA FUNCAO FAZ:' -ForegroundColor Yellow
    Write-Host "     $($Tweak.Explicacao)" -ForegroundColor Gray
    Write-Host ''
    if ($Tweak.Ganho) {
        Write-Host '   O QUE VOCE ESPERA:' -ForegroundColor Yellow
        Write-Host "     $($Tweak.Ganho)" -ForegroundColor Gray
        Write-Host ''
    }
    if ($Tweak.Problema) {
        Write-Host '   PROBLEMA QUE PODE CAUSAR:' -ForegroundColor Yellow
        Write-Host "     $($Tweak.Problema)" -ForegroundColor DarkYellow
        Write-Host ''
    }
    Write-Host '   RISCO:' -NoNewline -ForegroundColor Yellow
    Write-Host " $($Tweak.Risco)" -NoNewline -ForegroundColor $corRisco
    if ($Tweak.Reinicio) { Write-Host '   |   precisa reiniciar: SIM' -ForegroundColor Yellow }
    Write-Host ''
    if ($Tweak.Spec) {
        Write-Host '   O QUE SERA ALTERADO:' -ForegroundColor DarkCyan
        foreach ($s in $Tweak.Spec) { Write-Host "     $($s.Path) > $($s.Nome) = $($s.Valor)" -ForegroundColor DarkGray }
    }
    if ($Tweak.Servicos) {
        Write-Host '   SERVICOS QUE SERAO ALTERADOS:' -ForegroundColor DarkCyan
        foreach ($s in $Tweak.Servicos) { Write-Host "     $($s.Nome) > iniciar = $($s.Inicio)" -ForegroundColor DarkGray }
    }
    if ($Tweak.Bcd) {
        Write-Host '   AJUSTES DE BOOT QUE SERAO ALTERADOS:' -ForegroundColor DarkCyan
        foreach ($s in $Tweak.Bcd) { Write-Host "     $($s.Nome) = $($s.Valor)" -ForegroundColor DarkGray }
    }
    if ($Tweak.AcervoArquivo) {
        Write-Host '   ARQUIVO DO ACERVO QUE SERA EXECUTADO:' -ForegroundColor DarkCyan
        Write-Host "     $($Tweak.AcervoArquivo)" -ForegroundColor DarkGray
    }
    if ($Tweak.Comandos) {
        Write-Host '   COMANDOS QUE SERAO EXECUTADOS:' -ForegroundColor DarkCyan
        foreach ($s in $Tweak.Comandos) { Write-Host "     $($s.Cmd)" -ForegroundColor DarkGray }
    }
    Write-Host ''
    if ((Test-KTTweakAtiva -Tweak $Tweak) -eq $true) { Write-Host '   STATUS: ja aplicada neste PC' -ForegroundColor Green }
    else { Write-Host '   STATUS: ainda nao aplicada' -ForegroundColor DarkGray }
    Write-Host ''
}

function Show-KTExplicacoes {
    param([string]$Filtro = '', [switch]$Silencioso)
    foreach ($t in @($global:KT_Registry | Where-Object { -not $Filtro -or $_.Categoria -eq $Filtro })) {
        Show-KTExplicacao -Tweak $t
    }
    if (-not $Silencioso) {
        Write-Host '   Pressione ENTER para voltar' -ForegroundColor DarkGray
        [void](Read-Host)
    }
}

# --------------------------------------------------------------------
#  Aba de acoes (limpeza e ferramentas)
# --------------------------------------------------------------------
function Invoke-KTAbaFerramentas {
    while ($true) {
        Show-KTTitulo
        Write-Host '   ABA: LIMPEZA E FERRAMENTAS' -ForegroundColor Magenta
        Write-Host ''
        Write-Host '   [1] Diagnostico do sistema' -ForegroundColor Gray
        Write-Host '   [2] Ver quanto da para liberar (sem apagar)' -ForegroundColor Gray
        Write-Host '   [3] Executar limpeza' -ForegroundColor Gray
        Write-Host '   [4] Planos de energia (35 planos .pow do acervo)' -ForegroundColor Green
        Write-Host '   [5] Ferramentas de memoria' -ForegroundColor Green
        Write-Host '   [6] Desfazer alteracoes' -ForegroundColor Yellow
        Write-Host '   [7] Criar ponto de restauracao do Windows' -ForegroundColor Yellow
        Write-Host '   [8] Situacao do Windows Update' -ForegroundColor Cyan
        Write-Host '   [0] Voltar' -ForegroundColor DarkGray
        Write-Host ''
        $o = Read-Host '   Escolha'
        switch ($o) {
            '1' { Show-KTDiagnostico }
            '2' { Show-KTLimpezaRelatorio | Out-Null }
            '3' { Show-KTLimpezaRelatorio | Out-Null
                 if ((Read-Host '   Executar a limpeza? (s/N)') -eq 's') { Invoke-KTCleanup } }
            '4' { Show-KTPlanos }
            '5' { Show-KTFerramentasMemoria }
            '6' { Show-KTMenuReverter }
            '7' { New-KTRestorePoint }
            '8' { Show-KTStatusUpdate }
            '0' { return }
        }
        if ($o -eq '4' -or $o -eq '5') { continue }
        Write-Host ''
        [void](Read-Host '   Pressione ENTER para continuar')
    }
}

function Show-KTMenuReverter {
    $bks = @(Get-KTBackups)
    if (-not $bks) { Write-KTLog 'Nenhum backup encontrado para desfazer.' 'AVISO'; return }
    Write-Host ''
    Write-Host '   BACKUPS DISPONIVEIS:' -ForegroundColor Yellow
    for ($i = 0; $i -lt $bks.Count; $i++) {
        $motivo = '?'
        try { $motivo = (Get-Content (Join-Path $bks[$i].FullName 'meta.json') -Raw | ConvertFrom-Json).Motivo } catch { }
        Write-Host ("   {0,2}. {1}  ({2})" -f ($i+1), $bks[$i].Name, $motivo) -ForegroundColor Gray
    }
    $b = Read-Host '   Numero do backup a desfazer (0 = cancelar)'
    if ($b -and $b -match '^\d+$' -and [int]$b -ge 1 -and [int]$b -le $bks.Count) { Revert-KTBackup -Diretorio $bks[[int]$b - 1].FullName }
}

# --------------------------------------------------------------------
#  PERFIS PRONTOS: aplica conjuntos de uma vez
# --------------------------------------------------------------------
function Show-KTPerfis {
    while ($true) {
        Show-KTTitulo
        Write-Host '   PERFIS PRONTOS - escolha o objetivo e o app aplica o conjunto' -ForegroundColor Magenta
        Write-Host ''
        Write-Host '   [1] ESTABILIDADE + FPS     - o estado aplicado neste PC' -ForegroundColor Green
        Write-Host '       (anti-stutter + otimizacao segura + input lag; VBS off, sem Tweaks agressivos)' -ForegroundColor DarkGray
        Write-Host '   [2] FPS EXTREME            - estabilidade-fps + agressivos do EXM' -ForegroundColor Yellow
        Write-Host '       (rede extrema, Edge removido, IRP/NVMe; itens com risco Med/Alto)' -ForegroundColor DarkGray
        Write-Host '   [3] ESTABILIDADE PURA      - so o seguro e reversivel, sem nada agressivo' -ForegroundColor Cyan
        Write-Host '   [4] PRIVACIDADE MAXIMA     - telemetria/sync/ads + WU bloqueado' -ForegroundColor Cyan
        Write-Host '   [0] Voltar' -ForegroundColor DarkGray
        Write-Host ''
        $o = Read-Host '   Escolha'
        switch ($o) {
            '1' { Show-KTAplicarPerfil -Nome 'estabilidade-fps' }
            '2' { Show-KTAplicarPerfil -Nome 'fps-extreme' }
            '3' { Show-KTAplicarPerfil -Nome 'basico' }
            '4' { Show-KTAplicarPerfil -Nome 'update.total' }
            '0' { return }
        }
        Write-Host ''
        [void](Read-Host '   Pressione ENTER para continuar')
    }
}

function Show-KTAplicarPerfil {
    param([string]$Nome)
    if (-not $global:KT_Perfis.Contains($Nome)) { Write-KTLog "Perfil invalido: $Nome" 'ERRO'; return }
    $sel = Resolve-KTSelecao -Ids $global:KT_Perfis[$Nome]
    if (-not $sel) { Write-KTLog 'Perfil sem tweaks validas.' 'AVISO'; return }
    Show-KTTitulo
    Write-Host ("   PERFIL: " + $Nome.ToUpper()) -ForegroundColor Magenta
    Write-Host ('   ' + $sel.Count + ' tweaks serao aplicadas (as ja aplicadas sao puladas).') -ForegroundColor Gray
    Write-Host ''
    if ((Read-Host '   Confirmar? (s/N)') -ne 's') { Write-KTLog 'Cancelado pelo usuario.' 'AVISO'; return }
    New-KTRestorePoint -Descricao "Kairo Tweaker - Perfil $Nome" | Out-Null
    Invoke-KTTransacao -Tweaks $sel -Motivo "perfil-$Nome" | Out-Null
}

# --------------------------------------------------------------------
#  Menu principal
# --------------------------------------------------------------------
function Invoke-KTMenu {
    param([switch]$Silencioso)
    while ($true) {
        Show-KTTitulo
        $abas = @(Get-KTAbasComAcervo)
        $totalMarc = (Get-KTSelecionadas).Count
        Write-Host '   ESCOLHA UMA ABA (as marcadas ficam guardadas entre abas)' -ForegroundColor DarkGray
        Write-Host ''
        $n = 0
        foreach ($aba in $abas) {
            $n++
            $c = Get-KTContagemAba -Aba $aba.Chave
            $extra = if ($c.Marcadas -gt 0) { " [$($c.Marcadas) marcada(s)]" } else { '' }
            Write-Host ("   [{0}] {1,-36} {2,2} itens{3}" -f $n, $aba.Titulo, $c.Total, $extra) -ForegroundColor White
        }
        Write-Host ("   [{0}] {1}" -f ($n+1), 'PERFIS PRONTOS (Estabilidade+FPS / FPS Extreme / mais)') -ForegroundColor Green
        Write-Host ("   [{0}] {1}" -f ($n+2), 'Limpeza e Ferramentas') -ForegroundColor White
        Write-Host ''
        if ($totalMarc -gt 0) {
            Write-Host "   [$($n+3)] APLICAR TUDO QUE ESTA MARCADO ($totalMarc item(ns), todas as abas)" -ForegroundColor Green
            Write-Host "   [$($n+4)] Ver explicacoes de tudo" -ForegroundColor Cyan
            Write-Host "   [$($n+5)] Limpar TODAS as marcacoes" -ForegroundColor Yellow
        }
        Write-Host "   [0] Sair" -ForegroundColor DarkGray
        Write-Host ''

        $op = Read-Host '   Opcao'
        if ($op -eq '0' -or $op -eq '') { return }

        if ($op -match '^\d+$') {
            $i = [int]$op
            if ($i -ge 1 -and $i -le $abas.Count) {
                $aba = $abas[$i-1]
                if ($aba.Chave -eq 'Jogos por Título') { Show-KTJogos }
                else { Invoke-KTAbaTweaks -Aba $aba.Chave }
            } elseif ($i -eq ($abas.Count + 1)) {
                Show-KTPerfis
            } elseif ($i -eq ($abas.Count + 2)) {
                Invoke-KTAbaFerramentas
            } elseif ($i -eq ($abas.Count + 3) -and $totalMarc -gt 0) {
                Show-KTTitulo
                Write-Host '   APLICANDO TUDO QUE ESTA MARCADO' -ForegroundColor Magenta
                Write-Host ''
                New-KTRestorePoint -Descricao 'Kairo Tweaker - Selecao personalizada'
                Invoke-KTTransacao -Tweaks (Get-KTSelecionadas) -Motivo 'menu-personalizado' | Out-Null
                Write-Host ''
                [void](Read-Host '   Pressione ENTER para continuar')
            } elseif ($i -eq ($abas.Count + 4) -and $totalMarc -gt 0) {
                Show-KTTitulo
                Show-KTExplicacoes -Silencioso
                [void](Read-Host '   Pressione ENTER para voltar')
            } elseif ($i -eq ($abas.Count + 5) -and $totalMarc -gt 0) {
                $global:KT_Selecionados.Clear()
                Write-KTLog 'Marcacoes limpas.' 'OK'
            }
        }
    }
}
