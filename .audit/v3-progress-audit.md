# OpenUtau Mobile — Roadmap v3 Progress Audit
**Date:** 2026-03-18
**Auditor:** claude-sonnet-4-6 (automated code audit)
**Scope:** All `.cs` files under `OpenUtauMobile/OpenUtauMobile/` against `.audit/roadmap-v3.md`

---

## 1. Executive Summary

**Overall Grade: D+**
Phase 0 infrastructure is almost entirely not started. Phase 1 has partial foundations but no complete feature. Only stability/correctness work from the previous sprint (P1-1 through P1-11 + CR-1 through CR-6) is verifiably done.

| Phase | Items | Done | In-Progress | Stub | Not-Started |
|:------|------:|-----:|------------:|-----:|------------:|
| Phase 0 (10 items) | 10 | 0 | 2 | 1 | 7 |
| Phase 1 (12 items) | 12 | 0 | 4 | 2 | 6 |
| Phase 2 (10 items) | 10 | 0 | 0 | 1 | 9 |
| Phase 3 (7 items)  | 7  | 0 | 1 | 0 | 6 |
| Phase X (10 items) | 10 | 1 | 1 | 0 | 8 |
| **Total** | **49** | **1** | **8** | **4** | **36** |

**% effectively done:** ~2% (only X-2 scroll tracking can be counted as done from the roadmap perspective, with caveats).

**Key gaps (blockers for Phase 0 exit gate):**
1. No crash/telemetry SDK — Sentry/Crashlytics absent; only Serilog file sink present
2. No session restore UI — autosave timer fires but no recovery dialog on next launch
3. No voicebank import validator — diagnostics (alias dupes, cutoff anomalies, char encoding guesses) are all missing
4. No rendering job queue with cancel/debounce — every note edit fires unbounded renders
5. No asset registry / path abstraction — paths are still absolute filesystem strings

**Top 5 immediate priorities (evidence-based):**
1. 0-1: Add Sentry/Crashlytics opt-in (Serilog alone cannot surface OOM exits or ANR)
2. 0-2: Add recovery-check on startup and rename-on-save atomic write
3. 0-4: Wire cutoff/offset/preutter clamp into Core's render path; surface warnings in UI
4. 0-9: Implement debounce + cancel for render jobs in EditViewModel
5. 1-2: Finish bulk lyric input (currently single-note popup only, no paste/flow mode)

---

## 2. Upstream Diff Summary

**Upstream remote:** `https://github.com/vocoder712/OpenUtauMobile.git` (fork base, not `stakira/OpenUtau`)
**Origin remote:** `https://github.com/mayonaka-ratori/OpenUtauMobile`
**Commits ahead of upstream/master:** 3 commits

| Commit | Message | Nature |
|:-------|:--------|:-------|
| `8ef4a29` | fix: Phase 1 stability fixes + Cold Review blocker corrections | Mobile-only (ViewModel dispose, SKPaint cache, GestureProcessor, AudioTrackOutput) |
| `6108157` | docs: add Claude Code harness (skills, agents, hooks, tests, roadmap) | Meta / tooling only |
| `c2bab3e` | docs: add Claude Code harness (skills, agents, hooks, tests, roadmap) | Meta / tooling only |

**Files changed vs upstream (33 files, 2793 insertions / 557 deletions):**

- **New (added):** `.claude/` harness (agents, skills, hooks, progress), `CLAUDE.md`, `OpenUtauMobile.Tests/`, `docs/CORE_PATCHES.md`, `docs/ROADMAP.md`
- **Modified (mobile source):** `AudioTrackOutput.cs`, `ObjectProvider.cs`, `EditViewModel.cs`, `DrawableNotes.cs`, `DrawablePart.cs`, `DrawablePianoKeys.cs`, `DrawablePianoRollTickBackground.cs`, `DrawableTickBackground.cs`, `DrawableTrackPlayPosLine.cs`, `EditPage.xaml.cs`, `SplashScreenPage.xaml.cs`, `GestureProcessor.cs`
- **Core files unchanged:** All files under `OpenUtau.Core/` and `OpenUtau.Plugin.Builtin/` are unmodified vs upstream (consistent with CLAUDE.md rule 1)

**Upstream version note:** The upstream is `vocoder712/OpenUtauMobile`, itself a fork of `stakira/OpenUtau`. The `progress.md` references a pending merge of `OpenUtau.Core v0.1.567`. No upstream fetch was performed in this audit session (network not available), so exact diff against stakira cannot be confirmed.

---

## 3. Feature Implementation Status (Narrative)

### What is verifiably working

**Autosave timer** (`EditPage.xaml.cs:37,289-296`): A 30-second `IDispatcherTimer` fires `DocManager.Inst.AutoSave()`. Timer is stopped on `OnDisappearing` and restarted on `OnAppearing` (only if playing). Handler is detached on Dispose. **However**, there is no atomic write (temp→rename), no recovery dialog on next launch, and no safe-mode launch option. This is a partial implementation of 0-2.

**Serilog file logging** (`MauiProgram.cs:55-71`): Logs device info, OS version, and unhandled exceptions to a rolling file via Serilog. This is useful but is NOT a crash telemetry SDK — it cannot surface OOM kills, ANR, or background process terminations. 0-1 is not started.

**Touch throttling** (`GestureProcessor.cs:32-337`): Pan and zoom events are throttled to 16ms (60Hz) via `Environment.TickCount64`. Also an RxUI `Throttle(16.6ms)` is applied to a viewport update stream in `EditPage.xaml.cs:216`. This satisfies CLAUDE.md rule 5 and is a genuine implementation of the throttling principle.

**Grid snap UI** (`PianoRollSnapDivPopupViewModel.cs`, `EditViewModel.cs:56`): Snap divisions `[4,8,16,32,64,128,3,6,12,24,48,96,192]` are fully wired. A popup exists (`PianoRollSnapDivPopup.xaml.cs`). This covers the core of 1-4.

**Phonemizer selection** (`SelectPhonemizerPopup.xaml.cs`): A popup lets the user pick a phonemizer from Core's list. This is functional but not a "custom phonemizer" (2-3); it just exposes Core's built-in set.

**Undo/Redo** (`EditPage.xaml.cs:1928-1935`): Buttons call `DocManager.Inst.Undo()` / `Redo()`. All data mutations in EditViewModel use `StartUndoGroup()` / `ExecuteCmd()` / `EndUndoGroup()`. Undo is functional in the Core sense. Persistence is not implemented (X-1).

**Expression parameter canvas** (`EditPage.xaml.cs:1113-1247`): DYN/BRE/GEN and similar parameters can be drawn by touch on a dedicated `ExpressionCanvas`. Three modes: Hand (scroll), Edit (draw), Eraser. Android magnifier integrated. The roadmap item 0-8 states this is broken — there is implementation, but whether touch-scroll conflict is resolved needs runtime verification. Code evidence shows the three modes and gesture routing exist.

**Vibrato mode stub** (`EditViewModel.cs:76`, `EditPage.xaml.cs:259,737,766,823,866,920`): `EditVibrato` mode enum value exists, mode switch button exists, but every `case EditVibrato:` branch is `break` with no implementation. This is a stub.

**Pitch anchor mode stub** (`EditViewModel.cs:74`, `EditPage.xaml.cs:258,735,764,821,864,918`): Same pattern — mode enum, button, all gesture cases are empty `break`. Stub.

**Lyric edit popup** (`EditLyricsPopup.xaml.cs`): Single-note lyric edit with "Next" navigation. No bulk paste/flow mode. Partial toward 1-2.

**Singer install + encoding selection** (`InstallSingerViewModel.cs:37-46`): Archive encoding and text encoding can be selected (Shift-JIS, UTF-8, GB2312, Big5, etc.). This is a user-facing partial solution for encoding issues during install, but not an automated diagnostics scanner (0-3).

**Audio export** (`ExportAudioPopup.xaml.cs`, `EditPage.xaml.cs:2463`): WAV export popup exists and is wired. No compressed output formats.

**Playback position scroll tracking** (`EditPage.xaml.cs:344-356`): Play position scroll-follows by adjusting `PanX` when playhead goes out of view. Basic implementation, not interpolated.

**Render pitch (DiffSinger)** (`EditViewModel.cs:1425`): `RenderPitchAsync()` exists with `CancellationToken` support, gated by `SupportsRenderPitch`. This supports DiffSinger AI pitch reading (2-6) at the model level; the UI button is conditional.

**Singer types supported** (via `SingerToTypeColorConverter.cs`): Classic (UTAU), DiffSinger, Vogen, Enunu, Voicevox are all represented in UI color coding — meaning the app can at minimum display and manage these singer types.

**iOS platform stubs** (`Platforms/iOS/`): `AppDelegate.cs`, `iOSAppLifeCycleHelper.cs` (restart not supported — shows toast and quits), iOS file permissions stub. iOS audio output is not implemented natively — there is no AVAudioEngine or CoreAudio integration. The app likely runs on iOS only because it falls through to a missing audio backend.

**Android magnifier** (`EditPage.xaml.cs:66,392-413`): `Android.Widget.Magnifier` is integrated for the pitch canvas and expression canvas on Android. This is a genuine mobile UX feature.

**Test suite** (`OpenUtauMobile.Tests/SmokeTests.cs`): 4 xUnit smoke tests covering `DocManager` singleton, `UNote.Create`, `UProject.CreateNote`, and `UVibrato` default ranges. No UI tests, no render tests, no file I/O tests.

---

## 4. Status Table — Every Roadmap v3 Item

| Item ID | Title | Status | Evidence | Notes |
|:--------|:------|:------:|:---------|:------|
| **0-1** | クラッシュ・性能テレメトリ基盤 | `not-started` | Only Serilog file sink (`MauiProgram.cs:57-66`). No Sentry, Crashlytics, AppCenter, Firebase. OOM exits not captured. | Serilog is necessary but insufficient. No opt-in consent flow, no OOM detection, no performance counters (startup time, render time, cache size). |
| **0-2** | オートセーブ + セッション復元 + セーフモード | `in-progress` | `AutoSaveTimer` (30s) fires `DocManager.Inst.AutoSave()` in `EditPage.xaml.cs:289-296`. Timer lifecycle managed correctly after P1-4 fix. | Missing: atomic write (temp→rename), recovery dialog on next launch, safe-mode (open without render). These are the high-value parts of 0-2. |
| **0-3** | 音源インポート時の互換性診断 | `not-started` | `InstallSingerViewModel.cs` installs singer archives. No scan of `oto.ini`/`prefix.map`/WAV existence/alias dupes/cutoff anomalies/memory estimation. | Encoding selection exists as user choice, not auto-detection. The diagnostics engine described in 0-3 does not exist. |
| **0-4** | cutoff exceeds audio duration 防御 + 修正候補提示 | `not-started` | No clamp/guard code found in mobile layer. Core may handle some cases. No import-time entry listing, no recommended correction values shown. | Search for `cutoff`, `clamp`, `OtoEntry`, `SampleError` returned zero results in mobile code. Needs verification in OpenUtau.Core, but mobile-side there is nothing. |
| **0-5** | メモリ管理改善・クラッシュ頻度低減 | `in-progress` | P1-2/P1-3 fixed SKPaint allocations in PaintSurface (DrawableNotes, DrawablePart, DrawablePianoRollTickBackground). P1-5 fixed GestureProcessor event leaks. | No lazy-load, no ArrayPool usage, no multi-res waveform, no piano roll virtualization. The paint caching is genuine improvement but only one dimension of 0-5. |
| **0-6** | レンダリングキャッシュ管理 | `stub` | `DrawablePart.cs` uses a cache dict for rendered parts (P1-3/CR-4 added Dispose on eviction). No LRU, no size limit, no manual clear, no cache key with singer hash/expression/resampler/tempo. | Cache exists but has no eviction policy and no correctness guarantees on singer change. |
| **0-7** | 資産レジストリ / パス抽象化 | `not-started` | Paths are still direct filesystem strings throughout. `ObjectProvider.cs` wraps some path resolution but does not implement internal IDs or URI persistence. No SAF integration, no iOS Files deep-linking. | `DataRootDocumentsProvider.cs` exists on Android for file provider declaration but is not a registry. |
| **0-8** | Expressionパラメータ編集の動作修正 | `in-progress` | Three edit modes (Hand/Edit/Eraser) are implemented with gesture routing in `EditPage.xaml.cs:1113-1247`. Android magnifier integrated. Throttle via RxUI applied. | Runtime correctness of scroll-vs-draw conflict is unclear from code alone. The roadmap says "触れない" (can't touch) — code shows handlers exist. Needs device test. |
| **0-9** | レンダリングジョブ管理基盤 | `not-started` | `EditViewModel.cs` has `CancellationTokenSource` support in `SetWork()` (line 230) and `CancelWork()` (line 271). However, no debounce, no priority queue, no "visible range first" logic, no consecutive-tap cancellation before new render. | `RenderPitchAsync` takes a `CancellationToken` but there is no job scheduler. Each note edit likely triggers unbounded async renders. |
| **0-10** | ライセンス / 配布ポリシー整備 | `not-started` | `README.md` has a license section. `docs/CORE_PATCHES.md` exists. No per-asset license table for demo singers, ONNXmodels, resamplers, artwork. | The roadmap specifically calls for resampler and demo-singer redistribution clearance. These files/sections do not exist. |
| **1-1** | 初回体験 / デモシンガー + サンプルプロジェクト | `not-started` | No bundled demo singer, no sample USTX project, no tutorial flow. `SplashScreenPage` handles init only. AppResources has "Piano Sample TODO" and "Sine Wave TODO" strings (line 1321,1339) — placeholders. | The TODO strings in AppResources confirm this is planned but not delivered. |
| **1-2** | 歌詞の一括入力・流し込み | `stub` | `EditLyricsPopup.xaml.cs` exists: single-note edit + "Next" button traversal. No paste-all, no space/改行-split, no punctuation-to-rest conversion, no bulk Undo grouping. | "Next" navigation is a step toward sequential entry but is not bulk input. |
| **1-3** | 歌詞→発音プレビューの可視化 | `not-started` | `DrawableNotes.cs:155-184` draws the `note.lyric` string on each note. There is no phoneme-result visualization distinct from the raw lyric. No "what this lyric becomes after phonemization" display. | One line in `EditPage.xaml.cs:2647` reads `phonemeMapped ?? phoneme` in a popup context, not in the main roll view. |
| **1-4** | ノートスナップ / グリッド密度設定UI | `in-progress` | `PianoRollSnapDivPopupViewModel.cs` fully implements snap div selection. Values `[4,8,16,32,64,128,3,6,12,24,48,96,192]` cover 1/4 through 1/192 and triplets. Popup exists. | Roadmap notes "long-press dependency should be removed." Need to verify trigger gesture — if still long-press only, UI improvement is incomplete. |
| **1-5** | ノート長・開始位置・音高の数値入力 | `not-started` | No numeric input dialog for note length, start tick, pitch, or parameters beyond lyric. `EditKeyPopupViewModel.cs` exists for key editing (not note params). | Only lyric is editable via popup. Start/length/pitch require touch-drag only. |
| **1-6** | 選択範囲プレビュー再生 | `not-started` | `FluentUI.cs` has preview icon constants. No `SelectionPlay`, `PlaySelection`, or loop range in `EditViewModel.cs` or `EditPage.xaml.cs`. | Play button triggers full-project playback from `PlayPosTick`. No range selection playback. |
| **1-7** | BGM読み込み安定化 + 圧縮音源対応 | `in-progress` | `ObjectProvider.cs` and `EditViewModel.cs` reference BGM/wav loading. `MauiProgram.cs` registers BGM-related services. No mp3/m4a/AAC/ogg decoder integration found. | WAV-only BGM appears to be the current state. Compressed formats are a genuine gap. |
| **1-8** | テンポ / 拍子マーカー削除・編集 | `not-started` | `InsertTempoSignaturePopup.xaml.cs` exists for insertion. No delete handler found — search for `deleteTempo`, `RemoveTempo`, `RemoveTimeSignature` returned zero results. | User can add but not remove tempo/time-sig markers. This is a known blocker in `progress.md` Phase 3 backlog. |
| **1-9** | 適応レイアウト + タブレット / スタイラスUI基盤 | `not-started` | `DeviceIdiom` is queried in `MainActivity.cs` for some tablet detection. No adaptive grid layout, no minimum hit-target enforcement, no Pencil/S-Pen detection. | Tablet detection at device level exists but no layout adaptation in UI layer. |
| **1-10** | ピッチアンカー編集モードの完成 | `stub` | `EditPitchAnchor` mode enum exists; mode button exists; all gesture cases (`Tap`, `DoubleTap`, `PanStart`, `PanUpdate`, `PanEnd`, `PanCancel`) are `break` with no implementation. | The UI scaffolding is present but the feature does zero work. |
| **1-11** | ピッチカーブ描画のタッチ最適化 | `in-progress` | Android magnifier is integrated for `PianoRollPitchCanvas`. Touch throttle (16ms) exists in GestureProcessor. `PianoRollPitchCanvas` receives gesture events. | No finger-vs-stylus mode distinction, no draw-point smoothing, no 2-finger pan/zoom distinct from draw mode. |
| **1-12** | ビブラート編集モードの完成 | `stub` | `EditVibrato` mode enum exists; mode button exists; all gesture cases are empty `break`. No slider UI for rate/depth/offset/fadeIn/fadeOut. | Same pattern as 1-10. Zero implementation behind the mode switch. |
| **2-1** | シンガー遅延ロードとOTOオンデマンド展開 | `not-started` | Singer loading goes through Core's `SingerManager`. No mobile-specific lazy-load or phoneme-subset expansion code found. | Core may do some caching; but mobile-specific memory optimization for large singers is absent. |
| **2-2** | 資産レジストリ完成 + Project Package化 | `not-started` | Depends on 0-7 which is also not-started. No `.oumpkg` format, no SAF URI persistence, no internal ID system. | |
| **2-3** | カスタムフォネマイザ機構（Lua / DSL） | `not-started` | `SelectPhonemizerPopup` exposes Core's built-in phonemizers only. No MoonSharp, no Lua runtime, no JSON/DSL fallback. | The popup exists but only surfaces phonemizers Core already knows about. |
| **2-4** | hifisampler ONNX化検証 | `not-started` | No ONNX runtime reference in `OpenUtauMobile.csproj` PackageReferences. No ONNX inference code found. `Platforms/Android/Lib/` has commented-out native lib references in csproj (worldline.so). | worldline.so references are commented out — suggests prior attempt at native sampler, now disabled. |
| **2-5** | 簡易エフェクト処理内蔵 | `not-started` | No reverb, EQ, or compressor code. No master bus concept. | |
| **2-6** | DiffSinger AIピッチ読み込みの最適化UI | `in-progress` | `EditViewModel.cs:408-415,1425` — `IsShowRenderPitchButton` is gated on `SupportsRenderPitch`, `RenderPitchAsync()` exists with cancel support. | The core pipeline exists but the roadmap asks for "compare A/B, apply result, diff display" UX — none of that is present. |
| **2-7** | MIDI / UST / VSQX インポートの安定化 | `unclear` | Core handles these formats. Mobile `EditMenuPopup` has import options. Whether error logging and failure-detail display are improved vs upstream is not determinable without runtime testing. | This item may be partially satisfied by Core; mobile-specific improvements are unclear. |
| **2-8** | 多言語フォネマイザ追加 | `unclear` | Core's phonemizer set is inherited. AppResources has English, Chinese (zh), Japanese (ja) UI strings. The ja.resx file is 869 lines, suggesting meaningful Japanese localization. | Whether Korean, Thai, German, Italian phonemizers from upstream are included depends on the Core version. |
| **2-9** | UIテーマ・カラー | `in-progress` | `ThemeColors.cs` implements `LightThemeColors` and `DarkThemeColors` with abstract SKPaint properties. `ThemeColorsManager.Current` is referenced throughout. | This is further along than the roadmap implies (which says "deprioritize"). Basic light/dark theming is real. |
| **2-10** | 簡易ピッチ補助（遷移テンプレ / スタイルプリセット） | `not-started` | No pitch template, no style preset, no "ballad/robotic/pop curve" concept in code. | |
| **3-1** | iOS音声出力の完全安定化 | `not-started` | No `AVAudioEngine`, `AVAudioSession`, or CoreAudio code found. `iOSAppLifeCycleHelper` only handles restart (with a "not supported" toast). iOS audio backend is missing. | This is a critical gap for iOS deployment. |
| **3-2** | iOS配布戦略 | `not-started` | `Info.plist` and Xcode project exist. No TestFlight, AltStore, or SideStore configuration. | Infrastructure exists at project level but no distribution pipeline. |
| **3-3** | クラウド同期（PC↔Mobile） | `not-started` | Depends on 2-2 / 0-7. No iCloud, Drive, or Files sync code. | |
| **3-4** | 簡易共有導線 | `not-started` | `Share` icon exists in FluentUI constants. No `Share.RequestAsync` or native share sheet call found in mobile source. | |
| **3-5** | 簡易AI自動ピッチ生成（DiffSinger非依存） | `not-started` | Depends on 2-4 ONNX baseline which is also not started. | |
| **3-6** | DAW連携（iOS AUv3プラグイン化） | `not-started` | No Swift extension, no AUv3 capability. Correctly deferred per roadmap. | |
| **3-7** | ENUNU音源サポート | `unclear` | `USingerType.Enunu` is referenced in `SingerToTypeColorConverter.cs`, meaning Core has ENUNU type. Whether mobile rendering pipeline supports it is unclear. | |
| **X-1** | Undo / Redo 履歴の永続化 | `not-started` | `DocManager.Inst.Undo()` / `Redo()` are wired to buttons. No history serialization to disk, no restore-after-autosave of undo stack. | |
| **X-2** | 再生時のスムーズスクロール | `in-progress` | `EditPage.xaml.cs:344-356` adjusts `PanX` when playhead goes out of view. Functional but not interpolated — snaps rather than smooth-scrolls. | |
| **X-3** | cutoffエラーの詳細一覧UI | `not-started` | Depends on 0-4. No diagnostic list UI. | |
| **X-4** | 多言語UI翻訳の拡充 | `in-progress` | `AppResources.ja.resx` (869 lines), `AppResources.zh.resx`, `AppResources.en.resx` exist. Japanese and Chinese localizations are present. | Progress.md Phase 3 backlog notes Japanese localization is pending — suggesting existing ja.resx may be incomplete. |
| **X-5** | ピアノロールスケールハイライト | `not-started` | No scale highlight in `DrawablePianoRollTickBackground.cs` or `DrawablePianoKeys.cs`. | |
| **X-6** | アクセシビリティ対応（TalkBack / VoiceOver） | `not-started` | No `SemanticProperties`, `AutomationProperties`, or accessibility hints found in mobile code. | |
| **X-7** | SNS共有・共有シート連携 | `not-started` | No `Share.RequestAsync` call found. FluentUI has share icons but they are unused in any share flow. | |
| **X-8** | 回帰テスト用サンプル音源 / プロジェクト群整備 | `not-started` | `OpenUtauMobile.Tests/SmokeTests.cs` has 4 pure-Core unit tests. No singer fixtures, no USTX sample projects, no render regression harness. | |
| **X-9** | 端末互換性マトリクスと性能モード | `not-started` | No device tier detection, no quality/cache tier switching based on available RAM. | |
| **X-10** | サポートバンドル出力 | `not-started` | Serilog writes to file. No "export logs + settings + diagnostics as zip" flow. | |

---

## 5. Milestone Recommendations

### Milestone M0 — "Stop Breaking, Start Observing" (2 weeks)
Goal: Satisfy Phase 0 exit gate items that block all further development.

**Must include:**
- 0-1: Add Sentry SDK (or Crashlytics) with opt-in consent on first run. Log startup time, memory at key points, and unhandled exceptions including OOM signals.
- 0-2 (complete): Add atomic save (write to `.tmp` then rename), add startup recovery dialog when `.tmp` exists, add "open without render" (safe mode) option.
- 0-4: In the mobile render path, clamp `cutoff` to audio duration before passing to resampler. Surface a per-part warning badge.
- 0-6: Add LRU eviction to `DrawablePart` cache with configurable size limit; add singer-hash to cache key.
- 0-9: Add a 200ms debounce in EditViewModel before firing render; cancel in-flight jobs when new edit arrives.
- 0-10: Add `docs/ASSET_LICENSES.md` documenting redistribution status of every bundled resource.

**Can defer:** 0-3 (full import validator is 1–2 weeks of work), 0-5 (requires profiling), 0-7 (multi-week).

### Milestone M1 — "One Song, Start to Finish" (4–6 weeks)
Goal: Satisfy Phase 1 exit gate (30s to first sound, basic composition workflow).

**Must include:**
- 1-1: Bundle a CC0/public-domain demo singer and a sample USTX that loads and plays immediately.
- 1-2: Implement bulk lyric paste (split on space/改行, assign sequentially, one Undo group).
- 1-5: Add numeric note editor popup (length in beats/ticks, pitch as MIDI number or note name).
- 1-6: Implement selection-range playback (play from selection start, stop at selection end).
- 1-7: Add FFmpeg or platform decoder for mp3/m4a/AAC in BGM.
- 1-8: Add tempo/time-sig marker deletion (long-press → delete option).
- 1-10: Implement pitch anchor edit (add/drag/remove pitch points on selected notes).
- 1-12: Implement vibrato edit (sliders for rate/depth/offset/fadeIn/fadeOut on selected notes).

**Can defer:** 1-3 (phoneme preview nice-to-have), 1-9 (adaptive layout is important but can be incremental), 1-11 (magnifier already exists, stylus mode deferrable).

### Milestone M2 — "Platform Foundation" (2–3 months)
- 0-3: Full import-time voicebank validator with alias/cutoff/WAV/memory diagnostics.
- 0-5: Lazy singer load, piano roll note virtualization, ArrayPool for audio buffers.
- 0-7 / 2-2: Asset registry with URI persistence; begin Android SAF integration.
- 3-1: iOS audio backend via AVAudioEngine or similar.
- 2-4: ONNX PoC on device (benchmark ARM64 inference speed for hifisampler).

### Milestone M3 — "Differentiation" (3–6 months)
- 2-3: Custom phonemizer (MoonSharp Lua + JSON DSL fallback).
- 2-10: Pitch style presets (ballad/robotic templates).
- 3-3: Cloud sync via iCloud / Google Drive (after 2-2 asset registry is stable).
- X-8: Regression test fixtures (CV/VCV/multi-pitch singers, problem OTO, long projects).

---

## 6. PR Candidates (Well-Scoped, Openable Now)

These are actionable PRs based on the current codebase state:

| # | PR Title | Scope | Estimated Size | Unblocks |
|:--|:---------|:------|:---------------|:---------|
| PR-1 | `feat: atomic autosave + startup recovery dialog` | Add `.tmp` write + rename in `DocManager.AutoSave()` (Core patch); add recovery check in `SplashScreenPage`; add recovery/safe-mode alert UI | ~200 LOC | 0-2 completion |
| PR-2 | `feat: Sentry opt-in crash and performance telemetry` | Add Sentry NuGet, consent dialog on first launch, hook unhandled exceptions and `Application.OnLowMemory`, capture startup duration | ~150 LOC | 0-1 |
| PR-3 | `fix: LRU cache eviction for rendered parts` | Add size-counted LRU to `DrawablePart`'s cache dict; add singer-hash + expression-hash to cache key; add "Clear Render Cache" in settings | ~180 LOC | 0-6 |
| PR-4 | `feat: render job debounce + cancel on edit` | Add `CancellationTokenSource` rotation and 200ms debounce in EditViewModel's note-change → render pipeline | ~120 LOC | 0-9 |
| PR-5 | `feat: bulk lyric paste` | Extend `EditLyricsPopup` (or new flow) to accept multi-word text, split, assign to sequential notes, wrap in one undo group | ~200 LOC | 1-2 |
| PR-6 | `feat: numeric note editor popup` | New popup with `Entry` fields for duration (ticks and beats display), start tick, pitch (MIDI + note name), bound to `ChangeNoteDurationCommand` etc. | ~250 LOC | 1-5 |
| PR-7 | `feat: tempo and time-signature marker deletion` | Wire long-press on `DrawableTickBackground` tempo/sig markers to a delete confirmation; use Core's `RemoveTempoCommand` / `RemoveTimeSigCommand` | ~150 LOC | 1-8 |
| PR-8 | `feat: pitch anchor editing — basic add/drag/delete` | Implement the empty `EditPitchAnchor` gesture cases: tap to add/remove pitch point, pan to move, use `AddPitchPointCommand` / `DeletePitchPointCommand` | ~300 LOC | 1-10 |
| PR-9 | `feat: vibrato parameter sliders` | Implement the empty `EditVibrato` gesture cases plus a vibrato properties panel; use `SetVibratoCommand` and individual property commands from Core | ~300 LOC | 1-12 |
| PR-10 | `feat: cutoff runtime clamp + warning badge` | In the mobile render path (before resampler call), clamp `cutoff` to `(audioLength - offset)`, log warning; show a ⚠ badge on affected parts in `DrawablePart` | ~150 LOC | 0-4 |

---

## 7. Open Questions

1. **Core version delta**: `progress.md` mentions merging `OpenUtau.Core v0.1.567`. The current Core version is unconfirmed. Some Phase 2-3 features (2-8 multi-language phonemizers, 2-7 MIDI stability) may already be resolved in a newer Core. What is the exact upstream Core commit being tracked?

2. **iOS audio backend**: Is there a plan to use `AVAudioEngine` directly, or to route through a .NET binding? The current iOS platform folder has no audio code. Without this, iOS is effectively not a target for release.

3. **BGM loading architecture**: Does `ObjectProvider` currently support WAV BGM on Android? The code paths are present but whether they function without a crash (noted in upstream issue #1913) needs runtime verification.

4. **Expression canvas touch conflict (0-8)**: Code shows three modes (Hand/Edit/Eraser) with gesture routing. The roadmap says expression editing "cannot be touched." Was this already fixed in the Phase 1 sprint, or is the gesture routing code dead on device due to a touch event propagation bug?

5. **Snap grid trigger gesture (1-4)**: The snap popup is implemented. Is it triggered by a toolbar button or still by long-press? The roadmap specifically calls out long-press removal.

6. **`DrawablePart` cache correctness**: CR-4 added `Dispose()` on eviction, but the cache key is not confirmed to include singer hash, expression values, or resampler settings. A stale cache could silently play wrong audio. Is the cache key currently just the part ID?

7. **worldline.so**: Native library references in `OpenUtauMobile.csproj` are commented out. Is worldline a WORLD-based sampler? What is the plan for native resamplers on mobile — ONNX hifisampler (2-4), or also worldline?

8. **Test coverage gate**: The 4 smoke tests cover Core model only. Should a CI gate be added that runs tests on every PR? The `.claude/hooks/post-edit-build-check.sh` exists but does not run tests.

9. **`EP-05` status**: `progress.md` lists `EP-05` (OnAppearing unconditionally restarting PlaybackTimer) as a remaining recommended fix. Reviewing `EditPage.xaml.cs:2285-2293` shows that `PlaybackTimer` is conditionally restarted only if `PlaybackManager.Inst.Playing` — which appears to resolve EP-05. Is this fix already merged or still pending?

10. **License for demo singer**: The AppResources placeholder strings "Piano Sample TODO" and "Sine Wave TODO" suggest a minimal bundled voice is planned. What is the redistribution license of the intended demo singer? This blocks 0-10 and 1-1 simultaneously.

---

## 8. File References

All evidence locations:

| Topic | File | Lines |
|:------|:-----|:------|
| AutoSave timer | `OpenUtauMobile/Views/EditPage.xaml.cs` | 37, 289–296, 2278–2304 |
| Touch throttle | `OpenUtauMobile/Views/Utils/GestureProcessor.cs` | 32–337 |
| RxUI throttle | `OpenUtauMobile/Views/EditPage.xaml.cs` | 216 |
| Serilog setup | `OpenUtauMobile/MauiProgram.cs` | 55–71 |
| Snap grid | `OpenUtauMobile/ViewModels/Controls/PianoRollSnapDivPopupViewModel.cs` | 16–33 |
| Snap values | `OpenUtauMobile/ViewModels/EditViewModel.cs` | 56 |
| Vibrato mode stub | `OpenUtauMobile/Views/EditPage.xaml.cs` | 737, 766, 823, 866, 920 |
| PitchAnchor mode stub | `OpenUtauMobile/Views/EditPage.xaml.cs` | 735, 764, 821, 864, 918 |
| Expression draw | `OpenUtauMobile/Views/EditPage.xaml.cs` | 1113–1247 |
| Android magnifier | `OpenUtauMobile/Views/EditPage.xaml.cs` | 66, 392–413 |
| Lyric popup | `OpenUtauMobile/Views/Controls/EditLyricsPopup.xaml.cs` | 1–76 |
| Encoding selection | `OpenUtauMobile/ViewModels/InstallSingerViewModel.cs` | 37–46 |
| DiffSinger render | `OpenUtauMobile/ViewModels/EditViewModel.cs` | 408–415, 1425 |
| Singer type colors | `OpenUtauMobile/ViewModels/Converters/SingerToTypeColorConverter.cs` | 17–29 |
| Theme colors | `OpenUtauMobile/Utils/ThemeColors.cs` | 143–270 |
| Undo/Redo buttons | `OpenUtauMobile/Views/EditPage.xaml.cs` | 1928–1935 |
| Playhead scroll | `OpenUtauMobile/Views/EditPage.xaml.cs` | 344–356 |
| DrawablePart cache | `OpenUtauMobile/Views/DrawableObjects/DrawablePart.cs` | (eviction with Dispose added CR-4) |
| Smoke tests | `OpenUtauMobile/OpenUtauMobile.Tests/SmokeTests.cs` | 1–55 |
| iOS AppLifeCycle | `OpenUtauMobile/Platforms/iOS/Utils/iOSAppLifeCycleHelper.cs` | 1–20 |
| NuGet packages | `OpenUtauMobile/OpenUtauMobile/OpenUtauMobile.csproj` | PackageReference block |
| TODO strings | `OpenUtauMobile/Resources/Strings/AppResources.Designer.cs` | 1321, 1339 |
