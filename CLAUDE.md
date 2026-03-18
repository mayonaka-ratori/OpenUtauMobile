# CLAUDE.md

## Project

OpenUtau Mobile — singing voice synthesis editor for Android/iOS.
Fork of vocoder712/OpenUtauMobile. Uses OpenUtau Core engine (stakira/OpenUtau).

## Build

  dotnet build OpenUtauMobile/OpenUtauMobile.csproj -f net9.0-android  # Android
  dotnet build OpenUtauMobile/OpenUtauMobile.csproj -f net9.0-ios      # iOS (macOS only)
  dotnet test OpenUtauMobile.Tests/                                     # Tests

## Rules

1. OpenUtau.Core/ and OpenUtau.Plugin.Builtin/ — avoid modifications.
   If you must, document in docs/CORE_PATCHES.md.
2. All data changes: DocManager.Inst.StartUndoGroup() then .ExecuteCmd(cmd) then .EndUndoGroup()
3. ViewModels must implement ICmdSubscriber + ReactiveUI subscriptions. Clean up both on dispose.
4. SkiaSharp objects: cache as fields, never allocate in PaintSurface.
5. Touch handlers: throttle to 60Hz or less.
6. Build-check before every commit.

## Conventions

- Branches: fix/, feat/, perf/ + short name
- Commits: Conventional Commits (fix:, feat:, perf:, refactor:, test:, docs:)
- Nullable enabled, ImplicitUsings enabled

## References

- .claude/skills/ — architecture, Core API, SkiaSharp patterns, MAUI patterns
- .claude/agents/ — subagent definitions (auditor, implementer, tester)
- .claude/progress.md — sprint status
