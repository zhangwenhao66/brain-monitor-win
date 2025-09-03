using System;
using System.Globalization;
using System.Windows.Data;

namespace BrainMirror.Converters
{
    public class DateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                // 将UTC时间转换为本地时间
                DateTime localTime = dateTime.ToLocalTime();
                
                // 如果提供了格式参数，使用指定的格式
                if (parameter is string format)
                {
                    return localTime.ToString(format, culture);
                }
                
                // 默认格式
                return localTime.ToString("yyyy-MM-dd HH:mm", culture);
            }
            
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
