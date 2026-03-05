# ADR-0023 — Dois níveis de reset: MacroReset vs LevelReset

## Status

- Estado: **Aceito (Implementado)**
- Data (decisão): 2026-02-19
- Última atualização: 2026-03-04
- Tipo: Implementação
- Escopo: NewScripts/Modules (WorldLifecycle, SceneFlow, LevelFlow, ContentSwap)

## Resumo

Manter dois níveis explícitos de reset:

- **MacroReset**: reset completo de mundo por macro rota.
- **LevelReset**: reset local de level/conteúdo, sem transição macro.

## Decisão

1. Contrato público em `IWorldResetCommands`:
   - `ResetMacroAsync(SceneRouteId macroRouteId, string reason, string macroSignature, CancellationToken ct)`
   - `ResetLevelAsync(LevelId levelId, string reason, LevelContextSignature levelSignature, CancellationToken ct)`
2. `WorldResetCommands` publica eventos V2 canônicos:
   - `WorldLifecycleResetRequestedV2Event`
   - `WorldLifecycleResetCompletedV2Event`
3. `ResetKind` define domínio (`Macro` / `Level`).

## Implementação atual (fonte de verdade: código)

### MacroReset

- `WorldResetCommands.ResetMacroAsync(...)` valida macroRoute/signature, publica `RequestedV2`, aciona `IWorldResetService.TriggerResetAsync(...)` e publica `CompletedV2`.
- `WorldLifecycleSceneFlowResetDriver` integra `SceneTransitionScenesReadyEvent` com reset de mundo e completion do gate.

### LevelReset

- `WorldResetCommands.ResetLevelAsync(...)` valida `levelId` + `levelSignature`, resolve snapshot atual, publica `RequestedV2`, aciona `IContentSwapChangeService.RequestContentSwapInPlaceAsync(...)` e publica `CompletedV2`.
- `LevelSwapLocalService.SwapLocalAsync(...)` chama `IWorldResetCommands.ResetLevelAsync(...)` como etapa principal do swap local.

### Eventos V2

- `WorldLifecycleResetRequestedV2Event` e `WorldLifecycleResetCompletedV2Event` carregam `kind`, `macroRouteId`, `levelId`, `contentId`, `macroSignature`, `levelSignature`, `success/notes`.

## Critérios de aceite (DoD)

- [x] Comandos de reset macro e level existem e são distintos no contrato.
- [x] `WorldResetCommands` implementa os dois comandos.
- [x] Eventos V2 são publicados para requested/completed.
- [x] LevelReset executa por content swap in-place no fluxo atual.
- [ ] Hardening: política por level para forçar macro reset em cenários específicos.

## Changelog

- 2026-03-04: ADR auditado contra o código; removidas dependências de evidência por log.
