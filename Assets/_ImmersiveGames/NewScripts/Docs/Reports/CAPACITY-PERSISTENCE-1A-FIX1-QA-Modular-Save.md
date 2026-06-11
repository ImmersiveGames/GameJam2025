# CAPACITY-PERSISTENCE-1A-FIX1 — QA Modular Save

## Objetivo

Adicionar um caminho explícito de QA para salvar o snapshot modular atual sem depender de route-exit/BackToMenu como gatilho indireto.

## Decisão de ownership

- `SessionActivityPipeline` continua dono da captura do snapshot da activity.
- `OperationalRouteActivitySaveSaveOnExitStage` continua dono da política/comando de save de RouteActivitySave.
- `SessionOperationalActivitySaveAdapter` continua dono do side-effect físico de persistência.
- `SessionActivityDebugPanel` é somente harness de QA; ele não persiste diretamente.

## Mudanças

- `SessionActivityHost` expõe QA para capturar o `ActivityCapabilitySnapshotEnvelope` atual.
- `SessionActivityPipeline` executa `ActivityObjectSnapshotCaptureStage` em modo QA e restaura a identidade/stage anterior após a captura para não deslocar o lifecycle ativo.
- `SessionOperationalPipeline` expõe QA para solicitar save do snapshot capturado.
- `OperationalRouteActivitySaveSaveOnExitStage` adiciona `OperationalRouteActivitySaveQaSaveCommand` e executa save modular explícito via adapter canônico.
- `SessionActivityDebugPanel` adiciona botões/context menus:
  - `Capture Current Activity Snapshot Envelope`
  - `Save Captured Activity Snapshot Envelope`
  - `Capture And Save Current Activity Snapshot Envelope`

## Evidência esperada

```text
checkpoint='QaCapabilitySnapshotEnvelopeCapture' checkpointStatus='Passed'
checkpoint='RouteActivitySaveQaSave' checkpointStatus='Passed'
payloadKind='CapabilitySnapshotEnvelope'
contributorResolutionKind='capability_snapshot_envelope_resolved'
recordCount='1'
```

## Fora de escopo

- Não altera SaveRuntime core/backend.
- Não recria payload `objects: []`.
- Não reintroduz fallback legado.
- Não altera route-exit macro.
