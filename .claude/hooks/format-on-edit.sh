#!/usr/bin/env bash
# Auto-format files after Claude edits them.
#   .cs   -> dotnet format (nearest .csproj, edited file only)
#   .xaml -> XAML Styler (Settings.XamlStyler rules)
# Wired as a PostToolUse(Write|Edit) hook; the tool payload arrives as JSON on stdin.
set -u

root=$(cd "$(dirname "$0")/../.." && pwd)

# file_path may use Windows backslashes; normalise to forward slashes for bash.
f=$(jq -r '(.tool_response.filePath // .tool_input.file_path) // empty' | tr '\\' '/')
[ -z "$f" ] && exit 0

case "$f" in
  *.xaml)
    dotnet xstyler -c "$root/Settings.XamlStyler" -f "$f" -l None >/dev/null 2>&1 || true
    ;;
  *.cs)
    dir=$(dirname "$f"); proj=""
    while [ "$dir" != "/" ] && [ "$dir" != "." ] && [ -n "$dir" ]; do
      proj=$(ls "$dir"/*.csproj 2>/dev/null | head -1)
      [ -n "$proj" ] && break
      dir=$(dirname "$dir")
    done
    [ -n "$proj" ] && dotnet format "$proj" --include "$f" --no-restore -v quiet >/dev/null 2>&1 || true
    ;;
esac

exit 0
