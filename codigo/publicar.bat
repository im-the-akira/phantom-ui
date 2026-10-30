@echo off
rem Gera o PHANTOM UI pronto para levar a outro PC, a partir de codigo\.
rem Saida: ..\temp\dist\PhantomUI.exe + LEIA-ME.txt + LICENSE.txt
rem Sempre recompila do zero a partir do codigo atual.
cd /d "%~dp0"
echo Fechando o PhantomUI se estiver aberto ...
taskkill /f /im PhantomUI.exe >nul 2>&1
echo Gerando PhantomUI.exe a partir do codigo atual ...
rmdir /s /q obj\Release >nul 2>&1
dotnet publish PhantomUI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -o "..\temp\dist" -nologo -v q
if errorlevel 1 (
    echo.
    echo FALHOU. Veja os erros acima. O .exe antigo NAO foi atualizado.
    pause
    exit /b 1
)
copy /y "LEIA-ME.txt" "..\temp\dist\LEIA-ME.txt" >nul
copy /y "LICENSE" "..\temp\dist\LICENSE.txt" >nul
echo.
echo Pronto: %~dp0..\temp\dist
for %%F in ("..\temp\dist\PhantomUI.exe") do echo Arquivo gerado em %%~tF (confira se e de agora^)
pause
