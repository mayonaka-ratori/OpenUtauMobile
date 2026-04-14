using DynamicData;
using DynamicData.Binding;
using OpenUtau.Core;
using OpenUtau.Core.Analysis.Some;
using OpenUtau.Core.Ustx;
using OpenUtau.Utils.Messages;
using OpenUtauMobile.Resources.Strings;
using OpenUtauMobile.Utils;
using OpenUtauMobile.Views.DrawableObjects;
using OpenUtauMobile.Views.Utils;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Reactive.Disposables;
using Serilog;
using SkiaSharp;
using Preferences = OpenUtau.Core.Util.Preferences;

namespace OpenUtauMobile.ViewModels
{
    public partial class EditViewModel : ReactiveObject, IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private bool _disposed = false;
        /* UI-related properties */
        public double MainLayoutHeight { get; set; } = 1000d;
        public double MainEditHeight { get; set; } = 600d;
        public double PianoRollHeight { get; set; } = 800d;
        #region Track - Main Edit Area Drag Divider
        [Reactive] public double DivPosY { get; set; } = 100d;
        [Reactive] public Rect BoundDiv { get; set; } = new Rect(0d, 100d, 1, 50d);
        [Reactive] public Rect BoundTrack { get; set; } = new Rect(0d, 0d, 1, 0d);
        [Reactive] public Rect BoundPianoRoll { get; set; } = new Rect(0d, 150d, 1, 0d);
        [Reactive] public Rect BoundDivControl { get; set; } = new Rect(0d, 100d, 60d, 50d);
        #endregion
        #region Piano Roll - Expression Area Drag Divider
        [Reactive] public double ExpHeight { get; set; } = 50d; // Cannot use directly — this is relative to the bottom edge
        [Reactive] public double DivExpPosY { get; set; } // Dynamically computed from ExpHeight
        [Reactive] public Rect BoundExpDiv { get; set; } = new Rect(0d, 0d, 1, 50d); // Drag divider
        [Reactive] public Rect BoundExp { get; set; } = new Rect(0d, 0d, 1, 0d); // Expression area
        [Reactive] public Rect BoundExpDivControl { get; set; } = new Rect(0d, 0d, 60d, 50d); // Drag handle
        #endregion
        #region Transformers
        public Transformer TrackTransformer { get; set; } = new();
        public Transformer PianoRollTransformer { get; set; } = new();
        #endregion
        public double Density => DeviceDisplay.MainDisplayInfo.Density;
        [Reactive] public double HeightPerTrack { get; set; } = 60d; // Height per track
        [Reactive] public double HeightPerPianoKey { get; set; } = 40d; // Height per piano key
        [Reactive] public bool IsOpenDetailedTrackHeader { get; set; } = false; // Whether the detailed track header is open
        [Reactive] public double AvatarSize { get; set; } = 35d; // Avatar size
        [Reactive] public bool IsShowRemoveNoteButton { get; set; } = false; // Whether to show the Remove Note button
        [Reactive] public bool IsShowRenderPitchButton { get; set; } = false; // Whether to show the Render Pitch button
        [Reactive] public bool IsShowAudioTranscribeButton { get; set; } = false; // Whether to show the Audio Transcribe button
        [Reactive] public bool IsShowSelectButton { get; set; } = false;
        public double OriginalVolume { get; set; } = 0d; // Stores the original volume before a pan gesture
        public int[] SnapDivs = [4, 8, 16, 32, 64, 128, 3, 6, 12, 24, 48, 96, 192]; // Available quantization divisions
        #region Edit Modes
        /// <summary>
        /// Current track edit mode.
        /// </summary>
        [Reactive] public TrackEditMode CurrentTrackEditMode { get; set; } = TrackEditMode.Normal; // Default: read-only mode
        /// <summary>
        /// Current piano roll note edit mode.
        /// </summary>
        [Reactive] public NoteEditMode CurrentNoteEditMode { get; set; } = NoteEditMode.EditNote; // Default: note-edit mode
        /// <summary>
        /// Current expression edit mode.
        /// </summary>
        [Reactive] public ExpressionEditMode CurrentExpressionEditMode { get; set; } = ExpressionEditMode.Hand; // Default: hand mode
        [Reactive] public SelectionMode CurrentSelectMode { get; set; } = SelectionMode.Single;
        #endregion
        [Reactive] public ObservableCollectionExtended<UPart> PhonemizingParts { get; set; } = []; // Parts currently being phonemized
        [Reactive] public string PhonemizingPartName { get; set; } = string.Empty; // Name of the part currently being phonemized
        [Reactive] public bool IsPhonemizing { get; set; } = false; // Whether phonemization is in progress
        [Reactive] public ObservableCollectionExtended<UPart> SelectedParts { get; set; } = []; // Collection of selected parts
        [Reactive] public string EditingPartName { get; set; } = string.Empty; // Name of the part currently being edited
        [Reactive] public UVoicePart? EditingPart { get; set; } = null; // Part currently open in the piano roll
        [Reactive] public ObservableCollectionExtended<UNote> SelectedNotes { get; set; } = []; // Collection of selected notes
        /// <summary>
        /// Determined by SelectedNotes — do not modify directly.
        /// </summary>
        [Reactive] public UNote? EditingNote { get; set; } = null; // Note being edited (for future properties panel)
        #region Track Quantization
        [Reactive] public bool IsTrackSnapToGrid { get; set; } = true; // Whether snap-to-grid is enabled for the track
        [Reactive] public int TrackSnapDiv { get; set; } = 4; // Track grid snap density: 1/x bar
        /// <summary>
        /// Tick length of one grid unit under the current track quantization.
        /// </summary>
        public int TrackSnapUnitTick
        {
            get
            {
                if (TrackSnapDiv <= 0)
                {
                    return DocManager.Inst.Project.resolution * 4; // Default: return one bar's tick length
                }
                return DocManager.Inst.Project.resolution * 4 / TrackSnapDiv;
            }
        } // Tick length per grid unit
        #endregion
        #region Piano Roll Quantization
        [Reactive] public bool IsPianoRollSnapToGrid { get; set; } = true; // Whether snap-to-grid is enabled for the piano roll
        [Reactive] public int PianoRollSnapDiv { get; set; } = 4; // Piano roll grid snap density: 1/x bar
        /// <summary>
        /// Tick length of one grid unit under the current piano roll quantization.
        /// </summary>
        public int PianoRollSnapUnitTick
        {
            get
            {
                if (PianoRollSnapDiv <= 0)
                {
                    return DocManager.Inst.Project.resolution * 4; // Default: return one bar's tick length
                }
                return DocManager.Inst.Project.resolution * 4 / PianoRollSnapDiv;
            }
        } // Tick length per grid unit
        #endregion
        #region Move Part Fields
        private SKPoint _startMovePartsPosition; // Starting position when drag begins
        private int _startMovePartsTrackNo; // Starting track number when drag begins
        public bool IsMovingParts = false; // Whether parts are currently being moved
        private List<int> _oldMovedPartsPos = []; // Saves part positions at move start
        private List<int> _oldMovedPartsTrackNo = []; // Saves part track numbers at move start
        private UndoScope? _movePartsUndoScope;
        #endregion
        #region Create Part Fields
        private SKPoint _startCreatePartPosition; // Starting position when part creation begins
        public bool IsCreatingPart = false; // Whether a part is currently being created
        private UndoScope? _createPartUndoScope;
        #endregion
        #region Resize Part Fields
        private SKPoint _startResizePartPosition; // Starting position when part resize begins
        public bool IsResizingPart = false; // Whether a part is currently being resized
        private UPart? _resizingPart; // Part currently being resized
        private int _resizingPartOriginalDuration; // Original duration of the part at resize start
        private UndoScope? _resizePartUndoScope;
        #endregion
        #region Resize Note Fields
        private UNote? _resizingNote; // Note currently being resized
        public bool IsResizingNote = false; // Whether a note is currently being resized
        private int _initialBound2TouchOffset = 0; // Initial X offset from the note's right edge to the touch point (logical coords) at resize start
        private UndoScope? _resizeNotesUndoScope;
        #endregion
        #region Move Note Fields
        private SKPoint _startMoveNotesPosition; // Starting position when note drag begins
        public bool IsMovingNotes = false; // Whether notes are currently being moved
        private int _originalPosition; // Original position of the note at move start
        private int _startMoveNoteToneReversed; // Tone of the touched note at drag start (raw, not offset by total key count)
        private int _offsetPosition; // Cumulative position offset after last move — used to compute relative displacement
        private int _offsetTone; // Cumulative tone offset after last move — used to compute relative displacement
                                 //private List<int> _originalNoteTones; // Original tone of each note at move start
        private UndoScope? _moveNotesUndoScope;
        #endregion
        #region Pitch Curve Fields
        private int? _lastPitchTick; // Tick position of the last pitch point
        private double? _lastPitchValue; // Pitch value of the last pitch point
        private UndoScope? _drawPitchUndoScope;
        #endregion
        #region Expression Drawing State Fields
        private int _lastExpTick = 0;
        private int _lastExpValue = 0;
        private UExpressionDescriptor? _editingExpressionDescriptor;
        // Expression value currently being drawn
        public int currentExpressionValue = 0;
        private UndoScope? _drawExpressionUndoScope;
        private UndoScope? _resetExpressionUndoScope;
        #endregion
        /* Backend data properties */
        [Reactive] public string Path { get; set; } = string.Empty;
        [Reactive] public ObservableCollectionExtended<UTrack> Tracks { get; set; } = [];
        //[Reactive] public ObservableCollectionExtended<UPart> Parts { get; set; } = [];
        [Reactive] public int PlaybackStartPosition { get; set; } = 0;
        [Reactive] public bool PlaybackWasStoppedManually { get; set; } = true;
        [Reactive] public int PlayPosTick { get; set; } = 0;
        [Reactive] public bool Playing { get; set; } = false;
        [Reactive] public byte[] CurrentPortrait { get; set; } = [];
        [Reactive] public double PortraitOpacity { get; set; } = 1d;
        public HashSet<DrawablePart> DrawableParts { get; set; } = [];
        /// <summary>
        /// DrawableNotes instance currently shown in the piano roll. Null when no part is being edited.
        /// </summary>
        public DrawableNotes? EditingNotes { get; set; }
        private static List<UNote> _clipboard = [];
        [Reactive] public bool PlayPosWaitingRendering { get; set; } = false; // Waiting for render
        public double OriginalPan { get; internal set; }
        [Reactive] public ObservableCollectionExtended<RunningWork> RunningWorks { get; set; } = []; // List of currently running background tasks
        [Reactive] public UExpressionDescriptor PrimaryExpressionDescriptor { get; set; } = null!;
        [Reactive] public UExpressionDescriptor SecondaryExpressionDescriptor { get; set; } = null!;
        [Reactive] public string PrimaryExpressionAbbr { get; set; } = string.Empty;
        [Reactive] public string SecondaryExpressionAbbr { get; set; } = string.Empty;
        [Reactive] public Color EditingPartColor { get; set; } = Colors.Transparent; // Accent colour of the part currently being edited
        public void InitExpressions()
        {
            PrimaryExpressionDescriptor = DocManager.Inst.Project.expressions.FirstOrDefault().Value;
            SecondaryExpressionDescriptor = DocManager.Inst.Project.expressions.Skip(1).FirstOrDefault().Value;
            UpdateExpressions();
        }
        public void UpdateExpressions()
        {
            PrimaryExpressionAbbr = PrimaryExpressionDescriptor.abbr;
            SecondaryExpressionAbbr = SecondaryExpressionDescriptor.abbr;
        }
        public void SetWork(WorkType type, string id, double progress = 0, string detail = "", CancellationTokenSource? cancellationTokenSource = null)
        {
            RunningWork? existingWork = RunningWorks.FirstOrDefault(w => w.Id == id);
            if (existingWork != null)
            {
                // Work with this ID already exists — update it
                existingWork.Type = type;
                existingWork.Progress = progress;
                existingWork.Detail = detail;
                // Notify property change
                this.RaisePropertyChanged(nameof(RunningWorks));
                Console.WriteLine($"Update work {id}: type={type}, progress={progress}, detail={detail}");
            }
            else
            {
                // No existing entry — add a new work item
                RunningWorks.Add(new RunningWork
                {
                    Id = id,
                    Type = type,
                    Progress = progress,
                    Detail = detail,
                    CancellationTokenSource = cancellationTokenSource
                });
                Console.WriteLine($"Add work {id}: type={type}, progress={progress}, detail={detail}");
            }
        }

        public void RemoveWork(string id)
        {
            var workToRemove = RunningWorks.FirstOrDefault(w => w.Id == id);
            if (workToRemove != null)
            {
                RunningWorks.Remove(workToRemove);
                Console.WriteLine($"Remove work {id}");
            }
        }

        public void TryCancelWork(string id)
        {
            RunningWork? work = RunningWorks.FirstOrDefault(work => work.Id == id);
            if (work == null || work.CancellationTokenSource == null)
            {
                return;
            }
            work.CancellationTokenSource.Cancel();
            Console.WriteLine($"Cancel work {id}");
        }



        public EditViewModel()
        {
            // Subscribe to DivPosY changes
            this.WhenAnyValue(x => x.DivPosY)
                .Subscribe(_ => UpdateTrackMainEditBoundaries())
                .DisposeWith(_disposables);
            // Subscribe to ExpHeight changes
            this.WhenAnyValue(x => x.ExpHeight)
                .Subscribe(_ =>
                {
                    DivExpPosY = MainEditHeight - ExpHeight;
                    UpdatePianoRollExpBoundaries();
                })
                .DisposeWith(_disposables);
            // Subscribe to phonemizing-parts list changes
            PhonemizingParts.CollectionChanged += OnPhonemizingPartsChanged;
            // Subscribe to selected-parts list changes
            SelectedParts.CollectionChanged += OnSelectedPartsChanged;
            // Subscribe to selected-notes list changes — update editing note
            // Note: this subscription occasionally fails to fire — a manual trigger method is provided as workaround
            SelectedNotes.CollectionChanged += OnSelectedNotesChanged;
        }

        private void OnPhonemizingPartsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            IsPhonemizing = (PhonemizingParts.Count != 0);
            if (IsPhonemizing)
            {
                PhonemizingPartName = AppResources.PhonemizingInProgress;
                foreach (var part in PhonemizingParts)
                {
                    if (part != null)
                    {
                        PhonemizingPartName += part.DisplayName + ' ';
                    }
                }
            }
        }

        private void OnSelectedPartsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            IsShowRenderPitchButton = false; // Reset Render Pitch button visibility on each selection change
            IsShowAudioTranscribeButton = false; // Reset Audio Transcribe button visibility on each selection change
            if (SelectedParts.Count == 0)
            {
                EditingPart = null; // Clear editing part
                EditingPartName = string.Empty; // Clear editing part name
                EditingNotes = null; // Clear editing note group
                CurrentPortrait = []; // Clear current portrait
                EditingPartColor = Colors.Transparent;
            }
            else
            {
                // Set editing part to the first selected voice part
                foreach (var part in SelectedParts)
                {
                    if (part is UVoicePart voicePart)
                    {
                        EditingPart = voicePart;
                        UpdateIsShowRenderPitchButton();
                        LoadPortrait();
                        EditingPartColor = ViewConstants.TrackMauiColors[DocManager.Inst.Project.tracks[voicePart.trackNo].TrackColor];
                        break;
                    }
                }
                if (EditingPart == null)
                {
                    EditingPartName = string.Empty; // No voice part selected — clear editing part name
                    EditingNotes = null; // Clear editing note group
                    if (SelectedParts.Count > 0)
                    {
                        if (SelectedParts[0] is UWavePart)
                        {
                            // If the first selected part is a wave part, show the Audio Transcribe button
                            IsShowAudioTranscribeButton = true;
                        }
                    }
                    return;
                }
                if (SelectedParts.Count == 1)
                {
                    EditingPartName = EditingPart.DisplayName; // Update editing part name
                }
                else
                {
                    EditingPartName = string.Format(AppResources.NPartsSelected, SelectedParts.Count); // Multiple parts selected — show count
                }
            }
        }

        private void OnSelectedNotesChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            HandleSelectedNotesChanged();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            PhonemizingParts.CollectionChanged -= OnPhonemizingPartsChanged;
            SelectedParts.CollectionChanged -= OnSelectedPartsChanged;
            SelectedNotes.CollectionChanged -= OnSelectedNotesChanged;
            _disposables.Dispose();
            GC.SuppressFinalize(this);
        }

        public void ValidateSelectedParts()
        {
            // Check whether any selected parts have been removed from the project
            SelectedParts.RemoveMany([.. SelectedParts
                    .Where(part =>
                    {
                        if (DocManager.Inst.Project.parts.Contains(part))
                        {
                            return false; // Part still exists in the project — keep it
                        }
                        return true; // Part no longer exists in the project — remove it
                    })]);
            if (SelectedParts.Count == 0)
            {
                EditingPart = null; // Clear editing part
                EditingPartName = string.Empty; // Clear editing part name
                EditingNotes = null; // Clear editing note group
                CurrentPortrait = []; // Clear current portrait
                EditingPartColor = Colors.Transparent;
            }
        }

        public void UpdateIsShowRenderPitchButton()
        {
            if (SelectedParts.Count == 0 || EditingPart == null)
            {
                IsShowRenderPitchButton = false; // Hide Render Pitch button
                return;
            }
            IsShowRenderPitchButton = DocManager.Inst.Project.tracks[EditingPart.trackNo].RendererSettings.Renderer?.SupportsRenderPitch ?? false; // Show/hide Render Pitch button based on whether the track's renderer supports it

        }

        public void UpdateTrackMainEditBoundaries()
        {
            BoundTrack = new Rect(BoundTrack.X, BoundTrack.Y, BoundTrack.Width, DivPosY); // Height equals DivPosY
            BoundDiv = new Rect(BoundDiv.X, DivPosY, BoundDiv.Width, BoundDiv.Height); // Top edge is DivPosY
            BoundPianoRoll = new Rect(BoundPianoRoll.X, DivPosY + 50d, BoundPianoRoll.Width, MainLayoutHeight - DivPosY - 50d); // Top = DivPosY + divider height (50); Height = TotalHeight - DivPosY - 50
            // TODO: clamp ExpHeight to prevent exceeding MainEditHeight
        }
        public void UpdatePianoRollExpBoundaries()
        {
            BoundExpDiv = new Rect(BoundExpDiv.X, DivExpPosY, BoundExpDiv.Width, BoundExpDiv.Height); // Top edge is DivExpPosY
            BoundExp = new Rect(BoundExp.X, DivExpPosY + 50d, BoundExp.Width, MainEditHeight - DivExpPosY - 50d); // Top = DivExpPosY + divider height (50); Height = TotalHeight - DivExpPosY - 50
        }

        public void HandleSelectedNotesChanged()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var notesToRemove = new System.Collections.Generic.List<UNote>();
                foreach (var note in SelectedNotes)
                {
                    if (EditingPart == null)
                    {
                        notesToRemove.Add(note); // If no part is being edited, the note is invalid
                        continue;
                    }

                    // Check if the note still belongs to the current editing part
                    if (!EditingPart.notes.Contains(note))
                    {
                        notesToRemove.Add(note);
                    }
                }
                
                foreach (var note in notesToRemove)
                {
                    SelectedNotes.Remove(note);
                }

                UNote? firstSelectedNote = SelectedNotes.FirstOrDefault();
                if (firstSelectedNote == null) // The collection is empty
                {
                    EditingNote = null; // Clear the currently edited note
                    IsShowRemoveNoteButton = false; // Hide the remove note button
                }
                else
                {
                    EditingNote = firstSelectedNote; // Set the first selected note as the edited note
                    IsShowRemoveNoteButton = true; // Show the remove note button
                }
            });
        }

        public async Task Init()
        {
            await LoadProject(Path);
        }

        public static async Task LoadProject(string path)
        {
            await Task.Run(() =>
            {
                try
                {
                    // New project
                    if (string.IsNullOrEmpty(path))
                    {
                        DocManager.Inst.ExecuteCmd(new LoadProjectNotification(OpenUtau.Core.Format.Ustx.Create()));
                    }
                    else
                    {
                        // Open existing
                        string[] files = { path };
                        OpenUtau.Core.Format.Formats.LoadProject(files);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to load project.");
                    DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(AppResources.ErrorFailLoadProject, ex));
                }
            });
        }


        public void SetBoundDivControl()
        {
            //BoundDivControl = new Rect(0d, DivPosY, 50d, 50d);
            BoundDivControl = new Rect(BoundDivControl.X, DivPosY, BoundDivControl.Width, BoundDivControl.Height);
        }

        public void SetBoundExpDivControl()
        {
            BoundExpDivControl = new Rect(BoundExpDivControl.X, DivExpPosY, BoundExpDivControl.Width, BoundExpDivControl.Height);
        }

        /// <summary>
        /// Start moving parts.
        /// </summary>
        /// <param name="parts"></param>
        /// <param name="startPosition">Logical coordinates</param>
        public void StartMoveParts(IEnumerable<UPart> parts, SKPoint startPosition)
        {
            if (parts == null || !parts.Any())
            {
                return;
            }
            _startMovePartsPosition = startPosition; // Record starting position at drag begin
            _startMovePartsTrackNo = (int)Math.Floor((startPosition.Y / Density) / HeightPerTrack);
            IsMovingParts = true; // Mark as moving parts
            _oldMovedPartsPos.Clear(); // Clear previous part positions
            _oldMovedPartsTrackNo.Clear(); // Clear previous part track numbers
            foreach (var part in parts)
            {
                if (part == null)
                {
                    continue;
                }
                // Record part position and track number at move start
                _oldMovedPartsPos.Add(part.position);
                _oldMovedPartsTrackNo.Add(part.trackNo);
            }
            // Start an undo group
            _movePartsUndoScope = new UndoScope();
        }

        /// <summary>
        /// Update position of parts being moved.
        /// </summary>
        /// <param name="currentPosition">Logical coordinates</param>
        public void UpdateMoveParts(SKPoint currentPosition)
        {
            if (!IsMovingParts)
            {
                return;
            }
            // Calculate offset

            var offsetX = currentPosition.X - _startMovePartsPosition.X;
            var offsetY = currentPosition.Y - _startMovePartsPosition.Y;
            // If offset is below threshold, do not move
            if (Math.Abs(offsetX) < 5)
            {
                return;
            }
            int deltaTrackNo = (int)Math.Floor((currentPosition.Y / Density) / HeightPerTrack) - _startMovePartsTrackNo;
            // Calculate new positions
            int[] newPositions = new int[SelectedParts.Count];
            int[] newTrackNos = new int[SelectedParts.Count];
            for (int i = 0; i < SelectedParts.Count; i++)
            {
                var part = SelectedParts[i];
                // Calculate new position
                int newPosition = (int)(_oldMovedPartsPos[i] + offsetX);
                // If snap-to-grid is enabled, align new position to the nearest grid line
                if (IsTrackSnapToGrid)
                {
                    newPosition = TrackTickToLinedTick(newPosition);
                    if (newPosition == part.position && deltaTrackNo == 0)
                    { return; }
                }
                int newTrackNo = _oldMovedPartsTrackNo[i] + deltaTrackNo;
                // Check whether the new position is within valid bounds
                if (newPosition < 0 || newTrackNo < 0 || newTrackNo >= DocManager.Inst.Project.tracks.Count)
                {
                    Console.WriteLine($"Part position out of range — cannot move.");
                    return; // If the new position is out of range, do not move
                }
                newPositions[i] = newPosition;
                newTrackNos[i] = newTrackNo;
            }
            // Update selected part positions
            for (int i = 0; i < SelectedParts.Count; i++)
            {
                var part = SelectedParts[i];
                if (part == null)
                {
                    continue;
                }
                // Execute move command
                try
                {
                    DocManager.Inst.ExecuteCmd(new MovePartCommand(DocManager.Inst.Project, part, newPositions[i], newTrackNos[i]));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        /// <summary>
        /// End moving parts.
        /// </summary>
        public void EndMoveParts()
        {
            IsMovingParts = false; // Reset moving state
            // End undo group
            _movePartsUndoScope?.Dispose();
            _movePartsUndoScope = null;
        }

        /// <summary>
        /// Start creating a part.
        /// </summary>
        /// <param name="currentPoint">Logical coordinates</param>
        public void StartCreatePart(SKPoint currentPoint)
        {
            _startCreatePartPosition = currentPoint; // Record starting position at part creation begin
            IsCreatingPart = true; // Mark as creating part
            int trackNo = (int)Math.Floor((currentPoint.Y / Density) / HeightPerTrack);
            // Ensure track number is within valid range
            if (trackNo < 0 || trackNo >= DocManager.Inst.Project.tracks.Count)
            {
                Console.WriteLine($"Track number {trackNo} out of range — cannot create part.");
                IsCreatingPart = false; // Creation failed — reset state
                return;
            }
            int position = (int)currentPoint.X;
            if (IsTrackSnapToGrid)
            {
                position = TrackTickToFloorLinedTick(position);
            }
            // Create an initial part
            UVoicePart part = new()
            {
                position = position,
                // Initial duration equals one grid unit's tick length
                Duration = TrackSnapUnitTick,
                trackNo = trackNo,
                name = AppResources.NewPart
            };
            // Clear selected parts
            SelectedParts.Clear();
            SelectedParts.Add(part);
            _createPartUndoScope = new UndoScope();
            try
            {
                DocManager.Inst.ExecuteCmd(new AddPartCommand(DocManager.Inst.Project, part));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                IsCreatingPart = false;
                _createPartUndoScope?.Dispose();
                _createPartUndoScope = null;
                SelectedParts.Clear();
                foreach (var p in DocManager.Inst.Project.parts)
                {
                    if (p.trackNo >= DocManager.Inst.Project.tracks.Count || p.trackNo < 0)
                    {
                        DocManager.Inst.ExecuteCmd(new RemovePartCommand(DocManager.Inst.Project, p));
                    }
                }
            }
        }

        /// <summary>
        /// Update the length of the part currently being created.
        /// </summary>
        /// <param name="sKPoint"></param>
        public void UpdateCreatePart(SKPoint sKPoint)
        {
            if (!IsCreatingPart || SelectedParts == null || SelectedParts.Count == 0)
            {
                return;
            }
            int width = (int)(sKPoint.X - _startCreatePartPosition.X);
            if (IsTrackSnapToGrid)
            {
                width = TrackTickToLinedTick(SelectedParts[0].position + width) - SelectedParts[0].position;
                if (width == SelectedParts[0].Duration)
                { return; }
            }
            if (width <= 0)
            {
                return;
            }
            DocManager.Inst.ExecuteCmd(new ResizePartCommand(DocManager.Inst.Project, SelectedParts[0], width - SelectedParts[0].Duration, false));
        }

        /// <summary>
        /// End part creation.
        /// </summary>
        public void EndCreatePart()
        {
            IsCreatingPart = false;
            _createPartUndoScope?.Dispose();
            _createPartUndoScope = null;
        }

        /// <summary>
        /// Delete the selected parts.
        /// </summary>
        public void RemoveSelectedParts()
        {
            List<UPart> partsToRemove = [.. SelectedParts];
            using var undo = new UndoScope();
            foreach (UPart part in partsToRemove)
            {
                if (part == null) continue;
                try
                {
                    DocManager.Inst.ExecuteCmd(new RemovePartCommand(DocManager.Inst.Project, part));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    break;
                }
            }
            SelectedParts.Clear();
        }

        public static void AddTrack()
        {
            UTrack track = new UTrack(DocManager.Inst.Project)
            {
                TrackNo = DocManager.Inst.Project.tracks.Count,
                TrackName = AppResources.NewTrack,
                TrackColor = ViewConstants.TrackMauiColors.ElementAt(ObjectProvider.Random.Next(ViewConstants.TrackMauiColors.Count)).Key,
            };
            // Open undo group
            using var undo = new UndoScope();
            DocManager.Inst.ExecuteCmd(new AddTrackCommand(DocManager.Inst.Project, track));
        }

        internal void StartResizePart(UPart part, SKPoint sKPoint)
        {
            _startResizePartPosition = sKPoint; // Record starting position when beginning to resize a part
            IsResizingPart = true; // Mark that a part resize is in progress
            _resizingPart = part; // Record the part being resized
            _resizingPartOriginalDuration = part.Duration; // Record the original duration at resize start
            // Start an undo group
            _resizePartUndoScope = new UndoScope();
        }

        internal void UpdateResizePart(SKPoint sKPoint)
        {
            if (!IsResizingPart || _resizingPart == null)
            {
                return;
            }
            int offsetX = (int)(sKPoint.X - _startResizePartPosition.X);
            int newWidth = Math.Max(0, _resizingPartOriginalDuration + offsetX);
            if (IsTrackSnapToGrid)
            {
                newWidth = TrackTickToLinedTick(_resizingPart.position + newWidth) - _resizingPart.position;
                if (newWidth == _resizingPart.Duration)
                { return; }
            }
            if (newWidth <= 0)
            {
                return;
            }
            DocManager.Inst.ExecuteCmd(new ResizePartCommand(DocManager.Inst.Project, _resizingPart, newWidth - _resizingPart.Duration, false));
        }

        internal void EndResizePart()
        {
            IsResizingPart = false; // Reset part-resize state
            _resizePartUndoScope?.Dispose(); // End undo group
            _resizePartUndoScope = null;
        }

        /// <summary>
        /// Track: get the nearest grid-line tick position.
        /// </summary>
        /// <param name="tick"></param>
        /// <returns>Snapped tick</returns>
        public int TrackTickToLinedTick(int tick)
        {
            if (tick < 0 || TrackSnapDiv <= 0)
            {
                return 0;
            }

            // Calculate the nearest grid-line position
            int linedTick = (int)Math.Round((double)tick / TrackSnapUnitTick) * TrackSnapUnitTick;

            return linedTick;
        }

        /// <summary>
        /// Piano roll: get the nearest grid-line tick position.
        /// </summary>
        /// <param name="tick"></param>
        /// <returns>Snapped tick</returns>
        public int PianoRollTickToLinedTick(int tick)
        {
            if (tick < 0 || PianoRollSnapDiv <= 0)
            {
                return 0;
            }

            // Calculate the nearest grid-line position
            int linedTick = (int)Math.Round((double)tick / PianoRollSnapUnitTick) * PianoRollSnapUnitTick;

            return linedTick;
        }

        /// <summary>
        /// Piano roll: get the preceding grid-line tick position.
        /// </summary>
        /// <param name="tick"></param>
        /// <returns>Snapped tick</returns>
        public int PianoRollTickToFloorLinedTick(int tick)
        {
            if (tick < 0 || PianoRollSnapDiv <= 0)
            {
                return 0;
            }

            // Calculate the nearest preceding grid-line position
            int linedTick = ((int)Math.Floor((double)tick / PianoRollSnapUnitTick)) * PianoRollSnapUnitTick; // parentheses are load-bearing here

            return linedTick;
        }

        /// <summary>
        /// Track: get the preceding grid-line tick position.
        /// </summary>
        /// <param name="tick"></param>
        /// <returns>Snapped tick</returns>
        public int TrackTickToFloorLinedTick(int tick)
        {
            if (tick < 0 || TrackSnapDiv <= 0)
            {
                return 0;
            }

            // Calculate the nearest preceding grid-line position
            int linedTick = ((int)Math.Floor((double)tick / TrackSnapUnitTick)) * TrackSnapUnitTick;

            return linedTick;
        }

        /// <summary>
        /// Move a track up.
        /// </summary>
        /// <param name="track">The track to move up</param>
        /// <returns>Whether the move succeeded</returns>
        public bool MoveTrackUp(UTrack track)
        {
            if (track == DocManager.Inst.Project.tracks.First())
            {
                return false;
            }
            using var undo = new UndoScope();
            DocManager.Inst.ExecuteCmd(new MoveTrackCommand(DocManager.Inst.Project, track, true));
            Tracks = [.. DocManager.Inst.Project.tracks];
            return true;
        }

        /// <summary>
        /// Move a track down.
        /// </summary>
        /// <param name="track">The track to move down</param>
        /// <returns>Whether the move succeeded</returns>
        public bool MoveTrackDown(UTrack track)
        {
            if (track == DocManager.Inst.Project.tracks.Last())
            {
                return false;
            }
            using var undo = new UndoScope();
            DocManager.Inst.ExecuteCmd(new MoveTrackCommand(DocManager.Inst.Project, track, false));
            Tracks = [.. DocManager.Inst.Project.tracks];
            return true;
        }

        public void ToggleTrackMuted(UTrack track)
        {
            track.Muted = !track.Muted;
            Console.WriteLine($"Track {track.TrackNo} Muted: {track.Muted}");
            RefreshTrack(track);
        }

        /// <summary>
        /// Create a default note.
        /// </summary>
        /// <param name="currentPoint">Logical coordinate</param>
        public void CreateDefaultNote(SKPoint currentPoint)
        {
            int tone = (int)Math.Floor(ViewConstants.TotalPianoKeys - currentPoint.Y / Density / HeightPerPianoKey);
            if (tone < 0 || tone >= ViewConstants.TotalPianoKeys) return;
            if (EditingPart is not UVoicePart voicePart) return;
            int position = PianoRollTickToFloorLinedTick((int)currentPoint.X);
            if (position < voicePart.position || position >= voicePart.End) return;

            UNote note = DocManager.Inst.Project.CreateNote(
                noteNum: tone,
                posTick: position - voicePart.position,
                durTick: PianoRollSnapUnitTick
                );
            note.lyric = "a";
            SelectedNotes.Clear();

            using var undo = new UndoScope();
            try
            {
                DocManager.Inst.ExecuteCmd(new AddNoteCommand(voicePart, note));
                SelectedNotes.Add(note);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                IsCreatingPart = false;
                SelectedNotes.Clear();
            }
        }

        public void SetSinger(UTrack track, USinger newSinger)
        {
            if (track.Singer != newSinger)
            {
                using var undo = new UndoScope();
                DocManager.Inst.ExecuteCmd(new TrackChangeSingerCommand(DocManager.Inst.Project, track, newSinger));
                // First try to set the phonemizer preferred by the user for this singer
                if (!string.IsNullOrEmpty(newSinger?.Id) &&
                    OpenUtau.Core.Util.Preferences.Default.SingerPhonemizers.TryGetValue(newSinger.Id, out var phonemizerName) &&
                    TryChangePhonemizer(phonemizerName, track))
                {
                }
                else if (!string.IsNullOrEmpty(newSinger?.DefaultPhonemizer))
                { // Otherwise try the singer's default phonemizer
                    TryChangePhonemizer(newSinger.DefaultPhonemizer, track);
                }
                // If the singer switch failed
                if (newSinger == null || !newSinger.Found)
                {
                    // Reset to the default renderer
                    var settings = new URenderSettings();
                    DocManager.Inst.ExecuteCmd(new TrackChangeRenderSettingCommand(DocManager.Inst.Project, track, settings));
                }
                else if (newSinger.SingerType != track.RendererSettings.Renderer?.SingerType)
                {
                    var settings = new URenderSettings
                    {
                        // Get the default renderer for the singer type
                        renderer = OpenUtau.Core.Render.Renderers.GetDefaultRenderer(newSinger.SingerType),
                    };
                    DocManager.Inst.ExecuteCmd(new TrackChangeRenderSettingCommand(DocManager.Inst.Project, track, settings));
                }
                DocManager.Inst.ExecuteCmd(new VoiceColorRemappingNotification(track.TrackNo, true));
                // Update recent-singers list
                if (!string.IsNullOrEmpty(newSinger?.Id) && newSinger.Found)
                {
                    OpenUtau.Core.Util.Preferences.Default.RecentSingers.Remove(newSinger.Id);
                    OpenUtau.Core.Util.Preferences.Default.RecentSingers.Insert(0, newSinger.Id);
                    if (OpenUtau.Core.Util.Preferences.Default.RecentSingers.Count > 16)
                    {
                        OpenUtau.Core.Util.Preferences.Default.RecentSingers.RemoveRange(
                            16, OpenUtau.Core.Util.Preferences.Default.RecentSingers.Count - 16);
                    }
                }
                OpenUtau.Core.Util.Preferences.Save();
                RefreshTrack(track);
                UpdateIsShowRenderPitchButton();
            }
        }

        private bool TryChangePhonemizer(string phonemizerName, UTrack track)
        {
            try
            {
                var factory = DocManager.Inst.PhonemizerFactories.FirstOrDefault(factory => factory.type.FullName == phonemizerName);
                var phonemizer = factory?.Create();
                if (phonemizer != null)
                {
                    DocManager.Inst.ExecuteCmd(new TrackChangePhonemizerCommand(DocManager.Inst.Project, track, phonemizer));
                    return true;
                }
            }
            catch (Exception e)
            {
                Serilog.Log.Error(e, $"Failed to load phonemizer: {phonemizerName}");
            }
            return false;
        }

        public void ToggleDetailedTrackHeader()
        {
            IsOpenDetailedTrackHeader = !IsOpenDetailedTrackHeader;
            if (IsOpenDetailedTrackHeader)
            {
                HeightPerTrack = 100d;
                AvatarSize = 50d;
            }
            else
            {
                HeightPerTrack = 60d;
                AvatarSize = 35d;
            }
        }

        /// <summary>
        /// Refreshes all tracks.
        /// </summary>
        public void RefreshTrack()
        {
            try
            {
                Tracks.Clear();
                Tracks = [.. DocManager.Inst.Project.tracks];
            }
            catch
            { }
        }

        /// <summary>
        /// Refreshes a single track.
        /// </summary>
        /// <param name="track">The track to refresh.</param>
        public void RefreshTrack(UTrack track)
        {
            try
            {
                int index = Tracks.IndexOf(track);
                if (index >= 0)
                {
                    Tracks.RemoveAt(index);
                    Tracks.Insert(index, track);
                }
            }
            catch
            { }
        }

        public void UpdateTrackVolume(UTrack track, double deltaVolume)
        {
            double newVolume = Math.Clamp(track.Volume + deltaVolume, -60.0, 12.0);
            if (newVolume != track.Volume)
            {
                track.Volume = newVolume;
                DocManager.Inst.ExecuteCmd(new VolumeChangeNotification(track.TrackNo, newVolume));
                Console.WriteLine($"Track {track.TrackNo} Volume: {track.Volume}");
            }
        }

        public void RemoveNotes()
        {
            if (SelectedNotes.Count > 0 && EditingPart is UVoicePart part)
            {
                using var undo = new UndoScope();
                List<UNote> notesToRemove = SelectedNotes.ToList();
                DocManager.Inst.ExecuteCmd(new RemoveNoteCommand(part, notesToRemove));
                SelectedNotes.Clear();
                HandleSelectedNotesChanged();
            }
        }

        public void StartResizeNotes(SKPoint sKPoint, UNote resizingNote)
        {
            if (SelectedNotes.Count == 0 || SelectedNotes == null || EditingPart == null)
            {
                return;
            }
            _resizingNote = resizingNote; // Record note being resized
            IsResizingNote = true; // Mark resize as in progress
            _initialBound2TouchOffset = (int)sKPoint.X - (EditingPart.position + resizingNote.position + resizingNote.duration);
            // Start undo group
            _resizeNotesUndoScope = new UndoScope();
        }

        public void StartMoveNotes(SKPoint sKPoint)
        {
            if (EditingPart == null || SelectedNotes.Count == 0 || SelectedNotes == null)
            {
                return;
            }
            _startMoveNotesPosition = sKPoint; // Starting position when note drag begins
            IsMovingNotes = true; // Mark move as in progress
            _originalPosition = SelectedNotes[0].position; // Original position of first note at move start
            _startMoveNoteToneReversed = (int)Math.Floor(sKPoint.Y / Density / HeightPerPianoKey);
            _offsetPosition = 0; // Reset position offset
            _offsetTone = 0; // Reset tone offset
            // Start undo group
            _moveNotesUndoScope = new UndoScope();
        }

        internal void UpdateMoveNotes(SKPoint point)
        {
            if (!IsMovingNotes || SelectedNotes.Count == 0 || SelectedNotes == null || EditingPart == null)
            {
                return;
            }
            // Compute drag distance
            float newOffsetPosition = point.X - _startMoveNotesPosition.X;
            int hoverToneReversed = (int)Math.Floor((float)(point.Y / Density / HeightPerPianoKey));
            if (IsPianoRollSnapToGrid) // Snap-to-grid: align first note's start to nearest grid line
            {
                int newPosition = PianoRollTickToLinedTick((int)(_originalPosition + newOffsetPosition));
                newOffsetPosition = newPosition - _originalPosition;
            }
            int deltaPosition = (int)newOffsetPosition - _offsetPosition;
            int newOffsetTone = _startMoveNoteToneReversed - hoverToneReversed;
            int deltaTone = newOffsetTone - _offsetTone;
            if (deltaPosition == 0 && deltaTone == 0)
            { return; } // No change — skip update
            Console.WriteLine($"deltaPosition: {newOffsetPosition}, deltaTone: {newOffsetTone}");
            // Update note position
            DocManager.Inst.ExecuteCmd(new MoveNoteCommand(EditingPart, [.. SelectedNotes], deltaPosition, deltaTone));
            _offsetPosition = (int)newOffsetPosition;
            _offsetTone = newOffsetTone;
        }

        internal void UpdateResizeNotes(SKPoint point)
        {
            if (!IsResizingNote || SelectedNotes.Count == 0 || SelectedNotes == null || EditingPart == null || _resizingNote == null)
            {
                return;
            }
            int rightBound = EditingPart.position + _resizingNote.position + _resizingNote.duration;
            int deltaDuration = (int)(point.X - rightBound - _initialBound2TouchOffset);
            if (IsPianoRollSnapToGrid)
            {
                int snapedX = PianoRollTickToLinedTick((int)point.X - _initialBound2TouchOffset);
                deltaDuration = snapedX - rightBound;
            }
            if (deltaDuration == 0)
            { return; } // No change — skip update
            Console.WriteLine($"deltaDuration: {deltaDuration}");
            // Update note duration
            DocManager.Inst.ExecuteCmd(new ResizeNoteCommand(EditingPart, [.. SelectedNotes], deltaDuration));
        }

        public void EndMoveNotes()
        {
            IsMovingNotes = false; // Reset move state
            _moveNotesUndoScope?.Dispose(); // End undo group
            _moveNotesUndoScope = null;
        }

        public void EndResizeNotes()
        {
            IsResizingNote = false; // Reset resize state
            _resizeNotesUndoScope?.Dispose(); // End undo group
            _resizeNotesUndoScope = null;
        }

        /// <summary>
        /// 進行中のノート操作を強制終了する安全網メソッド。
        /// ジェスチャー状態が壊れた可能性がある場合（BUG-C、OnAppearing 等）に呼ぶ。
        /// 冪等: 複数回呼んでも安全。
        /// </summary>
        public void ForceEndAllInteractions()
        {
            if (IsMovingNotes)
            {
                IsMovingNotes = false;
                _moveNotesUndoScope?.Dispose();
                _moveNotesUndoScope = null;
            }
            if (IsResizingNote)
            {
                IsResizingNote = false;
                _resizeNotesUndoScope?.Dispose();
                _resizeNotesUndoScope = null;
            }
        }

        /// <summary>
        /// Samples the existing pitch (in cents) at the specified logical position.
        /// </summary>
        /// <param name="point">Logical coordinates.</param>
        /// <returns>Pitch in cents if a note exists at that position; otherwise null.</returns>
        public double? SamplePitch(SKPoint point)
        {
            if (EditingPart == null)
            {
                return null;
            }
            double tick = point.X;
            var note = EditingPart.notes.FirstOrDefault(n => n.End >= tick);
            if (note == null && EditingPart.notes.Count > 0)
            {
                note = EditingPart.notes.Last();
            }
            if (note == null)
            {
                return null;
            }
            double pitch = note.tone * 100;
            // Add intra-note pitch variation (sampled from the note's pitch control points)
            pitch += note.pitch.Sample(DocManager.Inst.Project, EditingPart, note, tick) ?? 0;
            // Edge case: if the next note starts immediately after this one, blend the transition
            if (note.Next != null && note.Next.position == note.End)
            {
                double? delta = note.Next.pitch.Sample(DocManager.Inst.Project, EditingPart, note.Next, tick);
                if (delta != null)
                {
                    pitch += delta.Value + note.Next.tone * 100 - note.tone * 100;
                }
            }
            return pitch;
        }

        /// <summary>
        /// Converts a tick position and pitch value to a logical view coordinate.
        /// </summary>
        /// <param name="tick">Tick position relative to the entire project.</param>
        /// <param name="pitch">Pitch value in cents.</param>
        /// <returns>Logical coordinates.</returns>
        public SKPoint PitchAndTickToPoint(int tick, double pitch)
        {
            return new SKPoint(
                x: tick,
                y: (float)((ViewConstants.TotalPianoKeys - pitch / 100 - 0.5f) * HeightPerPianoKey * Density)
            );
        }

        /// <summary>
        /// Converts a logical coordinate point to a tick position and pitch value.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="tick"></param>
        /// <param name="pitch"></param>
        public void PointToPitchAndTick(SKPoint point, out int tick, out double pitch)
        {
            tick = (int)point.X;
            pitch = 100 * (ViewConstants.TotalPianoKeys - 0.5d - point.Y / (HeightPerPianoKey * Density));
        }

        public void StartDrawPitch(SKPoint point)
        {
            if (EditingNotes == null)
            {
                return;
            }
            _lastPitchValue = null;
            _lastPitchTick = null;
            // Begin undo group
            _drawPitchUndoScope = new UndoScope();
        }

        /// <summary>
        /// Draws the pitch curve at the given logical position.
        /// </summary>
        /// <param name="point">Logical coordinates.</param>
        public void UpdateDrawPitch(SKPoint point)
        {
            if (EditingPart == null)
            {
                return;
            }

            // Get the tick position and pitch at the current point
            int tick = (int)point.X - EditingPart.position;
            PointToPitchAndTick(point, out _, out double expectedPitch);

            // Sample the baseline pitch at the current position
            SKPoint samplePoint = PitchAndTickToPoint(
                (int)Math.Round(tick / 5.0) * 5,
                expectedPitch);
            double? basePitch = SamplePitch(samplePoint);
            if (basePitch == null)
            {
                Console.WriteLine($"Cannot sample baseline pitch; skipping draw.");
                return;
            }
            //Debug.WriteLine($"Sampled baseline pitch: {basePitch} cent");

            // Calculate the delta between the expected pitch and the baseline (in cents)
            int pitchDelta = (int)Math.Round(expectedPitch - basePitch.Value);
            //Debug.WriteLine($"Draw pitch point: tick={tick}, expectedPitch={expectedPitch}, delta={pitchDelta} cent");

            // Create the curve from the previous point to the current point
            DocManager.Inst.ExecuteCmd(new SetCurveCommand(
                DocManager.Inst.Project,
                EditingPart,
                OpenUtau.Core.Format.Ustx.PITD,
                tick,                 // current point position
                pitchDelta,           // current point pitch delta
                _lastPitchTick ?? tick,    // previous point position (use current on first call)
                (int)(_lastPitchValue != null ? _lastPitchValue.Value : pitchDelta)  // previous point pitch delta (use current on first call)
            ));

            // Update previous-point state; store actual delta, not raw pitch
            _lastPitchTick = tick;
            _lastPitchValue = pitchDelta;
        }

        /// <summary>
        /// Ends pitch curve drawing and commits the undo group.
        /// </summary>
        public void EndDrawPitch()
        {
            _drawPitchUndoScope?.Dispose(); // End undo group
            _drawPitchUndoScope = null;
            _lastPitchValue = null;
            _lastPitchTick = null;
        }

        public void AddTempoSignature(int tick, double bpm)
        {
            using var undo = new UndoScope();
            DocManager.Inst.ExecuteCmd(new AddTempoChangeCommand(DocManager.Inst.Project, tick, bpm));
        }

        internal void AddTimeSignature(int bar, int barPerBeat, int barUnit)
        {
            using var undo = new UndoScope();
            DocManager.Inst.ExecuteCmd(new AddTimeSigCommand(DocManager.Inst.Project, bar, barPerBeat, barUnit));
        }

        public void ImportAudio(string file)
        {
            try
            {
                UProject project = DocManager.Inst.Project;
                UWavePart part = new()
                {
                    FilePath = file,
                };
                part.Load(project);
                if (part == null)
                {
                    return;
                }
                int trackNo = project.tracks.Count;
                part.trackNo = trackNo;
                using var undo = new UndoScope();
                DocManager.Inst.ExecuteCmd(new AddTrackCommand(project, new UTrack(project) { TrackNo = trackNo }));
                DocManager.Inst.ExecuteCmd(new AddPartCommand(project, part));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to import audio");
            }
        }

        internal void RemoveTrack(UTrack track)
        {
            using var undo = new UndoScope();
            DocManager.Inst.ExecuteCmd(new RemoveTrackCommand(DocManager.Inst.Project, track));
            RefreshTrack();
        }

        internal void LoadPortrait()
        {
            //Debug.WriteLine("Attempting to load singer portrait");
            if (EditingPart == null)
            {
                CurrentPortrait = [];
                return;
            }
            if (OpenUtau.Core.Util.Preferences.Default.ShowPortrait)
            {
                //Debug.WriteLine($"Attempting to load singer portrait");
                if (DocManager.Inst.Project.tracks[EditingPart.trackNo].Singer is USinger singer && singer != null)
                {
                    CurrentPortrait = singer.LoadPortrait();
                    //Debug.WriteLine($"Loaded singer portrait {singer.Name}, images size: {CurrentPortrait.Length}");
                    PortraitOpacity = Preferences.Default.CustomPortraitOptions ? Preferences.Default.PortraitOpacity : singer.PortraitOpacity;
                }
            }
        }

        public void SetTimeSignature(int beatPerBar, int beatUnit)
        {
            using var undo = new UndoScope();
            DocManager.Inst.ExecuteCmd(new TimeSignatureCommand(DocManager.Inst.Project, beatPerBar, beatUnit));
        }

        public void SetBpm(double bpm)
        {
            if (bpm == DocManager.Inst.Project.tempos[0].bpm)
            {
                return;
            }
            using var undo = new UndoScope();
            DocManager.Inst.ExecuteCmd(new BpmCommand(DocManager.Inst.Project, bpm));
        }

        public void SetKey(int key)
        {
            if (key == DocManager.Inst.Project.key)
            {
                return;
            }
            using var undo = new UndoScope();
            DocManager.Inst.ExecuteCmd(new KeyCommand(DocManager.Inst.Project, key));
        }

        public void RenderPitchAsync(
            UVoicePart part, List<UNote> selectedNotes,
            Action<string, int, int> setProgressCallback, CancellationToken cancellationToken, string workId)
        {
            Console.WriteLine("Starting pitch render");
            UProject project = DocManager.Inst.Project;
            var renderer = project.tracks[part.trackNo].RendererSettings.Renderer; // Get renderer for this track
            if (renderer == null || !renderer.SupportsRenderPitch)
            {
                DocManager.Inst.ExecuteCmd(new ErrorMessageNotification("Not supported")); // Show error notification
                return;
            }
            var notes = selectedNotes.Count > 0 ? selectedNotes : part.notes.ToList(); // Get list of selected notes
            var positions = notes.Select(n => n.position + part.position).ToHashSet(); // Get absolute positions of selected notes
            var phrases = part.renderPhrases.Where(phrase => phrase.notes.Any(n => positions.Contains(phrase.position + n.position))).ToArray(); // Get render phrases containing selected notes
            float minPitD = -1200;
            if (project.expressions.TryGetValue(OpenUtau.Core.Format.Ustx.PITD, out var descriptor))
            {
                minPitD = descriptor.min; // Get PITD expression minimum value
            }

            int finished = 0;
            setProgressCallback(workId, 0, phrases.Length);
            List<SetCurveCommand> commands = new List<SetCurveCommand>();
            for (int ph_i = phrases.Count() - 1; ph_i >= 0; ph_i--)
            { // Iterate each render phrase
                Console.WriteLine($"Rendering pitch phrase {ph_i + 1}/{phrases.Length}");
                var phrase = phrases[ph_i];
                var result = renderer.LoadRenderedPitch(phrase);
                if (result == null)
                {
                    continue;
                }
                int? lastX = null;
                int? lastY = null;
                // TODO: Optimize interpolation and command.
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                // Take the first negative tick before start and the first tick after end for each segment;
                // Reverse traversal, so that when the score slices are too close, priority is given to covering the consonant pitch of the next segment, reducing the impact on vowels.
                for (int i = 0; i < result.tones.Length; i++)
                {
                    if (result.tones[i] < 0)
                    {
                        continue;
                    }
                    int x = phrase.position - part.position + (int)result.ticks[i];
                    if (result.ticks[i] < 0)
                    {
                        if (i + 1 < result.ticks.Length && result.ticks[i + 1] > 0) { }
                        else
                            continue;
                    }
                    if (x >= phrase.position + phrase.duration)
                    {
                        i = result.tones.Length - 1;
                    }
                    int pitchIndex = Math.Clamp((x - (phrase.position - part.position - phrase.leading)) / 5, 0, phrase.pitches.Length - 1);
                    float basePitch = phrase.pitchesBeforeDeviation[pitchIndex];
                    int y = (int)(result.tones[i] * 100 - basePitch);
                    lastX ??= x;
                    lastY ??= y;
                    if (y > minPitD)
                    {
                        commands.Add(new SetCurveCommand(
                            project, part, OpenUtau.Core.Format.Ustx.PITD, x, y, lastX.Value, lastY.Value));
                    }
                    lastX = x;
                    lastY = y;
                }
                finished += 1;
                setProgressCallback(workId, finished, phrases.Length);
            }
            setProgressCallback(workId, phrases.Length, phrases.Length);

            DocManager.Inst.PostOnUIThread(() =>
            {
                using var undo = new UndoScope();
                commands.ForEach(DocManager.Inst.ExecuteCmd);
            });
        }

        public void ImportMidi(string file)
        {
            if (file == null)
            {
                return;
            }
            UProject project = DocManager.Inst.Project;
            List<UVoicePart> parts = OpenUtau.Core.Format.MidiWriter.Load(file, project);
            using var undo = new UndoScope();
            foreach (UVoicePart part in parts)
            {
                UTrack track = new(project)
                {
                    TrackNo = project.tracks.Count
                };
                part.trackNo = track.TrackNo;
                if (part.name != AppResources.NewPart)
                {
                    track.TrackName = part.name;
                }
                part.AfterLoad(project, track);
                DocManager.Inst.ExecuteCmd(new AddTrackCommand(project, track));
                DocManager.Inst.ExecuteCmd(new AddPartCommand(project, part));
            }
        }
        /// <summary>
        /// Begins drawing an expression curve at the given position.
        /// </summary>
        /// <param name="point">Actual canvas coordinates.</param>
        /// <param name="canvasHeight">Actual canvas height (before Density scaling).</param>
        public void StartDrawExpression(SKPoint point, float canvasHeight)
        {
            if (EditingPart == null)
            {
                return;
            }
            UProject project = DocManager.Inst.Project;
            UTrack track = DocManager.Inst.Project.tracks[EditingPart.trackNo];
            if (!track.TryGetExpDescriptor(project, PrimaryExpressionAbbr, out _editingExpressionDescriptor)) // Try to get descriptor by abbreviation (e.g. DYN)
            {
                // Failed — clear descriptor and return
                _editingExpressionDescriptor = null;
                return;
            }
            if (_editingExpressionDescriptor.max <= _editingExpressionDescriptor.min)
            {
                // Invalid descriptor
                return;
            }
            _lastExpTick = (int)PianoRollTransformer.ActualToLogicalX(point.X) - EditingPart.position;
            _lastExpValue = (int)(_editingExpressionDescriptor.max - point.Y * (_editingExpressionDescriptor.max - _editingExpressionDescriptor.min) / (float)canvasHeight / (float)Density);
            _drawExpressionUndoScope = new UndoScope();
        }
        /// <summary>
        /// Continues drawing an expression curve as the pointer moves.
        /// </summary>
        /// <param name="point">Actual canvas coordinates.</param>
        /// <param name="canvasHeight">Actual canvas height (before Density scaling).</param>
        public void UpdateDrawExpression(SKPoint point, float canvasHeight)
        {
            if (EditingPart == null || _editingExpressionDescriptor == null)
            {
                return;
            }
            int currentTick = (int)PianoRollTransformer.ActualToLogicalX(point.X) - EditingPart.position;
            float currentValueExact = (_editingExpressionDescriptor.max - point.Y * (_editingExpressionDescriptor.max - _editingExpressionDescriptor.min) / (float)canvasHeight / (float)Density);
            int currentValue;
            if (_editingExpressionDescriptor.type == UExpressionType.Curve)
            {
                currentValue = (int)currentValueExact;
                currentExpressionValue = currentValue;
                UpdateDrawCurveExpression(currentTick, currentValue);
            }
            else
            {
                // Round to nearest integer
                currentValue = (int)Math.Round(currentValueExact);
                currentExpressionValue = currentValue;
                UpdateDrawPhonemeExp(currentTick, currentValue);
            }
            _lastExpTick = currentTick;
            _lastExpValue = currentValue;
        }
        /// <summary>
        /// Updates a curve-type expression at the current tick.
        /// </summary>
        /// <param name="currentTick"></param>
        /// <param name="currentValue"></param>
        private void UpdateDrawCurveExpression(int currentTick, int currentValue)
        {
            if (EditingPart == null)
            {
                return;
            }
            DocManager.Inst.ExecuteCmd(new SetCurveCommand(
                DocManager.Inst.Project,
                EditingPart,
                PrimaryExpressionAbbr,
                currentTick,
                currentValue,
                _lastExpTick,
                _lastExpValue
            ));
        }
        /// <summary>
        /// Updates a phoneme expression value at the current tick.
        /// </summary>
        /// <param name="currentTick"></param>
        /// <param name="currentValue"></param>
        private void UpdateDrawPhonemeExp(int currentTick, int currentValue)
        {
            if (EditingPart == null || _editingExpressionDescriptor == null)
            {
                return;
            }
            UProject project = DocManager.Inst.Project;
            UTrack track = DocManager.Inst.Project.tracks[EditingPart.trackNo];
            List<NoteHitInfo> hits = HitTestExpRange(_lastExpTick, currentTick);
            foreach (var hit in hits)
            {
                if (Preferences.Default.LockUnselectedNotesExpressions && SelectedNotes.Count > 0 && !SelectedNotes.Contains(hit.phoneme.Parent))
                {
                    continue;
                }
                float x = hit.note.position + hit.phoneme.position;
                // Only interpolate for points within the dragged range
                int y = currentValue;
                if (x >= Math.Min(_lastExpTick, currentTick) && x <= Math.Max(_lastExpTick, currentTick))
                {
                    y = (int)Lerp(_lastExpTick, _lastExpValue, currentTick, currentValue, x);
                }
                y = Math.Clamp(y, (int)_editingExpressionDescriptor.min, (int)_editingExpressionDescriptor.max);

                float oldValue = hit.phoneme.GetExpression(DocManager.Inst.Project, track, PrimaryExpressionAbbr).Item1;
                if (y == (int)oldValue)
                {
                    continue;
                }
                DocManager.Inst.ExecuteCmd(new SetPhonemeExpressionCommand(
                        project,
                        track,
                        EditingPart, 
                        hit.phoneme, 
                        PrimaryExpressionAbbr,
                        y));
            }
        }
        /// <summary>
        /// Linear interpolation between two (x, y) points at position x.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static float Lerp(float x1, float y1, float x2, float y2, float x)
        {
            const float EPSILON = 1e-6f;
            if (Math.Abs(x2 - x1) < EPSILON)
            {
                return y1;
            }
            return y1 + (y2 - y1) * (x - x1) / (x2 - x1);
        }
        /// <summary>
        /// Ends expression curve drawing and commits the undo group.
        /// </summary>
        public void EndDrawExpression()
        {
            _drawExpressionUndoScope?.Dispose();
            _drawExpressionUndoScope = null;
        }

        /// <summary>
        /// Hit-tests phonemes within the given tick range.
        /// </summary>
        /// <param name="tick1">Start tick.</param>
        /// <param name="tick2">End tick.</param>
        /// <returns>List of hit results.</returns>
        public List<NoteHitInfo> HitTestExpRange(int tick1, int tick2)
        {
            if (tick1 > tick2)
            {
                (tick1, tick2) = (tick2, tick1);
            }
            var hits = new List<NoteHitInfo>();
            if (EditingPart == null)
            {
                return hits;
            }
            foreach (var phoneme in EditingPart.phonemes)
            {
                double leftBound = phoneme.position;
                double rightBound = phoneme.End;
                var note = phoneme.Parent;
                if (leftBound > tick2 || rightBound < tick1)
                {
                    continue;
                }
                int left = phoneme.position;
                int right = phoneme.End;
                if (left <= tick2 && tick1 <= right)
                {
                    hits.Add(new NoteHitInfo(note, phoneme)
                    {
                        hitX = true,
                    });
                }
            }
            return hits;
        }

        /// <summary>
        /// Holds hit-test result information for a note/phoneme.
        /// </summary>
        public class NoteHitInfo
        {
            public UNote note;
            public UPhoneme phoneme;
            public bool hitBody;
            public bool hitResizeArea;
            public bool hitResizeAreaFromStart;
            public bool hitX;
            public NoteHitInfo(UNote note, UPhoneme phoneme)
            {
                this.note = note;
                this.phoneme = phoneme;
            }
            public NoteHitInfo(UNote note, UPhoneme phoneme, bool hitBody, bool hitResizeArea, bool hitResizeAreaFromStart, bool hitX)
            {
                this.note = note;
                this.phoneme = phoneme;
                this.hitBody = hitBody;
                this.hitResizeArea = hitResizeArea;
                this.hitResizeAreaFromStart = hitResizeAreaFromStart;
                this.hitX = hitX;
            }
        }
        /// <summary>
        /// Begins resetting an expression curve at the given position.
        /// </summary>
        /// <param name="point">Actual canvas coordinates.</param>
        public void StartResetExpression(SKPoint point)
        {
            if (EditingPart == null)
            {
                return;
            }
            UProject project = DocManager.Inst.Project;
            UTrack track = DocManager.Inst.Project.tracks[EditingPart.trackNo];
            if (!track.TryGetExpDescriptor(project, PrimaryExpressionAbbr, out _editingExpressionDescriptor)) // Try to get descriptor by abbreviation (e.g. DYN)
            {
                // Failed — clear descriptor and return
                _editingExpressionDescriptor = null;
                return;
            }
            if (_editingExpressionDescriptor.max <= _editingExpressionDescriptor.min)
            {
                // Invalid descriptor
                return;
            }
            _lastExpTick = (int)PianoRollTransformer.ActualToLogicalX(point.X) - EditingPart.position;
            //_lastExpValue = (int)(_editingExpressionDescriptor.max - point.Y * (_editingExpressionDescriptor.max - _editingExpressionDescriptor.min) / (float)canvasHeight / (float)Density);
            _resetExpressionUndoScope = new UndoScope();
        }
        /// <summary>
        /// Continues resetting an expression curve as the pointer moves.
        /// </summary>
        /// <param name="point">Actual canvas coordinates.</param>
        public void UpdateResetExpression(SKPoint point)
        {
            if (EditingPart == null || _editingExpressionDescriptor == null)
            {
                return;
            }
            int currentTick = (int)PianoRollTransformer.ActualToLogicalX(point.X) - EditingPart.position;
            //float currentValueExact = (_editingExpressionDescriptor.max - point.Y * (_editingExpressionDescriptor.max - _editingExpressionDescriptor.min) / (float)canvasHeight / (float)Density);
            //int currentValue;
            if (_editingExpressionDescriptor.type == UExpressionType.Curve)
            {
                //currentValue = (int)currentValueExact;
                currentExpressionValue = (int)_editingExpressionDescriptor.defaultValue;
                UpdateResetCurveExpression(currentTick);
            }
            else
            {
                // Round to nearest integer
                //currentValue = (int)Math.Round(currentValueExact);
                currentExpressionValue = 0;
                UpdateResetPhonemeExp(currentTick);
            }
            _lastExpTick = currentTick;
            //_lastExpValue = currentValue;
        }
        /// <summary>
        /// Resets a curve-type expression to its default value at the current tick.
        /// </summary>
        /// <param name="currentTick"></param>
        private void UpdateResetCurveExpression(int currentTick)
        {
            if (EditingPart == null || _editingExpressionDescriptor == null)
            {
                return;
            }
            DocManager.Inst.ExecuteCmd(new SetCurveCommand(
                DocManager.Inst.Project,
                EditingPart,
                PrimaryExpressionAbbr,
                currentTick,
                (int)_editingExpressionDescriptor.defaultValue,
                _lastExpTick,
                (int)_editingExpressionDescriptor.defaultValue
            ));
        }
        /// <summary>
        /// Resets a phoneme expression to null (default) at the current tick.
        /// </summary>
        /// <param name="currentTick"></param>
        private void UpdateResetPhonemeExp(int currentTick)
        {
            if (EditingPart == null || _editingExpressionDescriptor == null)
            {
                return;
            }
            UProject project = DocManager.Inst.Project;
            UTrack track = DocManager.Inst.Project.tracks[EditingPart.trackNo];
            List<NoteHitInfo> hits = HitTestExpRange(_lastExpTick, currentTick);
            foreach (var hit in hits)
            {
                if (Preferences.Default.LockUnselectedNotesExpressions && SelectedNotes.Count > 0 && !SelectedNotes.Contains(hit.phoneme.Parent))
                {
                    continue;
                }
                //float x = hit.note.position + hit.phoneme.position;
                // Only interpolate for points within the dragged range
                //int y = currentValue;
                //if (x > Math.Max(_lastExpTick, currentTick) && x < Math.Min(_lastExpTick, currentTick))
                //{
                //    y = (int)Lerp(_lastExpTick, _lastExpValue, currentTick, currentValue, x);
                //}
                //y = Math.Clamp(y, (int)_editingExpressionDescriptor.min, (int)_editingExpressionDescriptor.max);

                //float oldValue = hit.phoneme.GetExpression(DocManager.Inst.Project, track, PrimaryExpressionAbbr).Item1;
                //if (y == (int)oldValue)
                //{
                //    continue;
                //}
                DocManager.Inst.ExecuteCmd(new SetPhonemeExpressionCommand(
                        project,
                        track,
                        EditingPart,
                        hit.phoneme,
                        PrimaryExpressionAbbr,
                        null));
            }
        }
        /// <summary>
        /// Ends expression curve reset and commits the undo group.
        /// </summary>
        public void EndResetExpression()
        {
            _resetExpressionUndoScope?.Dispose();
            _resetExpressionUndoScope = null;
        }
        public void CopySelectedNotes()
        {
            if (SelectedNotes.Count == 0) return;
            _clipboard.Clear();
            foreach (var note in SelectedNotes)
            {
                // We clone the note so changes to the original don't affect the clipboard
                _clipboard.Add(note.Clone());
            }
            Console.WriteLine($"Copied {_clipboard.Count} notes.");
        }
        public void PasteNotes()
        {
            // Validation: Must have data and a valid destination part
            if (_clipboard.Count == 0 || EditingPart == null) return;
            if (EditingPart is not UVoicePart voicePart) return;

            // Determine the "Anchor" of the clipboard
            // We find the earliest start position among the copied notes.
            // This ensures that if you copy a melody, the FIRST note hits the cursor.
            int clipboardMinPos = _clipboard.Min(n => n.position);

            // Calculate Target Position in Local Part Time
            // PlayPosTick is Global Project Time. We must convert it to Local Part Time.
            // This logic allows copying from Part A and pasting into Part B correctly.
            int targetLocalTick = PlayPosTick - voicePart.position;

            // Snap to Grid (Quantization)
            // If snapping is on, we align the paste target to the nearest grid line
            if (IsPianoRollSnapToGrid)
            {
                targetLocalTick = PianoRollTickToLinedTick(targetLocalTick);
            }

            // Calculate the Shift Offset
            // How far do we move the notes? 
            // Target - Anchor = The distance to shift.
            int offset = targetLocalTick - clipboardMinPos;

            List<UNote> newlyPastedNotes = new();

            using var undo = new UndoScope();
            try
            {
                foreach (var clipNote in _clipboard)
                {
                    // Clone again so we can paste multiple times without reference issues
                    var newNote = clipNote.Clone();

                    // Apply the offset to preserve relative rhythm and chords
                    newNote.position += offset;

                    // Safety: Don't allow notes to exist before the start of the part
                    if (newNote.position < 0) continue;

                    // Add the command to the queue
                    DocManager.Inst.ExecuteCmd(new AddNoteCommand(voicePart, newNote));
                    newlyPastedNotes.Add(newNote);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to paste notes");
            }

            // Update Selection to the new notes
            // This is standard UI behavior: after pasting, select the new items
            if (newlyPastedNotes.Count > 0)
            {
                SelectedNotes.Clear();
                SelectedNotes.AddRange(newlyPastedNotes);
                HandleSelectedNotesChanged();

                // Force the canvas to redraw to show the new notes
                MessageBus.Current.SendMessage(new RefreshCanvasMessage());
            }
        }
        public void SelectAllNotes()
        {
            if (EditingPart is not UVoicePart voicePart)
            {
                SelectedNotes.Clear();
                return;
            }

            SelectedNotes.Clear();

            // Select all notes from the voice part's note list
            SelectedNotes.AddRange(voicePart.notes);

            HandleSelectedNotesChanged();

            // Request canvas update
            MessageBus.Current.SendMessage(new RefreshCanvasMessage());

            Console.WriteLine($"Selected {SelectedNotes.Count} notes.");
        }
        /// <summary>
        /// Imports tracks from loaded projects. This operation cannot be undone.
        /// </summary>
        /// <param name="loadedProjects"></param>
        /// <param name="importTempo"></param>
        public void ImportTracks(UProject[] loadedProjects, bool importTempo){
            if (loadedProjects == null || loadedProjects.Length < 1) {
                return;
            }
            OpenUtau.Core.Format.Formats.ImportTracks(DocManager.Inst.Project, loadedProjects, importTempo);
        }

        /// <summary>
        /// Transcribes a dry audio wave part to a voice part.
        /// </summary>
        /// <param name="wavePart"></param>
        public async Task<UVoicePart?> AudioTranscribe(UWavePart wavePart, Action<double, string> progress)
        {
            if (wavePart == null)
            {
                return null;
            }
            int wavDurS = (int)(wavePart.fileDurationMs / 1000.0);
            Task<UVoicePart> transcribeTask = Task.Run(() =>
            {
                using Some some = new();
                return some.Transcribe(DocManager.Inst.Project, wavePart, wavPosS =>
                {
                    Console.WriteLine($"Transcription progress: {wavPosS}/{wavDurS}");
                    progress.Invoke((double)wavPosS / wavDurS * 100, $"{wavePart.name}\n{wavPosS}/{wavDurS}");
                });
            });
            UVoicePart? result = await transcribeTask.ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Log.Error(task.Exception, $"Failed to transcribe part {wavePart.name}");
                    DocManager.Inst.ExecuteCmd(new ErrorMessageNotification("Audio transcription failed", task.Exception));
                    return null;
                }
                UVoicePart voicePart = task.Result;
                if (voicePart != null)
                {
                    return voicePart;
                }
                return null;
            });
            return result;
        }

        #region Vibrato Edit Methods

        /// <summary>
        /// 選択ノートの最初のノートのビブラートパラメータを返す。UI 初期値用。
        /// </summary>
        public UVibrato? GetVibratoForSelectedNote()
        {
            if (SelectedNotes.Count == 0) return null;
            return SelectedNotes[0].vibrato;
        }

        /// <summary>
        /// 選択ノートのビブラートを有効/無効トグルする。
        /// 有効化時は length=50 (50%) に設定。無効化時は length=0。
        /// </summary>
        public void ToggleVibratoForSelectedNotes()
        {
            if (SelectedNotes.Count == 0 || EditingPart is not UVoicePart voicePart) return;
            bool anyEnabled = SelectedNotes.Any(n => n.vibrato.length > 0);
            float newLength = anyEnabled ? 0f : 50f;
            using var undo = new UndoScope();
            foreach (var note in SelectedNotes)
                DocManager.Inst.ExecuteCmd(new VibratoLengthCommand(voicePart, note, newLength));
        }

        /// <summary>ビブラート長さ (0〜100 %) を選択ノートに適用する。</summary>
        public void SetVibratoLength(float length)
        {
            if (SelectedNotes.Count == 0 || EditingPart is not UVoicePart voicePart) return;
            using var undo = new UndoScope();
            foreach (var note in SelectedNotes)
                DocManager.Inst.ExecuteCmd(new VibratoLengthCommand(voicePart, note, length));
        }

        /// <summary>ビブラート深さ (5〜200 cents) を選択ノートに適用する。</summary>
        public void SetVibratoDepth(float depthCents)
        {
            if (SelectedNotes.Count == 0 || EditingPart is not UVoicePart voicePart) return;
            using var undo = new UndoScope();
            foreach (var note in SelectedNotes)
                DocManager.Inst.ExecuteCmd(new VibratoDepthCommand(voicePart, note, depthCents));
        }

        /// <summary>ビブラート周期 (5〜500 ms) を選択ノートに適用する。</summary>
        public void SetVibratoPeriod(float periodMs)
        {
            if (SelectedNotes.Count == 0 || EditingPart is not UVoicePart voicePart) return;
            using var undo = new UndoScope();
            foreach (var note in SelectedNotes)
                DocManager.Inst.ExecuteCmd(new VibratoPeriodCommand(voicePart, note, periodMs));
        }

        /// <summary>ビブラートフェードイン (0〜100 %) を選択ノートに適用する。</summary>
        public void SetVibratoFadeIn(float fadeIn)
        {
            if (SelectedNotes.Count == 0 || EditingPart is not UVoicePart voicePart) return;
            using var undo = new UndoScope();
            foreach (var note in SelectedNotes)
                DocManager.Inst.ExecuteCmd(new VibratoFadeInCommand(voicePart, note, fadeIn));
        }

        /// <summary>ビブラートフェードアウト (0〜100 %) を選択ノートに適用する。</summary>
        public void SetVibratoFadeOut(float fadeOut)
        {
            if (SelectedNotes.Count == 0 || EditingPart is not UVoicePart voicePart) return;
            using var undo = new UndoScope();
            foreach (var note in SelectedNotes)
                DocManager.Inst.ExecuteCmd(new VibratoFadeOutCommand(voicePart, note, fadeOut));
        }

        #endregion

    #region Phoneme Edit Methods

        /// <summary>
        /// 指定インデックスのフォネームのオフセット（ティック）を設定する。
        /// 0 を渡すとオーバーライドをクリアする。
        /// </summary>
        public void SetPhonemeOffset(UNote note, int phonemeIndex, int offsetTicks)
        {
            if (EditingPart is not UVoicePart voicePart) return;
            using var undo = new UndoScope();
            DocManager.Inst.ExecuteCmd(new PhonemeOffsetCommand(voicePart, note, phonemeIndex, offsetTicks));
        }

        /// <summary>
        /// 指定インデックスのフォネームのプレアッター差分（ms）を設定する。
        /// 0 を渡すとオーバーライドをクリアする。
        /// </summary>
        public void SetPhonemePreutter(UNote note, int phonemeIndex, float deltaMsec)
        {
            if (EditingPart is not UVoicePart voicePart) return;
            using var undo = new UndoScope();
            DocManager.Inst.ExecuteCmd(new PhonemePreutterCommand(voicePart, note, phonemeIndex, deltaMsec));
        }

        /// <summary>
        /// 指定インデックスのフォネームのオーバーラップ差分（ms）を設定する。
        /// 0 を渡すとオーバーライドをクリアする。
        /// </summary>
        public void SetPhonemeOverlap(UNote note, int phonemeIndex, float deltaMsec)
        {
            if (EditingPart is not UVoicePart voicePart) return;
            using var undo = new UndoScope();
            DocManager.Inst.ExecuteCmd(new PhonemeOverlapCommand(voicePart, note, phonemeIndex, deltaMsec));
        }

        /// <summary>
        /// 指定インデックスのフォネームエイリアスを上書きする。
        /// null を渡すとオーバーライドをクリアする。
        /// </summary>
        public void SetPhonemeAlias(UNote note, int phonemeIndex, string? alias)
        {
            if (EditingPart is not UVoicePart voicePart) return;
            using var undo = new UndoScope();
            DocManager.Inst.ExecuteCmd(new ChangePhonemeAliasCommand(voicePart, note, phonemeIndex, alias));
        }

        /// <summary>
        /// 選択中の全ノートのフォネームタイミングオーバーライドを一括クリアする。
        /// </summary>
        public void ClearPhonemeTimingForSelectedNotes()
        {
            if (SelectedNotes.Count == 0 || EditingPart is not UVoicePart voicePart) return;
            using var undo = new UndoScope();
            foreach (var note in SelectedNotes)
                DocManager.Inst.ExecuteCmd(new ClearPhonemeTimingCommand(voicePart, note));
        }

    #endregion
    }
}