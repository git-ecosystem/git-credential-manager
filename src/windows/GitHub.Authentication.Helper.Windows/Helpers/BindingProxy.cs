// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows;

namespace GitHub.Authentication.Helper.Helpers
{
    // http://www.thomaslevesque.com/2011/03/21/wpf-how-to-bind-to-data-when-the-datacontext-is-not-inherited/
    public class BindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        public object Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "It'll be fiiiiine")]
        public override string ToString()
        {
            return "BindingProxy: " + Convert.ToString(Data, CultureInfo.CurrentCulture);
        }

        // Using a DependencyProperty as the backing store for Data. This enables animation, styling,
        // binding, etc...
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));
    }
}
