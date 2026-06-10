# ACTOR-CAPACITY-0A — ActorCapabilitySurface homogênea e lifecycle contributions

## Status

`DOCUMENTATION ONLY / READY FOR REVIEW`

Este documento congela a decisão arquitetural antes de qualquer implementação runtime.

Nenhum arquivo de runtime deve ser alterado neste corte. O próximo passo permitido é criar contratos passivos de contribution, sem trocar o caminho ativo.

---

## Objetivo

Redesenhar a fronteira entre `Actor`, `ActorCapabilitySurface`, capabilities locais, reset, snapshot, restore, release e persistência para chegar a um shape mais modular e saudável:

```text
Actor = composição de capabilities
Capability = dona do estado e comportamento local
ActorCapabilitySurface = índice homogêneo/passivo de endpoints e contributions
ActivityEntry/Exit stages = donos de quando, ordem, required/optional, skip/fail
SaveRuntime/Persistence = dono de persistência física, não a capability
```

---

## Problema observado

A auditoria do código atual mostrou que `ActorCapabilitySurface` ainda está concreta demais. Ela conhece capacidades específicas como:

```text
PresentationEndpoint
AttributeEndpoint
ActorCameraTargetEndpoint
ActorMovementEndpoint
ActorPermissionReceiver
ActorCommandSourceHub
ActorProjectileFireEndpoint
```

Isso cria um catálogo crescente de capacidades possíveis dentro da surface.

O problema não é existir uma surface. O problema é a surface conhecer nominalmente cada capability nova.

### Sintomas

```text
Nova capability exige alteração na ActorCapabilitySurface.
Scanners consomem propriedades concretas da surface.
ActivityCapabilityScanResult possui lanes fixas por tipo concreto de contribution.
Reset de Actor ainda usa endpoint wrapper em vez da própria capability resetar seu estado.
Snapshot/restore/release ainda não possuem modelo modular equivalente para Actor capabilities.
Save persiste payload agregado, não records modulares por capability.
```

---

## Decisão central

Manter `ActorCapabilitySurface`, mas reduzir sua responsabilidade para índice local homogêneo.

A surface não deve ser:

```text
um catálogo de todas as capabilities possíveis;
um service locator genérico;
um owner de lifecycle;
um executor de setup/binding/reset/save;
um ponto que decide required/optional;
um lugar que implementa comportamento das capabilities.
```

A surface deve ser:

```text
um índice local/passivo de endpoints e contribution providers;
um ponto estável de discovery do Actor;
um cache técnico local para evitar scan espalhado;
um contrato homogêneo que alimenta projection/plans da ActivityEntry.
```

---

## Shape alvo

### Camada 1 — Capability local

Cada capability mantém o próprio estado e declara as fases que suporta.

Exemplos:

```text
PlayerMovementController
- mantém estado de movimento;
- declara reset de MovementTransient;
- aplica movimento localmente.

ActorProjectileFireEndpoint
- mantém cooldown/fire state;
- declara command sink;
- declara reset se houver estado transitório;
- solicita spawn por adapter.

ActorAttributeEndpoint
- mantém attributes;
- pode declarar snapshot/restore/reset se o estado for persistível/resetável.
```

### Camada 2 — Contribution providers

Capabilities declaram suas capacidades por interfaces pequenas e homogêneas:

```text
IActorSetupContributionProvider
IActorBindingContributionProvider
IActorPermissionReceiverContributionProvider
IActorResetContributionProvider
IActorSnapshotContributionProvider
IActorRestoreContributionProvider
IActorReleaseContributionProvider
```

Essas interfaces declaram o que a capability oferece. Elas não decidem quando executar.

### Camada 3 — ActorCapabilitySurface homogênea

A surface indexa providers/contributions:

```text
ActorCapabilitySurface
- GetProviders<TProvider>()
- GetContributions<TContribution>()
- TryGetSingleEndpoint<TEndpoint>() quando houver contrato forte
```

Regras:

```text
queries devem ser tipadas;
sem lookup textual;
sem fallback silencioso;
sem execução de lifecycle;
sem decisão de requiredness;
sem branch player/nonplayer.
```

### Camada 4 — Projection / Plans

A ActivityEntry projeta as contributions em planos:

```text
SetupPlan
BindingPlan
Gate/PermissionPlan
ResetPlan
SnapshotPlan
RestorePlan
ReleasePlan
```

A projection decide agrupamento técnico. A policy/stage decide required/optional, skip/fail e ordenação.

### Camada 5 — Persistence

Capabilities não salvam diretamente.

Fluxo correto:

```text
Capability -> SnapshotContribution
Stage -> SnapshotEnvelope
Pipeline/SavePolicy -> SaveCommand
SaveAdapter -> ISaveService/SaveRuntime
Backend técnico -> persistência física
```

Proibido:

```text
Capability.Save()
MovementController.SaveToDisk()
AttributeEndpoint.WritePlayerPrefs()
SnapshotProvider chamar ISaveService diretamente
```

---

## Reset, Snapshot, Restore, Release

### Reset

Regra:

```text
A capability que possui o estado resetável deve saber resetar esse estado.
```

Exemplo desejado:

```text
PlayerMovementController implementa reset de MovementTransient.
ActorProjectileFireEndpoint implementa reset de cooldown/fire state, se aplicável.
ActorAttributeEndpoint implementa reset de attributes, se aplicável.
```

`ResetEndpoint` separado só deve existir quando houver motivo concreto. Não deve existir apenas como wrapper que redireciona para a capability real.

### Snapshot

Regra:

```text
A capability que possui estado snapshotável deve produzir seu snapshot local.
```

Ela não decide se o snapshot será salvo. Ela apenas produz payload local versionado.

### Restore

Regra:

```text
A capability que produziu/entende o snapshot deve saber restaurar seu estado local.
```

O stage decide quando aplicar o restore.

### Release

Regra:

```text
A capability que possui recurso local liberável deve saber liberar/limpar esse recurso.
```

O stage decide quando liberar.

---

## Fronteiras de ownership

| Categoria | Owner correto |
|---|---|
| Actor runtime identity | `Actor` / materialization owner |
| Estado local da capability | própria capability |
| Declaração de endpoint/contribution | própria capability |
| Índice local dos endpoints/contributions | `ActorCapabilitySurface` |
| Required/optional/fail/skip | stage/policy |
| Ordem de setup/binding/reset/restore | `ActivityEntryPipeline` e stages |
| Ordem de snapshot/release/exit | `SessionActivityPipeline` ou exit stages/pipeline futuro |
| Persistência física | `SaveRuntime` / adapters |
| Logs/facts | fact/recorder, sem side-effect |

---

## Antiobjetivos

Não fazer:

```text
não remover ActorCapabilitySurface;
não transformar ActorCapabilitySurface em service locator;
não criar manager/coordinator genérico;
não criar ResetManager;
não deixar capability chamar SaveRuntime;
não criar fallback textual;
não comparar ActorId, PlayerActorId, PlayerSlotId e ActorInstanceRuntimeId como equivalentes;
não reabrir ActivityCapabilityInventory como catálogo geral de behavior;
não criar branches player/nonplayer;
não migrar tudo em um corte grande.
```

---

## Estado atual auditado

| Área | Estado atual | Problema | Direção |
|---|---|---|---|
| `ActorCapabilitySurface` | Campos/propriedades concretas por capability | Catálogo crescente | Índice homogêneo de providers/contributions |
| Scanners de Actor | Leem propriedades concretas da surface | Acoplamento por capability | Ler contributions por fase |
| `ActivityCapabilityScanResult` | Lanes fixas por contribution concreta | Projection não homogênea | Coleções por phase/contrato |
| Actor Reset | `PlayerActorDefaultResetEndpoint` agrega grupos diferentes | Wrapper artificial | Capability específica declara reset |
| Movement reset | Estado real está no `PlayerMovementController` | Reset externo só encaminha | `PlayerMovementController` declara reset contribution |
| ActivityObject lifecycle | Já usa interfaces homogêneas | Ainda inventory/targetId e wrappers | Evoluir para lifecycle contribution provider |
| SaveRuntime | Persistência separada corretamente | Payload agregado, não modular | Snapshot envelope modular por capability |

---

## Plano de implementação

### ACTOR-CAPACITY-0A — Documento de decisão

Status deste documento.

Objetivo:

```text
Congelar a decisão antes de runtime.
```

Aceite:

```text
sem alteração de runtime;
documento criado;
plano aprovado.
```

---

### ACTOR-CAPACITY-1A — Passive capability contribution contracts

Objetivo:

```text
Criar contratos passivos para contributions de Actor capabilities.
```

Escopo permitido:

```text
IActorSetupContributionProvider
IActorBindingContributionProvider
IActorPermissionReceiverContributionProvider
IActorResetContributionProvider
IActorSnapshotContributionProvider
IActorRestoreContributionProvider
IActorReleaseContributionProvider
contratos de contribution mínimos
sem troca de fluxo ativo
```

Fora do escopo:

```text
não alterar scanners ativos;
não alterar reset ativo;
não alterar save;
não alterar ActivityObject ainda;
não remover propriedades concretas da surface.
```

Risco: baixo.

---

### ACTOR-CAPACITY-1B — Homogeneous ActorCapabilitySurface index

Objetivo:

```text
Fazer ActorCapabilitySurface coletar/indexar providers e contributions de forma homogênea.
```

Escopo:

```text
GetProviders<TProvider>()
GetContributions<TContribution>()
cache técnico local por tipo
queries tipadas
observabilidade de contributions indexadas
```

Fora do escopo:

```text
não remover propriedades concretas no mesmo corte;
não trocar todos os scanners;
não criar service locator;
não executar lifecycle na surface.
```

Risco: médio.

---

### ACTOR-CAPACITY-2A — Movement reset contribution

Objetivo:

```text
Migrar MovementTransient reset para a própria capability de movement.
```

Escopo:

```text
PlayerMovementController declara reset contribution/endpoint para MovementTransient.
PlayerActorDefaultResetEndpoint deixa de ser owner do reset de MovementTransient.
Reset continua chamado pelo fluxo canônico atual.
```

Critério:

```text
ActorResetQaApplied preservado;
MovementBindingCompleted preservado;
MovementControlEnabled/Disabled preservado;
RestartCurrentActivity PASS;
Activity01ToActivity02 PASS;
RouteExitBackToMenu PASS;
sem FATAL;
sem Exception;
sem route_transition_failed;
sem foreign/stale indevido.
```

Risco: médio.

---

### ACTOR-CAPACITY-2B — Actor-level reset split

Objetivo:

```text
Separar reset de actor-level e reset de capability-level.
```

Direção:

```text
Placement = actor/materialization/placement contribution.
ActivityParticipation = participation state contribution.
MovementTransient = movement capability contribution.
```

Critério:

```text
PlayerActorDefaultResetEndpoint removido, reduzido ou documentado como bridge curta;
nenhum grupo de reset fica agregado artificialmente sem motivo;
sem regressão de reset QA.
```

Risco: médio/alto.

---

### ACTOR-CAPACITY-2C — Actor reset resolution via lifecycle contributions

Objetivo:

```text
Resolver reset de Actor via surface/projection, não por scan direto de MonoBehaviours.
```

Trocar:

```text
GetComponentsInChildren<MonoBehaviour>() is IActorResetEndpoint
```

por:

```text
ActorCapabilitySurface lifecycle contributions
```

Critério:

```text
sem actor_reset_endpoint_missing indevido;
sem fallback direto;
owner visível no log;
smoke macro preservado.
```

Risco: alto.

---

### ACTOR-CAPACITY-3A — Passive actor snapshot/restore/release contribution contracts

Objetivo:

```text
Adicionar contratos passivos de Snapshot/Restore/Release equivalentes ao modelo de Reset.
```

Escopo:

```text
IActorSnapshotContributionProvider
IActorRestoreContributionProvider
IActorReleaseContributionProvider
ActorCapabilitySnapshotRecord conceitual/passivo
schemaId/schemaVersion/payload local
sem persistência física ainda
```

Risco: médio.

---

### ACTIVITY-OBJECT-CAPACITY-1A — Object lifecycle contribution provider

Objetivo:

```text
Evoluir ActivityObject lifecycle para providers/contributions homogêneos.
```

Escopo:

```text
IActivityObjectResetContributionProvider
IActivityObjectSnapshotContributionProvider
IActivityObjectRestoreContributionProvider
IActivityObjectReleaseContributionProvider
ActivityObjectTransformSnapshotProvider como primeiro exemplo real
```

Fora do escopo:

```text
não misturar Actor reset com ActivityObject reset;
não alterar SaveRuntime;
não trocar targetId sem auditoria própria.
```

Risco: médio.

---

### CAPACITY-SNAPSHOT-1A — Modular snapshot envelope

Objetivo:

```text
Criar envelope modular para snapshots de capability/object/actor.
```

Shape conceitual:

```text
ActivitySnapshotEnvelope
- activityId
- entrySequence
- records[]
  - ownerKind
  - ownerId
  - actorInstanceRuntimeId?
  - objectId?
  - capabilityId
  - schemaId
  - schemaVersion
  - payload
```

Sem alterar backend.

Risco: médio/alto.

---

### CAPACITY-PERSISTENCE-1A — Save adapter consumes modular snapshot envelope

Objetivo:

```text
Save continua fora das capabilities e passa a persistir envelope modular.
```

Fluxo:

```text
Capability -> SnapshotContribution
Stage -> SnapshotEnvelope
Pipeline/SavePolicy -> SaveCommand
SaveAdapter -> ISaveService
```

Risco: alto. Só executar após snapshot envelope validado.

---

## Ordem recomendada

```text
ACTOR-CAPACITY-0A
ACTOR-CAPACITY-1A
ACTOR-CAPACITY-1B
ACTOR-CAPACITY-2A
ACTOR-CAPACITY-2B
ACTOR-CAPACITY-2C
ACTOR-CAPACITY-3A
ACTIVITY-OBJECT-CAPACITY-1A
CAPACITY-SNAPSHOT-1A
CAPACITY-PERSISTENCE-1A
```

---

## Smoke mínimo para cortes runtime

Após qualquer corte runtime desta frente:

```text
Boot -> Menu
Menu -> Sandbox
Activity 01 entry
CompleteActivationWindow
QA Reset Current Player Actor quando aplicável
RestartCurrentActivity
CompleteActivationWindow novamente
CompleteCurrentActivity
Activity 01 -> Activity 02
Activity 02 ActivityRunning
BackToMenu / RouteExit
```

Checkpoints mínimos:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
ActorResetQaApplied quando o corte tocar reset
MovementBindingCompleted preservado
MovementControlEnabled/Disabled preservado
CameraBindingCompleted preservado quando aplicável
ActivityObjectReset PassedApplied/PassedNoCommands preservado quando aplicável
sem fallback silencioso
sem trilho paralelo novo
owner correto visível nos logs
```

---

## Critério de aceite arquitetural

Um corte desta frente só pode ser aceito como PASS arquitetural se confirmar:

```text
ActorCapabilitySurface não decide lifecycle;
capability mantém estado local;
capability declara contributions por contrato homogêneo;
pipeline/stage decide quando executar;
policy decide skip/fail/required/optional;
adapter executa side-effect técnico quando houver;
SaveRuntime é chamado apenas por adapter/pipeline autorizado;
capability não chama SaveRuntime;
sem fallback textual;
sem registry paralelo;
sem manager/coordinator genérico;
sem branch player/nonplayer;
sem ActivityCapabilityInventory como catálogo geral de behavior.
```

---

## Conclusão

A direção final congelada por este documento é:

```text
Actor = composição de capabilities.
Capability = dona do estado/comportamento local.
ActorCapabilitySurface = índice homogêneo/passivo de contributions.
ActivityEntry/Exit stages = donos do quando/ordem/fail/skip.
SaveRuntime/Persistence = dono da persistência física.
```

A primeira implementação recomendada é:

```text
ACTOR-CAPACITY-1A — Passive capability contribution contracts
```

A primeira prova funcional recomendada é:

```text
ACTOR-CAPACITY-2A — Movement reset contribution
```
