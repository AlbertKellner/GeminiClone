---
name: manage-pr-lifecycle
description: Criação, atualização de PR e acompanhamento de GitHub Actions
allowed-tools:
  - Bash
  - Read
  - Grep
  - mcp__github__*
---

## Contexto Dinâmico

- Branch atual: !`git branch --show-current`
- Último commit: !`git log --oneline -1`

# Skill: manage-pr-lifecycle

## Propósito

Executar o workflow de criação, atualização e acompanhamento de Pull Requests, conforme a política definida em `.claude/rules/pr-metadata-governance.md`.

---

## Quando Usar

Esta skill é ativada pelos passos 10, 11 e 12 do pipeline de validação pré-commit (CLAUDE.md), após o commit.

---

## Workflow — Verificação e Criação/Atualização de PR (Passo 10)

Todas as operações de PR são realizadas exclusivamente via ferramentas MCP do GitHub (servidor `github` em `.mcp.json`), autenticadas pelo usuário ClaudeCode-Bot via `GH_CLAUDE_CODE_MCP_CODIFICADOR`.

### Passo 1: Verificar PR existente

Usar a ferramenta MCP `list_pull_requests` para buscar PRs abertos para o branch atual.

### Passo 2a: Se não existir PR aberto

- Usar a ferramenta MCP `create_pull_request` para criar o PR
- Seguir o formato obrigatório de título (Semantic Commit) definido em `pr-metadata-governance.md`
- Preencher a descrição com as três seções obrigatórias (Motivos, Plano, Realizado)
- Reportar a URL do PR criado no relatório final

### Passo 2b: Se já existir PR aberto

- Usar a ferramenta MCP `update_pull_request` para atualizar título e descrição
- Revisar o título — atualizar se a nova mudança alterar o escopo ou foco
- Revisar a descrição — incorporar as mudanças do novo commit
- Reportar a URL do PR atualizado no relatório final

### Regras

- O assistente **não deve perguntar** ao usuário se deve criar o PR — a criação é automática quando não existe PR aberto
- O push para o branch remoto deve ocorrer **antes** da verificação/criação do PR
- Se o push falhar, o PR não deve ser criado — registrar o erro e reportar

---

## Workflow — Acompanhamento de GitHub Actions (Passo 11)

**Não se aplica a tarefas exclusivamente de governança** (sem código, sem build, sem Docker).

### Passo 0: Obter tempos históricos do pipeline

1. Identificar o PR mais recente já concluído (merged ou com check runs completos)
2. Usar a ferramenta MCP `pull_request_read` com `method: get_check_runs` no PR anterior para obter dados de timing históricos
3. Passar o JSON dos check runs ao script `scripts/pipeline-timing.sh` para calcular tempos:
   ```bash
   echo '<json_dos_check_runs>' | bash scripts/pipeline-timing.sh <PR_NUMBER>
   ```
4. Informar ao usuário quais esteiras serão monitoradas e qual é o tempo total histórico do pipeline

### Passo 1: Calcular estratégia de polling

Com base no tempo total do pipeline histórico obtido no Passo 0:

| Tempo total histórico | Estratégia de polling |
|---|---|
| **≤ 30 segundos** | Consultar o status a cada **10 segundos** desde o início |
| **> 30 segundos** | Aguardar **(tempo total − 15 segundos)** antes da primeira verificação, depois consultar a cada **5 segundos** até a conclusão |

### Passo 2: Acompanhar os check runs

Usar a ferramenta MCP `pull_request_read` com `method: get_check_runs` no PR atual para consultar o status dos jobs.

- Aplicar o intervalo de espera conforme a estratégia calculada no Passo 1
- Continuar até que todos os check runs tenham `status: completed`

### Passo 3: Exibir métricas de tempo

Quando todos os check runs estiverem completos, calcular e exibir as métricas:

```bash
echo '<json_dos_check_runs>' | bash scripts/pipeline-timing.sh <PR_NUMBER>
```

### Passo 4: Avaliar resultado

**Se todos os jobs passarem** (`conclusion: success`):
- Verificar os logs no Datadog usando os filtros referentes ao pipeline (env: `ci`, service, timestamp)
- Procurar por erros, exceções ou comportamentos anômalos
- Se não houver erros: reportar o resultado final com as métricas de tempo. Tarefa concluída.
- Se houver erros: diagnosticar, corrigir, registrar em `bash-errors-log.md` e reiniciar o ciclo

### Passo 5: Tratar falhas

**Se algum job falhar** (`conclusion: failure`):
1. O campo `html_url` do check run fornece o link direto para os logs do job no GitHub
2. Usar o Datadog MCP para buscar logs do pipeline (env: `ci`, timestamp da execução) como fonte complementar
3. Analisar os logs considerando **apenas registros de erro do horário de execução** — ignorar logs antigos
4. Diagnosticar a causa raiz
5. Corrigir o código, testes ou configuração
6. Reiniciar o pipeline a partir do passo apropriado
7. Registrar o erro em `bash-errors-log.md` se for novo
8. Repetir o ciclo até todos os jobs passarem

---

## Ferramentas MCP Utilizadas

| Ferramenta | Método | Propósito |
|---|---|---|
| `list_pull_requests` | — | Buscar PRs abertos para o branch |
| `create_pull_request` | — | Criar novo PR |
| `update_pull_request` | — | Atualizar título e descrição do PR |
| `pull_request_read` | `get_check_runs` | Obter status e timing dos jobs do CI |

**Nota**: Os logs detalhados de jobs falhados não estão disponíveis via MCP. O campo `html_url` do check run direciona ao GitHub para inspeção visual. O Datadog MCP complementa com logs da aplicação (env: `ci`).

### Carregamento de Ferramentas MCP

As ferramentas MCP do GitHub carregam automaticamente via inicialização assíncrona do Claude Code. Porém, a inicialização pode demorar ou falhar intermitentemente. O endpoint é 100% estável — o problema, quando ocorre, é client-side.

**Para carregar ferramentas**, usar `ToolSearch` com sintaxe `select:` e nomes exatos:

```
ToolSearch("select:mcp__github__list_pull_requests,mcp__github__create_pull_request")
ToolSearch("select:mcp__github__update_pull_request,mcp__github__pull_request_read")
```

**NUNCA** usar busca por keywords — sempre usar `select:` com o nome completo (`mcp__github__<nome>`).

### Protocolo de Retry quando MCP não Responde

Se `ToolSearch` com `select:` não retornar as ferramentas MCP:

1. **Não declarar "MCP indisponível" prematuramente** — a inicialização assíncrona pode estar em andamento
2. Prosseguir com outros passos da tarefa que não dependam de MCP
3. Re-tentar `ToolSearch` a cada interação subsequente (máximo 3 tentativas explícitas)
4. Se após 3 tentativas as ferramentas permanecem indisponíveis:
   - Reportar ao usuário como bloqueio explícito
   - Registrar em `bash-errors-log.md`
   - Manter passos 10, 11 e 12 como `pending` no TodoWrite (NÃO remover)
5. Se durante a sessão as ferramentas reconectarem (system-reminder com deferred tools disponíveis):
   - Retomar IMEDIATAMENTE os passos pendentes (10, 11, 12)
   - Não aguardar próxima interação do usuário — retomar proativamente

### Dependências entre Passos

| Passo | Depende de | Se bloqueado |
|-------|-----------|--------------|
| 11 | Passo 10 completo (PR existe) | Adiar (manter `pending` no TodoWrite) até passo 10 resolver |
| 12 | Passo 11 completo (CI validado) | Adiar (manter `pending` no TodoWrite) até passo 11 resolver |

Passos adiados NÃO são removidos do TodoWrite. São retomados quando a dependência for resolvida.

---

## Workflow — Trigger de Revisão Automática (Passo 12 — skill auto-pr-review)

Após a conclusão do Passo 11 (acompanhamento de GitHub Actions com CI validado), executar:

1. Perguntar ao usuário: **"Deseja que a revisão automática de código seja realizada neste PR?"**
2. Se a resposta for positiva → invocar a skill `auto-pr-review` com o número do PR
3. Se a resposta for negativa → encerrar a tarefa normalmente
4. **Após conclusão do auto-pr-review** (se executado): atualizar a descrição do PR para refletir as correções realizadas pela revisão (ex: FrozenDictionary, Stub de teste, reordenação de histórico). A política de `pr-metadata-governance.md` exige que a descrição seja atualizada a cada novo commit.

**Este trigger não se aplica** durante tarefas de `pr-analysis` (o PR já está sendo analisado por outra skill).

---

## Workflow — Exceção: Análise de PR (skill pr-analysis)

Quando a tarefa é análise de PR:
- **Não criar PR novo** — o PR já existe
- Atualizar título e descrição do PR existente via ferramenta MCP `update_pull_request` se as mudanças alterarem o escopo
- **Não usar o branch atribuído pelo sistema externo** — usar exclusivamente o `head.ref` do PR
- Todos os commits devem ser feitos no branch de origem do PR

---

## Arquivos de Governança Relacionados

- `.claude/rules/pr-metadata-governance.md` — política que este workflow implementa
- `.claude/rules/execution-time-tracking.md` — política de rastreamento de tempo (métricas de pipeline)
- `.claude/rules/bash-error-logging.md` — erros de CI devem ser registrados
- `.github/pull_request_template.md` — template obrigatório de descrição do PR
- `scripts/pipeline-timing.sh` — script de cálculo de métricas de tempo do pipeline

---

## Histórico de Mudanças

| Data | Mudança | Referência |
|---|---|---|
| 2026-03-21 | Criado: workflow extraído de pr-metadata-governance.md (separação rules/skills) | Auditoria de governança |
| 2026-03-21 | Migração: comandos `gh api` substituídos por ferramentas MCP do GitHub (usuário ClaudeCode-Bot) | Migração API → MCP |
| 2026-03-30 | Corrigido: ferramentas MCP inexistentes substituídas por `pull_request_read` + `get_check_runs`; integrado `scripts/pipeline-timing.sh` para cálculo de métricas de tempo | Correção de implementação |
| 2026-04-02 | Adicionado: tratamento de indisponibilidade de ferramentas MCP (retry 3x, manter pending, retomar ao reconectar) e dependências explícitas entre passos 10→10.1→11 | Análise de causa raiz — omissões de pipeline |
| 2026-04-02 | Adicionado: item 4 no trigger de revisão automática — atualizar descrição do PR após conclusão do auto-pr-review para refletir correções realizadas | Análise de omissões pós-review |
| 2026-04-02 | Corrigido: seção MCP — ferramentas carregam automaticamente (inicialização assíncrona); protocolo de retry substituiu declaração prematura de indisponibilidade | Diagnóstico de MCP — Erro 12 |
