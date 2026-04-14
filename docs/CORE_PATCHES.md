# Core Patches Log

This file documents all modifications made to OpenUtau.Core/ and
OpenUtau.Plugin.Builtin/ in this mobile fork.

These directories are shared with the desktop version (stakira/OpenUtau).
Modifications should be minimal and well-documented to ease future upstream merges.

## Pre-existing patches (from vocoder712/OpenUtauMobile)

These patches were already present when this fork was created.

### 1. Target framework change

- **File**: OpenUtau.Core/OpenUtau.Core.csproj
- **Change**: TargetFrameworks from `net8.0` to `net9.0;net9.0-android;net9.0-ios`
- **Reason**: MAUI requires platform-specific TFMs

### 2. Maui Essentials dependency

- **File**: OpenUtau.Core/OpenUtau.Core.csproj
- **Change**: Added `Microsoft.Maui.Essentials 9.0.30` package reference
- **Reason**: Required for platform-specific file access and app lifecycle

### 3. DependencyInstaller

- **File**: OpenUtau.Core/DependencyInstaller.cs (new file)
- **Change**: Added mobile-specific dependency resolution
- **Reason**: Desktop uses file-system plugin discovery; mobile bundles plugins in APK

### 4. Plugin loading for Android

- **File**: OpenUtau.Core/DocManager.cs (SearchAllPlugins method)
- **Change**: Uses `Assembly.Load("OpenUtau.Plugin.Builtin")` instead of `Assembly.LoadFile(path)`
- **Reason**: On Android, plugin DLLs are embedded in the APK, not separate files on disk

### 5. Missing PackageManager

- **File**: OpenUtau.Core/PackageManager.cs
- **Change**: Does NOT exist in this fork (exists in upstream v0.1.567+)
- **Reason**: Not yet synced from upstream. Needed for voicebank package management.

## Patches added by this fork

### 6. Atomic write for Save / AutoSave

- **Date**: 2026-04-14
- **File**: OpenUtau.Core/Format/USTx.cs (`Save()` lines 97-112, `AutoSave()` lines 114-127)
- **Change**: Replaced `File.WriteAllText(filePath, ...)` with temp-file + `File.Move(overwrite: true)` pattern in both `Save()` and `AutoSave()`. Temp file is `filePath + ".tmp"`.
- **Reason**: On mobile, the OS can suspend the process mid-write during background transitions, leaving a corrupt partial file. Atomic rename ensures the destination is always complete or absent.
- **Upstream PR**: N/A — mobile-specific risk mitigation

### 7. TelemetryOptIn / CrashReportShareOptIn preference fields

- **Date**: 2026-04-14
- **File**: OpenUtau.Core/Util/Preferences.cs (`SerializablePreferences`, within `#region OpenUtau Mobile 特定选项`)
- **Change**: Added two fields:
  - `public bool TelemetryOptIn = true;` — controls whether local crash/performance telemetry is recorded
  - `public bool CrashReportShareOptIn = false;` — gates whether crash signatures are included in exported support bundles
- **Reason**: Telemetry opt-in state must persist across sessions. Storing in `SerializablePreferences` reuses the existing JSON serialisation / save path. Both fields are mobile-only and have no effect on the desktop build.
- **Upstream PR**: N/A — mobile-specific

### 8. Runtime cut_off clamp in Worldline SynthRequestWrapper

- **Date**: 2026-04-14
- **File**: OpenUtau.Core/Render/Worldline.cs (`SynthRequestWrapper` constructor, before `Validate()` call)
- **Change**: Added a pre-validation clamp for positive `cut_off` values. If `total_ms - offset - cut_off < 10ms`, `cut_off` is reduced so that at least 10ms of audio remains. Logs `[0-4]` warning via Serilog. Negative `cut_off` (consonant-relative length) is unaffected.
- **Reason**: On mobile, malformed OTO entries with `cutoff > usable audio` previously threw `CutOffExceedDurationError` and aborted rendering entirely. The clamp degrades gracefully to a slightly shorter render window rather than crashing. The warning log feeds into the 0-1 telemetry pipeline.
- **Upstream PR**: N/A — defensive fix; upstream also throws, making this a mobile-specific mitigation

### 9. Explicit cutoff > fileDuration check in VoicebankErrorChecker

- **Date**: 2026-04-14
- **File**: OpenUtau.Core/Classic/VoicebankErrorChecker.cs (`CheckOto()`, before the `cutoff` variable computation)
- **Change**: Added early-return check: when `oto.Cutoff > 0 && oto.Cutoff > fileDuration`, reports a specific error message including the actual cutoff value, file duration, and a suggested safe cutoff (`≤ fileDuration - offset - max(consonant, 10) ms`). Returns `false` immediately to avoid cascading misleading "Cutoff must be to the right of X" errors.
- **Reason**: Previously, a positive cutoff exceeding file duration would produce negative intermediate values, causing every downstream check to also fail with unrelated messages (e.g. "Cutoff must be to the right of preutter"). The new message gives voice bank authors the exact numbers they need to correct the OTO entry.
- **Upstream PR**: Candidate for upstream submission — pure diagnostic improvement, no behaviour change at runtime

### Template for new entries

- **Date**: YYYY-MM-DD
- **File**: path/to/file.cs
- **Change**: Brief description of what was changed
- **Reason**: Why this Core modification was necessary
- **Upstream PR**: Link if submitted, or "N/A — mobile-specific"
