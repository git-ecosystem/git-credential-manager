// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace GitHub.UI.Converters
{
    /// <summary>
    /// Serves as a base class for value converters (IValueConverter) which are also markup
    /// extensions (MarkupExtension).
    /// </summary>
    /// <remarks>
    /// I was curious why our value converters are all markup extensions and then I found this blog
    /// post:
    /// http://drwpf.com/blog/2009/03/17/tips-and-tricks-making-value-converters-more-accessible-in-markup/
    /// Very clever! One of the comments suggested a base class, so I went and wrote my own without
    /// looking at theirs because I know mine will be better. ;)
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public abstract class ValueConverterMarkupExtension<T> : MarkupExtension, IValueConverter where T : class, IValueConverter, new()
    {
        private static T _converter;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _converter = _converter ?? new T();
        }

        public abstract object Convert(object value, Type targetType, object parameter, CultureInfo culture);

        /// <summary>
        /// Only override this if this converter might be used with 2-way data binding.
        /// </summary>
        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
