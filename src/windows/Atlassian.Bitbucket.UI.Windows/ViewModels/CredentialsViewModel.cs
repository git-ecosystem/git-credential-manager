// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Diagnostics;
using System.Security;
using System.Windows.Input;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.UI;
using Microsoft.Git.CredentialManager.UI.ViewModels;
using Microsoft.Git.CredentialManager.UI.ViewModels.Validation;

namespace Atlassian.Bitbucket.UI.ViewModels
{
    /// <summary>
    /// The ViewModel behind the Basic Auth username/password UI prompt.
    /// </summary>
    public class CredentialsViewModel : WindowViewModel
    {
        private readonly ModelValidator _modelValidator = new ModelValidator();

        // Exists for XAML preview
        public CredentialsViewModel() : this(null) { }

        public CredentialsViewModel(string username)
        {
            LoginCommand = new RelayCommand(Accept, () => IsValid);
            CancelCommand = new RelayCommand(Cancel);
            ForgotPasswordCommand = new RelayCommand(() => OpenDefaultBrowser(BitbucketResources.PasswordResetUrl));
            SignUpCommand = new RelayCommand(() => OpenDefaultBrowser(BitbucketResources.SignUpLinkUrl));

            LoginValidator = PropertyValidator.For(this, x => x.Login).Required(BitbucketResources.LoginRequired);
            PasswordValidator = PropertyValidator.For(this, x => x.Password).Required(BitbucketResources.PasswordRequired);

            _modelValidator.Add(LoginValidator);
            _modelValidator.Add(PasswordValidator);
            _modelValidator.IsValidChanged += (s, e) => LoginCommand.RaiseCanExecuteChanged();

            // Set last to allow validator to run
            if (!string.IsNullOrWhiteSpace(username))
            {
                Login = username;
            }
        }

        public override bool IsValid => _modelValidator.IsValid;

        public override string Title => BitbucketResources.CredentialsWindowTitle;

        private string _login;

        /// <summary>
        /// Bitbucket login which is either the user name or email address.
        /// </summary>
        public string Login
        {
            get => _login;
            set => SetAndRaisePropertyChanged(ref _login, value);
        }

        public PropertyValidator<string> LoginValidator { get; }

        private SecureString _password;

        /// <summary>
        /// Bitbucket password.
        /// </summary>
        public SecureString Password
        {
            get => _password;
            set => SetAndRaisePropertyChanged(ref _password, value);
        }

        public PropertyValidator<SecureString> PasswordValidator { get; }

        /// <summary>
        /// Start the process to validate the username/password
        /// </summary>
        public RelayCommand LoginCommand { get; }

        /// <summary>
        /// Cancel the authentication attempt.
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Hyperlink to the Bitbucket forgotten password process.
        /// </summary>
        public ICommand ForgotPasswordCommand { get; }

        /// <summary>
        /// Hyperlink to the Bitbucket sign up process.
        /// </summary>
        public ICommand SignUpCommand { get; }
    }
}
