# Phase 4 進捗レポート (Roadmap v3 Phase 0 実装)

## ステータス: ✅ 0-4 COMPLETE (2026-04-14) / ✅ 0-2 COMPLETE (2026-04-14) / ✅ 0-1 COMPLETE (2026-04-14)

---

## 0-1: クラッシュ・性能テレメトリ基盤

| 変更ファイル | 内容 | ステータス |
|-------------|------|----------|
| `Utils/Telemetry/SessionMetrics.cs` (新規) | `SessionMetrics` / `ProjectSnapshot` POCO | ✅ 2026-04-14 |
| `Utils/Telemetry/TelemetryService.cs` (新規) | シングルトン。StartSession/MarkStartupComplete/ReportException/ReportSlowFrame/CaptureProjectSnapshot/ExportSupportBundle | ✅ 2026-04-14 |
| `MauiProgram.cs` | AppDomain + TaskScheduler 例外ハンドラに ReportException 追加、StartSession 呼び出し | ✅ 2026-04-14 |
| `Platforms/Android/MainApplication.cs` | OnCreate で AndroidEnvironment.UnhandledExceptionRaiser 購読 | ✅ 2026-04-14 |
| `Views/SplashScreenPage.xaml.cs` | MarkStartupComplete + CaptureProjectSnapshot を起動完了直後に呼ぶ | ✅ 2026-04-14 |
| `Views/EditPage.Rendering.cs` | TrackCanvas / PianoRollCanvas の PaintSurface finally に ReportSlowFrame 追加 | ✅ 2026-04-14 |
| `OpenUtau.Core/Util/Preferences.cs` | TelemetryOptIn / CrashReportShareOptIn フィールド追加 | ✅ 2026-04-14 |
| `ViewModels/SettingsViewModel.cs` | TelemetryOptIn / CrashReportShareOptIn プロパティ追加 + Save() に反映 | ✅ 2026-04-14 |
| `Views/SettingsPage.xaml` | 「診断」タブ + GridDiagnostics (Switch×2 + Export ボタン) | ✅ 2026-04-14 |
| `Views/SettingsPage.xaml.cs` | ButtonTabDiagnostics ハンドラ + UpdateTab case 4 + ButtonExportSupportBundle_Clicked | ✅ 2026-04-14 |
| `Resources/Strings/AppResources*.resx` | Diagnostics / TelemetryOptIn / CrashReportShareOptIn / ExportSupportBundle 文字列 (4言語) | ✅ 2026-04-14 |
| `Resources/Strings/AppResources.Designer.cs` | 上記8文字列に対応するプロパティ追加 | ✅ 2026-04-14 |
| `docs/CORE_PATCHES.md` | パッチ #7 として SerializablePreferences 変更を記録 | ✅ 2026-04-14 |

### 設計方針
- 外部SDK (Sentry/AppCenter) は不使用。既存 Serilog ファイル sink に `[TEL]` プレフィックスで構造化出力。
- 例外捕捉は AppDomain / TaskScheduler / AndroidEnvironment の3経路。
- 遅延フレーム: PaintSurface ごとの `_swTel` Stopwatch で生ms を収集、60秒毎に集約ログ。
- オプトアウト時は slow_frame / project_snapshot ログを抑止。例外のみ記録継続。
- サポートバンドル: Logs/ + session.json を zip に圧縮し Share API で共有。外部送信なし。

### ビルド確認 (2026-04-14)
- `dotnet build OpenUtauMobile.csproj -f net9.0-android -c Debug` → **0 エラー** / 1605 警告 (既存)
- `dotnet test OpenUtauMobile.Tests/ -f net9.0` → **14/14 合格**

---

## 0-2: Autosave + Session Restore + Safe Mode

| Commit | 内容 | ステータス |
|--------|------|----------|
| 1 | USTx.cs 原子的書き込み (temp+move) + CORE_PATCHES.md | ✅ 2026-04-14 |
| 2 | MauiProgram.cs バックグラウンド遷移時保存 | ✅ 2026-04-14 |
| 3 | RecoveryPopup + HomePage 復元ダイアログ + EditPage SkipLoadOnInit | ✅ 2026-04-14 |
| 4 | セーフモード (EditPage バナー + PreRender フラグ) | ✅ 2026-04-14 |

### Commit 1 詳細 (2026-04-14)

- `OpenUtau.Core/Format/USTx.cs` — `Save()` / `AutoSave()` の `File.WriteAllText` を `temp+File.Move(overwrite:true)` に置換
- `docs/CORE_PATCHES.md` — パッチ #6 として記録
- ビルド: 0 errors / テスト: 14/14 合格

### Commit 2 詳細 (2026-04-14)

- `OpenUtauMobile/MauiProgram.cs` — `ConfigureLifecycleEvents` 追加 (Android: OnPause/OnStop, iOS: DidEnterBackground)
- ビルド: 0 errors

### Commit 3 詳細 (2026-04-14)

- `OpenUtauMobile/Views/Controls/RecoveryPopup.xaml` + `.xaml.cs` — 新規作成 (3ボタン: 復元/セーフモード/破棄)
- `OpenUtauMobile/Views/HomePage.xaml.cs` — `CheckAndOfferRecovery()` を `OnAppearing` に追加
- `OpenUtauMobile/Views/EditPage.xaml.cs` — `recovered` / `safeMode` パラメータ追加、復元時は `Formats.RecoveryProject()` を呼ぶ
- ビルド: 0 errors / テスト: 14/14 合格

### Commit 4 詳細 (2026-04-14)

- `OpenUtauMobile/Views/EditPage.xaml` — SafeModeBanner 行 + Grid 行追加
- `OpenUtauMobile/Views/EditPage.xaml.cs` — safeMode 時に `PreRender=false` + バナー表示
- `OpenUtauMobile/Views/EditPage.Toolbar.cs` — `ButtonSafeModeExit_Clicked` ハンドラ追加
- ビルド: 0 errors / テスト: 14/14 合格

---

## 0-4: cutoff 防御 + 修正候補提示

| 変更ファイル | 内容 | ステータス |
|-------------|------|----------|
| `OpenUtau.Core/Render/Worldline.cs` (Core Patch #8) | `SynthRequestWrapper` コンストラクタに cut_off クランプ追加 | ✅ 2026-04-14 |
| `OpenUtau.Core/Classic/VoicebankErrorChecker.cs` (Core Patch #9) | `CheckOto()` に cutoff > fileDuration の明示的チェック追加 | ✅ 2026-04-14 |
| `docs/CORE_PATCHES.md` | パッチ #8 / #9 記録 | ✅ 2026-04-14 |

### 設計方針

**ランタイム (Patch #8):**
- `cut_off > 0` かつ `total_ms - offset - cut_off < 10ms` のとき、`cut_off` を `total_ms - offset - 10ms` にクランプ
- `CutOffExceedDurationError` をスローせずレンダリングを継続 (音声ウィンドウ最低 10ms を確保)
- `Log.Warning("[0-4] ...")` でクランプ量を記録 → 0-1 テレメトリログに `[0-4]` タグで収集

**インポート時 (Patch #9):**
- `oto.Cutoff > 0 && oto.Cutoff > fileDuration` の場合、早期 `return false`
- メッセージ例: `"Cutoff (450.0ms) exceeds file duration (380.0ms). Suggested cutoff ≤ 60.0ms."`
- 根本原因を示す 1 メッセージに集約 (従来は下流の全チェックが連鎖的に誤メッセージを出力していた)

### ビルド確認 (2026-04-14)
- `dotnet build OpenUtauMobile.csproj -f net9.0-android -c Debug` → **0 エラー** / 1730 警告 (既存)
- `dotnet test OpenUtauMobile.Tests/ -f net9.0` → **14/14 合格**
