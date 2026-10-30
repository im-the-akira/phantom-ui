using System.ComponentModel;

namespace PhantomUI;

/// <summary>
/// Sistema bilíngue (PT-BR / EN) em tempo real para o PHANTOM UI.
/// </summary>
public sealed class Loc : INotifyPropertyChanged
{
    public static Loc Instance { get; } = new();

    public static bool IsEn { get; private set; }
    public static event Action? LanguageChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    public static void SetLanguage(string? lang)
    {
        bool en = string.Equals(lang, "en", StringComparison.OrdinalIgnoreCase);
        if (IsEn == en) return;
        IsEn = en;
        Instance.PropertyChanged?.Invoke(Instance, new PropertyChangedEventArgs(string.Empty));
        LanguageChanged?.Invoke();
    }

    public static void Toggle() => SetLanguage(IsEn ? "pt" : "en");

    public static string Pick(string pt, string en) => IsEn ? en : pt;

    public string Code => IsEn ? "EN" : "PT";
    public string LangTooltip => IsEn ? "Switch language to Português (BR)" : "Alternar idioma para English";
}
