// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Globalization;
using System.Windows;

namespace GitHub.UI.Converters
{
    [Localizability(LocalizationCategory.NeverLocalize)]
    public sealed class BooleanToVisibilityConverter : ValueConverterMarkupExtension<BooleanToVisibilityConverter>
    {
        private readonly System.Windows.Controls.BooleanToVisibilityConverter _converter = new System.Windows.Controls.BooleanToVisibilityConverter();

        public override object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return _converter.Convert(value, targetType, parameter, culture);
        }

        public override object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return _converter.ConvertBack(value, targetType, parameter, culture);
        }
    }
}
