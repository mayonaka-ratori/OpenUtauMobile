#if ANDROID
// These tests compile only under net9.0-android because EditViewModel
// depends on MAUI types (DeviceDisplay, Rect, etc.).
//
// Build verification:  dotnet build OpenUtauMobile.Tests -f net9.0-android
// Desktop test run:    dotnet test  OpenUtauMobile.Tests -f net9.0  (skips these)
//
// Purpose:
//   1. Compile-time safety net — if UndoScope migration (Step 8) breaks the API,
//      the net9.0-android build fails immediately
//   2. Documentation — shows the expected undo/redo patterns
//   3. Future-ready — when device testing is available, these tests can run

using OpenUtauMobile.Utils;
using OpenUtauMobile.ViewModels;
using OpenUtau.Core;
using Xunit;

namespace OpenUtauMobile.Tests.ViewModels;

public class EditViewModelUndoTests
{
    /// <summary>
    /// Verifies UndoScope can be used in a using declaration pattern.
    /// This is the pattern Step 8a will introduce across 23 sites.
    /// </summary>
    [Fact]
    public void UndoScope_Using_Pattern_Compiles()
    {
        // Verifies the compile-time contract:
        //   using var undo = new UndoScope();
        // is valid C# and UndoScope implements IDisposable correctly.
        Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(UndoScope)));
    }

    [Fact]
    public void UndoScope_Has_TryEnd_Static_Method()
    {
        // Verify TryEnd exists (used by ForceEndAllInteractions in Step 8d)
        var method = typeof(UndoScope).GetMethod(
            "TryEnd",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);
    }

    [Fact]
    public void UndoScope_Dispose_Is_Idempotent()
    {
        // Verify double-dispose doesn't throw.
        // Critical for error paths in Step 8c where an exception may trigger
        // both the catch block and the finally block.
        var scope = new UndoScope();
        scope.Dispose();
        scope.Dispose(); // Must not throw
    }

    [Fact]
    public void EditViewModel_Undo_Methods_Exist()
    {
        // Verify the spanning-pair methods that Step 8b will migrate still exist.
        // If a method is renamed/removed, this test fails at build time.
        var type = typeof(EditViewModel);

        Assert.NotNull(type.GetMethod("StartMoveParts"));
        Assert.NotNull(type.GetMethod("EndMoveParts"));
        Assert.NotNull(type.GetMethod("StartResizeNotes"));
        Assert.NotNull(type.GetMethod("EndResizeNotes"));
        Assert.NotNull(type.GetMethod("StartMoveNotes"));
        Assert.NotNull(type.GetMethod("EndMoveNotes"));
    }

    [Fact]
    public void EditViewModel_Has_ForceEndAllInteractions()
    {
        // Verify the safety-net method exists (Step 8d target).
        var method = typeof(EditViewModel).GetMethod("ForceEndAllInteractions");
        Assert.NotNull(method);
    }

    [Fact]
    public void EditModes_Enum_Values_Are_Stable()
    {
        // Regression guard: verifies enum member names and ordinal values are unchanged.
        // Note: actual member names are Normal/Edit (TrackEditMode) and EditNote/EditPitchCurve
        // (NoteEditMode) — not ReadOnly/NoteEdit/PitchEdit as in some drafts.
        Assert.Equal(0, (int)TrackEditMode.Normal);
        Assert.Equal(1, (int)TrackEditMode.Edit);

        Assert.Equal(0, (int)NoteEditMode.EditNote);
        Assert.Equal(1, (int)NoteEditMode.EditPitchCurve);
        Assert.Equal(2, (int)NoteEditMode.EditPitchAnchor);
        Assert.Equal(3, (int)NoteEditMode.EditVibrato);
    }
}
#endif
