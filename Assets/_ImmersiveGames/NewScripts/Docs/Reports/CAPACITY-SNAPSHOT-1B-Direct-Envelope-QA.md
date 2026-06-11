# CAPACITY-SNAPSHOT-1B — Direct envelope QA / legacy object payload stop

## Status

Implemented. Pending compile + smoke.

## Objetivo

Remover o remendo do `CAPACITY-SNAPSHOT-1A` que populava `SessionActivitySnapshotPayload.Objects` apenas para manter o smoke antigo passando.

O snapshot canônico do corte passa a ser o envelope modular:

```text
ActivityCapabilitySnapshotEnvelope
-> ActivityCapabilitySnapshotRecord
```

## Mudanças

- `ActivityObjectSnapshotCaptureStage` não cria mais `SessionActivitySnapshotPayloadObject` durante captura canônica.
- `SessionActivitySnapshotPayload` passa a ser válido quando contém `CapabilitySnapshotEnvelope` válido.
- `ActivityObjectSnapshotCaptureStage` armazena payload envelope-only com `legacyObjectCount='0'`.
- `SessionActivityDebugPanel` deixa de emitir o checkpoint antigo `ActivityObjectSnapshotCapture`.
- O QA/smoke passa a emitir checkpoint direto:

```text
CapabilitySnapshotEnvelopeCapture
```

com:

```text
recordCount
ownerKinds
envelopeSchemaId
legacyObjectCount='0'
canonicalPayload='CapabilitySnapshotEnvelope'
```

## SaveRuntime

`OperationalRouteActivitySaveSaveOnExitStage` não serializa envelope-only como payload legado vazio.

Enquanto a persistência modular não for implementada, payload envelope-only é rejeitado explicitamente com:

```text
modular_snapshot_envelope_persistence_not_integrated
```

Isso evita fallback silencioso para `objects: []`.

## Boundary

- Capture stage produz snapshot modular.
- QA valida snapshot modular diretamente.
- SaveRuntime ainda não é owner de persistência modular neste corte.
- Persistência física do envelope fica para corte posterior.

## Não mexido

- Restore modular.
- Parser de loaded snapshot.
- ActivityObject lifecycle scanner.
- Actor snapshot.
- ActivityObject reset/release.
