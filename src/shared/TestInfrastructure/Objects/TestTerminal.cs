using System;
using System.Collections.Generic;

namespace GitCredentialManager.Tests.Objects
{
    public class TestTerminal : ITerminal
    {
        public IDictionary<string, string> Prompts = new Dictionary<string, string>();
        public IDictionary<string, string> SecretPrompts = new Dictionary<string, string>();
        public IList<(string, object[])> Messages = new List<(string, object[])>();

        #region ITerminal

        public void WriteLine(string format, params object[] args)
        {
            Messages.Add((format, args));
        }

        string ITerminal.Prompt(string prompt)
        {
            if (!Prompts.TryGetValue(prompt, out string result))
            {
                throw new Exception($"No result has been configured for prompt text '{prompt}'");
            }

            return result;
        }

        string ITerminal.PromptSecret(string prompt)
        {
            if (!SecretPrompts.TryGetValue(prompt, out string result))
            {
                throw new Exception($"No result has been configured for secret prompt text '{prompt}'");
            }

            return result;
        }

        #endregion
    }
}
