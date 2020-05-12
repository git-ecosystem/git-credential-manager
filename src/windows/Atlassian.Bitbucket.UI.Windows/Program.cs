// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.UI;

namespace Atlassian.Bitbucket.UI
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            IGui gui = new Gui();

            try
            {
                // Show test UI when given no arguments
                if (args.Length == 0)
                {
                    gui.ShowDialogWindow(() => new Tester());
                }
                else
                {
                    var prompts = new AuthenticationPrompts(gui);
                    var resultDict = new Dictionary<string, string>();

                    if (StringComparer.OrdinalIgnoreCase.Equals(args[0], "userpass"))
                    {
                        string username = CommandLineUtils.GetParameter(args, "--username");
                        if (prompts.ShowCredentialsPrompt(ref username, out string password))
                        {
                            resultDict["username"] = username;
                            resultDict["password"] = password;
                        }
                        else
                        {
                            throw new OperationCanceledException("authentication prompt was canceled");
                        }
                    }
                    else if (StringComparer.OrdinalIgnoreCase.Equals(args[0], "oauth"))
                    {
                        if (!prompts.ShowOAuthPrompt())
                        {
                            throw new OperationCanceledException("authentication prompt was canceled");
                        }

                        resultDict["continue"] = "1";
                    }
                    else
                    {
                        throw new Exception($"unknown argument '{args[0]}'");
                    }

                    Console.Out.WriteDictionary(resultDict);
                }
            }
            catch (Exception e)
            {
                Console.Out.WriteDictionary(new Dictionary<string, string>
                {
                    ["error"] = e.Message
                });
                Environment.Exit(-1);
            }
        }
    }
}
