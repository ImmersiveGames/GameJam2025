# WorldLifecycle Module - Runtime

## Visão Geral

Este diretório contém a implementação **determinística** do ciclo do mundo (reset + spawn + hooks) e a integração **mínima** com o SceneFlow necessária para cumprir o contrato do Baseline.

Pontos-chave:

- O reset do mundo é orquestrado por um **controller de cena** (`WorldLifecycleController`) + um **orchestrator puro** (`WorldLifecycleOrchestrator`).
- A integração com SceneFlow ocorre via **driver** (`WorldLifecycleSceneFlowResetDriver`), que dispara reset em `ScenesReady` (apenas em `profile=gameplay`) e publica `WorldLifecycleResetCompletedEvent` para liberar o completion gate.
- Não existe mais `WorldLifecycleRuntimeCoordinator` (removido por obsolescência).

## Estrutura

### Core (Runtime)

- `IWorldResetRequestService.cs` — Interface pública para solicitar reset (via DI global).
- `WorldResetRequestService.cs` — Implementação canônica de `IWorldResetRequestService`.
- `WorldLifecycleController.cs` — MonoBehaviour de cena que enfileira e executa resets de forma sequencial.
- `WorldLifecycleOrchestrator.cs` — Fluxo determinístico do reset (Gate → Hooks → Despawn → ScopedReset → Spawn → Hooks → Release).
- `WorldLifecycleResetCompletedEvent.cs` — Evento emitido ao final do reset (usado pelo completion gate do SceneFlow).
- `WorldLifecycleResetCompletionGate.cs` — Gate que aguarda `WorldLifecycleResetCompletedEvent` antes de liberar `FadeOut/Completed`.
- `WorldLifecycleTokens.cs` — Tokens canônicos do WorldLifecycle usados com `SimulationGate`.

### Integração com SceneFlow (Runtime)

- `WorldLifecycleSceneFlowResetDriver.cs` — Driver canônico SceneFlow → WorldLifecycle:
  - Observa `SceneTransitionScenesReadyEvent`.
  - Em `profile=gameplay`, localiza `WorldLifecycleController` na cena alvo e dispara `ResetWorldAsync(reason)`.
  - Publica `WorldLifecycleResetCompletedEvent(signature, reason)` **sempre** (best-effort), evitando timeout do gate.

## Namespaces

Todos os arquivos deste diretório permanecem em:

- `_ImmersiveGames.NewScripts.Lifecycle.World.Runtime`

## Notas de manutenção

- **Não** mover lógica de reset para o driver. O driver deve permanecer fino e best-effort.
- O `WorldLifecycleOrchestrator` é o ponto de verdade para **ordem** e **determinismo**. Mudanças aqui exigem revisão de ADRs/evidências.
- Sempre que alterar strings de `reason`/`signature`, atualizar evidências e regras de matching.
