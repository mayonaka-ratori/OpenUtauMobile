#!/bin/bash
# SessionStart hook: ensures all hook scripts are executable.
# Runs once at session start. No user action required.

HOOK_DIR="$(dirname "$0")"

for script in "$HOOK_DIR"/*.sh; do
  if [ -f "$script" ] && [ ! -x "$script" ]; then
    chmod +x "$script"
  fi
done

exit 0
