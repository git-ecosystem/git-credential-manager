from os import environ as env
import requests
import time
import json

if 'RELEASE' in env and env['RELEASE']:
    release = env['RELEASE'].strip()
else:
    release = "latest"

release_arg = "latest" if release == "latest" else f"tags/{release}"

print(f"release_arg set to {release_arg}")

release_url = f"https://api.github.com/repos/microsoft/git-credential-manager-core/releases/{release_arg}"

try:
    print(f"Fetching release {release_url}")
    r = requests.get(release_url)

    # if at first you don't succeed...
    if r.status_code != 200:
        print(f"Trying again, initial status {r.status_code}")
        time.sleep(2)
        r = requests.get(release_url)

    print("Response:")
    print(json.dumps(r.json(), indent=4))

    if r.status_code != 200:
        raise ValueError(f"Failed to fetch release info from {release_url}")

    asset = [a for a in r.json()['assets'] if a['name'].endswith(".deb")][0]
    asset_url = asset['browser_download_url']
    asset_name = asset['name']

    print(f"Found asset {asset_name}")
    print(f"Writing asset URL: {asset_url} to asset_url.txt")
    
    with open('asset_name.txt', 'w') as f:
        f.write(asset_name)

    with open('asset_url.txt', 'w') as f:
        f.write(asset_url)

except RuntimeError as ex:
    print("Oh dear...")
    print(ex)
    exit(1)
