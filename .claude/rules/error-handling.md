---
description: Logging failures and keeping async void handlers from swallowing or crashing
---

# Error handling & logging

## `async void` handlers
- **Every `async void` method has a `try/catch` around its whole body that logs** the exception through `ILogger`. This covers event handlers, `OnLaunched`, `OnNavigatedTo` overrides, and lambdas like `button.Click += async (s, e) => ...`.
- Why: an exception escaping an `async void` lands on the dispatcher, not on a caller. WinUI raises `Application.UnhandledException`; Uno's Skia dispatcher only logs it and carries on (unoplatform/uno#6569). Either way nothing on the call path sees it, so catch it where the context still exists.
- Prefer moving the body into an `async Task` method and keeping the `async void` a thin try/catch wrapper (see `App.OnLaunched` → `LaunchAsync`).
- `[RelayCommand]` on an `async Task` method is **not** `async void` — but its exceptions still surface on the dispatcher. Catch failures the user should know about and show them via `IErrorDialogService`.

## Logging
- Log through injected `ILogger<T>`; never `Debug.WriteLine` or `Console.WriteLine` (stripped from Release / invisible on device).
- Everything at the host's minimum level ends up in the on-device log file (see `docs/logging.md`). Don't log secrets or personal data.
- Use message templates, not interpolation: `logger.LogError(ex, "Could not save {ItemId}", id)`.

## Telling the user
- When a failure is user-visible, show it with `IErrorDialogService.ShowAsync(title, message, exception)` — it appends the exception message and, on desktop heads, points at the log folder with an "Open logs folder" button.
- Don't swallow exceptions silently. If a catch genuinely recovers, log at `Warning` or above with why it's safe.
