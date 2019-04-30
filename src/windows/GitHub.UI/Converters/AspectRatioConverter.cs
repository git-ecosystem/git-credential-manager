// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.ComponentModel;
using System.Globalization;

namespace GitHub.UI.Converters
{
    public class AspectRatioConverter : DoubleConverter
    {
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string valueString && valueString.Contains(":"))
            {
                var split = valueString.Split(new[] { ':' }, 2);

                if (split.Length == 2)
                {
                    var ci = CultureInfo.InvariantCulture;

                    if (double.TryParse(split[0], NumberStyles.Float, ci, out double d1)
                        && double.TryParse(split[1], NumberStyles.Float, ci, out double d2))
                    {
                        return d1 / d2;
                    }
                }
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}
