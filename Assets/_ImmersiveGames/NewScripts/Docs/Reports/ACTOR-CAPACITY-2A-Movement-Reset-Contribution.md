# ACTOR-CAPACITY-2A — Movement Reset Contribution

## Status

Implemented / pending compile + smoke.

## Objetivo

Mover o reset `MovementTransient` para a capability que possui o estado de movement, sem alterar o lifecycle macro de reset e sem tocar Save, Snapshot, ActivityObject ou Persistence.

## Decisão

`MovementTransient` não deve ser responsabilidade de `PlayerActorDefaultResetEndpoint`.

O estado transitório de movimento pertence a:

```text
PlayerMovementController
```

Logo, o próprio controller passa a expor o contrato homogêneo de reset:

```text
IActorResetEndpoint
IActorResetContributionProvider
```

`PlayerActorDefaultResetEndpoint` permanece temporariamente apenas para os grupos actor-level ainda ativos:

```text
Placement
ActivityParticipation
```

## Alterações

### PlayerMovementController

Passa a implementar:

```text
IActorResetEndpoint
IActorResetContributionProvider
```

E declara suporte somente a:

```text
ActorResetGroup.MovementTransient
```

O reset executado continua sendo o comportamento local já existente:

```text
ClearMovementState()
```

### PlayerActorDefaultResetEndpoint

Deixa de suportar:

```text
ActorResetGroup.MovementTransient
```

Remove o campo serializado `movementController`, porque o wrapper não deve mais encaminhar reset de movement.

Continua suportando:

```text
Placement
ActivityParticipation
```

## Fronteira preservada

```text
ActivityEntryPipeline continua dono do quando/ordem do reset.
ActorResetAdapter continua executando o command canônico ativo.
PlayerActorResetEndpointResolver continua como bridge ativa até ACTOR-CAPACITY-2C.
ActorCapabilitySurface já indexa IActorResetContributionProvider, mas o runtime ativo ainda consome IActorResetEndpoint.
SaveRuntime não foi alterado.
Snapshot/Restore/Release não foram alterados.
ActivityObject não foi alterado.
```

## O que este corte remove

```text
PlayerActorDefaultResetEndpoint como owner de MovementTransient.
Campo serializado movementController no PlayerActorDefaultResetEndpoint.
Encaminhamento artificial PlayerActorDefaultResetEndpoint -> PlayerMovementController.ClearMovementState().
```

## O que este corte não remove

```text
PlayerActorDefaultResetEndpoint ainda existe para Placement e ActivityParticipation.
PlayerActorResetEndpointResolver ainda faz scan de IActorResetEndpoint.
ActorResetAdapter ainda usa o resolver antigo.
```

Esses pontos ficam para cortes posteriores:

```text
ACTOR-CAPACITY-2B — Actor-level reset split
ACTOR-CAPACITY-2C — Actor reset resolution via lifecycle contributions
```

## Critério de aceite

Compile:

```text
sem erros CS
```

Smoke curto:

```text
Boot -> Menu -> Sandbox
CompleteActivationWindow
QA Reset Current Player Actor, se disponível
sem FATAL
sem Exception
sem route_transition_failed
ActorResetQaApplied preservado, se QA reset for acionado
ActivityObjectReset preservado
MovementBindingCompleted preservado
MovementControlEnabled preservado em ActivityRunning
CameraBindingCompleted preservado
```

## Classificação arquitetural

```text
PASS esperado se compilar e preservar smoke.
Não é PASS final da frente de reset, porque o resolver ainda escaneia IActorResetEndpoint diretamente.
Este corte apenas move o ownership local de MovementTransient para a capability correta.
```
