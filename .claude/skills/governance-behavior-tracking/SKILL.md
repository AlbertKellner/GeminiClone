# Skill: governance-behavior-tracking

## Nome

Rastreamento de Comportamentos Esperados

## Descrição

Coleta, apresenta, rastreia e verifica todos os comportamentos esperados durante uma sessão de trabalho, conforme definidos nos arquivos de governança do repositório.

---

## Quando Usar

Esta skill é ativada automaticamente no início de qualquer tarefa de implementação, governança ou análise de PR. É o primeiro ato operacional após a classificação de escopo.

---

## Workflow Interno

```
FASE 0 — DETECTAR NOVA TAREFA

  Toda mensagem do usuário que constitui uma nova diretiva operacional (nova
  implementação, remoção, refatoração, correção, alteração de governança) é
  uma NOVA TAREFA — mesmo que chegue como interrupção mid-session.

  O assistente DEVE:
  1. Reconhecer a mudança de contexto
  2. Classificar o escopo da nova tarefa (Fase 1)
  3. Reiniciar o pipeline de comportamentos para a nova tarefa
  4. Manter o TodoWrite anterior como referência histórica, mas criar nova
     lista de comportamentos para a nova tarefa

  Tratar uma nova diretiva como "continuação" da tarefa anterior — sem
  classificação de escopo nem pipeline — é uma omissão de governança.

FASE 1 — COLETAR COMPORTAMENTOS ESPERADOS

  1. Classificar o escopo da tarefa:
     - Código → todos os passos do pipeline (0 → 11)
     - Governança → apenas passos 0.1, 9, 10
     - Análise de PR → conforme skill pr-analysis

  2. Derivar passos do pipeline pré-commit (CLAUDE.md):
     - Escopo código:
       [0]  Verificar pré-requisitos de ambiente
       [0.1] Executar auditoria de governança
       [1]  dotnet build
       [2]  dotnet run + health check
       [3]  dotnet test (gate obrigatório)
       [4]  docker compose up -d
       [5]  Health check HTTP 200
       [6]  Validação de endpoints via HTTP
       [7]  Exibir logs do container
       [8]  docker compose down
       [9]  Commit
       [10] Criar/atualizar PR
       [11] Acompanhar CI + verificar logs Datadog
       [12] Perguntar sobre revisão automática de PR
     - Escopo governança:
       [0.1] Executar auditoria de governança
       [9]   Commit
       [9.1] Validação de governança via subagentes (condicional — apenas quando afeta pipeline)
       [10]  Criar/atualizar PR

  3. Derivar comportamentos obrigatórios (CLAUDE.md seção "Comportamento Obrigatório"):
     - Interpretar antes de agir
     - Ler governança relevante antes de implementar
     - Verificar ambiguidades antes de implementar
     - Classificar trechos técnicos enviados pelo usuário (quando aplicável)
     - Atualizar governança primeiro (quando aplicável)
     - Seguir prioridade entre fontes de verdade
     - Usar contexto acumulado do repositório
     - Não depender de repetição de instruções
     - Avaliar eficiência em toda tarefa
     - Proteção de branch em análise de PR (quando aplicável)
     - Rastrear comportamentos esperados (esta skill)

  4. Derivar comportamentos de skills ativados:
     - implement-request: normalizar, classificar, ler governança, verificar ambiguidades,
       classificar trechos, registrar dúvidas, atualizar governança, avaliar propagação,
       implementar, verificar cobertura, relatar
     - validate-endpoints: identificar endpoints, obter token, consumir, validar cache,
       exibir logs storytelling
     - manage-pr-lifecycle: verificar PR existente, criar/atualizar, acompanhar CI
     - verify-environment: checklist de pré-requisitos
     - Outros skills conforme ativação pela tarefa

  5. Eliminar duplicatas (comportamentos que aparecem em múltiplas fontes)

  6. Remover comportamentos inaplicáveis ao escopo:
     - Sem trechos técnicos → remover "Classificar trechos técnicos"
     - Sem definição durável nova → remover "Atualizar governança primeiro"
     - Sem endpoint novo/alterado → remover "Validação de endpoints"
     - Escopo governança → remover passos de build/docker/test

FASE 2 — APRESENTAR VIA TODOWRITE

  1. Criar lista TodoWrite com todos os comportamentos, organizados em grupos:

     GRUPO: Governança e Interpretação
       - Interpretar intenção do usuário
       - Ler governança relevante
       - Verificar ambiguidades
       - [outros aplicáveis]

     GRUPO: Pipeline de Validação
       - [Passo 0] Verificar ambiente
       - [Passo 0.1] Auditoria de governança
       - [Passo 1] Build
       - [outros passos aplicáveis]

     GRUPO: Encerramento
       - [Passo 9] Commit
       - [Passo 10] PR
       - [Passo 11] CI + Datadog
       - [Passo 12] Perguntar sobre revisão automática de PR
       - Verificação final de comportamentos (esta skill, Fase 4)

  2. Todos marcados como pending inicialmente

FASE 3 — ATUALIZAR DURANTE EXECUÇÃO

  1. Marcar cada comportamento como in_progress ao iniciá-lo
  2. Marcar como completed ao concluí-lo com sucesso
  3. Se a tarefa revelar novos comportamentos necessários:
     - Adicionar ao TodoWrite como pending
  4. Se um comportamento se tornar inaplicável durante a execução:
     - Remover do TodoWrite (não manter como pendente)
  5. Se um comportamento é bloqueado por erro de ferramenta (MCP ainda não conectou, Docker DNS, rede):
     - NÃO remover do TodoWrite — manter como pending com nota de bloqueio
     - NÃO declarar "MCP indisponível" prematuramente — a inicialização assíncrona pode estar em andamento
     - Prosseguir com outros passos que não dependam da ferramenta bloqueada
     - Re-tentar ToolSearch a cada interação (máximo 3 tentativas explícitas)
     - Se a ferramenta reconectar durante a sessão, retomar imediatamente os passos adiados
     - Apenas na Fase 4, se não foi possível retomar após 3 tentativas, classificar como (iii)

FASE 4 — VERIFICAÇÃO FINAL (OBRIGATÓRIA)

  ENFORCEMENT: A Fase 4 é OBRIGATÓRIA e não pode ser omitida. O relatório de
  comportamentos deve ser o último ato antes de considerar a tarefa concluída.
  A ausência do relatório é ela própria uma omissão de comportamento.

  1. Revisar o TodoWrite inteiro
  2. Para cada comportamento NÃO marcado como completed:
     a. Classificar o motivo:
        (i)   Inaplicável ao escopo — justificar
        (ii)  Omitido/esquecido — executar imediatamente
        (iii) Bloqueado por erro — registrar em bash-errors-log.md
              Exemplo: passo 10 bloqueado por MCP ainda não conectado → verificar se
              MCP reconectou durante a sessão (re-tentar ToolSearch); se sim, executar
              agora; se não após 3 tentativas, registrar como (iii) com justificativa
     b. Para (ii) e (iii), investigar causa raiz:
        - Por que o comportamento não foi executado?
        - O que na governança falhou em garantir a execução?
        - Qual correção previne recorrência?
     c. Implementar correção da causa raiz (atualizar skill, rule, hook ou pipeline)

  3. Emitir relatório de comportamentos:
     - Total esperados
     - Total executados
     - Total inaplicáveis (com justificativa)
     - Total omitidos e corrigidos (com causa raiz)
     - Ações de saneamento (se houver)
```

---

## Arquivos de Governança Relacionados

- `.claude/rules/governance-behavior-tracking.md` — política que este workflow implementa
- `CLAUDE.md` — fonte dos passos do pipeline e comportamentos obrigatórios
- `.claude/rules/execution-time-tracking.md` — complementar (tempo + comportamentos)
- `.claude/rules/governance-audit.md` — auditoria de artefatos complementa auditoria de comportamentos
- `Instructions/operating-model.md` — classificação de tipos de mensagem e skills ativados

---

## Histórico de Mudanças

| Data | Mudança | Referência |
|---|---|---|
| 2026-03-21 | Criado: workflow de rastreamento de comportamentos esperados | Instrução do usuário |
| 2026-04-02 | Corrigido: passo 10.1 (revisão automática de PR) adicionado em 3 locais — lista de passos escopo código, GRUPO Encerramento e exemplos. Causa raiz: omissão nos exemplos levou a não inclusão no TodoWrite | Análise de causa raiz — omissão de comportamento |
| 2026-04-02 | Adicionado: Fase 3 item 5 (comportamentos bloqueados por erro de ferramenta — manter pending, retomar ao reconectar); Fase 4 marcada como OBRIGATÓRIA com enforcement explícito; exemplo de MCP em categoria (iii) | Análise exaustiva de omissões |
| 2026-04-02 | Corrigido: "MCP indisponível" substituído por "MCP ainda não conectou"; adicionado protocolo de retry (3 tentativas via ToolSearch) antes de declarar bloqueio | Diagnóstico de MCP — Erro 12 |
| 2026-04-02 | Adicionado: Fase 0 (detectar nova tarefa) — toda nova diretiva do usuário requer reclassificação de escopo e reinício do pipeline; tratar como continuação sem classificação é omissão de governança | Análise de omissões na remoção de feature |
