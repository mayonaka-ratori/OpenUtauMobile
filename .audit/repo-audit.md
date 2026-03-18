# Repo Audit

## Status
## master...origin/master
?? .audit/
?? .claude/agents/phase_auditor_prompt.md

## Remotes
origin	https://github.com/mayonaka-ratori/OpenUtauMobile (fetch)
origin	https://github.com/mayonaka-ratori/OpenUtauMobile (push)
upstream	https://github.com/vocoder712/OpenUtauMobile.git (fetch)
upstream	https://github.com/vocoder712/OpenUtauMobile.git (push)

## Branches
  feat/claude-code-harness 6108157 docs: add Claude Code harness (skills, agents, hooks, tests, roadmap)
* master                   8ef4a29 [origin/master] fix: Phase 1 stability fixes + Cold Review blocker corrections

## Upstream Base
upstream/master

## Unique Commits vs Upstream
> 8ef4a29 (HEAD -> master, origin/master, origin/HEAD) fix: Phase 1 stability fixes + Cold Review blocker corrections
> 6108157 (origin/feat/claude-code-harness, feat/claude-code-harness) docs: add Claude Code harness (skills, agents, hooks, tests, roadmap)
> c2bab3e docs: add Claude Code harness (skills, agents, hooks, tests, roadmap)

## Diff Stat
 .claude/agents/auditor.md                          | 152 +++++++
 .claude/agents/implementer.md                      | 105 +++++
 .claude/agents/pm.md                               | 212 +++++++++
 .claude/agents/tester.md                           |  77 ++++
 .claude/audit-2026-03-18.md                        | 486 +++++++++++++++++++++
 .claude/hooks/check-core-protection.sh             |  25 ++
 .claude/hooks/ensure-hooks-executable.sh           |  13 +
 .claude/hooks/post-edit-build-check.sh             |  43 ++
 .claude/progress.md                                | 114 +++++
 .claude/settings.json                              |  42 ++
 .claude/skills/maui-mobile-patterns/SKILL.md       |  86 ++++
 .claude/skills/mcp-github-setup/SKILL.md           |  51 +++
 .claude/skills/openutau-core-api/SKILL.md          | 126 ++++++
 .claude/skills/project-overview/SKILL.md           | 101 +++++
 .claude/skills/skia-performance/SKILL.md           |  95 ++++
 CLAUDE.md                                          |  34 ++
 OpenUtauMobile.Tests/OpenUtauMobile.Tests.csproj   |  17 +
 OpenUtauMobile.Tests/SmokeTests.cs                 |  55 +++
 OpenUtauMobile.sln                                 |  22 +
 .../Android/Utils/Audio/AudioTrackOutput.cs        |  47 +-
 OpenUtauMobile/Utils/ObjectProvider.cs             |   4 +-
 OpenUtauMobile/ViewModels/EditViewModel.cs         | 141 +++---
 .../Views/DrawableObjects/DrawableNotes.cs         | 134 +++---
 .../Views/DrawableObjects/DrawablePart.cs          | 187 ++++----
 .../Views/DrawableObjects/DrawablePianoKeys.cs     |  41 +-
 .../DrawablePianoRollTickBackground.cs             |  69 ++-
 .../DrawableObjects/DrawableTickBackground.cs      | 150 ++++---
 .../DrawableObjects/DrawableTrackPlayPosLine.cs    |  19 +-
 OpenUtauMobile/Views/EditPage.xaml.cs              | 292 +++++++++----
 OpenUtauMobile/Views/SplashScreenPage.xaml.cs      |   8 +-
 OpenUtauMobile/Views/Utils/GestureProcessor.cs     |  55 ++-
 README.md                                          | 185 +++-----
 docs/CORE_PATCHES.md                               |  53 +++
 docs/ROADMAP.md                                    | 109 +++++
 34 files changed, 2793 insertions(+), 557 deletions(-)

## Diff Name Status
A	.claude/agents/auditor.md
A	.claude/agents/implementer.md
A	.claude/agents/pm.md
A	.claude/agents/tester.md
A	.claude/audit-2026-03-18.md
A	.claude/hooks/check-core-protection.sh
A	.claude/hooks/ensure-hooks-executable.sh
A	.claude/hooks/post-edit-build-check.sh
A	.claude/progress.md
A	.claude/settings.json
A	.claude/skills/maui-mobile-patterns/SKILL.md
A	.claude/skills/mcp-github-setup/SKILL.md
A	.claude/skills/openutau-core-api/SKILL.md
A	.claude/skills/project-overview/SKILL.md
A	.claude/skills/skia-performance/SKILL.md
A	CLAUDE.md
A	OpenUtauMobile.Tests/OpenUtauMobile.Tests.csproj
A	OpenUtauMobile.Tests/SmokeTests.cs
M	OpenUtauMobile.sln
M	OpenUtauMobile/Platforms/Android/Utils/Audio/AudioTrackOutput.cs
M	OpenUtauMobile/Utils/ObjectProvider.cs
M	OpenUtauMobile/ViewModels/EditViewModel.cs
M	OpenUtauMobile/Views/DrawableObjects/DrawableNotes.cs
M	OpenUtauMobile/Views/DrawableObjects/DrawablePart.cs
M	OpenUtauMobile/Views/DrawableObjects/DrawablePianoKeys.cs
M	OpenUtauMobile/Views/DrawableObjects/DrawablePianoRollTickBackground.cs
M	OpenUtauMobile/Views/DrawableObjects/DrawableTickBackground.cs
M	OpenUtauMobile/Views/DrawableObjects/DrawableTrackPlayPosLine.cs
M	OpenUtauMobile/Views/EditPage.xaml.cs
M	OpenUtauMobile/Views/SplashScreenPage.xaml.cs
M	OpenUtauMobile/Views/Utils/GestureProcessor.cs
M	README.md
A	docs/CORE_PATCHES.md
A	docs/ROADMAP.md

## Package / Framework References
