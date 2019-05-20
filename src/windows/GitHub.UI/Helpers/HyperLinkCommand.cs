// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Diagnostics;

namespace GitHub.UI.Helpers
{
    /// <summary>
    /// Command that opens a browser to the URL specified by the command parameter.
    /// </summary>
    public class HyperLinkCommand : ActionCommand
    {
        public HyperLinkCommand() : base(ExecuteNavigateUrl)
        {
        }

        private static void ExecuteNavigateUrl(object parameter)
        {
            var commandParameter = parameter as string;
            if (string.IsNullOrWhiteSpace(commandParameter))
            {
                return;
            }

            if (Uri.TryCreate(commandParameter, UriKind.Absolute, out Uri navigateUrl))
            {
                Process.Start(new ProcessStartInfo(navigateUrl.AbsoluteUri));
            }
        }
    }
}
