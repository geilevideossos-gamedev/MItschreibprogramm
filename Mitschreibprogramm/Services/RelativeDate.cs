using System.Globalization;

namespace Mitschreibprogramm.Services;

public static class RelativeDate
{
    // "heute 10:04", "gestern", "28.09.", with the year only when it is not the current one.
    public static string Format(DateTime value, DateTime now)
    {
        if (value.Date == now.Date)
        {
            return "heute " + value.ToString("HH:mm", CultureInfo.InvariantCulture);
        }

        if (value.Date == now.Date.AddDays(-1))
        {
            return "gestern";
        }

        return value.ToString(value.Year == now.Year ? "dd.MM." : "dd.MM.yyyy", CultureInfo.InvariantCulture);
    }
}
