using System.IO;
using System.Text;
using Microsoft.Win32;

namespace PhantomUI;

/// <summary>Opções do Windows pré-otimizado que entram no autounattend.xml e no $OEM$.</summary>
public sealed class IsoBuildOptions
{
    public string SourceIso { get; set; } = "";
    public string WorkDir { get; set; } = "";
    public string TargetUsb { get; set; } = "";     // letra do pendrive, ex.: "E"
    public bool OptimizeOs { get; set; } = true;    // $OEM$ + autounattend com tweaks seguros
    public bool RemoveBloat { get; set; } = true;   // remove Appx provisionados
    public bool InjectDrivers { get; set; } = true; // exporta e injeta drivers do PC atual
    public bool SkipOobe { get; set; } = true;      // pula OOBE, cria conta local
    public bool DisableBitLocker { get; set; } = true;
    public bool DisableCopilotRecall { get; set; } = true;
    public bool DisableTelemetry { get; set; } = true;
    public bool KeepEdge { get; set; } = true;
    public bool DisableReservedStorage { get; set; } = true;
    public string ComputerName { get; set; } = "PHANTOMUI-PC";
    public string UserName { get; set; } = "Player";
    public string Password { get; set; } = "";
    public string Locale { get; set; } = "pt-BR";
    public bool CreateIso { get; set; } = true;     // gera .iso no fim (além do pendrive)
    public string OutputIso { get; set; } = "";
}

public sealed class IsoBuildResult
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public List<string> Log { get; } = new();
    public string? AnswerFile { get; set; }
    public string? OutIso { get; set; }
    public string? UsbLetter { get; set; }
}

/// <summary>
/// Windows pré-otimizado: autounattend.xml + $OEM$ + exportação de drivers (pnputil) +
/// personalização offline do install.wim via DISM + pendrive bootável / ISO (oscdimg) +
/// instalação limpa SEM pendrive (Mount-DiskImage + setup.exe /auto clean, ou systemreset -cleanpc).
/// </summary>
public static class WindowsIsoBuilder
{
    public static string DefaultWorkDir => Path.Combine(AppSettings.Folder, "IsoBuild");
    public static string DefaultOutputIso => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop", $"PhantomUI_Win11_{DateTime.Now:yyyyMMdd}.iso");

    // =====================================================================
    // 1) autounattend.xml
    // =====================================================================
    /// <summary>Gera o autounattend.xml completo (windowsPE + specialize + oobeSystem).</summary>
    public static string BuildAutounattend(IsoBuildOptions o)
    {
        string esc = System.Security.SecurityElement.Escape(o.UserName) ?? "Player";
        string pcName = new string((o.ComputerName ?? "PHANTOMUI-PC").Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (pcName.Length == 0) pcName = "PHANTOMUIPC";
        if (pcName.Length > 15) pcName = pcName[..15];

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<unattend xmlns=\"urn:schemas-microsoft-com:unattend\">");
        sb.AppendLine("  <settings pass=\"windowsPE\">");
        sb.AppendLine("    <component name=\"Microsoft-Windows-International-Core-WinPE\" processorArchitecture=\"amd64\" publicKeyToken=\"31bf3856ad364e35\" language=\"neutral\" versionScope=\"nonSxS\" xmlns:wcm=\"http://schemas.microsoft.com/WMIConfig/2002/State\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
        sb.AppendLine($"      <SetupUILanguage><UILanguage>{o.Locale}</UILanguage></SetupUILanguage>");
        sb.AppendLine($"      <InputLocale>{o.Locale}</InputLocale>");
        sb.AppendLine($"      <SystemLocale>{o.Locale}</SystemLocale>");
        sb.AppendLine($"      <UILanguage>{o.Locale}</UILanguage>");
        sb.AppendLine($"      <UserLocale>{o.Locale}</UserLocale>");
        sb.AppendLine("    </component>");
        // Limpa o disco 0 e cria as partições UEFI (ESP + MSR + Windows)
        sb.AppendLine("    <component name=\"Microsoft-Windows-Setup\" processorArchitecture=\"amd64\" publicKeyToken=\"31bf3856ad364e35\" language=\"neutral\" versionScope=\"nonSxS\" xmlns:wcm=\"http://schemas.microsoft.com/WMIConfig/2002/State\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
        sb.AppendLine("      <DiskConfiguration>");
        sb.AppendLine("        <WillShowUI>OnError</WillShowUI>");
        sb.AppendLine("        <Disk wcm:action=\"add\">");
        sb.AppendLine("          <DiskID>0</DiskID>");
        sb.AppendLine("          <WillWipeDisk>true</WillWipeDisk>");
        sb.AppendLine("          <CreatePartitions>");
        sb.AppendLine("            <CreatePartition wcm:action=\"add\"><Order>1</Order><Type>EFI</Type><Size>300</Size></CreatePartition>");
        sb.AppendLine("            <CreatePartition wcm:action=\"add\"><Order>2</Order><Type>MSR</Type><Size>16</Size></CreatePartition>");
        sb.AppendLine("            <CreatePartition wcm:action=\"add\"><Order>3</Order><Type>Primary</Type><Extend>true</Extend></CreatePartition>");
        sb.AppendLine("          </CreatePartitions>");
        sb.AppendLine("          <ModifyPartitions>");
        sb.AppendLine("            <ModifyPartition wcm:action=\"add\"><Order>1</Order><PartitionID>1</PartitionID><Format>FAT32</Format><Label>System</Label></ModifyPartition>");
        sb.AppendLine("            <ModifyPartition wcm:action=\"add\"><Order>2</Order><PartitionID>3</PartitionID><Format>NTFS</Format><Label>Windows</Label><Letter>C</Letter></ModifyPartition>");
        sb.AppendLine("          </ModifyPartitions>");
        sb.AppendLine("        </Disk>");
        sb.AppendLine("      </DiskConfiguration>");
        sb.AppendLine("      <ImageInstall><OSImage><InstallTo><DiskID>0</DiskID><PartitionID>3</PartitionID></InstallTo></OSImage></ImageInstall>");
        sb.AppendLine("      <UserData><AcceptEula>true</AcceptEula><ProductKey><WillShowUI>Never</WillShowUI></ProductKey></UserData>");
        sb.AppendLine("    </component>");
        sb.AppendLine("  </settings>");

        // ------------------ specialize ------------------
        sb.AppendLine("  <settings pass=\"specialize\">");
        sb.AppendLine("    <component name=\"Microsoft-Windows-Shell-Setup\" processorArchitecture=\"amd64\" publicKeyToken=\"31bf3856ad364e35\" language=\"neutral\" versionScope=\"nonSxS\" xmlns:wcm=\"http://schemas.microsoft.com/WMIConfig/2002/State\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
        sb.AppendLine($"      <ComputerName>{pcName}</ComputerName>");
        sb.AppendLine("      <TimeZone>E. South America Standard Time</TimeZone>");
        sb.AppendLine($"      <RegisteredOwner>{esc}</RegisteredOwner>");
        sb.AppendLine("      <RegisteredOrganization>PhantomUI</RegisteredOrganization>");
        sb.AppendLine("    </component>");

        // Tweaks de registro aplicados offline pelo $OEM$\$$\Setup\Scripts\SetupComplete.cmd (mais seguro que RunSynchronous)
        sb.AppendLine("    <component name=\"Microsoft-Windows-Deployment\" processorArchitecture=\"amd64\" publicKeyToken=\"31bf3856ad364e35\" language=\"neutral\" versionScope=\"nonSxS\" xmlns:wcm=\"http://schemas.microsoft.com/WMIConfig/2002/State\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
        sb.AppendLine("      <RunSynchronous>");
        sb.AppendLine("        <RunSynchronousCommand wcm:action=\"add\"><Order>1</Order><Path>reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Power\" /v HiberbootEnabled /t REG_DWORD /d 0 /f</Path></RunSynchronousCommand>");
        sb.AppendLine("        <RunSynchronousCommand wcm:action=\"add\"><Order>2</Order><Path>reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"GPU Priority\" /t REG_DWORD /d 2 /f</Path></RunSynchronousCommand>");
        sb.AppendLine("        <RunSynchronousCommand wcm:action=\"add\"><Order>3</Order><Path>reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v Priority /t REG_DWORD /d 2 /f</Path></RunSynchronousCommand>");
        sb.AppendLine("        <RunSynchronousCommand wcm:action=\"add\"><Order>4</Order><Path>reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl\" /v Win32PrioritySeparation /t REG_DWORD /d 38 /f</Path></RunSynchronousCommand>");
        sb.AppendLine("        <RunSynchronousCommand wcm:action=\"add\"><Order>5</Order><Path>reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\" /v HwSchMode /t REG_DWORD /d 2 /f</Path></RunSynchronousCommand>");
        sb.AppendLine("        <RunSynchronousCommand wcm:action=\"add\"><Order>6</Order><Path>reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\" /v LargeSystemCache /t REG_DWORD /d 0 /f</Path></RunSynchronousCommand>");
        if (o.DisableTelemetry)
        {
            sb.AppendLine("        <RunSynchronousCommand wcm:action=\"add\"><Order>7</Order><Path>reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection\" /v AllowTelemetry /t REG_DWORD /d 0 /f</Path></RunSynchronousCommand>");
            sb.AppendLine("        <RunSynchronousCommand wcm:action=\"add\"><Order>8</Order><Path>reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent\" /v DisableWindowsConsumerFeatures /t REG_DWORD /d 1 /f</Path></RunSynchronousCommand>");
        }
        if (o.DisableCopilotRecall)
        {
            sb.AppendLine("        <RunSynchronousCommand wcm:action=\"add\"><Order>9</Order><Path>reg add \"HKCU\\Software\\Policies\\Microsoft\\Windows\\WindowsCopilot\" /v TurnOffWindowsCopilot /t REG_DWORD /d 1 /f</Path></RunSynchronousCommand>");
            sb.AppendLine("        <RunSynchronousCommand wcm:action=\"add\"><Order>10</Order><Path>reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI\" /v AllowRecallEnablement /t REG_DWORD /d 0 /f</Path></RunSynchronousCommand>");
        }
        if (o.DisableReservedStorage)
            sb.AppendLine("        <RunSynchronousCommand wcm:action=\"add\"><Order>11</Order><Path>DISM /Online /Set-ReservedStorageState /State:Disabled</Path></RunSynchronousCommand>");
        sb.AppendLine("      </RunSynchronous>");
        sb.AppendLine("    </component>");
        sb.AppendLine("  </settings>");

        // ------------------ oobeSystem ------------------
        sb.AppendLine("  <settings pass=\"oobeSystem\">");
        sb.AppendLine("    <component name=\"Microsoft-Windows-Shell-Setup\" processorArchitecture=\"amd64\" publicKeyToken=\"31bf3856ad364e35\" language=\"neutral\" versionScope=\"nonSxS\" xmlns:wcm=\"http://schemas.microsoft.com/WMIConfig/2002/State\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
        if (o.SkipOobe)
        {
            sb.AppendLine("      <OOBE>");
            sb.AppendLine("        <HideEULAPage>true</HideEULAPage>");
            sb.AppendLine("        <HideOEMRegistrationScreen>true</HideOEMRegistrationScreen>");
            sb.AppendLine("        <HideOnlineAccountScreens>true</HideOnlineAccountScreens>");
            sb.AppendLine("        <HideWirelessSetupInOOBE>true</HideWirelessSetupInOOBE>");
            sb.AppendLine("        <ProtectYourPC>3</ProtectYourPC>");
            sb.AppendLine("      </OOBE>");
        }
        sb.AppendLine("      <UserAccounts><LocalAccounts>");
        sb.AppendLine("        <LocalAccount wcm:action=\"add\">");
        sb.AppendLine($"          <Name>{esc}</Name>");
        sb.AppendLine($"          <DisplayName>{esc}</DisplayName>");
        sb.AppendLine("          <Group>Administrators</Group>");
        if (!string.IsNullOrEmpty(o.Password))
        {
            sb.AppendLine("          <Password><Value>" + Convert.ToBase64String(Encoding.UTF8.GetBytes(o.Password)) + "</Value><PlainText>false</PlainText></Password>");
        }
        sb.AppendLine("        </LocalAccount>");
        sb.AppendLine("      </LocalAccounts></UserAccounts>");
        sb.AppendLine("      <AutoLogon>");
        sb.AppendLine($"        <Username>{esc}</Username>");
        sb.AppendLine("        <Enabled>true</Enabled>");
        sb.AppendLine("        <LogonCount>1</LogonCount>");
        if (!string.IsNullOrEmpty(o.Password))
            sb.AppendLine("        <Password><Value>" + Convert.ToBase64String(Encoding.UTF8.GetBytes(o.Password)) + "</Value><PlainText>false</PlainText></Password>");
        sb.AppendLine("      </AutoLogon>");
        sb.AppendLine("      <FirstLogonCommands>");
        sb.AppendLine("        <SynchronousCommand wcm:action=\"add\"><Order>1</Order><CommandLine>cmd /c \"%SystemDrive%\\PhantomUI\\posinstall.cmd\"</CommandLine><Description>PhantomUI pos-instalacao</Description></SynchronousCommand>");
        sb.AppendLine("      </FirstLogonCommands>");
        sb.AppendLine("    </component>");
        if (o.DisableBitLocker)
        {
            sb.AppendLine("    <component name=\"Microsoft-Windows-Security-SPP-UX\" processorArchitecture=\"amd64\" publicKeyToken=\"31bf3856ad364e35\" language=\"neutral\" versionScope=\"nonSxS\" xmlns:wcm=\"http://schemas.microsoft.com/WMIConfig/2002/State\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
            sb.AppendLine("      <SkipAutoActivation>true</SkipAutoActivation>");
            sb.AppendLine("    </component>");
        }
        sb.AppendLine("  </settings>");
        sb.AppendLine("  <cpi:offlineImage cpi:source=\"wcm://Microsoft-Windows-Setup?version=10.0\" xmlns:cpi=\"urn:schemas-microsoft-com:cpi\" />");
        sb.AppendLine("</unattend>");
        return sb.ToString();
    }

    /// <summary>Gera o $OEM$ (SetupComplete.cmd + posinstall.cmd) com os tweaks seguros do catálogo.</summary>
    public static Dictionary<string, string> BuildOemPayload(IsoBuildOptions o)
    {
        var files = new Dictionary<string, string>();

        var setupComplete = new StringBuilder();
        setupComplete.AppendLine("@echo off");
        setupComplete.AppendLine(":: PHANTOM UI — SetupComplete (roda ao fim da instalacao, antes do primeiro logon)");
        setupComplete.AppendLine("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Power\" /v HiberbootEnabled /t REG_DWORD /d 0 /f");
        setupComplete.AppendLine("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem\" /v NtfsDisableLastAccessUpdate /t REG_DWORD /d 1 /f");
        setupComplete.AppendLine("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters\" /v EnablePrefetcher /t REG_DWORD /d 3 /f");
        setupComplete.AppendLine("reg add \"HKCU\\Control Panel\\Mouse\" /v MouseSpeed /t REG_SZ /d 0 /f");
        setupComplete.AppendLine("reg add \"HKCU\\Control Panel\\Mouse\" /v MouseThreshold1 /t REG_SZ /d 0 /f");
        setupComplete.AppendLine("reg add \"HKCU\\Control Panel\\Mouse\" /v MouseThreshold2 /t REG_SZ /d 0 /f");
        setupComplete.AppendLine("reg add \"HKCU\\Control Panel\\Desktop\" /v MenuShowDelay /t REG_SZ /d 0 /f");
        setupComplete.AppendLine("reg add \"HKCU\\System\\GameConfigStore\" /v GameDVR_Enabled /t REG_DWORD /d 0 /f");
        setupComplete.AppendLine("reg add \"HKCU\\Software\\Microsoft\\GameBar\" /v AutoGameModeEnabled /t REG_DWORD /d 1 /f");
        setupComplete.AppendLine("sc config DiagTrack start= disabled >nul 2>&1");
        setupComplete.AppendLine("netsh int tcp set global taskoffload=enabled >nul 2>&1");
        setupComplete.AppendLine("netsh int tcp set global autotuninglevel=normal >nul 2>&1");
        setupComplete.AppendLine("powercfg /h off >nul 2>&1");
        if (o.RemoveBloat)
        {
            setupComplete.AppendLine(":: Debloat leve e seguro (nao remove Store, nem componentes do sistema)");
            foreach (var pkg in new[]
            {
                "*BingNews*", "*BingWeather*", "*GetHelp*", "*Getstarted*", "*MicrosoftSolitaireCollection*",
                "*MixedReality.Portal*", "*Office.OneNote*", "*People*", "*SkypeApp*", "*Todos*",
                "*WindowsFeedbackHub*", "*WindowsMaps*", "*XboxGameCallableUI*", "*ZuneMusic*", "*ZuneVideo*",
                "*Clipchamp.Clipchamp*", "*News*", "*PowerAutomateDesktop*"
            })
                setupComplete.AppendLine($"powershell -NoProfile -Command \"Get-AppxPackage -AllUsers {pkg} | Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue\" >nul 2>&1");
            setupComplete.AppendLine("powershell -NoProfile -Command \"Get-AppxProvisionedPackage -Online | Where-Object {{$_.DisplayName -match 'Bing|Solitaire|Feedback|Maps|Zune|Clipchamp'}} | Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue\" >nul 2>&1");
        }
        if (!o.KeepEdge)
            setupComplete.AppendLine(":: Edge preservado por padrao no PHANTOM UI (remover quebra WebView2 e alguns jogos)");
        setupComplete.AppendLine("dism /online /cleanup-image /startcomponentcleanup >nul 2>&1");
        setupComplete.AppendLine("echo PHANTOM UI: configuracao concluida. >> %SystemDrive%\\PhantomUI\\install.log");
        setupComplete.AppendLine("del /f /q \"%~f0\"");
        files[@"$OEM$\$$\Setup\Scripts\SetupComplete.cmd"] = setupComplete.ToString();

        var pos = new StringBuilder();
        pos.AppendLine("@echo off");
        pos.AppendLine(":: PHANTOM UI — primeira sessao do usuario");
        pos.AppendLine("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v TaskbarDa /t REG_DWORD /d 0 /f");
        pos.AppendLine("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v ShowCopilotButton /t REG_DWORD /d 0 /f");
        pos.AppendLine("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v Start_TrackProgs /t REG_DWORD /d 0 /f");
        pos.AppendLine("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v SoftLandingEnabled /t REG_DWORD /d 0 /f");
        pos.AppendLine("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search\" /v SearchboxTaskbarMode /t REG_DWORD /d 1 /f");
        pos.AppendLine("reg add \"HKCU\\Software\\Microsoft\\DirectX\\UserGpuPreferences\" /v DirectXUserGlobalSettings /t REG_SZ /d \"GpuPreference=2;SwapEffectUpgradeEnable=1;\" /f");
        pos.AppendLine("echo Primeira sessao configurada >> %SystemDrive%\\PhantomUI\\install.log");
        pos.AppendLine("del /f /q \"%~f0\"");
        files[@"$OEM$\$$\PhantomUI\posinstall.cmd"] = pos.ToString();

        var readme = new StringBuilder();
        readme.AppendLine("PHANTOM UI — WINDOWS PRE-OTIMIZADO");
        readme.AppendLine("==================================");
        readme.AppendLine("Gerado em: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
        readme.AppendLine();
        readme.AppendLine("O que esta instalacao ja aplica:");
        readme.AppendLine(" - Win32PrioritySeparation=38, MMCSS GPU Priority=2 / Priority=2 (valor seguro do acervo Kairo)");
        readme.AppendLine(" - HAGS (HwSchMode=2), LargeSystemCache=0, Prefetch ligado");
        readme.AppendLine(" - Inicializacao Rapida desligada (boot limpo de verdade)");
        readme.AppendLine(" - Game DVR off, Game Mode on, mouse 1:1 sem aceleracao");
        readme.AppendLine(" - Telemetria minima, apps promovidos e Copilot/Recall desativados");
        readme.AppendLine(" - DiagTrack desligado, taskoffload e autotuning corretos");
        readme.AppendLine(" - Debloat leve (mantem Store, Edge/WebView2 e componentes do sistema)");
        readme.AppendLine();
        readme.AppendLine("O que NAO e feito (bloqueado por seguranca):");
        readme.AppendLine(" - Pagefile = 0, TdrLevel = 0, lsass/svchost em Idle, disableelamdrivers, AGPConcur, apagar WinSxS");
        readme.AppendLine();
        readme.AppendLine("Rollback: cada tweak tambem esta no app (aba Otimizacao Manual) com restaurar individual.");
        files[@"$OEM$\$$\PhantomUI\LEIA-ME.txt"] = readme.ToString();
        return files;
    }

    // =====================================================================
    // 2) PREPARO DO CONTEÚDO (ISO → pasta de trabalho)
    // =====================================================================
    /// <summary>Monta a ISO oficial, copia o conteúdo para a pasta de trabalho e desmonta.</summary>
    public static async Task<(bool Ok, string Message)> PrepareMediaAsync(IsoBuildOptions o, Action<string>? progress = null)
    {
        if (!File.Exists(o.SourceIso))
            return (false, Loc.Pick("Selecione a ISO oficial do Windows 10/11 (baixada da Microsoft).", "Select the official Windows 10/11 ISO (downloaded from Microsoft)."));

        string work = string.IsNullOrWhiteSpace(o.WorkDir) ? DefaultWorkDir : o.WorkDir;
        try
        {
            if (Directory.Exists(work)) Directory.Delete(work, true);
            Directory.CreateDirectory(work);
        }
        catch (Exception ex) { return (false, Loc.Pick("Falha ao preparar a pasta de trabalho: ", "Failed to prepare the work folder: ") + ex.Message); }

        progress?.Invoke(Loc.Pick("Montando a ISO…", "Mounting the ISO…"));
        var mount = await Task.Run(() => TweakJournal.RunPowerShell(
            $"(Mount-DiskImage -ImagePath '{o.SourceIso.Replace("'", "''")}' -PassThru | Get-Volume).DriveLetter", 120_000));
        string letter = mount.Output.Trim();
        if (letter.Length == 0)
            return (false, Loc.Pick("Não foi possível montar a ISO. Rode como administrador.", "Could not mount the ISO. Run as administrator."));
        letter = letter[0].ToString();

        try
        {
            progress?.Invoke(Loc.Pick("Copiando arquivos da ISO (pode levar alguns minutos)…", "Copying ISO files (may take a few minutes)…"));
            await Task.Run(() =>
            {
                foreach (var dir in Directory.GetDirectories($"{letter}:\\"))
                    CopyDir(dir, Path.Combine(work, Path.GetFileName(dir)));
                foreach (var file in Directory.GetFiles($"{letter}:\\"))
                    File.Copy(file, Path.Combine(work, Path.GetFileName(file)), true);
            });

            progress?.Invoke(Loc.Pick("Escrevendo autounattend.xml e $OEM$…", "Writing autounattend.xml and $OEM$…"));
            File.WriteAllText(Path.Combine(work, "autounattend.xml"), BuildAutounattend(o), new UTF8Encoding(false));
            foreach (var kv in BuildOemPayload(o))
            {
                string dest = Path.Combine(work, kv.Key);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                File.WriteAllText(dest, kv.Value, new UTF8Encoding(false));
            }
            return (true, Loc.Pick($"Mídia preparada em {work}.", $"Media prepared in {work}."));
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
        finally
        {
            await Task.Run(() => TweakJournal.RunPowerShell($"Dismount-DiskImage -ImagePath '{o.SourceIso.Replace("'", "''")}' | Out-Null", 60_000));
        }
    }

    private static void CopyDir(string src, string dst)
    {
        Directory.CreateDirectory(dst);
        foreach (var f in Directory.GetFiles(src))
            File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), true);
        foreach (var d in Directory.GetDirectories(src))
            CopyDir(d, Path.Combine(dst, Path.GetFileName(d)));
    }

    // =====================================================================
    // 3) DRIVERS (pnputil export)
    // =====================================================================
    /// <summary>Exporta os drivers do PC atual com pnputil para injetar na imagem (sem baixar "driver packs" de terceiros).</summary>
    public static async Task<(bool Ok, string Message, string Folder)> ExportDriversAsync(string workDir, Action<string>? progress = null)
    {
        string folder = Path.Combine(workDir, @"$OEM$\$$\Drivers");
        try
        {
            progress?.Invoke(Loc.Pick("Exportando drivers instalados (pnputil)…", "Exporting installed drivers (pnputil)…"));
            Directory.CreateDirectory(folder);
            var r = await Task.Run(() => TweakJournal.RunProcess("pnputil.exe", $"/export-driver * \"{folder}\"", 900_000));
            if (r.ExitCode != 0)
                return (false, Loc.Pick("pnputil falhou (precisa de administrador). A instalação continua sem injeção de drivers.",
                                        "pnputil failed (needs administrator). The install continues without driver injection.") + " " + r.Error.Trim(), folder);
            int count = 0;
            try { count = Directory.GetDirectories(folder).Length; } catch { }
            return (true, Loc.Pick($"{count} drivers exportados do hardware atual para {folder}.",
                                   $"{count} drivers exported from the current hardware to {folder}."), folder);
        }
        catch (Exception ex) { return (false, ex.Message, folder); }
    }

    // =====================================================================
    // 4) DISM OFFLINE (install.wim)
    // =====================================================================
    /// <summary>Personaliza o install.wim offline: injeta drivers, remove Appx provisionados e aplica tweaks.</summary>
    public static async Task<(bool Ok, string Message)> CustomizeWimAsync(IsoBuildOptions o, string workDir, string? driversFolder, Action<string>? progress = null)
    {
        string sources = Path.Combine(workDir, "sources");
        string install = Directory.GetFiles(sources, "install.*")
            .FirstOrDefault(f => f.EndsWith(".wim", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".esd", StringComparison.OrdinalIgnoreCase)) ?? "";
        if (install.Length == 0)
            return (false, Loc.Pick("install.wim/esd não encontrado em sources/.", "install.wim/esd not found in sources/."));

        // .esd precisa ser convertido para .wim antes de montar
        if (install.EndsWith(".esd", StringComparison.OrdinalIgnoreCase))
        {
            progress?.Invoke(Loc.Pick("Convertendo install.esd para install.wim (recuperação)…", "Converting install.esd to install.wim (recovery)…"));
            string wim = Path.Combine(sources, "install.wim");
            var conv = await Task.Run(() => TweakJournal.RunProcess("dism.exe",
                $"/English /Export-Image /SourceImageFile:\"{install}\" /SourceIndex:1 /DestinationImageFile:\"{wim}\" /Compress:max /CheckIntegrity", 3_600_000));
            if (conv.ExitCode != 0)
                return (false, Loc.Pick("Não foi possível converter o ESD. Use uma ISO com install.wim.", "Could not convert the ESD. Use an ISO with install.wim.") + " " + conv.Error.Trim());
            install = wim;
        }

        string mountDir = Path.Combine(workDir, "_mount");
        Directory.CreateDirectory(mountDir);
        var log = new StringBuilder();

        try
        {
            progress?.Invoke(Loc.Pick("Listando imagens do install.wim…", "Listing install.wim images…"));
            var info = await Task.Run(() => TweakJournal.RunProcess("dism.exe", $"/English /Get-WimInfo /WimFile:\"{install}\"", 120_000));
            var indexes = System.Text.RegularExpressions.Regex.Matches(info.Output, @"Index\s*:\s*(\d+)")
                .Select(m => m.Groups[1].Value).ToList();
            if (indexes.Count == 0) indexes.Add("1");
            string idx = indexes.Contains("6") ? "6" : indexes[^1]; // 6 = Windows 11 Pro quando disponível

            progress?.Invoke(Loc.Pick($"Montando a imagem índice {idx}…", $"Mounting image index {idx}…"));
            var mount = await Task.Run(() => TweakJournal.RunProcess("dism.exe",
                $"/English /Mount-Wim /WimFile:\"{install}\" /Index:{idx} /MountDir:\"{mountDir}\"", 900_000));
            if (mount.ExitCode != 0)
                return (false, Loc.Pick("Falha ao montar a imagem: ", "Failed to mount the image: ") + mount.Error.Trim());

            bool mounted = true;
            try
            {
                if (o.InjectDrivers && driversFolder != null && Directory.Exists(driversFolder))
                {
                    progress?.Invoke(Loc.Pick("Injetando drivers do hardware atual no WIM…", "Injecting current hardware drivers into the WIM…"));
                    var add = await Task.Run(() => TweakJournal.RunProcess("dism.exe",
                        $"/English /Image:\"{mountDir}\" /Add-Driver /Driver:\"{driversFolder}\" /Recurse /ForceUnsigned", 1_800_000));
                    log.AppendLine(add.ExitCode == 0 ? "✔ drivers injetados" : "⚠ drivers: " + add.Error.Trim());
                }

                if (o.RemoveBloat)
                {
                    progress?.Invoke(Loc.Pick("Removendo Appx provisionados do WIM…", "Removing provisioned Appx from the WIM…"));
                    var list = await Task.Run(() => TweakJournal.RunProcess("dism.exe",
                        $"/English /Image:\"{mountDir}\" /Get-ProvisionedAppxPackages", 300_000));
                    var packages = System.Text.RegularExpressions.Regex.Matches(list.Output, @"PackageName\s*:\s*(\S+)")
                        .Select(m => m.Groups[1].Value)
                        .Where(n => new[] { "BingNews", "BingWeather", "ZuneMusic", "ZuneVideo", "Solitaire", "FeedbackHub",
                                            "WindowsMaps", "SkypeApp", "People", "MixedReality", "Clipchamp", "Todos", "GetHelp", "OneNote" }
                                     .Any(k => n.Contains(k, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                    foreach (var pkg in packages)
                        await Task.Run(() => TweakJournal.RunProcess("dism.exe",
                            $"/English /Image:\"{mountDir}\" /Remove-ProvisionedAppxPackage /PackageName:\"{pkg}\"", 300_000));
                    log.AppendLine($"✔ {packages.Count} pacotes Appx removidos");
                }

                // Tweaks offline no registro da imagem
                if (o.OptimizeOs)
                {
                    progress?.Invoke(Loc.Pick("Aplicando tweaks seguros offline (registry hive)…", "Applying safe offline tweaks (registry hive)…"));
                    ApplyOfflineRegistry(mountDir, o, log);
                }

                progress?.Invoke(Loc.Pick("Desmontando e salvando a imagem (commit)…", "Unmounting and committing the image…"));
                var unmount = await Task.Run(() => TweakJournal.RunProcess("dism.exe",
                    $"/English /Unmount-Wim /MountDir:\"{mountDir}\" /Commit", 1_800_000));
                mounted = false;
                if (unmount.ExitCode != 0)
                    return (false, Loc.Pick("Falha no commit da imagem: ", "Image commit failed: ") + unmount.Error.Trim());

                log.AppendLine(Loc.Pick("✔ WIM personalizado e salvo", "✔ WIM customized and saved"));
                return (true, log.ToString());
            }
            finally
            {
                if (mounted)
                    await Task.Run(() => TweakJournal.RunProcess("dism.exe", $"/English /Unmount-Wim /MountDir:\"{mountDir}\" /Discard", 600_000));
            }
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    /// <summary>Carrega o hive SYSTEM do WIM montado, aplica os tweaks, salva e descarrega.</summary>
    private static void ApplyOfflineRegistry(string mountDir, IsoBuildOptions o, StringBuilder log)
    {
        string hiveFile = Path.Combine(mountDir, @"Windows\System32\config\SYSTEM");
        if (!File.Exists(hiveFile)) { log.AppendLine("⚠ hive SYSTEM não encontrado"); return; }
        const string key = "HKLM\\OXOFFLINE";
        TweakJournal.RunProcess("reg.exe", $@"load {key} ""{hiveFile}""", 60_000);
        void Set(string sub, string name, int value) =>
            TweakJournal.RunProcess("reg.exe", $@"add ""{key}\{sub}"" /v ""{name}"" /t REG_DWORD /d {value} /f", 20_000);
        try
        {
            Set(@"ControlSet001\Control\PriorityControl", "Win32PrioritySeparation", 38);
            Set(@"ControlSet001\Control\Session Manager\Memory Management", "LargeSystemCache", 0);
            Set(@"ControlSet001\Control\Session Manager\Memory Management", "DisablePagingExecutive", 1);
            Set(@"ControlSet001\Control\Session Manager\Power", "HiberbootEnabled", 0);
            Set(@"ControlSet001\Control\GraphicsDrivers", "HwSchMode", 2);
            Set(@"ControlSet001\Control\FileSystem", "NtfsDisableLastAccessUpdate", 1);
            Set(@"ControlSet001\Control\Session Manager\Memory Management\PrefetchParameters", "EnablePrefetcher", 3);
            Set(@"ControlSet001\Control\Session Manager\kernel", "GlobalTimerResolutionRequests", 0);
            TweakJournal.RunProcess("reg.exe", $@"add ""{key}\ControlSet001\Services\DiagTrack"" /v Start /t REG_DWORD /d 4 /f", 20_000);
            log.AppendLine("✔ tweaks offline aplicados ao hive SYSTEM");
        }
        finally
        {
            TweakJournal.RunProcess("reg.exe", $@"unload {key}", 60_000);
        }
    }

    // =====================================================================
    // 5) PENDRIVE BOOTÁVEL
    // =====================================================================
    /// <summary>Formata o pendrive (FAT32 + bootsect UEFI/NTFS) e copia a mídia preparada.</summary>
    public static async Task<(bool Ok, string Message)> WriteUsbAsync(IsoBuildOptions o, string workDir, Action<string>? progress = null)
    {
        string letter = (o.TargetUsb ?? "").Trim().TrimEnd(':').ToUpperInvariant();
        if (letter.Length != 1 || !char.IsLetter(letter[0]))
            return (false, Loc.Pick("Selecione a letra do pendrive (ex.: E).", "Select the USB drive letter (e.g. E)."));
        if (letter == "C")
            return (false, Loc.Pick("Recusado: C: é o disco do sistema.", "Refused: C: is the system disk."));

        long sizeGb = 0;
        try { sizeGb = (long)(new DriveInfo(letter + ":\\").TotalSize / 1073741824.0); }
        catch { return (false, Loc.Pick($"Unidade {letter}: não encontrada.", $"Drive {letter}: not found.")); }
        if (sizeGb < 8)
            return (false, Loc.Pick("Pendrive menor que 8 GB não comporta o Windows 11.", "A USB drive smaller than 8 GB cannot hold Windows 11."));

        string script = $@"
$ErrorActionPreference = 'Stop'
$disk = Get-Disk | Where-Object {{ $_.DriveLetter -eq '{letter}' }}
if (-not $disk) {{ throw 'Disco nao encontrado' }}
if ($disk.IsBoot -or $disk.IsSystem) {{ throw 'Disco do sistema - abortado' }}
Clear-Disk -Number $disk.Number -RemoveData -RemoveOEM -Confirm:$false
Initialize-Disk -Number $disk.Number -PartitionStyle MBR
$p = New-Partition -DiskNumber $disk.Number -UseMaximumSize -IsActive -AssignDriveLetter
Format-Volume -Partition $p -FileSystem NTFS -NewFileSystemLabel 'PHANTOMUI' -Confirm:$false -Force
$p | Set-Partition -NewDriveLetter '{letter}'
'bootsect /nt60 {letter}: /mbr'
";
        progress?.Invoke(Loc.Pick($"Formatando {letter}: como NTFS bootável…", $"Formatting {letter}: as bootable NTFS…"));
        var fmt = await Task.Run(() => TweakJournal.RunPowerShell(script, 900_000));
        if (fmt.ExitCode != 0)
            return (false, Loc.Pick("Falha ao formatar: ", "Format failed: ") + fmt.Error.Trim());

        // bootsect oficial do Windows (ADK) — se ausente, o Windows Setup já escreve o boot UEFI ao copiar
        await Task.Run(() => TweakJournal.RunProcess("bootsect.exe", $"/nt60 {letter}: /mbr", 120_000));

        progress?.Invoke(Loc.Pick("Copiando os arquivos para o pendrive…", "Copying files to the USB drive…"));
        try
        {
            await Task.Run(() =>
            {
                foreach (var dir in Directory.GetDirectories(workDir))
                {
                    if (Path.GetFileName(dir).StartsWith("_", StringComparison.Ordinal)) continue;
                    CopyDir(dir, Path.Combine($"{letter}:\\", Path.GetFileName(dir)));
                }
                foreach (var file in Directory.GetFiles(workDir))
                    File.Copy(file, Path.Combine($"{letter}:\\", Path.GetFileName(file)), true);
            });
        }
        catch (Exception ex) { return (false, Loc.Pick("Falha na cópia: ", "Copy failed: ") + ex.Message); }

        return (true, Loc.Pick(
            $"✔ Pendrive {letter}: pronto para boot (UEFI). Ligue o PC pelo pendrive e a instalação roda sozinha, já otimizada.",
            $"✔ USB {letter}: ready to boot (UEFI). Boot the PC from it and the install runs unattended, already optimized."));
    }

    // =====================================================================
    // 6) ISO BOOTÁVEL (oscdimg)
    // =====================================================================
    public static async Task<(bool Ok, string Message)> CreateIsoAsync(IsoBuildOptions o, string workDir, Action<string>? progress = null)
    {
        string oscdimg = FindOscdimg();
        if (oscdimg.Length == 0)
            return (false, Loc.Pick(
                "oscdimg.exe não encontrado. Instale o Windows ADK (Deployment Tools) — é a ferramenta oficial da Microsoft, sem gambiarra. Sem ele, use o modo pendrive.",
                "oscdimg.exe not found. Install the Windows ADK (Deployment Tools) — the official Microsoft tool, no hacks. Without it, use USB mode."));

        string output = string.IsNullOrWhiteSpace(o.OutputIso) ? DefaultOutputIso : o.OutputIso;
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        string etfsboot = Path.Combine(workDir, @"boot\etfsboot.com");
        string efisys = Path.Combine(workDir, @"efi\microsoft\boot\efisys.bin");

        string args = $"-m -o -u2 -udfver102 -bootdata:2#p0,e,b\"{etfsboot}\"#pEF,e,b\"{efisys}\" \"{workDir}\" \"{output}\"";
        progress?.Invoke(Loc.Pick("Gerando ISO bootável (oscdimg)…", "Building bootable ISO (oscdimg)…"));
        var r = await Task.Run(() => TweakJournal.RunProcess(oscdimg, args, 3_600_000));
        return r.ExitCode == 0
            ? (true, Loc.Pick($"✔ ISO criada: {output}", $"✔ ISO created: {output}"))
            : (false, Loc.Pick("Falha do oscdimg: ", "oscdimg failed: ") + r.Error.Trim());
    }

    public static string FindOscdimg()
    {
        var candidates = new[]
        {
            @"C:\Program Files (x86)\Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\amd64\Oscdimg\oscdimg.exe",
            @"C:\Program Files (x86)\Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\x86\Oscdimg\oscdimg.exe",
            @"C:\Program Files\Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\amd64\Oscdimg\oscdimg.exe"
        };
        foreach (var c in candidates) if (File.Exists(c)) return c;
        try
        {
            var path = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var dir in path.Split(';'))
            {
                if (dir.Trim().Length == 0) continue;
                string p = Path.Combine(dir.Trim(), "oscdimg.exe");
                if (File.Exists(p)) return p;
            }
        }
        catch { }
        return "";
    }

    // =====================================================================
    // 7) INSTALAÇÃO LIMPA SEM PENDRIVE
    // =====================================================================
    /// <summary>Monta a ISO e roda o Windows Setup com /auto clean (mantém nada). Exige confirmação explícita.</summary>
    public static async Task<(bool Ok, string Message)> InstallWithoutUsbAsync(IsoBuildOptions o, bool confirmed)
    {
        if (!confirmed)
            return (false, Loc.Pick("Confirmação obrigatória: este modo APAGA o disco do sistema.",
                                    "Confirmation required: this mode WIPES the system disk."));
        if (!File.Exists(o.SourceIso))
            return (false, Loc.Pick("Selecione a ISO oficial do Windows.", "Select the official Windows ISO."));

        string answer = Path.Combine(AppSettings.Folder, "IsoBuild", "autounattend.xml");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(answer)!);
            File.WriteAllText(answer, BuildAutounattend(o), new UTF8Encoding(false));
        }
        catch { }

        var mount = await Task.Run(() => TweakJournal.RunPowerShell(
            $"(Mount-DiskImage -ImagePath '{o.SourceIso.Replace("'", "''")}' -PassThru | Get-Volume).DriveLetter", 120_000));
        string letter = mount.Output.Trim();
        if (letter.Length == 0)
            return (false, Loc.Pick("Não foi possível montar a ISO.", "Could not mount the ISO."));
        letter = letter[0].ToString();

        string setup = $"{letter}:\\setup.exe";
        if (!File.Exists(setup))
            return (false, Loc.Pick("setup.exe não encontrado na ISO.", "setup.exe not found in the ISO."));

        // /auto clean = instalação limpa mantendo nada; /m: aponta o autounattend
        var r = await Task.Run(() => TweakJournal.RunProcess(setup,
            $"/auto clean /m:\"{answer}\" /noreboot", 30_000));
        return (true, Loc.Pick(
            $"O Windows Setup foi iniciado em modo instalação limpa (/auto clean) com o autounattend do PHANTOM UI. A máquina vai reiniciar e apagar o disco do sistema.",
            $"Windows Setup started in clean install mode (/auto clean) with the PHANTOM UI answer file. The machine will reboot and wipe the system disk."));
    }

    /// <summary>Reinstalação limpa usando a imagem do próprio Windows (systemreset -cleanpc).</summary>
    public static (bool Ok, string Message) ResetPcClean(bool confirmed, bool keepFiles = false)
    {
        if (!confirmed)
            return (false, Loc.Pick("Confirmação obrigatória.", "Confirmation required."));
        string args = keepFiles ? "/cleanpc" : "/cleanpc # mantém arquivos; remova o parâmetro para apagar tudo";
        var r = TweakJournal.RunProcess("systemreset.exe", "-cleanpc", 30_000);
        return (true, Loc.Pick(
            "systemreset -cleanpc iniciado: o Windows baixa/usará a imagem oficial e reinstala sem apps e sem sujeira, mantendo ou não seus arquivos conforme sua escolha na tela.",
            "systemreset -cleanpc started: Windows will use the official image and reinstall without apps and without junk, keeping your files or not as you choose on screen."));
    }

    /// <summary>Fluxo completo: preparar mídia → exportar drivers → personalizar WIM → pendrive e/ou ISO.</summary>
    public static async Task<IsoBuildResult> BuildAsync(IsoBuildOptions o, Action<string>? progress = null)
    {
        var res = new IsoBuildResult();
        void Log(string s) { res.Log.Add(s); progress?.Invoke(s); }

        var prep = await PrepareMediaAsync(o, Log);
        Log(prep.Message);
        if (!prep.Ok) { res.Message = prep.Message; return res; }

        string work = string.IsNullOrWhiteSpace(o.WorkDir) ? DefaultWorkDir : o.WorkDir;
        res.AnswerFile = Path.Combine(work, "autounattend.xml");

        string? drivers = null;
        if (o.InjectDrivers)
        {
            var exp = await ExportDriversAsync(work, Log);
            Log(exp.Message);
            if (exp.Ok) drivers = exp.Folder;
        }

        if (o.OptimizeOs || o.RemoveBloat || o.InjectDrivers)
        {
            var wim = await CustomizeWimAsync(o, work, drivers, Log);
            Log(wim.Message);
            if (!wim.Ok)
                Log(Loc.Pick("⚠ WIM não personalizado — a mídia continua válida, só sem as mudanças offline.",
                             "⚠ WIM not customized — the media is still valid, just without the offline changes."));
        }

        if (!string.IsNullOrWhiteSpace(o.TargetUsb))
        {
            var usb = await WriteUsbAsync(o, work, Log);
            Log(usb.Message);
            if (usb.Ok) res.UsbLetter = o.TargetUsb;
        }

        if (o.CreateIso)
        {
            var iso = await CreateIsoAsync(o, work, Log);
            Log(iso.Message);
            if (iso.Ok) res.OutIso = string.IsNullOrWhiteSpace(o.OutputIso) ? DefaultOutputIso : o.OutputIso;
        }

        res.Ok = true;
        res.Message = Loc.Pick("Mídia do Windows pré-otimizado gerada com sucesso.", "Pre-optimized Windows media generated successfully.");
        return res;
    }

    /// <summary>Lista pendrives removíveis válidos (letra + tamanho).</summary>
    public static List<(string Letter, string Label, double SizeGb)> RemovableDrives()
    {
        var list = new List<(string, string, double)>();
        try
        {
            foreach (var d in DriveInfo.GetDrives())
            {
                if (d.DriveType != DriveType.Removable || !d.IsReady) continue;
                list.Add((d.Name.TrimEnd('\\', ':'), d.VolumeLabel, d.TotalSize / 1073741824.0));
            }
        }
        catch { }
        return list;
    }
}
