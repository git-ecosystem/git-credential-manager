using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace GitHub.UI.Octicons
{
    public class AspectRatioConverter : DoubleConverter
    {
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var valueString = value as string;

            if (valueString != null && valueString.Contains(':'))
            {
                var split = valueString.Split(new[] { ':' }, 2);

                if (split.Length == 2)
                {
                    double d1, d2;
                    var ci = CultureInfo.InvariantCulture;

                    if (double.TryParse(split[0], NumberStyles.Float, ci, out d1) 
                        && double.TryParse(split[1], NumberStyles.Float, ci, out d2))
                    {
                        return d1 / d2;
                    }
                }
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}
