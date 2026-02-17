# Smoke — DataCleanup v1 (2026-02-17)

## Objetivo
Validar o estado pós DataCleanup v1 no fluxo canônico (Boot → Menu → Gameplay) e confirmar o relatório do validador de configuração SceneFlow.

## Resultado do validator
- Resultado: **PASS**.
- Linha de referência: "[SceneFlow][Validation] PASS. Report generated at: Assets/_ImmersiveGames/NewScripts/Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md"
- Report: [`../../SceneFlow-Config-ValidationReport-DataCleanup-v1.md`](../../SceneFlow-Config-ValidationReport-DataCleanup-v1.md)

## Evidência de smoke runtime
- Log bruto: [`../../lastlog.log`](../../lastlog.log)
- Trecho relevante (PlayButton → Gameplay):

```log
[MenuPlayButtonBinder] [OBS][LevelFlow] MenuPlay -> StartGameplayAsync levelId='level.1' reason='Menu/PlayButton'.
[GameNavigationService] [OBS][Navigation] DispatchIntent -> intentId='to-gameplay', sceneRouteId='level.1', styleId='style.gameplay', reason='Menu/PlayButton'
[SceneTransitionService] [SceneFlow] TransitionStarted id=2 ... routeId='level.1' ... reason='Menu/PlayButton'
[SceneTransitionService] [OBS][SceneFlow] RouteExecutionPlan routeId='level.1' activeScene='GameplayScene' toLoad=[GameplayScene, UIGlobalScene] toUnload=[NewBootstrap, MenuScene]
```

## Conclusão
- **PASS** para smoke de DataCleanup v1 + navegação PlayButton → Gameplay + validator.

## Próximos passos
- Sem pendências abertas para P-002 nesta rodada; manter monitoramento via `lastlog.log` e validator antes de novos merges.
