#!/bin/bash
# Hook: SessionStart — Auto-validate environment and inject context
# Runs once when a new session starts.
# - Cleans stale session state files
# - Prints branch context
# - Verifies critical environment variables
# Exit 0 always (informative, never blocking)

set -euo pipefail

REPO_ROOT="$(git rev-parse --show-toplevel 2>/dev/null || echo ".")"
STALE_THRESHOLD_SEC=14400 # 4 hours

# --- Clean stale session state files ---
for state_file in "$REPO_ROOT/.claude/.pre-planning-done" "$REPO_ROOT/.claude/.pr-analysis-context" "$REPO_ROOT/.claude/.compact-state"; do
  if [[ -f "$state_file" ]]; then
    file_age=$(( $(date +%s) - $(stat -c %Y "$state_file" 2>/dev/null || echo 0) ))
    if (( file_age > STALE_THRESHOLD_SEC )); then
      rm -f "$state_file"
    fi
  fi
done

# --- Inject branch context ---
current_branch=$(git rev-parse --abbrev-ref HEAD 2>/dev/null || echo "unknown")
recent_commits=$(git log --oneline -3 2>/dev/null || echo "no commits")

echo "[SessionStart] Branch: $current_branch"
echo "[SessionStart] Últimos commits:"
echo "$recent_commits" | sed 's/^/  /'

# --- Verify critical environment variables ---
missing_vars=()
for var in DD_API_KEY GH_CLAUDE_CODE_MCP_CODIFICADOR; do
  if [[ -z "${!var:-}" ]]; then
    missing_vars+=("$var")
  fi
done

if (( ${#missing_vars[@]} > 0 )); then
  echo "[SessionStart] AVISO: Variáveis ausentes: ${missing_vars[*]}"
  echo "[SessionStart] Consultar scripts/required-vars.md para detalhes."
fi

# --- Verify MCP server connectivity ---
mcp_check() {
  local name="$1" token_var="$2"
  local token="${!token_var:-}"
  [[ -z "$token" ]] && return 1
  local http_code
  http_code=$(curl -s -o /dev/null -w "%{http_code}" -m 10 \
    -X POST "https://api.githubcopilot.com/mcp/" \
    -H "Authorization: Bearer $token" \
    -H "Content-Type: application/json" \
    -d '{"jsonrpc":"2.0","method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"health","version":"1.0"}},"id":1}')
  [[ "$http_code" == "200" ]]
}

for server_info in "github:GH_CLAUDE_CODE_MCP_CODIFICADOR" "github-revisor:GH_CLAUDE_CODE_MCP_REVISOR"; do
  name="${server_info%%:*}"
  var="${server_info##*:}"
  if [[ -z "${!var:-}" ]]; then
    echo "[SessionStart] MCP '$name': token ausente ($var)"
  elif mcp_check "$name" "$var"; then
    echo "[SessionStart] MCP '$name': endpoint acessível (HTTP 200)"
  else
    echo "[SessionStart] AVISO: MCP '$name' não respondeu — verificar token e rede"
  fi
done

# --- Persist environment variables for the session via CLAUDE_ENV_FILE ---
if [[ -n "${CLAUDE_ENV_FILE:-}" ]]; then
  echo "export CURRENT_BRANCH=$current_branch" >> "$CLAUDE_ENV_FILE"
  echo "export REPO_ROOT=$REPO_ROOT" >> "$CLAUDE_ENV_FILE"
fi

exit 0
