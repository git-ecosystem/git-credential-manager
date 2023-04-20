using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GitCredentialManager.UI.Controls;

namespace GitCredentialManager.UI
{
    public class AvaloniaApp : Avalonia.Application
    {
        private readonly Func<Window> _mainWindowFunc;

        public AvaloniaApp() { }

        public AvaloniaApp(Func<Window> mainWindowFunc)
        {
            _mainWindowFunc = mainWindowFunc;
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && _mainWindowFunc != null)
            {
                desktop.MainWindow = _mainWindowFunc();
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void About(object sender, EventArgs e)
        {
            var window = new AboutWindow();
            window.Show();
        }
    }
}
