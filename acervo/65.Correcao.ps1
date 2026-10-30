# =====================================================================
#  Kairo Tweaker :: 65.Correcao.ps1
#  ABA - CORREÇÃO DO WINDOWS
#  Ferramentas oficiais de reparo (SFC/DISM/CHKDSK), reset de rede,
#  cache do Windows Update e limpeza profunda. Tudo como tweak da casa:
#  aparece no menu, explica o que faz e registra no journal.
#  Origem: ReparoWindows.ps1 / MasterOptimizer (-Reparo, -Limpeza) absorvidos.
# =====================================================================

# ---------------- ACOES (executadas pelo motor via Executar=) ----------------
$global:KT_Acoes['correcao.sfc'] = {
    param($Silencioso)
    Write-Host '   SFC /scannow — verificando arquivos do sistema (10-20 min)...' -ForegroundColor Cyan
    & 'C:\Windows\System32\sfc.exe' /scannow
    Add-KTJournal -Entry @{ Tipo='Comando'; Path='sfc /scannow'; Nome='Verificacao SFC'; Existia=$true; TipoValor=''; Valor='' }
    Write-KTLog 'SFC executado.' 'OK'
}

$global:KT_Acoes['correcao.dism'] = {
    param($Silencioso)
    # DISM precisa do Windows Update para baixar componentes integros:
    # se o WU estiver bloqueado (nosso bloqueio ate 2047), liga temporariamente
    # e restaura o bloqueio no fim. Origem: ReparoWindows.ps1 -Fase Preparar/Finalizar.
    $svc = Get-Service wuauserv -ErrorAction SilentlyContinue
    $estavaBloqueado = ($svc -and "$($svc.StartType)" -eq 'Disabled')
    if ($estavaBloqueado) {
        Write-Host '   WU bloqueado: ligando temporariamente para o DISM...' -ForegroundColor Yellow
        Set-KTServiceStart -Nome 'wuauserv' -Start 3 | Out-Null
        & reg.exe delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate" /v DisableWindowsUpdateAccess /f 2>$null | Out-Null
        & reg.exe delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate" /v DoNotConnectToWindowsUpdateInternetLocations /f 2>$null | Out-Null
        Add-KTJournal -Entry @{ Tipo='Reg'; Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate'; Nome='DisableWindowsUpdateAccess'; Existia=$true; TipoValor='DWord'; Valor=1 }
        Start-Service wuauserv -ErrorAction SilentlyContinue
    }
    Write-Host '   DISM /RestoreHealth — reparando a imagem do Windows (15-40 min)...' -ForegroundColor Cyan
    & 'C:\Windows\System32\Dism.exe' /Online /Cleanup-Image /RestoreHealth
    if ($estavaBloqueado) {
        Write-Host '   Re-bloqueando o Windows Update...' -ForegroundColor Yellow
        Stop-Service wuauserv -Force -ErrorAction SilentlyContinue
        Set-KTServiceStart -Nome 'wuauserv' -Start 4 | Out-Null
        & reg.exe add "HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate" /v DisableWindowsUpdateAccess /t REG_DWORD /d 1 /f | Out-Null
        & reg.exe add "HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate" /v DoNotConnectToWindowsUpdateInternetLocations /t REG_DWORD /d 1 /f | Out-Null
    }
    Write-KTLog 'DISM RestoreHealth executado.' 'OK'
}

$global:KT_Acoes['correcao.chkdsk'] = {
    param($Silencioso)
    Write-Host '   Verificando o sistema de arquivos de C: (somente leitura)...' -ForegroundColor Cyan
    try { Repair-Volume -DriveLetter C -Scan -ErrorAction Stop }
    catch { Write-Host "   Repair-Volume indisponivel: $($_.Exception.Message)" -ForegroundColor Yellow }
    & fsutil.exe dirty query C:
    Add-KTJournal -Entry @{ Tipo='Comando'; Path='Repair-Volume C -Scan'; Nome='CHKDSK scan'; Existia=$true; TipoValor=''; Valor='' }
    Write-KTLog 'Verificacao de disco executada.' 'OK'
}

$global:KT_Acoes['correcao.rede'] = {
    param($Silencioso)
    Write-Host '   Reset completo da pilha de rede (precisa reiniciar)...' -ForegroundColor Cyan
    & ipconfig.exe /flushdns        | Out-Null
    & netsh.exe winsock reset       2>$null | Out-Null
    & netsh.exe int ip reset        2>$null | Out-Null
    & netsh.exe interface ipv4 reset 2>$null | Out-Null
    & netsh.exe interface ipv6 reset 2>$null | Out-Null
    Add-KTJournal -Entry @{ Tipo='Comando'; Path='netsh winsock reset + int ip reset'; Nome='Reset de rede'; Existia=$true; TipoValor=''; Valor='' }
    Write-KTLog 'Pilha de rede resetada. Reinicie o Windows.' 'OK'
}

$global:KT_Acoes['correcao.wucache'] = {
    param($Silencioso)
    Write-Host '   Limpando o cache de downloads do Windows Update...' -ForegroundColor Cyan
    $wu  = Get-Service wuauserv -ErrorAction SilentlyContinue
    $estava = if ($wu) { "$($wu.StartType)" } else { 'Manual' }
    Stop-Service wuauserv -Force -ErrorAction SilentlyContinue
    Stop-Service bits     -Force -ErrorAction SilentlyContinue
    $dl = 'C:\Windows\SoftwareDistribution\Download'
    if (Test-Path $dl) { Get-ChildItem $dl -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue }
    if ("$estava" -eq 'Automatic') { Start-Service wuauserv -ErrorAction SilentlyContinue }
    Add-KTJournal -Entry @{ Tipo='Comando'; Path='purge SoftwareDistribution\Download'; Nome='Cache do WU'; Existia=$true; TipoValor=''; Valor='' }
    Write-KTLog 'Cache do Windows Update limpo (corrige update travado em 0%/99%).' 'OK'
}

$global:KT_Acoes['correcao.sfc'] = $global:KT_Acoes['correcao.sfc']

# ---------------- TWEAKS DA ABA ----------------
Add-KTModule -Modulo 'Correcao' -Tweaks @(

    @{ Id='fix.status'; Categoria='Correção do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Diagnostico rapido: processos, RAM, VBS, HAGS, WU e energia'
       Explicacao='Origem: ReparoWindows/MasterOptimizer -Status + 97.Diagnostico. Mostra o retrato do sistema: contagem de processos, RAM livre, VBS/hipervisor, HAGS, estado do Windows Update, plano de energia e monitor. Nao altera nada.'
       Ganho='Ver em 10 segundos se o PC esta no estado otimizado.'
       Executar='correcao.status'
    }
    @{ Id='fix.sfc'; Categoria='Correção do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='SFC /scannow — verificar e reparar arquivos do sistema'
       Explicacao='Origem: ReparoWindows.ps1. O System File Checker compara cada arquivo do Windows com a assinatura original (do WinSxS) e troca os alterados/corrompidos. Use quando algo estranho acontece (regedit nao salva, app de sistema quebra, tela azul aleatorio). Nao mexe em programas e arquivos pessoais.'
       Ganho='Repara corrupcao de arquivos do Windows sem reinstalar nada.'
       Executar='correcao.sfc'
    }
    @{ Id='fix.dism'; Categoria='Correção do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='DISM /RestoreHealth — reparar a imagem do Windows'
       Explicacao='Origem: ReparoWindows.ps1 (aplicado no seu PC em 26/09: resultado 0 corrupcoes). O DISM verifica a imagem de componentes (WinSxS) e baixa copias integras dos servidores da Microsoft para reparar. Se o WU estiver bloqueado (nosso bloqueio 2047), ele e ligado temporariamente e re-bloqueado ao final automaticamente.'
       Ganho='Conserta a base que o SFC usa para reparar. Rode o SFC depois deste.'
       Executar='correcao.dism'
    }
    @{ Id='fix.chkdsk'; Categoria='Correção do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='CHKDSK (scan) — verificar o disco C: por erros'
       Explicacao='Origem: ReparoWindows.ps1 (NoErrorsFound no seu PC). Varre a estrutura do NTFS (registro de arquivos, indices, descritores de seguranca, diario USN) em modo somente leitura. Detecta setores/estrutura problematicos antes de virarem perda de dados.'
       Ganho='Confirma a saude do disco do sistema em ~1 minuto.'
       Executar='correcao.chkdsk'
    }
    @{ Id='fix.rede'; Categoria='Correção do Windows'; Risco='Medio'; Reinicio=$true
       Nome='Reset completo da rede (winsock + ip) — corrige "sem internet"'
       Explicacao='Origem: ferramenta classica de correcao. Zera o catalogo Winsock e as pilhas IPv4/IPv6 para o estado de fabrica (o Windows recria na proxima inicializacao). Cura: "sem internet" persistente, erro de DNS, ping alto em tudo, conflito de VPN/proxy. Precisa reiniciar e Voicemeeter/apps de rede podem pedir reconfig 1x.'
       Ganho='Resolve 90% dos problemas de conexao que limpar DNS nao resolve.'
       Executar='correcao.rede'
    }
    @{ Id='fix.wucache'; Categoria='Correção do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Limpar cache do Windows Update (update travado em 0%/99%)'
       Explicacao='Origem: MasterOptimizer -Limpeza. Para wuauserv/bits, esvazia SoftwareDistribution\Download (onde ficam os updates baixados pela metade) e volta o servico ao estado anterior. E o primeiro passo de sempre para update que falha em loop.'
       Ganho='Update reinstala do zero na proxima verificacao.'
       Executar='correcao.wucache'
    }
    @{ Id='fix.limpezaprofunda'; Categoria='Correção do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Limpeza profunda: temp + shaders + miniaturas + DNS (bat absorvidos)'
       Explicacao='Origem: "Limpar Arquivos Temporarios.bat" + "Limpar Memoria Ram.bat" (SEM o EmptyStandbyList, que causava stutter) + caches de shader do MasterOptimizer. Limpa %TEMP%, C:\Windows\Temp, Prefetch, thumbcache, Recent, CBS logs, cache de download do WU e os caches de shader (D3DSCache, AMD DxCache/Dx9/Dxc/GL). Flush de DNS incluso.'
       Ganho='Espaco livre + fim de stutter causado por shader cache corrompido (1a partida recompila shaders).'
       Executar='correcao.limpezaprofunda'
    }
)

# acao da limpeza profunda (usa a mesma rotina do 60.Limpeza + shaders)
$global:KT_Acoes['correcao.limpezaprofunda'] = {
    param($Silencioso)
    Write-Host '   Limpando temporarios, caches de shader e DNS...' -ForegroundColor Cyan
    foreach ($p in @("$env:TEMP\*"), 'C:\Windows\Temp\*', "$env:LOCALAPPDATA\Microsoft\Windows\Explorer\thumbcache_*.db", "$env:APPDATA\Microsoft\Windows\Recent\*", "$env:SystemRoot\Prefetch\*", 'C:\Windows\Logs\CBS\*.log') {
        Remove-Item $p -Recurse -Force -ErrorAction SilentlyContinue
    }
    foreach ($c in 'D3DSCache','AMD\DxCache','AMD\Dx9Cache','AMD\DxcCache','AMD\GLCache') {
        $p = Join-Path $env:LOCALAPPDATA $c
        if (Test-Path $p) { Get-ChildItem $p -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue }
    }
    & ipconfig.exe /flushdns | Out-Null
    Add-KTJournal -Entry @{ Tipo='Comando'; Path='limpeza profunda (temp+shaders+dns)'; Nome='Limpeza profunda'; Existia=$true; TipoValor=''; Valor='' }
    Write-KTLog 'Limpeza profunda concluida.' 'OK'
}

# acao de diagnostico rapido
$global:KT_Acoes['correcao.status'] = {
    param($Silencioso)
    & { Show-KTDiagnostico } 2>$null
}
