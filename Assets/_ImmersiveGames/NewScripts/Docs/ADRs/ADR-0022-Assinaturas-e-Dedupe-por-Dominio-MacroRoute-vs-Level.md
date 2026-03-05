# ADR-0022 — Assinaturas e Dedupe por Domínio (MacroRoute vs Level)

## Status

- Estado: **Aceito (Implementado)**
- Data (decisão): 2026-02-19
- Última atualização: 2026-03-04
- Tipo: Implementação
- Escopo: NewScripts/Modules (SceneFlow, LevelFlow, WorldLifecycle)

## Resumo

Separar assinatura e dedupe por domínio:

- **Macro / SceneFlow**: assinatura de transição de cena (`SceneTransitionContext.ContextSignature`).
- **Level / LevelFlow**: assinatura de seleção de level (`LevelContextSignature`).

## Decisão

1. A assinatura canônica do macro é `SceneTransitionContext.ContextSignature` (calculada por `ComputeSignature` quando não vem da request).
2. A assinatura canônica do level é `LevelContextSignature`.
3. O dedupe macro ocorre em duas camadas:
   - `SceneTransitionService.ShouldDedupe(...)` (dedupe de `TransitionAsync`).
   - `WorldLifecycleSceneFlowResetDriver.ShouldSkipDuplicate(...)` (dedupe de reset por assinatura no ScenesReady).
4. Dedupe de level é por `SelectionVersion` monotônico e guardas do fluxo de stage (`LevelStageOrchestrator`).

## Implementação atual (fonte de verdade: código)

### Assinatura macro (SceneFlow)

- `SceneTransitionContext` expõe `ContextSignature` e calcula assinatura com `route/style/profile/profileAsset/active/fade/load/unload` em `ComputeSignature(...)`.
- `SceneTransitionSignature.Compute(...)` retorna explicitamente `context.ContextSignature` como assinatura canônica.

### Assinatura level (LevelFlow)

- `LevelContextSignature.Create(...)` monta `level:{levelId}|route:{routeId}|content:{contentId}|reason:{reason}`.
- `LevelFlowRuntimeService` e `LevelSwapLocalService` publicam `LevelSelectedEvent` com `selectionVersion` + `levelSignature`.

### Dedupe macro

- `SceneTransitionService.ShouldDedupe(...)` evita start duplicado em janela curta, comparando assinatura com último start/completed.
- `WorldLifecycleSceneFlowResetDriver.ShouldSkipDuplicate(...)` evita reset duplicado quando assinatura está in-flight ou recém concluída.

### Dedupe level

- `LevelStageOrchestrator` mantém `_lastProcessedSelectionVersion` e ignora eventos/snapshots com versão menor/igual.
- `LevelSwapLocalService` e `LevelFlowRuntimeService` incrementam `selectionVersion` a partir do snapshot atual.

## Critérios de aceite (DoD)

- [x] Existe distinção explícita entre assinatura macro e assinatura de level.
- [x] Dedupe macro implementado em `SceneTransitionService` + `WorldLifecycleSceneFlowResetDriver`.
- [x] Dedupe de level baseado em `SelectionVersion` no orquestrador de stages.
- [ ] Hardening: testes automatizados para colisão controlada (mesma macro signature com level signatures diferentes).

## Changelog

- 2026-03-04: ADR auditado contra o código; seção de implementação migrada para evidências de classes/métodos.
