using CommunityToolkit.Maui.Views;

namespace OpenUtauMobile.Views.Controls;

public enum RecoveryChoice { Recover, RecoverSafe, Discard }

public partial class RecoveryPopup : Popup
{
    public RecoveryPopup()
    {
        InitializeComponent();
    }

    private void ButtonRecover_Clicked(object sender, EventArgs e)
        => CloseAsync(RecoveryChoice.Recover);

    private void ButtonSafe_Clicked(object sender, EventArgs e)
        => CloseAsync(RecoveryChoice.RecoverSafe);

    private void ButtonDiscard_Clicked(object sender, EventArgs e)
        => CloseAsync(RecoveryChoice.Discard);
}
