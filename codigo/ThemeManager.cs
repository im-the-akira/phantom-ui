using System.Windows;
using System.Windows.Media;

namespace PhantomUI;

/// <summary>
/// Gerencia a paleta Dark Gamer Modern (padrão) e Modo Claro do PHANTOM UI.
/// Todos os pincéis são referenciados por DynamicResource para atualização instantânea.
/// </summary>
public static class ThemeManager
{
    public static bool IsLight { get; private set; }

    private static readonly (object Key, string Dark, string Light)[] Palette =
    {
        ("Bg", "#07090F", "#F4F6FA"),
        ("Panel", "#0C101A", "#EAEFF7"),
        ("Card", "#111724", "#FFFFFF"),
        ("Hover", "#182235", "#E2E8F0"),
        ("Line", "#1E293B", "#CBD5E1"),
        ("LineStrong", "#334155", "#94A3B8"),
        ("Fg", "#F8FAFC", "#0F172A"),
        ("Muted", "#94A3B8", "#475569"),
        ("Faint", "#64748B", "#64748B"),
        ("Ink", "#05080F", "#FFFFFF"),
        ("Accent", "#00E5FF", "#0284C7"),
        ("AccentHover", "#33ECFF", "#0369A1"),
        ("AccentDim", "#0F2A38", "#E0F2FE"),
        ("AccentViolet", "#7C4DFF", "#6D28D9"),
        ("RiskLow", "#00E676", "#16A34A"),
        ("RiskMed", "#FFB300", "#D97706"),
        ("RiskHigh", "#FF5252", "#DC2626"),
        ("RiskCrit", "#FF1744", "#B91C1C"),
        ("RiskBlocked", "#C084FC", "#7E22CE"),
        ("ThumbBrush", "#263348", "#CBD5E1"),
        ("ThumbHover", "#475569", "#94A3B8"),
        ("ThumbDrag", "#00E5FF", "#0284C7"),
        (SystemColors.ControlBrushKey, "#07090F", "#F4F6FA"),
        (SystemColors.HighlightBrushKey, "#182235", "#E2E8F0"),
        (SystemColors.InactiveSelectionHighlightBrushKey, "#182235", "#E2E8F0"),
    };

    public static void Apply(bool light)
    {
        IsLight = light;
        var res = Application.Current.Resources;
        foreach (var (key, dark, lightHex) in Palette)
        {
            var color = (Color)ColorConverter.ConvertFromString(light ? lightHex : dark);
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            res[key] = brush;
        }
    }
}
