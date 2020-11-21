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
            IGui gui = new Gui();

            try
            {
                // Show test UI when given no arguments
                if (args.Length == 0)
                {
                    gui.ShowWindow(() => new Tester());
                }
                else
                {
                    var prompts = new AuthenticationPrompts(gui);
                    var resultDict = new Dictionary<string, string>();

                    if (StringComparer.OrdinalIgnoreCase.Equals(args[0], "prompt"))
                    {
                        string enterpriseUrl = CommandLineUtils.GetParameter(args, "--enterprise-url");
                        bool passwordLogin = CommandLineUtils.TryGetSwitch(args, "--password");
                        bool patLogin = CommandLineUtils.TryGetSwitch(args, "--pat");
                        bool oauthLogin = CommandLineUtils.TryGetSwitch(args, "--oauth");
                        string username = CommandLineUtils.GetParameter(args, "--username");

                        if (!passwordLogin && !patLogin && !oauthLogin)
                        {
                            throw new Exception("at least one authentication mode must be specified");
                        }

                        var result = prompts.ShowCredentialPrompt(
                            enterpriseUrl, passwordLogin, patLogin, oauthLogin,
                            ref username,
                            out string password);

                        switch (result)
                        {
                            case CredentialPromptResult.Basic:
                            case CredentialPromptResult.Password:
                            case CredentialPromptResult.PAT:
                                resultDict["username"] = username;
                                resultDict["password"] = password;
                                break;

                            case CredentialPromptResult.OAuth:
                                break;

                            case CredentialPromptResult.Cancel:
                                throw new OperationCanceledException("authentication prompt was canceled");

                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        resultDict["mode"] = result.ToString().ToLower();
                    }
                    else if (StringComparer.OrdinalIgnoreCase.Equals(args[0], "2fa"))
                    {
                        bool isSms = CommandLineUtils.TryGetSwitch(args, "--sms");

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
    }
}
