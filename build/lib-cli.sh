#!/bin/bash
#
# Shared helper functions for the platform build scripts (build/<os>/<package>).
#
# This file is a function library; source it (don't execute it) from a build
# script two levels below build/, e.g.:
#
#     THISDIR="$( cd "$(dirname "$0")" ; pwd -P )"
#     . "$THISDIR/../../lib-cli.sh"
#

# Guard against accidental direct execution.
if [ "${BASH_SOURCE[0]}" = "$0" ]; then
    echo "error: lib-cli.sh is a function library and must be sourced, not executed" >&2
    exit 1
fi

# ---------------------------------------------------------------------------
# Logging
# ---------------------------------------------------------------------------

# info <message>...  Write an informational message to stdout.
info () {
    echo "$*"
}

# verbose <message>...  Write an informational message to stdout, but only when
# verbose output is enabled: the GCM_BUILD_VERBOSE environment variable is set to
# a non-empty value. Scripts enable this by parsing a -v/--verbose flag and
# calling enable_verbose, which child scripts then inherit.
verbose () {
    [ -n "${GCM_BUILD_VERBOSE:-}" ] || return 0
    echo "$*"
}

# enable_verbose
# Turn on verbose output by setting and exporting GCM_BUILD_VERBOSE, so that
# verbose() prints and any child scripts inherit the setting. This is the single
# place that knows the variable name. Call it directly (e.g. from a -v/--verbose
# argument handler), not inside a $(...) command substitution or pipeline, where
# the export would be confined to a subshell and lost.
enable_verbose () {
    export GCM_BUILD_VERBOSE=1
}

# warn <message>...  Write a warning message to stderr.
warn () {
    echo "warning: $*" >&2
}

# error <message>... Write an error message to stderr.
error () {
    echo "error: $*" >&2
}

# die <message>...   Write an error message to stderr and exit with status 1.
die () {
    echo "fatal: $*" >&2
    exit 1
}

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------

# make_absolute <path>
# Echo an absolute form of <path>: an already-absolute path is echoed
# unchanged, a relative path is resolved against the current directory. The
# path is not required to exist.
make_absolute () {
    case "$1" in
        /*) echo "$1" ;;
        *)  echo "$PWD/$1" ;;
    esac
}

# repo_root
# Echo the absolute, symlink-resolved path of the repository root: the directory
# one level above this library (build/..). It resolves relative to the library's
# own location, so it returns the same path regardless of the caller's working
# directory or which script sourced it.
repo_root () {
    ( cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd -P )
}

# ---------------------------------------------------------------------------
# Artifacts layout
# ---------------------------------------------------------------------------

# _artifacts_dir <kind> <project> <config> [runtime]
# Shared implementation for bin_dir/publish_dir. Echo the artifacts directory
# <root>/out/<kind>/<project>/<pivot>, where <pivot> is <config>, or
# <config>_<runtime> when a runtime is given (the repository root is located
# relative to this library). Returns 1 without output if <project> or <config>
# is missing.
_artifacts_dir () {
    local kind="$1" project="${2:-}" config="${3:-}" runtime="${4:-}"
    if [ -z "$project" ] || [ -z "$config" ]; then
        return 1
    fi
    local pivot
    if [ -n "$runtime" ]; then
        pivot="${config}_${runtime}"
    else
        pivot="$config"
    fi
    echo "$(repo_root)/out/$kind/$project/$pivot"
}

# bin_dir <project> <config> [runtime]
# Echo the artifacts build-output directory for project <project>:
# out/bin/<project>/<config>[_<runtime>], mirroring where 'dotnet build' writes
# under the repository artifacts output. The directory is not required to exist.
# Prints an error and returns 1 if <project> or <config> is missing, so call it
# as:
#
#     dir="$(bin_dir "$PROJECT_NAME" "$CONFIGURATION" "$RUNTIME")" || exit 1
bin_dir () {
    _artifacts_dir bin "$@" || { error "bin_dir: usage: bin_dir <project> <config> [runtime]"; return 1; }
}

# publish_dir <project> <config> [runtime]
# Echo the artifacts publish directory for project <project>:
# out/publish/<project>/<config>[_<runtime>], mirroring where 'dotnet publish'
# writes under the repository artifacts output. The directory is not required to
# exist. Prints an error and returns 1 if <project> or <config> is missing, so
# call it as:
#
#     dir="$(publish_dir "$PROJECT_NAME" "$CONFIGURATION" "$RUNTIME")" || exit 1
publish_dir () {
    _artifacts_dir publish "$@" || { error "publish_dir: usage: publish_dir <project> <config> [runtime]"; return 1; }
}

# package_dir <config>
# Echo the artifacts package directory for build configuration <config>:
# out/package/<config> (the repository root is located relative to this library).
# Unlike bin_dir/publish_dir this is neither per-project nor per-runtime: it is
# the shared directory that final packages and archives are written to, with the
# runtime encoded in each file name. The directory is not required to exist.
# Prints an error and returns 1 if <config> is missing, so call it as:
#
#     dir="$(package_dir "$CONFIGURATION")" || exit 1
package_dir () {
    local config="${1:-}"
    if [ -z "$config" ]; then
        error "package_dir: usage: package_dir <config>"
        return 1
    fi
    echo "$(repo_root)/out/package/$config"
}

# ---------------------------------------------------------------------------
# Argument parsing
# ---------------------------------------------------------------------------

# require_value <flag> [value]...
# Validate that the space-separated option <flag> was given a value; calls die()
# (which exits) if not. Pass the remaining argument list so the value can be
# inspected, e.g.:
#
#     --version) require_value "$@"; VERSION="$2"; shift 2 ;;
#
# Because this exits the calling shell, it must be invoked directly, not inside a
# $(...) command substitution or pipeline where the exit would be swallowed.
require_value () {
    if [ "$#" -lt 2 ]; then
        die "option '$1' requires a value"
    fi
    case "$2" in
        -*) die "option '$1' requires a value" ;;
    esac
}

# bool_flag <--name|--no-name>
# Echo "true" for an affirmative flag (e.g. --aot) or "false" for its negated
# form (--no-aot), so a script can accept a paired boolean option from a single
# branch of its argument loop:
#
#     --aot|--no-aot) AOT="$(bool_flag "$1")"; shift ;;
#
bool_flag () {
    case "$1" in
        --no-*) printf 'false' ;;
        --*)    printf 'true'  ;;
        *)      die "bool_flag: not a --flag/--no-flag option: '$1'" ;;
    esac
}

# ---------------------------------------------------------------------------
# Runtime identifiers
# ---------------------------------------------------------------------------

# detect_runtime
# Echo the default .NET runtime identifier for the current host: the operating
# system (from 'uname -s': Darwin->osx, Linux->linux) joined to the architecture
# (from 'uname -m'), e.g. 'osx-arm64' or 'linux-x64'. Prints an error and returns
# 1 for an unrecognised host, so call it as: RID="$(detect_runtime)" || exit 1
detect_runtime () {
    local os arch
    case "$(uname -s)" in
        Darwin) os="osx" ;;
        Linux)  os="linux" ;;
        *)
            error "unsupported host OS '$(uname -s)'; specify --runtime explicitly"
            return 1
            ;;
    esac
    case "$(uname -m)" in
        x86_64|amd64)  arch="x64" ;;
        arm64|aarch64) arch="arm64" ;;
        armv7l|armv6l) arch="arm" ;;
        *)
            error "unsupported host architecture '$(uname -m)'; specify --runtime explicitly"
            return 1
            ;;
    esac
    echo "$os-$arch"
}

# validate_runtime <runtime> [os]
# Return 0 if <runtime> is a recognised .NET runtime identifier, otherwise print
# an error and return 1. The valid set lives here; passing <os> ('osx' or
# 'linux') restricts the check to that OS's runtimes, e.g.:
#
#     validate_runtime "$RUNTIME"        # any supported runtime
#     validate_runtime "$RUNTIME" osx    # osx-x64 or osx-arm64 only
validate_runtime () {
    local runtime="$1"
    local os="${2:-}"
    local osx_runtimes="osx-x64 osx-arm64"
    local linux_runtimes="linux-x64 linux-arm64 linux-arm"
    local valid

    case "$os" in
        osx)   valid="$osx_runtimes" ;;
        linux) valid="$linux_runtimes" ;;
        "")    valid="$osx_runtimes $linux_runtimes" ;;
        *)     error "unknown os '$os' (expected 'osx' or 'linux')" ; return 1 ;;
    esac

    local r
    for r in $valid; do
        [ "$runtime" = "$r" ] && return 0
    done
    error "unknown runtime '$runtime' (expected one of: $valid)"
    return 1
}

# normalize_runtime [runtime]
# Echo a validated runtime identifier for the current host. With no <runtime>
# (or an empty one), echoes the host's own runtime; otherwise echoes <runtime>
# after validating it against the runtimes valid for the host's OS. Prints an
# error and returns 1 for an unsupported host or an invalid runtime. Call it in
# a command substitution:
#
#     RUNTIME="$(normalize_runtime "$RUNTIME")" || exit 1
normalize_runtime () {
    local runtime="${1:-}"
    local host_rid
    host_rid="$(detect_runtime)" || return 1
    if [ -z "$runtime" ]; then
        echo "$host_rid"
        return 0
    fi
    validate_runtime "$runtime" "${host_rid%%-*}" || return 1
    echo "$runtime"
}

# ---------------------------------------------------------------------------
# Version
# ---------------------------------------------------------------------------

# read_version_file
# Echo the whitespace-stripped contents of the repository VERSION file, which
# lives in the repo root (one directory up from this library). Prints an error
# and returns 1 if the file does not exist.
read_version_file () {
    local file
    file="$(repo_root)/VERSION"
    if [ ! -f "$file" ]; then
        error "version file '$file' not found"
        return 1
    fi
    tr -d '[:space:]' < "$file"
}

# normalize_version [version]
# Echo <version> as a 3-component 'x.y.z' identifier: extra components are
# dropped and missing ones default to 0. When <version> is empty, the version is
# read from the repository VERSION file instead.
normalize_version () {
    local version="${1:-}"
    if [ -z "$version" ]; then
        version="$(read_version_file)" || return 1
    fi
    local major minor patch _rest
    IFS='.' read -r major minor patch _rest <<< "$version"
    echo "${major:-0}.${minor:-0}.${patch:-0}"
}

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------

# normalize_configuration [configuration]
# Echo a validated, lowercased build configuration name: 'debug' or 'release'.
# When <configuration> is empty, 'release' is returned. Prints an error and
# returns 1 for an unrecognised configuration.
normalize_configuration () {
    local config
    config="$(printf '%s' "${1:-}" | tr '[:upper:]' '[:lower:]')"
    case "$config" in
        debug|release) echo "$config" ;;
        "")            echo "release" ;;
        *)             error "unknown configuration '${1:-}' (expected 'Debug' or 'Release')"; return 1 ;;
    esac
}
