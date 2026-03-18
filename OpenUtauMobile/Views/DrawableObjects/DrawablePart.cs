using NWaves.Signals;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using OpenUtauMobile.Utils;
using OpenUtauMobile.ViewModels;
using OpenUtauMobile.Resources.Strings;
using SkiaSharp;
using Color = Microsoft.Maui.Graphics.Color;

namespace OpenUtauMobile.Views.DrawableObjects
{
    /// <summary>
    /// 可绘制分片类型
    /// </summary>
    public class DrawablePart : IDisposable
    {
        public SKCanvas Canvas { get; set; } = null!;
        public EditViewModel ViewModel { get; set; } = null!;
        public float HeightPerTrack => (float)ViewModel.HeightPerTrack * (float)ViewModel.Density;
        public bool IsSelected { get; set; } = false; // 是否被选中
        public bool IsResizable { get; set; } = true; // 是否可调整长度
        private float RightHandleX { get; set; } // 逻辑坐标
        private float RightHandleY { get; set; } // 逻辑坐标
        private float R { get; set; } // 手柄半径，逻辑坐标
        /// <summary>
        /// 关联UPart对象
        /// </summary>
        public UPart Part { get; set; } = null!;

        // Part fill: Color set per-call from track color + alpha
        private readonly SKPaint _partFillPaint = new() { Style = SKPaintStyle.Fill };
        // Part border for selected state: theme color, stroke
        private readonly SKPaint _partBorderPaint = new()
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 3,
            Color = ThemeColorsManager.Current.SelectedPartBorder
        };
        // Title and info text paint: theme color
        private readonly SKPaint _titlePaint = new()
        {
            Color = ThemeColorsManager.Current.PartLabel
        };
        // Title font — Size 30f, Typeface set in Draw
        private readonly SKFont _titleFont = new() { Size = 30f };
        // Wave load info paint: fixed black (shared across all instances)
        private static readonly SKPaint _waveLoadInfoPaint = new() { Color = SKColors.Black };
        // Track mini-note paint: Color and StrokeWidth set per-call
        private readonly SKPaint _trackNotesPaint = new() { Style = SKPaintStyle.Stroke };
        // Handle: fixed yellow fill (shared across all instances)
        private static readonly SKPaint _handlePaint = new()
        {
            Color = SKColors.Yellow,
            Style = SKPaintStyle.Fill
        };
        // Waveform paint: theme color, StrokeWidth set per-call
        private readonly SKPaint _waveformPaint = new() { Style = SKPaintStyle.Stroke };
        private bool _disposed = false;

        /// <summary>
        /// 创建可绘制分片（ViewModel-only constructor; call Update() before Draw()）
        /// </summary>
        public DrawablePart(EditViewModel viewModel)
        {
            ViewModel = viewModel;
        }

        /// <summary>
        /// 更新每帧可变状态。PaintSurface 内で Draw() の前に呼ぶこと。
        /// </summary>
        public void Update(SKCanvas canvas, UPart part, bool isSelected = false, bool isResizable = false)
        {
            Canvas = canvas;
            Part = part;
            IsSelected = isSelected;
            IsResizable = isResizable;
            if (isResizable)
            {
                RightHandleX = (float)(Part.position + Part.Duration);
                RightHandleY = (float)(Part.trackNo + 0.5f) * HeightPerTrack;
                R = 10f; // 手柄半径
            }
        }

        /// <summary>
        /// 判断一个点是否在分片内
        /// </summary>
        /// <param name="point">逻辑坐标</param>
        /// <returns></returns>
        public bool IsPointInside(SKPoint point)
        {
            double left = Part.position;
            double right = Part.position + Part.Duration;
            double top = Part.trackNo * HeightPerTrack;
            double bottom = top + HeightPerTrack;
            return point.X >= left && point.X <= right && point.Y >= top && point.Y <= bottom;
        }

        public bool IsPointInHandle(SKPoint point)
        {
            // 将手柄中心点转换为实际坐标
            float handleActualX = RightHandleX * ViewModel.TrackTransformer.ZoomX + ViewModel.TrackTransformer.PanX;
            float handleActualY = RightHandleY * ViewModel.TrackTransformer.ZoomY + ViewModel.TrackTransformer.PanY;

            // 将检测点从逻辑坐标转换为实际坐标
            float pointActualX = point.X * ViewModel.TrackTransformer.ZoomX + ViewModel.TrackTransformer.PanX;
            float pointActualY = point.Y * ViewModel.TrackTransformer.ZoomY + ViewModel.TrackTransformer.PanY;

            // 在实际坐标系中计算距离
            float distance = (float)Math.Sqrt(
                Math.Pow(pointActualX - handleActualX, 2) +
                Math.Pow(pointActualY - handleActualY, 2)
            );

            // 与手柄实际半径比较（乘以设备密度因子）
            float actualRadius = R * (float)DeviceDisplay.Current.MainDisplayInfo.Density;

            return distance <= actualRadius;
        }

        /// <summary>
        /// 绘制逻辑封装
        /// </summary>
        public void Draw()
        {
            _titleFont.Typeface = ObjectProvider.NotoSansCJKscRegularTypeface;
            DrawRectangle();
            if (IsResizable)
            {
                DrawHandle();
            }
            DrawTitle();
            if (Part is UVoicePart voicePart && voicePart.notes.Count > 0)
            {
                DrawNotes(voicePart);
            }
            else if (Part is UWavePart wavePart)
            {
                // 增强波形状态显示逻辑
                if (wavePart.Peaks == null)
                {
                    DrawWaveLoadingInfo(AppResources.ProcessingWaveform);
                    return;
                }

                switch (wavePart.Peaks.Status)
                {
                    case TaskStatus.Canceled:
                        DrawWaveLoadingInfo(AppResources.ProcessingWaveformCancelled);
                        break;
                    case TaskStatus.Faulted:
                        DrawWaveLoadingInfo(string.Format(AppResources.ProcessingWaveformError, wavePart.Peaks.Exception?.Message ?? AppResources.UnknownError));
                        break;
                    case TaskStatus.Running:
                        DrawWaveLoadingInfo(AppResources.RenderingWaveformFile);
                        break;
                    case TaskStatus.WaitingForActivation:
                        break;
                    case TaskStatus.WaitingToRun:
                        DrawWaveLoadingInfo(AppResources.RenderingWaveformFile);
                        break;
                    case TaskStatus.Created:
                        DrawWaveLoadingInfo(AppResources.ProcessingWaveformNotStarted);
                        break;
                    case TaskStatus.RanToCompletion when wavePart.Peaks.Result == null:
                        DrawWaveLoadingInfo(AppResources.ProcessingWaveformEmpty);
                        break;
                    case TaskStatus.RanToCompletion:
                        try
                        {
                            DrawWaveform(wavePart);
                        }
                        catch (Exception ex)
                        {
                            DrawWaveLoadingInfo(string.Format(AppResources.ProcessingWaveformError, ex.Message));
                        }
                        break;
                    default:
                        DrawWaveLoadingInfo(string.Format(AppResources.UnknownStatus, wavePart.Peaks.Status));
                        break;
                }
            }
        }

        private void DrawWaveLoadingInfo(string info)
        {
            // 保存当前的变换矩阵
            SKMatrix originalMatrix = Canvas.TotalMatrix;
            // 恢复到默认矩阵，使文字不受缩放影响
            Canvas.ResetMatrix();

            // 计算位置
            float x = (float)(Part.position * originalMatrix.ScaleX + originalMatrix.TransX + 5);
            float y = (float)(Part.trackNo * HeightPerTrack * originalMatrix.ScaleY + originalMatrix.TransY + 60);
            // 绘制信息文本
            Canvas.DrawText(info, x, y, SKTextAlign.Left, _titleFont, _waveLoadInfoPaint);

            // 恢复原始变换矩阵
            Canvas.SetMatrix(originalMatrix);
        }

        /// <summary>
        /// 画出轮廓矩形
        /// </summary>
        public void DrawRectangle()
        {
            // 保存当前的变换矩阵
            SKMatrix originalMatrix = Canvas.TotalMatrix;
            // 恢复到默认矩阵，使矩形大小不受缩放影响
            Canvas.ResetMatrix();
            // 计算位置
            float x = (Part.position + 1) * (float)originalMatrix.ScaleX + (float)originalMatrix.TransX;
            float y = (float)(Part.trackNo * HeightPerTrack + 1) * (float)originalMatrix.ScaleY + (float)originalMatrix.TransY;
            float width = (Part.Duration - 2) * (float)originalMatrix.ScaleX;
            float height = (float)(HeightPerTrack - 2) * (float)originalMatrix.ScaleY;
            string color = DocManager.Inst.Project.tracks[Part.trackNo].TrackColor;
            _partFillPaint.Color = ViewConstants.TrackSkiaColors[color].WithAlpha(150);
            // 绘制矩形
            Canvas.DrawRect(x, y, width, height, _partFillPaint);
            // 如果被选中，绘制边框
            if (IsSelected)
            {
                Canvas.DrawRect(x, y, width, height, _partBorderPaint);
            }
            // 恢复原始变换矩阵
            Canvas.SetMatrix(originalMatrix);
        }

        /// <summary>
        /// 绘制标题
        /// </summary>
        public void DrawTitle()
        {
            // 保存当前的变换矩阵
            SKMatrix originalMatrix = Canvas.TotalMatrix;
            // 恢复到默认矩阵，使文字不受缩放影响
            Canvas.ResetMatrix();

            // 计算位置
            float x = (float)(Part.position * originalMatrix.ScaleX + originalMatrix.TransX + 5);
            float y = (float)(Part.trackNo * HeightPerTrack * originalMatrix.ScaleY + originalMatrix.TransY + 30);
            // 绘制标题
            Canvas.DrawText(Part.DisplayName, x, y, SKTextAlign.Left, _titleFont, _titlePaint);

            // 恢复原始变换矩阵
            Canvas.SetMatrix(originalMatrix);
        }

        /// <summary>
        /// 绘制音符
        /// </summary>
        public void DrawNotes(UVoicePart voicePart)
        {
            // Notes
            int maxTone = voicePart.notes.Max(note => note.tone);
            int minTone = voicePart.notes.Min(note => note.tone);
            int leftTick = (int)(-ViewModel.TrackTransformer.PanX / ViewModel.TrackTransformer.ZoomX);
            int rightTick = (int)(Canvas.DeviceClipBounds.Size.Width / ViewModel.TrackTransformer.ZoomX + leftTick);

            if (maxTone - minTone < 10) // 如果音域较窄，则扩展到10
            {
                int additional = (10 - (maxTone - minTone)) / 2;
                minTone -= additional;
                maxTone += additional;
            }
            Application? app = Application.Current;
            if (app?.Resources != null && app.Resources.TryGetValue("TrackNote", out var accentColor) && accentColor is Color color)
            {
                _trackNotesPaint.Color = SKColor.Parse(color.ToHex());
            }
            else
            {
                _trackNotesPaint.Color = SKColors.Magenta;
            }
            _trackNotesPaint.StrokeWidth = HeightPerTrack / (maxTone - minTone + 10);
            foreach (var note in voicePart.notes)
            {
                if (note.End + Part.position < leftTick)
                {
                    continue;
                }
                if (note.position + Part.position > rightTick)
                {
                    break;
                }
                float y = (Part.trackNo + 1 - (float)(note.tone - (minTone - 5)) / (maxTone - minTone + 10)) * HeightPerTrack;
                SKPoint start = new(note.position + Part.position, y);
                SKPoint end = new(note.End + Part.position, y);
                Canvas.DrawLine(start, end, _trackNotesPaint);
            }
        }

        /// <summary>
        /// 绘制音频波形
        /// </summary>
        /// <param name="wavePart">包含峰值数据的音频片段</param>
        private void DrawWaveform(UWavePart wavePart)
        {
            if (wavePart.Peaks == null ||
                !wavePart.Peaks.IsCompletedSuccessfully ||
                wavePart.Peaks.Result == null)
            {
                return;
            }
            // 保存当前的变换矩阵
            SKMatrix originalMatrix = Canvas.TotalMatrix;
            Canvas.ResetMatrix(); // 恢复到默认矩阵，使波形不受缩放影响
            float height = (float)ViewModel.HeightPerTrack * (float)ViewModel.Density; // 总高度
            float monoChnlAmp = height / 2; // 如果是单声道的振幅高度（画布）
            float stereoChnlAmp = height / 4; // 如果是双声道的振幅高度（画布）
            float tickOffset = (float)(-ViewModel.TrackTransformer.PanX / ViewModel.TrackTransformer.ZoomX); // 当前视图的Tick偏移位置
            float tickWidth = (float)ViewModel.TrackTransformer.ZoomX; // 每个Tick对应的像素宽度

            TimeAxis timeAxis = DocManager.Inst.Project.timeAxis;
            DiscreteSignal[] peaks = wavePart.Peaks.Result;
            int x = 0; // 正在绘制的像素位置
            if (tickOffset <= wavePart.position)
            {
                // Part starts in or to the right of view.
                x = (int)(tickWidth * (wavePart.position - tickOffset));
            }
            if (x >= Canvas.DeviceClipBounds.Width)
            {
                return;
            }
            int posTick = (int)(tickOffset + x / tickWidth);
            double posMs = timeAxis.TickPosToMsPos(posTick);
            double offsetMs = timeAxis.TickPosToMsPos(wavePart.position);
            int sampleIndex = (int)(wavePart.peaksSampleRate * (posMs - offsetMs) * 0.001);
            sampleIndex = Math.Clamp(sampleIndex, 0, peaks[0].Length);
            _waveformPaint.Color = ThemeColorsManager.Current.TrackNote.WithAlpha(200);
            _waveformPaint.StrokeWidth = Math.Max(1, tickWidth);
            while (x < Canvas.DeviceClipBounds.Width)
            {
                if (posTick >= wavePart.position + wavePart.Duration)
                {
                    break;
                }
                int nextPosTick = (int)(tickOffset + (x + 1) / tickWidth);
                double nexPosMs = timeAxis.TickPosToMsPos(nextPosTick);
                int nextSampleIndex = (int)(wavePart.peaksSampleRate * (nexPosMs - offsetMs) * 0.001);
                nextSampleIndex = Math.Clamp(nextSampleIndex, 0, peaks[0].Length);
                if (nextSampleIndex > sampleIndex)
                {
                    for (int i = 0; i < peaks.Length; ++i)
                    {
                        ArraySegment<float> segment = new ArraySegment<float>(peaks[i].Samples, sampleIndex, nextSampleIndex - sampleIndex);
                        float min = segment.Min();
                        float max = segment.Max();
                        float ySpan = peaks.Length == 1 ? monoChnlAmp : stereoChnlAmp;
                        float yOffset = i == 1 ? monoChnlAmp : 0;
                        Canvas.DrawLine(x,
                            (float)(ySpan * (1 + -min) + yOffset + Part.trackNo * height) * originalMatrix.ScaleY + originalMatrix.TransY,
                            x,
                            (float)(ySpan * (1 + -max) + yOffset + Part.trackNo * height) * originalMatrix.ScaleY + originalMatrix.TransY,
                            _waveformPaint);
                    }
                }
                x++;
                posTick = nextPosTick;
                posMs = nexPosMs;
                sampleIndex = nextSampleIndex;
            }
            // 恢复原始变换矩阵
            Canvas.SetMatrix(originalMatrix);
        }

        /// <summary>
        /// 绘制右侧长度调整手柄
        /// </summary>
        private void DrawHandle()
        {
            // 保存当前的变换矩阵
            SKMatrix originalMatrix = Canvas.TotalMatrix;
            // 恢复到默认矩阵，使手柄大小不受缩放影响
            Canvas.ResetMatrix();
            float x = RightHandleX * originalMatrix.ScaleX + originalMatrix.TransX;
            float y = RightHandleY * originalMatrix.ScaleY + originalMatrix.TransY;
            float r = R * (float)DeviceDisplay.Current.MainDisplayInfo.Density;
            Canvas.DrawCircle(x, y, r, _handlePaint);
            // 恢复原始变换矩阵
            Canvas.SetMatrix(originalMatrix);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _partFillPaint.Dispose();
            _partBorderPaint.Dispose();
            _titlePaint.Dispose();
            _titleFont.Dispose();
            _trackNotesPaint.Dispose();
            _waveformPaint.Dispose();
            // _waveLoadInfoPaint, _handlePaint are static — not disposed per-instance
            GC.SuppressFinalize(this);
        }
    }
}
