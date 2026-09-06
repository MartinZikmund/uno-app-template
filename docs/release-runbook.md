# Release runbook

The operational half of [release-pipeline.md](./release-pipeline.md). This is the page to
open when something is on fire.

- [Cut a release](#cut-a-release)
- [Ship a patch](#ship-a-patch)
- [Halt a bad rollout](#halt-a-bad-rollout)
- [A store failed and the others succeeded](#a-store-failed-and-the-others-succeeded)
- [Restart a halted rollout](#restart-a-halted-rollout)
- [Forward-merge](#forward-merge)
- [Break glass: republish an old run](#break-glass-republish-an-old-run)
- [Things that will bite you](#things-that-will-bite-you)

## Cut a release

**Actions → Cut release branch → Run workflow.** Tick `dry_run` the first time; it prints
the plan and changes nothing.

The workflow refuses to start if the release branch already exists, if `main`'s latest CI
run is not green, or if `version.json` has no prerelease tag (see
[versioning.md](./versioning.md)). Then it:

1. runs `nbgv prepare-release` — creates `release/vX.Y` and moves `main` to the next minor
2. pushes both
3. starts `release.yml` on the new branch

If it fails partway it deletes the release branch it created, so you can re-run after
fixing the cause rather than untangling a half-cut repo.

**What lands without you:** Play internal, TestFlight, a Microsoft Store *draft*, the web
deploy, the `vX.Y.Z` tag, a draft GitHub Release.

**What waits:** the three production approvals. Verify on a real device from Play internal
or TestFlight *first* — that is what those tracks are for.

## Ship a patch

**A patch is just another commit on `release/vX.Y`.** No patch workflow, no patch branch,
no hand-edited version. The git height moves, every store's version moves with it, and the
same `release.yml` runs.

```bash
# Preferred: fix on main, then cherry-pick. Use when main has not drifted.
git switch main && git switch -c fix/crash-on-launch
#   ... failing test FIRST, then the fix ...
gh pr create --base main && gh pr merge --squash
git switch release/v1.0 && git cherry-pick <sha-on-main>

# Or hotfix directly on the release branch, when main has drifted badly.
git switch release/v1.0 && git switch -c hotfix/crash-on-launch
gh pr create --base release/v1.0 && gh pr merge --squash
```

```bash
git push origin release/v1.0    # this is the entire release action
```

A **human** push triggers `release.yml` normally — the GitHub App is only needed for the
bot-made initial cut. With no file edited, one commit moves everything:

| | before | after |
|---|---|---|
| nbgv `SimpleVersion` | `1.0.42` | `1.0.43` |
| Windows `Identity/@Version` | `1.0.42.0` | `1.0.43.0` |
| Android `versionCode` | `10000042` | `10000043` |
| iOS `CFBundleVersion` | `1.0.42` | `1.0.43` |

Then verify on Play internal / TestFlight, approve the three gated environments, and
**merge the forward-merge PR**.

> If the crash is already live, **halt first** — before you write a line of code. See
> below.

## Halt a bad rollout

```bash
gh workflow run store-ops.yml -f action=play-halt
gh workflow run store-ops.yml -f action=appstore-pause
gh workflow run store-ops.yml -f action=msstore-rollout-halt
```

**Halting does not roll anyone back.** Customers who already received the build keep it.
Halting only stops the bleed; the fix must be a *higher* version. This is the single most
misunderstood fact in mobile releases, and it is why halting comes before fixing.

## A store failed and the others succeeded

1. **Re-run failed jobs** on the release run. The successful jobs' artifacts are still
   there, so nothing recompiles and only the failed store retries.
2. If a publish already succeeded, `publish-once` makes a re-run a no-op. That guard
   records that a *job* finished — **not** that a store *accepted* the submission. For an
   asynchronous certification failure hours later, use `store-ops.yml → republish`, which
   does not consult that guard at all.
3. Past the 30-day re-run window, see *break glass* below. Release artifacts are retained
   **90 days** specifically so that lever stays usable.

## Restart a halted rollout

Do **not** resume at the halted percentage — the bad build is still out there, and the
patch is a fresh release. Approve `play-production` (which publishes at 10 %), then walk it
up deliberately:

```bash
gh workflow run store-ops.yml -f action=play-bump -f value=0.25
gh workflow run store-ops.yml -f action=play-bump -f value=0.50
gh workflow run store-ops.yml -f action=play-finalize
gh workflow run store-ops.yml -f action=msstore-rollout-update   -f value=50
gh workflow run store-ops.yml -f action=msstore-rollout-finalize
```

**Setting 100 % is not the same as finalizing.** An un-finalized Play or Microsoft Store
rollout is the top cause of the *next* release being blocked. `store-health.yml` catches the
**Play** case weekly and opens an issue; the Microsoft Store has no equivalent probe yet, so
check it with `store-ops.yml -f action=msstore-status` after any staged rollout.

## Forward-merge

`forward-merge.yml` keeps one open `release/vX.Y → main` PR. **Merge it.** When resolving
the `version.json` conflict, keep **main's** version — the release branch's stable version
must never travel back to `main`. Skip this and the next minor ships without your fix.

## Break glass: republish an old run

```bash
gh workflow run store-ops.yml \
  -f action=republish -f value=<run_id> -f republish_targets=play,web -f force=true
```

Downloads that run's artifacts and re-pushes them — the exact bits that were tested, with
no rebuild and therefore no version change. `play` and `web` re-publish directly; `msstore`
downloads the `.msixbundle` and points you at it, because Store submission needs a Windows
runner — re-run the release run's `publish-msstore-draft` job, or upload it by hand.

## Things that will bite you

| Store | What bites | What to do |
|---|---|---|
| **App Store** | Only **one** version may sit in *Waiting for Review / In Review / Pending Developer Release*. Cancelling developer-rejects the submission and sends you to the **back of the review queue**. A patch also restarts Apple's 7-day phased curve from day 1. | `submit-appstore` runs a guard and fails loudly rather than half-succeeding. Cancelling is `store-ops -f action=appstore-cancel-review` — a deliberate human act, never automation's. |
| **Play** | Re-uploading a consumed `versionCode` fails with HTTP 400 *"Version code N has already been used"*. | The git height guarantees a new code. To force a genuine re-upload of the same commit: `git commit --allow-empty -m "chore: bump build" && git push`. |
| **Play** | An un-finalized staged rollout blocks the next release. | `store-ops -f action=play-finalize`. |
| **Microsoft Store** | A pending submission ⇒ **409**. An un-finalized gradual rollout ⇒ 409 on the next submission. | `publish-msstore-draft` pre-flights the submission status and refuses early; `store-ops -f action=msstore-delete-draft` unblocks, and itself refuses to delete anything past draft state. |
| **Microsoft Store** | An API-created submission that you edit in the **Partner Center UI** can never be committed by the API again. | **Never edit an API-created submission in the Partner Center UI.** |
| **Microsoft Store** | The `msstore` CLI supports **free products only**; paid products are not yet supported. | If the app becomes paid, swap the Store jobs for raw REST against `manage.devcenter.microsoft.com/v1.0`, or StoreBroker. Both drive the identical API. |
| **Two live trains** | Patching `release/v1.0` after `release/v1.1` shipped produces a *lower* version than the live one — rejected on Play, superseded on the Microsoft Store. | Ship it to a dedicated Play track / package flight, or forward the fix into the current train. A business decision, not a CI operation. |

### Credentials and their expiry

| Credential | Expires | Notes |
|---|---|---|
| App Store Connect API key | never | Scope it to **App Manager**, not Admin. |
| Apple distribution certificate | 1 year | Rotate before it lapses or every iOS release fails. |
| Apple provisioning profile | 1 year | Referenced by **name** (`vars.APPLE_PROVISIONING_PROFILE_NAME`), so regenerating it does not require a secret change. |
| Play service account JSON | never | |
| Entra client secret (Microsoft Store) | up to 2 years | The most commonly forgotten one. |
| GitHub App private key | never | `store-health.yml` verifies weekly that it still mints a token. |
