#!/bin/bash

# Product information
NAME="Git Credential Manager"
IDENTIFIER="com.microsoft.GitCredentialManager"
INSTALL_LOCATION="/usr/local/share/gcm-core"

# Directories
THISDIR="$( cd "$(dirname "$0")" ; pwd -P )"
ROOT="$( cd $THISDIR/../.. ; pwd -P )"
OUT=$ROOT/out
PKGSCRIPTS=$ROOT/macos/installer/scripts
MACOUT=$OUT/macos
INSTALLEROUT=$MACOUT/installer/
PKGPAYLOAD=$INSTALLEROUT/payload

# Parse script arguments
for i in "$@"
do
case $i in
    -v=*|--version=*)
    VERSION="${i#*=}"
    shift # past argument=value
    ;;
    -c=*|--configuration=*)
    CONFIGURATION="${i#*=}"
    shift # past argument=value
    ;;
    *)
          # unknown option
    ;;
esac
done

# Set default arguments
VERSION=${VERSION:=1.0}
CONFIGURATION=${CONFIGURATION:=Release}

# Ensure output directories exist
mkdir -p $MACOUT $PKGPAYLOAD

# Publish the core product
dotnet publish --runtime osx-x64 --configuration $CONFIGURATION --output $PKGPAYLOAD $ROOT/common/src/Git-Credential-Manager

# Build the native credential helpers
pod install --project-directory=$ROOT/macos/Microsoft.Authentication.Helper
xcodebuild \
    -workspace $ROOT/macos/Microsoft.Authentication.Helper/Microsoft.Authentication.Helper.xcworkspace \
    -scheme Microsoft.Authentication.Helper \
    -configuration $CONFIGURATION \
    -derivedDataPath $MACOUT/Microsoft.Authentication.Helper

# Copy helper executables to payload directory
cp $MACOUT/Microsoft.Authentication.Helper/Build/Products/$CONFIGURATION/Microsoft.Authentication.Helper $PKGPAYLOAD

# Copy uninstaller script
cp $ROOT/macos/installer/uninstall-gcm.sh $PKGPAYLOAD

# Remove any unwanted .DS_Store files
find $PKGPAYLOAD -name '*.DS_Store' -type f -delete

# Set full read, write, execute permissions for owner and just read and execute permissions for group and other
/bin/chmod -R 755 $PKGPAYLOAD

# Remove any extended attributes (ACEs)
/usr/bin/xattr -rc $PKGPAYLOAD

# Build installer package
/usr/bin/pkgbuild \
    --root $PKGPAYLOAD/ \
    --install-location "$INSTALL_LOCATION" \
    --scripts $PKGSCRIPTS/ \
    --identifier "$IDENTIFIER" \
    --version "$VERSION" \
    "$INSTALLEROUT/$NAME.pkg"
