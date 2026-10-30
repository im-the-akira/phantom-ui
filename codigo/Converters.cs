using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PhantomUI;

/// <summary>Converte uma cor em texto hexadecimal (#RRGGBB) para um Brush congelado, usado nos badges de risco/severidade.</summary>
public sealed class ColorBrushConverter : IValueConverter
{
    private static readonly Dictionary<string, Brush> Cache = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string hex = value as string ?? "#6B7280";
        lock (Cache)
        {
            if (Cache.TryGetValue(hex, out var brush)) return brush;
            try
            {
                brush = (Brush)new BrushConverter().ConvertFromString(hex)!;
                brush.Freeze();
            }
            catch { brush = Brushes.Gray; }
            Cache[hex] = brush;
            return brush;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
