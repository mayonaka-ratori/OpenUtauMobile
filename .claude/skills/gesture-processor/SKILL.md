# GestureProcessor Skill Document

**Files:** `Views/Utils/GestureProcessor.cs` (451行) / `Views/Utils/TouchEventArgs.cs` (90行)
**Last Updated:** 2026-03-20

---

## Section 1: State Machine

**States:** `None | Tap | DoubleTap | Pan | Zoom | XZoom | YZoom`
**Field:** `_currentState` (L23) — internal enum, not exposed publicly.

| From | Trigger | To | Action | Line |
|------|---------|----|--------|------|
| None | Pressed (1st finger) | None | `_hasPanStarted=false`, `StartGestureDetection()` (stub) | L105–129 |
| None | Pressed (2nd finger) | Zoom/XZoom/YZoom | `CancelCurrentGesture()`, `InitializeZoomGesture()` | L131–135 |
| None | Pressed (3rd+ finger) | None | ID queued in `_pointQueue`, no state change | L112–116 |
| None | Moved ≥5px (single) | Pan | `_hasPanStarted=true`, fire `PanStart` | L255–259 |
| None | Released (short, <5px, <300ms) | Tap | Fire `Tap`, record `_lastTapPosition/Time` | L169–208 |
| None | Released (short, near prev tap, <300ms) | DoubleTap | Fire `DoubleTap`, clear `_lastTapPosition` | L175–188 |
| None | Released (long or >5px) | None | `FinalizeGesture()` (no event) | L232–234 |
| Pan | Moved (single, throttled 16ms) | Pan | Fire `PanUpdate` | L262–266 |
| Pan | Released (last finger) | None | Fire `PanEnd`, `FinalizeGesture()` → `_transformer.EndPan()` | L223–226, L232–234 |
| Pan | Cancelled (last finger) | None | Fire `PanEnd(e.Location)`, `FinalizeGesture()` | L85–88 |
| Zoom/XZoom/YZoom | Moved (2 fingers, throttled) | same | Fire `ZoomUpdate/XZoomUpdate/YZoomUpdate` | L312–322 |
| Zoom/XZoom/YZoom | Released (one of 2 fingers) | Pan | `SwitchToPanFromZoom()` → Fire `PanStart(LastPosition)` | L236–238 |
| Zoom/XZoom/YZoom | Cancelled (one finger) | Pan | `SwitchToPanFromZoom()` | L89–91 |
| Zoom/XZoom/YZoom | Released (last finger) | None | `FinalizeGesture()` (no PanEnd) | L232–234 |
| Any | `ForceReset()` called | None | Fire `PanEnd(SKPoint.Empty)` if was Pan; clear all state | L358–369 |
| None | Pressed, `_activePoints > 0` (stale) | None | `_activePoints.Clear()`, `_hasPanStarted=false` before proceeding | L105–109 |

**`InitializeZoomGesture` routing (L270–310):**
- `deltaX < 200 && deltaY < 200` → **no state change** (両指近接すぎ、無視)
- `deltaX < 200` only → **YZoom**, fire `YZoomStart`
- `deltaY < 200` only → **XZoom**, fire `XZoomStart`
- both ≥ 200 → **Zoom**, fire `ZoomStart`

**`FinalizeGesture` (L392–409):**
- Pan → `_transformer.EndPan()`
- Zoom/XZoom/YZoom → (no-op, EndZoom commented out)
- Always: `_currentState = None`, `_hasPanStarted = false`

---

## Section 2: Touch Event Flow

**Entry point:** `ProcessTouch(sender, e)` L49 — sets `e.Handled = true` always (L70).

### Pressed → `HandleTouchDown` (L99)
```
Pressed received
  → _activePoints.Count > 0 && state == None?
      YES → _activePoints.Clear(), _hasPanStarted=false  [stale cleanup, L105-109]
  → _activePoints.Count >= 2?
      YES → _pointQueue.Enqueue(id); return              [3rd+ finger queued, L112-116]
  → _activePoints[id] = new TouchPoint(...)              [upsert, L119]
  → _hasPanStarted = false                               [L122]
  → Count == 1?
      YES → _lastSinglePoint = location; StartGestureDetection() [STUB, L125-129]
  → Count == 2?
      YES → CancelCurrentGesture(); InitializeZoomGesture()      [L131-135]
```

### Moved → `HandleTouchMove` (L142)
```
Moved received
  → _activePoints.TryGetValue(id)?  NO → return          [L144]
  → point.Update(location)                               [L146]
  → Count == 1 → HandleSingleTouchMove(point)
      → delta.LengthSquared >= 25 (≥5px)?
          NO  → return (waiting)
          YES → _hasPanStarted = true
              → state == None?
                  YES → CancelCurrentGesture(); state=Pan; fire PanStart(LastPos, StartPos)  [L255-259]
              → state == Pan?
                  YES → ShouldThrottle(16ms)? return; fire PanUpdate(LastPos)               [L262-266]
  → Count == 2 → HandleZoomGestureMove()
      → ShouldThrottle(16ms)? return
      → state == XZoom → fire XZoomUpdate
      → state == YZoom → fire YZoomUpdate
      → state == Zoom  → fire ZoomUpdate
```

### Released → `HandleTouchUp` (L164)
```
Released received
  → _activePoints.TryGetValue(id)?
      YES →
        → !_hasPanStarted && delta<5px && state==None && duration<300ms?
            → _lastTapPosition != null && time diff ≤ 300ms?
                → distance ≤ 20px? → state=DoubleTap; fire DoubleTap; clear lastTap  [L184-188]
                → distance > 20px? → state=Tap; fire Tap; update lastTap             [L193-197]
            → else → state=Tap; fire Tap; update lastTap                              [L203-207]
        → _activePoints.Remove(id)                                                    [L213]
        → _pointQueue.Count > 0 → dequeue → add synthetic TouchPoint                 [L216-220]
        → state == Pan? → fire PanEnd(LastPos)                                        [L223-226]
  → _activePoints.Count == 0 → FinalizeGesture()                                     [L232-234]
  → Count == 1 && state in Zoom/XZoom/YZoom → SwitchToPanFromZoom()                  [L236-238]
```

### Cancelled → `HandleTouchCancel` (L77)
```
Cancelled received
  → _activePoints.Remove(id)      [L80]
  → _pointQueue.Clear()           [L81]
  → Count == 0?
      → state == Pan? fire PanEnd(e.Location)   [L85-86]
      → FinalizeGesture()                        [L87]
  → Count == 1 && state in Zoom/XZoom/YZoom → SwitchToPanFromZoom()  [L89-91]
```

---

## Section 3: Event API

| Event | EventArgs | 発火タイミング | PianoRoll handler (EditPage行) |
|-------|-----------|--------------|-------------------------------|
| `Tap` | `TapEventArgs { Position }` | Released: delta<5px, duration<300ms, 2nd tap distant/none | L728 |
| `DoubleTap` | `TapEventArgs { Position }` | Released: 2nd tap within 300ms & 20px of 1st | L783 |
| `PanStart` | `PanStartEventArgs { StartPosition, OriginalTouchDown }` | Moved: 1st time delta≥5px in None state | L812 |
| `PanUpdate` | `PanUpdateEventArgs { Position }` | Moved: delta≥5px, state==Pan, throttled 16ms | L871 |
| `PanEnd` | `PanEndEventArgs { EndPosition }` | Released/Cancelled: last finger up while Pan | L914 |
| `ZoomStart` | `ZoomStartEventArgs { Point1, Point2 }` | 2nd Pressed: both axes ≥200px apart | L968 |
| `ZoomUpdate` | `ZoomUpdateEventArgs { Point1, Point2 }` | Moved: 2 fingers, state==Zoom, throttled 16ms | L973 |
| `XZoomStart` | `ZoomStartEventArgs { Point1, Point2 }` | 2nd Pressed: only X axis ≥200px | L979 |
| `XZoomUpdate` | `ZoomUpdateEventArgs { Point1, Point2 }` | Moved: 2 fingers, state==XZoom, throttled 16ms | L984 |
| `YZoomStart` | `ZoomStartEventArgs { Point1, Point2 }` | 2nd Pressed: only Y axis ≥200px | L990 |
| `YZoomUpdate` | `ZoomUpdateEventArgs { Point1, Point2 }` | Moved: 2 fingers, state==YZoom, throttled 16ms | L995 |

**`PanStartEventArgs` の2プロパティ:**
- `StartPosition` = `point.LastPosition` (PanStart 発火時点の位置 — 5px ドリフト後)
- `OriginalTouchDown` = `point.StartPosition` (最初の Pressed 位置 — ヒットテスト用)

**`Dispose()` (L375):** 全イベントを `null` クリア (invocation list 解放)。EditPage.Dispose() から呼ぶ。

---

## Section 4: PianoRoll Touch Decision Tree

```
Touch on PianoRollCanvas / PianoRollPitchCanvas
  → PianoRollCanvas_Touch (EditPage L1869) / PianoRollPitchCanvas は直接転送
  → _pianoRollGestureProcessor.ProcessTouch(sender, e)

  [Pressed]
    → HandleTouchDown: _activePoints に登録。state=None 維持。

  [Moved — 1 finger]
    → delta < 5px: 何もしない（tap waiting）
    → delta ≥ 5px: PanStart 発火
        → EditPage PanStart handler (L812)
            → state == EditNote && SelectedNotes.Count > 0 && EditingNotes != null:
                var originalLogical = PianoRollTransformer.ActualToLogical(e.OriginalTouchDown)
                → IsPointInHandle(originalLogical)?
                    YES → StartResizeNotes(originalLogical, note); return  [BUG-B 修正済]
                → IsPointInNote(originalLogical) && note in SelectedNotes?
                    YES → StartMoveNotes(originalLogical); return          [BUG-A 修正済]
                → else → PianoRollTransformer.StartPan(e.StartPosition)
            → state == EditNote && no selection:
                → PianoRollTransformer.StartPan(e.StartPosition)
            → state == EditPitchCurve:
                → StartDrawPitch(e.StartPosition); IsUserDrawingCurve=true
            → state == EditPitchAnchor:
                → (独自ハンドリング)
            → state == EditVibrato:
                → (独自ハンドリング)
    → PanUpdate 発火 (throttled 16ms)
        → EditPage PanUpdate handler (L871)
            → IsMovingNotes?  → UpdateMoveNotes(ActualToLogical(e.Position))
            → IsResizingNote? → UpdateResizeNotes(ActualToLogical(e.Position))
            → else (pan):     → PianoRollTransformer.UpdatePan(e.Position)
            → IsUserDrawingCurve? → UpdateDrawPitch(e.Position)

  [Moved — 2 fingers]
    → HandleZoomGestureMove: ZoomUpdate/XZoomUpdate/YZoomUpdate 発火
        → PianoRollTransformer.UpdateZoom/UpdateXZoom/UpdateYZoom

  [Released — 1 finger]
    → delta<5px, <300ms → Tap 発火
        → EditPage Tap handler (L728)
            → EditNote:
                → IsPointInNote? selected? → select/deselect toggle
                → empty space? → deselect all
            → EditNote (double Tap L783): → LyricPopup 表示
            → EditPitchCurve: → ピッチアンカー追加/削除
    → delta≥5px → PanEnd 発火
        → EditPage PanEnd handler (L914)
            → IsMovingNotes?  → EndMoveNotes() [IsMovingNotes=false + EndUndoGroup]
            → IsResizingNote? → EndResizeNotes()
            → else → PianoRollTransformer.EndPan()
            → IsUserDrawingCurve? → EndDrawPitch(); IsUserDrawingCurve=false

  [Cancelled]
    → PanEnd 発火 (if was Pan) → 同上 PanEnd handler
```

---

## Section 5: Known Bugs & Fixes Applied

| Bug ID | 症状 | 根本原因 | 修正内容 | ファイル・行 | 状態 |
|--------|------|---------|---------|------------|------|
| BUG-A | ノートドラッグが動かない | `PanStart` が5px ドリフト後の `StartPosition` をヒットテストに使用 → タッチ開始位置からずれてノートを拾えない | `PanStartEventArgs` に `OriginalTouchDown` 追加。EditPage ヒットテストを `e.OriginalTouchDown` に変更 | `TouchEventArgs.cs` L29–41、`GestureProcessor.cs` L259、`EditPage.xaml.cs` L824 | ✅ コード完了、実機テスト待ち |
| BUG-B | リサイズハンドルが当たらない | `IsPointInHandle` の Y 軸計算が `* ZoomY` → ズーム時にヒット矩形が描画位置からずれる (X軸は `/ ZoomX` で正しい) | `* ZoomY` → `/ ZoomY` に修正 | `DrawableNotes.cs` L331–332 | ✅ コード完了、実機テスト待ち |
| BUG-C | 操作後パンが効かなくなる | Android システムジェスチャーが Released/Cancelled を消費 → `_activePoints` にゴミエントリが残り新規ジェスチャー開始不能 | (1) `HandleTouchDown` にステールポイントクリーンアップ追加 (2) `ForceReset()` メソッド追加 (3) `OnAppearing` で全プロセッサに `ForceReset()` 呼び出し | `GestureProcessor.cs` L105–109, L358–369; `EditPage.xaml.cs` L2636–2648 | ✅ コード完了、実機テスト待ち |

---

## Section 6: Coordinate Systems

```
[Screen pixels — SKTouchEventArgs.Location / SKPaintSurfaceEventArgs]
  = 物理ピクセル座標 (density 考慮済み SkiaSharp 座標)
  = Canvas の左上が (0, 0)

  ↓  ActualToLogical(actual)  [Transformer.cs L85-90]
     logical.X = (actual.X - PanX) / ZoomX
     logical.Y = (actual.Y - PanY) / ZoomY

[Logical coordinates — IsPointInNote / IsPointInHandle に渡す座標]
  = ズーム・パン補正済み "論理空間"
  X: tick 単位 (note.position, note.duration と同スケール)
  Y: HeightPerPianoKey 単位 (ViewConstants.TotalPianoKeys - tone) で音高方向

  ↓  Tick/Tone への変換 (アプリ固有)
     tick   = logical.X - PositionX (DrawableNotes.PositionX = EditingPart.position)
     tone   = ViewConstants.TotalPianoKeys - floor(logical.Y / HeightPerPianoKey) - 1

[Tick/Tone — UNote.position, UNote.tone (Core モデル)]
  = MIDI tick (BPM・TimeSignature に依存しないサンプル単位)
  = tone: 0=C0, 60=C5 (MusicMath.KeysInOctave)
```

**逆変換 (描画時):**
```
  actual = LogicalToActual(logical)
  actual.X = logical.X * ZoomX + PanX
  actual.Y = logical.Y * ZoomY + PanY
```

**`GetTransformMatrix()` (L76):**
```
  SKMatrix = Scale(ZoomX, ZoomY).PostConcat(Translate(PanX, PanY))
  canvas.SetMatrix(matrix) → 全 DrawCall が自動変換される
```

**`IsPointInHandle` のヒット矩形 (DrawableNotes.cs L329–332):**
```
  left   = PositionX + note.position + note.duration + Spacing / ZoomX
  right  = left + HandleSize / ZoomX          ← 論理空間で一定サイズ
  top    = (TotalPianoKeys - note.tone - 0.5f) * HeightPerPianoKey - HalfHandleSize / ZoomY
  bottom = top + HandleSize / ZoomY           ← BUG-B 修正: *ZoomY → /ZoomY
```

---

## Section 7: Edge Cases & Gotchas

**1. `StartGestureDetection` は空スタブ (L243–245)**
- 第1指タッチ時に呼ばれるが何もしない。将来のタップ前処理用プレースホルダ。
- 現状 Tap 判定は Released 時に行われる（タッチダウン時ではない）。

**2. `PanStartEventArgs.StartPosition` vs `OriginalTouchDown`**
- `StartPosition` = PanStart 発火時点 (5px ドリフト後) — `Transformer.StartPan()` に渡す
- `OriginalTouchDown` = 最初の Pressed 位置 — `IsPointInNote / IsPointInHandle` に渡す
- **両者を混同すると BUG-A 再発。** ヒットテストには必ず `OriginalTouchDown` を使うこと。
- `SwitchToPanFromZoom()` では1引数コンストラクタを使用 → `OriginalTouchDown = StartPosition` にフォールバック（正常）。

**3. `HandleTouchUp` vs `HandleTouchCancel` の対称性**
- `HandleTouchCancel` は `_pointQueue.Clear()` を追加で行う (L81)。Up は行わない。
- 両方とも `case 1 when Zoom/XZoom/YZoom` で `SwitchToPanFromZoom()` を呼ぶ。
- `P2-B1b`: `HandleTouchUp` に `GestureState.Zoom` が欠落していた → `SwitchToPanFromZoom()` 未呼び出しでグレー画面固着。修正済み (commit `bb28e4a`)。
- 将来状態を追加する場合は **必ず両メソッドに対称的に** 追加すること。

**4. `ForceReset()` は `OnAppearing` で全プロセッサに呼ぶ (EditPage L2636)**
- Android システムジェスチャー（画面端スワイプ等）が Released を消費した場合の安全網。
- Pan 中だった場合は `PanEnd` を発火してから状態クリア → `EndMoveNotes/EndResizeNotes` が確実に呼ばれる。
- `EditViewModel.ForceEndAllInteractions()` も同時に呼ぶこと（`IsMovingNotes / IsResizingNote` フラグのリセット）。

**5. `HandleTouchDown` のステールポイントクリーンアップ (L105–109)**
- `_activePoints.Count > 0 && state == None` の条件でのみ発動。
- Pan/Zoom 中に新指が来た場合は発動しない（正常な2本指操作と区別するため）。
- この条件が満たされない場合の補完が `ForceReset()`。

**6. `Console.WriteLine` vs `Debug.WriteLine`**
- Android logcat に出力されるのは `Console.WriteLine` のみ。
- `Debug.WriteLine` は Visual Studio デバッグ出力には出るが logcat には出ない。
- パフォーマンス計測ログ (`PaintSurfaceProfiler`) は `Console.WriteLine` で実装済み。

**7. Throttle は `Environment.TickCount64` ベース (L335)**
- `ShouldThrottle` は ref パラメータで `_lastPanTicks` / `_lastZoomTicks` を更新。
- Pan と Zoom は独立したカウンタなので互いに干渉しない。
- 16ms = 約 60Hz。変更時は `ThrottleIntervalMs` (L31) を修正。

**8. 3本指以上のタッチ**
- `_activePoints.Count >= 2` で `_pointQueue.Enqueue` → `return`。
- 3本指イベントは完全無視（ジェスチャー状態に影響しない）。
- 2本指の1本が離れた際に `_pointQueue` からデキューして補充 (HandleTouchUp L216–220)。
