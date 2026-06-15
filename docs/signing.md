# Code signing with Azure Trusted Signing

The release workflow ([`.github/workflows/release.yml`](../.github/workflows/release.yml))
can sign `ScreenSnap.exe` and the MSI installer using
[Azure Trusted Signing](https://learn.microsoft.com/azure/trusted-signing/).
Trusted Signing issues short-lived certificates from a Microsoft-operated CA
and is recognized by SmartScreen and Smart App Control, so signed builds
won't trigger the "Windows protected your PC" prompt.

Signing is **opt-in**: the two signing steps in the workflow are gated on
the `AZURE_TRUSTED_SIGNING_ACCOUNT` repository variable. Without it, builds
still run and produce *unsigned* MSIs (useful for forks and for local
`workflow_dispatch` test runs).

## One-time setup (on Azure)

1. **Subscription.** Have an Azure subscription you can create resources in.
2. **Trusted Signing Account.** In the Azure portal, create a *Trusted
   Signing Account* in a supported region (e.g. East US, West Europe).
   Note the regional endpoint, e.g. `https://eus.codesigning.azure.net/`.
3. **Identity validation.** From the Trusted Signing Account, start an
   *Identity Validation* request. Microsoft verifies the publisher identity
   (individual or organization). This can take a few business days.
4. **Certificate Profile.** Once identity validation is approved, create a
   *Certificate Profile* under the account (Public Trust). The profile
   name is what you'll pass to the signing step.
5. **App registration + federated credential.** In Microsoft Entra ID,
   create an app registration. Add a *federated credential* of type
   *GitHub Actions* pointing at this repository
   (`alex-oswald/ScreenSnap`) and the `Release` workflow / `build` job /
   `main` branch and `v*` tags. This lets GitHub Actions authenticate via
   OIDC without storing a client secret.
6. **Role assignment.** On the Trusted Signing Account (or the Certificate
   Profile), grant the app registration the
   **Trusted Signing Certificate Profile Signer** role.

## GitHub repo configuration

Set these as **Repository variables** (not secrets — none of these are
sensitive on their own; the OIDC trust relationship is what authenticates):

`Settings → Secrets and variables → Actions → Variables → New repository variable`

| Variable | Example | Notes |
| --- | --- | --- |
| `AZURE_TENANT_ID` | `00000000-0000-0000-0000-000000000000` | Entra tenant the app registration lives in. |
| `AZURE_CLIENT_ID` | `00000000-0000-0000-0000-000000000000` | App registration (client) ID. |
| `AZURE_SUBSCRIPTION_ID` | `00000000-0000-0000-0000-000000000000` | Subscription containing the Trusted Signing Account. |
| `AZURE_TRUSTED_SIGNING_ENDPOINT` | `https://eus.codesigning.azure.net/` | Regional endpoint URL for the Trusted Signing Account. |
| `AZURE_TRUSTED_SIGNING_ACCOUNT` | `screensnap-signing` | Trusted Signing Account name. Acts as the *enable* flag — if it's empty, signing is skipped. |
| `AZURE_TRUSTED_SIGNING_PROFILE` | `screensnap-publictrust` | Certificate Profile name under the account. |

No client secret is required: the workflow uses GitHub's OIDC token
(`permissions: id-token: write`) and the federated credential on the app
registration.

## What gets signed

- `publish/<arch>/ScreenSnap.exe` — signed **before** the MSI is packed
  so the running app has a verified publisher (important for SmartScreen
  and Smart App Control reputation).
- `dist/ScreenSnap-<version>-<arch>.msi` — signed **after** packaging so
  the installer download is trusted.

Other files inside the MSI (the bundled .NET / Windows App SDK runtime,
CsWin32-generated DLLs, etc.) are intentionally left unsigned. Signing
every binary in a self-contained publish is overkill for an app this size
and would slow the workflow considerably.

## Disabling signing temporarily

Delete (or rename) the `AZURE_TRUSTED_SIGNING_ACCOUNT` variable in the
GitHub repo settings. The next workflow run will produce unsigned MSIs
without any other changes.
