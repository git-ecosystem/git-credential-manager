using System.Collections;
using System.Collections.Generic;

namespace GitCredentialManager
{
    public class TerminalMenu : IEnumerable<TerminalMenuItem>
    {
        private readonly ITerminal _terminal;
        private readonly IList<TerminalMenuItem> _items = new List<TerminalMenuItem>();

        public TerminalMenu(ITerminal terminal, string title = null)
        {
            _terminal = terminal;
            Title = title;
        }

        public string Title { get; set; }

        public TerminalMenuItem Add(string name)
        {
            var item = new TerminalMenuItem(name);
            _items.Add(item);
            return item;
        }

        public TerminalMenuItem Show(int? defaultOption = null)
        {
            bool hasDefault = defaultOption.HasValue && defaultOption.Value > -1;
            if (hasDefault)
            {
                EnsureArgument.InRange(defaultOption.Value, nameof(defaultOption), 0, _items.Count, upperInclusive: false);
            }

            string title = $"{Title ?? "Select an option"}:";

            string promptDefaultTag = hasDefault ? " (enter for default)" : string.Empty;
            string prompt = $"option{promptDefaultTag}";

            while (true)
            {
                _terminal.WriteLine(title);

                for (var i = 0; i < _items.Count; i++)
                {
                    string itemDefaultTag = i == defaultOption ? " (default)" : string.Empty;

                    // Use 1-based numbers for the UI
                    _terminal.WriteLine($"  {i + 1}. {_items[i].Name}{itemDefaultTag}");
                }

                string optionStr = _terminal.Prompt(prompt);

                if (string.IsNullOrWhiteSpace(optionStr))
                {
                    if (hasDefault)
                    {
                        return _items[defaultOption.Value];
                    }

                    _terminal.WriteLine("No default option is configured.\n");
                    continue;
                }

                if (!int.TryParse(optionStr, out int option))
                {
                    _terminal.WriteLine($"Invalid option '{optionStr}'. Expected a number.\n");
                    continue;
                }

                // The option as the user enters it is using a 1-based index
                // so we must subtract one to get to the 0-based index we use here.
                option--;

                if (option < 0 || option >= _items.Count)
                {
                    _terminal.WriteLine($"Invalid option '{optionStr}'.\n");
                    continue;
                }

                return _items[option];
            }
        }

        public IEnumerator<TerminalMenuItem> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _items).GetEnumerator();
        }
    }

    public class TerminalMenuItem
    {
        public TerminalMenuItem(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
