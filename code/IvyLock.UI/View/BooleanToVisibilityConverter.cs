using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace IvyLock.View
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        private object GetVisibility(object value)
        {
            if (value is bool b)
                return b ? Visibility.Visible : Visibility.Collapsed;
            else return Visibility.Visible;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo language)
        {
            return GetVisibility(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo language)
        {
            throw new NotImplementedException();
        }
    }
}