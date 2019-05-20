// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Globalization;
using System.Windows;

namespace GitHub.UI.Converters
{
    [Localizability(LocalizationCategory.NeverLocalize)]
    public sealed class BooleanToHiddenVisibilityConverter : ValueConverterMarkupExtension<BooleanToHiddenVisibilityConverter>
    {
        public override object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return value is bool b && b ? Visibility.Visible : Visibility.Hidden;
        }

        public override object ConvertBack(object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return value is Visibility v && v == Visibility.Visible;
        }
    }
}
