# Legacy Cleanup Report — NewScripts (Standalone)

## Objetivo
Remover qualquer dependência do legado (`Assets/_ImmersiveGames/Scripts` e namespaces `_ImmersiveGames.Scripts.*`)
dentro de `Assets/_ImmersiveGames/NewScripts`, mantendo o pipeline operacional:
SceneFlow + Fade + WorldLifecycle + Gate + GameLoop.

## Varredura (Tarefa A)
**Termos de busca usados (excluindo `Docs/`):**
- `_ImmersiveGames.Scripts`
- `Assets/_ImmersiveGames/Scripts`
- `GameManagerSystems`
- `SceneManagement`
- `StateMachineSystems`
- `Legacy`
- `LegacyScene`
- `Legacy*Bridge`
- `FindObjectsOfType`
- `FindObjectOfType`
- `Resources.FindObjects`

**Resultados por arquivo (classificação):**
> Observação: os matches abaixo são *falsos positivos* de `SceneManagement` por conta de `UnityEngine.SceneManagement`.
> Nenhum deles referencia o legado.

**QA**
- `Assets/_ImmersiveGames/NewScripts/QA/GameplayReset/GameplayResetQaSpawner.cs`
  - Trecho: `using UnityEngine.SceneManagement;`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/QA/PlayerMovementLeakSmokeBootstrap.cs`
  - Trecho: `using UnityEngine.SceneManagement;`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/QA/Editor/PlayerMovementLeakSmokeBootstrapCI.cs`
  - Trecho: `using UnityEditor.SceneManagement;`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/QA/DependencyDISmokeQATester.cs`
  - Trecho: `using UnityEngine.SceneManagement;`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/QA/WorldLifecycleBaselineRunner.cs`
  - Trecho: `using UnityEngine.SceneManagement;`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/QA/SceneFlowPlayModeSmokeBootstrap.cs`
  - Trecho: `using UnityEngine.SceneManagement;`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/WorldLifecycle/QA/WorldLifecycleQATools.cs`
  - Trecho: `using UnityEngine.SceneManagement;`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/WorldLifecycle/Spawn/QA/WorldMovementPermissionQaRunner.cs`
  - Trecho: `using UnityEngine.SceneManagement;`

**Core service / runtime**
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/DI/DependencyManager.cs`
  - Trecho: `using UnityEngine.SceneManagement;`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/DI/SceneServiceRegistry.cs`
  - Trecho: `using UnityEngine.SceneManagement;`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/DI/SceneServiceCleaner.cs`
  - Trecho: `using UnityEngine.SceneManagement;`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/DI/DependencyInjector.cs`
  - Trecho: `using UnityEngine.SceneManagement;`
- `Assets/_ImmersiveGames/NewScripts/Gameplay/Reset/PlayersResetParticipant.cs`
  - Trecho: `using UnityEngine.SceneManagement;`
- `Assets/_ImmersiveGames/NewScripts/Gameplay/Reset/GameplayResetOrchestrator.cs`
  - Trecho: `using UnityEngine.SceneManagement;`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/WorldLifecycle/Runtime/WorldLifecycleRuntimeCoordinator.cs`
  - Trecho: `using UnityEngine.SceneManagement;`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Scene/NewSceneBootstrapper.cs`
  - Trecho: `using UnityEngine.SceneManagement;`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Scene/SceneTransitionService.cs`
  - Trecho: `using UnityEngine.SceneManagement;`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/SceneFlow/Fade/NewScriptsFadeService.cs`
  - Trecho: `using UnityEngine.SceneManagement;`

**Bridge**
- Nenhuma ocorrência.

**Docs**
- Excluídas pela varredura.

## ASMDEF Audit
**Escopo:** `Assets/_ImmersiveGames/NewScripts/**/*.asmdef`

- Resultado: **nenhum `.asmdef` encontrado em NewScripts**.
- Assemblies do legado detectados em `Assets/_ImmersiveGames/Scripts/**` (para referência de risco):
  - `Assets/_ImmersiveGames/Scripts/_ImmersiveGames.Scripts.asmdef`
  - `Assets/_ImmersiveGames/Scripts/Tests/Tests.asmdef`

| Path | Assembly Name | References | Legacy Risk (Yes/No) | Notes |
| --- | --- | --- | --- | --- |
| *(none)* | *(n/a)* | *(n/a)* | No | NewScripts não possui .asmdef no momento. |

**Nota:** Sem `.asmdef`, não há enforcement de boundaries por assembly. Regressões devem ser evitadas via CI/search ou pela criação futura de `.asmdef` (não aplicada nesta rodada).

## Observação (non-blocker): readiness/snapshot ordem
- **Ordem observada no bootstrap:** `InitializeReadinessGate()` ocorre antes de `RegisterStateDependentService()`.
  - Evidência: `GlobalBootstrap.Initialize()` chama `InitializeReadinessGate();` antes de `RegisterStateDependentService();`.
  - Trecho (GlobalBootstrap.Initialize):
    - `InitializeReadinessGate();`
    - `RegisterGameLoopSceneFlowCoordinatorIfAvailable();`
    - `...`
    - `RegisterStateDependentService();`
- **Snapshot inicial publicado no construtor do GameReadinessService.**
  - Trecho (GameReadinessService..ctor): `PublishSnapshot("bootstrap");`
- **StateDependent guarda snapshot antes de bloquear por readiness.**
  - Trecho (NewScriptsStateDependentService.OnReadinessChanged):
    - `_hasReadinessSnapshot = true;`
    - `_gameplayReady = evt.Snapshot.GameplayReady;`

## Resultado (Tarefas B e C)
- **Nenhuma referência real ao legado foi encontrada** dentro de `Assets/_ImmersiveGames/NewScripts`.
- **Nenhuma correção em código foi necessária** (apenas atualização deste relatório).
- **Tarefa C (ajuste de bootstrap/readiness):** não aplicável — sem evidência de ordem incorreta nesta varredura.

## Mudanças realizadas nesta rodada
- Ajuste da observação de readiness/snapshot com evidências objetivas do bootstrap e serviços.
- Adição de nota sobre ausência de enforcement via `.asmdef`.

## Arquivos alterados/criados
- Atualizado:
  - `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Legacy-Cleanup-Report.md`

## Mini changelog
- docs(reports): corrigir evidências de readiness e nota sobre asmdef enforcement

## Verificações finais recomendadas
1) Search: `_ImmersiveGames.Scripts` em `Assets/_ImmersiveGames/NewScripts` → 0 results.
2) Search: `Assets/_ImmersiveGames/Scripts` em `Assets/_ImmersiveGames/NewScripts` → 0 results.
3) Search: `Legacy` em `Assets/_ImmersiveGames/NewScripts` → 0 results.
4) Search: `FindObjectsOfType` / `FindObjectOfType` em `Assets/_ImmersiveGames/NewScripts` → 0 results.
5) Verificar `.asmdef` em `Assets/_ImmersiveGames/NewScripts` e revisar `references/precompiledReferences` → sem legado.
