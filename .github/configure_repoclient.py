from os import environ as env
import json

def check_var(name:str) -> bool:
    if name not in env:
        print(f"Required env var {name} is missing!")
        exit(1)

for var in ['APT_REPO_ID', 'AZURE_AAD_ID', 'AAD_CLIENT_SECRET']:
    check_var(var)

repo_id = env['APT_REPO_ID']
aad_id = env['AZURE_AAD_ID']
password = env['AAD_CLIENT_SECRET']

repo_config = {
    "AADResource": "https://microsoft.onmicrosoft.com/945999e9-da09-4b5b-878f-b66c414602c0",
    "AADTenant": "72f988bf-86f1-41af-91ab-2d7cd011db47",
    "AADAuthorityUrl": "https://login.microsoftonline.com",
    "server": "azure-apt-cat.cloudapp.net",
    "port": "443",

    "repositoryId": repo_id,
    "AADClientId": aad_id,
    "AADClientSecret": password,
}

configs = [
	("config.json", repo_config),
]

for filename, data in configs:
	with open(filename, 'w') as fp:
		json.dump(data, fp)

