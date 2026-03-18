using OpenUtauMobile.Views.Utils;
using SkiaSharp;
using Xunit;

namespace OpenUtauMobile.Tests;

/// <summary>
/// Regression tests for Transformer — coordinate math, pan, and zoom.
/// Transformer has zero MAUI dependencies and can be tested in plain net9.0.
/// </summary>
public class TransformerTests
{
    // 1. Default construction
    [Fact]
    public void Constructor_DefaultValues_AllZeroExceptZoom()
    {
        var t = new Transformer();

        Assert.Equal(0f, t.PanX);
        Assert.Equal(0f, t.PanY);
        Assert.Equal(1f, t.ZoomX);
        Assert.Equal(1f, t.ZoomY);
    }

    // 2. SetPanX within default limits [-500, 0]
    [Fact]
    public void SetPanX_WithinLimits_UpdatesProperty()
    {
        var t = new Transformer();

        t.SetPanX(-100f);

        Assert.Equal(-100f, t.PanX);
    }

    // 3. SetZoomX within default limits [1, 20]
    [Fact]
    public void SetZoomX_WithinLimits_UpdatesProperty()
    {
        var t = new Transformer();

        t.SetZoomX(5f);

        Assert.Equal(5f, t.ZoomX);
    }

    // 4. Identity transform: pan=0, zoom=1 → ActualToLogical returns same point
    [Fact]
    public void ActualToLogical_Identity_ReturnsSamePoint()
    {
        var t = new Transformer(); // PanX=0, PanY=0, ZoomX=1, ZoomY=1

        var result = t.ActualToLogical(new SKPoint(10f, 20f));

        Assert.Equal(10f, result.X);
        Assert.Equal(20f, result.Y);
    }

    // 5. With ZoomX=2: actual X is divided by zoom
    [Fact]
    public void ActualToLogical_WithZoom_ReturnsCorrectPoint()
    {
        var t = new Transformer();
        t.SetZoomX(2f); // ZoomX=2, ZoomY stays 1

        // (actual.X - PanX) / ZoomX = (20 - 0) / 2 = 10
        // (actual.Y - PanY) / ZoomY = (30 - 0) / 1 = 30
        var result = t.ActualToLogical(new SKPoint(20f, 30f));

        Assert.Equal(10f, result.X);
        Assert.Equal(30f, result.Y);
    }

    // 6. With PanX offset: actual coordinate is shifted before dividing
    [Fact]
    public void ActualToLogical_WithPan_ReturnsCorrectPoint()
    {
        // Constructor bypasses clamping — PanX=-20 is within default [-500, 0]
        var t = new Transformer(panX: -20f);

        // (actual.X - PanX) / ZoomX = (10 - (-20)) / 1 = 30
        // (actual.Y - PanY) / ZoomY = (0 - 0) / 1 = 0
        var result = t.ActualToLogical(new SKPoint(10f, 0f));

        Assert.Equal(30f, result.X);
        Assert.Equal(0f, result.Y);
    }

    // 7. Full pan gesture cycle: StartPan → UpdatePan → EndPan
    [Fact]
    public void StartPan_UpdatePan_EndPan_UpdatesPosition()
    {
        var t = new Transformer(); // PanX=0, PanY=0

        t.StartPan(new SKPoint(100f, 100f));
        // delta = (60-100, 70-100) = (-40, -30)
        // PanX = Clamp(0 + (-40), -500, 0) = -40
        // PanY = Clamp(0 + (-30), -100, 0) = -30
        t.UpdatePan(new SKPoint(60f, 70f));
        t.EndPan();

        Assert.Equal(-40f, t.PanX);
        Assert.Equal(-30f, t.PanY);
    }

    // 8. Full zoom gesture cycle: StartZoom → UpdateZoom changes ZoomX
    [Fact]
    public void StartZoom_UpdateZoom_ChangesZoomLevel()
    {
        var t = new Transformer(); // ZoomX=1, ZoomY=1, PanX=0, PanY=0

        // Two fingers at (0,0) and (100,100) — distance 100 on each axis
        t.StartZoom(new SKPoint(0f, 0f), new SKPoint(100f, 100f));
        // Move to (0,0) and (200,200) — distance doubles to 200 on each axis
        // scaleX = 200/100 = 2 → ZoomX = Clamp(1*2, 1, 20) = 2
        t.UpdateZoom(new SKPoint(0f, 0f), new SKPoint(200f, 200f));

        Assert.Equal(2f, t.ZoomX);
    }
}
