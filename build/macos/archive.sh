#!/bin/bash
#
# Archives the published Git Credential Manager application into distributable
# .tar.gz files: a payload tarball of the shipping binaries and a separate
# symbols tarball, written alongside the installer package.
#
set -euo pipefail

THISDIR="$( cd "$(dirname "$0")" ; pwd -P )"
. "$THISDIR/../lib-cli.sh"

print_usage () {
    cat <<EOF
Usage: $(basename "$0") [options]

Create distributable archives of the published git-credential-manager
application: a payload tarball of the shipping binaries and a separate symbols
tarball. Archives are written to the top-level artifacts package directory.

Options:
  -c, --configuration <name>   Build configuration: Debug or Release.
                               (default: Release)
  -r, --runtime <rid>          Target runtime identifier: 'osx-x64' or 'osx-arm64'.
                               (default: auto-detected from the host architecture)
  --version <version>          Version to embed in the archive file names.
                               (default: the repository VERSION file)
  --bindir <dir>               Directory of published binaries to archive.
                               (default: the application's default publish dir)
  --symdir <dir>               Directory of debug symbols to archive.
                               (default: <bindir>.sym)
  --output <dir>               Directory to write the archives to.
                               (default: out/package/<config>)
  -v, --verbose                Enable verbose output. (default: off)
  -h, --help                   Show this help text and exit.

Examples:
  $(basename "$0")
  $(basename "$0") --configuration Release --runtime osx-arm64 --version 2.6.1
EOF
}

# Defaults
CONFIGURATION=""
RUNTIME=""
VERSION=""
BINDIR=""
SYMDIR=""
OUTPUT=""

# Parse arguments.
while [ "$#" -gt 0 ]; do
    case "$1" in
        -h|--help)          print_usage; exit 0 ;;
        -v|--verbose)       enable_verbose; shift ;;
        -c|--configuration) require_value "$@"; CONFIGURATION="$2"; shift 2 ;;
        -r|--runtime)       require_value "$@"; RUNTIME="$2"; shift 2 ;;
        --version)          require_value "$@"; VERSION="$2"; shift 2 ;;
        --bindir)           require_value "$@"; BINDIR="$2"; shift 2 ;;
        --symdir)           require_value "$@"; SYMDIR="$2"; shift 2 ;;
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

# Source of the published binaries to archive (override with --bindir; defaults
# to the application's default artifacts publish directory, from publish.sh).
if [ -n "$BINDIR" ]; then
    BINDIR="$(make_absolute "$BINDIR")"
else
    BINDIR="$(publish_dir git-credential-manager "$CONFIGURATION" "$RUNTIME")" || exit 1
fi

# Source of the debug symbols to archive (override with --symdir; defaults to
# the sibling symbol directory produced by publish.sh).
if [ -n "$SYMDIR" ]; then
    SYMDIR="$(make_absolute "$SYMDIR")"
else
    SYMDIR="$BINDIR.sym"
fi

# Destination directory for the archives (override with --output; defaults to
# the top-level artifacts package directory).
if [ -n "$OUTPUT" ]; then
    OUTDIR="$(make_absolute "$OUTPUT")"
else
    OUTDIR="$(package_dir "$CONFIGURATION")" || exit 1
fi

PAYLOAD_TARBALL="$OUTDIR/gcm-$RUNTIME-$VERSION.tar.gz"
SYMBOLS_TARBALL="$OUTDIR/gcm-$RUNTIME-$VERSION-symbols.tar.gz"

# Pre-execution checks
[ -d "$BINDIR" ] || die "Binaries directory '$BINDIR' not found. Did you publish first?"

mkdir -p "$OUTDIR"

verbose "configuration: $CONFIGURATION"
verbose "runtime:       $RUNTIME"
verbose "version:       $VERSION"
verbose "bin dir:       $BINDIR"
verbose "symbol dir:    $SYMDIR"
verbose "output dir:    $OUTDIR"

# Archive the shipping binaries.
info "Archiving binaries from '$BINDIR'..."
rm -f "$PAYLOAD_TARBALL"
tar -czf "$PAYLOAD_TARBALL" -C "$BINDIR" . || die "Failed to create binaries archive"
info "Created $PAYLOAD_TARBALL"

# Archive the debug symbols, if any were produced.
if [ -d "$SYMDIR" ]; then
    info "Archiving symbols from '$SYMDIR'..."
    rm -f "$SYMBOLS_TARBALL"
    tar -czf "$SYMBOLS_TARBALL" -C "$SYMDIR" . || die "Failed to create symbols archive"
    info "Created $SYMBOLS_TARBALL"
else
    warn "symbols directory '$SYMDIR' not found; skipping symbols archive"
fi

info "Archiving complete."
