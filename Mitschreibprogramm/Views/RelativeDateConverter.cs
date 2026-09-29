using System.Globalization;
using System.Windows.Data;
using Mitschreibprogramm.Services;

namespace Mitschreibprogramm.Views;

public sealed class RelativeDateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is DateTimeOffset modified ? RelativeDate.Format(modified.LocalDateTime, DateTime.Now) : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
