# Regra: Consulta Pré-Planejamento Obrigatória

## Classificação

Meta-governança (ponte entre domínio técnico e de negócio). Aplica-se tanto a tarefas de código quanto de governança, exigindo resolução de dúvidas antes de qualquer planejamento ou execução.

## Propósito

Esta rule define a política de consulta e resolução de dúvidas obrigatória antes de qualquer planejamento, execução de código ou alteração de governança. O workflow procedural de resolução é executado pela skill `resolve-ambiguity`.

---

## Princípio Fundamental

> Nenhuma implementação ou alteração de governança começa com dúvidas pendentes resolúveis.
> Dúvidas resolúveis por consulta a documentação não devem ser apresentadas ao usuário.
> Somente pontos verdadeiramente abertos justificam interação com o usuário.
> Fluxos e cenários não mapeados devem ser identificados antes de prosseguir.

---

## Políticas

### Gate Pré-Planejamento

Antes de iniciar planejamento, execução de código ou proposta/revisão/alteração de governança, o assistente deve:

1. Verificar `open-questions.md` por dúvidas abertas relevantes à tarefa
2. Identificar definições pendentes nos arquivos de governança pertinentes (regras de negócio, modelo de domínio, invariantes, glossário)
3. Mapear cenários não cobertos pela governança ou regras existentes
4. Mapear fluxos de negócio não documentados em `Instructions/business/workflows.md` que a tarefa pressupõe ou afeta
5. Compilar a lista completa de lacunas identificadas

### Consulta de Documentação para Integrações

Quando a tarefa envolver integração com serviços, APIs ou bibliotecas externas:
- Consultar documentação online atualizada antes de formular perguntas ao usuário
- Basear a análise em informação atualizada, não em suposições genéricas
- Resolver autonomamente as dúvidas que a documentação responder
- Remover da lista de pendências as dúvidas resolvidas por documentação

### Apresentação Filtrada ao Usuário

- Apresentar ao usuário APENAS os pontos que permanecem verdadeiramente abertos após consulta autônoma
- Cada ponto apresentado deve incluir: contexto, por que importa, e o que já foi consultado sem sucesso
- Não apresentar dúvidas que podem ser resolvidas com consulta adicional

### Iteração até Resolução

- Se o usuário responder parcialmente, atualizar a lista e re-consultar documentação se necessário
- Repetir o ciclo de consulta + apresentação até que nenhuma pendência reste
- Registrar resoluções nos arquivos definitivos conforme `governance-policies.md` §4

### Adaptações de Governança

- Se código novo exigir adaptações em rules, skills, hooks ou CLAUDE.md, identificar as necessidades
- Apresentar sugestões objetivas ao usuário antes de implementar adaptações
- As sugestões devem referenciar os arquivos específicos e as seções a alterar

### Gate de Prosseguimento

- Só prosseguir com codificação ou alterações de governança quando:
  - Todas as dúvidas identificadas estiverem resolvidas
  - Necessidades de adaptação de governança estiverem identificadas e aprovadas
- Se restarem dúvidas irresolvíveis (usuário indisponível, informação inexistente), aplicar a política de ambiguidades de `governance-policies.md` §4 (premissa conservadora para não-bloqueantes, registro e espera para bloqueantes)

---

## Relação com Comportamentos #2 e #3

### Complemento ao Comportamento #2

O comportamento #2 ("ler governança relevante antes de implementar") é ampliado: além de ler, verificar ativamente se há lacunas, definições pendentes, cenários ou fluxos não mapeados na governança lida.

### Relação com o Comportamento #3

O comportamento #3 ("verificar ambiguidades, prosseguir com premissa conservadora") permanece como mecanismo de fallback. Este comportamento #12 cria um gate anterior: dúvidas devem ser resolvidas iterativamente antes de recorrer a premissas conservadoras. O comportamento #3 aplica-se apenas a dúvidas que sobrevivam ao processo de consulta e iteração de #12 sem resolução possível.

---

## Enforcement

O hook `.claude/hooks/pre-planning-gate.sh` é acionado automaticamente antes de Edit/Write (via `PreToolUse` em `.claude/settings.json`). Verifica se a consulta pré-planejamento foi registrada na sessão atual via arquivo de estado `.claude/.pre-planning-done`. Se não foi, emite lembrete.

O arquivo `.claude/.pre-planning-done` é transiente (não versionado) e deve ser criado pelo assistente após completar o ciclo de consulta pré-planejamento, sinalizando que o gate foi satisfeito para a sessão.

---

## Relação com Outras Rules

- `governance-policies.md` §4 — política de ambiguidades (fallback quando #12 não resolve)
- `source-of-truth-priority.md` — hierarquia usada para resolver conflitos encontrados na fase de consulta
- `instruction-review.md` — alterações nesta rule ativam revisão via REVIEW.md
- `governance-behavior-tracking.md` — este comportamento deve ser rastreado na lista de comportamentos esperados

---

## Histórico de Mudanças

| Data | Mudança | Referência |
|---|---|---|
| 2026-03-31 | Criado: política de consulta pré-planejamento obrigatória com gate iterativo, consulta de documentação para integrações e enforcement via hook | Instrução do usuário |
