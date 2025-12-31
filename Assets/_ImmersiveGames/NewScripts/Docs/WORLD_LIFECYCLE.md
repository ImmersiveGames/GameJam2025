# World Lifecycle (NewScripts)

> Este documento implementa operacionalmente as decisões descritas no **ADR – Ciclo de Vida do Jogo, Reset por Escopos e Fases Determinísticas**.

## Atualização (2025-12-31)

- **Sem flash** confirmado: `LoadingHudScene` é exibida somente após `FadeInCompleted` e é ocultada antes do `FadeOut` (`phase='BeforeFadeOut'`), com `Hide(phase='Completed')` como segurança.
- Ordem operacional validada: **FadeIn → LoadingHUD Show → Load/Unload → ScenesReady → WorldLifecycle Reset (ou Skip) → completion gate → LoadingHUD Hide → FadeOut → Completed**.
- Evidência: logs de produção (startup → Menu → Gameplay), com `GameReadinessService` mantendo o `SimulationGate` fechado durante transição/reset e liberando no `SceneTransitionCompletedEvent`.

## Atualização (2025-12-30)

- Fluxo de **produção** validado end-to-end: Startup → Menu → Gameplay via SceneFlow + Fade + LoadingHUD + Navigation.
- `WorldLifecycleRuntimeCoordinator` reage a `SceneTransitionScenesReadyEvent`:
    - Profile `startup`/frontend: reset **skip** + emite `WorldLifecycleResetCompletedEvent(contextSignature, reason)`.
    - Profile `gameplay`: dispara **hard reset** (`ResetWorldAsync`) e emite `WorldLifecycleResetCompletedEvent(contextSignature, reason)` ao concluir.
- `SceneTransitionService` aguarda o `WorldLifecycleResetCompletedEvent` (via `ISceneTransitionCompletionGate`) antes do `FadeOut`.
    - A chave é o `contextSignature` computado a partir do `SceneTransitionContext` (assinatura canônica via `SceneTransitionSignatureUtil.Compute(context)`; hoje equivalente a `context.ToString()`).
- Hard reset em Gameplay confirma spawn via `WorldDefinition` (Player/Eater) e execução do orchestrator com gate (`WorldLifecycle.WorldReset`).
- `IStateDependentService` bloqueia input/movimento enquanto `SimulationGate` está fechado e/ou `gameplayReady=false`; libera ao final. Pausa também fecha gate via `GamePauseGateBridge`.

```log
[INFO] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] SceneTransitionScenesReady recebido. Context=SceneTransitionContext(Load=[MenuScene, UIGlobalScene], Unload=[NewBootstrap], TargetActive='MenuScene', UseFade=True, Profile='startup')
[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Reset SKIPPED (startup/frontend). profile='startup', activeScene='MenuScene'.
[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='startup', signature='SceneTransitionContext(Load=[MenuScene, UIGlobalScene], Unload=[NewBootstrap], TargetActive='MenuScene', UseFade=True, Profile='startup')', reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'.
