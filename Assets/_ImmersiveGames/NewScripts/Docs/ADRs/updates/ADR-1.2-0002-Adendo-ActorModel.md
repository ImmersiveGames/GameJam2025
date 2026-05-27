# Adendo para ADR-1.2-0002 — NonPlayerActor alinhado ao ADR-1.2-0008

Adicionar ao final do ADR-1.2-0002.

---

## Adendo normativo — NonPlayerActor como especialização de Actor

Após o ADR-1.2-0008, `NonPlayerActor` é tratado como especialização concreta/transitória de `Actor`, não como lifecycle paralelo permanente.

O MVP de `NonPlayerActor` continua válido para:

```text
scene-authored actors
ActivityScoped actors
RouteScoped actors
ActorPresentation MVP
participation por Activity entry
```

Mas sua nomenclatura não autoriza novos trilhos separados para:

```text
NonPlayerActorAttributes
NonPlayerActorMovement
NonPlayerActorInteraction
NonPlayerActorAI
NonPlayerActorCombat
```

Novas funções devem convergir para:

```text
ActorCapability
ActorEndpoint
ActorCapabilitySurface
ActivityCapabilityInventory
runtime reference tipada
```

`NpcActor`, `EnemyActor` e outros atores não-player futuros podem herdar ou especializar `NonPlayerActor`, mas o pipeline deve consumir contratos de `Actor`/capability, não branches por `ActorKind`.

`ActorKind='NonPlayer'` permanece aceitável para log/metadata durante a migração. Não é contrato de lifecycle.
