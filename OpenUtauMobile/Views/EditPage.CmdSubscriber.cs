// EditPage.CmdSubscriber.cs — ICmdSubscriber / command notification handlers (partial class)
// Extracted from EditPage.xaml.cs in Phase 2.5 Step 5.
//
// NOTE: Canvas references (TrackCanvas, PianoRollCanvas, etc.), ViewModel,
// and helper methods (UpdateTrackCanvasPanLimit, RefreshProjectInfoDisplay, etc.)
// are declared in other partial class files. They are accessible here via partial class.
// Subscribe/Unsubscribe calls remain in the constructor and Dispose() in EditPage.xaml.cs.

using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Views;
using OpenUtau.Api;
using OpenUtau.Core;
using OpenUtau.Core.Render;
using OpenUtau.Core.Ustx;
using OpenUtau.Utils.Messages;
using OpenUtauMobile.Utils;
using OpenUtauMobile.ViewModels;
using OpenUtauMobile.ViewModels.Converters;
using OpenUtauMobile.Views.Controls;
using OpenUtauMobile.Views.DrawableObjects;
using OpenUtauMobile.Views.Utils;
using OpenUtauMobile.Resources.Strings;
using ReactiveUI;
using Serilog;
using SkiaSharp;
using System.Diagnostics;
using System.Reactive.Disposables;
using Preferences = OpenUtau.Core.Util.Preferences;
using DynamicData;
using OpenUtau.Core.Format;
using System.Threading.Tasks;

namespace OpenUtauMobile.Views;

public partial class EditPage
{
    public void OnNext(UCommand cmd, bool isUndo)
    {
        // iOS 需要确保 UI 更新在主线程执行
        MainThread.BeginInvokeOnMainThread(() =>
        {
        if (cmd is SetPlayPosTickNotification setPlayPosTickNotification)
        {
            _viewModel.PlayPosTick = setPlayPosTickNotification.playPosTick;
            _viewModel.PlayPosWaitingRendering = setPlayPosTickNotification.waitingRendering;
            _viewModel.PianoRollTransformer.SetPanX((float)(ViewConstants.PianoRollPlaybackLinePos * _viewModel.Density - _viewModel.PlayPosTick * _viewModel.PianoRollTransformer.ZoomX));
        }
        else if (cmd is ProgressBarNotification progressBarNotification)
        {
            ProgressbarWaitingRender.Progress = progressBarNotification.Progress / 100f;
            LabelProgress.Text = $"{progressBarNotification.Progress:0.##}%";
            LabelProgressMsg.Text = progressBarNotification.Info;
        }
        else if (cmd is SetCurveCommand setCurveCommand)
        {
            PianoRollPitchCanvas.InvalidateSurface();
            ExpressionCanvas.InvalidateSurface();
        }
        else if (cmd is AddNoteCommand addNoteCommand)
        {
            _viewModel.HandleSelectedNotesChanged();
            PianoRollCanvas.InvalidateSurface();
            TrackCanvas.InvalidateSurface(); // 重绘走带画布
            PianoRollPitchCanvas.InvalidateSurface();
        }
        else if (cmd is MoveNoteCommand moveNoteCommand)
        {
            PianoRollCanvas.InvalidateSurface();
            TrackCanvas.InvalidateSurface();
            PianoRollPitchCanvas.InvalidateSurface();
        }
        else if (cmd is ResizeNoteCommand resizeNoteCommand)
        {
            PianoRollCanvas.InvalidateSurface();
            TrackCanvas.InvalidateSurface();
            PianoRollPitchCanvas.InvalidateSurface();
        }
        else if (cmd is MovePartCommand movePartCommand)
        {
            UpdateTrackCanvasPanLimit();
            UpdatePianoRollCanvasPanLimit();
            TrackCanvas.InvalidateSurface(); // 重绘走带画布
            PianoRollCanvas.InvalidateSurface(); // 重绘钢琴卷帘画布
            PianoRollPitchCanvas.InvalidateSurface();
            PianoRollTickBackgroundCanvas.InvalidateSurface();
        }
        else if (cmd is AddPartCommand addPartCommand)
        {
            TrackCanvas.InvalidateSurface(); // 重绘走带画布
        }
        else if (cmd is ResizePartCommand resizePartCommand)
        {
            UpdateTrackCanvasPanLimit();
            UpdatePianoRollCanvasPanLimit();
            TrackCanvas.InvalidateSurface();
            PianoRollCanvas.InvalidateSurface(); // 重绘钢琴卷帘画布
            PianoRollTickBackgroundCanvas.InvalidateSurface();
        }
        else if (cmd is RemovePartCommand removePartCommand)
        {
            if (_viewModel.EditingPart == removePartCommand.part)
            {
                _viewModel.EditingPart = null;
                _viewModel.EditingNotes = null;
                _viewModel.SelectedNotes = [];
            }
            _viewModel.SelectedParts.Remove(removePartCommand.part);
            _viewModel.UpdateIsShowRenderPitchButton();
            UpdateTrackCanvasPanLimit();
            UpdatePianoRollCanvasPanLimit();
            TrackCanvas.InvalidateSurface(); // 重绘走带画布
            PianoRollCanvas.InvalidateSurface(); // 重绘钢琴卷帘画布
            PianoRollPitchCanvas.InvalidateSurface();
            PianoRollTickBackgroundCanvas.InvalidateSurface();
        }
        else if (cmd is RenamePartCommand renamePartCommand)
        {
            TrackCanvas.InvalidateSurface(); // 重绘走带画布
        }
        else if (cmd is AddTrackCommand addTrackCommand)
        {
            _viewModel.Tracks = [.. DocManager.Inst.Project.tracks];
            TrackCanvas.InvalidateSurface();
            UpdateTrackCanvasZoomLimit();
        }
        else if (cmd is PhonemizingNotification phonemizingNotification)
        {
            _viewModel.SetWork(type: WorkType.Phonemize, id: phonemizingNotification.part.GetHashCode().ToString(), detail: phonemizingNotification.part.DisplayName);
        }
        else if (cmd is PhonemizedNotification phonemizedNotification)
        {
            _viewModel.RemoveWork(id: phonemizedNotification.part.GetHashCode().ToString());
            PhonemeCanvas.InvalidateSurface();
            ExpressionCanvas.InvalidateSurface();
            PianoRollPitchCanvas.InvalidateSurface();
        }
        else if (cmd is LoadingNotification loadingNotification)
        {
            if (loadingNotification.startLoading)
            {
                WorkType workType = WorkType.Other;
                if (loadingNotification.window == typeof(UWavePart))
                {
                    workType = WorkType.ReadWave;
                }
                _viewModel.SetWork(type: workType, id: loadingNotification.loadObject);
            }
            else
            {
                _viewModel.RemoveWork(loadingNotification.loadObject);
                TrackCanvas.InvalidateSurface();
            }
        }
        else if (cmd is ChangeNoteLyricCommand changeNoteLyricCommand)
        {
            PianoRollCanvas.InvalidateSurface();
        }
        else if (cmd is RemoveNoteCommand removeNoteCommand)
        {
            _viewModel.HandleSelectedNotesChanged();
            PianoRollCanvas.InvalidateSurface();
            TrackCanvas.InvalidateSurface();
            PianoRollPitchCanvas.InvalidateSurface();
            PhonemeCanvas.InvalidateSurface();
            ExpressionCanvas.InvalidateSurface();
        }
        else if (cmd is RemoveTrackCommand removeTrackCommand)
        {
            _viewModel.ValidateSelectedParts(); // 验证选中分片中是否有被删除的分片
            _viewModel.RefreshTrack();
            _viewModel.HandleSelectedNotesChanged();
            TrackCanvas.InvalidateSurface();
            PianoRollCanvas.InvalidateSurface(); // 重绘钢琴卷帘画布
            UpdateTrackCanvasZoomLimit();
            PianoRollPitchCanvas.InvalidateSurface();
            PhonemeCanvas.InvalidateSurface();
            ExpressionCanvas.InvalidateSurface();
        }
        else if (cmd is TrackChangeSingerCommand trackChangeSingerCommand)
        {
            if (_viewModel.Tracks.Remove(trackChangeSingerCommand.track))
            {
                _viewModel.Tracks.Insert(trackChangeSingerCommand.track.TrackNo, trackChangeSingerCommand.track);
            }
            _viewModel.UpdateIsShowRenderPitchButton();
            TrackCanvas.InvalidateSurface(); // 重绘走带画布
            PianoRollCanvas.InvalidateSurface(); // 重绘钢琴卷帘画布
            _viewModel.LoadPortrait();
        }
        else if (cmd is AddTempoChangeCommand addTempoChangeCommand)
        {
            TrackCanvas.InvalidateSurface(); // 重绘走带画布
            PlaybackTickBackgroundCanvas.InvalidateSurface();
            RefreshProjectInfoDisplay();
        }
        else if (cmd is AddTimeSigCommand addTimeSigCommand)
        {
            TrackCanvas.InvalidateSurface(); // 重绘走带画布
            PlaybackTickBackgroundCanvas.InvalidateSurface();
            RefreshProjectInfoDisplay();
        }
        else if (cmd is BpmCommand bpmCommand)
        {
            TrackCanvas.InvalidateSurface(); // 重绘走带画布
            PlaybackTickBackgroundCanvas.InvalidateSurface();
            RefreshProjectInfoDisplay();
        }
        else if (cmd is TimeSignatureCommand timeSigCommand)
        {
            TrackCanvas.InvalidateSurface(); // 重绘走带画布
            PlaybackTickBackgroundCanvas.InvalidateSurface();
            RefreshProjectInfoDisplay();
        }
        else if (cmd is TrackChangePhonemizerCommand phonemizerCommand)
        {
            _viewModel.RefreshTrack(phonemizerCommand.track);
        }
        else if (cmd is ExportingNotification exportingNotification)
        {
            _viewModel.SetWork(WorkType.Export, exportingNotification.Id, exportingNotification.Progress, exportingNotification.Info);
        }
        else if (cmd is ExportedNotification exportedNotification)
        {
            _viewModel.RemoveWork(id: exportedNotification.Id);
            Toast.Make(exportedNotification.Info, CommunityToolkit.Maui.Core.ToastDuration.Short, 16).Show();
        }
        else if (cmd is KeyCommand keyCommand)
        {
            PianoKeysCanvas.InvalidateSurface();
            RefreshProjectInfoDisplay();
        }
        else if (cmd is ChangeTrackColorCommand changeTrackColorCommand)
        {
            _viewModel.RefreshTrack(changeTrackColorCommand.track);
            TrackCanvas.InvalidateSurface(); // 重绘走带画布
            PianoRollCanvas.InvalidateSurface(); // 重绘钢琴卷帘画布
            ExpressionCanvas.InvalidateSurface();
            if (_viewModel.EditingPart == null)
                return;
            _viewModel.EditingPartColor = ViewConstants.TrackMauiColors[DocManager.Inst.Project.tracks[_viewModel.EditingPart.trackNo].TrackColor];
        }
        else if (cmd is RenameTrackCommand renameTrackCommand)
        {
            _viewModel.RefreshTrack(renameTrackCommand.track);
        }
        else if (cmd is LoadProjectNotification loadProject)
        {
            OpenUtau.Core.Util.Preferences.AddRecentFileIfEnabled(loadProject.project.FilePath);
            _viewModel.Tracks = [.. OpenUtau.Core.DocManager.Inst.Project.tracks];
            _viewModel.Path = OpenUtau.Core.DocManager.Inst.Project.FilePath;
            PianoKeysCanvas.InvalidateSurface();
            PianoRollCanvas.InvalidateSurface();
            PianoRollTickBackgroundCanvas.InvalidateSurface();
            PlaybackPosCanvas.InvalidateSurface();
            TrackCanvas.InvalidateSurface();
            PlaybackTickBackgroundCanvas.InvalidateSurface();
            PianoRollPitchCanvas.InvalidateSurface();
            UpdateTrackCanvasZoomLimit();
            UpdatePianoRollCanvasZoomLimit();
            RefreshProjectInfoDisplay();
            _viewModel.InitExpressions();
        }
        else if (cmd is SaveProjectNotification saveProjectNotification)
        {
            OpenUtau.Core.Util.Preferences.AddRecentFileIfEnabled(saveProjectNotification.Path);
            _viewModel.Path = OpenUtau.Core.DocManager.Inst.Project.FilePath;
#if !WINDOWS
            CommunityToolkit.Maui.Alerts.Toast.Make(AppResources.Saved, CommunityToolkit.Maui.Core.ToastDuration.Short, 16).Show();
#endif
        }
        }); // MainThread.BeginInvokeOnMainThread
    }
}
