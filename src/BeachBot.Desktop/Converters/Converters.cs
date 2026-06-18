using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BeachBot.Desktop.Converters;

/// <summary>Formats a <see cref="DateTimeOffset"/>/<see cref="DateTime"/> in local time. ConverterParameter is the format string.</summary>
public sealed class LocalDateTimeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var format = parameter as string ?? "dd.MM.yyyy HH:mm";
        return value switch
        {
            DateTimeOffset dto => dto.ToLocalTime().ToString(format, CultureInfo.CurrentCulture),
            DateTime dt => dt.ToLocalTime().ToString(format, CultureInfo.CurrentCulture),
            _ => "—",
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Visible when the string is non-empty, Collapsed otherwise.</summary>
public sealed class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Inverts a boolean (e.g. enable a button only when not busy).</summary>
public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : false;
}
