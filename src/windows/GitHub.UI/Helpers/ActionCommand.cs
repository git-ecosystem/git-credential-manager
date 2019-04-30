// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Windows.Input;

namespace GitHub.UI.Helpers
{
    /// <summary>
    /// Command that performs the specified action when invoked.
    /// </summary>
    public class ActionCommand : ICommand
    {
        private readonly Action<object> _commandAction;

        public ActionCommand(Action commandAction) : this(_ => commandAction())
        { }

        public ActionCommand(Action<object> commandAction)
        {
            _commandAction = commandAction;
        }

        public event EventHandler CanExecuteChanged;

        private bool _isEnabled = true;

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                _isEnabled = value;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool CanExecute(object parameter)
        {
            return _isEnabled;
        }

        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                _commandAction(parameter);
            }
        }
    }
}
