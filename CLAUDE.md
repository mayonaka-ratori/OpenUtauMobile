# OpenUtau Mobile — Claude Code Project Guide

## Quick Start

- **Always read first**: `.claude/agents/pm.md` (full project context, workflow, prompt templates)
- **Report language**: Japanese
- **Prompt language**: English (for Claude Code tasks)

## Skill Files (read as needed)

| File | When to Read |
|------|-------------|
| `.claude/agents/pm.md` | Every session start |
| `.claude/skills/editpage-architecture/SKILL.md` | EditPage.xaml.cs changes, PaintSurface work, gesture handling |
| `.claude/skills/gesture-processor/SKILL.md` | Touch/gesture bugs, GestureProcessor changes |
| `.claude/skills/skia-performance/SKILL.md` | Canvas rendering optimization |
| `.claude/skills/maui-mobile-patterns/SKILL.md` | MAUI lifecycle, navigation, platform-specific code |
| `.claude/skills/openutau-core-api/SKILL.md` | DocManager, Command pattern, ICmdSubscriber |
| `.claude/skills/project-overview/SKILL.md` | Solution structure, NuGet deps, folder layout |

## Build & Test

```bash
dotnet build OpenUtauMobile/OpenUtauMobile.csproj -f net9.0-android -c Debug
dotnet test OpenUtauMobile.Tests/ -f net9.0
```

Expected: **0 errors** / **12/12 tests pass** / 1605 warnings (pre-existing, not our fault).

## Key Constraints

1. Do NOT modify `OpenUtau.Core/` or `OpenUtau.Plugin.Builtin/` without explicit PM approval. Document in `docs/CORE_PATCHES.md`.
2. All data changes: use `using var undo = new UndoScope()` (OpenUtauMobile.Utils) — wraps StartUndoGroup/EndUndoGroup. For spanning gestures use field-based `UndoScope?` pattern.
3. SkiaSharp objects: cache as fields, **never allocate in PaintSurface**.
4. Touch handlers: throttle to 60Hz or less (`ThrottleIntervalMs = 16`).
5. `Console.WriteLine` for logcat output — `Debug.WriteLine` is **invisible** on Android.
6. All reports in **Japanese**.
7. Conventional Commits: `fix:`, `perf:`, `feat:`, `refactor:`, `docs:`, `test:`
8. Update `.claude/progress-phase2.5.md` on every commit (Phase 2.5 active).

## Current Focus

- **Phase 2**: ✅ COMPLETE (2026-03-21) — see `.claude/progress-phase2.md`
- **Phase 2.5**: ✅ COMPLETE (2026-04-12, 44/44 UndoScope 移行) — see `.claude/progress-phase2.5.md`
- **Phase 3**: ✅ COMPLETE (2026-04-14) — see `.claude/progress-phase3.md`
  - ✅ Step A: ビブラート編集 UI (A-1 ViewModel / A-2,3 XAML+Toolbar / A-4 波形オーバーレイ)
  - ✅ Step B: フォネーム編集 (B-1 ViewModel / B-2 タップダイアログ)
  - ✅ Step C: クオンタイズボタングループ (C-1)
  - ✅ 後続: テスト追加 (17/17) + E-01/E-02/E-03 + OP-01 fix + BUG-D close
- **Phase 4**: 未着手 — デバイステスト → Phase 0/1 残タスク
- **EditPage**: 4 partial files (xaml.cs ~1750L + Rendering.cs + Toolbar.cs + CmdSubscriber.cs)

## Test Device

- **Device**: Pixel 10 Pro XL, Android 16 (API 36), density ≈ 3.5
- **ADB path**: `C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe`
- **Logcat filter**:
  ```powershell
  & "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" logcat -d | Select-String "PaintSurface Performance" -Context 0,12
  ```
