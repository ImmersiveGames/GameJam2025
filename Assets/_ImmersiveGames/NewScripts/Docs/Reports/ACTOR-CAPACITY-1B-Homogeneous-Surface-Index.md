# ACTOR-CAPACITY-1B — Homogeneous ActorCapabilitySurface Index

## Status

Implemented / pending compile + smoke.

## Objetivo

Adicionar à `ActorCapabilitySurface` um índice homogêneo e passivo de providers de contribution, sem trocar ainda o caminho runtime ativo de setup, binding, reset, snapshot, restore, release ou save.

Este corte prepara o shape final:

```text
Actor = composição de capabilities.
Capability = dona do estado e comportamento local.
ActorCapabilitySurface = índice local passivo de endpoints/contributions.
ActivityEntry/Exit stages = donos do quando, ordem, fail e skip.
SaveRuntime = dono da persistência física.
```

## Decisão aplicada

`ActorCapabilitySurface` agora também indexa providers homogêneos:

```text
IActorSetupContributionProvider
IActorBindingContributionProvider
IActorPermissionReceiverContributionProvider
IActorResetContributionProvider
IActorSnapshotContributionProvider
IActorRestoreContributionProvider
IActorReleaseContributionProvider
```

Cada lista pode conter zero, um ou vários providers. Isso permite que um Actor seja composto por capacidades diferentes sem a surface precisar declarar cada capability concreta como propriedade final.

## O que permanece transitório

As propriedades concretas atuais permanecem no código ativo:

```text
PresentationEndpoint
AttributeEndpoint
ActorCameraTargetEndpoint
ActorMovementEndpoint
ActorPermissionReceiver
ActorCommandSourceHub
ActorProjectileFireEndpoint
```

Classificação: legado transitório ainda ativo.

Motivo: os scanners e stages atuais ainda dependem dessas propriedades. Removê-las neste corte quebraria runtime sem entregar a projection homogênea completa.

## Fronteira preservada

Este corte não altera:

```text
ActivityCapabilityScanResult
ActivityCapabilityInventoryBuilder
ActivityCapability*Scanner
PlayerActorDefaultResetEndpoint
PlayerMovementController
PlayerActorResetEndpointResolver
ActivityObject lifecycle
SessionActivityPipeline
ActivityEntryPipeline
SaveRuntime
Foundation/Platform/Pooling
```

## Regras congeladas

```text
ActorCapabilitySurface não decide lifecycle.
ActorCapabilitySurface não decide requiredness.
ActorCapabilitySurface não executa setup, binding, reset, snapshot, restore ou release.
ActorCapabilitySurface não chama SaveRuntime.
Providers declaram capability/contribution local.
Stages/policies decidem quando e se a contribution é usada.
Adapters/endpoints executam side-effects locais ou técnicos.
```

## Critério de aceite

Compile:

```text
sem erros CS
```

Smoke curto recomendado:

```text
Boot -> Menu -> Sandbox
CompleteActivationWindow
FirePrimary uma vez, se possível
sem FATAL
sem Exception
sem route_transition_failed
MovementBindingCompleted preservado
CameraBindingCompleted preservado
ActorCommandDispatchAccepted preservado quando FirePrimary for acionado
```

## Próximo corte recomendado

```text
ACTOR-CAPACITY-2A — Movement reset contribution
```

Objetivo: validar o novo modelo com uma capability concreta pequena, movendo `MovementTransient` para o próprio `PlayerMovementController` via reset contribution, sem tocar Save/Persistence.
