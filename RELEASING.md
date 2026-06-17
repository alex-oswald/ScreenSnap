# Releasing ScreenSnap

ScreenSnap is released by pushing a Git tag of the form `vMAJOR.MINOR.PATCH`
(optionally with a prerelease label) to `main`. The `Release` workflow
(`.github/workflows/release.yml`) does the rest: build, sign, package, and
publish.

## Tag conventions

| Tag pattern        | Kind        | Example          | GitHub Release flag |
|--------------------|-------------|------------------|---------------------|
| `vX.Y.Z`           | Stable      | `v1.2.3`         | normal              |
| `vX.Y.Z-alphaN`    | Prerelease  | `v1.2.3-alpha7`  | prerelease          |
| `vX.Y.Z-betaN`     | Prerelease  | `v1.2.3-beta5`   | prerelease          |
| `vX.Y.Z-rcN`       | Prerelease  | `v1.2.3-rc2`     | prerelease          |

The prerelease label is preserved in the file name and
`InformationalVersion`, but stripped before deriving the numeric MSI
`ProductVersion` (see below).

## GitHub Environments

The build job's `environment:` is selected from the tag at run time:

```yaml
environment: ${{ (github.ref_type == 'tag' && !contains(github.ref_name, '-'))
                 && 'release'
                 || 'development' }}
```

- **`release`** — stable tags (`vX.Y.Z`).
- **`development`** — prerelease tags (`vX.Y.Z-...`) and manual
  `workflow_dispatch` test runs.

The Azure Trusted Signing variables
(`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`,
`AZURE_TRUSTED_SIGNING_ACCOUNT`, `AZURE_TRUSTED_SIGNING_ENDPOINT`,
`AZURE_TRUSTED_SIGNING_PROFILE`) live at the **repository level**, so both
environments inherit them with no extra setup. Approval gates and reviewer
rules can be added per environment in GitHub `Settings → Environments`
without touching the workflow.

### Azure federated credentials (one-time)

`azure/login@v2` uses OIDC, with one federated credential per environment
on the Azure app registration referenced by `AZURE_CLIENT_ID`. The OIDC
subject is `repo:<owner>/<repo>:environment:<env-name>`, so each
environment needs its own credential:

| Environment   | Subject identifier                                          |
|---------------|-------------------------------------------------------------|
| `release`     | `repo:alex-oswald/ScreenSnap:environment:release`           |
| `development` | `repo:alex-oswald/ScreenSnap:environment:development`       |

If signing fails with `AADSTS70021` / "No matching federated identity
record found", that credential is missing — add it on the app registration
in the Azure portal under `Certificates & secrets → Federated credentials`.

## MSI ProductVersion

`ProductVersion` is the numeric `major.minor.patch` from the tag, with any
prerelease suffix stripped (e.g. `v1.2.3-beta5` -> `1.2.3`). The file name
and the .NET `InformationalVersion` keep the original semver string
(`ScreenSnap-1.2.3-beta5-x64.msi`).

Caveat: because the suffix is stripped, every prerelease of the same
`x.y.z` produces an MSI with the same `ProductVersion`. `<MajorUpgrade>`
only fires on a strictly greater version, so a fresh install of, say,
`v1.2.3-beta6` over an existing `v1.2.3-beta5` falls into Windows
Installer maintenance-mode reconfigure (progress bar, then nothing). To
test a new prerelease over an old one on the same machine, uninstall the
old one first:

```pwsh
Get-ChildItem HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall |
  ForEach-Object { Get-ItemProperty $_.PSPath } |
  Where-Object DisplayName -like 'ScreenSnap*' |
  Select-Object DisplayName, DisplayVersion, PSChildName
msiexec /x "{ProductCode-GUID}" /qb
```

For stable releases (which bump `patch`), `<MajorUpgrade>` works as
expected.

## Cutting a release

1. Update the `<Version>` / changelog in the repo (if applicable) and merge.
2. Tag and push:
   ```bash
   git tag v1.2.3-beta6
   git push origin v1.2.3-beta6
   ```
3. Watch the `Release` workflow in Actions. Confirm it targets the expected
   environment (`development` for prereleases, `release` for stable).
4. The workflow uploads signed `x64` and `arm64` MSIs plus
   `SHA256SUMS.txt` to a new GitHub Release. Prereleases are flagged as
   such automatically.

## Local test build (unsigned)

```pwsh
dotnet publish src/ScreenSnap/ScreenSnap.csproj `
  -c Release -r win-x64 -p:Platform=x64 `
  --self-contained true -p:WindowsAppSDKSelfContained=true `
  -p:WindowsPackageType=None `
  -o publish/x64

wix build installer/ScreenSnap.wxs `
  -arch x64 `
  -ext WixToolset.Util.wixext `
  -d PublishDir="$((Resolve-Path publish/x64).Path)" `
  -d ProductVersion=0.0.1 `
  -d UtilCA=Wix4UtilCA_X64 `
  -o dist/ScreenSnap-local-x64.msi
```

The local MSI is unsigned, so SmartScreen will prompt; install behavior is
otherwise identical to the released artifact.

## Install behavior

- Per-user install under `%LocalAppData%\Programs\ScreenSnap` — no admin /
  UAC prompt.
- After a fresh install or major upgrade, `ScreenSnap.exe` launches
  automatically. Uninstall and maintenance-mode reconfigure do not relaunch
  the app.
- Uninstall via *Settings → Apps → Installed apps* or
  `msiexec /x {ProductCode}` — the ProductCode is in
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\{...}`.
