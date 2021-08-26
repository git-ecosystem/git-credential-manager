using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Authentication
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
            IDictionary<string, string> standardInput = null)
        {
            return InvokeHelperAsync(path, args, null, CancellationToken.None);
        }

        protected async Task<IDictionary<string, string>> InvokeHelperAsync(string path, string args,
            IDictionary<string, string> standardInput, CancellationToken ct)
        {
            var procStartInfo = new ProcessStartInfo(path)
            {
                Arguments = args,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = false, // Do not redirect stderr as tracing might be enabled
                UseShellExecute = false
            };

            // We flush the trace writers here so that the we don't stomp over the
            // authentication helper's messages.
            Context.Trace.Flush();

            var process = Process.Start(procStartInfo);
            if (process is null)
            {
                throw new Exception($"Failed to start helper process '{path}'");
            }

            // Kill the process upon a cancellation request
            ct.Register(() => process.Kill());

            if (!(standardInput is null))
            {
                await process.StandardInput.WriteDictionaryAsync(standardInput);
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

                throw new Exception($"helper error ({exitCode}): {errorMessage}");
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

                throw new InvalidOperationException("Cannot prompt because user interactivity has been disabled.");
            }
        }

        protected void ThrowIfTerminalPromptsDisabled()
        {
            if (!Context.Settings.IsTerminalPromptsEnabled)
            {
                Context.Trace.WriteLine($"{Constants.EnvironmentVariables.GitTerminalPrompts} is 0; terminal prompts have been disabled.");

                throw new InvalidOperationException("Cannot prompt because terminal prompts have been disabled.");
            }
        }

        protected bool TryFindHelperExecutablePath(string envar, string configName, string defaultValue, out string path)
        {
            bool isOverride = false;
            if (Context.Settings.TryGetSetting(
                envar, Constants.GitConfiguration.Credential.SectionName, configName, out string helperName))
            {
                Context.Trace.WriteLine($"UI helper override specified: '{helperName}'.");
                isOverride = true;
            }
            else
            {
                // Use the default helper if none was specified.
                // On Windows append ".exe" for the default helpers only. If a user has specified their own
                // helper they should append the correct extension.
                helperName = PlatformUtils.IsWindows() ? $"{defaultValue}.exe" : defaultValue;
            }

            // If the user set the helper override to the empty string then they are signalling not to use a helper
            if (string.IsNullOrEmpty(helperName))
            {
                path = null;
                return false;
            }

            if (Path.IsPathRooted(helperName))
            {
                path = helperName;
            }
            else
            {
                string executableDirectory = Path.GetDirectoryName(Context.ApplicationPath);
                path = Path.Combine(executableDirectory!, helperName);
            }

            if (!Context.FileSystem.FileExists(path))
            {
                // Only warn for missing helpers specified by the user, not in-box ones
                if (isOverride)
                {
                    Context.Trace.WriteLine($"UI helper '{helperName}' was not found at '{path}'.");
                    Context.Streams.Error.WriteLine($"warning: could not find configured UI helper '{helperName}'");
                }

                return false;
            }

            return true;
        }

        public static string QuoteCmdArg(string str)
        {
            char[] needsQuoteChars = {'"', ' ', '\\', '\n', '\r', '\t'};
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
