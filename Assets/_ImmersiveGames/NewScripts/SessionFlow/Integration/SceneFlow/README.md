# SessionFlow / Integration / SceneFlow

`SessionOperationalRouteTransitionBridge` observa a transicao tecnica do `SessionOperationalPipeline`.

No profile `Base11Sandbox`, `SessionOperationalNavigationComposer` compoe:
- `Base11SandboxStartupNavigationProducer`
- `SessionOperationalNavigationService`
- `ISessionOperationalTransitionPort` via `SceneFlowSessionOperationalTransitionAdapter` temporario

`SceneFlowInputModeBridge` emite `InputModeRequestEvent` diretamente quando `SessionIntegration` nao esta composto. Nao ha dependencia obrigatoria de `IntroStage`, `GameLoop` ou `RunEndRail` neste caminho minimo.
