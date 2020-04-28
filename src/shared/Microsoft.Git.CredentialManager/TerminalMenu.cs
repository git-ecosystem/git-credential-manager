// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Git.CredentialManager
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

        public string Title { get; }

        public void Add(TerminalMenuItem item)
        {
            if (item.IsDefault && _items.Any(x => x.IsDefault))
            {
                throw new ArgumentException("A default menu item has already been added.");
            }

            _items.Add(item);
        }

        public int Show()
        {
            TerminalMenuItem defaultItem = _items.FirstOrDefault(x => x.IsDefault);
            bool hasDefault = !(defaultItem is null);

            string title = $"{Title ?? "Select an option"}:";

            string promptDefaultTag = hasDefault ? " (enter for default)" : string.Empty;
            string prompt = $"option{promptDefaultTag}";

            while (true)
            {
                _terminal.WriteLine(title);

                foreach (TerminalMenuItem item in _items)
                {
                    string itemDefaultTag = item.IsDefault ? " (default)" : string.Empty;
                    _terminal.WriteLine($"  {item.Id}. {item.Name}{itemDefaultTag}");
                }

                string optionStr = _terminal.Prompt(prompt);

                if (string.IsNullOrWhiteSpace(optionStr))
                {
                    if (hasDefault)
                    {
                        return defaultItem.Id;
                    }

                    _terminal.WriteLine("No default option is configured.\n");
                    continue;
                }

                if (!int.TryParse(optionStr, out int option))
                {
                    _terminal.WriteLine($"Invalid option '{optionStr}'. Expected a number.\n");
                    continue;
                }

                if (_items.All(x => x.Id != option))
                {
                    _terminal.WriteLine($"Invalid option '{optionStr}'.\n");
                    continue;
                }

                return option;
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
        public TerminalMenuItem(int id, string name, bool isDefault = false)
        {
            Id = id;
            Name = name;
            IsDefault = isDefault;
        }

        public int Id { get; }

        public string Name { get; }

        public bool IsDefault { get; }
    }
}
