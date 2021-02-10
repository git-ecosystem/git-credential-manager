# Development and debugging

Start by cloning this repository:

```shell
git clone https://github.com/microsoft/Git-Credential-Manager-Core
```

## Building

The `Git-Credential-Manager.sln` solution can be opened and built in Visual Studio, Visual Studio for Mac, Visual Studio Code, or JetBrains Rider.

### macOS

To build from inside an IDE, make sure to select the `MacDebug` or `MacRelease` solution configurations.

To build from the command line, run:

```shell
dotnet build -c MacDebug
```

You can find a copy of the installer .pkg file in `out/osx/Installer.Mac/pkg/Debug`.

The flat binaries can also be found in `out/osx/Installer.Mac/pkg/Debug/payload`.

### Windows

To build from inside an IDE, make sure to select the `WindowsDebug` or `WindowsRelease` solution configurations.

To build from the command line, run:

```powershell
msbuild /t:restore /p:Configuration=WindowsDebug
msbuild /p:Configuration=WindowsDebug
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

To debug from inside an IDE you'll want to set `Git-Credential-Manager` as the startup project, and specify one of `get`, `store`, or `erase` as a program argument.

To simulate Git interacting with GCM Core, when you start from your IDE of choice, you'll need to enter the following [information over standard input](https://git-scm.com/docs/git-credential#IOFMT):

```text
protocol=http<LF>
host=<HOSTNAME><LF>
<LF>
<LF>
```

..where `<HOSTNAME>` is a supported hostname such as `github.com`, and `<LF>` is a line feed (or CRLF, we support both!).

You may also include the following optional fields, depending on your scenario:

```text
username=<USERNAME><LF>
password=<PASSWORD><LF>
```

For more information about how Git interacts with credential helpers, please read Git's [documentation](https://git-scm.com/docs/gitcredentials#_custom_helpers).

### Attaching to a running process

If you want to debug an already running GCM Core process, set the `GCM_DEBUG` environment variable to `1` or `true`. The process will wait on launch for a debugger to attach before continuing.

This is useful when debugging interactions between GCM Core and Git, and you want Git to be the one launching us.

### Collect trace output

If you want to debug a release build or installation of GCM Core, you can set the `GCM_TRACE` environment variable to `1` to print trace information to standard error, or to an absolute file path to write trace information to a file.

For example:

```shell
$ GCM_TRACE=1 git-credential-manager-core version
> 18:47:56.526712 ...er/Application.cs:69 trace: [RunInternalAsync] Git Credential Manager version 2.0.124-beta+e1ebbe1517 (macOS, .NET Core 3.1.3) 'version'
> Git Credential Manager version 2.0.124-beta+e1ebbe1517 (macOS, .NET Core 3.1.3)
```
