// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Diagnostics;
using System.Windows.Input;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.UI;
using Microsoft.Git.CredentialManager.UI.ViewModels;

namespace Atlassian.Bitbucket.UI.ViewModels
{
    /// <summary>
    /// The ViewModel behind the OAuth UI prompt
    /// </summary>
    public class OAuthViewModel : WindowViewModel
    {
        public OAuthViewModel()
        {
            OkCommand = new RelayCommand(Accept);
            CancelCommand = new RelayCommand(Cancel);
            LearnMoreCommand = new RelayCommand(() => OpenDefaultBrowser(BitbucketResources.TwoFactorLearnMoreLinkUrl));
            ForgotPasswordCommand = new RelayCommand(() => OpenDefaultBrowser(BitbucketResources.PasswordResetUrl));
            SignUpCommand = new RelayCommand(() => OpenDefaultBrowser(BitbucketResources.SignUpLinkUrl));
        }

        public override string Title => BitbucketResources.OAuthWindowTitle;

        public override bool IsValid => true;

        /// <summary>
        /// Provides a link to Bitbucket OAuth documentation
        /// </summary>
        public ICommand LearnMoreCommand { get; }

        /// <summary>
        /// Hyperlink to the Bitbucket forgotten password process.
        /// </summary>
        public ICommand ForgotPasswordCommand { get; }

        /// <summary>
        /// Hyperlink to the Bitbucket sign up process.
        /// </summary>
        public ICommand SignUpCommand { get; }

        /// <summary>
        /// Run the OAuth dance.
        /// </summary>
        public ICommand OkCommand { get; }

        /// <summary>
        /// Cancel the authentication attempt.
        /// </summary>
        public ICommand CancelCommand { get; }
    }
}
