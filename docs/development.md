# Development and debugging

Start by cloning this repository:

```shell
git clone https://github.com/git-ecosystem/git-credential-manager
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

GCM has two tracing systems - one that is distinctly GCM's and one that
implements certain features of [Git's Trace2 API][trace2]. Below are
instructions for how to use each.

#### `GCM_TRACE`

If you want to debug a release build or installation of GCM, you can set the
`GCM_TRACE` environment variable to `1` to print trace information to standard
error, or to an absolute file path to write trace information to a file.

For example:

```shell
$ GCM_TRACE=1 git-credential-manager version
> 18:47:56.526712 ...er/Application.cs:69 trace: [RunInternalAsync] Git Credential Manager version 2.0.124-beta+e1ebbe1517 (macOS, .NET 5.0) 'version'
> Git Credential Manager version 2.0.124-beta+e1ebbe1517 (macOS, .NET 5.0)
```

#### Git's Trace2 API

This API can also be used to print debug, performance, and telemetry information
to stderr or a file in various formats.

##### Supported format targets

1. The Normal Format Target: Similar to `GCM_TRACE`, this target writes
human-readable output and is best suited for debugging. It can be enabled via
environment variable or config, for example:

    ```shell
    export GIT_TRACE2=1
    ```

    or

    ```shell
    git config --global trace2.normalTarget ~/log.normal
    ```

0. The Performance Format Target: This format is column-based and geared toward
analyzing performance during development and testing. It can be enabled via
environment variable or config, for example:

    ```shell
    export GIT_TRACE2_PERF=1
    ```

    or

    ```shell
    git config --global trace2.perfTarget ~/log.perf
    ```

0. The Event Format Target: This format is json-based and is geared toward
collection of large quantities of data for advanced analysis. It can be enabled
via environment variable or config, for example:

    ```shell
    export GIT_TRACE2_EVENT=1
    ```

    or

    ```shell
    git config --global trace2.eventTarget ~/log.event
    ```

You can read more about each of these format targets in the [corresponding
section][trace2-targets] of Git's Trace2 API documentation.

##### Supported events

The below describes, at a high level, the Trace2 API events that are currently
supported in GCM and the information they provide:

1. `version`: contains the version of the current executable (e.g. GCM or a
helper exe)
0. `start`: contains the complete argv received by current executable's `Main()`
method
0. `exit`: contains current executable's exit code
0. `child_start`: describes a child process that is about to be spawned
0. `child_exit`: describes a child process at exit
0. `region_enter`: describes a region (e.g. a timer for a section of code that
is interesting) on entry
0. `region_leave`: describes a region on leaving

You can read more about each of these format targets in the [corresponding
section][trace2-events] of Git's Trace2 API documentation.

Want to see more events? Consider contributing! We'd :love: to see your
awesome work in support of building out this API.

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
dotnet ~/.nuget/packages/reportgenerator/*/*/net8.0/ReportGenerator.dll -reports:./**/TestResults/**/coverage.cobertura.xml -targetdir:./out/code-coverage
```

or

```shell
dotnet {$env:USERPROFILE}/.nuget/packages/reportgenerator/*/*/net8.0/ReportGenerator.dll -reports:./**/TestResults/**/coverage.cobertura.xml -targetdir:./out/code-coverage
```

Or via VSCode Terminal/Run Task:

```console
report coverage - nix
```

or

```console
report coverage - win
```

## Linting Documentation

Documents are linted using [markdownlint][markdownlint] which can be installed
as a CLI tool via NPM or as an [extension in VSCode][vscode-markdownlint]. See
the [documentation on GitHub][markdownlint]. The configuration used for
markdownlint is in [.markdownlint.jsonc][markdownlint-config].

Documents are checked for link validity using [lychee][lychee]. Lychee can be
installed in a variety of ways depending on your platform, see the [docs on GitHub][lychee-docs].
Some URLs are ignored by lychee, per the [lycheeignore][lycheeignore].

[dotnet-web]: https://dotnet.microsoft.com/
[custom-helpers]: https://git-scm.com/docs/gitcredentials#_custom_helpers
[ioformat]: https://git-scm.com/docs/git-credential#IOFMT
[lychee]: https://lychee.cli.rs/
[lychee-docs]: https://github.com/lycheeverse/lychee
[lycheeignore]: ../.lycheeignore
[markdownlint]: https://github.com/DavidAnson/markdownlint-cli2
[markdownlint-config]: ../.markdownlint.jsonc
[trace2]: https://git-scm.com/docs/api-trace2
[trace2-events]: https://git-scm.com/docs/api-trace2#_event_specific_keyvalue_pairs
[trace2-targets]: https://git-scm.com/docs/api-trace2#_trace2_targets
[vscode-markdownlint]: https://github.com/DavidAnson/vscode-markdownlint
