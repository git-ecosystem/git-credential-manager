#!/bin/bash
#
# Publishes the Git Credential Manager application as framework-dependent IL for
# the .NET tool package.
#
set -euo pipefail

THISDIR="$( cd "$(dirname "$0")" ; pwd -P )"
. "$THISDIR/../lib-cli.sh"

print_usage () {
    cat <<EOF
Usage: $(basename "$0") [options]

Publish git-credential-manager as portable, framework-dependent IL, ready to be
packed into the .NET tool NuGet package. Unlike the platform publish scripts this
produces no native apphost and is not runtime-specific, ahead-of-time compiled,
trimmed, or self-contained - the tool runs via 'dotnet'. The bulky native debug
symbols (SkiaSharp/HarfBuzzSharp .pdb under runtimes/) are removed afterwards;
the small managed .pdb files are kept so installed-tool stack traces stay useful.

Options:
  -c, --configuration <name>   Build configuration to publish: Debug or Release.
                               (default: Release)
  -o, --output <dir>           Directory to publish the application to.
                               (default: the tool's default artifacts dir)
  --version <version>          Version to stamp into the published binaries.
                               (default: the repository VERSION file)
  -v, --verbose                Enable verbose output. (default: off)
  -h, --help                   Show this help text and exit.

Examples:
  $(basename "$0")
  $(basename "$0") --configuration Release --version 2.6.1
EOF
}

# Defaults
CONFIGURATION=""
OUTPUT=""
VERSION=""

# Parse arguments.
while [ "$#" -gt 0 ]; do
    case "$1" in
        -h|--help)          print_usage; exit 0 ;;
        -v|--verbose)       enable_verbose; shift ;;
        -c|--configuration) require_value "$@"; CONFIGURATION="$2"; shift 2 ;;
        -o|--output)        require_value "$@"; OUTPUT="$2"; shift 2 ;;
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

# Directories
GCM_SRC="$(repo_root)/src/git-credential-manager"

# Resolve the publish output directory (default: the tool's runtime-agnostic
# artifacts publish directory).
if [ -n "$OUTPUT" ]; then
    OUTDIR="$(make_absolute "$OUTPUT")"
else
    OUTDIR="$(publish_dir dntool "$CONFIGURATION")" || exit 1
fi

verbose "configuration: $CONFIGURATION"
verbose "version:       $VERSION"
verbose "output dir:    $OUTDIR"

# Publish the application as framework-dependent IL:
#   --self-contained false   run on the user's installed .NET runtime
#   -p:PublishAot=false      override the product default (a tool cannot be AOT)
#   -p:PublishTrimmed=false  override the non-AOT default (cannot trim a
#                            framework-dependent app)
#   -p:UseAppHost=false      no native launcher; the tool is invoked via 'dotnet'
# No runtime identifier is specified, so the output is portable (tools/<tfm>/any).
info "Publishing .NET tool application..."
dotnet publish "$GCM_SRC" \
    --configuration="$CONFIGURATION" \
    --framework net10.0 \
    --output "$OUTDIR" \
    --self-contained false \
    -p:PublishAot=false \
    -p:PublishTrimmed=false \
    -p:UseAppHost=false \
    -p:VersionOverride="$VERSION" || die "Failed to publish application"

# Drop the native debug symbols (hundreds of MB of SkiaSharp/HarfBuzzSharp
# Windows .pdb files under runtimes/) that a portable publish drags in for every
# RID. They are useless in a .NET tool package and dwarf the actual payload; the
# managed .pdb files at the root are kept so installed-tool stack traces remain
# symbolicated. pack.sh does not re-publish, so this removal sticks.
info "Removing native debug symbols..."
find "$OUTDIR/runtimes" -name '*.pdb' -type f -delete 2>/dev/null || true

info "Publish complete."
