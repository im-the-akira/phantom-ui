# =====================================================================
#  Kairo Tweaker :: 97.Diagnostico.ps1
#  Leitura do estado real do PC (sem alterar nada)
# =====================================================================

function Show-KTDiagnostico {
    $os  = Get-CimInstance Win32_OperatingSystem
    $cpu = Get-CimInstance Win32_Processor | Select-Object -First 1
    $gpu = Get-CimInstance Win32_VideoController | Select-Object -First 1

    $totalGB = [math]::Round($os.TotalVisibleMemorySize / 1MB, 1)
    $livreGB = [math]::Round($os.FreePhysicalMemory / 1MB, 1)
    $usadoPct = [math]::Round((1 - ($os.FreePhysicalMemory / $os.TotalVisibleMemorySize)) * 100)

    Write-Host ''
    Write-Host '  DIAGNOSTICO DO SISTEMA' -ForegroundColor Cyan
    Write-Host ('  ' + ('-' * 66)) -ForegroundColor DarkGray
    Write-Host ("  {0,-26} {1}" -f 'Sistema',        $os.Caption)          -ForegroundColor Gray
    Write-Host ("  {0,-26} {1}" -f 'Build',          $os.BuildNumber)      -ForegroundColor Gray
    Write-Host ("  {0,-26} {1}" -f 'CPU',            "$($cpu.Name -replace '\s+',' ')") -ForegroundColor Gray
    Write-Host ("  {0,-26} {1}" -f 'Núcleos/Threads', "$($cpu.NumberOfCores) / $($cpu.NumberOfLogicalProcessors)") -ForegroundColor Gray
    Write-Host ("  {0,-26} {1}" -f 'GPU',            $gpu.Name)            -ForegroundColor Gray
    $ramCor = if ($usadoPct -gt 85) { 'Red' } elseif ($usadoPct -gt 70) { 'Yellow' } else { 'Green' }
    Write-Host ("  {0,-26} {1} GB total / {2} GB livres ({3}% em uso)" -f 'RAM', $totalGB, $livreGB, $usadoPct) -ForegroundColor $ramCor
    Write-Host ('  ' + ('-' * 66)) -ForegroundColor DarkGray

    # Disco
    Write-Host '  DISCOS' -ForegroundColor Magenta
    foreach ($d in (Get-CimInstance Win32_LogicalDisk -Filter 'DriveType=3')) {
        $usado = ($d.Size - $d.FreeSpace)
        $pct = if ($d.Size) { [math]::Round($usado / $d.Size * 100) } else { 0 }
        $tam = [math]::Round($d.Size / 1GB, 0)
        $liv = [math]::Round($d.FreeSpace / 1GB, 1)
        $cor = if ($pct -gt 90) { 'Red' } elseif ($pct -gt 75) { 'Yellow' } else { 'Green' }
        Write-Host ("   {0} {1,6} GB  |  {2,6} GB livres  |  {3,3}% usado" -f $d.DeviceID, $tam, $liv, $pct) -ForegroundColor $cor
    }

    # Top processos
    Write-Host '  TOP 8 PROCESSOS POR CPU' -ForegroundColor Magenta
    Get-Process -ErrorAction SilentlyContinue |
        Where-Object { $_.CPU -ne $null } |
        Sort-Object CPU -Descending | Select-Object -First 8 |
        ForEach-Object {
            Write-Host ("   {0,-24} {1,8:N1}s CPU  {2,7} MB" -f $_.ProcessName, $_.CPU, [math]::Round($_.WorkingSet64/1MB)) -ForegroundColor DarkGray
        }

    # Plano de energia
    $plano = (& powercfg.exe /getactivescheme 2>$null) -join ' '
    Write-Host '  PLANO DE ENERGIA ATUAL' -ForegroundColor Magenta
    Write-Host ("   {0}" -f $plano.Trim()) -ForegroundColor DarkGray

    # Estado das optimizations
    Write-Host '  ESTADO DAS OTIMIZACOES (por aba)' -ForegroundColor Magenta
    foreach ($aba in $global:KT_Abas) {
        $lista = @($global:KT_Registry | Where-Object { $_.Categoria -eq $aba.Chave })
        if (-not $lista.Count) { continue }
        $ok = 0
        foreach ($t in $lista) { if ((Test-KTTweakAtiva -Tweak $t) -eq $true) { $ok++ } }
        $cor = if ($ok -eq $lista.Count) { 'Green' } elseif ($ok -gt 0) { 'Yellow' } else { 'DarkGray' }
        Write-Host ("    {0,-28} {1,2} de {2,2}" -f $aba.Titulo, $ok, $lista.Count) -ForegroundColor $cor
    }
    # Espaço limpável
    try {
        $r = Measure-KTLimpeza
        Write-Host ("   Espaço limpável: {0} MB" -f $r.TotalMB) -ForegroundColor Cyan
    } catch { }

    # Hardware detectado (para recomendacoes)
    $ssd = (Get-PhysicalDisk -ErrorAction SilentlyContinue | Where-Object { $_.MediaType -eq 'SSD' })
    if ($ssd) { Write-Host '   Armazenamento: SSD detectado (recomenda-se desligar SysMain)' -ForegroundColor Green }
    else       { Write-Host '   Armazenamento: sem SSD detectado via CIM (Mantenha SysMain ligado se usar HD)' -ForegroundColor Yellow }

    $bateria = Get-CimInstance Win32_Battery -ErrorAction SilentlyContinue
    if ($bateria) { Write-Host '   ATENCAO: notebook detectado - evite planos de energia de maximo desempenho' -ForegroundColor Yellow }

    Write-Host ('  ' + ('-' * 66)) -ForegroundColor DarkGray
    Write-Host ''
}
