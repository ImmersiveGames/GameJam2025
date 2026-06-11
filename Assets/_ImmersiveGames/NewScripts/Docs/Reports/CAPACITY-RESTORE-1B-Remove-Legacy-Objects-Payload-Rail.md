# CAPACITY-RESTORE-1B — Remove legacy Objects payload rail

Status: APPLIED / PENDING COMPILE + SMOKE

## Objetivo

Remover do caminho ativo de snapshot/save/restore o trilho legado `SessionActivitySnapshotPayload.Objects` / `LegacyObjectSnapshot`.

O baseline funcional já validado em `SAVE-PORT-1A/FIX2` usa `CapabilitySnapshotEnvelope` como payload canônico. Este corte fecha o resíduo que ainda permitia serialização ou classificação ativa de payload legado por `Objects`.

## Decisões de ownership

- Pipeline owner: `SessionOperationalPipeline` para save/load de route activity; `ActivityEntryPipeline` para restore.
- Categoria: contrato runtime + stage de save; não é backend.
- Comportamento final: `SessionActivitySnapshotPayload` carrega apenas `ActivityCapabilitySnapshotEnvelope`.
- Bridge transitória removida: `LegacyObjectSnapshot` / `Objects`.
- Compatibilidade: payload legado salvo continua não suportado pelo reader e deve ser rejeitado explicitamente no load; não deve haver escrita nova nesse formato.
- Owner duplicado evitado: backend permanece blob store; não decide shape de Activity snapshot.

## Alterações

- `SessionActivitySnapshotPayloadObject` removido.
- `SessionActivitySnapshotPayload.Objects` removido.
- `SessionActivitySnapshotPayload.HasLegacyObjectPayload` removido.
- `SessionActivitySnapshotPayload.IsValid` agora exige `CapabilitySnapshotEnvelope` válido.
- `ActivityObjectSnapshotCaptureStage` constrói payload só com envelope.
- `OperationalRouteActivitySaveSaveOnExitStage` não serializa `LegacyObjectSnapshot`.
- `SessionOperationalActivitySaveAdapter` não classifica payload `objects` como payload conhecido.
- Logs de QA mantêm `recordCount`, `payloadKind` e `targetIds` derivados de records do envelope.
- `legacyObjectCount` removido dos logs runtime novos do capture/QA.

## Evidência esperada

Smoke mínimo:

1. Boot -> Menu -> Sandbox.
2. Complete Activation Window em `activity_01`.
3. Capture Current Activity Snapshot Envelope.
4. Save Captured Activity Snapshot Envelope.
5. Back To Menu.
6. Menu -> Sandbox.
7. Verificar load/restore.
8. Ir para `activity_02` e executar capture/save QA no-content.
9. Back To Menu.

Critérios:

- Sem `FATAL`.
- Sem `Exception`.
- Sem `route_transition_failed`.
- Sem `checkpointStatus='Failed'`.
- Sem `LegacyObjectSnapshot` em logs novos.
- Sem `legacyObjectCount` em logs runtime novos.
- `RouteActivitySaveQaSave Passed` com `payloadKind='CapabilitySnapshotEnvelope'` e `recordCount='1'` em `activity_01`.
- `RouteActivitySaveSnapshotLoad Passed` com `payloadKind='CapabilitySnapshotEnvelope'` e `recordCount='1'`.
- `ActivityObjectSnapshotRestore Passed` com `restoreVerified='true'`.
- `QaCapabilitySnapshotEnvelopeCapture SkippedNoContent` em `activity_02`.
- `RouteActivitySaveQaSave SkippedNoContent` em `activity_02`.

## Observação

Se existir save antigo em PlayerPrefs com payload legado, o load deve continuar rejeitando/ignorando sem fallback silencioso. Para validar o corte limpo, preferir limpar saves antigos ou gerar novo save modular antes do segundo load.
