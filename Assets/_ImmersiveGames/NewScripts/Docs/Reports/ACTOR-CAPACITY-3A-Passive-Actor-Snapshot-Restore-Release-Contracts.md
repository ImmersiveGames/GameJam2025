# ACTOR-CAPACITY-3A — Passive Actor Snapshot/Restore/Release Contracts

## Status

`IMPLEMENTED / PENDING COMPILE`

Este corte é **contrato passivo**. Não altera runtime ativo, não troca stage, não altera SaveRuntime, não altera ActivityObject e não cria envelope modular persistido.

## Objetivo

Preparar a frente de lifecycle modular de Actor capabilities para que cada capability possa declarar, de forma homogênea:

- snapshot local;
- restore local;
- release local.

A decisão de **quando** capturar, restaurar ou liberar continua fora da capability.

```text
Actor = composição de capabilities
Capability = dona do estado local e da operação local
ActorCapabilitySurface = índice técnico/passivo de providers
ActivityEntry/Exit stages = donos do quando, ordem, fail/skip e handoff
SaveRuntime/Persistence = dono da persistência física
```

## Arquivo alterado

```text
NewScripts/Actors/Capabilities/Contracts/ActorCapabilityContributionContracts.cs
```

## Contratos adicionados

### Payload tipado de snapshot

```text
ActorCapabilitySnapshotPayload
```

O payload carrega:

```text
SessionActivityIdentity
ActorId
ActorInstanceRuntimeId
ActorKind
ActorRole
ActorScope
ActorCapabilityId
SchemaId
SchemaVersion
PayloadFormat
Payload
Source
Reason
```

Este payload ainda é local e passivo. Ele **não é SaveRecord**, **não é SaveAddress** e **não chama ISaveService**.

### Formato de payload

```text
ActorCapabilitySnapshotPayloadFormat.Unknown
ActorCapabilitySnapshotPayloadFormat.Json
ActorCapabilitySnapshotPayloadFormat.Text
ActorCapabilitySnapshotPayloadFormat.BinaryBase64
```

### Compatibilidade de restore

```text
ActorCapabilityRestoreCompatibility.Unknown
ActorCapabilityRestoreCompatibility.Compatible
ActorCapabilityRestoreCompatibility.IncompatibleActor
ActorCapabilityRestoreCompatibility.IncompatibleCapability
ActorCapabilityRestoreCompatibility.IncompatibleSchema
ActorCapabilityRestoreCompatibility.IncompatibleVersion
ActorCapabilityRestoreCompatibility.Unsupported
```

### Resultados locais

```text
ActorCapabilitySnapshotCaptureResult
ActorCapabilityRestoreResult
ActorCapabilityReleaseResult
```

Estes resultados são retornos locais de capability/endpoint. Eles ainda não são facts, comandos ou registros de save.

## Interfaces adicionadas

```text
IActorCapabilitySnapshotEndpoint
IActorCapabilityRestoreEndpoint
IActorCapabilityReleaseEndpoint
```

E as contribution interfaces existentes agora apontam para endpoints locais:

```text
IActorSnapshotContribution.SnapshotEndpoint
IActorRestoreContribution.RestoreEndpoint
IActorReleaseContribution.ReleaseEndpoint
```

## Boundary normativo

### Capability pode

- declarar que possui snapshot/restore/release;
- produzir payload local quando comandada por stage;
- restaurar seu próprio estado quando comandada por stage;
- liberar seu próprio estado quando comandada por stage.

### Capability não pode

- decidir quando snapshot roda;
- decidir se ausência de payload é failure ou skip global;
- persistir payload em backend;
- chamar SaveRuntime/ISaveService;
- comparar identidade de domínios diferentes;
- criar fallback silencioso.

## Owners

| Decisão | Owner correto |
|---|---|
| Capturar snapshot de Actor capability | Activity exit/snapshot stage futuro |
| Restaurar snapshot de Actor capability | ActivityEntryPipeline/stage futuro |
| Liberar state local de capability | Activity exit/release stage futuro |
| Persistir payload | SaveRuntime adapter |
| Indexar providers | ActorCapabilitySurface |
| Executar operação local da capability | Endpoint local da própria capability |

## O que não mudou

```text
ActorCapabilitySurface
PlayerActorResetEndpointResolver
ActorResetAdapter
PlayerActor
PlayerActorParticipationState
PlayerMovementController
ActivityObject
SessionActivityPipeline
ActivityEntryPipeline
SaveRuntime
Foundation/Platform/Pooling
```

## Compatibilidade/legado

Não houve remoção neste corte porque ainda não há runtime de snapshot/restore/release de Actor capability. O corte só prepara contratos para os próximos passos.

A remoção de trilhos concretos deve ocorrer quando houver substituto ativo validado por smoke.

## Próximos cortes sugeridos

```text
ACTIVITY-OBJECT-CAPACITY-1A — Object lifecycle contribution provider
CAPACITY-SNAPSHOT-1A — Modular snapshot envelope
CAPACITY-PERSISTENCE-1A — Save adapter consumes modular snapshot envelope
```

Antes de mexer em SaveRuntime, a recomendação é criar a projection modular e validar que Actor/Object snapshots entram em envelope técnico sem persistência física.

## Critério de validação deste corte

Como o corte é passivo:

```text
compile sem erro CS
sem missing script
smoke curto sem regressão inesperada
```

Critérios de smoke:

```text
sem FATAL
sem Exception
sem route_transition_failed
ActivityParticipantResetApplied preservado
MovementBindingCompleted preservado
CameraBindingCompleted preservado
ActorCommandDispatchAccepted preservado, se FirePrimary for acionado
RestartCurrentActivity preservado, se executado
```
