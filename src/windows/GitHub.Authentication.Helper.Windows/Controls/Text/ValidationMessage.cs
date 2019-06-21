// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GitHub.UI.ViewModels.Validation;

namespace GitHub.Authentication.Helper.Controls.Text
{
    public class ValidationMessage : UserControl
    {
        private const double defaultTextChangeThrottle = 0.2;

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property.Name == nameof(Validator))
            {
                ShowError = Validator.ValidationResult.Status == ValidationStatus.Invalid;
                Visibility = ShowError ? Visibility.Visible : Visibility.Hidden;
                Text = Validator.ValidationResult.Message;

                // This might look like an event handler leak, but we're making sure Validator can
                // only be set once. If we ever want to allow it to be set more than once, we'll need
                // to make sure to unsubscribe this event.
                Validator.PropertyChanged += (s, pce) =>
                {
                    if (pce.PropertyName == nameof(Validator.ValidationResult))
                    {
                        ShowError = Validator.ValidationResult.Status == ValidationStatus.Invalid;
                        Visibility = ShowError ? Visibility.Visible : Visibility.Hidden;
                        Text = Validator.ValidationResult.Message;
                    }
                };
            }

            base.OnPropertyChanged(e);
        }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(ValidationMessage));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            private set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty ShowErrorProperty = DependencyProperty.Register(nameof(ShowError), typeof(bool), typeof(ValidationMessage));

        public bool ShowError
        {
            get { return (bool)GetValue(ShowErrorProperty); }
            set { SetValue(ShowErrorProperty, value); }
        }

        public static readonly DependencyProperty TextChangeThrottleProperty = DependencyProperty.Register(nameof(TextChangeThrottle), typeof(double), typeof(ValidationMessage), new PropertyMetadata(defaultTextChangeThrottle));

        public double TextChangeThrottle
        {
            get { return (double)GetValue(TextChangeThrottleProperty); }
            set { SetValue(TextChangeThrottleProperty, value); }
        }

        public static readonly DependencyProperty ValidatorProperty = DependencyProperty.Register(nameof(Validator), typeof(PropertyValidator), typeof(ValidationMessage));

        public PropertyValidator Validator
        {
            get { return (PropertyValidator)GetValue(ValidatorProperty); }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(ValidatorProperty));
                Debug.Assert(Validator == null, "Only set this property once for now. If we really need it to be set more than once, we need to make sure we're not leaking event handlers");
                SetValue(ValidatorProperty, value);
            }
        }

        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(Octicon), typeof(ValidationMessage), new PropertyMetadata(Octicon.stop));

        public Octicon Icon
        {
            get { return (Octicon)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register(nameof(Fill), typeof(Brush), typeof(ValidationMessage), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0xe7, 0x4c, 0x3c))));

        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        public static readonly DependencyProperty ErrorAdornerTemplateProperty = DependencyProperty.Register(nameof(ErrorAdornerTemplate), typeof(string), typeof(ValidationMessage), new PropertyMetadata("validationTemplate"));

        public string ErrorAdornerTemplate
        {
            get { return (string)GetValue(ErrorAdornerTemplateProperty); }
            set { SetValue(ErrorAdornerTemplateProperty, value); }
        }

        private bool IsAdornerEnabled()
        {
            return !string.IsNullOrEmpty(ErrorAdornerTemplate)
                && !ErrorAdornerTemplate.Equals("None", StringComparison.OrdinalIgnoreCase);
        }
    }
}
