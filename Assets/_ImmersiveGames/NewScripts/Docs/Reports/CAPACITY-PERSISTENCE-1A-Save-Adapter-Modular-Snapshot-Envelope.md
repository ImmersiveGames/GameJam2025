# CAPACITY-PERSISTENCE-1A — Save adapter consumes modular snapshot envelope

## Objetivo

Integrar `RouteActivitySave` ao payload canônico `ActivityCapabilitySnapshotEnvelope`, sem voltar ao remendo `objects: []` e sem tratar envelope-only como erro transitório.

## Decisão

`ActivityObjectSnapshotCaptureStage` continua dono do capture timing e produz `SessionActivitySnapshotPayload` com `CapabilitySnapshotEnvelope`.

`OperationalRouteActivitySaveSaveOnExitStage` passa a serializar diretamente o envelope modular como payload persistível.

`SessionOperationalActivitySaveAdapter` continua sendo adapter técnico do `SaveRuntime`; ele grava/carrega a string resolvida e registra o tipo do payload consumido.

`LoadedSessionActivitySnapshotPayloadParser` passa a aceitar o formato modular e projeta records `ActivityObject` + `activity_object.transform_snapshot.v1` para o shape runtime existente de restore.

## Boundary

- Capture continua em `ActivityObjectSnapshotCaptureStage`.
- Persistência física continua no `SaveRuntime` via `SessionOperationalActivitySaveAdapter`.
- Restore timing continua em `ActivityEntryPipeline`.
- Parser faz projeção técnica de payload carregado; não decide lifecycle.

## Não objetivos

- Não criar SaveRuntime novo.
- Não recriar fallback legado.
- Não persistir `objects: []` para envelope-only.
- Não alterar `ActivityObject` lifecycle.
- Não alterar `ActivityEntryPipeline` timing.

## Evidência esperada

- `RouteActivitySnapshotPayloadResolved` com `contributorResolutionKind='capability_snapshot_envelope_resolved'` quando houver payload útil.
- `RouteActivitySaveSaveCompleted` com detail contendo `payloadKind='CapabilitySnapshotEnvelope'` quando save-on-exit gravar envelope modular.
- `RouteActivitySaveSnapshotLoad Passed` continua carregando para `LoadedSessionActivitySnapshotPayload` quando houver load-on-enter com payload modular salvo.
