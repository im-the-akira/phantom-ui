# =====================================================================
#  Kairo Tweaker :: 23.JogosNovos.ps1
#  ABA "JOGOS POR TITULO" - titulos adicionais pedidos pelo dono do PC.
#  Gera config/portable no mesmo formato do 22.Games (kairo.cfg) e adiciona
#  ajustes de sistema especificos por jogo (ProcessPriority, GPU pref).
# =====================================================================

$global:KT_Jogos += @(
    @{ Id='thefinals';  Nome='The Finals' }
    @{ Id='overwatch2'; Nome='Overwatch 2' }
    @{ Id='apex';       Nome='Apex Legends' }
    @{ Id='bf6';        Nome='Battlefield 6' }
    @{ Id='bf2042';     Nome='Battlefield 2042' }
    @{ Id='bfredsec';   Nome='Battlefield REDSEC' }
    @{ Id='arcriders';  Nome='ARC Raiders' }
    @{ Id='wardogs';    Nome='War Dogs' }
    @{ Id='arenabreakout'; Nome='Arena Breakout' }
    @{ Id='pubg';       Nome='PUBG' }
    @{ Id='tarkov';     Nome='Escape from Tarkov' }
    @{ Id='deltaforce'; Nome='Delta Force' }
    @{ Id='dayz';       Nome='DayZ' }
    @{ Id='marvelrivals'; Nome='Marvel Rivals' }
)

# Configs por jogo (mesmo esquema do 22.Games: arquivo kairo.cfg salvo na pasta do jogo)
$cfgComum = @'
// KAIRO OPTIMIZER - config de latencia (nao sobrescreve sua config: e um arquivo separado)
// Use via argumentos de inicializacao do jogo quando suportado: +exec kairo.cfg
'@

$global:KT_ConfigsJogos['thefinals'] = @{ Titulo='The Finals - latencia'; Aviso='UE5: ative FSR3/DLSS quality'; ComoUsar='Ajuste no menu do jogo'; Conteudo=($cfgComum + "`n// The Finals: prio GPU dedicada`n") }
$global:KT_ConfigsJogos['overwatch2'] = @{ Titulo='Overwatch 2 - latencia'; Aviso='Use o anti-cheat padrao; sem alterar processos'; ComoUsar='Menu: Video > reduce buffering ON'; Conteudo=($cfgComum + "`n// OW2: reduce buffering, max framerate = monitor`n") }
$global:KT_ConfigsJogos['apex'] = @{ Titulo='Apex Legends - latencia'; Aviso='EAC: nao usar injecao'; ComoUsar='Steam: +exec kairo.cfg ou pasta cfg'; Conteudo=($cfgComum + "`ncl_smooth 0`ncl_showpos 0`nmat_queue_mode 2`nfps_max 0`n") }
$global:KT_ConfigsJogos['bf6'] = @{ Titulo='Battlefield 6 - latencia'; Aviso='JCB EAC: ajuste grafico Medio p/ 75Hz'; ComoUsar='Menu in-game'; Conteudo=($cfgComum + "`n// BF6: future frame rendering OFF (input lag menor)`n") }
$global:KT_ConfigsJogos['bf2042'] = @{ Titulo='Battlefield 2042 - latencia'; Aviso='Idem BF6'; ComoUsar='Menu in-game'; Conteudo=$cfgComum }
$global:KT_ConfigsJogos['bfredsec'] = @{ Titulo='Battlefield REDSEC - latencia'; Aviso='Modo BR do BF; idem BF6'; ComoUsar='Menu in-game'; Conteudo=$cfgComum }
$global:KT_ConfigsJogos['arcriders'] = @{ Titulo='ARC Raiders - latencia'; Aviso='UE5: FSR/XeSS'; ComoUsar='Menu in-game'; Conteudo=$cfgComum }
$global:KT_ConfigsJogos['wardogs'] = @{ Titulo='War Dogs - latencia'; Aviso=''; ComoUsar='Menu in-game'; Conteudo=$cfgComum }
$global:KT_ConfigsJogos['arenabreakout'] = @{ Titulo='Arena Breakout - latencia'; Aviso='ACE anticheat: SGuard roda junto (picos de CPU normais)'; ComoUsar='Menu in-game'; Conteudo=$cfgComum }
$global:KT_ConfigsJogos['pubg'] = @{ Titulo='PUBG - latencia'; Aviso='antlatency command disponivel no console'; ComoUsar='Steam launch: +antlatency'; Conteudo=($cfgComum + "`n// PUBG: -USEALLAVAILABLECORES -malloc=system`n") }
$global:KT_ConfigsJogos['tarkov'] = @{ Titulo='Escape from Tarkov - latencia'; Aviso='BSG launcher: use -force-d3d11 se stutter'; ComoUsar='Launcher > Parametros'; Conteudo=($cfgComum + "`n// EFK: -force-d3d11 -noSwiftShader`n") }
$global:KT_ConfigsJogos['deltaforce'] = @{ Titulo='Delta Force - latencia'; Aviso='SGuard anticheat em tempo real (picos 10-20% CPU normais)'; ComoUsar='GPU pref ja aplicado (GpuPreference=0)'; Conteudo=$cfgComum }
$global:KT_ConfigsJogos['dayz'] = @{ Titulo='DayZ - latencia'; Aviso='Enfusion: stutter de streaming normal'; ComoUsar='Steam: -noPause -noBenchmark'; Conteudo=($cfgComum + "`n// DayZ: -cpuCount=16 -exThreads=7`n") }
$global:KT_ConfigsJogos['marvelrivals'] = @{ Titulo='Marvel Rivals - latencia'; Aviso='UE5: upscaling quality'; ComoUsar='Menu in-game'; Conteudo=$cfgComum }

# Tweak de GPU dedicada por executavel conhecido (como feito no Delta Force real)
Add-KTModule -Modulo 'JogosNovos' -Tweaks @(

    @{ Id='jogo.gpu.apex'; Categoria='Jogos por Título'; Risco='Baixo'; Reinicio=$false
       Nome='Apex Legends: GPU dedicada + flip model'
       Explicacao='Registra r5apex.exe/apex do Steam em UserGpuPreferences (GpuPreference=0 + SwapEffectUpgradeEnable=1).'
       Problema='Nenhum.'
       Comandos=@(
           @{ Cmd='reg add "HKCU\Software\Microsoft\DirectX\UserGpuPreferences" /v "B:\Program Files (x86)\Steam\steamapps\common\Apex Legends\r5apex.exe" /t REG_SZ /d "GpuPreference=0;SwapEffectUpgradeEnable=1;" /f'; Rev='reg delete "HKCU\Software\Microsoft\DirectX\UserGpuPreferences" /v "B:\Program Files (x86)\Steam\steamapps\common\Apex Legends\r5apex.exe" /f' }
       )
    }
    @{ Id='jogo.gpu.pubg'; Categoria='Jogos por Título'; Risco='Baixo'; Reinicio=$false
       Nome='PUBG: GPU dedicada + flip model'
       Explicacao='Registra TsIgPUBG.exe/PUBG no UserGpuPreferences.'
       Problema='Nenhum.'
       Comandos=@(
           @{ Cmd='reg add "HKCU\Software\Microsoft\DirectX\UserGpuPreferences" /v "B:\Program Files (x86)\Steam\steamapps\common\PUBG\TslGame\Binaries\Win64\TsIgPUBG.exe" /t REG_SZ /d "GpuPreference=0;SwapEffectUpgradeEnable=1;" /f'; Rev='reg delete "HKCU\Software\Microsoft\DirectX\UserGpuPreferences" /v "B:\Program Files (x86)\Steam\steamapps\common\PUBG\TslGame\Binaries\Win64\TsIgPUBG.exe" /f' }
       )
    }
    @{ Id='jogo.gpu.tarkov'; Categoria='Jogos por Título'; Risco='Baixo'; Reinicio=$false
       Nome='Tarkov: GPU dedicada + flip model'
       Explicacao='Registra EscapeFromTarkov.exe no UserGpuPreferences.'
       Problema='Nenhum.'
       Comandos=@(
           @{ Cmd='reg add "HKCU\Software\Microsoft\DirectX\UserGpuPreferences" /v "C:\Battlestate Games\BSG Launcher\EscapeFromTarkov.exe" /t REG_SZ /d "GpuPreference=0;SwapEffectUpgradeEnable=1;" /f'; Rev='reg delete "HKCU\Software\Microsoft\DirectX\UserGpuPreferences" /v "C:\Battlestate Games\BSG Launcher\EscapeFromTarkov.exe" /f' }
       )
    }
    @{ Id='jogo.gpu.marvel'; Categoria='Jogos por Título'; Risco='Baixo'; Reinicio=$false
       Nome='Marvel Rivals: GPU dedicada + flip model'
       Explicacao='Registra MarvelGame.exe no UserGpuPreferences (pasta Steam padrao).'
       Problema='Nenhum.'
       Comandos=@(
           @{ Cmd='reg add "HKCU\Software\Microsoft\DirectX\UserGpuPreferences" /v "B:\Program Files (x86)\Steam\steamapps\common\Marvel Rivals\MarvelGame\Binaries\Win64\MarvelGame.exe" /t REG_SZ /d "GpuPreference=0;SwapEffectUpgradeEnable=1;" /f'; Rev='reg delete "HKCU\Software\Microsoft\DirectX\UserGpuPreferences" /v "B:\Program Files (x86)\Steam\steamapps\common\Marvel Rivals\MarvelGame\Binaries\Win64\MarvelGame.exe" /f' }
       )
    }
    @{ Id='jogo.gpu.dayz'; Categoria='Jogos por Título'; Risco='Baixo'; Reinicio=$false
       Nome='DayZ: GPU dedicada + flip model'
       Explicacao='Registra DayZ_x64.exe no UserGpuPreferences.'
       Problema='Nenhum.'
       Comandos=@(
           @{ Cmd='reg add "HKCU\Software\Microsoft\DirectX\UserGpuPreferences" /v "B:\Program Files (x86)\Steam\steamapps\common\DayZ\DayZ_x64.exe" /t REG_SZ /d "GpuPreference=0;SwapEffectUpgradeEnable=1;" /f'; Rev='reg delete "HKCU\Software\Microsoft\DirectX\UserGpuPreferences" /v "B:\Program Files (x86)\Steam\steamapps\common\DayZ\DayZ_x64.exe" /f' }
       )
    }
    @{ Id='jogo.gpu.arena'; Categoria='Jogos por Título'; Risco='Baixo'; Reinicio=$false
       Nome='Arena Breakout: GPU dedicada + flip model'
       Explicacao='Registra o executavel do Arena Breakout Infinite (Steam).'
       Problema='Nenhum.'
       Comandos=@(
           @{ Cmd='reg add "HKCU\Software\Microsoft\DirectX\UserGpuPreferences" /v "B:\Program Files (x86)\Steam\steamapps\common\Arena Breakout Infinite\Binaries\Win64\ArenaBreakoutInfinite.exe" /t REG_SZ /d "GpuPreference=0;SwapEffectUpgradeEnable=1;" /f'; Rev='reg delete "HKCU\Software\Microsoft\DirectX\UserGpuPreferences" /v "B:\Program Files (x86)\Steam\steamapps\common\Arena Breakout Infinite\Binaries\Win64\ArenaBreakoutInfinite.exe" /f' }
       )
    }
    @{ Id='jogo.cfg.gerar'; Categoria='Jogos por Título'; Risco='Baixo'; Reinicio=$false
       Nome='Gerar kairo.cfg para TODOS os 23 jogos (na pasta de cada)'
       Explicacao='Origem: 22.Games.ps1 ampliado. Gera o arquivo kairo.cfg/ini na pasta de cada jogo com comandos de latencia. Nao sobrescreve config nenhuma: arquivo separado que o jogo so le se apontado nos argumentos de inicializacao.'
       Problema='Nenhum (arquivo extra na pasta do jogo, ignorado se nao referenciado).'
       Executar='jogos.gerartodos'
    }
)

$global:KT_Acoes['jogos.gerartodos'] = {
    param($Silencioso)
    $feitos = 0
    foreach ($id in $global:KT_ConfigsJogos.Keys) {
        $cfg = $global:KT_ConfigsJogos[$id]
        try {
            $dir = switch ($id) {
                'thefinals'  { "$env:USERPROFILE\Documents\The Finals" }
                'overwatch2' { "$env:USERPROFILE\Documents\Overwatch" }
                'apex'       { 'B:\Program Files (x86)\Steam\steamapps\common\Apex Legends\cfg' }
                'bf6'        { "$env:USERPROFILE\Documents\Battlefield 6" }
                'bf2042'     { "$env:USERPROFILE\Documents\Battlefield 2042" }
                'bfredsec'   { "$env:USERPROFILE\Documents\Battlefield REDSEC" }
                'arcriders'  { "$env:USERPROFILE\Documents\ARC Raiders" }
                'wardogs'    { "$env:USERPROFILE\Documents\War Dogs" }
                'arenabreakout' { "$env:USERPROFILE\Documents\Arena Breakout" }
                'pubg'       { 'B:\Program Files (x86)\Steam\steamapps\common\PUBG\TslGame\SaveConfig' }
                'tarkov'     { "$env:USERPROFILE\Documents\Escape from Tarkov" }
                'deltaforce' { 'B:\Program Files (x86)\Steam\steamapps\common\Delta Force\Game\DeltaForce\Saved\Config' }
                'dayz'       { 'B:\Program Files (x86)\Steam\steamapps\common\DayZ\cfg' }
                'marvelrivals' { "$env:USERPROFILE\Documents\Marvel Rivals" }
                default      { "$env:USERPROFILE\Documents\KairoOptimizer" }
            }
            if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
            $arq = Join-Path $dir 'kairo.cfg'
            Set-Content -Path $arq -Value $cfg.Conteudo -Encoding UTF8
            $feitos++
            Add-KTJournal -Entry @{ Tipo='ArquivoJogo'; Path=$arq; Nome=$id; Existia=(Test-Path $arq); TipoValor=''; Valor=$cfg.Titulo }
        } catch { }
    }
    Write-Host ("  kairo.cfg gerado em " + $feitos + " pastas de jogos.") -ForegroundColor Green
    Write-Host "  (pastas inexistentes foram criadas; jogos que nao existem ainda terao a pasta pronta)" -ForegroundColor DarkGray
}
