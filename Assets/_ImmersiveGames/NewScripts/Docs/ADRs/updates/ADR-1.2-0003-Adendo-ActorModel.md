# Adendo para ADR-1.2-0003 — Typed Identity alinhado ao ADR-1.2-0008

Adicionar ao final do ADR-1.2-0003.

---

## Adendo normativo — ActorKind/ActorRole não substituem typed identity

Após o ADR-1.2-0008, reforça-se:

```text
ActorKind e ActorRole são metadata, não identidade funcional principal.
```

Decisões de lifecycle, ownership, capability readiness, release, reset, snapshot ou participation devem usar identidades tipadas ou runtime references tipadas.

Não permitido:

```text
parsear actorInstanceRuntimeId para recuperar lifecycle
usar ActorKind como switch central de setup/release
usar ActorRole como substituto de ActorInstanceId
usar string de componentPath como identidade primária de actor
```

Permitido:

```text
usar ActorKind/ActorRole em logs
usar ActorKind/ActorRole em observabilidade
usar ActorKind/ActorRole como metadata de authoring/policy
usar IDs textuais estáveis em bordas de log/save/snapshot
```

A direção final para Actor é:

```text
ActorDefinitionId / ActorDefinitionRef
ActorInstanceId
ActorCapabilityId
runtime reference tipada do endpoint
```

Texto derivado pode existir para display/correlação, mas não deve ser contrato funcional recuperado por parsing.
