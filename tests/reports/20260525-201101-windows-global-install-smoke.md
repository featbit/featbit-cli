# Windows Global Install Smoke Test

- Execution timestamp: 2026-05-25 20:11:01 +08:00
- CLI version: v0.1.3 from release `VERSION.txt`
- Repository commit: 80290a2
- Scope: user-account global install smoke test requested by user; not a complete read/write CLI validation run.
- Install source: `C:\Users\hu-be\Downloads\featbit-cli-win-x64\featbit.exe`
- Install target: `C:\Users\hu-be\AppData\Local\Programs\featbit\bin\featbit.exe`
- Project key: `featbit-cli-testing`
- Redacted project identifier: `019e...30e`
- Organization identifier: `4ce9...159`
- Tokens, environment secrets, and Authorization headers: not printed or recorded.

## Commands

| Case | Command | Exit code | Status |
| --- | --- | ---: | --- |
| Install release exe | `Copy-Item C:\Users\hu-be\Downloads\featbit-cli-win-x64\featbit.exe C:\Users\hu-be\AppData\Local\Programs\featbit\bin\featbit.exe -Force` | 0 | passed |
| User PATH update | `[Environment]::SetEnvironmentVariable('Path', '<featbit-bin-first>;...', 'User')` | 0 | passed |
| Help from installed exe | `C:\Users\hu-be\AppData\Local\Programs\featbit\bin\featbit.exe --help` | 0 | passed |
| Config validation | `C:\Users\hu-be\AppData\Local\Programs\featbit\bin\featbit.exe config validate` | 0 | passed |
| Project list | `C:\Users\hu-be\AppData\Local\Programs\featbit\bin\featbit.exe project list` | 0 | passed |
| User PATH command resolution | `featbit --help` with refreshed user PATH | 0 | passed |
| Negative parser smoke | `C:\Users\hu-be\AppData\Local\Programs\featbit\bin\featbit.exe flag list not-a-guid` | 1 | passed |

## Evidence

### Help

```text
FeatBit CLI - manage feature flags via the FeatBit API.
Default host: https://app-api.featbit.co
Usage:
  featbit <resource> <command> [flags]
```

### Config Validation

```text
Config validation succeeded.
Host: https://app-api.featbit.co
Organization: 4ce9...159
Projects fetched: 2
```

### Project List

```text
Id          | Name                     | Key                      | EnvCount
019e...30e  | FeatBit Cli Testing      | featbit-cli-testing      | 2
c83e...fa6  | FeatBit Official Website | featbit-official-website | 2
```

### User PATH Resolution

```text
SOURCE=C:\Users\hu-be\AppData\Local\Programs\featbit\bin\featbit.exe
FeatBit CLI - manage feature flags via the FeatBit API.
Default host: https://app-api.featbit.co
```

### Negative Parser Smoke

```text
Error: Unknown command: flag list not-a-guid. Run 'featbit --help' for usage.
INVALID_GUID_EXIT=1
```

## Disposable Flags

No disposable feature flags were created in this smoke test.

## Cleanup

No cleanup required.
