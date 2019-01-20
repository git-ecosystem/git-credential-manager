#!/bin/bash

# Unconfigure GCM
HELPER=`git config --system credential.helper`
if [ $HELPER = "/usr/local/share/gcm-core/git-credential-manager" ]
then
	echo "Resetting credential helper to 'osxkeychain'..."
	sudo git config --system credential.helper osxkeychain
else
	echo "GCM was not configured as the Git credential helper."
fi

# Remove GCM symlink
if [ -L /usr/local/bin/git-credential-manager ]
then
	echo "Deleting GCM symlink..."
	rm /usr/local/bin/git-credential-manager
else
	echo "No GCM symlink found."
fi

# Forget package installation/delete receipt
sudo pkgutil --forget com.microsoft.GitCredentialManager

# Remove application files
if [ -d /usr/local/share/gcm-core/ ]
then
	echo "Deleting GCM application files..."
	sudo rm -rf /usr/local/share/gcm-core/
else
	echo "No GCM application files found."
fi