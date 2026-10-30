# Coleta o ambiente da maquina de teste, sem dado pessoal.
# Uso: pwsh -File codigo\scripts\coletar-ambiente.ps1
$ErrorActionPreference = 'Continue'
$out = @()
$out += "PHANTOM UI - ambiente de teste"
$out += "Data: " + (Get-Date -Format 'yyyy-MM-dd HH:mm')
$os = Get-CimInstance Win32_OperatingSystem
$out += "Windows: $($os.Caption) build $($os.BuildNumber) $($os.OSArchitecture)"
$cs = Get-CimInstance Win32_ComputerSystem
$out += "Modelo: $($cs.Manufacturer) $($cs.Model) notebook=$($cs.PCSystemType -eq 2)"
$cpu = Get-CimInstance Win32_Processor | Select-Object -First 1
$out += "CPU: $($cpu.Name) nucleos=$($cpu.NumberOfCores) logicos=$($cpu.NumberOfLogicalProcessors)"
$ram = [math]::Round($cs.TotalPhysicalMemory / 1GB, 0)
$out += "RAM: $ram GB"
Get-CimInstance Win32_PhysicalMemory | ForEach-Object {
    $out += "  pente: $([math]::Round($_.Capacity/1GB,0)) GB $($_.Speed) MHz slot $($_.DeviceLocator)"
}
Get-CimInstance Win32_VideoController | ForEach-Object { $out += "GPU: $($_.Name) driver $($_.DriverVersion)" }
Get-CimInstance Win32_DiskDrive | ForEach-Object { $out += "Disco: $($_.Model) $([math]::Round($_.Size/1GB,0)) GB" }
$out += "BIOS: $((Get-CimInstance Win32_BIOS).SMBIOSBIOSVersion)"
$out += "SecureBoot: $([string](Confirm-SecureBootUEFI -ErrorAction SilentlyContinue))"
$out += "Virtualizacao no firmware: $((Get-CimInstance Win32_Processor).VirtualizationFirmwareEnabled)"
$out += "PowerShell: $($PSVersionTable.PSVersion) | pwsh presente: $([bool](Get-Command pwsh -ErrorAction SilentlyContinue))"
$out += "dotnet: $((dotnet --version) -join ' ')"
$out += "winget: $([bool](Get-Command winget -ErrorAction SilentlyContinue))"
$out += "oscdimg (ADK): $([bool](Get-Command oscdimg -ErrorAction SilentlyContinue))"
$out += "Elevado: $(([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))"
$out += "Anti-cheats: $((Get-Service -ErrorAction SilentlyContinue | Where-Object { $_.Name -match 'vgc|vgk|EasyAntiCheat|BEService|FACEIT' } | ForEach-Object { $_.Name }) -join ', ')"
$out -join [Environment]::NewLine
