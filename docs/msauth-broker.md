# Microsoft Authentication Broker

Git Credential Manager (GCM) can integrate with the Microsoft authentication
broker for your operating system to enable seamless single sign-on and secure
authentication.

> [!IMPORTANT]
> As of **GCM 3.0**, broker integration is the **default option** for authentication
on Windows, macOS, and Linux, when supported. Previous versions of GCM only
supported the broker on Windows and required an explicit opt-in.

## How to enable or disable

As of GCM 3.0, wherever possible, the broker will automatically be used for
Microsoft authentication _by default_.

You can opt-out of broker support by setting the environment variable
[`GCM_MSAUTH_USEBROKER`][GCM_MSAUTH_USEBROKER] or setting the Git configuration
value [`credential.msauthUseBroker`][credential.msauthUseBroker] to `false`.

> [!NOTE]
> Brokered authentication only applies to Git hosting providers that use
> Microsoft authentication (e.g., Azure Repos). It does not impact
> authentication with GitHub, Bitbucket or other Git hosts.

## What is the broker?

The Microsoft authentication broker performs credential negotiation on behalf of
an application, simplifies many common authentication challenges and provides a
more seamless and secure authentication experience.

Brokered authentication also has the benefit of deeper integration with
operating system features such as biometrics (Windows Hello, Touch ID),
hardware-backed credential storage ([Secure Enclave][secure-enclave],
[TPM][tpm], [passkeys][passkeys]), and compliance with enterprise
[conditional access security policies][conditional-access].

![Diagram of how the authentication broker interacts with other components][broker-diagram]

### Surprising behaviors

Integration with the broker offers convenience and other benefits, but may also
make unexpected other changes on your device. On a device owned and managed by
your institution or employer, the broker is probably the right choice.

On a personal device or a device owned by a different institution (e.g. if
you're a contractor working for Company A with access to resources at
Company B), there are surprising behaviors that you should be aware of.

> [!IMPORTANT]
> GCM does **not** have control over these behaviors, but we want to call them
> out clearly for full transparency and awareness.

#### Work or school accounts

If your device is not already joined to Entra ID or enrolled in Intune when you
first sign in with a work or school account, you may be asked to complete the
[Entra ID join][entrajoin-info] or [Intune join][intunejoin-info] process
depending on your company policies.

> [!WARNING]
> If your company requires you to join Entra ID or enroll in Intune to access
> your work or school account, please be aware that doing so may result in
> changes to your device configuration such as:
>
> - your company's IT administrators may have access to monitor your device
> - software packages or updates may be installed
> - device settings may be modified without the ability to revert them
> - disk encryption may be enabled
>
> If you have questions about these changes, **please contact your company's
> IT administrators before continuing**.

It is possible to disconnect your device from corporate policies after the fact.
Please see the instructions for your operating system below.

##### Windows

Open the Settings app:
[**Accounts** > **Access work or school**][appx-settings-workplace] and select
**Disconnect**

![Manage device connection to corporate policies (Windows 11)][entradisconnect-win11]

##### macOS

To remove a single sign-on (SSO) account from macOS, open the
[Company Portal][appx-companyportal-mac] app, click on your **profile icon**
in the _top right_ of the window, then select **Remove account from this
device**:

![Open the profile page in the Company Portal on macOS][companyportal-profileicon-macos]

![Remove SSO account from the Company Portal on macOS][companyportal-removesso-macos]

##### Linux

The Intune app provides a command-line tool `dsreg` to manage device
registration on Linux devices, including deregistration using the `--unregister`
option.

```shell
$ dsreg --help
dsreg - Device Registration Command Tool for Linux

Similar to Windows dsregcmd, this tool queries device registration status,
PRT (Primary Refresh Token) information, and broker configuration.

USAGE:
  dsreg [options]

OPTIONS:
  --status             Display comprehensive device registration and PRT status (default)
  --help               Display this help message
  --tenant-id <id>     Query device registration for a specific tenant
  --getdrstoken        Acquire DRS access token for device registration
  --unregister         Unregister device from the tenant (requires root and DRS access token)
  --cleanup            Clean all broker state (accounts, tokens, credentials)

EXAMPLES:
  dsreg
  dsreg --status
  dsreg --tenant-id 12345678-90ab-cdef-1234-567890abcdef
  dsreg --tenant-id <tenant-id> --unregister
  echo "<access-token>" | dsreg --tenant-id <tenant-id> --unregister

OUTPUT INFORMATION:
  Device State         - Registration status, Device ID, Tenant information
  Primary Refresh Token - PRT presence, timestamps, age, expiration
  Broker Information   - Version, client ID, device mode

REQUIREMENTS:
  - Microsoft Identity Broker installed and configured
  - Appropriate permissions to query device state
```

#### Microsoft accounts (Windows only)

> [!WARNING]
> If you are using local accounts on Windows, when you first sign in to a
> Microsoft account, that account may be linked to your local account and
> sign-in options may be affected.

You can disconnect your Microsoft account from your local account after the fact
from the Settings app: [**Accounts** > **Your info**][appx-settings-yourinfo]
and select **Sign in with a local account instead**

![Disconnect Microsoft account from local account (Windows 11)][msadisconnect-win11]

## Windows

The [Web Account Manager][wam] component of Windows provides the broker
functionality on Windows and comes _built into_ **Windows 10 version 1703**
(build 15063) and later, or **Windows Server 2019** (build 17763) and later on
servers.

Here is an example of the broker account selection prompt on Windows:

![Broker account selection on Windows][userpicker-windows]

Accounts that appear above can be managed or removed from the Settings app:

- [**Accounts** > **Your accounts**][appx-settings-accounts] (Windows 11)
- [**Accounts** > **Email & accounts**][appx-settings-accounts] (Windows 10)

![Manage known Windows accounts from Settings (Windows 11)][manageaccounts-win11]

## macOS

On macOS the Platform Single-sign-on Extension (PSSO) provides the underlying
broker functionality for authentication. This is bundled with the Microsoft
Intune Company Portal app. The app must be installed and _your device must be
enrolled_ for broker integration to work correctly.

Please read the Microsoft Intune [enrollment guide][enroll-macos] for further
instructions on enrolling your Mac.

Here is an example of the broker account selection prompt on macOS:

![Broker account selection on macOS][userpicker-macos]

You can see the join status of your Mac in the
[Company Portal][appx-companyportal-mac] app:

![Company Portal app on macOS showing device status][companyportal-status-macos]

## Linux

The [Microsoft Intune app][intunebroker-linux] acts as the authentication
broker on Linux distributions. Devices must be configured with a _GNOME
graphical desktop environment_ - **headless devices are not supported**.
Furthermore, the [Microsoft Edge browser][msedge] _must also be installed_.

> [!NOTE]
> The Microsoft Intune app for Linux is **not supported** on Windows Subsystem
> for Linux (WSL). Please see the [WSL documentation][wsl] for integrating with
> the host Windows environment instead.

Please read the Microsoft Intune [enrollment guide][enroll-linux] for further
instructions on enrolling your Linux device.

You can see the join status of your Linux device in the
[Microsoft Intune app][intunebroker-linux]:

![Microsoft Intune app on Linux showing device status][intuneapp-linux]

In addition to the Intune app, you can also see join status using the `dsreg`
command-line tool with the `--status` option:

```shell
$ dsreg --status
Account
-----------------------------------------------------------------
Home Account ID                    : 00000000-0000-0000-0000-000000000000.00000000-0000-0000-0000-000000000000
Environment                        : login.windows.net
Tenant ID                          : 00000000-0000-0000-0000-000000000000

Device State
-----------------------------------------------------------------
Device Registration Status         : Registered
Device ID                          : 00000000-0000-0000-0000-000000000000
Tenant ID                          : 00000000-0000-0000-0000-000000000000

Primary Refresh Token
-----------------------------------------------------------------
PRT Present                        : YES
PRT Cached At                      : 2026-09-15 10:33:57 UTC
PRT Age                            : 0.04 hours
PRT Expires On                     : 2026-09-29 10:33:56 UTC
Session Key Protocol               : 3.0

Broker Information
-----------------------------------------------------------------
Broker Version                     : 3.0.2
Broker Service Name                : microsoft-identity-device-broker.service
Broker Binary Path                 : /opt/microsoft/identity-broker/bin/microsoft-identity-broker
Broker Binary Timestamp            : 2026-04-22 19:28:58 UTC
```

## Using the current OS account by default

GCM can be configured to automatically use the default or current OS account for
authentication when using the broker. By default GCM will first ask if you wish
to continue with the default account before proceeding.

![Default OS account prompt][default-account-prompt]

If you wish to **always use** the current OS account, you can set the
[`GCM_MSAUTH_USEDEFAULTACCOUNT`][GCM_MSAUTH_USEDEFAULTACCOUNT] environment
variable or set the
[`credential.msauthUseDefaultAccount`][credential.msauthUseDefaultAccount] Git
configuration value to `true`.

In certain cloud hosted environments such as [Microsoft Dev Box][devbox], this
setting is **_automatically enabled (`true`)_**.

To disable the prompt and/or automatic account selection, set the environment
variable [`GCM_MSAUTH_USEDEFAULTACCOUNT`][GCM_MSAUTH_USEDEFAULTACCOUNT] or the
[`credential.msauthUseDefaultAccount`][credential.msauthUseDefaultAccount] Git
configuration value explicitly to `false`.

## Legacy broker support

In previous versions of GCM, support for the authentication broker was limited
to Windows only, required explicit opt-in, and had several limitations compared
to the current implementation.

> [!TIP]
> Please update to the [latest Git Credential Manager][windows-install] where
> the below issues have now been addressed.

Please read below for more information on previous versions of GCM and their
broker support.

### Running as administrator

#### GCM 2.1 and later

From version 2.1 onwards, GCM uses a version of the [Microsoft Authentication
Library (MSAL)][msal-dotnet] that supports use of the Windows
broker from an elevated process.

#### Previous versions

The Windows broker ("WAM") makes heavy use of [COM][ms-com], a remote procedure
call (RPC) technology built into Windows. In order to integrate with WAM, Git
Credential Manager and the underlying
[Microsoft Authentication Library (MSAL)][msal-dotnet] must use COM interfaces
and RPCs. When you run Git Credential Manager as an elevated process, some of
the calls made between GCM and WAM may fail due to differing process security
levels. This can happen when you run `git` from an Administrator command-prompt
or perform Git operations from Visual Studio running as Administrator.

If you've enabled using the broker, GCM will check whether it's running in an
elevated process. If it is, GCM will automatically attempt to modify the COM
security settings for the running process so that GCM and WAM can work together.
However, this automatic process security change is not guaranteed to succeed.
Various external factors like registry or system-wide COM settings may cause it
to fail. If GCM can't modify the process's COM security settings, GCM prints a
warning message and won't be able to use the broker.

```text
warning: broker initialization failed
Failed to set COM process security to allow Windows broker from an elevated process (0x80010119).
See https://aka.ms/gcm/wamadmin for more information.
```

[appx-companyportal-mac]: companyportal://#
[appx-settings-accounts]: ms-settings:emailandaccounts
[appx-settings-workplace]: ms-settings:workplace
[appx-settings-yourinfo]: ms-settings:yourinfo
[broker-diagram]: img/broker-diagram.png
[conditional-access]: https://docs.microsoft.com/azure/active-directory/conditional-access/overview
[companyportal-profileicon-macos]: img/broker-companyportal-profileicon-mac.png
[companyportal-removesso-macos]: img/broker-companyportal-removesso-mac.png
[companyportal-status-macos]: img/broker-companyportal-mac.png
[credential.msauthUseBroker]: configuration.md#credentialmsauthusebroker
[credential.msauthUseDefaultAccount]: configuration.md#credentialmsauthusedefaultaccount-experimental
[default-account-prompt]: img/broker-osaccount.png
[devbox]: https://azure.microsoft.com/en-us/products/dev-box
[enroll-linux]: https://learn.microsoft.com/en-us/intune/user-help/enrollment/enroll-linux
[enroll-macos]: https://learn.microsoft.com/en-us/intune/user-help/enrollment/enroll-company-portal-macos
[entradisconnect-win11]: img/broker-entradisconnect-win11.png
[entrajoin-info]: https://learn.microsoft.com/en-us/entra/identity/devices/overview
[GCM_MSAUTH_USEBROKER]: environment.md#GCM_MSAUTH_USEBROKER
[GCM_MSAUTH_USEDEFAULTACCOUNT]: environment.md#GCM_MSAUTH_USEDEFAULTACCOUNT-experimental
[intuneapp-linux]: img/broker-intuneapp-linux.png
[intunebroker-linux]: https://learn.microsoft.com/en-us/intune/user-help/company-portal/intune-app-linux
[intunejoin-info]: https://learn.microsoft.com/en-us/intune/user-help/enrollment
[manageaccounts-win11]: img/broker-manageaccounts-win11.png
[ms-com]: https://docs.microsoft.com/en-us/windows/win32/com/the-component-object-model
[msadisconnect-win11]: img/broker-msadisconnect-win11.png
[msal-dotnet]: https://aka.ms/msal-net
[msedge]: https://www.microsoft.com/edge
[passkeys]: https://www.microsoft.com/en-gb/security/business/security-101/what-is-passkey
[secure-enclave]: https://support.apple.com/en-gb/guide/security/sec59b0b31ff/web
[tpm]: https://docs.microsoft.com/en-us/windows/security/information-protection/tpm/trusted-platform-module-overview
[userpicker-macos]: img/broker-userpicker-mac.png
[userpicker-windows]: img/broker-userpicker-windows.png
[wam]: https://docs.microsoft.com/azure/active-directory/devices/concept-primary-refresh-token#key-terminology-and-components
[windows-install]: install.md#windows
[wsl]: wsl.md
