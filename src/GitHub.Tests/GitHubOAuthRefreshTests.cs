using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.Authentication.OAuth;
using GitCredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace GitHub.Tests;

public class GitHubOAuthRefreshTests
{
    private static readonly Uri Target = new("https://github.com");
    private readonly TestCommandContext _context = new();
    private readonly Mock<IGitHubAuthentication> _auth = new(MockBehavior.Strict);
    private readonly Mock<IGitHubRestApi> _api = new(MockBehavior.Strict);
    private readonly FixedClock _clock = new();

    private GitHubHostProvider Provider() => new(_context, _api.Object, _auth.Object, _clock);

    private static GitRequest Request(string password = null, string host = "github.com", string user = "alice", string path = null) =>
        new(new Dictionary<string, string>
        {
            ["protocol"] = "https",
            ["host"] = host,
            ["path"] = path,
            ["username"] = user,
            ["password"] = password
        });

    private OAuth2TokenResult Tokens(string access = "new-access", string refresh = "new-refresh") =>
        new(access, "bearer")
        {
            RefreshToken = refresh,
            ExpiresIn = TimeSpan.FromHours(8),
            RefreshTokenExpiresIn = TimeSpan.FromDays(180)
        };

    private GitHubOAuthCredential Seed(bool expired = true, string host = "github.com")
    {
        var uri = new Uri($"https://{host}");
        GitHubOAuthCredential credential = GitHubOAuthCredential.Create(
            "alice", GitHubConstants.OAuthClientId, Tokens("old-access", "old-refresh"),
            _clock.GetUtcNow().AddHours(expired ? -9 : -1));
        Save(uri, credential);
        return credential;
    }

    private void Save(Uri uri, GitHubOAuthCredential credential)
    {
        _context.CredentialStore.Add(
            GitHubHostProvider.GetOAuthServiceName(uri), credential.Account,
            JsonSerializer.Serialize(credential, GitHubCredentialJsonContext.Default.GitHubOAuthCredential));
        _context.CredentialStore.Add(uri.AbsoluteUri.TrimEnd('/'), credential.Account, credential.AccessToken);
    }

    private GitHubOAuthCredential Stored()
    {
        Assert.True(_context.CredentialStore.TryGet(
            GitHubHostProvider.GetOAuthServiceName(Target), "alice", out TestCredential record));
        return JsonSerializer.Deserialize(record.Password, GitHubCredentialJsonContext.Default.GitHubOAuthCredential);
    }

    [Theory]
    [InlineData(AuthenticationModes.Browser)]
    [InlineData(AuthenticationModes.Device)]
    public async Task Login_PersistsFullCredentialBeforeGitStore(AuthenticationModes mode)
    {
        _auth.Setup(x => x.GetAuthenticationAsync(Target, "alice", It.IsAny<AuthenticationModes>()))
            .ReturnsAsync(new AuthenticationPromptResult(mode));
        if (mode == AuthenticationModes.Browser)
        {
            _auth.Setup(x => x.GetOAuthTokenViaBrowserAsync(
                    Target, It.Is<IEnumerable<string>>(s => s.Contains("offline_access")), "alice"))
                .ReturnsAsync(Tokens());
        }
        else
        {
            _auth.Setup(x => x.GetOAuthTokenViaDeviceCodeAsync(
                    Target, It.Is<IEnumerable<string>>(s => s.Contains("offline_access"))))
                .ReturnsAsync(Tokens());
        }
        _api.Setup(x => x.GetUserInfoAsync(Target, "new-access"))
            .ReturnsAsync(new GitHubUserInfo { Login = "alice" });

        GitResponse result = await Provider().GetCredentialAsync(Request());

        Assert.Equal("new-access", result.Credential.Password);
        Assert.True(result.IsCredentialEphemeral);
        Assert.Equal(_clock.GetUtcNow().AddHours(8).ToUnixTimeSeconds().ToString(),
            result.AdditionalProperties["password_expiry_utc"]);
        Assert.Equal("new-refresh", Stored().RefreshToken);
        Assert.Equal(_clock.GetUtcNow().AddDays(180), Stored().RefreshTokenExpiresAt);
    }

    [Theory]
    [InlineData("github.com", true)]
    [InlineData("octocorp.ghe.com", true)]
    [InlineData("github.example.com", false)]
    public async Task Login_RequestsOfflineAccessOnlyOnCloudHosts(string host, bool requestRefresh)
    {
        Uri target = new($"https://{host}");
        _auth.Setup(x => x.GetOAuthTokenViaDeviceCodeAsync(
                target, It.Is<IEnumerable<string>>(s => s.Contains("offline_access") == requestRefresh)))
            .ReturnsAsync(new OAuth2TokenResult("legacy-token", "bearer"));
        _api.Setup(x => x.GetUserInfoAsync(target, "legacy-token"))
            .ReturnsAsync(new GitHubUserInfo { Login = "alice" });

        int exitCode = await Provider().CreateCommand().InvokeAsync(
            $"login --url https://{host} --username alice --device");
        Assert.Equal(0, exitCode);
        Assert.False(_context.CredentialStore.Contains(
            GitHubHostProvider.GetOAuthServiceName(target), "alice"));
    }

    [Theory]
    [InlineData("github.com")]
    [InlineData("gist.github.com")]
    public async Task Get_ValidCredentialDoesNotRefresh(string host)
    {
        Seed(expired: false);
        GitResponse result = await Provider().GetCredentialAsync(Request(host: host));
        Assert.Equal("old-access", result.Credential.Password);
        Assert.True(result.IsCredentialEphemeral);
        _auth.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Get_ConcurrentProvidersShareOneRefreshAndPersistRotation()
    {
        Seed();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource<OAuth2TokenResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _auth.Setup(x => x.RefreshOAuthTokenAsync(Target, "old-refresh"))
            .Returns(() => { started.SetResult(); return finish.Task; });

        Task<GitResponse> first = Provider().GetCredentialAsync(Request());
        await started.Task;
        Task<GitResponse> second = Provider().GetCredentialAsync(Request());
        finish.SetResult(Tokens());
        GitResponse[] results = await Task.WhenAll(first, second);

        Assert.All(results, result => Assert.Equal("new-access", result.Credential.Password));
        Assert.Equal("new-refresh", Stored().RefreshToken);
        Assert.Equal(_clock.GetUtcNow().AddHours(8), Stored().ExpiresAt);
        _auth.Verify(x => x.RefreshOAuthTokenAsync(Target, "old-refresh"), Times.Once);
        Assert.Equal("new-access", (await Provider().GetCredentialAsync(Request())).Credential.Password);
    }

    [Fact]
    public async Task Get_FindsPairEvenIfLegacyTokenWasNotSaved()
    {
        Seed(expired: false);
        ((ICredentialStore)_context.CredentialStore).Remove("https://github.com", "alice");
        GitResponse result = await Provider().GetCredentialAsync(Request(user: null));
        Assert.Equal("alice", result.Credential.Account);
        Assert.Equal("old-access", result.Credential.Password);
    }

    [Theory]
    [InlineData("bad_refresh_token")]
    [InlineData("invalid_grant")]
    public async Task Get_RejectedRefreshRemovesPairAndReauthenticates(string code)
    {
        Seed();
        _auth.Setup(x => x.RefreshOAuthTokenAsync(Target, "old-refresh"))
            .ThrowsAsync(new OAuth2Exception("refresh rejected") { ErrorCode = code });
        _auth.Setup(x => x.GetAuthenticationAsync(Target, "alice", It.IsAny<AuthenticationModes>()))
            .ReturnsAsync(new AuthenticationPromptResult(AuthenticationModes.Pat, new GitCredential("alice", "pat")));

        Assert.Equal("pat", (await Provider().GetCredentialAsync(Request())).Credential.Password);
        Assert.Equal(0, _context.CredentialStore.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Get_TemporaryAndConfigurationFailuresPreserveCredentials(bool configurationError)
    {
        Seed();
        Exception exception = configurationError
            ? new OAuth2Exception("client configuration unavailable") { ErrorCode = "incorrect_client_credentials" }
            : new HttpRequestException("offline");
        _auth.Setup(x => x.RefreshOAuthTokenAsync(Target, "old-refresh")).ThrowsAsync(exception);

        Assert.Same(exception, await Record.ExceptionAsync(() => Provider().GetCredentialAsync(Request())));
        Assert.Equal("old-refresh", Stored().RefreshToken);
        _auth.Verify(x => x.GetAuthenticationAsync(
            It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<AuthenticationModes>()), Times.Never);
    }

    [Fact]
    public async Task Get_ExpiredRefreshTokenPromptsWithoutCallingTokenEndpoint()
    {
        GitHubOAuthCredential credential = Seed();
        credential.RefreshTokenExpiresAt = _clock.GetUtcNow().AddSeconds(-1);
        Save(Target, credential);
        _auth.Setup(x => x.GetAuthenticationAsync(Target, "alice", It.IsAny<AuthenticationModes>()))
            .ReturnsAsync(new AuthenticationPromptResult(AuthenticationModes.Pat, new GitCredential("alice", "pat")));
        Assert.Equal("pat", (await Provider().GetCredentialAsync(Request())).Credential.Password);
        _auth.Verify(x => x.RefreshOAuthTokenAsync(It.IsAny<Uri>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task StoreAndErase_OlderGitRequestsDoNotOverwriteRotatedPair()
    {
        Seed(expired: false);
        GitHubHostProvider provider = Provider();
        await provider.StoreCredentialAsync(Request(password: "older-access"));
        await provider.EraseCredentialAsync(Request(password: "older-access"));
        Assert.Equal("old-access", Stored().AccessToken);
        await provider.EraseCredentialAsync(Request(password: "old-access"));
        Assert.False(_context.CredentialStore.Contains("https://github.com", "alice"));
        Assert.Equal("old-refresh", Stored().RefreshToken);
        _auth.Setup(x => x.RefreshOAuthTokenAsync(Target, "old-refresh")).ReturnsAsync(Tokens());
        Assert.Equal("new-access", (await provider.GetCredentialAsync(Request())).Credential.Password);
    }

    [Fact]
    public async Task Login_PatReplacementClearsRefreshMetadata()
    {
        Seed();
        Assert.Equal(0, await Provider().CreateCommand().InvokeAsync(
            "login --username alice --force --token replacement-pat"));
        Assert.False(_context.CredentialStore.Contains(GitHubHostProvider.GetOAuthServiceName(Target), "alice"));
        Assert.Equal("replacement-pat", (await Provider().GetCredentialAsync(Request())).Credential.Password);
    }

    [Fact]
    public async Task Logout_RemovesBothCredentials()
    {
        Seed();
        Assert.Equal(0, await Provider().CreateCommand().InvokeAsync("logout alice"));
        Assert.Equal(0, _context.CredentialStore.Count);
    }

    [Fact]
    public async Task Get_OmittedReplacementRefreshTokenKeepsOriginalExpiry()
    {
        GitHubOAuthCredential previous = Seed();
        OAuth2TokenResult result = Tokens(refresh: null);
        result.RefreshTokenExpiresIn = null;
        _auth.Setup(x => x.RefreshOAuthTokenAsync(Target, "old-refresh")).ReturnsAsync(result);
        await Provider().GetCredentialAsync(Request());
        Assert.Equal("old-refresh", Stored().RefreshToken);
        Assert.Equal(previous.RefreshTokenExpiresAt, Stored().RefreshTokenExpiresAt);
    }

    private ICommandContext WithStorage(ICredentialStore store, IFileSystem fileSystem = null)
    {
        var context = new Mock<ICommandContext>();
        context.SetupGet(x => x.CredentialStore).Returns(store);
        context.SetupGet(x => x.FileSystem).Returns(fileSystem ?? _context.FileSystem);
        context.SetupGet(x => x.Settings).Returns(_context.Settings);
        context.SetupGet(x => x.Console).Returns(_context.Console);
        context.SetupGet(x => x.Trace).Returns(_context.Trace);
        return context.Object;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NoStorage_DoesNotRequestRefreshAndRejectsForcedExpiration(bool serverExpires)
    {
        _context.Settings.CredentialBackingStore = Constants.CredentialStoreNames.None;
        var provider = new GitHubHostProvider(WithStorage(new NullCredentialStore()), _api.Object, _auth.Object, _clock);
        string[] scopes = null;
        _auth.Setup(x => x.GetAuthenticationAsync(Target, "alice", It.IsAny<AuthenticationModes>()))
            .ReturnsAsync(new AuthenticationPromptResult(AuthenticationModes.Device));
        _auth.Setup(x => x.GetOAuthTokenViaDeviceCodeAsync(Target, It.IsAny<IEnumerable<string>>()))
            .Callback<Uri, IEnumerable<string>>((_, requested) => scopes = requested.ToArray())
            .ReturnsAsync(serverExpires ? Tokens() : new OAuth2TokenResult("new-access", "bearer"));
        _api.Setup(x => x.GetUserInfoAsync(Target, "new-access"))
            .ReturnsAsync(new GitHubUserInfo { Login = "alice" });
        if (serverExpires)
        {
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.GetCredentialAsync(Request()));
            Assert.Contains("credentialStore", exception.Message);
        }
        else
        {
            GitResponse result = await provider.GetCredentialAsync(Request());
            Assert.False(result.IsCredentialEphemeral);
            Assert.Equal("new-access", result.Credential.Password);
        }
        Assert.DoesNotContain("offline_access", scopes);
        Assert.Empty(_context.FileSystem.Directories);
    }

    [Fact]
    public async Task AddExistingAccountWithPat_ReplacesRefreshableCredential()
    {
        Seed(expired: false);
        _context.CredentialStore.Add("https://github.com", "bob", "bob-pat");
        _auth.Setup(x => x.SelectAccountAsync(Target, It.IsAny<IEnumerable<string>>())).ReturnsAsync((string)null);
        _auth.Setup(x => x.GetAuthenticationAsync(Target, null, It.IsAny<AuthenticationModes>()))
            .ReturnsAsync(new AuthenticationPromptResult(AuthenticationModes.Pat, new GitCredential("alice", "replacement")));
        GitHubHostProvider provider = Provider();
        Assert.Equal("replacement", (await provider.GetCredentialAsync(Request(user: null))).Credential.Password);
        await provider.StoreCredentialAsync(Request(password: "replacement"));
        Assert.Equal("replacement", (await provider.GetCredentialAsync(Request())).Credential.Password);
        Assert.False(_context.CredentialStore.Contains(GitHubHostProvider.GetOAuthServiceName(Target), "alice"));
    }

    [Theory]
    [InlineData("https://alice@github.com", null)]
    [InlineData("https://github.com/org/repo", "org/repo")]
    public async Task LoginAndLogout_UseTheSameCanonicalServicesAsGit(string url, string path)
    {
        _auth.Setup(x => x.GetOAuthTokenViaDeviceCodeAsync(It.IsAny<Uri>(), It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(Tokens());
        _api.Setup(x => x.GetUserInfoAsync(It.IsAny<Uri>(), "new-access"))
            .ReturnsAsync(new GitHubUserInfo { Login = "alice" });
        GitHubHostProvider provider = Provider();
        Assert.Equal(0, await provider.CreateCommand().InvokeAsync($"login --url {url} --device"));
        Assert.Equal("new-access", (await provider.GetCredentialAsync(Request(path: path))).Credential.Password);
        Assert.Equal(0, await provider.CreateCommand().InvokeAsync($"logout alice --url {url}"));
        Assert.Equal(0, _context.CredentialStore.Count);
    }

    [Fact]
    public async Task Erase_CanRemoveCorruptRefreshMetadata()
    {
        _context.CredentialStore.Add(GitHubHostProvider.GetOAuthServiceName(Target), "alice", "invalid-json");
        _context.CredentialStore.Add("https://github.com", "alice", "access");
        await Provider().EraseCredentialAsync(Request(password: "access"));
        Assert.Equal(0, _context.CredentialStore.Count);
    }

    [Theory]
    [InlineData("get")]
    [InlineData("list")]
    [InlineData("store")]
    [InlineData("erase")]
    public async Task FileBackedStore_DoesNotReadWhileAnotherProviderIsRotating(string operation)
    {
        string root = Directory.CreateTempSubdirectory("gcm-oauth-test-").FullName;
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.SetupGet(x => x.UserDataDirectoryPath).Returns(Path.Combine(root, "gcm"));
        fileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns<string>(Directory.Exists);
        fileSystem.Setup(x => x.CreateDirectory(It.IsAny<string>()))
            .Callback<string>(path => Directory.CreateDirectory(path));
        fileSystem.Setup(x => x.EnumerateFiles(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>(Directory.EnumerateFiles);
        fileSystem.Setup(x => x.DeleteFile(It.IsAny<string>())).Callback<string>(File.Delete);
        bool rotating = false;
        int readsDuringRotation = 0;
        fileSystem.Setup(x => x.OpenFileStream(
                It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>()))
            .Returns<string, FileMode, FileAccess, FileShare>((path, mode, access, share) =>
            {
                if (rotating && mode == FileMode.Open && path.EndsWith(".credential"))
                {
                    readsDuringRotation++;
                }
                return File.Open(path, mode, access, share);
            });
        var store = new PlaintextCredentialStore(fileSystem.Object, Path.Combine(root, "credentials"));
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource<OAuth2TokenResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            GitHubOAuthCredential credential = Seed();
            store.AddOrUpdate(GetOAuthService(), "alice",
                JsonSerializer.Serialize(credential, GitHubCredentialJsonContext.Default.GitHubOAuthCredential));
            store.AddOrUpdate("https://github.com", "alice", "old-access");
            _auth.Setup(x => x.RefreshOAuthTokenAsync(Target, "old-refresh")).Returns(() =>
            {
                rotating = true;
                started.SetResult();
                return finish.Task;
            });
            ICommandContext context = WithStorage(store, fileSystem.Object);
            var firstProvider = new GitHubHostProvider(context, _api.Object, _auth.Object, _clock);
            var secondProvider = new GitHubHostProvider(context, _api.Object, _auth.Object, _clock);
            Task<GitResponse> first = firstProvider.GetCredentialAsync(Request());
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Task second = operation switch
            {
                "get" => secondProvider.GetCredentialAsync(Request()),
                "list" => secondProvider.CreateCommand().InvokeAsync("list"),
                "store" => secondProvider.StoreCredentialAsync(Request(password: "old-access")),
                "erase" => secondProvider.EraseCredentialAsync(Request(password: "old-access")),
                _ => throw new InvalidOperationException()
            };
            rotating = false;
            finish.SetResult(Tokens());
            await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(0, readsDuringRotation);
            Assert.Equal("new-access", (await first).Credential.Password);
            Assert.Contains("new-refresh", store.Get(GetOAuthService(), "alice").Password);
            _auth.Verify(x => x.RefreshOAuthTokenAsync(Target, "old-refresh"), Times.Once);
        }
        finally
        {
            rotating = false;
            finish.TrySetResult(Tokens());
            Directory.Delete(root, recursive: true);
        }

        static string GetOAuthService() => GitHubHostProvider.GetOAuthServiceName(Target);
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 12, 0, 0, 0, TimeSpan.Zero);
    }
}
