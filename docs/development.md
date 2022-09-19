# Development and debugging

Start by cloning this repository:

```shell
git clone https://github.com/GitCredentialManager/git-credential-manager
```

You also need the latest version of the .NET SDK which can be downloaded and
installed from the [.NET website][dotnet-web].

## Building

The `Git-Credential-Manager.sln` solution can be opened and built in Visual
Studio, Visual Studio for Mac, Visual Studio Code, or JetBrains Rider.

### macOS

To build from inside an IDE, make sure to select the `MacDebug` or `MacRelease`
solution configurations.

To build from the command line, run:

```shell
dotnet build -c MacDebug
```

You can find a copy of the installer .pkg file in `out/osx/Installer.Mac/pkg/Debug`.

The flat binaries can also be found in `out/osx/Installer.Mac/pkg/Debug/payload`.

### Windows

To build from inside an IDE, make sure to select the `WindowsDebug` or
`WindowsRelease` solution configurations.

To build from the command line, run:

```powershell
dotnet build -c WindowsDebug
```

You can find a copy of the installer .exe file in `out\windows\Installer.Windows\bin\Debug\net472`.

The flat binaries can also be found in `out\windows\Payload.Windows\bin\Debug\net472\win-x86`.

### Linux

The two available solution configurations are `LinuxDebug` and `LinuxRelease`.

To build from the command line, run:

```shell
dotnet build -c LinuxDebug
```

You can find a copy of the Debian package (.deb) file in `out/linux/Packaging.Linux/deb/Debug`.

The flat binaries can also be found in `out/linux/Packaging.Linux/payload/Debug`.

## Debugging

To debug from inside an IDE you'll want to set `Git-Credential-Manager` as the
startup project, and specify one of `get`, `store`, or `erase` as a program
argument.

To simulate Git interacting with GCM, when you start from your IDE of choice,
you'll need to enter the following [information over standard input][ioformat]:

```text
protocol=http<LF>
host=<HOSTNAME><LF>
<LF>
<LF>
```

..where `<HOSTNAME>` is a supported hostname such as `github.com`, and `<LF>` is
a line feed (or CRLF, we support both!).

You may also include the following optional fields, depending on your scenario:

```text
username=<USERNAME><LF>
password=<PASSWORD><LF>
```

For more information about how Git interacts with credential helpers, please
read Git's documentation on [custom helpers][custom-helpers].

### Attaching to a running process

If you want to debug an already running GCM process, set the `GCM_DEBUG`
environment variable to `1` or `true`. The process will wait on launch for a
debugger to attach before continuing.

This is useful when debugging interactions between GCM and Git, and you want
Git to be the one launching us.

### Collect trace output

If you want to debug a release build or installation of GCM, you can set the
`GCM_TRACE` environment variable to `1` to print trace information to standard
error, or to an absolute file path to write trace information to a file.

For example:

```shell
$ GCM_TRACE=1 git-credential-manager-core version
> 18:47:56.526712 ...er/Application.cs:69 trace: [RunInternalAsync] Git Credential Manager version 2.0.124-beta+e1ebbe1517 (macOS, .NET 5.0) 'version'
> Git Credential Manager version 2.0.124-beta+e1ebbe1517 (macOS, .NET 5.0)
```

### Code coverage metrics

If you want code coverage metrics these can be generated either from the command
line:

```shell
dotnet test --collect:"XPlat Code Coverage" --settings=./.code-coverage/coverlet.settings.xml
```

Or via the VSCode Terminal/Run Task:

```console
test with coverage
```

HTML reports can be generated using ReportGenerator, this should be installed
during the build process, from the command line:

```shell
dotnet ~/.nuget/packages/reportgenerator/*/*/net6.0/ReportGenerator.dll -reports:./**/TestResults/**/coverage.cobertura.xml -targetdir:./out/code-coverage
```

or

```shell
dotnet {$env:USERPROFILE}/.nuget/packages/reportgenerator/*/*/net6.0/ReportGenerator.dll -reports:./**/TestResults/**/coverage.cobertura.xml -targetdir:./out/code-coverage
```

Or via VSCode Terminal/Run Task:

```console
report coverage - nix
```

or

```console
report coverage - win
```

[dotnet-web]: https://dotnet.microsoft.com/
[custom-helpers]: https://git-scm.com/docs/gitcredentials#_custom_helpers
[ioformat]: https://git-scm.com/docs/git-credential#IOFMT
