# Regra: Pipeline de Validação de Governança

## Classificação

Meta-governança (ponte entre domínio técnico e de negócio). Valida que mudanças em arquivos de governança são efetivamente aplicadas pelo assistente em sessões reais, cobrindo tanto novos comportamentos quanto regressão de comportamentos existentes.

## Propósito

Esta rule define a política de validação funcional obrigatória de mudanças em arquivos de governança. O workflow procedural está em `.claude/skills/governance-validation-pipeline/SKILL.md`.

---

## Princípio Fundamental

> Governança que não é validada funcionalmente não é confiável.
> Auditoria estrutural (imports, contagens, referências) não substitui validação funcional.
> A única forma de confirmar que um comportamento de governança é efetivamente aplicado é testá-lo em uma sessão real via subagente.

---

## Políticas

### Quando Ativar

Esta validação é executada no **passo 9.1** do pipeline pré-commit, após o commit (passo 9) e antes da criação/atualização do PR (passo 10). É ativada **apenas quando a tarefa altera aspectos que afetam o pipeline de codificação**: passos do pipeline pré-commit (0–12), comportamentos obrigatórios (1–13), skills de pipeline, rules que governam o fluxo de codificação, ou hooks de enforcement. Mudanças puramente documentais (glossário, wiki, ADRs, regras de negócio sem impacto no pipeline) **não ativam** este passo.

### Mecanismo de Validação

A validação utiliza dois subagentes executando o mesmo comando de teste:

| Subagente | Branch | Isolamento | Propósito |
|---|---|---|---|
| Subagente dev | Branch de desenvolvimento (atual) | Nenhum — roda na branch atual | Validar que o novo comportamento é aplicado + regressão de comportamentos existentes |
| Subagente main | Branch `main` | `isolation: "worktree"` — cópia temporária | Teste regressivo: comparar com comportamento da branch principal |

Ambos os subagentes são lançados **em paralelo** com o mesmo comando. A comparação de resultados é feita quando ambos terminam.

### Comando Idêntico

O prompt enviado a ambos os subagentes deve ser **textualmente idêntico**. Isso garante que:
- Os mesmos cenários de teste são executados em ambas as branches
- O input é consistente para comparação de output
- Diferenças no resultado refletem exclusivamente diferenças na governança

### Seleção Automática de Cenário de Teste

O assistente seleciona automaticamente o cenário de teste com base na natureza da mudança de governança:

| Cenário | Quando Usar | Exemplo |
|---|---|---|
| Endpoint simples sem recursos externos | A mudança não requer consumo de API externa, banco ou Datadog | "Crie um endpoint GET de teste: se receber 1, retorna 'Um'; se receber 2, retorna 'Dois'" |
| Endpoint conforme governança mais recente | O cenário simples não atende aos critérios da governança mais recente | Comando adaptado que atenda aos requisitos mínimos das regras vigentes |
| Consumo de API externa (OpenMeteo) | A mudança envolve integrações HTTP externas ou resiliência | "Implemente consumo da API OpenMeteo (https://open-meteo.com) para clima e temperatura de São Paulo" |
| Apenas planejamento (sem codificação) | A validação pode ser feita verificando o plano de execução | "Considere a criação de um endpoint X — NÃO codifique, apenas elabore o plano" |

A escolha do cenário é reportada ao usuário no início da validação.

### Gate Bloqueante (Branch de Desenvolvimento)

A validação na branch de desenvolvimento é um **gate bloqueante**:
- Se o novo comportamento de governança **não for aplicado** pelo subagente → falha
- O assistente deve diagnosticar a causa raiz, corrigir os arquivos de governança, commitar e relançar o subagente
- **Máximo de 3 tentativas**; após 3 falhas consecutivas → escalar ao usuário via AskUserQuestion
- A falha bloqueia o avanço para o passo 10 (PR)

### Regressão com Branch Main (Não-Bloqueante)

A comparação com a branch main é **não-bloqueante**:
- Diferenças **esperadas**: novo comportamento presente no dev, ausente no main → OK
- Diferenças **inesperadas**: passo presente no main, ausente no dev → ALERTA (reportado no relatório final)
- Os resultados comparativos são incluídos no relatório final e na descrição do PR

### Relatório Obrigatório

Ao final da validação, o assistente deve emitir relatório contendo:
- Status geral: APROVADO / REPROVADO
- Resultado da validação na branch dev (novo comportamento aplicado? regressão detectada?)
- Resultado da regressão na branch main (diferenças esperadas e inesperadas)
- Número de tentativas executadas
- Ações corretivas realizadas (se houver)

---

## Relação com Outras Rules

- `governance-behavior-tracking.md` — o passo 9.1 é um comportamento rastreado; esta rule complementa com validação funcional
- `governance-audit.md` — a auditoria verifica consistência estrutural; esta rule verifica eficácia funcional
- `pr-metadata-governance.md` — o relatório de validação é incluído na descrição do PR
- `bash-error-logging.md` — falhas durante a validação devem ser registradas

---

## Histórico de Mudanças

| Data | Mudança | Referência |
|---|---|---|
| 2026-04-01 | Criado: política de validação funcional de governança via subagentes | Instrução do usuário |
