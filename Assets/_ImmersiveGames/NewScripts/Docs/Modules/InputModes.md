# InputModes

## Estado atual

- `InputModeService` é o aplicador canônico e implementa leitura de estado via `IInputModeStateService`.
- `InputModeCoordinator` é o writer canônico dos requests.
- `InputModeRequestKind` é o contrato efetivo dos requests (`FrontendMenu`, `Gameplay`, `PauseOverlay`).
- `IPlayerInputLocator` encapsula a descoberta concreta de `PlayerInput`.
- `InputModeChangedEvent` é o hook oficial de mudança efetiva de modo.
- `SceneFlowInputModeBridge`, `GameLoop`, `Pause` e `PostRun` publicam requests; o serviço aplica o mapa efetivo.

## Ownership

- `IInputModeService`: aplicacao efetiva do mapa/input mode.
- `IInputModeStateService`: leitura canonica do modo atual.
- `InputModeChangedEvent`: observacao de transicao efetiva.
- `SceneFlowInputModeBridge`: traducao de eventos de SceneFlow para requests de InputMode.
- `IPlayerInputLocator`: descoberta concreta de `PlayerInput`.

## Regras praticas

- `InputModes` não conhece regra semântica de `SceneFlow` por string.
- `startup` não é decidido por InputModes.
- `FrontendMenu`, `Gameplay` e `PauseOverlay` são os requests canonicos já publicados.
- o post global usa requests explícitos, não inferência por stage.

