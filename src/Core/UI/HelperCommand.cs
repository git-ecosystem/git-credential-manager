using System;
using System.Collections.Generic;
using System.CommandLine;

namespace GitCredentialManager.UI
{
    public abstract class HelperCommand : Command
    {
        protected ICommandContext Context { get; }

        public HelperCommand(ICommandContext context, string name, string description)
            : base(name, description)
        {
            Context = context;
        }

        protected IntPtr GetParentHandle()
        {
            // Check if the user has specified a parent window ID
            if (int.TryParse(Context.Settings.ParentWindowId, out int id))
            {
                return new IntPtr(id);
            }

            // Check if we can use the console window as a parent
            IntPtr consoleParent = PlatformUtils.GetConsoleParentWindow();
            if (consoleParent != IntPtr.Zero)
            {
                return consoleParent;
            }

            return IntPtr.Zero;
        }

        protected void WriteResult(IDictionary<string, string> result)
        {
            Context.Streams.Out.WriteDictionary(result);
        }
    }
}
