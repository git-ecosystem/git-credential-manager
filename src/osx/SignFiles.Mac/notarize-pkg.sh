#!/bin/bash

# This file was based on https://github.com/microsoft/BuildXL/blob/8c2348ff04e6ca78726bb945fb2a0f6a55a5c7d6/Private/macOS/notarize.sh
#
# For detailed explanation see: https://developer.apple.com/documentation/security/notarizing_your_app_before_distribution/customizing_the_notarization_workflow

usage() {
    cat <<EOM
$(basename $0) - Handy script to notarize an installer package (.pkg)
Usage: $(basename $0) -id <apple_id> -p <password> -pkg <path_to_pkg>
        -id  or --appleid         # A valid Apple ID email address, account must have correct certificates available
        -p   or --password        # The password for the specified Apple ID or Apple One-Time password (to avoid 2FA)
        -pkg or --package         # The path to an already signed flat-package
EOM
    exit 0
}

declare arg_AppleId=""
declare arg_Password=""
declare arg_PackagePath=""

[ $# -eq 0 ] && { usage; }

function parseArgs() {
    arg_Positional=()
    while [[ $# -gt 0 ]]; do
        cmd="$1"
        case $cmd in
        --help | -h)
            usage
            shift
            exit 0
            ;;
        --appleid | -id)
            arg_AppleId=$2
            shift
            ;;
        --password | -p)
            arg_Password="$2"
            shift
            ;;
        --package | -pkg)
            arg_PackagePath="$2"
            shift
            ;;
        *)
            arg_Positional+=("$1")
            shift
            ;;
        esac
    done
}

function getPackageId {
  local PKG=$(cd "$(dirname "$1")"; pwd)/$(basename "$1")
  local PKGDEST=$(mktemp -d | tr -d '\r')
  xar -x -f "${PKG}" --exclude '^(?:(?!PackageInfo).)*$' -C "${PKGDEST}"
  if [ ! -e "${PKGDEST}/PackageInfo" ]; then
      echo "error: can't find 'PackageInfo'; maybe meta-package"
      return 1
  fi
  cat "${PKGDEST}/PackageInfo" | tr -d '\r' | tr -d '\n' | sed 's:^.*identifier="\([^"]*\)".*$:\1:g'
  rm -rf "${PKGDEST}"
}

parseArgs "$@"

if [[ -z $arg_AppleId ]]; then
    echo "[ERROR] Must supply valid / non-empty Apple ID!"
    exit 1
fi

if [[ -z $arg_Password ]]; then
    echo "[ERROR] Must supply valid / non-empty password!"
    exit 1
fi

if [[ ! -f "$arg_PackagePath" ]]; then
    echo "[ERROR] Must supply valid / non-empty path to package!"
    exit 1
fi

declare bundle_id=$(getPackageId ${arg_PackagePath})

if [[ -z $bundle_id ]]; then
    echo "[ERROR] No identifier found in package info!"
    exit 1
fi

echo "Notarizating $arg_PackagePath"

echo -e "Current state:\n"
xcrun stapler validate -v "$arg_PackagePath"

if [[ $? -eq 0 ]]; then
    echo "$arg_PackagePath already notarized and stapled, nothing to do!"
    exit 0
fi

set -e

declare start_time=$(date +%s)

declare output="/tmp/progress.xml"

echo "Uploading package to notarization service, please wait..."
xcrun altool --notarize-app -t osx -f $arg_PackagePath --primary-bundle-id $bundle_id -u $arg_AppleId -p $arg_Password --output-format xml | tee $output

declare request_id=$(/usr/libexec/PlistBuddy -c "print :notarization-upload:RequestUUID" $output)

echo "Checking notarization request validity..."
if [[ $request_id =~ ^\{?[A-F0-9a-f]{8}-[A-F0-9a-f]{4}-[A-F0-9a-f]{4}-[A-F0-9a-f]{4}-[A-F0-9a-f]{12}\}?$ ]]; then
    declare attempts=5

    while :
    do
        echo "Waiting a bit before checking on notarization status again..."

        sleep 20
        xcrun altool --notarization-info $request_id -u $arg_AppleId -p $arg_Password --output-format xml | tee $output

        declare status=$(/usr/libexec/PlistBuddy -c "print :notarization-info:Status" $output)
        echo "Status: $status"

        if [[ -z $status ]]; then
            echo "Left attempts: $attempts"

            if (($attempts <= 0)); then
                break
            fi

            ((attempts--))
        else
            if [[ $status != "in progress" ]]; then
                break
            fi
        fi
    done

    declare end_time=$(date +%s)
    echo -e "Completed in $(($end_time-$start_time)) seconds\n"

    if [[ "$status" != "success" ]]; then
        echo "Error notarizing, exiting..." >&2
        exit 1
    else
        declare url=$(/usr/libexec/PlistBuddy -c "print :notarization-info:LogFileURL" $output)

        if [ "$url" ]; then
            curl $url
        fi

        # Staple the ticket to the package
        xcrun stapler staple "$arg_PackagePath"

        echo -e "State after notarization:\n"
        xcrun stapler validate -v "$arg_PackagePath"
        echo -e "Stapler exit code: $? (must be zero on success!)\n"
    fi
else
    echo "Invalid request id found in 'altool' output, aborting!" >&2
    exit 1
fi
