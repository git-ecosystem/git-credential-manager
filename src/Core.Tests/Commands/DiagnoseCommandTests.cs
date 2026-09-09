using System;
using System.CommandLine;
using System.Net.Http;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using GitCredentialManager.Commands;
using GitCredentialManager.Diagnostics;
using GitCredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace Core.Tests.Commands;

public class DiagnoseCommandTests
{
    [Fact]
    public async Task DiagnoseCommand_AllSuccessful_ReturnsZero()
    {
        string skip = It.IsAny<string>();
        var diagnosticMock = new Mock<IDiagnostic>(MockBehavior.Strict);
        diagnosticMock.SetupGet(x => x.Name).Returns("TestDiagnostic");
        diagnosticMock.Setup(x => x.CanRun(out skip)).Returns(true);
        diagnosticMock.Setup(x => x.RunAsync(It.IsAny<Action<string>>()))
            .ReturnsAsync(new DiagnosticResult([], []));

        var context = new TestCommandContext();
        var command = new DiagnoseCommand(context);
        command.AddDiagnostic(diagnosticMock.Object);

        int result = await command.InvokeAsync([]);

        Assert.Equal(0, result);
        diagnosticMock.Verify(x => x.RunAsync(It.IsAny<Action<string>>()), Times.Once);
    }

    [Fact]
    public async Task DiagnoseCommand_AtLeastOneFailure_ReturnsNonZero()
    {
        string skip = It.IsAny<string>();
        var diagnosticMock1 = new Mock<IDiagnostic>(MockBehavior.Strict);
        diagnosticMock1.SetupGet(x => x.Name).Returns("TestDiagnostic1");
        diagnosticMock1.Setup(x => x.CanRun(out skip)).Returns(true);
        diagnosticMock1.Setup(x => x.RunAsync(It.IsAny<Action<string>>()))
            .ReturnsAsync(new DiagnosticResult([
                new DiagnosticReport(DiagnosticReportKind.Error, "Failure")
            ], []));

        var diagnosticMock2 = new Mock<IDiagnostic>(MockBehavior.Strict);
        diagnosticMock2.SetupGet(x => x.Name).Returns("TestDiagnostic2");
        diagnosticMock2.Setup(x => x.CanRun(out skip)).Returns(true);
        diagnosticMock2.Setup(x => x.RunAsync(It.IsAny<Action<string>>()))
            .ReturnsAsync(new DiagnosticResult([], []));

        var context = new TestCommandContext();
        var command = new DiagnoseCommand(context);
        command.AddDiagnostic(diagnosticMock1.Object);
        command.AddDiagnostic(diagnosticMock2.Object);

        int result = await command.InvokeAsync([]);

        Assert.NotEqual(0, result);
        diagnosticMock1.Verify(x => x.RunAsync(It.IsAny<Action<string>>()), Times.Once);
        diagnosticMock2.Verify(x => x.RunAsync(It.IsAny<Action<string>>()), Times.Once);
    }

    [Fact]
    public async Task DiagnoseCommand_Warnings_NonStrict_ReturnsZero()
    {
        string skip = It.IsAny<string>();
        var diagnosticMock = new Mock<IDiagnostic>(MockBehavior.Strict);
        diagnosticMock.SetupGet(x => x.Name).Returns("TestDiagnostic");
        diagnosticMock.Setup(x => x.CanRun(out skip)).Returns(true);
        diagnosticMock.Setup(x => x.RunAsync(It.IsAny<Action<string>>()))
            .ReturnsAsync(new DiagnosticResult([
                new DiagnosticReport(DiagnosticReportKind.Warning, "Caution")
            ], []));

        var context = new TestCommandContext();
        var command = new DiagnoseCommand(context);
        command.AddDiagnostic(diagnosticMock.Object);

        int result = await command.InvokeAsync([]);

        Assert.Equal(0, result);
        diagnosticMock.Verify(x => x.RunAsync(It.IsAny<Action<string>>()), Times.Once);
    }

    [Fact]
    public async Task DiagnoseCommand_Warnings_Strict_ReturnsNonZero()
    {
        string skip = It.IsAny<string>();
        var diagnosticMock = new Mock<IDiagnostic>(MockBehavior.Strict);
        diagnosticMock.SetupGet(x => x.Name).Returns("TestDiagnostic");
        diagnosticMock.Setup(x => x.CanRun(out skip)).Returns(true);
        diagnosticMock.Setup(x => x.RunAsync(It.IsAny<Action<string>>()))
            .ReturnsAsync(new DiagnosticResult([
                new DiagnosticReport(DiagnosticReportKind.Warning, "Caution")
            ], []));

        var context = new TestCommandContext();
        var command = new DiagnoseCommand(context);
        command.AddDiagnostic(diagnosticMock.Object);

        int result = await command.InvokeAsync(["--strict"]);

        Assert.NotEqual(0, result);
        diagnosticMock.Verify(x => x.RunAsync(It.IsAny<Action<string>>()), Times.Once);
    }

    [Fact]
    public async Task NetworkingDiagnostic_SendHttpRequest_Primary_OK()
    {
        var primaryUriString = "http://example.com";
        var reporter = new TestDiagnosticReporter();
        var context = new TestCommandContext();
        var networkingDiagnostic = new NetworkingDiagnostic(context);
        var primaryUri = new Uri(primaryUriString);
        var httpHandler = new TestHttpMessageHandler();
        var httpResponse = new HttpResponseMessage();

        httpHandler.Setup(HttpMethod.Head, primaryUri, httpResponse);

        await networkingDiagnostic.SendHttpRequestAsync(reporter, new HttpClient(httpHandler));

        httpHandler.AssertRequest(HttpMethod.Head, primaryUri, expectedNumberOfCalls: 1);
        Assert.Single(reporter.Progress);
        Assert.Equal($"Sending HEAD request to {primaryUriString}", reporter.Progress[0]);
    }

    [Fact]
    public async Task NetworkingDiagnostic_SendHttpRequest_Backup_OK()
    {
        var primaryUriString = "http://example.com";
        var backupUriString = "http://httpforever.com";
        var reporter = new TestDiagnosticReporter();
        var context = new TestCommandContext();
        var networkingDiagnostic = new NetworkingDiagnostic(context);
        var primaryUri = new Uri(primaryUriString);
        var backupUri = new Uri(backupUriString);
        var httpHandler = new TestHttpMessageHandler { SimulatePrimaryUriFailure = true };
        var httpResponse = new HttpResponseMessage();

        httpHandler.Setup(HttpMethod.Head, primaryUri, httpResponse);
        httpHandler.Setup(HttpMethod.Head, backupUri, httpResponse);

        await networkingDiagnostic.SendHttpRequestAsync(reporter, new HttpClient(httpHandler));

        httpHandler.AssertRequest(HttpMethod.Head, primaryUri, expectedNumberOfCalls: 1);
        httpHandler.AssertRequest(HttpMethod.Head, backupUri, expectedNumberOfCalls: 1);
        Assert.Equal(2, reporter.Progress.Count);
        Assert.Single(reporter.Warnings);
        Assert.Equal($"Sending HEAD request to {primaryUriString}", reporter.Progress[0]);
        Assert.Equal("HEAD request failed", reporter.Warnings[0]);
        Assert.Equal($"Sending HEAD request to {backupUriString}", reporter.Progress[1]);
    }

    [Fact]
    public async Task NetworkingDiagnostic_SendHttpRequest_No_Network()
    {
        var primaryUriString = "http://example.com";
        var backupUriString = "http://httpforever.com";
        var reporter = new TestDiagnosticReporter();
        var context = new TestCommandContext();
        var networkingDiagnostic = new NetworkingDiagnostic(context);
        var primaryUri = new Uri(primaryUriString);
        var backupUri = new Uri(backupUriString);
        var httpHandler = new TestHttpMessageHandler { SimulateNoNetwork = true };
        var httpResponse = new HttpResponseMessage();

        httpHandler.Setup(HttpMethod.Head, primaryUri, httpResponse);
        httpHandler.Setup(HttpMethod.Head, backupUri, httpResponse);

        await networkingDiagnostic.SendHttpRequestAsync(reporter, new HttpClient(httpHandler));

        httpHandler.AssertRequest(HttpMethod.Head, primaryUri, expectedNumberOfCalls: 1);
        httpHandler.AssertRequest(HttpMethod.Head, backupUri, expectedNumberOfCalls: 1);
        Assert.Equal(2, reporter.Progress.Count);
        Assert.Equal(2, reporter.Warnings.Count);
        Assert.Equal($"Sending HEAD request to {primaryUriString}", reporter.Progress[0]);
        Assert.Equal("HEAD request failed", reporter.Warnings[0]);
        Assert.Equal($"Sending HEAD request to {backupUriString}", reporter.Progress[1]);
        Assert.Equal("HEAD request failed", reporter.Warnings[1]);
    }
}
