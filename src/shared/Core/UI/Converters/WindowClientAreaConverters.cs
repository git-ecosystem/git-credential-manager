using Avalonia.Data.Converters;
using Avalonia.Platform;

namespace GitCredentialManager.UI.Converters
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
