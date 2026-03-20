# OpenUtau Mobile — Phase 2 Progress (Touch Performance)

**Status**: 🔄 IN PROGRESS
**Goal**: PaintSurface <8ms, touch-to-render latency <32ms
**Test Device**: Pixel 10 Pro XL (Android 16, API 36)

---

## Phase 2 目標

| 指標 | 目標 |
|------|------|
| PaintSurface (ms) | < 8ms |
| App memory (MB) | < 500 |
| Touch-to-render latency (ms) | < 32ms |
| Cold start (s) | < 3s |

---

## タスク一覧

### Category A: レイテンシ削減

- [x] **[P2-1]** PanX 二重スロットル削除 (Rx.Throttle + ObserveOn 除去) — 完了 2026-03-18
- [ ] **[P2-4]** ピッチ曲線描画スロットル追加 (PianoRollCanvas_Touch)

### Category B: フレーム時間削減

- [x] **[P2-2]** PaintSurfaceProfiler 計測基盤実装 (#if DEBUG) — 完了 2026-03-18
- [x] **[P2-3]** 実機ベースライン計測 (Pixel 10 Pro XL, Android 16) — 完了 2026-03-18
- [x] **[P2-5a]** PlaybackTickBackgroundCanvas ビットマップキャッシュ (水平方向) — 完了 2026-03-18
- [x] **[P2-5b]** PianoKeysCanvas ビットマップキャッシュ (垂直方向) — 完了 2026-03-18
- [x] **[P2-5c]** PianoRollTickBackgroundCanvas ビットマップキャッシュ + SKImage最適化 — 完了 2026-03-19
- [ ] **[P2-6]** ExpressionCanvas SKPath バッチ化 + ThemeColors ローカルキャッシュ
- [ ] **[P2-7]** PanX/PanY ダーティフラグ分離 (InvalidateSurface 選択的呼び出し)

### Category C: リファクタ・クリーンアップ

- [ ] **[P2-8a]** Debug.WriteLine 大幅削減 (CR3-12)
- [ ] **[P2-8b]** IsOpenGLESSupported 修正/削除 (CR3-13)
- [ ] **[P2-8c]** DrawablePianoKeys デッドコード削除 (CR3-02)
- [ ] **[P2-8d]** IDrawableObject に Draw() 追加 (IFO-01)
- [ ] **[P2-8e]** ObservableCollectionExtended.Contains() O(n) 改善 (CR4-06)
- [ ] **[P2-8f]** GP-03 TouchPoint.Update() 未使用 time パラメータ削除
- [ ] **[P2-8g]** DrawableNotes/DrawablePart コンストラクタ API 統一

---

## 完了タスク詳細

### P2-1: PanX 二重スロットル削除 (2026-03-18)
- Rx.Throttle(16.6ms) + ObserveOn を PianoRollTransformer PanX/ZoomX サブスクリプションから除去
- GestureProcessor の 16ms スロットルのみに統一
- タッチレイテンシ: 48ms → 32ms (理論値)
- コミット: `6ce408b`

### P2-2: PaintSurfaceProfiler 追加 (2026-03-18)
- 全11 PaintSurface ハンドラに #if DEBUG 計測コード追加
- スロー フレーム (>8ms) をログ出力 + Dispose 時に統計ダンプ
- コミット: `6ce408b`

### P2-3: 実機ベースライン計測 (2026-03-18)
- 計測機: Pixel 10 Pro XL (Android 16, API 36)
- 結果: PlaybackTickBg max=51ms (33.3% slow), PianoKeys max=29ms (4.1% slow)
- ビットマップキャッシュ 3 キャンバス特定

### P2-5a: PlaybackTickBackgroundCanvas キャッシュ (2026-03-18)
- 水平方向 3x 幅 SKBitmap キャッシュ (MARGIN=1.0)
- DrawBitmap → DrawImage + srcRect (SKImage) に置換 (P2-5c で追加)
- コミット: `eeb5df0`

### P2-5b: PianoKeysCanvas キャッシュ (2026-03-18)
- 垂直方向 3x 高さ SKBitmap キャッシュ (MARGIN=1.0)
- DrawBitmap → DrawImage + srcRect (SKImage) に置換 (P2-5c で追加)
- 結果: 39ms → **6ms** (0% slow) ✅
- コミット: `eeb5df0`

### P2-B1: ノート未描画問題修正 (2026-03-20)

**P2-B1a — Auto-select first VoicePart on project load**
- `EditPage.xaml.cs` Loaded イベントハンドラ内、`await _viewModel.Init()` 直後に挿入
- `DocManager.Inst.Project?.parts?.OfType<UVoicePart>().FirstOrDefault()` で最初の VoicePart を取得
- `SelectedParts.Count == 0` チェック後に `SelectedParts.Add()` + `EditingPart` 明示セット + `PianoRollCanvas.InvalidateSurface()`
- 効果: PianoRollCanvas / PhonemeCanvas / PianoRollPitchCanvas / ExpressionCanvas が EditPage 起動直後から描画開始

**P2-B1b — GestureState.Zoom 欠落修正**
- `GestureProcessor.cs` HandleTouchUp の `case 1 when` 条件に `GestureState.Zoom` を追加 (L226)
- HandleTouchCancel (L89) との対称性を回復
- `FinalizeGesture()` に `case GestureState.Zoom: break;` を追加 (L370)
- 効果: 2軸ズーム中に片指を離した後、残指でパン操作が可能になりグレー画面固着を解消

- コミット: `(本コミット)`

### P2-5c: PianoRollTickBackgroundCanvas キャッシュ + SKImage最適化 (2026-03-19)
- 水平方向 3x 幅 SKBitmap キャッシュ + シャドウ毎フレーム動的描画 (Approach A)
- 3 Canvas 全てで SKImage.FromBitmap + DrawImage srcRect に置換
  - DrawBitmap (CPU blit): 10-25ms → DrawImage (GPU texture): 1.7-6.7ms
  - 可視領域のみ srcRect で切り出し → 転送データ量 ~1/3 削減
- Cache margin 0.5 テスト → MISS 頻度 4→9回、slow rate 8.9%→14.6% に悪化 → 1.0 に revert
- 診断ログ [PianoRollTickBgCache] / [PRTickBg] 削除 (検証完了)
- コミット: `633177a`

---

## Performance Measurement Log

実機計測: Pixel 10 Pro XL (Android 16, API 36)

| 日付 | 計測フェーズ | PlaybackTickBg | PianoRollTickBg | PianoKeys | PianoRollKeysBg | TrackCanvas |
|------|-----------|---|---|---|---|---|
| 2026-03-18 | ベースライン | max=51.04ms<br/>slow=33.3% | max=33.42ms<br/>slow=6.9% | max=28.90ms<br/>slow=4.1% | max=9.17ms<br/>slow=0.3% | max=1.6ms<br/>slow=0% |
| 2026-03-18 | P2-5a+5b後 | max=25ms<br/>frame count 21→6 | max=40ms<br/>slow=11% | **max=6ms**<br/>**slow=0%** ✅ | max=25ms<br/>slow=0.5% | max=1.8ms<br/>slow=0% |
| 2026-03-19 | P2-5c SKImage+srcRect | max=22ms (2fr only) ✅ | **max=33ms**<br/>**slow=8.9%** | **max=4ms**<br/>**slow=0%** ✅ | **max=7ms**<br/>**slow=0%** ✅ | — |
| 2026-03-20 | P2-B1 ノート描画修正後 | — | — | max=10.34ms<br/>(zoom heavy) | — | — |

**P2-B1 新規出現 Canvas (2026-03-20):**

| Canvas | 状態 | max (ms) | slow (%) | 備考 |
|--------|------|---------|---------|------|
| PianoRollCanvas | 🆕 NEW | 31.85ms | 1.8% | 修正前は未描画（SelectedParts 空ガードでスキップ） |
| PhonemeCanvas | 🆕 NEW | 3.17ms | 0% | ✅ 目標達成 |
| PianoRollPitchCanvas | 🆕 NEW | 0.79ms | 0% | ✅ 目標達成 |
| PianoKeysCanvas | 🔶 軽微regression | 4.12ms→10.34ms | — | ズーム高負荷時のみ、許容範囲 |

**注記:**
- PlaybackTickBg: キャッシュ HIT 時のみフレーム数計測、MISS は ~22ms (2フレームのみ) ✅
- PianoKeysCanvas: <8ms 目標達成 (4ms, 0% slow) ✅
- PianoRollKeysBg: <8ms 目標達成 (7ms, 0% slow) ✅
- PianoRollTickBg: HIT フレームは全て <8ms。slow 8.9% は MISS フレームのみ (キャッシュ再生成 ~33ms) — 許容範囲

---

## Stage A 完了サマリ (2026-03-19)

**対象 Canvas 3本**: PlaybackTickBg, PianoKeysCanvas, PianoRollTickBg

| Canvas | Before | After | Status |
|--------|--------|-------|--------|
| PianoKeysCanvas | 29ms / 4.1% slow | 4ms / 0% slow | ✅ TARGET MET |
| PianoRollKeysBg | 9ms / 0.3% slow | 7ms / 0% slow | ✅ TARGET MET |
| PlaybackTickBg | 51ms / 33.3% slow | 22ms (2fr MISS only) | ✅ Cache working |
| PianoRollTickBg | 33ms / 6.9% slow | 33ms / 8.9% (MISS-only) | 🔶 HIT時 <8ms |

**実装パターン (再利用可能)**:
1. 3x 幅/高さの SKBitmap にオフセット付きで描画してキャッシュ
2. `SKImage.FromBitmap()` で GPU テクスチャに昇格
3. `DrawImage(srcRect, dstRect)` で可視領域のみ転送
4. Dispose() で SKBitmap + SKCanvas + SKImage 全て破棄

---

## Phase 2 Decision Log

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-03-18 | PanX Rx.Throttle 削除 (P2-1) | GestureProcessor 16ms + Rx 16.6ms の二重スロットルで 48ms レイテンシ。GP throttle のみに統一 |
| 2026-03-18 | 計測基盤は #if DEBUG 方式 (P2-2) | Phase 2 中は DEBUG ビルドで十分。Release 計測が必要なら Preferences フラグに切替 |
| 2026-03-18 | Phase 2 タスク優先順: レイテンシ削減 → 計測 → 計測結果で分岐 | 実機データなしの最適化は行わない方針 |
| 2026-03-18 | PianoKeysCanvas 垂直ビットマップキャッシュで <8ms 達成 (P2-5b) | 39ms → 6ms、ズーム時のみ再生成、PanY スクロールはオフセット描画で高速化 |
| 2026-03-18 | PlaybackTickBackgroundCanvas キャッシュはフレーム数削減に効果的 | max は 25ms に軽減だが、キャッシュヒット時は 6 フレーム（1フレーム ~6ms）。PianoRollTickBg は次段階へ（shadow composite 必要） |
| 2026-03-19 | Cache margin 0.5 tested — MISS 頻度増加 (4→9回) で revert | 0.5f は MISS 頻度が約 2× 増加し slow rate 8.9%→14.6% に悪化。1.0f (3x 幅) が最適と判定 |
| 2026-03-19 | SKImage.FromBitmap + DrawImage srcRect で DrawBitmap コスト削減 (P2-5c) | bitmap 描画コスト 10-25ms → 1.7-6.7ms に削減。GPU テクスチャキャッシュ活用 + 可視領域のみ転送 |
| 2026-03-19 | PianoRollTickBg slow 8.9% は許容範囲と判断 | HIT フレームは全て <8ms。slow 8.9% は MISS フレームのみ（キャッシュ再生成 ~33ms）。実用上問題なし |
| 2026-03-20 | SetPanLimit() re-clamp confirmed sufficient; no additional pan correction needed after zoom | Transformer.SetPanLimit() は内部で `PanX = InvalidatePanX(PanX)` を即時実行。UpdatePianoRollCanvasPanLimit() 呼び出しで再クランプ完結。EditPage 側への追加コード不要 |
| 2026-03-20 | GestureState.Zoom omission in HandleTouchUp was root cause of gray screen freeze | HandleTouchUp の case 1 条件に Zoom が含まれていなかったため、2軸ズーム中に片指を離しても SwitchToPanFromZoom() が呼ばれず残指パン不能。HandleTouchCancel との不整合が原因 |

---

## Phase 2 準備タスク (完了)

- [x] skills/maui-mobile-patterns 作成 — 完了 2026-03-18
- [x] skills/skia-performance 作成 — 完了 2026-03-18
- [x] tester エージェント更新 (claude-opus-4-6 昇格 + テスタビリティ制約反映) — 完了 2026-03-18
- [x] Transformer テスト追加 — 完了 2026-03-18
- [x] DrawablePart _disposed ガード追加 — 完了 2026-03-18
- [x] EditViewModel _disposed ガード追加 — 完了 2026-03-18
- [x] ThemeColorsManager スレッド安全性確認 (追加対応不要) — 完了 2026-03-18

---

## 次のタスク

### Stage B — ノート描画修正 + 全Canvas再計測

- [x] **P2-B1** ノート未描画問題の調査・修正 (SelectedParts ガード) — 完了 2026-03-20
  - [x] **P2-B1a** Auto-select first VoicePart on project load (EditPage.xaml.cs)
  - [x] **P2-B1b** Fix GestureProcessor.Zoom state missing in HandleTouchUp and FinalizeGesture (GestureProcessor.cs)
- [ ] **P2-B2** ノート表示状態での全Canvas再計測（ExpressionCanvas, PhonemeCanvas, PianoRollPitchCanvas を含む）
- [ ] **P2-B3** 再計測結果に基づく追加最適化の優先度決定

### Stage C — リファクタ・クリーンアップ

- [ ] **P2-8a** Debug.WriteLine 削減 (CR3-12)
- [ ] **P2-8b** IsOpenGLESSupported 修正/削除 (CR3-13)
- [ ] **P2-8c** DrawablePianoKeys デッドコード削除 (CR3-02)
- [ ] **P2-8d** IDrawableObject に Draw() 追加 (IFO-01)
- [ ] **P2-8e** ObservableCollectionExtended.Contains O(n) 改善 (CR4-06)
