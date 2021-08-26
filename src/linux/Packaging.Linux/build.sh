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
BITBUCKET_UI_SRC="$SRC/shared/Atlassian.Bitbucket.UI"
GITHUB_UI_SRC="$SRC/shared/GitHub.UI"
PROJ_OUT="$OUT/linux/Packaging.Linux"

# Build parameters
FRAMEWORK=net5.0
RUNTIME=linux-x64

# Perform pre-execution checks
CONFIGURATION="${CONFIGURATION:=Debug}"
if [ -z "$VERSION" ]; then
    die "--version was not set"
fi

ARCH="`dpkg-architecture -q DEB_HOST_ARCH`"
if test -z "$ARCH"; then
  die "Could not determine host architecture!"
fi

# Outputs
PAYLOAD="$PROJ_OUT/payload/$CONFIGURATION"
SYMBOLOUT="$PROJ_OUT/payload.sym/$CONFIGURATION"

TAROUT="$PROJ_OUT/tar/$CONFIGURATION"
TARBALL="$TAROUT/gcmcore-linux_$ARCH.$VERSION.tar.gz"
SYMTARBALL="$TAROUT/symbols-linux_$ARCH.$VERSION.tar.gz"

DEBOUT="$PROJ_OUT/deb/$CONFIGURATION"
DEBROOT="$DEBOUT/root"
DEBPKG="$DEBOUT/gcmcore-linux_$ARCH.$VERSION.deb"

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
mkdir -p "$PAYLOAD" "$SYMBOLOUT" "$DEBROOT"

# Publish core application executables
echo "Publishing core application..."
dotnet publish "$GCM_SRC" \
	--configuration="$CONFIGURATION" \
	--framework="$FRAMEWORK" \
	--runtime="$RUNTIME" \
	--self-contained=true \
	-p:PublishSingleFile=true \
	--output="$(make_absolute "$PAYLOAD")" || exit 1

echo "Publishing Bitbucket UI helper..."
dotnet publish "$BITBUCKET_UI_SRC" \
	--configuration="$CONFIGURATION" \
	--framework="$FRAMEWORK" \
	--runtime="$RUNTIME" \
	--self-contained=true \
	-p:PublishSingleFile=true \
	--output="$(make_absolute "$PAYLOAD")" || exit 1

echo "Publishing GitHub UI helper..."
dotnet publish "$GITHUB_UI_SRC" \
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
# PACKING
#####################################################################
echo "Packing Packaging.Linux..."
# Cleanup any old archive files
if [ -e "$TAROUT" ]; then
    echo "Deleteing old archive '$TAROUT'..."
    rm "$TAROUT"
fi

# Ensure the parent directory for the archive exists
mkdir -p "$TAROUT" || exit 1

# Set full read, write, execute permissions for owner and just read and execute permissions for group and other
echo "Setting file permissions..."
/bin/chmod -R 755 "$PAYLOAD" || exit 1

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
cat >"$DEBROOT/DEBIAN/control" <<EOF
Package: gcmcore
Version: $VERSION
Section: vcs
Priority: optional
Architecture: $ARCH
Depends:
Maintainer: GCM-Core <gitfundamentals@github.com>
Description: Cross Platform Git Credential Manager Core command line utility.
 GCM Core supports authentication with a number of Git hosting providers
 including GitHub, BitBucket, and Azure DevOps.
 For more information see https://aka.ms/gcmcore
EOF

# Copy GCM Core & UI helper binaries to target installation location
cp "$PAYLOAD/git-credential-manager-core" "$INSTALL_TO" || exit 1
cp "$PAYLOAD/Atlassian.Bitbucket.UI" "$INSTALL_TO" || exit 1
cp "$PAYLOAD/GitHub.UI" "$INSTALL_TO" || exit 1

# Create symlinks
ln -s -r "$INSTALL_TO/git-credential-manager-core" \
    "$LINK_TO/git-credential-manager-core" || exit 1
ln -s -r "$INSTALL_TO/Atlassian.Bitbucket.UI" \
    "$LINK_TO/Atlassian.Bitbucket.UI" || exit 1
ln -s -r "$INSTALL_TO/GitHub.UI" \
    "$LINK_TO/GitHub.UI" || exit 1

dpkg-deb --build "$DEBROOT" "$DEBPKG" || exit 1

echo "Pack complete."
