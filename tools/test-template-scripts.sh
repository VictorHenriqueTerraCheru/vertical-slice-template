#!/usr/bin/env bash
set -euo pipefail

source_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
work="$(mktemp -d)"
failures=0

cleanup() {
  rm -rf "$work"
}
trap cleanup EXIT

fresh_clone() {
  local target
  target="$(mktemp -d "$work/clone.XXXXXX")"
  git clone -q "$source_root" "$target"
  git -C "$target" config user.email "test@example.com"
  git -C "$target" config user.name "Test"
  printf '%s' "$target"
}

pass() {
  echo "ok   - $1"
}

fail() {
  echo "FAIL - $1"
  failures=$((failures + 1))
}

expect_refusal() {
  local description="$1" expected="$2" dir="$3"
  shift 3
  local output status=0
  output="$(cd "$dir" && "$@" 2>&1)" || status=$?
  if [ "$status" -ne 0 ] && grep -qi -- "$expected" <<<"$output"; then
    pass "$description"
  else
    fail "$description (código $status, saída: $output)"
  fi
}

expect_clean_tree() {
  local description="$1" dir="$2"
  if [ -z "$(git -C "$dir" status --porcelain)" ]; then
    pass "$description"
  else
    fail "$description"
  fi
}

expect_success() {
  local description="$1" dir="$2"
  shift 2
  local output status=0
  output="$(cd "$dir" && "$@" 2>&1)" || status=$?
  if [ "$status" -eq 0 ]; then
    pass "$description"
  else
    fail "$description (código $status)"
    echo "$output" | tail -20
  fi
}

commit_all() {
  git -C "$1" add -A
  git -C "$1" commit -q -m "wip"
}

clone="$(fresh_clone)"
for invalid in acme 1Acme Acme-X Ab; do
  expect_refusal "rename recusa o nome $invalid" "inválido" "$clone" bash tools/rename-project.sh "$invalid"
  expect_clean_tree "rename com $invalid não altera nada" "$clone"
done

clone="$(fresh_clone)"
expect_refusal "rename sem argumento mostra o uso" "uso" "$clone" bash tools/rename-project.sh
expect_clean_tree "rename sem argumento não altera nada" "$clone"

clone="$(fresh_clone)"
echo "x" >> "$clone/README.md"
before="$(git -C "$clone" status --porcelain)"
expect_refusal "rename recusa árvore suja" "alterações" "$clone" bash tools/rename-project.sh Acme
expect_refusal "archive recusa árvore suja" "alterações" "$clone" bash tools/archive-example.sh
if [ "$before" = "$(git -C "$clone" status --porcelain)" ] && [ -d "$clone/src/Starter.Api/Feature/Todos" ]; then
  pass "árvore suja continua como estava"
else
  fail "árvore suja foi alterada"
fi

clone="$(fresh_clone)"
expect_success "rename Acme" "$clone" bash tools/rename-project.sh Acme
leftover="$(git -C "$clone" grep -iI starter -- ':!tools/rename-project.sh' ':!tools/test-template-scripts.sh' || true)"
if [ -z "$leftover" ] && [ -f "$clone/Acme.slnx" ] && [ -d "$clone/src/Acme.Api" ] && [ -d "$clone/tests/Acme.Api.Tests" ]; then
  pass "rename não deixa starter e cria Acme.slnx e src/Acme.Api"
else
  fail "rename deixou starter ou não criou os caminhos: $leftover"
fi
expect_success "build depois do rename" "$clone" dotnet build
expect_success "testes depois do rename" "$clone" dotnet test --no-build
commit_all "$clone"
expect_refusal "rename repetido diz que não há o que renomear" "nada a renomear" "$clone" bash tools/rename-project.sh Acme

clone="$(fresh_clone)"
expect_success "rename MyStarter" "$clone" bash tools/rename-project.sh MyStarter
if [ -d "$clone/src/MyStarter.Api" ] && [ -d "$clone/tests/MyStarter.Api.Tests" ]; then
  pass "rename MyStarter mantém src/MyStarter.Api e tests/MyStarter.Api.Tests"
else
  fail "rename MyStarter apagou ou não criou as pastas do projeto"
fi

clone="$(fresh_clone)"
bash -c "cd '$clone' && bash tools/rename-project.sh Acme" >/dev/null
commit_all "$clone"
cp "$clone/tools/archive-example.sh" "$work/archive-copy.sh"
expect_success "archive-example" "$clone" bash tools/archive-example.sh
mold="$clone/ai/moldes/Todos"
assistant_mold="$clone/ai/moldes/Assistant"
if [ -z "$(git -C "$clone" grep -iI todo -- 'src/**/*.cs' 'tests/**/*.cs' || true)" ] \
  && [ -z "$(git -C "$clone" grep -I 'Assistant' -- 'src/**/*.cs' 'tests/**/*.cs' | grep -v 'ChatRole.Assistant' | grep -v 'Assistant,' || true)" ] \
  && [ -f "$assistant_mold/Modules/AssistantModule.cs" ] && [ -d "$assistant_mold/Feature/Services" ] \
  && [ -f "$assistant_mold/tests/AssistantTests.cs" ] && [ -f "$assistant_mold/bruno/send-message.bru" ] \
  && [ -d "$clone/src/Acme.Api/Feature/AiUsage" ] && [ -f "$clone/src/Acme.Api/Common/Services/OpenAiChatModelClient.cs" ] \
  && [ -f "$mold/Modules/TodoModule.cs" ] && [ -d "$mold/Feature/Services" ] \
  && [ -f "$mold/Entities/TodoItem.cs" ] && [ -f "$mold/Entities/TodoEvent.cs" ] \
  && [ -f "$mold/Configurations/TodoItemConfiguration.cs" ] && [ -f "$mold/tests/TodoCrudTests.cs" ] \
  && [ -f "$mold/bruno/create.bru" ] && [ ! -e "$clone/tools/archive-example.sh" ] && [ ! -e "$clone/tools/rename-project.sh" ]; then
  pass "archive deixa src e tests sem Todo e sem o assistente, mantém IA e gasto, e completa os moldes"
else
  fail "archive deixou Todo ou o assistente em src ou tests, ou um molde está incompleto"
fi
expect_success "check-docs depois do archive" "$clone" bash tools/check-docs.sh
expect_success "build depois do archive" "$clone" dotnet build
expect_success "testes depois do archive" "$clone" dotnet test --no-build
commit_all "$clone"
expect_refusal "archive repetido diz que já foi arquivado" "já arquivado" "$clone" bash "$work/archive-copy.sh"

clone="$(fresh_clone)"
expect_success "build antes do rename" "$clone" dotnet build
expect_success "rename TodoApp depois de um build" "$clone" bash tools/rename-project.sh TodoApp
if [ ! -e "$clone/src/Starter.Api" ] && [ ! -e "$clone/tests/Starter.Api.Tests" ] && [ -d "$clone/src/TodoApp.Api" ]; then
  pass "rename remove as pastas antigas com bin e obj"
else
  fail "rename deixou pastas Starter em src ou tests"
fi
commit_all "$clone"
expect_success "archive com nome contendo Todo" "$clone" bash tools/archive-example.sh
mold="$clone/ai/moldes/Todos"
leftover="$(git -C "$clone" grep -nI Todo -- 'src/**/*.cs' 'tests/**/*.cs' | perl -ne 's/TodoApp//g; print if /Todo/' || true)"
if [ -z "$leftover" ] && [ -f "$mold/Modules/TodoModule.cs" ] && [ -f "$clone/src/TodoApp.Api/Infrastructure/DependencyInjections/DependencyInjection.cs" ]; then
  pass "archive com TodoApp deixa src e tests sem o exemplo"
else
  fail "archive com TodoApp deixou o exemplo ou quebrou a estrutura: $leftover"
fi
expect_success "check-docs com TodoApp depois do archive" "$clone" bash tools/check-docs.sh
expect_success "build com TodoApp depois do archive" "$clone" dotnet build

if [ "$failures" -ne 0 ]; then
  echo "$failures verificação(ões) falharam"
  exit 1
fi

echo "Todas as verificações passaram"
