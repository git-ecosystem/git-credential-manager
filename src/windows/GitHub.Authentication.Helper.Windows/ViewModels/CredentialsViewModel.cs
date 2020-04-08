// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Windows.Input;
using GitHub.Authentication.Helper.Properties;
using GitHub.UI.Helpers;
using GitHub.UI.ViewModels;
using GitHub.UI.ViewModels.Validation;

namespace GitHub.Authentication.Helper.ViewModels
{
    public class CredentialsViewModel : DialogViewModel
    {
        public CredentialsViewModel()
        {
            UseOAuthCommand = new ActionCommand(_ =>
            {
                Result = AuthenticationDialogResult.Ok;
                UseOAuth = true;
            });
            LoginCommand = new ActionCommand(_ => Result = AuthenticationDialogResult.Ok);
            CancelCommand = new ActionCommand(_ => Result = AuthenticationDialogResult.Cancel);

            LoginValidator = PropertyValidator.For(this, x => x.Login)
                .Required(Resources.LoginRequired);

            PasswordValidator = PropertyValidator.For(this, x => x.Password)
                .Required(Resources.PasswordRequired);

            ModelValidator = new ModelValidator(LoginValidator, PasswordValidator);
            ModelValidator.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ModelValidator.IsValid))
                {
                    IsValid = ModelValidator.IsValid;
                }
            };
        }

        /// <summary>
        /// Flag to use OAuth
        /// </summary>
        public bool UseOAuth
        {
            get;
            set;
        }

        private string _login;

        /// <summary>
        /// GitHub login which is either the user name or email address.
        /// </summary>
        public string Login
        {
            get { return _login; }
            set
            {
                _login = value;
                RaisePropertyChangedEvent(nameof(Login));
            }
        }

        public PropertyValidator<string> LoginValidator { get; }

        private string _password;

        /// <summary>
        /// GitHub login which is either the user name or email address.
        /// </summary>
        public string Password
        {
            get { return _password; }
            set
            {
                // Hack: Because we're binding one way to source, we need to skip the initial value
                // that's sent when the binding is setup by the XAML
                if (_password == null && value == null) return;
                _password = value;
                RaisePropertyChangedEvent(nameof(Password));
            }
        }

        private bool _showBasic;
        public bool IsBasicVisible
        {
            get => _showBasic;
            set => SetAndRaisePropertyChangedEvent(ref _showBasic, value, nameof(IsBasicVisible));
        }

        private bool _showOAuth;
        public bool IsOAuthVisible
        {
            get => _showOAuth;
            set => SetAndRaisePropertyChangedEvent(ref _showOAuth, value, nameof(IsOAuthVisible));
        }

        public PropertyValidator<string> PasswordValidator { get; }

        public ModelValidator ModelValidator { get; }

        public ICommand UseOAuthCommand { get; }
        public ICommand LoginCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand HyperLinkCommand { get; } = new HyperLinkCommand();
        public ICommand ForgotPasswordCommand { get; } = new HyperLinkCommand();
    }
}
