using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Globalization;

namespace GitHub.UI.Controls
{
    /// <summary>
    /// Interaction logic for TwoFactorInput.xaml
    /// </summary>
    public partial class TwoFactorInput : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(TwoFactorInput), new PropertyMetadata(""));

        TextBox[] TextBoxes = null;
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

        public void SetFocus()
        {
            one.Focus();
        }

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (sender == null)
            {
                throw new ArgumentNullException(nameof(sender));
            }
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            var isText = e.SourceDataObject.GetDataPresent(DataFormats.Text, true);
            if (!isText) return;

            var text = e.SourceDataObject.GetData(DataFormats.Text) as string;
            if (text == null) return;
            e.CancelCommand();
            SetText(text);
        }

        void SetText(string text)
        {
            if (String.IsNullOrEmpty(text))
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
                TextBoxes[i].Text = digits[i].ToString(CultureInfo.InvariantCulture);
            }
            SetValue(TextProperty, String.Join("", digits));
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetText(value); }
        }

        private void SetupTextBox(TextBox textBox)
        {
            if (textBox == null)
            {
                throw new ArgumentNullException(nameof(textBox));
            }

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
                // Make sure we can't insert additional text into a textbox.
                // Each textbox should only allow one character.
                if (textBox.SelectionLength == 0 && textBox.Text.Any())
                {
                    textBox.SelectAll();
                }
            };

            textBox.TextChanged += (sender, args) =>
            {
                SetValue(TextProperty, String.Join("", GetTwoFactorCode()));
                var change = args.Changes.FirstOrDefault();
                args.Handled = (change != null && change.AddedLength > 0) && MoveNext();
            };
        }

        bool MoveNext()
        {
            return MoveFocus(FocusNavigationDirection.Next);
        }

        bool MovePrevious()
        {
            return MoveFocus(FocusNavigationDirection.Previous);
        }

        static bool MoveFocus(FocusNavigationDirection navigationDirection)
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
            if (textBox == null)
            {
                throw new ArgumentNullException(nameof(textBox));
            }

            return String.IsNullOrEmpty(textBox.Text) ? " " : textBox.Text;
        }

        private string GetTwoFactorCode()
        {
            return String.Join("", TextBoxes.Select(textBox => textBox.Text));
        }
    }
}
