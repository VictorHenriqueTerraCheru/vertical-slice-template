#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"

missing=0

while IFS= read -r file; do
  while IFS= read -r path; do
    case "$path" in
      ai/PLANO.md|ai/moldes/*|tools/rename-project.sh|tools/archive-example.sh|tools/test-template-scripts.sh) continue ;;
    esac
    if [ ! -e "$path" ]; then
      echo "$file: $path"
      missing=1
    fi
  done < <(grep -oE '`(src|tests|ai|tools|docs)/[^`]*`' "$file" | tr -d '`' | grep -v '<' | sort -u)
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
