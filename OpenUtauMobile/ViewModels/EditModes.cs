// Extracted from EditViewModel.cs (Phase 2.5 Step 2)
// These enum types were previously nested inside EditViewModel class.
// Moved to standalone file to reduce EditViewModel.cs size and improve navigability.
namespace OpenUtauMobile.ViewModels;

public enum TrackEditMode // 定义走带编辑模式（枚举类型）
{
    // 只读模式
    Normal,
    // 编辑模式
    Edit,
};

public enum NoteEditMode // 定义音符编辑模式（枚举类型）
{
    // 只读模式
    // Normal,
    // 音符编辑模式
    EditNote,
    // 音高曲线编辑模式
    EditPitchCurve,
    // 音高锚点编辑模式
    EditPitchAnchor,
    // 颤音编辑模式
    EditVibrato,
};

public enum ExpressionEditMode // 定义表达式编辑模式（枚举类型）
{
    // 只读模式
    Hand,
    // 编辑模式
    Edit,
    // 橡皮擦模式
    Eraser,
};

public enum SelectionMode
{
    Single,
    Multi,
}
