#!/usr/bin/env zsh

# install-gcm-mac (macOS): Download a Git installer from https://github.com/microsoft/git/releases
#
# macOS-only: This script targets mac (.pkg) assets only, and does not support other platforms.

# Shell safety for zsh
set -e
set -u
set -o pipefail

# --- defaults ---
DO_INSTALL=true		# install by default
LIST_ONLY=false
OUT=""				# empty -> ~/Downloads/<asset_name>
QUIET=false
REPO="git-ecosystem/git-credential-manager"
VERSION="latest"

# --- tool paths (use absolute paths) ---
AWK="/usr/bin/awk"
CURL="/usr/bin/curl"
GREP="/usr/bin/grep"
HEAD="/usr/bin/head"
INSTALLER="/usr/sbin/installer"
JQ="/usr/bin/jq"
MKDIR="/bin/mkdir"
RM="/bin/rm"
RMDIR="/bin/rmdir"
SUDO="/usr/bin/sudo"
UNAME="/usr/bin/uname"

err() { printf "[error] %s\n" "$*" 1>&2; }
die() { err "$*"; exit 1; }
print_q() { $QUIET || printf "%s\n" "$*"; }

# Extract asset name and URL pairs as tab-separated values from provided JSON.
# Arg1: JSON string
# Arg2: with-sources | no-sources (include tarball/zip pseudo-assets)
extract_assets_from() {
	local _json="$1" _with_src="${2:-with-sources}"
	printf "%s" "$_json" | "$JQ" -r '.assets[] | [.name, .browser_download_url] | @tsv'
	if [[ "$_with_src" == "with-sources" ]]; then
		local tb zb
		tb=$(printf "%s" "$_json" | "$JQ" -r '.tarball_url // empty')
		zb=$(printf "%s" "$_json" | "$JQ" -r '.zipball_url // empty')
		[[ -n "$tb" ]] && printf "source.tar.gz\t%s\n" "$tb"
		[[ -n "$zb" ]] && printf "source.zip\t%s\n" "$zb"
	fi
}

# Download URL to path with error handling
download_to() {
	local url="$1" out_path="$2"
	# Remove any existing file first to avoid partial overwrite issues
	if [[ -e "$out_path" ]]; then
		"$RM" -f "$out_path" || die "Failed to remove existing output: $out_path"
	fi
	if ! "$CURL" -fL --progress-bar -o "$out_path" "$url"; then
		# Clean up any partial file left by curl
		[[ -e "$out_path" ]] && "$RM" -f "$out_path" || true
		die "Download failed."
	fi
}

# Install a .pkg with a custom label and cleanup
install_pkg_with_label() {
	local pkg_path="$1" label="${2:-package}"

	print_q "Installing ${label} (requires sudo)..."
	$SUDO "$INSTALLER" -pkg "$pkg_path" -target / || die "Installer failed."
	print_q "Install complete.\n"
	if [[ -f "$pkg_path" ]]; then
		$RM -f "$pkg_path"
		print_q "Removed installer: %s\n" "$pkg_path"
	fi
}


# --- version helpers ---
# Extract a core version from any string.
# Prefers X.Y.Z; if not present, returns first X.Y as-is (no normalization).
# Examples:
#  - v2.44.0.vfs.0.0 -> 2.44.0
#  - git version 2.39.3 (Apple Git-146) -> 2.39.3
#  - git version 2.50 (Apple ...) -> 2.50
core_ver() {
	local s="$1" v=""
	# Prefer first occurrence of X.Y.Z
	v=$(printf "%s" "$s" | "$AWK" 'match($0, /[0-9]+(\.[0-9]+){2}/){print substr($0,RSTART,RLENGTH); exit}')
	if [[ -z "$v" ]]; then
		# Fallback to X.Y and return as-is
		v=$(printf "%s" "$s" | "$AWK" 'match($0, /[0-9]+(\.[0-9]+){1}/){print substr($0,RSTART,RLENGTH); exit}')
	fi
	[[ -n "$v" ]] || v="0.0.0"
	printf "%s" "$v"
}

# Compare two versions A and B (both 'X.Y.Z')
# echo -1 if A<B, 0 if A==B, 1 if A>B
vercmp() {
	local a b IFS=.
	local -a A B
	A=(${(s:.:)1})
	B=(${(s:.:)2})
	while (( ${#A[@]} < 3 )); do A+=(0); done
	while (( ${#B[@]} < 3 )); do B+=(0); done
	for i in 1 2 3; do
		if (( ${A[$i]:-0} < ${B[$i]:-0} )); then echo -1; return; fi
		if (( ${A[$i]:-0} > ${B[$i]:-0} )); then echo 1; return; fi
	done
	echo 0
}

print_ver_status() {
	# $1 = tool name, $2 = installed version (or empty), $3 = latest tag
	local tool="$1" inst="$2" tag="$3"
	local latest core_inst core_latest cmp
	latest=$(core_ver "$tag")
	if [[ -n "$inst" ]]; then
		core_inst=$(core_ver "$inst")
		cmp=$(vercmp "$core_inst" "$latest")
		case "$cmp" in
			-1) printf "$tool: installed is $core_inst, latest is $latest -> update available\n" ;;
			 0) printf "$tool: installed is $core_inst, latest is $latest -> up-to-date\n" ;;
			 1) printf "$tool: installed is $core_inst, latest is $latest -> newer than release\n" ;;
		esac
	else
		printf "$tool: not installed, latest $latest available\n"
	fi
}

usage() {
	cat <<END_OF_USAGE
Usage: $1 [--version <tag>|latest] [--output <output_path|dir>] [--list] [--no-install] [--quiet]
Examples:
  $1                				Download and install the latest package (prompts for sudo)
  $1 --version v2.50.1.vfs.0.2		Download and install a specific package
  $1 --list							Show the information for the specified (or latest) package but do not download or install
  $1 --no-install					Show the latest package but do not install 
Options:
  --version <tag>|latest    		Release tag or 'latest' for microsoft/git (default: latest)
  --output <path|dir>           	Output file or directory for downloads (default: ~/Downloads/<asset>)
  --list                        	List matching assets and version status only (no downloads or installs)
  --no-install                  	Download only; do not install. Prints installed vs release versions when applicable
  --quiet                      		Reduce output verbosity
  -h, --help                    	Show this help and exit
Notes:
  - Default download location when --output is omitted: ~/Downloads
  - By default this downloads and installs Git (use --no-install to download only).
END_OF_USAGE
}

# --- arg parsing ---
while [[ $# -gt 0 ]]; do
  case "$1" in
    --output)
      OUT=${2:?}; shift 2 ;;
    --list)
      LIST_ONLY=true; shift ;;
    --no-install)
      DO_INSTALL=false; shift ;;
    --version)
      VERSION=${2:-latest}; shift 2 ;;
    --quiet)
      QUIET=true; shift ;;
    -h|--help)
      usage $(basename "$0"); exit 0 ;;
    *)
      err "Unknown argument: $1"; usage; exit 2 ;;
  esac
done

local api_base="https://api.github.com/repos/${REPO}/releases"
local api_url
if [[ "$VERSION" == "latest" ]]; then
	api_url="${api_base}/latest"
else
	api_url="${api_base}/tags/${VERSION}"
fi

local headers=(
	-H 'Accept: application/vnd.github+json'
	-H 'X-GitHub-Api-Version: 2022-11-28'
)

print_q "Fetching release metadata from ${api_url} …"
if ! json=$("$CURL" -fsSL "${headers[@]}" "$api_url"); then
	die "Failed to fetch release metadata."
fi

# Determine latest GCM tag from JSON
local latest_gcm_tag=$(printf "%s" "$json" | "$JQ" -r '.tag_name // empty')

# When not installing, report version status
local installed_gcm_ver=""
if command -v git-credential-manager >/dev/null 2>&1; then
	installed_gcm_ver=$(git-credential-manager --version 2>/dev/null || true)
elif command -v gcm >/dev/null 2>&1; then
	installed_gcm_ver=$(gcm --version 2>/dev/null || true)
fi

if [[ "$LIST_ONLY" == true || "$DO_INSTALL" == false ]]; then
	if [[ -n "$latest_gcm_tag" ]]; then
		print_ver_status "Git Credential Manager" "$installed_gcm_ver" "$latest_gcm_tag"
	fi
fi

# get the list of available versions and their URLs from JSON
gcm_assets=$(extract_assets_from "$json" no-sources)
[[ -n "$gcm_assets" ]] || die "No GCM assets found for '${VERSION}'."

# Keep only assets that end in ".pkg"; others are not Mac assets
local gcm_pat
arch=$("$UNAME" -m)
if [[ "$arch" == "arm64" ]]; then
	gcm_pat='gcm-osx-arm64.*\.pkg$'
else
	gcm_pat='gcm-osx-x64.*\.pkg$'
fi
gcm_matches=$(printf "%s\n" "$gcm_assets" | "$GREP" -E -i -- "$gcm_pat" || true)

# Report what we found if asked
if $LIST_ONLY; then
	if [[ -z "$gcm_matches" ]]; then
		print_q "GCM: no assets matched regex: $gcm_pat"
		printf "%s\n" "$gcm_assets" | "$AWK" -F '\t' '{printf "  - %s\n", $1}'
	else
		print_q "GCM assets (filtered by '$gcm_pat') for ${REPO}@${VERSION}:"
		printf "%s\n" "$gcm_matches" | "$AWK" -F '\t' '{printf "  - %s\n    %s\n", $1, $2}'
	fi
	exit 0
fi

# Pick the most suitable match
gcm_match=$(printf "%s\n" "$gcm_assets" | "$GREP" -E -i -- "$gcm_pat" | "$HEAD" -n1 || true)
if [[ -z "$gcm_match" ]]; then
	# Fallback to any mac pkg if arch-specific not found
	gcm_match=$(printf "%s\n" "$gcm_assets" | "$GREP" -E -i -- 'gcm-osx-.*\.pkg$' | "$HEAD" -n1 || true)
fi
[[ -n "$gcm_match" ]] || die "No suitable GCM .pkg asset found."
local gcm_name=$(printf "%s" "$gcm_match" | "$AWK" -F '\t' '{print $1}')
local gcm_url=$(printf "%s" "$gcm_match" | "$AWK" -F '\t' '{print $2}')

print_q "Selected asset: $gcm_name"
print_q "Download URL: $gcm_url"

# Resolve output path
local out_dir
local out_path
if [[ -z "$OUT" ]]; then
	out_dir="${HOME}/Downloads"
	"$MKDIR" -p "$out_dir"
	out_path="${out_dir%/}/${gcm_name}"
else
	if [[ -d "$OUT" ]]; then
		out_path="${OUT%/}/${gcm_name}"
	else
		if [[ "$OUT" == */ ]]; then
			"$MKDIR" -p "$OUT"
			out_path="${OUT%/}/${gcm_name}"
		else
			out_dir=${OUT:h}
			[[ -z "$out_dir" || -d "$out_dir" ]] || "$MKDIR" -p "$out_dir"
			out_path="$OUT"
		fi
	fi
fi

# Download the package unless we're showing information only
print_q "Downloading to: $out_path"
download_to "$gcm_url" "$out_path"
print_q "Download complete."

# Do the install if asked
if $DO_INSTALL; then
	install_pkg_with_label "$out_path" "Git Credential Manager"
fi
