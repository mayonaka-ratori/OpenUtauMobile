#!/bin/bash
# PostToolUse hook for Edit|Write tools.
# Runs a quick build check after any file is edited or written.
# Exit code is non-blocking (PostToolUse cannot block, tool already ran),
# but stderr output is shown to Claude as feedback.

INPUT=$(cat)
FILE_PATH=$(echo "$INPUT" | jq -r '.tool_input.file_path // empty')

# Skip non-C# files (no need to build-check markdown, json, etc.)
case "$FILE_PATH" in
  *.cs|*.csproj|*.xaml|*.resx)
    ;;
  *)
    exit 0
    ;;
esac

# Quick build check targeting Android (fastest platform build)
echo "Running post-edit build check..." >&2
BUILD_OUTPUT=$(dotnet build OpenUtauMobile/OpenUtauMobile.csproj -f net9.0-android --nologo --verbosity quiet 2>&1)
BUILD_EXIT=$?

if [ $BUILD_EXIT -ne 0 ]; then
  echo "BUILD FAILED after editing $FILE_PATH" >&2
  echo "$BUILD_OUTPUT" | tail -20 >&2
  echo '{"systemMessage": "Build failed after edit. Review the errors above and fix before continuing."}' 
  exit 0
fi

# Run tests if they exist
if [ -d "OpenUtauMobile.Tests" ]; then
  TEST_OUTPUT=$(dotnet test OpenUtauMobile.Tests/ --nologo --verbosity quiet 2>&1)
  TEST_EXIT=$?
  if [ $TEST_EXIT -ne 0 ]; then
    echo "TESTS FAILED after editing $FILE_PATH" >&2
    echo "$TEST_OUTPUT" | tail -20 >&2
    echo '{"systemMessage": "Tests failed after edit. Review and fix before continuing."}'
    exit 0
  fi
fi

exit 0
