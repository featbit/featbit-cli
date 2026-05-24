# AI Agent Test Report - featbit-cli-testing

## Summary

- Status: passed with one documented configuration finding
- Timestamp: 2026-05-24
- Scope: full live CLI validation against the dedicated FeatBit test project
- Project key: `featbit-cli-testing`
- Host: `https://app-api.featbit.co`
- Evaluation host used for passing evaluation tests: `https://app-eval.featbit.co`
- Token handling: access token and environment secret were read from local config/API and redacted from this report

## Environment

| Name | Value |
| --- | --- |
| Project | `FeatBit Cli Testing` |
| Project key | `featbit-cli-testing` |
| Prod env ID | `41962905-e3d1-4ee7-bd07-3d269b481d55` |
| Dev env ID | `b9085e64-8926-4a15-b7d4-8896143d1394` |
| Test env used | Dev |
| Environment secret | available, redacted |

## Disposable Flags

| Purpose | Flag key | Flag ID | Env ID | Tags | Variation IDs | Cleanup |
| --- | --- | --- | --- | --- | --- | --- |
| Main read/write/audit flow | `cli-e2e-manual-20260524-170000` | `019e593d-3153-713f-a9bc-4543ff388ca2` | `b9085e64-8926-4a15-b7d4-8896143d1394` | `cli`, `e2e` | true: `eb4e549a-63de-4b9a-94b2-aedf352b9295`; false: `6e8d9159-00e8-446f-a192-d9c6381261c5` | archived, confirmed absent from default list |
| Evaluation host flow | `cli-eval-20260524-172253` | `019e594b-7084-75b0-ac38-9d0a63ea15a4` | `b9085e64-8926-4a15-b7d4-8896143d1394` | `cli`, `e2e`, `eval` | true: `54be4519-dc8f-4dfb-b34a-feb3f83d5dcd`; false: `369798f3-547d-45d3-83f5-3da5aa1b0c18` | archived, confirmed absent from default list |

Final cleanup check:

```text
featbit flag list --env-id b9085e64-8926-4a15-b7d4-8896143d1394 --all --json
=> totalCount: 0
```

## Build And Help

| Case | Command | Exit code | Status | Details |
| --- | --- | ---: | --- | --- |
| S1 | `dotnet build featbit-cli.slnx -c Release --no-restore` | 0 | passed | Build completed with 0 warnings and 0 errors. |
| S2 | `featbit flag create --help` | 0 | passed | Help includes `--tags <tags>` and JSON example. |

## Config And Project Commands

| Case | Command | Exit code | Status | Details |
| --- | --- | ---: | --- | --- |
| C1 | `featbit config show` | 0 | passed | Host was `https://app-api.featbit.co`; token was masked as `api-...tlSg`; org was present. |
| C2 | `featbit config validate` | 0 | passed | Validation succeeded and returned `Projects fetched: 1`, proving token scope is limited to the test project. |
| P1 | `featbit project list --json` | 0 | passed | Returned only `FeatBit Cli Testing` with key `featbit-cli-testing`. |
| P2 | `featbit project get --project-id <redacted> --json` | 0 | passed | Returned Prod and Dev environments. |

## Flag Read Commands

| Case | Command | Exit code | Status | Details |
| --- | --- | ---: | --- | --- |
| F1 | `featbit flag list --env-id b9085e64-8926-4a15-b7d4-8896143d1394 --name cli-e2e-manual-20260524-170000 --json` | 0 | passed | Returned exactly 1 flag; JSON included `tags: ["cli","e2e"]`, variations, description, and timestamps. |
| F2 | `featbit flag list --env-id b9085e64-8926-4a15-b7d4-8896143d1394 --tags cli --json` | 0 | passed | Tag filter returned the test flag with complete `cli` tag. |
| F3 | `featbit flag list --env-id b9085e64-8926-4a15-b7d4-8896143d1394 --all` | 0 | passed | Table output showed the flag, enabled state `off`, type `boolean`, and tags `cli,e2e`. |
| F4 | `featbit project flags --project-id <redacted> --tags cli --all --json` | 0 | passed | Project-wide query returned Prod with 0 flags and Dev with the tagged test flag. |

## Flag Write Commands

| Case | Command | Exit code | Status | Details |
| --- | --- | ---: | --- | --- |
| W1 | `featbit flag create --env-id b9085e64-8926-4a15-b7d4-8896143d1394 --flag-name "CLI E2E Test Flag" --flag-key cli-e2e-manual-20260524-170000 --description "Created by AI agent write-command e2e test" --tags cli,e2e --json` | 0 | passed | Created boolean flag with `cli` and `e2e` tags and true/false variations. |
| W2 | `featbit flag toggle --env-id b9085e64-8926-4a15-b7d4-8896143d1394 --flag-key cli-e2e-manual-20260524-170000 --enabled true` | 0 | passed | Output confirmed flag enabled. |
| W3 | `featbit flag toggle --env-id b9085e64-8926-4a15-b7d4-8896143d1394 --flag-key cli-e2e-manual-20260524-170000 --enabled false` | 0 | passed | Output confirmed flag disabled. |
| W4 | `featbit flag set-rollout --env-id b9085e64-8926-4a15-b7d4-8896143d1394 --flag-key cli-e2e-manual-20260524-170000 --rollout <70/30 variation JSON>` | 0 | passed | Rollout updated using true variation `eb4e...9295` at 70% and false variation `6e8...61c5` at 30%. |
| W5 | `featbit flag archive --env-id b9085e64-8926-4a15-b7d4-8896143d1394 --flag-key cli-e2e-manual-20260524-170000 --json` | 0 | passed | API returned `data: true`. Follow-up list by name returned `totalCount: 0`. |

## Evaluation Commands

| Case | Command | Exit code | Status | Details |
| --- | --- | ---: | --- | --- |
| E1 | `featbit flag evaluate --user-key cli-e2e-user-001 --flag-keys cli-e2e-manual-20260524-170000 --env-secret <redacted> --json` | 1 | finding | Default eval host fell back to management host and returned HTTP 404. This is a configuration/documentation issue, not a feature flag data failure. |
| E2 | `featbit flag evaluate --eval-host https://app-eval.featbit.co --user-key cli-eval-user-001 --flag-keys cli-eval-20260524-172253 --env-secret <redacted> --json` | 0 | passed | Returned the eval test flag with variation `true` and match reason `default`. |
| E3 | `featbit flag evaluate --eval-host https://app-eval.featbit.co --user-key cli-eval-tag-user --tags eval --tag-filter or --env-secret <redacted> --json` | 0 | passed | Tag-filtered evaluation returned `cli-eval-20260524-172253` through the `eval` tag. |

## Audit Log Commands

| Case | Command | Exit code | Status | Details |
| --- | --- | ---: | --- | --- |
| A1 | `featbit flag audit-logs --env-id b9085e64-8926-4a15-b7d4-8896143d1394 --flag-id 019e593d-3153-713f-a9bc-4543ff388ca2 --all --json` | 0 | passed | Returned 5 logs for `FeatureFlag`, including Create, TurnFlagOn, TurnFlagOff, rollout update, and final TurnFlagOn. |
| A2 | `featbit flag audit-logs --env-id b9085e64-8926-4a15-b7d4-8896143d1394 --flag-key cli-e2e-manual-20260524-170000 --page-size 3` | 0 | passed | Key-based lookup resolved the flag and table output showed the latest 3 audit entries with `TotalCount: 5`. |
| A3 | `featbit flag audit-logs --env-id b9085e64-8926-4a15-b7d4-8896143d1394 --flag-id 019e594b-7084-75b0-ac38-9d0a63ea15a4 --all --json` | 0 | passed | Returned 2 logs for the evaluation flag: Create and TurnFlagOn. |

## Negative Tests

| Case | Command | Exit code | Status | Details |
| --- | --- | ---: | --- | --- |
| N1 | `featbit flag list --env-id invalid-guid` | 1 | passed | Parser rejected invalid GUID: `--env-id must be a valid GUID.` |
| N2 | `featbit flag toggle --env-id b9085e64-8926-4a15-b7d4-8896143d1394 --flag-key cli-e2e-manual-20260524-170000 --enabled yes` | 1 | passed | Parser rejected invalid boolean: `--enabled must be 'true' or 'false'.` |
| N3 | `featbit flag set-rollout --env-id b9085e64-8926-4a15-b7d4-8896143d1394 --flag-key cli-e2e-manual-20260524-170000 --rollout [{"variationId":"bad","percentage":60}]` | 1 | passed | Client validation rejected rollout total: `Percentages must sum to 100, but got 60.` |

## Findings

- `flag evaluate` defaults `--eval-host` to the management host. For `https://app-api.featbit.co`, evaluation succeeds when `--eval-host https://app-eval.featbit.co` is supplied. The README and test story should make this explicit.
- Test reports are generated under `tests/reports/`, and `.gitignore` now ignores that directory.

## Redaction Check

- Full access token: not included.
- Environment secret: not included.
- Authorization header: not included.
