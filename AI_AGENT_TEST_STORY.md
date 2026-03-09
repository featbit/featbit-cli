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
| `HOST` | FeatBit API root URL. Default is `https://app-api.featbit.co`. |
| `TOKEN` | Valid FeatBit access token. |
| `ORG` | Valid organization ID. |
| `PROJECT_ID` | Valid project ID for read tests. |
| `ENV_ID` | Valid environment ID for flag list tests. |
| `FLAG_NAME_FRAGMENT` | Optional known fragment for filtering flag list results. |

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

## Completion Criteria

The test story is complete when:

1. Setup cases pass.
2. Configuration cases pass.
3. Validation cases behave as documented.
4. Project and flag commands succeed against a real environment.
5. Negative cases produce understandable failures.
6. The agent report is complete and reproducible.