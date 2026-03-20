using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtauMobile.Views.Utils
{
    // 点击事件参数
    public class TapEventArgs : EventArgs
    {
        public SKPoint Position { get; }

        public TapEventArgs(SKPoint position)
        {
            Position = position;
        }
    }

    // 平移开始事件参数
    public class PanStartEventArgs : EventArgs
    {
        public SKPoint StartPosition { get; }
        /// <summary>
        /// 最初のタッチダウン位置（TouchPoint.StartPosition）。
        /// ヒットテストに使用する。PanStart 発火時の5px ドリフト前の座標。
        /// </summary>
        public SKPoint OriginalTouchDown { get; }

        /// <summary>後方互換コンストラクタ（SwitchToPanFromZoom 等から利用）</summary>
        public PanStartEventArgs(SKPoint startPosition)
            : this(startPosition, startPosition)
        {
        }

        public PanStartEventArgs(SKPoint startPosition, SKPoint originalTouchDown)
        {
            StartPosition = startPosition;
            OriginalTouchDown = originalTouchDown;
        }
    }

    // 平移进行事件参数
    public class PanUpdateEventArgs : EventArgs
    {
        public SKPoint Position { get; }
        public PanUpdateEventArgs(SKPoint position)
        {
            Position = position;
        }
    }

    // 平移结束事件参数
    public class PanEndEventArgs : EventArgs
    {
        public SKPoint EndPosition { get; }
        public PanEndEventArgs(SKPoint endPosition)
        {
            EndPosition = endPosition;
        }
    }

    // 缩放开始事件参数
    public class ZoomStartEventArgs : EventArgs
    {
        public SKPoint Point1 { get; }
        public SKPoint Point2 { get; }

        public ZoomStartEventArgs(SKPoint point1, SKPoint point2)
        {
            Point1 = point1;
            Point2 = point2;
        }
    }

    // 缩放进行事件参数
    public class ZoomUpdateEventArgs : EventArgs
    {
        public SKPoint Point1 { get; }
        public SKPoint Point2 { get; }

        public ZoomUpdateEventArgs(SKPoint point1, SKPoint point2)
        {
            Point1 = point1;
            Point2 = point2;
        }
    }
}
