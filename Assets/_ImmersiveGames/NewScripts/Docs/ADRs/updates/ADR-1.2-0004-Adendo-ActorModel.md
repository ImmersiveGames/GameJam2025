# Adendo para ADR-1.2-0004 — ActorAttributes alinhado ao ADR-1.2-0008

Adicionar ao final do ADR-1.2-0004 antes de continuar a migração de Attributes.

---

## Adendo normativo — ActorAttributes como capability de Actor tipado

Após o ADR-1.2-0008, `ActorAttributes` deve convergir para capability genérica de `Actor`.

Regra:

```text
ActorAttributeEndpoint pertence ao Actor que expõe a capability.
Não pertence a um trilho final NonPlayerActorAttribute*.
```

O MVP atual pode manter logs/métodos transitórios `NonPlayerActorAttribute*`, mas isso não é shape final.

Próximas migrações devem evitar:

```text
NonPlayerActorAttributeSetupStage como modelo final
PlayerActorAttributeStage paralelo
ObjectActorAttributeStage paralelo
switch ActorKind para aplicar atributo
```

Direção correta:

```text
ActorCapabilitySurface / ActorAttributeEndpoint
-> ActivityCapabilityInventory
-> ActorAttribute runtime reference
-> ActorAttribute setup/release genérico
-> mutation local no ActorAttributeEndpoint
```

O pipeline pode decidir setup/release/reset timing e validar identity/readiness no boundary MVP/QA.

Gameplay moment-to-moment futuro, como dano/heal/stamina, deve preferir relação local entre endpoints/capabilities, não roteamento global obrigatório via pipeline.
