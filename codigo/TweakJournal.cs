using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace PhantomUI;

public enum TweakOpKind
{
    RegSet,
    RegDelete,
    ServiceStartMode,
    ScheduledTask,
    BcdSet,
    BcdDelete,
    ShellCommand
}

/// <summary>Operação atômica reversível executada pelo motor de otimização.</summary>
public sealed class TweakOp
{
    public TweakOpKind Kind { get; init; }
    public string Target { get; init; } = "";      // ex.: HKLM\SOFTWARE\... ou nome do serviço / task / bcd
    public string ValueName { get; init; } = "";   // nome do valor no registro
    public object? Value { get; init; }            // novo valor
    public RegistryValueKind RegKind { get; init; } = RegistryValueKind.DWord;
    public string ApplyCmd { get; init; } = "";
    public string UndoCmd { get; init; } = "";

    public static TweakOp RegDword(string keyPath, string name, int val) => new()
    {
        Kind = TweakOpKind.RegSet, Target = keyPath, ValueName = name, Value = val, RegKind = RegistryValueKind.DWord
    };

    public static TweakOp RegQword(string keyPath, string name, long val) => new()
    {
        Kind = TweakOpKind.RegSet, Target = keyPath, ValueName = name, Value = val, RegKind = RegistryValueKind.QWord
    };

    public static TweakOp RegString(string keyPath, string name, string val, bool expand = false) => new()
    {
        Kind = TweakOpKind.RegSet, Target = keyPath, ValueName = name, Value = val,
        RegKind = expand ? RegistryValueKind.ExpandString : RegistryValueKind.String
    };

    public static TweakOp RegBinary(string keyPath, string name, byte[] val) => new()
    {
        Kind = TweakOpKind.RegSet, Target = keyPath, ValueName = name, Value = val, RegKind = RegistryValueKind.Binary
    };

    public static TweakOp RegDel(string keyPath, string name) => new()
    {
        Kind = TweakOpKind.RegDelete, Target = keyPath, ValueName = name
    };

    public static TweakOp Service(string serviceName, int startType) => new()
    {
        Kind = TweakOpKind.ServiceStartMode, Target = serviceName, Value = startType
    };

    public static TweakOp TaskState(string taskPath, bool enable) => new()
    {
        Kind = TweakOpKind.ScheduledTask, Target = taskPath, Value = enable
    };

    public static TweakOp Bcd(string key, string val) => new()
    {
        Kind = TweakOpKind.BcdSet, Target = key, ValueName = val
    };

    public static TweakOp BcdDel(string key) => new()
    {
        Kind = TweakOpKind.BcdDelete, Target = key
    };

    public static TweakOp Cmd(string applyPsOrCmd, string undoPsOrCmd) => new()
    {
        Kind = TweakOpKind.ShellCommand, ApplyCmd = applyPsOrCmd, UndoCmd = undoPsOrCmd
    };
}

/// <summary>Registro persistido em JSONL para desfazer com exatidão cirúrgica qualquer tweak ou sessão.</summary>
public sealed class JournalRecord
{
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string SessionId { get; set; } = "";
    public string TweakId { get; set; } = "";
    public string TweakTitle { get; set; } = "";
    public TweakOpKind Kind { get; set; }
    public string Target { get; set; } = "";
    public string ValueName { get; set; } = "";
    public bool ExistedBefore { get; set; }
    public string PriorRegKind { get; set; } = "";
    public string? PriorSerialized { get; set; }
    public int? PriorServiceStart { get; set; }
    public bool? PriorTaskEnabled { get; set; }
    public string? PriorBcdValue { get; set; }
    public string UndoCmd { get; set; } = "";
}

public sealed class JournalSessionInfo
{
    public string SessionId { get; set; } = "";
    public DateTime CreatedLocal { get; set; }
    public string Label { get; set; } = "";
    public int OpCount { get; set; }
    public string FolderPath { get; set; } = "";
    public string Summary => $"{CreatedLocal:dd/MM/yyyy HH:mm:ss} · {Label} ({OpCount} ops)";
}

/// <summary>
/// Motor transacional de backup e rollback (evolução em C# nativo do 90.Motor.ps1 do Kairo Tweaker).
/// Salva estado exato anterior em %AppData%\PhantomUI\Backups\ (sessões + índice por TweakId).
/// </summary>
public static class TweakJournal
{
    public static string BackupsRoot => Path.Combine(AppSettings.Folder, "Backups");
    private static string PerTweakDir => Path.Combine(BackupsRoot, "PerTweak");

    public static (bool Ok, string Message) CreateRestorePoint(string description = "PhantomUI Safe Checkpoint")
    {
        try
        {
            // Habilita frequência imediata de ponto de restauração (evita bloqueio de 24h do Windows)
            using (var k = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore", true))
                k?.SetValue("SystemRestorePointCreationFrequency", 0, RegistryValueKind.DWord);

            var r = RunPowerShell($"Enable-ComputerRestore -Drive \"$env:SystemDrive\\\" -ErrorAction SilentlyContinue; Checkpoint-Computer -Description '{description.Replace("'", "")}' -RestorePointType 'MODIFY_SETTINGS' -ErrorAction Stop", 90_000);
            return r.ExitCode == 0
                ? (true, Loc.Pick("Ponto de Restauração do Windows criado com sucesso.", "Windows System Restore Point created successfully."))
                : (false, Loc.Pick("Aviso ao criar Ponto de Restauração (Proteção do Sistema pode estar desativada no disco): ", "Restore Point warning: ") + r.Error);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public static string BeginSession(string label)
    {
        Directory.CreateDirectory(BackupsRoot);
        string id = $"sess_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..4]}";
        string dir = Path.Combine(BackupsRoot, id);
        Directory.CreateDirectory(dir);
        var meta = new JournalSessionInfo
        {
            SessionId = id,
            CreatedLocal = DateTime.Now,
            Label = label,
            OpCount = 0,
            FolderPath = dir
        };
        File.WriteAllText(Path.Combine(dir, "meta.json"), JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true }));
        return id;
    }

    public static List<JournalSessionInfo> ListSessions()
    {
        var list = new List<JournalSessionInfo>();
        try
        {
            if (!Directory.Exists(BackupsRoot)) return list;
            foreach (var dir in Directory.GetDirectories(BackupsRoot, "sess_*"))
            {
                string metaPath = Path.Combine(dir, "meta.json");
                string jlPath = Path.Combine(dir, "journal.jsonl");
                if (!File.Exists(metaPath)) continue;
                var info = JsonSerializer.Deserialize<JournalSessionInfo>(File.ReadAllText(metaPath));
                if (info == null) continue;
                info.FolderPath = dir;
                if (File.Exists(jlPath))
                    info.OpCount = File.ReadAllLines(jlPath).Count(l => !string.IsNullOrWhiteSpace(l));
                list.Add(info);
            }
        }
        catch { }
        return list.OrderByDescending(s => s.CreatedLocal).ToList();
    }

    public static bool HasTweakBackup(string tweakId)
    {
        string p = Path.Combine(PerTweakDir, SafeFileName(tweakId) + ".jsonl");
        return File.Exists(p);
    }

    public static (bool Ok, string Message) ApplyTweak(TweakEntry tweak, string? sessionId = null)
    {
        if (tweak.Risk == TweakRisk.BlockedHazard)
            return (false, Loc.Pick(
                "Bloqueado por segurança: este ajuste destrutivo causa tela azul, corrupção ou travamento.",
                "Blocked for safety: this destructive tweak causes BSOD, corruption, or system instability."));

        string sid = sessionId ?? BeginSession(tweak.Title);
        string sessFile = Path.Combine(BackupsRoot, sid, "journal.jsonl");
        Directory.CreateDirectory(PerTweakDir);
        string tweakFile = Path.Combine(PerTweakDir, SafeFileName(tweak.Id) + ".jsonl");
        bool firstTimeForTweak = !File.Exists(tweakFile);

        var errors = new List<string>();
        var tweakRecords = new List<string>();

        foreach (var op in tweak.Ops)
        {
            try
            {
                var rec = CaptureAndExecute(op, sid, tweak.Id, tweak.Title);
                string json = JsonSerializer.Serialize(rec);
                File.AppendAllText(sessFile, json + Environment.NewLine, Encoding.UTF8);
                if (firstTimeForTweak) tweakRecords.Add(json);
            }
            catch (Exception ex)
            {
                errors.Add($"{op.Kind} ({op.Target}): {ex.Message}");
            }
        }

        if (firstTimeForTweak && tweakRecords.Count > 0)
            File.WriteAllLines(tweakFile, tweakRecords, Encoding.UTF8);

        if (tweak.CustomApply != null)
        {
            try { tweak.CustomApply(); }
            catch (Exception ex) { errors.Add(ex.Message); }
        }

        tweak.RefreshState();
        return errors.Count == 0
            ? (true, Loc.Pick($"Aplicado com backup transacional: {tweak.Title}", $"Applied with transactional backup: {tweak.Title}"))
            : (false, string.Join(" | ", errors));
    }

    public static (bool Ok, string Message) RevertTweak(TweakEntry tweak)
    {
        var errors = new List<string>();
        string tweakFile = Path.Combine(PerTweakDir, SafeFileName(tweak.Id) + ".jsonl");

        if (File.Exists(tweakFile))
        {
            var lines = File.ReadAllLines(tweakFile, Encoding.UTF8).Where(l => !string.IsNullOrWhiteSpace(l)).Reverse().ToList();
            foreach (var line in lines)
            {
                try
                {
                    var rec = JsonSerializer.Deserialize<JournalRecord>(line);
                    if (rec != null) UndoRecord(rec);
                }
                catch (Exception ex) { errors.Add(ex.Message); }
            }
            if (errors.Count == 0)
            {
                try { File.Delete(tweakFile); } catch { }
            }
        }
        else if (tweak.FallbackRevertOps.Count > 0)
        {
            foreach (var op in tweak.FallbackRevertOps)
            {
                try { CaptureAndExecute(op, "revert_fallback", tweak.Id, tweak.Title); }
                catch (Exception ex) { errors.Add(ex.Message); }
            }
        }

        if (tweak.CustomRevert != null)
        {
            try { tweak.CustomRevert(); }
            catch (Exception ex) { errors.Add(ex.Message); }
        }

        tweak.RefreshState();
        return errors.Count == 0
            ? (true, Loc.Pick($"Revertido ao padrão seguro: {tweak.Title}", $"Reverted to safe default: {tweak.Title}"))
            : (false, string.Join(" | ", errors));
    }

    public static (int Restored, List<string> Errors) RollbackSession(string sessionId)
    {
        var errors = new List<string>();
        int count = 0;
        string jlPath = Path.Combine(BackupsRoot, sessionId, "journal.jsonl");
        if (!File.Exists(jlPath)) return (0, new List<string> { "Sessão não encontrada." });

        var lines = File.ReadAllLines(jlPath, Encoding.UTF8).Where(l => !string.IsNullOrWhiteSpace(l)).Reverse().ToList();
        foreach (var line in lines)
        {
            try
            {
                var rec = JsonSerializer.Deserialize<JournalRecord>(line);
                if (rec != null)
                {
                    UndoRecord(rec);
                    count++;
                }
            }
            catch (Exception ex) { errors.Add(ex.Message); }
        }
        return (count, errors);
    }

    private static JournalRecord CaptureAndExecute(TweakOp op, string sessionId, string tweakId, string title)
    {
        var rec = new JournalRecord
        {
            SessionId = sessionId,
            TweakId = tweakId,
            TweakTitle = title,
            Kind = op.Kind,
            Target = op.Target,
            ValueName = op.ValueName,
            UndoCmd = op.UndoCmd
        };

        switch (op.Kind)
        {
            case TweakOpKind.RegSet:
            case TweakOpKind.RegDelete:
            {
                var (root, sub) = SplitRegistryPath(op.Target);
                using (var existing = root.OpenSubKey(sub, false))
                {
                    object? cur = existing?.GetValue(op.ValueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                    if (cur != null && existing != null)
                    {
                        rec.ExistedBefore = true;
                        var vk = existing.GetValueKind(op.ValueName);
                        rec.PriorRegKind = vk.ToString();
                        rec.PriorSerialized = SerializeRegValue(cur, vk);
                    }
                }

                if (op.Kind == TweakOpKind.RegSet)
                {
                    using var writable = root.CreateSubKey(sub, true);
                    if (writable != null && op.Value != null)
                        writable.SetValue(op.ValueName, op.Value, op.RegKind);
                }
                else
                {
                    using var writable = root.OpenSubKey(sub, true);
                    writable?.DeleteValue(op.ValueName, false);
                }
                break;
            }

            case TweakOpKind.ServiceStartMode:
            {
                string svcKey = $@"SYSTEM\CurrentControlSet\Services\{op.Target}";
                using var k = Registry.LocalMachine.OpenSubKey(svcKey, true);
                if (k != null)
                {
                    object? prior = k.GetValue("Start");
                    if (prior != null) { rec.ExistedBefore = true; rec.PriorServiceStart = Convert.ToInt32(prior); }
                    int nextStart = Convert.ToInt32(op.Value ?? 3);
                    k.SetValue("Start", nextStart, RegistryValueKind.DWord);
                    if (nextStart == 4)
                        RunProcess("sc.exe", $"stop \"{op.Target}\"", 8000);
                }
                break;
            }

            case TweakOpKind.ScheduledTask:
            {
                var q = RunProcess("schtasks.exe", $"/Query /TN \"{op.Target}\" /FO CSV /NH", 8000);
                rec.ExistedBefore = q.ExitCode == 0;
                rec.PriorTaskEnabled = !q.Output.Contains("Disabled", StringComparison.OrdinalIgnoreCase) &&
                                       !q.Output.Contains("Desabilitado", StringComparison.OrdinalIgnoreCase);
                bool enable = Convert.ToBoolean(op.Value ?? false);
                if (rec.ExistedBefore)
                    RunProcess("schtasks.exe", $"/Change /TN \"{op.Target}\" {(enable ? "/Enable" : "/Disable")}", 10000);
                break;
            }

            case TweakOpKind.BcdSet:
            case TweakOpKind.BcdDelete:
            {
                var cur = RunProcess("bcdedit.exe", "/enum {current}", 8000);
                foreach (var line in cur.Output.Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith(op.Target + " ", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.StartsWith(op.Target + "\t", StringComparison.OrdinalIgnoreCase))
                    {
                        rec.ExistedBefore = true;
                        rec.PriorBcdValue = trimmed[op.Target.Length..].Trim();
                        break;
                    }
                }
                if (op.Kind == TweakOpKind.BcdSet)
                    RunProcess("bcdedit.exe", $"/set {{current}} {op.Target} {op.ValueName}", 8000);
                else
                    RunProcess("bcdedit.exe", $"/deletevalue {{current}} {op.Target}", 8000);
                break;
            }

            case TweakOpKind.ShellCommand:
            {
                if (!string.IsNullOrWhiteSpace(op.ApplyCmd))
                    RunPowerShell(op.ApplyCmd, 35_000);
                break;
            }
        }

        return rec;
    }

    private static void UndoRecord(JournalRecord rec)
    {
        switch (rec.Kind)
        {
            case TweakOpKind.RegSet:
            case TweakOpKind.RegDelete:
            {
                var (root, sub) = SplitRegistryPath(rec.Target);
                if (!rec.ExistedBefore)
                {
                    using var k = root.OpenSubKey(sub, true);
                    k?.DeleteValue(rec.ValueName, false);
                }
                else if (rec.PriorSerialized != null && Enum.TryParse<RegistryValueKind>(rec.PriorRegKind, out var vk))
                {
                    using var k = root.CreateSubKey(sub, true);
                    object? val = DeserializeRegValue(rec.PriorSerialized, vk);
                    if (k != null && val != null) k.SetValue(rec.ValueName, val, vk);
                }
                break;
            }

            case TweakOpKind.ServiceStartMode:
            {
                if (rec.ExistedBefore && rec.PriorServiceStart.HasValue)
                {
                    using var k = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{rec.Target}", true);
                    k?.SetValue("Start", rec.PriorServiceStart.Value, RegistryValueKind.DWord);
                    if (rec.PriorServiceStart.Value == 2)
                        RunProcess("sc.exe", $"start \"{rec.Target}\"", 8000);
                }
                break;
            }

            case TweakOpKind.ScheduledTask:
            {
                if (rec.ExistedBefore && rec.PriorTaskEnabled.HasValue)
                    RunProcess("schtasks.exe", $"/Change /TN \"{rec.Target}\" {(rec.PriorTaskEnabled.Value ? "/Enable" : "/Disable")}", 10000);
                break;
            }

            case TweakOpKind.BcdSet:
            case TweakOpKind.BcdDelete:
            {
                if (rec.ExistedBefore && !string.IsNullOrWhiteSpace(rec.PriorBcdValue))
                    RunProcess("bcdedit.exe", $"/set {{current}} {rec.Target} {rec.PriorBcdValue}", 8000);
                else
                    RunProcess("bcdedit.exe", $"/deletevalue {{current}} {rec.Target}", 8000);
                break;
            }

            case TweakOpKind.ShellCommand:
            {
                if (!string.IsNullOrWhiteSpace(rec.UndoCmd))
                    RunPowerShell(rec.UndoCmd, 35_000);
                break;
            }
        }
    }

    public static (RegistryKey Root, string SubKey) SplitRegistryPath(string fullPath)
    {
        string p = fullPath.Trim().Replace('/', '\\');
        int slash = p.IndexOf('\\');
        string hive = slash > 0 ? p[..slash].ToUpperInvariant() : p.ToUpperInvariant();
        string sub = slash > 0 ? p[(slash + 1)..] : "";
        RegistryKey root = hive switch
        {
            "HKLM" or "HKEY_LOCAL_MACHINE" => Registry.LocalMachine,
            "HKCU" or "HKEY_CURRENT_USER" => Registry.CurrentUser,
            "HKCR" or "HKEY_CLASSES_ROOT" => Registry.ClassesRoot,
            "HKU" or "HKEY_USERS" => Registry.Users,
            _ => Registry.CurrentUser
        };
        return (root, sub);
    }

    public static object? ReadReg(string fullPath, string valueName)
    {
        try
        {
            var (root, sub) = SplitRegistryPath(fullPath);
            using var k = root.OpenSubKey(sub, false);
            return k?.GetValue(valueName);
        }
        catch { return null; }
    }

    public static int? ReadServiceStart(string serviceName)
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}", false);
            object? v = k?.GetValue("Start");
            return v != null ? Convert.ToInt32(v) : null;
        }
        catch { return null; }
    }

    private static string SerializeRegValue(object val, RegistryValueKind kind) => kind switch
    {
        RegistryValueKind.Binary when val is byte[] b => Convert.ToBase64String(b),
        RegistryValueKind.MultiString when val is string[] arr => JsonSerializer.Serialize(arr),
        _ => Convert.ToString(val, System.Globalization.CultureInfo.InvariantCulture) ?? ""
    };

    private static object? DeserializeRegValue(string raw, RegistryValueKind kind) => kind switch
    {
        RegistryValueKind.DWord => int.TryParse(raw, out int d) ? d : 0,
        RegistryValueKind.QWord => long.TryParse(raw, out long q) ? q : 0L,
        RegistryValueKind.Binary => Convert.FromBase64String(raw),
        RegistryValueKind.MultiString => JsonSerializer.Deserialize<string[]>(raw) ?? Array.Empty<string>(),
        _ => raw
    };

    private static string SafeFileName(string id)
    {
        var sb = new StringBuilder(id.Length);
        foreach (char c in id)
            sb.Append(char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '_');
        return sb.ToString();
    }

    public static (int ExitCode, string Output, string Error) RunProcess(string exe, string args, int timeoutMs = 20_000)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo(exe, args)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            if (p == null) return (-1, "", "Falha ao iniciar processo");
            var outTask = p.StandardOutput.ReadToEndAsync();
            var errTask = p.StandardError.ReadToEndAsync();
            if (!p.WaitForExit(timeoutMs))
            {
                try { p.Kill(true); } catch { }
                return (-2, "", "Timeout");
            }
            return (p.ExitCode, outTask.Result, errTask.Result);
        }
        catch (Exception ex) { return (-1, "", ex.Message); }
    }

    public static (int ExitCode, string Output, string Error) RunPowerShell(string script, int timeoutMs = 30_000)
    {
        string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        return RunProcess("powershell.exe", $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand {encoded}", timeoutMs);
    }
}
