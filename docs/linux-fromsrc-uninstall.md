# Uninstalling after installing from source

These instructions will guide you in removing GCM after running the
[install from source script][install-from-source] on your Linux distribution.

:rotating_light: PROCEED WITH CAUTION :rotating_light:

For completeness, we provide uninstall instructions for _the GCM application,
the GCM repo, and the maximum number of dependencies*_ for all distributions.
This repo and these dependencies may or may not have already been present on
your system when you ran the install from source script, and uninstalling them
could impact other programs and/or your normal workflows. Please keep this in
mind when following the instructions below.

*Certain distributions require some dependencies of the script to function as
expected, so we only include instructions to remove the non-required
dependencies.

## All distributions

**Note:** If you ran the install from source script from a pre-existing clone of
the `git-credential-manager` repo or outside of your `$HOME` directory, you will
need to modify the final two commands below to point to the location of your
pre-existing clone or the directory from which you ran the install from source
script.

```console
git-credential-manager-core unconfigure &&
sudo rm $(command -v git-credential-manager-core) &&
sudo rm -rf /usr/local/share/gcm-core &&
sudo rm -rf ~/git-credential-manager &&
sudo rm ~/install-from-source.sh
```

## Debian/Ubuntu

**Note:** If you had a pre-existing installation of dotnet that was not
installed via `apt` or `apt-get` when you ran the install from source script,
you will need to remove it using [these instructions][uninstall-dotnet] and
remove `dotnet-*` from the below command.

```console
sudo apt remove dotnet-* dpkg-dev apt-transport-https git curl wget
```

## Linux Mint

**Note:** If you had a pre-existing installation of dotnet when you ran the
install from source script that was not located at `~/.dotnet`, you will need to
modify the first command below to point to the custom install location. If you
would like to remove the specific version of dotnet that the script installed
and keep other versions, you can do so with [these instructions][uninstall-dotnet].

```console
sudo rm -rf ~/.dotnet &&
sudo apt remove git curl
```

## Fedora/CentOS/RHEL

**Note:** If you had a pre-existing installation of dotnet when you ran the
install from source script that was not located at `~/.dotnet`, you will need to
modify the first command below to point to the custom install location. If you
would like to remove the specific version of dotnet that the script installed
and keep other versions, you can do so with [these instructions][uninstall-dotnet].

```console
sudo rm -rf ~/.dotnet
```

## Alpine

**Note:** If you had a pre-existing installation of dotnet when you ran the
install from source script that was not located at `~/.dotnet`, you will need to
modify the first command below to point to the custom install location. If you
would like to remove the specific version of dotnet that the script installed
and keep other versions, you can do so with [these instructions][uninstall-dotnet].

```console
sudo rm -rf ~/.dotnet &&
sudo apk del icu-libs krb5-libs libgcc libintl libssl1.1 libstdc++ zlib which
bash coreutils gcompat git curl
```

[install-from-source]: ../src/linux/Packaging.Linux/install-from-source.sh
[uninstall-dotnet]: https://docs.microsoft.com/en-us/dotnet/core/install/remove-runtime-sdk-versions?pivots=os-linux#uninstall-net
