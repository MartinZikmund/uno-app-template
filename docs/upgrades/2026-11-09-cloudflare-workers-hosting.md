---
title: Move WebAssembly hosting from Azure Static Web Apps to Cloudflare Workers
impact: recommended
features: [packaging]
platforms: [wasm]
---

## Why

The WebAssembly head now deploys to Cloudflare Workers static assets instead of Azure Static Web Apps. Production comes
from `release.yml`, and each pull request gets a preview at a stable `pr-<number>` URL. Previews are versions rather than
environments, so there's nothing to tear down when a pull request closes.

Full guide: [deployment.md](https://github.com/MartinZikmund/uno-app-template/blob/main/docs/deployment.md).

## Who is affected

Apps with the WebAssembly head and the template's release workflows. Skip this if your app already has `wrangler.jsonc`.

If you'd rather stay on Azure, keep your workflows and record the note with `mzuno-app upgrade --mark-applied`.

## Steps

`mzuno-app upgrade --patch` delivers steps 1 to 3, except where you've changed those files.
**Steps 4 to 7 are yours: no patch can create a Cloudflare account, token, secret or Worker.**

1. **Add the Worker config.** Copy
   [`wrangler.jsonc`](https://github.com/MartinZikmund/uno-app-template/blob/main/wrangler.jsonc) to the repository root.
   Replace `AppTemplate` in `assets.directory`, and set `name` to your Worker's name: lowercase letters, digits and
   hyphens, 63 characters at most. A patch derives it from your app's name. **Settle it now,** because it decides the
   hostname, and renaming later orphans the old Worker and every preview alias under it.

2. **Replace the web host's config.** Delete
   `src/AppTemplate/Platforms/WebAssembly/wwwroot/staticwebapp.config.json` and add
   [`_headers`](https://github.com/MartinZikmund/uno-app-template/blob/main/src/AppTemplate/Platforms/WebAssembly/wwwroot/_headers)
   beside it. If you added routes, redirects or headers to the old file, move them across yourself: Workers handles
   single-page fallback through `wrangler.jsonc`, and `_headers` takes only headers. Keep its rules from overlapping,
   because Cloudflare joins duplicate headers with a comma rather than letting the specific rule win.

3. **Update the workflows.** Take these from
   [`.github/`](https://github.com/MartinZikmund/uno-app-template/tree/main/.github), replacing `AppTemplate` with your
   app's name:
   - `actions/prepare-wasm-assets/` (new)
   - `workflows/_build-wasm.yml`, which now calls that action before uploading
   - `workflows/wasm-pr-preview.yml`, rewritten for `wrangler versions upload`
   - in `workflows/release.yml`, the `publish-web` job
   - in `workflows/store-ops.yml`, the `release-wasm` download and the `Re-deploy the web build` step

   Also add `.wrangler/` to `.gitignore`.

4. **Prepare Cloudflare.** Create the `workers.dev` subdomain on your account under Workers & Pages → Subdomain, then
   create an API token from the **Edit Cloudflare Workers** template.

5. **Add two secrets, and remove the old one.** The deploy steps skip with a warning unless both new ones are set:

   ```bash
   gh secret set CLOUDFLARE_API_TOKEN
   gh secret set CLOUDFLARE_ACCOUNT_ID
   gh secret delete AZURESTATICWEBAPPSDEPLOYMENTTOKEN
   ```

   If those secrets live in the `web-production` environment, or you keep separate copies for `store-ops`, set them there
   too with `--env <name>`, and delete the old token from each.

6. **Create the Worker** by running `dotnet publish` and `npx wrangler deploy` once, using the commands under Verify, or
   by cutting a release. `versions upload` fails if the Worker doesn't exist yet, so previews won't work until then.

7. **Retire Azure.** Once the new site works, move any custom domain across (the zone must be on Cloudflare DNS), then
   delete the Static Web App resource in the Azure portal. Nothing here removes it, and it keeps serving the old site
   until you do.

## Verify

1. Run these locally. `wrangler dev` should print `Parsed 2 valid header rules`:

   ```bash
   dotnet publish src/AppTemplate/AppTemplate.csproj -c Release -f net10.0-browserwasm
   npx wrangler dev
   npx wrangler versions upload --dry-run
   ```

2. Open a pull request in a public repository. A comment should appear with the preview's `pr-<number>` URL. Preview
   deploys don't run for private repositories.
3. Cut a release and check that the `publish-web` job deployed to your Worker's `workers.dev` address.
