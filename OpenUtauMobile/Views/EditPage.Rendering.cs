// EditPage.Rendering.cs — PaintSurface handlers and rendering helpers (partial class)
// Extracted from EditPage.xaml.cs in Phase 2.5 Step 3.
//
// NOTE: All SKPaint/SKFont/SKPath fields, bitmap cache fields, Drawable instance
// caches, and state fields (e.g., isDrawingExpression, drawingExpressionPointer,
// IsUserDrawingCurve, TouchingPoint, _stalePartKeysBuffer) are declared in
// EditPage.xaml.cs. They are accessible here because this is a partial class
// of the same EditPage.

using OpenUtau.Core;
using OpenUtau.Core.Render;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using OpenUtauMobile.Utils;
using OpenUtauMobile.ViewModels;
using OpenUtauMobile.Views.DrawableObjects;
using OpenUtauMobile.Views.Utils;
using SkiaSharp;
using Preferences = OpenUtau.Core.Util.Preferences;

namespace OpenUtauMobile.Views;

public partial class EditPage
{
    /// <summary>
    /// Redraws the track canvas.
    /// </summary>
    private void TrackCanvas_PaintSurface(object sender, SkiaSharp.Views.Maui.SKPaintSurfaceEventArgs e)
    {
#if DEBUG
        var _sw = PaintSurfaceProfiler.Start();
#endif
        try
        {
        // Clear canvas
        e.Surface.Canvas.Clear(SKColors.Transparent);
        if (e.Surface.Canvas.DeviceClipBounds.Height < 5) // Early exit for near-zero height — avoids unnecessary work
        {
            return;
        }
        // Apply canvas transform
        e.Surface.Canvas.SetMatrix(_viewModel.TrackTransformer.GetTransformMatrix());
        // Draw track separator lines (reuse cached instance)
        _drawableTrackBackground ??= new DrawableTrackBackground(e.Surface.Canvas, _viewModel.HeightPerTrack * _viewModel.Density);
        _drawableTrackBackground.Canvas = e.Surface.Canvas;
        _drawableTrackBackground.HeightPerTrack = _viewModel.HeightPerTrack * _viewModel.Density;
        _drawableTrackBackground.Draw();
        // Clear the drawable-parts collection
        _viewModel.DrawableParts.Clear();
        // キャッシュから削除されたパートのエントリを除去（P2-C3: .Where().ToList() → 再利用バッファで毎フレームアロケーション排除）
        var currentParts = DocManager.Inst.Project.parts;
        _stalePartKeysBuffer.Clear();
        foreach (var key in _drawablePartCache.Keys)
        {
            if (!currentParts.Contains(key))
                _stalePartKeysBuffer.Add(key);
        }
        foreach (var staleKey in _stalePartKeysBuffer)
        {
            _drawablePartCache[staleKey].Dispose();
            _drawablePartCache.Remove(staleKey);
        }
        // Draw parts (DrawablePart instances cached per UPart)
        foreach (UPart part in currentParts)
        {
            bool isResizeable = _viewModel.SelectedParts.Contains(part) && _viewModel.CurrentTrackEditMode == TrackEditMode.Edit;
            if (!_drawablePartCache.TryGetValue(part, out var drawablePart))
            {
                drawablePart = new DrawablePart(_viewModel);
                _drawablePartCache[part] = drawablePart;
            }
            drawablePart.Update(e.Surface.Canvas, part,
                isSelected: _viewModel.SelectedParts.Contains(part),
                isResizable: isResizeable);
            _viewModel.DrawableParts.Add(drawablePart);
            drawablePart.Draw();
        }
        }
        finally
        {
#if DEBUG
            PaintSurfaceProfiler.End(_sw, nameof(TrackCanvas));
#endif
        }
    }

    private void PianoRollCanvas_PaintSurface(object sender, SkiaSharp.Views.Maui.SKPaintSurfaceEventArgs e)
    {
#if DEBUG
        var _sw = PaintSurfaceProfiler.Start();
#endif
        try
        {
        // Clear canvas
        e.Surface.Canvas.Clear(SKColors.Transparent);
        if (e.Surface.Canvas.DeviceClipBounds.Height < 5)
        {
            // Early exit for near-zero height
            return;
        }
        if (_viewModel.SelectedParts.Count == 0)
        {
            return; // No selected part — nothing to draw
        }
        // Apply canvas transform
        //e.Surface.Canvas.SetMatrix(_viewModel.PianoRollTransformer.GetTransformMatrix());
        if (_viewModel.SelectedParts[0] is UVoicePart part)
        {
            // DrawableNotes インスタンスをキャッシュし、毎フレームの new を排除
            if (_drawableNotes == null)
            {
                _drawableNotes = new DrawableNotes(e.Surface.Canvas, part, _viewModel,
                    ViewConstants.TrackSkiaColors[DocManager.Inst.Project.tracks[part.trackNo].TrackColor]);
            }
            else
            {
                _drawableNotes.Canvas = e.Surface.Canvas;
                _drawableNotes.Part = part;
                _drawableNotes.NotesColor = ViewConstants.TrackSkiaColors[DocManager.Inst.Project.tracks[part.trackNo].TrackColor];
            }
            _drawableNotes.Draw();
            _viewModel.EditingNotes = _drawableNotes;
            // EditVibrato モード時にビブラート波形オーバーレイを描画
            if (_viewModel.CurrentNoteEditMode == NoteEditMode.EditVibrato)
                DrawVibratoOverlay(e.Surface.Canvas, e.Info, part);
        }
        }
        finally
        {
#if DEBUG
            PaintSurfaceProfiler.End(_sw, nameof(PianoRollCanvas));
#endif
        }
    }

    private void PlaybackPosCanvas_PaintSurface(object sender, SkiaSharp.Views.Maui.SKPaintSurfaceEventArgs e)
    {
#if DEBUG
        var _sw = PaintSurfaceProfiler.Start();
#endif
        SKCanvas Canvas = e.Surface.Canvas;
        // Clear canvas
        Canvas.Clear(SKColors.Transparent);
        // Compute playback-line X position
        float x = (float)(_viewModel.PlayPosTick * _viewModel.TrackTransformer.ZoomX + _viewModel.TrackTransformer.PanX);
        Canvas.DrawLine(x, 0f, x, Canvas.DeviceClipBounds.Height, _playbackPosPaint);
#if DEBUG
        PaintSurfaceProfiler.End(_sw, nameof(PlaybackPosCanvas));
#endif
    }

    private void PianoKeysCanvas_PaintSurface(object sender, SkiaSharp.Views.Maui.SKPaintSurfaceEventArgs e)
    {
#if DEBUG
        var _sw = PaintSurfaceProfiler.Start();
#endif
        var canvas = e.Surface.Canvas;
        var screenWidth = (float)e.Info.Width;
        var screenHeight = (float)e.Info.Height;
        if (screenWidth < 1 || screenHeight < 1)
        {
#if DEBUG
            PaintSurfaceProfiler.End(_sw, nameof(PianoKeysCanvas));
#endif
            return;
        }
        var transformer = _viewModel.PianoRollTransformer;
        float currentPanY = transformer.PanY;
        float currentZoomY = transformer.ZoomY;
        int currentProjectKey = DocManager.Inst.Project?.key ?? 0;
        bool cacheInvalid =
            _pianoKeysCacheBitmap == null ||
            _pianoKeysCacheBitmap.Width != (int)screenWidth ||
            _pianoKeysCacheBitmap.Height != (int)(screenHeight * (1 + 2 * PIANO_KEYS_CACHE_MARGIN)) ||
            _pianoKeysCachedZoomY != currentZoomY ||
            _pianoKeysCachedProjectKey != currentProjectKey ||
            Math.Abs(currentPanY - _pianoKeysCacheOriginPanY) > screenHeight * PIANO_KEYS_CACHE_MARGIN;
        if (cacheInvalid)
        {
            // Diagnostic logging removed — cache validation complete
            // [PianoKeysCache] MISS: logged during development
            _pianoKeysCacheCanvas?.Dispose();
            _pianoKeysCacheBitmap?.Dispose();
            int bitmapWidth = (int)screenWidth;
            int bitmapHeight = (int)(screenHeight * (1 + 2 * PIANO_KEYS_CACHE_MARGIN));
            _pianoKeysCacheBitmap = new SKBitmap(bitmapWidth, bitmapHeight);
            _pianoKeysCacheCanvas = new SKCanvas(_pianoKeysCacheBitmap);
            float cachePanY = currentPanY - screenHeight * PIANO_KEYS_CACHE_MARGIN;
            DrawPianoKeysToCanvas(_pianoKeysCacheCanvas, bitmapWidth, bitmapHeight, cachePanY, currentZoomY, currentProjectKey);
            _pianoKeysCacheImage?.Dispose();
            _pianoKeysCacheImage = SKImage.FromBitmap(_pianoKeysCacheBitmap);
            _pianoKeysCacheOriginPanY = currentPanY;
            _pianoKeysCachedZoomY = currentZoomY;
            _pianoKeysCachedProjectKey = currentProjectKey;
        }
        else
        {
            // Diagnostic logging removed — cache validation complete
            // [PianoKeysCache] HIT: logged during development
        }
        canvas.Clear();
        float offsetY = (currentPanY - _pianoKeysCacheOriginPanY) + screenHeight * PIANO_KEYS_CACHE_MARGIN;
        {
            float srcTop = Math.Max(0, offsetY);
            float srcBottom = Math.Min(_pianoKeysCacheImage!.Height, offsetY + screenHeight);
            float dstTop = srcTop - offsetY;
            var srcRect = new SKRect(0, srcTop, screenWidth, srcBottom);
            var dstRect = new SKRect(0, dstTop, screenWidth, dstTop + (srcBottom - srcTop));
            canvas.DrawImage(_pianoKeysCacheImage, srcRect, dstRect);
        }
#if DEBUG
        PaintSurfaceProfiler.End(_sw, nameof(PianoKeysCanvas));
#endif
    }

    private void DrawPianoKeysToCanvas(SKCanvas Canvas, float width, float height, float panY, float zoomY, int projectKey)
    {
        Canvas.Clear();
        float heightPerPianoKey = (float)(_viewModel.HeightPerPianoKey * _viewModel.Density);

        float viewTop = -panY / zoomY;
        float viewBottom = viewTop + height / zoomY;
        int topKeyNum = Math.Max(0, (int)Math.Floor(viewTop / heightPerPianoKey));
        int bottomKeyNum = Math.Min(ViewConstants.TotalPianoKeys, (int)Math.Ceiling(viewBottom / heightPerPianoKey));
        float y = topKeyNum * heightPerPianoKey * zoomY + panY;
        for (int i = topKeyNum; i < bottomKeyNum; i++)
        {
            _pianoKeysPaint.Color = ViewConstants.PianoKeys[i].IsBlackKey ? ThemeColorsManager.Current.BlackPianoKey : ThemeColorsManager.Current.WhitePianoKey;
            Canvas.DrawRect(0, y, width, heightPerPianoKey * zoomY, _pianoKeysPaint);
            y += heightPerPianoKey * zoomY;
        }
        // Draw key-name labels
        y = (float)(topKeyNum + 0.5f) * heightPerPianoKey * zoomY + panY;
        //heightPerPianoKey = heightPerPianoKey * zoomY;
        PianoKey? drawingKey = null;
        _pianoKeyFont.Size = (float)(heightPerPianoKey * 0.5 * zoomY);
        _pianoKeyFont.Typeface = ObjectProvider.NotoSansCJKscRegularTypeface;
        for (int i = topKeyNum; i < bottomKeyNum; i++)
        {
            drawingKey = ViewConstants.PianoKeys[i];
            int numberedNotationIndex = drawingKey.NoteNum - 60 - projectKey;
            _pianoKeyTextPaint.Color = drawingKey.IsBlackKey ? ThemeColorsManager.Current.BlackPianoKeyText : ThemeColorsManager.Current.WhitePianoKeyText;
            Canvas.DrawText(drawingKey.NoteName, 5, y, _pianoKeyFont, _pianoKeyTextPaint);
            if (numberedNotationIndex >= 0 && numberedNotationIndex <= 11)
            {
                Canvas.DrawText(MusicMath.NumberedNotations[numberedNotationIndex], 50 * (float)_viewModel.Density, y, _pianoKeyFont, _pianoKeyTextPaint);
            }
            y += heightPerPianoKey * zoomY;
        }
        // (Transform matrix reset not needed here)
        //Canvas.SetMatrix(originalMatrix);
    }

    private void TimeLineCanvas_PaintSurface(object sender, SkiaSharp.Views.Maui.SKPaintSurfaceEventArgs e)
    {
#if DEBUG
        var _sw = PaintSurfaceProfiler.Start();
        PaintSurfaceProfiler.End(_sw, nameof(TimeLineCanvas));
#endif
    }

    private void PlaybackTickBackgroundCanvas_PaintSurface(object sender, SkiaSharp.Views.Maui.SKPaintSurfaceEventArgs e)
    {
#if DEBUG
        var _sw = PaintSurfaceProfiler.Start();
#endif
        var canvas = e.Surface.Canvas;
        var screenWidth = (float)e.Info.Width;
        var screenHeight = (float)e.Info.Height;
        if (screenWidth < 1 || screenHeight < 1)
        {
#if DEBUG
            PaintSurfaceProfiler.End(_sw, nameof(PlaybackTickBackgroundCanvas));
#endif
            return;
        }
        var transformer = _viewModel.TrackTransformer;
        float currentPanX = transformer.PanX;
        float currentZoomX = transformer.ZoomX;
        int currentSnapDiv = _viewModel.TrackSnapDiv;
        int currentTempoHash = ComputeTempoTimeSignatureHash();
        // キャッシュ無効化判定
        bool cacheInvalid =
            _tickBgCacheBitmap == null ||
            _tickBgCacheBitmap.Width != (int)(screenWidth * (1 + 2 * TICK_BG_CACHE_MARGIN)) ||
            _tickBgCacheBitmap.Height != (int)screenHeight ||
            _tickBgCachedZoomX != currentZoomX ||
            _tickBgCachedSnapDiv != currentSnapDiv ||
            _tickBgCachedTempoHash != currentTempoHash ||
            Math.Abs(currentPanX - _tickBgCacheOriginPanX) > screenWidth * TICK_BG_CACHE_MARGIN;
        if (cacheInvalid)
        {
            // Diagnostic logging removed — cache validation complete
            // [TickBgCache] MISS: logged during development
            // 旧キャッシュを破棄
            _tickBgCacheCanvas?.Dispose();
            _tickBgCacheBitmap?.Dispose();
            // オーバーサイズビットマップを生成（スクリーン幅の3倍）
            int bitmapWidth = (int)(screenWidth * (1 + 2 * TICK_BG_CACHE_MARGIN));
            int bitmapHeight = (int)screenHeight;
            _tickBgCacheBitmap = new SKBitmap(bitmapWidth, bitmapHeight);
            _tickBgCacheCanvas = new SKCanvas(_tickBgCacheBitmap);
            _tickBgCacheCanvas.Clear(ThemeColorsManager.Current.TrackBackground);
            // キャッシュ用マトリクス：現在のPanXから左マージン分オフセット
            float cachePanX = currentPanX - screenWidth * TICK_BG_CACHE_MARGIN;
            var cacheMatrix = SKMatrix.CreateScale(currentZoomX, transformer.ZoomY)
                .PostConcat(SKMatrix.CreateTranslation(cachePanX, transformer.PanY));
            _tickBgCacheCanvas.SetMatrix(cacheMatrix);
            // 既存 Drawable を再利用してビットマップに描画
            _drawableTickBackground ??= new DrawableTickBackground(_tickBgCacheCanvas, _viewModel);
            _drawableTickBackground.Canvas = _tickBgCacheCanvas;
            _drawableTickBackground.Draw();
            _tickBgCacheImage?.Dispose();
            _tickBgCacheImage = SKImage.FromBitmap(_tickBgCacheBitmap);
            // キャッシュ状態を記録
            _tickBgCacheOriginPanX = currentPanX;
            _tickBgCachedZoomX = currentZoomX;
            _tickBgCachedSnapDiv = currentSnapDiv;
            _tickBgCachedTempoHash = currentTempoHash;
        }
        // Diagnostic logging removed — cache validation complete
        // [TickBgCache] HIT: logged during development
        // キャッシュビットマップをオフセット描画
        canvas.Clear(ThemeColorsManager.Current.TrackBackground);
        float offsetX = (currentPanX - _tickBgCacheOriginPanX) + screenWidth * TICK_BG_CACHE_MARGIN;
        {
            float srcLeft = Math.Max(0, offsetX);
            float srcRight = Math.Min(_tickBgCacheImage!.Width, offsetX + screenWidth);
            float dstLeft = srcLeft - offsetX;
            var srcRect = new SKRect(srcLeft, 0, srcRight, screenHeight);
            var dstRect = new SKRect(dstLeft, 0, dstLeft + (srcRight - srcLeft), screenHeight);
            canvas.DrawImage(_tickBgCacheImage, srcRect, dstRect);
        }
#if DEBUG
        PaintSurfaceProfiler.End(_sw, nameof(PlaybackTickBackgroundCanvas));
#endif
    }

    private void PianoRollTickBackgroundCanvas_PaintSurface(object sender, SkiaSharp.Views.Maui.SKPaintSurfaceEventArgs e)
    {
#if DEBUG
        var _sw = PaintSurfaceProfiler.Start();
#endif
        var canvas = e.Surface.Canvas;
        var screenWidth = (float)e.Info.Width;
        var screenHeight = (float)e.Info.Height;
        if (screenWidth < 1 || screenHeight < 5)
        {
#if DEBUG
            PaintSurfaceProfiler.End(_sw, nameof(PianoRollTickBackgroundCanvas));
#endif
            return;
        }
        var transformer = _viewModel.PianoRollTransformer;
        float currentPanX = transformer.PanX;
        float currentZoomX = transformer.ZoomX;
        int currentSnapDiv = _viewModel.PianoRollSnapDiv;
        // Opt-A: MISS を 2 段階に分離
        //   Tier 1 (needsFullRebuild): サイズ変更 / ズーム変更 / スナップ変更 → Dispose + new SKBitmap
        //   Tier 2 (needsPanRefresh): パンドリフトのみ → 既存ビットマップを再利用して Clear + Redraw
        int expectedBitmapWidth = (int)(screenWidth * (1 + 2 * PIANO_ROLL_TICK_BG_CACHE_MARGIN));
        int expectedBitmapHeight = (int)screenHeight;
        bool needsFullRebuild =
            _pianoRollTickBgCacheBitmap == null ||
            _pianoRollTickBgCacheBitmap.Width != expectedBitmapWidth ||
            _pianoRollTickBgCacheBitmap.Height != expectedBitmapHeight ||
            _pianoRollTickBgCachedZoomX != currentZoomX ||
            _pianoRollTickBgCachedSnapDiv != currentSnapDiv;
        bool needsPanRefresh = !needsFullRebuild
            && Math.Abs(currentPanX - _pianoRollTickBgCacheOriginPanX) > screenWidth * PIANO_ROLL_TICK_BG_CACHE_MARGIN;
        if (needsFullRebuild)
        {
            // Tier 1: ビットマップ破棄・再確保（サイズ/ズーム/スナップ変更時のみ）
            _pianoRollTickBgCacheCanvas?.Dispose();
            _pianoRollTickBgCacheBitmap?.Dispose();
            _pianoRollTickBgCacheBitmap = new SKBitmap(expectedBitmapWidth, expectedBitmapHeight);
            _pianoRollTickBgCacheCanvas = new SKCanvas(_pianoRollTickBgCacheBitmap);
            _pianoRollTickBgCacheCanvas.Clear(SKColors.Transparent);
            float cachePanX = currentPanX - screenWidth * PIANO_ROLL_TICK_BG_CACHE_MARGIN;
            var cacheMatrix = SKMatrix.CreateScale(currentZoomX, transformer.ZoomY)
                .PostConcat(SKMatrix.CreateTranslation(cachePanX, transformer.PanY));
            _pianoRollTickBgCacheCanvas.SetMatrix(cacheMatrix);
            DrawPianoRollGridToCanvas(_pianoRollTickBgCacheCanvas);
            _pianoRollTickBgCacheImage?.Dispose();
            _pianoRollTickBgCacheImage = SKImage.FromBitmap(_pianoRollTickBgCacheBitmap);
            _pianoRollTickBgCacheOriginPanX = currentPanX;
            _pianoRollTickBgCachedZoomX = currentZoomX;
            _pianoRollTickBgCachedSnapDiv = currentSnapDiv;
        }
        else if (needsPanRefresh)
        {
            // Tier 2: ビットマップ再利用（パンドリフトのみ — 38MB SKBitmap アロケーション不要）
            // SKImage は再生成が必要（GPU テクスチャを最新内容に更新）
            _pianoRollTickBgCacheCanvas!.Clear(SKColors.Transparent);
            float cachePanX = currentPanX - screenWidth * PIANO_ROLL_TICK_BG_CACHE_MARGIN;
            var cacheMatrix = SKMatrix.CreateScale(currentZoomX, transformer.ZoomY)
                .PostConcat(SKMatrix.CreateTranslation(cachePanX, transformer.PanY));
            _pianoRollTickBgCacheCanvas.SetMatrix(cacheMatrix);
            DrawPianoRollGridToCanvas(_pianoRollTickBgCacheCanvas);
            _pianoRollTickBgCacheImage?.Dispose();
            _pianoRollTickBgCacheImage = SKImage.FromBitmap(_pianoRollTickBgCacheBitmap);
            _pianoRollTickBgCacheOriginPanX = currentPanX;
        }
        // HIT path: DrawImage (GPU テクスチャ) — DrawBitmap (CPU blit) より大幅高速 (1.7-6.7ms vs 10-25ms)
        canvas.Clear(SKColors.Transparent);
        float offsetX = (currentPanX - _pianoRollTickBgCacheOriginPanX) + screenWidth * PIANO_ROLL_TICK_BG_CACHE_MARGIN;
        {
            float srcLeft = Math.Max(0, offsetX);
            float srcRight = Math.Min(_pianoRollTickBgCacheImage!.Width, offsetX + screenWidth);
            float dstLeft = srcLeft - offsetX;
            var srcRect = new SKRect(srcLeft, 0, srcRight, screenHeight);
            var dstRect = new SKRect(dstLeft, 0, dstLeft + (srcRight - srcLeft), screenHeight);
            canvas.DrawImage(_pianoRollTickBgCacheImage, srcRect, dstRect);
        }
        // シャドウを動的描画（毎フレーム: 最大 2 DrawRect calls — 非常に低コスト）
        DrawPianoRollShadow(canvas, screenWidth, screenHeight, currentPanX, currentZoomX);
        // 再生位置ライン（固定スクリーン位置: 毎フレーム 1 DrawLine call）
        float posX = (float)DeviceDisplay.Current.MainDisplayInfo.Density * ViewConstants.PianoRollPlaybackLinePos;
        canvas.DrawLine(new SKPoint(posX, 0), new SKPoint(posX, screenHeight + 0.5f), _pianoRollPlaybackLinePaint);
#if DEBUG
        PaintSurfaceProfiler.End(_sw, nameof(PianoRollTickBackgroundCanvas));
#endif
    }

    /// <summary>
    /// DrawablePianoRollTickBackground.Draw() のグリッドライン部分のみを再実装。
    /// シャドウと再生位置ラインは含まない（動的描画のため除外）。
    /// </summary>
    private void DrawPianoRollGridToCanvas(SKCanvas canvas)
    {
        // Opt-E: Typeface 変更ガード（不要な割り当てを回避）
        var targetTypeface = ObjectProvider.NotoSansCJKscRegularTypeface;
        if (_pianoRollBarFont.Typeface != targetTypeface)
            _pianoRollBarFont.Typeface = targetTypeface;
        var project = OpenUtau.Core.DocManager.Inst.Project;
        int snapUnit = project.resolution * 4 / _viewModel.PianoRollSnapDiv;
        while (snapUnit < ViewConstants.MinTicklineWidth)
            snapUnit *= 2;
        int canvasWidth = canvas.DeviceClipBounds.Size.Width;
        int canvasHeight = canvas.DeviceClipBounds.Size.Height;
        double minLineTick = ViewConstants.MinTicklineWidth;
        double leftTick = (-canvas.TotalMatrix.TransX) / canvas.TotalMatrix.ScaleX;
        double rightTick = leftTick + canvasWidth / canvas.TotalMatrix.ScaleX;
        float bottom = (-canvas.TotalMatrix.TransY) * canvas.TotalMatrix.ScaleY + canvasHeight;
        project.timeAxis.TickPosToBarBeat((int)leftTick, out int bar, out int _, out int _);
        bar = Math.Max(0, bar);
        if (bar > 0) bar--;
        int barTick = project.timeAxis.BarBeatToTickPos(bar, 0);
        SKMatrix originalMatrix = canvas.TotalMatrix;
        canvas.ResetMatrix();
        // Opt-C: ThemeColorsManager.Current.* をループ前にローカルキャッシュ（660+ プロパティチェーン呼び出し/MISS を排除）
        var barlinePaint = ThemeColorsManager.Current.PianoRollBarlinePaint;
        var barlineHeadPaint = ThemeColorsManager.Current.PianoRollBarlineHeadPaint;
        var beatlinePaint = ThemeColorsManager.Current.PianoRollBeatlinePaint;
        var beatlineHeadPaint = ThemeColorsManager.Current.PianoRollBeatlineHeadPaint;
        while (barTick <= rightTick)
        {
            float x = (float)Math.Round((double)barTick) + 0.5f;
            float y = 20 * (float)_viewModel.Density;
            canvas.DrawText((bar + 1).ToString(), (x + 10) * originalMatrix.ScaleX + originalMatrix.TransX, 30, _pianoRollBarFont, _pianoRollBarTextPaint);
            canvas.DrawLine(new SKPoint(x * originalMatrix.ScaleX + originalMatrix.TransX, y), new SKPoint(x * originalMatrix.ScaleX + originalMatrix.TransX, bottom + 0.5f), barlinePaint);
            canvas.DrawLine(new SKPoint(x * originalMatrix.ScaleX + originalMatrix.TransX, 0), new SKPoint(x * originalMatrix.ScaleX + originalMatrix.TransX, y), barlineHeadPaint);
            UTimeSignature timeSig = project.timeAxis.TimeSignatureAtBar(bar);
            int nextBarTick = project.timeAxis.BarBeatToTickPos(bar + 1, 0);
            int ticksPerBeat = project.resolution * 4 * timeSig.beatPerBar / timeSig.beatUnit;
            int ticksPerLine = snapUnit;
            if (ticksPerBeat < snapUnit)
                ticksPerLine = ticksPerBeat;
            else if (ticksPerBeat % snapUnit != 0)
            {
                if (ticksPerBeat > minLineTick)
                    ticksPerLine = ticksPerBeat;
                else
                    ticksPerLine = nextBarTick - barTick;
            }
            if (nextBarTick > leftTick)
            {
                for (int tick = barTick + ticksPerLine; tick < nextBarTick; tick += ticksPerLine)
                {
                    project.timeAxis.TickPosToBarBeat(tick, out int _, out int _, out int snapRemainingTicks);
                    x = (float)(tick + 0.5);
                    y = 20 * (float)_viewModel.Density;
                    if (snapRemainingTicks == 0)
                    {
                        canvas.DrawLine(new SKPoint(x * originalMatrix.ScaleX + originalMatrix.TransX, y), new SKPoint(x * originalMatrix.ScaleX + originalMatrix.TransX, bottom + 0.5f), beatlinePaint);
                        canvas.DrawLine(new SKPoint(x * originalMatrix.ScaleX + originalMatrix.TransX, 0), new SKPoint(x * originalMatrix.ScaleX + originalMatrix.TransX, y), beatlineHeadPaint);
                    }
                    else
                    {
                        canvas.DrawLine(new SKPoint(x * originalMatrix.ScaleX + originalMatrix.TransX, y), new SKPoint(x * originalMatrix.ScaleX + originalMatrix.TransX, bottom + 0.5f), beatlinePaint);
                    }
                }
            }
            barTick = nextBarTick;
            bar++;
        }
        canvas.SetMatrix(originalMatrix);
    }

    private void DrawPianoRollShadow(SKCanvas canvas, float screenWidth, float screenHeight, float panX, float zoomX)
    {
        var editingPart = _viewModel.EditingPart;
        float bottom = screenHeight + 0.5f;
        if (editingPart == null)
        {
            canvas.DrawRect(0, 0, screenWidth, bottom, _pianoRollShadowPaint);
            return;
        }
        double leftTick = -panX / zoomX;
        double rightTick = leftTick + screenWidth / zoomX;
        if (editingPart.position > rightTick || editingPart.End <= leftTick)
        {
            canvas.DrawRect(0, 0, screenWidth, bottom, _pianoRollShadowPaint);
        }
        else
        {
            if (editingPart.position > leftTick)
            {
                float partStartX = (float)editingPart.position * zoomX + panX;
                canvas.DrawRect(0, 0, partStartX, bottom, _pianoRollShadowPaint);
            }
            if (editingPart.End < rightTick)
            {
                float partEndX = (float)editingPart.End * zoomX + panX;
                canvas.DrawRect(partEndX, 0, screenWidth, bottom, _pianoRollShadowPaint);
            }
        }
    }

    private void PianoRollKeysBackgroundCanvas_PaintSurface(object sender, SkiaSharp.Views.Maui.SKPaintSurfaceEventArgs e)
    {
#if DEBUG
        var _sw = PaintSurfaceProfiler.Start();
#endif
        SKCanvas canvas = e.Surface.Canvas;
        canvas.Clear(ThemeColorsManager.Current.WhitePianoRollBackground);
        //canvas.SetMatrix(_viewModel.GetTransformMatrix());
        // Only draw backgrounds for black-key rows (performance)
        float heightPerPianoKey = (float)(_viewModel.HeightPerPianoKey * _viewModel.Density);
        float viewTop = -_viewModel.PianoRollTransformer.PanY / _viewModel.PianoRollTransformer.ZoomY;
        float viewBottom = viewTop + canvas.DeviceClipBounds.Size.Height / _viewModel.PianoRollTransformer.ZoomY;
        int topKeyNum = Math.Max(0, (int)Math.Floor(viewTop / _viewModel.HeightPerPianoKey / _viewModel.Density));
        int bottomKeyNum = Math.Min(ViewConstants.TotalPianoKeys, (int)Math.Ceiling(viewBottom / heightPerPianoKey));
        float y = (float)(topKeyNum * heightPerPianoKey) * _viewModel.PianoRollTransformer.ZoomY + _viewModel.PianoRollTransformer.PanY;
        for (int i = topKeyNum; i < bottomKeyNum; i++)
        {
            if (ViewConstants.PianoKeys[i].IsBlackKey)
            {
                canvas.DrawRect(0, y, canvas.DeviceClipBounds.Size.Width, heightPerPianoKey * _viewModel.PianoRollTransformer.ZoomY, _pianoRollBlackKeyBgPaint);
            }
            y += heightPerPianoKey * _viewModel.PianoRollTransformer.ZoomY;
        }
#if DEBUG
        PaintSurfaceProfiler.End(_sw, nameof(PianoRollKeysBackgroundCanvas));
#endif
    }

    /// <summary>
    /// Redraws the pitch curve canvas.
    /// </summary>
    private void PianoRollPitchCanvas_PaintSurface(object sender, SkiaSharp.Views.Maui.SKPaintSurfaceEventArgs e)
    {
#if DEBUG
        var _sw = PaintSurfaceProfiler.Start();
#endif
        try
        {
        SKCanvas canvas = e.Surface.Canvas;
        canvas.Clear();
        if (_viewModel.EditingPart == null || _viewModel.EditingPart is not UVoicePart)
        {
            return; // No part being edited — nothing to draw
        }
        if (_viewModel.CurrentNoteEditMode == NoteEditMode.EditNote)
        {
            return; // Note-edit mode — pitch overlay not shown
        }
        float pitchDisplayPrecision = Preferences.Default.PitchDisplayPrecision; // display precision

        int leftTick = (int)(-_viewModel.PianoRollTransformer.PanX / _viewModel.PianoRollTransformer.ZoomX);
        int rightTick = (int)(canvas.DeviceClipBounds.Size.Width / _viewModel.PianoRollTransformer.ZoomX + leftTick);

        const int interval = 5; // One sample point per 5 ticks
        foreach (RenderPhrase phrase in _viewModel.EditingPart.renderPhrases) // Iterate over all render phrases
        {
            if (phrase.position > rightTick || phrase.end < leftTick)
            {
                continue;
            }
            int pitchStartTick = phrase.position - phrase.leading;
            int startIdx = Math.Max(0, (leftTick - pitchStartTick) / interval);
            int endIdx = Math.Min(phrase.pitches.Length, (rightTick - pitchStartTick) / interval + 1);
            _pitchLinePath.Reset();
            // Compute step size
            int step = Math.Max(1, (int)(pitchDisplayPrecision / _viewModel.PianoRollTransformer.ZoomX));
            bool isFirstPoint = true;
            for (int i = startIdx; i < endIdx; i += step)
            {
                int t = pitchStartTick + i * interval;
                float p = phrase.pitches[i];
                SKPoint point = _viewModel.PianoRollTransformer.LogicalToActual(_viewModel.PitchAndTickToPoint(t, p));
                if (isFirstPoint)
                {
                    _pitchLinePath.MoveTo(point);
                    isFirstPoint = false;
                }
                else
                {
                    _pitchLinePath.LineTo(point);
                }
            }
            canvas.DrawPath(_pitchLinePath, _pitchLinePaint);
        }
        // Draw touch-point indicator
        if (IsUserDrawingCurve)
        {
            canvas.DrawCircle(TouchingPoint, 10f, _touchCursorPaint);
        }
        }
        finally
        {
#if DEBUG
            PaintSurfaceProfiler.End(_sw, nameof(PianoRollPitchCanvas));
#endif
        }
    }

    private int ComputeTempoTimeSignatureHash()
    {
        var project = DocManager.Inst.Project;
        if (project == null) return 0;
        var hash = new HashCode();
        foreach (var tempo in project.tempos)
        {
            hash.Add(tempo.position);
            hash.Add(tempo.bpm);
        }
        foreach (var ts in project.timeSignatures)
        {
            hash.Add(ts.barPosition);
            hash.Add(ts.beatPerBar);
            hash.Add(ts.beatUnit);
        }
        return hash.ToHashCode();
    }

    /// <summary>
    /// Redraws the phoneme canvas.
    /// </summary>
    private void PhonemeCanvas_PaintSurface(object sender, SkiaSharp.Views.Maui.SKPaintSurfaceEventArgs e)
    {
#if DEBUG
        var _sw = PaintSurfaceProfiler.Start();
#endif
        try
        {
        SKCanvas canvas = e.Surface.Canvas;
        // Clear canvas
        canvas.Clear();
        UVoicePart? part = _viewModel.EditingPart;
        if (part == null)
        {
            return;
        }
        UProject project = DocManager.Inst.Project;

        int leftTick = (int)(-_viewModel.PianoRollTransformer.PanX / _viewModel.PianoRollTransformer.ZoomX);
        int rightTick = (int)(canvas.DeviceClipBounds.Size.Width / _viewModel.PianoRollTransformer.ZoomX + leftTick);

        float y = 25f * (float)_viewModel.Density;
        float height = 20f * (float)_viewModel.Density;
        _phonemeOutlinePaint.Color = ViewConstants.TrackSkiaColors[DocManager.Inst.Project.tracks[part.trackNo].TrackColor];
        _textFont12.Size = 12 * (float)_viewModel.Density;
        _textFont12.Typeface = ObjectProvider.NotoSansCJKscRegularTypeface;
        // Iterate over phonemes
        foreach (var phoneme in part.phonemes)
        {
            double leftBound = project.timeAxis.MsPosToTickPos(phoneme.PositionMs - phoneme.preutter);
            double rightBound = phoneme.End + part.position;
            if (leftBound > rightTick || rightBound < leftTick || phoneme.Parent.OverlapError)
            {
                continue;
            }
            TimeAxis timeAxis = project.timeAxis;
            float x = (float)Math.Round(_viewModel.PianoRollTransformer.LogicalToActual(_viewModel.PitchAndTickToPoint(phoneme.position + part.position, 0)).X);
            double posMs = phoneme.PositionMs;
            if (!phoneme.Error)
            {
                // Pre-utterance (preutter) start point
                float x0 = _viewModel.PianoRollTransformer.LogicalToActual(_viewModel.PitchAndTickToPoint(timeAxis.MsPosToTickPos(posMs + phoneme.envelope.data[0].X), 0)).X;
                float y0 = (1 - phoneme.envelope.data[0].Y / 100) * height;
                // overlap
                float x1 = _viewModel.PianoRollTransformer.LogicalToActual(_viewModel.PitchAndTickToPoint(timeAxis.MsPosToTickPos(posMs + phoneme.envelope.data[1].X), 0)).X;
                float y1 = (1 - phoneme.envelope.data[1].Y / 100) * height;
                //float x2 = _viewModel.PianoRollTransformer.LogicalToActual(_viewModel.PitchAndTickToPoint(timeAxis.MsPosToTickPos(posMs + phoneme.envelope.data[2].X), 0)).X;
                //float y2 = (1 - phoneme.envelope.data[2].Y / 100) * height;
                float x3 = _viewModel.PianoRollTransformer.LogicalToActual(_viewModel.PitchAndTickToPoint(timeAxis.MsPosToTickPos(posMs + phoneme.envelope.data[3].X), 0)).X;
                float y3 = (1 - phoneme.envelope.data[3].Y / 100) * height;
                float x4 = _viewModel.PianoRollTransformer.LogicalToActual(_viewModel.PitchAndTickToPoint(timeAxis.MsPosToTickPos(posMs + phoneme.envelope.data[4].X), 0)).X;
                float y4 = (1 - phoneme.envelope.data[4].Y / 100) * height;

                //var pen = selectedNotes.Contains(phoneme.Parent) ? ThemeManager.AccentPen2 : ThemeManager.AccentPen1;
                //var brush = selectedNotes.Contains(phoneme.Parent) ? ThemeManager.AccentBrush2Semi : ThemeManager.AccentBrush1Semi;

                SKPoint point0 = new(x0, y + y0);
                SKPoint point1 = new(x1, y + y1);
                //SKPoint point2 = new(x2, y + y2);
                SKPoint point3 = new(x3, y + y3);
                SKPoint point4 = new(x4, y + y4);
                _phonemeEnvelopePath.Reset();
                _phonemeEnvelopePath.MoveTo(point0);
                _phonemeEnvelopePath.LineTo(point1);
                //_phonemeEnvelopePath.LineTo(point2);
                _phonemeEnvelopePath.LineTo(point3);
                _phonemeEnvelopePath.LineTo(point4);
                _phonemeEnvelopePath.Close();
                //Debug.WriteLine($"Phoneme {phoneme.phoneme} envelope points: \n0:{point0}, \n1:{point1}, \n2:{point2}, \n3:{point3}, \n4:{point4}");
                //var polyline = new PolylineGeometry(new Point[] { point0, point1, point2, point3, point4 }, true);
                // Phoneme polygon
                //Debug.WriteLine($"Phoneme {phoneme.phoneme} from {x0} to {x4}");
                canvas.DrawPath(_phonemeEnvelopePath, _phonemeOutlinePaint);
                //    // preutter control point
                //    brush = phoneme.preutterDelta.HasValue ? pen!.Brush : ThemeManager.BackgroundBrush;
                //    using (var state = context.PushTransform(Matrix.CreateTranslation(x0, y + y0 - 1)))
                //    {
                //        context.DrawGeometry(brush, pen, pointGeometry);
                //    }
                //    // overlap control point
                //    brush = phoneme.overlapDelta.HasValue ? pen!.Brush : ThemeManager.BackgroundBrush;
                //    using (var state = context.PushTransform(Matrix.CreateTranslation(point1)))
                //    {
                //        context.DrawGeometry(brush, pen, pointGeometry);
                //    }
                //}
                // Phoneme position vertical line
                canvas.DrawLine(new SKPoint(x, y), new SKPoint(x, y + height), _phonemePosLinePaint);
                // Phoneme label text
                string displayPhoneme = phoneme.phonemeMapped ?? phoneme.phoneme;
                canvas.DrawText(displayPhoneme, x + 2, 15 * (float)_viewModel.Density, _textFont12, ThemeColorsManager.Current.PhonemeTextPaint);
            }
        }
        }
        finally
        {
#if DEBUG
            PaintSurfaceProfiler.End(_sw, nameof(PhonemeCanvas));
#endif
        }
    }

    private void ExpressionCanvas_PaintSurface(object sender, SkiaSharp.Views.Maui.SKPaintSurfaceEventArgs e)
    {
#if DEBUG
        var _sw = PaintSurfaceProfiler.Start();
#endif
        try
        {
        SKCanvas canvas = e.Surface.Canvas;
        canvas.Clear();
        if (e.Surface.Canvas.DeviceClipBounds.Height < 5)
        {
            // Early exit for near-zero height
            return;
        }
        // Steps:
        // 1. Validate state
        // 2. Compute viewport
        // 3. Dispatch by expression type:
        //    - Curve (UExpressionType.Curve)
        //    - Numerical / Options (UExpressionType.Numerical/Options)

        // Validate state
        if (_viewModel.EditingPart == null)
        {
            return;
        }
        UProject project = DocManager.Inst.Project;
        UTrack track = project.tracks[_viewModel.EditingPart.trackNo];
        if (_viewModel.PrimaryExpressionAbbr == null)
        {
            return;
        }
        if (!track.TryGetExpDescriptor(project, _viewModel.PrimaryExpressionAbbr, out var descriptor)) // Retrieve expression descriptor by abbreviation (e.g. DYN)
        {
            return;
        }
        if (descriptor.max <= descriptor.min)
        {
            return;
        }
        // Compute viewport
        int leftTick = (int)(-_viewModel.PianoRollTransformer.PanX / _viewModel.PianoRollTransformer.ZoomX);
        int rightTick = (int)(canvas.DeviceClipBounds.Size.Width / _viewModel.PianoRollTransformer.ZoomX + leftTick);
        float descriptorRange = descriptor.max - descriptor.min;
        // Curve-type expression
        if (descriptor.type == UExpressionType.Curve)
        {
            UCurve? curve = _viewModel.EditingPart.curves.FirstOrDefault(c => c.descriptor == descriptor); // Select the curve for this expression
            float defaultHeight = (float)Math.Round(canvas.DeviceClipBounds.Height - canvas.DeviceClipBounds.Height * (descriptor.defaultValue - descriptor.min) / (descriptor.max - descriptor.min));
            // No curve found — draw a horizontal line at the default value
            if (curve == null)
            {
                //float x1 = (float)Math.Round(_viewModel.PianoRollTransformer.LogicalToActual(_viewModel.PitchAndTickToPoint(leftTick, 0)).X);
                //float x2 = (float)Math.Round(_viewModel.PianoRollTransformer.LogicalToActual(_viewModel.PitchAndTickToPoint(rightTick, 0)).X);
                //canvas.DrawLine(new SKPoint(x1, defaultHeight), new SKPoint(x2, defaultHeight), defaultPaint);
                canvas.DrawLine(0, defaultHeight, canvas.DeviceClipBounds.Width, defaultHeight, ThemeColorsManager.Current.EditedExpressionStrokePaint);
                return;
            }
            //int lTick = (int)Math.Floor((double)leftTick / 5) * 5;
            //int rTick = (int)Math.Ceiling((double)rightTick / 5) * 5;
            int lTick = leftTick;
            int rTick = rightTick;
            int index = curve.xs.BinarySearch(lTick - _viewModel.EditingPart.position);
            if (index < 0)
            {
                index = -index - 1;
            }
            index = Math.Max(0, index) - 1;
            // Draw segmented curve lines
            while (index < curve.xs.Count)
            {
                int tick1 = index < 0 ? lTick : curve.xs[index] + _viewModel.EditingPart.position;
                //tick1 += _viewModel.EditingPart.position;
                float value1 = index < 0 ? descriptor.defaultValue : curve.ys[index];
                float x1 = _viewModel.PianoRollTransformer.LogicalToActualX(tick1);
                float y1 = (descriptor.max - value1) * canvas.DeviceClipBounds.Height / descriptorRange;
                int tick2 = index == curve.xs.Count - 1 ? rTick : curve.xs[index + 1] + _viewModel.EditingPart.position;
                //tick2 += _viewModel.EditingPart.position;
                float value2 = index == curve.xs.Count - 1 ? descriptor.defaultValue : curve.ys[index + 1];
                float x2 = _viewModel.PianoRollTransformer.LogicalToActualX(tick2);
                float y2 = (descriptor.max - value2) * canvas.DeviceClipBounds.Height / descriptorRange;
                SKPaint paint = value1 == descriptor.defaultValue && value2 == descriptor.defaultValue ? ThemeColorsManager.Current.DefaultExpressionStrokePaint : ThemeColorsManager.Current.EditedExpressionStrokePaint; // Thick stroke for edited values, thin for default
                canvas.DrawLine(new SKPoint(x1, y1), new SKPoint(x2, y2), paint);
                //using (var state = canvas.PushTransform(Matrix.CreateTranslation(x1, y1))) {
                //    canvas.DrawGeometry(brush, null, pointGeometry);
                //}
                index++;
                if (tick2 >= rTick)
                {
                    break;
                }
            }
            // Draw touch-point indicator
            if (isDrawingExpression)
            {
                float radius = 10f;
                canvas.DrawCircle(drawingExpressionPointer, radius, ThemeColorsManager.Current.DrawingCursorPaint);
                _textFont12.Size = 12 * (float)_viewModel.Density;
                _textFont12.Typeface = ObjectProvider.NotoSansCJKscRegularTypeface;
                // Draw current value label
                canvas.DrawText($"{_viewModel.currentExpressionValue}", drawingExpressionPointer.X - 12f, drawingExpressionPointer.Y - 12f, _textFont12, ThemeColorsManager.Current.ExpressionOptionTextPaint);
            }
            return;
        }
        float innerRadius = 6 * (float)_viewModel.Density; // Inner circle radius
        float outerRadius = 12 * (float)_viewModel.Density; // Outer circle radius
        float optionHeight = 0;
        if (descriptor.type == UExpressionType.Options)
        {
            optionHeight = canvas.DeviceClipBounds.Height / descriptor.options.Length; // Height per option row
        }
        // Iterate over phonemes for both Options- and Numerical-type expressions
        foreach (UPhoneme phoneme in _viewModel.EditingPart.phonemes)
        {
            if (phoneme.Error || phoneme.Parent == null) // Skip phonemes with errors
            {
                continue;
            }
            double leftBound = phoneme.position + _viewModel.EditingPart.position; // Phoneme left boundary
            double rightBound = phoneme.End + _viewModel.EditingPart.position; // Phoneme right boundary
            if (leftBound >= rightTick || rightBound <= leftTick)
            {
                continue; // Phoneme out of view — skip
            }
            (float value, bool overriden) = phoneme.GetExpression(project, track, descriptor.abbr);
            float x1 = _viewModel.PianoRollTransformer.LogicalToActualX(phoneme.position + _viewModel.EditingPart.position);
            float x2 = _viewModel.PianoRollTransformer.LogicalToActualX(phoneme.End + _viewModel.EditingPart.position);
            // Numerical-type expression
            if (descriptor.type == UExpressionType.Numerical)
            {
                float valueHeight = (descriptor.max - value) * canvas.DeviceClipBounds.Height / descriptorRange;
                canvas.DrawLine(x1, canvas.DeviceClipBounds.Height, x1, valueHeight, ThemeColorsManager.Current.EditedExpressionStrokePaint);
                canvas.DrawLine(x1, valueHeight, x2, valueHeight, ThemeColorsManager.Current.EditedExpressionStrokePaint);
            }
            // Options-type expression
            else if (descriptor.type == UExpressionType.Options)
            {
                x1 += outerRadius;
                for (int i = 0; i < descriptor.options.Length; ++i) // For each option
                {
                    float y = optionHeight * (descriptor.options.Length - 1 - i + 0.5f);
                    if ((int)value == i) // Selected option
                    {
                        if (overriden) // Overridden (edited)
                        {
                            canvas.DrawCircle(x1, y, outerRadius, ThemeColorsManager.Current.EditedExpressionStrokePaint);
                            canvas.DrawCircle(x1, y, innerRadius, ThemeColorsManager.Current.EditedExpressionFillPaint);
                        }
                        else // Not overridden (default value)
                        {
                            canvas.DrawCircle(x1, y, outerRadius, ThemeColorsManager.Current.DefaultExpressionStrokePaint);
                            canvas.DrawCircle(x1, y, innerRadius, ThemeColorsManager.Current.DefaultExpressionFillPaint);
                        }
                    }
                    else // Unselected option
                    {
                        canvas.DrawCircle(x1, y, outerRadius, ThemeColorsManager.Current.DefaultExpressionStrokePaint);
                    }
                }
            }
        }
        // Options-type: background box and label text
        if (descriptor.type == UExpressionType.Options)
        {
            const int fontSize = 12;
            int padding = (int)(4 * _viewModel.Density);
            _textFont12.Size = fontSize * (float)_viewModel.Density;
            _textFont12.Typeface = ObjectProvider.NotoSansCJKscRegularTypeface;
            for (int i = 0; i < descriptor.options.Length; ++i)
            {
                string optionText = descriptor.options[i]; // Option label text
                if (string.IsNullOrEmpty(optionText))
                {
                    optionText = "\"\"";
                }
                float y = optionHeight * (descriptor.options.Length - 1 - i + 0.5f);
                const float x = 12f;
                float width = _textFont12.MeasureText(optionText) + padding + padding;
                float height = fontSize * (float)_viewModel.Density + padding + padding;
                canvas.DrawRect(x, y, width, height, ThemeColorsManager.Current.ExpressionOptionBoxPaint);
                canvas.DrawText(optionText, x + 4, y + 14 * (float)_viewModel.Density, _textFont12, ThemeColorsManager.Current.ExpressionOptionTextPaint);
            }
        }
        // Draw touch-point indicator
        if (isDrawingExpression)
        {
            float radius = 10f;
            canvas.DrawCircle(drawingExpressionPointer, radius, ThemeColorsManager.Current.DrawingCursorPaint);
            _textFont12.Size = 12 * (float)_viewModel.Density;
            _textFont12.Typeface = ObjectProvider.NotoSansCJKscRegularTypeface;
            // Draw current value label
            canvas.DrawText($"{_viewModel.currentExpressionValue}", drawingExpressionPointer.X - 12f, drawingExpressionPointer.Y - 12f, _textFont12, ThemeColorsManager.Current.ExpressionOptionTextPaint);
        }
        }
        finally
        {
#if DEBUG
            PaintSurfaceProfiler.End(_sw, nameof(ExpressionCanvas));
#endif
        }
    }

    /// <summary>
    /// EditVibrato モード時に、選択ノートのビブラート波形をピアノロール上にオーバーレイ描画する。
    /// _vibratoPaint / _vibratoPath は EditPage.xaml.cs のフィールドを使用（PaintSurface 内アロケーション禁止）。
    /// </summary>
    private void DrawVibratoOverlay(SKCanvas canvas, SKImageInfo info, UVoicePart voicePart)
    {
        if (_viewModel.SelectedNotes.Count == 0) return;

        var transformer = _viewModel.PianoRollTransformer;
        float zoomX = transformer.ZoomX;
        float zoomY = transformer.ZoomY;
        float panX = transformer.PanX;
        float panY = transformer.PanY;
        float heightPerKey = (float)_viewModel.HeightPerPianoKey * (float)_viewModel.Density;
        int partPositionX = voicePart.position;
        double tempo = DocManager.Inst.Project.tempos.Count > 0
            ? DocManager.Inst.Project.tempos[0].bpm : 120.0;
        float screenWidth = info.Width;

        foreach (var note in _viewModel.SelectedNotes)
        {
            var vib = note.vibrato;
            if (vib.length <= 0f) continue;

            // ビブラート区間（パート相対ティック）
            float startRatio = 1f - vib.length / 100f;
            int vibStartTick = note.position + (int)(note.duration * startRatio);
            int vibEndTick = note.position + note.duration;
            int totalVibTicks = vibEndTick - vibStartTick;
            if (totalVibTicks <= 0) continue;

            // 画面外スキップ
            float startScreenX = (partPositionX + vibStartTick) * zoomX + panX;
            float endScreenX = (partPositionX + vibEndTick) * zoomX + panX;
            if (endScreenX < 0 || startScreenX > screenWidth) continue;

            // ノート中央 Y
            float noteCenterY = (ViewConstants.TotalPianoKeys - note.tone - 0.5f) * heightPerKey * zoomY + panY;

            // 深さをピクセルに変換（100 cents = 1 semitone = 1 heightPerKey）
            float depthPx = vib.depth / 100f * heightPerKey * zoomY;

            // 周期をティックに変換
            double periodTicks = MusicMath.TempoMsToTick(tempo, vib.period);
            if (periodTicks < 1.0) continue;

            double shiftRad = vib.shift / 100.0 * 2.0 * Math.PI;

            // 描画範囲をクリップ
            float drawStartX = Math.Max(0f, startScreenX);
            float drawEndX = Math.Min(screenWidth, endScreenX);

            _vibratoPath.Reset();
            bool pathStarted = false;

            for (float px = drawStartX; px <= drawEndX; px += VibratoRenderStepPx)
            {
                // 画面 X → パート相対ティック
                float logicalTick = (px - panX) / zoomX - partPositionX;
                float vibTick = logicalTick - vibStartTick;
                if (vibTick < 0f) continue;

                // フェードイン・アウト エンベロープ
                float envelope = 1f;
                float inTicks = totalVibTicks * vib.@in / 100f;
                float outTicks = totalVibTicks * vib.@out / 100f;
                if (vibTick < inTicks && inTicks > 0f)
                    envelope = vibTick / inTicks;
                else if (vibTick > totalVibTicks - outTicks && outTicks > 0f)
                    envelope = (totalVibTicks - vibTick) / outTicks;

                // サイン波
                double phase = vibTick / periodTicks * 2.0 * Math.PI + shiftRad;
                float sinY = noteCenterY - (float)(Math.Sin(phase) * depthPx * envelope);

                if (!pathStarted)
                {
                    _vibratoPath.MoveTo(px, sinY);
                    pathStarted = true;
                }
                else
                {
                    _vibratoPath.LineTo(px, sinY);
                }
            }

            if (pathStarted)
                canvas.DrawPath(_vibratoPath, _vibratoPaint);
        }
    }
}
