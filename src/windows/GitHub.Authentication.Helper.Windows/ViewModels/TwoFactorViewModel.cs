// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Windows.Input;
using GitHub.Authentication.Helper.Properties;
using GitHub.UI.Helpers;
using GitHub.UI.ViewModels;

namespace GitHub.Authentication.Helper.ViewModels
{
    /// <summary>
    /// Simple view model for the GitHub Two Factor dialog.
    /// </summary>
    public class TwoFactorViewModel : DialogViewModel
    {
        /// <summary>
        /// This is used by the GitHub.Authentication test application
        /// </summary>
        public TwoFactorViewModel() : this(false) { }

        /// <summary>
        /// This quite obviously creates an instance of a <see cref="TwoFactorViewModel"/>.
        /// </summary>
        /// <param name="isSms">True if the 2fa authentication code is sent via SMS</param>
        public TwoFactorViewModel(bool isSms)
        {
            OkCommand = new ActionCommand(_ => Result = AuthenticationDialogResult.Ok);
            CancelCommand = new ActionCommand(_ => Result = AuthenticationDialogResult.Cancel);

            IsSms = isSms;
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(AuthenticationCode))
                {
                    // We currently rely on the UI to ensure that the authentication code consists of
                    // digits only.
                    IsValid = AuthenticationCode.Length == 6;
                }
            };
        }

        private string _authenticationCode;

        /// <summary>
        /// The Two-factor authentication code the user types in.
        /// </summary>
        public string AuthenticationCode
        {
            get { return _authenticationCode; }
            set
            {
                _authenticationCode = value;
                RaisePropertyChangedEvent(nameof(AuthenticationCode));
            }
        }

        public bool IsSms { get; }

        public string Description
        {
            get
            {
                return IsSms
                    ? Resources.TwoFactorSms
                    : Resources.OpenTwoFactorAuthAppText;
            }
        }

        public ICommand LearnMoreCommand { get; } = new HyperLinkCommand();

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }
    }
}
