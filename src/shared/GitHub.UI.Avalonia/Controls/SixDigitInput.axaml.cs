using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using GitCredentialManager.UI.Controls;

namespace GitHub.UI.Controls
{
    public class SixDigitInput : UserControl, IFocusable
    {
        public static readonly DirectProperty<SixDigitInput, string> TextProperty =
            AvaloniaProperty.RegisterDirect<SixDigitInput, string>(
                nameof(Text),
                o => o.Text,
                (o, v) => o.Text = v,
                defaultBindingMode: BindingMode.TwoWay);

        private PlatformHotkeyConfiguration _keyMap;
        private IClipboard _clipboard;
        private bool _ignoreTextBoxUpdate;
        private TextBox[] _textBoxes;
        private string _text;

        public SixDigitInput()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _keyMap = AvaloniaLocator.Current.GetService<PlatformHotkeyConfiguration>();
            _clipboard = AvaloniaLocator.Current.GetService<IClipboard>();
            _textBoxes = new[]
            {
                this.FindControl<TextBox>("one"),
                this.FindControl<TextBox>("two"),
                this.FindControl<TextBox>("three"),
                this.FindControl<TextBox>("four"),
                this.FindControl<TextBox>("five"),
                this.FindControl<TextBox>("six"),
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
            KeyboardDevice.Instance.SetFocusedElement(_textBoxes[0], NavigationMethod.Tab, KeyModifiers.None);
        }

        private void SetUpTextBox(TextBox textBox)
        {
            textBox.AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);

            void OnPreviewKeyDown(object sender, KeyEventArgs e)
            {
                // Handle paste
                if (_keyMap.Paste.Any(x => x.Matches(e)))
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
            string text = _clipboard.GetTextAsync().GetAwaiter().GetResult();
            Text = text;
        }

        private bool MoveNext() => MoveFocus(true);

        private bool MovePrevious() => MoveFocus(false);

        private bool MoveFocus(bool next)
        {
            // Get currently focused text box
            if (FocusManager.Instance.Current is TextBox textBox)
            {
                int textBoxIndex = Array.IndexOf(_textBoxes, textBox);
                if (textBoxIndex > -1)
                {
                    int nextIndex = next
                        ? Math.Min(_textBoxes.Length - 1, textBoxIndex + 1)
                        : Math.Max(0, textBoxIndex - 1);

                    KeyboardDevice.Instance.SetFocusedElement(_textBoxes[nextIndex], NavigationMethod.Tab, KeyModifiers.None);
                    return true;
                }
            }

            return false;
        }
    }
}
