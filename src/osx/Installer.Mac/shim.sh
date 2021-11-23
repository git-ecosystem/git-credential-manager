#!/bin/sh

echo >&2 "warning: git-credential-manager-core was renamed to git-credential-manager"
echo >&2 "warning: please update your credential.helper configuration"

exec $(dirname $0)/git-credential-manager "$@"
