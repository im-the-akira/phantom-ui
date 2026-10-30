# =====================================================================
#  Kairo Tweaker :: 55.PlanosEnergia.ps1
#  Biblioteca de planos de energia (.pow) + criacao de plano via powercfg
# =====================================================================

# Os arquivos .pow da pasta "5 - Energia Optimizer" sao planos oficiais de
# terceiros. O Windows apenas LE o arquivo e cria o plano: nada e executado
# de dentro deles, entao nao ha risco de codigo malicioso.
$global:KT_BibliotecaPow = Join-Path $global:KT_PastaAcervo '5 - Energia Optimizer'

function Get-KTPlanosInstalados {
    <# Le os planos que ja estao no Windows, marcando o ativo. #>
    $saida = & powercfg.exe /list 2>$null
    $lista = @()
    foreach ($l in $saida) {
        if ($l -match '([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})\s+\((.+?)\)') {
            $guid = $Matches[1]; $nome = $Matches[2]
            $lista += [pscustomobject]@{
                Guid = $guid; Nome = $nome
                Ativo = ($l -match '\*')
            }
        }
    }
    return $lista
}

function Get-KTPlanosBiblioteca {
    <# Le os .pow disponiveis na pasta externa. #>
    if (-not (Test-Path $global:KT_BibliotecaPow)) { return @() }
    $porPasta = @{}
    foreach ($f in Get-ChildItem $global:KT_BibliotecaPow -Recurse -Filter '*.pow' -File -ErrorAction SilentlyContinue) {
        $pasta = $f.Directory.Name
        if (-not $porPasta.ContainsKey($pasta)) { $porPasta[$pasta] = @() }
        $porPasta[$pasta] += $f
    }
    $resultado = @()
    foreach ($pasta in ($porPasta.Keys | Sort-Object)) {
        foreach ($f in $porPasta[$pasta]) {
            $resultado += [pscustomobject]@{
                Grupo   = $pasta
                Nome    = [System.IO.Path]::GetFileNameWithoutExtension($f.Name)
                Arquivo = $f.FullName
                KB      = [math]::Round($f.Length / 1KB, 0)
            }
        }
    }
    return $resultado
}

function Import-KTPlano {
    <#  Importa um plano .pow e opcionalmente o ativa.  #>
    param([Parameter(Mandatory)][string]$Arquivo, [switch]$Ativar)
    if (-not (Test-Path $Arquivo)) {
        Write-KTLog "Arquivo de plano nao encontrado: $Arquivo" 'ERRO'
        return $null
    }
    $saida = & powercfg.exe /import "$Arquivo" 2>&1 | Out-String
    $m = [regex]::Match($saida, '([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})')
    if (-not $m.Success) {
        Write-KTLog "Falha ao importar (arquivo pode estar corrompido): $(Split-Path $Arquivo -Leaf)" 'ERRO'
        return $null
    }
    $guid = $m.Groups[1].Value
    $nome = [System.IO.Path]::GetFileNameWithoutExtension($Arquivo)
    if ($Ativar) {
        $null = & powercfg.exe /setactive $guid 2>&1
        Write-KTLog "Plano '$nome' importado e ativado" 'OK'
    } else {
        Write-KTLog "Plano '$nome' importado. Ative depois se quiser usar." 'OK'
    }
    return [pscustomobject]@{ Guid = $guid; Nome = $nome }
}

function Set-KTPlanoAtivo {
    param([Parameter(Mandatory)][string]$Guid)
    $p = Get-KTPlanosInstalados | Where-Object { $_.Guid -eq $Guid }
    if (-not $p) { Write-KTLog "Plano nao encontrado no sistema: $Guid" 'ERRO'; return $false }
    $null = & powercfg.exe /setactive $Guid 2>&1
    Write-KTLog "Plano de energia ativado: $($p.Nome)" 'OK'
    return $true
}

function Remove-KTPlano {
    param([Parameter(Mandatory)][string]$Guid)
    $p = Get-KTPlanosInstalados | Where-Object { $_.Guid -eq $Guid }
    if (-not $p) { Write-KTLog 'Plano nao encontrado.' 'ERRO'; return $false }
    if ($p.Ativo) { Write-KTLog 'Nao da para remover o plano ativo. Ative outro antes.' 'AVISO'; return $false }
    $null = & powercfg.exe /delete $Guid 2>&1
    Write-KTLog "Plano removido: $($p.Nome)" 'OK'
    return $true
}

function Show-KTPlanos {
    <#  Menu interativo de planos de energia.  #>
    while ($true) {
        Show-KTTitulo
        Write-Host '   PLANOS DE ENERGIA' -ForegroundColor Magenta
        Write-Host ''
        Write-Host '   JA INSTALADOS NO WINDOWS' -ForegroundColor Cyan
        $inst = @(Get-KTPlanosInstalados)
        $i = 0
        foreach ($p in $inst) {
            $i++
            $marca = if ($p.Ativo) { ' <- ATIVO' } else { '' }
            $cor = if ($p.Ativo) { 'Green' } else { 'Gray' }
            $n = $p.Nome; if ($n.Length -gt 44) { $n = $n.Substring(0, 44) }
            Write-Host ("   {0,2}. {1,-45}{2}" -f $i, $n, $marca) -ForegroundColor $cor
        }
        Write-Host ''
        Write-Host '   BIBLIOTECA DE PLANOS (.pow)' -ForegroundColor Cyan
        if (-not (Test-Path $global:KT_BibliotecaPow)) {
            Write-Host "   Pasta nao encontrada: $global:KT_BibliotecaPow" -ForegroundColor DarkGray
            $b = @()
        } else {
            $b = @(Get-KTPlanosBiblioteca)
            $j = 100; $grupoAtual = ''
            foreach ($p in $b) {
                if ($p.Grupo -ne $grupoAtual) {
                    $grupoAtual = $p.Grupo
                    Write-Host "     [$grupoAtual]" -ForegroundColor DarkGray
                }
                $j++
                Write-Host ("   {0,3}. {1}" -f $j, $p.Nome) -ForegroundColor White
            }
        }
        Write-Host ''
        Write-Host '   numero = ativar/importar  |  R = remover  |  0 = voltar' -ForegroundColor DarkGray
        $op = Read-Host '   Escolha'
        if ($op -eq '0' -or $op -eq '') { return }
        if ($op -match '^\d+$') {
            $n = [int]$op
            if ($n -ge 1 -and $n -le $inst.Count) { Set-KTPlanoAtivo -Guid $inst[$n-1].Guid }
            elseif ($n -gt 100) {
                $idx = $n - 100
                if ($idx -ge 1 -and $idx -le $b.Count) {
                    $r = Import-KTPlano -Arquivo $b[$idx-1].Arquivo -Ativar
                    if ($r) { Set-KTPlanoAtivo -Guid $r.Guid }
                }
            }
        } elseif ($op -match '^[Rr]$') {
            $n = Read-Host '   Numero do plano instalado para remover'
            if ($n -match '^\d+$' -and [int]$n -ge 1 -and [int]$n -le $inst.Count) {
                Remove-KTPlano -Guid $inst[[int]$n-1].Guid
            }
        }
        Write-Host ''
        [void](Read-Host '   Pressione ENTER para continuar')
    }
}
