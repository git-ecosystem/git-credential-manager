# .github/workflows/deploy-ipn.yml
name: Build & Deploy IPN Handler

on:
  push:
    paths:
      - "src/ipn-handler.ts"
      - "src/mappings/**"
      - "deno.json"

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup Deno
        uses: denoland/setup-deno@v1
        with:
          deno-version: v1.x

      - name: Cache deps
        uses: actions/cache@v3
        with:
          path: ~/.cache/deno
          key: ${{ runner.os }}-deno-${{ hashFiles('**/*.ts') }}

      - name: Lint & Test
        run: |
          deno fmt --check
          deno lint
          deno test --allow-net --allow-env

  deploy:
    needs: build
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup Deno
        uses: denoland/setup-deno@v1
        with:
          deno-version: v1.x

      - name: Deploy to Deno Deploy
        run: deno deploy --project=peludo-ipn --token=${{ secrets.DENO_DEPLOY_TOKEN }} src/ipn-handler.ts