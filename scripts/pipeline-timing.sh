#!/bin/bash
# Script: pipeline-timing.sh
# Propósito: Calcular e exibir métricas de tempo do pipeline CI a partir de check runs do GitHub
# Uso: echo '<json_check_runs>' | bash scripts/pipeline-timing.sh [PR_NUMBER]
#
# Entrada: JSON do resultado de pull_request_read com method: get_check_runs (via stdin)
# Formato esperado: {"total_count": N, "check_runs": [{"name": "...", "started_at": "...", "completed_at": "...", "status": "...", "conclusion": "..."}, ...]}
#
# Saída: Tabela formatada com duração por job e total do pipeline

set -euo pipefail

PR_NUMBER="${1:-}"
INPUT=$(cat)

# Verificar se jq está disponível
if ! command -v jq &>/dev/null; then
    echo "ERRO: jq não está instalado. Necessário para processar JSON de check runs." >&2
    exit 1
fi

# Verificar se input é JSON válido com check_runs
if ! echo "$INPUT" | jq -e '.check_runs' &>/dev/null; then
    echo "ERRO: JSON inválido ou sem campo 'check_runs'." >&2
    echo "Formato esperado: {\"total_count\": N, \"check_runs\": [...]}" >&2
    exit 1
fi

TOTAL_COUNT=$(echo "$INPUT" | jq '.total_count')
if [ "$TOTAL_COUNT" -eq 0 ]; then
    echo "Nenhum check run encontrado."
    exit 0
fi

# Função: converter ISO 8601 para epoch seconds
iso_to_epoch() {
    local ts="$1"
    # Remover o Z e converter
    date -d "$ts" +%s 2>/dev/null || date -u -d "${ts%Z}" +%s 2>/dev/null || echo "0"
}

# Função: formatar duração em MM:SS ou HH:MM:SS
format_duration() {
    local secs="$1"
    local hours=$((secs / 3600))
    local mins=$(( (secs % 3600) / 60 ))
    local sec=$((secs % 60))
    if [ "$hours" -gt 0 ]; then
        printf "%02d:%02d:%02d" "$hours" "$mins" "$sec"
    else
        printf "%02d:%02d" "$mins" "$sec"
    fi
}

# Função: extrair hora de timestamp ISO
extract_time() {
    local ts="$1"
    date -d "$ts" +%H:%M:%S 2>/dev/null || date -u -d "${ts%Z}" +%H:%M:%S 2>/dev/null || echo "??:??:??"
}

# Extrair dados dos check runs e ordenar por started_at
RUNS=$(echo "$INPUT" | jq -r '.check_runs | sort_by(.started_at) | .[] | [.name, .status, .conclusion // "null", .started_at, .completed_at // "null"] | @tsv')

# Header
TITLE="Métricas do Pipeline"
if [ -n "$PR_NUMBER" ]; then
    TITLE="Métricas do Pipeline (PR #${PR_NUMBER})"
fi

echo ""
echo "=== $TITLE ==="
echo ""

# Calcular largura da coluna de nomes
MAX_NAME_LEN=3  # mínimo "Job"
while IFS=$'\t' read -r name status conclusion started completed; do
    name_len=${#name}
    if [ "$name_len" -gt "$MAX_NAME_LEN" ]; then
        MAX_NAME_LEN=$name_len
    fi
done <<< "$RUNS"

# Header da tabela
printf "%-${MAX_NAME_LEN}s   %-10s   %-8s   %s\n" "Job" "Status" "Duração" "Início → Fim"
SEPARATOR_LEN=$((MAX_NAME_LEN + 10 + 8 + 20 + 9))
printf '%*s\n' "$SEPARATOR_LEN" '' | tr ' ' '─'

# Variáveis para cálculo do total
PIPELINE_FIRST_START=""
PIPELINE_LAST_END=""
PIPELINE_FIRST_START_EPOCH=999999999999
PIPELINE_LAST_END_EPOCH=0
TOTAL_JOB_SECONDS=0
ALL_COMPLETED=true
HAS_FAILURES=false

# Processar cada run
while IFS=$'\t' read -r name status conclusion started completed; do
    # Status icon
    if [ "$status" != "completed" ]; then
        STATUS_DISPLAY="⏳ $status"
        ALL_COMPLETED=false
        DURATION_DISPLAY="--:--"
        TIME_RANGE="em andamento"
    else
        if [ "$conclusion" = "success" ]; then
            STATUS_DISPLAY="✅ ok"
        elif [ "$conclusion" = "failure" ]; then
            STATUS_DISPLAY="❌ falha"
            HAS_FAILURES=true
        elif [ "$conclusion" = "cancelled" ]; then
            STATUS_DISPLAY="⚠️  cancel"
        else
            STATUS_DISPLAY="⚪ $conclusion"
        fi

        if [ "$completed" != "null" ] && [ -n "$completed" ]; then
            START_EPOCH=$(iso_to_epoch "$started")
            END_EPOCH=$(iso_to_epoch "$completed")
            DURATION_SECS=$((END_EPOCH - START_EPOCH))
            TOTAL_JOB_SECONDS=$((TOTAL_JOB_SECONDS + DURATION_SECS))
            DURATION_DISPLAY=$(format_duration "$DURATION_SECS")
            TIME_RANGE="$(extract_time "$started") → $(extract_time "$completed")"

            # Rastrear primeiro e último timestamp para total do pipeline
            if [ "$START_EPOCH" -lt "$PIPELINE_FIRST_START_EPOCH" ]; then
                PIPELINE_FIRST_START_EPOCH=$START_EPOCH
                PIPELINE_FIRST_START="$started"
            fi
            if [ "$END_EPOCH" -gt "$PIPELINE_LAST_END_EPOCH" ]; then
                PIPELINE_LAST_END_EPOCH=$END_EPOCH
                PIPELINE_LAST_END="$completed"
            fi
        else
            DURATION_DISPLAY="--:--"
            TIME_RANGE="sem dados"
        fi
    fi

    printf "%-${MAX_NAME_LEN}s   %-10s   %-8s   %s\n" "$name" "$STATUS_DISPLAY" "$DURATION_DISPLAY" "$TIME_RANGE"

done <<< "$RUNS"

# Linha separadora
printf '%*s\n' "$SEPARATOR_LEN" '' | tr ' ' '─'

# Total do pipeline (wall clock: primeiro start até último end)
if [ "$PIPELINE_FIRST_START_EPOCH" -lt 999999999999 ] && [ "$PIPELINE_LAST_END_EPOCH" -gt 0 ]; then
    PIPELINE_TOTAL_SECS=$((PIPELINE_LAST_END_EPOCH - PIPELINE_FIRST_START_EPOCH))
    PIPELINE_DISPLAY=$(format_duration "$PIPELINE_TOTAL_SECS")
    PIPELINE_TIME_RANGE="$(extract_time "$PIPELINE_FIRST_START") → $(extract_time "$PIPELINE_LAST_END")"

    RESULT_STATUS="✅ ok"
    if [ "$HAS_FAILURES" = true ]; then
        RESULT_STATUS="❌ falha"
    elif [ "$ALL_COMPLETED" = false ]; then
        RESULT_STATUS="⏳ exec"
    fi

    printf "%-${MAX_NAME_LEN}s   %-10s   %-8s   %s\n" "Total do pipeline" "$RESULT_STATUS" "$PIPELINE_DISPLAY" "$PIPELINE_TIME_RANGE"

    # Soma dos jobs vs wall clock (mostra paralelismo)
    if [ "$TOTAL_JOB_SECONDS" -gt "$PIPELINE_TOTAL_SECS" ]; then
        OVERLAP_SECS=$((TOTAL_JOB_SECONDS - PIPELINE_TOTAL_SECS))
        echo ""
        echo "Soma dos jobs: $(format_duration $TOTAL_JOB_SECONDS) | Paralelismo economizou: $(format_duration $OVERLAP_SECS)"
    fi
else
    printf "%-${MAX_NAME_LEN}s   %-10s   %-8s   %s\n" "Total do pipeline" "—" "--:--" "sem dados suficientes"
fi

echo ""
