using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace NullSoftware.ToolKit.Converters
{
    [ValueConversion(typeof(string), typeof(Stream))]
    public class PathToResourceStreamConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string format = parameter as string ?? "pack://application:,,,/{0}";

            Uri uri = new Uri(string.Format(format, value));

            return Application.GetResourceStream(uri).Stream;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}