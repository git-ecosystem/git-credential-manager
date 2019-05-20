// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GitHub.UI.Converters;
using GitHub.UI.Helpers;

namespace GitHub.Authentication.Helper.Controls
{
    public class TwoFactorInputToTextBox : ValueConverterMarkupExtension<TwoFactorInputToTextBox>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is TwoFactorInput ? ((TwoFactorInput)value).TextBox : null;
        }
    }

    /// <summary>
    /// Interaction logic for TwoFactorInput.xaml
    /// </summary>
    public partial class TwoFactorInput : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(TwoFactorInput), new PropertyMetadata(""));

        private TextBox[] TextBoxes;
        public TextBox TextBox { get { return TextBoxes[0]; } }

        public TwoFactorInput()
        {
            InitializeComponent();

            TextBoxes = new[]
            {
                one,
                two,
                three,
                four,
                five,
                six
            };

            foreach (var textBox in TextBoxes)
            {
                SetupTextBox(textBox);
            }
        }

        public Task<bool> TryFocus()
        {
            return one.TryMoveFocus(FocusNavigationDirection.First);
        }

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            var isText = e.SourceDataObject.GetDataPresent(DataFormats.Text, true);
            if (!isText) return;

            var text = e.SourceDataObject.GetData(DataFormats.Text) as string;
            if (text == null) return;
            e.CancelCommand();
            SetText(text);
        }

        private void SetText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                foreach (var textBox in TextBoxes)
                {
                    textBox.Text = "";
                }
                SetValue(TextProperty, text);
                return;
            }
            var digits = text.Where(Char.IsDigit).ToList();
            for (int i = 0; i < Math.Min(6, digits.Count); i++)
            {
                TextBoxes[i].Text = digits[i].ToString();
            }
            SetValue(TextProperty, string.Join("", digits));
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetText(value); }
        }

        private void SetupTextBox(TextBox textBox)
        {
            DataObject.AddPastingHandler(textBox, new DataObjectPastingEventHandler(OnPaste));

            textBox.GotFocus += (sender, args) => textBox.SelectAll();

            textBox.PreviewKeyDown += (sender, args) =>
            {
                // Handle navigation.
                if (args.Key == Key.Left || args.Key == Key.Right || args.Key == Key.Back)
                {
                    args.Handled = args.Key == Key.Right ? MoveNext() : MovePrevious();
                    if (args.Key == Key.Back)
                    {
                        textBox.Text = "";
                    }
                }

                if (args.Key != Key.D0
                    && args.Key != Key.D1
                    && args.Key != Key.D2
                    && args.Key != Key.D3
                    && args.Key != Key.D4
                    && args.Key != Key.D5
                    && args.Key != Key.D6
                    && args.Key != Key.D7
                    && args.Key != Key.D8
                    && args.Key != Key.D9
                    && args.Key != Key.NumPad0
                    && args.Key != Key.NumPad1
                    && args.Key != Key.NumPad2
                    && args.Key != Key.NumPad3
                    && args.Key != Key.NumPad4
                    && args.Key != Key.NumPad5
                    && args.Key != Key.NumPad6
                    && args.Key != Key.NumPad7
                    && args.Key != Key.NumPad8
                    && args.Key != Key.NumPad9
                    && args.Key != Key.Tab
                    && args.Key != Key.Escape
                    && args.Key != Key.Delete
                    && (!(args.Key == Key.V && args.KeyboardDevice.Modifiers == ModifierKeys.Control))
                    && (!(args.Key == Key.Insert && args.KeyboardDevice.Modifiers == ModifierKeys.Shift)))
                {
                    args.Handled = true;
                }
            };

            textBox.SelectionChanged += (sender, args) =>
            {
                // Make sure we can't insert additional text into a textbox. Each textbox should only
                // allow one character.
                if (textBox.SelectionLength == 0 && textBox.Text.Any())
                {
                    textBox.SelectAll();
                }
            };

            textBox.TextChanged += (sender, args) =>
            {
                SetValue(TextProperty, string.Join("", GetTwoFactorCode()));
                var change = args.Changes.FirstOrDefault();
                args.Handled = (change != null && change.AddedLength > 0) && MoveNext();
            };
        }

        private static bool MoveNext()
        {
            return MoveFocus(FocusNavigationDirection.Next);
        }

        private static bool MovePrevious()
        {
            return MoveFocus(FocusNavigationDirection.Previous);
        }

        private static bool MoveFocus(FocusNavigationDirection navigationDirection)
        {
            var traversalRequest = new TraversalRequest(navigationDirection);
            var keyboardFocus = Keyboard.FocusedElement as UIElement;
            if (keyboardFocus != null)
            {
                keyboardFocus.MoveFocus(traversalRequest);
                return true;
            }
            return false;
        }

        private static string GetTextBoxValue(TextBox textBox)
        {
            return string.IsNullOrEmpty(textBox.Text) ? " " : textBox.Text;
        }

        private string GetTwoFactorCode()
        {
            return string.Join("", TextBoxes.Select(textBox => textBox.Text));
        }
    }
}
