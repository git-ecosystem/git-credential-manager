using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GitCredentialManager.UI.Controls;

public partial class ProgressWindow : Window
{
    public ProgressWindow()
    {
        InitializeComponent();
    }

    public static IntPtr ShowAndGetHandle(CancellationToken ct)
    {
        var tsc = new TaskCompletionSource<IntPtr>();
        
        Window CreateWindow()
        {
            var window = new ProgressWindow();
            window.Loaded += (s, e) => tsc.SetResult(window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero);
            return window;
        }

        Task _ = AvaloniaUi.ShowWindowAsync(CreateWindow, IntPtr.Zero, ct);

        return tsc.Task.GetAwaiter().GetResult();
    }
}
