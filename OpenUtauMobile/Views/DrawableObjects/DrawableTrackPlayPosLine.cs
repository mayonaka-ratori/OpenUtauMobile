using OpenUtauMobile.Views.Utils;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtauMobile.Views.DrawableObjects
{
    public class DrawableTrackPlayPosLine
    {
        public SKCanvas Canvas { get; set; } = null!;
        public int PlayPosTick { get; set; }
        public double TotalHeight { get; set; } = 0d;
        public double ResolutionX { get; set; } = 480d;

        private static readonly SKPaint _playPosPaint = new()
        {
            StrokeWidth = 3f,
            Color = SKColor.Parse("#B3F353"),
        };

        public DrawableTrackPlayPosLine(SKCanvas canvas, int playPosTick, double totalHeight, double resolutionX = 480)
        {
            Canvas = canvas;
            PlayPosTick = playPosTick;
            TotalHeight = totalHeight;
            ResolutionX = resolutionX;
        }
        public void Draw()
        {
            // 保存当前的变换矩阵
            SKMatrix originalMatrix = Canvas.TotalMatrix;
            // 恢复到默认矩阵，使文字不受缩放影响
            Canvas.ResetMatrix();
            // 计算位置
            float x = (float)(PlayPosTick * originalMatrix.ScaleX + originalMatrix.TransX);
            float y = 0f;
            // 绘制线条
            Canvas.DrawLine(x, y, x, (float)TotalHeight, _playPosPaint);
            // 恢复原始矩阵
            Canvas.SetMatrix(originalMatrix);
        }
    }
}
