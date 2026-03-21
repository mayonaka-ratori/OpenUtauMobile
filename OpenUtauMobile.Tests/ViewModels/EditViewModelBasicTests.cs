#if ANDROID
// These tests compile only under net9.0-android because EditViewModel
// depends on MAUI types (DeviceDisplay, Rect, etc.).
//
// Build verification:  dotnet build OpenUtauMobile.Tests -f net9.0-android
// Desktop test run:    dotnet test  OpenUtauMobile.Tests -f net9.0  (skips these)
//
// Running on device/emulator requires additional setup (see Phase 2.5 notes).

using OpenUtauMobile.ViewModels;
using Xunit;

namespace OpenUtauMobile.Tests.ViewModels;

public class EditViewModelBasicTests
{
    [Fact]
    public void EditViewModel_Type_Is_Accessible()
    {
        // Proves the test project can reference EditViewModel
        Assert.NotNull(typeof(EditViewModel));
    }

    [Fact]
    public void TrackEditMode_Enum_Has_Expected_Values()
    {
        Assert.Equal(0, (int)TrackEditMode.Normal);
        Assert.Equal(1, (int)TrackEditMode.Edit);
    }

    [Fact]
    public void NoteEditMode_Enum_Has_Expected_Values()
    {
        Assert.Equal(0, (int)NoteEditMode.EditNote);
        Assert.Equal(1, (int)NoteEditMode.EditPitchCurve);
        Assert.Equal(2, (int)NoteEditMode.EditPitchAnchor);
        Assert.Equal(3, (int)NoteEditMode.EditVibrato);
    }
}
#endif
