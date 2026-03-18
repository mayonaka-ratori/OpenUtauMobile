---
name: project-overview
description: OpenUtau Mobile project architecture, solution structure, Core version delta, NuGet dependencies, and mobile app folder layout. Load when understanding the overall project or onboarding.
---

# Project Overview

## Architecture

- Engine: OpenUtau.Core/ + OpenUtau.Plugin.Builtin/ (shared with desktop version)
- App: OpenUtauMobile/ — .NET MAUI, MVVM (ReactiveUI + CommunityToolkit.Mvvm)
- Drawing: SkiaSharp (SKCanvasView)
- State management: DocManager singleton (OpenUtau.Core/DocManager.cs)
- Runtime: .NET 9, C# 13
- Platforms: Android 5.0+, iOS 15.0+

## Core version

Based on upstream OpenUtau v0.1.565 with mobile-specific patches.
Version string in OpenUtau.Core/OpenUtau.Core.csproj: 0.1.565-patch2
Upstream (stakira/OpenUtau) latest: v0.1.567. About 2 versions behind.

Note: master branch is ahead of v1.1.7 release tag.
PR #106 (iOS support) and PR #107 (icon fix) are merged into master but not released.

## Core patches (Mobile-specific, already applied)

These are differences from upstream Core that already exist in this repo:

- TargetFrameworks changed to net9.0;net9.0-android;net9.0-ios (upstream: net8.0)
- Microsoft.Maui.Essentials added as dependency (not in upstream)
- DependencyInstaller.cs added (not in upstream)
- DocManager.SearchAllPlugins() patched: uses Assembly.Load instead of
  Assembly.LoadFile for Android, where built-in plugin DLLs are not separate files
- PackageManager.cs does NOT exist (added in upstream v0.1.567, not yet synced)

## Solution structure

OpenUtauMobile.sln contains 3 projects:

- OpenUtauMobile (MAUI app)
- OpenUtau.Core (engine)
- OpenUtau.Plugin.Builtin (built-in phonemizers)

## Key files

- OpenUtau.Core/DocManager.cs — central state, singleton via DocManager.Inst
- OpenUtau.Core/PlaybackManager.cs — playback control
- OpenUtau.Core/Ustx/UNote.cs — note model (includes UVibrato, UPitch, PitchPoint)
- OpenUtau.Core/Commands/NoteCommands.cs — all note/vibrato/phoneme commands
- OpenUtauMobile/ViewModels/EditViewModel.cs — piano roll ViewModel (main work target)
- OpenUtauMobile/Views/EditPage.xaml(.cs) — piano roll UI (main work target)

## Mobile app structure (OpenUtauMobile/)

ViewModels/
  EditViewModel.cs — piano roll editor VM

Views/
  HomePage.xaml — home screen
  EditPage.xaml — main editor (piano roll, arrangement, expression)
  SingerManagePage.xaml — voicebank management
  SettingsPage.xaml — settings
  OptionsPage.xaml — options
  AboutPage.xaml — about
  InstallSingerPage.xaml — voicebank installer
  InstallVogenSingerPage.xaml — Vogen voicebank installer
  SingerDetailPage.xaml — voicebank detail
  DependencyManagePage.xaml — dependency manager
  SplashScreenPage.xaml — splash screen

Views/Controls/ — popups and dialogs
  EditLyricsPopup, ChooseSingerPopup, SelectPhonemizerPopup,
  PianoRollSnapDivPopup, EditBpmPopup, EditKeyPopup, EditBeatPopup,
  EditMenuPopup, ExportAudioPopup, FileSaverPopup, ChooseTrackColorPopup,
  InsertTempoSignaturePopup, RenamePopup, ErrorPopup, ExitPopup, LoadingPopup

Platforms/
  Android/Lib/ — libworldline.so (arm64-v8a, armeabi-v7a, x86, x86_64)
  iOS/Utils/iOSAppLifeCycleHelper.cs
  Windows/Utils/WindowsAppLifeCycleHelper.cs

Resources/
  Strings/AppResources.resx — default localization (Chinese)
  Strings/AppResources.en.resx — English
  Icons/dark/, Icons/light/ — themed icons
  Colors/DarkThemeColors.xaml, LightThemeColors.xaml

## NuGet dependencies (OpenUtauMobile)

CommunityToolkit.Maui 11.2.0, CommunityToolkit.Mvvm 8.4.0,
Microsoft.Maui.Controls 9.0.100, ReactiveUI 20.2.45,
ReactiveUI.Maui 20.2.45, SkiaSharp.Views.Maui.Controls 3.119.0,
SharpCompress 0.38.0, Serilog (Console, Debug, File)

## NuGet dependencies (OpenUtau.Core, notable)

Microsoft.ML.OnnxRuntime 1.16.0 (DiffSinger inference),
NAudio.Core 2.2.1, NWaves 0.9.6, NumSharp 0.30.0,
YamlDotNet 15.1.2 (USTX serialization), Melanchall.DryWetMidi 7.2.0,
Microsoft.Maui.Essentials 9.0.30 (mobile patch)
