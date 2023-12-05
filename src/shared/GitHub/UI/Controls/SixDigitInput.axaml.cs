using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using GitCredentialManager;
using GitCredentialManager.UI.Controls;

namespace GitHub.UI.Controls
{
    public partial class SixDigitInput : UserControl, IFocusable
    {
        public static readonly DirectProperty<SixDigitInput, string> TextProperty =
            AvaloniaProperty.RegisterDirect<SixDigitInput, string>(
                nameof(Text),
                o => o.Text,
                (o, v) => o.Text = v,
                defaultBindingMode: BindingMode.TwoWay);

        private bool _ignoreTextBoxUpdate;
        private TextBox[] _textBoxes;
        private string _text;

        public SixDigitInput()
        {
            InitializeComponent();

            _textBoxes = new[]
            {
                _one,
                _two,
                _three,
                _four,
                _five,
                _six,
            };

            foreach (TextBox textBox in _textBoxes)
            {
                SetUpTextBox(textBox);
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                SetAndRaise(TextProperty, ref _text, value);
                if (!_ignoreTextBoxUpdate) SetTextBoxes(value);
            }
        }

        private void SetTextBoxes(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                foreach (TextBox textBox in _textBoxes)
                {
                    textBox.Text = string.Empty;
                }
            }
            else
            {
                IEnumerable<char> digits = text.Where(char.IsDigit);
                string digitsStr = string.Join(string.Empty, digits).PadRight(6);
                for (int i = 0; i < digitsStr.Length; i++)
                {
                    _textBoxes[i].Text = digitsStr.Substring(i, 1);
                }
            }
        }

        public void SetFocus()
        {
            // Workaround: https://github.com/git-ecosystem/git-credential-manager/issues/1293
            if (!PlatformUtils.IsMacOS())
                _textBoxes[0].Focus(NavigationMethod.Tab, KeyModifiers.None);
        }

        private void SetUpTextBox(TextBox textBox)
        {
            textBox.AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);

            void OnPreviewKeyDown(object sender, KeyEventArgs e)
            {
                // Handle paste
                if (TopLevel.GetTopLevel(this)?.PlatformSettings?.HotkeyConfiguration.Paste.Any(x => x.Matches(e)) ?? false)
                {
                    OnPaste();
                    e.Handled = true;
                }
                // Handle keyboard navigation
                else if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Back)
                {
                    e.Handled = e.Key == Key.Right ? MoveNext() : MovePrevious();
                    if (e.Key == Key.Back)
                    {
                        textBox.Text = string.Empty;
                    }
                }
                // Only allow 0-9, Tab, Escape, and Delete
                else if (e.Key != Key.D0 &&
                         e.Key != Key.D1 &&
                         e.Key != Key.D2 &&
                         e.Key != Key.D3 &&
                         e.Key != Key.D4 &&
                         e.Key != Key.D5 &&
                         e.Key != Key.D6 &&
                         e.Key != Key.D7 &&
                         e.Key != Key.D8 &&
                         e.Key != Key.D9 &&
                         e.Key != Key.NumPad0 &&
                         e.Key != Key.NumPad1 &&
                         e.Key != Key.NumPad2 &&
                         e.Key != Key.NumPad3 &&
                         e.Key != Key.NumPad4 &&
                         e.Key != Key.NumPad5 &&
                         e.Key != Key.NumPad6 &&
                         e.Key != Key.NumPad7 &&
                         e.Key != Key.NumPad8 &&
                         e.Key != Key.NumPad9 &&
                         e.Key != Key.Tab &&
                         e.Key != Key.Escape &&
                         e.Key != Key.Delete)
                {
                    e.Handled = true;
                }
            };

            textBox.PropertyChanged += (s, e) =>
            {
                if (e.Property.Name == nameof(TextBox.Text))
                {
                    try
                    {
                        _ignoreTextBoxUpdate = true;
                        Text = string.Join(string.Empty, _textBoxes.Select(x => x.Text));
                    }
                    finally
                    {
                        _ignoreTextBoxUpdate = false;
                    }

                    if (e.NewValue is string value && value.Length > 0)
                    {
                        MoveNext();
                    }
                }
            };
        }

        private void OnPaste()
        {
            Text = TopLevel.GetTopLevel(this)?.Clipboard?.GetTextAsync().GetAwaiter().GetResult();
        }

        private bool MoveNext() => MoveFocus(true);

        private bool MovePrevious() => MoveFocus(false);

        private bool MoveFocus(bool next)
        {
            // Get currently focused text box
            if (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is TextBox textBox)
            {
                int textBoxIndex = Array.IndexOf(_textBoxes, textBox);
                if (textBoxIndex > -1)
                {
                    int nextIndex = next
                        ? Math.Min(_textBoxes.Length - 1, textBoxIndex + 1)
                        : Math.Max(0, textBoxIndex - 1);

                    _textBoxes[nextIndex].Focus(NavigationMethod.Tab, KeyModifiers.None);
                    return true;
                }
            }

            return false;
        }
    }
}
