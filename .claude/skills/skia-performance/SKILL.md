---
name: skia-performance
description: SkiaSharp performance rules, SKPaint/SKFont/SKPath caching patterns, PaintSurface optimization, and dirty-region tracking patterns for OpenUtau Mobile.
---
# SkiaSharp Performance Rules
## Cardinal Rule
**NEVER allocate SKPaint, SKFont, SKPath, SKBitmap, or SKImage inside a PaintSurface handler.**
These are native (unmanaged) objects. Allocating them per frame causes:
1. Native memory leaks (if not disposed)
2. GC pressure (finalizer queue)
3. Frame time spikes (allocation + initialization cost)
## Caching Patterns
### Static Readonly (Theme-Independent)
```csharp
private static readonly SKPaint _gridPaint = new()
{
    Color = SKColors.Gray,
    Style = SKPaintStyle.Stroke,
    StrokeWidth = 1,
};
```
Use for: Colors that never change. Shared across all instances.
Do NOT dispose in instance Dispose() — they outlive any single instance.
### Instance Readonly (Theme-Dependent)
```csharp
private readonly SKPaint _pitchLinePaint = new()
{
    Color = ThemeColorsManager.Current.PitchLine,
    Style = SKPaintStyle.Stroke,
    StrokeWidth = 4,
};
```
Use for: Colors from ThemeColorsManager. Captured at construction time.
MUST be disposed in instance Dispose().
NOTE: Runtime theme changes are NOT supported — page must be recreated.
### SKPath Reuse (Reset Pattern)
```csharp
// Field:
private readonly SKPath _envelopePath = new();
// In PaintSurface:
_envelopePath.Reset();
_envelopePath.MoveTo(x1, y1);
_envelopePath.LineTo(x2, y2);
canvas.DrawPath(_envelopePath, paint);
```
Use for: Paths that change shape every frame. Reset() is far cheaper than new+dispose.
## Drawable Object Pattern
Drawable objects (DrawableNotes, DrawablePart, etc.) are cached in Dictionaries
keyed by their data model (UPart, etc.).
Rules:
- Create once, update properties, call Draw() repeatedly
- Canvas property is set per-frame (different SKCanvas each PaintSurface call)
- SKPaint fields are owned by the Drawable, disposed in Dispose()
- When evicting from cache: ALWAYS call Dispose() on the evicted instance
- IDisposable with _disposed guard and GC.SuppressFinalize at end
## PaintSurface Performance Target
- Target: < 8ms per PaintSurface call
- Measure with Stopwatch around the handler body
- Current baseline: unmeasured (Phase 2 will establish)
## Verification
To verify no PaintSurface allocations:
```bash
grep -n "new SK" OpenUtauMobile/Views/EditPage.xaml.cs
```
All matches should be field initializers only — zero inside method bodies.
## Phase 2 Optimization Targets
1. Dirty-region tracking: only redraw canvases whose data changed
2. Bitmap caching: render static layers to SKBitmap, composite on top
3. Touch-to-render pipeline: minimize layers between touch event and InvalidateSurface
4. ThemeColorsManager.Current: cache in local variable at Draw() start if profiled as hot
## Canvas Transform Safety
- `canvas.Save()` / `canvas.Restore()` MUST be paired
- `canvas.ResetMatrix()` MUST be followed by `canvas.SetMatrix(saved)` before return
- DrawablePart and others use this pattern — verify on any new Drawable
