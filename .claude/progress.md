# Progress Tracker

## Current Sprint

Status: NOT STARTED
Goal: Stabilize OpenUtau Mobile, improve touch performance, complete missing UI features.

## Backlog

### Phase 1 — Stability (Priority: HIGH)

- [ ] Audit SKPaint allocations in PaintSurface handlers (use auditor agent)
- [ ] Audit ReactiveUI subscription disposal in all ViewModels
- [ ] Audit ICmdSubscriber cleanup in all ViewModels
- [ ] Fix audio import crash (upstream issue #1913)
- [ ] Strengthen autosave — reduce interval, add recovery UI

### Phase 2 — Touch Performance (Priority: HIGH)

- [ ] Add touch throttle (16ms) to pitch curve drawing in EditPage
- [ ] Cache SKPaint/SKPath objects identified by auditor
- [ ] Implement dirty-region tracking to avoid full canvas redraws
- [ ] Profile PaintSurface and confirm under 8ms per frame

### Phase 3 — Missing Features (Priority: MEDIUM)

- [ ] Vibrato editing UI (use SetVibratoCommand and individual property commands)
- [ ] Phoneme timing editor (PhonemeOffsetCommand, PhonemePreutterCommand, PhonemeOverlapCommand)
- [ ] Quantize grid setting (1/4, 1/8, 1/16, 1/32, 1/64)
- [ ] Tempo/time signature marker deletion
- [ ] Japanese localization (AppResources.ja.resx)

### Phase 4 — Upstream Sync (Priority: LOW)

- [ ] Merge upstream OpenUtau.Core v0.1.567 changes
- [ ] Add PackageManager.cs (new in upstream)
- [ ] Update .NET 8 to .NET 9 delta patches if needed
- [ ] Plugin system exploration for mobile

## Performance Baselines

Record measurements here as work progresses.

| Metric | Before | After | Target |
| --- | --- | --- | --- |
| PaintSurface (ms) | — | — | < 8 |
| App memory (MB) | — | — | < 500 |
| Touch-to-render latency (ms) | — | — | < 32 |
| Cold start (s) | — | — | < 3 |

## Decision Log

Record architectural decisions and tradeoffs here.

| Date | Decision | Reason |
| --- | --- | --- |
| — | — | — |

## Core Patch Notes

If any Core modifications become necessary, summarize here and detail in docs/CORE_PATCHES.md.

| Date | File | Change | Why |
| --- | --- | --- | --- |
| — | — | — | — |
