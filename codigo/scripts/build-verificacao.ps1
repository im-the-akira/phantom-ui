# Build de verificacao do PHANTOM UI no Windows.
# Uso: pwsh -File codigo\scripts\build-verificacao.ps1 [-Release]
param([switch]$Release)
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent (Split-Path -Parent $PSCommandPath)
Push-Location $raiz
try {
    $cfg = if ($Release) { 'Release' } else { 'Debug' }
    Write-Host "PHANTOM UI - build de verificacao ($cfg)"
    Write-Host "Raiz: $raiz"
    foreach ($p in 'obj', 'bin') { if (Test-Path $p) { Remove-Item $p -Recurse -Force } }
    python scripts\verificar-codigo.py
    if ($LASTEXITCODE -ne 0) { throw "Verificação estática falhou." }
    dotnet build PhantomUI.csproj -c $cfg -v q --nologo
    if ($LASTEXITCODE -ne 0) { throw "Build falhou. O resultado acima e a evidencia." }
    Write-Host "Build ok: 0 erro."
}
finally { Pop-Location }
