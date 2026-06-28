#!/bin/bash
#
# Builds the Linux distributables for Git Credential Manager (the .deb package
# plus binary and symbol tarballs) by running the publish, pack and archive
# steps in sequence.
#
set -euo pipefail

THISDIR="$( cd "$(dirname "$0")" ; pwd -P )"
. "$THISDIR/../lib-cli.sh"

print_usage () {
    cat <<EOF
Usage: $(basename "$0") [options]

Build the Linux package (.deb) for Git Credential Manager, together with binary
and symbol tarballs. This runs publish.sh, pack.sh and archive.sh in sequence,
writing the results under the top-level artifacts package directory.

Options:
  -c, --configuration <name>   Build configuration to publish: Debug or Release.
                               (default: Release)
  --version <version>          Version to stamp into the binaries and package.
                               (default: the repository VERSION file)
  -r, --runtime <rid>          Target runtime identifier: 'linux-x64', 'linux-arm64'
                               or 'linux-arm'. (default: auto-detected from the host)
  -v, --verbose                Enable verbose output. (default: off)
  -h, --help                   Show this help text and exit.

Examples:
  $(basename "$0") --version 2.6.1
  $(basename "$0") --configuration Release --version 2.6.1 --runtime linux-x64
EOF
}

# Defaults
CONFIGURATION=""
VERSION=""
RUNTIME=""

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

info "Building Linux distribution..."
verbose "configuration: $CONFIGURATION"
verbose "runtime:       $RUNTIME"
verbose "version:       $VERSION"

# Publish the application.
"$THISDIR/publish.sh" --configuration "$CONFIGURATION" --runtime "$RUNTIME" --version "$VERSION"

# Build the .deb package.
"$THISDIR/pack.sh"    --configuration "$CONFIGURATION" --runtime "$RUNTIME" --version "$VERSION"

# Create the binaries and symbols tarballs.
"$THISDIR/archive.sh" --configuration "$CONFIGURATION" --runtime "$RUNTIME" --version "$VERSION"

info "Linux distribution build complete."
