#!/bin/bash
# Hook: Stop — Final behavior verification reminder
# Runs when Claude finishes responding.
# - Checks if pre-planning consultation was done
# - Reminds about incomplete pipeline steps
# Exit 0 always (informative, never blocking)

set -euo pipefail

REPO_ROOT="$(git rev-parse --show-toplevel 2>/dev/null || echo ".")"

reminders=()

# Check if pre-planning was done
if [[ ! -f "$REPO_ROOT/.claude/.pre-planning-done" ]]; then
  reminders+=("Consulta pré-planejamento (comportamento #12) não foi registrada nesta sessão.")
fi

# Check if there are uncommitted changes (pipeline may not be complete)
if git diff --quiet HEAD 2>/dev/null; then
  : # No uncommitted changes — OK
else
  reminders+=("Existem mudanças não commitadas. Verificar se o pipeline pré-commit foi concluído.")
fi

# Emit reminders if any
if (( ${#reminders[@]} > 0 )); then
  echo "[Stop] Lembretes de governança:"
  for reminder in "${reminders[@]}"; do
    echo "  - $reminder"
  done
fi

exit 0
