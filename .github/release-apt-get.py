from os import enviorn as env

repo_config = {
    "AADResource": "https://microsoft.onmicrosoft.com/945999e9-da09-4b5b-878f-b66c414602c0",
    "AADTenant": "72f988bf-86f1-41af-91ab-2d7cd011db47",
    "AADAuthorityUrl": "https://login.microsoftonline.com",
    "server": "azure-apt-cat.cloudapp.net",
    "port": "443",

    "repositoryId": env['APT_REPO_ID'],
    "AADClientId": env['AZURE_AAD_ID'],
    "AADClientSecret": env['AAD_CLIENT_SECRET']
}

configs = [
	("config.json", repo_config),
]

for filename, data in configs:
	with open(filename, 'w') as fp:
		json.dump(data, fp)

