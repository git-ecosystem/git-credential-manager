using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data.Converters;

namespace GitCredentialManager.UI.Converters
{
    public static class BoolConvertersEx
    {
        public static readonly IValueConverter ToThickness = new BoolToThicknessConverter(); 

        public static readonly IMultiValueConverter Or =
            new FuncMultiValueConverter<bool,bool>(x => x.Aggregate(false, (a, b) => a || b));

        public static readonly IMultiValueConverter And =
            new FuncMultiValueConverter<bool,bool>(x => x.Aggregate(true, (a, b) => a && b));

        private class BoolToThicknessConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is not bool b)
                {
                    return null;
                }

                if (parameter is int i)
                {
                    return b ? new Thickness(i) : new Thickness(0);
                }

                return b ? new Thickness(1) : new Thickness(0);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return null;
            }
        }
    }
}
