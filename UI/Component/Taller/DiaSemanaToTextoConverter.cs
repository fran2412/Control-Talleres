using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ControlTalleresMVP.UI.Component.Taller
{
    public class DiaSemanaToTextoConverter: IValueConverter
    {
        private static readonly Dictionary<DayOfWeek, string> Nombres = new()
        {
            [DayOfWeek.Monday] = "Lunes",
            [DayOfWeek.Tuesday] = "Martes",
            [DayOfWeek.Wednesday] = "Miércoles",
            [DayOfWeek.Thursday] = "Jueves",
            [DayOfWeek.Friday] = "Viernes",
            [DayOfWeek.Saturday] = "Sábado",
            [DayOfWeek.Sunday] = "Domingo",
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is DayOfWeek d ? Nombres[d] : value?.ToString() ?? "";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Nombres.FirstOrDefault(kv => kv.Value.Equals(value?.ToString(), StringComparison.OrdinalIgnoreCase)).Key;
    }
}
