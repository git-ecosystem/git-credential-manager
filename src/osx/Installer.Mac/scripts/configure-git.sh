#!/bin/bash
set -e

# When we're invoked from the macOS Installer, PATH is that of root's
# Even invoking `launchctl asuser` or `su` the PATH remains the same
# because the user's profile has not been run.
# To ensure we have the user's correct PATH we run `path_helper` first.
PATH=""
eval $(/usr/libexec/path_helper -s)

git config --system credential.helper /usr/local/share/gcm-core/git-credential-manager-core
git config --system credential.https://dev.azure.com.useHttpPath true

exit 0
