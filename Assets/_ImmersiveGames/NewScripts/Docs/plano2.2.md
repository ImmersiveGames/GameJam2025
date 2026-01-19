# Plano 2.2 — Execução (implementação + QA objetivo)

## Progresso (atualizado em 2026-01-18)

- **A1 / G-01**: PASS (log mostra PhaseChangeRequested inplace + pending + reset + commit; sem [Fade] e sem [LoadingHUD]).
- **A2 / G-02**: Docs atualizados (Observability Contract vira fonte de verdade; Reason-Map deprecated).
- **A3 / G-03**: Docs atualizados (README/índice ADRs e atalhos para snapshot 2026-01-18).

Próximo passo de implementação: **Linha B / B1 (Catálogo + Resolver)**.

---

﻿# Plano 2.2 — Execução (implementação + QA objetivo)

> Este arquivo complementa `plano2.2.md`: aqui a ênfase é **execução** (ordem, entregáveis e QA mínimo para evidência), sem discutir arquitetura novamente.

## Pré-condição
- Baseline 2.1 fechado via snapshot datado (2026-01-18) e ponte `LATEST.md` válida.

## Meta
- Implementar a evolução 2.2 de forma incremental, mantendo regressão controlada.

## Linha de trabalho A — Fechar gates (ADR-0018)

### A1) G-01 — In-Place “sem visuais” como padrão de evidência
**Código**
- Garantir que In-Place ignore Loading HUD (mesmo se options.UseLoadingHud=true).
- QA de In-Place deve chamar `RequestPhaseInPlaceAsync(..., options: null)`.

**QA (Context Menu, Play Mode)**
- `QA/Phases/InPlace/Commit (NoVisuals)`

**Evidência**
- Console log contendo:
    - `PhaseChangeRequested ... inplace`
    - `PhasePendingSet` (ou equivalente)
    - reset solicitado/concluído
    - `PhaseCommitted`
    - ausência de `[Fade]` e `[LoadingHUD]` no intervalo.

### A2) G-02 — Observability (reasons + links)
**Docs**
- Definir regra oficial (1 fonte de verdade) para reason:
    - driver/controller;
    - `WorldLifecycleResetCompletedEvent`.
- Remover/corrigir referência a `Reason-Map.md` se não existir, ou criar o arquivo.

**QA**
- Regerar snapshot datado e validar anchors.

### A3) G-03 — Docs sem drift
**Docs**
- Corrigir links em `ARCHITECTURE.md` e `README.md`.

## Linha de trabalho B — WorldCycle (config-driven) incremental

> Executar apenas após G-01..G-03 (reduz risco de regressões “por ambiguidade”).

### B1) Marco 1 — Catálogo + Resolver (sem Apply)
**Entregas**
- ScriptableObjects:
    - `WorldCycleDefinition`
    - `PhaseDefinition`
- Serviços (interfaces + implementação mínima):
    - `IWorldCycleCatalog`
    - `IPhaseDefinitionResolver`

**QA objetivo**
- Logs/anchors mostrando `phaseId` + `contentSignature` vindo do resolver.

### B2) Marco 2 — Commit canônico
**Entregas**
- WithTransition: consumir intent → pending → reset → commit em `ScenesReady`.
- In-Place: commit após reset (garantia).

**QA objetivo**
- Ações de Context Menu:
    - `QA/Phases/InPlace/Commit (NoVisuals)`
    - `QA/Phases/WithTransition/Commit (Gameplay Minimal)`

### B3) Marco 3 — Apply de WorldDefinition por fase
**Entregas**
- `IPhaseWorldConfigurationApplier` reconstruindo spawn registry antes do reset.
- Fallback seguro quando `worldDefinition == null`.

**QA objetivo**
- Anchor demonstrando spawn consistente com a fase.

### B4) Marco 4 — IntroStage config-driven
**Entregas**
- `IIntroStagePolicyResolver` baseado em `PhaseDefinition.introStagePolicy`.

**QA objetivo**
- Anchor mostrando policy aplicada e não-bloqueio preservado.

## Checklist de saída (para promoção)
- Gates do ADR-0018 PASS.
- Snapshot datado gerado e verificação curada atualizada.
- `LATEST.md` apontando para o snapshot.
- `CHANGELOG-docs.md` registrando a promoção.
