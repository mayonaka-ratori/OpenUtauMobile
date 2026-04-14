using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Views;
using DynamicData.Binding;
using OpenUtauMobile.Views.Controls;
using OpenUtauMobile.Resources.Strings;
using OpenUtauMobile.Utils.Permission;
using OpenUtauMobile.ViewModels;
using System.Threading.Tasks;
using OpenUtau.Core;
using OpenUtau.Core.Format;
using OpenUtauMobile.Utils;

namespace OpenUtauMobile.Views;

public partial class HomePage : ContentPage
{
    private HomePageViewModel _viewModel;
    private bool _isExit = false; // 退出标志
    private bool _recoveryCheckDone = false; // 復元チェックは起動時に一度だけ実行
    public HomePage()
	{
		InitializeComponent();
        _viewModel = (HomePageViewModel)BindingContext;
    }

    /// <summary>
    /// 重写返回键按下事件，实现再次按下返回键退出应用
    /// </summary>
    /// <returns></returns>
    protected override bool OnBackButtonPressed()
    {
        if (_isExit)
        {
            Application.Current?.Quit();
        }
        else
        {
            _isExit = true;

            Task.Run(async () =>
            {
                await Task.Delay(2000); // 2秒
                _isExit = false;
            });

            Toast.Make(AppResources.StringPressBackAgainToExit, CommunityToolkit.Maui.Core.ToastDuration.Short, 16).Show(); // toast显示提示
        }

        return true;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.RecentProjectsPaths = new ObservableCollectionExtended<string>(OpenUtau.Core.Util.Preferences.Default.RecentFiles);
        await CheckAndOfferRecovery();
    }

    private async Task CheckAndOfferRecovery()
    {
        if (_recoveryCheckDone) return;
        _recoveryCheckDone = true;

        string recoveryPath = OpenUtau.Core.Util.Preferences.Default.RecoveryPath;
        if (string.IsNullOrEmpty(recoveryPath) || !File.Exists(recoveryPath)) return;

        var popup = new RecoveryPopup();
        var result = await this.ShowPopupAsync(popup);

        if (result is RecoveryChoice choice)
        {
            if (choice == RecoveryChoice.Discard)
            {
                OpenUtau.Core.Util.Preferences.Default.RecoveryPath = string.Empty;
                OpenUtau.Core.Util.Preferences.Save();
            }
            else
            {
                bool safeMode = choice == RecoveryChoice.RecoverSafe;
                await Navigation.PushModalAsync(new EditPage(recoveryPath, recovered: true, safeMode: safeMode), false);
            }
        }
    }


    private async void ButtonNewProject_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new EditPage(string.Empty), false);
    }
    /// <summary>
    /// 支持的工程文件后缀名
    /// </summary>
    public string[] ProjectFileSuffix { get; } = [".ustx", ".vsqx", ".ust", ".mid", ".midi", ".ufdata", ".musicxml"];
    private async void ButtonOpenProject_Clicked(object sender, EventArgs e)
    {
        string projectPath = await ObjectProvider.PickFile(ProjectFileSuffix, this);
        if (!string.IsNullOrEmpty(projectPath))
        {
            await Navigation.PushModalAsync(new EditPage(projectPath), false);
        }
    }

    private void ButtonOpenSingerManage_Clicked(object sender, EventArgs e)
    {
        Navigation.PushModalAsync(new SingerManagePage()); // 跳转到歌手管理页面
    }

    private void ButtonOpenOptions_Clicked(object sender, EventArgs e)
    {
        Navigation.PushModalAsync(new OptionsPage()); // 跳转到选项页面
    }

    /// <summary>
    /// 打开最近的工程
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ButtonOpenRecent_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                return;
            }
            if (!File.Exists(projectPath))
            {
                Toast.Make(AppResources.FileNotFoundToast, CommunityToolkit.Maui.Core.ToastDuration.Short, 16).Show();
                return;
            }
            foreach (string suffix in ProjectFileSuffix)
            {
                if (projectPath.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) // 忽略大小写比较
                {
                    Navigation.PushModalAsync(new EditPage(projectPath), false);
                    return;
                }
            }
            Toast.Make(AppResources.IncorrectProjectFileToast, CommunityToolkit.Maui.Core.ToastDuration.Short, 16).Show();
        }
    }
}