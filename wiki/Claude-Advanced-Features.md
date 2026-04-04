# Claude — Recursos Avançados de Governança

## Descrição

Documenta todos os recursos avançados do Claude Code implementados na governança deste repositório: frontmatter em rules e skills, hooks de ciclo de vida, agentes dedicados, contexto dinâmico, proteção contra comandos destrutivos e carregamento de ferramentas MCP. Deve ser consultada para entender como cada mecanismo funciona e qual benefício traz.

## Contexto

O Claude Code oferece dezenas de recursos avançados para configuração de projetos. Este repositório implementa um subconjunto selecionado — priorizado por impacto (redução de contexto, automação, segurança) e risco (zero quebra de funcionalidade existente). As melhorias estão organizadas em 10 categorias, cada uma com explicação de funcionamento e benefício.

---

## 1. Frontmatter `paths:` em Rules (Lazy-Loading Contextual)

### O que é

O Claude Code carrega rules de `.claude/rules/` no contexto da conversa. Por padrão, **todas** as rules são carregadas em toda sessão, consumindo tokens do context window.

O frontmatter `paths:` é um mecanismo do Claude Code que instrui o sistema a carregar a rule **apenas quando o assistente acessar arquivos que casem com os padrões glob especificados**.

### Como funciona

```yaml
---
paths:
  - "**/*.cs"
  - "**/Endpoint*/**"
---
# Regra: Validação de Endpoint...
```

Quando o Claude Code inicia uma sessão:
- Rules **sem** `paths:` → carregam imediatamente no contexto (sempre presentes)
- Rules **com** `paths:` → ficam dormentes até que o assistente leia/edite um arquivo que case com os padrões

No exemplo acima, a rule `endpoint-validation.md` só carrega quando o assistente toca em arquivos `.cs` ou em pastas `Endpoint*/`. Em sessões de governança pura (só `.md`), ela nunca carrega.

### Benefício

- **Redução de ~630 linhas de contexto** em sessões que não tocam código
- Context window mais limpo = respostas mais focadas e menos risco de confusão entre rules
- 6 rules ficam lazy-loaded; 10 rules globais (meta-governança) continuam sempre presentes

### Rules com `paths:` e seus padrões

| Rule | Carrega quando toca em... |
|------|--------------------------|
| `endpoint-validation.md` | `**/*.cs`, `**/Endpoint*/**` |
| `folder-governance.md` | `**/*.cs`, `**/*.csproj` |
| `naming-governance.md` | `**/*.cs`, `**/*.csproj` |
| `environment-readiness.md` | `**/Dockerfile`, `**/docker-compose*.yml`, `**/*.csproj`, `**/appsettings*.json` |
| `bash-error-logging.md` | `bash-errors-log.md` |
| `execution-time-tracking.md` | `**/.session-timer` |

---

## 2. Frontmatter em Skills (Auto-Ativação, Isolamento e Menor Privilégio)

### O que é

Skills (`.claude/skills/*/SKILL.md`) são workflows procedurais. O frontmatter YAML no topo habilita recursos avançados do Claude Code:

| Campo | O que faz |
|-------|-----------|
| `name:` | Identificador único da skill (permite invocação por nome) |
| `description:` | Descrição curta que o Claude Code usa para decidir quando sugerir a skill |
| `paths:` | Auto-ativação: a skill é sugerida quando arquivos correspondentes são tocados |
| `context: fork` | Executa a skill em um **subagente isolado** com context window próprio |
| `allowed-tools:` | Lista branca de ferramentas — tudo fora da lista é bloqueado para essa skill |

### Como funciona — `context: fork`

```yaml
---
name: auto-pr-review
context: fork
allowed-tools:
  - Bash
  - Read
  - mcp__github__*
  - mcp__github-revisor__*
---
```

Quando `context: fork` está ativo, a skill roda em um **subagente com context window separado**. O subagente:
- Recebe o conteúdo da SKILL.md como prompt de tarefa
- Tem acesso apenas às ferramentas listadas em `allowed-tools:`
- Não polui o context window da conversa principal
- Retorna apenas o resultado final

### Como funciona — `allowed-tools:` (Menor Privilégio)

```yaml
allowed-tools:
  - Bash
  - Read
  - Grep
  - WebFetch
```

A skill `validate-endpoints` só precisa executar comandos, ler arquivos e fazer requests HTTP. Não precisa de `Write`, `Edit` nem `Agent`. Se a skill tentar usar uma ferramenta fora da lista, o Claude Code bloqueia.

### Benefício

- **`paths:`** — Skills só aparecem quando relevantes, reduzindo ruído de sugestões
- **`context: fork`** — Skills complexas (review de PR, validação de governança) rodam em isolamento, protegendo o contexto principal
- **`allowed-tools:`** — Princípio de menor privilégio: se uma skill não precisa editar arquivos, ela não pode

### Skills e seus frontmatters

| Skill | `paths:` | `context: fork` | `allowed-tools:` |
|-------|----------|-----------------|-------------------|
| `validate-endpoints` | `**/Endpoint*/**`, `**/*Controller*.cs` | Não | Bash, Read, Grep, WebFetch |
| `verify-environment` | `**/Dockerfile`, `**/docker-compose*.yml`, `**/*.csproj` | Não | Bash, Read, Glob |
| `apply-user-snippet` | `Instructions/snippets/**` | Não | — |
| `auto-pr-review` | — | **Sim** | Bash, Read, Grep, Glob, mcp\_\_github\_\_\*, mcp\_\_github-revisor\_\_\* |
| `governance-validation-pipeline` | — | **Sim** | Bash, Read, Grep, Glob, Agent |
| `evolve-governance` | `.claude/**`, `Instructions/**`, `CLAUDE.md`, `REVIEW.md` | Não | — |
| `review-instructions` | `.claude/**`, `Instructions/**`, `CLAUDE.md`, `REVIEW.md` | Não | — |
| `pr-analysis` | — | **Sim** | Bash, Read, Grep, Glob, Edit, Write, mcp\_\_github\_\_\*, mcp\_\_github-revisor\_\_\* |
| `manage-pr-lifecycle` | — | Não | Bash, Read, Grep, mcp\_\_github\_\_\* |

---

## 3. Novos Hooks (5 Event Types Adicionais)

### O que são hooks

Hooks são scripts que o Claude Code executa automaticamente em momentos específicos do ciclo de vida de uma sessão. O sistema suporta 23+ event types. Antes, o repositório usava apenas 2 (`PreToolUse` e `PostToolUse`). Agora usa 7.

### Hook `SessionStart` → `session-start.sh`

**Quando dispara**: Automaticamente quando uma nova sessão inicia ou é retomada.

**O que faz**:
1. **Limpa estado stale** — Remove arquivos de estado de sessão com mais de 4 horas (`.pre-planning-done`, `.pr-analysis-context`, `.compact-state`)
2. **Injeta contexto de branch** — Imprime branch atual e últimos 3 commits
3. **Verifica variáveis críticas** — Checa se `DD_API_KEY` e `GH_CLAUDE_CODE_MCP_CODIFICADOR` existem
4. **Persiste variáveis via `CLAUDE_ENV_FILE`** — Escreve `CURRENT_BRANCH` e `REPO_ROOT` em um arquivo especial que o Claude Code lê, tornando essas variáveis disponíveis em todos os comandos Bash da sessão

**Benefício**: O assistente já começa a sessão sabendo o branch, os commits recentes e o estado do ambiente — sem precisar rodar `git status` manualmente. Estado stale de sessões anteriores é limpo automaticamente.

### Hook `Stop` → `stop-verification.sh`

**Quando dispara**: Toda vez que o Claude termina de responder.

**O que faz**:
1. Verifica se `.pre-planning-done` existe (consulta pré-planejamento foi feita?)
2. Verifica se há mudanças não commitadas (`git diff`)
3. Emite lembretes se algo ficou pendente

**Benefício**: Funciona como rede de segurança — se o assistente esquecer de completar o pipeline, o hook emite lembrete. Previne encerramento prematuro de tarefas.

### Hook `PostToolUseFailure` → `bash-error-capture.sh`

**Quando dispara**: Automaticamente quando um comando Bash falha.

**O que faz**:
1. Lê o JSON de entrada da ferramenta (contém o comando que falhou)
2. Determina o próximo número de erro no log
3. Anexa uma entrada estruturada ao `bash-errors-log.md`

**Benefício**: Antes, o assistente precisava registrar manualmente cada erro bash (regra `bash-error-logging.md`). Agora o registro é automático — nenhum erro é silenciosamente perdido.

### Hooks `PreCompact`/`PostCompact` → `compact-context.sh`

**Quando dispara**: Antes e depois da compactação de contexto (quando a conversa fica longa demais e o Claude Code comprime mensagens antigas).

**O que faz**:
- **PreCompact (modo `pre`)**: Salva em `.claude/.compact-state`:
  - Branch atual
  - Se a consulta pré-planejamento foi feita
  - Se está em modo pr-analysis e qual é o head_ref do PR
  - Se o session timer existe
- **PostCompact (modo `post`)**: Lê o estado salvo e emite resumo para reinjeção no contexto compactado

**Benefício**: Sem este hook, quando o contexto é compactado, o assistente pode esquecer em que branch está, se já fez a consulta pré-planejamento, ou qual PR está analisando. O hook preserva esse estado crítico.

---

## 4. Hooks `if:` em PreToolUse (Bloqueio Seletivo)

### O que é

O campo `if:` dentro de um hook permite filtrar **quais comandos específicos** disparam aquele hook. Usa a sintaxe de permission rules do Claude Code.

### Como funciona

```json
{
  "matcher": "Bash",
  "hooks": [
    {
      "type": "command",
      "if": "Bash(rm -rf *)",
      "command": "echo 'BLOQUEADO' >&2; exit 2"
    },
    {
      "type": "command",
      "if": "Bash(git push --force*)",
      "command": "echo 'BLOQUEADO' >&2; exit 2"
    }
  ]
}
```

- `matcher: "Bash"` — o grupo inteiro escuta eventos Bash
- `if: "Bash(rm -rf *)"` — **este hook específico** só dispara se o comando começar com `rm -rf`
- `exit 2` — código de saída especial que **bloqueia a execução** (o comando não roda)

Hooks sem `if:` (como `branch-guard.sh`) rodam em **todos** os comandos Bash.

### Benefício

Proteção em tempo real contra comandos destrutivos. Mesmo em modo `bypassPermissions`, estes hooks impedem `rm -rf` e `git push --force` antes de executar. É uma camada de segurança adicional que não existia.

---

## 5. Agentes Dedicados (`.claude/agents/`)

### O que é

Agentes são subagentes especializados com configuração persistente: modelo, ferramentas permitidas, memória por projeto e skills precarregadas. Diferente de subagentes ad-hoc (lançados via `Agent tool`), agentes dedicados têm identidade e aprendem ao longo do tempo.

### Como funciona

```yaml
---
name: code-reviewer
tools: [Read, Grep, Glob, Bash]
disallowedTools: [Write, Edit]
model: sonnet
memory: project
skills: [review-alignment, review-instructions]
---
[Prompt de sistema do agente]
```

Quando o Claude Code invoca `subagent_type: code-reviewer`:
1. Lê o arquivo `.claude/agents/code-reviewer.md`
2. Aplica as restrições de ferramentas (read-only — não pode editar)
3. Carrega as skills `review-alignment` e `review-instructions` no contexto do agente
4. Usa o modelo Sonnet (mais rápido e barato que Opus)
5. Usa `memory: project` — o agente pode ler/escrever em `.claude/agent-memory/code-reviewer/MEMORY.md`, acumulando aprendizados entre sessões

### Agentes criados

| Agente | Propósito | Ferramentas | Restrição |
|--------|-----------|-------------|-----------|
| `code-reviewer` | Verifica código contra governança (Vertical Slice, SRP, nomenclatura, logging SNP-001, AOT) | Read, Grep, Glob, Bash | **Não pode escrever** (Write/Edit bloqueados) |
| `governance-auditor` | Verifica consistência entre artefatos de governança (propagação, duplicação, referências) | Read, Grep, Glob, Bash | **Não pode escrever nem lançar subagentes** (Write/Edit/Agent bloqueados) |

### Benefício

- **Especialização**: Cada agente tem um prompt de sistema focado em uma responsabilidade
- **Segurança**: Agentes read-only não podem acidentalmente modificar código ou governança
- **Memória**: Aprendem padrões do repositório ao longo do tempo via `MEMORY.md`
- **Economia**: Usam Sonnet em vez de Opus, reduzindo custo para tarefas de revisão

---

## 6. Contexto Dinâmico em Skills (`!command`)

### O que é

A sintaxe `` !`comando` `` dentro de um SKILL.md executa o comando **antes** de o Claude ver o prompt da skill. O output do comando substitui o placeholder inline.

### Como funciona

```markdown
## Contexto Dinâmico

- Branch atual: !`git branch --show-current`
- Últimos commits: !`git log --oneline -5`
```

Quando a skill é ativada, o Claude Code:
1. Detecta os blocos `` !`...` ``
2. Executa cada comando no shell
3. Substitui o placeholder pelo output real
4. Envia o resultado final ao Claude

O Claude recebe algo como:

```markdown
## Contexto Dinâmico

- Branch atual: claude/improve-governance-files-BWf3t
- Últimos commits:
  63801f0 docs(governance): adicionar agentes dedicados...
  62a7a37 docs(governance): adicionar recursos avançados...
```

### Skills que usam contexto dinâmico

| Skill | Comandos injetados |
|-------|-------------------|
| `pr-analysis` | `git branch --show-current`, `git log --oneline -5` |
| `manage-pr-lifecycle` | `git branch --show-current`, `git log --oneline -1` |

### Benefício

A skill já chega ao Claude com informação contextual atualizada, sem gastar tokens em chamadas de ferramenta para obter dados básicos como branch e commits. Reduz latência e tokens.

---

## 7. `autoMemoryEnabled: true` em Settings

### O que é

Habilita o sistema de memória automática do Claude Code. O Claude pode criar e consultar notas persistentes em `~/.claude/projects/<projeto>/memory/` entre sessões.

### Benefício

Informações aprendidas em uma sessão (como padrões do repositório, preferências do usuário, erros recorrentes) persistem para sessões futuras sem precisar reler toda a governança.

---

## 8. `env:` em Settings

### O que é

Variáveis de ambiente injetadas automaticamente em toda sessão.

```json
"env": {
  "GOVERNANCE_STRICT_MODE": "true",
  "IDLE_THRESHOLD_MS": "120000"
}
```

### Benefício

Hooks e scripts podem ler estas variáveis para configurar comportamento sem hardcoding. Por exemplo, `IDLE_THRESHOLD_MS` configura o limiar de inatividade do session-timer.

---

## 9. Documentação de Carregamento de Ferramentas MCP

### O problema

Ferramentas MCP do GitHub são **deferred tools** — existem no sistema mas não carregam automaticamente no contexto. Precisam ser carregadas explicitamente via `ToolSearch` com a sintaxe `select:` e nomes exatos.

A busca por keywords (`ToolSearch("mcp github create pull request")`) **nunca funciona** porque o algoritmo de matching não casa keywords genéricas com nomes como `mcp__github__create_pull_request`.

### A correção

Documentada a seção "Como carregar as ferramentas MCP" em 3 skills que dependem de MCP:

```
ToolSearch("select:mcp__github__list_pull_requests,mcp__github__create_pull_request")
```

### Skills com documentação de carregamento MCP

- `manage-pr-lifecycle/SKILL.md`
- `auto-pr-review/SKILL.md`
- `pr-analysis/SKILL.md`

### Benefício

Previne que o assistente conclua erroneamente que as ferramentas MCP "não estão disponíveis" quando na verdade apenas não foram carregadas corretamente.

---

## 10. Limpeza de Permissões Redundantes

### O que era

```json
"permissions": {
  "defaultMode": "bypassPermissions",
  "allow": ["Bash(*)", "Read(**)", "Write(**)", "Edit(**)", "..."],
  "deny": ["Bash(rm -rf /)", "Bash(docker system prune --all)"]
}
```

### O que ficou

```json
"permissions": {
  "defaultMode": "bypassPermissions",
  "deny": ["Bash(rm -rf /)", "Bash(docker system prune --all)"]
}
```

### Benefício

Com `bypassPermissions`, a lista `allow` é redundante — tudo já é permitido por padrão. Manter apenas `deny` torna a configuração mais limpa e a intenção mais clara: "tudo permitido exceto estes comandos destrutivos".

---

## Referências

- [Visão Geral do Claude](Claude-Overview) — Sistema de governança operacional e pipeline
- [Skills](Claude-Skills) — Catálogo completo de skills disponíveis
- [Hooks](Claude-Hooks) — Hooks configurados e comportamentos
- [Convenções e Restrições](Claude-Conventions) — Comportamentos obrigatórios
- [Arquitetura](Governance-Architecture) — Estrutura Vertical Slice e componentes
