# Phase 3 進捗レポート

## ステータス: ✅ COMPLETE (2026-04-14)

---

## 完了済みステップ

### Step A: ビブラート編集モード

| ステップ | 内容 | コミット相当 |
|---------|------|------------|
| A-1 | EditViewModel にビブラート操作メソッド追加 | `feat: Step A-1` |
| A-2/A-3 | VibratoPanel XAML UI + Toolbar.cs ハンドラ | `feat: Step A-2/A-3` |
| A-4 | EditPage.Rendering.cs にビブラート波形オーバーレイ描画 | `feat: Step A-4` |

#### 追加した ViewModel メソッド (EditViewModel.cs)

```csharp
GetVibratoForSelectedNote() → UVibrato?
ToggleVibratoForSelectedNotes()
SetVibratoLength(float length)       // VibratoLengthCommand
SetVibratoDepth(float depthCents)    // VibratoDepthCommand
SetVibratoPeriod(float periodMs)     // VibratoPeriodCommand
SetVibratoFadeIn(float fadeIn)       // VibratoFadeInCommand
SetVibratoFadeOut(float fadeOut)     // VibratoFadeOutCommand
```

#### UI
- `EditPage.xaml`: ExpressionCanvas をラップする Grid に `VibratoPanel` (ScrollView) を追加
  - ButtonVibratoToggle + Length/Depth/Period/FadeIn/FadeOut スライダー × ラベル
  - `CurrentNoteEditMode == EditVibrato` 時に表示, ExpressionCanvas は非表示
- `EditPage.Toolbar.cs`: `#region Vibrato Panel Handlers` — `RefreshVibratoPanelValues()` + スライダー ValueChanged ハンドラ群

#### レンダリング (EditPage.Rendering.cs)
- `DrawVibratoOverlay(SKCanvas, SKImageInfo, UVoicePart)` メソッド追加
- PianoRollCanvas_PaintSurface 内で `CurrentNoteEditMode == EditVibrato` 時に呼び出し
- フェードイン/アウト エンベロープ対応のサイン波描画
- `_vibratoPaint` / `_vibratoPath` は EditPage.xaml.cs フィールドとして事前確保（PaintSurface 内アロケーション禁止）

---

### Step B: フォネーム編集 UI

| ステップ | 内容 |
|---------|------|
| B-1 | EditViewModel にフォネームパラメータ編集メソッド追加 |
| B-2 | PhonemeCanvas タップ → ActionSheet + DisplayPromptAsync |

#### 追加した ViewModel メソッド (EditViewModel.cs)

```csharp
SetPhonemeOffset(UNote note, int phonemeIndex, int offsetTicks)    // PhonemeOffsetCommand
SetPhonemePreutter(UNote note, int phonemeIndex, float deltaMsec)  // PhonemePreutterCommand
SetPhonemeOverlap(UNote note, int phonemeIndex, float deltaMsec)   // PhonemeOverlapCommand
SetPhonemeAlias(UNote note, int phonemeIndex, string? alias)       // ChangePhonemeAliasCommand
ClearPhonemeTimingForSelectedNotes()                               // ClearPhonemeTimingCommand (選択ノート全体)
```

#### UI (EditPage.xaml.cs — phoneme Tap ハンドラ)
- タップ X → パート相対ティック変換 → 最近傍フォネーム探索（±300 tick）
- DisplayActionSheet でプレアッター / オーバーラップ / オフセット / エイリアス選択
- DisplayPromptAsync で値を入力 → ViewModel メソッド呼び出し → PhonemeCanvas 再描画

---

### Step C: クオンタイズ UI 改善

| ステップ | 内容 |
|---------|------|
| C-1 | ピアノロールツールバーにクオンタイズボタングループ追加 |

#### UI (EditPage.xaml + EditPage.Toolbar.cs)
- `QuantizeButtonGroup` HorizontalStackLayout: 1/4, 1/8, 1/16, 1/32, 3連 各 36px ボタン
- `BtnSnap_Clicked` ハンドラ: `_viewModel.PianoRollSnapDiv = div` + `UpdateQuantizeButtonHighlight(div)`
- アクティブボタンを `Color.FromArgb(ActiveQuantizeButtonArgb)` でハイライト

---

### Phase 3 後続タスク (2026-04-14 完了)

| タスク | 内容 | 状態 |
|--------|------|------|
| テスト追加 | EditViewModelPhase3Tests.cs (5件) + SmokeTests.cs 2件追記 | ✅ |
| E-01 XML doc | `Save()` XML doc 英語化 + Phase 3 追加メソッド整備 | ✅ |
| E-02 中国語コメント置換 | 7ファイル全置換 (EditViewModel.cs 含む) | ✅ |
| E-03 マジックナンバー定数化 | `VibratoRenderStepPx`, `ActiveQuantizeButtonArgb`, `ZoomStepFactor` | ✅ |
| BUG-D シャドウ境界ずれ | コード精査済み。`+0.5f` は意図的なサブピクセルカバー。Close。 | ✅ |
| OP-01 権限バグ修正 | `RequestStoragePermissionAsync` が denied 時に `false` を返すよう修正 | ✅ |

---

## ビルド・テスト結果

```
dotnet build ... -f net9.0-android -c Debug → 0 エラー / 1726 警告 (既存)
dotnet test ... -f net9.0               → 17/17 合格
```

---

## 残タスク (Phase 4 以降)

- [ ] デバイステスト: Pixel 10 Pro XL で各 UI 動作確認
  - EditVibrato モード切替 → VibratoPanel 表示 → スライダー操作 → 波形反映
  - PhonemeCanvas タップ → ActionSheet → プレアッター変更
  - クオンタイズボタン → PianoRoll グリッド反映
  - OP-01 修正の動作確認 (ファイルオープン時の権限ダイアログ)
  - BUG-D シャドウ境界の目視確認
  - C-04/C-05 logcat 計測
- [ ] P3-N01: `DrawVibratoOverlay` マルチテンポ対応
- [ ] P3-N02: VibratoPanel 初期値問題 — デバイステストで再現確認
