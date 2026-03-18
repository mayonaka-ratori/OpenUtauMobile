# Roadmap

## Phase 1 — Stability (Weeks 1–2)

The app crashes frequently and leaks memory. Nothing else matters until this is fixed.

### Phase 1 Tasks

- Audit all ViewModels for undisposed ReactiveUI subscriptions and ICmdSubscriber leaks
- Audit all SKCanvasView handlers for SKPaint/SKPath allocations in PaintSurface
- Fix .NET MAUI page lifecycle issues (Shell navigation retaining disposed pages)
- Fix audio import crash (upstream OpenUtau issue #1913)
- Strengthen autosave: shorter interval, crash recovery UI, validate autosave file integrity
- Establish performance baselines (memory, frame time, startup time)

### Phase 1 Exit criteria

- App runs for 30 minutes of continuous editing without crash
- Memory stays below 500MB during normal use
- All smoke tests pass

## Phase 2 — Touch Performance (Weeks 3–4)

The pitch curve editor is unusable on mobile because it was designed for mouse input.

### Phase 2 Tasks

- Add touch event throttling (≤60Hz) to pitch curve drawing in EditPage
- Cache all SkiaSharp objects identified by Phase 1 audit
- Implement dirty-region tracking to avoid full canvas redraws
- Adapt mouse-centric interaction patterns to touch:
  - Pinch to zoom (horizontal: time axis, vertical: pitch axis)
  - Two-finger scroll (separate from single-finger drawing)
  - Long-press for context menu (replace right-click)
  - Larger touch targets for note handles and control points
- Profile PaintSurface and confirm under 8ms per frame

### Phase 2 Exit criteria

- Pitch curve drawing feels responsive (< 32ms touch-to-render latency)
- PaintSurface consistently under 8ms
- Zoom/scroll is smooth and does not conflict with drawing gestures

## Phase 3 — Missing Features (Weeks 5–8)

Complete the features that are partially implemented or missing entirely.

### 3a. Vibrato editing UI

- Visual vibrato envelope overlay on notes
- Drag handles for length, depth, period, fade-in, fade-out
- Uses SetVibratoCommand and individual property commands from Core
- Reference: UVibrato model ranges in openutau-core-api skill

### 3b. Phoneme timing editor

- Display phoneme boundaries below notes (already partially working)
- Drag to adjust timing (PhonemeOffsetCommand)
- Adjust preutterance and overlap (PhonemePreutterCommand, PhonemeOverlapCommand)
- Tap to edit phoneme alias (ChangePhonemeAliasCommand)
- Reset button (ClearPhonemeTimingCommand)

### 3c. Quantize grid

- UI: dropdown or popup to select grid division (1/4, 1/8, 1/16, 1/32, 1/64)
- Snap-to-grid for note creation, movement, and resize
- Visual grid lines update to match selected division
- Persist selection per project

### 3d. Other

- Tempo/time signature marker deletion (currently can only add)
- Japanese localization (AppResources.ja.resx)
- Improve error messages and loading states

### Phase 3 Exit criteria

- Vibrato can be edited visually on all notes
- Phoneme timing can be adjusted by touch
- Quantize grid is selectable and functional
- Japanese UI is complete

## Phase 4 — Upstream Sync and Expansion (Ongoing)

### Phase 4 Tasks

- Merge upstream OpenUtau Core v0.1.567+ changes
- Add PackageManager.cs (new in upstream, enables voicebank management)
- Evaluate plugin system feasibility on mobile
- Explore advanced features: expression curve editor, singer switching per note

### MAUI evaluation checkpoint

At the end of Phase 2, evaluate whether .NET MAUI's rendering performance
is sufficient for a real-time music editor. If PaintSurface cannot consistently
achieve < 8ms even after optimization, consider:

- Option A: Replace specific views with platform-native renderers (Android Canvas / iOS Core Graphics) while keeping MAUI for navigation and layout
- Option B: Full UI rewrite in Flutter, Kotlin Multiplatform, or React Native, keeping OpenUtau.Core via C# binding or native interop
- Option C: Stay on MAUI if performance targets are met

This decision should be recorded in .claude/progress.md decision log.

## Non-goals (for now)

- Full DAW features (mixing, effects, multi-output)
- Desktop parity (desktop OpenUtau already exists and works well)
- Vocaloid file compatibility beyond what Core already supports
- iOS App Store distribution (self-build only due to Apple certificate requirements)
