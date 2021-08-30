using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Microsoft.Git.CredentialManager.UI.Converters
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConverterHelper.GetConditionalVisibility((bool)value, parameter);
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
