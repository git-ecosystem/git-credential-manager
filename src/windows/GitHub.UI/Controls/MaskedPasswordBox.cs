// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace GitHub.UI.Controls
{
    /// <summary>
    /// A masking TextBox control based on http://blogs.ugidotnet.org/leonardo
    /// </summary>
    /// <remarks>
    /// <para>
    /// You might be wondering why we don't use SecureString. The main reason is that the password
    /// value is passed back to the Git Credential Provider as a normal string. So there's no point
    /// in us using a SecureString here. It needs to be a secure string all the way down to really
    /// make a difference.
    /// </para>
    /// </remarks>
    public class MaskedPasswordBox : PromptTextBox
    {
        // Fake char to display in Visual Tree
        private const char PwdChar = '●';

        // flag used to bypass OnTextChanged
        private bool _dirtyBaseText;

        /// <summary>
        /// Only copy of real password
        /// </summary>
        /// <remarks>For more security use System.Security.SecureString type instead</remarks>
        private string _password = string.Empty;

        /// <summary>
        /// Provide access to base.Text without call OnTextChanged
        /// </summary>
        protected string BaseText
        {
            get { return base.Text; }
            set
            {
                _dirtyBaseText = true;
                base.Text = value;
                _dirtyBaseText = false;
            }
        }

        /// <summary>
        /// Clean Password
        /// </summary>
        public new string Text
        {
            get { return _password; }
            set
            {
                _password = value ?? string.Empty;
                BaseText = new string(PwdChar, _password.Length);
            }
        }

        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register(nameof(Password), typeof(string), typeof(MaskedPasswordBox), new UIPropertyMetadata(null));

        /// <summary>
        /// Copy of real password
        /// </summary>
        [Localizability(LocalizationCategory.Text)]
        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }

        /// <summary>
        /// TextChanged event handler for secure storing of password into Visual Tree, text is
        /// replaced with pwdChar chars, clean text is kept in Text property (CLR property not
        /// snoopable without mod)
        /// </summary>
        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            if (_dirtyBaseText)
                return;

            string currentText = BaseText;

            int selStart = SelectionStart;
            if (_password != null && currentText.Length < _password.Length)
            {
                // Remove deleted chars
                _password = _password.Remove(selStart, _password.Length - currentText.Length);
            }

            if (!string.IsNullOrEmpty(currentText))
            {
                for (int i = 0; i < currentText.Length; i++)
                {
                    if (currentText[i] != PwdChar)
                    {
                        Debug.Assert(_password != null, "Password can't be null here");
                        // Replace or insert char
                        string currentCharacter = currentText[i].ToString(CultureInfo.InvariantCulture);
                        _password = BaseText.Length == _password.Length ? _password.Remove(i, 1).Insert(i, currentCharacter) : _password.Insert(i, currentCharacter);
                    }
                }
                Debug.Assert(_password != null, "Password can't be null here");
                BaseText = new string(PwdChar, _password.Length);
                SelectionStart = selStart;
            }
            Password = _password ?? string.Empty;
            base.OnTextChanged(e);
        }
    }
}
