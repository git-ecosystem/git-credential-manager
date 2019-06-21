// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace GitHub.UI.Controls
{
    public class PromptTextBox : TextBox
    {
        public static readonly DependencyProperty PromptTextProperty =
            DependencyProperty.Register(nameof(PromptText), typeof(string), typeof(PromptTextBox), new UIPropertyMetadata(""));

        [Localizability(LocalizationCategory.Text)]
        [DefaultValue("")]
        public string PromptText
        {
            get => (string)GetValue(PromptTextProperty);
            set => SetValue(PromptTextProperty, value);
        }
    }
}
