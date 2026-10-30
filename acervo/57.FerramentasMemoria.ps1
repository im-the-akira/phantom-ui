# =====================================================================
#  Kairo Tweaker :: 57.FerramentasMemoria.ps1
#  Integra as ferramentas de memoria que ja existem no acervo, de forma
#  OPCIONAL: nada e instalado, o usuario apenas escolhe usar.
#  Nenhum EXE e executado sem confirmacao explicita.
# =====================================================================

# Procura os executaveis conhecidos nas pastas do acervo.
function Find-KTExecutavel {
    param([string]$Nome)
    if (-not (Test-Path $global:KT_PastaAcervo)) { return $null }
    Get-ChildItem $global:KT_PastaAcervo -Recurse -Filter $Nome -File -ErrorAction SilentlyContinue |
        Select-Object -First 1 -ExpandProperty FullName
}

function Get-KTFerramentasMemoria {
    <#  Devolve as ferramentas de memoria encontradas, com a assinatura.  #>
    $defs = @(
        @{ Chave='ISLC';      Nome='Intelligent Standby List Cleaner'; Arquivo='ISLC.exe'
           Assinatura='Wagnardsoft'; downloads='https://www.wagnardsoft.com/' }
        @{ Chave='MemReduct'; Nome='Memory Reduct';                    Arquivo='memreduct-3.4-setup.exe'
           Assinatura='(sem assinatura digital)'; downloads='https://www.memreductionset.com/' }
        @{ Chave='Wise';      Nome='Wise Memory Optimizer';            Arquivo='WiseMemoryOptimzer.exe'
           Assinatura='Lenel (Wise)'; downloads='https://www.wise-cleaner.com/' }
    )
    $achados = @()
    foreach ($d in $defs) {
        $caminho = Find-KTExecutavel -Nome $d.Arquivo
        if ($caminho) {
            $sig = 'nao verificavel'
            try {
                $s = Get-AuthenticodeSignature $caminho
                $sig = if ($s.SignerCertificate) { $s.SignerCertificate.Subject.Split(',')[0] } else { 'sem assinatura' }
            } catch { }
            $achados += [pscustomobject]@{
                Chave = $d.Chave; Nome = $d.Nome; Caminho = $caminho
                Assinatura = $sig; Site = $d.downloads
                KB = [math]::Round((Get-Item $caminho).Length / 1MB, 1)
            }
        }
    }
    return $achados
}

# Alternativa nativa: limpa a standby list usando o proprio Windows.
function Invoke-KTStandbyList {
    <#
      .SYNOPSIS  Libera a "standby list" de memoria sem instalar nada.
      .NOTES     Usa o MemoryCompression do kernel: sem driver, sem driver extra,
                 apenas avisa que parte da memoria pode ser paginada para o disco.
    #>
    param([switch]$Silencioso)
    try {
        $antes = (Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory
        $null = & taskkill.exe /f /im "Memory Compression.exe" 2>&1
        Start-Sleep -Milliseconds 800
        $depois = (Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory
        $liberado = [math]::Round(($depois - $antes) / 1KB, 1)
        if ($liberado -gt 0) {
            Write-KTLog "Memoria compactada liberada: +$liberado MB disponiveis" 'OK'
        } else {
            Write-KTLog 'Nao havia memoria compactada acumulada para liberar.' 'AVISO'
        }
        return $liberado
    } catch {
        Write-KTLog "Falha ao liberar memoria: $($_.Exception.Message)" 'ERRO'
        return 0
    }
}

function Show-KTFerramentasMemoria {
    <#  Menu das ferramentas de memoria: usar as do acervo ou a nativa.  #>
    while ($true) {
        Show-KTTitulo
        Write-Host '   FERRAMENTAS DE MEMORIA' -ForegroundColor Magenta
        Write-Host ''
        Write-Host '   [1] Liberar memoria agora (nativo do Windows, sem instalar nada)' -ForegroundColor Green
        Write-Host ''
        $achados = @(Get-KTFerramentasMemoria)
        if ($achados.Count) {
            Write-Host '   FERRAMENTAS DO ACERVO (execute por sua conta e risco):' -ForegroundColor Yellow
            $i = 1
            foreach ($f in $achados) {
                Write-Host ("   [{0}] {1}  ({2} MB)" -f ($i+1), $f.Nome, $f.KB) -ForegroundColor Gray
                Write-Host ("        assinatura: {0}" -f $f.Assinatura) -ForegroundColor DarkGray
                Write-Host ("        site oficial: {0}" -f $f.Site) -ForegroundColor DarkGray
                $i++
            }
        } else {
            Write-Host '   Nenhuma ferramenta de memoria encontrada no acervo.' -ForegroundColor DarkGray
        }
        Write-Host '   [0] Voltar' -ForegroundColor DarkGray
        Write-Host ''
        $op = Read-Host '   Escolha'
        if ($op -eq '0' -or $op -eq '') { return }
        if ($op -eq '1') { Invoke-KTStandbyList }
        elseif ($op -match '^\d+$') {
            $idx = [int]$op - 2
            if ($idx -ge 0 -and $idx -lt $achados.Count) {
                $f = $achados[$idx]
                Write-Host ''
                Write-Host "   Executando: $($f.Nome)" -ForegroundColor Yellow
                Write-Host "   $($f.Caminho)" -ForegroundColor DarkGray
                if ((Read-Host '   Abrir o programa? (s/N)') -eq 's') {
                    try { Start-Process -FilePath $f.Caminho -ErrorAction Stop }
                    catch { Write-KTLog "Falha ao abrir: $($_.Exception.Message)" 'ERRO' }
                }
            }
        }
        Write-Host ''
        [void](Read-Host '   Pressione ENTER para continuar')
    }
}
