# Bitbucket Authentication, 2FA and OAuth

By default for authenticating against private Git repositories Bitbucket supports SSH and username/password Basic Auth over HTTPS.
Username/password Basic Auth over HTTPS is also available for REST API access.
Additionally Bitbucket supports App-specific passwords which can be used via Basic Auth as username/app-specific-password.

To enhance security Bitbucket offers optional Two-Factor Authentication (2FA). When 2FA is enabled username/password Basic Auth access to the REST APIs and to Git repositories is suspended.
At that point users are left with the choice of username/apps-specific-password Basic Auth for REST APIs and Git interactions, OAuth for REST APIs and Git/Hg interactions or SSH for Git/HG interactions and one of the previous choices for REST APIs.
SSH and REST API access are beyond the scope of this document.
Read about [Bitbucket's 2FA implementation](https://confluence.atlassian.com/bitbucket/two-step-verification-777023203.html).

App-specific passwords are not particularly user friendly as once created Bitbucket hides their value, even from the owner.
They are intended for use within application that talk to Bitbucket where application can remember and use the app-specific-password.
[Additional information](https://confluence.atlassian.com/display/BITBUCKET/App+passwords).

OAuth is the intended authentication method for user interactions with HTTPS remote URL for Git repositories when 2FA is active.
Essentially once a client application has an OAuth access token it can be used in place of a user's password.
Read more about information [Bitbucket's OAuth implementation](https://confluence.atlassian.com/bitbucket/oauth-on-bitbucket-cloud-238027431.html).

Bitbucket's OAuth implementation follows the standard specifications for OAuth 2.0, which is out of scope for this document.
However it implements a comparatively rare part of OAuth 2.0 Refresh Tokens.
Bitbucket's Access Token's expire after 1 hour if not revoked, as opposed to GitHub's that expire after 1 year.
When GitHub's Access Tokens expire the user must anticipate in the standard OAuth authentication flow to get a new Access Token. Since this occurs, in theory, once per year this is not too onerous.
Since Bitbucket's Access Tokens expire every hour it is too much to expect a user to go through the OAuth authentication flow every hour.
Bitbucket implements refresh Tokens.
Refresh Tokens are issued to the client application at the same time as Access Tokens.
They can only be used to request a new Access Token, and then only if they have not been revoked.
As such the support for Bitbucket and the use of its OAuth in the Git Credentials Manager differs significantly from how VSTS and GitHub are implemented.
This is explained in more detail below.

## Multiple User Accounts

Unlike the GitHub implementation within the Git Credential Manager, the Bitbucket implementation stores 'secrets', passwords, app-specific passwords, or OAuth tokens, with usernames in the [Windows Credential Manager](https://msdn.microsoft.com/en-us/library/windows/desktop/aa374792(v=vs.85).aspx) vault.

Depending on the circumstances this means either saving an explicit username in to the Windows Credential Manager/Vault or including the username in the URL used as the identifying key of entries in the Windows Credential Manager vault, i.e. using a key such as `git:https://mminns@bitbucket.org/` rather than `git:https://bitbucket.org`.
This means that the Bitbucket implementation in the GCM can support multiple accounts, and usernames,  for a single user against Bitbucket, e.g. a personal account and a work account.

## Authentication User Experience

When the GCM is triggered by Git, the GCM will check the `host` parameter passed to it.
If it contains `bitbucket.org` it will trigger the Bitbucket related processes.

### Basic Authentication

If the GCM needs to prompt the user for credentials they will always be shown an initial dialog where they can enter a username and password. If the `username` parameter was passed into the GCM it is used to pre-populate the username field, although it can be overridden.
When username and password credentials are submitted the GCM will use them to attempt to retrieve a token, for Basic Authentication this token is in effect the password the user just entered.
The GCM retrieves this `token` by checking the password can be used to successfully retrieve the User profile via the Bitbucket REST API.

If the username and password credentials sent as Basic Authentication credentials works, then the password is identified as the token. The credentials, the username and the password/token, are then stored and the values returned to Git.

If the request for the User profile via the REST API fails with a 401 return code it indicates the username/password combination is invalid, nothing is stored and nothing is returned to Git.

However if the request fails with a 403 (Forbidden) return code, this indicates that the username and password are valid but 2FA is enabled on the Bitbucket Account.
When this occurs the user it prompted to complete the OAuth authentication process.

### OAuth

OAuth authentication prompts the User with a new dialog where they can trigger OAuth authentication.
This involves opening a browser request to `_https://bitbucket.org/site/oauth2/authorize?response_type=code&client_id={consumerkey}&state=authenticated&scope={scopes}&redirect_uri=http://localhost:34106/_`.
This will trigger a flow on Bitbucket where the user must login, potentially including a 2FA prompt, and authorize the GCM to access Bitbucket with the specified scopes.
The GCM will spawn a temporary, local webserver, listening on port 34106, to handle the OAuth redirect/callback.
Assuming the user successfully logins into Bitbucket and authorizes the GCM this callback will include the Access and Refresh Tokens.

The Access and Refresh Tokens will be stored against the username and the username/Access Token credentials returned to Git.

# On-Premise Bitbucket

On-premise Bitbucket, more correctly known as Bitbucket Server or Bitbucket DC, has a number of differences compared to the cloud instance of Bitbucket, https://bitbucket.org. 

As far as GCMC is concerned the main difference it doesn't support OAuth so only Basic Authentication is available.

It is possible to test with Bitbucket Server by running it locally using the following command from the Atlassian SDK:

	❯ atlas-run-standalone --product bitbucket

See https://developer.atlassian.com/server/framework/atlassian-sdk/atlas-run-standalone/.

This will download and run a standalone instance of Bitbucket Server which can be accessed using the credentials `admin`/`admin` at

	https://localhost:7990/bitbucket

Instructions on how to download and install the Atlassian SDK can be found here: https://developer.atlassian.com/server/framework/atlassian-sdk/
