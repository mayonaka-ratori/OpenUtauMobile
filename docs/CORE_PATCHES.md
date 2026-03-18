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

(None yet. Add entries here as modifications are made.)

### Template for new entries

- **Date**: YYYY-MM-DD
- **File**: path/to/file.cs
- **Change**: Brief description of what was changed
- **Reason**: Why this Core modification was necessary
- **Upstream PR**: Link if submitted, or "N/A — mobile-specific"
