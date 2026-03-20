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
dotnet test OpenUtauMobile.Tests/
```

Expected: **0 errors** / **12/12 tests pass** / 1738 warnings (pre-existing, not our fault).

## Key Constraints

1. Do NOT modify `OpenUtau.Core/` or `OpenUtau.Plugin.Builtin/` without explicit PM approval. Document in `docs/CORE_PATCHES.md`.
2. All data changes: `DocManager.Inst.StartUndoGroup()` → `.ExecuteCmd(cmd)` → `.EndUndoGroup()`
3. SkiaSharp objects: cache as fields, **never allocate in PaintSurface**.
4. Touch handlers: throttle to 60Hz or less (`ThrottleIntervalMs = 16`).
5. `Console.WriteLine` for logcat output — `Debug.WriteLine` is **invisible** on Android.
6. All reports in **Japanese**.
7. Conventional Commits: `fix:`, `perf:`, `feat:`, `refactor:`, `docs:`, `test:`
8. Update `.claude/progress-phase2.md` on every commit.

## Current Focus

- **Phase 2**: Performance + Touch optimization
- **Active**: Stage B — touch interaction fixes (BUG-A/B/C code complete, device testing pending)
- **Next**: Stage C (cleanup P2-8a〜g), then performance re-measurement
- **Key commits**: `5798ac6` (BUG-C+UI1), `c6afc55` (BUG-A+B), `974f97a` (P2-B2 DrawableNotes)

## Test Device

- **Device**: Pixel 10 Pro XL, Android 16 (API 36), density ≈ 3.5
- **ADB path**: `C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe`
- **Logcat filter**:
  ```powershell
  & "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" logcat -d | Select-String "PaintSurface Performance" -Context 0,12
  ```
