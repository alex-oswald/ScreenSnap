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

`N` is `1..99`. Tags with any other prerelease label (`v1.2.3-dev`,
`v1.2.3-beta.5`, etc.) are rejected by the version-derivation step — keep
the label set small so the MSI versioning rules below stay unambiguous.

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

## MSI ProductVersion encoding

`ProductVersion` must be `major.minor.build` with `build ≤ 65535`. To keep
every release tag producing a unique, monotonically increasing MSI version
(so `<MajorUpgrade>` actually fires), the workflow encodes the prerelease
number into the build field:

```
build = patch * 1000 + bucket + N
```

| Kind     | Bucket | `N` range | Example tag      | MSI ProductVersion |
|----------|--------|-----------|------------------|---------------------|
| alpha    | 0      | 1..99     | `v1.2.3-alpha7`  | `1.2.3007`          |
| beta     | 100    | 1..99     | `v1.2.3-beta5`   | `1.2.3105`          |
| rc       | 500    | 1..99     | `v1.2.3-rc2`     | `1.2.3502`          |
| stable   | 999    | (none)    | `v1.2.3`         | `1.2.3999`          |

Within a given `x.y.z`, this ordering holds:

```
alpha1 < ... < alpha99 < beta1 < ... < beta99 < rc1 < ... < rc99 < stable
```

The file name and the .NET `InformationalVersion` keep the original semver
string (e.g. `ScreenSnap-1.2.3-beta5-x64.msi`); only the WiX
`ProductVersion` is the numeric encoding.

Caveats:

- `patch * 1000 + 999` exceeds 65535 once `patch ≥ 65` — bump major/minor
  well before that.
- The `1..99` prerelease range matches typical release cadence. If you ever
  need more iterations of a single prerelease, cut a new patch instead
  (`v1.2.4-beta1`) rather than expanding the numeric ranges.

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
  -d ProductVersion=0.0.1999 `
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
