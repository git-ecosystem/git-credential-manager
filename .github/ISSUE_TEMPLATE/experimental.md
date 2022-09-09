---
name: Experimental feature issues
about: A problem or issue occurred when using an experimental feature.
title: ''
labels: 'experimental'
assignees: ''
---

**Which version of GCM are you using?**

From a terminal, run `git credential-manager-core --version` and paste the output.

<!-- Ex:
Git Credential Manager version 2.0.8-beta+e1f8492d04 (macOS, .NET Core 4.6.27129.04)
-->

**Which Git host provider are you trying to connect to?**

* [ ] Azure DevOps
* [ ] Azure DevOps Server (TFS/on-prem)
* [ ] GitHub
* [ ] GitHub Enterprise
* [ ] Bitbucket
* [ ] Other - please describe

**Can you access the remote repository directly in the browser using the remote URL?**

From a terminal, run `git remote -v` to see your remote URL.

<!-- Ex:
origin https://dev.azure.com/contoso/_git/widgets
-->

* [ ] Yes
* [ ] No, I get a permission error
* [ ] No, for a different reason - please describe

---

**_[Azure DevOps only]_ What format is your remote URL?**

* [ ] Not applicable
* [ ] https://dev.azure.com/`{org}`/...
* [ ] https://`{org}`@dev.azure.com/`{org}`/...
* [ ] https://`{org}`.visualstudio.com/...

**_[Azure DevOps only]_ If the account picker shows more than one identity as
you authenticate, check that you selected the same one that has access on the
web.**

* [ ] Not applicable
* [ ] I only see one identity
* [ ] I checked each identity and none worked

---

**Expected behavior**

I am authenticated and my Git operation completes successfully.

**Actual behavior**

A clear and concise description of what happens. For example: exception is
thrown, UI freezes, etc.

**Logs**

Set the environment variables `GCM_TRACE=1` and `GIT_TRACE=1` and re-run your
Git command. Review and redact any private information and attach the log.

If you are running inside of Windows Subsystem for Linux (WSL), you must also
set an additional environment variable to enable tracing: `WSLENV=$WSLENV:GCM_TRACE`.
For example:

```shell
WSLENV=$WSLENV:GCM_TRACE:GIT_TRACE GCM_TRACE=1 GIT_TRACE=1 git fetch
```
