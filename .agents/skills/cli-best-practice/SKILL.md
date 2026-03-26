---
name: cli-best-practice
description: Design and evaluate CLIs so that AI agents and automation pipelines can use them reliably.
license: MIT
metadata:
  version: 1.0.0
  tags: [cli, agent, tooling, dx, automation]
  applies-to: any CLI that an AI agent, script, or pipeline will invoke
---

# Skill: CLI Best Practice for Agent Use

## Purpose

Use this skill when:

- Designing a new CLI command or subcommand.
- Reviewing an existing CLI for agent-friendliness before publishing it.
- Deciding how flags, output, and error messages should behave.
- Writing a test story or agent workflow that exercises a CLI.

---

## Execution Procedure

```python
def cli_best_practice(target):
    mode = detect_mode(target)
    # "design" — building or modifying a CLI command or subcommand
    # "review" — evaluating an existing CLI for agent-friendliness
    # "test"   — writing an agent test story or pipeline workflow for a CLI

    if mode == "design":
        # Apply Core Rules 1–10 to every command being designed
        for rule in CORE_RULES:
            apply_rule(rule, target)
        return design_recommendations(target)      # rule-tagged actionable suggestions

    if mode == "review":
        # Walk every subcommand against the Evaluation Checklist
        findings = run_checklist(target)           # 15 checks, pass / fail / warn
        antipatterns = detect_antipatterns(target) # Anti-Patterns table
        report_findings(findings, antipatterns)    # grouped by severity
        return findings_report(target)

    if mode == "test":
        # Help structure test stories that exercise a CLI end-to-end
        apply_relevant_rules(target)               # Rules 1, 4, 5, 9, 10
        return test_story_template(target)
```

---

## Core Rules

### Rule 1 — Non-Interactive by Default

Every required input must be expressible as a flag. Interactive prompts are a fallback for humans; they block agents unconditionally.

**How to apply**

- Accept all required values as flags (e.g. `--env`, `--token`, `--tag`).
- Only drop into interactive mode when a required flag is missing **and** a TTY is detected.
- Never drop into interactive mode mid-execution after the command has started running.

**Good**

```bash
$ mycli deploy --env staging --tag v1.2.3
```

**Bad** (blocks an agent that cannot navigate arrow-key menus)

```bash
$ mycli deploy
? Which environment? (use arrow keys)
```

---

### Rule 2 — Scoped Help (No Upfront Context Dump)

Agents discover commands incrementally. Dumping all documentation on the first invocation wastes context and slows discovery.

**How to apply**

- Top-level `--help` lists only subcommands with one-line descriptions.
- Each subcommand has its own `--help` covering only its own flags and examples.
- Never print deeply nested help trees unless the user explicitly requests them.

**Discovery sequence an agent follows**

```bash
$ mycli             # learn subcommands
$ mycli deploy --help    # learn options for 'deploy' only
$ mycli deploy --env staging --tag v1.2.3
```

---

### Rule 3 — --help Includes Examples

Descriptions tell an agent what a flag does. Examples show an agent how to combine flags into a working invocation. Agents pattern-match off examples faster than they parse prose.

**Required structure for every `--help`**

```
Options:
  --env     Target environment (staging, production)
  --tag     Image tag (default: latest)
  --force   Skip confirmation

Examples:
  mycli deploy --env staging
  mycli deploy --env production --tag v1.2.3
  mycli deploy --env staging --force
```

**Rules**

- Include at least two examples per subcommand.
- The first example uses the minimum required flags.
- Subsequent examples illustrate common non-default combinations.
- If a flag accepts an enum, list all valid values in the description.

---

### Rule 4 — Accept Flags and stdin for Everything

Agents think in pipelines. They pass output from one command as input to the next. Positional-argument-only or interactive-only interfaces break composition.

**How to apply**

- Prefer named flags over positional arguments (or support both).
- Support `--stdin` (or `-`) to read structured input from a pipe.
- Accept computed values via command substitution.

```bash
# pipe-friendly
cat config.json | mycli config import --stdin

# command substitution
mycli deploy --env staging --tag $(mycli build --output tag-only)
```

---

### Rule 5 — Fail Fast with Actionable Errors

When a required flag is missing or invalid, exit immediately with a non-zero code, a clear English sentence, the correct invocation, and (where useful) how to find missing values.

**Error message template**

```
Error: <what is missing or wrong>
  <correct invocation>
  <how to get the missing value, if applicable>
```

**Example**

```
Error: No image tag specified.
  mycli deploy --env staging --tag <image-tag>
  Available tags: mycli build list --output tags
```

**Rules**

- Exit code must be non-zero (use `1` for user errors, `2` for misuse of CLI, `>= 3` for unexpected failures).
- Never hang or wait for stdin when a required flag is absent.
- Print errors to `stderr`, not `stdout`.

---

### Rule 6 — Idempotent Commands

Agents retry on timeouts and transient failures. Running the same command twice must produce the same observable result—never a duplicate or a crash.

**How to apply**

- For create operations: if the resource already exists, return success and report "no-op" rather than an error.
- For deploy operations: detect that the target state is already reached and short-circuit.
- Document idempotency guarantees in `--help`.

**Example output for a no-op retry**

```
service 'my-service' already exists — no changes made.
```

---

### Rule 7 — --dry-run for Destructive Actions

Agents should validate a plan before committing to it. Any command that modifies state must support `--dry-run`.

**Behaviour contract**

- `--dry-run` prints exactly what *would* happen and exits `0`.
- No state is mutated when `--dry-run` is set.
- The output must be deterministic enough for the agent to confirm the plan.

```bash
$ mycli deploy --env production --tag v1.2.3 --dry-run
Would deploy v1.2.3 to production
  - Stop 3 running instances
  - Pull image registry.io/app:v1.2.3
  - Start 3 new instances
No changes made.

$ mycli deploy --env production --tag v1.2.3
Deployed v1.2.3 to production
url: https://production.myapp.com
deploy_id: dep_abc123
duration: 34s
```

---

### Rule 8 — --yes / --force to Skip Confirmations

Confirmation prompts exist for human safety. Agents bypass them with a flag, not by attempting to simulate keystrokes.

**How to apply**

- Default to prompting for any destructive action when a TTY is present.
- When `--yes` or `--force` is set, skip all confirmation prompts unconditionally.
- Document in `--help` what `--yes` / `--force` bypasses.

```bash
$ mycli env delete --env staging --yes
Deleted environment 'staging'.
```

---

### Rule 9 — Predictable Command Structure

Once an agent learns one command pattern, it predicts others. Inconsistency forces the agent to read every `--help`, increasing latency and error rate.

**Pick a single pattern and apply it everywhere**

| Pattern | Example |
|---|---|
| `<resource> <verb>` (preferred) | `mycli service list` → `mycli deploy list` → `mycli config list` |
| `<verb> <resource>` | `mycli list service`, `mycli create service` |

**Rules**

- Choose one pattern and never mix them.
- The same verb (`list`, `get`, `create`, `delete`, `update`) behaves the same across all resources.
- Common flags (`--json`, `--env`, `--yes`, `--dry-run`) are available on every subcommand that logically needs them.

---

### Rule 10 — Return Data on Success

Agents parse output. Emojis are decorative and add noise. Return structured, parseable data for every successful operation.

**How to apply**

- Support `--json` on every command that returns data.
- In `--json` mode, write only valid JSON to `stdout` and all logs/warnings to `stderr`.
- In human mode, print one fact per line in `key: value` format.

```bash
# human mode
deployed v1.2.3 to staging
url: https://staging.myapp.com
deploy_id: dep_abc123
duration: 34s

# --json mode
{
  "version": "v1.2.3",
  "environment": "staging",
  "url": "https://staging.myapp.com",
  "deploy_id": "dep_abc123",
  "duration_seconds": 34
}
```

---

## Evaluation Checklist

Use this checklist to score an existing CLI or review a new one before shipping.

| # | Check | Pass condition |
|---|---|---|
| 1 | Every required input is a flag | No required positional arg that has no flag equivalent |
| 2 | No mid-execution interactive prompts | `strace` / process tracing shows no `read()` on TTY after startup |
| 3 | Top-level help is short | Lists subcommands only; ≤ 30 lines |
| 4 | Every subcommand has `--help` | `<cmd> --help` exits 0 and prints options |
| 5 | `--help` includes examples | At least 2 examples, first uses minimum flags |
| 6 | stdin supported where applicable | `echo '...' | <cmd> --stdin` works |
| 7 | Missing required flag exits non-zero immediately | `echo $?` is non-zero; no hang |
| 8 | Error message is actionable | Contains correct invocation |
| 9 | Errors go to stderr | `<cmd> 2>/dev/null` produces no error text on stdout |
| 10 | Repeated execution is idempotent | Running command twice produces same exit code and meaningful output |
| 11 | `--dry-run` exists on destructive commands | Exits 0, no state mutation |
| 12 | `--yes` / `--force` skips confirmations | No prompt when flag is set |
| 13 | Command pattern is consistent | All resources use same verb ordering |
| 14 | `--json` flag available on data commands | Output is valid JSON; parseable by `jq .` |
| 15 | Success output includes relevant IDs and URLs | Deploy ID, resource URL, etc. returned |

---

## Anti-Patterns

| Anti-pattern | Why it breaks agents | Fix |
|---|---|---|
| Arrow-key menus | Agents have no TTY input mechanism | Replace with `--flag` for each option |
| Paginated output requiring keypress | Agent sees only first page | Support `--all` or `--limit N` |
| Output mixed to stdout/stderr arbitrarily | Pipes break; JSON is corrupted | Structured data → stdout; logs → stderr |
| `--json` outputs pretty-print with ANSI colour codes | `jq` and JSON parsers reject ANSI escape sequences | Strip colour in non-TTY mode |
| Positional args in non-obvious order | Agent guesses wrong order | Use named flags; document order explicitly if positional args are unavoidable |
| Version-dependent flag names | Agent prompt learned old flags | Follow semver; deprecate with a warning before removing |
| Silent success (exit 0, no output) | Agent cannot confirm the operation succeeded | Always print at least one confirmation line |
| Ambiguous exit codes (everything is 0 or 1) | Agent cannot distinguish "not found" from "server error" | Define and document an exit code table |
