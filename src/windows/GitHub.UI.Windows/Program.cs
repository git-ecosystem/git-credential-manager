// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.UI;

namespace GitHub.UI
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            IGui gui;
            if (TryGetParentWindowHandle(out IntPtr parentHwnd))
            {
                gui = new Gui(parentHwnd);
            }
            else
            {
                gui = new Gui();
            }

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

                    if (StringComparer.OrdinalIgnoreCase.Equals(args[0], "prompt"))
                    {
                        string enterpriseUrl = GetParameter(args, "--enterprise-url");
                        bool basic = TryGetSwitch(args, "--basic");
                        bool oauth = TryGetSwitch(args, "--oauth");

                        if (!basic && !oauth)
                        {
                            throw new Exception("at least one authentication mode must be specified");
                        }

                        var result = prompts.ShowCredentialPrompt(
                            enterpriseUrl, basic, oauth,
                            out string username,
                            out string password);

                        switch (result)
                        {
                            case CredentialPromptResult.BasicAuthentication:
                                resultDict["mode"] = "basic";
                                resultDict["username"] = username;
                                resultDict["password"] = password;
                                break;

                            case CredentialPromptResult.OAuthAuthentication:
                                resultDict["mode"] = "oauth";
                                break;

                            case CredentialPromptResult.Cancel:
                                throw new OperationCanceledException("authentication prompt was canceled");

                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    else if (StringComparer.OrdinalIgnoreCase.Equals(args[0], "2fa"))
                    {
                        bool isSms = TryGetSwitch(args, "--sms");

                        if (!prompts.ShowAuthenticationCodePrompt(isSms, out string authCode))
                        {
                            throw new Exception("failed to get authentication code");
                        }

                        resultDict["code"] = authCode;
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

        private static bool TryGetParentWindowHandle(out IntPtr hwnd)
        {
            string envar = Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.GcmParentWindow);

            if (long.TryParse(envar, out long ptrInt))
            {
                hwnd = new IntPtr(ptrInt);
                return true;
            }

            hwnd = default(IntPtr);
            return false;
        }

        private static bool TryGetSwitch(string[] args, string name)
        {
            return args.Any(arg => StringComparer.OrdinalIgnoreCase.Equals(arg, name));
        }

        private static string GetParameter(string[] args, string name)
        {
            int index = Array.FindIndex(args, x => StringComparer.OrdinalIgnoreCase.Equals(x, name));

            if (-1 < index && index + 1 < args.Length)
            {
                return args[index + 1];
            }

            return null;
        }
    }
}
