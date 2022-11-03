# Install ESRP client
az storage blob download --file esrp.zip --auth-mode login --account-name esrpsigningstorage --container signing-resources --name microsoft.esrpclient.1.2.76.nupkg
Expand-Archive -Path esrp.zip -DestinationPath .\esrp

# Install certificates
az keyvault secret download --vault-name "$env:AZURE_VAULT" --name "$env:AUTH_CERT" --file out.pfx
certutil -f -importpfx out.pfx
Remove-Item out.pfx

az keyvault secret download --vault-name "$env:AZURE_VAULT" --name "$env:REQUEST_SIGNING_CERT" --file out.pfx
certutil -f -importpfx out.pfx
Remove-Item out.pfx