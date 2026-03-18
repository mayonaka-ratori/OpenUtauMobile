using DynamicData.Binding;
using Melanchall.DryWetMidi.Interaction;
using Microsoft.Maui.Graphics;
using OpenUtau.Core.Ustx;
using OpenUtauMobile.Utils;
using OpenUtauMobile.ViewModels;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtauMobile.Views.DrawableObjects
{
    class DrawableTickBackground : IDisposable
    {
        public SKCanvas Canvas { get; set; } = null!;
        public ObservableCollectionExtended<int>? SnapTicks { get; set; } = [];
        public EditViewModel ViewModel { get; set; } = null!;

        // Bar line paint (theme color)
        private readonly SKPaint _barLinePaint = new()
        {
            Color = ThemeColorsManager.Current.TimeLine,
            StrokeWidth = 2f
        };
        // Bar number text paint (theme color)
        private readonly SKPaint _barTextPaint = new()
        {
            Color = ThemeColorsManager.Current.BarNumber
        };
        // Bar number font — Size 30f, Typeface set in Draw()
        private readonly SKFont _barNumberFont = new() { Size = 30f };
        // Beat line paint (theme color, lower alpha)
        private readonly SKPaint _beatLinePaint = new()
        {
            Color = ThemeColorsManager.Current.TimeLine.WithAlpha(128),
            StrokeWidth = 2f
        };
        // Signature/tempo marker line (fixed red)
        private static readonly SKPaint _signaturePaint = new()
        {
            StrokeWidth = 5f,
            Color = SKColors.Red
        };
        // Tempo text (theme color)
        private readonly SKPaint _tempoTextPaint = new()
        {
            Color = ThemeColorsManager.Current.TempoSignatureText
        };
        // Tempo font — Size 20f, Typeface set in Draw()
        private readonly SKFont _tempoFont = new() { Size = 20f };
        // Time signature text (theme color)
        private readonly SKPaint _timeSigTextPaint = new()
        {
            Color = ThemeColorsManager.Current.TimeSignatureText
        };
        // Time signature font — Size 20f, Typeface set in Draw()
        private readonly SKFont _timeSigFont = new() { Size = 20f };
        // Timeline background (theme color, low alpha)
        private readonly SKPaint _timeLineBgPaint = new()
        {
            Color = ThemeColorsManager.Current.TimeLineBackground.WithAlpha(50),
            Style = SKPaintStyle.Fill
        };

        public DrawableTickBackground(SKCanvas canvas, EditViewModel viewModel, int snapDiv = 4)
        {
            Canvas = canvas;
            ViewModel = viewModel;
        }

        public void Draw()
        {
            var typeface = ObjectProvider.NotoSansCJKscRegularTypeface;
            _barNumberFont.Typeface = typeface;
            _tempoFont.Typeface = typeface;
            _timeSigFont.Typeface = typeface;

            UProject project = OpenUtau.Core.DocManager.Inst.Project;

            int canvasWidth = Canvas.DeviceClipBounds.Size.Width;
            int canvasHeight = Canvas.DeviceClipBounds.Size.Height;

            double minLineTick = ViewConstants.MinTicklineWidth;
            double leftTick = (-Canvas.TotalMatrix.TransX) / Canvas.TotalMatrix.ScaleX;
            double rightTick = leftTick + canvasWidth / Canvas.TotalMatrix.ScaleX;
            float bottom = (-Canvas.TotalMatrix.TransY) * Canvas.TotalMatrix.ScaleY + canvasHeight;

            project.timeAxis.TickPosToBarBeat((int)leftTick, out int bar, out int beat, out int remainingTicks);

            if (bar > 0)
            {
                bar--;
            }

            int barTick = project.timeAxis.BarBeatToTickPos(bar, 0);

            // 保存当前的变换矩阵
            SKMatrix originalMatrix = Canvas.TotalMatrix;
            // 恢复到默认矩阵，使文字不受缩放影响
            Canvas.ResetMatrix();

            float h = ViewConstants.TimeLineHeight * (float)ViewModel.Density;
            // 绘制时间轴背景色
            Canvas.DrawRect(0, 0, canvasWidth, h, _timeLineBgPaint);

            // 避免绘制过于密集的线条
            int snapUnit = project.resolution * 4 / ViewModel.TrackSnapDiv;
            while (snapUnit * originalMatrix.ScaleX < ViewConstants.MinTicklineWidth)
            {
                snapUnit *= 2;
            }

            while (barTick <= rightTick)
            {
                SnapTicks?.Add(barTick);

                // 小节线和数字
                float x = (float)Math.Round((double)barTick) + 0.5f;
                float y = -0.5f;

                Canvas.DrawText((bar + 1).ToString(), x * originalMatrix.ScaleX + originalMatrix.TransX + 20, 30, _barNumberFont, _barTextPaint);
                Canvas.DrawLine(new SKPoint(x * originalMatrix.ScaleX + originalMatrix.TransX, y), new SKPoint(x * originalMatrix.ScaleX + originalMatrix.TransX, bottom + 0.5f), _barLinePaint);

                // 小节之间的线
                UTimeSignature timeSig = project.timeAxis.TimeSignatureAtBar(bar);
                int nextBarTick = project.timeAxis.BarBeatToTickPos(bar + 1, 0);

                int ticksPerBeat = project.resolution * 4 * timeSig.beatPerBar / timeSig.beatUnit;
                int ticksPerLine = snapUnit;
                if (ticksPerBeat < snapUnit)
                {
                    ticksPerLine = ticksPerBeat;
                }
                else if (ticksPerBeat % snapUnit != 0)
                {
                    if (ticksPerBeat > minLineTick)
                    {
                        ticksPerLine = ticksPerBeat;
                    }
                    else
                    {
                        ticksPerLine = nextBarTick - barTick;
                    }
                }
                if (nextBarTick > leftTick)
                {
                    for (int tick = barTick + ticksPerLine; tick < nextBarTick; tick += ticksPerLine)
                    {
                        SnapTicks?.Add(tick);
                        project.timeAxis.TickPosToBarBeat(tick, out int snapBar, out int snapBeat, out int snapRemainingTicks);
                        x = (float)(tick + 0.5);
                        y = ViewConstants.TimeLineHeight * (float)ViewModel.Density + (-originalMatrix.TransY) * originalMatrix.ScaleY + originalMatrix.TransY;
                        Canvas.DrawLine(new SKPoint(x * originalMatrix.ScaleX + originalMatrix.TransX, y), new SKPoint(x * originalMatrix.ScaleX + originalMatrix.TransX, bottom + 0.5f), _beatLinePaint);
                    }
                }
                barTick = nextBarTick;
                bar++;
            }
            SnapTicks?.Add(barTick);

            float sigX;
            // 绘制曲速标记
            foreach (var tempo in project.tempos)
            {
                sigX = (float)Math.Round((double)tempo.position) * originalMatrix.ScaleX + originalMatrix.TransX;
                Canvas.DrawLine(new SKPoint(sigX, 0), new SKPoint(sigX, h), _signaturePaint);
                Canvas.DrawText(tempo.bpm.ToString("#0.00"),
                    sigX + 20,
                    h / 2,
                    _tempoFont,
                    _tempoTextPaint);
            }

            int timeSigTick;
            // 绘制拍号标记
            foreach (var timeSig in project.timeSignatures)
            {
                timeSigTick = project.timeAxis.BarBeatToTickPos(timeSig.barPosition, 0);
                sigX = (float)Math.Round(timeSigTick * originalMatrix.ScaleX + originalMatrix.TransX);
                Canvas.DrawLine(new SKPoint(sigX, 0), new SKPoint(sigX, h), _signaturePaint);
                Canvas.DrawText($"{timeSig.beatPerBar}/{timeSig.beatUnit}",
                    sigX + 20,
                    h,
                    _timeSigFont,
                    _timeSigTextPaint);
            }

            // 恢复原始矩阵
            Canvas.SetMatrix(originalMatrix);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _barLinePaint.Dispose();
            _barTextPaint.Dispose();
            _barNumberFont.Dispose();
            _beatLinePaint.Dispose();
            _tempoTextPaint.Dispose();
            _tempoFont.Dispose();
            _timeSigTextPaint.Dispose();
            _timeSigFont.Dispose();
            _timeLineBgPaint.Dispose();
            // _signaturePaint is static — not disposed per-instance
        }
    }
}
