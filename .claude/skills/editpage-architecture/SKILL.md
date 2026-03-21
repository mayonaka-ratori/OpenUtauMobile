# EditPage Architecture Map

**対象ファイル**: `OpenUtauMobile/Views/EditPage.xaml.cs` (partial class — 4ファイルに分割済み)
**最終更新**: 2026-03-21 (Phase 2.5 Step 5 完了)
**用途**: Claude Code が EditPage の巨大なコードベースを毎回再解析せずに済むよう、構造を事前マップとして提供する。

---

## EditPage File Structure (post Phase 2.5 Step 5)

| File | Lines | Responsibility |
|------|-------|---------------|
| EditPage.xaml.cs | 1,694 | Fields, constructor, lifecycle, gesture handlers, layout, scroll sync |
| EditPage.Rendering.cs | 967 | 11 PaintSurface handlers + 4 drawing helpers |
| EditPage.Toolbar.cs | 836 | 44 button handlers + 8 UI helpers (Save, AttemptExit, etc.) |
| EditPage.CmdSubscriber.cs | 273 | OnNext command subscriber (ICmdSubscriber) |
| EditModes.cs | 43 | TrackEditMode, NoteEditMode, ExpressionEditMode, SelectionMode enums |
| UndoScope.cs | 33 | IDisposable undo group guard (31サイトで使用中 — Phase 2.5 Step 8a/8b) |

---

## Section 1: 行範囲マップ

> **NOTE**: 以下の行番号は Phase 2.5 Step 5 分割前の EditPage.xaml.cs (3,640行) を参照しています。
> 現在の行番号は各 partial class ファイルを直接確認してください:
> - ジェスチャーハンドラ / フィールド → `EditPage.xaml.cs`
> - PaintSurface ハンドラ / 描画ヘルパー → `EditPage.Rendering.cs`
> - ボタンハンドラ / 保存/終了 → `EditPage.Toolbar.cs`
> - コマンド受信 (OnNext) → `EditPage.CmdSubscriber.cs`

| Region | 行範囲 | 説明 | 主要メソッド/フィールド |
|--------|--------|------|------------------------|
| **using / namespace** | 1–26 | 依存パッケージ宣言 | — |
| **クラス宣言・基本フィールド** | 27–74 | `_disposables`, `_viewModel`, タイマー, GestureProcessor ×5, 各種状態フラグ | `_trackGestureProcessor` 等5フィールド |
| **SKPaint/SKFont キャッシュ (#region 画笔)** | 75–136 | PaintSurface で使う全 Paint/Font/Path を フィールドとして宣言 | `_pitchLinePaint`, `_pianoKeysPaint`, `_textFont12`, `_phonemeEnvelopePath`, `_pitchLinePath` |
| **Drawable インスタンスキャッシュ** | 138–145 | PaintSurface 内での毎フレーム new を排除 (D-07) | `_drawableTrackBackground`, `_drawablePartCache`, `_drawableNotes`, `_drawableTickBackground`, `_drawablePianoRollTickBackground` |
| **PlaybackTickBg ビットマップキャッシュ (P2-5a)** | 147–156 | PlaybackTickBackgroundCanvas 用 SKBitmap/SKImage キャッシュフィールド | `_tickBgCacheBitmap`, `_tickBgCacheImage`, `TICK_BG_CACHE_MARGIN=1.0f` |
| **PianoKeysCanvas ビットマップキャッシュ (P2-5b)** | 158–166 | PianoKeysCanvas 用 SKBitmap/SKImage キャッシュフィールド | `_pianoKeysCacheBitmap`, `_pianoKeysCacheImage`, `PIANO_KEYS_CACHE_MARGIN=1.0f` |
| **PianoRollTickBg ビットマップキャッシュ (P2-5c)** | 168–182 | PianoRollTickBackgroundCanvas 用キャッシュ + グリッド専用 Paint/Font | `_pianoRollTickBgCacheBitmap`, `_pianoRollBarTextPaint`, `_pianoRollPlaybackLinePaint` |
| **Constructor `EditPage(string path)`** | 184–370 | 全初期化: GestureProcessor生成, ReactiveUI購読, タイマー設定, Transformer初期値設定, DocManager購読 | `SetupGestureEvents()`, `InitializeComponent()`, `WhenAnyValue(...)` |
| **レイアウトサイズ変更ハンドラ** | 372–407 | MainLayout/MainEdit の SizeChanged → ViewModel へ通知 | `OnMainLayoutSizeChanged`, `OnMainEditSizeChanged`, `PlaybackAutoScroll` |
| **Magnifier 初期化** | 441–493 | Android 29+ 専用 Magnifier ビルダー (音高・表情の2本) | `InitMagnifier`, `InitExpressionMagnifier` |
| **OnBackButtonPressed** | 495–499 | 戻るボタン → `AttemptExit()` | `OnBackButtonPressed` |
| **SetupGestureEvents (TrackCanvas)** | 506–725 | TrackCanvas 用 GestureProcessor の全イベント購読 | 下記 Section 3 参照 |
| **SetupGestureEvents (PianoRoll)** | 726–1000 | PianoRollCanvas 用 GestureProcessor の全イベント購読 | 下記 Section 3 参照 |
| **SetupGestureEvents (TimeLine)** | 1001–1093 | TimeLineCanvas 用 GestureProcessor の全イベント購読 | — |
| **SetupGestureEvents (Phoneme)** | 1094–1148 | PhonemeCanvas 用 GestureProcessor の全イベント購読 (ほぼ PianoRoll に転送) | — |
| **SetupGestureEvents (Expression)** | 1149–1332 | ExpressionCanvas 用 GestureProcessor の全イベント購読 (曲線描画/消去/パン) | `StartDrawExpression`, `StartResetExpression` |
| **SetupGestureEvents 末尾 + UpdateLeftScrollView** | 1333–1358 | スクロール同期ヘルパー | `UpdateLeftScrollView` |
| **OnNext (ICmdSubscriber)** | 1359–1594 | DocManager コマンド受信ディスパッチャ (全 UCommand ハンドリング) | 30 種以上の `if (cmd is XxxCommand)` ブロック |
| **分割線 (#region 分割线)** | 1596–1721 | TrackMain/ExpDiv ドラッグ分割線、パン・ズーム制限更新 | `DivControlPanUpdated`, `PanExpDivider_PanUpdated`, `UpdateTrackCanvasPanLimit`, `UpdatePianoRollCanvasPanLimit`, `UpdateTrackCanvasZoomLimit`, `UpdatePianoRollCanvasZoomLimit` |
| **ScrollView 同期** | 1724–1743 | ヘッダー ScrollView のスクロール同期 | `ScrollTrckHeaders_Scrolled` |
| **TrackCanvas Touch/Paint** | 1744–1802 | TrackCanvas のタッチ転送 + PaintSurface | `TrackCanvas_Touch`, `TrackCanvas_PaintSurface` |
| **再生ボタン・編集モード切替** | 1804–1828 | PlayOrPause, SwitchEditMode ボタンハンドラ | `ButtonPlayOrPause_Clicked`, `ButtonSwitchEditMode_Clicked` |
| **PianoRollCanvas Paint/Touch** | 1829–1873 | ノート描画 PaintSurface + タッチ転送 | `PianoRollCanvas_PaintSurface`, `PianoRollCanvas_Touch` |
| **PlaybackPosCanvas PaintSurface** | 1875–1890 | 再生ヘッドライン描画 | `PlaybackPosCanvas_PaintSurface` |
| **PianoKeysCanvas Paint/Draw/Touch** | 1891–1997 | 鍵盤描画 (ビットマップキャッシュ) | `PianoKeysCanvas_PaintSurface`, `DrawPianoKeysToCanvas`, `PianoKeysCanvas_Touch` |
| **ズーム/保存/undo/redo ボタン** | 1998–2056 | ZoomIn/Out, Save, SaveAs, Undo, Redo | `ButtonZoomIn_Clicked`, `ButtonZoomOut_Clicked`, `ButtonSave_Clicked`, `Save`, `SaveAs`, `ButtonUndo_Clicked`, `ButtonRedo_Clicked` |
| **TimeLineCanvas Paint/Touch** | 2057–2069 | タイムライン目盛描画 + タッチ転送 | `TimeLineCanvas_PaintSurface`, `TimeLineCanvas_Touch` |
| **PlaybackTickBg PaintSurface** | 2070–2146 | トラック背景グリッド描画 (ビットマップキャッシュ) | `PlaybackTickBackgroundCanvas_PaintSurface` |
| **パート削除・追加ボタン** | 2147–2160 | RemovePart, AddTrack | `ButtonRemovePart_Clicked`, `ButtonAddTrack_Clicked` |
| **PianoRollTickBg PaintSurface** | 2161–2294 | ピアノロール背景グリッド描画 (ビットマップキャッシュ) | `PianoRollTickBackgroundCanvas_PaintSurface` |
| **PianoRollGrid/Shadow ヘルパー** | 2295–2339 | グリッド線・シャドウ描画サブルーチン | `DrawPianoRollGridToCanvas`, `DrawPianoRollShadow` |
| **PianoRollKeysBg PaintSurface** | 2340–2367 | 黒鍵背景帯描画 | `PianoRollKeysBackgroundCanvas_PaintSurface` |
| **トラック操作ボタン群** | 2368–2460 | Rename, Mute, MoveUp/Down, SwitchNoteMode, SingerAvatar, ToggleDetailedHeader | 各 `Button*_Clicked` |
| **ボリューム・パン ジェスチャー** | 2478–2543 | トラックヘッダーのボリューム/パン ジェスチャー | `GestureChangeVolume_PanUpdated`, `GestureChangePan_PanUpdated`, `GestureResetPan_Tapped`, `GestureResetVolume_Tapped` |
| **ノート削除ボタン** | 2544–2553 | 選択ノート削除 | `ButtonRemoveNote_Clicked` |
| **PianoRollPitchCanvas PaintSurface** | 2554–2613 | ピッチ曲線描画 | `PianoRollPitchCanvas_PaintSurface` |
| **Lifecycle (OnDisappearing / OnAppearing / Dispose)** | 2619–2742 | タイマー停止/再開、BUG-C ForceReset、全リソース破棄 | `OnDisappearing`, `OnAppearing`, `Dispose` |
| **SnapToGrid / More ボタン群** | 2744–2901 | PianoRoll/Track のスナップ、長押し、ButtonMore メニュー, RemoveTrack | `ButtonPianoRollSnapToGrid_Clicked`, `TouchBehaviorPianoRollSnapToGrid_LongPressCompleted`, `ButtonMore_Clicked`, `ButtonRemoveTrack_Clicked` |
| **終了フロー** | 2903–2959 | 戻る/保存確認ダイアログ | `ButtonBack_Clicked`, `AttemptExit`, `AskIfSaveAndContinue` |
| **プロジェクト情報表示** | 2960–2972 | BPM/拍子/調号ラベル更新 | `RefreshProjectInfoDisplay` |
| **PhonemeCanvas Paint/Touch** | 2973–3071 | 音素エンベロープ描画 + タッチ転送 | `PhonemeCanvas_PaintSurface`, `PhonemeCanvas_Touch` |
| **ExpressionCanvas Paint/Touch** | 3073–3271 | 表情曲線描画 (Curve/Numerical/Options) + タッチ転送 | `ExpressionCanvas_PaintSurface`, `ExpressionCanvas_Touch` |
| **BPM/拍子/調号/ピッチレンダー/エクスポートボタン** | 3273–3375 | テンポ変更, TimeSig, Key, RenderPitch, AudioExport | `ButtonEditBpm_Clicked`, `ButtonRenderPitch_Clicked`, `UpdateRenderProgress` |
| **表情・Phonemizer・選択・コピー・貼り付けボタン** | 3379–3532 | トラック色/名前変更, Stop, Phonemizer変更, 表情選択, Select/Copy/Paste/SelectAll | 各 `Button*_Clicked` |
| **音声転写ボタン** | 3533–3567 | UWavePart → UVoicePart 変換 (AudioTranscribe) | `ButtonAudioTranscribe_Clicked` |

---

## Section 2: Canvas インベントリ

| Canvas Name (XAML x:Name) | PaintSurface ハンドラ | GestureProcessor | Drawable クラス | ビットマップキャッシュ | 最新計測 (max / slow%) |
|--------------------------|----------------------|------------------|-----------------|----------------------|------------------------|
| `TrackCanvas` | `TrackCanvas_PaintSurface` (L1755) | `_trackGestureProcessor` | `DrawableTrackBackground`, `DrawablePart` (Dictionary キャッシュ) | なし | max=1.8ms / 0% ✅ |
| `PlaybackTickBackgroundCanvas` | `PlaybackTickBackgroundCanvas_PaintSurface` (L2070) | なし (Touch なし) | `DrawableTickBackground` | ✅ P2-5a SKBitmap+SKImage (`_tickBgCache*`) | MISS 22ms / HIT <8ms ✅ |
| `PlaybackPosCanvas` | `PlaybackPosCanvas_PaintSurface` (L1875) | なし | なし (直接描画) | なし | ≈1ms / 0% ✅ |
| `TimeLineCanvas` | `TimeLineCanvas_PaintSurface` (L2057) | `_timeLineGestureProcessor` | `DrawableTickBackground` | なし | 未計測 |
| `PianoKeysCanvas` | `PianoKeysCanvas_PaintSurface` (L1891) | なし (Touch のみ転送なし) | なし (直接描画) | ✅ P2-5b SKBitmap+SKImage (`_pianoKeysCacheImage`) | max=4ms / 0% ✅ |
| `PianoRollKeysBackgroundCanvas` | `PianoRollKeysBackgroundCanvas_PaintSurface` (L2340) | なし | なし (直接描画) | なし | max=7ms / 0% ✅ |
| `PianoRollTickBackgroundCanvas` | `PianoRollTickBackgroundCanvas_PaintSurface` (L2161) | なし | `DrawablePianoRollTickBackground` | ✅ P2-5c SKBitmap+SKImage (`_pianoRollTickBgCacheImage`) | MISS 56ms (zoom) / HIT <8ms ✅ |
| `PianoRollCanvas` | `PianoRollCanvas_PaintSurface` (L1829) | `_pianoRollGestureProcessor` | `DrawableNotes` | なし (P2-B2で描画最適化) | max=20.29ms / 0.5% 🔶 |
| `PianoRollPitchCanvas` | `PianoRollPitchCanvas_PaintSurface` (L2554) | `_pianoRollGestureProcessor` (Touch転送) | なし (直接 SKPath 描画) | なし | max=0.81ms / 0% ✅ |
| `PhonemeCanvas` | `PhonemeCanvas_PaintSurface` (L2973) | `_phonemeGestureProcessor` | なし (直接描画) | なし | max=23.78ms / 0.4% 🔶 |
| `ExpressionCanvas` | `ExpressionCanvas_PaintSurface` (L3073) | `_expressionGestureProcessor` | なし (直接描画) | なし | 未計測 |

> 注: `PianoRollCanvas_Touch` (L1869) は `_pianoRollGestureProcessor.ProcessTouch()` に転送。
> `PianoKeysCanvas_Touch` (L1993) はスクロール同期のみ (GestureProcessor 非使用)。

---

## Section 3: GestureProcessor イベントマップ

### `_trackGestureProcessor` (TrackCanvas, Transformer: TrackTransformer)

| イベント | 購読行 | 動作概要 |
|---------|--------|---------|
| `Tap` | L508 | クリック位置でシーク (`SeekPlayPosTickNotification`) またはパート選択・選択解除 |
| `DoubleTap` | L575 | ダブルタップ位置のパートを編集モードで開く (EditPage 内部遷移) |
| `PanStart` | L587 | Normal=キャンバスパン開始 / Edit=ハンドル/パート ヒットテストして move/resize/create 開始 |
| `PanUpdate` | L620 | move/resize/create/pan の各状態に応じてViewModel を更新 |
| `PanEnd` | L654 | move/resize/create/pan の終了、`EndUndoGroup()` 呼び出し、ZoomLimit 更新 |
| `ZoomStart` | L688 | `TrackTransformer.StartZoom()` |
| `ZoomUpdate` | L693 | `TrackTransformer.UpdateZoom()` |
| `XZoomStart` | L701 | `TrackTransformer.StartXZoom()` |
| `XZoomUpdate` | L706 | `TrackTransformer.UpdateXZoom()`、ZoomLimit 更新 |
| `YZoomStart` | L712 | `TrackTransformer.StartYZoom()` |
| `YZoomUpdate` | L719 | `TrackTransformer.UpdateYZoom()`、ZoomLimit 更新 |

### `_pianoRollGestureProcessor` (PianoRollCanvas + PianoRollPitchCanvas, Transformer: PianoRollTransformer)

| イベント | 購読行 | 動作概要 |
|---------|--------|---------|
| `Tap` | L728 | EditNote=ノート選択/追加/削除 / EditPitchCurve=ピッチアンカー追加 / EditVibrato=ビブラート編集 |
| `DoubleTap` | L783 | 歌詞入力ポップアップ表示 (`LyricPopup`) |
| `PanStart` | L812 | **BUG-A 修正済**: `e.OriginalTouchDown` でヒットテスト → ハンドル/ノート/キャンバスパン を判定 |
| `PanUpdate` | L871 | `UpdateMoveNotes` / `UpdateResizeNotes` / PianoRollTransformer.UpdatePan / ピッチ/アンカー描画更新 |
| `PanEnd` | L914 | `EndMoveNotes` / `EndResizeNotes` / `EndPan` / `EndDrawPitch` / `EndDrawPitchAnchor` |
| `ZoomStart` | L968 | `PianoRollTransformer.StartZoom()` |
| `ZoomUpdate` | L973 | `PianoRollTransformer.UpdateZoom()`、ZoomLimit 更新 |
| `XZoomStart` | L979 | `PianoRollTransformer.StartXZoom()` |
| `XZoomUpdate` | L984 | X方向ズーム更新、ZoomLimit 更新 |
| `YZoomStart` | L990 | `PianoRollTransformer.StartYZoom()` |
| `YZoomUpdate` | L995 | Y方向ズーム更新、ZoomLimit 更新 |

### `_timeLineGestureProcessor` (TimeLineCanvas, Transformer: TrackTransformer)

| イベント | 購読行 | 動作概要 |
|---------|--------|---------|
| `Tap` | L1003 | タップ位置でシーク |
| `DoubleTap` | L1015 | テンポ変更ポップアップ (`EditBpmPopup`) |
| `PanStart` | L1035 | `TrackTransformer.StartPan()` |
| `PanUpdate` | L1042 | `TrackTransformer.UpdatePan()` |
| `PanEnd` | L1051 | `TrackTransformer.EndPan()` |
| `ZoomStart/Update` | L1058/1063 | TrackTransformer ズーム |
| `XZoomStart/Update` | L1070/1075 | X方向ズーム |
| `YZoomStart/Update` | L1080/1087 | Y方向ズーム |

### `_phonemeGestureProcessor` (PhonemeCanvas, Transformer: PianoRollTransformer)

| イベント | 購読行 | 動作概要 |
|---------|--------|---------|
| `Tap` | L1096 | 音素タップ (現状 stub) |
| `DoubleTap` | L1101 | 音素ダブルタップ (現状 stub) |
| `PanStart/Update/End` | L1106/1111/1115 | PianoRollTransformer パン (`StartPan/UpdatePan/EndPan`) — PianoRoll と同期 |
| `ZoomStart/Update` | L1119/1124 | PianoRollTransformer ズーム |
| `XZoom/YZoom` | L1129–1147 | PianoRollTransformer X/Y ズーム |

### `_expressionGestureProcessor` (ExpressionCanvas, Transformer: PianoRollTransformer)

| イベント | 購読行 | 動作概要 |
|---------|--------|---------|
| `Tap` | L1151 | Eraser モード: 単点リセット |
| `DoubleTap` | L1175 | (現状 stub) |
| `PanStart` | L1180 | Hand=パン / Edit=`StartDrawExpression` / Eraser=`StartResetExpression` |
| `PanUpdate` | L1218 | 曲線描画 or 消去 or パン更新、Magnifier 更新 |
| `PanEnd` | L1256 | `EndDrawExpression` / `EndResetExpression` / `EndPan`、Magnifier 非表示 |
| `ZoomStart/Update/XZoom/YZoom` | L1303–1331 | PianoRollTransformer ズーム |

---

## Section 4: ViewModel アクセス頻度 Top 20

| プロパティ/メソッド | アクセス回数 | カテゴリ |
|--------------------|------------|---------|
| `_viewModel.*` (全体) | 416 | — |
| `InvalidateSurface()` (11 Canvas 合計) | 114 | write (再描画トリガー) |
| `PianoRollTransformer.*` | 74 | read/write |
| `Density` | 39 | read |
| `TrackTransformer.*` | 56 | read/write |
| `DocManager.Inst.*` | 81 | read/call |
| `EditingPart` | 30 | read/write |
| `SelectedParts` | 22 | read/write |
| `SelectedNotes` | 19 | read/write |
| `PlayPosTick` | 21 | read/write |
| `IsMovingNotes` / `IsResizingNote` | 6 | read (guard) |
| `CurrentNoteEditMode` | 12 | read |
| `CurrentExpressionEditMode` | 8 | read |
| `HeightPerPianoKey` | 7 | read |
| `DrawableParts` | 5 | read/write |
| `EditingNotes.*` | 4 | read/call |
| `Playing` | 10 | read/write |
| `StartUndoGroup` / `EndUndoGroup` (DocManager経由) | 13残存 (Phase 2.5 8a/8b で31変換済み) | call |
| `StartMoveNotes` / `StartResizeNotes` | 2 | call |
| `UpdateMoveNotes` / `UpdateResizeNotes` | 2 | call |

---

## Section 5: 複雑度ホットスポット Top 5

| メソッド | 行範囲 | 行数 | 責務 | リファクタ候補? |
|---------|--------|------|------|----------------|
| `OnNext(UCommand, bool)` | L1359–1594 | 235 | 30種類以上の UCommand を `if/else if` チェーンで分岐、各コマンドに応じて InvalidateSurface・ViewModel 更新・ZoomLimit 更新 | ✅ 高 (Command→Handler 辞書化、PartialClass 分割) |
| `SetupGestureEvents()` (全体) | L504–1332 | 828 | 5プロセッサ×11イベント = 55 lambda を連続定義。内部に編集モード分岐ロジックも内包 | ✅ 高 (各 Canvas 単位で PartialClass に分離) |
| `ExpressionCanvas_PaintSurface` | L3073–3266 | 193 | Curve/Numerical/Options 3種の表情型を1メソッド内で描き分け、ビューポートクリッピング・セグメント描画を含む | 🔶 中 (型ごとに private Draw* メソッドに分割) |
| `PlaybackTickBackgroundCanvas_PaintSurface` | L2070–2146 | 76 | ビットマップキャッシュヒット/ミス判定 + グリッド生成 + キャッシュ描画 + 再生位置ライン | 🔶 低 (P2-5a 完成済み、現状許容範囲) |
| `Dispose()` | L2664–2742 | 78 | 3種ビットマップキャッシュ × 各 SKBitmap/SKCanvas/SKImage + Paint/Font/Path + GestureProcessor + ViewModel + DocManager の順序付き全破棄 | 🔶 低 (順序が重要なため分割困難だが現状は正しい) |

---

## Section 6: 依存グラフ

```
EditPage
├── ViewModels
│   └── EditViewModel          (BindingContext, 全ビジネスロジック委譲)
│       └── PianoRollTransformer, TrackTransformer  (座標変換)
│
├── Drawable Classes (Views/DrawableObjects/)
│   ├── DrawableTrackBackground   (トラック背景帯)
│   ├── DrawablePart              (パート矩形・タイトル)
│   ├── DrawableNotes             (ノート矩形・歌詞・ハンドル)
│   ├── DrawableTickBackground    (PlaybackTick背景グリッド)
│   └── DrawablePianoRollTickBackground  (PianoRollTick背景グリッド)
│
├── Views/Utils
│   ├── GestureProcessor          (×5インスタンス)
│   ├── Transformer               (PianoRollTransformer, TrackTransformer)
│   └── PaintSurfaceProfiler      (#if DEBUG 計測基盤)
│
├── OpenUtau.Core
│   ├── DocManager                (プロジェクト操作 Command パターン)
│   ├── PlaybackManager           (再生制御)
│   ├── UProject / UPart / UNote (データモデル)
│   └── ICmdSubscriber            (EditPage が実装)
│
├── Views/Controls
│   ├── ExitPopup                 (保存確認ダイアログ)
│   ├── LyricPopup                (歌詞入力)
│   └── LoadingPopup              (AudioTranscribe 進捗)
│
├── .NET MAUI Services
│   ├── IDispatcherTimer          (PlaybackTimer, AutoSaveTimer)
│   ├── DeviceDisplay             (KeepScreenOn)
│   └── MessageBus                (RefreshCanvasMessage 受信)
│
└── Android Platform (条件付き)
    └── Android.Widget.Magnifier  (#if ANDROID29_0_OR_GREATER)
```

---

## Section 7: Phase 2 変更ログ

| メソッド/リージョン | 変更内容 | コミット | 目的 |
|--------------------|---------|---------|------|
| `EditPage(string path)` Constructor (L197–205) | Auto-select first VoicePart on Loaded + P2-B1 ガード追加 | `bb28e4a` | P2-B1a: プロジェクト読み込み直後にノートが描画されない問題修正 |
| `SetupGestureEvents` (L726) / GestureProcessor.cs | `HandleTouchUp` の `case 1 when GestureState.Zoom` 追加 | `bb28e4a` | P2-B1b: 2本指ズーム後に1本指パン不能になるグレー画面固着修正 |
| `PlaybackTickBackgroundCanvas_PaintSurface` (L2070–2146) | SKBitmap 3×幅キャッシュ + `SKImage.FromBitmap()` + `DrawImage(srcRect)` | `eeb5df0`→`633177a` | P2-5a: キャッシュヒット時 HIT<8ms 達成 |
| `PianoKeysCanvas_PaintSurface` (L1891–1954) | SKBitmap 3×高さキャッシュ + SKImage.FromBitmap() + DrawImage(srcRect) | `eeb5df0`→`633177a` | P2-5b: 29ms → 4ms / 0% slow 達成 |
| `PianoRollTickBackgroundCanvas_PaintSurface` (L2161–2294) | SKBitmap 3×幅キャッシュ + shadow 動的描画分離 + SKImage.FromBitmap() | `633177a` | P2-5c: HIT時 <8ms 達成 |
| `DrawPianoRollGridToCanvas` (L2232–2294) | キャッシュCanvas書き込み用グリッドヘルパーとして新設 | `633177a` | P2-5c サポート (グリッドのみキャッシュ、shadow は毎フレーム) |
| `PianoRollCanvas_PaintSurface` (L1829–1868) | DrawableNotes 経由に統一 (B2-B1) | `bb28e4a` | SelectedParts ガード解消後の描画パス確立 |
| `_pianoRollGestureProcessor.PanStart` handler (L821–838) | `e.StartPosition` → `e.OriginalTouchDown` に変更 | `c6afc55` | P2-B3 BUG-A: タッチダウン元座標でヒットテストすることで 5px ドリフト問題を修正 |
| `OnAppearing` (L2630–2648) | `ForceReset()` × 5 + `ForceEndAllInteractions()` 追加 | `5798ac6` | P2-B3 BUG-C: アプリ復帰時に全ジェスチャーをリセット |
| `Dispose()` (L2664–2742) | 3種 SKBitmap/SKImage Dispose ブロック追加 (P2-5a/5b/5c) | `633177a` | キャッシュリソースリーク防止 |

## Section 8: Phase 2.5 変更ログ (2026-03-21)

| ファイル | 変更内容 | コミット |
|---------|---------|---------|
| `EditPage.Rendering.cs` (NEW, 967行) | 11 PaintSurface ハンドラ + 4描画ヘルパーを xaml.cs から抽出 | `d79e3af` |
| `EditPage.Toolbar.cs` (NEW, 836行) | 44 ボタンハンドラ + 8 UIヘルパーを xaml.cs から抽出 | `4642b26` |
| `EditPage.CmdSubscriber.cs` (NEW, 273行) | OnNext (ICmdSubscriber) を xaml.cs から抽出 | `fc41d47` |
| `EditModes.cs` (NEW, 43行) | TrackEditMode/NoteEditMode/ExpressionEditMode/SelectionMode を EditViewModel から抽出 | `47db2c1` |
| `UndoScope.cs` (NEW, 33行) | IDisposable undo group guard class | `3742545` |
| `EditViewModel.cs` | 31 StartUndoGroup/EndUndoGroup → UndoScope 変換 (8a: 22 simple, 8b: 9 spanning) | `7eba112`, `12de33f` |
| `EditPage.Toolbar.cs` | 8 undo pair → UndoScope 変換 (8a) | `7eba112` |
| `EditLyricsPopup.xaml.cs` | 2 undo pair → UndoScope 変換 (8a) | `7eba112` |

---

## クイックリファレンス: よく修正が必要な箇所

### タッチ操作を変更したい場合
- PianoRoll ノート tap/drag/resize: `SetupGestureEvents` (L726–1000)、特に `PanStart` (L812) / `PanUpdate` (L871) / `PanEnd` (L914)
- タッチ→ViewModel の接続: 各 GestureProcessor の PanStart/PanUpdate/PanEnd ハンドラ

### 新しい Canvas を追加したい場合
1. XAML に SKCanvasView を追加
2. `_canvasGestureProcessor` フィールド追加 (L39–47 パターン)
3. Constructor で `new GestureProcessor(transformer)` (L223–231 パターン)
4. `SetupGestureEvents()` にイベント購読ブロック追加 (L506 パターン)
5. PaintSurface ハンドラを追加 (#if DEBUG プロファイラを含む)
6. Dispose() にキャッシュ破棄ブロック追加 (L2687–2707 パターン)
7. OnAppearing に `ForceReset()` 追加 (L2636–2647 パターン)

### DocManager コマンドを新規追加したい場合
- `OnNext()` は `EditPage.CmdSubscriber.cs` に移動済み (Phase 2.5 Step 5)
- `else if (cmd is NewCommand)` を OnNext メソッドに追加
- 対応する `InvalidateSurface()` と ViewModel 更新を記述

### ビットマップキャッシュの実装パターン
1. フィールド: `_xxxCacheBitmap`, `_xxxCacheCanvas`, `_xxxCacheImage`, `_xxxCacheOriginPanX`, `_xxxCachedZoomX` (L147–182 参照)
2. PaintSurface: キャッシュキーを比較 → ミス時に再生成 → `SKImage.FromBitmap()` → `DrawImage(srcRect, dstRect)`
3. Dispose: `_xxxCacheCanvas?.Dispose()` → null / `_xxxCacheBitmap?.Dispose()` → null / `_xxxCacheImage?.Dispose()` → null
4. 参照実装: `PlaybackTickBackgroundCanvas_PaintSurface` (L2070)
