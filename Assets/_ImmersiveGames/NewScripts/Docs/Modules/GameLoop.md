# GameLoop

## Status atual (Baseline 3.1)

- Baseline 3.1 mantida sem mudanca funcional.
- Trilho canonico:
  - `GameLoopService` + state machine
  - `GameCommands` como API de comandos
  - `GameRunOutcomeService` (publisher idempotente de `GameRunEndedEvent`)
  - `GameRunStateService` (snapshot de estado/resultado)
  - `GameLoopSceneFlowCoordinator` para sync de start plan com SceneFlow/WorldLifecycle
  - IntroStage via `IIntroStageCoordinator` + `IIntroStageControlService`

## Ownership

| Componente | Owner de | Nao-owner de | Anchors |
|---|---|---|---|
| `Runtime/Services/GameLoopService.cs` | Ciclo de estados da run | Macro restart | publish `GameRunStartedEvent` |
| `Commands/GameCommands.cs` | Emissao de comandos definitivos (pause/resume/restart/exit/run-end request) | Consumo de reset | `[GameCommands] RequestRestart ...` |
| `Runtime/Bridges/GameLoopCommandEventBridge.cs` | Bridge pause/resume/exit -> `IGameLoopService` | Listener de reset (`GameResetRequestedEvent`) | `[OBS][LEGACY] ... listener disabled ...` |
| `Runtime/Bridges/GameLoopSceneFlowCoordinator.cs` | Sync StartPlan/SceneFlow -> `RequestReady` | Start gameplay efetivo (fica no pipeline IntroStage/LevelFlow) | `[GameLoopSceneFlow] Sync concluído ... RequestReady()` |
| `Runtime/Services/GameRunOutcomeService.cs` | Publicar `GameRunEndedEvent` no maximo uma vez por run | Controle de overlay/UI | `[GameLoop] Publicando GameRunEndedEvent ...` |
| `Runtime/Services/GameRunStateService.cs` | Snapshot de resultado/reason para UI/sistemas | Publicacao de fim de run | `GameRunStateService registrado ...` |
| `IntroStage/Runtime/*` + `IntroStageControlService.cs` | IntroStage policy/complete/skip | Selecao de level | `[IntroStageController] ...` |

## Timeline (encaixe no A-E)

1. `SceneFlow` completa transicao de start plan (`GameLoopSceneFlowCoordinator`).
2. Coordinator sincroniza para `RequestReady`.
3. `LevelFlow`/IntroStage pipeline decide quando liberar start efetivo.
4. `GameLoopService` entra em `Playing` e publica `GameRunStartedEvent`.
5. Durante run: comandos definitivos (`pause/resume/exit`) trafegam por `GameLoopCommandEventBridge`.
6. Fim de run: `GameRunOutcomeService` publica `GameRunEndedEvent` (idempotente).
7. `PostGameOwnershipService`/UI consomem estado pos-run.

## LEGACY/Compat

- Em `GameLoopCommandEventBridge`, listener de `GameResetRequestedEvent` permanece explicitamente desativado (owner canonico do reset: `MacroRestartCoordinator`, modulo Navigation).
- Nao houve move para `Modules/GameLoop/Legacy/**` em GL-1.1 por falta de evidencia conclusiva de inatividade em runtime.

## Requires manual confirmation (LF/GL hygiene)

- `Bindings/Bootstrap/GameStartRequestEmitter.cs` (MonoBehaviour + RuntimeInitializeOnLoadMethod)
- `Bindings/Inputs/GamePauseHotkeyController.cs` (MonoBehaviour, possivel binding de cena)
- `IntroStage/Dev/Editor/IntroStageDevTools.cs` (editor/dev)

## Status GL-1.2 (Live, 2026-03-06)
- Triagem A/B/C atualizada com evidência estática (runtime canônico vs compat/legacy vs dev/editor).
- `IntroStageDevTools` ficou explicitamente isolado em `#if UNITY_EDITOR` (arquivo `Modules/GameLoop/IntroStage/Dev/Editor/IntroStageDevTools.cs`).
- `GameStartRequestEmitter` e `GamePauseHotkeyController` mantidos como runtime canônico (sem mudança funcional).
- Restart canônico preservado: `GameCommands -> GameResetRequestedEvent -> MacroRestartCoordinator`.
- Snapshot desta etapa: `Docs/Reports/Audits/2026-03-06/Modules/GameLoop-Cleanup-Audit-v2.md`.

## Status GL-1.3 (Live, 2026-03-06)
- `GameStartRequestEmitter`: bootstrap automático legado removido (sem `RuntimeInitializeOnLoadMethod`) e isolamento via `EnsureInstalled()` no trilho canônico DevQA.
- `GamePauseHotkeyController`: classificado como dead/legacy por evidência estática (sem refs em assets e sem callsite canônico) e movido para `Modules/GameLoop/Legacy/Bindings/Inputs/`.
- Itens GL-1.3 isolados por `#if UNITY_EDITOR || DEVELOPMENT_BUILD` (Release exclui DevQA/DevTools alterados).
- Restart canônico preservado: `GameCommands -> GameResetRequestedEvent -> MacroRestartCoordinator`.
- Snapshot desta etapa: `Docs/Reports/Audits/2026-03-06/Modules/GameLoop-Cleanup-Audit-v3.md`.

## Status GL-1.3b (Live, 2026-03-06)
- Validacao por GUID dos alvos `GameStartRequestEmitter` e `GamePauseHotkeyController`: sem referencias em `.unity/.prefab/.asset` dentro do workspace local.
- Start em Release garantido por install canônico no stage GameLoop (`InstallGameLoopServices`), sem depender de DevQA.
- `GameStartRequestEmitter` permanece idempotente via `EnsureInstalled()` e publisher de `GameStartRequestedEvent`.
- `GamePauseHotkeyController` permanece classificado como legacy (movido para `Modules/GameLoop/Legacy/Bindings/Inputs/`).

## Status PA-1.1 (Live, 2026-03-07)
- `PauseOverlayController` foi convertido para `partial` sem alteração de fluxo runtime (pause/resume/overlay/input mode/gates).
- Tooling DevQA (`ContextMenu` QA de Pause/Resume) foi isolado em `Modules/GameLoop/Pause/Dev/PauseOverlayController.DevQA.cs`.
- Arquivo runtime `Modules/GameLoop/Pause/Bindings/PauseOverlayController.cs` ficou sem `UnityEditor`, `ContextMenu`, `MenuItem`, `AssetDatabase` e `FindAssets`.
- Build matrix preservada: DevQA disponível apenas em `UNITY_EDITOR || DEVELOPMENT_BUILD`; release exclui tooling Dev.
- Snapshot desta etapa: `Docs/Reports/Audits/2026-03-06/Modules/Pause-Cleanup-Audit-v1.md`.
