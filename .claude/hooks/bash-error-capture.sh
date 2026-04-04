#!/bin/bash
# Hook: PostToolUseFailure (Bash) — Auto-register bash errors
# Captures failed bash commands and appends structured entry to bash-errors-log.md.
# Exit 0 always (informative, never blocking)

set -euo pipefail

REPO_ROOT="$(git rev-parse --show-toplevel 2>/dev/null || echo ".")"
LOG_FILE="$REPO_ROOT/bash-errors-log.md"

# Read tool input from stdin (JSON with tool_input and tool_output)
input=$(cat)

# Extract command and error from JSON (best-effort)
command_executed=$(echo "$input" | python3 -c "
import json, sys
try:
    data = json.load(sys.stdin)
    print(data.get('tool_input', {}).get('command', 'unknown'))
except:
    print('unknown')
" 2>/dev/null || echo "unknown")

# Skip if command is unknown or trivial
if [[ "$command_executed" == "unknown" ]] || [[ "$command_executed" == "" ]]; then
  exit 0
fi

# Determine next error number
if [[ -f "$LOG_FILE" ]]; then
  last_number=$(grep -oP 'Erro \K\d+' "$LOG_FILE" | sort -n | tail -1 || echo "0")
else
  last_number=0
fi
next_number=$((last_number + 1))

# Get current date
current_date=$(date +%Y-%m-%d)

# Append structured entry
cat >> "$LOG_FILE" << EOF

## Erro $next_number — Captura automática via hook

| Campo | Valor |
|---|---|
| **Número** | $next_number |
| **Data** | $current_date |
| **Comando executado** | \`$command_executed\` |
| **Erro retornado** | Capturado automaticamente pelo hook bash-error-capture.sh |
| **Causa** | A ser investigada pelo assistente |
| **Novo comando / solução** | Pendente |
EOF

echo "[PostToolUseFailure] Erro registrado automaticamente em bash-errors-log.md (#$next_number)"

exit 0
