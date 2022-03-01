#!/bin/sh

# halt execution immediately on failure
# note there are some scenarios in which this will not exit;
# see https://www.gnu.org/software/bash/manual/html_node/The-Set-Builtin.html
# for additional details
set -e

is_ci=
for i in "$@"; do
    case "$i" in
        -y)
        is_ci=true
        shift # past argument=value
        ;;
    esac
done

# in non-ci scenarios, advertise what we will be doing and
# give user the option to exit
if [ -z $is_ci ]; then
    echo "This script will download, compile, and install Git Credential Manager to:

    /usr/local/bin

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

install_shared_packages() {
    pkg_manager=$1
    install_verb=$2

    local shared_packages="git curl"
    for package in $shared_packages; do
        # ensure we don't stomp on existing installations
        if [ ! -z $(which $package) ]; then
            continue
        fi

        if [ $pkg_manager = apk ]; then
            $sudo_cmd $pkg_manager $install_verb $package
        else
            $sudo_cmd $pkg_manager $install_verb $package -y
        fi
    done
}

ensure_dotnet_installed() {
    if [ -z "$(verify_existing_dotnet_installation)" ]; then
        curl -LO https://dot.net/v1/dotnet-install.sh
        chmod +x ./dotnet-install.sh
        bash -c "./dotnet-install.sh"

        # since we have to run the dotnet install script with bash, dotnet isn't added
        # to the process PATH, so we manually add it here
        cd ~
        export DOTNET_ROOT=$(pwd)/.dotnet
        add_to_PATH $DOTNET_ROOT
    fi
}

verify_existing_dotnet_installation() {
    # get initial pieces of installed sdk version(s)
    sdks=$(dotnet --list-sdks | cut -c 1-3)

    # if we have a supported version installed, return
    supported_dotnet_versions="6.0 5.0"
    for v in $supported_dotnet_versions; do
        if [ $(echo $sdks | grep "$v") ]; then
            echo $sdks
        fi
    done
}

add_to_PATH () {
  for directory; do
    if [ ! -d "$directory" ]; then
        continue; # skip nonexistent directory
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

sudo_cmd=

# if the user isn't root, we need to use `sudo` for certain commands
# (e.g. installing packages)
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
        install_shared_packages apt install

        # add dotnet package repository/signing key
        $sudo_cmd apt update && $sudo_cmd apt install wget -y
        curl -LO https://packages.microsoft.com/config/"$distribution"/"$version"/packages-microsoft-prod.deb
        $sudo_cmd dpkg -i packages-microsoft-prod.deb
        rm packages-microsoft-prod.deb

        # proactively install tzdata to prevent prompts
        export DEBIAN_FRONTEND=noninteractive
        $sudo_cmd apt install -y --no-install-recommends tzdata

        # install dotnet packages and dependencies if needed
        if [ -z "$(verify_existing_dotnet_installation)" ]; then
            $sudo_cmd apt update
            $sudo_cmd apt install apt-transport-https -y
            $sudo_cmd apt update
            $sudo_cmd apt install dotnet-sdk-5.0 dpkg-dev -y
        fi
    ;;
    linuxmint)
        $sudo_cmd apt update
        install_shared_packages apt install

        # install dotnet packages and dependencies
        $sudo_cmd apt install libc6 libgcc1 libgssapi-krb5-2 libssl1.1 libstdc++6 zlib1g libicu66 -y
        ensure_dotnet_installed
    ;;
    fedora | centos | rhel)
        $sudo_cmd dnf update -y
        install_shared_packages dnf install

        # install dotnet/gcm dependencies
        $sudo_cmd dnf install krb5-libs libicu openssl-libs zlib findutils which bash -y

        ensure_dotnet_installed
    ;;
    alpine)
        $sudo_cmd apk update
        install_shared_packages apk add

        # install dotnet/gcm dependencies
        $sudo_cmd apk add icu-libs krb5-libs libgcc libintl libssl1.1 libstdc++ zlib which bash coreutils gcompat

        ensure_dotnet_installed
    ;;
    *)
        echo "ERROR: Unsupported Linux distribution: $distribution"
        exit
    ;;
esac

# detect if the script is part of a full source checkout or standalone instead
script_path="$(cd "$(dirname "$0")" && pwd)"
toplevel_path="${script_path%/src/linux/Packaging.Linux}"
if [ "z$script_path" = "z$toplevel_path" ] || [ ! -f "$toplevel_path/Git-Credential-Manager.sln" ]; then
    toplevel_path="$PWD/git-credential-manager"
    test -d "$toplevel_path" || git clone https://github.com/GitCredentialManager/git-credential-manager
fi

cd "$toplevel_path"
$sudo_cmd dotnet build ./src/linux/Packaging.Linux/Packaging.Linux.csproj -c Release -p:InstallFromSource=true
add_to_PATH "/usr/local/bin"