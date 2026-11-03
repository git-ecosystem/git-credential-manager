using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GitCredentialManager.Authentication.Entra;

namespace GitCredentialManager.UI.Controls;

public partial class ProgressWindow : Window
{
    public ProgressWindow()
    {
        InitializeComponent();
    }

    public ProgressWindow(string context) : this()
    {
        if (!string.IsNullOrWhiteSpace(context))
        {
            var text = this.FindControl<TextBlock>("ContextText");
            text.Text = context;
            text.IsVisible = true;
        }
    }

    public static IntPtr ShowAndGetHandle(CancellationToken ct)
    {
        var tsc = new TaskCompletionSource<IntPtr>();

        // Capture the request context now: this runs on the calling (credential request) flow,
        // whereas the window is created on the UI thread.
        string context = DescribeOrigin();

        Window CreateWindow()
        {
            var window = new ProgressWindow(context);
            window.Loaded += (s, e) => tsc.SetResult(window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero);
            return window;
        }

        Task _ = AvaloniaUi.ShowWindowAsync(CreateWindow, IntPtr.Zero, ct);

        return tsc.Task.GetAwaiter().GetResult();
    }

    internal /* for testing purposes */ static string DescribeOrigin()
    {
        // The helper is started by Git inside the repository that triggered the request,
        // so the working directory tells which checkout is asking.
        string request = AuthPromptContext.Description;
        string cwd = Environment.CurrentDirectory;

        return string.IsNullOrWhiteSpace(request)
            ? $"Requested from: {cwd}"
            : $"Sign-in for: {request}{Environment.NewLine}Requested from: {cwd}";
    }
}
