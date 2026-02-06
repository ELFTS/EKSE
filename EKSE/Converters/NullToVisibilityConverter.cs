using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EKSE.Converters
{
    /// <summary>
    /// 将 null 值转换为 Visibility 的转换器
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 转换逻辑：null 返回 Collapsed，非 null 返回 Visible
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// 反向转换（不支持）
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
