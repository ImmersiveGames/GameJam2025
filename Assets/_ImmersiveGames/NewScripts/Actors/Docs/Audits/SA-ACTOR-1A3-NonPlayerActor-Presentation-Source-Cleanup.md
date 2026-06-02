# SA-ACTOR-1A3 — NonPlayerActor Presentation Source Cleanup

Status: IMPLEMENTED / AWAITING COMPILE + SMOKE

## Objetivo

Remover de `NonPlayerActor` as referências diretas e duplicadas de apresentação, mantendo `ActorCapabilitySurface -> ActorPresentationEndpoint -> ActorPresentationProfileAsset` como fonte canônica de presentation.

## Alterações

Arquivo alterado:

- `NewScripts/Actors/Runtime/NonPlayerActor.cs`

Removidos de `NonPlayerActor`:

- campo serializado `presentationProfile`;
- campo serializado `presentationEndpoint`;
- validação direta de `ActorPresentationProfileAsset` no `NonPlayerActor`;
- validação direta de `ActorPresentationEndpoint` como campo local do `NonPlayerActor`;
- usings de `Actors.Presentation.Authoring` e `Actors.Presentation.Runtime`.

Validação canônica nova:

```text
NonPlayerActor.ValidateLocalConfigurationOrThrow
-> Actor.CapabilitySurface
-> ActorCapabilitySurface.PresentationEndpoint
-> ActorPresentationEndpoint.ValidateOrThrow
```

## Ownership

| Item | Owner correto |
|---|---|
| Actor concreto de cena | `NonPlayerActor` como especialização de `Actor` |
| Descoberta de ator | `ActorSceneDiscoveryStage` + `ISceneAuthoredActor` |
| Surface de capacidades locais | `ActorCapabilitySurface` |
| Presentation endpoint/profile/containers | `ActorPresentationEndpoint` |
| Setup de presentation | `ActivityEntryActorPresentationStage` |

## Justificativa

O smoke anterior de `SA-ACTOR-1A1` confirmou que o fluxo ativo usa `ActorCapabilitySurface` e `ActorPresentationEndpoint` para presentation, com `ActorPresentationReady` preservado para os três actors da activity. As referências diretas em `NonPlayerActor` eram fonte autoral duplicada e podiam divergir da surface realmente consumida pelo pipeline.

## Fora do escopo

Não foi alterado neste corte:

- `NonPlayerActorScope`;
- `NonPlayerActorParticipationPolicy`;
- `nonPlayerActorId` serializado;
- `Actor.baseActorRoleMetadata` / `baseActorScopeMetadata`;
- `PlayerActor`;
- `ActivityEntryPipeline`;
- `SessionActivityPipeline`;
- RouteExit / Restart / CompleteCurrentActivity.

## Observação sobre YAML

A remoção dos campos serializados pode deixar dados órfãos em prefabs/assets até o próximo save do Unity. Isso é aceitável no desenvolvimento atual porque o runtime passa a validar e consumir a fonte canônica `ActorCapabilitySurface.PresentationEndpoint`. Não há compat de produto a preservar.

## Critério de aceite

Compile:

```text
sem erro CS
```

Smoke mínimo:

```text
Boot -> Menu -> Sandbox -> activity_01 -> CompleteActivationWindow -> BackToMenu
```

Critérios:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
ActivityEntryActorSceneDiscoveryCompleted
ActivityEntryActorInventoryFeedCompleted
ActorPresentationSetupCompleted
ActorPresentationReady para npc.generic.01
ActorPresentationReady para npc.route.generic.01
ActorPresentationReady para actor.player.primary
RouteExitBackToMenu PASS
```

## Próximo corte possível

Depois de compile/smoke, o próximo corte seguro é `SA-ACTOR-1A4` ou `SA-ACTOR-1A5`, mas somente após decidir se vamos pagar risco de migração de enum/YAML agora:

```text
SA-ACTOR-1A4 — Collapsar NonPlayerActorScope/NonPlayerActorParticipationPolicy para contratos genéricos.
SA-ACTOR-1A5 — Limpar metadata serializada do Actor base.
```
