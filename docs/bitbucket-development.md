# Bitbucket Authentication, 2FA and OAuth

By default for authenticating against private Git repositories Bitbucket
supports SSH and username/password Basic Auth over HTTPS. Username/password
Basic Auth over HTTPS is also available for REST API access. Additionally
Bitbucket supports App-specific passwords which can be used via Basic Auth as
username/app-specific-password.

To enhance security Bitbucket offers optional Two-Factor Authentication (2FA).
When 2FA is enabled username/password Basic Auth access to the REST APIs and to
Git repositories is suspended. At that point users are left with the choice of
username/apps-specific-password Basic Auth for REST APIs and Git interactions,
OAuth for REST APIs and Git/Hg interactions or SSH for Git/HG interactions and
one of the previous choices for REST APIs. SSH and REST API access are beyond
the scope of this document. Read about [Bitbucket's 2FA implementation][2fa-impl].

App-specific passwords are not particularly user friendly as once created
Bitbucket hides their value, even from the owner. They are intended for use
within application that talk to Bitbucket where application can remember and use
the app-specific-password. [Additional information][additional-info].

OAuth is the intended authentication method for user interactions with HTTPS
remote URL for Git repositories when 2FA is active. Essentially once a client
application has an OAuth access token it can be used in place of a user's
password. Read more about information [Bitbucket's OAuth implementation][oauth-impl].

Bitbucket's OAuth implementation follows the standard specifications for OAuth
2.0, which is out of scope for this document. However it implements a
comparatively rare part of OAuth 2.0 Refresh Tokens. Bitbucket's Access Token's
expire after 1 hour if not revoked, as opposed to GitHub's that expire after 1
year. When GitHub's Access Tokens expire the user must anticipate in the
standard OAuth authentication flow to get a new Access Token. Since this occurs,
in theory, once per year this is not too onerous. Since Bitbucket's Access
Tokens expire every hour it is too much to expect a user to go through the OAuth
authentication flow every hour.Bitbucket implements refresh Tokens.
Refresh Tokens are issued to the client application at the same time as Access
Tokens. They can only be used to request a new Access Token, and then only if
they have not been revoked. As such the support for Bitbucket and the use of its
OAuth in the Git Credentials Manager differs significantly from how VSTS and
GitHub are implemented. This is explained in more detail below.

## Multiple User Accounts

Unlike the GitHub implementation within the Git Credential Manager, the
Bitbucket implementation stores 'secrets', passwords, app-specific passwords, or
OAuth tokens, with usernames in the [Windows Credential Manager][wincred-manager]
vault.

Depending on the circumstances this means either saving an explicit username in
to the Windows Credential Manager/Vault or including the username in the URL
used as the identifying key of entries in the Windows Credential Manager vault,
i.e. using a key such as `git:https://mminns@bitbucket.org/` rather than
`git:https://bitbucket.org`. This means that the Bitbucket implementation in the
GCM can support multiple accounts, and usernames,  for a single user against
Bitbucket, e.g. a personal account and a work account.

## On-Premise Bitbucket

On-premise Bitbucket, more correctly known as Bitbucket Server or Bitbucket DC,
has a number of differences compared to the cloud instance of Bitbucket,
[bitbucket.org][bitbucket].

It is possible to test with Bitbucket Server by running it locally using the
following command from the Atlassian SDK:

    ❯ atlas-run-standalone --product bitbucket

See the developer documentation for [atlas-run-standalone][atlas-run-standalone].

This will download and run a standalone instance of Bitbucket Server which can
be accessed using the credentials `admin`/`admin` at

    https://localhost:7990/bitbucket

Atlassian has [documentation][atlassian-sdk] on how to download and install
their SDK.

## OAuth2 Configuration

Bitbucket DC [7.20](https://confluence.atlassian.com/bitbucketserver/bitbucket-data-center-and-server-7-20-release-notes-1101934428.html)
added support for OAuth2 Incoming Application Links and this can be used to
support OAuth2 authentication for Git. This is especially useful in environments
where Bitbucket uses SSO as it removes the requirement for users to manage
[SSH keys](https://confluence.atlassian.com/display/BITBUCKETSERVER0717/Using+SSH+keys+to+secure+Git+operations)
or manual [HTTP access tokens](https://confluence.atlassian.com/display/BITBUCKETSERVER0717/Personal+access+tokens).

### Host Configuration

For more details see
[Bitbucket's documentation on Data Center and Server Application Links to other Applications](https://confluence.atlassian.com/bitbucketserver/link-to-other-applications-1018764620.html)

Create Incoming OAuth 2 Application Link:
<!-- markdownlint-disable MD034 -->
1. Navigate to Administration/Application Links
1. Create Link
   1. Screen 1
      - External Application [check]
      - Incoming Application [check]
   1. Screen 2
      - Name : GCM
      - Redirect URL : `http://localhost:34106/`
      - Application Permissions : Repositories.Read [check], Repositories.Write [check]
   1. Save
   <!-- markdownlint-enable MD034 -->
   1. Copy the `ClientId` and `ClientSecret` to configure GCM

### Client Configuration

Set the OAuth2 configuration use the `ClientId` and `ClientSecret` copied above,
(for details see [credential.bitbucketDataCenterOAuthClientId](configuration.md#credential.bitbucketDataCenterOAuthClientId)
and [credential.bitbucketDataCenterOAuthClientSecret](configuration.md#credential.bitbucketDataCenterOAuthClientSecret))

    ❯ git config --global credential.bitbucketDataCenterOAuthClientId {`Copied ClientId`}

    ❯ git config --global credential.bitbucketDataCenterOAuthClientSecret {`Copied ClientSecret`}
<!-- markdownlint-disable MD034 -->
As described in [Configuration options](configuration.md#Configuration%20options)
the settings can be made more specific to apply only to a specific Bitbucket DC
host by specifying the host url, e.g. https://bitbucket.example.com/
<!-- markdownlint-enable MD034 -->

    ❯ git config --global credential.https://bitbucket.example.com.bitbucketDataCenterOAuthClientId {`Copied ClientId`}

    ❯ git config --global credential.https://bitbucket.example.com.bitbucketDataCenterOAuthClientSecret {`Copied ClientSecret`}
<!-- markdownlint-disable MD034 -->
Due to the way GCM resolves hosts and determines REST API urls, if the Bitbucket
DC instance is hosted under a relative url (e.g. https://example.com/bitbucket)
it is necessary to configure Git to send the full path to GCM. This is done
using the [credential.useHttpPath](configuration.md#credential.useHttpPath)
setting.
    ❯ git config --global credential.https://example.com/bitbucket.usehttppath true
<!-- markdownlint-enable MD034 -->

If a port number is used in the url of the Bitbucket DC instance the Git
configuration needs to reflect this. However, due to [Issue 608](https://github.com/git-ecosystem/git-credential-manager/issues/608)
the port is ignored when resolving [credential.bitbucketDataCenterOAuthClientId](configuration.md#credential.bitbucketDataCenterOAuthClientId)
and [credential.bitbucketDataCenterOAuthClientSecret](configuration.md#credential.bitbucketDataCenterOAuthClientSecret).
<!-- markdownlint-disable MD034 -->
For example, a Bitbucket DC host at https://example.com:7990/bitbucket would
require configuration in the form:
<!-- markdownlint-enable MD034 -->
    ❯ git config --global credential.https://example.com/bitbucket.bitbucketDataCenterOAuthClientId {`Copied ClientId`}

    ❯ git config --global credential.https://example.com/bitbucket.bitbucketDataCenterOAuthClientSecret {`Copied ClientSecret`}

    ❯ git config --global credential.https://example.com:7990/bitbucket.usehttppath true

[additional-info]:https://confluence.atlassian.com/display/BITBUCKET/App+passwords
[atlas-run-standalone]: https://developer.atlassian.com/server/framework/atlassian-sdk/atlas-run-standalone/
[bitbucket]: https://bitbucket.org
[2fa-impl]: https://confluence.atlassian.com/bitbucket/two-step-verification-777023203.html
[oauth-impl]: https://confluence.atlassian.com/bitbucket/oauth-on-bitbucket-cloud-238027431.html
[atlassian-sdk]: https://developer.atlassian.com/server/framework/atlassian-sdk/
[wincred-manager]: https://msdn.microsoft.com/en-us/library/windows/desktop/aa374792(v=vs.85).aspx
