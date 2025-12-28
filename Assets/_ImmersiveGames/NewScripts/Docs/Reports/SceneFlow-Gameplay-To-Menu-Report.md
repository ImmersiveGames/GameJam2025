# SceneFlow — Gameplay → Menu Report

- Data: 2025-12-28
- Objetivo: validar o caminho de produção **Gameplay → Menu** via `IGameNavigationService`, com Fade + LoadingHUD, unload da `GameplayScene` e `GameLoop` saindo de `Playing`.

## Sequência de ações

1. Boot → Menu (fluxo normal de startup).
2. Menu → Gameplay (Play).
3. Em Gameplay, acionar ExitToMenu (via PauseOverlay ou trigger de debug quando necessário).
4. Confirmar retorno ao Menu com `MenuScene` como ativa e `GameplayScene` descarregada.

## Trechos essenciais de log (esperado)

```
[SceneFlow] SceneTransitionStarted ... Profile='gameplay'
[WorldLifecycle] SceneTransitionScenesReady recebido ...
[SceneFlowGate] WorldLifecycleResetCompletedEvent recebido ...
[SceneFlow] SceneTransitionCompleted ...
[Navigation] ExitToMenu recebido -> RequestToMenu.
[SceneFlow] SceneTransitionStarted ... Profile='startup'
[SceneFlow] SceneTransitionScenesReady ...
[WorldLifecycle] Reset SKIPPED (startup/frontend) ...
[SceneFlow] SceneTransitionCompleted ...
[GameLoop] ExitToMenu recebido -> RequestReady (não voltar para Playing).
```

> Observação: no ambiente atual, a execução manual do Unity Editor não foi realizada.

## Checklist de validação

- [ ] Menu → Gameplay concluído com Fade + LoadingHUD
- [ ] ExitToMenu dispara transição via `IGameNavigationService`
- [ ] `GameplayScene` descarregada
- [ ] `MenuScene` ativa novamente
- [ ] `GameLoop` fora de `Playing`
- [ ] Gate tokens zerados (`SimulationGate` sem tokens ativos)

## Resultado

⚠️ **Pendente** — validação manual não executada neste ambiente. Recomendado rodar o playtest no Editor e coletar os logs acima para evidência.
