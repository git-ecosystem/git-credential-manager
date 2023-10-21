# Install ESRP client
az storage blob download --file esrp.zip --auth-mode login --account-name $env:AZURE_STORAGE_ACCOUNT --container $env:AZURE_STORAGE_CONTAINER --name $env:ESRP_TOOL
Expand-Archive -Path esrp.zip -DestinationPath .\esrp

# Install certificates
az keyvault secret download --vault-name "$env:AZURE_VAULT" --name "$env:AUTH_CERT" --file out.pfx
certutil -f -importpfx out.pfx
Remove-Item out.pfx

az keyvault secret download --vault-name "$env:AZURE_VAULT" --name "$env:REQUEST_SIGNING_CERT" --file out.pfx
certutil -f -importpfx out.pfx
Remove-Item out.pfx