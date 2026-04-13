#if ANDROID
// These tests compile only under net9.0-android because EditViewModel
// depends on MAUI types (DeviceDisplay, Rect, etc.).
//
// Build verification:  dotnet build OpenUtauMobile.Tests -f net9.0-android
// Desktop test run:    dotnet test  OpenUtauMobile.Tests -f net9.0  (skips these)
//
// Purpose:
//   API contract guard for Phase 3 additions:
//   - Vibrato editing methods (Step A)
//   - Phoneme editing methods (Step B)

using OpenUtauMobile.ViewModels;
using OpenUtau.Core.Ustx;
using Xunit;

namespace OpenUtauMobile.Tests.ViewModels;

public class EditViewModelPhase3Tests
{
    /// <summary>
    /// Verifies all 7 vibrato editing methods introduced in Phase 3 Step A exist.
    /// If any method is renamed or removed, the net9.0-android build fails immediately.
    /// </summary>
    [Fact]
    public void EditViewModel_HasVibratoMethods()
    {
        var type = typeof(EditViewModel);

        Assert.NotNull(type.GetMethod("GetVibratoForSelectedNote"));
        Assert.NotNull(type.GetMethod("ToggleVibratoForSelectedNotes"));
        Assert.NotNull(type.GetMethod("SetVibratoLength"));
        Assert.NotNull(type.GetMethod("SetVibratoDepth"));
        Assert.NotNull(type.GetMethod("SetVibratoPeriod"));
        Assert.NotNull(type.GetMethod("SetVibratoFadeIn"));
        Assert.NotNull(type.GetMethod("SetVibratoFadeOut"));
    }

    /// <summary>
    /// Verifies GetVibratoForSelectedNote returns UVibrato (nullable reference type;
    /// runtime ReturnType is the same underlying CLR type as UVibrato?).
    /// </summary>
    [Fact]
    public void GetVibratoForSelectedNote_ReturnsUVibratoType()
    {
        var method = typeof(EditViewModel).GetMethod("GetVibratoForSelectedNote");
        Assert.NotNull(method);
        Assert.Equal(typeof(UVibrato), method!.ReturnType);
    }

    /// <summary>
    /// Verifies all 5 phoneme parameter editing methods introduced in Phase 3 Step B exist.
    /// </summary>
    [Fact]
    public void EditViewModel_HasPhonemeMethods()
    {
        var type = typeof(EditViewModel);

        Assert.NotNull(type.GetMethod("SetPhonemeOffset"));
        Assert.NotNull(type.GetMethod("SetPhonemePreutter"));
        Assert.NotNull(type.GetMethod("SetPhonemeOverlap"));
        Assert.NotNull(type.GetMethod("SetPhonemeAlias"));
        Assert.NotNull(type.GetMethod("ClearPhonemeTimingForSelectedNotes"));
    }
}
#endif
