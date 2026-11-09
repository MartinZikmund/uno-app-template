---
title: Fix Dev bundle signing in the Windows build workflow
impact: recommended
features: [packaging]
platforms: [windows]
---

## Why

The Dev bundle's signing step in `_build-windows.yml` passed `winapp sign` an option it doesn't have, `--cert-password`.
The script ignored that failure and carried on, and a later `Get-PfxCertificate` with no password could then wait forever
for an answer nobody could give, so the job could hang instead of producing a signed Dev bundle.

## Who is affected

Apps with the Windows head and `.github/workflows/_build-windows.yml`. Skip this if your workflow's `Sign the Dev bundle`
step already sets `$PSNativeCommandUseErrorActionPreference`.

## Steps

`mzuno-app upgrade --patch` delivers this, except where you've changed that step.

1. In `_build-windows.yml`, change the `winapp` version from `'v0.6.1'` to `'v0.8.0'`.

2. Update the `Sign the Dev bundle with a generated certificate` step. Add the timeout beside `shell: pwsh`:

   ```yaml
   timeout-minutes: 10
   ```

   Then change its script. Before:

   ```powershell
   $ErrorActionPreference = 'Stop'
   # ...
   winapp cert generate --manifest "${{ steps.manifest.outputs.manifest }}" --output $pfx
   winapp sign "${{ steps.pack.outputs.bundle }}" --cert $pfx --cert-password password
   # ...
   $c = Get-PfxCertificate -FilePath $pfx
   ```

   After:

   ```powershell
   $ErrorActionPreference = 'Stop'
   $PSNativeCommandUseErrorActionPreference = $true
   # ...
   # winapp's default PFX password: public, dev-only, not a secret.
   $password = 'password'
   # ...
   winapp cert generate --manifest "${{ steps.manifest.outputs.manifest }}" --output $pfx --password $password
   winapp sign "${{ steps.pack.outputs.bundle }}" $pfx --password $password
   # ...
   $c = Get-PfxCertificate -FilePath $pfx -Password (ConvertTo-SecureString $password -AsPlainText -Force)
   ```

3. If your docs mention the pinned `winapp` version, update them too.

## Verify

Push to `main`. In `build-main.yml`, the Windows build job should finish with the signing step succeeding, and its
artifact should contain a signed `.msixbundle` and a `.cer`.
