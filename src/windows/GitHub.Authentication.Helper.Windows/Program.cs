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

                    if (!TryGetArgument(args, "--prompt", out string promptType))
                    {
                        throw new Exception("missing --prompt argument");
                    }
                    bool isSms = TryGetSwitch(args, "--sms");

                    if (StringComparer.OrdinalIgnoreCase.Equals(promptType, "userpass"))
                    {
                        if (!prompts.CredentialModalPrompt(out string username, out string password))
                        {
                            throw new Exception("failed to get username and password");
                        }

                        resultDict["username"] = username;
                        resultDict["password"] = password;
                    }
                    else if (StringComparer.OrdinalIgnoreCase.Equals(promptType, "authcode"))
                    {
                        if (!prompts.AuthenticationCodeModalPrompt(isSms, out string authCode))
                        {
                            throw new Exception("failed to get authentication code");
                        }

                        resultDict["authcode"] = authCode;
                    }
                    else
                    {
                        throw new Exception($"unknown prompt type '{promptType}'");
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
