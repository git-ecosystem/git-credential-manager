#!/bin/sh

# Halt execution immediately on failure.
# Note there are some scenarios in which this will not exit; see
# https://www.gnu.org/software/bash/manual/html_node/The-Set-Builtin.html
# for additional details.
set -e

is_ci=
aot=
for i in "$@"; do
    case "$i" in
        -y)
        is_ci=true
        shift # Past argument=value
        ;;
        --install-prefix=*)
        installPrefix="${i#*=}"
        shift # past argument=value
        ;;
        --aot)
        # Build a native ahead-of-time (AOT) compiled binary, identical to the
        # shipped package. Needs a C toolchain (clang + zlib headers) to link.
        aot=true
        shift
        ;;
        --no-aot)
        # Build a trimmed, self-contained (non-AOT) binary. This is the default
        # and needs only the .NET SDK. Listed for symmetry with --aot.
        aot=
        shift
        ;;
    esac
done

# If install-prefix is not passed, use default value
if [ -z "$installPrefix" ]; then
    installPrefix=/usr/local
fi

# Ensure install directory exists
if [ ! -d "$installPrefix" ]; then
    echo "The folder $installPrefix does not exist"
    exit
fi

# In non-ci scenarios, advertise what we will be doing and
# give user the option to exit.
if [ -z $is_ci ]; then
    echo "This script will download, compile, and install Git Credential Manager to:

    $installPrefix/bin

Git Credential Manager is licensed under the MIT License: https://aka.ms/gcm/license"

    while true; do
        # Display prompt once before reading input
        printf "Do you want to continue? [Y/n] "

        # Prefer reading from the controlling terminal (TTY) when available,
        # so that input works even if the script is piped (e.g. curl URL | sh)
        if [ -r /dev/tty ]; then
            read yn < /dev/tty
        # If no TTY is available, attempt to read from standard input (stdin)
        elif ! read yn; then
            # If input is not possible via TTY or stdin, assume a non-interactive environment
            # and abort with guidance for automated usage
            echo "Interactive prompt unavailable in this environment. Use 'sh -s -- -y' for automated install."
            exit 1
        fi

        case "$yn" in
            [Yy]*|"") break ;;
            [Nn]*) exit ;;
            *) echo "Please answer yes or no." ;;
        esac
    done
fi

install_packages() {
    pkg_manager=$1
    install_verb=$2
    packages=$3

    for package in $packages; do
        # Ensure we don't stomp on existing installations.
        if type $package >/dev/null 2>&1; then
            continue
        fi

        if [ $pkg_manager = apk ]; then
            $sudo_cmd $pkg_manager $install_verb $package
        elif [ $pkg_manager = zypper ]; then
            $sudo_cmd $pkg_manager -n $install_verb $package
        elif [ $pkg_manager = pacman ]; then
            $sudo_cmd $pkg_manager --noconfirm $install_verb $package
        else
            $sudo_cmd $pkg_manager $install_verb $package -y
        fi
    done
}

ensure_dotnet_installed() {
    if [ -z "$(verify_existing_dotnet_installation)" ]; then
        curl -LO https://dot.net/v1/dotnet-install.sh
        chmod +x ./dotnet-install.sh
        ./dotnet-install.sh --channel 10.0

        # dotnet-install.sh installs to $HOME/.dotnet by default but does not add
        # dotnet to this process's PATH, so do that here. Avoid `cd` so we do not
        # disturb the working directory the repo detection below relies on.
        export DOTNET_ROOT="$HOME/.dotnet"
        add_to_PATH "$DOTNET_ROOT"
    fi
}

verify_existing_dotnet_installation() {
    # Get the major.minor of each installed SDK (empty if dotnet is absent).
    sdks=$(dotnet --list-sdks 2>/dev/null | cut -d' ' -f1 | cut -d. -f1,2)

    # If a supported version is installed, echo the list; a non-empty result
    # signals "already installed" to the caller.
    supported_dotnet_versions="10.0"
    for v in $supported_dotnet_versions; do
        if echo "$sdks" | grep -q "$v"; then
            echo "$sdks"
            break
        fi
    done
}

add_to_PATH () {
  for directory; do
    if [ ! -d "$directory" ]; then
        continue; # Skip nonexistent directory.
    fi
    case ":$PATH:" in
        *":$directory:"*)
            break
        ;;
        *)
            export PATH=$PATH:$directory
        ;;
    esac
  done
}

apt_install() {
    pkg_name=$1

    $sudo_cmd apt update
    $sudo_cmd apt install $pkg_name -y 2>/dev/null
}

print_unsupported_distro() {
    prefix=$1
    distro=$2

    echo "$prefix: $distro is not officially supported by the GCM project."
    echo "See https://gh.io/gcm/linux for details."
}

version_at_least() {
	[ "$(printf '%s\n' "$1" "$2" | sort -V | head -n1)" = "$1" ]
}

sudo_cmd=

# If the user isn't root, we need to use `sudo` for certain commands
# (e.g. installing packages).
if [ -z "$sudo_cmd" ]; then
    if [ `id -u` != 0 ]; then
        sudo_cmd=sudo
    fi
fi

# Install the dependencies needed to build and run GCM. The runtime libraries
# and the .NET SDK differ per operating system; the optional --aot build also
# needs a C toolchain (clang + zlib headers) to link the native binary.
os="$(uname -s)"
case "$os" in
    Linux)
        eval "$(sed -n 's/^ID=/distribution=/p' /etc/os-release)"
        eval "$(sed -n 's/^VERSION_ID=/version=/p' /etc/os-release | tr -d '"')"
        case "$distribution" in
            debian | ubuntu)
                $sudo_cmd apt update
                install_packages apt install "curl git"

                # Install dotnet packages and dependencies if needed.
                if [ -z "$(verify_existing_dotnet_installation)" ]; then
                    # First try to use native feeds (Ubuntu 22.04 and later).
                    if ! apt_install dotnet10; then
                        # If the native feeds fail, we fall back to
                        # packages.microsoft.com. We begin by adding the dotnet package
                        # repository/signing key.
                        $sudo_cmd apt update && $sudo_cmd apt install wget -y
                        curl -LO https://packages.microsoft.com/config/"$distribution"/"$version"/packages-microsoft-prod.deb
                        $sudo_cmd dpkg -i packages-microsoft-prod.deb
                        rm packages-microsoft-prod.deb

                        # Proactively install tzdata to prevent prompts.
                        export DEBIAN_FRONTEND=noninteractive
                        $sudo_cmd apt install -y --no-install-recommends tzdata

                        $sudo_cmd apt update
                        $sudo_cmd apt install apt-transport-https -y
                        $sudo_cmd apt update
                        $sudo_cmd apt install dotnet-sdk-10.0 dpkg-dev -y
                    fi
                fi

                if [ -n "$aot" ]; then
                    install_packages apt install "clang zlib1g-dev"
                fi
            ;;
            fedora | centos | rhel | ol)
                $sudo_cmd dnf upgrade -y

                # Install dotnet/GCM dependencies.
                install_packages dnf install "curl git krb5-libs libicu openssl-libs zlib findutils which bash"

                ensure_dotnet_installed

                if [ -n "$aot" ]; then
                    install_packages dnf install "clang zlib-devel"
                fi
            ;;
            alpine)
                $sudo_cmd apk update

                # Install dotnet/GCM dependencies.
                # Alpine 3.14 and earlier need libssl1.1, while later versions need libssl3.
                if ( version_at_least "3.15" $version ) then
                    libssl_pkg="libssl3"
                else
                    libssl_pkg="libssl1.1"
                fi

                install_packages apk add "curl git icu-libs krb5-libs libgcc libintl $libssl_pkg libstdc++ zlib which bash coreutils gcompat"

                ensure_dotnet_installed

                if [ -n "$aot" ]; then
                    install_packages apk add "clang build-base zlib-dev"
                fi
            ;;
            sles | opensuse*)
                $sudo_cmd zypper -n update

                # Install dotnet/GCM dependencies.
                install_packages zypper install "curl git find krb5 libicu"

                ensure_dotnet_installed

                if [ -n "$aot" ]; then
                    install_packages zypper install "clang gcc zlib-devel"
                fi
            ;;
            arch | cachyos)
                print_unsupported_distro "WARNING" "$distribution"

                # --noconfirm required when running from container
                $sudo_cmd pacman -Syu --noconfirm

                # Install dotnet/GCM dependencies.
                install_packages pacman -Sy "curl git glibc gcc krb5 icu openssl libc++ zlib"

                ensure_dotnet_installed

                if [ -n "$aot" ]; then
                    install_packages pacman -Sy "clang"
                fi
            ;;
            mariner | azurelinux*)
                print_unsupported_distro "WARNING" "$distribution"
                $sudo_cmd tdnf update -y

                # Install dotnet/GCM dependencies.
                install_packages tdnf install "curl ca-certificates git krb5-libs libicu openssl-libs zlib findutils which bash awk"

                ensure_dotnet_installed

                if [ -n "$aot" ]; then
                    install_packages tdnf install "clang binutils zlib-devel"
                fi
            ;;
            *)
                print_unsupported_distro "ERROR" "$distribution"
                exit
            ;;
        esac
    ;;
    Darwin)
        # On macOS the runtime libraries are part of the system, so we only need
        # git and the .NET SDK. Homebrew is not required (and its non-portable
        # .NET cannot link an --aot build anyway).
        if ! type git >/dev/null 2>&1; then
            echo "git was not found. Install the Xcode Command Line Tools first:"
            echo "    xcode-select --install"
            exit 1
        fi

        if [ -n "$aot" ] && ! type clang >/dev/null 2>&1; then
            echo "A native (--aot) build needs clang. Install the Xcode Command Line Tools first:"
            echo "    xcode-select --install"
            exit 1
        fi

        ensure_dotnet_installed
    ;;
    *)
        echo "$os is not supported by this script."
        echo "See https://gh.io/gcm/linux for supported Linux distributions."
        exit 1
    ;;
esac

# Detect whether we are running from a full source checkout or standalone
# (for example piped from `curl ... | sh`); clone the repository if standalone.
script_path="$(cd "$(dirname "$0")" 2>/dev/null && pwd)"
toplevel_path="${script_path%/build}"
if [ "z$script_path" = "z$toplevel_path" ] || [ ! -f "$toplevel_path/git-credential-manager.slnx" ]; then
    toplevel_path="$PWD/git-credential-manager"
    test -d "$toplevel_path" || git clone https://github.com/git-ecosystem/git-credential-manager
fi

if [ -z "$DOTNET_ROOT" ]; then
    DOTNET_ROOT="$(dirname $(command -v dotnet))"
fi

# Select the per-OS publish script and the AOT mode. The default is a trimmed,
# self-contained (non-AOT) build that needs only the .NET SDK; --aot produces a
# native binary identical to the shipped package.
case "$os" in
    Linux)  publish_script="$toplevel_path/build/linux/publish.sh" ;;
    Darwin) publish_script="$toplevel_path/build/macos/publish.sh" ;;
esac
if [ -n "$aot" ]; then
    aot_arg=--aot
else
    aot_arg=--no-aot
fi

# Publish the application into a private staging directory. The per-OS publish
# script declares its own interpreter (via its shebang), so invoke it directly
# rather than assuming a particular shell is available here.
staging="$toplevel_path/out/install-from-source/payload"
rm -rf "$staging"
DOTNET_ROOT="$DOTNET_ROOT" PATH="$DOTNET_ROOT:$PATH" "$publish_script" \
    --configuration Release --output "$staging" "$aot_arg"

# Install the published payload under <prefix>/share/gcm-core and add a launcher
# symlink in <prefix>/bin. This matches the layout produced by the .deb package.
install_to="$installPrefix/share/gcm-core"
link_to="$installPrefix/bin"

# Only elevate for the install when the prefix is not writable by the current
# user (e.g. the default /usr/local). A user-writable prefix needs no sudo.
if [ "$(id -u)" = 0 ] || [ -w "$installPrefix" ]; then
    install_sudo=
else
    install_sudo=$sudo_cmd
fi

$install_sudo mkdir -p "$install_to" "$link_to"
$install_sudo cp -R "$staging/." "$install_to/"
$install_sudo chmod -R 755 "$install_to"

# Use a fixed relative target (bin and share are siblings under the prefix), so
# the link resolves once installed and we avoid GNU `ln -r` (absent on macOS).
$install_sudo ln -s -f "../share/gcm-core/git-credential-manager" "$link_to/git-credential-manager"

add_to_PATH "$installPrefix/bin"

echo "Install complete."
echo "You may need to restart your terminal, then configure GCM by running:"
echo "    git-credential-manager configure"
