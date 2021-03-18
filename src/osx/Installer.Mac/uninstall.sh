#!/bin/bash

THISDIR="$( cd "$(dirname "$0")" ; pwd -P )"
GCMBIN="$THISDIR/git-credential-manager-core"

# Ensure we're running as root
if [ $(id -u) != "0" ]
then
	sudo "$0" "$@"
	exit $?
fi

# Unconfigure (as the current user)
echo "Unconfiguring credential helper..."
sudo -u `/usr/bin/logname` "$GCMBIN" unconfigure

# Remove symlink
if [ -L /usr/local/bin/git-credential-manager-core ]
then
	echo "Deleting symlink..."
	rm /usr/local/bin/git-credential-manager-core
else
	echo "No symlink found."
fi

# Forget package installation/delete receipt
echo "Removing installation receipt..."
pkgutil --forget com.microsoft.gitcredentialmanager

# Remove application files
if [ -d "$THISDIR" ]
then
	echo "Deleting application files..."
	rm -rf "$THISDIR"
else
	echo "No application files found."
fi
