# AI Agent Test Specification — Write Commands

This story validates the five write commands added in the second CLI implementation phase.
Run it **after** `AI_AGENT_TEST_STORY.md` has passed, or at least after Setup and Configuration cases from that story have passed (config must be set and validated before this story begins).

## Objective

Prove that `featbit-cli` can safely and correctly:

- create a feature flag
- toggle a feature flag on and off
- update a feature flag's default rollout
- evaluate feature flags for a given user
- archive a feature flag

## Execution Rules

1. Treat this file as the source of truth for agent-driven validation.
2. Execute the End-to-End Write Flow in order — it is mandatory.
3. Individual test cases in later sections may be run in any order, but depend on the write flow having completed.
4. Record pass/fail and any captured output per case.
5. Stop only when a blocking failure makes later cases meaningless.
6. Prefer running the built CLI exactly as an end user would.

## Shared Inputs

These must be supplied before execution begins:

| Name | Description |
| --- | --- |
| `HOST` | Optional FeatBit API root URL. Omit to use CLI default (`https://app-api.featbit.co`). |
| `TOKEN` | Valid FeatBit access token. |
| `ORG` | Valid organization ID. |
| `ENV_SECRET` | Environment secret key (`X-FeatBit-Env-Secret`) for the target environment. Required for `flag evaluate`. Find it in the FeatBit console → Project → Environments → Client Key or Server Key. |

The following values must be discovered dynamically by the agent:

| Derived Name | How to get it |
| --- | --- |
| `ENV_ID` | Pick one environment from `featbit project get <PROJECT_ID> --json`. Same as in the prerequisite story. |
| `TEST_FLAG_KEY` | Generate a key the agent has not seen before, e.g. `cli-e2e-<yyyyMMdd-HHmmss>`. This key is used throughout the write flow and must remain consistent for the full run. |
| `VARIATION_ID_TRUE` | Extract from the JSON response of `flag create --json`. The variation whose `value` is `"true"`. |
| `VARIATION_ID_FALSE` | Extract from the JSON response of `flag create --json`. The variation whose `value` is `"false"`. |
| `EVAL_HOST` | Optional. If the FeatBit evaluation service is hosted separately from the admin API, supply this URL. Otherwise omit — the CLI defaults `--eval-host` to `--host`. |

## End-to-End Write Flow (Required)

The agent must execute and record all steps in order. This flow constitutes the main lifecycle test of the new commands.

### Flow Step 1: Discover `ENV_ID`

**Purpose**
Obtain the environment ID required by all flag write commands.

**Commands**

```bash
featbit project list --json
featbit project get <PROJECT_ID> --json
```

**Expected**

- Both commands exit with code `0`.
- Agent extracts `ENV_ID` from the environments array in the `project get` response.

**Record**

- `PROJECT_ID` chosen and from which field it was read.
- `ENV_ID` chosen and from which environment name/key.

---

### Flow Step 2: `flag create`

**Purpose**
Create a new boolean feature flag that will be used throughout the remaining steps.

**Command**

```bash
featbit flag create ${ENV_ID} \
  --flag-name "CLI E2E Test Flag" \
  --flag-key ${TEST_FLAG_KEY} \
  --description "Created by AI agent write-command e2e test" \
  --json
```

**Expected**

- Exit code is `0`.
- Output is valid JSON.
- Agent extracts `VARIATION_ID_TRUE` and `VARIATION_ID_FALSE` from the returned flag's `variations` array (the variation whose `value` equals `"true"` and the one whose `value` equals `"false"`, respectively).

**Record**

- `TEST_FLAG_KEY` used.
- `VARIATION_ID_TRUE` and `VARIATION_ID_FALSE` with source evidence (the raw JSON property path where each was found).

---

### Flow Step 3: Confirm Flag Appears in List

**Purpose**
Verify the newly created flag is visible in `flag list` output.

**Command**

```bash
featbit flag list ${ENV_ID} --name ${TEST_FLAG_KEY}
```

**Expected**

- Exit code is `0`.
- Output contains `${TEST_FLAG_KEY}`.
- The `Enabled` column shows `off` (flags are created disabled).

---

### Flow Step 4: `flag toggle` — Enable

**Purpose**
Enable the newly created flag.

**Command**

```bash
featbit flag toggle ${ENV_ID} ${TEST_FLAG_KEY} true
```

**Expected**

- Exit code is `0`.
- Output contains `enabled`.

---

### Flow Step 5: `flag toggle` — Disable

**Purpose**
Disable the flag to verify the toggle works in both directions.

**Command**

```bash
featbit flag toggle ${ENV_ID} ${TEST_FLAG_KEY} false
```

**Expected**

- Exit code is `0`.
- Output contains `disabled`.

---

### Flow Step 6: `flag set-rollout`

**Purpose**
Update the flag's default rollout (fallthrough) so that 70 % of traffic receives `true` and 30 % receives `false`.

**Command**

```bash
featbit flag set-rollout ${ENV_ID} ${TEST_FLAG_KEY} \
  --rollout "[{\"variationId\":\"${VARIATION_ID_TRUE}\",\"percentage\":70},{\"variationId\":\"${VARIATION_ID_FALSE}\",\"percentage\":30}]"
```

**Expected**

- Exit code is `0`.
- Output confirms the rollout was updated (e.g., contains `updated successfully`).

---

### Flow Step 7: Re-enable the Flag Before Evaluation

**Purpose**
The evaluation endpoint returns the flag's served variation only when the flag is enabled.

**Command**

```bash
featbit flag toggle ${ENV_ID} ${TEST_FLAG_KEY} true
```

**Expected**

- Exit code is `0`.

---

### Flow Step 8: `flag evaluate`

**Purpose**
Evaluate the test flag for a synthetic end user and confirm a variation is returned.

**Command**

```bash
featbit flag evaluate \
  --user-key "e2e-test-user-001" \
  --user-name "E2E Test User" \
  --flag-keys ${TEST_FLAG_KEY} \
  --env-secret ${ENV_SECRET} \
  --json
```

If `EVAL_HOST` differs from `HOST`, append `--eval-host ${EVAL_HOST}`.

**Expected**

- Exit code is `0`.
- Output is valid JSON.
- JSON contains an entry for `${TEST_FLAG_KEY}`.
- The entry includes a non-empty `variation` field (either `"true"` or `"false"`).

---

### Flow Step 9: `flag archive`

**Purpose**
Archive the test flag to clean up after the test run.

**Command**

```bash
featbit flag archive ${ENV_ID} ${TEST_FLAG_KEY}
```

**Expected**

- Exit code is `0`.
- Output contains `archived`.

### Flow Step 10: Confirm Flag Is No Longer in Default List

**Purpose**
Archived flags must not appear in the default (non-archived) flag listing.

**Command**

```bash
featbit flag list ${ENV_ID} --name ${TEST_FLAG_KEY}
```

**Expected**

- Exit code is `0`.
- Output does **not** contain `${TEST_FLAG_KEY}` in the rows (or shows `TotalCount: 0`).

---

## Individual Test Cases

### Case FW1: `flag create` Human-Readable Output

**Purpose**
Verify the non-JSON output path for flag creation.

**Command**

```bash
featbit flag create ${ENV_ID} \
  --flag-name "CLI Human Output Test" \
  --flag-key cli-human-test-$(date +%s)
```

**Expected**

- Exit code is `0`.
- Output contains `created successfully`.
- No raw JSON is printed.

> Note: The flag created here is a side-effect. Archive it manually or treat it as disposable.

---

### Case FW2: `flag toggle` JSON Output

**Purpose**
Verify `--json` flag works for toggle.

**Command**

```bash
featbit flag toggle ${ENV_ID} ${TEST_FLAG_KEY} true --json
```

Run this before Flow Step 9 (archiving).

**Expected**

- Exit code is `0`.
- Output is valid JSON.

---

### Case FW3: `flag set-rollout` with `--dispatch-key`

**Purpose**
Verify consistent bucketing by a user attribute is accepted.

**Command**

```bash
featbit flag set-rollout ${ENV_ID} ${TEST_FLAG_KEY} \
  --rollout "[{\"variationId\":\"${VARIATION_ID_TRUE}\",\"percentage\":50},{\"variationId\":\"${VARIATION_ID_FALSE}\",\"percentage\":50}]" \
  --dispatch-key "email"
```

Run this before Flow Step 9 (archiving).

**Expected**

- Exit code is `0`.
- Output confirms the rollout was updated.

---

### Case FW4: `flag evaluate` with Custom Properties

**Purpose**
Verify custom targeting properties are passed correctly.

**Command**

```bash
featbit flag evaluate \
  --user-key "e2e-props-user" \
  --custom-props "[{\"name\":\"country\",\"value\":\"US\"},{\"name\":\"plan\",\"value\":\"pro\"}]" \
  --flag-keys ${TEST_FLAG_KEY} \
  --env-secret ${ENV_SECRET} \
  --json
```

Run this before Flow Step 9 (archiving).

**Expected**

- Exit code is `0`.
- Output is valid JSON containing an evaluation result for `${TEST_FLAG_KEY}`.

---

### Case FW5: `flag evaluate` with Tag Filter

**Purpose**
Verify tag-based flag filtering in evaluation.

**Command**

```bash
featbit flag evaluate \
  --user-key "e2e-tag-user" \
  --env-secret ${ENV_SECRET} \
  --tags "nonexistent-tag-xyz" \
  --tag-filter and \
  --json
```

**Expected**

- Exit code is `0`.
- Output is valid JSON.
- The result set may be empty (`[]`) since no flags carry the tag — this is correct behavior.

---

### Case FW6: `flag archive` JSON Output

**Purpose**
Verify `--json` flag works for archive.

This must run **as part of Flow Step 9 or after creating a separate disposable flag**.

**Command**

```bash
featbit flag archive ${ENV_ID} ${TEST_FLAG_KEY} --json
```

**Expected**

- Exit code is `0`.
- Output is valid JSON.

---

## Input Validation

### Case N3: `flag toggle` — Invalid Status Value

**Purpose**
Verify the parser rejects status values that are neither `true` nor `false`.

**Command**

```bash
featbit flag toggle ${ENV_ID} ${TEST_FLAG_KEY} yes
```

**Expected**

- Exit code is non-zero.
- Output explains that status must be `'true'` or `'false'`.
- No API call is made.

---

### Case N4: `flag toggle` — Invalid `envId`

**Purpose**
Verify GUID validation for `envId`.

**Command**

```bash
featbit flag toggle not-a-guid my-flag true
```

**Expected**

- Exit code is non-zero.
- Output explains that `envId` must be a valid GUID.

---

### Case N5: `flag create` — Missing `--flag-name`

**Purpose**
Verify required option validation for flag creation.

**Command**

```bash
featbit flag create ${ENV_ID} --flag-key orphan-key
```

**Expected**

- Exit code is non-zero.
- Output mentions that `--flag-name` is required.

---

### Case N6: `flag create` — Missing `--flag-key`

**Purpose**
Verify required option validation for flag creation.

**Command**

```bash
featbit flag create ${ENV_ID} --flag-name "Orphan Flag"
```

**Expected**

- Exit code is non-zero.
- Output mentions that `--flag-key` is required.

---

### Case N7: `flag set-rollout` — Percentages Do Not Sum to 100

**Purpose**
Verify client-side validation rejects rollout assignments that do not sum to 100.

**Command**

```bash
featbit flag set-rollout ${ENV_ID} ${TEST_FLAG_KEY} \
  --rollout "[{\"variationId\":\"${VARIATION_ID_TRUE}\",\"percentage\":60},{\"variationId\":\"${VARIATION_ID_FALSE}\",\"percentage\":20}]"
```

**Expected**

- Exit code is non-zero.
- Output contains a message about percentages summing to 100.
- No API call is made (validation happens client-side).

---

### Case N8: `flag set-rollout` — Missing `--rollout`

**Purpose**
Verify that `--rollout` is required.

**Command**

```bash
featbit flag set-rollout ${ENV_ID} ${TEST_FLAG_KEY}
```

**Expected**

- Exit code is non-zero.
- Output explains that `--rollout` is required.

---

### Case N9: `flag evaluate` — Missing `--user-key`

**Purpose**
Verify that `--user-key` is required.

**Command**

```bash
featbit flag evaluate --env-secret ${ENV_SECRET}
```

**Expected**

- Exit code is non-zero.
- Output explains that `--user-key` is required.

---

### Case N10: `flag evaluate` — Missing `--env-secret`

**Purpose**
Verify that `--env-secret` is required.

**Command**

```bash
featbit flag evaluate --user-key test-user
```

**Expected**

- Exit code is non-zero.
- Output explains that `--env-secret` is required.

---

### Case N11: `flag archive` — Invalid `envId`

**Purpose**
Verify GUID validation for archive.

**Command**

```bash
featbit flag archive not-a-guid my-flag
```

**Expected**

- Exit code is non-zero.
- Output explains that `envId` must be a valid GUID.

---

## Help Surface Check

### Case S3: Updated Help Output

**Purpose**
Confirm the CLI help now surfaces all five new commands.

**Command**

```bash
featbit --help
```

**Expected output contains all of:**

- `flag toggle`
- `flag archive`
- `flag create`
- `flag set-rollout`
- `flag evaluate`
- `--flag-name`
- `--flag-key`
- `--rollout`
- `--user-key`
- `--env-secret`

---

## Flow Test Logging Requirements

For each write flow step (1–10), the report must include:

- Exact command executed
- Exit code observed
- Derived values extracted in that step (with the JSON path or field name they came from)
- Whether the output matched the expected shape

---

## Report Format

For each case, the agent should produce:

| Field | Meaning |
| --- | --- |
| `case_id` | Test case identifier, e.g. `FW1` |
| `status` | `passed`, `failed`, or `blocked` |
| `command` | Exact executed command (with substituted values) |
| `exit_code` | Observed process exit code |
| `observed` | Short factual summary |
| `evidence` | Relevant output snippet |

For End-to-End Write Flow steps also include:

| Field | Meaning |
| --- | --- |
| `flow_step` | One of `1..10` |
| `derived_values` | Values discovered in this step (e.g. `TEST_FLAG_KEY`, `VARIATION_ID_TRUE`) |

---

## Completion Criteria

This test story is complete when:

1. All ten write flow steps are executed in order and recorded.
2. `TEST_FLAG_KEY`, `VARIATION_ID_TRUE`, and `VARIATION_ID_FALSE` are correctly extracted in Flow Steps 1–2.
3. The full flag lifecycle (create → toggle on → toggle off → set-rollout → evaluate → archive) completes with exit code `0` at each step.
4. Evaluation returns a valid variation for the test user and the test flag key.
5. The archived flag no longer appears in the default `flag list` output (Flow Step 10).
6. All individual test cases (FW1–FW6) pass or are explicitly documented as blocked with a reason.
7. All negative cases (N3–N11) produce non-zero exit codes with descriptive error messages.
8. The help surface check (S3) confirms all new commands and key options are documented.
9. The agent report is complete and reproducible.
