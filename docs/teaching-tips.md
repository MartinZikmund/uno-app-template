# Teaching tips (coach marks)

Use this when you want to point new users at real controls — a short first-launch tour, or a tip
that waits until it's actually useful — instead of a carousel shown before anyone has touched the app.

The template ships a working sample on the Home page: a two-step tour (the hero, then the "Try me"
button) and one contextual tip that appears after the button has been clicked three times.
Settings → **Show tips again** replays everything.

## The pieces

| Type | Where | Job |
|------|-------|-----|
| `TipId` + `TipIdExtensions` | `AppTemplate.Core/Services/Tips/TipId.cs` | The tips, their stable storage ids, and the tour order (`TourSteps`). |
| `ITipService` / `TipService` | `AppTemplate.Core/Services/Tips/` | `ShouldShow`, `MarkSeen`, `Reset`, persisted in one preference. |
| `ITipAnchorHost` | `AppTemplate.Core/Services/Tips/ITipAnchorHost.cs` | Implemented by the view, so the view model can ask "is this tip's target on screen?" |
| `MainViewModel` | `AppTemplate.Core/ViewModels/MainViewModel.cs` | Owns sequencing through `ActiveTip`. |
| `MainView.xaml` | `AppTemplate/Views/` | One `TeachingTip` per tip, `IsOpen` bound one way. |

## How it fits together

- **Storage.** Seen tips live in a single `SeenTips` preference (via `IPreferences`) as
  comma-separated **string** ids such as `home-welcome`. Enum ordinals never reach storage, so
  reordering `TipId` can't change what a user has seen. Ids this build doesn't recognise (written by
  a newer version) are kept on write, so a downgrade doesn't replay tips.
- **Sequencing in the view model.** `ActiveTip` is a single `TipId?`, so at most one tip is ever
  open. Each tip gets a bool like `IsWelcomeTipOpen` that XAML binds with
  `IsOpen="{x:Bind ..., Mode=OneWay}"`. The whole flow is unit tested without a visual tree — see
  `MainViewModelTipTests`.
- **Closing goes back to the view model.** The action button binds to `TipNextCommand`. Every
  other close (close button, X) raises `Closed`, which the view forwards to `DismissTipCommand`
  unless the reason is `Programmatic` (the view model moving on, or the page unloading).
- **Skip ends the tour only.** Closing a tour step marks every tour step seen; contextual tips
  stay eligible.
- **Contextual tips wait for the tour.** They never fire while a tour step is unseen.

## Gotchas baked in

- **Open after layout.** `Loaded` fires before targets are arranged, and anchors inside an
  `ItemsRepeater` aren't arranged yet even at `ElementPrepared`. A tip opened against pre-layout
  bounds lands in the wrong place, so the view calls `EvaluateTips` through
  `DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, ...)`.
- **Missing anchor → skip, don't burn.** Before opening a tip the view model asks
  `ITipAnchorHost.IsAnchorReady`. If the target isn't loaded and laid out, the tip sits this load
  out and is **not** marked seen.
- **No light dismiss.** `IsLightDismissEnabled="False"`, so a stray click on the page can't quietly
  use up a tip.

## Adding a tip

1. Add a member to `TipId` and its id to `ToStorageId` (never change an existing id). If it's a
   tour step, add it to `TourSteps` in order.
2. Give the view model an `Is…TipOpen` property (with `[NotifyPropertyChangedFor]` on `ActiveTip`)
   and, for a contextual tip, the trigger that calls `TryOpen`.
3. Add a `TeachingTip` to the view with `Target`, `IsOpen`, `Closed="Tip_Closed"`, and register it
   in the view's `_tips` map.
4. Add the title/subtitle strings to **both** `Strings/en` and `Strings/cs`.
5. Test the sequencing in `AppTemplate.Core.Tests` with `FakePreferences` and `FakeTipAnchorHost`.

For placement, prefer the side with the most free space (`Top` above a hero near the middle,
`Bottom` under a button) and let `TeachingTip` fall back on its own when the window is small.
