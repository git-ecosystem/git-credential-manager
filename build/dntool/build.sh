#!/bin/bash
#
# Builds the Git Credential Manager .NET tool package (.nupkg + .snupkg) by
# publishing the framework-dependent IL and then packing it.
#
# In the release pipeline the published assemblies are code-signed between the
# publish and pack steps; for a local (unsigned) build this script runs both in
# sequence.
#
set -euo pipefail

THISDIR="$( cd "$(dirname "$0")" ; pwd -P )"
. "$THISDIR/../lib-cli.sh"

print_usage () {
    cat <<EOF
Usage: $(basename "$0") [options]

Build the .NET tool NuGet package for Git Credential Manager. This runs
publish.sh and pack.sh in sequence, writing the result under the top-level
artifacts package directory. (The release pipeline runs publish.sh, code-signs
the published assemblies, then runs pack.sh.)

Options:
  -c, --configuration <name>   Build configuration to publish: Debug or Release.
                               (default: Release)
  --version <version>          Version to stamp into the binaries and package.
                               (default: the repository VERSION file)
  -v, --verbose                Enable verbose output. (default: off)
  -h, --help                   Show this help text and exit.

Examples:
  $(basename "$0") --version 2.6.1
  $(basename "$0") --configuration Release --version 2.6.1
EOF
}

# Defaults
CONFIGURATION=""
VERSION=""

# Parse arguments.
while [ "$#" -gt 0 ]; do
    case "$1" in
        -h|--help)          print_usage; exit 0 ;;
        -v|--verbose)       enable_verbose; shift ;;
        -c|--configuration) require_value "$@"; CONFIGURATION="$2"; shift 2 ;;
        --version)          require_value "$@"; VERSION="$2"; shift 2 ;;
        --)                 shift; break ;;
        -*)                 die "unknown option '$1' (try '$(basename "$0") --help')" ;;
        *)                  die "unexpected argument '$1' (try '$(basename "$0") --help')" ;;
    esac
done

if [ "$#" -gt 0 ]; then
    die "unexpected argument '$1' (try '$(basename "$0") --help')"
fi

# Normalize arguments / apply defaults.
CONFIGURATION="$(normalize_configuration "$CONFIGURATION")" || exit 1
VERSION="$(normalize_version "$VERSION")" || exit 1

info "Building .NET tool package..."
verbose "configuration: $CONFIGURATION"
verbose "version:       $VERSION"

# Publish the application as framework-dependent IL.
"$THISDIR/publish.sh" --configuration "$CONFIGURATION" --version "$VERSION"

# Pack the published layout into the .nupkg/.snupkg.
"$THISDIR/pack.sh"    --configuration "$CONFIGURATION" --version "$VERSION"

info ".NET tool package build complete."
