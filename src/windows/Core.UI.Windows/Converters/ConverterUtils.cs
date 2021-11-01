using System;
using System.Windows;

namespace GitCredentialManager.UI.Converters
{
    public class ConverterHelper
    {
        private static char[] s_commaSeparator = new char[] { ',' };

        /// <summary>
        /// Returns true if parameter contains the specified option text.
        /// </summary>
        /// <param name="parameter">comma-separated options</param>
        /// <param name="option">option to search</param>
        /// <returns>true if parameter contains option, false otherwise</returns>
        private static bool ParameterContains(object parameter, String option)
        {
            string arg = parameter as string;
            if (!string.IsNullOrEmpty(arg))
            {
                string[] optionArgs = arg.Split(s_commaSeparator, StringSplitOptions.RemoveEmptyEntries);
                foreach (string optionArg in optionArgs)
                {
                    if (optionArg.Equals(option, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if parameter contains "Not", "!", or "Invert".
        /// </summary>
        /// <param name="parameter">comma-separated options</param>
        /// <returns>true if parameter has the invert option</returns>
        public static bool GetInvert(object parameter)
        {
            string arg = parameter as String;
            if (!string.IsNullOrEmpty(arg))
            {
                string[] options = arg.Split(s_commaSeparator, StringSplitOptions.RemoveEmptyEntries);
                foreach (string option in options)
                {
                    if (option.Equals("Not", StringComparison.OrdinalIgnoreCase) ||
                        option.Equals("!", StringComparison.OrdinalIgnoreCase) ||
                        option.Equals("Invert", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Returns the appropriate Visibility based on the show condition
        /// and the preferred Visibility.Collaped or Visibility.Hidden option
        /// in parameter.
        /// </summary>
        /// <param name="show">true to get Visibility.Visible,
        /// false for Visibility.Collapsed or Visibility.Hidden depending on parameter.</param>
        /// <param name="parameter">comma-separated options. "Not", "!", or "Invert" to invert
        /// the bool evaluation before converting to Visibility. "Hidden" to get
        /// Visibility.Hidden (where the default is Visibility.Collapsed).</param>
        /// <returns>Visibility.Collapsed or Visibility.Hidden</returns>
        public static Visibility GetConditionalVisibility(bool show, object parameter)
        {
            return GetConditionalVisibility(show, parameter, false);
        }

        /// <summary>
        /// Returns the appropriate Visibility based on the show condition
        /// and the preferred Visibility.Collaped or Visibility.Hidden option
        /// in parameter.
        /// </summary>
        /// <param name="show">true to get Visibility.Visible,
        /// false for Visibility.Collapsed or Visibility.Hidden depending on parameter.</param>
        /// <param name="parameter">comma-separated options. "Not", "!", or "Invert" to invert
        /// the bool evaluation before converting to Visibility. "Hidden" to get
        /// Visibility.Hidden (where the default is Visibility.Collapsed).</param>
        /// <param name="ignoreInvert">true to ignore the Invert option in parameter.
        /// This is used to avoid double-inverting.</param>
        /// <returns>Visibility.Collapsed or Visibility.Hidden</returns>
        public static Visibility GetConditionalVisibility(bool show, object parameter, bool ignoreInvert)
        {
            bool result = show;
            if (!ignoreInvert && GetInvert(parameter))
            {
                result = !show;
            }
            return result ? Visibility.Visible : GetCollapsedOrHidden(parameter);
        }

        /// <summary>
        /// Returns Visibility.Hidden if parameter contains "Hidden". Default is
        /// Visibility.Collapsed.
        /// </summary>
        /// <param name="parameter">comma-separated options. "Hidden" to get
        /// Visibility.Hidden. Default is Visibility.Collapsed.</param>
        /// <returns>Visibility.Collapsed or Visibility.Hidden</returns>
        internal static Visibility GetCollapsedOrHidden(object parameter)
        {
            return ParameterContains(parameter, "Hidden") ? Visibility.Hidden : Visibility.Collapsed;
        }

        /// <summary>
        /// Returns Visibility.Hidden if parameter[argIndex] contains "Hidden". Default is
        /// Visibility.Collapsed.
        /// </summary>
        /// <param name="parameter">parameter[argIndex] as comma-separated options.
        /// "Hidden" to get Visibility.Hidden. Default is Visibility.Collapsed.</param>
        /// <param name="argIndex">index to the actual argument to use in the parameter String[]</param>
        /// <returns>Visibility</returns>
        public static Visibility GetCollapsedOrHiddenFromArray(object parameter, int argIndex)
        {
            object[] args = parameter as object[];
            if (args != null && args.Length >= argIndex + 1)
            {
                return GetCollapsedOrHidden(args[argIndex] as string);
            }
            return Visibility.Collapsed;
        }
    }
}
