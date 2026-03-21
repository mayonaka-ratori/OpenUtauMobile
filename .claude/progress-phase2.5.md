# OpenUtau Mobile — Phase 2.5 Progress (Refactoring)

**Status**: 🔄 IN PROGRESS (2026-03-21〜)
**Goal**: EditPage.xaml.cs 分割・UndoScope 移行・テストアーキテクチャ確立
**Phase 2 Final**: see `.claude/progress-phase2.md`

---

## Phase A+B: Additive + Code Movement (COMPLETE)

| Step | Task | Status | Commit |
|------|------|--------|--------|
| 1 | UndoScope guard class + BUG-D 記録 | ✅ | `3742545` |
| 2 | Extract edit-mode enums → EditModes.cs | ✅ | `47db2c1` |
| 3 | Extract PaintSurface handlers → EditPage.Rendering.cs | ✅ | `d79e3af` |
| 4 | Extract button handlers → EditPage.Toolbar.cs | ✅ | `4642b26` |
| 5 | Extract OnNext → EditPage.CmdSubscriber.cs | ✅ | `fc41d47` |
| Audit | 0 critical / 0 warnings / 4 info (全解決) | ✅ | — |
| Cleanup | 39 unused usings 削除 + SelectionMode 完全修飾 | ✅ | `060692b` |

**EditPage.xaml.cs**: 3,640 → 1,694 行 (53% 削減)
**Partial class files**: 4本 (xaml.cs + Rendering.cs + Toolbar.cs + CmdSubscriber.cs)

---

## STOP GATE 1: Test Architecture — RESOLVED

Decision: **Option B** (multi-target TFM) — commit `4e8d109`
- `net9.0`: デスクトップ実行（`dotnet test` で使用）
- `net9.0-android`: MAUI 型ビルド検証（`#if ANDROID` 条件コンパイル）

---

## Phase C: Safety Net (COMPLETE)

| Step | Task | Status | Commit |
|------|------|--------|--------|
| 6 | Test project multi-target (net9.0 + net9.0-android) | ✅ | `4e8d109` |
| 7 | 6 undo 特性テスト (UndoScope + enum 安定性) | ✅ | `988d6b3` |

**テスト合計**: 12/12 pass (net9.0) / Android build-verified

---

## Phase D: Undo Migration (IN PROGRESS)

| Step | Task | Status | Commit |
|------|------|--------|--------|
| 8a | 22 simple 隣接ペア変換 (gap ≤5, error path なし) | ✅ | `7eba112` |
| 8b | 7 spanning pairs (field-based) + 2 single-method (using var) | ✅ | `12de33f` |
| 8c | 4 error-path サイト (StartCreatePart / RemoveSelectedParts / CreateDefaultNote / PasteNotes) | ⏳ PENDING | — |
| 8d | ForceEndAllInteractions orphaned calls → UndoScope.TryEnd() | ⏳ PENDING | — |

### Undo 移行進捗

| 分類 | 件数 | 状態 |
|------|------|------|
| Simple 隣接ペア (8a) | 22 | ✅ 変換済み |
| Spanning field-based (8b) | 7 | ✅ 変換済み |
| Single-method long-gap (8b) | 2 | ✅ 変換済み |
| Error-path (8c) | 11 | ⏳ PENDING |
| Orphaned (8d) | 2 | ⏳ PENDING |
| **合計** | **44** | **31/44 変換済み** |

**8b で追加したフィールド**:
`_movePartsUndoScope`, `_resizePartUndoScope`, `_resizeNotesUndoScope`,
`_moveNotesUndoScope`, `_drawPitchUndoScope`, `_drawExpressionUndoScope`, `_resetExpressionUndoScope`

---

## 次セッションのアクション

1. **Step 8c** — error-path ペアの変換
   - `StartCreatePart` / catch / `EndCreatePart` (3呼び出し)
   - `RemoveSelectedParts` / catch / End (3呼び出し)
   - `CreateDefaultNote` / catch / End (3呼び出し)
   - `PasteNotes` (try/catch 後に End) (2呼び出し)
2. **Step 8d** — `ForceEndAllInteractions` orphaned → `UndoScope.TryEnd()`
3. Phase 2.5 **COMPLETE** → Phase 3 計画

---

## Phase 2.5 Decision Log

| Date | Decision |
|------|---------|
| 2026-03-21 | Phase A+B 完了。EditPage 4 partial files 分割。独立監査 0 critical。SelectionMode 完全修飾で名前衝突回避。 |
| 2026-03-21 | STOP GATE 1 解決: Option B (multi-target TFM) — net9.0 実行 + net9.0-android ビルド検証の分離。`#if ANDROID` 条件コンパイルで MAUI 依存テストを分離。 |
| 2026-03-21 | Phase 2.5 Steps 1-8b を単一セッションで完了。31 undo サイト変換済み。残り 13 呼び出しは error-path (8c) および orphaned (8d) — catch 節含むため高リスク、次セッションに延期。 |
