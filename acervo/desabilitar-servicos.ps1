# Lista de serviços geralmente não utilizados
$services = @(
    "Fax",                         # Fax
    "MapsBroker",                  # Mapas Offline
    "RetailDemo",                  # Modo Demonstração
    "WSearch",                     # Windows Search (indexação)
    "XblGameSave",                 # Xbox Live Game Save
    "XboxNetApiSvc",               # Xbox Networking
    "XboxGipSvc",                  # Xbox Accessory
    "DiagTrack",                   # Telemetria
    "dmwappushservice",            # Telemetria WAP
    "RemoteRegistry",              # Registro remoto
    "SharedAccess",                # Internet Connection Sharing
    "TrkWks",                      # Distributed Link Tracking
    "WerSvc",                      # Relatório de erros
    "WMPNetworkSvc",               # Windows Media Player Network
    "SysMain",                     # Superfetch
    "PhoneSvc",                    # Phone Service
    "SEMgrSvc"                     # Payments NFC
    "Spooler",     # Impress�o
    "lfsvc",       # Geolocaliza��o
    "wisvc",       # Windows Insider
    "TabletInputService" # Teclado touch
)

foreach ($service in $services) {
    $svc = Get-Service -Name $service -ErrorAction SilentlyContinue
    if ($svc) {
        Write-Host "Desabilitando: $service"
        Stop-Service -Name $service -Force -ErrorAction SilentlyContinue
        Set-Service -Name $service -StartupType Disabled
    } else {
        Write-Host "Serviço não encontrado: $service"
    }
}

Write-Host "Processo finalizado!"
