# Adendo para ADR-1.2-0007 — Capability Inventory alinhado ao ADR-1.2-0008

Adicionar ao final do ADR-1.2-0007.

---

## Adendo normativo — ActorCapabilitySurface como fonte preferida de endpoints

Após o ADR-1.2-0008, `ActivityCapabilityInventory` deve preferir contracts/endpoints expostos por `ActorCapabilitySurface` ou fonte autorizada equivalente.

Direção:

```text
Actor / ActorCapabilitySurface
-> endpoint/capability local
-> scanner pequeno por módulo
-> ActivityCapabilityInventory
-> runtime reference tipada
-> stage consome seção do inventory
```

O inventory não deve depender de switches por `ActorKind` para descobrir capabilities.

`ActorKind`, `ActorRole`, `ActorScope` podem ser copiados para descriptor/log/metadata, mas não devem dirigir discovery funcional quando a capability pode ser descoberta por contrato local.

Regras adicionais:

1. `ActivityCapabilityInventory` não é owner de lifecycle.
2. `ActorInventoryFeed` e `ActorScanTarget` são ponte/read-model, não registry funcional final.
3. `ActorCapabilitySurface` não auto-binda e não executa lifecycle.
4. Runtime references continuam separadas dos descriptors.
5. Descriptor permanece descritivo; endpoint concreto fica em runtime reference tipada.
6. Ausência obrigatória de endpoint é fail-fast; ausência opcional é skip explícito.
7. Não criar scanner separado por `PlayerActor`, `NonPlayerActor`, `EnemyActor` ou `ObjectActor` quando a capability puder ser descoberta por contrato.
