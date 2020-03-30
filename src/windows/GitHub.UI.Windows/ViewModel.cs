// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GitHub.UI
{
    public abstract class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void SetAndRaisePropertyChanged<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            field = value;
            RaisePropertyChanged(propertyName);
        }

        public virtual bool IsValid { get; }
    }
}
