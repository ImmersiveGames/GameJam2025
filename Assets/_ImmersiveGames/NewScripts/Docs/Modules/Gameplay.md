# Gameplay

## Estado atual

- `ActorGroupRearm` e a nomenclatura canonica para rearm/reset de gameplay.
- `WorldDefinition` e runtime de spawn continuam owners do setup de mundo e atores.
- O rearm de gameplay acontece no escopo do mundo/cena, sem reabrir trilhos legacy.

## Ownership

- `WorldDefinition` + spawning runtime: setup e spawn de gameplay.
- `ActorGroupRearmOrchestrator`: rearm canonico de grupos de atores.
- `CameraResolverService`: camera gameplay para consumers globais.

## Boundary

- Runtime vivo: `Modules/Gameplay/Runtime/**`.
- Tooling/editor legado fora da superficie operacional.
