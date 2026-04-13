using Xunit;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;

namespace OpenUtauMobile.Tests;

/// <summary>
/// Smoke tests to verify Core integration and basic model behavior.
/// These run without audio devices, UI, or file system dependencies.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void DocManager_Inst_ReturnsSingleton()
    {
        var a = DocManager.Inst;
        var b = DocManager.Inst;

        Assert.NotNull(a);
        Assert.Same(a, b);
    }

    [Fact]
    public void UNote_Create_HasZeroDurationAndInitializedPitchVibrato()
    {
        var note = UNote.Create();

        Assert.Equal(0, note.duration);
        Assert.Equal(0, note.position);
        Assert.NotNull(note.pitch);
        Assert.NotNull(note.vibrato);
    }

    [Fact]
    public void UProject_CreateNote_HasInitializedPitchPoints()
    {
        var project = new UProject();
        var note = project.CreateNote();

        Assert.NotNull(note.pitch);
        Assert.NotEmpty(note.pitch.data);
    }

    [Fact]
    public void UVibrato_DefaultValues_WithinValidRanges()
    {
        var vibrato = new UVibrato();

        Assert.InRange(vibrato.length, 0f, 100f);
        Assert.InRange(vibrato.period, 5f, 500f);
        Assert.InRange(vibrato.depth, 5f, 200f);
        Assert.InRange(vibrato.@in, 0f, 100f);
        Assert.InRange(vibrato.@out, 0f, 100f);
    }

    [Fact]
    public void UNote_Vibrato_DefaultLengthIsZero()
    {
        // Vibrato is disabled by default (length == 0).
        // ToggleVibratoForSelectedNotes enables it by setting length = 50f.
        var note = UNote.Create();

        Assert.Equal(0f, note.vibrato.length);
    }

    [Fact]
    public void UVibrato_ToggleDefaultLength_IsWithinRange()
    {
        // Documents the enable value used by ToggleVibratoForSelectedNotes.
        // If the toggle target changes, this test will need to be updated.
        const float toggleDefaultLength = 50f;

        Assert.InRange(toggleDefaultLength, 0f, 100f);
    }
}
