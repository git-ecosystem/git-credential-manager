using System.Security;
using System.Windows;
using System.Windows.Controls;

namespace GitHub.UI.Controls
{
    /// <summary>
    ///   A SecureString based password PromptTextBox implementation.
    /// </summary>
    public class SecurePasswordBox : PromptTextBox
    {
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.RegisterAttached(nameof(Password), typeof(SecureString), typeof(SecurePasswordBox));

        // Fake char to display in Visual Tree
        private const char pwdChar = '●';

        /// <summary>
        /// Real password value.
        /// </summary>
        /// <remarks>
        /// For more security use System.Security.SecureString type instead
        /// </remarks>
        public SecureString Password
        {
            get
            {
                SecureString result = (SecureString)GetValue(PasswordProperty);
                if (result == null)
                {
                    result = new SecureString();
                    SetValue(PasswordProperty, result);
                }
                return result;
            }
            set
            {
                SecureString currentPassword = Password;
                if (!ReferenceEquals(currentPassword, value))
                {
                    currentPassword.Dispose();

                    SetValue(PasswordProperty, value ?? new SecureString());
                    UpdateText();
                }
            }
        }

        private bool updatingTextFromPasswordChange;
        private void UpdateText()
        {
            updatingTextFromPasswordChange = true;
            try
            {
                Text = new string(pwdChar, Password.Length);
            }
            finally
            {
                updatingTextFromPasswordChange = false;
            }
        }

        /// <summary>
        ///   TextChanged event handler for secure storing of password into Visual Tree,
        ///   text is replaced with pwdChar chars, clean text is kept in
        ///   Text property (CLR property not snoopable without mod)
        /// </summary>
        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            if (!updatingTextFromPasswordChange)
            {
                SecureString password = Password;
                int selectionStart = SelectionStart;
                foreach (TextChange change in e.Changes)
                {
                    password.RemoveAt(change.Offset, change.RemovedLength);
                    password.InsertAt(change.Offset, Text.Substring(change.Offset, change.AddedLength));
                }
                UpdateText();
                SelectionStart = selectionStart;
            }
            base.OnTextChanged(e);
        }
    }
}
