# Contributing

Hi there! We're thrilled that you'd like to contribute to GCM :tada:. Your help
is essential for keeping it great.

Contributions to GCM are [released][contribute-under-repo-license] to the public
under the [project's open source license][license].

Please note that this project is released with a
[Contributor Code of Conduct][code-of-conduct]. By participating in this project
you agree to abide by its terms.

## Start with an issue

1. Open an [issue][issue] to discuss the change you want to see.
   This helps us coordinate and reduce duplication.
1. Once we've had some discussion, you're ready to code!

## Submitting a pull request

1. [Fork][fork] and clone the repository
1. Configure and install the dependencies: `dotnet restore`
1. Make sure the tests pass on your machine: `dotnet test`
1. Create a new branch: `git switch -c my-branch-name`
1. Make your change, add tests, and make sure the tests still pass
1. For UI updates, test your changes by executing a `dotnet run` in applicable
   UI-related project directories:
    - `Atlassian.Bitbucket.UI.Avalonia`
    - `GitHub.UI.Avalonia`
    - `Atlassian.Bitbucket.UI.Windows`
    - `GitHub.UI.Windows`
1. Organize your changes into one or more [logical, descriptive commits][commits].
1. Push to your fork and [submit a pull request][pr]
1. Pat your self on the back and wait for your pull request to be reviewed and
   merged.

Here are a few things you can do that will increase the likelihood of your pull
request being accepted:

- Match existing code style.
- Write tests.
- Keep your change as focused as possible. If there are multiple changes you
   would like to make that are not dependent upon each other, consider
   submitting them as separate pull requests.

## Resources

- [How to Contribute to Open Source][how-to-contribute]
- [Using Pull Requests][prs]
- [GitHub Help][github-help]

[code-of-conduct]: CODE_OF_CONDUCT.md
[commits]: https://www.youtube.com/watch?v=4qLtKx9S9a8
[contribute-under-repo-license]: https://help.github.com/articles/github-terms-of-service/#6-contributions-under-repository-license
[fork]: https://github.com/GitCredentialManager/git-credential-manager/fork
[github-help]: https://help.github.com
[how-to-contribute]: https://opensource.guide/how-to-contribute/
[issue]: https://github.com/GitCredentialManager/git-credential-manager/issues/new/choose
[license]: LICENSE
[pr]: https://github.com/GitCredentialManager/git-credential-manager/compare
[prs]: https://help.github.com/articles/about-pull-requests/
