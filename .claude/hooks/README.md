# Hooks do Claude Code

## Propósito

Esta pasta contém os hooks de enforcement do Claude Code para este repositório. Hooks são scripts executados automaticamente antes ou depois de operações de ferramentas.

---

## Hooks Ativos

| Hook | Tipo | Matcher | Propósito |
|---|---|---|---|
| `instruction-change-detector.sh` | PostToolUse | Write\|Edit | Detecta mudanças em arquivos de governança e emite lembrete de revisão via REVIEW.md. A auditoria (`scripts/governance-audit.sh`) é executada no passo 0.1 do pipeline pré-commit, não por este hook. |
| `pre-commit-gate.sh` | Manual | — | Gate de validação: dotnet build + dotnet test antes de commit; paths resolvidos dinamicamente |
| `branch-guard.sh` | PostToolUse | Bash | Detecta operações de branch incorretas durante pr-analysis; emite alerta se o branch não for o head.ref esperado |
| `session-timer.sh` | PostToolUse | Bash | Auto-inicializa e rastreia tempo efetivo da sessão; cria `.claude/.session-timer` na primeira invocação; detecta segmentos de trabalho; exibe tempo acumulado periodicamente; informativo, nunca bloqueante |
| `pre-planning-gate.sh` | PreToolUse | Edit\|Write | Verifica se a consulta pré-planejamento foi executada na sessão; emite lembrete do comportamento #12 se não; usa `.claude/.pre-planning-done` como estado; informativo |
| `post-commit-pr-reminder.sh` | PostToolUse | Bash | Detecta `git commit`/`git push` e emite lembrete para executar passo 10 (criar/atualizar PR); informativo, nunca bloqueante |
| `session-start.sh` | SessionStart | — | Limpa estado stale, injeta contexto de branch, verifica variáveis de ambiente críticas; informativo |
| `stop-verification.sh` | Stop | — | Verifica consulta pré-planejamento e mudanças não commitadas; emite lembretes de governança; informativo |
| `bash-error-capture.sh` | PostToolUseFailure | Bash | Captura automaticamente erros de bash e registra entrada estruturada em `bash-errors-log.md`; informativo |
| `compact-context.sh` | PreCompact/PostCompact | — | Salva e restaura estado do pipeline durante compactação de contexto; garante continuidade de rastreamento |

---

## Configuração

Os hooks são configurados em `.claude/settings.json` na seção `hooks`:
- **SessionStart**: `session-start.sh` — inicialização de sessão
- **PreToolUse**: `pre-planning-gate.sh` — gate pré-planejamento (Edit|Write); bloqueio inline de `rm -rf` e `git push --force` via `if:` com exit 2
- **PostToolUse**: `instruction-change-detector.sh` (Write|Edit), `branch-guard.sh` + `session-timer.sh` + `post-commit-pr-reminder.sh` (Bash), hook `type: prompt` para mensagens de commit (Bash com filtro `git commit*`)
- **PostToolUseFailure**: `bash-error-capture.sh` — captura automática de erros (Bash)
- **PreCompact/PostCompact**: `compact-context.sh` — preservação de estado durante compactação
- **Stop**: `stop-verification.sh` — verificação final de governança
- **Manual**: `pre-commit-gate.sh` — gate de build/test no pipeline pré-commit

---

## Relação com Governança

- `instruction-change-detector.sh` → ativa `.claude/rules/instruction-review.md` → emite lembrete para executar `REVIEW.md`; a auditoria é executada no passo 0.1 do pipeline pré-commit
- `pre-commit-gate.sh` → implementa parte do pipeline de validação pré-commit definido em `CLAUDE.md`
- `branch-guard.sh` → protege o branch correto durante pr-analysis; usa `.claude/.pr-analysis-context` como contexto; arquivo criado pela skill pr-analysis
- `session-timer.sh` → implementa `.claude/rules/execution-time-tracking.md` → auto-inicializa, rastreia e exibe tempo efetivo acumulado; usa `.claude/.session-timer` como estado
- `pre-planning-gate.sh` → implementa enforcement do comportamento #12 de `.claude/rules/pre-planning-consultation.md` → verifica consulta pré-planejamento antes de edições; usa `.claude/.pre-planning-done` como estado
- `post-commit-pr-reminder.sh` → implementa enforcement do passo 10 de `.claude/rules/pr-metadata-governance.md` → lembra criação de PR após commit/push
- `session-start.sh` → limpa estado stale + injeta contexto de branch + verifica variáveis críticas ao iniciar sessão
- `stop-verification.sh` → verifica cumprimento de comportamentos de governança ao encerrar resposta
- `bash-error-capture.sh` → automatiza `.claude/rules/bash-error-logging.md` → registra erros automaticamente
- `compact-context.sh` → preserva estado do pipeline (branch, escopo, pré-planejamento) durante compactação de contexto

---

## Histórico de Mudanças

| Data | Mudança | Referência |
|---|---|---|
| 2026-03-18 | Criado: hooks reais substituindo placeholders | Reestruturação de governança |
| 2026-03-20 | Adicionado: branch-guard.sh para proteção de branch durante pr-analysis | Correção de workflow de PR |
| 2026-03-21 | Atualizado: documentação do instruction-change-detector.sh — emite lembrete mas não executa auditoria diretamente | Auditoria de governança |
| 2026-03-21 | Corrigido: branch-guard.sh criado (estava configurado mas inexistente); pre-commit-gate.sh refatorado com paths dinâmicos (paths hardcoded estavam obsoletos) | Análise de causas-raiz |
| 2026-03-30 | Adicionado: post-commit-pr-reminder.sh — enforcement informativo do passo 10 (criação de PR) após git commit/push | Verificação de conformidade de governança |
| 2026-03-31 | Adicionado: pre-planning-gate.sh — enforcement do comportamento #12 (consulta pré-planejamento) via PreToolUse | Instrução do usuário |
| 2026-04-01 | Adicionado: session-start.sh (SessionStart), stop-verification.sh (Stop), bash-error-capture.sh (PostToolUseFailure), compact-context.sh (PreCompact/PostCompact) — novos tipos de hook para automação de governança | Melhoria de governança com recursos avançados do Claude Code |
| 2026-04-01 | Adicionado: hook type:prompt para validação semântica de mensagens de commit (PostToolUse Bash com filtro git commit*) | Melhoria de governança |
| 2026-04-01 | Atualizado: settings.json com autoMemoryEnabled, env, limpeza de permissões redundantes, 5 novos event types | Melhoria de governança |
