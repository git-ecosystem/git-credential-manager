# NTLM and Kerberos Authentication

## Background

NTLM and Kerberos are two authentication protocols that are commonly used in
Windows environments.

In Git Credential Manager (GCM), we refer to these protocols under the umbrella
term "Windows Integrated Authentication".

### NTLM

[NTLM (NT LAN Manager)][ntlm-wiki] is a challenge-response authentication
protocol used in various Microsoft network protocols, such as
[SMB file sharing][smb-docs].

> [!CAUTION]
> NTLM is now considered _**insecure**_ due to weak cryptographic algorithms and
> vulnerabilities to various attacks, such as pass-the-hash and relay attacks.
> As such, it is not recommended for use in modern applications.
>
> There are several versions of NTLM, with NTLMv2 being the latest, however
> **all versions** are considered weak by modern security standards.
>
> Microsoft lists [NTLM as a deprecated protocol][ntlm-deprecated] and has
> removed NTLMv1 from Windows as of Windows 11 build 24H2 / Server 2025.

NTLM is advertised by HTTP servers using the `WWW-Authenticate: NTLM` header.
When a client receives this header, it can respond with an NTLM authentication
message to prove its identity.

### Kerberos

[Kerberos][kerberos-wiki], on the other hand, is a more secure and robust
authentication protocol that uses tickets to authenticate users and services.
It is the recommended authentication protocol for Windows domains and is widely
used in enterprise environments.

Unlike NTLM, Kerberos is typically not directly advertised by HTTP servers, but
is instead advertised using "SPNEGO" and the `WWW-Authenticate: Negotiate`
header.

#### GSS-API Negotiate and SPNEGO

Kerberos (or NTLM) authentication is typically initially established using the
[GSS-API][gssapi-wiki] ([RFC 2743][gssapi-rfc]) negotiation mechanism
["SPNEGO"][spnego-wiki] ([RFC 4178][spnego-rfc]). SPNEGO allows the client and
server to agree on which authentication protocol to use (Kerberos or NTLM) based
on their capabilities. Typically Kerberos is preferred if both the client and
server support it, with NTLM acting as a fallback.

## Built-in Support in Git

Git provides built-in support for NTLM and Kerberos authentication through the
use of [libcurl][libcurl], which is the underlying library used by Git for HTTP
and HTTPS communications. When Git is compiled with libcurl support, it can
leverage the authentication mechanisms provided by libcurl, including NTLM and
Kerberos.

On Windows, Git can use the native Windows [SSPI][sspi-wiki] (Security Support
Provider Interface) to perform NTLM and Kerberos authentication. This allows Git
to integrate seamlessly with the Windows authentication infrastructure.

> [!NOTE]
> As of Git for Windows version 2.XX.X, **NTLM support is disabled by default**.
> Kerberos support _remains enabled_.

### Re-enabling NTLM Support

You can re-enable NTLM support in Git for Windows for a particular remote by
setting Git config option [`http.<url>.allowNTLMAuth`][ntlm-config] to `true`.
For example, to enable NTLM authentication for `https://example.com`, you would
run the following command:

```shell
git config --global http.https://example.com.allowNTLMAuth true
```

> [!WARNING]
> Enabling NTLM authentication may expose you to security risks, as NTLM is
> considered insecure. It is recommended to use Kerberos authentication where
> possible, and to only use NTLM with trusted servers in secure environments.

> [!WARNING]
> Only ever use NTLM authentication over secure connections (i.e., HTTPS) to
> protect against eavesdropping and man-in-the-middle attacks.

When using GCM with a remote that supports NTLM authentication, GCM will warn
you if NTLM authentication is not enabled in Git but the remote server
advertises NTLM support.

![GCM warning prompt that NTLM is disabled inside of Git][ntlm-warning-image]

* Selecting "Just this time" will continue with NTLM authentication, but only
  for the current operation. The next time you interact with that remote, you
  will be prompted again.

* Selecting "Always for this remote" will set the `http.<url>.allowNTLMAuth`
  configuration option to `true` for that remote, and continue with NTLM
  authentication.

* Selecting "No" will prompt for a basic username/password credential, and Git's
  NTLM authentication support will remain disabled. If the remote server only
  supports NTLM then authentication will fail.

### Seamless Authentication

When using NTLM or Kerberos authentication with Git on Windows, it is possible
to achieve seamless authentication without prompting for credentials. This is
because Git can leverage the existing Windows user credentials to authenticate
with the server.

This means that if you are logged into your Windows account, Git can use those
credentials to authenticate with the remote server automatically, without
prompting you for a username or password.

This feature is enabled by default in Git. To disable this behavior, you can set
the [`http.<url>.emptyAuth`][emptyauth] configuration option to `false`. For
example, to disable seamless authentication for `https://example.com`, you would
run the following command:

```shell
git config --global http.https://example.com.emptyAuth false
```

If you disable seamless authentication, Git will prompt you for credentials
when accessing a remote that advertises NTLM or Kerberos support rather than
using the current Windows user's credentials.

[ntlm-wiki]: https://en.wikipedia.org/wiki/NTLM
[kerberos-wiki]: https://en.wikipedia.org/wiki/Kerberos_(protocol)
[smb-docs]: https://learn.microsoft.com/en-gb/windows/win32/fileio/microsoft-smb-protocol-and-cifs-protocol-overview
[ntlm-deprecated]: https://learn.microsoft.com/en-us/windows/whats-new/deprecated-features
[ntlm-config]: https://git-scm.com/docs/git-config#Documentation/git-config.txt-httpallowNTLMAuth
[gssapi-rfc]: https://datatracker.ietf.org/doc/html/rfc2743
[gssapi-wiki]: https://en.wikipedia.org/wiki/GSSAPI
[spnego-rfc]: https://datatracker.ietf.org/doc/html/rfc4178
[spnego-wiki]: https://en.wikipedia.org/wiki/SPNEGO
[libcurl]: https://curl.se/libcurl/
[sspi-wiki]: https://en.wikipedia.org/wiki/Security_Support_Provider_Interface
[emptyauth]: https://git-scm.com/docs/git-config#Documentation/git-config.txt-httpemptyAuth
[ntlm-warning-image]: img/ntlm-warning.png
