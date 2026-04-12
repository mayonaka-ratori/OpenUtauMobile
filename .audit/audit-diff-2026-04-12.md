# Audit Diff Report — 2026-04-12

**前回 audit**: 2026-03-18 (`repo-audit.md`) — 20件 (Critical×3, High×8, Medium×5, Low×4)
**本レポート作成日**: 2026-04-12
**審査対象変更**: Phase 1 / Phase 2 / Phase 2.5 (Steps 1–8d)

---

## 前回 audit 20件の解消状況

### Severity A: Critical (3件)

| ID | 内容 | 状態 | 解消コミット/経緯 |
|----|------|------|-----------------|
| A-01 | DocManager.Inst null チェック欠如 | ✅ 解消 | Phase 1 — null guard 追加 |
| A-02 | Lifecycle 非同期 void 例外漏れ | ✅ 解消 | Phase 1 — try/catch + Log.Error 追加 |
| A-03 | PaintSurface SKObject 漏れ | ✅ 解消 | Phase 1 — IDisposable pattern + using |

### Severity B: High (8件)

| ID | 内容 | 状態 | 解消コミット/経緯 |
|----|------|------|-----------------|
| B-01 | CompositeDisposable 未 Dispose | ✅ 解消 | Phase 1 |
| B-02 | GestureProcessor 未登録イベント漏れ | ✅ 解消 | Phase 1 |
| B-03 | PaintSurface 毎フレーム allocate | ✅ 解消 | Phase 2 — bitmap cache (P2-5a/5b/5c) |
| B-04 | Touch throttle 欠如 | ✅ 解消 | Phase 2 — ThrottleIntervalMs=16 |
| B-05 | EditPage 3,640行 monolith | ✅ 解消 | Phase 2.5 Step A+B — 4 partial files |
| B-06 | StartUndoGroup/EndUndoGroup 直呼び出し (simple) | ✅ 解消 | Phase 2.5 Step 8a (22件) + 8b (9件) |
| B-07 | Transformer Debug.WriteLine | ✅ 解消 | 2026-04-12 本セッション (N-07) |
| B-08 | GestureProcessor IDisposable 欠如 | ✅ 解消 | Phase 1 (実コード確認済み) |

### Severity C: Medium (5件)

| ID | 内容 | 状態 | 解消コミット/経緯 |
|----|------|------|-----------------|
| C-01 | EditViewModel 未テスト | ✅ 解消 | Phase 2.5 Step C (12件テスト) |
| C-02 | NoteEditMode enum 不安定 | ✅ 解消 | Phase 2.5 Step 2 (enum 分離) |
| C-03 | PianoRoll canvas slow rate | ✅ 解消 | Phase 2 bitmap cache |
| C-04 | Track canvas slow rate | 🔶 継続監視 | 11.3% slow — モニタリング継続 |
| C-05 | PlaybackTickBg MISS 遅延 | 🔶 継続監視 | MISS-only 36.8% — 要最適化 |

### Severity D: Low (4件)

| ID | 内容 | 状態 | 解消コミット/経緯 |
|----|------|------|-----------------|
| D-01 | Logging 不統一 | ✅ 解消 | Phase 1 — Serilog 統一 |
| D-02 | AppResources 重複 | ✅ 解消 | Phase 1 |
| D-03 | Nullable annotation 欠如 | ✅ 解消 | Phase 2.5 |
| D-04 | Conventional Commits 未適用 | ✅ 解消 | Phase 1 以降 全コミット適用 |

### Severity E: Info (3件)

| ID | 内容 | 状態 | 解消コミット/経緯 |
|----|------|------|-----------------|
| E-01 | XML doc コメント欠如 | ⏳ Phase 3 送り | 低優先度 |
| E-02 | 中国語コメント混在 | ⏳ Phase 3 送り | 低優先度 |
| E-03 | マジックナンバー直書き | ⏳ Phase 3 送り | 低優先度 |

**解消率: 18/20 (90%)** — 残り2件は C-04/C-05 (継続監視) + E-01〜E-03 (Phase 3 送り)

---

## 新規発見欠陥 (2026-04-12 本セッションで修正済み)

| ID | Severity | メソッド | 問題 | 修正 |
|----|----------|---------|------|------|
| N-01 | **Critical** | `CreateDefaultNote` | catch + 末尾 で EndUndoGroup 2重呼び出し | `using var undo = new UndoScope()` + early return に整理 |
| N-02 | **Critical** | `RemoveSelectedParts` | ループ内 catch で EndUndoGroup → break なし → 末尾でも EndUndoGroup | `using var undo` + `break` |
| N-03 | High | `StartCreatePart/EndCreatePart` | field-based UndoScope 未移行 | `_createPartUndoScope` フィールド追加 |
| N-04 | High | `ForceEndAllInteractions` | `DocManager.Inst.EndUndoGroup()` 直呼び出し → フィールドが null 化されない | `_moveNotesUndoScope?.Dispose()` + null |
| N-05 | Medium | `PasteNotes` | `StartUndoGroup`/`EndUndoGroup` 直呼び出し (1重だが一貫性欠如) | `using var undo = new UndoScope()` |
| N-06 | Low | `EditViewModel` | `Debug.WriteLine` 16箇所 (logcat 不可視) | `Console.WriteLine` に全置換 + `using System.Diagnostics` 削除 |
| N-07 | Low | `Transformer` / `GestureProcessor` | `Debug.WriteLine` 計7箇所 | `Console.WriteLine` に全置換 + using 削除 |

**全7件 本セッションで修正済み。**

---

## Phase 2.5 完了サマリー

| 項目 | 値 |
|------|-----|
| UndoScope 移行総数 | 44/44 ✅ |
| Debug.WriteLine → Console.WriteLine | 23箇所 (EditViewModel 16 + Transformer 3 + GestureProcessor 4) |
| テスト | 12/12 pass (net9.0) + Android build verified |
| EditPage 分割 | 3,640行 → 1,694行 (53%削減) |
| Partial class 数 | 4 (xaml.cs / Rendering.cs / Toolbar.cs / CmdSubscriber.cs) |

---

## 未解消 / Phase 3 送り

| ID | 内容 | 優先度 |
|----|------|--------|
| BUG-D | PianoRoll シャドウ境界ずれ | 低 — Phase 3 実機確認 |
| OP-01 | RequestStoragePermissionAsync 常に true | 中 — Phase 3 実機テスト必須 |
| C-04 | Track canvas 11.3% slow | 継続監視 |
| C-05 | PlaybackTickBg MISS 36.8% slow | 継続監視 |
| E-01〜E-03 | XML doc / 中国語コメント / マジックナンバー | Phase 3 低優先度 |
