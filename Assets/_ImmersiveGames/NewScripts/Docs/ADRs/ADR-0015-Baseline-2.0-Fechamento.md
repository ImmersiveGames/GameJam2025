# ADR-0015 — Baseline 2.0: Fechamento Operacional

## Status
- Estado: Implementado
- Data: 2026-01-05
- Escopo: NewScripts / Baseline 2.0

## Contexto

O Baseline 2.0 existe para garantir um **contrato mínimo e verificável** do pipeline de produção:
SceneFlow → ScenesReady → WorldLifecycleResetCompleted → Gate → FadeOut/Completed. Esse contrato
consolidou ordem de eventos, tokens de gate, assinatura de logs e invariantes HARD para evitar
regressões em transições e resets determinísticos.

Após estabilização e validação checklist-driven, faz sentido declarar o Baseline 2.0 como
**FECHADO e OPERACIONAL**, preservando a spec congelada e o histórico já registrado.

## Decisão

Declarar o **Baseline 2.0 FECHADO/OPERACIONAL** em **2026-01-05**.
A **spec permanece congelada**; qualquer mudança que altere assinaturas, ordem, tokens ou
invariantes deverá gerar nova versão (ex.: Baseline 2.1) com atualização explícita de spec
+ checklist + ADR.

## Fora de escopo

- Melhorias futuras de tooling/parser/regex do checklist-driven.
- Expansões do baseline para novos cenários além de A–E.
- Ajustes cosméticos de logs que não violem invariantes HARD.

## Consequências

### Benefícios

- Novas features **não podem alterar** assinaturas, ordem do pipeline ou razões canônicas sem
  atualizar **spec + checklist + ADR** (nova versão do baseline).
- Mudanças que toquem o contrato do Baseline 2.0 devem ser tratadas como **Baseline 2.1** ou
  novo ADR substitutivo, evitando regressões silenciosas.

### Trade-offs / Riscos

- (não informado)

## Notas de implementação

### Evidências (fechamento)

O fechamento do Baseline 2.0 foi validado via **snapshot datado** (evidência canônica para ADRs aceitos).

- Snapshot (2026-01-18): [`Baseline 2.1 — Evidência consolidada`](../Reports/Evidence/2026-01-18/Baseline-2.1-Evidence-2026-01-18.md)
- Log (snapshot): [`Logs/Baseline-2.1-Smoke-2026-01-18.log`](../Reports/Evidence/2026-01-18/Logs/Baseline-2.1-Smoke-2026-01-18.log)
- Verificação (snapshot): [`Baseline-2.1-ContractVerification-2026-01-18.md`](../Reports/Evidence/2026-01-18/Verifications/Baseline-2.1-ContractVerification-2026-01-18.md)

Observação: artefatos antigos do Baseline 2.0 (spec/checklist/smoke) foram removidos de `Reports/` para reduzir ruído; o snapshot datado permanece como evidência histórica.

### Escopo do “fechado” (A–E, checklist-driven)

O fechamento cobre:
- **A–E do checklist** (startup → menu, menu → gameplay, pause → resume, postgame victory → exit,
  postgame defeat → restart).
- **Invariantes HARD** do pipeline (ordem de eventos, reset antes de FadeOut, tokens balanceados,
  razões canônicas de reset/skip).
- **Assinaturas-chave** e motivos (`reason`) conforme contrato de observabilidade, validados pelo snapshot datado.

### Próximos passos pós-fechamento (fora do baseline)

- Operacionalizar o **ResetWorld trigger de produção** como iniciativa paralela.
- Itens do plano macro (melhorias de tooling/parser, extensões do checklist) seguem como backlog
  fora do Baseline 2.0.

## Evidências

- Metodologia: [`Reports/Evidence/README.md`](../Reports/Evidence/README.md)
- Evidência canônica (LATEST): [`Reports/Evidence/LATEST.md`](../Reports/Evidence/LATEST.md)
- Snapshot (2026-01-18): [`Baseline-2.1-Evidence-2026-01-18.md`](../Reports/Evidence/2026-01-18/Baseline-2.1-Evidence-2026-01-18.md)
- Contrato: [`Observability-Contract.md`](../Reports/Observability-Contract.md)

## Referências

- [Docs/README.md](../README.md)
- [Docs/ARCHITECTURE.md](../ARCHITECTURE.md)
- [Docs/WORLD_LIFECYCLE.md](../WORLD_LIFECYCLE.md)
