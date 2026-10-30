@echo off
sc stop "WerSvc"
sc config "WerSvc" start= disabled
echo Relatórios de Erros desativados!
pause
