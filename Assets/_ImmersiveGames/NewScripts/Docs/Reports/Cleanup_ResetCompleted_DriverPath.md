# Driver Path — WorldLifecycleResetCompletedEvent (SceneFlow)

## Fluxo (métodos e condições)

1. `OnScenesReady(SceneTransitionScenesReadyEvent evt)`
   - Encaminha para `HandleScenesReadyAsync(evt)`.

2. `HandleScenesReadyAsync(SceneTransitionScenesReadyEvent evt)`
   - Calcula `signature` via `SceneTransitionSignatureUtil.Compute(context)`.
   - **Defensivo:** se `signature` vazia → `PublishResetCompleted(...)`.
   - **SKIP:** se `!context.TransitionProfileId.IsGameplay` → `PublishResetCompleted(...)`.
   - **Fallback:** se `controllers.Count == 0` → `PublishResetCompleted(...)`.
   - **Gameplay (normal):** chama `ExecuteResetForGameplayAsync(signature, targetScene, controllers)` e usa o retorno para decidir se publica no `finally`.

3. `ExecuteResetForGameplayAsync(string signature, string targetScene, IReadOnlyList<WorldLifecycleController> controllers)`
   - **DI (ResetWorldService):**
     - Condição: `DependencyManager.Provider.TryGetGlobal<IResetWorldService>(out var resetService)`.
     - Ação: `resetService.TriggerResetAsync(...)`.
     - Retorna `false` → driver **não** publica `ResetCompleted`.
   - **Fallback (controllers):**
     - Executa `ResetWorldAsync` nos controllers.
     - Retorna `true` → driver publica `ResetCompleted` no `finally`.

4. `PublishResetCompleted(string signature, string reason, string profile, string target)`
   - Registra observability e **publica** o evento.

## Onde ocorre o Raise

- `PublishResetCompleted(...)`
  - Linha com publish: `EventBus<WorldLifecycleResetCompletedEvent>.Raise(new WorldLifecycleResetCompletedEvent(signature ?? string.Empty, reason));`
