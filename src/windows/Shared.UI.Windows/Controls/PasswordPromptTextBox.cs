using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.Git.CredentialManager.UI.Controls
{
    public class PasswordPromptTextBox : PromptTextBox
    {
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.RegisterAttached(nameof(Password), typeof(string), typeof(PasswordPromptTextBox));

        private const char MaskChar = '●';

        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set
            {
                if (!StringComparer.Ordinal.Equals(Password, value))
                {
                    SetValue(PasswordProperty, value);
                    UpdateText();
                }
            }
        }

        private bool _updatingTextFromPasswordChange;

        private void UpdateText()
        {
            _updatingTextFromPasswordChange = true;
            try
            {
                Text = new string(MaskChar, Password?.Length ?? 0);
            }
            finally
            {
                _updatingTextFromPasswordChange = false;
            }
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            if (!_updatingTextFromPasswordChange)
            {
                var password = new StringBuilder(Password);
                int selectionStart = SelectionStart;
                foreach (TextChange change in e.Changes)
                {
                    password.Remove(change.Offset, change.RemovedLength);
                    password.Insert(change.Offset, Text.Substring(change.Offset, change.AddedLength));
                }
                Password = password.ToString();
                UpdateText();
                SelectionStart = selectionStart;
            }
            base.OnTextChanged(e);
        }
    }
}
