# SessionFlow / Integration / SceneFlow

`SessionOperationalRouteTransitionBridge` observa a transicao tecnica do `SessionOperationalPipeline`.

No profile `Base11Sandbox`, `SessionOperationalNavigationComposer` compoe:
- `Base11SandboxStartupNavigationProducer`
- `SessionOperationalNavigationService`
- `ISessionOperationalTransitionPort` via `SceneFlowSessionOperationalTransitionAdapter` temporario

`SceneFlowInputModeBridge` continua sendo um bridge tecnico para o legado fora do Base11Sandbox. No Base11Sandbox, a intencao de input fica observada ou deferida no adapter tecnico do `SessionOperationalPipeline`, sem retornar ownership de `SceneFlow`.
