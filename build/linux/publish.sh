#!/bin/bash
#
# Publishes the Git Credential Manager application for the Linux package.
#
set -euo pipefail

THISDIR="$( cd "$(dirname "$0")" ; pwd -P )"
. "$THISDIR/../lib-cli.sh"

print_usage () {
    cat <<EOF
Usage: $(basename "$0") [options]

Publish the git-credential-manager application, ready for packaging. Debug
symbols (.pdb files and .dbg files) are moved out of the published output
into a sibling symbol directory.

Options:
  -c, --configuration <name>   Build configuration to publish: Debug or Release.
                               (default: Release)
  -r, --runtime <rid>          Target runtime identifier: 'linux-x64', 'linux-arm64'
                               or 'linux-arm'. (default: auto-detected from the host)
  -o, --output <dir>           Directory to publish the application to.
                               (default: the project's default artifacts dir)
  --symbol-output <dir>        Directory to move debug symbols (.pdb/.dbg) into.
                               (default: <output>.sym)
  --version <version>          Version to stamp into the published binaries.
                               (default: the repository VERSION file)
  --aot / --no-aot             Publish a native ahead-of-time (AOT) build, or a
                               trimmed, self-contained non-AOT build instead.
                               (default: --aot)
  -v, --verbose                Enable verbose output. (default: off)
  -h, --help                   Show this help text and exit.

Examples:
  $(basename "$0")
  $(basename "$0") --configuration Release --runtime linux-x64
EOF
}

# Defaults
CONFIGURATION=""
RUNTIME=""
OUTPUT=""
SYMBOL_OUTPUT=""
VERSION=""
AOT="true"

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
        -o|--output)        require_value "$@"; OUTPUT="$2"; shift 2 ;;
        --symbol-output)    require_value "$@"; SYMBOL_OUTPUT="$2"; shift 2 ;;
        --version)          require_value "$@"; VERSION="$2"; shift 2 ;;
        --aot|--no-aot)     AOT="$(bool_flag "$1")"; shift ;;
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

# Directories
GCM_SRC="$(repo_root)/src/git-credential-manager"

# Resolve the publish output directory (default: the project's artifacts publish
# directory) and the sibling directory that debug symbols are separated into.
if [ -n "$OUTPUT" ]; then
    OUTDIR="$(make_absolute "$OUTPUT")"
else
    OUTDIR="$(publish_dir git-credential-manager "$CONFIGURATION" "$RUNTIME")" || exit 1
fi
if [ -n "$SYMBOL_OUTPUT" ]; then
    SYMOUTDIR="$(make_absolute "$SYMBOL_OUTPUT")"
else
    SYMOUTDIR="$OUTDIR.sym"
fi
[ "$SYMOUTDIR" != "$OUTDIR" ] || die "--symbol-output must differ from the publish output directory"

verbose "configuration: $CONFIGURATION"
verbose "runtime:       $RUNTIME"
verbose "version:       $VERSION"
verbose "aot:           $AOT"
verbose "output dir:    $OUTDIR"
verbose "symbol dir:    $SYMOUTDIR"

# Publish the application to the resolved output directory.
info "Publishing application..."
dotnet publish "$GCM_SRC" \
    -v:normal \
    --configuration="$CONFIGURATION" \
    --runtime="$RUNTIME" \
    --output "$OUTDIR" \
    -p:PublishAot=$AOT \
    -p:VersionOverride="$VERSION" \
    || die "Failed to publish application"

# Separate debug symbols (managed .pdb files and native .dbg files) out of the
# shipping payload into the sibling symbol directory, so the published output
# holds only the files we ship.
info "Separating debug symbols into '$SYMOUTDIR'..."
rm -rf "$SYMOUTDIR"
mkdir -p "$SYMOUTDIR"
find "$OUTDIR" -maxdepth 1 -name '*.pdb' -exec mv {} "$SYMOUTDIR/" \; || die "Failed to move .pdb symbols"
find "$OUTDIR" -maxdepth 1 -name '*.dbg' -exec mv {} "$SYMOUTDIR/" \; || die "Failed to move .dbg symbols"

info "Publish complete."
