// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GitHub.UI.Controls
{
    public abstract class DialogUserControl : UserControl
    {
        protected DialogUserControl()
        {
            IsVisibleChanged += (s, e) =>
            {
                if (IsVisible)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(SetFocus));
                }
            };
        }

        protected abstract void SetFocus();
    }
}
