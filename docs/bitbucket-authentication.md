# Bitbucket Authentication

When GCM is triggered by Git, it will check the `host` parameter passed
to it. If this parameter contains `bitbucket.org` it will trigger Bitbucket
authentication and prompt you for credentials. In this scenario, you have two
options for authentication: `OAuth` or `Password/Token`.

### OAuth

The dialog GCM presents for authentication contains two tabs. The first tab
(labeled `Browser`) will trigger OAuth Authentication. Clicking the `Sign in
with your browser` button opens a browser request to
`_https://bitbucket.org/site/oauth2/authorize?response_type=code&client_id={consumerkey}&state=authenticated&scope={scopes}&redirect_uri=http://localhost:34106/_`. This triggers a flow on Bitbucket requiring you to log in
(and potentially complete 2FA) to authorize GCM to access Bitbucket with the
specified scopes. GCM will then spawn a temporary local webserver, listening on
port 34106, to handle the OAuth redirect/callback. Assuming you successfully
log into Bitbucket and authorize GCM, this callback will include the appropriate
tokens for GCM to handle authencation. These tokens are then stored in your
configured [credential store][credstores] and are returned to Git.

### Password/Token

**Note:** Bitbucket Data Center, also known as Bitbucket Server or Bitbucket On
Premises, only supports Basic Authentication - please follow the below
instructions if you are using this product.

The dialog GCM presents for authentication contains two tabs. The second tab
(labeled `Password/Token`) will trigger Basic Authentication. This tab contains
two fields, one for your username and one for your password or token. If the
`username` parameter was passed into GCM, that will pre-populate the username
field, although it can be overridden. Enter your username (if needed) and your
password or token (i.e. Bitbucket App Password) and click `Sign in`.

:rotating_light: Requirements for App Passwords :rotating_light:

If you are planning to use an [App Password][app-password] for basic
authentication, it must at a minimum have _Account Read_ permissions (as shown
below). If your App Password does not have these permissions, you will be
re-prompted for credentials on every interaction with the server.

![][app-password-example]

When your username and password are submitted, GCM will attempt to retrieve a
basic authentication token for these credentials via the Bitbucket REST API. If
this is successful, the credentials, username, and password/token are stored in
your configured [credential store][credstores] and are returned to Git.

If the API request fails with a 401 return code, the entered username/password
combination is invalid; nothing is stored and nothing is returned to Git. In
this scenario, re-attempt authentication, ensuring your credentials are correct.

If the API request fails with a 403 (Forbidden) return code, the username and
password are valid, but 2FA is enabled on the corresponding Bitbucket Account.
In this scenario, you will be prompted to complete the OAuth authentication
process.  If this is successful, the credentials, username, and password/token
are stored in your configured [credential store][credstores] and are returned to
Git.

[app-password]: https://support.atlassian.com/bitbucket-cloud/docs/app-passwords/
[app-password-example]: img/app-password.png
[credstores]: ./credstores.md
