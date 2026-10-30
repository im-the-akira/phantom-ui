# =====================================================================
#  Kairo Tweaker :: 60.Limpeza.ps1
#  Acoes de limpeza SEGURAS (o LimpezaCompletaPC.bat do acervo apagava
#  Prefetch, dllcache e cookies, o que DEGRADA o boot)
# =====================================================================

function Get-KTFolderSizeMB {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return 0 }
    try {
        $s = (Get-ChildItem $Path -Recurse -Force -File -ErrorAction SilentlyContinue |
              Measure-Object -Property Length -Sum).Sum
        if ($s) { return [math]::Round($s / 1MB, 1) }
    } catch { }
    return 0
}

function Get-KTLimparTargets {
    @(
        @{ Nome='Arquivos temporarios do usuário';  Path=$env:TEMP;                                                        Nivel='usuário' }
        @{ Nome='Windows\Temp';                     Path="$env:SystemRoot\Temp";                                         Nivel='sistema' }
        @{ Nome='Temp do sistema (C:\Temp)';        Path="$env:SystemDrive\Temp";                                         Nivel='sistema' }
        @{ Nome='Cache de shaders DXGI';            Path="$env:SystemRoot\Temp\DXGI";                                    Nivel='sistema' }
        @{ Nome='Logs do Windows (CBS)';            Path="$env:SystemRoot\Logs\CBS";                                      Nivel='sistema' }
        @{ Nome='Logs de aplicacoes';               Path="$env:SystemRoot\Logs";                                          Nivel='sistema' }
        @{ Nome='Cache do Windows Update';          Path="$env:SystemRoot\SoftwareDistribution\Download";                Nivel='sistema' }
        @{ Nome='Cache do Windows Installer';       Path="$env:SystemRoot\Installer\$PatchCache$";                        Nivel='sistema' }
        @{ Nome='MiniDumps de erro';                 Path="$env:SystemRoot\Minidump";                                      Nivel='sistema' }
        @{ Nome='Cache de thumbnails';              Path="$env:LOCALAPPDATA\Microsoft\Windows\Explorer";                 Filtro='thumbcache_*.db'; Nivel='usuário' }
        @{ Nome='Cache de shaders D3D';             Path="$env:LOCALAPPDATA\D3DSCache";                                   Nivel='usuário' }
        @{ Nome='Cache INet temporario';            Path="$env:LOCALAPPDATA\Microsoft\Windows\INetCache";                 Nivel='usuário' }
    )
}

function Measure-KTLimpeza {
    $linhas = @(); $total = 0
    foreach ($a in (Get-KTLimparTargets)) {
        if (-not (Test-Path $a.Path)) { continue }
        $tam = Get-KTFolderSizeMB -Path $a.Path
        if ($a.Filtro) {
            try {
                $m = (Get-ChildItem $a.Path -Filter $a.Filtro -File -Force -ErrorAction SilentlyContinue |
                      Measure-Object Length -Sum).Sum
                if ($m) { $tam = [math]::Round($m / 1MB, 1) }
            } catch { }
        }
        if ($tam -le 0) { continue }
        $total += $tam
        $linhas += [pscustomobject]@{ Nome = $a.Nome; Caminho = $a.Path; MB = $tam; nível = $a.nível }
    }
    return [pscustomobject]@{ Itens = $linhas; TotalMB = [math]::Round($total, 1) }
}

function Show-KTLimpezaRelatorio {
    $r = Measure-KTLimpeza
    Write-Host ''
    Write-Host '  ESPA‡O QUE PODE SER LIBERADO' -ForegroundColor Cyan
    Write-Host ('  ' + ('-' * 66)) -ForegroundColor DarkGray
    foreach ($l in ($r.Itens | Sort-Object MB -Descending)) {
        $cor = if ($l.nível -eq 'sistema') { 'Yellow' } else { 'Gray' }
        Write-Host ("  {0,-44} {1,9} MB" -f $l.Nome, $l.MB) -ForegroundColor $cor
    }
    Write-Host ('  ' + ('-' * 66)) -ForegroundColor DarkGray
    Write-Host ("  {0,-44} {1,9} MB" -f 'TOTAL', $r.TotalMB) -ForegroundColor Green
    Write-Host ''
    return $r
}

function Invoke-KTCleanup {
    <#
      .SYNOPSIS  Executa as acoes de limpeza do Kairo Tweaker.
      .PARAMETER ScanOnly  Apenas mede, sem apagar.
    #>
    param([switch]$ScanOnly, [switch]$IncluirPrefetch, [switch]$Silencioso)

    $r = Show-KTLimpezaRelatorio
    if ($ScanOnly) { Write-KTLog 'Modo analise: nada foi apagado.' 'TITULO'; return $r.TotalMB }
    if ($r.TotalMB -lt 5)  { Write-KTLog 'Espaco insignificant (<5 MB). Nada a fazer.' 'AVISO'; return $r.TotalMB }

    foreach ($l in $r.Itens) {
        try {
            if ($l.Caminho -like '*SoftwareDistribution\Download*') {
                Stop-Service wuauserv -Force -ErrorAction SilentlyContinue
                Stop-Service bits     -Force -ErrorAction SilentlyContinue
            }
            Get-ChildItem -LiteralPath $l.Caminho -Recurse -Force -File -ErrorAction SilentlyContinue |
                Remove-Item -Force -ErrorAction SilentlyContinue
        } catch { }
    }
    try { Clear-RecycleBin -Force -ErrorAction SilentlyContinue } catch { }

    if ($IncluirPrefetch) {
        Write-KTLog 'Limpando Prefetch: o proximo boot ficara mais LENTO (o Windows reconstroi).' 'AVISO'
        try {
            Get-ChildItem "$env:SystemRoot\Prefetch" -File -Force -ErrorAction SilentlyContinue |
                Where-Object { $_.Name -notlike 'NTFDCACHE*' } |
                Remove-Item -Force -ErrorAction SilentlyContinue
        } catch { }
    }
    try { Start-Service wuauserv -ErrorAction SilentlyContinue } catch { }
    try { Start-Service bits     -ErrorAction SilentlyContinue } catch { }

    if (-not $Silencioso) {
        try { Stop-Process -Name explorer -Force -ErrorAction SilentlyContinue } catch { }
    }
    Write-KTLog "Limpeza concluida. Aproximadamente $($r.TotalMB) MB liberados." 'OK'
    return $r.TotalMB
}

# Acoes registradas (executadas pelo menu principal).
# Usa .Add() para nao apagar as acoes de outros modulos.
$KT_Ac = @{
    'limpeza.scan'     = { param($Silencioso) Show-KTLimpezaRelatorio | Out-Null }
    'limpeza.executar' = { param($Silencioso) Invoke-KTCleanup -Silencioso:$Silencioso }
    'limpeza.prefetch' = { param($Silencioso) Invoke-KTCleanup -IncluirPrefetch -Silencioso:$Silencioso }
    'energia.ultimo'   = { param($Silencioso) Invoke-KTEnergiaMaxima -Silencioso:$Silencioso }
}
foreach ($k in $KT_Ac.Keys) { $global:KT_Acoes[$k] = $KT_Ac[$k] }
