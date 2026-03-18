---
name: maui-mobile-patterns
description: .NET MAUI mobile patterns for this project — page lifecycle, CompositeDisposable + ICmdSubscriber dual cleanup, platform folders, localization, native libraries, known MAUI quirks, and csproj settings. Load when creating or modifying ViewModels, pages, or platform-specific code.
---

# MAUI Mobile Patterns

## Page Lifecycle and Memory

Pages are NOT automatically disposed on navigation in MAUI.
ViewModels can leak if subscriptions are not cleaned up.

Always use CompositeDisposable for ReactiveUI subscriptions:

  public class EditViewModel : ReactiveObject, ICmdSubscriber, IDisposable {
      private readonly CompositeDisposable _disposables = new();

      public EditViewModel() {
          DocManager.Inst.AddSubscriber(this);
          this.WhenAnyValue(x => x.Something)
              .Subscribe(...)
              .DisposeWith(_disposables);
      }

      public void OnNext(UCommand cmd, bool isUndo) {
          // handle DocManager events
      }

      public void Dispose() {
          _disposables.Dispose();
          DocManager.Inst.RemoveSubscriber(this);
      }
  }

Two separate cleanup paths exist and both are required:

1. ReactiveUI subscriptions -> CompositeDisposable
2. ICmdSubscriber -> DocManager.Inst.RemoveSubscriber(this)

## Platform-Specific Code

- Platforms/Android/ — Android-specific (lifecycle, native libs)
- Platforms/iOS/ — iOS-specific (lifecycle, build requirements)
- Platforms/Windows/ — Windows-specific (for dev/testing)

Use dependency injection or partial classes over #if ANDROID / #if IOS.

## Localization

- Resources/Strings/AppResources.resx — default (Chinese)
- Resources/Strings/AppResources.en.resx — English
- To add Japanese: create AppResources.ja.resx
- Access in code: AppResources.ResourceKey
- Access in XAML: {x:Static resources:AppResources.ResourceKey}

## Native Libraries

- libworldline.so — WORLD-based resampler, one per ABI:
  - Platforms/Android/Lib/arm64-v8a/libworldline.so
  - Platforms/Android/Lib/armeabi-v7a/libworldline.so
  - Platforms/Android/Lib/x86/libworldline.so
  - Platforms/Android/Lib/x86_64/libworldline.so
- Called via P/Invoke from Core. Do not change calling conventions.

## Known MAUI Quirks (as of .NET 9 / MAUI 9.0.100)

- CollectionView leaks memory with complex DataTemplates. Prefer simple items.
- Shell navigation can leak pages. Call Navigation.PopAsync() explicitly.
- SKCanvasView.InvalidateSurface() must be called on the UI thread.
- Android back button behavior needs explicit handling in Shell.
- iOS builds require MtouchLink=None to avoid DryWetMidi linker issues
  (already configured in csproj).

## MVVM Libraries in Use

- ReactiveUI 20.2.45: WhenAnyValue, ObservableAsPropertyHelper, ReactiveCommand
- CommunityToolkit.Mvvm 8.4.0: ObservableObject, RelayCommand, ObservableProperty
- Both coexist in this project. Prefer ReactiveUI for new ViewModels
  to stay consistent with existing EditViewModel.

## Important csproj Settings

- AndroidCreatePackagePerAbi=True — separate APK per architecture
- JavaMaximumHeapSize=4G — needed for large builds
- Release uses AOT (RunAOTCompilation=True) and r8 linker
- iOS disables IL linker (MtouchLink=None)
