# Development and debugging

Start by cloning this repository:

```shell
git clone https://github.com/git-ecosystem/git-credential-manager
```

You also need the latest version of the .NET SDK which can be downloaded and
installed from the [.NET website][dotnet-web].

## Building

The `git-credential-manager.slnx` solution can be opened and built in Visual
Studio, Visual Studio Code, or JetBrains Rider.

Each platform's distributables (installers, packages, and archives) are produced
by building the matching project under `build/`, as shown below. These commands
mirror what CI runs.

### macOS

To build the macOS distribution from the command line, run:

```shell
dotnet build build/macos --configuration=Debug --runtime=osx-arm64
```

Use `osx-x64` or `osx-arm64` for the `--runtime`, or omit it to build for the
host architecture.

The installer package (`.pkg`) and the binary and symbol archives (`.tar.gz`)
are written to `out/package/debug` (`out/package/release` for a Release build).

The flat binaries can also be found in
`out/publish/git-credential-manager/debug_osx-arm64`.

> [!NOTE]
> **Building with Homebrew's .NET SDK**
>
> If your `dotnet` comes from Homebrew (`brew install dotnet`), building the
> macOS distribution fails during the Native AOT link step with an error like:
>
> ```text
> ld: library 'ssl' not found
> clang: error: linker command failed with exit code 1
> ```
>
> Homebrew's .NET is a _non-portable_ build: its Native AOT runtime pack ships a
> `nonportable.txt` marker that makes the linker reference `-lssl -lcrypto` (and
> brotli) directly, instead of the portable `dlopen` path macOS normally uses.
> Those Homebrew libraries aren't on the default linker search path, so the link
> fails. See [dotnet/runtime#120440][dotnet-runtime-120440] for details. The
> official .NET SDK is a portable build and is _not_ affected (it's also what CI
> uses).
>
> **Recommended:** use the official .NET SDK instead of Homebrew's. For example,
> install it side-by-side and put it ahead of Homebrew on your `PATH`:
>
> ```shell
> curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0 --install-dir ~/.dotnet
> export PATH="$HOME/.dotnet:$PATH"
> ```
>
> This also matters for distributable builds: a binary linked by Homebrew's
> `dotnet` gains a hard dependency on the Homebrew OpenSSL dylibs and shouldn't
> be shipped.
>
> **Workaround:** to keep using Homebrew's `dotnet`, add the Homebrew OpenSSL and
> brotli library directories to the linker search path before building:
>
> ```shell
> export LIBRARY_PATH="$(brew --prefix openssl@3)/lib:$(brew --prefix brotli)/lib:$LIBRARY_PATH"
> ```

### Windows

To build the Windows distribution from the command line, run:

```powershell
dotnet build build\windows --configuration=Debug --runtime=win-x64
```

Use `win-x64`, `win-x86`, or `win-arm64` for the `--runtime`, or omit it to
build for the host architecture.

The system and user installers (`gcm-<rid>-<version>.exe` and
`gcmuser-<rid>-<version>.exe`) and the binary archive (`.zip`) are written to
`out\package\debug` (`out\package\release` for a Release build).

The flat binaries can also be found in
`out\publish\git-credential-manager\debug_win-x64`.

### Linux

To build the Linux distribution from the command line, run:

```shell
dotnet build build/linux --configuration=Debug --runtime=linux-x64
```

Use `linux-x64`, `linux-arm64`, or `linux-arm` for the `--runtime`, or omit it
to build for the host architecture.

The Debian package (`.deb`) and the binary and symbol archives (`.tar.gz`) are
written to `out/package/debug` (`out/package/release` for a Release build).

The flat binaries can also be found in
`out/publish/git-credential-manager/debug_linux-x64`.

### .NET tool

The .NET tool NuGet package is platform-agnostic. Build it the way CI does,
through the distribution project, which publishes the product as portable IL and
packs it, stamping the version from the `VERSION` file:

```shell
dotnet build build/dntool --configuration=Debug
```

The package metadata and file layout live in
`build/dntool/Dntool.Distribution.csproj`; packing is just `dotnet pack` over
that project, so no `nuget.exe` and no hand-authored `.nuspec` are needed. The
`.nupkg` is written to `out/package/debug` (`out/package/release` for a Release
build).

To try the freshly built tool without affecting your global tools, install it
into an isolated tool path:

```shell
dotnet tool install --tool-path /tmp/gcm-tool \
  --add-source out/package/debug git-credential-manager
/tmp/gcm-tool/git-credential-manager --version
```

## Debugging

To debug from inside an IDE you'll want to set `git-credential-manager` as the
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
GCM_TRACE=1 out/publish/git-credential-manager/debug_osx-arm64/git-credential-manager --version
16:26:50.667032 ...e/Application.cs:107 trace: [RunInternalAsync] Version: 3.0.0
16:26:50.667295 ...e/Application.cs:108 trace: [RunInternalAsync] Runtime: .NET 10.0.9
16:26:50.667303 ...e/Application.cs:109 trace: [RunInternalAsync] Platform: macOS (ARM64)
16:26:50.667310 ...e/Application.cs:110 trace: [RunInternalAsync] OSVersion: 26.5.1
16:26:50.667316 ...e/Application.cs:111 trace: [RunInternalAsync] AppPath: /Users/user1/src/gcm/out/publish/git-credential-manager/debug_osx-arm64/git-credential-manager
16:26:50.667323 ...e/Application.cs:112 trace: [RunInternalAsync] InstallDir: /Users/user1/src/gcm/out/publish/git-credential-manager/debug_osx-arm64/
16:26:50.667330 ...e/Application.cs:113 trace: [RunInternalAsync] Arguments: --version
3.0.0+a9ecd9c6e31bbc5cb44c530edfd88a8784de5fb0
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
dotnet ~/.nuget/packages/reportgenerator/*/*/net10.0/ReportGenerator.dll -reports:./**/TestResults/**/coverage.cobertura.xml -targetdir:./out/code-coverage
```

or

```shell
dotnet {$env:USERPROFILE}/.nuget/packages/reportgenerator/*/*/net10.0/ReportGenerator.dll -reports:./**/TestResults/**/coverage.cobertura.xml -targetdir:./out/code-coverage
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

[dotnet-runtime-120440]: https://github.com/dotnet/runtime/issues/120440
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
