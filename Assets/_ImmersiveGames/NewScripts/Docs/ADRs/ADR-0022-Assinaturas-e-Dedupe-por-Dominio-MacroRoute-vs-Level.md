# ADR-0022 - Assinaturas e Dedupe por Dominio (MacroRoute vs Level)

## Status

- Estado: **Aceito (Implementado)**
- Data (decisao): 2026-02-19
- Ultima atualizacao: 2026-03-05
- Tipo: Implementacao
- Escopo: NewScripts/Modules (SceneFlow, LevelFlow, WorldLifecycle)

## Resumo

Separar assinatura e dedupe por dominio:

- **Macro / SceneFlow**: assinatura de transicao de cena (`SceneTransitionContext.ContextSignature`).
- **Level / LevelFlow**: assinatura de selecao de level (`LevelContextSignature`).

## Decisao

1. A assinatura canonica do macro e `SceneTransitionContext.ContextSignature`.
2. A assinatura canonica do level e `LevelContextSignature`.
3. O dedupe macro ocorre em duas camadas:
   - `SceneTransitionService.ShouldDedupe(...)`.
   - `WorldLifecycleSceneFlowResetDriver.ShouldSkipDuplicate(...)`.
4. Dedupe de level e por `SelectionVersion` monotonico (`LevelStageOrchestrator`).

## Implementacao atual (fonte de verdade: codigo)

### Assinatura macro (SceneFlow)

- `SceneTransitionContext` expoe `ContextSignature` e calcula assinatura em `ComputeSignature(...)`.
- `SceneTransitionSignature.Compute(...)` retorna `context.ContextSignature`.

### Assinatura level (LevelFlow)

- `LevelContextSignature.Create(...)` monta `level:{levelId}|route:{routeId}|content:{contentId}|reason:{reason}`.
- `LevelFlowRuntimeService` e `LevelSwapLocalService` publicam `LevelSelectedEvent` com `selectionVersion` + `levelSignature`.

### Dedupe macro

- `SceneTransitionService.ShouldDedupe(...)` evita start duplicado em janela curta.
- `WorldLifecycleSceneFlowResetDriver.ShouldSkipDuplicate(...)` evita reset duplicado em in-flight/recent.

### Dedupe level

- `LevelStageOrchestrator` mantem `_lastProcessedSelectionVersion` e ignora versao menor/igual.
- `LevelSwapLocalService` e `LevelFlowRuntimeService` incrementam `selectionVersion`.

## Criterios de aceite (DoD)

- [x] Distincao explicita entre assinatura macro e assinatura de level.
- [x] Dedupe macro em `SceneTransitionService` + `WorldLifecycleSceneFlowResetDriver`.
- [x] Dedupe level por `SelectionVersion` no orquestrador de stages.
- [ ] Hardening: testes automatizados para colisao controlada.

## Changelog

- 2026-03-05: revisado com base nas auditorias de 2026-03-04/2026-03-05 e no codigo atual.
- 2026-03-04: ADR auditado contra o codigo; implementacao migrada para evidencias de classes/metodos.
