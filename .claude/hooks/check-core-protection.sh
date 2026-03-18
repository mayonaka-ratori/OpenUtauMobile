#!/bin/bash
# PreToolUse hook for Bash commands.
# Blocks shell commands that would modify Core or Plugin directories.
# Claude Code's permissions.deny handles Edit/Write tools, but Bash commands
# like sed, mv, cp could bypass that. This hook catches those cases.

INPUT=$(cat)
COMMAND=$(echo "$INPUT" | jq -r '.tool_input.command // empty')

if [ -z "$COMMAND" ]; then
  exit 0
fi

# Check if the Bash command targets protected directories
if echo "$COMMAND" | grep -qE '(OpenUtau\.Core|OpenUtau\.Plugin\.Builtin)'; then
  # Allow read-only commands
  if echo "$COMMAND" | grep -qE '^(cat|less|head|tail|grep|find|ls|wc|file|diff|git diff|git log|git show|dotnet build|dotnet test)'; then
    exit 0
  fi
  # Block anything else that references Core/Plugin paths
  echo "Blocked: Bash command targets protected directory (OpenUtau.Core or OpenUtau.Plugin.Builtin). Use Edit tool instead (which is also denied by permissions). If this is a necessary Core patch, document it in docs/CORE_PATCHES.md first." >&2
  exit 2
fi

exit 0
