using System.Linq;
using Avalonia.Data.Converters;

namespace GitCredentialManager.UI.Converters
{
    public static class BoolConvertersEx
    {
        public static readonly IMultiValueConverter Or =
            new FuncMultiValueConverter<bool,bool>(x => x.Aggregate(false, (a, b) => a || b));

        public static readonly IMultiValueConverter And =
            new FuncMultiValueConverter<bool,bool>(x => x.Aggregate(true, (a, b) => a && b));
    }
}
