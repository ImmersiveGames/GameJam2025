# Gameplay

## Estado atual

- `ActorGroupRearm` continua sendo a nomenclatura canonica para rearm local de gameplay.
- `WorldDefinition` e o runtime de spawn continuam owners do setup de mundo e atores.
- O rearm de gameplay acontece no escopo da cena e nao reabre trilhos legados.

## Ownership

- `WorldDefinition` + spawning runtime: setup e spawn de gameplay.
- `GameplayStateGate`: gate canonico de acoes/gameplay por estado.
- `ActorGroupRearmOrchestrator`: rearm canonico de grupos de atores.
- `PlayerActorGroupRearmWorldParticipant`: ponte do reset de players para `ByActorKind(Player)`.
- `GameplayCameraResolver`: camera gameplay para consumers globais.

## Regras praticas

- Prefira `ByActorKind` como trilho principal.
- Use `ActorIdSet` apenas quando o caso realmente exigir selecao tecnica explicita.
- Nomeie e documente esse fluxo como `ActorGroupRearm`.

## Leitura cruzada

- `Docs/ADRs/ADR-0014-GameplayReset-Targets-Grupos.md`
- `Docs/Modules/SceneReset.md`
- `Docs/Modules/WorldReset.md`
