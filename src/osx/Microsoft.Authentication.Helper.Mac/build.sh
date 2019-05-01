#!/bin/bash
die () {
    echo "$*" >&2
    exit 1
}

echo "Building Microsoft.Authentication.Helper.Mac..."

# Directories
THISDIR="$( cd "$(dirname "$0")" ; pwd -P )"
ROOT="$( cd $THISDIR/../../.. ; pwd -P )"
SRC=$ROOT/src
OUT=$ROOT/out
MSAUTH_SRC=$SRC/osx/Microsoft.Authentication.Helper.Mac
MSAUTH_OUT=$OUT/osx/Microsoft.Authentication.Helper.Mac

# Parse script arguments
for i in "$@"
do
case $i in
    --configuration=*)
    CONFIGURATION="${i#*=}"
    shift # past argument=value
    ;;
    *)
          # unknown option
    ;;
esac
done

# Set default arguments
CONFIGURATION=${CONFIGURATION:=Debug}

MSAUTH_BINOUT=$MSAUTH_OUT/bin/$CONFIGURATION/native
MSAUTH_SYMOUT=$MSAUTH_OUT/bin/$CONFIGURATION/native.sym
MSAUTH_OBJOUT=$MSAUTH_OUT/xcodebuild

# Ensure output directories exist
mkdir -p $MSAUTH_OUT || exit 1

# # Ensure Cocoapods have been restored (try without updating the repo first as this can take a while)
echo "Restoring Cocoapods..."
pod install --project-directory=$MSAUTH_SRC
if [ $? -ne 0 ]; then
    echo "Failed to restore Cocoapods. Will update Cocoapods repository and try again..."
    pod install --repo-update --project-directory=$MSAUTH_SRC || exit 1
fi

# Build the Xcode workspace
echo "Building Xcode workspace..."
xcodebuild \
    -workspace $MSAUTH_SRC/Microsoft.Authentication.Helper.xcworkspace \
    -scheme Microsoft.Authentication.Helper \
    -configuration $CONFIGURATION \
    -derivedDataPath $MSAUTH_OBJOUT || exit 1

MSAUTH_EXEC=$MSAUTH_OBJOUT/Build/Products/$CONFIGURATION/Microsoft.Authentication.Helper

# Copy binaries
echo "Copying binaries..."
mkdir -p $MSAUTH_BINOUT || exit 1
cp $MSAUTH_EXEC $MSAUTH_BINOUT || exit 1

# Copy dSYM symbol files
echo "Copying symbols..."
mkdir -p $MSAUTH_SYMOUT || exit 1
cp -R $MSAUTH_EXEC.dSYM $MSAUTH_SYMOUT || exit 1

echo "Build of Microsoft.Authentication.Helper.Mac complete."
