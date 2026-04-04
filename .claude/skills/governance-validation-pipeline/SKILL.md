---
name: governance-validation-pipeline
description: Validação funcional de governança via subagentes em dev e main
context: fork
allowed-tools:
  - Bash
  - Read
  - Grep
  - Glob
  - Agent
---

# Skill: governance-validation-pipeline

## Nome

Pipeline de Validação de Governança

## Descrição

Valida mudanças em arquivos de governança via subagentes que testam a branch de desenvolvimento e comparam com a branch main, garantindo que novos comportamentos são efetivamente aplicados e que não houve regressão de comportamentos existentes.

---

## Quando Usar

Esta skill é executada no **passo 9.1** do pipeline pré-commit, após o commit (passo 9) e antes da criação/atualização do PR (passo 10). É ativada **apenas quando a tarefa altera aspectos que afetam o pipeline de codificação**: passos do pipeline (0–11), comportamentos obrigatórios (1–13), skills de pipeline, rules de fluxo de codificação, ou hooks de enforcement. Mudanças puramente documentais não ativam esta skill.

---

## Workflow Interno

```
FASE 1 — PREPARAÇÃO

  1. Verificar que o último commit foi realizado na branch de desenvolvimento:
     - git log -1 --oneline → confirmar commit mais recente
     - Se não há commit pendente → erro: o passo 9 (commit) deve ser executado antes

  2. Identificar quais arquivos de governança foram alterados:
     - git diff HEAD~1 --name-only → listar arquivos alterados no último commit
     - Filtrar por: .claude/rules/*.md, .claude/skills/*/SKILL.md, CLAUDE.md,
       Instructions/**/*.md, REVIEW.md, .claude/hooks/*, .claude/settings.json

  3. Selecionar o tipo de cenário de teste:
     a. A mudança NÃO requer recursos externos (API, banco, Datadog)?
        → Cenário: endpoint simples
        → Exemplo: "Crie um endpoint GET de teste: se receber 1, retorna 'Um';
          se receber 2, retorna 'Dois'"
     b. O cenário simples NÃO atende à governança mais recente?
        → Cenário: endpoint compatível com governança
        → Adaptar o comando de criação de feature para atender aos requisitos mínimos
     c. A mudança envolve integração HTTP externa ou resiliência?
        → Cenário: API OpenMeteo
        → "Implemente consumo da API OpenMeteo (https://open-meteo.com) para clima
          e temperatura de São Paulo (latitude -23.55, longitude -46.63)"
     d. A validação pode ser feita apenas no plano de execução (sem codificar)?
        → Cenário: apenas planejamento
        → "Considere a criação do endpoint X — NÃO codifique, apenas elabore o plano
          seguindo toda a governança do repositório"

  4. Construir o comando de teste idêntico para ambos os subagentes:
     - O comando deve incluir:
       (i)   Instruções específicas derivadas da nova governança
             (ex: "Antes de todo planejamento, envie 'início dos testes'")
       (ii)  O cenário de teste selecionado no passo 3
       (iii) Instrução de não codificar quando aplicável (cenário 3d)
       (iv)  Instrução de reportar quais passos de governança foram identificados
     - O texto deve ser IDÊNTICO para ambos os subagentes

  5. Construir critérios de validação:
     - Critérios de sucesso para o novo comportamento:
       O que o subagente da branch dev DEVE fazer que reflete a nova governança?
     - Critérios de regressão:
       Quais comportamentos pré-existentes devem continuar funcionando em ambas as branches?

FASE 2 — LANÇAMENTO PARALELO DOS SUBAGENTES

  1. Lançar AMBOS os subagentes em paralelo (mesma mensagem, dois Agent tool calls):

     Subagente DEV:
     - Tipo: general-purpose (sem isolamento)
     - Prompt: o comando de teste construído na Fase 1, passo 4
     - Executa na branch de desenvolvimento com o commit mais recente

     Subagente MAIN:
     - Tipo: general-purpose, isolation: "worktree"
     - Prompt: o MESMO comando de teste (textualmente idêntico)
     - Executa com a governança da branch main (sem o novo comportamento)

  2. Aguardar conclusão de AMBOS os subagentes

FASE 3 — ANÁLISE DOS RESULTADOS

  1. Analisar resultado do subagente DEV:
     a. O novo comportamento de governança foi aplicado?
        - Verificar contra os critérios de sucesso da Fase 1, passo 5
        - Se SIM → registrar como APROVADO
        - Se NÃO → ir para passo 4 (ciclo de correção)
     b. Os comportamentos pré-existentes foram preservados?
        - Verificar contra os critérios de regressão da Fase 1, passo 5
        - Se algum comportamento existente foi omitido → registrar regressão

  2. Analisar resultado do subagente MAIN:
     - Listar todos os passos/comportamentos de governança que o subagente identificou
     - Comparar com a lista de passos da branch dev

  3. Se regressão detectada na dev:
     - Registrar cada comportamento omitido com detalhes
     - Incluir no relatório final como item a investigar

  4. Ciclo de correção (se novo comportamento NÃO aplicado no DEV):
     - Diagnosticar causa raiz:
       • Governança ambígua ou mal redigida?
       • Arquivo não importado no CLAUDE.md?
       • Conflito com outra rule ou comportamento?
       • Posicionamento incorreto do novo comportamento?
     - Corrigir os arquivos de governança
     - Criar novo commit com a correção
     - Relançar APENAS o subagente DEV (o resultado do main permanece válido)
     - Controle de tentativas:
       • Tentativa 1 → relançar
       • Tentativa 2 → relançar
       • Tentativa 3 → se ainda falhar, escalar ao usuário via AskUserQuestion
         com diagnóstico detalhado de cada tentativa

FASE 4 — COMPARAÇÃO E RELATÓRIO

  1. Comparar resultados dev vs main:

     Diferenças esperadas (OK):
       - Novo comportamento presente no dev, ausente no main
       - Exemplo: passo 9.1 no plano do dev, ausente no plano do main

     Diferenças inesperadas (ALERTA):
       - Passo/comportamento presente no main, ausente no dev → possível regressão
       - Passo/comportamento executado de forma diferente no dev vs main sem justificativa

  2. Emitir relatório de validação de governança:

     Relatório de Validação — Comportamento #13
     ├── Status: APROVADO / REPROVADO
     ├── Branch dev:
     │   ├── Novo comportamento aplicado: ✅/❌ (+ evidência)
     │   ├── Regressão de comportamentos existentes: ✅ nenhuma / ❌ lista
     │   └── Tentativas: N/3
     ├── Branch main (regressão):
     │   ├── Passos identificados: [lista]
     │   ├── Diferenças esperadas: [lista] → OK
     │   └── Diferenças inesperadas: [lista] → ALERTA
     └── Ações corretivas: [lista ou "nenhuma"]

  3. Se APROVADO:
     - Prosseguir para passo 10 (criação/atualização de PR)
     - Incluir resumo do relatório na descrição do PR

  4. Se REPROVADO (após 3 tentativas):
     - Reportar ao usuário com diagnóstico completo
     - Aguardar orientação antes de prosseguir
```

---

## Arquivos de Governança Relacionados

- `.claude/rules/governance-validation-pipeline.md` — política que este workflow implementa
- `CLAUDE.md` — define o passo 9.1 no pipeline pré-commit e o comportamento #13
- `.claude/rules/governance-behavior-tracking.md` — rastreia o passo 9.1 como comportamento esperado
- `.claude/rules/governance-audit.md` — auditoria estrutural complementar a esta validação funcional
- `.claude/rules/bash-error-logging.md` — falhas durante validação devem ser registradas

---

## Histórico de Mudanças

| Data | Mudança | Referência |
|---|---|---|
| 2026-04-01 | Criado: workflow de validação de governança via subagentes | Instrução do usuário |
