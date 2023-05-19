using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.Linq;

namespace GitCredentialManager;

public static class CommandExtensions
{
    /// <summary>
    /// Add options to a command.
    /// </summary>
    /// <param name="command">Command to add options to.</param>
    /// <param name="arity">Specify the required arity of the options.</param>
    /// <param name="options">Set of options to add.</param>
    public static void AddOptionSet(this Command command, OptionArity arity, params Option[] options)
    {
        foreach (Option option in options)
        {
            command.AddOption(option);
        }

        // No need to add a validator if 0..* options are OK
        if (arity != OptionArity.ZeroOrMore)
        {
            command.AddValidator(r =>
            {
                int count = options.Count(o => r.FindResultFor(o) is not null);
                string optionList = string.Join(", ", options.Select(s => $"--{s.Name}"));
                switch (arity)
                {
                    case OptionArity.ZeroOrOne:
                        if (count > 1)
                            r.ErrorMessage = $"Can only specify one of: {optionList}";
                        break;

                    case OptionArity.ExactlyOne:
                        if (count != 1)
                            r.ErrorMessage = $"Require exactly one of: {optionList}";
                        break;

                    case OptionArity.Zero:
                        if (count != 0)
                        {
                            IEnumerable<string> usedOptions = options.Where(o => r.FindResultFor(o) is not null)
                                .Select(x => $"--{x.Name}");
                            r.ErrorMessage = $"{command.Name} does not support options: {string.Join(", ", usedOptions)}";
                        }
                        break;

                    case OptionArity.OneOrMore:
                        if (count == 0)
                            r.ErrorMessage = $"Require at least one of: {optionList}";
                        break;

                    case OptionArity.ZeroOrMore:
                        Debug.Fail("Should not have a validator for an arity of ZeroOrMore.");
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(arity), arity, null);
                }
            });
        }
    }
}

public enum OptionArity
{
    /// <summary>
    /// An arity that may have multiple values.
    /// </summary>
    ZeroOrMore = 0,

    /// <summary>
    /// An arity that must have exactly one value.
    /// </summary>
    ExactlyOne,

    /// <summary>
    /// An arity that does not allow any values.
    /// </summary>
    Zero,

    /// <summary>
    /// An arity that may have one value, but no more than one.
    /// </summary>
    ZeroOrOne,

    /// <summary>
    /// An arity that must have at least one value.
    /// </summary>
    OneOrMore,
}
