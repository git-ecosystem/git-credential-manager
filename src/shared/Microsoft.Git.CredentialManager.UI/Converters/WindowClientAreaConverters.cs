using Avalonia.Data.Converters;
using Avalonia.Platform;

namespace Microsoft.Git.CredentialManager.UI.Converters
{
    public static class WindowClientAreaConverters
    {
        public static readonly IValueConverter BoolToChromeHints =
            new FuncValueConverter<bool, ExtendClientAreaChromeHints>(
                x => x
                    ? ExtendClientAreaChromeHints.NoChrome
                    : ExtendClientAreaChromeHints.PreferSystemChrome);
    }
}
