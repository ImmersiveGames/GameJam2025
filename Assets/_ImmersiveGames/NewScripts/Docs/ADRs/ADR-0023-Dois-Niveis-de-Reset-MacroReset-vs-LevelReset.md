# ADR-0023 - Dois niveis de reset: MacroReset vs LevelReset

## Status

- Estado: **Aceito (Implementado)**
- Data (decisao): 2026-02-19
- Ultima atualizacao: 2026-03-05
- Tipo: Implementacao
- Escopo: NewScripts/Modules (WorldLifecycle, SceneFlow, LevelFlow, ContentSwap)

## Resumo

Manter dois niveis explicitos de reset:

- **MacroReset**: reset completo de mundo por macro rota.
- **LevelReset**: reset local de level/conteudo, sem transicao macro.

## Decisao

1. Contrato publico em `IWorldResetCommands`:
   - `ResetMacroAsync(SceneRouteId macroRouteId, string reason, string macroSignature, CancellationToken ct)`
   - `ResetLevelAsync(LevelId levelId, string reason, LevelContextSignature levelSignature, CancellationToken ct)`
2. `WorldResetCommands` publica eventos V2:
   - `WorldLifecycleResetRequestedV2Event`
   - `WorldLifecycleResetCompletedV2Event`
3. `ResetKind` define dominio (`Macro` / `Level`).

## Implementacao atual (fonte de verdade: codigo)

### MacroReset

- `WorldResetCommands.ResetMacroAsync(...)` valida macroRoute/signature, publica `RequestedV2`, aciona `IWorldResetService.TriggerResetAsync(...)` e publica `CompletedV2`.
- `WorldLifecycleSceneFlowResetDriver` integra `SceneTransitionScenesReadyEvent` com reset de mundo e completion do gate.

### LevelReset

- `WorldResetCommands.ResetLevelAsync(...)` valida `levelId` + `levelSignature`, resolve snapshot atual, publica `RequestedV2`, aciona `IContentSwapChangeService.RequestContentSwapInPlaceAsync(...)` e publica `CompletedV2`.
- `LevelSwapLocalService.SwapLocalAsync(...)` chama `IWorldResetCommands.ResetLevelAsync(...)`.

### Hardening H1 (2026-03-05)

- Para reset required no driver SceneFlow->WorldLifecycle:
  - Strict/Production: fail-fast `[FATAL][H1][WorldLifecycle]` quando `WorldResetService` esta ausente ou quando `TriggerResetAsync` falha.
  - DEV: fallback degradado com `[WARN][DEGRADED][WorldLifecycle]` e publication de completion para evitar deadlock.

### Eventos V2

- `WorldLifecycleResetRequestedV2Event` e `WorldLifecycleResetCompletedV2Event` carregam `kind`, `macroRouteId`, `levelId`, `contentId`, `macroSignature`, `levelSignature`, `success/notes`.

## Criterios de aceite (DoD)

- [x] Comandos de reset macro e level existem e sao distintos no contrato.
- [x] `WorldResetCommands` implementa os dois comandos.
- [x] Eventos V2 sao publicados para requested/completed.
- [x] LevelReset executa por content swap in-place no fluxo atual.
- [ ] Hardening: politica por level para forcar macro reset em cenarios especificos.

## Changelog

- 2026-03-05: revisado com base nas auditorias de 2026-03-04/2026-03-05 e no codigo atual.
- 2026-03-04: ADR auditado contra o codigo; removidas dependencias de evidencia por log.
