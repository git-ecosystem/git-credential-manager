#!/bin/bash
#
# Builds the Linux package (.deb) for Git Credential Manager: stages the
# published application into a Debian package root, writes the control file and
# a launcher symlink, then builds the .deb with dpkg-deb.
#
set -euo pipefail

THISDIR="$( cd "$(dirname "$0")" ; pwd -P )"
. "$THISDIR/../lib-cli.sh"

print_usage () {
    cat <<EOF
Usage: $(basename "$0") [options]

Build the Linux package (.deb) from the published git-credential-manager
application. The published files are staged into a private Debian package root
(installed under /usr/local/share/gcm-core with a launcher symlink in
/usr/local/bin); dpkg-deb then builds the package under the top-level artifacts
package directory.

Options:
  -c, --configuration <name>   Build configuration: Debug or Release.
                               (default: Release)
  -r, --runtime <rid>          Target runtime identifier: 'linux-x64', 'linux-arm64'
                               or 'linux-arm'. (default: auto-detected from the host)
  --version <version>          Version to stamp into the package.
                               (default: the repository VERSION file)
  --bindir <dir>               Directory of published binaries to package.
                               (default: the application's default publish dir)
  --output <dir>               Directory to write the package to.
                               (default: out/package/<config>)
  -v, --verbose                Enable verbose output. (default: off)
  -h, --help                   Show this help text and exit.

Examples:
  $(basename "$0")
  $(basename "$0") --configuration Release --runtime linux-x64 --version 2.6.1
EOF
}

# Install location and package assets
INSTALL_LOCATION="/usr/local/share/gcm-core"
LINK_LOCATION="/usr/local/bin"
# The control file template (with %%VERSION%%/%%ARCH%% tokens) lives in the
# sibling debian-package/ directory.
CONTROL_TEMPLATE="$THISDIR/debian-package/control"

# Defaults
CONFIGURATION=""
RUNTIME=""
VERSION=""
BINDIR=""
OUTPUT=""

# Parse arguments.
while [ "$#" -gt 0 ]; do
    case "$1" in
        -h|--help)
            print_usage
            exit 0
            ;;
        -v|--verbose)       enable_verbose; shift ;;
        -c|--configuration) require_value "$@"; CONFIGURATION="$2"; shift 2 ;;
        -r|--runtime)       require_value "$@"; RUNTIME="$2"; shift 2 ;;
        --version)          require_value "$@"; VERSION="$2"; shift 2 ;;
        --bindir)           require_value "$@"; BINDIR="$2"; shift 2 ;;
        --output)           require_value "$@"; OUTPUT="$2"; shift 2 ;;
        --)                 shift; break ;;
        -*)                 die "unknown option '$1' (try '$(basename "$0") --help')" ;;
        *)                  die "unexpected argument '$1' (try '$(basename "$0") --help')" ;;
    esac
done

if [ "$#" -gt 0 ]; then
    die "unexpected argument '$1' (try '$(basename "$0") --help')"
fi

# Normalise arguments / apply defaults.
CONFIGURATION="$(normalize_configuration "$CONFIGURATION")" || exit 1
RUNTIME="$(normalize_runtime "$RUNTIME")" || exit 1
VERSION="$(normalize_version "$VERSION")" || exit 1

# Source of the published binaries to package (override with --bindir; defaults
# to the application's default artifacts publish directory, from publish.sh).
if [ -n "$BINDIR" ]; then
    BINDIR="$(make_absolute "$BINDIR")"
else
    BINDIR="$(publish_dir git-credential-manager "$CONFIGURATION" "$RUNTIME")" || exit 1
fi

# Private scratch (the staged Debian package root), kept per runtime under the
# artifacts bin directory so that builds for different runtimes don't collide.
SCRATCH="$(bin_dir Linux.Distribution "$CONFIGURATION" "$RUNTIME")" || exit 1
DEBROOT="$SCRATCH/root"
INSTALL_DIR="$DEBROOT$INSTALL_LOCATION"
LINK_DIR="$DEBROOT$LINK_LOCATION"

# Destination directory for the package (override with --output; defaults to the
# top-level artifacts package directory). The file name is always defined here.
if [ -n "$OUTPUT" ]; then
    OUTDIR="$(make_absolute "$OUTPUT")"
else
    OUTDIR="$(package_dir "$CONFIGURATION")" || exit 1
fi
DEBOUT="$OUTDIR/gcm-$RUNTIME-$VERSION.deb"

# Map the .NET runtime identifier to a Debian architecture.
case "$RUNTIME" in
    linux-x64)   ARCH="amd64" ;;
    linux-arm64) ARCH="arm64" ;;
    linux-arm)   ARCH="armhf" ;;
    *)           die "unsupported runtime '$RUNTIME' for a Debian package" ;;
esac

# Pre-execution checks
[ -d "$BINDIR" ] || die "Publish directory '$BINDIR' not found. Did you publish first?"
[ -f "$CONTROL_TEMPLATE" ] || die "Control file template '$CONTROL_TEMPLATE' not found"

verbose "configuration: $CONFIGURATION"
verbose "runtime:       $RUNTIME"
verbose "version:       $VERSION"
verbose "architecture:  $ARCH"
verbose "bin dir:       $BINDIR"
verbose "scratch:       $SCRATCH"
verbose "output:        $DEBOUT"

# Stage a fresh, private package root.
info "Staging package root from '$BINDIR'..."
rm -rf "$DEBROOT"
mkdir -p "$DEBROOT/DEBIAN" "$INSTALL_DIR" "$LINK_DIR"
cp -R "$BINDIR/." "$INSTALL_DIR/" || die "Failed to stage payload"

# Normalise permissions.
info "Setting file permissions..."
chmod -R 755 "$INSTALL_DIR" || die "Failed to set payload permissions"

# Create a launcher symlink (relative, so it resolves once installed).
info "Creating launcher symlink..."
ln -s -r "$INSTALL_DIR/git-credential-manager" "$LINK_DIR/git-credential-manager" \
    || die "Failed to create launcher symlink"

# Generate the control file from the template, filling in the version and
# architecture tokens.
info "Writing Debian control file..."
sed -e "s/%%VERSION%%/$VERSION/g" \
    -e "s/%%ARCH%%/$ARCH/g" \
    "$CONTROL_TEMPLATE" > "$DEBROOT/DEBIAN/control" \
    || die "Failed to write Debian control file"

# Build the package.
info "Building Debian package..."
rm -f "$DEBOUT"
mkdir -p "$(dirname "$DEBOUT")"
dpkg-deb -Zxz --root-owner-group --build "$DEBROOT" "$DEBOUT" \
    || die "Failed to build Debian package"

info "Packaging complete: $DEBOUT"
