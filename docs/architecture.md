# Architecture

## Overview

```text
+------------------------------------------------------------------------------+
|                                                                              |
|                           Git-Credential-Manager                             |
|                                                                              |
+-+-------------+--------------+-----+---------------------+-----------------+-+
  |             |              |     |                     |                 |
  |             |              |     |             Windows |         Windows |
  |             |              |     |                     |                 |
  | +-----------v-----------+  |     |    +----------------v---------------+ |
  | |                       |  |     |    |                                | |
  | |        GitHub         <-------------+        GitHub.UI.Windows       | |
  | |                       |  |     |    |                                | |
  | +-+---------------------+  |     |    +-+------------------------------+ |
  |   |                        |     |      |                                |
  |   |  +---------------------v-+   |      | +------------------------------v-+
  |   |  |                       |   |      | |                                |
  |   |  |  Atlassian.Bitbucket  <------------+ Atlassian.Bitbucket.UI.Windows |
  |   |  |                       |   |      | |                                |
  |   |  +-+---------------------+   |      | +---------------+----------------+
  |   |    |                         |      |                 |
  |   |    |  +----------------------v-+    |                 |
  |   |    |  |                        |    |                 |
  |   |    |  |  Microsoft.AzureRepos  |    |                 |
  |   |    |  |                        |    |                 |
  |   |    |  +-----------+------------+    |                 |
  |   |    |              |                 |                 |
+-v---v----v--------------v------------+  +-v-----------------v----------------+
|                                      |  |                                    |
|                 Core                 <--+               Core.UI              |
|                                      |  |                                    |
+--------------------------------------+  +------------------------------------+
```

Git Credential Manager (GCM) is built to be Git host and platform/OS
agnostic. Most of the shared logic (command execution, the abstract platform
subsystems, etc) can be found in the `Core` class
library (C#). The library targets .NET Standard as well as .NET Framework.

> **Note**
>
> The reason for also targeting .NET Framework directly is that the
> `Microsoft.Identity.Client` ([MSAL.NET][msal])
> library requires a .NET Framework target to be able to show the embedded web
> browser auth pop-up on Windows platforms.
>
> There are extension points that now exist in MSAL.NET meaning we can plug-in
> our own browser pop-up handling code on .NET meaning both Windows and
> Mac. We haven't yet gotten around to exploring this.
>
> See [GCM issue 113][issue-113] for more information.

The entry-point for GCM can be found in the `Git-Credential-Manager`
project, a console application that targets both .NET and .NET Framework.
This project emits the `git-credential-manager-core(.exe)` executable, and
contains very little code - registration of all supported host providers and
running the `Application` object found in `Core`.

Providers have their own projects/assemblies that take dependencies on the
`Core` core assembly, and are dependents of the main
entry point application `Git-Credential-Manager`. Code in these binaries is
expected to run on all supported platforms and typically (see MSAL.NET note
above) does not include any graphical user interface; they use terminal prompts
only.

Where a provider needs some platform-specific interaction or graphical user
interface, the recommended model is to have a separate 'helper' executable that
the shared, core binaries shell out to. Currently the Bitbucket and GitHub
providers each have a WPF (Windows only) helper executable that shows
authentication prompts and messages.

The `Core.UI` project is a WPF (Windows only) assembly
that contains common WPF components and styles that are shared between provider
helpers on Windows.

### Cross-platform UI

We hope to be able to migrate the WPF/Windows only helpers to [Avalonia][avalonia]
in order to gain cross-platform graphical user interface support. See
[GCM issue 136][issue-136] for up-to-date progress on this effort.

### Microsoft authentication

For authentication using Microsoft Accounts or Azure Active Directory, things
are a little different. The `MicrosoftAuthentication` component is present in
the `Core` core assembly, rather than bundled with a
specific host provider. This was done to allow any service that may wish to in
the future integrate with Microsoft Accounts or Azure Active Directory can make
use of this reusable authentication component.

## Asynchronous programming

GCM makes use of the `async`/`await` model of .NET and C# in almost all
parts of the codebase where appropriate as usually requests end up going to the
network at some point.

## Command execution

```text
                             +---------------+
                             |               |
                             |      Git      |
                             |               |
                             +---+-------^---+
                                 |       |
                             +---v---+---+---+
                             | stdin | stdout|
                             +---+---+---^---+
                                 |       |
                            (2)  |       |  (7)
                          Select |       | Serialize
                         Command |       | Result
                                 |       |
                     (3)         |       |
                    Select       |       |
+---------------+  Provider  +---v-------+---+
| Host Provider |            |               |
|   Registry    <------------+    Command    |
|               |            |               |
+-------^-------+            +----+------^---+
        |                         |      |
        |                   (4)   |      |   (6)
        |                Execute  |      |  Return
        |              Operation  |      |  Result
        |    (1)                  |      |
        |  Register          +----v------+---+
        |                    |               |
        +--------------------+ Host Provider |
                             |               |
                             +-------^-------+
                                     |
                   (5) Use services  |
                                     |
                             +-------v-------+
                             |    Command    |
                             |    Context    |
                             +---------------+
```

Git Credential Manager maintains a set of known commands including
`Get|Store|EraseCommand`, as well as commands for install and help/usage.

GCM also maintains a set of known, registered host providers that implement
the `IHostProvider` interface. Providers register themselves by adding an
instance of the provider to the `Application` object via the `RegisterProvider`
method in [`Core.Program`][core-program].
The `GenericHostProvider` is registered last so that it can handle all other
HTTP-based remotes as a catch-all, and provide basic username/password auth and
detect the presence of Windows Integrated Authentication (Kerberos, NTLM,
Negotiate) support (1).

For each invocation of GCM, the first argument on the command-line is
matched against the known commands and if there is a successful match, the input
from Git (over standard input) is deserialized and the command is executed (2).

The `Get|Store|EraseCommand`s consult the host provider registry for the most
appropriate host provider. The default registry implementation select the a host
provider by asking each registered provider in turn if they understand the
request. The provider selection can be overridden by the user via the
[`credential.provider`][credential-provider] or [`GCM_PROVIDER`][gcm-provider]
configuration and environment variable respectively (3).

The `Get|Store|EraseCommand`s call the corresponding
`Get|Store|EraseCredentialAsync` methods on the `IHostProvider`, passing the
request from Git together with an instance of the `ICommandContext` (4). The
host provider can then make use of various services available on the command
context to complete the requested operation (5).

Once a credential has been created, retrieved, stored or erased, the host
provider returns the credential (for `get` operations only) to the calling
command (6). The credential is then serialized and returned to Git over standard
output (7) and GCM terminates with a successful exit code.

## Host provider

Host providers implement the `IHostProvider` interface. They can choose to
directly implement the interface they can also derive from the `HostProvider`
abstract class (which itself implements the `IHostProvider` interface).

The `HostProvider` abstract class implements the
`Get|Store|EraseCredentialAsync` methods and instead has the
`GenerateCredentialAsync` abstract method, and the `GetServiceName` virtual
method. Calls to `get`, `store`, or `erase` result in first a call to
`GetServiceName` which should return a stable and unique value for the provider
and request. This value forms part of the attributes associated with any stored
credential in the credential store. During a `get` operation the
credential store is queried for an existing credential with such service name.
If a credential is found it is returned immediately. Similarly, calls to `store`
and `erase` are handles automatically to store credentials against, and erase
credentials matching the service name. Methods are implemented as `virtual`
meaning you can always override this behaviour, for example to clear other
custom caches on an `erase` request, without having to reimplement the
lookup/store credential logic.

The default implementation of `GetServiceName` is usually sufficient for most
providers. It returns the computed remote URL (without a trailing slash) from
the input arguments from Git - `<protocol>://<host>[/<path>]` - no username is
included even if present.

Host providers are queried in turn, by priority (then registration order) via
the `IHostProvider.IsSupported(InputArguments)` method and passed the input
received from Git. If the provider recognises the request, for example by a
matching known host name, they can return `true`. If the provider wants to
cancel and abort an authentication request, for example if this is a HTTP (not
HTTPS) request for a known host, they should still return `true` and later
cancel the request.

Host providers can also be queried via the `IHostProvider.IsSupported(HttpResponseMessage)`
method and passed the response message from a HEAD call made to the remote URI.
This is useful for detecting on-premises instances based on header values. GCM
will only query a provider via this method overload if no other provider at the
same registration priority has returned `true` to the `InputArguments` overload.

Depending on the request from Git, one of `GetCredentialAsync` (for `get`
requests), `StoreCredentialAsync` (for `store` requests) or
`EraseCredentialAsync` (for `erase` requests) will be called. The argument
`InputArguments` contains the request information passed over standard input
from Git/the caller; the same as was passed to `IsSupported`.

The return value for the `get` operation must be an `ICredential` that Git can
use to complete authentication.

> **Note:**
>
> The credential can also be an instance where both username and password are
> the empty string, to signal to Git it should let cURL use "any auth"
> detection - typically to use Windows Integrated Authentication.

There are no return values for the `store` and `erase` operations as Git ignores
any output or exit codes for these commands. Failures for these operations are
best communicated via writing to the Standard Error stream via
`ICommandContext.Streams.Error`.

## Command context

The `ICommandContext` which contains numerous services which are useful for
interacting with various platform subsystems, such as the file system or
environment variables. All services on the command context are exposed as
interfaces for ease of testing and portability between different operating
systems and platforms.

Component|Description
-|-
CredentialStore|A secure operating system controlled location for storing and retrieving `ICredential` objects.
Settings|Abstraction over all GCM settings.
Streams|Abstraction over standard input, output and error streams connected to the parent process (typically Git).
Terminal|Provides interactions with an attached terminal, if it exists.
SessionManager|Provides information about the current user session.
Trace|Provides tracing information that may be useful for debugging issues in the wild. Secret information MUST be filtered out completely or via the `Write___Secret` method(s).
FileSystem|Abstraction over file system operations.
HttpClientFactory|Factory for creating `HttpClient` instances that are configured with the correct user agent, headers, and proxy settings.
Git|Provides interactions with Git and Git configuration.
Environment|Abstraction over the current system/user environment variables.
SystemPrompts|Provides services for showing system/OS native credential prompts.

## Error handling and tracing

GCM operates a 'fail fast' approach to unrecoverable errors. This usually
means throwing an `Exception` which will propagate up to the entry-point and be
caught, a non-zero exit code returned, and the error message printed with the
"fatal:" prefix. For errors originating from interop/native code, you should
throw an exception of the `InteropException` type. Error messages in exceptions
should be human readable. When there is a known or user-fixable issue,
instructions on how to self-remedy the issue, or links to relevant
documentation should be given.

Warnings can be emitted over the standard error stream
(`ICommandContext.Streams.Error`) when you want to alert the user to a potential
issue with their configuration that does not necessarily stop the
operation/authentication.

The `ITrace` component can be found on the `ICommandContext` object or passed in
directly to some constructors. Verbose and diagnostic information is be written
to the trace object in most places of GCM.

[avalonia]: https://avaloniaui.net/
[core-program]: ../src/shared/Git-Credential-Manager/Program.cs
[credential-provider]: configuration.md#credentialprovider
[issue-113]: https://github.com/GitCredentialManager/git-credential-manager/issues/113
[issue-136]: https://github.com/GitCredentialManager/git-credential-manager/issues/136
[gcm-provider]: environment.md#GCM_PROVIDER
[msal]: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet
