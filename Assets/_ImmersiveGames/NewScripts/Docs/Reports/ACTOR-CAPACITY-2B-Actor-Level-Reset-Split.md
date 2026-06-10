# ACTOR-CAPACITY-2B — Actor-level reset split

## Status

IMPLEMENTED / PENDING COMPILE + SMOKE.

## Objetivo

Separar o reset actor-level do reset capability-level sem trocar ainda o resolver inteiro de reset.

Este corte fecha o wrapper transitório `PlayerActorDefaultResetEndpoint` como runtime ativo. O reset passa a ser declarado pelos componentes que possuem a responsabilidade local:

- `PlayerActor` declara e executa `ActorResetGroup.Placement`.
- `PlayerActorParticipationState` declara e executa `ActorResetGroup.ActivityParticipation`.
- `PlayerMovementController` já declara e executa `ActorResetGroup.MovementTransient` desde `ACTOR-CAPACITY-2A`.

## Arquivos alterados

- `GameplayRuntime/Authoring/Actors/Player/PlayerActor.cs`
- `Actors/Players/Runtime/PlayerActorParticipationState.cs`
- `Resources/Actors/PlayerActor_v0.prefab`

## Arquivos removidos

- `Actors/Players/Runtime/PlayerActorDefaultResetEndpoint.cs`
- `Actors/Players/Runtime/PlayerActorDefaultResetEndpoint.cs.meta`

Se a ferramenta de aplicação do pacote não remover arquivos ausentes automaticamente, apagar esses dois arquivos manualmente.

## Decisão arquitetural

`Reset` é uma fase transversal. A capability ou componente local dono do estado deve expor o contrato homogêneo de reset.

O antigo `PlayerActorDefaultResetEndpoint` misturava responsabilidades:

- placement do Actor;
- participação da Activity;
- anteriormente, movement transient.

Depois do corte:

```text
PlayerActor
-> IActorResetEndpoint / IActorResetContributionProvider
-> Placement

PlayerActorParticipationState
-> IActorResetEndpoint / IActorResetContributionProvider
-> ActivityParticipation

PlayerMovementController
-> IActorResetEndpoint / IActorResetContributionProvider
-> MovementTransient
```

## O que não mudou

- `PlayerActorResetEndpointResolver` ainda resolve `IActorResetEndpoint` por scan local.
- `ActorResetAdapter` ainda executa groups pelo contrato antigo.
- `ActorCapabilitySurface.ResetContributionProviders` ainda é índice passivo.
- Snapshot/Restore/Release/Save não foram alterados.
- ActivityObject lifecycle não foi alterado.

A troca do resolver para usar surface/projection fica para `ACTOR-CAPACITY-2C`.

## Ownership

| Responsabilidade | Owner correto neste corte |
|---|---|
| Placement reset | `PlayerActor` |
| ActivityParticipation reset | `PlayerActorParticipationState` |
| MovementTransient reset | `PlayerMovementController` |
| Quando resetar | `ActivityEntryPipeline` / stage ativo |
| Como resolver endpoints neste corte | `PlayerActorResetEndpointResolver` transitório |
| Persistência | fora do escopo |

## Compatibilidade

Não foi preservado trilho ativo para `PlayerActorDefaultResetEndpoint`.

O prefab `PlayerActor_v0` foi atualizado para remover o componente antigo e adicionar `PlayerActorParticipationState` como endpoint local de participation reset.

## Critério de aceite

Compile:

- sem erro CS;
- sem missing script no prefab `PlayerActor_v0`;
- sem referência ativa ao GUID antigo de `PlayerActorDefaultResetEndpoint`.

Smoke:

- sem `FATAL`;
- sem `Exception`;
- sem `route_transition_failed`;
- `ActivityParticipantResetApplied resetGroups='Placement,ActivityParticipation' appliedGroups='2'` preservado;
- `MovementBindingCompleted` preservado;
- `MovementControlEnabled` preservado;
- `CameraBindingCompleted` preservado;
- `RestartCurrentActivity` preservado;
- `ActorCommandDispatchAccepted` preservado quando `FirePrimary` for acionado em `ActivityRunning`.

## Próximo corte

`ACTOR-CAPACITY-2C — Actor reset resolution via lifecycle contributions`.

Objetivo: substituir o scan direto de `IActorResetEndpoint` por resolução via `ActorCapabilitySurface` / lifecycle contributions, removendo o trilho próprio do resolver.
