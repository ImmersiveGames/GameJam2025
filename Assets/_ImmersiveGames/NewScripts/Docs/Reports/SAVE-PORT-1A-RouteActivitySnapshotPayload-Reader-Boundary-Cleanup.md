# SAVE-PORT-1A — RouteActivitySnapshotPayload reader boundary cleanup

Status: APPLIED / PENDING COMPILE + SMOKE.

## Objetivo

Remover do load-on-enter a projeção semântica do envelope modular para o shape legado `Objects`.

O reader de save passa a preservar o contrato salvo:

```text
CapabilitySnapshotEnvelope
```

O restore passa a consumir records do envelope diretamente no stage de `ActivityEntry`.

## Decisão aplicada

```text
RouteActivitySnapshotPayloadReader
- lê JSON salvo;
- valida schema/canonicalPayload/envelope identity;
- monta LoadedRouteActivitySnapshotPayload com ActivityCapabilitySnapshotEnvelope preservado;
- não filtra ActivityObject;
- não interpreta activity_object.transform_snapshot.v1;
- não monta Objects.
```

```text
ActivityEntryObjectSnapshotRestoreStage
- consome LoadedRouteActivitySnapshotPayload.CapabilitySnapshotEnvelope;
- filtra records ActivityObject + activity_object.transform_snapshot.v1;
- interpreta payload interno do record no owner de restore;
- monta ActivityObjectSnapshotRestoreCommand;
- chama IActivityObjectSnapshotRestoreEndpoint.
```

## Arquivos alterados

```text
SessionOperational/Contracts/RouteActivityLoadedSnapshotPayloadContracts.cs
SessionOperational/Contracts/RouteActivitySnapshotPayloadReader.cs
SessionOperational/Pipeline/OperationalRouteActivitySaveLoadOnEnterStage.cs
SessionOperational/Pipeline/OperationalConsumerEntryAndReadinessStage.cs
SessionOperational/Pipeline/SessionOperationalPipeline.cs
SessionActivity/Contracts/ActivityEntryPipelineContracts.cs
SessionActivity/Pipeline/ActivityEntryPipeline.cs
SessionActivity/Pipeline/SessionActivityPipeline.cs
SessionActivity/Pipeline/SessionActivityDebugPanel.cs
SessionActivity/Pipeline/Stages/ActivityEntryObjectSetupStages.cs
```

## Removido do caminho ativo

```text
LoadedSessionActivitySnapshotPayloadParser
LoadedSessionActivitySnapshotPayloadObject
LoadedSessionActivitySnapshotPayload.Objects para restore
ParseCapabilitySnapshotEnvelope -> Objects projection
BuildLoadedSnapshotPayloadByTargetId(Objects)
```

## Mantido fora deste corte

```text
SessionActivitySnapshotPayload.Objects no save-on-exit legado/fallback ainda existe.
Esse contrato pertence ao payload de captura/escrita e deve ser limpo em corte próprio se a auditoria confirmar que nenhum caminho ativo precisa dele.
```

## Smoke exigido

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
RouteActivitySaveSnapshotLoad checkpointStatus='Passed'
payloadKind='CapabilitySnapshotEnvelope'
canonicalPayload='CapabilitySnapshotEnvelope'
recordCount='1'
ActivityEntrySnapshotRestoreReady loadedSnapshotPayload='true'
ActivityObjectSnapshotRestore checkpointStatus='Passed'
restoreVerified='true'
snapshotRestoreTargetTransformMismatch='false'
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```
