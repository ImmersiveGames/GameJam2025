# CAPACITY-RESTORE-1C — QA Save Canonical Owner Alignment

## Status

APPLIED / PENDING COMPILE + SMOKE

## Objetivo

Alinhar o save manual de QA ao mesmo owner usado pelo load-on-enter de `RouteActivitySave`.

Antes do corte, o QA save gravava usando o `ActivityId` do payload (`activity_01`) como owner do registro, enquanto o load-on-enter procurava pelo owner da route activity/session (`SessionActivitySandboxSession`). O smoke conseguia passar quando o save-on-exit posterior regravava o owner canônico, mas o botão QA mantinha um trilho semântico diferente.

## Decisão

- `SessionOperationalPipeline` continua dono do save/load de rota.
- `ActivityEntryPipeline` continua dono do restore.
- O payload continua carregando `payloadActivityIdentity`/`sourceActivityId` como atividade real capturada.
- O registro de save passa a usar `saveOwnerActivityIdentity`, derivado de `sessionStateId`.
- Não há fallback: o owner canônico fica explícito em log.

## Fronteira

- `saveOwnerActivityIdentity`: identidade usada pelo adapter para endereço/chave do save de rota.
- `payloadActivityIdentity`: atividade que produziu o snapshot (`activity_01`, por exemplo).
- `requestedActivityIdentity`: atividade solicitada pelo botão QA para validação do payload ativo.

Essas identidades não devem ser tratadas como equivalentes.

## Arquivos alterados

- `SessionOperational/Pipeline/OperationalRouteActivitySaveSaveOnExitStage.cs`
- `SessionOperational/Pipeline/SessionOperationalPipeline.cs`
- `SessionOperational/Adapters/ISessionOperationalActivitySaveAdapter.cs`
- `SessionOperational/Adapters/SessionOperationalActivitySaveAdapter.cs`
- `SessionActivity/Pipeline/SessionActivityDebugPanel.cs`

## Evidência esperada

Após `Capture Current Activity Snapshot Envelope` + `Save Captured Activity Snapshot Envelope` em `activity_01`:

```text
RouteActivitySaveQaSave checkpointStatus='Passed'
saveOwnerActivityIdentity='SessionActivitySandboxSession'
payloadActivityIdentity='activity_01'
```

Ao entrar novamente no Sandbox, o load deve poder encontrar o payload pelo owner canônico sem depender de um save-on-exit intermediário:

```text
RouteActivitySaveSnapshotLoad checkpointStatus='Passed'
activityIdentity='SessionActivitySandboxSession'
sourceActivityId='activity_01'
recordCount='1'
```

Em `activity_02` no-content:

```text
RouteActivitySaveQaSave checkpointStatus='SkippedNoContent'
saveOwnerActivityIdentity='SessionActivitySandboxSession'
payloadActivityIdentity='activity_02'
```
