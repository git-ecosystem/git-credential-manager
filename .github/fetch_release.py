from os import environ as env
import requests
import time

if 'RELEASE' not in env or not env['RELEASE']:
    print("Require env var RELEASE is missing!")
    exit(1)

release = env['RELEASE']

release_arg = "latest" if release == "latest" else f"tags/{release}"
release_url = f"https://api.github.com/repos/microsoft/git-credential-manager-core/releases/{release_arg}"

try:
    r = requests.get(release_url)

    # if at first you don't succeed...
    if r.status_code != 200:
        time.sleep(2)
        r = requests.get(release_url)

    if r.status_code != 200:
        raise Error(f"Failed to fetch release info from {release_url}")

    asset = [a for a in r.json()['assets'] if a['name'].endswith(".deb")][0]
    asset_url = asset['browser_download_url']
    asset_name = asset['name']

    print(f"Found asset {asset_name}")
    print(f"Writing asset URL: {asset_url} to asset_url.txt")
    
    with open('asset_url.txt', 'w') as f:
        f.write(asset_url)

except RuntimeError as ex:
    print("Oh dear...")
    print(ex)
    exit(1)
