# Validating GCM's GPG signature

Follow the below instructions to import GCM's public key and use it to validate
the latest Debian package and/or tarball signature.

## Debian package

```shell
# Install needed packages
apt-get install -y curl debsig-verify

# Download public key signature file
curl -s https://api.github.com/repos/git-ecosystem/git-credential-manager/releases/latest \
| grep -E 'browser_download_url.*gcm-public.asc' \
| cut -d : -f 2,3 \
| tr -d \" \
| xargs -I 'url' curl -L -o gcm-public.asc 'url'

# De-armor public key signature file
gpg --output gcm-public.gpg --dearmor gcm-public.asc

# Note that the fingerprint of this key is "3C853823978B07FA", which you can
# determine by running:
gpg --show-keys gcm-public.asc | head -n 2 | tail -n 1 | tail -c 17

# Copy de-armored public key to debsig keyring folder
mkdir /usr/share/debsig/keyrings/3C853823978B07FA
mv gcm-public.gpg /usr/share/debsig/keyrings/3C853823978B07FA/

# Create an appropriate policy file
mkdir /etc/debsig/policies/3C853823978B07FA
cat > /etc/debsig/policies/3C853823978B07FA/generic.pol << EOL
<?xml version="1.0"?>
<!DOCTYPE Policy SYSTEM "https://www.debian.org/debsig/1.0/policy.dtd">
<Policy xmlns="https://www.debian.org/debsig/1.0/">

  <Origin Name="Git Credential Manager" id="3C853823978B07FA" Description="Git Credential Manager public key"/>

  <Selection>
    <Required Type="origin" File="gcm-public.gpg" id="3C853823978B07FA"/>
  </Selection>

  <Verification MinOptional="0">
    <Required Type="origin" File="gcm-public.gpg" id="3C853823978B07FA"/>
  </Verification>

</Policy>
EOL

# Download Debian package
curl -s https://api.github.com/repos/git-ecosystem/git-credential-manager/releases/latest \
| grep "browser_download_url.*deb" \
| cut -d : -f 2,3 \
| tr -d \" \
| xargs -I 'url' curl -L -o gcm.deb 'url'

# Verify
debsig-verify gcm.deb
```

## Tarball
```shell
# Download the public key signature file
curl -s https://api.github.com/repos/git-ecosystem/git-credential-manager/releases/latest \
| grep -E 'browser_download_url.*gcm-public.asc' \
| cut -d : -f 2,3 \
| tr -d \" \
| xargs -I 'url' curl -L -o gcm-public.asc 'url'

# Import the public key
gpg --import gcm-public.asc

# Download the tarball and its signature file
curl -s https://api.github.com/repos/ldennington/git-credential-manager/releases/latest \
| grep -E 'browser_download_url.*gcm-linux.*[0-9].[0-9].[0-9].tar.gz' \
| cut -d : -f 2,3 \
| tr -d \" \
| xargs -I 'url' curl -LO 'url'

# Trust the public key
echo -e "5\ny\n" |  gpg --command-fd 0 --expert --edit-key 3C853823978B07FA trust

# Verify the signature
gpg --verify gcm-linux_amd64*.tar.gz.asc gcm-linux*.tar.gz
```
