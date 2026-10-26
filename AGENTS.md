# Agent instructions

Follow [CONTRIBUTING.md](CONTRIBUTING.md) when contributing to Git Credential
Manager. Keep changes focused, match existing code style, and include tests and
documentation for behaviour changes.

## Ethos

Git Credential Manager is an open source project that values collaboration,
code quality, and adherence to best practices, whilst still being pragmatic
about trade-offs and exceptions to the rules. Don't be afraid to make pragmatic
decisions when necessary, but have a good and well argued rationale.

We try to not favor any particular platform, technology, or approach without
good reason, and aim to avoid hacks and workarounds where possible.

Be kind, be honest, and respectful.

## Project guidance

Read the guidance relevant to your change:

- [Development and debugging](docs/development.md): setup, build commands,
  debugging, testing, coverage, and documentation checks.
- [Architecture](docs/architecture.md): project boundaries, command execution,
  platform abstractions, credential storage, and error handling.
- [Host provider specification](docs/hostprovider.md): provider selection,
  credential operations, protocol responses, and authentication interaction.
- [Main thread dispatcher](docs/dispatcher.md): UI and broker threading,
  lifecycle rules, and testing seams.
- [Documentation index](docs/README.md): user-facing configuration and
  provider-specific guidance.
- [SECURITY.md](SECURITY.md): private reporting of security vulnerabilities.

## Making changes

- Inspect the affected implementation and tests before editing. Reuse existing
  helpers and patterns rather than introducing parallel implementations.
- Preserve Windows, macOS, and Linux behaviour. Use `ICommandContext` services
  and existing platform abstractions; reuse mocks from `src/TestInfrastructure`
  instead of accessing live services in unit tests.
- Keep Git's credential stdin/stdout separate from user interaction.
  Use `ICommandContext.Console` for messages and prompts, and secret-aware
  tracing for sensitive values. Respect interaction and prompt settings.
- Follow the dispatcher rules for main-thread work. Do not eagerly start the
  UI framework or block its main loop on asynchronous work.
- Keep dependencies, reflection, and serialization compatible with Native AOT
  and trimming. Use the existing source-generated JSON contexts and central
  package versions in `Directory.Packages.props`.
- Update user documentation when changing configuration, environment variables,
  commands, or authentication behaviour. Update developer documentation when
  changing architecture, APIs, build tooling, or test workflows.
- Do not hand-edit generated artifacts under `out/`. Do not use real secrets
  in source, tests, examples, or ordinary trace messages.
- Avoid making drive-by changes to other pieces of code; focus on the area
  relevant to your change.
- If making changes directly outside the scope of the request would make things
  clearer/cleaning and more maintainable, make the change or refactor as a
  standalone change in a commit first, then a second commit to introduce your
  main change.
- Stop and ask when requirements or repository rules are unclear.

## Validation

For every code change, build and run the full solution test suite:

```shell
dotnet build git-credential-manager.slnx
dotnet test git-credential-manager.slnx
```

Targeted tests can help during development but do not replace the full run.
Platform-specific tests skip on other operating systems; CI provides the
cross-platform matrix.

Exercise changed UI flows through the main executable. Validate publishing or
packaging changes with the applicable distribution build on its host OS; an
ordinary build does not validate Native AOT publishing. See the development
guide for commands.

For documentation-only changes, follow best practices for Markdown and use
common linters to check for spelling mistakes and layout issues.

## Commits

Use logical commits and follow the
[commit guidance](CONTRIBUTING.md#commits) for area-prefixed imperative subjects,
explanatory bodies, sign-offs, and AI-assistance trailers.
