# featbit-cli

`featbit-cli` is a cross-platform .NET 10 CLI for calling the FeatBit OpenAPI with an access token.

Supported operations:

- List projects in an organization
- Get a single project by ID
- List, create, toggle, archive, and set rollout for feature flags
- Evaluate feature flags for a given user

It also includes user-level configuration commands so you can store your FeatBit API host, access token, and organization ID outside the repository.

## Quick Start

1. Build or download the CLI binary (see [Build](#build) or [Native AOT Publish](#native-aot-publish)).
2. Initialize your local config once:

   ```bash
   featbit config init
   ```

3. Verify the credentials are correct:

   ```bash
   featbit config validate
   ```

4. Start using commands:

   ```bash
   featbit project list
   featbit project get <project-id>
   featbit flag list <env-id>
   featbit flag toggle <env-id> <key> true
   featbit flag evaluate --user-key <keyId> --env-secret <secret>
   ```

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

### Installation After Publish

After publishing, copy the output binary to a directory on your `PATH` so you can run `featbit` from anywhere:

**Windows:**

```powershell
# Copy to a directory already on PATH, for example:
copy src\FeatBit.Cli\bin\Release\net10.0\win-x64\publish\FeatBit.Cli.exe C:\tools\featbit.exe
```

**Linux / macOS:**

```bash
cp src/FeatBit.Cli/bin/Release/net10.0/linux-x64/publish/FeatBit.Cli /usr/local/bin/featbit
chmod +x /usr/local/bin/featbit
```

When running from source instead of a published binary, replace `featbit` in all examples below with:

```bash
dotnet run --project src/FeatBit.Cli --
```

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

### `config init`

Initialize config interactively. Prompts for host, access token, and organization ID. Press Enter to keep the current value for any field.

```bash
featbit config init
```

Example session:

```
Initialize FeatBit CLI user config
Press Enter to keep the current value.

Host [https://app-api.featbit.co]:
Access token [<empty>]: api-xxxxxxxxxxxxxxxx
Organization [<empty>]: 4ce9b8b9-0000-0000-0000-b13d0097b159
Config saved to: /home/user/.config/featbit/config.json
```

### `config set`

Set one or more values non-interactively. At least one of `--host`, `--token`, or `--org` must be provided.

```bash
featbit config set --host https://app-api.featbit.co --token api-xxxxx --org <organization-id>
```

You can set individual fields without touching the others:

```bash
featbit config set --token api-new-token
```

### `config show`

Display the current config. The token is masked for safety.

```bash
featbit config show
```

Example output:

```
Config file: /home/user/.config/featbit/config.json
host: https://app-api.featbit.co
token: api-...xTg
organization: 4ce9b8b9-0000-0000-0000-b13d0097b159
```

### `config clear`

Remove the saved config file.

```bash
featbit config clear
```

Prints `Config cleared.` or `Config file not found.` (exit code `0` in both cases).

### `config validate`

Validate the current configuration by calling the FeatBit API. Accepts the same `--host`, `--token`, and `--org` flags to override saved values without writing to disk.

```bash
featbit config validate
featbit config validate --token api-other-token
featbit config validate --host https://self-hosted.example.com
```

Example output on success:

```
Config validation succeeded.
Host: https://app-api.featbit.co
Organization: 4ce9b8b9-0000-0000-0000-b13d0097b159
Projects fetched: 3
```

Validation exit codes:

| Code | Meaning |
| ---- | ------- |
| `0` | Success |
| `1` | General failure |
| `2` | Authentication failure (HTTP 401 / 403) |
| `3` | Network failure (DNS, connection refused, timeout) |

## Usage

### `project list`

List all projects in the organization.

```bash
featbit project list
featbit project list --json
```

Table output columns: `Id`, `Name`, `Key`, `EnvCount`

Example table output:

```
Id                                   | Name          | Key           | EnvCount
-------------------------------------+---------------+---------------+---------
2c9b3a7d-0000-0000-0000-9e38128ca935 | My Project    | my-project    | 2
```

### `project get`

Fetch a single project and its environments by project ID (must be a valid GUID).

```bash
featbit project get <project-id>
featbit project get <project-id> --json
```

Table output shows project name, key, ID, then an environment table with columns: `EnvId`, `Name`, `Key`, `Description`

Example table output:

```
Project: My Project (my-project)
Id: 2c9b3a7d-0000-0000-0000-9e38128ca935

EnvId                                | Name | Key  | Description
-------------------------------------+------+------+------------
b60c6bc0-0000-0000-0000-7f0ade9ff94c | Dev  | dev  |
9ac3fe71-0000-0000-0000-b331fe9edd4b | Prod | prod |
```

### `project list` with explicit credentials

Override saved config on a single call without changing the config file:

```bash
featbit project list --host https://app-api.featbit.co --token api-xxxxx --org <organization-id>
```

All business commands accept `--host`, `--token`, and `--org` as overrides.

### `flag list`

List feature flags in an environment (must be a valid GUID).

```bash
featbit flag list <env-id>
```

Table output columns: `Id`, `Key`, `Name`, `Enabled`, `Type`, `Tags`

Example table output:

```
Id                                   | Key        | Name       | Enabled | Type    | Tags
-------------------------------------+------------+------------+---------+---------+-------
184da9ee-0000-0000-0000-e34f517f73b3 | my-feature | My Feature | on      | boolean | beta
TotalCount: 1
```

#### Pagination

By default the first page of 10 flags is returned. Use `--page-index` and `--page-size` to page through results:

```bash
featbit flag list <env-id> --page-index 0 --page-size 20
featbit flag list <env-id> --page-index 1 --page-size 20
```

| Option | Default | Description |
| ------ | ------- | ----------- |
| `--page-index` | `0` | Zero-based page number |
| `--page-size` | `10` | Number of flags per page |

#### Fetch all pages at once

```bash
featbit flag list <env-id> --all
```

Fetches every page and returns the combined result set. `TotalCount` reflects the total number of flags.

#### Filter by name or key

```bash
featbit flag list <env-id> --name my-flag
```

Passes the value as a server-side name/key filter. Partial matches are supported.

#### JSON output

```bash
featbit flag list <env-id> --json
featbit flag list <env-id> --all --json
```

JSON shape:

```json
{
  "success": true,
  "errors": [],
  "data": {
    "totalCount": 5,
    "items": [
      {
        "id": "...",
        "name": "...",
        "key": "...",
        "isEnabled": true,
        "variationType": "boolean",
        "tags": ["beta"],
        "createdAt": "2026-01-15T07:14:32Z",
        "updatedAt": "2026-02-01T13:17:35Z"
      }
    ]
  }
}
```

### JSON output for project commands

```bash
featbit project list --json
featbit project get <project-id> --json
```

All JSON responses share the same envelope:

```json
{
  "success": true,
  "errors": [],
  "data": { ... }
}
```

### `flag toggle`

Enable or disable a feature flag.

```bash
featbit flag toggle <env-id> <key> true
featbit flag toggle <env-id> <key> false
featbit flag toggle <env-id> <key> true --json
```

Example output:

```
Feature flag 'my-feature' is now enabled.
```

### `flag archive`

Archive a feature flag.

```bash
featbit flag archive <env-id> <key>
featbit flag archive <env-id> <key> --json
```

Example output:

```
Feature flag 'my-feature' has been archived.
```

### `flag create`

Create a new boolean feature flag.

```bash
featbit flag create <env-id> --flag-name <name> --flag-key <key>
featbit flag create <env-id> --flag-name <name> --flag-key <key> --description <desc>
featbit flag create <env-id> --flag-name <name> --flag-key <key> --json
```

| Option | Required | Description |
| ------ | -------- | ----------- |
| `--flag-name` | Yes | Display name of the flag |
| `--flag-key` | Yes | Unique key for the flag |
| `--description` | No | Optional description |

Example output:

```
Feature flag 'My Feature' (key: my-feature) created successfully.
```

### `flag set-rollout`

Update the rollout (percentage) assignments for a feature flag's variations.

```bash
featbit flag set-rollout <env-id> <key> --rollout '<json>'
featbit flag set-rollout <env-id> <key> --rollout '<json>' --dispatch-key <attribute>
featbit flag set-rollout <env-id> <key> --rollout '<json>' --json
```

The `--rollout` value is a JSON array of `{ variationId, percentage }` objects. Percentages must sum to 100.

Example:

```bash
featbit flag set-rollout <env-id> my-flag --rollout '[{"variationId":"<uuid1>","percentage":70},{"variationId":"<uuid2>","percentage":30}]'
```

| Option | Required | Description |
| ------ | -------- | ----------- |
| `--rollout` | Yes | JSON array of variation rollout assignments |
| `--dispatch-key` | No | User attribute used to determine bucket (default: user key) |

Example output:

```
Rollout for feature flag 'my-flag' updated successfully.
```

### `flag evaluate`

Evaluate feature flags for a given user against the FeatBit evaluation server.

```bash
featbit flag evaluate --user-key <keyId> --env-secret <secret>
featbit flag evaluate --user-key <keyId> --env-secret <secret> --flag-keys flag1,flag2
featbit flag evaluate --user-key <keyId> --env-secret <secret> --tags release --tag-filter or
featbit flag evaluate --user-key <keyId> --env-secret <secret> --json
```

| Option | Required | Description |
| ------ | -------- | ----------- |
| `--user-key` | Yes | Unique identifier for the user |
| `--env-secret` | Yes | Environment client-side SDK secret |
| `--eval-host` | No | Evaluation server URL (defaults to management host) |
| `--user-name` | No | Display name of the user |
| `--custom-props` | No | JSON object of custom user attributes |
| `--flag-keys` | No | Comma-separated list of flag keys to evaluate |
| `--tags` | No | Comma-separated list of tags to filter flags by |
| `--tag-filter` | No | Tag filter mode: `and` or `or` (default: `or`) |

Example table output:

```
Key        | Variation | MatchReason
-----------+-----------+------------
my-feature | true      | TargetMatch
beta-ui    | false     | Default
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
- All `flag` write commands (`toggle`, `archive`, `create`, `set-rollout`) require a valid management API token.
- `flag evaluate` calls the FeatBit evaluation server using the environment SDK secret, not the management API token.
