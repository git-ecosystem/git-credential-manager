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
                        bool showAll    = CommandLineUtils.TryGetSwitch(args, "--all");
                        bool showBasic  = CommandLineUtils.TryGetSwitch(args, "--basic") || showAll;
                        bool showOAuth  = CommandLineUtils.TryGetSwitch(args, "--oauth") || showAll;
                        bool showPat    = CommandLineUtils.TryGetSwitch(args, "--pat")   || showAll;
                        string username = CommandLineUtils.GetParameter(args, "--username");

                        if (!showBasic && !showOAuth && !showPat && !showAll)
                        {
                            throw new Exception("at least one authentication mode must be specified");
                        }

                        var result = prompts.ShowCredentialPrompt(
                            enterpriseUrl, showBasic, showOAuth, showPat,
                            ref username, out string password, out string token);

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

                            case CredentialPromptResult.PersonalAccessToken:
                                resultDict["mode"] = "pat";
                                resultDict["pat"] = token;
                                break;

                            case CredentialPromptResult.Cancel:
                                throw new OperationCanceledException("authentication prompt was canceled");

                            default:
                                throw new ArgumentOutOfRangeException();
                        }
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
