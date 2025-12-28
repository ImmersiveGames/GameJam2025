# Legacy Cleanup Report — NewScripts (Standalone)

## Objetivo
Remover qualquer dependência do legado (`Assets/_ImmersiveGames/Scripts` e namespaces `_ImmersiveGames.Scripts.*`)
dentro de `Assets/_ImmersiveGames/NewScripts`, mantendo o pipeline operacional:
SceneFlow + Fade + WorldLifecycle + Gate + GameLoop.

## Varredura (Tarefa A)
**Termos de busca usados (excluindo `Docs/`):**
- `_ImmersiveGames.Scripts`
- `Legacy`
- `Scripts.Scene` / `Scripts.Utils` / `Scripts.Game`
- `using Legacy`
- `FindObjectsOfType`
- `Resources.FindObjects`

**Resultados por arquivo (classificação):**
> Observação: os matches abaixo são *falsos positivos* do termo `Scripts.Game` por conta de namespaces/using de **NewScripts**.
> Nenhum deles referencia o legado.

**QA**
- `Assets/_ImmersiveGames/NewScripts/QA/Deprecated/GameplayResetQaProbe.cs`
- `Assets/_ImmersiveGames/NewScripts/QA/Deprecated/GameplayResetQaSpawner.cs`
- `Assets/_ImmersiveGames/NewScripts/QA/Deprecated/WorldLifecycleAutoTestRunner.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/QA/PlayerMovementLeakSmokeBootstrap.cs`
- `Assets/_ImmersiveGames/NewScripts/QA/Deprecated/WorldLifecycleBaselineRunner.cs`
- `Assets/_ImmersiveGames/NewScripts/QA/Deprecated/WorldLifecycleQATools.cs`
- `Assets/_ImmersiveGames/NewScripts/QA/Deprecated/WorldMovementPermissionQaRunner.cs`
- `Assets/_ImmersiveGames/NewScripts/QA/Deprecated/GameLoopStartRequestQAFrontend.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/GameLoop/QA/GameLoopStateFlowQATester.cs`

**Core service / runtime**
- `Assets/_ImmersiveGames/NewScripts/Gameplay/Reset/PlayersResetParticipant.cs`
- `Assets/_ImmersiveGames/NewScripts/Gameplay/Reset/IGameplayResetTargetClassifier.cs`
- `Assets/_ImmersiveGames/NewScripts/Gameplay/Reset/GameplayResetOrchestrator.cs`
- `Assets/_ImmersiveGames/NewScripts/Gameplay/Reset/GameplayResetContracts.cs`
- `Assets/_ImmersiveGames/NewScripts/Gameplay/Reset/DefaultGameplayResetTargetClassifier.cs`
- `Assets/_ImmersiveGames/NewScripts/Gameplay/Player/Movement/NewPlayerInputReader.cs`
- `Assets/_ImmersiveGames/NewScripts/Gameplay/Player/Movement/NewPlayerMovementController.cs`
- `Assets/_ImmersiveGames/NewScripts/Gameplay/GameLoop/GameLoopService.cs`
- `Assets/_ImmersiveGames/NewScripts/Gameplay/GameLoop/GameLoopSceneFlowCoordinator.cs`
- `Assets/_ImmersiveGames/NewScripts/Gameplay/GameLoop/GameLoopEvents.cs`
- `Assets/_ImmersiveGames/NewScripts/Gameplay/GameLoop/GameLoopStateMachine.cs`
- `Assets/_ImmersiveGames/NewScripts/Gameplay/GameLoop/GameLoopRuntimeDriver.cs`
- `Assets/_ImmersiveGames/NewScripts/Gameplay/GameLoop/IGameLoopContracts.cs`
- `Assets/_ImmersiveGames/NewScripts/Gameplay/GameLoop/GameLoopBootstrap.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/GlobalBootstrap.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Execution/Gate/GamePauseGateBridge.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/WorldLifecycle/Spawn/PlayerSpawnService.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/GameLoop/Production/GameStartRequestProductionBootstrapper.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/GameLoop/GameLoopEventInputBridge.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/State/NewScriptsStateDependentService.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Scene/NewSceneBootstrapper.cs`

**Bridge**
- Nenhuma ocorrência.

**Docs**
- Excluídas pela varredura.

## Resultado (Tarefas B e C)
- **Nenhuma referência real ao legado foi encontrada** dentro de `Assets/_ImmersiveGames/NewScripts`.
- **Nenhuma correção em código foi necessária** (apenas atualização deste relatório).
- **Tarefa C (ajuste de bootstrap/readiness):** não aplicável — sem evidência de ordem incorreta nesta varredura.

## Mudanças realizadas nesta rodada
- Atualização do relatório de limpeza (este arquivo).
- Arquivos movidos para `Deprecated`; paths atualizados.

## Arquivos alterados/criados
- Atualizado:
  - `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Legacy-Cleanup-Report.md`

## Mini changelog
- docs(reports): atualizar varredura e registrar ausência de legado em NewScripts

## Verificações finais recomendadas
1) Search: `_ImmersiveGames.Scripts` em `Assets/_ImmersiveGames/NewScripts` → 0 results.
2) Search: `Legacy` em `Assets/_ImmersiveGames/NewScripts` → 0 results.
3) Search: `FindObjectsOfType` em `Assets/_ImmersiveGames/NewScripts` → 0 results.
4) Search: `Resources.FindObjects` em `Assets/_ImmersiveGames/NewScripts` → 0 results.
