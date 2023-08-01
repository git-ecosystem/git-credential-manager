using System;
using System.Net.Http;
using System.Security.AccessControl;
using System.Text;
using GitCredentialManager.Diagnostics;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace Core.Tests.Commands;

public class DiagnoseCommandTests
{
    [Fact]
    public void NetworkingDiagnostic_SendHttpRequest_Primary_OK()
    {
        var primaryUriString = "http://example.com";
        var sb = new StringBuilder();
        var context = new TestCommandContext();
        var networkingDiagnostic = new NetworkingDiagnostic(context);
        var primaryUri = new Uri(primaryUriString);
        var httpHandler = new TestHttpMessageHandler();
        var httpResponse = new HttpResponseMessage();
        var expected = $"Sending HEAD request to {primaryUriString}... OK{Environment.NewLine}";

        httpHandler.Setup(HttpMethod.Head, primaryUri, httpResponse);

        networkingDiagnostic.SendHttpRequest(sb, new HttpClient(httpHandler));

        httpHandler.AssertRequest(HttpMethod.Head, primaryUri, expectedNumberOfCalls: 1);
        Assert.Contains(expected, sb.ToString());
    }

    [Fact]
    public void NetworkingDiagnostic_SendHttpRequest_Backup_OK()
    {
        var primaryUriString = "http://example.com";
        var backupUriString = "http://httpforever.com";
        var sb = new StringBuilder();
        var context = new TestCommandContext();
        var networkingDiagnostic = new NetworkingDiagnostic(context);
        var primaryUri = new Uri(primaryUriString);
        var backupUri = new Uri(backupUriString);
        var httpHandler = new TestHttpMessageHandler { SimulatePrimaryUriFailure = true };
        var httpResponse = new HttpResponseMessage();
        var expected = $"Sending HEAD request to {primaryUriString}... warning: HEAD request failed{Environment.NewLine}" +
                       $"Sending HEAD request to {backupUriString}... OK{Environment.NewLine}";

        httpHandler.Setup(HttpMethod.Head, primaryUri, httpResponse);
        httpHandler.Setup(HttpMethod.Head, backupUri, httpResponse);

        networkingDiagnostic.SendHttpRequest(sb, new HttpClient(httpHandler));

        httpHandler.AssertRequest(HttpMethod.Head, primaryUri, expectedNumberOfCalls: 1);
        httpHandler.AssertRequest(HttpMethod.Head, backupUri, expectedNumberOfCalls: 1);
        Assert.Contains(expected, sb.ToString());
    }

    [Fact]
    public void NetworkingDiagnostic_SendHttpRequest_No_Network()
    {
        var primaryUriString = "http://example.com";
        var backupUriString = "http://httpforever.com";
        var sb = new StringBuilder();
        var context = new TestCommandContext();
        var networkingDiagnostic = new NetworkingDiagnostic(context);
        var primaryUri = new Uri(primaryUriString);
        var backupUri = new Uri(backupUriString);
        var httpHandler = new TestHttpMessageHandler { SimulateNoNetwork = true };
        var httpResponse = new HttpResponseMessage();
        var expected = $"Sending HEAD request to {primaryUriString}... warning: HEAD request failed{Environment.NewLine}" +
                       $"Sending HEAD request to {backupUriString}... warning: HEAD request failed{Environment.NewLine}";

        httpHandler.Setup(HttpMethod.Head, primaryUri, httpResponse);
        httpHandler.Setup(HttpMethod.Head, backupUri, httpResponse);

        networkingDiagnostic.SendHttpRequest(sb, new HttpClient(httpHandler));

        httpHandler.AssertRequest(HttpMethod.Head, primaryUri, expectedNumberOfCalls: 1);
        httpHandler.AssertRequest(HttpMethod.Head, backupUri, expectedNumberOfCalls: 1);
        Assert.Contains(expected, sb.ToString());
    }
}
