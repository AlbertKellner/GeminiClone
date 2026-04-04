#!/bin/bash
# Hook: session-timer.sh
# Propósito: Auto-inicializar, rastrear e exibir tempo efetivo acumulado da sessão
# Ativação: PostToolUse em Bash
# Comportamento: Informativo (exit 0 sempre)
#
# Lógica:
#   - Se o arquivo de estado não existe → cria com valores iniciais (nova sessão)
#   - Se existe mas tem > 4 horas → reseta (sessão anterior encerrada anormalmente)
#   - Se gap entre invocações > IDLE_THRESHOLD_MS → novo segmento (assistente estava inativo)
#   - Acumula tempo efetivo e exibe periodicamente

REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
TIMER_FILE="$REPO_ROOT/.claude/.session-timer"

# Constantes configuráveis
IDLE_THRESHOLD_MS=120000        # 120 segundos — gap maior que isso = novo segmento
MAX_SESSION_AGE_MS=14400000     # 4 horas em milissegundos — sessão expirada
REPORT_INTERVAL_MS=60000        # Reportar a cada ~60 segundos de tempo efetivo

# Obter timestamp atual em milissegundos
NOW=$(date +%s%3N)

# --- Função: criar arquivo de estado inicial ---
init_session() {
    cat > "$TIMER_FILE" <<EOF
SESSION_START=$NOW
SEGMENTS_TOTAL_MS=0
LAST_SEGMENT_START=$NOW
LAST_INVOCATION=$NOW
LAST_REPORT_AT_MS=0
SEGMENT_COUNT=1
EOF
    local FORMATTED
    FORMATTED=$(date +%H:%M:%S)
    printf "[Sessão iniciada: %s]\n" "$FORMATTED"
    exit 0
}

# --- Se o arquivo não existe → nova sessão ---
if [ ! -f "$TIMER_FILE" ]; then
    init_session
fi

# --- Carregar variáveis do timer ---
source "$TIMER_FILE" 2>/dev/null || init_session

# Validar campos obrigatórios
if [ -z "$SESSION_START" ] || [ -z "$SEGMENTS_TOTAL_MS" ] || [ -z "$LAST_SEGMENT_START" ] || [ -z "$LAST_INVOCATION" ]; then
    init_session
fi

# --- Se sessão tem mais de 4 horas → resetar ---
SESSION_AGE_MS=$((NOW - SESSION_START))
if [ "$SESSION_AGE_MS" -gt "$MAX_SESSION_AGE_MS" ]; then
    init_session
fi

# --- Calcular gap desde última invocação ---
GAP_MS=$((NOW - LAST_INVOCATION))

if [ "$GAP_MS" -gt "$IDLE_THRESHOLD_MS" ]; then
    # Gap grande → assistente estava inativo → novo segmento
    # Não acumular o tempo de inatividade — apenas iniciar novo segmento
    SEGMENT_COUNT=$((SEGMENT_COUNT + 1))
    LAST_SEGMENT_START=$NOW
else
    # Continuação do segmento atual → acumular tempo desde última invocação
    SEGMENTS_TOTAL_MS=$((SEGMENTS_TOTAL_MS + GAP_MS))
fi

# Atualizar última invocação
LAST_INVOCATION=$NOW

# --- Formatar e exibir tempo efetivo ---
TOTAL_SEC=$((SEGMENTS_TOTAL_MS / 1000))
HOURS=$((TOTAL_SEC / 3600))
MINS=$(( (TOTAL_SEC % 3600) / 60 ))
SECS=$((TOTAL_SEC % 60))

# Verificar se deve reportar (a cada REPORT_INTERVAL_MS de tempo efetivo acumulado)
SINCE_LAST_REPORT=$((SEGMENTS_TOTAL_MS - LAST_REPORT_AT_MS))

if [ "$SINCE_LAST_REPORT" -ge "$REPORT_INTERVAL_MS" ]; then
    LAST_REPORT_AT_MS=$SEGMENTS_TOTAL_MS
    if [ "$HOURS" -gt 0 ]; then
        printf "[Tempo efetivo: %02d:%02d:%02d]\n" "$HOURS" "$MINS" "$SECS"
    else
        printf "[Tempo efetivo: %02d:%02d]\n" "$MINS" "$SECS"
    fi
fi

# --- Persistir estado atualizado ---
cat > "$TIMER_FILE" <<EOF
SESSION_START=$SESSION_START
SEGMENTS_TOTAL_MS=$SEGMENTS_TOTAL_MS
LAST_SEGMENT_START=$LAST_SEGMENT_START
LAST_INVOCATION=$LAST_INVOCATION
LAST_REPORT_AT_MS=$LAST_REPORT_AT_MS
SEGMENT_COUNT=$SEGMENT_COUNT
EOF

exit 0
