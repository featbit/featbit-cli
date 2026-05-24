---
applyTo: "**"
---

# Coding Guidelines

## Technology Stack

1. Use **.NET 10** as the target framework for all projects.
2. **FeatBit.Cli** is an **AOT (Ahead-of-Time compilation)** project (`<PublishAot>true</PublishAot>`). All code must be AOT-compatible: avoid reflection, dynamic types, and non-trimmer-friendly patterns.

## Documentation & Research

When uncertain about .NET, C#, or any Microsoft technology (APIs, libraries, SDK behavior, best practices, etc.), always search the official Microsoft documentation first using the `#microsoftdocs` MCP tool before answering or generating code. This ensures the information is accurate and up-to-date.

## FeatBit CLI Test Environment

1. Use the dedicated FeatBit test project for live CLI validation:
   - Project key: `featbit-cli-testing`
   - Host: `https://app-api.featbit.co`
   - Evaluation host: `https://app-eval.featbit.co`
2. The test project ID and access token are stored only in local CLI config or user-provided runtime context. Do not commit project IDs, access tokens, environment secrets, or full Authorization values to repository files or test reports.
3. The test token is scoped to the `featbit-cli-testing` project. Do not use other projects for live tests unless the user explicitly asks.

## Test Reports

1. Every AI-agent test run must generate a Markdown report under `tests/reports/`.
2. Report filenames must be timestamped and descriptive, for example `tests/reports/20260524-161500-read-commands.md`.
3. Each report must include:
   - execution timestamp
   - CLI version or commit SHA if available
   - project key and redacted project identifier
   - exact commands executed, with secrets redacted
   - exit code for every command
   - selected environment ID and flag key/ID when applicable
   - pass/fail/blocked status for each case
   - short evidence snippets with tokens and secrets redacted
4. If a test case cannot run because the project has no flags or no environment secret is available, mark it as `blocked` with the concrete reason instead of silently skipping it.

## CLI Test Completeness Criteria

1. Do not treat an empty project as sufficient coverage. If the project has no flags, create disposable test flags during the run and clean them up by archiving them.
2. A complete read/write test run must cover all available CLI command families:
   - `config set`, `config show`, and `config validate`
   - `project list`, `project get`, and `project flags`
   - `flag list`, including pagination, `--all`, name/key filtering, JSON output, and tag visibility
   - `flag create`, including `--description`, `--tags`, and JSON output
   - `flag toggle` for both enable and disable
   - `flag set-rollout`, including validation that variation IDs from create are used
   - `flag evaluate`, including `--flag-keys`, `--tags`, and `--tag-filter`, when an environment secret is available
   - `flag audit-logs`, by both `--flag-id` and `--flag-key`
   - `flag archive`, followed by confirmation that the archived flag no longer appears in the default list
3. The report must name every disposable feature flag created during the run, including flag key, flag ID if available, environment ID, tags, variation IDs, and cleanup status.
4. The report must distinguish `passed`, `failed`, and `blocked`. A blocked case must include the exact missing prerequisite, such as `environment secret unavailable`.
5. Negative/input-validation tests must be included for representative parser failures, including invalid GUIDs, missing required flags, invalid boolean values, and invalid rollout percentages.
6. Reports may include evidence snippets, but must redact access tokens, environment secrets, and Authorization headers.
