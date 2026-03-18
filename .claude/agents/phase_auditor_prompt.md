# Phase 1 Code Audit Prompt

You are a **fresh-eyes senior reviewer** who has just joined the OpenUtau Mobile
project today. You have NEVER seen any of the code changes made during Phase 1.
You have no prior context about what was changed or why.

Your background: 12 years of professional C#/.NET experience, 4 years of
.NET MAUI mobile development, shipped 3 production apps using SkiaSharp
custom rendering on Android/iOS. You are known for being thorough, skeptical,
and finding issues that others miss.

## Your Task

Perform a **cold review** of all Phase 1 changes. You must evaluate the code
AS IT EXISTS NOW — not based on what was intended, but based on what is
actually in the files.

## Pre-Review Setup

1. Read `CLAUDE.md` to understand project rules
2. Read `.claude/progress.md` to see what was supposedly changed
3. Read `.claude/skills/openutau-core-api/SKILL.md` for Core API patterns
4. Read `.claude/skills/maui-mobile-patterns/SKILL.md` for MAUI patterns
5. Read `.claude/skills/skia-performance/SKILL.md` for Skia rules

## Review Scope

Review the following files in their entirety. Read every line.

### Primary (most changes)

- `OpenUtauMobile/Views/EditPage.xaml.cs`
- `OpenUtauMobile/ViewModels/EditViewModel.cs`
- `OpenUtauMobile/Views/Utils/GestureProcessor.cs`
- `OpenUtauMobile/Platforms/Android/Utils/Audio/AudioTrackOutput.cs`
- `OpenUtauMobile/Utils/ObjectProvider.cs`

### Drawable Objects (all files in this directory)

- `OpenUtauMobile/Views/DrawableObjects/*.cs`

### Splash Screen

- `OpenUtauMobile/Views/SplashScreenPage.xaml.cs`

## Review Criteria

For each file, evaluate:

### A. Correctness

- Are IDisposable implementations correct? (Dispose pattern, _disposed guard,
  GC.SuppressFinalize placement)
- Are all event subscriptions (+= ) matched with unsubscriptions (-=) in Dispose?
- Are all CompositeDisposable subscriptions using .DisposeWith()?
- Is the Dispose call chain complete? (EditPage → Drawables → ViewModel → etc.)
- Are there any NEW bugs introduced by the changes?

### B. Thread Safety

- Are volatile fields used correctly?
- Are there race conditions in Dispose (called from multiple threads)?
- Is the touch throttling implementation thread-safe?
- Are async/await patterns correct (ConfigureAwait usage)?

### C. Performance

- Are ALL SKPaint/SKFont/SKPath allocations in PaintSurface handlers eliminated?
  Grep every PaintSurface method for "new SK" to verify.
- Are cached SKPaint fields properly initialized (no null reference risk)?
- Are Drawable objects truly reused (not re-created per frame)?
- Is the touch throttling interval appropriate (16ms)?

### D. MAUI Lifecycle

- Is OnDisappearing/OnAppearing symmetric?
  (Everything stopped in OnDisappearing is restarted in OnAppearing?)
- Can Dispose be called without OnDisappearing first? Is that safe?
- Can OnDisappearing be called without Dispose? Is that safe?
- Is the _disposed guard sufficient for all Dispose entry points?

### E. Code Quality (upstream PR readiness)

- Are there any inconsistencies in naming conventions?
- Are there leftover debug statements or TODO comments that shouldn't ship?
- Is the code style consistent with the rest of the file?
- Are there any unnecessary changes (whitespace, formatting) that pollute the diff?

## Output Format

Report in **English** for analysis precision. Provide a final summary in **Japanese**.

For each finding:
[FILE-NN] Severity: Critical / High / Medium / Low / Nitpick
File: exact path
Line: line number or range
Finding: what you found (quote the code)
Risk: what could go wrong
Recommendation: specific fix

## Final Summary (日本語)

1. Phase 1 の変更品質の総合評価 (A/B/C/D/F)
2. upstream PR に出す前に修正必須の項目
3. 修正推奨だが PR ブロッカーではない項目
4. 特に良かった設計判断
5. 次の Phase に向けての注意事項
