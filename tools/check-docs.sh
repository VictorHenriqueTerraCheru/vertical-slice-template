#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"

api_project="$(find src -mindepth 2 -maxdepth 2 -name '*.Api.csproj' | head -n 1)"
api_dir=""
if [ -n "$api_project" ]; then
  api_dir="$(dirname "$api_project")"
fi

archived=0
if [ -d ai/moldes/Todos ]; then
  archived=1
fi

missing=0

while IFS= read -r file; do
  while IFS= read -r path; do
    case "$path" in
      ai/PLANO.md|tools/rename-project.sh|tools/archive-example.sh|tools/test-template-scripts.sh) continue ;;
      ai/moldes/*) [ "$archived" -eq 0 ] && continue ;;
    esac
    case "$path" in
      Feature/*|Common/*|Modules/*|Infrastructure/*) resolved="$api_dir/$path" ;;
      *) resolved="$path" ;;
    esac
    if [ ! -e "$resolved" ]; then
      echo "$file: $path"
      missing=1
    fi
  done < <(grep -oE '`(src|tests|ai|tools|docs|bruno|Feature|Common|Modules|Infrastructure)/[^`]*`' "$file" | tr -d '`' | grep -v '<' | sort -u)
done < <(
  {
    [ -f AGENTS.md ] && echo AGENTS.md
    [ -f CLAUDE.md ] && echo CLAUDE.md
    find ai src tests -name 'AGENTS.md' -o -path 'ai/*' -name '*.md' -type f
  } | sort
)

if [ "$missing" -ne 0 ]; then
  echo "FAIL"
  exit 1
fi

echo "OK"
