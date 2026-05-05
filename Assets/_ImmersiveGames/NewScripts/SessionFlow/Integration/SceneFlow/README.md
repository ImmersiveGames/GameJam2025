# SessionFlow / Integration / SceneFlow

`SessionOperationalRouteTransitionBridge` observa e fecha o fluxo de `Boot -> Menu` e `Menu -> Sandbox` para o `SessionOperationalPipeline`.

No profile `Base11Sandbox`, `SceneFlowInputModeBridge` emite `InputModeRequestEvent` diretamente quando `SessionIntegration` nao esta composto. Nao ha dependencia obrigatoria de `IntroStage`, `GameLoop` ou `RunEndRail` neste caminho minimo.
