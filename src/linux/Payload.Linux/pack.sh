#!/bin/bash
die () {
    echo "$*" >&2
    exit 1
}

# Directories
THISDIR="$( cd "$(dirname "$0")" ; pwd -P )"
ROOT="$( cd "$THISDIR"/../../.. ; pwd -P )"
SRC="$ROOT/src"
OUT="$ROOT/out"

# Parse script arguments
for i in "$@"
do
case "$i" in
    --payload=*)
    PAYLOAD="${i#*=}"
    shift # past argument=value
    ;;
    --output=*)
    TAROUT="${i#*=}"
    shift # past argument=value
    ;;
    *)
          # unknown option
    ;;
esac
done

# Perform pre-execution checks
if [ -z "$PAYLOAD" ]; then
    die "--payload was not set"
elif [ ! -d "$PAYLOAD" ]; then
    die "Could not find '$PAYLOAD'. Did you run layout.sh first?"
fi
if [ -z "$TAROUT" ]; then
    die "--output was not set"
fi

# Cleanup any old archive files
if [ -e "$TAROUT" ]; then
    echo "Deleteing old archive '$TAROUT'..."
    rm "$TAROUT"
fi

# Ensure the parent directory for the archive exists
mkdir -p "$(dirname "$TAROUT")"

# Set full read, write, execute permissions for owner and just read and execute permissions for group and other
echo "Setting file permissions..."
/bin/chmod -R 755 "$PAYLOAD" || exit 1

# Build tarball
echo "Building archive..."
cd "$PAYLOAD"
tar -czvf "$TAROUT" * || exit 1

echo "Pack complete."
