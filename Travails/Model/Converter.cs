using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Travails.Model
{
    public class MyConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            DateTime date;
            if (values.Length == 2 && DateTime.TryParse(values[0].ToString(), out date))
            {
                return new Tuple<DateTime, string>(date, values[0].ToString());
            }

            return values.Clone();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            //throw new NotImplementedException();
            return null;
        }
    }

    internal class MyCloneConverter : IValueConverter
    {
        public static MyCloneConverter Instance = new MyCloneConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Freezable)
            {
                value = (value as Freezable).Clone();
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }
}
