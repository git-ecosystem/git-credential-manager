using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Microsoft.Git.CredentialManager.UI.Converters
{
    [ValueConversion(typeof(string), typeof(Visibility))]
    public class NonEmptyStringToVisibleConverter : IValueConverter
    {
        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConverterHelper.GetConditionalVisibility(!string.IsNullOrEmpty(value as string), parameter);
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
