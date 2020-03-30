// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace GitHub.UI.Converters
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BooleanAndVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool show = values.Cast<bool>().Aggregate(true, (x, y) => x && y);
            return ConverterHelper.GetConditionalVisibility(show, parameter);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new[] { Binding.DoNothing };
        }
    }
}
