#!/bin/bash
#
# Downloads the macOS developer signing certificate (and its signing identity)
# from Azure Key Vault and imports the certificate into a keychain so that
# codesign can use it.
#
# This uses the Azure CLI ('az') to read the secrets, so it must run with an
# authenticated CLI - e.g. inside an Azure Pipelines 'AzureCLI@2' task, which
# provides a CLI authenticated against the task's service connection.
#
# The resolved developer signing identity is written to stdout; all progress
# output goes to stderr, so a caller can capture just the identity with:
#
#     identity="$(import-developer-certificate.sh --vault myvault)"
#
set -euo pipefail

die () {
    echo "fatal: $*" >&2
    exit 1
}

make_absolute () {
    case "$1" in
        /*) echo "$1" ;;
        *)  echo "$PWD/$1" ;;
    esac
}

print_usage () {
    cat <<EOF
Usage: $(basename "$0") --vault <name> [options]

Download the macOS developer signing certificate from Azure Key Vault and import
it into a keychain for codesign. Requires an authenticated Azure CLI ('az'). The
developer signing identity is written to stdout.

Options:
  --vault <name>                Azure Key Vault to read the secrets from. (required)
  --certificate-secret <name>   Key Vault secret holding the base64-encoded .p12
                                certificate. (default: mac-developer-certificate)
  --password-secret <name>      Key Vault secret holding the .p12 password.
                                (default: mac-developer-certificate-password)
  --identity-secret <name>      Key Vault secret holding the signing identity.
                                (default: mac-developer-certificate-identity)
  --keychain <path>             Keychain to create and import into.
                                (default: \$TMPDIR/gcm-build.keychain)
  -h, --help                    Show this help text and exit.

Examples:
  $(basename "$0") --vault my-key-vault
EOF
}

# Defaults
VAULT=""
CERT_SECRET="mac-developer-certificate"
PASSWORD_SECRET="mac-developer-certificate-password"
IDENTITY_SECRET="mac-developer-certificate-identity"
KEYCHAIN=""

# Parse arguments.
while [ "$#" -gt 0 ]; do
    case "$1" in
        -h|--help)              print_usage; exit 0 ;;
        --vault)                VAULT="${2:?--vault requires a value}"; shift 2 ;;
        --certificate-secret)   CERT_SECRET="${2:?--certificate-secret requires a value}"; shift 2 ;;
        --password-secret)      PASSWORD_SECRET="${2:?--password-secret requires a value}"; shift 2 ;;
        --identity-secret)      IDENTITY_SECRET="${2:?--identity-secret requires a value}"; shift 2 ;;
        --keychain)             KEYCHAIN="${2:?--keychain requires a value}"; shift 2 ;;
        --)                     shift; break ;;
        -*)                     die "unknown option '$1' (try '$(basename "$0") --help')" ;;
        *)                      die "unexpected argument '$1' (try '$(basename "$0") --help')" ;;
    esac
done

[ "$#" -eq 0 ] || die "unexpected argument '$1' (try '$(basename "$0") --help')"
[ -n "$VAULT" ] || die "--vault was not specified"

# Resolve the keychain path (default: an ephemeral keychain under the temp dir).
if [ -n "$KEYCHAIN" ]; then
    KEYCHAIN="$(make_absolute "$KEYCHAIN")"
else
    KEYCHAIN="${TMPDIR:-/tmp}/gcm-build.keychain"
fi

command -v az >/dev/null 2>&1       || die "the Azure CLI ('az') is required but was not found on PATH"
command -v security >/dev/null 2>&1 || die "'security' is required (this script must run on macOS)"

# Send everything except the final identity to stderr, so stdout carries only
# the signing identity for the caller to capture.
exec 3>&1 1>&2

echo "Importing developer certificate from Key Vault '$VAULT'..."
echo "certificate secret: $CERT_SECRET"
echo "password secret:    $PASSWORD_SECRET"
echo "identity secret:    $IDENTITY_SECRET"
echo "keychain:           $KEYCHAIN"

# Read the certificate password and signing identity (plain string secrets).
CERT_PASSWORD="$(az keyvault secret show --vault-name "$VAULT" --name "$PASSWORD_SECRET" --query value -o tsv)" \
    || die "failed to read secret '$PASSWORD_SECRET' from Key Vault '$VAULT'"
IDENTITY="$(az keyvault secret show --vault-name "$VAULT" --name "$IDENTITY_SECRET" --query value -o tsv)" \
    || die "failed to read secret '$IDENTITY_SECRET' from Key Vault '$VAULT'"

# Download and base64-decode the .p12 certificate to a temporary file, removed
# again as soon as it has been imported (or if the script exits early). mktemp
# creates the file, so the download needs --overwrite to write into it (newer
# az CLI refuses to overwrite an existing --file otherwise).
CERT_FILE="$(mktemp)"
trap 'rm -f "$CERT_FILE"' EXIT
az keyvault secret download --vault-name "$VAULT" --name "$CERT_SECRET" \
    --encoding base64 --file "$CERT_FILE" --overwrite \
    || die "failed to download secret '$CERT_SECRET' from Key Vault '$VAULT'"

# Use a random, ephemeral password for the throwaway build keychain; it is only
# ever used here (the keychain stays unlocked for codesign in later steps).
KEYCHAIN_PASSWORD="$(uuidgen)"

# Create, unlock and default the keychain so codesign can find the identity.
echo "Creating keychain '$KEYCHAIN'..."
rm -f "$KEYCHAIN"
security create-keychain -p "$KEYCHAIN_PASSWORD" "$KEYCHAIN"
security default-keychain -s "$KEYCHAIN"
security unlock-keychain -p "$KEYCHAIN_PASSWORD" "$KEYCHAIN"

# A new keychain keeps a default auto-lock (300s timeout and lock-on-sleep) that
# 'unlock-keychain' does not clear, so it re-locks part-way through a long
# signing run and makes codesign block on a GUI unlock prompt (which hangs
# headless CI). Disable auto-lock so it stays unlocked for the whole build.
security set-keychain-settings "$KEYCHAIN"

# Import the certificate, authorising codesign to use the private key. The
# format is stated explicitly because 'security import' otherwise infers it
# from the file extension, and the mktemp file deliberately has none (without
# this it fails with "SecKeychainItemImport: Unknown format in import").
echo "Importing certificate..."
security import "$CERT_FILE" -f pkcs12 -k "$KEYCHAIN" -P "$CERT_PASSWORD" -T /usr/bin/codesign

# Allow codesign to use the private key without an interactive prompt.
security set-key-partition-list -S apple-tool:,apple:,codesign: -s -k "$KEYCHAIN_PASSWORD" "$KEYCHAIN"

echo "Developer certificate imported."
echo "signing identity: $IDENTITY"

# Emit the signing identity on the real stdout for the caller.
printf '%s\n' "$IDENTITY" >&3
