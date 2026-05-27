# ADR-1.2-0004 — ActorAttributes como ActorCapability

Status: Accepted / Base 1.2  
Área: ActorAttributes / ActorCapability / SessionActivity  
Atualização: 4C + H4D-B3 — CLOSED / PASS

---

## Contexto

ActorAttributes começou preso ao trilho de `NonPlayerActorAttribute*`. Isso contradizia a modelagem de Base 1.2, onde attributes são capability local de Actor, não propriedade exclusiva de NonPlayer.

---

## Decisão

### 1. ActorAttribute é capability local de Actor

`ActorAttributeEndpoint` pertence ao Actor e é exposto por `ActorCapabilitySurface`.

Qualquer Actor pode expor attributes:

- PlayerActor;
- NonPlayerActor;
- EnemyActor;
- ObjectActor;
- outros actors futuros.

### 2. Setup/release são decididos pelo SessionActivityPipeline

O endpoint executa comportamento local. O pipeline decide quando setup/release ocorre.

### 3. Caminho nominal

O caminho nominal deve usar:

- `ActivityCapabilityInventory`;
- `ActorScanTarget`;
- `ActorCapabilitySurface`;
- `ActorAttributeEndpointReference`.

Eventos genéricos aceitos:

- `ActorAttributeSetupStarted`;
- `ActorAttributeProfileResolved`;
- `ActorAttributeReady`;
- `ActorAttributeSetupSkipped`;
- `ActorAttributeSetupFailed`;
- `ActorAttributeSetupCompleted`;
- `ActorAttributeReleaseStarted`;
- `ActorAttributeReleased`;
- `ActorAttributeReleaseSkipped`;
- `ActorAttributeReleaseFailed`;
- `ActorAttributeReleaseCompleted`.

### 4. Store ativo por ActorInstanceId

O store ativo deve ser keyed por `ActorInstanceId`.

Não usar store nominal por `NonPlayerId`.

Checkpoint H4D-B3:

- `_activeActorAttributeCapabilitiesByActorInstanceId`;
- `ActorAttributeCapabilityState`;
- readiness de participation consulta por `ActorInstanceId`;
- release itera/remover por `ActorInstanceId`.

### 5. Proibições

Não usar:

- `ActorKind` para decidir store;
- parsing de `actorId`;
- parsing de `actorInstanceRuntimeId`;
- fallback por GameObject/path/scene;
- trilho `NonPlayerActorAttribute*` no caminho nominal.

---

## Resultado atual

Checkpoint aceito:

- `AttributeEndpoint 2/2/1`;
- `ActorAttributeSetupCompleted 2/2/1`;
- `ActorAttributeReleaseCompleted` nos releases;
- `ActorParticipation` usa readiness de attributes sem depender de store `NonPlayerId`;
- sem `NonPlayerActorAttribute*` no caminho nominal.

---

## Débitos

- Remover ou renomear eventuais nomes históricos restantes quando comprovadamente mortos.
- Reduzir metadata `actorKind` em logs se virar ruído, mantendo-a aceitável como observabilidade.
