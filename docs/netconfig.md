# Network and HTTP configuration

Git Credential Manager Core's network and HTTP(S) behavior can be configured in a few different ways via [environment variables](environment.md) and [configuration options](configuration.md).

## HTTP Proxy

If your computer sits behind a network firewall that requires the use of a proxy server to reach repository remotes or the wider Internet, there are various methods for configuring GCM to use a proxy.

The simplist way to configure a proxy for _all_ HTTP(S) remotes is to [use the standard Git HTTP(S) proxy setting `http.proxy`](https://git-scm.com/docs/git-config#Documentation/git-config.txt-httpproxy).

For example to configure a proxy for all remotes for the current user:

```shell
git config --global http.proxy http://proxy.example.com
```

To specify a proxy for a particular remote you can [use the `remote.<name>.proxy` repository-level setting](https://git-scm.com/docs/git-config#Documentation/git-config.txt-remoteltnamegtproxy), for example:

```shell
git config --local remote.origin.proxy http://proxy.example.com
```

The advantage to using these standard configuration options is that in addition to GCM being configured to use the proxy, Git itself will be configured at the same time. This is probably the most commonly desired case in environments behind an Internet-blocking firewall.

### Authenticated proxies

Some proxy servers do not accept anonymous connections and require authentication. In order to specify the credentials to be used with a proxy, you can specify the username and password as part of the proxy URL setting.

The format follows [RFC 3986 section 3.2.1](https://tools.ietf.org/html/rfc3986#section-3.2.1) by including the credentials in the 'user information' part of the URI. The password is optional.

```text
protocol://username[:password]@hostname
```

For example, to specify the username `john.doe` and the password `letmein123` for the proxy server `proxy.example.com`:

```text
https://john.doe:letmein123@proxy.example.com
```

If you have special characters (as defined by [RFC 3986 section 2.2](https://tools.ietf.org/html/rfc3986#section-2.2)) in your username or password such as `:`, `@`, or any other non-URL friendly character you can URL-encode them ([section 2.1](https://tools.ietf.org/html/rfc3986#section-2.2)).

For example, a space character would be encoded with `%20`.

### Other proxy options

GCM Core supports other ways of configuring a proxy for convenience and compatibility.

1. GCM-specific configuration options (_**only** respected by GCM; **deprecated**_):
   - `credential.httpProxy`
   - `credential.httpsProxy`
1. cURL environment variables (_also respected by Git_):
   - `HTTP_PROXY`
   - `HTTPS_PROXY`
   - `ALL_PROXY`
1. `GCM_HTTP_PROXY` environment variable (_**only** respected by GCM; **deprecated**_)

## TLS Verification

If you are using self-signed TLS (SSL) certificates with a self-hosted host provider such as GitHub Enteprise Server or Azure DevOps Server (previously TFS), you may see the following error message when attempting to connect using Git and/or GCM:

```shell
$ git clone https://ghe.example.com/john.doe/myrepo
fatal: The remote certificate is invalid according to the validation procedure.
```

The **recommended and safest option** is to acquire a TLS certificate signed by a public trusted certificate authority (CA). There are multiple public CAs; here is a non-exhaustive list to consider: [Let's Encrypt](https://letsencrypt.org/), [Comodo](https://www.comodoca.com/), [Digicert](https://www.digicert.com/), [GoDaddy](https://www.godaddy.com/web-security/ssl-certificate), [GlobalSign](https://www.globalsign.com/en/ssl/).

If it is not possible to **obtain a TLS certifiate from a trusted 3rd party** then you should try to add the _specific_ self-signed certificate or one of the CA certificates in the verification chain to your operating system's trusted certificate store ([macOS](https://support.apple.com/en-gb/guide/keychain-access/kyca2431/mac), [Windows](https://blogs.technet.microsoft.com/sbs/2008/05/08/installing-a-self-signed-certificate-as-a-trusted-root-ca-in-windows-vista/)).

If you are _unable_ to either **obtain a trusted certificate**, or trust the self-signed certificate you can disable certificate verification in Git and GCM.

---
**Security Warning** :warning:

Disabling verification of TLS (SSL) certificates removes protection against a [man-in-the-middle (MITM) attack](https://en.wikipedia.org/wiki/Man-in-the-middle_attack).

Only disable certificate verification if you are sure you need to, are aware of all of the risks, and are unable to trust specific self-signed certificates (as described above).

---

The [environment variable `GIT_SSL_NO_VERIFY`](https://git-scm.com/book/en/v2/Git-Internals-Environment-Variables#_networking) and [Git configuration option `http.sslVerify`](https://git-scm.com/docs/git-config#Documentation/git-config.txt-httpsslVerify) can be used to control TLS (SSL) certifcate verification.

To disable verification for a specific remote (for example <https://example.com>):

```shell
git config --global http.https://example.com.sslVerify false
```

To disable verification for the current user for **_all remotes_** (**not recommended**):

```shell
# Environment variable (Windows)
SET GIT_SSL_NO_VERIFY=1

# Environment variable (macOS/Linux)
export GIT_SSL_NO_VERIFY=1

# Git configuration (Windows/macOS/Linux)
git config --global http.sslVerify false
```

---

**Note:** You may also experience similar verification errors if you are using a network traffic inspection tool such as [Telerik Fiddler](https://www.telerik.com/fiddler). If you are using such tools please consult their documentation for trusting the proxy root certificates.
