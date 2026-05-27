# Adendo para ADR-1.2-0006 — ActivityCapabilityPermission alinhado ao ADR-1.2-0008

Adicionar ao final do ADR-1.2-0006.

---

## Adendo normativo — Permissions por capability, não por ActorKind

Após o ADR-1.2-0008, `ActivityCapabilityPermission` deve ser entendida como permissão semântica aplicada a capabilities/endpoints, não a categorias de actor.

Correto:

```text
Pipeline publica ActivityCapabilityPermissionCommand com identity.
Permission runtime valida active identity.
Receivers/endpoints locais reagem conforme capability.
```

Incorreto:

```text
if ActorKind == Player -> enable movement
if ActorKind == Enemy -> pause brain
if ActorKind == Object -> enable interaction
```

A capability local deve expor receiver/endpoint apropriado.

Exemplo de movement:

```text
PlayerInputMovementIntentProvider
AIMovementIntentProvider
ScriptedMovementIntentProvider
-> ActorMovementController / ActorMovementEndpoint
-> permission local reaction
```

O pipeline decide quando a Activity permite a capability. O endpoint decide como reagir localmente.
