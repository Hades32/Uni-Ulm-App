using System;

namespace UniUlmApp
{
    /// <summary>
    /// This converter converts a DateTime object to a week day name
    /// </summary>
    public class WeekDayNameConverter : System.Windows.Data.IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object parameter, System.Globalization.CultureInfo culture)
        {
            var date = (DateTime)value;
            return date.ToString("dddd");
        }

        public Object ConvertBack(Object value, Type targetType, Object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// This converter converts a DateTime object to short date
    /// </summary>
    public class ShortDateConverter : System.Windows.Data.IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object parameter, System.Globalization.CultureInfo culture)
        {
            var date = (DateTime)value;
            return date.ToShortDateString();
        }

        public Object ConvertBack(Object value, Type targetType, Object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
