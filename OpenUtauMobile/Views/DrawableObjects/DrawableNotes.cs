using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using OpenUtauMobile.Utils;
using OpenUtauMobile.ViewModels;
using OpenUtauMobile.Views.Utils;
using SkiaSharp;
using System;
using System.Collections.Generic;
namespace OpenUtauMobile.Views.DrawableObjects
{
    public class DrawableNotes : IDisposable
    {
        public SKCanvas Canvas { get; set; } = null!;
        public UVoicePart Part { get; set; } = null!;
        public float HeightPerPianoKey { get; set; }
        public SKColor NotesColor { get; set; }
        /// <summary>
        /// 实际坐标而非逻辑坐标
        /// </summary>
        private static float Spacing => 15f;
        /// <summary>
        /// 实际坐标而非逻辑坐标
        /// </summary>
        private const int DefaultTouchTargetSize = 16;
        public float HalfHandleSize => (float)(DefaultTouchTargetSize * ViewModel.Density);
        private float HandleSize => HalfHandleSize * 2;
        /// <summary>
        /// 当前分片的起始位置tick
        /// </summary>
        public float PositionX { get; set; }
        public EditViewModel ViewModel { get; set; } = null!;
        // 计算可视区域的左右边界（逻辑坐标）
        private int LeftTick { get; set; }
        private int RightTick { get; set; }

        // Note fill paint: Color updated per-frame from NotesColor
        private readonly SKPaint _notesFillPaint = new() { Style = SKPaintStyle.Fill };
        // Selected note border: theme color, stroke
        private readonly SKPaint _selectedNotePaint = new()
        {
            Color = ThemeColorsManager.Current.SelectedNoteBorder,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 5
        };
        // Drag handle: fixed yellow fill (shared across all instances)
        private static readonly SKPaint _handlePaint = new()
        {
            Color = SKColors.Yellow,
            Style = SKPaintStyle.Fill
        };
        // Triangle arrows in handle: fixed white fill (shared across all instances)
        private static readonly SKPaint _trianglePaint = new()
        {
            Color = SKColors.White,
            Style = SKPaintStyle.Fill
        };
        // Reusable SKPath for triangle drawing (avoids per-note allocation)
        private readonly SKPath _trianglePath = new();
        // Lyric text: theme color
        private readonly SKPaint _lyricPaint = new()
        {
            Color = ThemeColorsManager.Current.LyricsText,
            IsAntialias = true
        };
        // Lyric font: Size and Typeface set before each draw loop
        private readonly SKFont _lyricsFont = new();
        // BN-1: HashSet for O(1) selected-note lookup (avoids O(n²) from List.Contains per note)
        private readonly HashSet<UNote> _selectedNotesSet = new HashSet<UNote>();
        private bool _disposed = false;

        public DrawableNotes(SKCanvas canvas, UVoicePart part, EditViewModel viewModel, SKColor notesColor)
        {
            Part = part;
            Canvas = canvas;
            ViewModel = viewModel;
            NotesColor = notesColor;
        }

        public void Draw()
        {
            // Refresh per-frame derived state (must recalculate on each call when instance is cached)
            HeightPerPianoKey = (float)ViewModel.HeightPerPianoKey * (float)ViewModel.Density;
            PositionX = Part.position;
            LeftTick = (int)(-ViewModel.PianoRollTransformer.PanX / ViewModel.PianoRollTransformer.ZoomX);
            RightTick = (int)(Canvas.DeviceClipBounds.Width / ViewModel.PianoRollTransformer.ZoomX + LeftTick);

            // BN-1: Rebuild HashSet from SelectedNotes for O(1) lookup inside draw loop
            _selectedNotesSet.Clear();
            if (ViewModel.SelectedNotes.Count > 0)
            {
                foreach (var n in ViewModel.SelectedNotes)
                    _selectedNotesSet.Add(n);
            }

            DrawNotesAndLyrics();
        }

        /// <summary>
        /// BN-2: Single-pass draw — merges DrawRectangle() + DrawLyrics() into one loop.
        /// BN-3: Transformer properties cached as locals to avoid repeated property access per note.
        /// Drawing order: selection border → fill rect → lyric text (per note), handles on top.
        /// </summary>
        public void DrawNotesAndLyrics()
        {
            // BN-3: Cache Transformer properties as locals (6 property accesses → 1 per value)
            float zoomX = ViewModel.PianoRollTransformer.ZoomX;
            float zoomY = ViewModel.PianoRollTransformer.ZoomY;
            float panX = ViewModel.PianoRollTransformer.PanX;
            float panY = ViewModel.PianoRollTransformer.PanY;
            float density = (float)ViewModel.Density;

            // Paint / font setup — once before loop (was per-frame in DrawLyrics header)
            _notesFillPaint.Color = NotesColor;
            _lyricPaint.Color = ThemeColorsManager.Current.LyricsText;
            _lyricsFont.Size = 15 * density;
            // Font optimization: only assign Typeface when it actually changed
            var typeface = ObjectProvider.NotoSansCJKscRegularTypeface;
            if (_lyricsFont.Typeface != typeface)
                _lyricsFont.Typeface = typeface;

            // Single pass over Part.notes (SortedSet ordered by position)
            foreach (UNote note in Part.notes)
            {
                // 计算音符的绝对位置
                int noteStart = (int)(PositionX + note.position);
                int noteEnd = noteStart + note.duration;

                // 跳过左侧不可见的音符
                if (noteEnd < LeftTick)
                    continue;

                // 跳过右侧不可见的音符 notes按position排序
                if (noteStart > RightTick)
                    break;

                float x = (PositionX + note.position) * zoomX + panX;
                float y = (ViewConstants.TotalPianoKeys - note.tone - 1) * HeightPerPianoKey * zoomY + panY;
                float w = note.duration * zoomX;
                float h = HeightPerPianoKey * zoomY;

                // a. 选择边框 (BN-1: _selectedNotesSet.Contains — O(1))
                if (_selectedNotesSet.Contains(note))
                {
                    Canvas.DrawRect(x, y, w, h, _selectedNotePaint);
                }

                // b. 填充矩形
                _notesFillPaint.Color = note.Error ? NotesColor.WithAlpha(100) : NotesColor;
                Canvas.DrawRect(x, y, w, h, _notesFillPaint);

                // c. 歌词文本
                if (!string.IsNullOrEmpty(note.lyric))
                {
                    float lx = noteStart * zoomX + panX;
                    float ly = (ViewConstants.TotalPianoKeys - note.tone - 1.5f) * HeightPerPianoKey * zoomY + panY;
                    Canvas.DrawText(note.lyric, lx, ly, _lyricsFont, _lyricPaint);
                }
            }

            // 绘制可拖拽手柄 — drawn on top after all notes
            if (ViewModel.SelectedNotes.Count > 0 && ViewModel.CurrentNoteEditMode == NoteEditMode.EditNote)
            {
                float halfHandleSize = (float)(DefaultTouchTargetSize * density);
                float handleSize = halfHandleSize * 2;
                foreach (UNote note in ViewModel.SelectedNotes)
                {
                    float left = (PositionX + note.position + note.duration) * zoomX + panX + Spacing;
                    float right = left + handleSize;
                    float top = (ViewConstants.TotalPianoKeys - note.tone - 0.5f) * HeightPerPianoKey * zoomY - halfHandleSize + panY;
                    float centerY = top + halfHandleSize;
                    float centerX = left + halfHandleSize;
                    float bottom = top + handleSize;
                    // 右侧手柄
                    Canvas.DrawRoundRect(left, top, handleSize, handleSize, 4, 4, _handlePaint);
                    // 里面画两个小三角形，表示可拖拽
                    _trianglePath.Reset();
                    _trianglePath.MoveTo(left + 4, centerY);
                    _trianglePath.LineTo(centerX - 2, bottom - 6);
                    _trianglePath.LineTo(centerX - 2, top + 6);
                    _trianglePath.Close();
                    Canvas.DrawPath(_trianglePath, _trianglePaint);
                    _trianglePath.Reset();
                    _trianglePath.MoveTo(right - 4, centerY);
                    _trianglePath.LineTo(centerX + 2, bottom - 6);
                    _trianglePath.LineTo(centerX + 2, top + 6);
                    _trianglePath.Close();
                    Canvas.DrawPath(_trianglePath, _trianglePaint);
                }
            }
        }

        public UNote? IsPointInNote(SKPoint point)
        {
            float left;
            float right;
            float top;
            float bottom;
            foreach (UNote note in Part.notes)
            {
                left = PositionX + note.position;
                right = left + note.duration;
                top = (ViewConstants.TotalPianoKeys - note.tone - 1) * HeightPerPianoKey;
                bottom = top + HeightPerPianoKey;
                if (point.X >= left && point.X <= right && point.Y >= top && point.Y <= bottom)
                {
                    return note;
                }
            }
            return null;
        }

        /// <summary>
        /// 判断点是否在可拖拽手柄上
        /// </summary>
        /// <param name="point">逻辑坐标</param>
        /// <returns></returns>
        public UNote? IsPointInHandle(SKPoint point)
        {
            if (ViewModel.SelectedNotes.Count == 0 || ViewModel.CurrentNoteEditMode != NoteEditMode.EditNote)
            {
                return null;
            }
            foreach (UNote note in ViewModel.SelectedNotes)
            {
                float left = PositionX + note.position + note.duration + Spacing / ViewModel.PianoRollTransformer.ZoomX;
                float right = left + HandleSize / ViewModel.PianoRollTransformer.ZoomX;
                float top = (ViewConstants.TotalPianoKeys - note.tone - 0.5f) * HeightPerPianoKey - HalfHandleSize / ViewModel.PianoRollTransformer.ZoomY;
                float bottom = top + HandleSize / ViewModel.PianoRollTransformer.ZoomY;
                if (point.X >= left && point.X <= right && point.Y >= top && point.Y <= bottom)
                {
                    return note;
                }
            }
            return null;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _notesFillPaint.Dispose();
            _selectedNotePaint.Dispose();
            _lyricPaint.Dispose();
            _lyricsFont.Dispose();
            _trianglePath.Dispose();
            // _handlePaint, _trianglePaint are static — not disposed per-instance
            GC.SuppressFinalize(this);
        }
    }
}
