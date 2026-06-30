#!/bin/bash
#
# Builds the macOS distributables for Git Credential Manager (the .pkg installer
# plus payload and symbol tarballs) by running the publish, codesign, pack and
# archive steps in sequence.
#
set -euo pipefail

THISDIR="$( cd "$(dirname "$0")" ; pwd -P )"
. "$THISDIR/../lib-cli.sh"

print_usage () {
    cat <<EOF
Usage: $(basename "$0") [options]

Build the macOS installer package (.pkg) for Git Credential Manager, together
with binary and symbol tarballs. This runs publish.sh, pack.sh and archive.sh
in sequence, writing the results under the top-level artifacts package directory.

Options:
  -c, --configuration <name>   Build configuration to publish: Debug or Release.
                               (default: Release)
  --version <version>          Version to stamp into the binaries and package.
                               (default: the repository VERSION file)
  -r, --runtime <rid>          Target runtime identifier: 'osx-x64' or 'osx-arm64'.
                               (default: auto-detected from the host architecture)
  --dev-identity <id>          Developer signing identity. When set, the published
                               binaries are codesigned (with entitlements and the
                               hardened runtime) before packaging. (default: unsigned)
  -v, --verbose                Enable verbose output. (default: off)
  -h, --help                   Show this help text and exit.

Examples:
  $(basename "$0") --version 2.6.1
  $(basename "$0") --configuration Release --version 2.6.1 --runtime osx-arm64
EOF
}

# Defaults
CONFIGURATION=""
VERSION=""
RUNTIME=""
DEV_IDENTITY=""

# Parse arguments.
while [ "$#" -gt 0 ]; do
    case "$1" in
        -h|--help)
            print_usage
            exit 0
            ;;
        -v|--verbose)       enable_verbose; shift ;;
        -c|--configuration) require_value "$@"; CONFIGURATION="$2"; shift 2 ;;
        --version)          require_value "$@"; VERSION="$2"; shift 2 ;;
        -r|--runtime)       require_value "$@"; RUNTIME="$2"; shift 2 ;;
        --dev-identity)     require_value "$@"; DEV_IDENTITY="$2"; shift 2 ;;
        --)                 shift; break ;;
        -*)                 die "unknown option '$1' (try '$(basename "$0") --help')" ;;
        *)                  die "unexpected argument '$1' (try '$(basename "$0") --help')" ;;
    esac
done

if [ "$#" -gt 0 ]; then
    die "unexpected argument '$1' (try '$(basename "$0") --help')"
fi

# Normalize arguments/use defaults if unset
CONFIGURATION="$(normalize_configuration "$CONFIGURATION")" || exit 1
RUNTIME="$(normalize_runtime "$RUNTIME")" || exit 1
VERSION="$(normalize_version "$VERSION")" || exit 1

info "Building macOS distribution..."
verbose "configuration: $CONFIGURATION"
verbose "runtime:       $RUNTIME"
verbose "version:       $VERSION"

# Publish the application
"$THISDIR/publish.sh" --configuration "$CONFIGURATION" --runtime "$RUNTIME" --version "$VERSION"

# Developer-sign the published Mach-O files (attaching entitlements) before
# packaging, when a developer identity is given.
if [ -n "$DEV_IDENTITY" ]; then
    BINDIR="$(publish_dir git-credential-manager "$CONFIGURATION" "$RUNTIME")" || exit 1
    "$THISDIR/codesign.sh" developer \
        --bindir "$BINDIR" \
        --identity "$DEV_IDENTITY"
fi

# Package the (signed) binaries into the installer (.pkg)
"$THISDIR/pack.sh"    --configuration "$CONFIGURATION" --runtime "$RUNTIME" --version "$VERSION"

# Create the (signed) binaries and symbols tarballs.
"$THISDIR/archive.sh" --configuration "$CONFIGURATION" --runtime "$RUNTIME" --version "$VERSION"

info "macOS distribution build complete."
