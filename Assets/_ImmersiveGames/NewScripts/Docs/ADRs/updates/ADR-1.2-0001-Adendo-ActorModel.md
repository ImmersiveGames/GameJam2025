# Adendo para ADR-1.2-0001 — ActorPresentation alinhado ao ADR-1.2-0008

Adicionar ao final do ADR-1.2-0001.

---

## Adendo normativo — ActorPresentation sobre Actor tipado

Após o ADR-1.2-0008, `ActorPresentation` deve ser lido como capability de `Actor` tipado/concreto.

Regra atualizada:

```text
ActorPresentation não pertence a trilho separado de PlayerActor ou NonPlayerActor.
ActorPresentation pertence ao Actor que expõe ActorPresentationEndpoint.
```

`PlayerActor`, `NpcActor`, `EnemyActor`, `ObjectActor`, `InteractiveActor` e demais especializações podem possuir `ActorPresentation`, desde que exponham endpoint/capability compatível.

`ActorKind` e `ActorRole` podem aparecer em logs e metadata, mas não devem dirigir o lifecycle de setup/release por `if/switch` central.

A direção final é:

```text
ActorCapabilitySurface / ActorPresentationEndpoint
-> ActivityCapabilityInventory
-> PresentationEndpointReference
-> ActorPresentation setup/release genérico
```

O checkpoint recente de Base 1.2 validou que setup e release de `ActorPresentation` podem operar genericamente por `Actor`:

```text
ActivityCapabilityInventory
-> PresentationEndpointReference
-> ActorPresentationSetupFromInventoryStage
-> active ActorPresentation handles
-> ActorPresentationReleaseGenericStage
```

Qualquer menção histórica a `NonPlayerActorPresentation*` deve ser lida como evidência de MVP/transição, não como shape final.
