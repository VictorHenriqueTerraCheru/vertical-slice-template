#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -ne 1 ]; then
  echo "Uso: tools/rename-project.sh <NomeEmPascalCase>" >&2
  exit 1
fi

name="$1"

if ! [[ "$name" =~ ^[A-Z][A-Za-z0-9]*$ ]]; then
  echo "Nome inválido: '$name'. Use PascalCase, só letras e números, começando por letra maiúscula." >&2
  exit 1
fi

if [ "$(printf '%s' "$name" | tr -cd 'A-Za-z' | wc -c)" -lt 3 ]; then
  echo "Nome inválido: '$name'. Use ao menos 3 letras." >&2
  exit 1
fi

cd "$(git rev-parse --show-toplevel)"

if [ -n "$(git status --porcelain)" ]; then
  echo "A árvore git tem alterações. Commite ou descarte antes de renomear." >&2
  exit 1
fi

excluded=(':!tools/rename-project.sh' ':!tools/test-template-scripts.sh')

if ! git grep -qiI starter -- . "${excluded[@]}"; then
  echo "Nada a renomear: nenhuma ocorrência de 'starter' no repositório." >&2
  exit 1
fi

lower="$(printf '%s' "$name" | tr '[:upper:]' '[:lower:]')"
export NEW_NAME="$name" NEW_LOWER="$lower"

while IFS= read -r -d '' file; do
  perl -pi -e 's/Starter/$ENV{NEW_NAME}/g; s/starter/$ENV{NEW_LOWER}/g' "$file"
done < <(git grep -lIiz starter -- . "${excluded[@]}")

while IFS= read -r -d '' path; do
  case "$path" in
    tools/rename-project.sh|tools/test-template-scripts.sh) continue ;;
  esac
  target="${path//Starter/$name}"
  target="${target//starter/$lower}"
  if [ "$target" != "$path" ]; then
    mkdir -p "$(dirname "$target")"
    git mv "$path" "$target"
  fi
done < <(git ls-files -z)

if [ "$name" != "Starter" ]; then
  find src tests -mindepth 1 -maxdepth 1 -name '*Starter*' -exec rm -rf {} +
fi

find . -depth -type d -empty -not -path "./.git/*" -delete

echo "Projeto renomeado para $name. Revise com git status e commite."
