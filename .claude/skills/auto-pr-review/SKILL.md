---
name: auto-pr-review
description: Revisão automática de código em PRs com ciclo Revisor↔Codificador
context: fork
allowed-tools:
  - Bash
  - Read
  - Grep
  - Glob
  - mcp__github__*
  - mcp__github-revisor__*
---

# Skill: auto-pr-review

## Propósito

Executar revisão automática de código em Pull Requests usando dois papéis distintos — **Revisor** e **Codificador** — com ciclo iterativo até aprovação. A revisão verifica conformidade integral com as diretrizes de governança do repositório.

---

## Nome

Revisão Automática de Pull Request

## Quando Usar

Esta skill é ativada:
- Automaticamente após a conclusão do acompanhamento de GitHub Actions (passo 12 do pipeline, após o passo 11) — o assistente pergunta ao usuário se deseja revisão automática
- Explicitamente pelo usuário: "faça a revisão automática do PR", "execute o auto-pr-review do PR #N"

**A skill NUNCA prossegue sem confirmação positiva do usuário.**

---

## Papéis e Identidades

| Papel | Username GitHub | MCP Server | Tools Prefix | Email (git) | Faz commit? |
|-------|----------------|------------|--------------|-------------|-------------|
| **Codificador** | `ClaudeCode-Bot` | `github` | `mcp__github__*` | `ClaudeCode-Bot@users.noreply.github.com` | Sim |
| **Revisor** | `Claude-Revisor` | `github-revisor` | `mcp__github-revisor__*` | `Claude-Revisor@users.noreply.github.com` | Não (nunca) |

**Git push**: sempre usa credenciais padrão do container (AlbertKellner), independente do papel ativo.

---

## Workflow

```
1. IDENTIFICAR PR
   - Receber número do PR (do trigger pós-passo-11 ou indicação do usuário)
   - Obter head.ref, arquivos alterados via MCP (pull_request_read method: get + get_files)

2. CONFIRMAR COM USUÁRIO
   - Perguntar: "Deseja que a revisão automática de código seja realizada neste PR?"
   - Só prosseguir com confirmação positiva
   - Se negativa → encerrar sem ação

3. CICLO DE REVISÃO (máximo 10 iterações)

   PRÉ-CONDIÇÃO (verificar ANTES de cada fase 3a ou 3b):
   - Consultar status do PR via MCP (pull_request_read method: get)
   - Se PR está closed ou merged → ENCERRAR ciclo, reportar ao usuário
   - Se há conflito de merge → PAUSAR ciclo, reportar ao usuário

   3a. FASE REVISOR (subagent isolado)

       REGRA CRÍTICA DE ISOLAMENTO MCP:
       Para TODA operação GitHub (ler PR, comentar, submeter review, resolver threads)
       usar APENAS ferramentas com prefixo mcp__github-revisor__.
       NUNCA usar mcp__github__ durante esta fase.

       PASSOS:
       1. git fetch origin <head.ref> && git checkout <head.ref>
       2. NÃO fazer git commit nem git push (Revisor nunca altera código)
       3. Ler lista de arquivos alterados via MCP (pull_request_read method: get_files)
       4. Ler o conteúdo COMPLETO de cada arquivo alterado (Read tool — arquivo inteiro, não apenas diff)
       5. Ler governança relevante:
          - Instructions/architecture/engineering-principles.md
          - Instructions/architecture/patterns.md
          - Instructions/architecture/naming-conventions.md
          - Instructions/architecture/folder-structure.md
          - Instructions/architecture/technical-overview.md
          - Instructions/snippets/canonical-snippets.md
          - Instructions/glossary/ubiquitous-language.md
          - Instructions/business/business-rules.md
          - .claude/rules/ (todas as policies aplicáveis)
       6. Verificar cada arquivo inteiro contra diretrizes de governança
       7. Ler TODAS as threads de review (inclusive outdated) via get_review_comments:
          - Para threads outdated: re-avaliar se o problema persiste no código atual
          - Para threads resolvidas por humano: respeitar resolução (não re-abrir)

       TRATAMENTO DE COMENTÁRIOS DE TERCEIROS (não-Revisor, não-Codificador):
       - Se comentário é aderente à governança → responder "concordo"
       - Se comentário NÃO é aderente → explicar por que não está em conformidade
         com as diretrizes de governança e sugerir alternativa alinhada

       TRATAMENTO DE REVIEWS FORMAIS DE HUMANOS:
       - Se humano submeteu REQUEST_CHANGES → incorporar apontamentos na análise
       - Tratar como apontamentos adicionais a serem verificados

       VERIFICAÇÃO DE CORREÇÕES DO CODIFICADOR:
       - Threads com última resposta do Codificador indicando correção feita:
         - Verificar a alteração correspondente no código atual
         - Se adequada → resolver thread (mcp__github-revisor__resolve_review_thread)
       - Threads com última resposta do Codificador indicando inviabilidade:
         - Re-avaliar: se justificativa for válida → resolver thread
         - Se justificativa for insuficiente → insistir com alternativa

       SUBMISSÃO DE REVIEW:
       - Se problemas encontrados:
         a. Criar review pendente (mcp__github-revisor__pull_request_review_write method: create, sem event)
         b. Comentar nos trechos/linhas exatos (mcp__github-revisor__add_comment_to_pending_review)
         c. Submeter review REQUEST_CHANGES (mcp__github-revisor__pull_request_review_write method: submit_pending, event: REQUEST_CHANGES)
       - Se nenhum problema restante + todas threads do Revisor resolvidas:
         a. Submeter review APPROVE (mcp__github-revisor__pull_request_review_write method: create, event: APPROVE)
         b. ENCERRAR ciclo → ir para passo 4

       REGRA DE INSISTÊNCIA HUMANA:
       - Se um humano insistir em comentário que o Revisor considerou não aderente à governança:
         - O Revisor NÃO cede — governança prevalece
         - Re-explicar a diretriz e indicar que a governança deve ser alterada primeiro
         - O Codificador NÃO implementará (thread sem "concordo" do Revisor)

   3b. FASE CODIFICADOR (subagent isolado)

       REGRA CRÍTICA DE ISOLAMENTO MCP:
       Para TODA operação GitHub (ler PR, comentar, responder)
       usar APENAS ferramentas com prefixo mcp__github__.
       NUNCA usar mcp__github-revisor__ durante esta fase.

       PASSOS:
       1. git fetch origin <head.ref> && git checkout <head.ref>
       2. git config user.name "ClaudeCode-Bot"
          git config user.email "ClaudeCode-Bot@users.noreply.github.com"
       3. Ler threads de review via MCP (mcp__github__pull_request_read method: get_review_comments)

       FILTRO DE THREADS — para cada thread não resolvida:
       (a) Se TODOS os comentários são do Codificador (ClaudeCode-Bot) e/ou Revisor (Claude-Revisor)
           → thread processável
       (b) Se há comentário(s) de terceiro(s) (nem Codificador nem Revisor):
           - Se último comentário da thread é do Revisor com texto exato "concordo"
             → thread processável
           - Caso contrário → IGNORAR thread (não há solução de implementação aprovada)

       IMPLEMENTAÇÃO:
       4. Para cada thread processável:
          - Se consegue implementar: implementar correção seguindo governança
          - Se NÃO consegue (inviável, conflito com outra regra):
            responder na thread explicando por que não é possível
       5. git add dos arquivos alterados
       6. git commit -m "fix: corrigir apontamentos de revisão automática"
          (identidade Codificador configurada no passo 2)
       7. git push -u origin <head.ref>
          (credenciais padrão AlbertKellner — NÃO alterar git config para push)
       8. Se git push falhar por conflito de merge → PAUSAR ciclo, reportar ao usuário

       RESPOSTAS:
       9. Responder a cada thread processada via MCP (mcp__github__add_reply_to_pull_request_comment):
          - Correção feita → informar o que foi alterado, referenciar commit
          - Inviabilidade → explicar por que não é possível implementar

       10. Voltar para pré-condição → 3a (próxima iteração do Revisor)

4. ENCERRAMENTO
   - Se Revisor aprovou: reportar resultado ao usuário
     (reviews humanas são independentes do ciclo — só o Revisor precisa aprovar)
   - Incluir no relatório final:
     - Threads resolvidas (com resumo de cada correção)
     - Threads ignoradas de humanos (sem "concordo" do Revisor) — listar com justificativa
     - Threads resolvidas por humano manualmente (respeitadas, não reimplementadas)
     - Número de iterações executadas
   - Se max 10 iterações atingido: pausar e reportar estado detalhado de cada thread
   - Se conflito de merge: pausar e reportar ao usuário
   - Se PR fechado/merged externamente: reportar estado e encerrar
```

---

## Saídas Esperadas

- Review comments do Revisor nos trechos exatos que precisam de ajuste
- Correções implementadas pelo Codificador em resposta aos apontamentos
- Respostas individuais em cada thread de review
- PR aprovado pelo Revisor quando todos os apontamentos forem resolvidos
- Relatório final com estado de todas as threads

---

## Ferramentas MCP Utilizadas

### Fase Revisor (prefixo `mcp__github-revisor__`)

| Ferramenta | Método | Propósito |
|---|---|---|
| `pull_request_read` | `get` | Verificar status do PR |
| `pull_request_read` | `get_files` | Listar arquivos alterados |
| `pull_request_read` | `get_review_comments` | Ler todas as threads de review |
| `pull_request_read` | `get_reviews` | Verificar reviews formais existentes |
| `pull_request_review_write` | `create` (sem event) | Criar review pendente |
| `pull_request_review_write` | `submit_pending` | Submeter review (REQUEST_CHANGES ou APPROVE) |
| `pull_request_review_write` | `create` (com event) | Submeter review direta (APPROVE) |
| `add_comment_to_pending_review` | — | Adicionar comentário inline à review pendente |
| `add_reply_to_pull_request_comment` | — | Responder a comentários de terceiros ("concordo" ou alternativa) |
| `resolve_review_thread` | — | Resolver thread após verificar correção |

### Fase Codificador (prefixo `mcp__github__`)

| Ferramenta | Método | Propósito |
|---|---|---|
| `pull_request_read` | `get` | Verificar status do PR |
| `pull_request_read` | `get_review_comments` | Ler threads de review para identificar apontamentos |
| `add_reply_to_pull_request_comment` | — | Responder a cada thread processada |

### Carregamento de Ferramentas MCP

As ferramentas MCP carregam automaticamente via inicialização assíncrona do Claude Code. Para usá-las, carregar via `ToolSearch` com sintaxe `select:`:

```
ToolSearch("select:mcp__github__pull_request_read,mcp__github__add_reply_to_pull_request_comment")
ToolSearch("select:mcp__github-revisor__pull_request_read,mcp__github-revisor__pull_request_review_write")
ToolSearch("select:mcp__github-revisor__add_comment_to_pending_review,mcp__github-revisor__add_reply_to_pull_request_comment")
```

**NUNCA** usar busca por keywords — sempre usar `select:` com nome exato da ferramenta.

**Se ToolSearch não retornar as ferramentas**: não declarar "MCP indisponível" prematuramente — a inicialização assíncrona pode estar em andamento. Prosseguir com passos que não dependam de MCP e re-tentar `ToolSearch` nas interações seguintes (máximo 3 tentativas). Se persistir, reportar ao usuário e registrar em `bash-errors-log.md`.

---

## Arquivos de Governança Relacionados

- `.claude/rules/auto-pr-review-governance.md` — política que este workflow implementa
- `.claude/rules/governance-policies.md` — políticas de contexto (§2), propagação (§3), ambiguidade (§4)
- `.claude/rules/source-of-truth-priority.md` — hierarquia de prioridade entre fontes de verdade
- `.claude/rules/architecture-governance.md` — governança de decisões arquiteturais
- `.claude/rules/naming-governance.md` — governança de nomenclatura
- `.claude/rules/pr-metadata-governance.md` — governança de metadados de PR
- `Instructions/architecture/technical-overview.md` — visão técnica e restrições
- `Instructions/architecture/engineering-principles.md` — princípios de engenharia
- `Instructions/architecture/patterns.md` — padrões adotados
- `Instructions/architecture/naming-conventions.md` — convenções de nomenclatura
- `Instructions/architecture/folder-structure.md` — estrutura de pastas
- `Instructions/snippets/canonical-snippets.md` — snippets normativos obrigatórios
- `Instructions/glossary/ubiquitous-language.md` — terminologia do domínio
- `Instructions/business/business-rules.md` — regras de negócio ativas

---

## Histórico de Mudanças

| Data | Mudança | Referência |
|---|---|---|
| 2026-03-31 | Criado: skill de revisão automática de PR com ciclo Revisor↔Codificador | Instrução do usuário |
| 2026-04-02 | Corrigido: seção MCP — ferramentas carregam automaticamente (inicialização assíncrona); protocolo de retry adicionado | Diagnóstico de MCP — Erro 12 |
