// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Git.CredentialManager;

namespace GitHub.Authentication.Helper
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var gui = new Gui();

            try
            {
                // Show test UI when given no arguments
                if (args.Length == 0)
                {
                    gui.ShowDialogWindow(() => new Tester());
                }
                else
                {
                    IntPtr parentHwnd = GetParentWindowHandle();
                    var prompts = new AuthenticationPrompts(gui, parentHwnd);
                    var resultDict = new Dictionary<string, string>();

                    if (StringComparer.OrdinalIgnoreCase.Equals(args[0], "prompt"))
                    {
                        bool basic = TryGetSwitch(args, "--basic");
                        bool oauth = TryGetSwitch(args, "--oauth");

                        if (!basic && !oauth)
                        {
                            throw new Exception("at least authentication mode must be specified");
                        }

                        var result = prompts.ShowCredentialPrompt(basic, oauth, out string username, out string password);

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
                                Environment.Exit(-1);
                                break;

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
                Console.Out.WriteLine(e.Message);
                Environment.Exit(-1);
            }
        }

        private static IntPtr GetParentWindowHandle()
        {
            string hwndStr = Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.GcmParentWindow);
            return ConvertUtils.TryToInt32(hwndStr, out int ptr) ? new IntPtr(ptr) : IntPtr.Zero;
        }

        private static bool TryGetArgument(string[] args, string name, out string value)
        {
            for (var i = 0; i < args.Length; i++)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(args[i], name) && i + 1 < args.Length)
                {
                    value = args[i + 1];
                    return true;
                }
            }

            value = null;
            return false;
        }

        private static bool TryGetSwitch(string[] args, string name)
        {
            return args.Any(arg => StringComparer.OrdinalIgnoreCase.Equals(arg, name));
        }
    }
}
