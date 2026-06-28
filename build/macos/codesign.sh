#!/bin/bash
#
# Code-sign Git Credential Manager for macOS. Currently supports developer
# signing (attaching entitlements and enabling the hardened runtime).
#
set -euo pipefail

THISDIR="$( cd "$(dirname "$0")" ; pwd -P )"
. "$THISDIR/../lib-cli.sh"

print_usage () {
    cat <<EOF
Usage: $(basename "$0") <command> [options]

Code-sign Git Credential Manager for macOS.

Commands:
  developer   Developer-sign the Mach-O files in a directory, attaching an
              entitlements file and enabling the hardened runtime.

Run '$(basename "$0") <command> --help' for command-specific options.
EOF
}

print_developer_usage () {
    cat <<EOF
Usage: $(basename "$0") developer [options]

Developer-sign every Mach-O binary (executables and dylibs) found recursively
under <bindir>, attaching an entitlements file and enabling the hardened
runtime. Files are signed in place.

Options:
  --bindir <dir>          Directory of files to sign (searched recursively). (required)
  --identity <id>         Signing identity, e.g. a Developer ID. (required)
  -v, --verbose           Enable verbose output. (default: off)
  -h, --help              Show this help text and exit.
EOF
}

# log_keychain_info
# Log keychain state for diagnostics.
log_keychain_info () {
    info "keychain diagnostics:"
    security list-keychains -d user 2>&1 | sed 's/^/    search:   /' || true
    local default_kc
    default_kc="$(security default-keychain -d user 2>/dev/null | sed -E 's/^[[:space:]]*"?//; s/"?[[:space:]]*$//')"
    info "    default:  ${default_kc:-(none)}"
    if [ -n "$default_kc" ]; then
        security show-keychain-info "$default_kc" 2>&1 | sed 's/^/    settings: /' || true
    fi
    security find-identity -v -p codesigning 2>&1 | sed 's/^/    identity: /' || true
}

# sign_developer <bindir> <identity>
# Sign every Mach-O file under <bindir> in place.
sign_developer () {
    local bindir="$1" identity="$2"
    local entitlements="$THISDIR/entitlements.xml"
    local options="runtime"

    # Check for the entitlements file.
    [ -f "$entitlements" ] || die "developer: entitlements file '$entitlements' not found"

    info "Developer-signing Mach-O files under '$bindir'"
    info "  identity:     $identity"
    info "  entitlements: $entitlements"
    info "  options:      ${options:-(none)}"

    log_keychain_info

    local count=0 f start dur
    while IFS= read -r -d '' f; do
        # Only Mach-O binaries (executables and dylibs) need code-signing.
        file --mime "$f" 2>/dev/null | grep -q 'x-mach-binary' || continue
        info "  signing ${f#"$bindir"/} (started $(date -u '+%H:%M:%SZ'))"
        start=$(date +%s)
        codesign --sign "$identity" --force --timestamp --options $options \
            --entitlements "$entitlements" --verbose=4 "$f" || die "failed to sign '$f'"
        dur=$(( $(date +%s) - start ))
        info "    signed ${f#"$bindir"/} in ${dur}s"
        count=$((count + 1))
    done < <(find "$bindir" -type f -print0)

    if [ "$count" -eq 0 ]; then
        warn "no Mach-O files found under '$bindir'; nothing was signed"
    else
        info "Developer signing complete ($count file(s) signed)"
    fi
}

cmd_developer () {
    local bindir="" entitlements="" identity=""
    while [ "$#" -gt 0 ]; do
        case "$1" in
            -h|--help)      print_developer_usage; exit 0 ;;
            -v|--verbose)   enable_verbose; shift ;;
            --bindir)       require_value "$@"; bindir="$2"; shift 2 ;;
            --identity)     require_value "$@"; identity="$2"; shift 2 ;;
            --)             shift; break ;;
            -*)             die "unknown option '$1' (try '$(basename "$0") developer --help')" ;;
            *)              die "unexpected argument '$1' (try '$(basename "$0") developer --help')" ;;
        esac
    done

    [ -n "$bindir" ]       || die "developer: --bindir was not specified"
    [ -n "$identity" ]     || die "developer: --identity was not specified"
    [ -d "$bindir" ]       || die "developer: directory '$bindir' not found"

    # codesign needs the entitlements as an absolute path.
    sign_developer "$bindir" "$identity"
}

# Dispatch on the command.
[ "$#" -gt 0 ] || { print_usage; exit 1; }
COMMAND="$1"; shift
case "$COMMAND" in
    developer)  cmd_developer "$@" ;;
    -h|--help)  print_usage; exit 0 ;;
    *)          die "unknown command '$COMMAND' (try '$(basename "$0") --help')" ;;
esac
