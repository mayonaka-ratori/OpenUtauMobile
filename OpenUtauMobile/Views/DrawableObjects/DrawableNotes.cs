using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using OpenUtauMobile.Utils;
using OpenUtauMobile.ViewModels;
using OpenUtauMobile.Views.Utils;
using SkiaSharp;
using System;
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

            DrawRectangle();
            DrawLyrics();
        }

        public void DrawRectangle()
        {
            _notesFillPaint.Color = NotesColor;

            foreach (UNote note in Part.notes)
            {
                // 计算音符的绝对位置
                int noteStart = (int)(PositionX + note.position);
                int noteEnd = noteStart + note.duration;

                // 跳过左侧不可见的音符
                if (noteEnd < LeftTick)
                {
                    continue;
                }

                // 跳过右侧不可见的音符 notes按position排序
                if (noteStart > RightTick)
                {
                    break;
                }

                // 绘制边框
                if (ViewModel.SelectedNotes.Contains(note))
                {
                    Canvas.DrawRect((PositionX + note.position) * ViewModel.PianoRollTransformer.ZoomX + ViewModel.PianoRollTransformer.PanX, (ViewConstants.TotalPianoKeys - note.tone - 1) * HeightPerPianoKey * ViewModel.PianoRollTransformer.ZoomY + ViewModel.PianoRollTransformer.PanY, note.duration * ViewModel.PianoRollTransformer.ZoomX, HeightPerPianoKey * ViewModel.PianoRollTransformer.ZoomY, _selectedNotePaint);
                }
                // 如果是错误音符，颜色增加透明度
                _notesFillPaint.Color = note.Error ? NotesColor.WithAlpha(100) : NotesColor;
                // 绘制实心矩形
                Canvas.DrawRect((PositionX + note.position) * ViewModel.PianoRollTransformer.ZoomX + ViewModel.PianoRollTransformer.PanX, (ViewConstants.TotalPianoKeys - note.tone - 1) * HeightPerPianoKey * ViewModel.PianoRollTransformer.ZoomY + ViewModel.PianoRollTransformer.PanY, note.duration * ViewModel.PianoRollTransformer.ZoomX, HeightPerPianoKey * ViewModel.PianoRollTransformer.ZoomY, _notesFillPaint);
            }
            // 绘制可拖拽手柄
            if (ViewModel.SelectedNotes.Count > 0 && ViewModel.CurrentNoteEditMode == EditViewModel.NoteEditMode.EditNote)
            {
                foreach (UNote note in ViewModel.SelectedNotes)
                {
                    float left = (PositionX + note.position + note.duration) * ViewModel.PianoRollTransformer.ZoomX + ViewModel.PianoRollTransformer.PanX + Spacing;
                    float right = left + HandleSize;
                    float top = (ViewConstants.TotalPianoKeys - note.tone - 0.5f) * HeightPerPianoKey * ViewModel.PianoRollTransformer.ZoomY - HalfHandleSize + ViewModel.PianoRollTransformer.PanY;
                    float centerY = top + HalfHandleSize;
                    float centerX = left + HalfHandleSize;
                    float bottom = top + HandleSize;
                    // 右侧手柄
                    Canvas.DrawRoundRect(left,
                        top,
                        HandleSize,
                        HandleSize,
                        4,
                        4,
                        _handlePaint);
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

        public void DrawLyrics()
        {
            _lyricPaint.Color = ThemeColorsManager.Current.LyricsText;
            _lyricsFont.Size = 15 * (float)ViewModel.Density;
            _lyricsFont.Typeface = ObjectProvider.NotoSansCJKscRegularTypeface;
            // 绘制歌词文本
            foreach (UNote note in Part.notes)
            {
                // 计算音符的绝对位置
                int noteStart = (int)(PositionX + note.position);
                int noteEnd = noteStart + note.duration;

                // 跳过左侧不可见的音符
                if (noteEnd < LeftTick)
                {
                    continue;
                }

                // 跳过右侧不可见的音符 notes按position排序
                if (noteStart > RightTick)
                {
                    break;
                }
                // 计算文本位置
                float x = noteStart * ViewModel.PianoRollTransformer.ZoomX + ViewModel.PianoRollTransformer.PanX;
                float y = (ViewConstants.TotalPianoKeys - note.tone - 1.5f) * HeightPerPianoKey * ViewModel.PianoRollTransformer.ZoomY + ViewModel.PianoRollTransformer.PanY;
                // 绘制歌词
                if (!string.IsNullOrEmpty(note.lyric))
                {
                    Canvas.DrawText(note.lyric, x, y, _lyricsFont, _lyricPaint);
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
            if (ViewModel.SelectedNotes.Count == 0 || ViewModel.CurrentNoteEditMode != EditViewModel.NoteEditMode.EditNote)
            {
                return null;
            }
            foreach (UNote note in ViewModel.SelectedNotes)
            {
                float left = PositionX + note.position + note.duration + Spacing / ViewModel.PianoRollTransformer.ZoomX;
                float right = left + HandleSize / ViewModel.PianoRollTransformer.ZoomX;
                float top = (ViewConstants.TotalPianoKeys - note.tone - 0.5f) * HeightPerPianoKey - HalfHandleSize * ViewModel.PianoRollTransformer.ZoomY;
                float bottom = top + HandleSize * ViewModel.PianoRollTransformer.ZoomY;
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
