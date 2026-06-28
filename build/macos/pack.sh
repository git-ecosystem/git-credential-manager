#!/bin/bash
#
# Builds the macOS installer (.pkg) for Git Credential Manager: stages a private
# copy of the published application, adds the uninstaller, builds a component
# package with pkgbuild, then wraps it in the final product (distribution)
# package with productbuild.
#
set -euo pipefail

THISDIR="$( cd "$(dirname "$0")" ; pwd -P )"
. "$THISDIR/../lib-cli.sh"

print_usage () {
    cat <<EOF
Usage: $(basename "$0") [options]

Build the macOS product package (.pkg) from the published git-credential-manager
application. The published files are copied into a private scratch directory
and the uninstaller is added; pkgbuild then produces a component package and
productbuild wraps it in the final product package under the top-level
artifacts package directory.

Options:
  -c, --configuration <name>   Build configuration: Debug or Release.
                               (default: Release)
  -r, --runtime <rid>          Target runtime identifier: 'osx-x64' or 'osx-arm64'.
                               (default: auto-detected from the host architecture)
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
  $(basename "$0") --configuration Release --runtime osx-arm64 --version 2.6.1
EOF
}

# Package identifiers and install location
COMPONENT_IDENTIFIER="com.microsoft.gitcredentialmanager"
PRODUCT_IDENTIFIER="com.microsoft.gitcredentialmanager.dist"
INSTALL_LOCATION="/usr/local/share/gcm-core"

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

# Installer package assets (distribution definitions, resources, scripts, the
# uninstaller) live in the sibling installer/ directory.
INSTALLER_SRC="$THISDIR/installer"
RESXPATH="$INSTALLER_SRC/resources"

# Source of the published binaries to package (override with --bindir; defaults
# to the application's default artifacts publish directory, from layout.sh).
if [ -n "$BINDIR" ]; then
    BINDIR="$(make_absolute "$BINDIR")"
else
    BINDIR="$(publish_dir git-credential-manager "$CONFIGURATION" "$RUNTIME")" || exit 1
fi

# Private scratch (the staged payload copy and the intermediate component
# package), kept per runtime under the artifacts bin directory so that builds
# for different runtimes don't collide.
SCRATCH="$(bin_dir Mac.Distribution "$CONFIGURATION" "$RUNTIME")" || exit 1
PAYLOAD="$SCRATCH/payload"
COMPONENTDIR="$SCRATCH/components"
COMPONENTOUT="$COMPONENTDIR/$COMPONENT_IDENTIFIER.component.pkg"

# Destination directory for the package (override with --output; defaults to the
# top-level artifacts package directory). The file name is always defined here.
if [ -n "$OUTPUT" ]; then
    OUTDIR="$(make_absolute "$OUTPUT")"
else
    OUTDIR="$(package_dir "$CONFIGURATION")" || exit 1
fi
DISTOUT="$OUTDIR/gcm-$RUNTIME-$VERSION.pkg"

# Select the distribution definition for the target runtime.
if [ "$RUNTIME" = "osx-x64" ]; then
    DISTPATH="$INSTALLER_SRC/distribution.x64.xml"
else
    DISTPATH="$INSTALLER_SRC/distribution.arm64.xml"
fi

# Pre-execution checks
[ -d "$BINDIR" ] || die "Publish directory '$BINDIR' not found. Did you publish first?"

verbose "configuration: $CONFIGURATION"
verbose "runtime:       $RUNTIME"
verbose "version:       $VERSION"
verbose "bin dir:       $BINDIR"
verbose "scratch:       $SCRATCH"
verbose "distribution:  $DISTPATH"
verbose "output:        $DISTOUT"

# Stage a fresh, private copy of the published application
info "Staging payload from '$BINDIR'..."
rm -rf "$PAYLOAD"
ditto "$BINDIR" "$PAYLOAD" || die "Failed to stage payload"

# Add the uninstaller script
info "Copying uninstall script..."
cp "$INSTALLER_SRC/uninstall.sh" "$PAYLOAD" || die "Failed to copy uninstall script"

# Remove any unwanted .DS_Store files
find "$PAYLOAD" -type f -name '.DS_Store' -delete || die "Failed to remove .DS_Store files"

# Normalise permissions and clear extended attributes
info "Setting file permissions..."
/bin/chmod -R 755 "$PAYLOAD" || die "Failed to set payload permissions"
/usr/bin/xattr -rc "$PAYLOAD" || die "Failed to remove extended attributes"

# Build the intermediate component package
info "Building app component package..."
rm -f "$COMPONENTOUT"
mkdir -p "$COMPONENTDIR"
/usr/bin/pkgbuild \
    --root "$PAYLOAD/" \
    --install-location "$INSTALL_LOCATION" \
    --scripts "$INSTALLER_SRC/scripts" \
    --identifier "$COMPONENT_IDENTIFIER" \
    --version "$VERSION" \
    "$COMPONENTOUT" || die "Failed to build app component package"

# Build the final product (distribution) package
info "Building product package..."
rm -f "$DISTOUT"
mkdir -p "$(dirname "$DISTOUT")"
/usr/bin/productbuild \
    --package-path "$COMPONENTDIR" \
    --resources "$RESXPATH" \
    --distribution "$DISTPATH" \
    --identifier "$PRODUCT_IDENTIFIER" \
    --version "$VERSION" \
    "$DISTOUT" || die "Failed to build product package"

info "Packaging complete: $DISTOUT"
