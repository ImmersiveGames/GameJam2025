# InputModes

## Estado atual

- `SceneFlowInputModeBridge` sincroniza InputMode com o fluxo de cenas.
- `PostGameOwnershipService` sincroniza o modo de input do post global.
- A decisao principal continua vindo do `RouteKind` observado no contexto da transicao.

## Ownership

- `IInputModeService`: aplicacao efetiva do mapa/input mode.
- `SceneFlowInputModeBridge`: traducao de eventos de SceneFlow para requests de InputMode.
- `PostGameOwnershipService`: ownership do input no `PostGame` global.

## Regras praticas

- `frontend` e `gameplay` pertencem a `RouteKind`.
- `startup` nao e decidido por InputModes.
- O post game usa input de frontend por ownership global, nao por stage do level.
