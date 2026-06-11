# CAPACITY-SNAPSHOT-1A — Modular snapshot envelope

## Status

Implemented / pending compile + smoke.

## Objetivo

Introduzir um envelope modular de snapshot de capabilities sem alterar a persistência física do `SaveRuntime` neste corte.

## Boundary

- `ActivityObjectSnapshotCaptureStage` continua sendo o owner da captura em saída de activity.
- `ActivityObjectTransformSnapshotProvider` continua sendo o endpoint local que produz o snapshot de transform.
- `SessionActivitySnapshotPayload.Objects` permanece como payload legado/compatível usado pelo fluxo atual de save/restore.
- `ActivityCapabilitySnapshotEnvelope` passa a carregar registros modulares por capability dentro do payload runtime.
- `SaveRuntime` não foi alterado e ainda serializa o shape anterior.

## Arquivos

- `SessionActivity/Contracts/ActivityCapabilitySnapshotEnvelopeContracts.cs`
- `SessionActivity/Contracts/SessionActivitySnapshotPayloadContracts.cs`
- `SessionActivity/Pipeline/Stages/ActivityObjectSnapshotCaptureStage.cs`

## Contratos adicionados

- `ActivityCapabilitySnapshotOwnerKind`
- `ActivityCapabilitySnapshotPayloadFormat`
- `ActivityCapabilitySnapshotRecord`
- `ActivityCapabilitySnapshotEnvelope`

## Runtime behavior esperado

Para `activity_01`, o comportamento anterior deve permanecer:

- `ActivityCapabilityInventoryValidationPassed`
- `ActivityObjectReset PassedApplied`
- `ActivityObjectSnapshotCapture capturedCount='1'`
- `ActivityObjectRelease appliedCount='1'`

Além disso, a captura deve emitir observabilidade nova:

```text
[OBS][CapabilitySnapshotEnvelope] event='CapabilitySnapshotEnvelopeCaptured'
```

## Fora do corte

- Persistência física do envelope no `SaveRuntime`.
- Parser/load do envelope modular.
- Merge entre snapshot de Actor e ActivityObject.
- Migração de restore para consumir envelope modular.
