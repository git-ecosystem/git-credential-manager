#!/bin/sh

# Halt execution immediately on failure.
# Note there are some scenarios in which this will not exit; see
# https://www.gnu.org/software/bash/manual/html_node/The-Set-Builtin.html
# for additional details.
set -e

is_ci=
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
        read -p "Do you want to continue? [Y/n] " yn
        case $yn in
            [Yy]*|"")
                break
            ;;
            [Nn]*)
                exit
            ;;
            *)
                echo "Please answer yes or no."
            ;;
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
        bash -c "./dotnet-install.sh --channel 8.0"

        # Since we have to run the dotnet install script with bash, dotnet isn't
        # added to the process PATH, so we manually add it here.
        cd ~
        export DOTNET_ROOT=$(pwd)/.dotnet
        add_to_PATH $DOTNET_ROOT
    fi
}

verify_existing_dotnet_installation() {
    # Get initial pieces of installed sdk version(s).
    sdks=$(dotnet --list-sdks | cut -c 1-3)

    # If we have a supported version installed, return.
    supported_dotnet_versions="8.0"
    for v in $supported_dotnet_versions; do
        if [ $(echo $sdks | grep "$v") ]; then
            echo $sdks
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

eval "$(sed -n 's/^ID=/distribution=/p' /etc/os-release)"
eval "$(sed -n 's/^VERSION_ID=/version=/p' /etc/os-release | tr -d '"')"
case "$distribution" in
    debian | ubuntu)
        $sudo_cmd apt update
        install_packages apt install "curl git"

        # Install dotnet packages and dependencies if needed.
        if [ -z "$(verify_existing_dotnet_installation)" ]; then
            # First try to use native feeds (Ubuntu 22.04 and later).
            if ! apt_install dotnet8; then
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
                $sudo_cmd apt install dotnet-sdk-8.0 dpkg-dev -y
            fi
        fi
    ;;
    fedora | centos | rhel)
        $sudo_cmd dnf upgrade -y

        # Install dotnet/GCM dependencies.
        install_packages dnf install "curl git krb5-libs libicu openssl-libs zlib findutils which bash"

        ensure_dotnet_installed
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
    ;;
    sles | opensuse*)
        $sudo_cmd zypper -n update

        # Install dotnet/GCM dependencies.
        install_packages zypper install "curl git find krb5 libicu libopenssl1_1"

        ensure_dotnet_installed
    ;;
    arch)
        print_unsupported_distro "WARNING" "$distribution"

        # --noconfirm required when running from container
        $sudo_cmd pacman -Syu --noconfirm

        # Install dotnet/GCM dependencies.
        install_packages pacman -Sy "curl git glibc gcc krb5 icu openssl libc++ zlib"

        ensure_dotnet_installed
    ;;
    mariner)
        print_unsupported_distro "WARNING" "$distribution"
        $sudo_cmd tdnf update -y

        # Install dotnet/GCM dependencies.
        install_packages tdnf install "curl ca-certificates git krb5-libs libicu openssl-libs zlib findutils which bash awk"

        ensure_dotnet_installed
    ;;
    *)
        print_unsupported_distro "ERROR" "$distribution"
        exit
    ;;
esac

# Detect if the script is part of a full source checkout or standalone instead.
script_path="$(cd "$(dirname "$0")" && pwd)"
toplevel_path="${script_path%/src/linux/Packaging.Linux}"
if [ "z$script_path" = "z$toplevel_path" ] || [ ! -f "$toplevel_path/Git-Credential-Manager.sln" ]; then
    toplevel_path="$PWD/git-credential-manager"
    test -d "$toplevel_path" || git clone https://github.com/git-ecosystem/git-credential-manager
fi

if [ -z "$DOTNET_ROOT" ]; then
    DOTNET_ROOT="$(dirname $(which dotnet))"
fi

cd "$toplevel_path"
$sudo_cmd env "PATH=$PATH" $DOTNET_ROOT/dotnet build ./src/linux/Packaging.Linux/Packaging.Linux.csproj -c Release -p:InstallFromSource=true -p:installPrefix=$installPrefix
add_to_PATH "$installPrefix/bin"
