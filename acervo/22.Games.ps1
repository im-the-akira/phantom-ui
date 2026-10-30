# =====================================================================
#  Kairo Tweaker :: 22.Games.ps1
#  ABA "JOGOS POR TITULO" - configuracao por jogo
#
#  Para cada titulo o app gera um arquivo de config (autoexec.cfg,
#  .ini, options.txt) com os comandos de latencia e video usados em
#  partidas competitivas. O arquivo NAO sobrescreve a sua config:
#  ele tem nome proprio (kairo.cfg) e o jogo so le se voce apontar.
# =====================================================================

$global:KT_Jogos = @(
    @{ Id='cs2';       Nome='Counter-Strike 2';    Pasta='Steam\steamapps\common\Counter-Strike Global Offence\game\csgo\cfg'; Arq='kairo.cfg' }
    @{ Id='gta5';      Nome='GTA V';               Pasta='Documents\Rockstar Games\GTA V';                          Arq='kairo.cfg' }
    @{ Id='fivem';     Nome='FiveM';               Pasta='AppData\Local\FiveM\FiveM.app\data\settings';             Arq='kairo.cfg' }
    @{ Id='valorant';  Nome='VALORANT';            Pasta='AppData\Local\VALORANT\Saved\Config';                       Arq='kairo.ini' }
    @{ Id='fortnite';  Nome='Fortnite';            Pasta='AppData\Local\FortniteGame\Saved\Config\WindowsClient';     Arq='kairo.ini' }
    @{ Id='warzone';   Nome='Warzone';             Pasta='AppData\Local\Activision\CoD\Warozone';                     Arq='kairo.cfg' }
    @{ Id='mw4';       Nome='CoD Modern Warfare';  Pasta='AppData\Local\Activision\CoD\Modern Warfare';              Arq='kairo.cfg' }
    @{ Id='bo7';       Nome='Black Ops 7';         Pasta='AppData\Local\Activision\CoD\Black Ops 7';                  Arq='kairo.cfg' }
    @{ Id='minecraft'; Nome='Minecraft Java';      Pasta='AppData\Roaming\.minecraft';                                Arq='kairo.txt' }
)

function Get-KTJogo { param([string]$Id) $global:KT_Jogos | Where-Object { $_.Id -eq $Id } }

function ConvertTo-KTLongPath {
    <#
      .SYNOPSIS  Devolve um caminho que o .NET consegue ler mesmo com mais
                 de 260 caracteres (limite MAX_PATH).
      .NOTES     Caminhos de jogo passam do limite com folga, ex:
                 "Counter-Strike Global Offence". O prefixo \\?\ e a forma
                 nativa do Windows de ignorar esse limite.
    #>
    param([string]$Path)
    if ($Path.Length -gt 240 -and $Path.IndexOf('\?') -lt 0) {
        return "\\?\$Path"
    }
    return $Path
}

function Get-KTJogoPath {
    param([Parameter(Mandatory)]$Jogo)
    $dir = Join-Path $env:USERPROFILE $Jogo.Pasta
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
    return (Join-Path $dir $Jogo.Arq)
}

# =====================================================================
#  CONFIGS POR JOGO
#  Valores de rede: interp/cl_interp menores = menos lag de interpolacao.
# =====================================================================

$global:KT_ConfigsJogos = @{}

$global:KT_ConfigsJogos['cs2'] = @{
    Titulo = 'Counter-Strike 2 - latencia minima'
    Aviso  = 'Valores de LAN competitivo. Ajuste fps_max ao seu monitor.'
    ComoUsar = 'Steam > CS2 > Propriedades > Opcoes de inicializacao: -exec "kairo.cfg"'
    Conteudo = @'
// Kairo Tweaker - CS2 (kairo.cfg)
// --- Rede: latencia minima ---
rate 786432
cl_interp 0
cl_updaterate 128
cl_cmdrate 128
cl_lag_compensation 1

// --- Fluidez (fps_max = Hz do seu monitor) ---
fps_max 400
fps_max_ui 120

// --- Desempenho ---
engine_no_focus_sleep 1
cl_forcepreload 1
mat_queue_mode 2
mat_vsync 0
cl_allow_taunt 0
cl_autohelp 0
'@
}

$global:KT_ConfigsJogos['gta5'] = @{
    Titulo = 'GTA V - desempenho'
    Aviso  = 'Afeta taxa de quadros e stream de assets. Se ja roda bem, ignore.'
    ComoUsar = 'Copie as linhas para %APPDATA%\Rockstar Games\GTA V\scripts.cfg'
    Conteudo = @'
; Kairo Tweaker - GTA V
set fxaa 0
set ignoreblur 1
set pauseonfocus 0
set streaming_session_foc 0
set dbgmenu 0
set ragdoll_time_multiplier 0
set ped_density_multiplier 0
set car_density_multiplier 0
'@
}

$global:KT_ConfigsJogos['fivem'] = @{
    Titulo = 'FiveM - streamed e conexao'
    Aviso  = 'Reduz travamento ao entrar em servidor. Nao altera nada do anticheat.'
    ComoUsar = 'Copie para AppData\Local\FiveM\FiveM.app\data\settings\ (ou para o servidor)'
    Conteudo = @'
; Kairo Tweaker - FiveM (client settings)
set onesync on
set txAdminRate 50
set minimumTimeBeforeJoin 10
set cinematic_transition 0
'@
}


$global:KT_ConfigsJogos['valorant'] = @{
    Titulo = 'VALORANT - video e FPS'
    Aviso  = 'RiotSettings nao aceita tweak de rede. Esta config afeta video e FPS.'
    ComoUsar = 'Copie para AppData\Local\VALORANT\Saved\Config\<build>\Windows\GameUser.ini'
    Conteudo = @'
[GameUserSettings]
bUseVSync=False
bUseDynamicResolution=False
FullscreenMode=2
PreferredFullscreenMode=2
bLimitFPS=True
FrameRateLimit=999.000000
AntiAliasing=0
sg.PostProcessAA=0
sg.ResolutionQuality=100.000000
sg.ViewDistanceQuality=100.000000
bLimitBackgroundFrameRate=False
'@
}

$global:KT_ConfigsJogos['fortnite'] = @{
    Titulo = 'Fortnite - video e FPS'
    Aviso  = 'Afeta desempenho grafico. Se rodar bem, ignore.'
    ComoUsar = 'Copie para AppData\Local\FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini'
    Conteudo = @'
[/Script/EngineSettings.GameMapsSettings]
GameDIP=Full

[/Script/Engine.Engine]
bSmoothFrameRate=True
bUseVSync=False
bUseDynamicResolution=False
ResolutionSizeX=1920
ResolutionSizeY=1080
LastConfirmedResolutionSizeX=1920
LastConfirmedResolutionSizeY=1080
FullscreenMode=0
PreferredFullscreenMode=0
ScreenPercentage=100.000000
sg.ViewDistanceQuality=100.000000
sg.AntiAliasingQuality=2
sg.ShadowQuality=2
sg.PostProcessQuality=1
sg.TextureQuality=0
sg.EffectsQuality=0
sg.FoliageQuality=0
sg.ShadingQuality=0
'@
}

# Warzone, MW4 e BO7 usam o mesmo motor (IW), entao a config e a mesma.
$conteudoCOD = @'
// Kairo Tweaker - Call of Duty (kairo.cfg)
// --- FPS ---
cl_maxFPS 300
cl_maxfpsNoFocus 0
cl_useSmoothing 0
// --- Texturas ---
r_texFilter 0
r_AnisotropicFilter 0
r_TextureDetail 0
r_TextureDetail_VectorMode 0
// --- Sombras ---
r_ShadowMapResolution 0
r_ShadowDetail 0
r_SpotDetail 0
// --- Pos-processamento ---
mat_motionBlur 0
mat_depthOfField 0
mat_picmip 0
fxaa_enable 0
r_LODDithering 0
// --- Nuvens e monitor ---
cl_cloudRate 8
r_preferedRefreshRate 1
'@

foreach ($id in @('warzone','mw4','bo7')) {
    $global:KT_ConfigsJogos[$id] = @{
        Titulo   = "Call of Duty ($id) - video"
        Aviso    = 'Mesmos comandos do motor COD. Afeta video e FPS.'
        ComoUsar = 'Copie para %APPDATA%\..\Local\Activision\CoD\_folder\kairo.cfg e adicione "exec kairo.cfg" no autoexec.cfg'
        Conteudo = $conteudoCOD
    }
}

$global:KT_ConfigsJogos['minecraft'] = @{
    Titulo = 'Minecraft Java - FPS'
    Aviso  = 'Afeta FPS e distancia de chunks. Muda o visual.'
    ComoUsar = 'Copie para %APPDATA%\.minecraft\options.txt'
    Conteudo = @"
# Kairo Tweaker - Minecraft Java (options.txt)
fps:260
maxFps:260
enableVsync:false
renderDistance:12
simulationDistance:12
graphics:fast
ao:false
particles:2
mipmapLevels:0
guiScale:2
entityDistanceScaling:75.0
entityShadows:false
fancy:false
pauseOnLostFocus:false
"@
}

# =====================================================================
#  APLICACAO E MENU
# =====================================================================

function Test-KTJogoInstalado {
    param([Parameter(Mandatory)][string]$Id)
    $siglas = switch ($Id) {
        'cs2'       { @('730') }
        'gta5'      { @('GTA5', 'Grand Theft Auto V') }
        'fivem'     { @('FiveM', 'FiveM_Launcher') }
        'valorant'  { @('Riot Vanguard', 'VALORANT') }
        'fortnite'  { @('EpicGamesLauncher', 'FortniteClient') }
        'warzone'   { @('Call of Duty HQ', 'CODWarzone') }
        'mw4'       { @('Call of Duty HQ') }
        'bo7'       { @('Call of Duty HQ', 'Black Ops') }
        'minecraft' { @('.minecraft', 'launcher_minecraft') }
        default     { @() }
    }
    foreach ($s in $siglas) {
        if (Test-Path "HKLM:\SOFTWARE\WOW6432Node\Uninstall\$s" -ErrorAction SilentlyContinue) { return $true }
        if (Test-Path "HKCU:\SOFTWARE\$s" -ErrorAction SilentlyContinue) { return $true }
    }
    return $false
}

function Set-KTConfigJogo {
    param([Parameter(Mandatory)][string]$Id, [switch]$Silencioso)
    $cfg = $global:KT_ConfigsJogos[$Id]
    if (-not $cfg) { Write-KTLog "Jogo desconhecido: $Id" 'ERRO'; return $false }
    $jogo = Get-KTJogo -Id $Id
    $arq  = Get-KTJogoPath -Jogo $jogo
    $arqC = ConvertTo-KTLongPath -Path $arq
    $existe = Test-Path $arqC
    Add-KTJournal -Entry @{ Tipo='ArquivoJogo'; Path=$arqC; Nome=$Id; Existia=$existe; TipoValor=''; Valor='' }
    try {
        [System.IO.File]::WriteAllText($arqC, $cfg.Conteudo)
    } catch {
        # fallback: se ainda assim falhar, tenta o caminho longo
        try { Set-Content -Path $arq -Value $cfg.Conteudo -Encoding ASCII; $arqC = $arq }
        catch { Write-KTLog "Nao foi possivel gravar a config: $($_.Exception.Message)" 'ERRO'; return $false }
    }
    if (-not $Silencioso) { Write-KTLog "Config criada: $arqC" 'OK' }
    return $true
}

function Show-KTJogoConfig {
    param([Parameter(Mandatory)][string]$Id)
    $cfg = $global:KT_ConfigsJogos[$Id]
    if (-not $cfg) { return }
    Write-Host ''
    Write-Host ('   ' + ('=' * 64)) -ForegroundColor DarkCyan
    Write-Host "   $($cfg.Titulo)" -ForegroundColor Cyan
    Write-Host ('   ' + ('=' * 64)) -ForegroundColor DarkCyan
    Write-Host ''
    Write-Host '   AVISO:' -ForegroundColor Yellow
    Write-Host "     $($cfg.Aviso)" -ForegroundColor Gray
    Write-Host ''
    Write-Host '   COMO USAR:' -ForegroundColor Yellow
    Write-Host "     $($cfg.ComoUsar)" -ForegroundColor Gray
    Write-Host ''
    Write-Host '   CONTEUDO:' -ForegroundColor Yellow
    foreach ($l in ($cfg.Conteudo -split "`n")) { Write-Host "     $($l.TrimEnd())" -ForegroundColor DarkGray }
    Write-Host ''
}

function Show-KTJogos {
    while ($true) {
        Show-KTTitulo
        Write-Host '   JOGOS POR TITULO - CONFIG DE LATENCIA E VIDEO' -ForegroundColor Magenta
        Write-Host ''
        $i = 0
        foreach ($j in $global:KT_Jogos) {
            $i++
            $inst = if (Test-KTJogoInstalado -Id $j.Id) { 'instalado' } else { 'nao detectado' }
            $cor = if ($inst -eq 'instalado') { 'Green' } else { 'DarkGray' }
            Write-Host ("   {0,2}. {1,-24}" -f $i, $j.Nome) -ForegroundColor White -NoNewline
            Write-Host ("  {0}" -f $inst) -ForegroundColor $cor
        }
        Write-Host ''
        Write-Host '   numero = ver config  |  G = gerar arquivo  |  0 = voltar' -ForegroundColor DarkGray
        $op = Read-Host '   Escolha'
        if ($op -eq '0' -or $op -eq '') { return }
        if ($op -match '^\d+$') {
            $n = [int]$op
            if ($n -ge 1 -and $n -le $global:KT_Jogos.Count) { Show-KTJogoConfig -Id $global:KT_Jogos[$n-1].Id }
        } elseif ($op -match '^[Gg]$') {
            $n = Read-Host '   Numero do jogo'
            if ($n -match '^\d+$' -and [int]$n -ge 1 -and [int]$n -le $global:KT_Jogos.Count) {
                $id = $global:KT_Jogos[[int]$n-1].Id
                $global:KT_BackupDir = New-KTBackup -Motivo "config-jogo-$id"
                $null = Set-KTConfigJogo -Id $id
            }
        }
        Write-Host ''
        [void](Read-Host '   ENTER para continuar')
    }
}
