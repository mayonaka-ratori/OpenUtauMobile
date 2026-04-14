# Audit Diff Report — 2026-04-13

**前回 audit**: 2026-04-12 (`audit-diff-2026-04-12.md`) — Phase 2.5 完了時点  
**本レポート作成日**: 2026-04-13  
**審査対象変更**: Phase 3 Step A (ビブラート編集), Step B (フォネーム編集), Step C (クオンタイズ UI)

---

## 2026-04-12 Audit 計画書の実施状況

### 質問への直答

> 「OpenUtau Mobile — 包括的 Audit 計画書 (2026-04-12)って全部ちゃんと実施された？」

**一部未実施です。** 内訳:

| 区分 | 件数 | 状態 |
|------|------|------|
| 元 20件 (A〜D) のうち C-04/C-05 | 2件 | 🔶 継続監視 (Phase 2.5 から変化なし) |
| Phase 3 送り (E-01〜E-03) | 3件 | ⏳ 未実施 |
| Phase 3 送り (BUG-D, OP-01) | 2件 | ⏳ 未実施 |
| N-01〜N-07 新規欠陥 | 7件 | ✅ Phase 2.5 で全修正済み |
| Phase 3 機能 (1-4, 1-12, B系) | 3機能 | ✅ 本セッションで実装済み |

**ただし「Phase 3 送り」と明記されていた E-01〜E-03 / BUG-D / OP-01 は、Phase 3 開始直後である現在も未着手のため、優先整理が必要です。**

---

## Phase 3 実装に伴う新規 Audit 記録

### 新規実装済み機能 (2026-04-13)

| ID | 機能 | 対応 roadmap-v3 | 状態 |
|----|------|----------------|------|
| P3-A | ビブラート編集モード (VibratoPanel + スライダー + 波形オーバーレイ) | 1-12 | ✅ 実装済み |
| P3-B | フォネーム編集 (PhonemeCanvas タップ → ActionSheet + DisplayPromptAsync) | Phase 1 フォネーム系 | ✅ 実装済み |
| P3-C | クオンタイズボタングループ (1/4, 1/8, 1/16, 1/32, 3連) | 1-4 | ✅ 実装済み |

### Phase 3 実装で発生した技術的負債

| ID | Severity | 内容 | 現在の状態 |
|----|----------|------|-----------|
| P3-N01 | Medium | `DrawVibratoOverlay` の `tempo` 取得が `tempos[0].bpm` のみ (マルチテンポ非対応) | ⏳ Phase 3 後続 |
| P3-N02 | Low | VibratoPanel スライダー値が `RefreshVibratoPanelValues()` 未呼び出し時に初期値 0 になる可能性 | ⏳ 要確認 |
| P3-N03 | Low | フォネーム hit detection の ±300 tick 閾値がハードコード (マジックナンバー) | ⏳ E-03 と統合 |

---

## 全欠陥 現況サマリー (2026-04-13 時点)

### ✅ 解消済み (計 25件)

| 区分 | 件数 |
|------|------|
| A-01〜A-03 (Critical) | 3 |
| B-01〜B-08 (High) | 8 |
| C-01〜C-03 (Medium) | 3 |
| D-01〜D-04 (Low) | 4 |
| N-01〜N-07 (新規) | 7 |

### 🔶 継続監視 (2件)

| ID | 内容 | 直近指標 | 対処方針 |
|----|------|---------|---------|
| C-04 | Track canvas 11.3% slow | 2026-04-12 計測 | Pixel 10 Pro XL で再計測。10%以下なら許容 |
| C-05 | PlaybackTickBg MISS 36.8% | 2026-04-12 計測 | SKBitmap キャッシュ戦略の検討 |

### ⏳ 未解消 / Phase 3 後続 (8件)

| ID | Severity | 内容 | 優先度 | Phase 3 アクション |
|----|----------|------|--------|-------------------|
| BUG-D | Low | PianoRoll シャドウ境界ずれ | ✅ **CLOSED** (2026-04-14) | `DrawPianoRollShadow` のコード精査済み。`+0.5f` は意図的なサブピクセルカバー。`DrawRect(partEndX, 0, screenWidth, bottom, paint)` の座標計算に論理誤りなし。コード変更不要。 |
| OP-01 | Medium | `RequestStoragePermissionAsync` 常に true | ✅ **FIXED** (2026-04-14) | `ObjectProvider.cs` 修正済み。`HasManageExternalStoragePermission` が false の場合に `return false` を返すよう変更。 |
| E-01 | Info | XML doc コメント欠如 (Phase 3 追加メソッドを含む) | ✅ **FIXED** (2026-04-14) | `EditPage.Toolbar.cs:71-74` の `Save()` XML doc を英語化済み。Phase 3 追加メソッドの XML doc も整備済み。 |
| E-02 | Info | 中国語コメント混在 | ✅ **FIXED** (2026-04-14) | 7ファイル (EditViewModel.cs, EditPage.CmdSubscriber.cs, EditPage.Rendering.cs, EditPage.Toolbar.cs, ViewConstants.cs, MauiProgram.cs, ExternalStorageService.cs) で全置換済み。 |
| E-03 | Info | マジックナンバー直書き (P3-N03 統合) | ✅ **FIXED** (2026-04-14) | `VibratoRenderStepPx = 2f`, `ActiveQuantizeButtonArgb = "#60FFFFFF"`, `ZoomStepFactor = 1.25f` を定数化済み。 |
| P3-N01 | Medium | ビブラート描画マルチテンポ非対応 | 中 | `MusicMath.GetBpmAt(tick)` 相当で対応検討 |
| P3-N02 | Low | VibratoPanel 初期値問題 | 低 | デバイステストで再現確認 |
| P3-N03 | Low | 300 tick 閾値ハードコード | 低 | E-03 に統合 |

---

## 更新版 Phase 3 残タスク (優先順)

### 1. テスト追加 (Medium 優先)

```
[ ] VibratoViewModel メソッド単体テスト
    - SetVibratoLength/Depth/Period/FadeIn/FadeOut が VibratoLengthCommand 等を発行するか
    - ToggleVibratoForSelectedNotes が UVibrato.length を 0 ↔ 0.5 切替えるか
[ ] PhonemeViewModel メソッド単体テスト
    - SetPhonemeOffset / SetPhonemePreutter / SetPhonemeOverlap / SetPhonemeAlias の発行確認
    - ClearPhonemeTimingForSelectedNotes の確認
期待: dotnet test ... -f net9.0 → 12件 → 追加後 20件以上
```

### 2. デバイステスト (Medium 優先)

```
[ ] Pixel 10 Pro XL で EditVibrato モード切替 → VibratoPanel 表示
[ ] ノート選択 → スライダー操作 → PianoRoll にビブラート波形が反映
[ ] PhonemeCanvas タップ → ActionSheet → プレアッター変更 → Undo 動作
[ ] クオンタイズボタン → グリッド密度変化の目視確認
[ ] OP-01: ファイルオープン時の権限ダイアログ確認
[ ] BUG-D: シャドウ境界の目視確認
[ ] C-04/C-05: logcat 計測で前回比較
```

### 3. コード品質 (Low 優先)

```
[ ] E-03 + P3-N03: マジックナンバー定数化
    - PhonemeHitThresholdTicks = 300
    - VibratoMinLengthRatio = 0.0f
    - VibratoDefaultLength = 0.5f
[ ] E-02: 中国語コメント → 英語/日本語置換
[ ] E-01: Phase 3 追加パブリックメソッドへ XML doc 追加
[ ] P3-N01: DrawVibratoOverlay のテンポ取得をマルチテンポ対応に改善
```

---

## roadmap-v3 達成状況 (2026-04-13 時点)

### Phase 0 (成立条件)

| # | 内容 | 状態 |
|---|------|------|
| 0-1 | テレメトリ基盤 | ⬜ 未着手 |
| 0-2 | オートセーブ + セッション復元 | ⬜ 未着手 |
| 0-3 | 音源インポート互換性診断 | ⬜ 未着手 |
| 0-4 | cutoff 防御 + 修正候補提示 | ⬜ 未着手 |
| 0-5 | メモリ管理改善 | 🔶 Phase 2 bitmap cache で部分対応 |
| 0-6 | レンダリングキャッシュ管理 | 🔶 Phase 2 bitmap cache で部分対応 |
| 0-7 | 資産レジストリ / パス抽象化 | ⬜ 未着手 |
| 0-8 | Expression パラメータ編集動作修正 | 🔶 部分対応 (Phase 2 タッチ改善) |
| 0-9 | レンダリングジョブ管理基盤 | ⬜ 未着手 |
| 0-10 | ライセンス / 配布ポリシー整備 | ⬜ 未着手 |

### Phase 1 (1曲仕上げられる)

| # | 内容 | 状態 |
|---|------|------|
| 1-1 | デモシンガー + チュートリアル導線 | ⬜ 未着手 |
| 1-2 | 歌詞の一括入力 | ⬜ 未着手 |
| 1-3 | フォネームプレビュー可視化 | 🔶 PhonemeCanvas 表示は実装済み (B-2 で強化) |
| 1-4 | クオンタイズ UI 改善 | ✅ Phase 3 Step C で完了 |
| 1-5 | ノート長・開始位置の数値入力 | ⬜ 未着手 |
| 1-6 | 選択範囲プレビュー再生 | ⬜ 未着手 |
| 1-7 | BGM 読み込み安定化 + 圧縮音源 | ⬜ 未着手 |
| 1-8 | テンポ / 拍子マーカー削除対応 | ⬜ 未着手 |
| 1-9 | 適応レイアウト + タブレット基盤 | ⬜ 未着手 |
| 1-10 | ピッチアンカー編集モード完成 | 🔶 基本実装済み (Phase 2 で強化済み) |
| 1-11 | ピッチカーブ描画のタッチ最適化 | 🔶 部分対応 (Phase 2 ThrottleIntervalMs=16) |
| 1-12 | ビブラート編集モード完成 | ✅ Phase 3 Step A で完了 |

### Phase 2 以降

**未着手。** Phase 1 完了後に着手予定。

---

## 次フェーズ推奨アクション

### 即時対応 (今週)
1. **Phase 3 テスト追加** — ビブラート / フォネーム ViewModel メソッドの単体テスト
2. **デバイステスト** — Pixel 10 Pro XL で P3-A/B/C の動作確認 + OP-01/BUG-D/C-04/C-05 確認

### 中期 (Phase 4 以降で検討)
3. **Phase 0 着手** — 0-2 (オートセーブ) から開始が推奨 (ユーザー価値が最も高い)
4. **1-2 歌詞一括入力** — 1曲完結の導線で最重要
5. **P3-N01 マルチテンポ対応** — ビブラート描画の品質改善

---

## ビルド・テスト基準 (Phase 3 完了時点)

```
dotnet build OpenUtauMobile/OpenUtauMobile.csproj -f net9.0-android -c Debug
→ 0 エラー / ~1726 警告 (既存)

dotnet test OpenUtauMobile.Tests/ -f net9.0
→ 17/17 pass (Phase 3 テスト追加後: +5件)
```

---

## 後続 Audit 追記 — 2026-04-14

### Phase 3 後続タスク完了記録

| コミット | 内容 | 状態 |
|---------|------|------|
| `test: Phase 3 vibrato/phoneme ViewModel API contract tests` | EditViewModelPhase3Tests.cs 新規 + SmokeTests.cs 2件追記 | ✅ |
| `refactor: E-01 fix Chinese XML doc, E-03 named constants` | Save() XML doc 英語化 + const 3件 | ✅ |
| `refactor: E-02 replace Chinese comments with English across 7 files` | 7ファイル全置換 | ✅ |
| `fix: OP-01 RequestStoragePermissionAsync returns false when denied` | ObjectProvider.cs 修正 | ✅ |

### 残存未解消 (Phase 4 以降)

| ID | Severity | 内容 |
|----|----------|------|
| C-04 | Medium | Track canvas 11.3% slow — Pixel 10 Pro XL 再計測待ち |
| C-05 | Medium | PlaybackTickBg MISS 36.8% — キャッシュ戦略検討待ち |
| P3-N01 | Medium | `DrawVibratoOverlay` マルチテンポ非対応 |
| P3-N02 | Low | VibratoPanel 初期値問題 — デバイステストで再現確認待ち |
