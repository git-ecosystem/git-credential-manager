// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;

namespace Microsoft.Git.CredentialManager.UI.ViewModels
{
    public abstract class WindowViewModel : ViewModel
    {
        public abstract string Title { get; }

        public event EventHandler Accepted;
        public event EventHandler Canceled;

        public void Accept()
        {
            Accepted?.Invoke(this, EventArgs.Empty);
        }

        public void Cancel()
        {
            Canceled?.Invoke(this, EventArgs.Empty);
        }
    }
}
