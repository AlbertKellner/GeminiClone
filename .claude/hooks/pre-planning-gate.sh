#!/bin/bash
# Hook: pre-planning-gate.sh
# Propósito: Enforcement do comportamento obrigatório #12 (Consulta pré-planejamento)
# Gatilho: PreToolUse (Edit|Write) — acionado antes de qualquer escrita de arquivo
#
# Verifica se o assistente já executou a consulta pré-planejamento na sessão atual.
# Se não executou, emite lembrete para que o assistente verifique dúvidas, definições
# pendentes e cenários/fluxos não mapeados antes de prosseguir.
#
# O arquivo de estado .claude/.pre-planning-done é transiente (não versionado).
# O assistente o cria após completar o ciclo de consulta pré-planejamento.

REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
STATE_FILE="$REPO_ROOT/.claude/.pre-planning-done"
FILE_PATH="${1:-}"

# Ignorar edições em arquivos transientes e no próprio arquivo de estado
if [[ "$FILE_PATH" == *".pre-planning-done"* ]] || \
   [[ "$FILE_PATH" == *".session-timer"* ]] || \
   [[ "$FILE_PATH" == *".pr-analysis-context"* ]]; then
  exit 0
fi

# Ignorar edições no arquivo de plano (plan mode)
if [[ "$FILE_PATH" == */plans/* ]]; then
  exit 0
fi

# Se o gate já foi satisfeito nesta sessão, não emitir lembrete
if [ -f "$STATE_FILE" ]; then
  # Verificar se o arquivo de estado não é muito antigo (4 horas = 14400 segundos)
  if [ "$(uname)" = "Darwin" ]; then
    FILE_AGE=$(( $(date +%s) - $(stat -f %m "$STATE_FILE") ))
  else
    FILE_AGE=$(( $(date +%s) - $(stat -c %Y "$STATE_FILE") ))
  fi

  if [ "$FILE_AGE" -lt 14400 ]; then
    exit 0
  fi
  # Arquivo muito antigo — sessão anterior; remover e emitir lembrete
  rm -f "$STATE_FILE"
fi

# Emitir lembrete
echo "[Pre-planning gate] Existem dúvidas, definições pendentes ou fluxos/cenários não mapeados?"
echo "Verifique open-questions.md e os arquivos de governança pertinentes antes de prosseguir."
echo "Comportamento obrigatório #12 — ver .claude/rules/pre-planning-consultation.md"
echo ""
echo "Após completar a consulta pré-planejamento, crie o arquivo .claude/.pre-planning-done"
echo "para sinalizar que o gate foi satisfeito: touch .claude/.pre-planning-done"

exit 0
