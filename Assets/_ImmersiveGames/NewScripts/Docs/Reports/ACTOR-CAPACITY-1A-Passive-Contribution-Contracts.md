# ACTOR-CAPACITY-1A — Passive capability contribution contracts

## Status

`IMPLEMENTED / PENDING COMPILE + SMOKE`

Este corte cria apenas contratos passivos para o modelo de `ActorCapabilitySurface` homogênea e lifecycle contributions.

Não altera o caminho runtime ativo.

---

## Objetivo

Preparar a Base 2.0 para migrar de:

```text
ActorCapabilitySurface com propriedades concretas por capability
```

para:

```text
ActorCapabilitySurface como índice local homogêneo de contribution providers
```

Sem trocar ainda scanners, reset, snapshot, restore, release, save ou persistence.

---

## Arquivo criado

```text
NewScripts/Actors/Capabilities/Contracts/ActorCapabilityContributionContracts.cs
```

---

## Contratos criados

### Identidade de capability

```text
ActorCapabilityId
```

Value object tipado para identificar uma capability local sem depender de string solta no runtime futuro.

### Fase da contribution

```text
ActorCapabilityContributionPhase
- Setup
- Binding
- PermissionReceiver
- Reset
- Snapshot
- Restore
- Release
```

### Requiredness declarada

```text
ActorCapabilityContributionRequirement
- Optional
- Required
```

Observação: requiredness declarada é metadata da contribution. A decisão final de bloquear, pular ou falhar continua pertencendo a stage/policy.

### Contexto passivo

```text
ActorCapabilityContributionContext
```

Carrega a identidade runtime resolvida do actor durante scan/projection:

```text
SessionActivityIdentity
ActorId
ActorInstanceRuntimeId
ActorKind
ActorRole
ActorScope
componentPath
source
reason
```

### Descriptor comum

```text
ActorCapabilityContributionDescriptor
```

É o header homogêneo de qualquer contribution.

### Interfaces de contribution

```text
IActorCapabilityContribution
IActorSetupContribution
IActorBindingContribution
IActorPermissionReceiverContribution
IActorResetContribution
IActorSnapshotContribution
IActorRestoreContribution
IActorReleaseContribution
```

### Interfaces de provider

```text
IActorSetupContributionProvider
IActorBindingContributionProvider
IActorPermissionReceiverContributionProvider
IActorResetContributionProvider
IActorSnapshotContributionProvider
IActorRestoreContributionProvider
IActorReleaseContributionProvider
```

Essas interfaces permitem que a própria capability declare o que oferece, sem que a `ActorCapabilitySurface` precise conhecer nominalmente cada capability possível.

---

## O que este corte não faz

```text
não altera ActorCapabilitySurface;
não remove propriedades concretas da surface;
não altera scanners ativos;
não altera ActivityCapabilityScanResult;
não altera Reset ativo;
não altera PlayerActorDefaultResetEndpoint;
não altera PlayerMovementController;
não altera Snapshot/Restore/Release ativo;
não altera SaveRuntime;
não altera ActivityObject;
não cria manager/coordinator;
não cria fallback;
não cria runtime path paralelo.
```

---

## Por que não remover legado neste corte

A regra do projeto é remover trilhos superados sempre que possível. Aqui, porém, os trilhos antigos ainda são o caminho ativo de smoke.

Remover `ActorCapabilitySurface.PresentationEndpoint`, `ActorMovementEndpoint`, `ActorProjectileFireEndpoint` ou scanners concretos agora quebraria o runtime sem que o novo index homogêneo estivesse ativo.

Classificação:

```text
contratos novos = comportamento alvo passivo;
propriedades concretas atuais = legado transitório ainda ativo;
remoção = permitida somente depois de ACTOR-CAPACITY-1B/2A+ quando houver substituto runtime validado.
```

---

## Ownership congelado

| Categoria | Owner correto |
|---|---|
| Estado local da capability | A própria capability |
| Declaração de contribution | A própria capability via provider |
| Indexação local | `ActorCapabilitySurface` futura homogênea |
| Required/optional/fail/skip | Stage/policy |
| Execução de reset/snapshot/restore/release | Stage chama endpoint/contribution local |
| Persistência física | `SaveRuntime` / adapter autorizado |

---

## Próximo corte recomendado

```text
ACTOR-CAPACITY-1B — Homogeneous ActorCapabilitySurface index
```

Objetivo:

```text
Adicionar index/cache homogêneo de providers/contributions à ActorCapabilitySurface, mantendo propriedades concretas antigas apenas como transição curta.
```

Depois disso, a primeira prova runtime recomendada continua sendo:

```text
ACTOR-CAPACITY-2A — Movement reset contribution
```

---

## Critério para aceitar PASS

Como este corte é contrato passivo, o mínimo é:

```text
compile sem erros CS;
sem alteração comportamental esperada;
smoke curto opcional apenas para confirmar ausência de regressão de boot/entry.
```

Não aceitar como PASS se houver:

```text
erro CS;
ambiguidade de namespace;
contrato chamando SaveRuntime;
contrato executando side-effect;
contrato decidindo lifecycle;
manager/coordinator novo;
comparação textual entre domínios de identity.
```
