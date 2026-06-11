# SAVE-PORT-1A-FIX1 — QA load pointer + no-content classification

## Objetivo

Corrigir a evidência necessária para fechar o corte `SAVE-PORT-1A` sem reintroduzir bridge legado.

## Alterações

- `OperationalRouteActivitySaveLoadOnEnterStage` deixou de exigir `SaveCurrentState.CurrentSnapshotId` diretamente.
- `ProgressionSlotContextResolver` passa a ser o owner técnico único da resolução de `slotId/snapshotId` para RouteActivitySave.
- Load-on-enter agora observa e loga `RouteActivitySaveLoadPointerResolved` antes de consultar o adapter.
- `DefaultProgressionSlotContextResolver` agora loga:
  - `snapshotPointerSource`
  - `currentSnapshotIdRaw`
  - `currentRevision`
- QA save e save-on-exit agora logam `RouteActivitySaveCurrentSnapshotPointerObserved` após save.
- QA capture/save em activity no-content agora emite `SkippedNoContent`, não `Failed`.
- Botões de snapshot QA ficam desabilitados em activity sem gameplay content.
- O painel exibe `Snapshot QA: canCapture/canSave/contentMode/stage`.

## Owner

- `SessionOperationalPipeline / RouteActivitySave`: decide quando load/save roda.
- `IProgressionSlotContextResolver`: resolve ponteiro técnico de slot/snapshot.
- `ISaveBackend`: continua apenas persistindo blob/record.
- `ActivityEntryObjectSnapshotRestoreStage`: continua dono do restore semântico dos records.

## Evidência esperada

Primeira entrada sem snapshot salvo pode registrar:

```text
RouteActivitySaveLoadPointerResolved ... snapshotPointerSource='revision_fallback'
RouteActivitySaveSnapshotLoad checkpointStatus='Waiting'
```

Após `Capture + Save Snapshot Envelope (QA)`:

```text
RouteActivitySaveCurrentSnapshotPointerObserved snapshotIdMatches='true'
RouteActivitySaveQaSave checkpointStatus='Passed'
```

Na próxima entrada com snapshot salvo:

```text
RouteActivitySaveSnapshotLoad checkpointStatus='Passed' payloadKind='CapabilitySnapshotEnvelope' recordCount='1'
ActivityEntrySnapshotRestoreReady loadedSnapshotPayload='true'
ActivityObjectSnapshotRestore checkpointStatus='Passed' restoredCount='1' restoreVerified='true'
```

Para `activity_02` no-content:

```text
QaCapabilitySnapshotEnvelopeCapture checkpointStatus='SkippedNoContent'
RouteActivitySaveQaSave checkpointStatus='SkippedNoContent'
```

## Status

`APPLIED / PENDING COMPILE + SMOKE`.
