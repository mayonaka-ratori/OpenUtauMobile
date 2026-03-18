# OpenUtau Mobile — Improved Fork

An improved fork of [OpenUtau Mobile](https://github.com/vocoder712/OpenUtauMobile),
the mobile edition of [OpenUtau](https://github.com/stakira/OpenUtau) singing voice
synthesis editor.

## Goal

Make OpenUtau Mobile a genuinely usable singing voice editor on smartphones.
The original mobile port is functional but unstable, with incomplete features and
poor touch performance. This fork aims to fix that.

## What we're improving

| Area | Problem | Target |
| --- | --- | --- |
| Stability | Frequent crashes, memory leaks | < 500MB memory, no crash in normal use |
| Touch performance | Pitch curve drawing is sluggish | < 8ms per frame, < 32ms touch latency |
| Vibrato editing | UI partially implemented, unusable | Full visual vibrato editor |
| Phoneme editing | Display only, cannot edit | Drag-to-adjust phoneme timing and alias |
| Quantize grid | Fixed grid, no user setting | Selectable: 1/4, 1/8, 1/16, 1/32, 1/64 |
| Audio import | Crashes on some files (issue #1913) | Reliable WAV/MP3 import |
| Plugin system | Not available on mobile | Evaluate feasibility, implement if possible |
| Localization | Chinese and English only | Add Japanese |

## What works already

Multi-track editing, DiffSinger AI voice synthesis, USTX/UST/MIDI/VSQX import,
basic pitch curve drawing, mute/volume/pan per track.

## Architecture

This project shares the OpenUtau Core engine with the desktop version.
The mobile app is a separate .NET MAUI frontend.

```text
OpenUtau.Core/                ← Shared engine (avoid modifications)
OpenUtau.Plugin.Builtin/      ← Built-in phonemizers (avoid modifications)
OpenUtauMobile/               ← MAUI mobile app (our work target)
OpenUtauMobile.Tests/         ← Tests
```

Core modifications are allowed when necessary but must be documented in
`docs/CORE_PATCHES.md` and kept minimal to ease future upstream merges.

## Tech stack

- Language: C# 13 / .NET 9
- UI framework: .NET MAUI (may evaluate alternatives if performance ceiling is hit)
- Drawing: SkiaSharp
- State management: OpenUtau DocManager singleton + command pattern
- AI inference: ONNX Runtime (DiffSinger)
- Platforms: Android 5.0+, iOS 15.0+ (iOS requires self-build)

## Development

### Prerequisites

- .NET 9 SDK
- Visual Studio 2022+ / Rider / VS Code with C# extension
- Android SDK (for Android builds)
- macOS + Xcode 15+ (for iOS builds)

### Build

```bash
dotnet build OpenUtauMobile/OpenUtauMobile.csproj -f net9.0-android
dotnet build OpenUtauMobile/OpenUtauMobile.csproj -f net9.0-ios      # macOS only
```

### Test

```bash
dotnet test OpenUtauMobile.Tests/
```

### AI-assisted development (Vibe Coding)

This project is set up for [Claude Code](https://docs.anthropic.com/en/docs/claude-code).
Configuration is in `.claude/` directory. Start a session with:

```bash
claude
```

Claude will automatically load project skills, use specialized agents
(auditor, implementer, tester), and follow project conventions.

## Roadmap

See [docs/ROADMAP.md](docs/ROADMAP.md) for the detailed development plan.

## Upstream

- Desktop OpenUtau: [stakira/OpenUtau](https://github.com/stakira/OpenUtau)
- Original mobile port: [vocoder712/OpenUtauMobile](https://github.com/vocoder712/OpenUtauMobile)

## License

Apache 2.0 (same as original OpenUtau Mobile)
