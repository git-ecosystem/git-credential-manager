# Generic Host Provider OAuth

Many Git hosts use the popular standard OAuth2 or OpenID Connect (OIDC)
authentication mechanisms to secure repositories they host.
Git Credential Manager supports any generic OAuth2-based Git host by simply
setting some configuration.

## Registering an OAuth application

In order to use GCM with a Git host that supports OAuth you must first have
registered an OAuth application with your host. The instructions on how to do
this can be found with your Git host provider's documentation.

When registering a new application, you should make sure to set an HTTP-based
redirect URL that points to `localhost`; for example:

```text
http://localhost
http://localhost:<port>
http://127.0.0.1
http://127.0.0.1:<port>
```

Note that you cannot use an HTTPS redirect URL. GCM does not require a specific
port number be used; if your Git host requires you to specify a port number in
the redirect URL then GCM will use that. Otherwise an available port will be
selected at the point authentication starts.

You must ensure that all scopes required to read and write to Git repositories
have been granted for the application or else credentials that are generated
will cause errors when pushing or fetching using Git.

As part of the registration process you should also be given a Client ID and,
optionally, a Client Secret. You will need both of these to configure GCM.

## Configure GCM

In order to configure GCM to use OAuth with your Git host you need to set the
following values in your Git configuration:

- Client ID
- Client Secret (optional)
- Redirect URL (optional, defaults to `http://127.0.0.1`)
- Scopes (optional)
- OAuth Endpoints
  - Authorization Endpoint
  - Token Endpoint
  - Device Code Authorization Endpoint (optional)

OAuth endpoints can be found by consulting your Git host's OAuth app development
documentation. The URLs can be either absolute or relative to the host name;
for example: `https://example.com/oauth/authorize` or `/oauth/authorize`.

In order to set these values, you can run the following commands, where `<HOST>`
is the hostname of your Git host:

```shell
git config --global credential.<HOST>.oauthClientId <ClientID>
git config --global credential.<HOST>.oauthClientSecret <ClientSecret>
git config --global credential.<HOST>.oauthRedirectUri <RedirectURL>
git config --global credential.<HOST>.oauthAuthorizeEndpoint <AuthEndpoint>
git config --global credential.<HOST>.oauthTokenEndpoint <TokenEndpoint>
git config --global credential.<HOST>.oauthScopes <Scopes>
git config --global credential.<HOST>.oauthDeviceEndpoint <DeviceEndpoint>
```

**Example commands:**

- `git config --global credential.https://example.com.oauthClientId C33F2751FB76`

- `git config --global credential.https://example.com.oauthScopes "code:write profile:read"`

**Example Git configuration**

```ini
[credential "https://example.com"]
    oauthClientId = 9d886e36-5771-4f2b-8c8b-420c68ad5baa
    oauthClientSecret = 4BC5BD4704EAE28FD832
    oauthRedirectUri = "http://127.0.0.1"
    oauthAuthorizeEndpoint = "/login/oauth/authorize"
    oauthTokenEndpoint = "/login/oauth/token"
    oauthDeviceEndpoint = "/login/oauth/device"
    oauthScopes = "code:write profile:read"
    oauthDefaultUserName = "OAUTH"
    oauthUseClientAuthHeader = false
```

### Additional configuration

Depending on the specific implementation of OAuth with your Git host you may
also need to specify additional behavior.

#### Token user name

If your Git host requires that you specify a username to use with OAuth tokens
you can either include the username in the Git remote URL, or specify a default
option via Git configuration.

Example Git remote with username: `https://username@example.com/repo.git`.
In order to use special characters you need to URL encode the values; for
example `@` becomes `%40`.

By default GCM uses the value `OAUTH-USER` unless specified in the remote URL,
or overridden using the `credential.<HOST>.oauthDefaultUserName` configuration.

#### Include client authentication in headers

If your Git host's OAuth implementation has specific requirements about whether
the client ID and secret should or should not be included in an `Authorization`
header during OAuth requests, you can control this using the following setting:

```shell
git config --global credential.<HOST>.oauthUseClientAuthHeader <true|false>
```

The default behavior is to include these values; i.e., `true`.
