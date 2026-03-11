# ContentSwap (Baseline 3.1)

## Status atual (2026-03-06)
- Owner canônico de troca: `Modules/ContentSwap/Runtime/InPlaceContentSwapService.cs` via `IContentSwapChangeService`.
- Owner canônico de contexto/estado: `Modules/ContentSwap/Runtime/ContentSwapContextService.cs` via `IContentSwapContextService`.
- Registro canônico no pipeline: `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs` + `GlobalCompositionRoot.ContentLevels.cs`.

## Ownership final
- `InPlaceContentSwapService`: owner do fluxo de request in-place, guard de in-flight, validação de gate e commit.
- `ContentSwapContextService`: owner do estado `Current/Pending` e publicação dos eventos de contexto.
- Não-owner: SceneFlow transition/fade/loading e macro-restart orchestration.

## Integração com LevelFlow/WorldReset
- `Modules/WorldLifecycle/Runtime/WorldResetCommands.cs` consome `IContentSwapChangeService` no reset de nível (`ResetLevelAsync`).
- `Modules/LevelFlow/**` não chama `IContentSwapChangeService` diretamente no trilho canônico atual; integra por contexto/eventos de nível.
- `Modules/SceneFlow/**` não possui integração canônica direta com execução de ContentSwap (InPlace-only).

## LEGACY/Compat isolado
- Dentro de `Modules/ContentSwap/**`: nenhum arquivo classificado como `LEGACY_COMPAT` nesta etapa.
- Consumidor legado externo ao modulo: nenhum remanescente apos `BATCH-CLEANUP-STD-1`.

## Manual confirmation required
- `Modules/ContentSwap/Legacy/Dev/Runtime/ContentSwapDevBootstrapper.cs` foi removido em `BATCH-CLEANUP-STD-2` por prova de tipo morto (callsite + GUID = 0).
- `Modules/ContentSwap/Dev/Bindings/ContentSwapDevContextMenu.cs`: `#if UNITY_EDITOR`; wiring real depende de contexto de editor/play mode.
## CS-1.2 (ownership/publish consolidation, behavior-preserving)

### Ownership final
- `ContentSwapContextService` Ã© owner do estado (`Current/Pending`) e publisher Ãºnico de:
  - `ContentSwapPendingSetEvent`
  - `ContentSwapPendingClearedEvent`
  - `ContentSwapCommittedEvent`
- `InPlaceContentSwapService` Ã© executor canÃ´nico do request InPlace (validaÃ§Ãµes/gates/fluxo) e delega `SetPending/TryCommitPending/ClearPending` ao context service.

### Timeline canÃ´nica
1. `RequestContentSwapInPlaceAsync(...)` (executor)
2. `SetPending(...)` (context service) -> `ContentSwapPendingSetEvent`
3. `TryCommitPending(...)` (context service) -> `ContentSwapCommittedEvent`
4. `ClearPending(...)` (context service, apenas quando aplicÃ¡vel) -> `ContentSwapPendingClearedEvent`

### Consumers por mÃ³dulo
- `WorldLifecycle`: usa `IContentSwapChangeService` em `WorldResetCommands.ResetLevelAsync(...)`.
- `LevelFlow`: sem consumer canÃ´nico direto de ContentSwap no trilho atual.
- `Navigation (Legacy)`: nenhum consumer legacy remanescente apos `BATCH-CLEANUP-STD-1`.

EvidÃªncia detalhada: `Docs/Reports/Audits/2026-03-06/Modules/ContentSwap-Cleanup-Audit-v2.md`.
## BATCH-CLEANUP-STD-2
- Removed in `BATCH-CLEANUP-STD-2`: `Modules/ContentSwap/Legacy/Dev/Runtime/ContentSwapDevBootstrapper.cs`.
- Rationale: bootstrap legacy de DevQA sem callsite em `.cs` fora do proprio arquivo e sem referencia por GUID em assets.

