---
paths:
  - "**/.session-timer"
---

# Regra: Rastreamento de Tempo de Execução Efetivo

## Propósito

Esta rule define a política de rastreamento e reporte de tempo de execução efetivo do assistente durante sessões de trabalho. O tempo efetivo exclui períodos de ociosidade (espera por resposta ou interação do usuário).

---

## Princípio Fundamental

> Visibilidade do tempo de execução permite ao usuário avaliar eficiência e planejar sessões.
> Tempo ocioso não é tempo de trabalho — apenas o tempo em que o assistente está ativamente processando é contabilizado.

---

## Políticas

### Tempo Efetivo de Sessão

O rastreamento de tempo efetivo é **auto-gerenciado pelo hook `session-timer.sh`**, que é acionado automaticamente após cada chamada Bash (PostToolUse). O assistente não precisa criar nem atualizar o arquivo de estado manualmente.

#### Inicialização automática

O hook `session-timer.sh` auto-inicializa o arquivo `.claude/.session-timer` na primeira invocação:
- Se o arquivo não existe → cria com valores iniciais e exibe `[Sessão iniciada: HH:MM:SS]`
- Se o arquivo existe mas tem mais de 4 horas → reseta (sessão anterior encerrada anormalmente)
- Se o arquivo existe e é válido → continua a sessão

#### Segmentos de Trabalho

A detecção de segmentos é automática, baseada no intervalo entre invocações:
- **Gap > 120 segundos** entre invocações → novo segmento (assistente estava inativo aguardando o usuário)
- **Gap ≤ 120 segundos** → continuação do mesmo segmento; tempo acumulado
- O tempo de inatividade (gap entre segmentos) **não é contabilizado**

#### Reporte Periódico

A cada ~60 segundos de tempo efetivo acumulado, o hook exibe automaticamente:

```
[Tempo efetivo: MM:SS]
```

#### Reporte Final

Ao concluir a sessão ou a última tarefa, o assistente deve emitir um resumo baseado nos dados do arquivo de estado:

```
[Tempo total efetivo da sessão: HH:MM:SS (N segmentos de trabalho)]
```

#### Arquivo de Estado

| Campo | Descrição |
|---|---|
| `SESSION_START` | Epoch em milissegundos do início da sessão |
| `SEGMENTS_TOTAL_MS` | Tempo efetivo acumulado em milissegundos |
| `LAST_SEGMENT_START` | Epoch em milissegundos do início do segmento atual |
| `LAST_INVOCATION` | Epoch em milissegundos da última invocação do hook |
| `LAST_REPORT_AT_MS` | Valor de `SEGMENTS_TOTAL_MS` no momento do último reporte periódico |
| `SEGMENT_COUNT` | Número de segmentos de trabalho processados |

- **Localização**: `.claude/.session-timer`
- **Formato**: pares `CHAVE=VALOR` (compatível com `source` do bash)
- **Ciclo de vida**: criado automaticamente pelo hook na primeira invocação, transiente — não versionado (adicionado ao `.gitignore`)

### Métricas de Tempo do Pipeline CI

O tempo de execução do pipeline de CI/CD é rastreado via check runs do GitHub Actions, disponíveis pela ferramenta MCP `pull_request_read` com método `get_check_runs`.

#### Fonte de dados

Cada check run retorna `started_at` e `completed_at`, permitindo calcular:
- Duração individual de cada job
- Tempo total do pipeline (wall clock: primeiro `started_at` até último `completed_at`)
- Ganho de paralelismo (soma dos jobs vs. wall clock)

#### Cálculo

O script `scripts/pipeline-timing.sh` recebe o JSON dos check runs via stdin e exibe tabela formatada:

```bash
echo '<json_dos_check_runs>' | bash scripts/pipeline-timing.sh <PR_NUMBER>
```

#### Quando reportar

- **Após conclusão do CI (passo 11)**: exibir métricas de tempo do pipeline como parte do relatório final
- **Para estimar tempo de espera**: consultar check runs do PR anterior para obter tempos históricos e calcular estratégia de polling

---

## Relação com Outras Rules

- `environment-readiness.md` — a inicialização do timer faz parte da preparação do ambiente de sessão
- `governance-policies.md` — eficiência de execução (§2) complementada por visibilidade de tempo
- `pr-metadata-governance.md` — métricas de pipeline são reportadas no encerramento da tarefa (passo 11)

---

## Histórico de Mudanças

| Data | Mudança | Referência |
|---|---|---|
| 2026-03-21 | Criado: regra de rastreamento de tempo de execução efetivo | Instrução do usuário |
| 2026-03-30 | Corrigido: inicialização auto-gerenciada pelo hook (não depende mais do assistente); adicionadas métricas de pipeline CI via check runs + `pipeline-timing.sh` | Correção de implementação |
