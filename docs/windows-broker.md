# Web Account Manager integration

Git Credential Manager (GCM) knows how to integrate with the
[Web Account Manager (WAM)][azure-refresh-token-terms] feature of Windows. GCM
uses WAM to store credentials for Azure DevOps. Authentication requests are said
to be "brokered" to the operating system. Currently, GCM will share
authentication state with a few other Microsoft developer tools like Visual
Studio and the Azure CLI, meaning fewer authentication prompts. Enabling WAM
integration may also be required with certain
[Conditional Access policies][azure-conditional-access], which enterprises use
to help protect their assets, including source code.

Integration with the WAM broker offers convenience and other benefits, but may
also make unexpected other changes on your device. On a device owned and managed
by your institution or employer, WAM is probably the right choice. On a personal
device or a device owned by a different institution (e.g. if you're a contractor
working for Company A with access to resources at Company B), there are
surprising behaviors that you should be aware of before enabling WAM integration.

Note that this only affects [Azure DevOps][azure-devops].
It doesn't impact authentication with GitHub, Bitbucket, or any other Git host.

## How to enable

You can opt-in to WAM support by setting the environment variable
[`GCM_MSAUTH_USEBROKER`][GCM_MSAUTH_USEBROKER] or setting the Git configuration
value [`credential.msauthUseBroker`][credential.msauthUseBroker].

## Features

When you turn on WAM support, GCM can cooperate with Windows and with other
WAM-enabled software on your machine. This means a more seamless experience,
fewer multi-factor authentication prompts, and the ability to use additional
authentication technologies like smart cards and Windows Hello. These
convenience and security features make a good case for enabling WAM.

## Surprising behaviors

The WAM and Windows identity systems are complex, addressing a very broad range
of customer use cases. What works for a solo home user may not be adequate for a
corporate-managed fleet of 100,000 devices and vice versa. The GCM team isn't
responsible for the user experience or choices made by WAM, but by integrating
with WAM, we inherit some of those choices. Therefore, we want you to be aware
of some defaults and experiences if you choose to use WAM integration.

### For work or school accounts (Azure AD-backed identities)

When you sign into an Azure DevOps organization backed by Azure AD (often your
company or school email), if your machine is already joined to Azure AD matching
that Azure DevOps organization, you'll get a seamless and easy-to-use experience.

If your machine isn't Azure AD-joined, or is Azure AD-joined to a different
tenant, WAM will present you with a dialog box suggesting you stay signed in and
allow the organization to manage your device. The dialog box has changed a bit
in various versions of Windows; here are two examples from 2021:

![Consent dialog pre-21H1][aad-questions]

![Consent dialog post-21H1][aad-questions-21h1]

Depending on what you click, one of three things can happen:

- If you leave "allow my organization to manage my device" checked and click
"OK", your computer will be registered with the Azure AD tenant backing the
organization.
It may also be MDM-enrolled ("Mobile Device Management" -- think Intune,
AirWatch, MobileIron, etc.), meaning an administrator can deploy policies to
your machine: requiring certain kinds of sign-in, turning on antivirus and
firewall software, and enabling BitLocker.
Your identity will also be available to other apps on the computer for signing
in, some of which may do so automatically.
![Example of policies pushed to an Intune-enrolled device][aad-bitlocker]
- If you uncheck "allow my organization to manage my device" and click "OK",
your computer will be registered with Azure AD but will not be MDM-enrolled.
Your identity will be available to other apps on the computer for signing in.
Other apps may log you in automatically or prompt you again to allow your
organization to manage your device. Despite joining Azure AD, your
organization's Conditional Access policies may still prevent you from accessing
Azure DevOps.
If so, you'll be prompted with instructions on how to enroll in MDM.
- If you instead click "No, sign in to this app only", your machine will not be
joined to Azure AD or MDM-enrolled, so no policies can be enforced, and your
identity won't be made available to other apps on the computer.
Similar to the above, your organization's Conditional Access policies may
prevent you from proceeding.

If Conditional Access is required to access your organization's Git repositories,
you can [enable WAM integration][GCM_MSAUTH_USEBROKER] (or follow other
instructions your organization provides).

#### Removing device management

If you've allowed your computer to be managed and want to undo it, you can go
into **Settings**, **Accounts**, **Access work or school**.
In the section where you see your email address and organization name, click
**Disconnect**.

![Finding your work or school account][aad-work-school]

![Disconnecting from Azure AD][aad-disconnect]

### For Microsoft accounts

When you sign into an Azure DevOps organization backed by Microsoft account
(MSA) identities (email addresses like `@outlook.com` or `@gmail.com` fall into
this category), you may be prompted to select an existing "work or school
account" or use a different one.

In order to sign in with an MSA you should continue and select "Use a different
[work or school] account", but enter your MSA credentials when prompted. This is
due to a configuration outside of our control. We expect this experience to
improve over time and a "personal account" option to be presented in the future.

![Initial dialog to choose an existing or different account][ms-sign-in]

If you've connected your MSA to Windows or signed-in to other Microsoft
applications such as Office, then you may see this account listed in the
authentication prompts when using GCM. For any connected MSA, you can control
whether or not the account is available to other Microsoft applications in
**Settings**, **Accounts**, **Emails & accounts**:

![Allow all Microsoft apps to access your identity][all-ms-apps]

![Microsoft apps must ask to access your identity][apps-must-ask]

Two very important things to note:

- If you haven't connected any Microsoft accounts to Windows before, the first
account you connect will cause the local Windows user account to be converted to
a connected account.
- In addition, you can't change the usage preference for the first Microsoft
account connected to Windows: all Microsoft apps will be able to sign you in
with that account.

As far as we can tell, there are no workarounds for either of these behaviors
(other than to not use the WAM broker).

## Running as administrator

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

### Possible solutions

In order to fix the problem, there are a few options:

1. Run Git or Git Credential Manager from non-elevated processes.
2. Disable the broker by setting the
   [`GCM_MSAUTH_USEBROKER`][GCM_MSAUTH_USEBROKER]
   environment variable or the
   [`credential.msauthUseBroker`][credential.msauthUseBroker]
   Git configuration setting to `false`.

[azure-refresh-token-terms]: https://docs.microsoft.com/azure/active-directory/devices/concept-primary-refresh-token#key-terminology-and-components
[azure-conditional-access]: https://docs.microsoft.com/azure/active-directory/conditional-access/overview
[azure-devops]: https://dev.azure.com
[GCM_MSAUTH_USEBROKER]: environment.md#GCM_MSAUTH_USEBROKER
[credential.msauthUseBroker]: configuration.md#credentialmsauthusebroker
[aad-questions]: img/aad-questions.png
[aad-questions-21h1]: img/aad-questions-21H1.png
[aad-bitlocker]: img/aad-bitlocker.png
[aad-work-school]: img/aad-work-school.png
[aad-disconnect]: img/aad-disconnect.png
[ms-sign-in]: img/get-signed-in.png
[all-ms-apps]: img/all-microsoft.png
[apps-must-ask]: img/apps-must-ask.png
[ms-com]: https://docs.microsoft.com/en-us/windows/win32/com/the-component-object-model
[msal-dotnet]: https://aka.ms/msal-net
