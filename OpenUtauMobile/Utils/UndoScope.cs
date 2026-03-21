using OpenUtau.Core;
namespace OpenUtauMobile.Utils;
/// <summary>
/// Wraps DocManager.Inst.StartUndoGroup() / EndUndoGroup() in a
/// disposable scope so that 'using var undo = new UndoScope();'
/// guarantees the group is closed — even if an exception occurs.
/// </summary>
internal sealed class UndoScope : IDisposable
{
    private bool _disposed;
    public UndoScope()
    {
        DocManager.Inst.StartUndoGroup();
    }
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            DocManager.Inst.EndUndoGroup();
        }
    }
    /// <summary>
    /// Attempts to end an undo group that was started elsewhere.
    /// Used by safety-net cleanup methods (e.g., ForceEndAllInteractions)
    /// that close groups they did not open.
    /// </summary>
    public static void TryEnd()
    {
        DocManager.Inst.EndUndoGroup();
    }
}
