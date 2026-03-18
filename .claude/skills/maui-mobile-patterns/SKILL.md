---
name: maui-mobile-patterns
description: .NET MAUI mobile lifecycle patterns, memory management, IDisposable conventions, navigation patterns, and testability constraints specific to OpenUtau Mobile.
---
# MAUI Mobile Patterns
## Page Lifecycle
OpenUtau Mobile uses modal navigation (`PushModalAsync` / `PopModalAsync`).
Pages are NOT automatically disposed by MAUI — manual `IDisposable` is required.
### Lifecycle Event Sequence
1. Constructor → `OnAppearing` → (user interaction) → `OnDisappearing` → `Dispose()`
2. `OnDisappearing` can fire WITHOUT `Dispose()` (e.g., app backgrounding, overlay)
3. `Dispose()` can fire WITHOUT `OnDisappearing` (e.g., `AttemptExit` direct call)
4. Both must be independently safe
### OnDisappearing (Pause — Resumable)
- Stop timers (PlaybackTimer, AutoSaveTimer)
- Pause audio playback
- Release screen lock
- Do NOT dispose resources — page may return via OnAppearing
### OnAppearing (Resume)
- Restart timers unconditionally (handlers check state internally)
- Re-acquire screen lock if needed
- Do NOT re-create disposed resources
### Dispose (Destroy — Final)
- Called from `AttemptExit()` after `PopModalAsync()`
- Must be idempotent (`_disposed` guard pattern)
- Order: Timers → Events (-=) → GestureProcessors → Drawables → Magnifiers → _disposables (Rx) → ViewModel → DocManager → Playback → SKPaint/SKFont/SKPath → GC.SuppressFinalize
## IDisposable Convention (Project-Wide)
All IDisposable classes MUST follow this pattern:
```csharp
private bool _disposed = false;  // volatile if cross-thread access
public void Dispose()
{
    if (_disposed) return;
    _disposed = true;
    // ... cleanup in defined order ...
    GC.SuppressFinalize(this);  // Always LAST line
}
```
Rules:
- `_disposed` guard is MANDATORY on every IDisposable
- `GC.SuppressFinalize(this)` is ALWAYS the last line of Dispose()
- No finalizers in this project (SuppressFinalize is defensive)
- Double-Dispose must be safe (guard prevents it)
- `_disposed` is `volatile` when accessed from multiple threads (e.g., AudioTrackOutput)
## Event Subscription Pattern
```csharp
// In constructor — use field-backed handler for unsubscription:
_handler = (s, e) => { ... };
SomeControl.Event += _handler;
// In Dispose:
SomeControl.Event -= _handler;
```
- Inline lambdas CANNOT be unsubscribed — always use field-backed handlers
- ReactiveUI subscriptions use `.DisposeWith(_disposables)` pattern
- ICmdSubscriber: `AddSubscriber(this)` in constructor, `RemoveSubscriber(this)` in Dispose AND in `AttemptExit` before `PopModalAsync`
## Navigation Pattern
- SplashScreen → HomePage → EditPage (all modal)
- SplashScreen: initialization, then `PushModalAsync(new HomePage())`
- On ANY error during init: block navigation to HomePage (`return` after catch)
- EditPage exit: `RemoveSubscriber` → `PopModalAsync` → `Dispose()`
## Timer Pattern
- Use `Dispatcher.CreateTimer()` (MAUI dispatcher timer)
- Store Tick handler as named field for -= unsubscription
- Stop in OnDisappearing, restart in OnAppearing (unconditionally)
- Stop + unsubscribe in Dispose
## ThemeColorsManager
- Static class, initialized once in static constructor
- `ThemeColorsManager.Current` returns `ThemeColors` (SKColor properties)
- Effectively immutable after init (no public setter, no runtime theme switch)
- Thread-safe in practice (read-only after init) but not formally synchronized
- Runtime theme changes NOT supported — documented in EditPage SKPaint fields
- In test environments: `App.Current` is null → falls back to `LightThemeColors`
## Testability Constraints
OpenUtauMobile.csproj targets `net9.0-android;net9.0-ios`.
The test project (OpenUtauMobile.Tests/) targets plain `net9.0`.
This means:
- **CANNOT reference OpenUtauMobile.csproj** from tests (framework mismatch)
- Classes inside OpenUtauMobile/ are NOT directly testable
- Only Core types (OpenUtau.Core) and standalone utility classes are testable
- Exception: `Transformer` class has zero MAUI dependencies and can be tested
  by copying or linking the source file into the test project
### Workarounds (Phase 3 candidates)
1. Extract MAUI-independent logic into `OpenUtauMobile.Shared.csproj` (net9.0)
2. Build tests as `net9.0-android` (requires emulator for execution)
3. Use source-linking for individual classes
