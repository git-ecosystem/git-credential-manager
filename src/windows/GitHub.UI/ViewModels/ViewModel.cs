// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.ComponentModel;

namespace GitHub.UI.ViewModels
{
    /// <summary>
    /// Rather than bring in all the overhead of an MVVM framework, we'll just do the simplest
    /// possible thing.
    /// </summary>
    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChangedEvent(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void SetAndRaisePropertyChangedEvent<T>(ref T field, T value, string propertyName)
        {
            field = value;
            RaisePropertyChangedEvent(propertyName);
        }
    }
}
