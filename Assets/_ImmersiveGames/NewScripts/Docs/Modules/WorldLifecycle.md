# WorldLifecycle

## Status atual (Baseline 3.1)

### Fluxo canônico (texto)
No estado atual, o SceneFlow publica `SceneTransitionScenesReadyEvent`, o `WorldLifecycleSceneFlowResetDriver` decide se deve resetar, e o macro reset é delegado para `WorldResetService`, que por sua vez executa `WorldResetOrchestrator`. O completion V1 destrava o gate via `WorldLifecycleResetCompletionGate`.

### Fluxo canônico (bullet diagram)
- `SceneTransitionService` publica `SceneTransitionScenesReadyEvent`
- `WorldLifecycleSceneFlowResetDriver.OnScenesReady`
- `WorldLifecycleSceneFlowResetDriver.ExecuteResetWhenRequiredAsync`
- `WorldResetService.TriggerResetAsync(WorldResetRequest)`
- `WorldResetOrchestrator.ExecuteAsync`
- `EventBus<WorldLifecycleResetStartedEvent>.Raise(...)` (V1)
- execução do reset nos controllers alvo
- `EventBus<WorldLifecycleResetCompletedEvent>.Raise(...)` (V1)
- `WorldLifecycleResetCompletionGate` consome completed e libera o gate

## Ownership (Baseline 3.1)

- Owner do macro reset (world reset): `Modules/WorldLifecycle/WorldRearm/Application/WorldResetService.cs`.
- Owner da execução do pipeline macro reset (guards/validation/execute/publish V1): `Modules/WorldLifecycle/WorldRearm/Application/WorldResetOrchestrator.cs`.
- Owner do reset local/scoped por cena: `Modules/WorldLifecycle/Bindings/WorldLifecycleController.cs` + `Modules/WorldLifecycle/Runtime/WorldLifecycleOrchestrator.cs`.
- Owner do handoff SceneFlow -> WorldLifecycle: `Modules/WorldLifecycle/Runtime/WorldLifecycleSceneFlowResetDriver.cs`.
- Owner dos eventos V2 (commands/telemetria): `Modules/WorldLifecycle/Runtime/WorldResetCommands.cs`.

## Eventos V1 vs V2

| Grupo | Eventos | Finalidade | Consumidores atuais |
|---|---|---|---|
| V1 (gate) | `WorldLifecycleResetStartedEvent`, `WorldLifecycleResetCompletedEvent` | Coordenação do reset para trilho de SceneFlow/Gates | `WorldLifecycleResetCompletionGate`, `GameLoopSceneFlowCoordinator` (completed) |
| V2 (commands/telemetry) | `WorldLifecycleResetRequestedV2Event`, `WorldLifecycleResetCompletedV2Event` | Observabilidade de comandos macro/level reset | Sem `EventBus.Register` no escopo auditado |

## LEGACY/Compat (apenas nota)

- Coexistem dois orquestradores com responsabilidades diferentes e ativas:
  - `WorldResetOrchestrator` (macro reset, V1 para gates)
  - `WorldLifecycleOrchestrator` (reset local/scoped por controller de cena)
- Existe fallback publisher de `WorldLifecycleResetCompletedEvent` em:
  - `WorldLifecycleSceneFlowResetDriver` (SKIP/fallback)
  - `WorldResetService` (catch/best-effort)

## Referências

- Audit detalhado: `Docs/Reports/Audits/2026-03-06/WorldLifecycle-Cleanup-Audit-v1.md`

## WL-1.2 (publishers V1/V2 consolidation, behavior-preserving)

- V1 (`WorldLifecycleResetStartedEvent` / `WorldLifecycleResetCompletedEvent`) agora tem publish central explícito no `WorldResetOrchestrator` via helpers:
  - `PublishResetStartedV1(...)`
  - `PublishResetCompletedV1(...)`
- `WorldResetService` (fallback em catch) e `WorldLifecycleSceneFlowResetDriver` (SKIP/fallback) não fazem mais `EventBus<V1>.Raise` direto; ambos roteiam para o helper do `WorldResetOrchestrator`.
- V2 (`WorldLifecycleResetRequestedV2Event` / `WorldLifecycleResetCompletedV2Event`) permanece exclusivamente em `WorldResetCommands` (owner de commands/telemetria).
- Boundarys explícitos:
  - `WorldResetOrchestrator`: owner pipeline macro + publish V1 gate.
  - `WorldLifecycleController` / `WorldLifecycleOrchestrator`: trilho local/scoped por cena.
  - `WorldResetCommands`: owner V2 commands/telemetria.

Evidência detalhada: `Docs/Reports/Audits/2026-03-06/Modules/WorldLifecycle-Cleanup-Audit-v2.md`.
