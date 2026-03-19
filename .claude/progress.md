# OpenUtau Mobile — Project Progress

**Last Updated**: 2026-03-19
**Current Phase**: Phase 2 (Touch Performance)

## Phase Overview

| Phase | Status | Summary | Details |
|-------|--------|---------|---------|
| Phase 1 — Stabilization | ✅ COMPLETE | IDisposable, lifecycle, thread safety. Grade A-. | [progress-phase1.md](progress-phase1.md) |
| Phase 2 — Touch Performance | 🔄 IN PROGRESS | Bitmap cache, SKImage optimization. Stage A done. | [progress-phase2.md](progress-phase2.md) |
| Phase 3 — Unfinished Features | ⬚ PLANNED | Vibrato UI, phoneme editing, L10n, test infra. | — |
| Phase 4 — Upstream Sync | ⬚ PLANNED | Core update, plugin support, final PR. | — |

## Active Sprint

**Phase 2 Stage B**: ノート未描画問題の調査・修正 → 全Canvas再計測

### 直近の完了タスク

- [x] P2-5c PianoRollTickBg bitmap cache + SKImage (2026-03-19)
- [x] P2-5b PianoKeysCanvas bitmap cache (2026-03-18)
- [x] P2-5a PlaybackTickBg bitmap cache (2026-03-18)

### 直近の計測結果 (2026-03-19, Pixel 10 Pro XL)

| Canvas | max | slow率 | Status |
|--------|-----|--------|--------|
| PianoKeysCanvas | 4ms | 0% | ✅ TARGET MET |
| PianoRollKeysBg | 7ms | 0% | ✅ TARGET MET |
| PianoRollTickBg | 33ms | 8.9% | 🔶 HIT時<8ms |
| PlaybackTickBg | 22ms | 2fr only | ✅ Cache working |

## Key Technical Decisions (Latest)

- SKImage.FromBitmap + DrawImage srcRect: 3-5x faster than DrawBitmap
- Cache margin 1.0 (3x size) optimal; 0.5 tested and reverted
- Shadow composite approach for dynamic overlay on cached grid
- SnapTicks confirmed dead code (no external readers)

## Environment

- .NET 9 + MAUI + SkiaSharp, target net9.0-android
- Windows 11, VS 2026 (18.4.1)
- Test device: Pixel 10 Pro XL (Android 16, API 36)
- Tests: 12/12 passing (SmokeTests 4, TransformerTests 8)
- Build warnings: 1725 (Core origin, out of scope)

## Sub-agents

- auditor v3 ✅ | implementer v2 ✅ | tester v2 ✅
- Skills: project-overview, openutau-core-api, maui-mobile-patterns, skia-performance
