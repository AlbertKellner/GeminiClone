#!/usr/bin/env bash
# Hook: post-commit-pr-reminder.sh
# Tipo: PostToolUse (Bash) — informativo, nunca bloqueante
# Propósito: Lembra o assistente de executar o passo 10 (criar/atualizar PR)
#            após git commit ou git push bem-sucedido.
#
# Referência: .claude/rules/pr-metadata-governance.md
#             CLAUDE.md — Pipeline de Validação Pré-Commit, passo 10

# Ler o comando executado a partir do stdin (formato JSON do Claude Code)
TOOL_INPUT=$(cat)
COMMAND=$(echo "$TOOL_INPUT" | grep -oP '"command"\s*:\s*"[^"]*"' | head -1 | sed 's/"command"\s*:\s*"//;s/"$//')

# Verificar se o comando é git commit ou git push
if ! echo "$COMMAND" | grep -qE 'git (commit|push)'; then
  exit 0
fi

# Verificar se estamos em um branch de trabalho (não main/master)
CURRENT_BRANCH=$(git branch --show-current 2>/dev/null)
if [ -z "$CURRENT_BRANCH" ] || echo "$CURRENT_BRANCH" | grep -qE '^(main|master)$'; then
  exit 0
fi

# Verificar se estamos em contexto de pr-analysis (PR já existe)
if [ -f ".claude/.pr-analysis-context" ]; then
  exit 0
fi

echo ""
echo "[LEMBRETE] Passo 10 do pipeline: verificar/criar PR para o branch '$CURRENT_BRANCH'."
echo "           Política: todo trabalho commitado deve ter um PR associado."
echo ""

exit 0
