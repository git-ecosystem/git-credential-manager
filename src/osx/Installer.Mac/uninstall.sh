#!/bin/bash

# Ensure we're running as root
if [ $(id -u) != "0" ]
then
	sudo "$0" "$@"
	exit $?
fi

# Unconfigure
echo "Unconfiguring credential helper..."
/usr/local/share/gcm-core/git-credential-manager-core unconfigure

# Remove symlink
if [ -L /usr/local/bin/git-credential-manager-core ]
then
	echo "Deleting symlink..."
	rm /usr/local/bin/git-credential-manager-core
else
	echo "No symlink found."
fi

# Forget package installation/delete receipt
sudo pkgutil --forget com.microsoft.GitCredentialManager

# Remove application files
if [ -d /usr/local/share/gcm-core/ ]
then
	echo "Deleting application files..."
	sudo rm -rf /usr/local/share/gcm-core/
else
	echo "No application files found."
fi
