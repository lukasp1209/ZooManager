using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ZooManager.UI.Converters
{
    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }
        public bool CollapseWhenFalse { get; set; } = true;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var flag = value is bool b && b;
            if (Invert) flag = !flag;

            if (flag) return Visibility.Visible;
            return CollapseWhenFalse ? Visibility.Collapsed : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Visibility v)
                return Binding.DoNothing;

            var flag = v == Visibility.Visible;
            return Invert ? !flag : flag;
        }
    }
}