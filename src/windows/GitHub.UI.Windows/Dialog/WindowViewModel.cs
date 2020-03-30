using System;

namespace GitHub.UI
{
    public abstract class WindowViewModel : ViewModel
    {
        public abstract string Title { get; }

        public event EventHandler Accepted;
        public event EventHandler Canceled;

        public void Accept()
        {
            Accepted?.Invoke(this, EventArgs.Empty);
        }

        public void Cancel()
        {
            Canceled?.Invoke(this, EventArgs.Empty);
        }
    }
}
