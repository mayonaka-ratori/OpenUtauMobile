using OpenUtauMobile.Utils;
using OpenUtauMobile.Views.Utils;
using OpenUtauMobile.Resources.Strings;
using SkiaSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtauMobile
{
    public static class ViewConstants
    {
        public const string ReleaseVersionLyrics = "就算世界太多美好只一眨眼就变幻\n可回忆始终璀璨"; // lyrics for v1.1.x release version
        public const int MinTicklineWidth = 24; // minimum tick line width
        public const int TotalPianoKeys = 120; // total number of piano keys
        public static List<PianoKey> PianoKeys = [];
        public const int TimeLineHeight = 20; // timeline height; multiply by Density in canvas
        public const int PianoRollPlaybackLinePos = 200; // playback line X offset from piano roll left edge; multiply by Density
        public const int DivHeight = 50; // track/main-edit divider height; multiply by Density in canvas
        public static List<LanguageOption> LanguageOptions = [
            new LanguageOption("English(US)", "en"),
            new LanguageOption("简体中文", "zh"),
            new LanguageOption("日本語", "ja"),
            new LanguageOption(AppResources.System, ""),
        ];
        //public static Dictionary<int, int> MajorKeyNo = new()
        //{
        //    { 0, 60 }, // C4
        //    { 1, 61 }, // C#4
        //    { 2, 62 }, // D4
        //    { 3, 63 }, // D#4
        //    { 4, 64 }, // E4
        //    { 5, 65 }, // F4
        //    { 6, 66 }, // F#4
        //    { 7, 67 }, // G4
        //    { 8, 68 }, // G#4
        //    { 9, 69 }, // A4
        //    { 10, 70 }, // A#4
        //    { 11, 71 }, // B4
        //};
        #region Track color constants
        /// <summary>
        /// Track colors, MAUI version.
        /// </summary>
        public static readonly Dictionary<string, Color> TrackMauiColors = new()
        {
            { "Pink", Color.FromRgba("#F06292")},
            { "Red", Color.FromRgba("#EF5350")},
            { "Orange", Color.FromRgba("#FF8A65")},
            { "Yellow", Color.FromRgba("#FBC02D")},
            { "Light Green", Color.FromRgba("#CDDC39")},
            { "Green", Color.FromRgba("#66BB6A")},
            { "Light Blue", Color.FromRgba("#4FC3F7")},
            { "Blue", Color.FromRgba("#4EA6EA")},
            { "Purple", Color.FromRgba("#BA68C8")},
            { "Pink2", Color.FromRgba("#E91E63")},
            { "Red2", Color.FromRgba("#D32F2F")},
            { "Orange2", Color.FromRgba("#FF5722")},
            { "Yellow2", Color.FromRgba("#FF8F00")},
            { "Light Green2", Color.FromRgba("#AFB42B")},
            { "Green2", Color.FromRgba("#2E7D32")},
            { "Light Blue2", Color.FromRgba("#1976D2")},
            { "Blue2", Color.FromRgba("#3949AB")},
            { "Purple2", Color.FromRgba("#7B1FA2")},
        };
        /// <summary>
        /// Track colors, SkiaSharp version.
        /// </summary>
        public static readonly Dictionary<string, SKColor> TrackSkiaColors = TrackMauiColors.ToDictionary(kv => kv.Key, kv => SKColor.Parse(kv.Value.ToHex()));
        #endregion
    }
}
