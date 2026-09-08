using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace FKP41
{
    public class TimeSpanConveter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            TimeSpan timeSpan = (TimeSpan)value;
            if (timeSpan.TotalMinutes >= 10)
            {
                return $"{(uint)timeSpan.TotalMinutes} min";
            }
            else if (timeSpan.TotalMinutes >= 1)
            {
                return $"{timeSpan.TotalMinutes.ToString("F1", culture)} min";
            }
            else if (timeSpan.TotalSeconds < 1)
            {
                return $"{timeSpan.TotalMilliseconds.ToString("F0", culture)} ms";
            }
            else
            {
                return $"{timeSpan.TotalSeconds.ToString("F1", culture)} s";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
