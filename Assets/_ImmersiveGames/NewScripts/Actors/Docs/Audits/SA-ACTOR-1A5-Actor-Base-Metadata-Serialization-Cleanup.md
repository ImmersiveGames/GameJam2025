# SA-ACTOR-1A5 — Actor base metadata serialization cleanup

Status: IMPLEMENTED / AWAITING COMPILE + SMOKE

## Objetivo

Remover do `Actor` base campos serializados que duplicavam metadata já declarada pelas especializações concretas.

A documentação de Actor converge para este shape:

```text
Actor = raiz runtime abstrata.
PlayerActor / NonPlayerActor = especializações concretas.
Role/Scope = metadata declarada pela especialização concreta, não authoring genérico no Actor base.
```

## Arquivo alterado

```text
NewScripts/Actors/Runtime/Actor.cs
```

## Alteração

Removido do `Actor` base:

```csharp
[SerializeField] private ActorRole baseActorRoleMetadata = ActorRole.Unknown;
[SerializeField] private ActorScope baseActorScopeMetadata = ActorScope.Unknown;
```

Alterado para contrato abstrato:

```csharp
public abstract ActorRole ActorRoleMetadata { get; }
public abstract ActorScope ActorScopeMetadata { get; }
```

## Por que isso é correto

`Actor` é abstrato e não deve carregar metadata autoral de role/scope genérica quando as especializações concretas já declaram esses valores.

Após os cortes anteriores:

```text
NonPlayerActor -> ActorRole.SceneAuthoredNonPlayer + ActorScope serializado local canônico
PlayerActor -> ActorRole.PrimaryPlayer + ActorScope.RouteScoped
```

Portanto, os campos `baseActorRoleMetadata` e `baseActorScopeMetadata` eram redundantes e podiam confundir ownership de metadata.

## O que não mudou

```text
RuntimeActorInstanceId continua runtime-only, não serializado.
ActorDefinitionRef continua default no Actor base.
ActorCapabilitySurface continua resolvida pelo Actor base.
NonPlayerActor não foi alterado neste corte.
PlayerActor não foi alterado neste corte.
Nenhum pipeline/stage/adapter foi alterado.
```

## Ownership

| Item | Owner correto |
|---|---|
| Role de um actor concreto | Especialização concreta de `Actor` |
| Scope de um actor concreto | Especialização concreta de `Actor` ou authoring local canônico da especialização |
| Lifecycle de setup/readiness | `ActivityEntryPipeline` |
| Materialização de player actor | `ActivityEntryPipeline` + adapters de materialização |
| Presentation/Attributes/Participation | Stages/Endpoints canônicos de Actor |

## Critério de aceite

Compile:

```text
sem erro CS
sem warning CS novo
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
