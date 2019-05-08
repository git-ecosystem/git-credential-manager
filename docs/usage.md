# Command-line usage

After installation, Git will use Git Credential Manager Core and you will only need to interact with any authentication dialogs asking for credentials.
GCM Core stays invisible as much as possible, so ideally you’ll forget that you’re depending on GCM at all.

Assuming GCM Core has been installed, use your favorite terminal to execute the following commands to interact directly with GCM.

```shell
git credential-manager [<command> [<args>]]
```

## Commands

### version

Displays the current version.

### get / store / erase

Commands for interaction with Git. You shouldn't need to run these manually.
