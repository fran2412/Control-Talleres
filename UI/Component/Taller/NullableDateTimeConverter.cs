using System;
using System.Globalization;
using System.Windows.Data;

namespace ControlTalleresMVP.UI.Component.Taller
{
    public class NullableDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                return dateTime.ToString("dd/MM/yyyy");
            }
            return "Sin fecha";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
