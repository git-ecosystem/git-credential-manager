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
            if (int.TryParse(Context.Settings.ParentWindowId, out int id))
            {
                return new IntPtr(id);
            }

            return IntPtr.Zero;
        }

        protected void WriteResult(IDictionary<string, string> result)
        {
            Context.Streams.Out.WriteDictionary(result);
        }
    }
}
