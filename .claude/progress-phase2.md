# OpenUtau Mobile — Phase 2 FINAL Summary

**Status**: ✅ COMPLETE (2026-03-21)
**Goal**: PaintSurface <8ms, touch-to-render latency <32ms
**Active tracking**: see `.claude/progress-phase2.5.md`

---

## Final Baseline (2026-03-21, Pixel 10 Pro XL, Android 16, API 36)

| Canvas | max (ms) | slow% | frames | Status |
|--------|----------|-------|--------|--------|
| PlaybackTickBg | 55.71 | 36.8% | 19 | 🔶 MISS-only (HIT <8ms ✅) |
| PianoRollTickBg | 55.22 | 12.4% | 1449 | 🔶 改善 (was 36.9%) |
| PianoRollCanvas | 47.97 | 0.3% | 1557 | ✅ 実用的 |
| PianoKeysCanvas | 39.15 | 0.3% | 1487 | ✅ 実用的 |
| TrackCanvas | 32.34 | 11.3% | 62 | 🔶 要モニタリング |
| PianoRollKeysBg | 31.23 | 0.3% | 1474 | ✅ 実用的 |
| PhonemeCanvas | 29.02 | 0.1% | 1455 | ✅ 実用的 |
| PianoRollPitchCanvas | 22.64 | 0.1% | 1568 | ✅ 実用的 |
| ExpressionCanvas | 0.79 | 0% | 1457 | ✅ TARGET MET |
| PlaybackPosCanvas | 0.37 | 0% | 52 | ✅ TARGET MET |
| TimeLineCanvas | 0.00 | 0% | 7 | ✅ TARGET MET |

---

## Key Achievements

- 全11 Canvas が Profiler パイプライン内 (Phase 2 前は 7本)
- 6本が <8ms 目標達成、3本が slow% <0.5% (実用的水準)
- PianoRollTickBg slow% 36.9% → 4.1–12.4%、TrackCanvas 40% → 5–15%
- ノート描画修正 (auto-select VoicePart on load)
- ズームジェスチャー修正 (GestureState.Zoom in HandleTouchUp)
- ノートドラッグ/リサイズ修正 (BUG-A PanStart drift / BUG-B ZoomY inversion / BUG-C stuck gesture)
- ExpressionCanvas 可視化修正 (ExpHeight 50 → 150)
- 104行デッドコード削除 (DrawRectangle/DrawLyrics)
- per-frame LINQ アロケーション 6件排除
- PianoRollTickBg パンドリフト時ビットマップ再利用 (38MB GC 削減)
- SKImage GPU テクスチャパターン確立・SKILL.md に記録

---

## Key Commits

| コミット | 内容 |
|---------|------|
| `6ce408b` | P2-1 PanX 二重スロットル削除 + P2-2 PaintSurfaceProfiler |
| `eeb5df0` | P2-5a PlaybackTickBg + P2-5b PianoKeysCanvas ビットマップキャッシュ |
| `633177a` | P2-5c PianoRollTickBg キャッシュ + SKImage GPU テクスチャ最適化 |
| `bb28e4a` | P2-B1/B2 ノート描画修正 + DrawableNotes 最適化 (31→20ms) |
| `c6afc55` | BUG-A(PanStart drift) + BUG-B(ZoomY inversion) 修正 |
| `5798ac6` | BUG-C(stuck gesture) + UI1(ExitPopup button) 修正 |
| `4f645b9` | P2-C Stage C cleanup (ExprCanvas/LINQ/dead code) |
| `a3dc4ac` | P2-D1 Opt-D リバート (DrawBitmap → SKImage 復元) |
| `abba8da` | P2-D1 Opt-A/C/E 完了 (MISS slow 22.9% → 4.1%) |
| `1258aba` | Phase 2 COMPLETE — final baseline 記録 |

---

## Known Issues (deferred)

### BUG-D: PianoRoll Shadow boundary misalignment
- **Status**: OPEN (deferred to Phase 3+)
- **Symptom**: グレーシャドウ領域が編集可能パート領域に侵食。パート先頭付近のノートがシャドウで一部隠れる。
- **Investigation (2026-03-21)**: `DrawPianoRollShadow()` 座標計算は正しい。原因候補: PianoKeysCanvas MAUI Shadow bleed、EditingPart.position ミスマッチ、upstream 既存問題。
- **Next step**: デバイスで `editingPart.position/ZoomX/PanX` をログ出力して確認。
- **Commit**: `3742545` (BUG-D 初記録)

---

## Phase 2 Decision Log (summary)

| Date | Decision |
|------|---------|
| 2026-03-18 | PanX Rx.Throttle 削除 — GP 16ms + Rx 16.6ms 二重スロットル解消 |
| 2026-03-18 | #if DEBUG 計測基盤 — Release 計測必要時は Preferences フラグに切替 |
| 2026-03-19 | SKImage.FromBitmap + DrawImage(srcRect) — CPU blit 10-25ms → GPU 1.7-6.7ms |
| 2026-03-19 | Cache margin 1.0 (3×) — 0.5 は MISS 2× 増加で revert |
| 2026-03-20 | BUG-A/B/C は upstream 既存バグと判定 — P2-B2 DrawableNotes 変更は無関係 |
| 2026-03-21 | NEVER DrawBitmap rule — Opt-D 実装後 slow=100% 回帰、CPU blit の既知劣性を再確認 |
| 2026-03-21 | Phase 2 closed — 全11Canvas Profiler 内、実用的水準達成。Phase 2.5 (リファクタ) へ移行 |
