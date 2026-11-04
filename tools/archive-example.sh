#!/usr/bin/env bash
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

if [ -n "$(git status --porcelain)" ]; then
  echo "A árvore git tem alterações. Commite ou descarte antes de arquivar." >&2
  exit 1
fi

if [ -d ai/moldes/Todos ]; then
  echo "Exemplo já arquivado: ai/moldes/Todos/ existe." >&2
  exit 1
fi

api_project="$(find src -mindepth 2 -maxdepth 2 -name '*.Api.csproj' | head -n 1)"
tests_project="$(find tests -mindepth 2 -maxdepth 2 -name '*.Api.Tests.csproj' | head -n 1)"
api_dir=""
tests_dir=""
[ -n "$api_project" ] && api_dir="$(dirname "$api_project")"
[ -n "$tests_project" ] && tests_dir="$(dirname "$tests_project")"
project="$(basename "$api_dir" .Api)"
export PROJECT="$project"

if [ -z "$api_dir" ] || [ -z "$tests_dir" ] || [ ! -d "$api_dir/Feature/Todos" ]; then
  echo "Estrutura inesperada: não encontrei o exemplo Todos para arquivar." >&2
  exit 1
fi

mold=ai/moldes/Todos
persistence="$api_dir/Infrastructure/Persistence"

mkdir -p "$mold/Entities" "$mold/Configurations" "$mold/Modules"

git mv "$api_dir/Feature/Todos" "$mold/Feature"
git mv "$api_dir/Modules/TodoModule.cs" "$mold/Modules/TodoModule.cs"
git mv "$persistence/Entities/TodoItem.cs" "$mold/Entities/TodoItem.cs"
git mv "$persistence/Entities/TodoEvent.cs" "$mold/Entities/TodoEvent.cs"
git mv "$persistence/Configurations/TodoItemConfiguration.cs" "$mold/Configurations/TodoItemConfiguration.cs"
git mv "$persistence/Configurations/TodoEventConfiguration.cs" "$mold/Configurations/TodoEventConfiguration.cs"
git mv "$tests_dir/Todos" "$mold/tests"

if [ -d bruno/Todos ]; then
  mkdir -p "$mold/bruno"
  for file in bruno/Todos/*; do
    git mv "$file" "$mold/bruno/$(basename "$file")"
  done
fi

for file in "$api_dir/Infrastructure/DependencyInjections/DependencyInjection.cs" "$persistence/AppDbContext.cs"; do
  perl -ni -e '(my $l = $_) =~ s/\Q$ENV{PROJECT}\E//g; print unless $l =~ /Todo/' "$file"
done

find src tests -depth -type d -empty -delete
if [ -d bruno ]; then
  find bruno -depth -type d -empty -delete
fi

git rm -rq "$persistence/Migrations"
rm -rf "$persistence/Migrations"

pointer_files=()
while IFS= read -r -d '' file; do
  pointer_files+=("$file")
done < <(git ls-files -z -- '*.md' ':!ai/moldes/**' ':!.claude/skills/THIRD_PARTY_NOTICES.md')

if [ "${#pointer_files[@]}" -gt 0 ]; then
  perl -pi -e '
    s{(?:src/[\w.]+/)?Feature/Todos}{ai/moldes/Todos/Feature}g;
    s{(?:src/[\w.]+/)?Modules/TodoModule\.cs}{ai/moldes/Todos/Modules/TodoModule.cs}g;
    s{(?:src/[\w.]+/)?Infrastructure/Persistence/Entities/(Todo(?:Item|Event)\.cs)}{ai/moldes/Todos/Entities/$1}g;
    s{(?:src/[\w.]+/)?Infrastructure/Persistence/Configurations/(Todo(?:Item|Event)Configuration\.cs)}{ai/moldes/Todos/Configurations/$1}g;
    s{tests/[\w.]+/Todos}{ai/moldes/Todos/tests}g;
    s{bruno/Todos}{ai/moldes/Todos/bruno}g;
  ' "${pointer_files[@]}"
fi

leftover="$(git grep -nI Todo -- 'src/**/*.cs' 'tests/**/*.cs' | perl -ne 's/\Q$ENV{PROJECT}\E//g; print if /Todo/' || true)"
if [ -n "$leftover" ]; then
  echo "Sobraram referências a Todo fora do molde:" >&2
  echo "$leftover" >&2
  exit 1
fi

dotnet tool restore >/dev/null
dotnet restore "$api_dir" >/dev/null
dotnet ef migrations add Initial --project "$api_dir" --output-dir Infrastructure/Persistence/Migrations
git add -A

git rm -qf tools/rename-project.sh tools/test-template-scripts.sh .github/workflows/template-check.yml
git rm -qf tools/archive-example.sh

echo "Exemplo arquivado em $mold. Rode dotnet build, dotnet test e tools/check-docs.sh, depois commite."
