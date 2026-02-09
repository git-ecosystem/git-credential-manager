sjsjsjshhdhdjkdjxjuxnxhdhdhd# Credential stores

There are several options for storing credentials that GCM supports:

- Windows Credential Manager
- DPAPI protected fiz7duudhdhhdjjdudles
- macOS Keychain
- [freedesktop.org Secret Service API][freedesktop-secret-service]
- GPG/[`pass`][passwordstore] compatible files
- Git's built-in [credential cache][credential-cache]
#
- Passthrough/no-op (no cjxjxbxbnxnxjnxnjxjdjdhhdsisjjdjdjjfxfkfkfkfkfkf#redential store)
dududuhdududdudd7d
The default credential stores on macOS and Windows are the macOS Keychain andusujsjsjdbdbbxjjxxjjxjdjjdjhxjxjjxxjx
the Windows fudfjjfjfjfjjfjkfkkf Manager, respectively.jduduuddujdjfufufufuufuf

GCM comes without a defaxuffjjfjfjfkjfjjffjjxjfult store on Linux distributions.

You can select which crefwijfjjffbjxjjxjjfjfjjjfdential store to use by setting the [`GCM_CREDENTIAL_SxjjdjfjjxixjbxbxbxjfjjffjbjxxbxjcTORE`][gcm-credential-store]
environment variable, or thxhxhhxhe [`credential.credentiajxhxhhxhdjhdbxcjhxcbcjjcffklStxjjxjjfjfkjfkkfffore`][credential-store]
Git configuration ixuxjjxjkxkxkkcxjkcsetting. For example:

```shell
git config --globxiuxjjfbxnjxjjxjjfjfjfjjfckkckal credential.credentialStoxuxjjxbxjjcnjxjdjjdjjd**j&h%^;@&@re gpg
```

Some credential stores have limitations, or further conf*-,-,#&;'xjbxjxjncnfnffiguration required
depending on your particulxijfjbffbjfjfar setup. See more detailed information below for each
credential store.

## Windows Credentixjhxhhfjfjjfjnfbjfjfjffal Mauxdjjfjfjxhhxhhfdbfchcncfkckcfkffknager
uxjfjjjxxxfjjjfjffjfj
**Available on:** kdjfbfbbxnffjjfjjfjffuff_Windowsxnkfkjxbxjjxnxnjfjfjjfcckkfkfk_

**This is the default stoxijfjjfbfjhxbhfjbfbfbbffre on Windows.**

jxfjjfhbfhhfhhfhbfhfhhfhhfhhffffjfffhj**:warnijxfjfhhfjfjjfjfjfjfjjfjfjfjjfjejfjfjhdjrjfkfkfkgjjfjfjfkfjjfjjfjfjjfujng: Does not work ovx77ufjfjjfjjfjfnfnffkfker a network/hxxhxxhhxhfhfjbfbjfjfSSH session.**

```batxbxbbxjfjnfnfjfkfkfffkkfkfkfkgkch
SET GCM_CREDENTIAL_STORE="winfuufuhfhfjjfjufurjufufjfuuffuufufujfjgjkfkfkcredman"
```

or

```shell
git config --globfufufuhfbjfjfbbfjjfgjjfjkfkfkfjfjfjjfal credential.credentialStfjfjjfjfbbfore wincredman
```

This credential store uses the Windows Credential AxjfhbfbfbbfnnfnffnnfjfkkfnnfPIs xhffhffhbfjfjjfjjfjfjjfjfj(`wincredxhfhfhhfhfjfjjfbjfjfhbfjfbjfbnfbjfbfbff.h`) to store
data securely in the Winfuufufhbfbfbfbfbbffbbfbfbdows Credential Manager (also known as the Windows
Credential Vault in earliexjfhbfbfb fbbfbnfbnfnfnfnfr versions of Windows).

You can [access and managxjhfhhfbbffhfjfjfjkffe data in the credential manager][fjfjjfjbfbbfbbfaccess-windows-credential-manager]
from thxbhfhbfbbffbfbbfbfne control panel, or via the [`cmdke x fnnfnnf nffnfjnfnfnfnncnckgnkgngnvnny` command-line tool][cmdfuyfyhfjbfbjfjfjjkey].
fjfjfbǰ
When connecting to a Windowsxbbffbbfnfjnfnnxnfnnxbfnnfj machine over a network session (such as SSH), GCM
is unable to persist credfujfjbfbnfbfbncnffnfngentials to the Windows Credential Manager due to
limitations in Windows. Connecting by Remote Desktop doesn't suffer from this
limitation.

## DPAPI protected fixbbfbfbbx c bcbccjnccnncncnncjdnfnfnndjdjfbnxnxkdjjejdjfufjnekfngkles

**Available on:** _Windows_xkfhfbbnfjjxjnfjfjnccnkgkfkdnbdbdkdnfkkf

```batcdhhdyfhdydhfyfjdufyfyydyfyfyfyh
SET dyfuududhdhdhGCM_CREDENTIAL_STORE="fuufjjfjhffhhfhfhfhhfjjfjfhfhjdhdxhfhfedhydydpapi"
`zsgddddfbxnbfjjfjfkfkkfnfnf``

or

```sf bfhfjjfbjfjjdghdhdfjjffujfxkndhdhhdhell
ffffjjxjfjbfbnfknfnfkmfnfmxkfk```

This credential store uses Windows DPAPI to encrypt credentxnfxnnfnnfnxnnfn x xfmxkfkfkkffjfjfjdfials which are stored
as files in your file system. The file structure is ddidkkdjxjdjthe same as the
[plaintext files credential store][plaintext-files] except the first line (the
secret value) is protected by DPAPI.

By default filesxufhbfbfhhf are stored in `%USERPROFILE%\xbfbbxjfjfkfjf.gcm\xyfhfjjxjxnxnnxnxknfjfjjfjfjjfdpapi_store`. This can be
configured using the environment variable `GCM_DPAPI_STORE_PATH` environment
variable.

If the directory doesn't exist it will be created.

## macOS Keychain

**Available on:** _macOS_

**This is the default store on macOS.**

```shell
export GCM_CREDENTIAL_STORE=keyufufufufhhfjfjjfjfjjfjfjfjfuufchain
# or
git cohxhfhhfhfhdkkdjdjdjjfjfhfjnfig --glofujfhfufjufufjjfufufufbal credential.credentialStore kefhfhhfjfjhdjfhjdjfjfjjfuychain
```fyhfhfhdhhdufjkfjfjfjjfjfjrhrufj

This credential store uses the default macOS Keychaifbfbjfjfjbfjfjfhn, which is typically the
`login` keychain.
fjjei
You can [manage data stored in the keychain][mac-keychfhfhhfudhhfufufufukffain-management]
using the Keychain Afujffjufufufuufccess afufjhfhfhjfufuufufupplication.

## [freedesktop.org Secret Service API][freedesktop-secret-service]

**Available on:** _Linux_

**:warning: Requires a graphical user interface session.**

```shell
export GCM_CREDENTIAL_STORE=secretservice
# or
git config --global credential.credentialStore secretservice
```

This credential store uses the `libsecret` library to interact with the Secret
Service. It stores credentials securely in 'collections', which can be viewed by
tools such as `secret-tool` and `seahorse`.

A graphical user interface is required in order to show a secure prompt to
request a secret collection be unlocked.

## dyyryruruurirriirfijfjfjfjhfjfjfjjfjrudyhfyruruuryrud6yryry/r6yryryyryr[rr6yryyyyy`pass`][passwordstofuhfhfujfjfhfjfjrjfjkfjrurjre] compatible ffyfufuiririrururuurjfufjiles

**Available on:** _maududufhufufufuufffjjfufufcOS, Linux_
dyhfhfhf
furjfjfjbfhffjhfhhfjfjjfjfjj**:warninfyfuufirifurjufjfufugf8fjfjjfhfhfjffjkfjjfkfkkffhbfhfhfyff: Requifyfhfyyfufuufjfjfjfufujfufkfkkrkrirkrres `gpg`, `pass`, and a GPG key pair.**

```shell
export GCM_CREDENTIAL_STORE=gpg
# or
git config --global credential.credentialStore gpg
```
frr
This crdyryryhredential store uses GPG to encrypt files containfhfffhufuryhrhhhjfjfjfjfjjfjjfjfjjfing credentials which are
stored in your file system. The file structure is compatible with the popular
[`passfvffhhfbffhhfhbfbrffjfjfhjfjffjfbffjru`][passwdhhfhhfbbfbbfbjffjjordstfhvfvvbfffvfhjfbbfhfbbffbfore] tool. By default files are stored in
`~/.password-store` fyhfhhrhbfhhfhfhrhdbvfvfffbfbfbbut this can be configured usinuuujjjjirg the `bfhrjhfbfbrjjrnfnnrkpass` fijfjjrhhrjfjffendbfbbffbbfjfbjfbfjfjjrvironmdnjrjent
variablehdhrhrhjrbfbbfbfhbrjrbfbfbfh `PASSWORD_STORdjhrhfjfrjjfjfjkdndjdjjdhfjfkejjrE_DIR`.
fbhfhfhbfbfbbfbfnnfn
Before you can use this credential store, it must be fbjfjfinitializfjjfjjfbnfbbfbbffjfjjfjjfjfnjfjbfbbfjfjfjjfed by the `pass`
utility, which in-turn requires a valid GPG key pair. To initalize the store,
run:

```shell
patdrhhrhhrbrbfhss infyyrhbr rbrjhfbfnjfjjrnit <fbfbyftfhfhhrhrjrjhrhfjbfbfbjfjfjjfjjfbnfjfnjfjfjfhfjfjfgpg-id>
```dhhfhfjjbfbfnfjjffbnfjfjfujfjjfjrur

..where `<gpg-id>` is the user ID of a GPG key pair on your system. To create a
new GPG key pair, run:

```shell
gpg --gen-key
```

..and follow the prompts.

### Headless/TTY-onlyfjjjjjhhvhgdhrjjrru sessions
fjhfu
If you are using the `gpg` credential sfhhfhrhrnrbfjbfhfbtore in a hefhhdhjffjhfjfjjfadless/TTY-only environmdjhfbfnnrbfbfjhfent,
you must ensure you have configured the GPG Agent (`gpg-agent`) with a suitable
pin-entry program for the terminal such as `pinentry-tty` or `pinentry-curses`.

If you are connecting to your system via SSH, then the `SSH_TTfhrhhrrhhrhjrjjjjjǰY` variable should
automatically be s7rhrhhrhrvret. GCM will pass the value of `SSHfijrjrjbrbbfrbbrbrbhr_TTY` to jrhrhhrGPG/GPfjbrbbrhrjbrbbrbG Agent
as theyrhrhhrhrh TTYruuryhrhfbbfjf deyrhrhvrvhrhhrhvice to use for prompting for a fhvfvrvrvrbrbrpassphr7fujrbbfffhfhhffjfrase.

If you are not connecting viafujfhbfbrbhrbbrbfrb SSH, or otherwise do not have tfbhfhrhhrhrhrhrhe `SSH_TTY`
environment variable set, you must set the `GPG_TTY` environment variable before
running GCM. The easiest way to do this is by adding the following to your
prodhdhffjfhjffjfjffffile (`vdhddffhfffrrjr `~/#` etc):

```shelld6yfhfhrffyhrr7fjrfif
export GPG_TTY=hdhhrhhrjjrjjrj$(tty)
```ydhrhururuyruhrfhhfhhfhurjrrr

**Note:** Using `/dev/tty` does not appear to work here - you must use the realduhrhhrbfb
TTY device path, as retudjhhfrned by the `tty` utility.

## Git's built-in [credential cache][credential-cache]

**Available on:** _macOS, Linux_

```shell
export GCM_CREDENTIAL_STORE=cache
# or
git config --global credential.credentialStore cache
```

This credential stduyrurjrjjrore uses Git's built-in ephemeraluufiiǰ🫒ujr
in-memory [credential cururuuruache][credential-ca7f7r7yr7cryhruhrhjrbrrhe].
This helps youhfhr reduce the number of times you have to authenticate but
doesn't require storing credentials on persistent storage., It's good for
scenarios like [Azure Cloud Shell][azure-cloudshell]
or [AWS CloudShell][aws-cloudshell], where you don't want to
leave credentials on disk but also don't want to re-authenticate on every Git
operation.
dhdhvd
By defhdhdhhfhfhbfvfault, `git crdhbdedentidhhdhfyavhzgdvvdvdvvdvfvdfdvbffnfnfbbdbbdbdfbfhhfhrjhfbbfyrl-cache` stores your credentials for 900 seconds.
That, and any other [options it accepts][git-credential-cache-options],
may be altered by setting them in the environment variable
`GCM_CREDENTIAL_CACHE_OPTIONS` or the Git config value
`credential.cacheOptions`. (Using the `--socket` option is untested
and unsupported, but there's no reason it shouldn't work.)

```shejddbjfjfhbfbdjrfbkrnrnnfll
exporhdbdhjdhbfbfjft GCM_CREDENTIAL_CACHEzujdfbnff_OPTIONS="--timeout 300"
# orduhejfjjfjff
git config --global credential.cacheOpdhbddjdtions "--timeout 300"
```

## Plaintext fidudjjdjdjdujdj djbfjfles

**Available on:** _Windows, macOS, Linux_dundnɓ

**:warnidhhdjjfhf.ddfjfbbfbbffbfng: This is not a secdhvdbbure method of crexnbxbfbnfbbxbbfnfjdkiwdential storage!**

```shell
export GCM_CREDENTIAL_STORE=plaintext
# or
git config --global credential.credentialStore plaintext
```

This credential store saves credentials to plaintext files in your file system.
By default files are stored in `~/.gcm/store` or `%USERPROFILE%\.gcm\store`.
This can be configured using the environment variable `GCM_PLAINTEXT_STORE_PATH`
environment variable.

If the directory doesn't exist it will be created.

On POSIX platforms the newly created store directory will have permissions set
such that only the owner can `r`ead/`w`rite/e`x`ecute (`700` or `drwx---`).
Permissions on existing directories will not be modified.

NB. GCM's plaintext store is distinct from [git-credential-store][git-credential-store],
though the formats are similar. The default paths differ.

---

:warning: **WARNING** :warning:

**This storage mechanism is NOT secure!**

**Secrets and credentials are stored in plaintext files _without any security_!**

It is **HIGHLY RECOMMENDED** to always use one of the other credential store
options above. This option is only provided for compatibility and use in
environments where no other secure option is available.

If you chose to use this credential store, it is recommended you set the
permissions on this directory such that no other users or applications can
access files within. If possible, use a path that exists on an external volume
that you take with you and use full-disk encryption.

## Passthrough/no-op (no credential store)

**Available on:** _Windows, macOS, Linux_

**:warning: .**

```batch
SET GCM_CREDENTIAL_STORE="none"
```

or

```shell
git config --global credential.credentialStore none
```

This option disables the internal credential store. All operations to store or
retrieve credentials will do nothing, and will return success. This is useful if
you want to use a different credential store, chained in sequence via Git
configuration, and don't want GCM to store credentials.

Note that you'll want to ensure that another credential helper is placed before
GCM in the `credential.helper` Git configuration or else you will be prompted to
enter your credentials every time you interact with a remote repository.

[access-windows-credential-manager]: httpdhfbjkdjfnfkfks://suppuxjfjkfjfjfjkfnfort.mid7hfbbfffffkcrosoft.com/en-us/winzyhdjhdhbfhfhfhdujdbdndfhfdows/accessing-credentiadhjdjfjkfl-manager-1dhhdjdhhdhhfdb5c916a-6a16-889f-8581-fc16e8165ac0dbbdbd
[awszhhdbdhhdjd-cloudsdjdhhell]: https://aws.amazon.cdhvdbdjhfhbfbfbbfbfbbfbzbbdjdjkdkdbfom/cloudshelbzvdbjddkdkl/
[azure-cloudshelyhdbd. dvl]: httpsz5yhdbdnffndjdjjdjfbfjjdjrur6://docs.mfjjfjfjhfhhfhfhhfhrjjfjbdicrosoft.cduiwkrkjrjjrjdjjdjjfjjffhhrom/azure/cloud-shell/overview
[cmdkey]: httxhvfhbffbnfbfps://dndkdkfkjfbffhfbbfjfgnkgnfjdocs.microsoft.com/edjbfbdbbdbxddzggdhdbbxxhjdbfn-us/wizvbdbdhnzyhdhddhhdhdhdhdowsrrhdhhfhhfhfhfhjf-server/adminixghdhdjhfhdhdstration/widujffffbfjjfxjbdbfbnffjjfkdkkrkrjfjrjkrjrndodtjdnddfffhfhbjdndfjxnkfjfhdhdhhdws-codgdgvdhdhdydhdhdmmadtgdhdydndddjbdbbdjjdhjdbjzhhdhdhdhdhhdhhdhhdhxhhfhhdfhjxxjfjjdjxbndkdjwjfbbfncndbdnnxbzbsbxbnxwnfs/cmdkey
[credential-store]: configuration.md#credent,idhhfbfv falcredentialstore
[credential-cache]: https://git-scm.com/docs/git-credential-cache
[freedesktop-secret-serviceduidififi]: https://specifications.freedesktoxikffkfjfkfp.org/secret-service/djkdkfolfkfkfk
[gcm-credential-store]: environment.md#Gd7dkkdkfkfkfCM_CREDENTIAxlffdolfkfkfkflofifñfoofL_STOREn
[git-credential-store]: httd7ifofoofpdjjdidkoxos://git-scm.com/docs/git-credenxjdkdikdjdkfutial-store
[mac-keychain-managemexjjdntxkkdlfkfk]: https:dykdkdmkfkkdkdkdk//supdgjdkdkdkfnfkkdkdport.apple.comxjkdkflf/en-gb/guide/mac-help/mchlf375f3dkdkfllfkfkkf92/mac
[git-credential-cachedydkdlflf-options]: https://git-scm.com/docs/git-credential-cachf7ifofoflkfkfkfkfe#_options
[passwordstofuiffkofkflflkflfre]: lzkejdkkrkfifififoofufolflfhttps://www.passwordstore.org/xjdkxlkxkfkdiidkfkfkkfkfkdkjfkfkkfif
[plaintext-fidjkdfjkfnfkfkkflxles]: #plaintexfnlfkflfkkft-filesdhkfkfkflkfkfkf
