# =====================================================================
#  Kairo Tweaker :: 90.Motor.ps1
#  Aplica/detecta/reverte tweaks e acoes
# =====================================================================

# Detecta se a tweak ja esta aplicada (estado real, nao apenas "rodou")
function Test-KTTweakAtiva {
    param([Parameter(Mandatory)]$Tweak)
    try {
        if ($Tweak.Spec)   { return (Test-KTRegSpec  -Spec $Tweak.Spec) }
        if ($Tweak.Servicos) {
            foreach ($s in $Tweak.Servicos) {
                $svc = Get-Service -Name $s.Nome -ErrorAction SilentlyContinue
                if (-not $svc) { continue }
                $atual = "$($svc.StartType)"
                if ($atual -eq $s.Inicio) { return $true }
                return $false
            }
            return $true
        }
        if ($Tweak.Bcd) {
            foreach ($b in $Tweak.Bcd) {
                $v = Get-KTBcdValue -Nome $b.Nome
                if (-not $v.Existia) { return $false }
                if ([string]$v.Valor -ne [string]$b.Valor) { return $false }
            }
            return $true
        }
    } catch { }
    return $null   # sem como detectar (acoes/comandos)
}

function Invoke-KTAplicarTweak {
    <#
      .SYNOPSIS  Aplica uma tweak registrando tudo no journal (reversivel).
      .PARAMETER Forcar  Aplica mesmo se ja estiver marcada como aplicada.
    #>
    param([Parameter(Mandatory)]$Tweak, [switch]$Forcar, [switch]$Silencioso)

    if (-not $Forcar) {
        $estado = Test-KTTweakAtiva -Tweak $Tweak
        if ($estado -eq $true) {
            if (-not $Silencioso) { Write-Host "  [=] $($Tweak.Nome): ja aplicada" -ForegroundColor DarkGray }
            return 'ja-aplicada'
        }
    }

    $erro = $null
    try {
        if ($Tweak.AcervoArquivo) {
            $r = Invoke-KTItemAcervo -Arquivo $Tweak.AcervoArquivo -Silencioso:$Silencioso
            if ($r -eq 'falhou') { throw 'execucao do item do acervo falhou' }
        }
        if ($Tweak.Executar) {
            $acao = $global:KT_Acoes[$Tweak.Executar]
            if (-not $acao) { throw "Acao registrada nao encontrada: $($Tweak.Executar)" }
            & $acao $Silencioso | Out-Null
        }
        if ($Tweak.Spec)     { Invoke-KTRegSpec -Spec $Tweak.Spec }
        if ($Tweak.Bcd)      { foreach ($b in $Tweak.Bcd) { Set-KTBcdValue -Nome $b.Nome -Valor $b.Valor } }
        if ($Tweak.Servicos) { foreach ($s in $Tweak.Servicos) { Set-KTServiceStart -Nome $s.Nome -Start $s.Inicio | Out-Null } }
        if ($Tweak.Comandos) { foreach ($c in $Tweak.Comandos) { Invoke-KTCommand -Comando $c.Cmd -Reverso $c.Rev | Out-Null } }
    } catch {
        $erro = $_.Exception.Message
    }

    if ($erro) {
        if (-not $Silencioso) { Write-Host "  [X] $($Tweak.Nome): FALHOU - $erro" -ForegroundColor Red }
        Write-KTLog "Falha ao aplicar $($Tweak.Id): $erro" 'ERRO' -SemTela
        return 'falhou'
    }
    if (-not $Silencioso) { Write-Host "  [+] $($Tweak.Nome)" -ForegroundColor Green }
    Write-KTLog "Aplicado: $($Tweak.Id) - $($Tweak.Nome)" 'OK' -SemTela
    return 'aplicada'
}

# Aplica um conjunto de tweaks dentro de uma unica transacao
function Invoke-KTTransacao {
    param([Parameter(Mandatory)][AllowEmptyCollection()][array]$Tweaks, [string]$Motivo = 'otimizacao', [switch]$Forcar)

    if (-not $Tweaks -or $Tweaks.Count -eq 0) {
        Write-KTLog 'Nenhuma tweak selecionada.' 'AVISO'
        return
    }

    $global:KT_BackupDir = New-KTBackup -Motivo $Motivo
    Write-KTLog "Backup criado: $global:KT_BackupDir" 'TITULO'

    $res = @{ aplicada = 0; 'ja-aplicada' = 0; falhou = 0 }
    $precisaReiniciar = $false
    $aplicadasReal = @()
    $i = 0
    foreach ($t in $Tweaks) {
        $i++
        $pct = [math]::Round(($i / $Tweaks.Count) * 100)
        Write-Host ("  [{0,3}%] " -f $pct) -NoNewline -ForegroundColor DarkCyan
        $r = Invoke-KTAplicarTweak -Tweak $t -Forcar:$Forcar
        $res[$r] = $res[$r] + 1
        if ($r -eq 'aplicada') {
            $aplicadasReal += $t
            if ($t.Reinicio) { $precisaReiniciar = $true }
        }
    }

    # Remove o backup vazio quando nada mudou (evita lixo e "reverter" sem efeito)
    if ($aplicadasReal.Count -eq 0 -and $global:KT_BackupDir -and (Test-Path $global:KT_BackupDir)) {
        $j = Join-Path $global:KT_BackupDir 'journal.jsonl'
        $temConteudo = $false
        if (Test-Path $j) { $temConteudo = [bool](Get-Content $j -Encoding UTF8 | Where-Object { $_.Trim() }) }
        if (-not $temConteudo) {
            Remove-Item -LiteralPath $global:KT_BackupDir -Recurse -Force -ErrorAction SilentlyContinue
            Write-KTLog 'Nada novo a registrar: todas as tweaks ja estavam aplicadas.' 'AVISO'
            $global:KT_BackupDir = $null
            return $res
        }
    }

    Write-Host ''
    Write-KTLog "Aplicadas: $($res['aplicada']) | Ja aplicadas: $($res['ja-aplicada']) | Falhas: $($res['falhou'])" 'TITULO'
    if ($precisaReiniciar) { Write-KTLog 'Reinicie o Windows para que todas as alteracoes tenham efeito.' 'AVISO' }
    return $res
}
