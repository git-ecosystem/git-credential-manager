# Validating GCM's GPG signature

Follow the below instructions to import GCM's public key and use it to validate
the latest Debian package and/or tarball signature.

## Debian package

```shell
# Install needed packages
apt-get install -y curl debsig-verify

# Download public key signature file
curl -Os https://packages.microsoft.com/keys/microsoft-2025.asc

# De-armor public key signature file
gpg --output microsoft-2025.gpg --dearmor microsoft-2025.asc

# Note that the fingerprint of this key is "EE4D7792F748182B", which you can
# determine by running:
gpg --show-keys microsoft-2025.asc | head -n 2 | tail -n 1 | tail -c 17

# Copy de-armored public key to debsig keyring folder
mkdir /usr/share/debsig/keyrings/EE4D7792F748182B
mv microsoft-2025.gpg /usr/share/debsig/keyrings/EE4D7792F748182B/

# Create an appropriate policy file
mkdir /etc/debsig/policies/EE4D7792F748182B
cat > /etc/debsig/policies/EE4D7792F748182B/generic.pol << EOL
<?xml version="1.0"?>
<!DOCTYPE Policy SYSTEM "https://www.debian.org/debsig/1.0/policy.dtd">
<Policy xmlns="https://www.debian.org/debsig/1.0/">

  <Origin Name="Git Credential Manager" id="EE4D7792F748182B" Description="Git Credential Manager public key"/>

  <Selection>
    <Required Type="origin" File="microsoft-2025.gpg" id="EE4D7792F748182B"/>
  </Selection>

  <Verification MinOptional="0">
    <Required Type="origin" File="microsoft-2025.gpg" id="EE4D7792F748182B"/>
  </Verification>

</Policy>
EOL

# Download Debian package (substitute `x64` with `arm64` on ARM machines)
curl -s https://api.github.com/repos/git-ecosystem/git-credential-manager/releases/latest \
| grep "browser_download_url.*-x64-.*deb" \
| cut -d : -f 2,3 \
| tr -d \" \
| xargs -I 'url' curl -L -o gcm.deb 'url'

# Verify
debsig-verify gcm.deb
```

## Tarball
```shell
# Download the public key signature file
curl -Os https://packages.microsoft.com/keys/microsoft-2025.asc

# Import the public key
gpg --import microsoft-2025.asc

# Download the tarball and its signature file
curl -s https://api.github.com/repos/git-ecosystem/git-credential-manager/releases/latest \
| grep -E 'browser_download_url.*gcm-linux.*[0-9].[0-9].[0-9].tar.gz' \
| cut -d : -f 2,3 \
| tr -d \" \
| xargs -I 'url' curl -LO 'url'

# Trust the public key
echo -e "5\ny\n" |  gpg --command-fd 0 --expert --edit-key EE4D7792F748182B trust

# Verify the signature
gpg --verify gcm-linux_amd64*.tar.gz.asc gcm-linux*.tar.gz
```
