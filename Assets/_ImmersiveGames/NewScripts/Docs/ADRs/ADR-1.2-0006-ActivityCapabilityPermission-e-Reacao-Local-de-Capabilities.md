# ADR-1.2-0006 — ActivityCapabilityPermission e Reação Local de Capabilities

Status: Accepted / Base 1.2  
Área: Permission / Movement / ActorCapability  
Atualização: H4D-B2 — CLOSED / PASS

---

## Contexto

`PermissionTarget` controla se uma capability local pode reagir ao estado de gameplay. No caso atual, a aplicação principal é movement control do PlayerActor.

A migração para ActorCapabilitySurface removeu o uso de `PlayerActorTargets` como fonte primária do scanner, mas o contrato de runtime ainda contém `playerActorId/playerSlotId`.

---

## Decisão

### 1. Scanner usa ActorCapabilitySurface

`ActivityCapabilityPermissionScanner` deve consumir:

- `ActorScanTarget`;
- `ActorCapabilitySurface`;
- `PlayerMovementController` exposto pela surface.

### 2. Identity player-specific é obrigatória para receiver atual

Enquanto o receiver atual for player-specific, a identity deve vir de contrato explícito:

- `PlayerActorIdentity`;
- `PlayerActorId`;
- `PlayerSlotId`.

Não é permitido fallback para `target.ActorId`.

### 3. Missing identity deve ser observável

Se houver endpoint player-specific mas `PlayerActorIdentity` estiver ausente/inválido:

- não emitir `PermissionTarget`;
- registrar evento/unresolved observável com `reason='player_identity_missing'`;
- não fabricar identity.

### 4. Runtime mantém reação local

O pipeline publica permission state:

- `Blocked`;
- `Allowed`;
- `Unbound`.

O receiver local aplica a reação concreta.

---

## Resultado atual

Checkpoint H4D-B2:

- `PermissionTarget:1` preservado nas entries esperadas;
- `MovementBindingCompleted` preservado;
- `MovementBindingRetained` preservado na `activity_02`;
- sem `PermissionTargetIdentityUnresolved` em cenário válido;
- sem fallback para `target.ActorId`.

---

## Débitos

- Generalizar PermissionTarget para actors não-player quando houver receivers reais.
- Reduzir `playerActorId/playerSlotId` quando movement/permission forem generalizados.
