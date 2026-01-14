#!/bin/bash

SIGN_DIR=$1
DEVELOPER_ID=$2
ENTITLEMENTS_FILE=$3

if [ -z "$SIGN_DIR" ]; then
    echo "error: missing directory argument"
    exit 1
elif [ -z "$DEVELOPER_ID" ]; then
    echo "error: missing developer id argument"
    exit 1
elif [ -z "$ENTITLEMENTS_FILE" ]; then
    echo "error: missing entitlements file argument"
    exit 1
fi

# The codesign command needs the entitlements file to be given as an absolute
# file path; relative paths can cause issues.
if [[ "${ENTITLEMENTS_FILE}" != /* ]]; then
  echo "error: entitlements file argument must be an absolute path"
  exit 1
fi

echo "======== INPUTS ========"
echo "Directory: $SIGN_DIR"
echo "Developer ID: $DEVELOPER_ID"
echo "Entitlements: $ENTITLEMENTS_FILE"
echo "======== END INPUTS ========"
echo
echo "======== ENTITLEMENTS ========"
cat "$ENTITLEMENTS_FILE"
echo "======== END ENTITLEMENTS ========"
echo

cd "$SIGN_DIR" || exit 1
for f in *
do
    macho=$(file --mime "$f" | grep mach)
    # Runtime sign dylibs and Mach-O binaries
    if [[ $f == *.dylib ]] || [ -n "$macho" ];
    then
        echo "Signing with entitlements and hardening: $f"
        codesign -s "$DEVELOPER_ID" "$f" --timestamp --force --options=runtime --entitlements "$ENTITLEMENTS_FILE"
    elif [ -d "$f" ];
    then
        echo "Signing files in subdirectory: $f"
        (
            cd "$f" || exit 1
            for i in *
            do
                codesign -s "$DEVELOPER_ID" "$i" --timestamp --force
            done
        )
    else
        echo "Signing: $f"
        codesign -s "$DEVELOPER_ID" "$f" --timestamp --force
    fi
done
