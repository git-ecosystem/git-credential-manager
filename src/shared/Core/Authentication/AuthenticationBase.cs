using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.UI.ViewModels;

namespace GitCredentialManager.Authentication
{
    public abstract class AuthenticationBase
    {
        protected readonly ICommandContext Context;

        protected AuthenticationBase(ICommandContext context)
        {
            EnsureArgument.NotNull(context, nameof(context));

            Context = context;
        }

        protected Task<IDictionary<string, string>> InvokeHelperAsync(string path, string args,
            StreamReader standardInput = null)
        {
            return InvokeHelperAsync(path, args, standardInput, CancellationToken.None);
        }

        protected internal virtual async Task<IDictionary<string, string>> InvokeHelperAsync(string path, string args,
            StreamReader standardInput, CancellationToken ct)
        {
            var procStartInfo = new ProcessStartInfo(path)
            {
                Arguments = args,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = false, // Do not redirect stderr as tracing might be enabled
                UseShellExecute = false,
                StandardOutputEncoding = EncodingEx.UTF8NoBom,
            };

            Context.Trace.WriteLine($"Starting helper process: {path} {args}");

            // We flush the trace writers here so that the we don't stomp over the
            // authentication helper's messages.
            Context.Trace.Flush();

            var process = ChildProcess.Start(Context.Trace2, procStartInfo, Trace2ProcessClass.UIHelper);
            if (process is null)
            {
                var format = "Failed to start helper process: {0} {1}";
                var message = string.Format(format, path, args);

                throw new Trace2Exception(Context.Trace2, message, format);
            }

            // Kill the process upon a cancellation request
            ct.Register(() => process.Kill());

            // Write the standard input to the process if we have any to write
            if (standardInput is not null)
            {
#if NETFRAMEWORK
                await standardInput.BaseStream.CopyToAsync(process.StandardInput.BaseStream);
#else
                await standardInput.BaseStream.CopyToAsync(process.StandardInput.BaseStream, ct);
#endif
                process.StandardInput.Close();
            }

            IDictionary<string, string> resultDict = await process.StandardOutput.ReadDictionaryAsync(StringComparer.OrdinalIgnoreCase);

            await Task.Run(() => process.WaitForExit(), ct);
            int exitCode = process.ExitCode;

            if (exitCode != 0)
            {
                if (!resultDict.TryGetValue("error", out string errorMessage))
                {
                    errorMessage = "Unknown";
                }

                throw new Trace2Exception(Context.Trace2, $"helper error ({exitCode}): {errorMessage}");
            }

            return resultDict;
        }

        protected void ThrowIfUserInteractionDisabled()
        {
            if (!Context.Settings.IsInteractionAllowed)
            {
                string envName = Constants.EnvironmentVariables.GcmInteractive;
                string cfgName = string.Format("{0}.{1}",
                    Constants.GitConfiguration.Credential.SectionName,
                    Constants.GitConfiguration.Credential.Interactive);

                Context.Trace.WriteLine($"{envName} / {cfgName} is false/never; user interactivity has been disabled.");
                throw new Trace2InvalidOperationException(Context.Trace2, "Cannot prompt because user interactivity has been disabled.");
            }
        }

        protected void ThrowIfGuiPromptsDisabled()
        {
            if (!Context.Settings.IsGuiPromptsEnabled)
            {
                Context.Trace.WriteLine($"{Constants.EnvironmentVariables.GitTerminalPrompts} is 0; GUI prompts have been disabled.");
                throw new Trace2InvalidOperationException(Context.Trace2, "Cannot show prompt because GUI prompts have been disabled.");
            }
        }

        protected void ThrowIfTerminalPromptsDisabled()
        {
            if (!Context.Settings.IsTerminalPromptsEnabled)
            {
                Context.Trace.WriteLine($"{Constants.EnvironmentVariables.GitTerminalPrompts} is 0; terminal prompts have been disabled.");
                throw new Trace2InvalidOperationException(Context.Trace2, "Cannot prompt because terminal prompts have been disabled.");
            }
        }
        
        protected void ThrowIfWindowCancelled(WindowViewModel viewModel)
        {
            if (!viewModel.WindowResult)
            {
                throw new Exception("User cancelled dialog.");
            }
        }
        
        protected IntPtr GetParentWindowHandle()
        {
            if (int.TryParse(Context.Settings.ParentWindowId, out int id))
            {
                return new IntPtr(id);
            }

            return IntPtr.Zero;
        }

        protected bool TryFindHelperCommand(string envar, string configName, string defaultValue, out string command, out string args)
        {
            command = null;
            args = null;

            //
            // Search for UI helpers with the following precedence and logic..
            //
            // 1. (unset): use the default helper name that's in the source code and go to #3
            // 2. <absolute>: use the absolute path only and exactly as entered
            // 3. <relative>: search for..
            //     a. <appdir>/<relative>(.exe) - run this directly
            //     b. <appdir>/<relative>(.dll) - use `dotnet exec` to run
            //     c. Search PATH for <relative>(.exe) - run this directly
            //        NOTE: do NOT search PATH for <relative>(.dll) as we don't know if this is a dotnet executable..
            //
            // We print warning messages for missing helpers specified by the user, not the in-box ones.
            //
            if (Context.Settings.TryGetPathSetting(
                   envar, Constants.GitConfiguration.Credential.SectionName, configName, out string helperName))
            {
                // If the user set the helper override to the empty string then they are signalling not to use a helper
                if (string.IsNullOrEmpty(helperName))
                {
                    Context.Trace.WriteLine("UI helper override specified as the empty string.");
                    return false;
                }

                Context.Trace.WriteLine($"UI helper override specified: '{helperName}'.");
            }
            else if (string.IsNullOrWhiteSpace(defaultValue))
            {
                Context.Trace.WriteLine("No default UI supplied.");
                return false;
            }
            else
            {
                // Whilst we evaluate using the Avalonia/in-proc GUIs on Windows we include
                // a 'fallback' flag that lets us continue to use the WPF out-of-proc helpers.
                if (PlatformUtils.IsWindows() &&
                    Context.Settings.TryGetSetting(
                        Constants.EnvironmentVariables.GcmDevUseLegacyUiHelpers,
                        Constants.GitConfiguration.Credential.SectionName,
                        Constants.GitConfiguration.Credential.DevUseLegacyUiHelpers,
                        out string str) && str.IsTruthy())
                {
                    Context.Trace.WriteLine($"Using default legacy UI helper: '{defaultValue}'.");
                    helperName = defaultValue;
                }
                else
                {
                    return false;
                }
            }

            //
            // Check for an absolute path.. run this directly without intermediaries or modification
            //
            if (Path.IsPathRooted(helperName))
            {
                if (Context.FileSystem.FileExists(helperName))
                {
                    Context.Trace.WriteLine($"UI helper found at '{helperName}'.");
                    command = helperName;
                    return true;
                }

                Context.Trace.WriteLine($"UI helper was not found at '{helperName}'.");
                Context.Streams.Error.WriteLine($"warning: could not find configured UI helper '{helperName}'");
                return false;
            }

            //
            // Search the installation directory for an in-box helper
            //
            string appDir = Context.InstallationDirectory;
            string inBoxExePath = Path.Combine(appDir, PlatformUtils.IsWindows() ? $"{helperName}.exe" : helperName);
            string inBoxDllPath = Path.Combine(appDir, $"{helperName}.dll");

            // Look for in-box native executables
            if (Context.FileSystem.FileExists(inBoxExePath))
            {
                Context.Trace.WriteLine($"Found in-box native UI helper: '{inBoxExePath}'");
                command = inBoxExePath;
                return true;
            }

            // Look for in-box .NET framework-dependent executables
            if (Context.FileSystem.FileExists(inBoxDllPath))
            {
                string dotnetName = PlatformUtils.IsWindows() ? "dotnet.exe" : "dotnet";
                if (!Context.Environment.TryLocateExecutable(dotnetName, out string dotnetPath))
                {
                    Context.Trace.WriteLine($"Unable to run UI helper '{inBoxDllPath}' without the .NET CLI.");
                    Context.Streams.Error.WriteLine($"warning: could not find .NET CLI to run UI helper '{inBoxDllPath}'");
                    return false;
                }

                Context.Trace.WriteLine($"Found in-box framework-dependent UI helper: '{inBoxDllPath}'");
                command = dotnetPath;
                args = $"exec {QuoteCmdArg(inBoxDllPath)} ";
                return true;
            }

            //
            // Search the PATH for a native executable (do NOT search for out-of-box .NET framework-dependent DLLs)
            //
            if (Context.Environment.TryLocateExecutable(helperName, out command))
            {
                Context.Trace.WriteLine($"Found UI helper on PATH: '{helperName}'");
                return true;
            }

            //
            // No helper found!
            //
            Context.Trace.WriteLine($"UI helper '{helperName}' was not found.");
            Context.Streams.Error.WriteLine($"warning: could not find UI helper '{helperName}'");
            return false;
        }

        public static string QuoteCmdArg(string str)
        {
            char[] needsQuoteChars = { '"', ' ', '\\', '\n', '\r', '\t' };
            bool needsQuotes = str.Any(x => needsQuoteChars.Contains(x));

            if (!needsQuotes)
            {
                return str;
            }

            // Replace all '\' characters with an escaped '\\', and all '"' with '\"'
            string escapedStr = str.Replace("\\", "\\\\").Replace("\"", "\\\"");

            // Bookend the escaped string with double-quotes '"'
            return $"\"{escapedStr}\"";
        }
    }
}
