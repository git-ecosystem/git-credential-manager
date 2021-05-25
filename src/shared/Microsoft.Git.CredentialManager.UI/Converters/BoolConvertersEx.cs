using System.Linq;
using Avalonia.Data.Converters;

namespace Microsoft.Git.CredentialManager.UI.Converters
{
    public static class BoolConvertersEx
    {
        /// <summary>
        /// A multi-value converter that returns true if any inputs are true.
        /// </summary>
        public static readonly IMultiValueConverter Or =
            new FuncMultiValueConverter<bool, bool>(x => x.Any(y => y));
    }
}
