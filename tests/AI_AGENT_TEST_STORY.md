# AI Agent Test Specification

This repository does not keep a dedicated test project for the CLI.

Validation is performed by an AI agent that executes the built CLI against a real or controlled FeatBit environment and records the observed behavior.

## Objective

Prove that `featbit-cli` can safely and correctly:

- manage user configuration
- validate API connectivity and credentials
- list projects
- get a single project by ID
- list feature flags in an environment

## Execution Rules

1. Treat this file as the source of truth for agent-driven validation.
2. Execute tests in order unless a precondition blocks later cases.
3. Record pass/fail and any captured output per case.
4. Stop only when a blocking failure makes later cases meaningless.
5. Prefer running the built CLI exactly as an end user would.

## Shared Inputs

The agent needs these values before running command-level cases:

| Name | Description |
| --- | --- |
| `HOST` | Optional FeatBit API root URL. If omitted, use CLI default `https://app-api.featbit.co`. |
| `TOKEN` | Valid FeatBit access token. |
| `ORG` | Valid organization ID. |

The following values must be discovered dynamically by the agent during execution:

| Derived Name | How to get it |
| --- | --- |
| `PROJECT_ID` | Pick one project from `featbit project list`. |
| `ENV_ID` | Pick one environment from `featbit project get <PROJECT_ID>`. |
| `FLAG_NAME_FRAGMENT` | Optional. Pick a fragment from an existing flag name or key returned by `featbit flag list <ENV_ID>`. |

## End-to-End Discovery Flow (Required)

The agent must execute and record this exact flow in sequence. This flow is mandatory and is part of test completion criteria.

### Flow Step 1: `config init`

**Command**

```bash
featbit config init
```

**Input**

- `Host`: press Enter for default, or provide `HOST`
- `Access token`: `TOKEN`
- `Organization`: `ORG`

**Expected**

- Exit code is `0`
- Output contains `Config saved to:`

### Flow Step 2: `config set`

**Command**

```bash
featbit config set --host ${HOST} --token ${TOKEN} --org ${ORG}
```

**Expected**

- Exit code is `0`
- Output contains `Config saved to:`

### Flow Step 3: `config show` Consistency Check

**Command**

```bash
featbit config show
```

**Expected**

- Exit code is `0`
- Output host and organization match the values provided in Steps 1-2
- Token is masked

### Flow Step 4: `project list`

**Command**

```bash
featbit project list --json
```

**Expected**

- Exit code is `0`
- Output is valid JSON
- Agent extracts one valid `PROJECT_ID` from returned data

### Flow Step 5: `project get`

**Command**

```bash
featbit project get --project-id ${PROJECT_ID} --json
```

**Expected**

- Exit code is `0`
- Output is valid JSON
- Agent extracts one valid `ENV_ID` from the selected project's environments

### Flow Step 6: `flag list`

**Command**

```bash
featbit flag list --env-id ${ENV_ID} --all --json
```

**Expected**

- Exit code is `0`
- Output is valid JSON
- JSON includes flag items with update timestamp fields (if available from API)

### Flow Step 7: Stale Feature Flag Table

**Purpose**
Identify flags whose `updated` date is older than one month.

**Rule**

- Let `now` be the execution timestamp in UTC.
- A flag is `stale feature flag` if `updated_at < now - 30 days`.
- If an update timestamp is missing or unparsable, mark status as `unknown` and include a note.

**Required output table**

| project_id | env_id | flag_key | flag_name | updated_at | age_days | stale_status |
| --- | --- | --- | --- | --- | --- | --- |
| ... | ... | ... | ... | ... | ... | `stale` / `active` / `unknown` |

## Flow Test Logging Requirements

For Flow Steps 1-7, the report must include:

- exact command
- exit code
- selected IDs (`PROJECT_ID`, `ENV_ID`) with source evidence
- consistency check result for `config show`
- stale table generation result and row count

## Setup

### Case S1: Build

**Purpose**
Confirm the repository builds successfully.

**Command**

```bash
dotnet build featbit-cli.slnx -c Release
```

**Expected**

- Exit code is `0`
- No build errors are present

### Case S2: Help Surface

**Purpose**
Confirm the CLI exposes the documented commands.

**Command**

```bash
dotnet run --project src/FeatBit.Cli -- --help
```

**Expected output contains**

- `project list`
- `project get`
- `flag list`
- `config init`
- `config set`
- `config show`
- `config clear`
- `config validate`

## Configuration

### Case C1: Clear Existing Config

**Purpose**
Ensure the agent starts from a known config state.

**Command**

```bash
featbit config clear
```

**Expected**

- Exit code is `0`
- Output reports either `Config cleared.` or `Config file not found.`

### Case C2: Interactive Init

**Purpose**
Verify `config init` can create user config interactively.

**Input**

- `Host`: blank to accept default, or `HOST`
- `Access token`: `TOKEN`
- `Organization`: `ORG`

**Command**

```bash
featbit config init
```

**Expected**

- Exit code is `0`
- Output contains `Config saved to:`
- A user config file is created in the resolved user-profile path

### Case C3: Show Saved Config

**Purpose**
Verify stored config can be displayed safely.

**Command**

```bash
featbit config show
```

**Expected**

- Exit code is `0`
- Output contains `host:`
- Output contains `organization:`
- Output contains `token:`
- Token value is masked, not printed in full

### Case C4: Non-Interactive Update

**Purpose**
Verify `config set` updates individual values.

**Command**

```bash
featbit config set --host ${HOST} --token ${TOKEN} --org ${ORG}
```

If `HOST` is not provided externally, use:

```bash
featbit config set --token ${TOKEN} --org ${ORG}
```

**Expected**

- Exit code is `0`
- Output contains `Config saved to:`

## Validation

### Case V1: Positive Validation

**Purpose**
Verify current config can authenticate and call the API.

**Command**

```bash
featbit config validate
```

**Expected**

- Exit code is `0`
- Output contains `Config validation succeeded.`
- Output contains resolved host
- Output contains resolved organization or `<empty>`
- Output contains `Projects fetched:`

### Case V2: Invalid Token

**Purpose**
Verify auth failures are surfaced with machine-usable exit codes.

**Command**

```bash
featbit config validate --token invalid-token
```

**Expected**

- Output contains `Config validation failed.`
- Exit code is preferably `2`
- If backend behavior is atypical, exit code `1` is acceptable only with clear error output

### Case V3: Invalid Host

**Purpose**
Verify network failures map to the documented exit code.

**Command**

```bash
featbit config validate --host https://nonexistent.invalid
```

**Expected**

- Output contains `Config validation failed.`
- Exit code is `3` when the failure is classified as a network issue

## Business Commands

### Case P1: Project List Table Output

**Purpose**
Verify the default project list output works for humans.

**Command**

```bash
featbit project list
```

**Expected**

- Exit code is `0`
- Output contains a table header and at least one project row when projects exist

### Case P2: Project List JSON Output

**Purpose**
Verify machine-readable JSON output.

**Command**

```bash
featbit project list --json
```

**Expected**

- Exit code is `0`
- Output is valid JSON
- JSON contains `success`
- JSON contains `data`

### Case P3: Project Get Table Output

**Purpose**
Verify fetching a specific project by ID.

**Command**

```bash
featbit project get ${PROJECT_ID}
```

`PROJECT_ID` must be selected from Case P2 output.

**Expected**

- Exit code is `0`
- Output contains project name, key, ID, and environment information when available

### Case P4: Project Get JSON Output

**Purpose**
Verify JSON output for a specific project.

**Command**

```bash
featbit project get ${PROJECT_ID} --json
```

`PROJECT_ID` must be selected from Case P2 output.

**Expected**

- Exit code is `0`
- Output is valid JSON
- JSON contains the requested project record

### Case F1: Flag List Table Output

**Purpose**
Verify flag listing for an environment.

**Command**

```bash
featbit flag list ${ENV_ID}
```

`ENV_ID` must be selected from Case P4 output.

**Expected**

- Exit code is `0`
- Output contains a table header
- Output contains `TotalCount:`

### Case F2: Flag List All Pages

**Purpose**
Verify `--all` aggregates paged results.

**Command**

```bash
featbit flag list ${ENV_ID} --all
```

`ENV_ID` must be selected from Case P4 output.

**Expected**

- Exit code is `0`
- Output contains flags from the aggregated result set
- Output contains `TotalCount:`

### Case F3: Flag Filter

**Purpose**
Verify name/key filtering.

**Command**

```bash
featbit flag list ${ENV_ID} --name ${FLAG_NAME_FRAGMENT}
```

`ENV_ID` must be selected from Case P4 output.
`FLAG_NAME_FRAGMENT` should be discovered from prior flag list output.

**Expected**

- Exit code is `0`
- Output reflects a filtered result set

### Case F4: Flag List JSON Output

**Purpose**
Verify machine-readable JSON output for flags.

**Command**

```bash
featbit flag list ${ENV_ID} --json
```

`ENV_ID` must be selected from Case P4 output.

**Expected**

- Exit code is `0`
- Output is valid JSON
- JSON contains `totalCount`
- JSON contains `items`

## Input Validation

### Case N1: Invalid Project ID

**Purpose**
Verify invalid GUID input is rejected before API invocation.

**Command**

```bash
featbit project get invalid-guid
```

**Expected**

- Exit code is non-zero
- Output explains that `projectId` must be a valid GUID

### Case N2: Invalid Environment ID

**Purpose**
Verify invalid GUID input is rejected before API invocation.

**Command**

```bash
featbit flag list invalid-guid
```

**Expected**

- Exit code is non-zero
- Output explains that `envId` must be a valid GUID

## Report Format

For each case, the agent should produce:

| Field | Meaning |
| --- | --- |
| `case_id` | Test case identifier, for example `V1` |
| `status` | `passed`, `failed`, or `blocked` |
| `command` | Exact executed command |
| `exit_code` | Observed process exit code |
| `observed` | Short factual summary |
| `evidence` | Relevant output snippet |

For End-to-End Discovery Flow, also include:

| Field | Meaning |
| --- | --- |
| `flow_step` | One of `1..7` |
| `derived_values` | Values discovered in this step (for example `PROJECT_ID`, `ENV_ID`) |
| `stale_table_path_or_inline` | Path to generated table artifact or inline markdown table |

## Completion Criteria

The test story is complete when:

1. Setup cases pass.
2. Configuration cases pass.
3. Validation cases behave as documented.
4. Project and flag commands succeed against a real environment.
5. Negative cases produce understandable failures.
6. End-to-end discovery flow (Steps 1-7) is fully executed and recorded.
7. A stale feature flag table is generated with clear `stale`/`active`/`unknown` status.
8. The agent report is complete and reproducible.