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

echo "======== INPUTS ========"
echo "Directory: $SIGN_DIR"
echo "Developer ID: $DEVELOPER_ID"
echo "Entitlements: $ENTITLEMENTS_FILE"
echo "======== END INPUTS ========"

cd $SIGN_DIR
for f in *
do
    macho=$(file --mime $f | grep mach)
    # Runtime sign dylibs and Mach-O binaries
    if [[ $f == *.dylib ]] || [ ! -z "$macho" ];
    then
        echo "Runtime Signing $f"
        codesign -s "$DEVELOPER_ID" $f --timestamp --force --options=runtime --entitlements $ENTITLEMENTS_FILE
    elif [ -d "$f" ];
    then
        echo "Signing files in subdirectory $f"
        cd $f
        for i in *
        do
            codesign -s "$DEVELOPER_ID" $i --timestamp --force
        done
        cd ..
    else
        echo "Signing $f"
        codesign -s "$DEVELOPER_ID" $f  --timestamp --force
    fi
done
