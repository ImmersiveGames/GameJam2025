# InputModes

## Estado atual

- `SceneFlowInputModeBridge` sincroniza InputMode com o fluxo de cenas.
- A decisao principal vem do `RouteKind` observado no contexto da transicao.

## Ownership

- `IInputModeService`: aplicacao efetiva do mapa/input mode.
- `SceneFlowInputModeBridge`: traducao de eventos de SceneFlow para requests de InputMode.

## Semantica de fluxo

- `frontend` e `gameplay` pertencem a `RouteKind`.
- `startup` nao e decidido por InputModes; entra pelo bootstrap/boot path.
