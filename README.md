# featbit-cli

`featbit-cli` is a cross-platform .NET 10 CLI for calling the FeatBit OpenAPI with an access token.

The current scope focuses on three common operations:

- List projects in an organization
- Get a single project by ID
- List feature flags in an environment

It also includes user-level configuration commands so you can store your FeatBit API host, access token, and organization ID outside the repository.

## Features

- .NET 10 CLI implementation
- Designed for Windows, Linux, and macOS
- Access token authentication via `Authorization: api-<token>`
- Default FeatBit API host: `https://app-api.featbit.co`
- User configuration stored in the user profile, not in the repository
- JSON output for automation and table output for humans
- Validation command with machine-friendly exit codes

## Project Structure

- `src/FeatBit.Cli` - CLI application
- `AI_AGENT_TEST_STORY.md` - AI agent driven validation story
- `featbit-openapis.json` - FeatBit OpenAPI contract source

## Build

Requirements:

- .NET 10 SDK

Build the solution:

```bash
dotnet build featbit-cli.slnx -c Release
```

Run the CLI from source:

```bash
dotnet run --project src/FeatBit.Cli -- --help
```

## Native AOT Publish

The CLI is configured for Native AOT publish targets:

- `win-x64`
- `linux-x64`
- `osx-x64`
- `osx-arm64`

Example:

```bash
dotnet publish src/FeatBit.Cli/FeatBit.Cli.csproj -c Release -r win-x64
```

On Windows, Native AOT requires the platform linker prerequisites, including Visual Studio C++ build tools.

## Configuration

The CLI resolves configuration in this order:

1. Command-line arguments
2. Environment variables
3. User config file

### Default Host

If no host is provided, the CLI uses:

```text
https://app-api.featbit.co
```

### Environment Variables

- `FEATBIT_HOST`
- `FEATBIT_TOKEN`
- `FEATBIT_ORG`
- `FEATBIT_USER_CONFIG_FILE` (optional override for the user config path)

### User Config Location

By default, user config is stored outside the repository.

Typical locations:

- Windows: `%APPDATA%\featbit\config.json`
- macOS: `~/Library/Application Support/featbit/config.json`
- Linux: `~/.config/featbit/config.json`

## Config Commands

Initialize config interactively:

```bash
featbit config init
```

Set one or more values directly:

```bash
featbit config set --host https://app-api.featbit.co --token api-xxxxx --org <organization-id>
```

Show current config:

```bash
featbit config show
```

Clear saved config:

```bash
featbit config clear
```

Validate the current configuration by calling the FeatBit API:

```bash
featbit config validate
```

Validation exit codes:

- `0` - success
- `1` - general failure
- `2` - authentication failure
- `3` - network failure

## Usage

List projects:

```bash
featbit project list
```

List projects with explicit parameters:

```bash
featbit project list --host https://app-api.featbit.co --token api-xxxxx --org <organization-id>
```

Get a project by ID:

```bash
featbit project get <project-id>
```

List feature flags in an environment:

```bash
featbit flag list <env-id>
```

List all flags across pages:

```bash
featbit flag list <env-id> --all
```

Filter flags by name or key:

```bash
featbit flag list <env-id> --name my-flag
```

Use JSON output for automation:

```bash
featbit project list --json
featbit project get <project-id> --json
featbit flag list <env-id> --json
```

## Help

Show built-in help:

```bash
featbit --help
```

## Testing Approach

This repository uses an AI agent driven testing workflow instead of maintaining a dedicated CLI unit test project. The test artifact is structured as an execution-oriented specification with ordered cases, expected results, and reporting fields.

See:

- `AI_AGENT_TEST_STORY.md`

## Notes

- Authentication uses the FeatBit access token format `api-<token>`.
- If the token already includes the `api-` prefix, it is used as-is.
- The CLI currently targets project and feature flag read operations only.
