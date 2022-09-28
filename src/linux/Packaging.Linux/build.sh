#!/bin/bash
die () {
    echo "$*" >&2
    exit 1
}

make_absolute () {
    case "$1" in
    /*)
        echo "$1"
        ;;
    *)
        echo "$PWD/$1"
        ;;
    esac
}

#####################################################################
# Building
#####################################################################
echo "Building Packaging.Linux..."
# Parse script arguments
for i in "$@"
do
case "$i" in
    --configuration=*)
    CONFIGURATION="${i#*=}"
    shift # past argument=value
    ;;
    --version=*)
    VERSION="${i#*=}"
    shift # past argument=value
    ;;
    --install-from-source=*)
    INSTALL_FROM_SOURCE=${i#*=}
    shift # past argument=value
    ;;
    *)
          # unknown option
    ;;
esac
done

# Directories
THISDIR="$( cd "$(dirname "$0")" ; pwd -P )"
ROOT="$( cd "$THISDIR"/../../.. ; pwd -P )"
SRC="$ROOT/src"
OUT="$ROOT/out"
GCM_SRC="$SRC/shared/Git-Credential-Manager"
GCM_UI_SRC="$SRC/shared/Git-Credential-Manager.UI.Avalonia"
BITBUCKET_UI_SRC="$SRC/shared/Atlassian.Bitbucket.UI.Avalonia"
GITHUB_UI_SRC="$SRC/shared/GitHub.UI.Avalonia"
GITLAB_UI_SRC="$SRC/shared/GitLab.UI.Avalonia"
PROJ_OUT="$OUT/linux/Packaging.Linux"

# Build parameters
FRAMEWORK=net6.0
RUNTIME=linux-x64

# Perform pre-execution checks
CONFIGURATION="${CONFIGURATION:=Debug}"
if [ -z "$VERSION" ]; then
    die "--version was not set"
fi

if [ $INSTALL_FROM_SOURCE = false ]; then
    ARCH="`dpkg-architecture -q DEB_HOST_ARCH`"
    if test -z "$ARCH"; then
    die "Could not determine host architecture!"
    fi
fi

# Outputs
PAYLOAD="$PROJ_OUT/payload/$CONFIGURATION"
SYMBOLOUT="$PROJ_OUT/payload.sym/$CONFIGURATION"

if [ $INSTALL_FROM_SOURCE = false ]; then
    TAROUT="$PROJ_OUT/tar/$CONFIGURATION"
    TARBALL="$TAROUT/gcm-linux_$ARCH.$VERSION.tar.gz"
    SYMTARBALL="$TAROUT/gcm-linux_$ARCH.$VERSION-symbols.tar.gz"

    DEBOUT="$PROJ_OUT/deb/$CONFIGURATION"
    DEBROOT="$DEBOUT/root"
    DEBPKG="$DEBOUT/gcm-linux_$ARCH.$VERSION.deb"
else
    INSTALL_LOCATION="/usr/local"
fi

# Cleanup payload directory
if [ -d "$PAYLOAD" ]; then
    echo "Cleaning existing payload directory '$PAYLOAD'..."
    rm -rf "$PAYLOAD"
fi

# Cleanup symbol directory
if [ -d "$SYMBOLOUT" ]; then
    echo "Cleaning existing symbols directory '$SYMBOLOUT'..."
    rm -rf "$SYMBOLOUT"
fi

# Ensure directories exists
mkdir -p "$PAYLOAD" "$SYMBOLOUT"

if [ $INSTALL_FROM_SOURCE = false ]; then
    mkdir -p "$DEBROOT"
else
    mkdir -p "$INSTALL_LOCATION"
fi

if [ -z "$DOTNET_ROOT" ]; then
    DOTNET_ROOT="$(dirname $(which dotnet))"
fi

# Publish core application executables
echo "Publishing core application..."
$DOTNET_ROOT/dotnet publish "$GCM_SRC" \
	--configuration="$CONFIGURATION" \
	--framework="$FRAMEWORK" \
	--runtime="$RUNTIME" \
	--self-contained=true \
	-p:PublishSingleFile=true \
	--output="$(make_absolute "$PAYLOAD")" || exit 1

echo "Publishing core UI helper..."
$DOTNET_ROOT/dotnet publish "$GCM_UI_SRC" \
	--configuration="$CONFIGURATION" \
	--framework="$FRAMEWORK" \
	--runtime="$RUNTIME" \
	--self-contained=true \
	-p:PublishSingleFile=true \
	--output="$(make_absolute "$PAYLOAD")" || exit 1

echo "Publishing Bitbucket UI helper..."
$DOTNET_ROOT/dotnet publish "$BITBUCKET_UI_SRC" \
	--configuration="$CONFIGURATION" \
	--framework="$FRAMEWORK" \
	--runtime="$RUNTIME" \
	--self-contained=true \
	-p:PublishSingleFile=true \
	--output="$(make_absolute "$PAYLOAD")" || exit 1

echo "Publishing GitHub UI helper..."
$DOTNET_ROOT/dotnet publish "$GITHUB_UI_SRC" \
	--configuration="$CONFIGURATION" \
	--framework="$FRAMEWORK" \
	--runtime="$RUNTIME" \
	--self-contained=true \
	-p:PublishSingleFile=true \
	--output="$(make_absolute "$PAYLOAD")" || exit 1

echo "Publishing GitLab UI helper..."
$DOTNET_ROOT/dotnet publish "$GITLAB_UI_SRC" \
	--configuration="$CONFIGURATION" \
	--framework="$FRAMEWORK" \
	--runtime="$RUNTIME" \
	--self-contained=true \
	-p:PublishSingleFile=true \
	--output="$(make_absolute "$PAYLOAD")" || exit 1

# Collect symbols
echo "Collecting managed symbols..."
mv "$PAYLOAD"/*.pdb "$SYMBOLOUT" || exit 1

echo "Build complete."

#####################################################################
# PACKING AND INSTALLING
#####################################################################
# Set full read, write, execute permissions for owner and just read and execute permissions for group and other
echo "Setting file permissions..."
/bin/chmod -R 755 "$PAYLOAD" || exit 1

if [ $INSTALL_FROM_SOURCE = false ]; then
    echo "Packing Packaging.Linux..."
    # Cleanup any old archive files
    if [ -e "$TAROUT" ]; then
        echo "Deleteing old archive '$TAROUT'..."
        rm "$TAROUT"
    fi

    # Ensure the parent directory for the archive exists
    mkdir -p "$TAROUT" || exit 1

    # Build binaries tarball
    echo "Building binaries tarball..."
    pushd "$PAYLOAD"
    tar -czvf "$TARBALL" * || exit 1
    popd

    # Build symbols tarball
    echo "Building symbols tarball..."
    pushd "$SYMBOLOUT"
    tar -czvf "$SYMTARBALL" * || exit 1
    popd

    # Build .deb
    INSTALL_TO="$DEBROOT/usr/local/share/gcm-core/"
    LINK_TO="$DEBROOT/usr/local/bin/"
    mkdir -p "$DEBROOT/DEBIAN" "$INSTALL_TO" "$LINK_TO" || exit 1

# make the debian control file
# this is purposefully not indented, see
# https://stackoverflow.com/questions/9349616/bash-eof-in-if-statement
# for details
cat >"$DEBROOT/DEBIAN/control" <<EOF
Package: gcm
Version: $VERSION
Section: vcs
Priority: optional
Architecture: $ARCH
Depends:
Maintainer: GCM <gitfundamentals@github.com>
Description: Cross Platform Git Credential Manager command line utility.
 GCM supports authentication with a number of Git hosting providers
 including GitHub, BitBucket, and Azure DevOps.
 For more information see https://aka.ms/gcm
EOF
else
    echo "Installing..."

    # Install directories
    INSTALL_TO="$INSTALL_LOCATION/share/gcm-core/"
    LINK_TO="$INSTALL_LOCATION/bin/"
    MESSAGE="Install complete."
fi

mkdir -p "$INSTALL_TO" "$LINK_TO"

# Copy all binaries and shared libraries to target installation location
cp -R "$PAYLOAD"/* "$INSTALL_TO" || exit 1

# Create symlink
if [ ! -f "$LINK_TO/git-credential-manager-core" ]; then
    ln -s -r "$INSTALL_TO/git-credential-manager-core" \
        "$LINK_TO/git-credential-manager-core" || exit 1
fi

if [ $INSTALL_FROM_SOURCE = false ]; then
    dpkg-deb --build "$DEBROOT" "$DEBPKG" || exit 1
fi

echo $MESSAGE
