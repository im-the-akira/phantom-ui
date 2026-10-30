#!/usr/bin/env bash
# Build de verificacao do PHANTOM UI fora do Windows (CI Linux e agente).
# Nao executa WPF, WMI, powercfg, bcdedit, pnputil, DISM nem oscdimg. So prova que o codigo fecha.
set -euo pipefail
cfg="${1:-Release}"
raiz="$(cd "$(dirname "$0")/.." && pwd)"
cd "$raiz"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
python3 "$raiz/scripts/verificar-codigo.py" || { echo "Verificacao estatica falhou."; exit 1; }
rm -rf obj bin
dotnet build PhantomUI.csproj -c "$cfg" \
  -p:EnableWindowsTargeting=true \
  -p:BaseIntermediateOutputPath=/tmp/ox_obj/ \
  -p:BaseOutputPath=/tmp/ox_bin/ \
  -o /tmp/ox_out/ -v q --nologo
echo "Build ok ($cfg): 0 erro."
