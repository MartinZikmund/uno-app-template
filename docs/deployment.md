# Web deployment

The WebAssembly head is hosted on **Cloudflare Workers static assets** as an assets-only
Worker named `app-template`.

| | Trigger | URL |
|---|---|---|
| **Production** (Prod channel) | push to `release/v**` → `release.yml` | <https://app-template.mzikmund.workers.dev> |
| **PR preview** (Dev channel) | PR to `main`, public repo only → `wasm-pr-preview.yml` | `https://pr-<number>-app-template.mzikmund.workers.dev` |

`mzikmund` is the account-wide `workers.dev` subdomain. It can be renamed in the dashboard,
which changes every URL above — Cloudflare reissues certificates for the new name, so expect TLS
handshakes to fail for a few minutes afterwards. Nothing in CI hardcodes it: the preview URL is
scraped from wrangler's output rather than constructed.

Previews are uploaded with `wrangler versions upload --preview-alias pr-<number>`, which
publishes a version **without** making it live. The alias is stable, so every push to a PR
refreshes the same URL, and the workflow posts it as a sticky PR comment. Cloudflare retains
the 1000 most recent aliases, so there is no teardown job when a PR closes.

wrangler prints *two* URLs per preview: a per-version one that changes on every push, and the
stable `pr-<number>` alias. The workflow deliberately scrapes the alias — the wrangler-action
`deployment-url` output carries the per-version URL, which would make the comment link move.

## One-time setup

1. Create the `workers.dev` subdomain on the Cloudflare account (Workers & Pages → Subdomain).
   Preview URLs only work on `workers.dev`, never on a custom domain.
2. Create an API token from the **Edit Cloudflare Workers** template
   ([dash.cloudflare.com/profile/api-tokens](https://dash.cloudflare.com/profile/api-tokens)).
3. Add two repository secrets:

   ```bash
   gh secret set CLOUDFLARE_API_TOKEN    # paste the token from step 2
   gh secret set CLOUDFLARE_ACCOUNT_ID   # Workers & Pages → Account ID
   ```

   Without them both deploy steps skip themselves and the workflows still pass, so forks stay
   green.
4. Run `npx wrangler deploy` once (or merge a release) to **create** the Worker —
   `versions upload` uploads into an existing Worker and fails if there isn't one.

## Adopting this template

Rename `app-template` in [`wrangler.jsonc`](../wrangler.jsonc) to your own Worker name, the same
way you replace `AppTemplate` elsewhere. That name decides the deployed hostname, so pick it
before the first deploy: renaming later orphans the old Worker and every preview alias under it.
Nothing else in the deployment path is app-specific.

## Configuration

- **[`wrangler.jsonc`](../wrangler.jsonc)** (repo root) is an assets-only Worker — there is no
  `main` script, so Workers serves the published `wwwroot` directly. `not_found_handling:
  "single-page-application"` serves real files when they exist and falls back to `index.html`
  otherwise, which is what Uno's client-side navigation needs on a deep link or a refresh.
  `assets.directory` points at the local publish output, so `npx wrangler dev` works with no
  flags straight after a `dotnet publish`; CI downloads the `release-wasm` artifact into that
  same path so there is one source of truth for it.
- **`src/AppTemplate/Platforms/WebAssembly/wwwroot/_headers`** carries the cache policy.
  Cloudflare *joins* duplicate headers from overlapping rules with a comma rather than letting
  the specific one win, so the two rules are deliberately disjoint: `/_framework/*` and
  `/package_*` are both fully content-addressed and frozen for a year, while the root
  (`index.html`, `service-worker.js`, `manifest.webmanifest`, `build-info.json`) keeps the
  Workers default of `public, max-age=0, must-revalidate` so a new deploy is picked up on the
  next visit.

MIME types (`application/wasm`) and Brotli/gzip compression are handled at the edge, so no MIME
mapping is needed. `Platforms/WebAssembly/wwwroot/web.config` is a leftover from IIS hosting and
is inert here; it is kept for anyone hosting the same output on IIS.

## The prepare-wasm-assets action

[`.github/actions/prepare-wasm-assets`](../.github/actions/prepare-wasm-assets/action.yml) runs
between publish and upload in both `_build-wasm.yml` and `wasm-pr-preview.yml`. It:

- copies `_headers` in if the publish glob ever drops it. It survives today — the SDK carries
  `Platforms/WebAssembly/wwwroot/` through verbatim — so this is a warned-about safety net, not
  the normal path;
- deletes the `.br`/`.gz` siblings the SDK emits for servers that content-negotiate by rewriting
  to them, as `web.config` does on IIS. Workers compresses at the edge and never serves those
  siblings, and nothing references them — not `service-worker.js`, not `index.html` — so they
  are inert weight;
- fails the run if any single asset crosses Cloudflare's 25 MiB per-file limit, rather than
  letting wrangler reject the upload halfway through. The free plan also allows 20,000 files.

## Local dry run

```bash
dotnet publish src/AppTemplate/AppTemplate.csproj -c Release -f net10.0-browserwasm
npx wrangler dev                      # serve the built output locally
npx wrangler versions upload --dry-run
```

`wrangler dev` prints `✨ Parsed 2 valid header rules` on startup. If it says 0 rules, `_headers`
did not reach the publish output — which is the case the CI action warns about and repairs.

Two behaviours worth knowing: `/index.html` 307-redirects to `/` (Workers canonicalises it), and
because SPA fallback returns the shell for anything not on disk, a *missing* asset comes back as
`200` + `index.html` rather than a `404`.

## Custom domain

A Worker custom domain requires the zone to be on Cloudflare DNS. Point the subdomain at the
Worker under Workers & Pages → your Worker → Settings → Domains & Routes.

## Related

- [release-pipeline.md](./release-pipeline.md) — every workflow, environment and secret.
- [versioning.md](./versioning.md) — the Dev/Prod channel model the deployed site inherits.
