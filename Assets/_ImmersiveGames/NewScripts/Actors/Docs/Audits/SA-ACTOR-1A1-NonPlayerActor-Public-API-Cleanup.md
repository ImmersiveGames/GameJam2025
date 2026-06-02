# SA-ACTOR-1A1 — NonPlayerActor Public API Cleanup

Status: IMPLEMENTED / AWAITING COMPILE + SMOKE

## Objetivo

Reduzir `NonPlayerActor` para uma especialização concreta de `Actor` usada pelo discovery canônico de atores de cena, sem expor API paralela que incentive rails `NonPlayer` separados.

## Alterações

### Runtime

Arquivo alterado:

- `NewScripts/Actors/Runtime/NonPlayerActor.cs`

Removidos da API pública:

- `NonPlayerActorId`
- `ActorScope`
- `ParticipationPolicy`
- `PresentationProfile`
- `PresentationEndpoint`
- `ParticipatingActivities`
- `ValidateOrThrow(string source)`
- `ContainsActivityId(string activityId)`
- `IsValid`
- `ResolveParticipatingActivityIdsOrFail(string source)`

Mantidos:

- `ActorId` como override canônico de `Actor`.
- `ActorRoleMetadata = SceneAuthoredNonPlayer` como metadata local.
- `ActorScopeMetadata` como metadata canônica `ActorScope`.
- `ISceneAuthoredActor.SceneActorScope`.
- `ISceneAuthoredActor.SceneActorParticipationPolicy`.
- `ResolveExplicitParticipationActivityIdsOrFail`.
- `ValidateSceneAuthoredConfigurationOrThrow`.
- campos serializados existentes para evitar migração YAML neste corte.

## Deleções necessárias

Remover do projeto:

- `NewScripts/Actors/ActivitySetup/ActivityNonPlayerActorRegistry.cs`
- `NewScripts/Actors/ActivitySetup/NonPlayerActorInstanceSource.cs`

Esses arquivos representam o trilho antigo especializado por `NonPlayer`. O trilho ativo deve permanecer:

- `ActivitySceneActorRegistry`
- `SceneAuthoredActorInstanceSource`
- `ISceneAuthoredActor`
- `ActorCapabilitySurface`

## Fora do escopo

Não foram removidos neste corte:

- `NonPlayerActorScope`
- `NonPlayerActorParticipationPolicy`
- campos serializados `presentationProfile` e `presentationEndpoint`

Motivo: remover ou migrar esses campos pode exigir migração de assets/prefabs/YAML. Esse trabalho deve ser corte separado, com smoke dedicado.

## Critério de aceite

Compile:

- sem erro CS.

Smoke mínimo:

- Boot -> Menu -> Sandbox -> `activity_01` -> CompleteActivationWindow -> BackToMenu.
- sem `FATAL`.
- sem `Exception`.
- sem `route_transition_failed`.
- sem foreign/stale indevido.
- `ActivityEntryActorSceneDiscoveryCompleted` presente.
- `ActivityEntryActorInventoryFeedCompleted` presente.
- `ActorPresentationReady` para actor de cena presente.
- `RouteExitBackToMenu` PASS.

Critério arquitetural:

- nenhum uso ativo de `ActivityNonPlayerActorRegistry`.
- nenhum uso ativo de `NonPlayerActorInstanceSource`.
- nenhum código externo dependendo de `NonPlayerActor.NonPlayerActorId`, `ActorScope`, `ParticipationPolicy`, `PresentationProfile`, `PresentationEndpoint` ou `ContainsActivityId`.
