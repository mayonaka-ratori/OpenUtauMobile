// EditPage.Toolbar.cs — Button/UI event handlers (partial class)
// Extracted from EditPage.xaml.cs in Phase 2.5 Step 4.
//
// NOTE: Fields, ViewModel references, and named XAML elements are declared
// in EditPage.xaml.cs. They are accessible here via partial class.
// Helper methods Save(), SaveAs(), AttemptExit(), AskIfSaveAndContinue(),
// RefreshProjectInfoDisplay(), UpdateRenderProgress() are co-located here
// since they are called exclusively from button handlers.

using CommunityToolkit.Maui.Views;
using OpenUtau.Api;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using OpenUtauMobile.Utils;
using OpenUtauMobile.ViewModels;
using OpenUtauMobile.Views.Controls;
using OpenUtauMobile.Views.Utils;
using OpenUtauMobile.Resources.Strings;
using Serilog;
using System.Diagnostics;
using Preferences = OpenUtau.Core.Util.Preferences;
using OpenUtau.Core.Format;
using System.Threading.Tasks;

namespace OpenUtauMobile.Views;

public partial class EditPage
{
    private const float ZoomStepFactor = 1.25f;
    private const string ActiveQuantizeButtonArgb = "#60FFFFFF";

    private void ButtonPlayOrPause_Clicked(object sender, EventArgs e)
    {
        if (PlaybackManager.Inst.Playing) // 如果正在播放 => 暂停
        {
            PlaybackManager.Inst.PlayOrPause();
            // When pausing we don't want the playhead to jump to start yet
            _viewModel.PlaybackWasStoppedManually = false;
        }
        else // 如果没有播放 => 播放
        {
            // Set start position
            _viewModel.PlaybackStartPosition = _viewModel.PlayPosTick;
            _viewModel.PlaybackWasStoppedManually = false;
            PlaybackManager.Inst.StopPlayback();
            PlaybackManager.Inst.PlayOrPause();
        }
    }

    private void ButtonSwitchEditMode_Clicked(object sender, EventArgs e)
    {
        _viewModel.CurrentTrackEditMode = _viewModel.CurrentTrackEditMode == TrackEditMode.Edit ? TrackEditMode.Normal : TrackEditMode.Edit;
        // 重绘走带画布
        TrackCanvas.InvalidateSurface();
    }

    private void ButtonZoomIn_Clicked(object sender, EventArgs e)
    {
        _viewModel.TrackTransformer.SetZoomX(_viewModel.TrackTransformer.ZoomX * ZoomStepFactor);
        _viewModel.TrackTransformer.SetPanX(_viewModel.TrackTransformer.PanX * ZoomStepFactor); // 放大时保持左侧位置不变
    }

    private void ButtonZoomOut_Clicked(object sender, EventArgs e)
    {
        _viewModel.TrackTransformer.SetZoomX(_viewModel.TrackTransformer.ZoomX / ZoomStepFactor);
        _viewModel.TrackTransformer.SetPanX(_viewModel.TrackTransformer.PanX / ZoomStepFactor); // 缩小时保持左侧位置不变
    }

    private async void ButtonSave_Clicked(object sender, EventArgs e)
    {
        await Save();
    }

    /// <summary>
    /// Saves the current project. Calls SaveAs() for unsaved projects.
    /// </summary>
    /// <returns>true if saved successfully; false if the user cancelled.</returns>
    private async Task<bool> Save()
    {
        if (!DocManager.Inst.Project.Saved)
        {
            return await SaveAs(); // 新项目必须另存为
        }
        else
        {
            DocManager.Inst.ExecuteCmd(new SaveProjectNotification(string.Empty)); // 保持当前路径保存
            return true;
        }
    }

    /// <summary>
    /// Prompts the user to choose a file path and saves the current project there.
    /// </summary>
    /// <returns>true if saved successfully; false if the user cancelled.</returns>
    private async Task<bool> SaveAs()
    {
        string path = await ObjectProvider.SaveFile([".ustx"], this);
        if (!string.IsNullOrEmpty(path))
        {
            DocManager.Inst.ExecuteCmd(new SaveProjectNotification(path));
            return true;
        }
        return false;
    }

    private void ButtonUndo_Clicked(object sender, EventArgs e)
    {
        DocManager.Inst.Undo();
    }

    private void ButtonRedo_Clicked(object sender, EventArgs e)
    {
        DocManager.Inst.Redo();
    }

    private void ButtonRemovePart_Clicked(object sender, EventArgs e)
    {
        if (_viewModel.SelectedParts.Count > 0)
        {
            // 删除选中的分片
            _viewModel.RemoveSelectedParts();
        }
    }

    private void ButtonAddTrack_Clicked(object sender, EventArgs e)
    {
        EditViewModel.AddTrack();
    }

    private async void ButtonRenamePart_Clicked(object sender, EventArgs e)
    {
        // 启动撤销组
        DocManager.Inst.StartUndoGroup();
        // 重命名选中的第一个分片
        if (_viewModel.SelectedParts.Count > 0)
        {
            UPart part = _viewModel.SelectedParts[0];
            Popup popup = new RenamePopup(part.DisplayName, AppResources.RenamePart);
            object? result = await this.ShowPopupAsync(popup);
            if (result != null)
            {
                if (result is string newName)
                {
                    DocManager.Inst.ExecuteCmd(new RenamePartCommand(DocManager.Inst.Project, part, newName));
                }
            }
        }
        // 结束撤销组
        DocManager.Inst.EndUndoGroup();
    }

    private void ButtonMuted_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is UTrack track)
        {
            _viewModel.ToggleTrackMuted(track);
        }
    }

    /// <summary>
    /// 将当前轨道上移一层
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ButtonMoveUp_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is UTrack track)
        {
            if (_viewModel.MoveTrackUp(track))
            {
                // 移动成功，更新UI
                TrackCanvas.InvalidateSurface();
            }
        }
    }

    /// <summary>
    /// 将当前轨道下移一层
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ButtonMoveDown_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is UTrack track)
        {
            if (_viewModel.MoveTrackDown(track))
            {
                // 移动成功，更新UI
                TrackCanvas.InvalidateSurface();
            }
        }
    }

    private void ButtonSwitchNoteMode_Clicked(object sender, EventArgs e)
    {
        //if (sender == ButtonSwitchNormolMode)
        //{
        //    _viewModel.CurrentNoteEditMode = NoteEditMode.Normal;
        //}
        if (sender == ButtonSwitchEditNoteMode)
        {
            _viewModel.CurrentNoteEditMode = NoteEditMode.EditNote;
        }
        else if (sender == ButtonSwitchEditPitchCurveMode)
        {
            _viewModel.CurrentNoteEditMode = NoteEditMode.EditPitchCurve;
        }
        else if (sender == ButtonSwitchEditPitchAnchorMode)
        {
            _viewModel.CurrentNoteEditMode = NoteEditMode.EditPitchAnchor;
        }
        else if (sender == ButtonSwitchEditVibratoMode)
        {
            _viewModel.CurrentNoteEditMode = NoteEditMode.EditVibrato;
        }
        else
        {
            Console.WriteLine("未知の NoteEditMode ボタン");
        }
    }

    private async void ButtonSingerAvatar_Clicked(object sender, EventArgs e)
    {
        if (sender is ImageButton button && button.BindingContext is UTrack track)
        {
            Popup popup = new ChooseSingerPopup(track);
            object? result = await this.ShowPopupAsync(popup);
            if (result is USinger newSinger)
            {
                _viewModel.SetSinger(track, newSinger);
            }
        }
    }

    private void ButtonToggleDetailedTrackHeader_Clicked(object sender, EventArgs e)
    {
        _viewModel.ToggleDetailedTrackHeader();
    }

    private void GestureChangeVolume_PanUpdated(object sender, PanUpdatedEventArgs e)
    {
        if (sender is Slider slider && slider.BindingContext is UTrack track)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    // 保存初始音量
                    _viewModel.OriginalVolume = track.Volume;
                    break;
                case GestureStatus.Running:
                    double deltaVolume = e.TotalX / 20; // 每移动20像素，音量变化1.0
                    slider.Value = Math.Clamp(_viewModel.OriginalVolume + deltaVolume, -24, 12);
                    break;
                case GestureStatus.Completed:
                    Debug.WriteLine("Pan completed");
                    _viewModel.RefreshTrack(track);
                    break;
                case GestureStatus.Canceled:
                    break;
            }
        }
    }

    private void GestureResetPan_Tapped(object sender, TappedEventArgs e)
    {
        Debug.WriteLine("重置声像");
        if (sender is Slider slider)
        {
            slider.Value = 0;
        }
    }

    private void GestureChangePan_PanUpdated(object sender, PanUpdatedEventArgs e)
    {
        if (sender is Slider slider && slider.BindingContext is UTrack track)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    // 保存初始声像
                    _viewModel.OriginalPan = track.Pan;
                    break;
                case GestureStatus.Running:
                    double deltaPan = e.TotalX; // 每移动100像素，声像变化1.0
                    slider.Value = Math.Clamp(_viewModel.OriginalPan + deltaPan, -100.0, 100.0);
                    break;
                case GestureStatus.Completed:
                    Debug.WriteLine("Pan completed");
                    _viewModel.RefreshTrack(track);
                    break;
                case GestureStatus.Canceled:
                    break;
            }
        }
    }

    private void GestureResetVolume_Tapped(object sender, TappedEventArgs e)
    {
        Debug.WriteLine("重置音量");
        if (sender is Slider slider)
        {
            slider.Value = 0;
        }
    }

    private void ButtonRemoveNote_Clicked(object sender, EventArgs e)
    {
        _viewModel.RemoveNotes();
    }

    private void ButtonPianoRollSnapToGrid_Clicked(object sender, EventArgs e)
    {
        if (isPianoRollSnapDivButtonLongPressed)
        {
            // 如果是长按触发的点击事件，忽略此次点击
            isPianoRollSnapDivButtonLongPressed = false;
            return;
        }
        Debug.WriteLine("单击");
        _viewModel.IsPianoRollSnapToGrid = !_viewModel.IsPianoRollSnapToGrid;
        //ButtonPianoRollSnapToGrid.BackgroundColor = _viewModel.IsPianoRollSnapToGrid ? Color.FromArgb("#FF4081") : Color.FromRgba("#FFFFFF");
        //ButtonPianoRollSnapToGrid.Text = _viewModel.IsPianoRollSnapToGrid ? "磁" : "不";
        if (Application.Current?.Resources != null)
        {
            ButtonPianoRollSnapToGrid.ImageSource = _viewModel.IsPianoRollSnapToGrid
                ? (ImageSource)Application.Current.Resources["magnet"]
                : (ImageSource)Application.Current.Resources["magnet-off"];
        }
    }

    private async void TouchBehaviorPianoRollSnapToGrid_LongPressCompleted(object sender, CommunityToolkit.Maui.Core.LongPressCompletedEventArgs e)
    {
        Debug.WriteLine("长按完成");
        // 阻止单击事件
        isPianoRollSnapDivButtonLongPressed = true;
        // 弹出选择菜单
        Popup popup = new PianoRollSnapDivPopup(_viewModel.PianoRollSnapDiv, _viewModel.SnapDivs, AppResources.PianoRollQuantization);
        object? result = await this.ShowPopupAsync(popup);
        if (result is int newSnapDiv)
        {
            _viewModel.PianoRollSnapDiv = newSnapDiv;
            PianoRollTickBackgroundCanvas.InvalidateSurface();
            Debug.WriteLine($"选择了新的量化: {newSnapDiv}");
        }
    }

    private async void TouchBehaviorTrackSnapToGrid_LongPressCompleted(object sender, CommunityToolkit.Maui.Core.LongPressCompletedEventArgs e)
    {
        Debug.WriteLine("长按完成");
        // 阻止单击事件
        isTrackSnapDivButtonLongPressed = true;
        // 弹出选择菜单
        Popup popup = new PianoRollSnapDivPopup(_viewModel.TrackSnapDiv, _viewModel.SnapDivs, "走带量化");
        object? result = await this.ShowPopupAsync(popup);
        if (result is int newSnapDiv)
        {
            _viewModel.TrackSnapDiv = newSnapDiv;
            PlaybackTickBackgroundCanvas.InvalidateSurface();
            Debug.WriteLine($"选择了新的量化: {newSnapDiv}");
        }
    }

    private void ButtonTrackSnapToGrid_Clicked(object sender, EventArgs e)
    {
        if (isTrackSnapDivButtonLongPressed)
        {
            // 如果是长按触发的点击事件，忽略此次点击
            isTrackSnapDivButtonLongPressed = false;
            return;
        }
        Debug.WriteLine("单击");
        _viewModel.IsTrackSnapToGrid = !_viewModel.IsTrackSnapToGrid;
        if (Application.Current?.Resources != null)
        {
            ButtonTrackSnapToGrid.ImageSource = _viewModel.IsTrackSnapToGrid
                ? (ImageSource)Application.Current.Resources["magnet"]
                : (ImageSource)Application.Current.Resources["magnet-off"];
        }
    }

    private async void ButtonMore_Clicked(object sender, EventArgs e)
    {
        Popup popup = new EditMenuPopup();
        object? result = await this.ShowPopupAsync(popup);
        if (result is not string action) return;
        switch (action)
        {
            case "import_audio":
            {
                string path = await ObjectProvider.PickFile([".wav", ".mp3", ".flac", ".ogg"], this);
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
                _viewModel.ImportAudio(path);
                break;
            }
            case "import_tracks":
            {
                string path = await ObjectProvider.PickFile([".ustx", ".vsqx", ".ust", ".mid", ".midi", ".ufdata", ".musicxml"], this);
                string[] files = [path]; // 暂且只支持一个一个地选
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
                try {
                    UProject[] loadedProjects = Formats.ReadProjects(files);
                    if (loadedProjects == null || loadedProjects.Length == 0) {
                        return;
                    }
                    // 为新项目导入曲速，否则询问用户
                    bool importTempo = DocManager.Inst.Project.parts.Count == 0; // 当前是新项目（没有分片）则直接导入曲速
                    if (!importTempo && loadedProjects[0].tempos.Count > 0) {
                        var tempoString = string.Join("\n",
                            loadedProjects[0].tempos
                                .Select(tempo => $"位于 {tempo.position} 的曲速标记为 {tempo.bpm}")
                        );
                        // 询问用户是否导入曲速
                        importTempo = await DisplayAlert(AppResources.ImportTracksCaption, AppResources.AskIfImportTempo + '\n' + tempoString, AppResources.Confirm,
                            AppResources.CancelText);
                    }
                    _viewModel.ImportTracks(loadedProjects, importTempo);
                } catch (Exception ex) {
                    Log.Error(ex, $"导入轨道失败\n文件：{string.Join(", ", files)}\n错误：{ex.Message}");
                    DocManager.Inst.ExecuteCmd(new ErrorMessageNotification("导入轨道失败：", ex));
                }
                break;
            }
            case "save_as":
                await SaveAs();
                break;
            case "export_audio":
            {
                string file = await ObjectProvider.SaveFile([".wav"], this);
                if (!string.IsNullOrEmpty(file))
                {
                    Popup exportPopup = new ExportAudioPopup(file);
                    await this.ShowPopupAsync(exportPopup);
                }

                break;
            }
            case "settings":
                await Navigation.PushModalAsync(new SettingsPage());
                break;
            case "import_midi":
            {
                string path = await ObjectProvider.PickFile([".mid", ".midi"], this);
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
                _viewModel.ImportMidi(path);
                break;
            }
            default:
                Debug.WriteLine($"未知的操作: {action}");
                break;
        }
    }

    private void ButtonRemoveTrack_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is UTrack track)
        {
            _viewModel.RemoveTrack(track);
        }
    }

    private async void ButtonBack_Clicked(object sender, EventArgs e)
    {
        await AttemptExit();
    }

    private async Task AttemptExit()
    {
        if (DocManager.Inst.ChangesSaved)
        { // 如果已经保存，直接关闭
            if (Preferences.Default.ClearCacheOnQuit)
            {
                Log.Information("Clearing cache...");
                PathManager.Inst.ClearCache();
                Log.Information("Cache cleared.");
            }
            DocManager.Inst.RemoveSubscriber(this); // ページ離脱前に通知を遮断 (CR3-17)
            await Navigation.PopModalAsync();
            Dispose();
            return;
        }
        if (!await AskIfSaveAndContinue())
        { // 询问是否保存
            return; // 如果’取消’，则不关闭
        }
        DocManager.Inst.RemoveSubscriber(this); // ページ離脱前に通知を遮断 (CR3-17)
        await Navigation.PopModalAsync(); // 不保存，直接退出
        Dispose(); // (B-02) 保存なし離脱パスでも完全破棄を保証
    }

    private async Task<bool> AskIfSaveAndContinue()
    {
        Popup popup = new ExitPopup();
        object? result = await this.ShowPopupAsync(popup);
        if (result is string action)
        {
            switch (action)
            {
                case "save":
                    if (await Save())
                    {
                        return true; // 保存成功，继续关闭
                    }
                    else
                    {
                        return false; // 保存失败或取消，取消关闭
                    }
                case "discard":
                    return true; // 不保存，继续关闭
                case "cancel":
                    return false; // 取消关闭
                default:
                    return false; // 取消关闭
            }
        }
        return false;
    }

    public void RefreshProjectInfoDisplay()
    {
        LabelBpm.Text = DocManager.Inst.Project.tempos[0].bpm.ToString("F2");
        LabelBeatUnit.Text = DocManager.Inst.Project.timeSignatures[0].beatUnit.ToString();
        LabelBeatPerBar.Text = DocManager.Inst.Project.timeSignatures[0].beatPerBar.ToString();
        LabelKeyName.Text = $"1 = {MusicMath.KeysInOctave[DocManager.Inst.Project.key].Item1}";
    }

    private async void ButtonEditBpm_Clicked(object sender, EventArgs e)
    {
        Popup popup = new EditBpmPopup(DocManager.Inst.Project.tempos[0].bpm.ToString());
        object? result = await this.ShowPopupAsync(popup);
        if (result is string bpmStr && double.TryParse(bpmStr, out double newBpm) && newBpm > 0)
        {
            using var undo = new UndoScope();
            DocManager.Inst.ExecuteCmd(new BpmCommand(DocManager.Inst.Project, newBpm));
            RefreshProjectInfoDisplay();
        }
    }

    private async void ButtonEditBeat_Clicked(object sender, EventArgs e)
    {
        Popup popup = new EditBeatPopup(DocManager.Inst.Project.timeSignatures[0].beatPerBar, DocManager.Inst.Project.timeSignatures[0].beatUnit);
        object? result = await this.ShowPopupAsync(popup);
        if (result is Tuple<int, int> newTimeSignature)
        {
            using var undo = new UndoScope();
            DocManager.Inst.ExecuteCmd(new TimeSignatureCommand(DocManager.Inst.Project, newTimeSignature.Item1, newTimeSignature.Item2));
            RefreshProjectInfoDisplay();
        }
    }

    private async void ButtonEditKey_Clicked(object sender, EventArgs e)
    {
        Popup popup = new EditKeyPopup(DocManager.Inst.Project.key);
        object? result = await this.ShowPopupAsync(popup);
        if (result is int newKey)
        {
            using var undo = new UndoScope();
            DocManager.Inst.ExecuteCmd(new KeyCommand(DocManager.Inst.Project, newKey));
        }
    }

    private async void ButtonRenderPitch_Clicked(object sender, EventArgs e)
    {
        bool isContinue = true;
        if (Preferences.Default.WarnOnRenderPitch)
        {
            isContinue = await DisplayAlert(AppResources.LoadPitchRenderingResult, AppResources.LoadPitchRenderingResultPrompt, AppResources.Confirm, AppResources.CancelText);
        }
        if (!isContinue)
        {
            return;
        }
        if (_viewModel.EditingPart != null)
        {
            List<UNote> notes = [];
            if (_viewModel.SelectedNotes.Count > 0)
            {
                notes.AddRange(_viewModel.SelectedNotes);
            }
            else
            {
                notes.AddRange(_viewModel.EditingPart.notes);
            }
            CancellationTokenSource cts = new();
            string workId = notes.GetHashCode().ToString();
            _viewModel.SetWork(WorkType.RenderPitch, workId, 0.5d, string.Empty, cts);
            await Task.Run(() =>
            {
                _viewModel.RenderPitchAsync(_viewModel.EditingPart, notes, (workId, renderedPhrases, totalPhrases) =>
                {
                    UpdateRenderProgress(workId, renderedPhrases, totalPhrases);
                }, cts.Token, workId);
            });
        }
    }

    public void UpdateRenderProgress(string workId, int renderedPhrases, int totalPhrases)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (renderedPhrases >= totalPhrases)
            {
                _viewModel.RemoveWork(workId);
            }
            else
            {
                double progress = totalPhrases > 0 ? (double)renderedPhrases / totalPhrases : 0;
                _viewModel.SetWork(WorkType.RenderPitch, workId, progress, string.Empty, null);
            }
        });
    }

    private void ButtonTryCancelWork_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is RunningWork work)
        {
            _viewModel.TryCancelWork(work.Id);
        }
    }

    private void ButtonExchangeExp_Clicked(object sender, EventArgs e)
    {
        UExpressionDescriptor tmp = _viewModel.PrimaryExpressionDescriptor;
        _viewModel.PrimaryExpressionDescriptor = _viewModel.SecondaryExpressionDescriptor;
        _viewModel.SecondaryExpressionDescriptor = tmp;
        _viewModel.UpdateExpressions();
        ExpressionCanvas.InvalidateSurface();
    }

    private async void ButtonChangeTrackColor_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is UTrack track)
        {
            Popup popup = new ChooseTrackColorPopup();
            object? result = await this.ShowPopupAsync(popup);
            if (result is string colorKey)
            {
                using var undo = new UndoScope();
                DocManager.Inst.ExecuteCmd(new ChangeTrackColorCommand(DocManager.Inst.Project, track, colorKey));
            }
        }
    }

    private async void ButtonChangeTrackName_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is UTrack track)
        {
            Popup popup = new RenamePopup(track.TrackName, AppResources.RenameTrack);
            object? result = await this.ShowPopupAsync(popup);
            if (result is string newName && !string.IsNullOrEmpty(newName) && newName != track.TrackName)
            {
                using var undo = new UndoScope();
                DocManager.Inst.ExecuteCmd(new RenameTrackCommand(DocManager.Inst.Project, track, newName));
            }
        }
    }

    private void ButtonStop_Clicked(object sender, EventArgs e)
    {
        // If the playback wasn't stopped manually (e.g: it was playing or paused), return to the recorded start position
        if (_viewModel.PlaybackWasStoppedManually == false)
        {
            PlaybackManager.Inst.StopPlayback();
            _viewModel.PlaybackWasStoppedManually = true;
            DocManager.Inst.ExecuteCmd(new SeekPlayPosTickNotification(_viewModel.PlaybackStartPosition));
        }
        else
        {
            // If it was already stopped, pressing stop again returns it to the very start (Tick 0)
            DocManager.Inst.ExecuteCmd(new SeekPlayPosTickNotification(0));
        }
    }

    private async void ButtonChangePhonemizer_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is UTrack track)
        {
            Popup popup = new SelectPhonemizerPopup();
            object? result = await this.ShowPopupAsync(popup);
            if (result is PhonemizerFactory factory)
            {
                try
                {
                    Phonemizer phonemizer = factory.Create();
                    if (track.Phonemizer != null && track.Phonemizer.GetType() == phonemizer.GetType())
                    {
                        return;
                    }
                    DocManager.Inst.StartUndoGroup();
                    DocManager.Inst.ExecuteCmd(new TrackChangePhonemizerCommand(DocManager.Inst.Project, track, phonemizer));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "未能更改音素器");
                    DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(ex));
                }
                finally
                {
                    DocManager.Inst.EndUndoGroup();
                }
            }
        }
    }

    private async void ButtonPrimaryExp_Clicked(object sender, EventArgs e)
    {
        string[] abbrs = [.. DocManager.Inst.Project.expressions.Keys];
        string result = await DisplayActionSheet(AppResources.SelectExpression, AppResources.CancelText, null, abbrs);
        if (!string.IsNullOrEmpty(result) &&
            result != AppResources.CancelText &&
            DocManager.Inst.Project.expressions.TryGetValue(result, out UExpressionDescriptor? newExpressionDescriptor) &&
            newExpressionDescriptor != null)
        {
            using var undo = new UndoScope();
            _viewModel.PrimaryExpressionDescriptor = newExpressionDescriptor;
            _viewModel.UpdateExpressions();
            ExpressionCanvas.InvalidateSurface();
        }
    }

    private async void ButtonSecondaryExp_Clicked(object sender, EventArgs e)
    {
        string[] abbrs = [.. DocManager.Inst.Project.expressions.Keys];
        string result = await DisplayActionSheet(AppResources.SelectExpression, AppResources.CancelText, null, abbrs);
        if (!string.IsNullOrEmpty(result) &&
            result != AppResources.CancelText &&
            DocManager.Inst.Project.expressions.TryGetValue(result, out UExpressionDescriptor? newExpressionDescriptor) &&
            newExpressionDescriptor != null)
        {
            using var undo = new UndoScope();
            _viewModel.SecondaryExpressionDescriptor = newExpressionDescriptor;
            _viewModel.UpdateExpressions();
            ExpressionCanvas.InvalidateSurface();
        }
    }

    private void ButtonSwitchExpressionEditMode_Clicked(object sender, EventArgs e)
    {
        if (sender == ButtonSwitchExpressionHandMode)
        {
            _viewModel.CurrentExpressionEditMode = ExpressionEditMode.Hand;
        }
        else if (sender == ButtonSwitchExpressionEditMode)
        {
            _viewModel.CurrentExpressionEditMode = ExpressionEditMode.Edit;
        }
        else if (sender == ButtonSwitchExpressionEraserMode)
        {
            _viewModel.CurrentExpressionEditMode = ExpressionEditMode.Eraser;
        }
    }
    private void ButtonSelect_Clicked(object sender, EventArgs e)
    {
        _viewModel.CurrentSelectMode = 
        _viewModel.CurrentSelectMode == OpenUtauMobile.ViewModels.SelectionMode.Multi 
                ? OpenUtauMobile.ViewModels.SelectionMode.Single 
                : OpenUtauMobile.ViewModels.SelectionMode.Multi;
        _viewModel.SelectedNotes.Clear(); 
        PianoRollCanvas.InvalidateSurface(); 
    }
    private void ButtonCopy_Clicked(object sender, EventArgs e)
    {
        _viewModel.CopySelectedNotes();
        CommunityToolkit.Maui.Alerts.Toast.Make(AppResources.CopiedToClipboardToast, CommunityToolkit.Maui.Core.ToastDuration.Short).Show();
    }
    private void ButtonPaste_Clicked(object sender, EventArgs e)
    {
        _viewModel.PasteNotes();

        PianoRollCanvas.InvalidateSurface();
        PianoRollPitchCanvas.InvalidateSurface();
        PhonemeCanvas.InvalidateSurface();
    }
    private void ButtonSelectAll_Clicked(object sender, EventArgs e)
    {
        _viewModel.SelectAllNotes();
        PianoRollCanvas.InvalidateSurface();
    }

    private async void ButtonAudioTranscribe_Clicked(object sender, EventArgs e)
    {
        if (_viewModel.SelectedParts == null || _viewModel.SelectedParts.Count == 0 || _viewModel.SelectedParts[0] is not UWavePart wavePart)
        {
            return;
        }
        try
        {
            LoadingPopup popup = new(true);
            this.ShowPopup(popup);
            UVoicePart? result = await _viewModel.AudioTranscribe(wavePart, (progress, message) =>
            {
                popup.Update(progress, message);
            });
            if (result != null)
            {
                var project = DocManager.Inst.Project;
                UTrack track = new(project)
                {
                    TrackNo = project.tracks.Count
                };
                result.trackNo = track.TrackNo;
                using var undo = new UndoScope();
                DocManager.Inst.ExecuteCmd(new AddTrackCommand(project, track));
                DocManager.Inst.ExecuteCmd(new AddPartCommand(project, result));
            }
            await popup.Finish();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "干声转换失败");
            DocManager.Inst.ExecuteCmd(new ErrorMessageNotification("干声转换失败：", ex));
        }
    }

    #region Vibrato Panel Handlers

    /// <summary>
    /// VibratoPanel のスライダー値を選択ノートの現在値に合わせてリフレッシュする。
    /// EditVibrato モードに切り替えた直後に呼ぶ。
    /// </summary>
    internal void RefreshVibratoPanelValues()
    {
        var vib = _viewModel.GetVibratoForSelectedNote();
        if (vib == null) return;
        // スライダー ValueChanged ハンドラが ViewModel を呼ばないよう一時フラグを立てる
        _suppressVibratoSliderEvents = true;
        SliderVibratoLength.Value = vib.length;
        SliderVibratoDepth.Value = vib.depth;
        SliderVibratoPeriod.Value = vib.period;
        SliderVibratoFadeIn.Value = vib.@in;
        SliderVibratoFadeOut.Value = vib.@out;
        LabelVibratoLength.Text = $"Length: {vib.length:F0}%";
        LabelVibratoDepth.Text = $"Depth: {vib.depth:F0} cents";
        LabelVibratoPeriod.Text = $"Period: {vib.period:F0} ms";
        LabelVibratoFadeIn.Text = $"Fade In: {vib.@in:F0}%";
        LabelVibratoFadeOut.Text = $"Fade Out: {vib.@out:F0}%";
        _suppressVibratoSliderEvents = false;
    }

    private bool _suppressVibratoSliderEvents = false;

    private void ButtonVibratoToggle_Clicked(object sender, EventArgs e)
    {
        _viewModel.ToggleVibratoForSelectedNotes();
        RefreshVibratoPanelValues();
        PianoRollCanvas.InvalidateSurface();
    }

    private void SliderVibratoLength_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (_suppressVibratoSliderEvents) return;
        LabelVibratoLength.Text = $"Length: {e.NewValue:F0}%";
        _viewModel.SetVibratoLength((float)e.NewValue);
        PianoRollCanvas.InvalidateSurface();
    }

    private void SliderVibratoDepth_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (_suppressVibratoSliderEvents) return;
        LabelVibratoDepth.Text = $"Depth: {e.NewValue:F0} cents";
        _viewModel.SetVibratoDepth((float)e.NewValue);
        PianoRollCanvas.InvalidateSurface();
    }

    private void SliderVibratoPeriod_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (_suppressVibratoSliderEvents) return;
        LabelVibratoPeriod.Text = $"Period: {e.NewValue:F0} ms";
        _viewModel.SetVibratoPeriod((float)e.NewValue);
        PianoRollCanvas.InvalidateSurface();
    }

    private void SliderVibratoFadeIn_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (_suppressVibratoSliderEvents) return;
        LabelVibratoFadeIn.Text = $"Fade In: {e.NewValue:F0}%";
        _viewModel.SetVibratoFadeIn((float)e.NewValue);
        PianoRollCanvas.InvalidateSurface();
    }

    private void SliderVibratoFadeOut_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (_suppressVibratoSliderEvents) return;
        LabelVibratoFadeOut.Text = $"Fade Out: {e.NewValue:F0}%";
        _viewModel.SetVibratoFadeOut((float)e.NewValue);
        PianoRollCanvas.InvalidateSurface();
    }

    #endregion

    #region Quantize Button Handlers

    /// <summary>
    /// 1/4, 1/8, 1/16, 1/32, 三連符ボタンで PianoRollSnapDiv を変更し、
    /// アクティブなボタンを視覚的にハイライトする。
    /// </summary>
    private void BtnSnap_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button btn) return;
        int div = btn.Text switch
        {
            "1/4"  => 4,
            "1/8"  => 8,
            "1/16" => 16,
            "1/32" => 32,
            "3連"  => 3,
            _      => 16,
        };
        _viewModel.PianoRollSnapDiv = div;
        Console.WriteLine($"[Quantize] SnapDiv set to 1/{div}");
        UpdateQuantizeButtonHighlight(div);
        PianoRollCanvas.InvalidateSurface();
        PianoRollTickBackgroundCanvas.InvalidateSurface();
    }

    /// <summary>
    /// アクティブなクオンタイズボタンをハイライト表示する。
    /// </summary>
    private void UpdateQuantizeButtonHighlight(int activeDiv)
    {
        var buttons = new[] { (BtnSnap4, 4), (BtnSnap8, 8), (BtnSnap16, 16), (BtnSnap32, 32), (BtnSnapTriplet, 3) };
        foreach (var (btn, div) in buttons)
        {
            btn.BackgroundColor = (div == activeDiv)
                ? Color.FromArgb(ActiveQuantizeButtonArgb)
                : Colors.Transparent;
        }
    }

    #endregion
}
