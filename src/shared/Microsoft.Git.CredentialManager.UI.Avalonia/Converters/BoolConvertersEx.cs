using System.Linq;
using Avalonia.Data.Converters;

namespace GitCredentialManager.UI.Converters
{
    public static class BoolConvertersEx
    {
        public static readonly IMultiValueConverter Or =
            new FuncMultiValueConverter<bool,bool>(x => x.Aggregate(false, (a, b) => a || b));
    }
}
