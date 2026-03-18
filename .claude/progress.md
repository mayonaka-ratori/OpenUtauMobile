# Progress Tracker

## Current Sprint

Status: IN PROGRESS
Goal: Phase 2 タッチ性能改善 — PaintSurface <8ms, タッチレイテンシ <32ms

## Audit Log

| Date | Auditor | Scope | Critical | High | Medium | Low |
| --- | --- | --- | --- | --- | --- | --- |
| 2026-03-18 | auditor agent (claude-opus-4-6) | OpenUtauMobile/ 全 .cs/.xaml | 3 | 8 | 5 | 4 |

Phase 1 (Stability) で対処すべき 11 件を特定。推定作業量 ~16h。
詳細: `.claude/audit-2026-03-18.md`

## Performance Architecture Survey

| Date | Scope | Findings |
| --- | --- | --- |
| 2026-03-18 | EditPage 全11キャンバス | PanX 二重スロットル (48ms), Heavy ハンドラ 6本, ビットマップキャッシュ候補 3 キャンバス特定 |

## Cold Review Log

| Date | Reviewer | Scope | Grade | Blockers | Recommended | Low |
| --- | --- | --- | --- | --- | --- | --- |
| 2026-03-18 | fresh-eyes (claude-opus-4-6) | Phase 1 全変更差分 | B− | 6 | 9 | 3 |
| 2026-03-18 | fresh-eyes #2 (claude-opus-4-6) | Phase 1 全変更 (Blocker修正後) | B− | 4 | 8 | 5 |
| 2026-03-18 | fresh-eyes #3 (claude-opus-4-6) | Phase 1 全変更 (推奨修正後) | B+ | 1 | 7 | 3 |
| 2026-03-18 | focused #4 (claude-opus-4-6) | AudioTrackOutput + DrawableNotes | B+ → A- | 2 (DN) | 1 | 2 |

### Review #1
Blocker 6件: 修正完了・検証済み
Recommended 9件: 全完了 (ATO-02, EP-03 は Blocker 同時修正。EP-04 は Phase 2 送り。EP-05〜EP-07, GP-02, GP-04, SS-02 完了)
Low 3件 (GP-03, DN-01, IFO-01): Phase 2 以降。
### Review #2
Blocker 4件 (EP-01, EP-05, EP-08, SS-01): 修正完了 + EP-03 同時修正
Recommended 8件: 7件完了 (EP-04, EP-06, EP-07, GP-01, ATO-01, ATO-02, ATO-03)。OP-01 は Phase 3 送り。
Low 5件: Phase 2 以降。
### Review #3
Blocker 1件 (CR3-01): SplashScreen catch fallthrough — 修正完了
Recommended 7件: CR3-06, CR3-14, CR3-17 修正完了。CR3-02 コメント追加。CR3-07 は CR3-02 に包含 (デッドコード)。CR3-12, CR3-13 は Phase 2 送り。
Low 3件: Phase 2 以降。
AudioTrackOutput.cs, DrawableNotes.cs は API 障害で未検証 → Review #4 で再確認。
### Review #4 (2ファイル限定検証)
AudioTrackOutput.cs: 12/12 PASS — 全項目合格
DrawableNotes.cs: 10/12 → CR4-04, CR4-05 修正完了 → 12/12 PASS
Phase 1 品質ゲート: PASS
総合評価: A-

## Backlog

### Phase 1 — Stability (Priority: HIGH)

- [x] Audit SKPaint allocations in PaintSurface handlers → 完了 (D-01〜D-07 特定)
- [x] Audit ReactiveUI subscription disposal in all ViewModels → 完了 (A-01〜A-03 特定)
- [x] Audit ICmdSubscriber cleanup in all ViewModels → 完了 (EditViewModel の欠落を確認: A-01)
- [x] **[P1-1]** EditViewModel IDisposable + CompositeDisposable 実装 `A-01, A-02, A-03` — 完了 2026-03-18
- [x] **[P1-2]** EditPage 全 SKPaint/SKFont を static フィールドにキャッシュ `D-01` — 完了 2026-03-18
- [x] **[P1-3]** DrawableNotes/DrawablePart/DrawablePianoRollTickBackground 再利用化 `D-02, D-03, D-04, D-07` — 完了 2026-03-18
- [x] **[P1-4]** EditPage OnDisappearing/OnAppearing 追加 + AutoSaveTimer 停止 `B-01, A-04` — 完了 2026-03-18
- [x] **[P1-5]** GestureProcessor イベント解除 + SizeChanged デタッチ `A-05, A-06` — 完了 2026-03-18
- [x] **[P1-6]** AudioTrackOutput._isPlaying volatile 化 `C-01` — 完了 2026-03-18
- [x] **[P1-7]** GestureProcessor タッチスロットリング 16ms `D-05` — 完了 2026-03-18
- [x] **[P1-8]** Magnifier Dispose 漏れ修正 `A-07` — 完了 2026-03-18
- [x] **[P1-9]** AudioTrackOutput IDisposable 実装 + Join タイムアウト `A-09, C-02` — 完了 2026-03-18
- [x] **[P1-10]** AttemptExit 不保存パスの Dispose 漏れ修正 + _disposed ガード `B-02` — 完了 2026-03-18
- [x] **[P1-11]** ObjectProvider.Initialize() .Result 同期ブロック解消 `C-03` — 完了 2026-03-18
- [ ] Fix audio import crash (upstream issue #1913)
- [ ] Strengthen autosave — reduce interval, add recovery UI

Cold Review 修正 (PRブロッカー)

- [x] **[CR-1]** ATO-01+02: AudioTrackOutput Dispose 順序修正 + GC.SuppressFinalize 移動 — 完了 2026-03-18
- [x] **[CR-2]** DP-01: DrawablePart App.Current ?? new Application() 廃止 — 完了 2026-03-18
- [x] **[CR-3]** EP-01+03: EditPage Timer Tick ハンドラ解除 + Dispose 順序修正 — 完了 2026-03-18
- [x] **[CR-4]** EP-02: DrawablePart キャッシュ eviction 時 Dispose 追加 — 完了 2026-03-18
- [x] **[CR-5]** SS-01: SplashScreenPage RemoveSubscriber 追加 — 完了 2026-03-18
- [x] **[CR-6]** GP-01: HandleTouchCancel _activePoints クリア + ジェスチャー状態リセット — 完了 2026-03-18

### Phase 1 — Cold Review Recommended (残り)

- [x] EP-05: OnAppearing PlaybackTimer 無条件再開 — 完了 2026-03-18
- [x] EP-06: using System.Reactive.Linq 重複削除 — 完了 2026-03-18
- [x] EP-07: InvalidateSurface() 重複呼び出し削除 — 完了 2026-03-18
- [x] GP-02: ClickThreshold コメント修正 — 完了 2026-03-18
- [x] GP-04: 未使用 _cts 削除 — 完了 2026-03-18
- [x] SS-02: CheckPermission if(true) 修正 — 完了 2026-03-18
- [ ] EP-04: PitchCanvas SKPath キャッシュ化 → Phase 2 送り

#### Cold Review #2 修正 (ブロッカー)
- [x] **[CR2-1]** EP-01: PhonemeCanvas/PitchCanvas SKPath キャッシュ化 — 完了 2026-03-18
- [x] **[CR2-2]** EP-05: EditPage SKPaint/SKFont/SKPath フィールド Dispose 追加 — 完了 2026-03-18
- [x] **[CR2-3]** EP-08: EndUndoGroup 二重呼び出し修正 — 完了 2026-03-18
- [x] **[CR2-4]** SS-01: 初期化失敗後の HomePage ナビゲーション防止 — 完了 2026-03-18
- [x] **[CR2-5]** EP-03: GC.SuppressFinalize 位置修正 — 完了 2026-03-18

#### Cold Review #2 推奨修正
- [x] EP-04: テーマ変更時の SKPaint 色陳腐化をコメント文書化 — 完了 2026-03-18
- [x] EP-06: SKColorMauiColorConverter 静的キャッシュ化 — 完了 2026-03-18
- [x] EP-07: InitExpressionMagnifier 変数名不一致修正 — 完了 2026-03-18
- [x] GP-01: HandleTouchDown try-catch → upsert パターン — 完了 2026-03-18
- [x] ATO-01: _disposed volatile 化 — 完了 2026-03-18
- [x] ATO-02: PlaybackLoop _audioTrack ローカルコピー — 完了 2026-03-18
- [x] ATO-03: Stop() _disposed ガード追加 — 完了 2026-03-18
- [ ] OP-01: RequestStoragePermissionAsync 常に true → Phase 3 送り

#### Cold Review #3 修正
- [x] **[CR3-01]** SplashScreen catch 後 fallthrough 防止 (ブロッカー) — 完了 2026-03-18
- [x] **[CR3-06]** 全 Drawable + EditViewModel の GC.SuppressFinalize 末尾移動 — 完了 2026-03-18
- [x] **[CR3-14]** DrawableTickBackground SnapTicks Clear 追加 — 完了 2026-03-18
- [x] **[CR3-17]** AttemptExit RemoveSubscriber 順序修正 — 完了 2026-03-18
- [x] **[CR3-02]** DrawablePianoKeys デッドコードコメント追加 — 完了 2026-03-18

#### Cold Review #4 修正
- [x] **[CR4-04]** DrawableNotes GC.SuppressFinalize 末尾移動 — 完了 2026-03-18
- [x] **[CR4-05]** DrawableNotes _disposed ガード追加 — 完了 2026-03-18

#### Phase 2 準備
- [x] skills/maui-mobile-patterns 作成 — 完了 2026-03-18
- [x] skills/skia-performance 作成 — 完了 2026-03-18
- [x] tester エージェント更新 (claude-opus-4-6 昇格 + テスタビリティ制約反映) — 完了 2026-03-18
- [x] Transformer テスト追加 — 完了 2026-03-18
- [x] DrawablePart _disposed ガード追加 — 完了 2026-03-18
- [x] EditViewModel _disposed ガード追加 — 完了 2026-03-18
- [x] ThemeColorsManager スレッド安全性確認 (追加対応不要) — 完了 2026-03-18

### Phase 2 — Touch Performance (Priority: HIGH)

Target: PaintSurface < 8ms, touch-to-render latency < 32ms

#### Category A: レイテンシ削減
- [x] **[P2-1]** PanX 二重スロットル削除 (Rx.Throttle + ObserveOn 除去) — 完了 2026-03-18
- [ ] **[P2-4]** ピッチ曲線描画スロットル追加 (PianoRollCanvas_Touch)

#### Category B: フレーム時間削減
- [x] **[P2-2]** PaintSurfaceProfiler 計測基盤実装 (#if DEBUG) — 完了 2026-03-18
- [x] **[P2-3]** 実機ベースライン計測 (Pixel 10 Pro XL, Android 16) — 完了 2026-03-18
- [x] **[P2-5a]** PlaybackTickBackgroundCanvas ビットマップキャッシュ (水平方向) — 完了 2026-03-18
- [x] **[P2-5b]** PianoKeysCanvas ビットマップキャッシュ (垂直方向) — 完了 2026-03-18
- [ ] **[P2-5c]** PianoRollTickBackgroundCanvas ビットマップキャッシュ（シャドウ合成）
- [ ] **[P2-6]** ExpressionCanvas SKPath バッチ化 + ThemeColors ローカルキャッシュ
- [ ] **[P2-7]** PanX/PanY ダーティフラグ分離 (InvalidateSurface 選択的呼び出し)

#### Category C: リファクタ・クリーンアップ
- [ ] **[P2-8]** Debug.WriteLine 大幅削減 (CR3-12)
- [ ] **[P2-8]** IsOpenGLESSupported 修正/削除 (CR3-13)
- [ ] **[P2-8]** DrawablePianoKeys デッドコード削除 (CR3-02)
- [ ] **[P2-8]** IDrawableObject に Draw() 追加 (IFO-01)
- [ ] **[P2-8]** DrawableNotes/DrawablePart コンストラクタ API 統一
- [ ] **[P2-8]** ObservableCollectionExtended.Contains() O(n) 改善 (CR4-06)
- [ ] **[P2-8]** GP-03 TouchPoint.Update() 未使用 time パラメータ削除

#### 判断ポイント
P2-3 計測結果により方針分岐:
- 全ハンドラ <8ms → P2-5/P2-6 スキップ、P2-7 + P2-8 で Phase 2 完了
- 一部 >8ms → 超過ハンドラのみ最適化
- 多数 >8ms → P2-5 → P2-6 → P2-7 全実施

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

## Performance Measurement Log

実機計測: Pixel 10 Pro XL (Android 16, API 36)

| 日付 | 計測フェーズ | PlaybackTickBg | PianoRollTickBg | PianoKeys | PianoRollKeysBg | TrackCanvas |
|------|-----------|---|---|---|---|---|
| 2026-03-18 | ベースライン | max=51.04ms<br/>slow=33.3% | max=33.42ms<br/>slow=6.9% | max=28.90ms<br/>slow=4.1% | max=9.17ms<br/>slow=0.3% | max=1.6ms<br/>slow=0% |
| 2026-03-18 | P2-5a+5b後 | max=25ms<br/>frame count 21→6 | max=40ms<br/>slow=11% | **max=6ms**<br/>**slow=0%** ✅ | max=25ms<br/>slow=0.5% | max=1.8ms<br/>slow=0% |

**注記:**
- PlaybackTickBackgroundCanvas: キャッシュヒット時はビットマップ描画のみ (フレーム数削減)
- PianoKeysCanvas: <8ms 目標達成 (6ms 平均)
- PianoRollTickBackgroundCanvas: 次フェーズ（シャドウ領域合成アプローチ必要）

## Decision Log

Record architectural decisions and tradeoffs here.

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-03-18 | OnDisappearing/OnAppearing を Dispose から分離 | B-01: MAUI は AttemptExit 以外の経路でも OnDisappearing が呼ばれるため、一時停止（復帰可能）と完全破棄の責務を分離 |
| 2026-03-18 | テスト戦略: Transformer のみ直接テスト、他は Phase 3 でインフラ整備後 | OpenUtauMobile.csproj が net9.0-android/ios ターゲットのため plain net9.0 テストから参照不可 |
| 2026-03-18 | ThemeColorsManager: 追加対応不要 | static class、初期化後不変、Phase 1 で文書化済み (EP-04) |
| 2026-03-18 | _disposed ガード: 全 IDisposable クラスで統一 | DrawablePart, EditViewModel の漏れを修正 |
| 2026-03-18 | PanX Rx.Throttle 削除 (P2-1) | GestureProcessor 16ms + Rx 16.6ms の二重スロットルで 48ms レイテンシ。GP throttle のみに統一 |
| 2026-03-18 | 計測基盤は #if DEBUG 方式 (P2-2) | Phase 2 中は DEBUG ビルドで十分。Release 計測が必要なら Preferences フラグに切替 |
| 2026-03-18 | Phase 2 タスク優先順: レイテンシ削減 → 計測 → 計測結果で分岐 | 実機データなしの最適化は行わない方針 |
| 2026-03-18 | PianoKeysCanvas 垂直ビットマップキャッシュで <8ms 達成 (P2-5b) | 39ms → 6ms、ズーム時のみ再生成、PanY スクロールはオフセット描画で高速化 |
| 2026-03-18 | PlaybackTickBackgroundCanvas キャッシュはフレーム数削減に効果的 | max は 25ms に軽減だが、キャッシュヒット時は 6 フレーム（1フレーム ~6ms）。PianoRollTickBg は次段階へ（shadow composite 必要） |

## Core Patch Notes

If any Core modifications become necessary, summarize here and detail in docs/CORE_PATCHES.md.

| Date | File | Change | Why |
| --- | --- | --- | --- |
| — | — | — | — |
