using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ControlTalleresMVP.UI.Component.Clases
{
    /// <summary>
    /// Converts progress bar values to width for rounded progress bar template.
    /// </summary>
    public class ProgressBarWidthConverter : IMultiValueConverter
    {
        public static readonly ProgressBarWidthConverter Instance = new();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3)
                return 0.0;

            if (values[0] is not double actualWidth ||
                values[1] is not double value ||
                values[2] is not double maximum)
            {
                // Try parsing as different numeric types
                if (!TryGetDouble(values[0], out actualWidth) ||
                    !TryGetDouble(values[1], out value) ||
                    !TryGetDouble(values[2], out maximum))
                {
                    return 0.0;
                }
            }

            if (maximum <= 0 || actualWidth <= 0)
                return 0.0;

            var ratio = Math.Min(1.0, Math.Max(0.0, value / maximum));
            return actualWidth * ratio;
        }

        private static bool TryGetDouble(object value, out double result)
        {
            result = 0.0;
            if (value == null || value == DependencyProperty.UnsetValue)
                return false;

            if (value is double d) { result = d; return true; }
            if (value is int i) { result = i; return true; }
            if (value is decimal dec) { result = (double)dec; return true; }
            if (value is float f) { result = f; return true; }

            return double.TryParse(value.ToString(), out result);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
