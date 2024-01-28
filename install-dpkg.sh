#!/bin/bash

set -e

if dpkg -s "gcm" &> /dev/null; then
    echo "GCM already installed, nothing to do."
    exit 1
fi

if [ "$EUID" -ne 0 ]; then
    echo "This script must be run with sudo."
    exit 1
fi

base_path=$(pwd)
linux_packaging_path="$base_path/src/linux/Packaging.Linux"
linux_out_packaging_debug_path="$base_path/out/linux/Packaging.Linux/Debug"
version="2.4.1"

arch=$(uname -m)

if [[ "$arch" == arm64* || "$arch" == aarch64 ]]; then
    out_arch="arm64"
else
    out_arch="amd64"
fi

echo "Run layout script"
sudo -u "$SUDO_USER" "$linux_packaging_path/layout.sh"

echo "Run pack script"
sudo -u "$SUDO_USER" "$linux_packaging_path/pack.sh" "--version=$version" "--payload=$linux_out_packaging_debug_path/payload" "--symbols=$linux_out_packaging_debug_path/payload.sym"

echo "Install GCM"
dpkg -i "$linux_out_packaging_debug_path/deb/gcm-linux_$out_arch.$version.deb"

if [[ "$(sudo -u "$SUDO_USER" git config --global credential.helper)" != "manager" ]]; then
    echo "Configure Git to use GCM"
    sudo -u "$SUDO_USER" git config --global credential.helper manager
fi
