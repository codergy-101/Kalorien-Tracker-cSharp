using System.Globalization;
using System.Windows.Data;

namespace Kalorien_Tracker
{
    public class ProgressBarWidthConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double progress)
            {
                return progress * 5; // Adjust the multiplier as needed
            }

            return 0;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}