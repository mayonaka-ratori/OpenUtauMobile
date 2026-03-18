---
name: openutau-core-api
description: OpenUtau Core API reference — DocManager singleton, command pattern, ICmdSubscriber, all note/vibrato/phoneme commands, UVibrato ranges, pitch model, and autosave. Load when writing or modifying code that interacts with Core.
---

# OpenUtau Core API

## DocManager — Central State

- Singleton: `DocManager.Inst` (always use this static accessor, never lowercase)
- Inherits `SingletonBase<DocManager>`
- Current project: `DocManager.Inst.Project` (`UProject`)
- `UProject` contains: `List<UTrack>` tracks, `List<UPart>` parts
- `UVoicePart` contains: `SortedSet<UNote>` notes, curves, expressions
- `UNote` contains: lyric, tone, duration, position, pitch (`UPitch`), vibrato (`UVibrato`)
- `UNote.Create()` returns a note with pitch and vibrato initialized but duration=0
- `UProject.CreateNote()` returns a note with default portamento pitch points

## Command Pattern

All data mutations must go through this pattern:

  DocManager.Inst.StartUndoGroup();
  DocManager.Inst.ExecuteCmd(new SomeCommand(args));
  DocManager.Inst.EndUndoGroup();

ExecuteCmd posts to UI thread if called from background thread.
EndUndoGroup triggers PreRenderNotification automatically.

## Event Subscription (ICmdSubscriber)

ViewModels subscribe to DocManager commands via a separate pub/sub system.
This is NOT the same as ReactiveUI subscriptions. Both must be managed.

  public class MyViewModel : ICmdSubscriber, IDisposable {
      public MyViewModel() {
          DocManager.Inst.AddSubscriber(this);
      }
      public void OnNext(UCommand cmd, bool isUndo) {
          // React to commands: refresh UI when notes change, etc.
      }
      public void Dispose() {
          DocManager.Inst.RemoveSubscriber(this);
      }
  }

## Note Commands (in OpenUtau.Core/Commands/NoteCommands.cs)

- `AddNoteCommand(part, note)` or `(part, List<UNote>)`
- `RemoveNoteCommand(part, note)` or `(part, List<UNote>)`
- `MoveNoteCommand(part, note, deltaPos, deltaNoteNum)` or `(part, List<UNote>, ...)`
- `ResizeNoteCommand(part, note, deltaDur)` or `(part, List<UNote>, ...)`
- `ChangeNoteLyricCommand(part, note, newLyric)` or `(part, UNote[], string[])`

## Vibrato Commands (in NoteCommands.cs)

All inherit from VibratoCommand which skips Phonemizer/Phoneme validation.

- SetVibratoCommand(part, note, vibrato) — replace entire UVibrato
- VibratoLengthCommand(part, note, length)
- VibratoDepthCommand(part, note, depth)
- VibratoPeriodCommand(part, note, period)
- VibratoFadeInCommand(part, note, fadeIn)
- VibratoFadeOutCommand(part, note, fadeOut)
- VibratoShiftCommand(part, note, shift)
- VibratoDriftCommand(part, note, drift)
- VibratoVolumeLinkCommand(part, note, volLink)

## Vibrato Model (UVibrato in Ustx/UNote.cs)

Property  Type   Min   Max   Unit
length    float  0     100   percent of note duration
period    float  5     500   milliseconds
depth     float  5     200   cents
in        float  0     100   percent of vibrato length (in + out <= 100)
out       float  0     100   percent of vibrato length (in + out <= 100)
shift     float  0     100   percent of period
drift     float  -100  100   cents offset
volLink   float  -100  100   percent volume link

## Phoneme Commands (in NoteCommands.cs)

NOTE: There is NO MovePhonemeCommand. Use PhonemeOffsetCommand instead.

- PhonemeOffsetCommand(part, note, index, offset) — shift phoneme position
- PhonemePreutterCommand(part, note, index, delta) — adjust preutter
- PhonemeOverlapCommand(part, note, index, delta) — adjust overlap
- ClearPhonemeTimingCommand(part, note) — reset all timing overrides
- ChangePhonemeAliasCommand(part, note, index, alias) — override phoneme symbol

## Other Command Categories

- TrackCommands: AddTrack, RemoveTrack, MoveTrack, etc.
- PartCommands: AddPart, RemovePart, MovePart, ResizePart
- ExpressionCommands: SetCurve, ResetExpression
- TempoCommands: AddTempoChange, RemoveTempoChange
- TimeSignatureCommands: AddTimeSig, RemoveTimeSig

## Notification Commands (special, no undo)

- SaveProjectNotification, LoadProjectNotification
- SetPlayPosTickNotification
- PreRenderNotification
- SingersChangedNotification, SingersRefreshedNotification
- ValidateProjectNotification, ProgressBarNotification

## Singer and Phonemizer

- Singer loaded via SingerManager.Inst
- Phonemizer set per track: track.Phonemizer
- Phonemizer.Process(notes, prev, next) returns Phoneme[]
- Plugin loading on Android uses Assembly.Load (patched from upstream)

## Pitch Model (`UPitch` in Ustx/UNote.cs)

- `List<PitchPoint>` data — sorted by X position
- `PitchPoint.X`: float, position in ms relative to note start
- `PitchPoint.Y`: float, pitch in 0.1 semitones relative to note tone
- `PitchPoint.shape`: PitchPointShape enum (io, l, i, o)
- snapFirst: bool — if true, first point snaps to previous note

## AutoSave

- DocManager.Inst.AutoSave() saves to {filename}-autosave.ustx
- CrashSave() saves to {filename}-backup.ustx on unhandled exception
- Both skip if no changes since last save
