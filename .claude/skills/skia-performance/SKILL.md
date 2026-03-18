---
name: skia-performance
description: SkiaSharp performance rules for mobile — SKPaint caching, draw clipping, path batching, bitmap caching, touch throttling, and InvalidateSurface discipline. Load when working on any SKCanvasView drawing or touch handling code.
---

# Skia Performance

## Problem

SKCanvasView.PaintSurface fires every frame when invalidated.
Naive implementation causes GC pressure and dropped frames on mobile devices.

## Rule 1: Cache all SKPaint/SKFont/SKTypeface as static or instance fields

BAD — allocates every frame:

```csharp
void OnPaintSurface(SKPaintSurfaceEventArgs e) {
    using var paint = new SKPaint { Color = SKColors.Red };
    canvas.DrawRect(rect, paint);
}
```

GOOD — allocate once, reuse:

```csharp
private static readonly SKPaint NotePaint = new() {
    Color = SKColors.Red,
    IsAntialias = true
};
void OnPaintSurface(SKPaintSurfaceEventArgs e) {
    canvas.DrawRect(rect, NotePaint);
}
```

## Rule 2: Clip before draw

Only render elements within the visible area.

```csharp
float viewLeft = scrollX;
float viewRight = scrollX + canvasWidth;
foreach (var note in notes) {
    if (note.Right < viewLeft || note.Left > viewRight) continue;
    // draw note
}
```

## Rule 3: Batch paths

Combine multiple small shapes into a single SKPath where possible.

```csharp
var path = new SKPath();  // cached as field, call Reset() each frame
foreach (var note in visibleNotes) {
    path.AddRect(note.Rect);
}
canvas.DrawPath(path, NotePaint);
```

## Rule 4: SKBitmap caching for static content

For elements that rarely change (piano keyboard, grid lines),
render to an SKBitmap once, then blit with DrawBitmap each frame.
Invalidate the cached bitmap only when zoom/scroll changes significantly.

## Rule 5: Touch throttling pattern

```csharp
private long _lastTouchTicks = 0;
void OnTouch(SKTouchEventArgs e) {
    long now = Environment.TickCount64;
    if (now - _lastTouchTicks < 16) return;  // ~60fps cap
    _lastTouchTicks = now;
    // process touch
}
```

## Rule 6: Avoid InvalidateSurface() storms

Batch multiple state changes before calling InvalidateSurface() once.
Never call InvalidateSurface() inside a loop or from every property setter.

## Performance measurement

Add timing in PaintSurface during development:

```csharp
var sw = System.Diagnostics.Stopwatch.StartNew();
// ... drawing code ...
sw.Stop();
System.Diagnostics.Debug.WriteLine($"Paint: {sw.ElapsedMilliseconds}ms");
```

Target: under 8ms per frame (leaves headroom for 60fps).
