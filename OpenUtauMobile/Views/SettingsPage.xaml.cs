using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using OpenUtauMobile.ViewModels;
using OpenUtauMobile.Resources.Strings;
using OpenUtauMobile.Utils.Telemetry;
using Serilog;
using CommunityToolkit.Maui.Storage;
using ReactiveUI;
using System.Reactive.Disposables;

namespace OpenUtauMobile.Views;

public partial class SettingsPage : ContentPage, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private int _currentTabIndex = 0;
    private SettingsViewModel Viewmodel {  get; set; }
    private int CurrentTabIndex
    {
        get
        {
            return _currentTabIndex;
        }
        set
        {
            _currentTabIndex = value;
            UpdateTab();
        }
    }
    public SettingsPage()
	{
		InitializeComponent();
        Viewmodel = (SettingsViewModel)BindingContext;
        Viewmodel.WhenAnyValue(vm => vm.EnableAdditionalSingerPath).Subscribe(enable =>
        {
            if (!enable) // 关闭额外歌手路径
            {
                Viewmodel.AdditionalSingerPath = string.Empty;
            }
            else if (string.IsNullOrEmpty(Viewmodel.AdditionalSingerPath))// 启用额外歌手路径却未设置路径，弹出选择文件夹对话框
            {
                SetAdditionalSingerPath();
            }
        })
        .DisposeWith(_disposables);
    }

    protected override bool OnBackButtonPressed()
    {
        return true;
    }

    private void ButtonTab_Clicked(object sender, EventArgs e)
    {
        if (sender == ButtonTabEditAndBehavior)
        {
            CurrentTabIndex = 0;
        }
        else if (sender == ButtonTabRenderAndPerformance)
        {
            CurrentTabIndex = 1;
        }
        else if (sender == ButtonTabFileAndStorage)
        {
            CurrentTabIndex = 2;
        }
        else if (sender == ButtonTabAppearanceAndLanguage)
        {
            CurrentTabIndex = 3;
        }
        else if (sender == ButtonTabDiagnostics)
        {
            CurrentTabIndex = 4;
        }
    }

    private void UpdateTab()
    {
        switch (CurrentTabIndex)
        {
            case 0:
                GridEditAndBehavior.IsVisible = true;
                GridRenderAndPerformance.IsVisible = false;
                GridFileAndStorage.IsVisible = false;
                GridAppearanceAndLanguage.IsVisible = false;
                GridDiagnostics.IsVisible = false;
                break;
            case 1:
                GridEditAndBehavior.IsVisible = false;
                GridRenderAndPerformance.IsVisible = true;
                GridFileAndStorage.IsVisible = false;
                GridAppearanceAndLanguage.IsVisible = false;
                GridDiagnostics.IsVisible = false;
                break;
            case 2:
                GridEditAndBehavior.IsVisible = false;
                GridRenderAndPerformance.IsVisible = false;
                GridFileAndStorage.IsVisible = true;
                GridAppearanceAndLanguage.IsVisible = false;
                GridDiagnostics.IsVisible = false;
                break;
            case 3:
                GridEditAndBehavior.IsVisible = false;
                GridRenderAndPerformance.IsVisible = false;
                GridFileAndStorage.IsVisible = false;
                GridDiagnostics.IsVisible = false;
                GridAppearanceAndLanguage.IsVisible = true;
                break;
            case 4:
                GridEditAndBehavior.IsVisible = false;
                GridRenderAndPerformance.IsVisible = false;
                GridFileAndStorage.IsVisible = false;
                GridAppearanceAndLanguage.IsVisible = false;
                GridDiagnostics.IsVisible = true;
                break;
        }
    }

    private void ButtonCancel_Clicked(object sender, EventArgs e)
    {
        Navigation.PopModalAsync();
    }

    private void ButtonSave_Clicked(object sender, EventArgs e)
    {
        Save();
    }

    private void Save()
    {
        try
        {
            Viewmodel.Save();
            Toast.Make(AppResources.SettingsSavedToast, ToastDuration.Short).Show();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "保存设置时出现未处理异常");
            Toast.Make(AppResources.SettingsSaveErrorToast, ToastDuration.Short).Show();
        }
    }

    private void ButtonConfirm_Clicked(object sender, EventArgs e)
    {
        Save();
        Navigation.PopModalAsync();
    }

    private void ButtonSelectAdditionalSingerPath_Clicked(object sender, EventArgs e)
    {
        SetAdditionalSingerPath();
    }


    private async void ButtonExportSupportBundle_Clicked(object sender, EventArgs e)
    {
        try
        {
            string zipPath = await Task.Run(() => TelemetryService.Inst.ExportSupportBundle());
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = AppResources.ExportSupportBundle,
                File = new ShareFile(zipPath),
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "サポートバンドルのエクスポートに失敗");
            await Toast.Make(AppResources.ExportSupportBundleErrorToast, ToastDuration.Short).Show();
        }
    }

    /// <summary>
    /// 选择额外歌手路径
    /// </summary>
    private async void SetAdditionalSingerPath()
    {
        FolderPickerResult result = await FolderPicker.Default.PickAsync();
        if (result.IsSuccessful)
        {
            string folderPath = result.Folder.Path;
            if (Viewmodel.AdditionalSingerPath != folderPath)
            {
                Viewmodel.AdditionalSingerPath = folderPath;
            }
        }
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Dispose();
    }
}