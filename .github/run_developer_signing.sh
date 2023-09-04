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
name: "Lint documentation"

on:
  workflow_dispatch:
  push:
    branches: [ main, linux ]
    paths:
      - '**.md'
      - '.github/workflows/lint-docs.yml'
  pull_request:
    branches: [ main, linux ]
    paths:
      - '**.md'
      - '.github/workflows/lint-docs.yml'

jobs:
  lint-markdown:
    name: Lint markdown files
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - uses: DavidAnson/markdownlint-cli2-action@3aaa38e446fbd2c288af4291aa0f55d64651050f
        with:
          globs: |
            "**/*.md"
            "!.github/ISSUE_TEMPLATE"

  check-links:
    name: Check for broken links
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Run link checker
        # For any troubleshooting, see:
        # https://github.com/lycheeverse/lychee/blob/master/docs/TROUBLESHOOTING.md
        uses: lycheeverse/lychee-action@ec3ed119d4f44ad2673a7232460dc7dff59d2421

        with:
          # user-agent: if a user agent is not specified, some websites (e.g.
          # GitHub Docs) return HTTP errors which Lychee will interpret as
          # a broken link.
          # no-progress: do not show progress bar. Recommended for
          # non-interactive shells (e.g. for CI)
          # inputs: by default (.), this action checks files matching the
          # patterns: './**/*.md' './**/*.html'
          args: >-
            --user-agent "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.4896.75 Safari/537.36"
            --no-progress .
          fail: true
        env:
          # A token is used to avoid GitHub rate limiting. A personal token with
          # no extra permissions is enough to be able to check public repos
          # See: https://github.com/lycheeverse/lychee#github-token
          GITHUB_TOKEN: ${{secrets.GITHUB_TOKEN}}
