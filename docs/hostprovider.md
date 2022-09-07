# Git Credential Manager Host Provider

## Abstract

Git Credential Manger, the cross-platform and cross-host Git credential
helper, can be extended to support any Git hosting service allowing seamless
authentication to secured Git repositories by implementing and registering a
"host provider".

## 1. Introduction

Git Credential Manager (GCM) is a host and platform agnostic Git
credential helper application. Support for authenticating to any Git hosting
service can be added to GCM by creating a custom "host provider" and
registering it within the product. Host providers can be submitted via a pull
request on [the Git Credential Manager repository on GitHub][gcm].

This document outlines the required and expected behaviour of a host provider,
and what is required to implement and register one.

### 1.1. Notational Conventions

The key words "MUST", "MUST NOT", "REQUIRED", "SHALL", "SHALL NOT",
"SHOULD", "SHOULD NOT", "RECOMMENDED", "MAY", and "OPTIONAL" in this
specification are to be interpreted as described in
[[RFC2119][rfc-2119]].

### 1.2. Abbreviations

Throughout this document you may see multiple abbreviations of product names and
security or credential objects.

"Git Credential Manager" is abbreviated to "GCM". "Git Credential
Manager for Windows" is abbreviated to "GCM for Windows" or "GCM Windows".
"Git Credential Manager for Mac & Linux" is abbreviated to "GCM for
Mac/Linux" or "GCM Mac/Linux".

OAuth2 [[RFC6749][rfc-6749]] "access tokens" are
abbreviated to "ATs" and "refresh tokens" to "RTs". "Personal Access Tokens" are
abbreviated to "PATs".

## 2. Implementation

Writing and adding a host provider to GCM requires two main actions:
implementing the `IHostProvider` interface, and registering an instance of the
provider with the application via the host provider registry.

Host providers MUST implement the `IHostProvider` interface. They can choose to
directly implement the interface they MAY derive from the `HostProvider`
abstract class (which itself implements the `IHostProvider` interface) - see
[2.6][hostprovider-base-class].

Implementors MUST implement all interface properties and abstract methods.

The `Id` and `Name` properties MUST be implemented and MUST NOT return default
or empty values.

The `Id` field MUST be unique over the set of all providers, or
else an error will be thrown at registration time. The `Id` field MAY be a
unique random string of characters and digits such as a UUID, but it is
RECOMMENDED to use a human-readable value consisting of letter characters in the
range \[a-z\] only.

The `Name` property MUST be a human readable string and MUST identify the Git
hosting service this provider supports.

The `SupportedAuthorityIds` property MUST return an instance of an object and
NOT a `null` reference. Populating this collection with values is OPTIONAL but
highly RECOMMENDED. You should return a set of stable identifiers of all
authorities that the provider supports authentication against.

### 2.1. Registration

Host providers must provide an instance of their `IHostProvider` type to the
GCM application host provider registry to be considered for handling
requests.

The main GCM `Application` object has one principal registry which you can
register providers with by calling the `RegisterProvider` method.

#### 2.1.2. Ordering

The default host provider registry in GCM has multiple priority levels that
host providers can be registered at: High, Normal, and Low.

For each priority level (starting with High, then Normal, then Low), the
registry will call each host provider in the order they were registered in,
unless the user has overridden the provider selection process.

There are no rules or restrictions on the ordering of host providers, except
that the `GenericHostProvider` MUST be registered last and at the Low priority.
The generic provider is a catch-all provider implementation that will handle any
request in a standard way.

### 2.2. Handling Requests

The `IsSupported(InputArguments)` method will be called on all registered host
providers in-turn on the invocation of a `get`, `store`, or `erase` request. The
first host provider to return `true` will be called upon to handle the specific
request. If the user has overridden the host provider selection process, a
specific host provider may be selected instead, and the
`IsSupported(InputArguments)` method will NOT be called.

This method MUST return `true` if and only if the provider understands the
request and can serve or handle the request. If the provider does not know how
to handle the request it MUST return `false` instead.

If no host provider returns `true` to a call to the `IsSupported(InputArguments)`
method for a each host provider priority level, then a HTTP HEAD request will be
made to the remote URL and each host provider will be be called via the
`IsSupported(HttpResponseMessage)` method. A host provider SHOULD use this call
to check for recognised on-premises instances (for example, by inspecting
response headers) and return `true` if it wishes to be called upon to handle the
credential request, otherwise it MUST return `false`.

Host providers SHOULD NOT make further network calls if possible during any of
the `IsSupported` method overloads to avoid degrading the performance of the
overall application.

#### 2.2.1. Rejecting Requests

The `IsSupported` methods MUST return `true` if the host provider would like to
cancel the authentication operation based on the current context or input.
For example, if provider requires a secure protocol but the requested protocol
for a supported hostname is `http` and not `https`.

Host providers MUST instead cancel the request from the `GetCredentialAsync`
method by throwing an `Exception`. Implementors MUST provide detailed
information regarding the reason why the authentication cannot continue, for
example "HTTP is not secure, please use HTTPS".

### 2.3. Retrieving Credentials

The `GetCredentialAsync` method will be called when a `get` request is made.
The method MUST return an instance of an `ICredential` capable of fulfilling the
specific access request. The argument passed to `GetCredentialAsync` contains
properties indicating the required `protocol` and `host` for this request. The
`username` and `path` properties are OPTIONAL, however if they are present, they
MUST be considered and used to direct the authentication.

The host provider MAY attempt to locate any existing credential, stored by the
`StoreCredentialAsync` method, before resorting to the creation a new one.

The host provider MAY choose to check if a stored credential is still valid
by inspecting any stored metadata associated with the value. A host provider MAY
also choose to further validate a retrieved stored credential by making a web
request. However, it is NOT RECOMMENDED to make any request that is known to be
slow or that typically produces inconclusive validation results.

If a provider chooses to make a validation web request and that request fails or
is inconclusive, it SHOULD assume the credential is still valid and return it
anyway, letting Git (the caller) attempt to use it and validate it itself.

The returned `ICredential` MAY leave both the username and password values as
the empty string or `null`. This signals to Git (or rather cURL) that it should
negotiate the authentication mechanism with the remote itself. This is typically
used for Windows Integrated Authentication.

#### 2.3.1 Authentication Prompts

When it is not possible to locate an existing credential suitable for the
current request, a host provider SHOULD prompt the user to complete an
authentication flow.

The method, modes, and interactions for performing authentication will vary
widely between Git hosting services and their supported authentication
authorities. A host provider SHOULD attempt to detect the best authentication
experience given the current environment or context, and select that one to
attempt first.

Host providers are RECOMMENDED to attempt authentication mechanisms that do not
require user interaction if possible. If there are multiple authentication
mechanisms that could be equally considered "best" they MAY prompt the user
to make a selection. Host providers MAY wish to remember such a selection for
future use, however they MUST make it clear how to clear this stored selection
to the user.

If interaction is required to complete authentication a host provider MUST first
check if interaction has been disabled (`ISettings.IsInteractionAllowed`), and
an exception MUST be thrown if interaction has been disallowed.

Authentication prompts that display a graphical user interface such as a window
are MUST be preferred when an interactive "desktop" session is available.

If an authentication prompt is required when an interactive session is not
available and a terminal/TTY is attached then a provider MUST first check if
terminal prompts are enabled (`ISettings.IsTerminalPromptsEnabled`), and an
exception MUST be thrown if interaction has been disallowed.

### 2.4. Storing Credentials

Host providers MAY store credentials at various stages of a typical
authentication flow, or when explicitly requested to do so in a call to
`StoreCredentialAsync`.

Providers SHOULD use the credential store (exposed as `ICredentialStore`) to
persist secret values and credential entities such as passwords, PATs and OAuth
tokens.

The typical Git credential helper call pattern is one call to `get`, followed by
either a `store` request in case of a HTTP 200 (OK) response, or `erase` in case
of HTTP 401 (Unauthorized) response. In some cases there is additional context
that is present as part of the `get` request or during the generation of a new
credential that is not present during the subsequent call to `store` (or
`erase`). In these cases providers MAY store the credential during the `get`
rather than, or as well as during the `store`.

Host providers MAY store multiple credentials or tokens in the same request if
it is required. One example where multiple credential storage is needed is with
OAuth2 access tokens (AT) and refresh tokens (RT). Both the AT and RT SHOULD be
stored in the same location using the credential store with complementary
credential service names.

### 2.5. Erasing Credentials

If host providers have stored credentials in the credential store, they MUST
respond to requests to erase them in calls to `EraseCredentialAsync`.

If a host provider cannot locate a credential to erase it MUST NOT raise an
error and MUST exit successfully. A warning message MAY be emitted to the
tracing system.

Host providers MUST NOT perform their own repeated validation of credentials
for the purposes of ignoring the request to erase them. The ultimate authority
on the validity of a credential is the caller (Git).

Providers MAY validate any additional or ancillary credentials (such as OAuth
RTs) are still valid when a request to erase the primary credential (such as an
OAuth AT) is made, and choose not to delete those additional credentials. The
primary credential MUST still always be erased in all cases.

### 2.6 `HostProvider` base class

The `HostProvider` abstract base class is provided for the convenience of host
provider implementors. This base class implements most required methods of the
`IHostProvider` interface with common credential recall and storage behaviour.

The `GetCredentialAsync`, `StoreCredentialAsync`, and `EraseCredentialAsync`
methods are implemented as `virtual` meaning they MAY be overridden by derived
classes to customise the behaviour of those operations. It is NOT RECOMMENDED
to derive from the `HostProvider` base class if the implementor must override
most of the methods as implemented - implementors SHOULD implement the
`IHostProvider` interface directly instead.

Implementors that choose to derive from this base class MUST implement all
abstract methods and properties. The primary abstract method to implement
is `GenerateCredentialAsync`.

There is also an additional `virtual` method named `GetServiceName` that is used
by the default implementations of the `Get|Store|EraseCredentialAsync` methods
to locate and store credentials.

#### 2.6.1 `GetServiceName`

The `GetServiceName` virtual method, if overriden, MUST return a string that
identifies the service/provider for this request, and is used for storing
credentials. The value returned MUST be stable - i.e, it MUST return the same
value given the same or equivalent input arguments.

By default this method returns the full remote URI, without a trailing slash,
including protocol/scheme, hostname, and path if present in the input arguments.
Any username in the input arguments is never included in the URI.

#### 2.6.2 `GenerateCredentialAsync`

The `GenerateCredentialAsync` method will be called if an existing credential
with a matching service (from `GetServiceName`) and account is not found in the
credential store.

This method MUST return a freshly created/generated credential and not any
existing or stored one. It MAY use existing or stored ancillary data or tokens,
such as OAuth refresh tokens, to generate the new token (such as an OAuth AT).

### 2.7. External Metadata

Host providers MAY wish to store extra data about authentications or users
collected or produced during authentication operations. These SHOULD be stored
in a per-user, local location such as the user's home or profile directory.

Secrets, credentials or other sensitive data SHOULD be stored in the credential
store, or otherwise protected by some form of per-user, local encryption.

In the case of stored data caches, providers SHOULD invalidate relevant parts
of, or the entire cache, when a call to `EraseCredentialAsync` is made.

## 3. Helpers

Host providers MAY wish to make use of platform or operating system specific
features such as native APIs and native graphical user interfaces, in order to
offer a better authentication experience.

Host providers MUST function without the presence of a helper, even if that
function is to fail gracefully with a user-friendly error message, including
a remedy to correct their installation. Host providers SHOULD always offer a
terminal/TTY or text-based authentication mechanism alongside any graphical
interface provided by a helper.

In order to achieve this host providers MUST introduce an out-of-process
"helper" executable that can be invoked from the main GCM process. This
allows the "helper" executable full implementation freedom of runtime, language,
etc.

Communications between the main and helper processes MAY use any IPC mechanism
available. It is RECOMMENDED implementors use standard input/output streams or
file descriptors to send and receive data as this is consistent with how Git and
GCM communicate. UNIX sockets or Windows Named Pipes MAY also be used when
an ongoing back-and-forth communication is required.

### 3.1. Discovery

It is RECOMMENDED that helper discovery is achieved by simply checking for the
presence of the expected executable file. The name and path of the helper
executable SHOULD be configurable by the user via Git's configuration files.

## 4. Error Handling

If an unrecoverable error occurs a host provider MUST throw an exception and
MUST include detailed failure information in the error message. If the reason
for failure can be fixed by the user the error message MUST include instructions
to fix the problem, or a link to online documentation.

In the case of a recoverable error, host providers SHOULD print a warning
message to the standard error stream, and MUST include the error information and
the recovery steps take in the trace log.

In the case of an authentication error, providers SHOULD attempt to prompt the
user again with a message indicating the incorrect authentication details have
been entered.

## 5. Custom Commands

If a host provider wishes to surface custom commands the SHOULD implement the
`ICommandProvider` interface.

Each provider is given the opportunity to create a single `ProviderCommand`
instance to which further sub-commands can be parented to. Commanding is
provided by the `System.CommandLine` API library [[1][references]].

There are no limitations on what format sub-commands, arguments, or options must
take, but implementors SHOULD attempt to follow existing practices and styles.

## References

1. [`System.CommandLine` API][github-dotnet-cli]

[gcm]: https://github.com/GitCredentialManager/git-credential-manager
[github-dotnet-cli]: https://github.com/dotnet/command-line-api
[hostprovider-base-class]: #26-hostprovider-base-class
[references]: #references
[rfc-2119]: https://tools.ietf.org/html/rfc2119
[rfc-6749]: https://tools.ietf.org/html/rfc6749
