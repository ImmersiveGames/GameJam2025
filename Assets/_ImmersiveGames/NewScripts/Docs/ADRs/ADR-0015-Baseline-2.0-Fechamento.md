# ADR-0015 — Baseline 2.0: Fechamento Operacional

- Date: 2026-01-05
- Status: Accepted (Closed / Operational)
- Scope: NewScripts / Baseline 2.0

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

## Evidências (fechamento)

- **Spec frozen**: `Docs/Reports/Baseline-2.0-Spec.md`.
- **Checklist operacional** (seção A–E): `Docs/Reports/Baseline-2.0-Checklist.md`.
- **Checklist-driven verification (Pass)**:
  `Docs/Reports/Baseline-2.0-ChecklistVerification-LastRun.md`.
  - Status: **Pass**
  - Blocks: 5 | Pass: 5 | Fail: 0
  - Log lines: 804 | Evidence: 20
  - Todos os blocos estão **Pass** (A–E).
- **Log canônico**: `Docs/Reports/Baseline-2.0-Smoke-LastRun.log`.

## Escopo do “fechado” (A–E, checklist-driven)

O fechamento cobre:
- **A–E do checklist** (startup → menu, menu → gameplay, pause → resume, postgame victory → exit,
  postgame defeat → restart).
- **Invariantes HARD** do pipeline (ordem de eventos, reset antes de FadeOut, tokens balanceados,
  razões canônicas de reset/skip).
- **Assinaturas-chave** e motivos (`reason`) descritos na spec congelada.

## Fora do escopo do fechamento

- Melhorias futuras de tooling/parser/regex do checklist-driven.
- Expansões do baseline para novos cenários além de A–E.
- Ajustes cosméticos de logs que não violem invariantes HARD.

## Consequências

- Novas features **não podem alterar** assinaturas, ordem do pipeline ou razões canônicas sem
  atualizar **spec + checklist + ADR** (nova versão do baseline).
- Mudanças que toquem o contrato do Baseline 2.0 devem ser tratadas como **Baseline 2.1** ou
  novo ADR substitutivo, evitando regressões silenciosas.

## Próximos passos pós-fechamento (fora do baseline)

- Operacionalizar o **ResetWorld trigger de produção** como iniciativa paralela.
- Itens do plano macro (melhorias de tooling/parser, extensões do checklist) seguem como backlog
  fora do Baseline 2.0.

## Referências

- `Docs/Reports/Baseline-2.0-Spec.md` (spec congelada)
- `Docs/Reports/Baseline-2.0-Checklist.md` (checklist A–E)
- `Docs/Reports/Baseline-2.0-ChecklistVerification-LastRun.md` (evidência Pass)
- `Docs/Reports/Baseline-2.0-Smoke-LastRun.log` (log canônico)
- `Docs/README.md` / `Docs/ARCHITECTURE.md` / `Docs/WORLD_LIFECYCLE.md` (status e contrato)
