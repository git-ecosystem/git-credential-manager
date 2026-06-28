#!/bin/bash
#
# Packs the published Git Credential Manager layout into the .NET tool NuGet
# package (.nupkg) via Dntool.Distribution.csproj.
#
set -euo pipefail

THISDIR="$( cd "$(dirname "$0")" ; pwd -P )"
. "$THISDIR/../lib-cli.sh"

PACKAGE_PROJECT="$THISDIR/Dntool.Distribution.csproj"

print_usage () {
    cat <<EOF
Usage: $(basename "$0") [options]

Pack the published git-credential-manager layout into the .NET tool NuGet
package via Dntool.Distribution.csproj. Packing reads the pre-published layout
as-is (no rebuild), so assemblies code-signed after publishing are packaged
exactly as they are. The project lays the payload out under tools/<tfm>/any
alongside DotnetToolSettings.xml.

Options:
  -c, --configuration <name>   Build configuration: Debug or Release.
                               (default: Release)
  --version <version>          Version to stamp into the package.
                               (default: the repository VERSION file)
  --bindir <dir>               Directory of published binaries to pack.
                               (default: the tool's default publish dir)
  --output <dir>               Directory to write the package to.
                               (default: out/package/<config>)
  -v, --verbose                Enable verbose output. (default: off)
  -h, --help                   Show this help text and exit.

Examples:
  $(basename "$0")
  $(basename "$0") --configuration Release --version 2.6.1
EOF
}

# Defaults
CONFIGURATION=""
VERSION=""
BINDIR=""
OUTPUT=""

# Parse arguments.
while [ "$#" -gt 0 ]; do
    case "$1" in
        -h|--help)          print_usage; exit 0 ;;
        -v|--verbose)       enable_verbose; shift ;;
        -c|--configuration) require_value "$@"; CONFIGURATION="$2"; shift 2 ;;
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
VERSION="$(normalize_version "$VERSION")" || exit 1

# Source of the published binaries to pack (override with --bindir; defaults to
# the tool's default artifacts publish directory, from publish.sh).
if [ -n "$BINDIR" ]; then
    BINDIR="$(make_absolute "$BINDIR")"
else
    BINDIR="$(publish_dir dntool "$CONFIGURATION")" || exit 1
fi

# Destination directory for the package (override with --output; defaults to the
# top-level artifacts package directory).
if [ -n "$OUTPUT" ]; then
    OUTDIR="$(make_absolute "$OUTPUT")"
else
    OUTDIR="$(package_dir "$CONFIGURATION")" || exit 1
fi

# Pre-execution checks
[ -d "$BINDIR" ] || die "Publish directory '$BINDIR' not found. Did you publish first?"

verbose "configuration: $CONFIGURATION"
verbose "version:       $VERSION"
verbose "bin dir:       $BINDIR"
verbose "output dir:    $OUTDIR"

# Pack the pre-published (and, in the release pipeline, code-signed) layout into
# the .NET tool package via Dntool.Distribution.csproj. The project compiles
# nothing, so pack simply zips the published files: it does not rebuild or
# re-publish, leaving the signed assemblies untouched. --no-build is essential -
# it skips the Build target (and thus the project's build.sh hook), so packing
# does not recurse. The version and the absolute publish path are passed as
# MSBuild properties; the _CustomPack target lays it out under tools/<tfm>.
info "Packing .NET tool package..."
mkdir -p "$OUTDIR"

# 'dotnet pack --no-build' below implies --no-restore, so restore the package
# project explicitly first. Running this script via the build.sh script and
# the Dntool project would have already restored it; this is only required when
# running publish.sh and pack.sh as separate steps directly.
# Without this the `dotnet pack` command fails with NETSDK1004 (missing
# project.assets.json). Restore does not run the project's build.sh hook (that
# fires AfterTargets=Build), so packing still does not recurse.
dotnet restore "$PACKAGE_PROJECT" || die "Failed to restore .NET tool project"

dotnet pack "$PACKAGE_PROJECT" \
    --configuration "$CONFIGURATION" \
    --no-build \
    --output "$OUTDIR" \
    -p:PackageVersion="$VERSION" \
    -p:GcmPublishDir="$BINDIR" || die "Failed to pack .NET tool"

info "Packaging complete: $OUTDIR"
