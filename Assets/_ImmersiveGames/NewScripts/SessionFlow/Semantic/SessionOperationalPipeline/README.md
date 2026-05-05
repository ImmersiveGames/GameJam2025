# SessionOperationalPipeline v0

Rail operacional canônico do Base11Sandbox para intenção de navegação e handoff técnico de transição.

Fluxo:
- `NavigateToRoute`
- `RouteResolved`
- `RequestRouteTransition`
- `ISessionOperationalTransitionPort` via `SceneFlowSessionOperationalTransitionAdapter` temporario
- `SceneTransitionStarted`

O profile `Base11Sandbox` compõe este rail para `BootStartPlanRequestedEvent -> NavigateToRoute(routeId='to-menu')`.
