# ADR-2.0-0002 â€” SessionActivity Ownership Decomposition e ActivityEntryPipeline

## Status

Aceito / congelado incrementalmente.
Ultimo checkpoint real consolidado: `SA-14B1 - ActivityObject exit correlation explicit entry result CLOSED / PASS funcional + PASS arquitetural do corte`.
O estado superior agora reflete o fechamento funcional/documental de SA-14B1 e nao deve contradizer os cortes posteriores ja registrados neste ADR.

## Ãrea

`SessionActivity` / `ActivityEntryPipeline` / `RouteExit teardown` / `ActivityCapability` / `Actor participation`

## Contexto

A auditoria de `SessionActivity` identificou que o fluxo ainda nÃ£o atende ao objetivo da Base 2.0. O problema principal nÃ£o Ã© apenas tamanho de arquivo: o `SessionActivityPipeline` concentra decisÃµes e execuÃ§Ã£o de lifecycle, transition, content load/release, snapshot, route-exit teardown, visual readiness, actor setup, capability setup e permission handling.

A auditoria tambÃ©m identificou que `ActivityEntryPipeline` existe apenas como contrato/boundary, sem pipeline concreto, e que o lifecycle de `RouteExit teardown` possui owner duplicado entre `SessionActivityHost` e `SessionActivityPipeline`.

Base 2.0 nÃ£o deve transformar um pipeline grande em outro componente grande. A regra anti-deslocamento do `SessionOperational` passa a valer tambÃ©m para `SessionActivity`: nenhuma extraÃ§Ã£o Ã© aceita apenas para reduzir tamanho; toda extraÃ§Ã£o precisa ter owner, categoria e critÃ©rio de aceite.

## Problema

O estado atual gera estes riscos:

1. `SessionActivityPipeline` permanece como god object.
2. `ActivityEntryPipeline` ainda nÃ£o Ã© owner real do entry lifecycle.
3. `RouteExit teardown` tem decisÃ£o/estado duplicados entre Host e Pipeline.
4. `SessionActivityHost` mistura boundary externo, decisÃ£o de flow e registro global via service locator.
5. DuplicaÃ§Ã£o de listas/policies de stages pode gerar branch drift.
6. Permission target mistura domÃ­nios de identidade (`PlayerActorId`, `PlayerSlotId`, receiver tÃ©cnico) em um mesmo campo.
7. QA/hardcodes podem continuar mascarando contrato real.

## DecisÃ£o

### 1. `SessionActivityPipeline` permanece como owner macro

`SessionActivityPipeline` Ã© owner de:

```text
session activity lifecycle macro
activity-to-activity transition policy
restart current activity lifecycle
route-exit handoff recebido do SessionOperational
ordem macro Entry -> ActivationWindow -> ActivityRunning -> Completion -> DeactivationWindow -> Exit/Next
proteÃ§Ã£o foreign/stale da sessÃ£o ativa
```

Ele pode chamar pipelines/stages concretos explicitamente.

Ele nÃ£o deve executar diretamente:

```text
ActivityContent load/prepare/release
Actor setup/readiness/release
Object setup/reset/restore/release
Input/Movement/Camera binding concreto
Inventory/capability discovery concreto
Snapshot capture/restore concreto
Permission receiver reaction concreta
Unity side-effects
```

### 2. `ActivityEntryPipeline` vira pipeline concreto

`ActivityEntryPipeline` Ã© owner do lifecycle determinÃ­stico de uma `ActivityEntry`.

Ele recebe um comando de entrada com payload runtime jÃ¡ resolvido:

```text
ActivityEntryCommand
- PipelineIdentity
- RouteIdentity
- SessionActivityIdentity
- ActivityId
- EntrySequence
- Activity asset/profile resolvido
- ActivityContentProfile resolvido quando houver
- Player/Actor participation context resolvido
- Route-scoped context necessÃ¡rio
```

Ele produz resultado/facts:

```text
ActivityEntryResult
- Completed
- SkippedNoContent
- Failed
- RejectedStaleOrForeign
- BlockedByRequiredCapability
```

Ele Ã© owner de:

```text
ActivityContent prepare/load readiness
ActivityCapabilityInventory preview/resolution
Actor scan targets e capability discovery por entry
Actor/object setup readiness
Object reset/restore de entry
Actor readiness dentro da Activity
Input/Movement/Camera binding readiness
Permission target discovery/preparation
Entry readiness antes de ActivationWindow/ActivityRunning
```

Ele nÃ£o decide:

```text
qual Ã© a prÃ³xima Activity
quando a Activity termina
quando a ActivationWindow Ã© completada pelo usuÃ¡rio/QA
quando a DeactivationWindow Ã© completada
quando a rota troca
save/progression global
route operation lifecycle
```

### 2A. `ENTRY-BOUNDARY-DOC-0`

This section freezes the active Base 2.0 boundary for `SessionActivity`.
It supersedes transitional wording that treated inventory-backed setup/binding as an allowed final shape.

#### Raw Scan

```text
Raw scan discovers components, behaviors and local endpoints.
Raw scan does not decide requiredness.
Raw scan does not perform setup.
Raw scan does not perform binding.
```

#### ActorCapabilitySurface

```text
ActorCapabilitySurface is the technical local index of the Actor.
ActorCapabilitySurface does not decide lifecycle.
ActorCapabilitySurface does not decide requiredness.
ActorCapabilitySurface does not replace setup or binding plans.
```

#### Projection

```text
Projection transforms local discovery into:
- TransversalContractContribution
- SetupContribution
- BindingContribution
- LifecycleRecordContribution
```

#### Transversal Inventory

```text
Transversal Inventory contains only:
- Gate / Permission
- Reset
- Snapshot / Restore
- Release / Teardown
```

```text
Transversal Inventory does not contain Presentation, Attributes, CameraTarget, ObjectEmitter, Movement, CommandSink, Dash, Attack, Interact or AI.
Transversal Inventory is not a general Actor catalog.
Transversal Inventory is not a service locator.
Transversal Inventory is not setup.
Transversal Inventory is not binding.
```

#### Setup Plan

```text
Setup Plan contains presentation setup, attribute setup, actor local setup and retained setup.
```

#### Binding Plan

```text
Binding Plan contains player input binding, command sink binding, movement binding, camera binding and permission receiver binding.
```

#### Runtime State

```text
Runtime State stores correlation for exit, teardown, release, snapshot and continuity.
Runtime State does not become the source of truth for setup or binding.
```

#### Current normative invariants

```text
No behavior is required by default.
Local capability does not imply inventory.
Behavior may be discovered in raw scan, but it only enters a phase if it declares that phase's contract.
Inventory is not setup.
Inventory is not binding.
Inventory is not a service locator.
```

#### Transitional shapes deprecated by this boundary

```text
ObjectEmitter is local Actor behavior and does not belong to transversal inventory.
PermissionTarget contributes only a minimal reference to gate logic; receiver preparation belongs to permission runtime/stage.
PresentationEndpoint belongs to ActorCapabilitySurface and Setup Plan.
AttributeEndpoint belongs to ActorCapabilitySurface and Setup Plan.
CameraTarget belongs to ActorCapabilitySurface and Binding Plan.
```

### 2B. ENTRY-BOUNDARY Closure â€” PASS funcional + PASS arquitetural parcial

This checkpoint freezes the closure of the ActivityEntryPipeline boundary after:

```text
ENTRY-BOUNDARY-1B â€” ActivityGateBindingStage
ENTRY-BOUNDARY-1C â€” Presentation SetupContribution
ENTRY-BOUNDARY-1D â€” Attribute SetupContribution
ENTRY-BOUNDARY-1E â€” Camera BindingContribution
ENTRY-BOUNDARY-H2B â€” ObjectEmission permission receiver contribution flow
```

#### Normative decisions registered

```text
ActivityCapabilityInventory is only transversal inventory.
It may contain only ResetEndpoint, SnapshotProvider, SnapshotRestoreEndpoint, ReleaseEndpoint and real transversal Gate/Permission metadata.
It must not contain PresentationEndpoint, AttributeEndpoint, CameraTarget, ObjectEmitter, ProjectileEmitter, CommandSink, Movement or any other local behavior.

Setup Plan uses ActorPresentationSetupContribution and ActorAttributeSetupContribution.
Setup stages no longer use ActivityCapabilityInventory as a service locator.

Binding Plan uses ActorCameraBindingContribution and ActivityPermissionReceiverContribution.
Camera no longer uses CameraTarget as inventory reference.

ActivityGateBindingStage is the explicit owner of permission/gate binding.
Scanner does not create receiver.
Scanner does not register receiver.
Scanner does not decide requiredness by Player, Actor, Scope, Participation or component presence.
Movement and ObjectEmission enter the gate through the same provider/contribution flow.

ObjectEmission does not return to inventory.
There is no ActivityCapabilityKind.ObjectEmitter.
There is no ObjectEmission-specific scanner.
ObjectEmission participates in ActivityGameplayControl by receiver/provider only.

This closure does not implement real ObjectEmission runtime.
The next block may continue with follow-up ObjectEmission runtime/trajectory/audio, but the MVP closure is recorded below and it must not reintroduce inventory as behavior catalog.
Any new behavior must declare phase contracts through interface/contribution: SetupContribution, BindingContribution, PermissionReceiverContribution, and Reset/Snapshot/Release contribution when applicable.
```

#### Smoke evidence summary

```text
Inventory preview without PresentationEndpoint, AttributeEndpoint, CameraTarget, ObjectEmitter or ProjectileEmitter.
Presentation and Attribute via SetupContributions.
Camera via BindingContributions.
Gate with movement.receiver + object_emission.receiver.
ActorObjectEmissionPermissionApplied state='Allowed'.
ActorObjectEmissionCommandAccepted in ActivityRunning.
RestartCurrentActivity PASS.
Activity01ToActivity02 PASS.
RouteExitBackToMenu PASS.
No FATAL.
No Exception.
No route_transition_failed.
No foreign/stale indevido.
```

### 2C. ACT-EMIT-2 — ObjectEmission Pool/Rent/Return MVP â€” PASS

This checkpoint closes the ObjectEmission MVP after `ACT-EMIT-2B`, `ACT-EMIT-2D`, `ACT-EMIT-2D2`, `ACT-EMIT-2E` and `ACT-EMIT-2E-H1`.

#### Normative decisions registered

```text
FirePrimary is read by PlayerActorCommandInputHub.
ActorObjectEmitterEndpoint accepts or rejects the command and builds the resolved payload.
ActivityGateBindingStage controls permission via ActivityGameplayControl.
ObjectEmission participates in the gate through IActivityPermissionReceiverProvider / ActivityPermissionReceiverContribution.
ObjectEmission does not return to ActivityCapabilityInventory.
There is no ActivityCapabilityKind.ObjectEmitter.
There is no ObjectEmission-specific scanner.
ObjectEmissionRuntimeComposer resolves IPoolService.
ObjectEmissionPoolRuntimeBridge wires the service to endpoints.
ObjectEmissionPoolAdapter is the sole owner of Rent.
ObjectEmissionPoolReturnSink is the explicit owner of Return.
ObjectEmissionPooledObject receives payload, controls local lifetime and requests return through the sink.
Endpoint and runtime object do not know IPoolService.
PoolService, GameObjectPool, PoolRuntimeHost and PoolAutoReturnTracker remain canonical infrastructure, not gameplay owners.
The MVP does not implement audio, damage, collision/impact gameplay or VFX.
```

#### Smoke evidence summary

```text
ActivityGateBindingStarted contributionCount='2'.
ActivityCapabilityPermissionReceiverRegistered movement.receiver.
ActivityCapabilityPermissionReceiverRegistered object_emission.receiver.
ActivityGateBindingCompleted receivers='2'.
ActorObjectEmissionPermissionApplied state='Allowed'.
ActorObjectEmissionCommandAccepted.
ObjectEmissionPoolRentCompleted.
ObjectEmissionSpawned.
ObjectEmissionPooledObjectLifetimeExpired.
ObjectEmissionPooledObjectReturnRequested.
ObjectEmissionReturnedToPool.
ObjectEmissionReturnedToPool with actorId, actorInstanceRuntimeId and profileId filled.
No pool_service_unavailable.
No Missing Script.
No AudioPlay.
No Damage.
No VFX.
```

#### Remark

```text
commandSource and commandReason still appear empty in the return log.
This is non-blocking observability for future hygiene if the fields remain redundant.
It is not a blocker because actorId, actorInstanceRuntimeId, profileId, source and reason are populated.
```

#### Next possible blocks

```text
ACT-EMIT-3A â€” movement/trajectory of the emitted object.
ACT-EMIT-4A â€” audio pooled by adapter.
ACT-EMIT-5A â€” collision/impact without damage.
ACT-EMIT-6A â€” damage as a separate capability.
```

### 3. `RouteExit teardown` deve ter owner Ãºnico

`SessionActivityPipeline` decide o lifecycle de teardown de activity para `RouteExit`.

`SessionActivityHost` deve ser endpoint/delegador externo, nÃ£o owner de decisÃ£o.

Permitido ao Host:

```text
expor boundary para SessionOperational
encaminhar request para SessionActivityPipeline
aguardar resultado publicado pelo pipeline
validar ausÃªncia/presenÃ§a mÃ­nima de pipeline ativo
retornar result externo
```

Proibido ao Host:

```text
classificar stage de teardown como policy final
manter lista prÃ³pria divergente de stages
avanÃ§ar lifecycle de route-exit por conta prÃ³pria
registrar estado como fonte de verdade de teardown
executar side-effects de teardown
```

### 4. Policies devem ser Ãºnicas e explÃ­citas

DuplicaÃ§Ãµes como `IsRouteExitTransitStage` e `IsDeactivationTransitionStage` devem convergir para policy Ãºnica quando fizerem parte do mesmo domÃ­nio de decisÃ£o.

Policy classifica:

```text
skip
failure
route-exit transit
stale/foreign
required/optional capability
stage allowed/blocked
```

Policy nÃ£o executa side-effect.

### 5. Commands nÃ£o carregam infraestrutura

Commands de Base 2.0 nÃ£o podem carregar:

```text
Stage
Boundary
Adapter
Func<T>
Action
MonoBehaviour executor genÃ©rico
state mutÃ¡vel compartilhado
ScriptableObject autoral inteiro quando sÃ³ Ã© necessÃ¡rio payload resolvido
```

Commands carregam payload runtime resolvido.

### 6. Facts nÃ£o executam side-effects

Facts registram o que aconteceu. NÃ£o podem:

```text
chamar EventBus
chamar adapter
alterar lifecycle
criar command operacional
resolver prÃ³xima stage
```

### 7. Adapters executam side-effects, nÃ£o lifecycle

Adapters podem executar side-effects Unity comandados por pipeline/stage:

```text
load/unload scene
materialize/release presentation
bind input/camera/movement
apply/reset endpoint local
capture/restore snapshot local
```

Adapters nÃ£o decidem:

```text
next activity
route exit
required vs optional
fallback de configuraÃ§Ã£o obrigatÃ³ria
entry lifecycle
policy de stage order
```

### 8. Permission identity precisa separar domÃ­nios

O dÃ©bito de `targetId` deve ser tratado como identidade ambÃ­gua.

SeparaÃ§Ã£o alvo:

```text
ActorInstanceRuntimeId / ActorId: identidade do actor runtime
PlayerActorId: identidade semÃ¢ntica de player actor
PlayerSlotId: slot/entrada do jogador
ReceiverId: identidade tÃ©cnica do receiver local
PermissionTargetId: identidade do alvo de permission, sem misturar slot/actor/receiver
```

Enquanto o receiver atual for player-specific, o contrato pode continuar carregando `PlayerActorId` e `PlayerSlotId`, mas nÃ£o deve comparar domÃ­nios diferentes como fallback.

### 9. Actor convergence continua normativa

`PlayerActor` e `NonPlayerActor` nÃ£o devem voltar a virar rails paralelos permanentes.

`ActivityEntryPipeline` deve consumir o shape jÃ¡ aceito de:

```text
ActorScanTarget
ActorCapabilitySurface
ActorInventoryFeed
ActorParticipation
ActorPresentation
ActorAttributes
CameraTarget
PermissionTarget
ActorReset contract
```

VariaÃ§Ã£o concreta de actor deve aparecer como typed policy/capability/endpoint, nÃ£o como branch global `player/nonplayer` no pipeline.

#### Guarda corretiva pÃ³s-auditoria SA-5

A tentativa de criar um corte especÃ­fico de `NonPlayerActorDiscovery` como owner de entry foi classificada como premissa arquitetural errada.

Regra normativa:

```text
Actor Ã© a Ãºnica entrada arquitetural para discovery/readiness/setup de actors.
PlayerActor, NonPlayerActor e outros tipos concretos podem existir como especializaÃ§Ãµes, metadata, endpoint, authoring ou fonte transitÃ³ria.
Essas especializaÃ§Ãµes nÃ£o podem definir cortes, stages ou lifecycle rails prÃ³prios no ActivityEntryPipeline.
```

Nomes transitÃ³rios existentes no cÃ³digo, como `NonPlayerActorDiscovery`, sÃ³ podem permanecer enquanto forem fontes/adapters para um contrato canÃ´nico de `ActorDiscovery`/`ActorInventoryFeed`. Eles nÃ£o podem ser promovidos a owner final nem usados como precedente para novos cortes.

### 10. Sem compatibility rails novos

NÃ£o criar:

```text
ActivityEntryPipeline paralelo opcional
manager/coordinator genÃ©rico para esconder pipeline novo
fallback para caminho antigo quando o novo falhar
alias/compat permanente para stages ou results antigos
bridge stage-to-stage como owner final
```

ExtraÃ§Ã£o transitÃ³ria sÃ³ Ã© aceita quando:

```text
for curta
for documentada
nÃ£o tiver dois owners ativos
nÃ£o criar fallback silencioso
remover ou substituir o caminho antigo no mesmo corte ou em corte imediatamente seguinte
```


### 11. Observabilidade nÃ£o pode antecipar lifecycle

Logs, facts, snapshots e traces precisam representar o lifecycle real, nÃ£o apenas a etapa recÃ©m-extraÃ­da.

Ã‰ proibido emitir evento com semÃ¢ntica de conclusÃ£o total quando apenas um subpasso terminou.

Exemplo proibido:

```text
ActivityEntryPipelineCompleted emitido ao fim de entry preparation, antes de ActivityContent load, Inventory, setup e readiness.
```

Forma correta:

```text
ActivityEntryPreparationCompleted
ActivityEntryPreparationAccepted
ActivityEntryContentLoadStarted
ActivityEntryContentLoadCompleted / ActivityEntryContentLoadSkipped
ActivityEntryInventoryPreviewStarted
ActivityEntryInventoryValidationCompleted
ActivityEntryReadinessCompleted
ActivityEntryPipelineCompleted somente quando o entry lifecycle inteiro terminar
```

Regra normativa:

```text
O nome do fact/log deve corresponder ao escopo realmente concluÃ­do.
Completed de pipeline inteiro sÃ³ pode ser emitido quando o pipeline inteiro terminou.
Completed de stage/subpasso deve carregar o nome do stage/subpasso, nÃ£o do pipeline pai.
```

Essa regra vale mesmo quando o smoke funcional passa. Smoke sem erro nÃ£o valida semÃ¢ntica de ownership.

### 12. Snapshots/Ã­ndices runtime tÃªm writer canÃ´nico Ãºnico

Snapshots e Ã­ndices runtime passivos, como `ActivityCapabilityInventoryPreview`, nÃ£o podem ter mÃºltiplos writers tardios.

O owner correto do inventory de entry Ã© o `ActivityEntryPipeline` ou o stage canÃ´nico chamado por ele.

Stages posteriores devem consumir o inventory resolvido. Eles nÃ£o podem reconstruir e sobrescrever o mesmo state canÃ´nico para satisfazer uma necessidade local.

Proibido:

```text
MovementBinding reconstruir ActivityCapabilityInventoryPreview e gravar CurrentActivityCapabilityInventoryPreview.
CameraBinding reconstruir ActivityCapabilityInventoryPreview e gravar CurrentActivityCapabilityInventoryPreview.
Actor/Object setup reconstruir inventory canÃ´nico para esconder ausÃªncia de capability.
```

Permitido:

```text
MovementBinding consultar o inventory canÃ´nico da entry.
CameraBinding consultar o inventory canÃ´nico da entry.
Actor/Object setup consultar o inventory canÃ´nico da entry.
Stage falhar explicitamente quando o inventory esperado estiver ausente, stale, foreign ou incompleto.
```

Se um stage posterior precisa de capability ausente no inventory canÃ´nico, a correÃ§Ã£o deve ocorrer no owner do inventory, nÃ£o por rebuild local.

Regra normativa:

```text
Um snapshot runtime canÃ´nico tem um writer ativo por lifecycle.
Consumidores nÃ£o podem virar writers para corrigir falta local.
Rebuild local sÃ³ Ã© permitido como diagnÃ³stico temporÃ¡rio, documentado e removido no mesmo corte ou no corte imediatamente seguinte.
```

### 13. CorreÃ§Ã£o funcional nÃ£o basta quando a fronteira continua ambÃ­gua

Um corte pode passar no smoke e ainda assim nÃ£o ser aceito como PASS arquitetural final se:

```text
o owner correto nÃ£o estiver visÃ­vel;
o log/fact declarar conclusÃ£o mais ampla do que ocorreu;
um state canÃ´nico tiver mÃºltiplos writers;
um consumidor posterior reconstruir dados que deveriam vir do owner anterior;
a correÃ§Ã£o esconder falta de contrato com fallback local.
```

Nesses casos, o corte pode ser classificado apenas como:

```text
PASS funcional
PASS arquitetural parcial
PENDING hygiene/ownership normalization
```

A normalizaÃ§Ã£o deve ser feita antes de migrar o prÃ³ximo bloco dependente.

## Ownership final por categoria

| Categoria | Owner correto | ObservaÃ§Ã£o |
|---|---|---|
| Session activity macro lifecycle | `SessionActivityPipeline` | Ordem macro, handoff, next/restart/route-exit |
| Activity entry lifecycle | `ActivityEntryPipeline` | Content/setup/readiness/bindings por entry |
| Activity transition policy | `SessionActivityPipeline` + policy dedicada | Decide next/restart/complete, nÃ£o side-effect |
| RouteExit teardown lifecycle | `SessionActivityPipeline` | Host delega, nÃ£o decide |
| ActivityContent side-effects | Adapter/stage de content | Pipeline comanda, adapter executa |
| Actor/Object setup | Entry stages | Sem rails player/nonplayer paralelos permanentes |
| Actor capability behavior local | Endpoint local | Endpoint reage, nÃ£o decide lifecycle global |
| Permission reaction concreta | Receiver local | Pipeline publica state; receiver aplica localmente |
| Facts/traces | Recorder/fact emitter | Registro apenas |
| Composition/global registry | Composition root/installer | NÃ£o no Host como lifecycle owner |
| QA probes | Endpoints QA isolados | Nunca owner final de lifecycle |

## Plano normativo consolidado de refatoraÃ§Ã£o

Este plano substitui a sequÃªncia inicial genÃ©rica. Ele Ã© parte normativa deste ADR e deve guiar a implementaÃ§Ã£o de `SessionActivity` Base 2.0.

A regra principal Ã©:

```text
SessionActivityPipeline mantÃ©m lifecycle macro, transition, restart, route-exit e handoffs.
ActivityEntryPipeline vira owner real do lifecycle determinÃ­stico da entry.
Stages executam passos determinÃ­sticos.
Policies classificam skip/failure/required/optional/stale/foreign.
Commands carregam payload runtime resolvido.
Facts registram o que ocorreu.
Adapters executam side-effects.
Endpoints reagem localmente.
```

Nenhum corte deve ser aceito apenas por reduzir tamanho de arquivo. Um corte sÃ³ Ã© vÃ¡lido se remover responsabilidade concreta do owner errado, atribuir owner correto, remover ou tornar inacessÃ­vel o caminho antigo equivalente, nÃ£o criar fallback e preservar smoke/log.

### Estado jÃ¡ fechado

| Corte | Status normativo | Resultado |
|---|---|---|
| `SA-0` | Fechado | ADR/plano inicial criados. |
| `SA-1` | Fechado | `RouteExit teardown` com owner Ãºnico no `SessionActivityPipeline`; Host delega. |
| `SA-2` | Fechado | `ActivityEntryPipeline` concreto criado. |
| `SA-2B` | Fechado | Owner `ActivityEntryPipeline` visÃ­vel nos logs. |
| `SA-3A` | Fechado | `ActivityContent load/prepare/readiness` movido para `ActivityEntryPipeline`. |
| `SA-3A-H1` | Fechado | Corrigida observabilidade prematura de `ActivityEntryPipelineCompleted`; `ActivityEntryPreparationAccepted` substitui conclusÃ£o falsa. |

### Estado ainda problemÃ¡tico

Mesmo apÃ³s `SA-3A-H1`, o cÃ³digo ainda nÃ£o atende ao desenho final do ADR porque:

```text
ActivityEntryPipeline ainda nÃ£o Ã© owner real de setup/readiness completo.
EmitNominalActivitySetup ainda concentra setup real no SessionActivityPipeline.
ObjectReset/ObjectRestore ainda pertencem ao miolo de entry e dependem de ordem correta com Inventory.
ActivityObjectEntryStage interno ainda Ã© wrapper/fachada se apenas chamar mÃ©todos Core do SessionActivityPipeline.
IActivityEntryRuntimeEndpoint ainda Ã© bridge transitÃ³ria e nÃ£o pode crescer como fachada permanente.
```

### Regra de replanejamento

O plano original `SA-3 = ActivityContent + Inventory` foi refinado pela auditoria consolidada. O prÃ³ximo passo nÃ£o Ã© mover apenas `ActivityCapabilityInventory` isoladamente. Antes, deve-se corrigir a ordem e o ownership do subfluxo mÃ­nimo que torna o inventory canÃ´nico Ãºtil para os consumidores.

---



### Corte corretivo aplicado localmente â€” SA-5A0 + SA-5A1

Status: implementado neste pacote, **pendente de smoke/log**.

Objetivo:

```text
Interromper regressÃ£o de Actor rails e isolar mistura de identidade antes de continuar ActorDiscovery genÃ©rico.
```

DecisÃµes aplicadas:

```text
SA-5A0 â€” Actor rail regression stopper
- Actor Ã© a Ãºnica entrada arquitetural para discovery/readiness/setup.
- PlayerActor/NonPlayerActor permanecem tipos concretos, mas nÃ£o podem definir stage/corte/rail final.
- Fontes transitÃ³rias podem alimentar ActorInventoryFeed/ActorScanTarget.
- README de SessionActivity recebeu guarda anti-regressÃ£o explÃ­cita.

SA-5A1 â€” Identity quarantine
- PlayerActorMaterializationAdapter nÃ£o pode mais definir ActorId a partir de PlayerSlotId.
- ActorId do PlayerActor materializado passa a ser o PlayerActorId semÃ¢ntico.
- Permission command expÃµe TargetActorId em vez de TargetId ambÃ­guo.
- Permission binding/reference exige TargetActorId quando scope=Actor.
- PlayerMovementPermissionReceiver sÃ³ aceita TargetActorId == PlayerActorId.
- PlayerSlotId e ReceiverId deixam de ser fallback de matching de alvo.
- Camera requirement matching deixa de aceitar PlayerSlotId como alias de TargetId.
```

NÃ£o congelar como PASS sem smoke contendo:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
MovementControlEnabled em ActivityRunning
MovementControlDisabled em completion/route-exit
sem PermissionTargetIdentityUnresolved em cenÃ¡rio vÃ¡lido
sem fallback TargetActorId == PlayerSlotId
sem fallback TargetActorId == ReceiverId
ActorId, PlayerActorId e PlayerSlotId observÃ¡veis como domÃ­nios separados
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```


### Corte de normalizaÃ§Ã£o aplicado â€” SA-5A0-H1

Status: **CLOSED / PASS funcional + PASS arquitetural do corte**.

Objetivo:

```text
Normalizar a base lexical/arquitetural antes da auditoria SA-5A para nÃ£o iniciar ActorDiscovery/ActorReadiness com a leitura torta de rails PlayerActor/NonPlayerActor.
```

DecisÃµes aplicadas:

```text
- `NonPlayerActorDiscoveryStage` foi renomeado para `ActorSceneDiscoveryStage`.
- O mÃ©todo de emissÃ£o passou de `EmitNonPlayerActorDiscoveryStage` para `EmitActorSceneDiscoveryStage`.
- Os facts/stages de discovery de cena passaram de `NonPlayerActorDiscovery*` para `ActorSceneDiscovery*`, preservando os valores numÃ©ricos dos enums.
- `NonPlayerActor` permanece como componente/fonte concreta scene-authored, mas nÃ£o como nome do stage/corte/owner arquitetural.
- `PlayerActorReadinessStage` foi renomeado para `ActivityParticipantReadinessStage`.
- Os facts/stages de readiness passaram de `PlayerActorReadiness*` para `ActivityParticipantReadiness*`, preservando os valores numÃ©ricos dos enums.
- Entries antigas e nÃ£o usadas `NonPlayerActorPresentation*` foram removidas dos contratos para nÃ£o sugerir rail paralelo de presentation.
- `NonPlayerActorDiscoveryRecord` nÃ£o usado foi removido dos contratos concretos.
```

Fronteira preservada:

```text
- NÃ£o move ActorPresentation.
- NÃ£o move ActorAttributes.
- NÃ£o move ActorParticipation.
- NÃ£o altera Camera, Permission, Movement, Reset, Release, Deactivation ou RouteExit.
- NÃ£o altera a semÃ¢ntica de activity_01/activity_02.
- NÃ£o cria stage final para PlayerActor ou NonPlayerActor.
```

CritÃ©rio de aceite:

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
ActorSceneDiscovery preservado em activity_01 e skip/no-content preservado em activity_02
ActivityParticipantReadiness preservado
ActorPresentationSetupCompleted preservado
ActorAttributeSetupCompleted preservado
ActorParticipationEnterCompleted preservado
MovementBindingCompleted preservado
CameraBindingCompleted preservado
ActorResetQaApplied preservado
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```

### Resultado do smoke SA-5A0-H1

Smoke manual validado apÃ³s aplicaÃ§Ã£o do pacote.

EvidÃªncia aceita:

```text
sem erros CS observÃ¡veis pelo smoke no Editor
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
sem NonPlayerActorDiscoveryStage no log
sem PlayerActorReadinessStage no log
ActorPresentationSetupCompleted preservado
ActorAttributeSetupCompleted preservado
ActorParticipationEnterCompleted preservado
MovementBindingCompleted preservado
CameraBindingCompleted preservado
ActorResetQaApplied preservado
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
activity_01 preserva cenÃ¡rio com conteÃºdo/contributors
activity_02 preserva cenÃ¡rio negativo/no-content com skip explÃ­cito e PassedNoCommands
```

Nota de observabilidade:

```text
O smoke nÃ£o emite facts literais chamados ActorSceneDiscovery ou ActivityParticipantReadiness.
Neste corte, o aceite arquitetural Ã© restrito Ã  normalizaÃ§Ã£o lexical/contratual confirmada por compile/smoke e pela ausÃªncia dos nomes antigos de stage no log.
Adicionar facts explÃ­citos para ActorSceneDiscovery/ActivityParticipantReadiness pode ser tratado como hygiene futura, sem bloquear este PASS.
```


ApÃ³s esse smoke, a auditoria `SA-5A â€” ActorDiscovery / ActorReadiness ownership audit` pode comeÃ§ar sobre uma base menos contaminada por nomes de rails concretos.

---

## Roadmap normativo por fases

### Fase A â€” Consolidar entry lifecycle real

#### `SA-3B0 â€” Entry Setup Pre-Inventory Ownership / Ordering Correction`

Objetivo: mover para `ActivityEntryPipeline` o primeiro bloco real de setup que hoje impede o inventory canÃ´nico de nascer na ordem correta.

Escopo permitido:

```text
ActivitySetupInventory
ObjectSnapshotContractValidation
ObjectReset
ObjectRestore
ActivityCapabilityInventoryPreview
QA reset ligado a esse caminho canÃ´nico
```

Escopo proibido neste corte:

```text
ActorPresentation
ActorAttributes
ActorParticipation
PlayerInput
Movement
Camera
Permission identity cleanup
Release / Deactivation / RouteExit
```

CritÃ©rio arquitetural:

```text
ActivityEntryPipeline owna o subfluxo.
SessionActivityPipeline nÃ£o executa diretamente esse bloco.
ObjectReset nÃ£o reconstrÃ³i inventory local.
ObjectRestore nÃ£o depende de preview vazio/antigo.
QA reset chama caminho canÃ´nico, nÃ£o trilho paralelo.
Inventory canÃ´nico nasce antes dos consumidores desse bloco.
ActivityObjectEntryStage interno nÃ£o Ã© expandido como fachada.
IActivityEntryRuntimeEndpoint nÃ£o cresce como owner remoto do god pipeline.
```

#### `SA-3B1 â€” ActivityCapabilityInventory ownership final`

Objetivo: completar a migraÃ§Ã£o do `ActivityCapabilityInventory` para owner real no `ActivityEntryPipeline` ou em stage canÃ´nico chamado por ele.

Escopo:

```text
ActivityCapabilityInventoryPreviewStarted
ActivityCapabilityInventoryValidationStarted
ActivityCapabilityInventoryValidationPassed/Failed
ActivityCapabilityInventoryValidationCompleted
ActivityCapabilityInventoryPreviewObserved
CurrentActivityCapabilityInventoryPreview write/clear/read contract
```

Regras:

```text
ActivityCapabilityInventory Ã© snapshot/Ã­ndice runtime passivo.
Ele nÃ£o decide lifecycle.
Ele tem writer Ãºnico por lifecycle.
Consumidores nÃ£o podem reconstruÃ­-lo para corrigir falta local.
NÃ£o confundir ActivityCapabilityInventory com ActivitySetupInventory.
```

---

### Fase B â€” Transformar setup de objetos em entry stages reais

#### `SA-4A â€” ActivityObjectEntryStage real / auditoria`

Resultado da auditoria pÃ³s `SA-3B0` + `SA-3B1`:

```text
SA-3B0 jÃ¡ transferiu para ActivityEntryPipeline o subfluxo:
- ActivitySetupInventory;
- ObjectSnapshotContractValidation;
- ActivityCapabilityInventoryPreview;
- ObjectReset;
- ObjectRestore.

Portanto, SA-4A nÃ£o deve recriar ActivityObjectEntryStage do zero.
O dÃ©bito real restante Ã© ActivityObjectContributorDiscovery ainda nascer no SessionActivityPipeline.
```

DecisÃ£o normativa:

```text
SA-4A deve ser reinterpretado como sequÃªncia pequena de cleanup, comeÃ§ando por SA-4A0.
NÃ£o reabrir reset/restore/inventory sem evidÃªncia de regressÃ£o.
NÃ£o criar stage paralelo para o que SA-3B0 jÃ¡ moveu.
```

#### `SA-4A0 â€” ActivityObjectContributorDiscoveryStage real`

Objetivo: mover `ActivityObjectContributorDiscovery` para stage real chamado pelo `ActivityEntryPipeline`, removendo execuÃ§Ã£o concreta do `SessionActivityPipeline`.

Escopo:

```text
ActivityObjectContributorDiscoveryStarted
ActivityObjectContributorDiscovered
ActivityObjectContributorDiscoverySkippedNoContent
ActivityObjectContributorDiscoveryCompleted
ActivityObjectContributorDiscoveryFailed
CurrentActivityObjectContributorDiscoveryResult write/clear/read contract
```

Owner correto:

```text
ActivityEntryPipeline -> ActivityEntryObjectContributorDiscoveryStage
```

Regras:

```text
SessionActivityPipeline nÃ£o chama DiscoverActivityObjectContributorsOrSkipCore.
ActivityEntryPipeline chama ActivityEntryObjectContributorDiscoveryStage antes de ActivitySetupInventory.
Discovery result tem writer Ãºnico por entry.
ActivitySetupInventory e SnapshotContractValidation consomem discovery result produzido no mesmo owner.
Sem fallback para discovery antigo.
Sem bridge grande nova.
A bridge transitÃ³ria sÃ³ pode expor setter tÃ©cnico de CurrentActivityObjectContributorDiscoveryResult.
```

CritÃ©rio de aceite:

```text
ActivityObjectContributorDiscovery facts preservados.
ActivityObjectContributorDiscovery checkpoint preservado.
SessionActivityPipeline perde execuÃ§Ã£o direta de discovery.
ActivityEntryPipeline Ã© owner do stage.
Sem alteraÃ§Ã£o de ActorPresentation, ActorAttributes, ActorParticipation, PlayerInput, Movement, Camera, Release, Deactivation ou RouteExit.
```

---

#### `SA-4B â€” ActivityObjectSnapshot/Reset/Restore cleanup`

Objetivo: separar snapshot/reset/restore em commands/facts/adapters claros.

CritÃ©rio:

```text
Reset obrigatÃ³rio ausente = fail-fast.
Reset opcional ausente = skip explÃ­cito.
ResetAll cego proibido.
Snapshot restore nÃ£o decide lifecycle.
Facts nÃ£o executam side-effects.
Adapters/endpoints executam aplicaÃ§Ã£o local.
```

---

### Fase C â€” Actor setup por entry

#### `SA-5A â€” ActorDiscovery / ActorReadiness ownership audit`

Objetivo: auditar e redesenhar o setup de actors para garantir que `Actor` seja a Ãºnica entrada arquitetural de lifecycle/readiness no `ActivityEntryPipeline`.

Escopo canÃ´nico:

```text
ActorDiscovery
ActorReadiness
ActorScanTarget
ActorCapabilitySurface
ActorInventoryFeed
ActorParticipationContext
ActorInstanceRuntimeId
Actor capability endpoints
```

Fora do escopo como trilho arquitetural:

```text
PlayerActor readiness como subcorte separado
NonPlayerActor discovery como subcorte separado
branch global player/nonplayer
stage de lifecycle nomeado por especializaÃ§Ã£o concreta de Actor
```

CritÃ©rio:

```text
Actor Ã© a Ãºnica raiz de entrada para discovery/readiness.
PlayerActor, NonPlayerActor e especializaÃ§Ãµes futuras sÃ£o tipos/metadata/endpoints locais, nÃ£o owners de lifecycle.
Fontes transitÃ³rias com nomes antigos podem alimentar ActorInventoryFeed, mas nÃ£o definir stage/corte/owner canÃ´nico.
Sem comparar ActorId, PlayerActorId, ActorInstanceRuntimeId e PlayerSlotId como equivalentes.
VariaÃ§Ã£o concreta de actor aparece como typed policy/capability/endpoint, nunca como branch global player/nonplayer no pipeline.
```

DecisÃ£o corretiva:

```text
Qualquer corte chamado NonPlayerActorDiscovery, PlayerActorReadiness ou equivalente deve ser rejeitado antes de implementaÃ§Ã£o.
O prÃ³ximo corte autorizado nesta Ã¡rea Ã© auditoria/correÃ§Ã£o de ActorDiscovery genÃ©rico.
```

#### `SA-5B â€” ActorPresentation setup stage`

Objetivo: mover `ActorPresentation` setup para stage real de entry, sem transformar `ActivityEntryPipeline.cs` em novo monÃ³lito.

FormulaÃ§Ã£o correta:

```text
ActivityEntryPipeline ordena/chama o stage.
ActivityEntryActorPresentationStage executa o setup determinÃ­stico.
Policies classificam retain/materialize/skip/fail.
Adapters/endpoints executam side-effects.
SessionActivityPipeline perde o bloco concreto de presentation setup.
```

CritÃ©rio:

```text
Retention policy explÃ­cita.
Materialization em adapter.
Stage nÃ£o decide next activity.
Presentation obrigatÃ³ria ausente falha explicitamente.
Sem fallback silencioso.
ActivityEntryPipeline nÃ£o recebe loop grande de Presentation.
SessionActivityPipeline perde mais lÃ³gica concreta do que ganha.
Release ActivityExit/RouteExit fica fora deste corte.
```

#### `SA-5C â€” ActorAttributes setup stage`

Objetivo: mover setup de attributes para stage real de entry.

CritÃ©rio:

```text
Attributes sÃ£o capability local.
Pipeline/stage prepara/descobre.
ReaÃ§Ã£o local nÃ£o vira command global quando for aÃ§Ã£o local.
```


##### Status pÃ³s-smoke â€” SA-5C

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

EvidÃªncia validada no smoke:

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
ActivityEntryActorAttributeSetupStarted/Completed owner='ActivityEntryPipeline'
ActorAttributeReady preservado
ActorAttributeSetupCompleted preservado
ActorPresentationSetupCompleted preservado
ActorParticipationEnterCompleted preservado
MovementBindingCompleted preservado
CameraBindingCompleted preservado
ActorResetQaApplied preservado
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```

ConclusÃ£o arquitetural:

```text
ActorAttributes setup saiu do caminho concreto do SessionActivityPipeline.
ActivityEntryPipeline ficou como owner de ordem/lifecycle da entry.
ActivityEntryActorAttributeStage executa o setup determinÃ­stico de attributes.
SessionActivityPipeline ainda mantÃ©m lifecycle macro e ainda possui dÃ©bitos posteriores em ActorParticipation/Input/Movement/Camera.
IActivityEntryActorAttributeRuntimeBridge permanece transitÃ³ria e nÃ£o pode crescer como manager/coordinator.
```

DÃ©bitos remanescentes nÃ£o bloqueantes deste corte:

```text
ActorParticipationEnter ainda executa no SessionActivityPipeline.
PlayerInput, Movement e Camera ainda serÃ£o avaliados em cortes prÃ³prios.
ActorAttribute release ainda fica fora do escopo do SA-5C e serÃ¡ tratado em exit/release decomposition.
```

#### `SA-5D â€” ActorParticipation enter stage`

Objetivo: mover participation enter/readiness para stage real.

CritÃ©rio:

```text
Participation context explÃ­cito.
AusÃªncia obrigatÃ³ria fail-fast.
AusÃªncia opcional skip explÃ­cito.
Sem branch global player/nonplayer.
```

---

### Fase D â€” Input, Permission, Movement e Camera

#### `SA-6A â€” PlayerInput binding stage`

Objetivo: mover `PlayerInputBinding` para stage real de entry.

CritÃ©rio:

```text
Usa asset canÃ´nico jÃ¡ resolvido.
NÃ£o cria configuraÃ§Ã£o duplicada no prefab.
NÃ£o mexe no OperationalInputRuntime.
Binding Ã© preparation; enable/disable pertence a permission/lifecycle apropriado.
```

#### `SA-6B â€” Permission target preparation stage`

Objetivo: preparar permission targets como entry stage, sem redesenhar toda identity no mesmo corte.

CritÃ©rio:

```text
PermissionTarget discovery explÃ­cito.
Receiver registration explÃ­cito.
Initial state Blocked/Unbound explÃ­cito.
NÃ£o misturar PlayerActorId, PlayerSlotId, ReceiverId e PermissionTargetId.
NÃ£o mudar reaction local.
```

#### `SA-6C â€” Movement binding stage`

Objetivo: mover movement binding para stage real.

CritÃ©rio:

```text
Binding prepara.
Permission/runtime habilita ou bloqueia.
Receiver aplica localmente.
Pipeline nÃ£o chama controller diretamente para lifecycle fino.
```

#### `SA-6D â€” Camera binding stage`

Objetivo: mover camera target binding para stage real.

CritÃ©rio:

```text
Camera consome capability inventory canÃ´nico.
Camera nÃ£o reconstrÃ³i inventory.
Activity camera identity correta.
Skip/no-content preservado em activity_02.
```

---

### Fase E â€” Entry readiness boundary final

#### `SA-7 â€” EntryReadinessResult e handoff limpo para macro pipeline`

Objetivo: fazer `ActivityEntryPipeline` retornar resultado final de readiness completo para `SessionActivityPipeline`.

Resultados esperados:

```text
ActivityEntryResult.Completed
ActivityEntryResult.SkippedNoContent
ActivityEntryResult.Failed
ActivityEntryResult.RejectedStaleOrForeign
ActivityEntryResult.BlockedByRequiredCapability
```

CritÃ©rio:

```text
ActivityEntryPipeline decide readiness da entry.
SessionActivityPipeline decide apenas o prÃ³ximo macro passo: ActivationWindow ou fail/abort.
SessionActivityPipeline nÃ£o executa setup residual.
ActivityEntryPipeline tem inÃ­cio/fim semanticamente corretos.
ActivityEntryPipelineCompleted sÃ³ aparece quando a entry realmente terminou.
ActivationWindow continua fora do ActivityEntryPipeline.
```

---

### Fase F â€” Exit, release e dematerialization

#### `SA-8A â€” Exit/Release ownership audit`

Objetivo: decidir se precisa de `ActivityExitPipeline` ou se stages de exit chamados pelo `SessionActivityPipeline` bastam.

Auditar:

```text
ActorParticipation exit
ActorPresentation release
ActorAttributes release
ActivityObject snapshot capture
ActivityObject release
ActivityContent unload
ActivityObjectContributor unregister
DeactivationWindow ordering
RouteExit ordering
```

DecisÃ£o possÃ­vel A:

```text
SessionActivityPipeline mantÃ©m macro exit lifecycle.
Exit stages executam release/dematerialization.
```

DecisÃ£o possÃ­vel B:

```text
Criar ActivityExitPipeline somente se houver lifecycle determinÃ­stico prÃ³prio suficientemente grande.
NÃ£o criar pipeline por simetria estÃ©tica.
```

#### `SA-8B â€” ActivityObjectRelease / SnapshotCapture stage cleanup`

CritÃ©rio:

```text
Snapshot capture antes de release.
Release command explÃ­cito.
Unregister depois do release aplicÃ¡vel.
No-content = skip explÃ­cito.
```

#### `SA-8C â€” Actor release/participation exit cleanup`

CritÃ©rio:

```text
RouteScoped pode reter por policy.
ActivityScoped libera por ActivityExit.
RouteExit libera o que Ã© route-scoped quando aplicÃ¡vel.
Sem rail player/nonplayer paralelo.
```

---

### Fase G â€” Host, composition e boundaries

#### `SA-9A â€” SessionActivityHost boundary cleanup`

Objetivo: reduzir `SessionActivityHost` para boundary/endpoint externo, nÃ£o lifecycle owner.

CritÃ©rio:

```text
Host nÃ£o classifica lifecycle.
Host nÃ£o executa side-effects de teardown.
Host nÃ£o vira registry tardio.
QA chama comandos/stages canÃ´nicos.
```

#### `SA-9B â€” Composition / service locator cleanup`

CritÃ©rio:

```text
Composition root registra dependÃªncias.
Pipeline nÃ£o usa DependencyManager.Provider para lifecycle ativo.
Host nÃ£o registra lifecycle como fonte de verdade.
```

---

### Fase H â€” Permission identity final

#### `SA-10 â€” Permission identity separation`

Objetivo: resolver o dÃ©bito de identidade sem misturar domÃ­nios.

Escopo:

```text
PermissionTargetId
ReceiverId
PlayerActorId
PlayerSlotId
ActorInstanceRuntimeId
ActorId
ActivityParticipationContext
```

CritÃ©rio:

```text
Nenhum fallback comparando domÃ­nios diferentes.
Receiver tÃ©cnico nÃ£o vira ActorId.
PlayerSlotId nÃ£o vira PlayerActorId.
PermissionTarget tem identidade prÃ³pria.
Logs expÃµem os domÃ­nios separados.
```

---

### Fase I â€” State/fact hygiene

#### `SA-11A â€” ActivityEntry state/context extraction`

Objetivo: reduzir o uso de `SessionActivityRuntimeState` como saco global para entry.

Escopo:

```text
ActivityEntryContext
ActivityEntrySnapshot
ActivityEntryRuntimeState
Entry-local loaded content
Entry-local inventory
Entry-local setup result
```

CritÃ©rio:

```text
State da entry nÃ£o vaza como global mutÃ¡vel sem owner.
Foreign/stale continua protegido.
Restart cria novo entry context.
```

#### `SA-11B â€” Fact recorder hygiene`

CritÃ©rio:

```text
Fact recorder nÃ£o decide policy.
Fact recorder nÃ£o executa side-effect.
Logs mantÃªm owner correto.
Facts nÃ£o alteram lifecycle.
```

---

### Fase J â€” Contract/command hygiene

#### `SA-12 â€” Commands e contracts finais`

Auditar e limpar:

```text
ActivityEntryCommand
ActivityEntryContentLoadCommand
Activity setup commands
Object reset/restore commands
Actor setup commands
Permission commands
Camera/movement/input commands
```

Regra:

```text
Commands nÃ£o carregam Stage, Boundary, Adapter, Func<T>, Action, executor genÃ©rico, state mutÃ¡vel compartilhado ou ScriptableObject autoral inteiro quando sÃ³ Ã© necessÃ¡rio payload resolvido.
Commands carregam payload runtime resolvido e identity tipada.
```

##### Checkpoint SA-12-AUDIT â€” Commands/contracts hygiene

Status: `AUDITED / NEEDS SMALL COMMAND HYGIENE PATCH`.

A auditoria estÃ¡tica de `SA-12` confirmou que nÃ£o havia blocker de executor/delegate nos commands auditados:

```text
sem Action
sem Func<T>
sem adapters embutidos nos commands
sem delegates de execuÃ§Ã£o
sem SessionActivityRuntimeState embutido nos commands
```

O dÃ©bito restante foi classificado como higiene de contrato:

```text
commands carregando authoring asset inteiro;
wrappers internos carregando Stage/Boundary;
commands duplicando PipelineId/SessionStateId/ActivityId/ActivityOrdinal/EntrySequence quando SessionActivityIdentity jÃ¡ era a fonte do ciclo;
ActorAttributeCommand ainda usando strings livres para identidades runtime.
```

ConclusÃ£o:

```text
SA-12 nÃ£o exige pipeline novo.
SA-12 nÃ£o exige redesenhar lifecycle macro.
SA-12 deve ser resolvido por cortes pequenos de command hygiene.
```

##### Checkpoint SA-12B/C â€” Command boundary + identity duplication cleanup

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

Escopo fechado:

```text
ActivityObjectContributorUnregisterStageCommand
ActivityObjectResetCommand
ActivityObjectReleaseCommand
ActivityObjectSnapshotRestoreCommand
ActivityContentSceneUnloadCommand
```

CorreÃ§Ãµes aplicadas:

```text
ActivityObjectContributorUnregisterStageCommand nÃ£o carrega mais SessionActivityStage Stage.
ActivityObjectContributorUnregisterStage constrÃ³i suas identities locais internamente.
NÃ£o hÃ¡ fallback do wrapper para ActivityContentReleaseCompleted.
ActivityObjectResetCommand nÃ£o duplica PipelineId/SessionStateId/ActivityId/ActivityOrdinal/EntrySequence.
ActivityObjectReleaseCommand nÃ£o duplica PipelineId/SessionStateId/ActivityId/ActivityOrdinal/EntrySequence.
ActivityObjectSnapshotRestoreCommand nÃ£o duplica PipelineId/SessionStateId/ActivityId/ActivityOrdinal/EntrySequence.
ActivityContentSceneUnloadCommand nÃ£o duplica PipelineId/SessionStateId/ActivityId/ActivityOrdinal/EntrySequence.
Consumers passaram a usar command.Identity como fonte Ãºnica do ciclo.
```

##### Checkpoint SA-12D â€” ActorAttributeCommand typed identity

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

Escopo fechado:

```text
ActorAttributeCommand
produtores de ActorAttributeCommand
consumidores de ActorAttributeCommand
logs/facts de setup/release de ActorAttribute
```

CorreÃ§Ãµes aplicadas:

```text
ActorAttributeCommand nÃ£o carrega mais string PipelineIdentity.
ActorAttributeCommand nÃ£o carrega mais string ActivityIdentity.
ActorAttributeCommand nÃ£o carrega mais string ActorInstanceId.
ActorAttributeCommand carrega SessionActivityIdentity como identidade tipada do ciclo.
ActorAttributeCommand carrega ActorInstanceRuntimeId como identidade funcional runtime do actor.
Call sites foram migrados para o shape tipado.
Logs podem imprimir ToString()/Value apenas como observabilidade, nÃ£o como lookup funcional.
```

##### Checkpoint SA-12E â€” ActivityContent runtime scene reference

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

Escopo fechado:

```text
ActivityEntryContentLoadCommand
ActivityContentLoadPlan
ActivityContentLoadPlanScene
ActivityContentLoadedSceneRecord
ActivityContentSceneUnloadDispatchStage
call sites de content load/unload
```

CorreÃ§Ãµes aplicadas:

```text
ActivityEntryContentLoadCommand nÃ£o carrega mais SessionActivityDefinition.
ActivityEntryContentLoadCommand passou a carregar ActivityContentLoadPlan como payload runtime resolvido.
ActivityContentLoadPlan contÃ©m Identity, ActivityId, ActivityOrdinal, ActivityContentMode, ActivityContentProfileId, Scenes, Source e Reason.
ActivityContentLoadPlanScene carrega runtime scene reference mÃ­nima para load.
ActivityContentLoadedSceneRecord nÃ£o carrega mais SceneKeyAsset autoral como fonte de unload.
ActivityContentSceneUnloadDispatchStage passou a operar por ActivityContentSceneRuntimeReference.
activity_01 preserva content load com loadedScenes='1'.
activity_02 preserva no-content/skip explÃ­cito sem fallback para Route Scene.
```

ObservaÃ§Ã£o de escopo:

```text
O corte excedeu o mÃ­nimo inicialmente previsto para SA-12F2 porque tambÃ©m removeu SceneKeyAsset de ActivityContentLoadedSceneRecord e ajustou unload/object setup para runtime reference.
A expansÃ£o foi aceita porque permaneceu dentro da mesma fronteira arquitetural: ActivityContent runtime payload.
```

##### Checkpoint SA-12F â€” Reduce SessionActivityDefinition from ActivityEntry commands

Status: `PARTIAL / IN PROGRESS`.

Subcortes validados atÃ© este checkpoint:

```text
SA-12F1A/B â€” CLOSED / PASS funcional + PASS arquitetural do corte
SA-12F2    â€” CLOSED / PASS funcional + PASS arquitetural do corte
SA-12F3A   â€” CLOSED / PASS funcional + PASS arquitetural do corte
SA-12F3B   â€” CLOSED / PASS funcional + PASS arquitetural do corte
SA-12F3C   â€” CLOSED / PASS funcional + PASS arquitetural do command boundary
SA-12F4A   â€” CLOSED / PASS funcional + PASS arquitetural do corte
SA-12F4B   â€” CLOSED / PASS funcional + PASS arquitetural do corte
SA-12F4C   â€” CLOSED / PASS funcional + PASS arquitetural do corte
```

Escopo fechado:

```text
ActivityEntryContentLoadCommand foi reduzido para ActivityContentLoadPlan.
PlayerInput/Permission/Movement/Camera binding commands deixaram de carregar SessionActivityDefinition quando jÃ¡ possuÃ­am payload resolvido.
ActorPresentation/ActorAttribute setup commands deixaram de carregar SessionActivityDefinition.
ActivityEntryParticipantBindingCommand passou a carregar ActivityParticipantBindingPlan.
ActivityEntryObjectSetupCommand deixou de carregar SessionActivityDefinition apÃ³s separaÃ§Ã£o de ActivityObjectSetupInventoryPlan e ActivityObjectResetRestorePlan.
ActivitySetupInventoryBuilder passou a consumir ActivityObjectSetupInventoryPlan.
Reset/restore do object setup passaram a consumir ActivityObjectResetRestorePlan.
```

Notas de arquitetura:

```text
ActivityParticipantBindingPlan ficou intencionalmente estreito e fecha o command boundary, mas nÃ£o representa decomposiÃ§Ã£o completa de participant requirements/materialization/placement.
ActivityObjectSetupInventoryPlan Ã© payload de setup inventory.
ActivityObjectResetRestorePlan Ã© payload de reset/snapshot restore.
ActivityEntryObjectSetupCommand nÃ£o usa mais SessionActivityDefinition como carrier runtime.
```

EvidÃªncia funcional aceita para os subcortes:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
ActivityEntryPreparationStarted/Completed preservados
ActivityEntryContentLoadStarted/Completed preservados
ActivityContentSceneUnloadDispatched preservado
ActivityContentReleaseCompleted preservado
ActivityEntryParticipantBindingCompleted preservado
ActivityEntryPlayerInputBindingCompleted preservado
ActivityEntryPermissionTargetPreparationCompleted preservado
MovementBindingCompleted preservado
CameraBindingCompleted preservado
ActorPresentationSetupCompleted preservado
ActorAttributeSetupCompleted preservado
ActivityCapabilityInventoryValidationPassed preservado
ActivityCapabilityInventoryPreviewObserved preservado
ActivityObjectReset preservado como PassedApplied em activity_01 e PassedNoCommands em activity_02
```

##### Checkpoint SA-12F-BLOCKER-MOVEMENT-ACTIVITY02 â€” retained PlayerActor movement in no-content activity

Status: `CLOSED / PASS funcional + PASS arquitetural parcial`.

Problema fechado:

```text
Ao transicionar de activity_01 para activity_02, o PlayerActor SessionScoped permanecia visÃ­vel/materializado, mas movement nÃ£o funcionava.
activity_02 Ã© no-content, mas ActivityContent ausente nÃ£o implica PlayerActor ausente nem perda automÃ¡tica de movement/control.
```

Causas confirmadas durante os cortes:

```text
ActivityParticipationContext de activity_02 era gravado vazio quando nÃ£o havia participant requirements prÃ³prios.
O retained player binding existia no ActivityActorExitRuntimeState, mas era rejeitado por validaÃ§Ã£o de scope incorreta para ActorScope.SessionScoped.
O capability inventory de activity_02 projetava apenas PresentationEndpoint e nÃ£o projetava PermissionTarget/movement receiver do PlayerActor retido.
```

CorreÃ§Ãµes aceitas:

```text
ActivityEntryParticipantBindingStage passou a promover retained player binding para ActivityParticipationContext current-entry quando a activity nÃ£o possui participant requirements prÃ³prios, mas hÃ¡ PlayerActor SessionScoped vÃ¡lido.
A validaÃ§Ã£o passou a aceitar ActorInstanceRuntimeId SessionScoped atravessando activities sem rebadgear o runtime id para activity scope.
ActivityEntryPipeline passou a ter ActivityParticipationContext com activityParticipants='1' em activity_02.
PlayerInputBinding passou a bindar o player em activity_02.
MovementBinding passou a encontrar target em activity_02.
ActivityCapabilityInventory passou a receber a capability surface funcional do PlayerActor SessionScoped retido antes da PermissionTargetPreparation.
PermissionTargetPreparation passou a registrar receiver para activity_02.
ActivityGameplayControl Allowed passou a ser aplicado ao PlayerMovementPermissionReceiver de activity_02.
MovementControlEnabled voltou a ocorrer em activity_02.
```

EvidÃªncia aceita:

```text
LoadingCompleted
LoadingHidden
ActivityParticipantRetainedBindingChosen
ActivityParticipationContextPrepared activityParticipants='1' status='ResolvedFromRetainedSessionScopedActor'
ActivityEntryPlayerInputBindingCompleted totalBound='1' skipped='False'
ActivityCapabilityInventoryPreviewObserved contendo PermissionTarget
ActivityEntryPermissionTargetPreparationCompleted receivers='1' skipped='False'
PlayerMovementBound
MovementBindingCompleted totalBound='1'
ActivityCapabilityPermissionPublished state='Allowed' activityId='activity_02'
PlayerMovementPermissionApplied state='Allowed' activityId='activity_02'
MovementControlEnabled activityId='activity_02' affectedActors='1'
Activity01ToActivity02 checkpointStatus='Passed'
activity02ReachedRunning='true'
activity_02 no-content preservado
ActivityObjectReset checkpointStatus='PassedNoCommands'
```

DÃ©bito aceito:

```text
SA-12F-MOV-H1 â€” Retained PlayerActor target projection ownership hygiene.
Status: OPEN / MEDIUM DEBT.

Parte da projeÃ§Ã£o de ActorTargets para capability inventory ficou em SessionActivityPipeline como bridge tÃ©cnica:
- ResolvePlayerActorCapabilityTargetsForCurrentEntry(...)
- AddPlayerActorCapabilityTargetsFromParticipationContext(...)
- TryResolvePlayerActorHandleForCapabilityInventory(...)

A auditoria classificou o shape como PASS funcional / PASS arquitetural parcial porque nÃ£o hÃ¡ writer duplicado de inventory, fallback por string, first-player fallback, branch player/nonplayer novo ou lifecycle/policy sendo decidido fora do owner.
Mesmo assim, o owner conceitual final da projeÃ§Ã£o deve ser ActivityEntryPipeline / ActivityEntryActorInventoryStage / helper especÃ­fico de entry.
```

CritÃ©rio futuro para fechar o dÃ©bito:

```text
Mover a projeÃ§Ã£o de PlayerActor SessionScoped retido para helper/bridge do ActivityEntryPipeline ou ActivityEntryActorInventoryStage.
Preservar o mesmo smoke funcional de activity_02.
NÃ£o reconstruir inventory em consumidor posterior.
NÃ£o criar fallback por string, first actor, first player ou registry tardio.
Manter ActivityCapabilityInventory como snapshot/Ã­ndice runtime passivo com writer Ãºnico por lifecycle.
```

##### PendÃªncias restantes de SA-12

```text
SA-12F5 â€” auditoria/correÃ§Ã£o final dos resÃ­duos de SessionActivityDefinition em ActivityEntryCommand, content-load completion/failure, ActorParticipationEnterCommand e ActivityContentReleaseFinalizationStageCommand.
SA-12F-MOV-H1 â€” hygiene futuro: mover retained PlayerActor target projection bridge para ActivityEntryPipeline / ActivityEntryActorInventoryStage.
```

CritÃ©rio para os prÃ³ximos cortes:

```text
NÃ£o reabrir SA-12E salvo regressÃ£o explÃ­cita.
NÃ£o reabrir o blocker funcional de movement em activity_02 salvo regressÃ£o de smoke.
Resolver SA-12F5 por cortes pequenos de residual command hygiene.
Tratar SA-12F-MOV-H1 como hygiene futuro, nÃ£o blocker funcional.
NÃ£o criar compat/fallback paralelo.
NÃ£o criar pipeline novo.
Preservar smoke macro completo.
```

---

## Ordem normativa atualizada

```text
DONE  SA-0    ADR/plano
DONE  SA-1    RouteExit teardown owner unification
DONE  SA-2    ActivityEntryPipeline shell
DONE  SA-2B   ActivityEntry observability
DONE  SA-3A   ActivityContent load/readiness
DONE  SA-3A-H1 lifecycle log semantics + inventory writer hygiene
DONE  SA-3B0  Entry Setup Pre-Inventory Ownership / Ordering Correction
DONE  SA-3B1  ActivityCapabilityInventory ownership final

DONE  SA-4A0  ActivityObjectContributorDiscoveryStage real
      SA-4A1  ActivityObjectEntryStage/API cleanup, se auditoria pÃ³s-smoke ainda encontrar wrapper/debt
      SA-4B   Object snapshot/reset/restore cleanup

DONE  SA-5A0-H1 Actor rail naming normalization before audit
DONE  SA-5A1 ActorInventoryFeed / ActorScanTarget ownership normalization
DONE  SA-5B0 ActorPresentation ownership contradiction cleanup / extraction audit
DONE  SA-5B   ActorPresentation setup stage
DONE  SA-5C   ActorAttributes setup stage
DONE  SA-5D   ActorParticipation enter stage

DONE  SA-6A   PlayerInput binding stage
DONE  SA-6B   Permission target preparation stage
      SA-6C   Movement binding stage
      SA-6D   Camera binding stage

      SA-7    EntryReadinessResult final

      SA-8A   Exit/Release ownership audit
      SA-8B   ObjectRelease/SnapshotCapture cleanup
      SA-8C   Actor release/participation exit cleanup

      SA-9A   Host boundary cleanup
      SA-9B   Composition/service locator cleanup

DONE  SA-10   Permission identity separation

      SA-11A  Entry state/context extraction
DONE  SA-11B  Fact recorder hygiene

PEND  SA-12   Command/contract hygiene â€” partial
DONE  SA-12B/C Command boundary + identity duplication cleanup
DONE  SA-12D  ActorAttributeCommand typed identity
DONE  SA-12E  ActivityContent SceneKeyAsset/runtime scene reference
PART  SA-12F  Reduce SessionActivityDefinition from ActivityEntry*Command
DONE  SA-12F-BLOCKER-MOVEMENT-ACTIVITY02 â€” PASS funcional / PASS arquitetural parcial
PEND  SA-12F5 residual SessionActivityDefinition command hygiene
DEBT  SA-12F-MOV-H1 Retained PlayerActor target projection ownership hygiene
```

## CritÃ©rio global de viabilidade Base 2.0

A refatoraÃ§Ã£o de `SessionActivity` sÃ³ pode ser considerada viÃ¡vel para Base 2.0 quando:

```text
SessionActivityPipeline mantÃ©m apenas lifecycle macro, transition, restart, route-exit e handoffs.
ActivityEntryPipeline Ã© owner real de entry lifecycle.
ActivityEntryPipeline nÃ£o Ã© fachada do SessionActivityPipeline.
ActivityContent, Inventory, Object setup, Actor setup, Input, Movement e Camera tÃªm stages/owners explÃ­citos.
Release/Exit tÃªm owner claro, com ou sem ActivityExitPipeline.
Host Ã© boundary/delegador, nÃ£o owner de lifecycle.
QA chama caminhos canÃ´nicos.
Commands nÃ£o carregam infraestrutura.
Facts nÃ£o executam side-effects.
Adapters nÃ£o decidem lifecycle/policy.
Snapshots/Ã­ndices runtime tÃªm writer Ãºnico por lifecycle.
Identidades de domÃ­nios diferentes nÃ£o sÃ£o comparadas como equivalentes.
Sem fallback silencioso.
Sem trilho paralelo novo.
Sem compat desnecessÃ¡ria.
Smoke completo PASS.
```

## Smoke global mÃ­nimo

```text
Boot -> Menu
Menu -> Sandbox
Activity 01 entry com content scene
CompleteActivationWindow
ActivityRunning
RestartCurrentActivity
CompleteActivationWindow novamente
CompleteCurrentActivity
Activity 01 -> Activity 02 no-content/skip
Activity 02 ActivityRunning
BackToMenu / RouteExit
```

Com checkpoints:

```text
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
ActivityObjectSnapshotCapture PASS quando aplicÃ¡vel
ActivityObjectRelease PASS
ActivityObjectContributorUnregister PASS
CameraBindingCompleted preservado
MovementBindingCompleted preservado
MovementControlEnabled/Disabled preservado
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
```

## CritÃ©rios de aceite arquitetural

Um corte de `SessionActivity` sÃ³ pode ser aceito como PASS arquitetural quando:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem fallback silencioso
sem trilho paralelo novo
sem owner duplicado para o mesmo lifecycle
Host nÃ£o decide lifecycle de route-exit
ActivityEntryPipeline Ã© owner real dos steps migrados
SessionActivityPipeline mantÃ©m lifecycle macro
stages nÃ£o viram mini-pipeline
boundaries nÃ£o chamam sub-stages
commands nÃ£o carregam infraestrutura
facts nÃ£o executam side-effects
adapters nÃ£o decidem lifecycle/policy
identidades de domÃ­nios diferentes nÃ£o sÃ£o comparadas como equivalentes
logs mostram owner correto do passo executado
logs/facts nÃ£o antecipam completed de pipeline antes do lifecycle real
snapshots/Ã­ndices runtime canÃ´nicos possuem writer Ãºnico por lifecycle
consumidores nÃ£o reconstruem state canÃ´nico para corrigir falta local
```

## CritÃ©rios de smoke mÃ­nimos

ApÃ³s cada corte funcional, exigir log/smoke manual com pelo menos:

```text
Boot -> Menu -> Sandbox
Activity entry inicial
CompleteActivationWindow
RestartCurrentActivity
Activity01ToActivity02
CompleteCurrentActivity
BackToMenu / RouteExit
```

O log deve confirmar:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
ActivityEntry owner visÃ­vel nos logs quando aplicÃ¡vel
RouteExit teardown com owner Ãºnico
ActivityEntryPipelineCompleted nÃ£o aparece antes do fim real do entry lifecycle
ActivityContent/Inventory tÃªm owner visÃ­vel e sem writers duplicados
ActivityContent/Actor/Input/Movement/Camera sem regressÃ£o nos checkpoints existentes
```

## Fora do escopo deste ADR

```text
Progression Save real
Pooling real sem caso concreto
AI/combat/dialogue de actors
DLC/online delivery
Editor tooling amplo
limpeza completa de todos os IDs textuais
reescrever SessionOperational novamente
criar Run Pipeline completo
```

## DecisÃ£o final proposta

Aceitar este ADR como contrato de decomposiÃ§Ã£o de `SessionActivity` para Base 2.0.

ImplementaÃ§Ã£o sÃ³ deve comeÃ§ar pelo corte de menor risco:

```text
SA-1 â€” RouteExit teardown owner unification
```

Nenhum corte deve ser aceito como PASS sem smoke/log.


---

## Corte aplicado â€” SA-5A1 ActorInventoryFeed / ActorScanTarget ownership normalization

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

### Objetivo

Mover o ownership efetivo de `ActorSceneDiscovery`, `ActorInventoryFeed` e `ActorScanTarget` para o escopo de `ActivityEntryPipeline`, sem migrar ainda `ActorPresentation`, `ActorAttributes`, `ActorParticipation`, Input, Movement, Camera, Permission, Release, Deactivation ou RouteExit.

### AlteraÃ§Ãµes aplicadas

```text
ActivityEntryPipeline agora chama ActivityEntryActorInventoryStage.ExecuteSceneDiscovery durante ExecuteSetupInfrastructure.
ActivityEntryPipeline agora chama ActivityEntryActorInventoryStage.ExecuteActorInventoryFeed durante ExecuteCapabilityObjectSetup.
ActorScanTarget passa a nascer do ActorInventoryFeedResult produzido no owner da entry.
SessionActivityPipeline deixou de executar diretamente EmitActorSceneDiscoveryStage.
SessionActivityPipeline deixou de montar diretamente PlayerActorInstanceSource + NonPlayerActorInstanceSource.
SessionActivityPipeline passa a consumir o ActorInventoryFeedResult corrente produzido pela entry.
ActorSceneDiscoveryStage deixou de retornar tipos nested do SessionActivityPipeline.
```

### Fronteira preservada

```text
PlayerActor e NonPlayerActor continuam apenas como fontes concretas para o feed genÃ©rico de Actor.
ActivityEntryPipeline Ã© o owner do feed/targets da entry.
SessionActivityPipeline permanece owner do lifecycle macro.
ActivityNonPlayerActorRegistry e ActivityPlayerActorRegistry continuam Ã­ndices tÃ©cnicos, nÃ£o owners de lifecycle.
ActorPresentation, ActorAttributes e ActorParticipation ainda nÃ£o foram movidos neste corte.
```

### DÃ©bito aceito do corte

```text
IActivityEntryActorInventoryRuntimeBridge ainda Ã© bridge transitÃ³ria para expor registries e targets jÃ¡ existentes ao ActivityEntryPipeline.
Esse bridge nÃ£o pode virar owner permanente nem crescer para lifecycle/policy.
O prÃ³ximo corte deve continuar reduzindo o SessionActivityPipeline sem criar ActorManager/Coordinator.
```

### CritÃ©rio de aceite

```text
compilar sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
ActivityEntryActorSceneDiscoveryStarted/Completed visÃ­vel com owner ActivityEntryPipeline
ActivityEntryActorInventoryFeedStarted/Completed visÃ­vel com owner ActivityEntryPipeline
ActivityCapabilityInventoryValidationPassed preservado
ActorPresentationSetupCompleted preservado
ActorAttributeSetupCompleted preservado
ActorParticipationEnterCompleted preservado
ActorResetQaApplied preservado
MovementBindingCompleted preservado
CameraBindingCompleted preservado
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```

### Smoke / evidÃªncia aceita

```text
Boot -> Menu -> Sandbox
CompleteActivationWindow
QA Reset Current Player Actor
RestartCurrentActivity
CompleteActivationWindow
CompleteCurrentActivity
Activity01ToActivity02
BackToMenu / RouteExit
```

Resultado observado no smoke manual:

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
ActivityEntryActorSceneDiscoveryStarted/Completed com owner ActivityEntryPipeline
ActivityEntryActorInventoryFeedStarted/Completed com owner ActivityEntryPipeline
ActivityCapabilityInventoryValidationPassed preservado
ActorPresentationSetupCompleted preservado
ActorAttributeSetupCompleted preservado
ActorParticipationEnterCompleted preservado
ActorResetQaApplied preservado
MovementBindingCompleted preservado
CameraBindingCompleted preservado
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
activity_02 negativa/no-content preservada com skip explÃ­cito e PassedNoCommands
```

DecisÃ£o: `SA-5A1` estÃ¡ fechado como PASS do corte. O dÃ©bito restante Ã© mover os consumidores de Actor setup (`ActorPresentation`, `ActorAttributes`, `ActorParticipation`) para stages reais, sem reabrir Feed/ScanTarget.

---

## Corte documental â€” SA-5B0 ActorPresentation Ownership Contradiction Cleanup / Extraction Audit

Status: CLOSED / AUDIT + DOCUMENTATION ONLY.

### Objetivo

Limpar a contradiÃ§Ã£o antes do `SA-5B`: mover `ActorPresentation` para o owner correto nÃ£o significa adicionar lÃ³gica concreta em `ActivityEntryPipeline.cs`.

DecisÃ£o normativa:

```text
ActivityEntryPipeline Ã© owner de ordem/lifecycle da ActivityEntry.
ActivityEntryActorPresentationStage deve ser o executor determinÃ­stico do setup.
Policies classificam retain/materialize/skip/fail.
Adapters/endpoints executam side-effects.
SessionActivityPipeline deve perder o bloco concreto de ActorPresentation setup.
```

### Auditoria do bloco atual

O bloco de `ActorPresentation` ainda vive majoritariamente no `SessionActivityPipeline`:

```text
_actorPresentationPlanResolver
_actorPresentationMaterializationAdapter
_activeActorPresentationByActorInstanceId
ActorPresentationReleaseRail
ActorPresentationCapabilityState
EmitActorPresentationSetupFromInventoryStage
ResolveActorPresentationReferencesFromInventory
TryGetActivePresentationHandle
CanRetainPresentationHandle
StoreActivePresentationHandle
RemoveActivePresentationHandle
SyncNonPlayerPresentationHandle
IsNonPlayerPresentationReference
EmitActorPresentationReleaseGenericStage
ActorParticipationReadinessPolicy consultando presentation ativa
```

Responsabilidades concretas ainda no macro pipeline:

```text
validar ActivityCapabilityInventory
resolver ActorPresentationEndpointReference
validar endpoint/profile
resolver ActorPresentationResolvedPlan
classificar retain/materialize/skip/fail
chamar materialization adapter
gravas estado ativo por ActorInstanceId
sincronizar handle no ActivityNonPlayerActorRegistry
emitir facts/snapshots/logs detalhados
executar release por rail ActivityExit/RouteExit/BeforeRematerialization
```

### DecisÃ£o

`SA-5B` sÃ³ fica autorizado se for extraÃ§Ã£o real, nÃ£o redistribuiÃ§Ã£o de monÃ³lito.

Permitido:

```text
Criar ActivityEntryActorPresentationStage ou evoluir ActorPresentationSetupStage para stage real.
Mover setup from-inventory para stage dedicado.
Mover helpers de resolve references/retention/store state necessÃ¡rios ao setup.
Manter facts/logs equivalentes.
Preservar ActorPresentationSetupCompleted/Materialized/Retained/Ready.
```

Proibido:

```text
NÃ£o colocar loop/materialization/retention diretamente em ActivityEntryPipeline.cs.
NÃ£o adicionar lÃ³gica concreta nova ao SessionActivityPipeline.
NÃ£o mover ActorAttributes.
NÃ£o mover ActorParticipationEnter.
NÃ£o mover release/deactivation/route-exit.
NÃ£o mexer em PlayerInput, Movement, Camera ou Permission.
NÃ£o criar ActorManager/ActorCoordinator.
NÃ£o criar fallback para caminho antigo.
NÃ£o criar branch global Player/NonPlayer.
```

### CritÃ©rio de aceite para SA-5B

```text
SessionActivityPipeline deve perder mais lÃ³gica concreta do que ganhar.
ActivityEntryPipeline deve continuar pequeno: ordem, lifecycle e chamada de stage.
ActivityEntryActorPresentationStage executa setup determinÃ­stico.
Caminho antigo de setup no SessionActivityPipeline sai ou fica inacessÃ­vel.
Sem fallback silencioso.
Smoke preservado.
```

### Artefato

RelatÃ³rio detalhado criado em:

```text
NewScripts/Docs/Reports/SA-5B0-ActorPresentation-Ownership-Audit.md
```

---

## SA-5B â€” ActorPresentation setup stage extraction

**Status:** `CLOSED / PASS funcional + PASS arquitetural do corte`  
**Data:** 2026-05-31

### DecisÃ£o aplicada

`ActorPresentation` setup deixou de ser executado diretamente pelo `SessionActivityPipeline` e passou a ser executado por stage dedicado da entry:

```text
ActivityEntryPipeline.ExecuteActorPresentationSetup
-> ActivityEntryActorPresentationStage.Execute
```

O `ActivityEntryPipeline` permanece como owner de ordem/lifecycle da entry, mas nÃ£o recebeu o loop concreto de presentation. A execuÃ§Ã£o determinÃ­stica foi extraÃ­da para `ActivityEntryActorPresentationStage`.

### MudanÃ§a de ownership

Antes:

```text
SessionActivityPipeline.EmitActorPresentationSetupFromInventoryStage
-> resolve references
-> resolve plan
-> retain/materialize/skip/fail
-> store/sync handle
```

Depois:

```text
SessionActivityPipeline
-> chama ActivityEntryPipeline.ExecuteActorPresentationSetup

ActivityEntryPipeline
-> chama ActivityEntryActorPresentationStage

ActivityEntryActorPresentationStage
-> resolve references
-> resolve plan
-> classify retain/materialize/skip/fail
-> call presentation adapter
-> store/sync via bridge transitÃ³ria
```

### Bridge transitÃ³ria

Foi criada `IActivityEntryActorPresentationRuntimeBridge` para expor ao stage o mÃ­nimo necessÃ¡rio enquanto o estado de presentation ainda nÃ£o saiu totalmente do `SessionActivityPipeline`:

```text
CurrentActivityCapabilityInventoryPreview
TryGetActiveActorPresentationHandle
StoreActiveActorPresentationHandle
SyncActiveActorPresentationHandle
ReleaseActorPresentationBeforeRematerialization
```

Essa bridge Ã© transitÃ³ria. Ela nÃ£o deve virar manager/coordinator e nÃ£o deve crescer para Attributes, Participation, Movement ou Camera.

### EvidÃªncia de PASS

Smoke validado em 2026-05-31 confirmou:

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
ActivityEntryActorPresentationSetupStarted/Completed com owner ActivityEntryPipeline
ActorPresentationSetupCompleted preservado
ActorPresentationMaterialized preservado na primeira entrada
ActorPresentationRetained preservado no restart quando policy permite
ActorAttributeSetupCompleted preservado
ActorParticipationEnterCompleted preservado
MovementBindingCompleted preservado
CameraBindingCompleted preservado
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```



---

## SA-5C â€” ActorAttributes setup stage extraction

**Status:** `CLOSED / PASS funcional + PASS arquitetural do corte`  
**Data:** 2026-05-31

### DecisÃ£o aplicada

`ActorAttributes` setup deixa de ser executado diretamente pelo `SessionActivityPipeline` e passa a ser executado por stage dedicado da entry:

```text
ActivityEntryPipeline.ExecuteActorAttributeSetup
-> ActivityEntryActorAttributeStage.Execute
```

O `ActivityEntryPipeline` permanece owner de ordem/lifecycle da entry. Ele apenas chama o stage e valida o resultado. O loop concreto de attributes fica em `ActivityEntryActorAttributeStage`.

### MudanÃ§a de ownership

Antes:

```text
SessionActivityPipeline.EmitActorAttributeSetupFromInventoryStage
-> resolve references
-> resolve profile
-> initialize endpoint
-> classify ready/skip/fail
-> store active attribute capability
```

Depois:

```text
SessionActivityPipeline
-> chama ActivityEntryPipeline.ExecuteActorAttributeSetup

ActivityEntryPipeline
-> chama ActivityEntryActorAttributeStage

ActivityEntryActorAttributeStage
-> resolve references
-> resolve profile
-> initialize endpoint
-> classify ready/skip/fail
-> store active attribute capability via bridge transitÃ³ria
```

### Bridge transitÃ³ria

Foi criada `IActivityEntryActorAttributeRuntimeBridge` para expor ao stage o mÃ­nimo necessÃ¡rio enquanto o state de actor attributes ainda nÃ£o saiu totalmente do `SessionActivityPipeline`:

```text
CurrentActivityCapabilityInventoryPreview
StoreActiveActorAttributeCapability
RemoveActiveActorAttributeCapability
```

Essa bridge Ã© transitÃ³ria. Ela nÃ£o deve virar manager/coordinator e nÃ£o deve crescer para Participation, Movement ou Camera.

### EvidÃªncia de PASS

Smoke validado em 2026-05-31 confirmou:

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
ActivityEntryActorAttributeSetupStarted/Completed com owner ActivityEntryPipeline
ActorAttributeSetupCompleted preservado
ActorAttributeReady preservado
ActorPresentationSetupCompleted preservado
ActorParticipationEnterCompleted preservado
MovementBindingCompleted preservado
CameraBindingCompleted preservado
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```


---

## SA-5D â€” ActorParticipation enter stage extraction

**Status:** `CLOSED / PASS funcional + PASS arquitetural do corte`  
**Data:** 2026-05-31

### DecisÃ£o aplicada

`ActorParticipationEnter` deixa de ser executado diretamente pelo `SessionActivityPipeline` e passa a ser executado por stage dedicado da entry:

```text
ActivityEntryPipeline.ExecuteActorParticipationEnter
-> ActivityEntryActorParticipationStage.ExecuteEnter
```

O `ActivityEntryPipeline` permanece owner de ordem/lifecycle da entry. Ele apenas chama o stage e valida o resultado. A execuÃ§Ã£o concreta de participation enter fica em `ActivityEntryActorParticipationStage`.

### MudanÃ§a de ownership

Antes:

```text
SessionActivityPipeline.EmitActorParticipationEnterFromInventoryStage
-> monta ActorParticipationCommand
-> executa ActorParticipationStageExecutor
-> classifica entered/skipped/failed
-> registra active participations
-> emite ActorReady
```

Depois:

```text
SessionActivityPipeline
-> chama ActivityEntryPipeline.ExecuteActorParticipationEnter

ActivityEntryPipeline
-> chama ActivityEntryActorParticipationStage

ActivityEntryActorParticipationStage
-> consome ActorInventoryFeedResult da entry
-> aplica readiness policy via bridge transitÃ³ria
-> classifica entered/skipped/failed
-> registra active participations via bridge transitÃ³ria
-> emite ActorReady
```

### Bridge transitÃ³ria

Foi criada `IActivityEntryActorParticipationRuntimeBridge` para expor ao stage o mÃ­nimo necessÃ¡rio enquanto o state de participation/readiness ainda nÃ£o saiu totalmente do `SessionActivityPipeline`:

```text
EvaluateActorParticipationReadiness
StoreActiveActorParticipation
```

Essa bridge Ã© transitÃ³ria. Ela nÃ£o deve virar manager/coordinator e nÃ£o deve crescer para PlayerInput, Movement ou Camera.

### EvidÃªncia de PASS

Smoke validado em 2026-05-31 confirmou:

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
ActivityEntryActorParticipationEnterStarted/Completed com owner ActivityEntryPipeline
ActorParticipationEntered preservado
ActorReady preservado
ActorPresentationSetupCompleted preservado
ActorAttributeSetupCompleted preservado
MovementBindingCompleted preservado
CameraBindingCompleted preservado
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```

Leitura arquitetural:

```text
ActorParticipationEnter deixou de ser execuÃ§Ã£o concreta do SessionActivityPipeline.
ActivityEntryPipeline manteve ownership de ordem/lifecycle da entry.
ActivityEntryActorParticipationStage passou a executar enter/ready determinÃ­stico.
SessionActivityPipeline preserva lifecycle macro e ainda mantÃ©m exits/releases para cortes futuros.
```

DÃ©bitos restantes controlados:

```text
IActivityEntryActorParticipationRuntimeBridge ainda Ã© transitÃ³ria.
ActorParticipationExit ainda pertence ao fluxo de Exit/Release.
PlayerInput, Movement e Camera ainda serÃ£o cortes prÃ³prios da entry.
ActorPresentation/ActorAttribute release ainda pertence ao bloco futuro de Exit/Release decomposition.
```

---

## SA-6A â€” PlayerInput binding stage extraction

**Status:** `CLOSED / PASS funcional + PASS arquitetural do corte`  
**Data:** 2026-05-31

### DecisÃ£o aplicada

`PlayerInputBinding` deixa de ser executado diretamente pelo `SessionActivityPipeline` e passa a ser executado por stage dedicado da entry:

```text
ActivityEntryPipeline.ExecutePlayerInputBinding
-> ActivityEntryPlayerInputBindingStage.Execute
```

O `ActivityEntryPipeline` permanece owner de ordem/lifecycle da entry. Ele apenas chama o stage e valida o resultado. A execuÃ§Ã£o concreta do binding de `PlayerInput` fica em `ActivityEntryPlayerInputBindingStage`.

### MudanÃ§a de ownership

Antes:

```text
SessionActivityPipeline.EmitPlayerInputBindingStage
-> valida participant binding
-> monta requisitos de PlayerInput
-> chama PlayerInputBindingAdapter
-> classifica skip/fail/completed
-> emite PlayerInputBindingCommandIssued/PlayerInputBound/PlayerInputBindingCompleted
```

Depois:

```text
SessionActivityPipeline
-> passa referÃªncias passivas dos participant bindings da entry
-> chama ActivityEntryPipeline.ExecutePlayerInputBinding

ActivityEntryPipeline
-> chama ActivityEntryPlayerInputBindingStage

ActivityEntryPlayerInputBindingStage
-> monta requisitos de PlayerInput
-> chama PlayerInputBindingAdapter
-> classifica skip/fail/completed
-> emite PlayerInputBindingCommandIssued/PlayerInputBound/PlayerInputBindingCompleted
```

### Regra anti-monÃ³lito preservada

O corte nÃ£o move lÃ³gica concreta para dentro do `ActivityEntryPipeline.cs`. O pipeline de entry sÃ³ ordena e valida resultado; o trabalho concreto fica no stage dedicado.

O antigo `PlayerInputBindingStage` foi esvaziado como trilho ativo. O caminho canÃ´nico passa a ser `ActivityEntryPlayerInputBindingStage`.

### Escopo preservado

```text
Movement nÃ£o foi alterado.
Camera nÃ£o foi alterada.
Permission nÃ£o foi alterada.
Release/Deactivation/RouteExit nÃ£o foram alterados.
OperationalInputRuntime nÃ£o foi alterado.
```

### CritÃ©rio de smoke validado

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
ActivityEntryPlayerInputBindingStarted/Completed com owner ActivityEntryPipeline
PlayerInputBindingCompleted preservado
PlayerInputActionsReboundToCanonical preservado
ActorPresentationSetupCompleted preservado
ActorAttributeSetupCompleted preservado
ActorParticipationEnterCompleted preservado
MovementBindingCompleted preservado
CameraBindingCompleted preservado
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```

### EvidÃªncia do smoke

O smoke manual apÃ³s o compile hotfix confirmou o novo owner do binding de input na entry:

```text
ActivityEntryPlayerInputBindingStarted owner='ActivityEntryPipeline'
ActivityEntryPlayerInputBindingCompleted owner='ActivityEntryPipeline'
```

TambÃ©m confirmou que o rebinding canÃ´nico continuou ativo:

```text
PlayerInputActionsReboundToCanonical actorId='actor.player.primary' playerSlotId='player.slot.1'
```

Fluxos preservados:

```text
ActorPresentationSetupCompleted
ActorAttributeSetupCompleted
ActorParticipationEnterCompleted
MovementBindingCompleted
CameraBindingCompleted
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```

ObservaÃ§Ã£o: `PlayerInputBindingCommandIssued` e `PlayerInputBound` permanecem emitidos como `SessionActivityFactKind` pelo stage, mas nÃ£o aparecem como linhas `OBS` individuais no log manual. Como `ActivityEntryPlayerInputBindingCompleted`, `PlayerInputActionsReboundToCanonical`, `MovementBindingCompleted` e `CameraBindingCompleted` validam o caminho ativo, isso foi classificado como observabilidade interna nÃ£o bloqueante para este corte. Se a exigÃªncia futura for log `OBS` explÃ­cito para esses facts, tratar como hygiene local de observabilidade, nÃ£o como regressÃ£o funcional do SA-6A.

### Leitura arquitetural

```text
PlayerInputBinding deixou de ser execuÃ§Ã£o concreta do SessionActivityPipeline.
ActivityEntryPipeline manteve ownership de ordem/lifecycle da entry.
ActivityEntryPlayerInputBindingStage executa o trabalho concreto de binding.
Movement, Camera, Permission, Release, Deactivation e RouteExit permaneceram fora do corte.
```

DÃ©bitos restantes controlados:

```text
Movement binding ainda estÃ¡ no SessionActivityPipeline.
Camera binding ainda estÃ¡ no SessionActivityPipeline.
Permission target preparation ainda serÃ¡ corte prÃ³prio.
ActivityEntryPlayerInputBindingStage ainda usa bridge/endpoint de entry jÃ¡ existente para emitir facts/snapshots.
```


---

## Checkpoint â€” SA-6B Permission target preparation stage / CLOSED / PASS

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

### Objetivo

Extrair a preparaÃ§Ã£o de `PermissionTarget` para stage real da `ActivityEntry`, sem redesenhar toda identity de permission e sem alterar a reaÃ§Ã£o local dos receivers.

### MudanÃ§a de ownership

Antes:

```text
SessionActivityPipeline.EmitMovementBindingStage
-> BeginPermissionScope
-> ResolvePermissionReceiversFromInventory
-> valida receiver identity
-> ReplaceReceivers
```

Depois:

```text
SessionActivityPipeline
-> ActivityEntryPipeline.ExecutePermissionTargetPreparation
   -> ActivityEntryPermissionTargetPreparationStage
      -> BeginPermissionScope
      -> resolve PermissionTarget no ActivityCapabilityInventory
      -> valida receiver identity
      -> ReplaceReceivers

SessionActivityPipeline.EmitMovementBindingStage
-> continua responsÃ¡vel apenas pelo MovementBinding atÃ© SA-6C
```

### Regra anti-monÃ³lito preservada

`ActivityEntryPipeline.cs` sÃ³ ordena e valida resultado. A execuÃ§Ã£o concreta fica em `ActivityEntryPermissionTargetPreparationStage`.

### Escopo preservado

```text
Movement binding nÃ£o foi movido.
Camera binding nÃ£o foi movido.
Permission reaction local nÃ£o foi alterada.
ActivityCapabilityPermissionRuntime nÃ£o foi redesenhado.
Release/Deactivation/RouteExit nÃ£o foram alterados.
```

### CritÃ©rio de smoke esperado

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
ActivityEntryPermissionTargetPreparationStarted/Completed com owner ActivityEntryPipeline
ActivityCapabilityPermissionReceiverRegistered preservado
ActivityCapabilityPermissionPublished/Applied preservado para Blocked/Allowed/Unbound
PlayerMovementPermissionApplied preservado
MovementBindingCompleted preservado
MovementBindingRetained preservado na activity_02
CameraBindingCompleted preservado
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```

### DÃ©bito controlado

`MovementBinding` ainda estÃ¡ no `SessionActivityPipeline` e serÃ¡ tratado no `SA-6C`. O `SA-6B` apenas separa a preparaÃ§Ã£o dos permission targets para que `MovementBinding` nÃ£o continue sendo o owner indireto de receiver discovery/registration.


---

## Checkpoint â€” SA-6C Movement binding stage / CLOSED / PASS

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

### Objetivo

Extrair o binding concreto de Movement para um stage real da `ActivityEntry`, sem alterar `PermissionRuntime`, reaction local, Camera, Release, Deactivation ou RouteExit.

### MudanÃ§a de ownership

Antes:

```text
SessionActivityPipeline.EmitMovementBindingStage
-> monta MovementBindingRequirement
-> chama MovementBindingAdapter
-> emite PlayerMovementBound
-> grava movement control targets
-> classifica retained/skip/fail/completed
```

Depois:

```text
SessionActivityPipeline
-> ActivityEntryPipeline.ExecuteMovementBinding
   -> ActivityEntryMovementBindingStage
      -> monta MovementBindingRequirement
      -> chama MovementBindingAdapter
      -> emite PlayerMovementBound / MovementBindingRetained / MovementBindingCompleted
      -> grava movement control targets via bridge mÃ­nima

SessionActivityPipeline
-> continua apenas lifecycle macro e chama CameraBinding apÃ³s o resultado da entry
```

### CorreÃ§Ãµes de compile aplicadas antes do smoke

O primeiro pacote `SA-6C` exigiu dois hotfixes de compilaÃ§Ã£o antes do smoke:

```text
SA-6C-compilefix-movement-binding-stage
- restaurou SessionActivityPipeline.cs completo com TryGetSnapshotPayloadForSaveOnExit preservado.

SA-6C-compilefix2-movement-binding-references
- restaurou BuildMovementBindingReferences(...) como helper passivo para montar ActivityEntryMovementBindingReference.
```

Esses hotfixes nÃ£o reintroduziram execuÃ§Ã£o concreta de Movement no `SessionActivityPipeline`.

### EvidÃªncia do smoke

O smoke manual confirmou o novo owner do binding de Movement na entry:

```text
ActivityEntryMovementBindingStarted owner='ActivityEntryPipeline'
MovementBindingStarted owner='ActivityEntryPipeline'
PlayerMovementBound owner='ActivityEntryPipeline'
MovementBindingCompleted owner='ActivityEntryPipeline'
ActivityEntryMovementBindingCompleted owner='ActivityEntryPipeline'
```

Na `activity_02`, que Ã© cenÃ¡rio negativo/no-content, o binding preservou retenÃ§Ã£o explÃ­cita:

```text
MovementBindingStarted activityId='activity_02' owner='ActivityEntryPipeline'
MovementBindingRetained activityId='activity_02' owner='ActivityEntryPipeline'
MovementBindingCompleted activityId='activity_02' owner='ActivityEntryPipeline' status='RetainedExistingBinding'
ActivityEntryMovementBindingCompleted activityId='activity_02' retainedExisting='True'
```

O fluxo de permission permaneceu preservado:

```text
ActivityCapabilityPermissionPublished/Applied state='Blocked'
PlayerMovementPermissionApplied state='Blocked'
ActivityCapabilityPermissionPublished/Applied state='Allowed'
PlayerMovementPermissionApplied state='Allowed'
ActivityCapabilityPermissionPublished/Applied state='Unbound'
PlayerMovementPermissionApplied state='Unbound'
```

Fluxos preservados:

```text
CameraBindingCompleted
MovementControlEnabled
MovementControlDisabled/Unbound via permission no exit/restart
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
```

### Leitura arquitetural

```text
MovementBinding deixou de ser execuÃ§Ã£o concreta do SessionActivityPipeline.
ActivityEntryPipeline manteve ownership de ordem/lifecycle da entry.
ActivityEntryMovementBindingStage executa o trabalho concreto de binding.
PermissionRuntime continua command/fact/snapshot e nÃ£o decide lifecycle.
PlayerMovementPermissionReceiver continua reaction local.
Camera, Release, Deactivation e RouteExit permaneceram fora do corte.
```

### Regra anti-monÃ³lito preservada

`ActivityEntryPipeline.cs` sÃ³ ordena e valida resultado. A execuÃ§Ã£o concreta fica em `ActivityEntryMovementBindingStage`. O stage legado `PlayerMovementBindingStage` foi esvaziado para nÃ£o manter trilho paralelo ativo.

### Escopo preservado

```text
PermissionRuntime nÃ£o foi redesenhado.
Permission target preparation permanece no SA-6B.
Camera binding nÃ£o foi movido.
MovementControl enable/disable nÃ£o foi movido.
Release/Deactivation/RouteExit nÃ£o foram alterados.
```

### DÃ©bito controlado

`IActivityEntryMovementBindingRuntimeBridge` Ã© transitÃ³ria e expÃµe apenas registry, adapter e targets de movement control enquanto o state de MovementControl ainda estÃ¡ no `SessionActivityPipeline`. Camera binding permanece como prÃ³ximo corte.

## SA-6D â€” Camera binding stage â€” CLOSED / PASS funcional + PASS arquitetural do corte

### Objetivo

Extrair o binding concreto de Camera para um stage real da `ActivityEntry`, sem alterar `CameraPresentation` operacional, ActivityCamera preparation/release, Release, Deactivation ou RouteExit.

### MudanÃ§a de ownership validada

Antes:

```text
SessionActivityPipeline.EmitCameraBindingStage
-> lÃª ActivitySetupInventory
-> lÃª ActivityCapabilityInventory
-> resolve CameraTarget por participante/player actor
-> chama IActivityCameraPreparationExecutor.TryRebindTargets
-> emite PlayerCameraEndpointResolved / ActivityCameraTargetBound / CameraBindingCompleted
```

Depois validado no smoke:

```text
SessionActivityPipeline
-> ActivityEntryPipeline.ExecuteCameraBinding
   -> ActivityEntryCameraBindingStage
      -> resolve CameraTarget por participante/player actor
      -> chama IActivityCameraPreparationExecutor.TryRebindTargets
      -> emite PlayerCameraEndpointResolved / ActivityCameraTargetBound / CameraBindingCompleted
```

### EvidÃªncia funcional do smoke

O smoke manual pÃ³s-compilefix confirmou:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
```

E confirmou o novo owner no binding inicial da `activity_01`:

```text
ActivityEntryCameraBindingStarted owner='ActivityEntryPipeline'
CameraBindingStarted owner='ActivityEntryPipeline'
PlayerCameraEndpointResolved owner='ActivityEntryPipeline'
ActivityCameraTargetBound owner='ActivityEntryPipeline'
CameraBindingCompleted owner='ActivityEntryPipeline'
ActivityEntryCameraBindingCompleted owner='ActivityEntryPipeline' required='1' targetBound='True' skipped='False'
```

No restart da `activity_01`, o mesmo stage foi reexecutado para `entrySequence='2'` e preservou target binding/rebind:

```text
ActivityEntryCameraBindingStarted owner='ActivityEntryPipeline'
ActivityCameraTargetsRebound
ActivityCameraTargetBound owner='ActivityEntryPipeline'
CameraBindingCompleted owner='ActivityEntryPipeline'
```

Na `activity_02` negativa/no-content, o skip explÃ­cito foi preservado:

```text
ActivityEntryCameraBindingStarted owner='ActivityEntryPipeline'
CameraBindingSkippedNoRequiredCamera owner='ActivityEntryPipeline' reasonCode='no_activity_camera_requirement'
ActivityEntryCameraBindingCompleted owner='ActivityEntryPipeline' required='0' targetBound='False' skipped='True'
```

### Checkpoints preservados

```text
MovementBindingCompleted preservado
ActivityCapabilityPermissionPublished/Applied preservado para Blocked/Allowed/Unbound
PlayerMovementPermissionApplied preservado
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```

### Leitura arquitetural

```text
CameraBinding deixou de ser execuÃ§Ã£o concreta do SessionActivityPipeline.
ActivityEntryPipeline manteve ownership de ordem/lifecycle da entry.
ActivityEntryCameraBindingStage executa o trabalho concreto de binding.
CameraPresentation operacional continua no owner atual.
ActivityCamera preparation/release nÃ£o foi movido.
Release, Deactivation e RouteExit permaneceram fora do corte.
```

### Regra anti-monÃ³lito preservada

`ActivityEntryPipeline.cs` sÃ³ ordena e valida resultado. A execuÃ§Ã£o concreta fica em `ActivityEntryCameraBindingStage`. NÃ£o foi criado trilho paralelo ativo para camera binding.

### DÃ©bito controlado

`IActivityEntryCameraBindingRuntimeBridge` permanece transitÃ³ria e expÃµe apenas inventory, participantes, resoluÃ§Ã£o de handle e `IActivityCameraPreparationExecutor` enquanto `CameraPresentation` e o state de ActivityCamera continuam nos owners atuais.

## SA-7A â€” Exit / Release Decomposition Audit â€” CLOSED / AUDIT ONLY

### Contexto

ApÃ³s os cortes SA-5A1..SA-6D, a entrada da Activity jÃ¡ possui stages explÃ­citos para ActorInventoryFeed, ActorPresentation, ActorAttributes, ActorParticipation, PlayerInput, PermissionTargetPreparation, Movement e Camera.

A saÃ­da ainda concentra release, teardown, snapshot e unload dentro do `SessionActivityPipeline`.

### DecisÃ£o

```text
NÃ£o criar ActivityExitPipeline agora.
Manter SessionActivityPipeline como owner macro de saÃ­da por enquanto.
Extrair primeiro stages determinÃ­sticos de exit/release chamados pelo SessionActivityPipeline.
```

### RazÃ£o

Exit ainda mistura rails e timings diferentes:

```text
CompleteCurrentActivity
RestartCurrentActivity
RouteExit
NavigationExit
DeactivationWindow
transition blackout
ActorPresentation release
ActorAttribute release
ActorParticipation exit
ActivityObject snapshot/release
ActivityContent unload async
RouteActivitySave payload
ClosedForRouteExit
```

Criar `ActivityExitPipeline` agora produziria owner duplicado de lifecycle. A extraÃ§Ã£o deve comeÃ§ar por stages sem alterar ordering.

### Resultado da auditoria

O prÃ³ximo corte autorizado Ã©:

```text
SA-7B â€” ActivityExitActorTeardownStage
```

Objetivo:

```text
Extrair o teardown de actors para stage dedicado:
- ActorPresentation release/retain por rail;
- ActorAttribute release;
- ActorParticipation exit;
- player participation exit quando aplicÃ¡vel.
```

### Escopo proibido no SA-7B

```text
NÃ£o criar ActivityExitPipeline.
NÃ£o mover DeactivationWindow.
NÃ£o mover ActivityContentRelease async.
NÃ£o mover ActivityObject snapshot/release.
NÃ£o mover CompleteRouteExitClosure.
NÃ£o alterar RouteActivitySave.
NÃ£o alterar CameraPresentation operacional release.
NÃ£o alterar PermissionRuntime/reaction local.
NÃ£o criar branch global player/nonplayer.
```

### CritÃ©rio de aceite do SA-7B

```text
SessionActivityPipeline decide quando o teardown roda.
ActivityExitActorTeardownStage executa o teardown determinÃ­stico.
ActorPresentationReleased/Skipped preservado.
ActorAttributeReleased preservado.
ActorParticipationExited preservado.
Ordering de Restart/Activity01ToActivity02/RouteExit preservado.
ActivityObjectSnapshotCapture preservado.
ActivityObjectRelease preservado.
ActivityContentRelease preservado.
RestartCurrentActivity PASS.
Activity01ToActivity02 PASS.
RouteExitBackToMenu PASS.
sem FATAL / Exception / route_transition_failed / foreign/stale.
```

### RelatÃ³rio

RelatÃ³rio detalhado:

```text
NewScripts/Docs/Reports/SA-7A-Exit-Release-Decomposition-Audit.md
```

## SA-7B â€” ActivityExitActorTeardownStage â€” CLOSED / PASS funcional + PASS arquitetural do corte

### Objetivo

Extrair o teardown concreto de actors da saÃ­da da Activity para um stage dedicado, sem criar `ActivityExitPipeline` e sem alterar `DeactivationWindow`, `ActivityContentRelease`, `ActivityObject snapshot/release`, `RouteActivitySave`, `CameraPresentation` operacional ou `RouteExit` macro.

### MudanÃ§a de ownership aplicada

Antes:

```text
SessionActivityPipeline
-> EmitActorPresentationReleaseGenericStage
-> EmitActorAttributeReleaseFromInventoryStage
-> EmitActorParticipationExitFromInventoryStage
-> PlayerActorParticipationExit quando aplicÃ¡vel
```

Depois:

```text
SessionActivityPipeline
-> decide quando o teardown roda conforme rail atual
-> ActivityExitActorTeardownStage
   -> ActorPresentation release/retain por rail
   -> ActorAttribute release
   -> ActorParticipation exit
   -> PlayerActorParticipation exit quando aplicÃ¡vel
```

### Regra preservada

```text
SessionActivityPipeline continua owner do lifecycle macro de saÃ­da.
ActivityExitActorTeardownStage executa apenas o teardown determinÃ­stico de actors.
NÃ£o foi criado ActivityExitPipeline.
```

### Escopo preservado

```text
DeactivationWindow nÃ£o foi movida.
ActivityObjectSnapshotCapture nÃ£o foi movido.
ActivityObjectRelease nÃ£o foi movido.
ActivityContent scene unload async nÃ£o foi movido.
CompleteRouteExitClosure nÃ£o foi movido.
RouteActivitySave nÃ£o foi alterado.
CameraPresentation operacional release nÃ£o foi alterado.
PermissionRuntime/reaction local nÃ£o foi alterado.
```

### Bridge transitÃ³ria

`IActivityExitActorTeardownRuntimeBridge` foi criada como ponte mÃ­nima para o stage acessar state runtime ainda preso no `SessionActivityPipeline`:

```text
active ActorPresentation handles
active ActorAttribute capabilities
active ActorParticipation records
player participant binding resolution
player actor participation adapter/registry
```

Essa bridge Ã© transitÃ³ria e nÃ£o deve virar manager/coordinator.

### CritÃ©rio de smoke

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
ActorPresentationReleaseStarted/Released/Skipped/Completed preservado
ActorAttributeReleaseStarted/Released/Completed preservado
ActorParticipationExitStarted/Exited/Completed preservado
PlayerActorParticipationExit preservado quando aplicÃ¡vel
ActivityObjectSnapshotCapture PASS
ActivityObjectRelease PASS
ActivityObjectContributorUnregister PASS
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```

### Smoke / evidÃªncia

Smoke manual validado apÃ³s compile fix de `ActorParticipationExitCommand`.

O log confirmou:

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'

ActivityExitActorTeardownStage como owner visÃ­vel do teardown de actors
ActorPresentationReleaseStarted/Released/Skipped/Completed preservado
ActorAttributeReleaseStarted/Released/Completed preservado
ActorParticipationExitStarted/Exited/Completed preservado
ActorParticipationPlayerExitStarted/Completed preservado
ActivityCapabilityPermission Unbound preservado durante player exit
ActivityObjectSnapshotCapture PASS
ActivityObjectRelease PASS
ActivityObjectContributorUnregister PASS
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```

EvidÃªncia funcional relevante:

```text
ActivityExitActorTeardownStage executa ActorPresentation release no rail ActivityExit e RouteExit.
ActivityExitActorTeardownStage executa ActorAttribute release sem failures.
ActivityExitActorTeardownStage executa ActorParticipation exit sem failures.
ActivityObject snapshot/release/unregister continuam no caminho existente e passam.
ActivityContent scene unload async continua preservado fora do corte.
RouteExitBackToMenu aplica Menu route apÃ³s ClosedForRouteExit.
```

### Status

`SA-7B` estÃ¡ `CLOSED / PASS funcional + PASS arquitetural do corte`.

### DÃ©bito controlado

```text
IActivityExitActorTeardownRuntimeBridge permanece transitÃ³ria.
ActivityObject snapshot/release ainda pertence a corte futuro.
ActivityContent unload async ainda pertence a corte futuro.
DeactivationWindow e RouteExit macro continuam ownership do SessionActivityPipeline.
```



## SA-7C â€” ActivityObject Snapshot / Release / Unregister / Content Release Audit

Status: `CLOSED / AUDIT ONLY`

### DecisÃ£o

O SA-7C confirmou que o bloco de objetos da saÃ­da nÃ£o deve ser movido em um Ãºnico patch.

A ordem segura passa a ser:

```text
SA-7D â€” ActivityObjectSnapshotCaptureStage
SA-7E â€” ActivityObjectReleaseStage
SA-7F â€” ActivityObjectContributorUnregisterStage
SA-7G â€” ActivityContentReleaseAsync audit/extraction
```

### Motivo

O fluxo atual mistura responsabilidades diferentes:

```text
Snapshot capture produz payload para RouteActivitySave.
Object release executa side-effects em endpoints locais.
Contributor unregister limpa discovery/runtime state.
ActivityContentRelease controla pending operation e scene unload async.
```

Mover tudo junto aumentaria risco de regressÃ£o em restart, activity transition e route-exit.

### Owner preservado

```text
SessionActivityPipeline continua dono do macro lifecycle de saÃ­da.
Stages dedicados executam passos determinÃ­sticos.
RouteActivitySave continua consumidor de payload, nÃ£o owner de snapshot capture.
SaveRuntime continua persistÃªncia, nÃ£o decide snapshot.
```

### Bridge identificada

A classe interna `ActivityObjectExitStage` foi classificada como bridge transitÃ³ria, nÃ£o stage Base 2.0 real:

```text
CaptureSnapshot -> EmitObjectSnapshotCaptureStageCore
Release -> EmitObjectReleaseStageCore
UnregisterContributors -> EmitObjectContributorUnregisterStageCore
```

Ela deve ser substituÃ­da gradualmente por stages reais em arquivos prÃ³prios.

### PrÃ³ximo corte aceito

`SA-7D â€” ActivityObjectSnapshotCaptureStage`.

Escopo permitido:

```text
Extrair snapshot capture para stage dedicado.
Manter TryGetSnapshotPayloadForSaveOnExit como API pÃºblica.
Registrar payload/failure flags por bridge mÃ­nima.
Preservar facts/checkpoints de ActivityObjectSnapshotCapture.
```

Escopo proibido:

```text
NÃ£o mover ObjectRelease.
NÃ£o mover ContributorUnregister.
NÃ£o mover ActivityContentRelease async.
NÃ£o alterar RouteActivitySave.
NÃ£o criar ActivityExitPipeline.
NÃ£o executar save dentro do stage.
```

### Status

`SA-7C` estÃ¡ `CLOSED / AUDIT ONLY`.


## SA-7D â€” ActivityObjectSnapshotCaptureStage

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`

### DecisÃ£o

O snapshot capture de objetos da Activity foi extraÃ­do do bloco concreto do `SessionActivityPipeline` para um stage dedicado:

```text
SessionActivityPipeline
-> decide quando a saÃ­da/dematerialization exige snapshot
-> ActivityObjectSnapshotCaptureStage
   -> valida discovery/result atual
   -> resolve SnapshotProvider pelo ActivityCapabilityInventory
   -> executa ActivityObjectSnapshotCaptureCommand
   -> registra payload/failure flags por bridge mÃ­nima
   -> preserva facts/checkpoints de ActivityObjectSnapshotCapture
```

### Owner preservado

```text
SessionActivityPipeline continua dono do macro lifecycle de saÃ­da.
ActivityObjectSnapshotCaptureStage executa apenas o passo determinÃ­stico de captura.
RouteActivitySave continua consumidor do payload; nÃ£o decide capture.
SaveRuntime continua backend/executor de persistÃªncia; nÃ£o participa deste stage.
```

### Escopo aplicado

```text
Criado ActivityObjectSnapshotCaptureStage.
Criada bridge transitÃ³ria IActivityObjectSnapshotCaptureRuntimeBridge.
EmitObjectSnapshotCaptureStage agora delega ao stage dedicado.
Removido o caminho ativo EmitObjectSnapshotCaptureStageCore do macro pipeline.
ActivityObjectExitStage deixou de possuir CaptureSnapshot e permanece sÃ³ como bridge transitÃ³ria para Release/Unregister.
```

### Escopo explicitamente nÃ£o alterado

```text
ObjectRelease nÃ£o foi movido.
ContributorUnregister nÃ£o foi movido.
ActivityContentRelease async nÃ£o foi movido.
RouteActivitySave nÃ£o foi alterado.
TryGetSnapshotPayloadForSaveOnExit foi preservado como API pÃºblica.
ActivityExitPipeline nÃ£o foi criado.
```

### CritÃ©rio de smoke

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'

ActivityObjectSnapshotCaptureStarted/Captured/Completed preservado
ActivityObjectSnapshotCapture checkpoint PASS
ActivityObjectRelease PASS
ActivityObjectContributorUnregister PASS
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
RouteActivitySave payload continua consumÃ­vel quando existir snapshot
```

### Status

`SA-7D` estÃ¡ `CLOSED / PASS funcional + PASS arquitetural do corte`.


### Smoke / evidÃªncia SA-7D

Smoke manual validado apÃ³s compile. O log confirmou:

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'

ActivityObjectSnapshotCaptureStage executado como owner do snapshot capture
ActivityObjectSnapshotCaptureStarted preservado
ActivityObjectSnapshotCaptureCompleted preservado
ActivityObjectSnapshotCapture checkpointStatus='Passed'
ActivityObjectRelease checkpointStatus='Passed'
ActivityObjectContributorUnregister checkpointStatus='Passed'
RestartCurrentActivity checkpointStatus='Passed'
Activity01ToActivity02 checkpointStatus='Passed'
RouteExitBackToMenu checkpointStatus='Passed'
RouteActivitySaveSaveOnExitStageCompleted preservado no BackToMenu
```

EvidÃªncia observada:

```text
ActivityObjectSnapshotCaptureStarted owner='ActivityObjectSnapshotCaptureStage' activityId='activity_01' entrySequence='1'
ActivityObjectSnapshotCaptureCompleted owner='ActivityObjectSnapshotCaptureStage' activityId='activity_01' entrySequence='1' capturedCount='1' failedCount='0' targetIds='test_object_01' hasTransformPayload='true'
ActivityObjectSnapshotCapture checkpointStatus='Passed' activityId='activity_01' entrySequence='1' capturedCount='1' captureCompleted='true' captureFailed='false'

ActivityObjectSnapshotCaptureCompleted owner='ActivityObjectSnapshotCaptureStage' activityId='activity_01' entrySequence='2' capturedCount='1' failedCount='0' targetIds='test_object_01' hasTransformPayload='true'
ActivityObjectSnapshotCapture checkpointStatus='Passed' activityId='activity_01' entrySequence='2' capturedCount='1' captureCompleted='true' captureFailed='false'

ActivityObjectSnapshotCaptureCompleted owner='ActivityObjectSnapshotCaptureStage' activityId='activity_02' entrySequence='3' capturedCount='0' failedCount='0' targetIds='<none>' hasTransformPayload='false'
ActivityObjectSnapshotCapture checkpointStatus='Passed' activityId='activity_02' entrySequence='3' capturedCount='0' captureCompleted='true' captureFailed='false'

RestartCurrentActivity checkpointStatus='Passed'
Activity01ToActivity02 checkpointStatus='Passed'
RouteExitBackToMenu checkpointStatus='Passed'
```

ConclusÃ£o arquitetural:

```text
SessionActivityPipeline continua dono do macro lifecycle de saÃ­da/dematerialization.
ActivityObjectSnapshotCaptureStage executa apenas o passo determinÃ­stico de capture.
RouteActivitySave continua consumidor do payload; nÃ£o virou owner de snapshot capture.
ObjectRelease, ContributorUnregister, ActivityContentRelease async, DeactivationWindow e RouteExit nÃ£o foram movidos.
NÃ£o foi criado ActivityExitPipeline.
```

DÃ©bito controlado:

```text
IActivityObjectSnapshotCaptureRuntimeBridge permanece transitÃ³ria.
ActivityObjectReleaseStage fechado no SA-7E.
ActivityObjectContributorUnregisterStage fechado no SA-7F.
ActivityContentRelease async ainda exige auditoria/extraction prÃ³pria no SA-7G.
```

## SA-7E â€” ActivityObjectReleaseStage

Status: `Applied / Pending smoke`

### DecisÃ£o

O release de objetos da Activity foi extraÃ­do do caminho ativo do `SessionActivityPipeline` para um stage dedicado:

```text
SessionActivityPipeline
-> decide quando a saÃ­da/dematerialization exige release de objetos
-> ActivityObjectReleaseStage
   -> valida discovery/result atual
   -> resolve ReleaseEndpoint pelo ActivityCapabilityInventory
   -> executa ActivityObjectReleaseCommand
   -> preserva facts/checkpoints de ObjectRelease
```

### Owner preservado

```text
SessionActivityPipeline continua dono do macro lifecycle de saÃ­da.
ActivityObjectReleaseStage executa apenas o passo determinÃ­stico de release.
ActivityContentRelease async continua dono do unload de scenes.
RouteActivitySave continua consumidor do payload jÃ¡ capturado; nÃ£o decide release.
```

### Escopo aplicado

```text
Criado ActivityObjectReleaseStage.
Criada bridge transitÃ³ria IActivityObjectReleaseRuntimeBridge.
EmitObjectReleaseStage agora delega ao stage dedicado.
ActivityObjectExitStage deixa de possuir Release e permanece apenas como bridge transitÃ³ria para ContributorUnregister.
```

### Escopo explicitamente nÃ£o alterado

```text
ContributorUnregister nÃ£o foi movido.
ActivityContentRelease async nÃ£o foi movido.
RouteActivitySave nÃ£o foi alterado.
ActivityObjectSnapshotCaptureStage nÃ£o foi alterado.
DeactivationWindow e RouteExit nÃ£o foram movidos.
ActivityExitPipeline nÃ£o foi criado.
```

### CritÃ©rio de smoke

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'

ActivityObjectReleaseStarted/Applied/Completed preservado
ActivityObjectRelease checkpoint PASS
ActivityObjectSnapshotCapture checkpoint PASS
ActivityObjectContributorUnregister checkpoint PASS
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```


### Status

`SA-7E` estÃ¡ `CLOSED / PASS funcional + PASS arquitetural do corte`.

### EvidÃªncia de smoke

Smoke manual validado apÃ³s compile.

Resultado observado:

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
```

EvidÃªncia funcional relevante:

```text
ActivityObjectSnapshotCaptureStage:
  ActivityObjectSnapshotCaptureStarted/Completed preservado
  capturedCount='1'
  failedCount='0'
  targetIds='test_object_01'
  hasTransformPayload='true'

ActivityObjectReleaseStage:
  ActivityObjectReleaseStarted preservado
  ActivityObjectReleaseCompleted preservado
  commandCount='1'
  appliedCount='1'
  skippedCount='0'
  failedCount='0'

ActivityObjectSnapshotCapture checkpointStatus='Passed'
ActivityObjectRelease checkpointStatus='Passed'
ActivityObjectContributorUnregister checkpointStatus='Passed'
RestartCurrentActivity checkpointStatus='Passed'
Activity01ToActivity02 checkpointStatus='Passed'
RouteExitBackToMenu checkpointStatus='Passed'
```

ObservaÃ§Ã£o de observabilidade:

```text
NÃ£o hÃ¡ linha OBS literal ActivityObjectReleaseApplied no smoke.
A aplicaÃ§Ã£o estÃ¡ confirmada por ActivityObjectReleaseCompleted appliedCount='1'
e pelo checkpoint ActivityObjectRelease checkpointStatus='Passed' appliedCount='1'.
Se necessÃ¡rio, emitir ActivityObjectReleaseApplied como OBS explÃ­cito deve ser hygiene local futuro,
nÃ£o bloqueio funcional deste corte.
```

ConclusÃ£o arquitetural:

```text
SessionActivityPipeline continua dono do macro lifecycle de saÃ­da/dematerialization.
ActivityObjectReleaseStage executa apenas o passo determinÃ­stico de release.
ActivityObjectSnapshotCaptureStage continua separado e executa antes do release.
ContributorUnregister, ActivityContentRelease async, RouteActivitySave, DeactivationWindow e RouteExit nÃ£o foram movidos.
NÃ£o foi criado ActivityExitPipeline.
```

DÃ©bito controlado:

```text
IActivityObjectReleaseRuntimeBridge permanece transitÃ³ria.
ActivityObjectContributorUnregisterStage fechado no SA-7F.
ActivityContentRelease async ainda exige auditoria/extraction prÃ³pria no SA-7G.
```


## SA-7F â€” ActivityObjectContributorUnregisterStage

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

### DecisÃ£o

O unregister de contributors de ActivityObject foi extraÃ­do do caminho ativo do `SessionActivityPipeline` para um stage dedicado:

```text
SessionActivityPipeline
-> decide quando a saÃ­da/dematerialization exige unregister de contributors
-> ActivityObjectContributorUnregisterStage
   -> valida discovery result da entry
   -> emite ActivityObjectContributorUnregisterStarted
   -> emite ActivityObjectContributorUnregistered por contributor quando houver
   -> limpa CurrentActivityObjectContributorDiscoveryResult
   -> emite ActivityObjectContributorUnregisterCompleted
```

### Owner preservado

```text
SessionActivityPipeline continua dono do macro lifecycle de saÃ­da/dematerialization.
ActivityObjectContributorUnregisterStage executa apenas o passo determinÃ­stico de unregister.
ActivityObjectSnapshotCaptureStage continua separado e executa antes do release.
ActivityObjectReleaseStage continua separado e executa antes do unregister.
ActivityContentRelease async continua responsÃ¡vel pelo unload de scenes.
RouteActivitySave continua consumidor do payload capturado; nÃ£o decide unregister.
```

### Escopo aplicado

```text
Criado ActivityObjectContributorUnregisterStage.
Criada bridge transitÃ³ria IActivityObjectContributorUnregisterRuntimeBridge.
Removido ActivityObjectExitStage do caminho ativo.
EmitObjectContributorUnregisterStage agora delega ao stage dedicado.
CurrentActivityObjectContributorDiscoveryResult passa a ser limpo pelo stage dedicado.
```

### Escopo explicitamente nÃ£o alterado

```text
ActivityObjectSnapshotCaptureStage nÃ£o foi alterado.
ActivityObjectReleaseStage nÃ£o foi alterado.
ActivityContentRelease async nÃ£o foi movido.
RouteActivitySave nÃ£o foi alterado.
SaveRuntime nÃ£o foi alterado.
DeactivationWindow e RouteExit nÃ£o foram movidos.
ActivityExitPipeline nÃ£o foi criado.
```

### EvidÃªncia de smoke

Smoke manual validado apÃ³s compile.

Resultado observado:

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
```

EvidÃªncia funcional relevante:

```text
ActivityObjectContributorUnregisterStage:
  ActivityObjectContributorUnregisterStarted preservado
  ActivityObjectContributorUnregistered preservado em activity_01
  ActivityObjectContributorUnregisterCompleted preservado
  owner='ActivityObjectContributorUnregisterStage'

activity_01:
  unregisteredCount='1'
  skippedNoContributors='False'
  targetId='test_object_01'

activity_02:
  unregisteredCount='0'
  skippedNoContributors='True'

ActivityObjectContributorUnregister checkpointStatus='Passed'
ActivityObjectRelease checkpointStatus='Passed'
ActivityObjectSnapshotCapture checkpointStatus='Passed'
RestartCurrentActivity checkpointStatus='Passed'
Activity01ToActivity02 checkpointStatus='Passed'
RouteExitBackToMenu checkpointStatus='Passed'
```

ConclusÃ£o arquitetural:

```text
SessionActivityPipeline continua dono do macro lifecycle de saÃ­da/dematerialization.
ActivityObjectContributorUnregisterStage executa apenas o passo determinÃ­stico de unregister.
Snapshot capture, object release e contributor unregister agora estÃ£o separados em stages prÃ³prios.
ActivityContentRelease async, RouteActivitySave, DeactivationWindow e RouteExit nÃ£o foram movidos.
NÃ£o foi criado ActivityExitPipeline.
```

DÃ©bito controlado:

```text
IActivityObjectContributorUnregisterRuntimeBridge permanece transitÃ³ria.
ActivityContentRelease async ainda exige auditoria/extraction prÃ³pria no SA-7G.
```


## SA-7G â€” ActivityContentReleaseAsync audit

Status: `CLOSED / AUDIT ONLY`.

### DecisÃ£o

NÃ£o mover `ActivityContentRelease async` inteiro agora.

O bloco atual mistura:

```text
ActivityObjectSnapshotCaptureStage
ActivityObjectReleaseStage
ActivityContentSceneUnloadCommand
SessionActivityPendingOperation
async unload callback
ActivityContentReleaseCompleted
ActivityObjectContributorUnregisterStage
Restart continuation
Activity transition continuation
RouteExit closure
Deactivation continuation
```

Mover tudo para um stage/pipeline novo criaria risco de owner duplicado do macro lifecycle.

### Owner correto

```text
SessionActivityPipeline continua dono do macro lifecycle de saÃ­da/dematerialization.
ActivityContent scene unload dispatch pode virar stage determinÃ­stico.
UnityActivityContentSceneReleaseAdapter continua adapter de side-effect Unity.
UnitySessionActivityPendingOperationRunner continua bridge async tÃ©cnica.
RouteActivitySave continua consumidor do payload; nÃ£o decide unload/release.
```

### PrÃ³ximo corte recomendado

```text
SA-7G1 â€” ActivityContentSceneUnloadDispatchStage
```

Escopo do prÃ³ximo corte:

```text
Extrair apenas:
- validaÃ§Ã£o do prÃ³ximo loaded scene record;
- criaÃ§Ã£o de ActivityContentSceneUnloadCommand;
- criaÃ§Ã£o de SessionActivityPendingOperation;
- SetPendingOperation;
- ActivityContentSceneUnloadCommandIssued;
- chamada a RunActivityContentReleaseOperation.
```

Fica proibido no `SA-7G1`:

```text
NÃ£o mover CompleteActivityContentSceneUnloadOperation.
NÃ£o mover FinalizeActivityContentReleaseCompleted.
NÃ£o mover FailPendingOperation.
NÃ£o mover PendingActivityContentReleaseContext.
NÃ£o alterar RouteExit closure.
NÃ£o alterar restart/activity transition/deactivation continuation.
NÃ£o criar ActivityContentReleasePipeline.
NÃ£o criar ActivityExitPipeline.
```

### CritÃ©rio de aceite futuro

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
ActivityContentSceneUnloadCommandIssued preservado
ActivityContentSceneUnloaded preservado
ActivityContentReleaseCompleted preservado
ActivityObjectSnapshotCapture PASS
ActivityObjectRelease PASS
ActivityObjectContributorUnregister PASS
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```

### DÃ©bito documentado

```text
ExecuteNextActivityContentSceneRelease ainda Ã© bridge transitÃ³ria dentro do SessionActivityPipeline.
CompleteActivityContentSceneUnloadOperation permanece macro continuation owner.
PendingActivityContentReleaseContext permanece state tÃ©cnico do macro pipeline.
```

## SA-7G1 â€” ActivityContentSceneUnloadDispatchStage

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

### DecisÃ£o aplicada

O dispatch do unload async de uma scene de `ActivityContent` foi extraÃ­do para um stage dedicado:

```text
SessionActivityPipeline
-> mantÃ©m PendingActivityContentReleaseContext
-> decide que precisa descarregar a prÃ³xima scene
-> ActivityContentSceneUnloadDispatchStage
   -> valida LoadedSet e NextSceneIndex
   -> monta ActivityContentSceneUnloadCommand
   -> monta SessionActivityPendingOperation
   -> seta pending operation
   -> emite ActivityContentSceneUnloadCommandIssued
   -> chama RunActivityContentReleaseOperation
```

### Owner preservado

```text
SessionActivityPipeline continua dono do macro lifecycle de saÃ­da/dematerialization.
ActivityContentSceneUnloadDispatchStage executa apenas o dispatch determinÃ­stico do unload de uma scene.
CompleteActivityContentSceneUnloadOperation continua no SessionActivityPipeline como callback/continuation macro.
FinalizeActivityContentReleaseCompleted continua no SessionActivityPipeline.
FailPendingOperation continua no SessionActivityPipeline.
PendingActivityContentReleaseContext continua state tÃ©cnico do macro pipeline.
```

### Escopo aplicado

```text
Criado ActivityContentSceneUnloadDispatchStage.
Criada bridge transitÃ³ria IActivityContentSceneUnloadDispatchRuntimeBridge.
ExecuteNextActivityContentSceneRelease deixou de montar diretamente command/pending operation/runner call.
ActivityContentSceneUnloadCommandIssued foi preservado pelo stage dedicado.
Pending operation ActivityContentSceneUnload continua sendo criada antes do runner async.
```

### Escopo explicitamente nÃ£o alterado

```text
NÃ£o moveu CompleteActivityContentSceneUnloadOperation.
NÃ£o moveu FinalizeActivityContentReleaseCompleted.
NÃ£o moveu FailPendingOperation.
NÃ£o moveu PendingActivityContentReleaseContext.
NÃ£o alterou UnityActivityContentSceneReleaseAdapter.
NÃ£o alterou UnitySessionActivityPendingOperationRunner.
NÃ£o alterou restart/activity transition/deactivation continuation.
NÃ£o alterou RouteExit closure.
NÃ£o alterou RouteActivitySave.
NÃ£o criou ActivityContentReleasePipeline.
NÃ£o criou ActivityExitPipeline.
```

### CritÃ©rio de smoke

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'

ActivityContentSceneUnloadCommandIssued preservado
ActivityContentSceneUnloaded preservado
ActivityContentReleaseCompleted preservado
ActivityObjectSnapshotCapture checkpoint PASS
ActivityObjectRelease checkpoint PASS
ActivityObjectContributorUnregister checkpoint PASS
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
pendingOperation ActivityContentSceneUnload observado nos releases com content
activity_02 no-content preserva skip explÃ­cito
```

### DÃ©bito controlado

```text
IActivityContentSceneUnloadDispatchRuntimeBridge Ã© transitÃ³ria.
CompleteActivityContentSceneUnloadOperation ainda concentra continuation macro.
ActivityContentRelease finalization ainda deve ser auditada antes de qualquer extraÃ§Ã£o futura.
```

## SA-7G2A â€” ActivityContentReleaseFinalizationStage

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

### DecisÃ£o aplicada

A finalizaÃ§Ã£o determinÃ­stica de `ActivityContentRelease` foi extraÃ­da para stage dedicado:

```text
SessionActivityPipeline
-> decide que o content release chegou ao ponto de finalizaÃ§Ã£o
-> ActivityContentReleaseFinalizationStage
   -> emite ActivityContentReleaseFinalizationStarted
   -> preserva ActivityContentReleaseCompleted
   -> preserva SessionActivityDematerializationCompleted
   -> emite ActivityContentReleaseFinalizationCleanupStarted
   -> chama ActivityObjectContributorUnregisterStage
   -> limpa CurrentActivityContentLoadedSet
   -> limpa PendingActivityContentReleaseContext
   -> limpa awaiting continuation flag
   -> emite ActivityContentReleaseFinalizationCleanupCompleted
   -> emite ActivityContentReleaseFinalizationCompleted
-> SessionActivityPipeline continua a continuation macro
```

### Owner preservado

```text
SessionActivityPipeline continua dono do macro lifecycle.
ActivityContentReleaseFinalizationStage fecha apenas o release determinÃ­stico.
StartPendingRestartEntry permanece no SessionActivityPipeline.
CompleteRouteExitClosure permanece no SessionActivityPipeline.
ContinueAfterDeactivationAsync permanece no SessionActivityPipeline.
NextActivity continuation permanece no SessionActivityPipeline.
```

### Observabilidade obrigatÃ³ria aplicada

O corte preserva os nomes canÃ´nicos:

```text
ActivityContentReleaseCompleted
SessionActivityDematerializationCompleted
ActivityObjectContributorUnregisterStarted
ActivityObjectContributorUnregistered
ActivityObjectContributorUnregisterCompleted
```

E adiciona os eventos explÃ­citos:

```text
ActivityContentReleaseFinalizationStarted
ActivityContentReleaseFinalizationCleanupStarted
ActivityContentReleaseFinalizationCleanupCompleted
ActivityContentReleaseFinalizationCompleted
```

Campos observÃ¡veis adicionados:

```text
owner='ActivityContentReleaseFinalizationStage'
pipelineId
sessionStateId
activityId
entrySequence
stage
source
reason
completionKind
status
loadedSceneCount
releasedSceneCount
skippedNoContent
pendingReleaseContextPresentBefore
pendingReleaseContextPresentAfter
loadedSetPresentBefore
loadedSetPresentAfter
awaitingContinuationBefore
awaitingContinuationAfter
continuationKind
```

### Escopo explicitamente nÃ£o alterado

```text
CompleteActivityContentSceneUnloadOperation nÃ£o foi movido.
FailPendingOperation nÃ£o foi movido.
StartPendingRestartEntry nÃ£o foi movido.
CompleteRouteExitClosure nÃ£o foi movido.
ContinueAfterDeactivationAsync nÃ£o foi movido.
NextActivity continuation nÃ£o foi movido.
UnityActivityContentSceneReleaseAdapter nÃ£o foi alterado.
UnitySessionActivityPendingOperationRunner nÃ£o foi alterado.
RouteActivitySave nÃ£o foi alterado.
NÃ£o foi criado ActivityContentReleasePipeline.
NÃ£o foi criado ActivityExitPipeline.
```

### CritÃ©rio de smoke

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'

ActivityContentReleaseFinalizationStarted aparece
ActivityContentReleaseCompleted preservado
SessionActivityDematerializationCompleted preservado
ActivityContentReleaseFinalizationCleanupStarted aparece
ActivityContentReleaseFinalizationCleanupCompleted aparece
ActivityContentReleaseFinalizationCompleted aparece
ActivityObjectContributorUnregisterStarted/Completed preservado
pendingReleaseContextPresentBefore/After observado
loadedSetPresentBefore/After observado
awaitingContinuationBefore/After observado
continuationKind observado

ActivityObjectSnapshotCapture PASS
ActivityObjectRelease PASS
ActivityObjectContributorUnregister PASS
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
activity_02 no-content preserva skip explÃ­cito
```


## SA-7G2A-H1 â€” ActivityContentReleaseCompleted observability alias



### Status

`SA-7G2A-H1` estÃ¡ `CLOSED / PASS funcional + PASS arquitetural do hygiene`.

### EvidÃªncia de smoke

Smoke manual validado apÃ³s aplicaÃ§Ã£o do H1.

Resultado observado:

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
```

Observabilidade corrigida:

```text
event='ActivityContentReleaseCompleted'
owner='ActivityContentReleaseFinalizationStage'
```

O evento aparece nos trÃªs caminhos relevantes:

```text
RestartCurrentActivity:
  activityId='activity_01'
  entrySequence='1'
  status='Unloaded'
  loadedSceneCount='1'
  releasedSceneCount='1'
  skippedNoContent='false'
  continuationKind='RestartCurrentActivity'

Activity01ToActivity02:
  activityId='activity_01'
  entrySequence='2'
  status='Unloaded'
  loadedSceneCount='1'
  releasedSceneCount='1'
  skippedNoContent='false'
  continuationKind='CompleteActivity'

RouteExitBackToMenu:
  activityId='activity_02'
  entrySequence='3'
  status='SkippedNoContent'
  loadedSceneCount='0'
  releasedSceneCount='0'
  skippedNoContent='true'
  continuationKind='RouteExit'
```

Demais eventos de finalization preservados:

```text
ActivityContentReleaseFinalizationStarted
SessionActivityDematerializationCompleted
ActivityContentReleaseFinalizationCleanupStarted
ActivityObjectContributorUnregisterStarted
ActivityObjectContributorUnregisterCompleted
ActivityContentReleaseFinalizationCleanupCompleted
ActivityContentReleaseFinalizationCompleted
```

Checkpoints preservados:

```text
ActivityObjectSnapshotCapture checkpointStatus='Passed'
ActivityObjectRelease checkpointStatus='Passed'
ActivityObjectContributorUnregister checkpointStatus='Passed'
RestartCurrentActivity checkpointStatus='Passed'
Activity01ToActivity02 checkpointStatus='Passed'
RouteExitBackToMenu checkpointStatus='Passed'
```

ConclusÃ£o arquitetural:

```text
O H1 corrigiu somente a observabilidade literal exigida pelo SA-7G2.
NÃ£o houve alteraÃ§Ã£o de lifecycle.
NÃ£o houve alteraÃ§Ã£o de cleanup.
NÃ£o houve alteraÃ§Ã£o de continuation macro.
NÃ£o houve ActivityContentReleasePipeline.
NÃ£o houve ActivityExitPipeline.
SessionActivityPipeline segue dono da continuaÃ§Ã£o macro.
ActivityContentReleaseFinalizationStage segue responsÃ¡vel apenas pela finalizaÃ§Ã£o determinÃ­stica.
```

### Motivo

O smoke de `SA-7G2A` validou funcionalmente o fluxo de finalization, cleanup e continuation, mas a observabilidade literal `event='ActivityContentReleaseCompleted'` nÃ£o apareceu no log. O nome `ActivityContentReleaseCompleted` aparecia como `stage` e como fact interno, mas nÃ£o como evento OBS explÃ­cito do stage.

Como `SA-7G2` exigiu observabilidade canÃ´nica preservada, este hygiene adiciona um alias/fact OBS explÃ­cito sem alterar lifecycle.

### AlteraÃ§Ã£o

`ActivityContentReleaseFinalizationStage` passa a emitir:

```text
event='ActivityContentReleaseCompleted'
owner='ActivityContentReleaseFinalizationStage'
```

logo apÃ³s `SessionActivityFactKind.ActivityContentReleaseCompleted` ser registrado e antes de `SessionActivityDematerializationCompleted`.

Campos preservados:

```text
pipelineId
sessionStateId
activityId
entrySequence
stage='ActivityContentReleaseCompleted'
source
reason
completionKind
status
loadedSceneCount
releasedSceneCount
skippedNoContent
pendingReleaseContextPresentBefore
pendingReleaseContextPresentAfter
loadedSetPresentBefore
loadedSetPresentAfter
awaitingContinuationBefore
awaitingContinuationAfter
continuationKind
```

### Escopo

```text
Sem alteraÃ§Ã£o de lifecycle.
Sem alteraÃ§Ã£o de cleanup.
Sem alteraÃ§Ã£o de continuation macro.
Sem alteraÃ§Ã£o de RouteExit/Restart/NextActivity.
Sem novo pipeline.
Sem fallback.
```

### Smoke necessÃ¡rio

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
event='ActivityContentReleaseCompleted' owner='ActivityContentReleaseFinalizationStage' aparece
ActivityContentReleaseFinalizationStarted/CleanupStarted/CleanupCompleted/Completed preservados
SessionActivityDematerializationCompleted preservado
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```


## SA-7G2B â€” ActivityContentReleaseContinuation audit

Status: `CLOSED / AUDIT ONLY`.

### DecisÃ£o

NÃ£o criar `ActivityContentReleaseContinuationStage` ainda.

A continuaÃ§Ã£o pÃ³s-release ainda Ã© macro lifecycle do `SessionActivityPipeline`.

### Owner correto

```text
SessionActivityPipeline continua dono de:
- restart continuation;
- next activity continuation;
- route-exit closure;
- deactivation/complete continuation.
```

### PrÃ³ximo corte recomendado

```text
SA-7G2B-H1 â€” ActivityContentReleaseContinuationObservability
```

Escopo:

```text
Adicionar fact/OBS explÃ­cito para:
- ActivityContentReleaseContinuationResolved;
- ActivityContentReleaseContinuationStarted;
- ActivityContentReleaseContinuationCompleted.
```

Owner obrigatÃ³rio:

```text
owner='SessionActivityPipeline'
```

Campos obrigatÃ³rios:

```text
pipelineId
sessionStateId
activityId
entrySequence
stage
source
reason
continuationKind
continuationTargetActivityId
continuationTargetEntrySequence
hasPendingRestartTransition
hasPendingRouteExit
hasNextActivity
routeExitRequested
deactivationCompleted
activityCompletionRequested
releaseStatus
skippedNoContent
loadedSceneCount
releasedSceneCount
previousStage
nextStage
```

### Escopo proibido no H1

```text
NÃ£o criar ActivityContentReleaseContinuationStage.
NÃ£o criar ActivityContentReleasePipeline.
NÃ£o criar ActivityExitPipeline.
NÃ£o mover StartPendingRestartEntry.
NÃ£o mover CompleteRouteExitClosure.
NÃ£o mover ContinueAfterDeactivationAsync.
NÃ£o alterar route-exit handoff.
NÃ£o alterar restart lifecycle.
NÃ£o alterar next activity lifecycle.
```

### CritÃ©rio de aceite futuro

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
ActivityContentReleaseContinuationResolved aparece
ActivityContentReleaseContinuationStarted aparece
ActivityContentReleaseContinuationCompleted aparece quando aplicÃ¡vel
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
owner='SessionActivityPipeline'
```

## SA-7G2B-H1 â€” ActivityContentReleaseContinuationObservability

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

### DecisÃ£o aplicada

Observabilidade explÃ­cita foi adicionada para a continuaÃ§Ã£o macro pÃ³s-`ActivityContentReleaseFinalizationStage`.

Eventos novos:

```text
ActivityContentReleaseContinuationResolved
ActivityContentReleaseContinuationStarted
ActivityContentReleaseContinuationCompleted
```

Owner obrigatÃ³rio preservado:

```text
owner='SessionActivityPipeline'
```

### Regra arquitetural

```text
Pipeline decide a continuaÃ§Ã£o.
Logs tornam a decisÃ£o verificÃ¡vel.
```

O patch nÃ£o cria `ActivityContentReleaseContinuationStage`, nÃ£o cria `ActivityContentReleasePipeline` e nÃ£o cria `ActivityExitPipeline`.

### Escopo aplicado

```text
Adicionada telemetry interna transitÃ³ria ActivityContentReleaseContinuationTelemetry.
ActivityContentReleaseContinuationResolved Ã© emitido ao final da finalization.
ActivityContentReleaseContinuationStarted Ã© emitido imediatamente antes da chamada de continuaÃ§Ã£o macro.
ActivityContentReleaseContinuationCompleted Ã© emitido apÃ³s a chamada sÃ­ncrona/delegaÃ§Ã£o aplicÃ¡vel.
RestartCurrentActivity, NextActivity, RouteExit e CompleteActivity sÃ£o classificados explicitamente.
```

### Campos observÃ¡veis

```text
pipelineId
sessionStateId
activityId
entrySequence
stage
source
reason
continuationKind
continuationTargetActivityId
continuationTargetEntrySequence
hasPendingRestartTransition
hasPendingRouteExit
hasNextActivity
routeExitRequested
deactivationCompleted
activityCompletionRequested
releaseStatus
skippedNoContent
loadedSceneCount
releasedSceneCount
previousStage
nextStage
```

### Escopo explicitamente nÃ£o alterado

```text
StartPendingRestartEntry continua no SessionActivityPipeline.
CompleteRouteExitClosure continua no SessionActivityPipeline.
ContinueAfterDeactivationAsync continua no SessionActivityPipeline.
Route-exit handoff nÃ£o foi alterado.
Restart lifecycle nÃ£o foi alterado.
Next activity lifecycle nÃ£o foi alterado.
ActivityContentReleaseFinalizationStage nÃ£o foi alterado.
Unload adapter/runner nÃ£o foram alterados.
```

### CritÃ©rio de smoke

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'

ActivityContentReleaseContinuationResolved aparece
ActivityContentReleaseContinuationStarted aparece
ActivityContentReleaseContinuationCompleted aparece quando aplicÃ¡vel

RestartCurrentActivity:
  continuationKind='RestartCurrentActivity'
  owner='SessionActivityPipeline'
  continuationTargetActivityId='activity_01'

Activity01ToActivity02:
  continuationKind='NextActivity'
  owner='SessionActivityPipeline'
  continuationTargetActivityId='activity_02'

RouteExitBackToMenu:
  continuationKind='RouteExit'
  owner='SessionActivityPipeline'
  routeExitRequested='true'

ActivityObjectSnapshotCapture PASS
ActivityObjectRelease PASS
ActivityObjectContributorUnregister PASS
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```


## SA-7H â€” Release/Exit Bridge Debt audit

Status: `CLOSED / AUDIT ONLY`.

### DecisÃ£o

As bridges transitÃ³rias criadas nos cortes SA-7B atÃ© SA-7G2B-H1 sÃ£o aceitÃ¡veis temporariamente, mas nÃ£o sÃ£o contratos finais.

Bridges auditadas:

```text
IActivityExitActorTeardownRuntimeBridge
IActivityObjectSnapshotCaptureRuntimeBridge
IActivityObjectReleaseRuntimeBridge
IActivityObjectContributorUnregisterRuntimeBridge
IActivityContentSceneUnloadDispatchRuntimeBridge
IActivityContentReleaseFinalizationRuntimeBridge
```

### Owner preservado

```text
SessionActivityPipeline continua dono do macro lifecycle de saÃ­da/dematerialization/continuation.
Stages dedicados executam passos determinÃ­sticos.
Bridges apenas expÃµem state temporÃ¡rio ainda preso no pipeline.
```

### Regra

```text
NÃ£o expandir bridges.
NÃ£o transformar bridge em manager/coordinator.
NÃ£o expor continuation macro por bridge.
NÃ£o criar ActivityExitPipeline.
NÃ£o criar ActivityContentReleasePipeline.
```

### PrÃ³ximo corte recomendado

```text
SA-7H1 â€” SessionActivityExitRuntimeState audit/design
```

Escopo:

```text
Mapear fields internos do SessionActivityPipeline usados por release/exit.
Propor ActivityExitRuntimeState / ActivityObjectRuntimeState / ActivityContentReleaseRuntimeState.
Definir ownership de loaded set, pending release context, discovery result, inventory preview, snapshot payload.
Definir quais stages recebem state direto e quais continuam recebendo command.
Definir smoke e observabilidade.
```

Proibido:

```text
NÃ£o mover cÃ³digo runtime.
NÃ£o remover bridge ainda.
NÃ£o alterar lifecycle.
NÃ£o criar manager/coordinator.
NÃ£o mover continuation macro.
```


## SA-7H1 â€” SessionActivityExitRuntimeState audit/design

Status: `CLOSED / DESIGN ONLY`.

### DecisÃ£o

NÃ£o criar um Ãºnico `SessionActivityExitRuntimeState` gigante.

SeparaÃ§Ã£o aprovada para prÃ³ximos cortes:

```text
ActivityActorExitRuntimeState
ActivityObjectExitRuntimeState
ActivityContentReleaseRuntimeState
```

### Owner preservado

```text
SessionActivityPipeline continua dono do macro lifecycle:
- release ordering;
- deactivation window;
- restart continuation;
- next activity continuation;
- route-exit closure;
- foreign/stale guards;
- handoffs.
```

Runtime states armazenam state tÃ©cnico.  
Runtime states nÃ£o decidem lifecycle.

### Checkpoint SA-8C-DOC

Status: CLOSED / DOCUMENTED.

- `SA-ACTOR-1B1*` estÃ¡ fechado no trilho Actors.
- `ActorScope` ficou congelado como fonte canÃ´nica de `lifetime/retention/release`.
- `PlayerActor` e `NonPlayerActor` nÃ£o sÃ£o owners de lifetime.
- `PlayerParticipation` ficou restrito a `slot/selection/participant`.
- `ActivityActorExitRuntimeState` ficou classificado como `correlation store` tÃ©cnico.
- `ActivityPlayerActorRegistry` ficou classificado como Ã­ndice tÃ©cnico puro, sem `Destroy` local.
- O prÃ³ximo passo volta para decomposiÃ§Ã£o macro de `SessionActivity`; nÃ£o abrir `SessionScoped` ainda.

### Checkpoint ACTOR-COMP-0C -> 0F

Status: CLOSED / PASS funcional.

- `ActivityActorScopeCompatibilityPolicy` continua owner explícito de compatibilidade/eligibilidade de scope e reentry.
- `ActivityPlayerActorRegistry` ficou reduzido a índice técnico e lookup técnico.
- O registry não decide lifecycle, retain, release, reentry, materialization policy ou scope compatibility.
- Wrappers transitórios de `ActivityPlayerActorRegistry` foram removidos; os callers agora expressam intenção por stage/pipeline e usam a API técnica do registry.
- Smoke funcional preservado: `RestartCurrentActivity PASS`, `Activity01ToActivity02 PASS`, `RouteExitBackToMenu PASS`, `ActivityParticipantActorMaterializationRetained`, `ActivityParticipantRetainedBindingChosen`, `ActivityGateBindingCompleted receivers='2'`, `MovementControlEnabled`, `CameraBindingCompleted`, `ObjectEmissionSpawned`, `ObjectEmissionReturnedToPool`, sem `FATAL`, `Exception`, `route_transition_failed` ou foreign/stale indevido.

### Mapeamento

```text
ActivityActorExitRuntimeState:
  substitui gradualmente IActivityExitActorTeardownRuntimeBridge.

ActivityObjectExitRuntimeState:
  substitui gradualmente IActivityObjectSnapshotCaptureRuntimeBridge,
  IActivityObjectReleaseRuntimeBridge,
  IActivityObjectContributorUnregisterRuntimeBridge.

ActivityContentReleaseRuntimeState:
  substitui gradualmente IActivityContentSceneUnloadDispatchRuntimeBridge,
  IActivityContentReleaseFinalizationRuntimeBridge.
```

### PrÃ³ximo corte recomendado

```text
SA-7H2 â€” ActivityContentReleaseRuntimeState implementation
```

Motivo:

```text
Ã‰ o menor state coeso.
Cobre loaded set, pending release context e awaiting flag.
Reduz duas bridges relacionadas.
NÃ£o toca actor stores.
NÃ£o toca snapshot payload/save.
NÃ£o move continuation macro.
```

### Proibido no SA-7H2

```text
NÃ£o mover CompleteActivityContentSceneUnloadOperation.
NÃ£o mover StartPendingRestartEntry.
NÃ£o mover CompleteRouteExitClosure.
NÃ£o mover ContinueAfterDeactivationAsync.
NÃ£o criar manager/coordinator.
NÃ£o criar ActivityExitPipeline.
NÃ£o criar ActivityContentReleasePipeline.
NÃ£o alterar RouteActivitySave.
```

### CritÃ©rio de aceite futuro

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
ActivityContentSceneUnloadDispatchStage preservado
ActivityContentReleaseFinalizationStage preservado
ActivityContentReleaseContinuationResolved/Started/Completed preservado
ActivityObjectSnapshotCapture Passed
ActivityObjectRelease Passed
ActivityObjectContributorUnregister Passed
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
```

## SA-7H2 â€” ActivityContentReleaseRuntimeState implementation

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

### DecisÃ£o aplicada

Criado `ActivityContentReleaseRuntimeState` para concentrar o state tÃ©cnico de release async de ActivityContent:

```text
CurrentLoadedSet
PendingActivityContentReleaseContext
IsAwaitingContinuation
```

### Owner preservado

```text
SessionActivityPipeline continua dono da continuation macro.
ActivityContentReleaseRuntimeState guarda apenas state tÃ©cnico.
ActivityContentSceneUnloadDispatchStage continua stage de dispatch.
ActivityContentReleaseFinalizationStage continua stage de finalization.
```

### Escopo aplicado

```text
Criado NewScripts/SessionActivity/Pipeline/Runtime/ActivityContentReleaseRuntimeState.cs.
SessionActivityPipeline passa a delegar pending release context e awaiting flag ao runtime state.
Set/Clear de CurrentActivityContentLoadedSet passa a espelhar o state tÃ©cnico no runtime state.
Bridges existentes continuam como camada transitÃ³ria, mas agora leem/limpam o runtime state em vez de fields soltos do pipeline.
```

### Escopo explicitamente nÃ£o alterado

```text
CompleteActivityContentSceneUnloadOperation nÃ£o foi movido.
StartPendingRestartEntry nÃ£o foi movido.
CompleteRouteExitClosure nÃ£o foi movido.
ContinueAfterDeactivationAsync nÃ£o foi movido.
ActivityContentReleaseContinuation* permanece owner='SessionActivityPipeline'.
RouteActivitySave nÃ£o foi alterado.
ActivityExitPipeline nÃ£o foi criado.
ActivityContentReleasePipeline nÃ£o foi criado.
```

### Observabilidade nova esperada

```text
ActivityContentReleaseRuntimeStateLoadedSetStored
ActivityContentReleaseRuntimeStateLoadedSetCleared
ActivityContentReleaseRuntimeStatePendingContextStored
ActivityContentReleaseRuntimeStatePendingContextCleared
ActivityContentReleaseRuntimeStateAwaitingContinuationChanged
```

### CritÃ©rio de smoke

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
ActivityContentReleaseRuntimeState* observado nos releases com content
ActivityContentSceneUnloadDispatchStage preservado
ActivityContentReleaseFinalizationStage preservado
ActivityContentReleaseContinuationResolved/Started/Completed preservado
ActivityObjectSnapshotCapture Passed
ActivityObjectRelease Passed
ActivityObjectContributorUnregister Passed
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
```


## SA-7H2-H1 â€” ContentRelease bridge retirement audit

Status: `CLOSED / AUDIT ONLY`.

### DecisÃ£o

NÃ£o remover as duas bridges de `ActivityContentRelease` de uma vez.

Bridges auditadas:

```text
IActivityContentSceneUnloadDispatchRuntimeBridge
IActivityContentReleaseFinalizationRuntimeBridge
```

### Resultado

```text
IActivityContentSceneUnloadDispatchRuntimeBridge:
  pode ser reduzida/removida primeiro, desde que ActivityContentSceneUnloadDispatchStage dependa de ActivityContentReleaseRuntimeState e ports explÃ­citos de pending operation/runner.

IActivityContentReleaseFinalizationRuntimeBridge:
  pode ser reduzida/removida depois, desde que ActivityContentReleaseFinalizationStage dependa de ActivityContentReleaseRuntimeState e de ActivityObjectContributorUnregisterStage ou executor explÃ­cito.
```

### PrÃ³ximos cortes recomendados

```text
SA-7H2-H2 â€” ActivityContentSceneUnloadDispatchBridgeReduction
SA-7H2-H3 â€” ActivityContentReleaseFinalizationBridgeReduction
```

### Owner preservado

```text
SessionActivityPipeline continua dono do macro lifecycle e da continuation.
ActivityContentReleaseRuntimeState guarda state tÃ©cnico.
Stages executam passos determinÃ­sticos.
Bridges sÃ£o transitÃ³rias.
```

### Proibido

```text
NÃ£o mover CompleteActivityContentSceneUnloadOperation.
NÃ£o mover FailPendingOperation.
NÃ£o mover StartPendingRestartEntry.
NÃ£o mover CompleteRouteExitClosure.
NÃ£o mover ContinueAfterDeactivationAsync.
NÃ£o alterar RouteActivitySave.
NÃ£o criar ActivityContentReleasePipeline.
NÃ£o criar ActivityExitPipeline.
```

## SA-16B â€” ActivityContent async release completion boundary

Status: `CLOSED`.

### Fechamento SA-16B1

`SA-16B1 â€” ActivityContent unload callback boundary cleanup` concluÃ­do com `PASS funcional + PASS arquitetural do corte`.

### Resultado consolidado

```text
Auditoria confirmou que ActivityContent release Ã© async tÃ©cnico por causa do unload Unity.
O release continua sequencial/cascata no lifecycle macro.
O problema identificado era boundary interno: CompleteActivityContentSceneUnloadOperation misturava completion tÃ©cnica com macro continuation.
NÃ£o havia justificativa para criar ActivityContentReleasePipeline ou ActivityExitPipeline.
CompleteActivityContentSceneUnloadOperation foi reduzido ao papel de callback tÃ©cnico.
O callback tÃ©cnico valida completion, rejeita stale/foreign, consome pending operation, registra completion tÃ©cnica e delega a continuaÃ§Ã£o.
A macro continuation foi movida para mÃ©todo explÃ­cito do prÃ³prio SessionActivityPipeline: ContinueAfterActivityContentUnloadCompletionAsync(...).
Esse mÃ©todo concentra prÃ³ximo scene unload, finalizaÃ§Ã£o e branch final para CompleteRouteExitClosure, StartPendingRestartEntry e ContinueAfterDeactivationAsync.
SessionActivityPipeline continua sendo o Ãºnico owner de macro continuation.
ActivityContentReleaseRuntimeState continua sendo runtime state tÃ©cnico.
ActivityContentReleaseFinalizationStage continua stage puro de cleanup/finalization.
ActivityContentReleaseContinuationStage, quando citado, continua facade/log e nÃ£o pipeline novo.
NÃ£o foi criado manager/coordinator/processor.
NÃ£o houve alteraÃ§Ã£o em Save, Reset, Movement, Camera, Presentation ou Attributes.
```

### Invariantes registradas

```text
ActivityContent release pode ser async, mas apenas por side-effect Unity.
Async completion nÃ£o decide lifecycle.
Callback tÃ©cnico nÃ£o Ã© owner de continuation.
PendingOperation existe para completion tracking e stale/foreign validation.
PendingActivityContentReleaseContext Ã© state tÃ©cnico, nÃ£o owner de lifecycle.
No-content release Ã© skip explÃ­cito, nÃ£o erro.
Release nÃ£o decide save.
Release nÃ£o decide reset.
Release nÃ£o decide route transition.
SessionActivityPipeline Ã© o Ãºnico owner de macro continuation.
Stages executam passos determinÃ­sticos; adapters executam side-effects; runtime state nÃ£o decide policy/lifecycle.
```

### Smoke registrado

```text
Sem FATAL.
Sem Exception.
Sem route_transition_failed.
Sem foreign/stale indevido.
Sem checkpointStatus='Failed'.
ActivityContentUnloadCompletionTechnicalCompleted observado.
ActivityContentUnloadCompletionContinuationStarted observado.
ActivityContentUnloadCompletionContinuationCompleted observado.
ActivityContentSceneUnloadDispatched preservado.
ActivityContentReleaseFinalizationStarted preservado.
ActivityContentReleaseCompleted preservado.
SkippedNoContent preservado para activity_02.
RestartCurrentActivity PASS.
Activity01ToActivity02 PASS.
RouteExitBackToMenu PASS.
RouteActivitySave preservou classificacao NoActivityContentContributors, sem regressao para SnapshotPayloadExpectedButMissing.
```

## SA-16E1 - ActivityCameraAnchorHost explicit scene-scope composition

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

### Fechamento consolidado

```text
SessionOperationalActivityCameraAdapter nao varre mais a cena.
SessionOperationalActivityCameraAdapter nao resolve mais ActivityCameraAnchorHost por descoberta local.
SceneScopedActivityCameraAnchorHostResolver resolve o host por contrato explicito de scene-scope.
ActivityCameraAnchorHost registra o host no escopo da cena.
Ausencia de host obrigatorio falha explicitamente.
Hosts duplicados na mesma cena falham explicitamente.
ActivityCameraPresentationRequirementResolver continua recebendo host pronto e resolvendo requirement.
CinemachineActivityCameraDirector continua apenas aplicando side-effects no rig preparado.
ActivityEntryCameraBindingStage, PlayerCameraEndpoint e ActorCapabilitySurface nao foram alterados neste corte.
Nao houve fallback silencioso.
```

### Ownership final

```text
Scene composition/scene-scope resolve o ActivityCameraAnchorHost.
SessionOperationalRuntimeComposer injeta o contrato de resolucao no adapter.
SessionOperationalActivityCameraAdapter continua apenas orquestrando prepare/rebind.
ActivityCameraPresentationRequirementResolver continua sendo o resolver de requirement com host pronto.
CinemachineActivityCameraDirector continua owner dos side-effects Cinemachine.
```

### Invariantes finais

```text
Adapter nao faz lookup scene-local nem discovery por conta propria.
Host e resolvido por contrato explicito de composition/scene-scope.
Ausencia obrigatoria e erro explicito, nao fallback silencioso.
Nao existe owner duplicado de lifecycle de camera.
Nao alterar Camera behavior.
Nao alterar ActivityEntryCameraBindingStage.
Nao alterar PlayerCameraEndpoint.
Nao alterar Cinemachine behavior.
```

### Evidencia aceita

```text
ActivityCameraPresentationPrepareStarted preservado.
ActivityCameraPresentationPrepared preservado.
ActivityCameraPrepared preservado.
ActivityCameraTargetsRebound preservado.
ActivityCameraTargetBound preservado.
CameraBindingCompleted preservado.
Sem FATAL.
Sem Exception.
Sem route_transition_failed.
Sem foreign/stale indevido.
RestartCurrentActivity PASS.
Activity01ToActivity02 PASS.
RouteExitBackToMenu PASS.
```

### Fechamento final

```text
SA-16B: CLOSED.
SA-16B1: PASS funcional + PASS arquitetural do corte.
Debito residual controlado: ContinueAfterActivityContentUnloadCompletionAsync(...) ainda retorna Task sem await direto para preservar timing/semantica anterior; nao expandir esse padrao.
```

## SA-16C â€” PendingOperation callback contract

Status: `CLOSED`.

### Fechamento SA-16C1

`SA-16C1 â€” PendingOperation window unload callback boundary cleanup` concluido com `PASS funcional + PASS arquitetural do corte`.

### Fechamento SA-16C2

`SA-16C2 â€” PendingOperation kind contract cleanup` concluido com `PASS funcional + PASS arquitetural do corte`.

### Resultado consolidado

```text
Auditoria confirmou que PendingOperation e runtime state tecnico continuam corretos.
PendingOperation existe para tracking de operationId, activityId, entrySequence, stale/foreign validation, completion/failure e limpeza de pending state.
CompletePendingOperation foi reduzido ao callback tecnico para activation/deactivation window unload.
ContinueAfterActivationWindowSceneUnloadCompletion(...) e ContinueAfterDeactivationWindowSceneUnloadCompletion(...) concentram a continuation explicita no proprio SessionActivityPipeline.
SessionActivityPendingOperationKind passou a representar apenas operacoes async reais pendentes.
Valores sintéticos de command/completion foram removidos do contrato de pending operation e permanecem no contrato correto de command, quando aplicavel.
SessionActivityPipeline continua sendo o unico owner de macro lifecycle/continuation.
PendingActivityContentReleaseContext continua sendo state tecnico, nao owner de lifecycle.
No-content release continua skip explicito, nao erro.
No-window continua skip explicito, nao fallback silencioso.
Nao houve alteracao em Save, Reset, Movement, Camera, Presentation ou Attributes.
```

### Invariantes registradas

```text
Async completion nao decide lifecycle.
Callback tecnico nao e owner de continuation.
PendingOperation e runtime state tecnico.
PendingOperationKind representa apenas operacoes async pendentes reais.
Pending operation nao representa comando de usuario, completion manual ou continuation macro.
Pending operation existe para tracking, completion e stale/foreign validation.
SessionActivityPipeline e o owner unico de macro lifecycle/continuation.
Stages executam passos deterministicos.
Adapters executam side-effects.
Runtime state nao decide policy/lifecycle.
```

### Smoke registrado

```text
Sem FATAL.
Sem Exception.
Sem route_transition_failed.
Sem foreign/stale indevido.
Sem checkpointStatus='Failed'.
Sem error CS.
ActivityContentUnloadCompletionTechnicalCompleted observado.
ActivityContentUnloadCompletionContinuationStarted observado.
ActivityContentUnloadCompletionContinuationCompleted observado.
ActivationWindowSceneUnloadContinuationStarted observado.
ActivationWindowSceneUnloadContinuationCompleted observado.
DeactivationWindowSceneUnloadContinuationStarted observado.
DeactivationWindowSceneUnloadContinuationCompleted observado.
ActivityContentSceneUnloadDispatched preservado.
ActivityContentReleaseFinalizationStarted preservado.
ActivityContentReleaseCompleted preservado.
SkippedNoContent preservado para activity_02.
RestartCurrentActivity PASS.
Activity01ToActivity02 PASS.
RouteExitBackToMenu PASS.
RouteActivitySave preservou classificacao NoActivityContentContributors, sem regressao para SnapshotPayloadExpectedButMissing.
```

### Fechamento final

```text
SA-16B: CLOSED.
SA-16B1: PASS funcional + PASS arquitetural do corte.
SA-16C: CLOSED.
SA-16C1: PASS funcional + PASS arquitetural do corte.
SA-16C2: PASS funcional + PASS arquitetural do corte.
Debito residual controlado: helpers explicitos de continuation permanecem dentro do SessionActivityPipeline; nao expandir callbacks tecnicos com branches de lifecycle.
```

## SA-7H2-H2 â€” ActivityContentSceneUnloadDispatchBridgeReduction

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

### Resultado do smoke â€” 2026-06-02

Smoke manual validado a partir de `FullLog.txt` enviado em 2026-06-02.

EvidÃªncia aceita:

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
ActivityContentReleaseRuntimeStateLoadedSetStored observado
ActivityContentReleaseRuntimeStatePendingContextStored observado
ActivityContentReleaseRuntimeStateAwaitingContinuationChanged observado
ActivityContentSceneUnloadDispatchStage preservado
pendingOperation ActivityContentSceneUnload preservado
ActivityContentReleaseFinalizationStage preservado
ActivityContentReleaseContinuationResolved/Started/Completed preservado
ActivityObjectSnapshotCapture Passed
ActivityObjectRelease Passed
ActivityObjectContributorUnregister Passed
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
```

DecisÃ£o:

```text
SA-7H2-H2 fechado como PASS funcional + PASS arquitetural do corte.
A reduÃ§Ã£o da bridge de dispatch de unload nÃ£o regrediu restart, transition, route-exit, snapshot/release/unregister nem continuation macro.
```

### DecisÃ£o aplicada

A bridge transitÃ³ria de dispatch de unload de `ActivityContent` foi removida do caminho ativo:

```text
IActivityContentSceneUnloadDispatchRuntimeBridge
```

O stage passou a depender de contratos explÃ­citos:

```text
ActivityContentReleaseRuntimeState
IActivityEntryRuntimeEndpoint
ISessionActivityPendingOperationRunner
ISessionActivityPendingOperationCallback
```

### Owner preservado

```text
SessionActivityPipeline continua dono do callback e da continuation macro.
ActivityContentReleaseRuntimeState guarda state tÃ©cnico.
ActivityContentSceneUnloadDispatchStage executa apenas o dispatch determinÃ­stico de unload.
PendingOperationRunner continua bridge tÃ©cnica async.
UnityActivityContentSceneReleaseAdapter continua adapter de side-effect Unity.
```

### Escopo aplicado

```text
ActivityContentSceneUnloadDispatchStage lÃª PendingActivityContentReleaseContext via ActivityContentReleaseRuntimeState.
ActivityContentSceneUnloadDispatchStage monta SessionActivityPendingOperation localmente a partir de identity canÃ´nica.
ActivityContentSceneUnloadDispatchStage chama ISessionActivityPendingOperationRunner.RunActivityContentReleaseOperation diretamente.
SessionActivityPipeline deixou de implementar IActivityContentSceneUnloadDispatchRuntimeBridge.
MÃ©todos explÃ­citos da bridge de dispatch foram removidos.
```

### Escopo explicitamente nÃ£o alterado

```text
CompleteActivityContentSceneUnloadOperation nÃ£o foi movido.
FailPendingOperation nÃ£o foi movido.
StartPendingRestartEntry nÃ£o foi movido.
CompleteRouteExitClosure nÃ£o foi movido.
ContinueAfterDeactivationAsync nÃ£o foi movido.
ActivityContentReleaseFinalizationStage nÃ£o foi alterado.
ActivityObjectContributorUnregisterStage nÃ£o foi alterado.
RouteActivitySave nÃ£o foi alterado.
NÃ£o foi criado ActivityContentReleasePipeline.
NÃ£o foi criado ActivityExitPipeline.
```

### CritÃ©rio de smoke

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'

ActivityContentReleaseRuntimeStateLoadedSetStored observado
ActivityContentReleaseRuntimeStatePendingContextStored observado
ActivityContentReleaseRuntimeStateAwaitingContinuationChanged observado
ActivityContentSceneUnloadDispatchStage preservado
pendingOperation ActivityContentSceneUnload preservado
ActivityContentReleaseFinalizationStage preservado
ActivityContentReleaseContinuationResolved/Started/Completed preservado
ActivityObjectSnapshotCapture Passed
ActivityObjectRelease Passed
ActivityObjectContributorUnregister Passed
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
```

## SA-7H2-H3 â€” ActivityContentReleaseFinalizationBridgeReduction

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

### Resultado do smoke â€” 2026-06-02

Smoke manual validado a partir de `FullLog.txt` enviado em 2026-06-02.

EvidÃªncia aceita:

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
ActivityContentReleaseFinalizationStage preservado
ActivityContentReleaseCompleted preservado
SessionActivityDematerializationCompleted preservado
ActivityContentReleaseFinalizationCleanupStarted preservado
ActivityContentReleaseFinalizationCleanupCompleted preservado
ActivityContentReleaseFinalizationCompleted preservado
ActivityObjectContributorUnregisterStarted/Completed preservado
ActivityContentReleaseContinuationResolved/Started/Completed preservado
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
```

DecisÃ£o:

```text
SA-7H2-H3 fechado como PASS funcional + PASS arquitetural do corte.
A remoÃ§Ã£o da finalization bridge nÃ£o transformou ActivityContentReleaseFinalizationStage em owner de lifecycle macro; a continuation permanece no SessionActivityPipeline.
```

### DecisÃ£o aplicada

`IActivityContentReleaseFinalizationRuntimeBridge` foi removida do caminho ativo de finalization de `ActivityContentRelease`.

`ActivityContentReleaseFinalizationStage` passa a depender diretamente de:

```text
ActivityContentReleaseRuntimeState
IActivityObjectContributorUnregisterRuntimeBridge
IActivityEntryRuntimeEndpoint
```

### Owner preservado

```text
ActivityContentReleaseRuntimeState guarda state tÃ©cnico de release async.
ActivityContentReleaseFinalizationStage executa somente finalization determinÃ­stica.
ActivityObjectContributorUnregisterStage continua stage explÃ­cito.
SessionActivityPipeline continua dono de continuation macro.
```

### O que saiu

```text
IActivityContentReleaseFinalizationRuntimeBridge
implementaÃ§Ãµes explÃ­citas dessa bridge no SessionActivityPipeline
acesso indireto ao ActivityContentReleaseRuntimeState via bridge
chamada indireta de unregister via finalization bridge
```

### O que permanece

```text
ActivityContentReleaseCompleted preservado
SessionActivityDematerializationCompleted preservado
ActivityContentReleaseFinalizationCleanupStarted preservado
ActivityContentReleaseFinalizationCleanupCompleted preservado
ActivityContentReleaseFinalizationCompleted preservado
ActivityObjectContributorUnregisterStarted/Completed preservado
ActivityContentReleaseContinuationResolved/Started/Completed preservado
```

### Escopo proibido preservado

```text
CompleteActivityContentSceneUnloadOperation nÃ£o foi movido.
FailPendingOperation nÃ£o foi movido.
StartPendingRestartEntry nÃ£o foi movido.
CompleteRouteExitClosure nÃ£o foi movido.
ContinueAfterDeactivationAsync nÃ£o foi movido.
RouteActivitySave nÃ£o foi alterado.
ActivityContentReleasePipeline nÃ£o foi criado.
ActivityExitPipeline nÃ£o foi criado.
Manager/coordinator novo nÃ£o foi criado.
```

### ObservaÃ§Ã£o

`ActivityContentReleaseFinalizationStage` ainda usa `IActivityEntryRuntimeEndpoint.ClearCurrentActivityContentLoadedSet()` para limpar o loaded set espelhado em `SessionActivityRuntimeState`, enquanto `ActivityContentReleaseRuntimeState` permanece dono do state tÃ©cnico de release async.

Isso nÃ£o move lifecycle e nÃ£o reintroduz a bridge de finalization.
## SA-7H3A â€” ActivityObjectExitRuntimeState

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

### Resultado do smoke â€” 2026-06-02

Smoke manual validado a partir de `FullLog.txt` enviado em 2026-06-02.

EvidÃªncia aceita:

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
ActivityObjectExitRuntimeState* observado
ActivityObjectSnapshotCaptureStage preservado
ActivityObjectReleaseStage preservado
ActivityObjectContributorUnregisterStage preservado
ActivityObjectSnapshotCapture Passed
ActivityObjectRelease Passed
ActivityObjectContributorUnregister Passed
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
```

DecisÃ£o:

```text
SA-7H3A fechado como PASS funcional + PASS arquitetural do corte.
ActivityObjectExitRuntimeState ficou validado como owner tÃ©cnico de state de object exit, sem assumir lifecycle, save ou continuation macro.
```

### DecisÃ£o aplicada

`ActivityObjectExitRuntimeState` foi criado como owner tÃ©cnico do state de object exit:

```text
CurrentActivityObjectContributorDiscoveryResult
CurrentActivityCapabilityInventoryPreview
CurrentActivityCapabilityInventoryPreviewValidation
SessionActivitySnapshotPayloadForSaveOnExit
```

### Escopo aplicado

```text
Criado ActivityObjectExitRuntimeState.
SessionActivityPipeline passa a manter ActivityObjectExitRuntimeState.
IActivityObjectSnapshotCaptureRuntimeBridge passa a ler/gravar via ActivityObjectExitRuntimeState.
IActivityObjectReleaseRuntimeBridge passa a ler via ActivityObjectExitRuntimeState.
IActivityObjectContributorUnregisterRuntimeBridge passa a ler/limpar via ActivityObjectExitRuntimeState.
ISessionActivitySnapshotPayloadProvider.TryGetSnapshotPayloadForSaveOnExit passa a ler o payload via ActivityObjectExitRuntimeState.
```

### Compatibilidade tÃ©cnica transitÃ³ria

```text
As bridges de object exit continuam existindo como facade fina.
SessionActivityRuntimeState ainda mantÃ©m espelho para consumidores de entry que ainda nÃ£o foram migrados.
Esse espelho nÃ£o Ã© owner final e deve ser removido em cortes SA-7H3B/C/D.
```

### Owner preservado

```text
SessionActivityPipeline continua dono do macro lifecycle.
ActivityObjectExitRuntimeState guarda state tÃ©cnico.
Stages continuam executando passos determinÃ­sticos.
RouteActivitySave continua consumidor do payload.
```

### Proibido preservado

```text
RouteActivitySave nÃ£o foi movido.
Save nÃ£o Ã© executado pelo runtime state.
ActivityContentRelease nÃ£o foi movido.
Callback async nÃ£o foi movido.
Continuation macro nÃ£o foi movida.
ActivityExitPipeline nÃ£o foi criado.
Manager/coordinator novo nÃ£o foi criado.
```

### Smoke necessÃ¡rio

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
ActivityObjectExitRuntimeState* observado
ActivityObjectSnapshotCaptureStage preservado
ActivityObjectReleaseStage preservado
ActivityObjectContributorUnregisterStage preservado
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
```

## SA-7H3B â€” ActivityObjectSnapshotCaptureBridgeReduction

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

### Resultado do smoke â€” 2026-06-02

Smoke manual validado a partir de `FullLog.txt` enviado em 2026-06-02.

EvidÃªncia aceita:

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
ActivityObjectSnapshotCaptureStage preservado
ActivityObjectSnapshotCaptureStarted observado
ActivityObjectSnapshotCaptureCompleted observado
ActivityObjectExitRuntimeStateSnapshotPayloadStored observado
ActivityObjectSnapshotCapture Passed
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
```

DecisÃ£o:

```text
SA-7H3B fechado como PASS funcional + PASS arquitetural do corte.
ActivityObjectSnapshotCaptureStage passou a usar ActivityObjectExitRuntimeState como state tÃ©cnico direto sem reintroduzir bridge ativa ou mover RouteActivitySave.
```

### DecisÃ£o aplicada

`IActivityObjectSnapshotCaptureRuntimeBridge` saiu do caminho ativo.

`ActivityObjectSnapshotCaptureStage` passa a depender diretamente de:

```text
ActivityObjectExitRuntimeState
IActivityEntryRuntimeEndpoint
```

### Owner preservado

```text
ActivityObjectExitRuntimeState guarda discovery/inventory/snapshot payload.
ActivityObjectSnapshotCaptureStage executa apenas snapshot capture determinÃ­stico.
SessionActivityPipeline continua dono de ordering/lifecycle/continuation macro.
RouteActivitySave continua consumidor externo do payload.
```

### O que saiu

```text
IActivityObjectSnapshotCaptureRuntimeBridge
implementaÃ§Ãµes explÃ­citas dessa bridge no SessionActivityPipeline
acesso indireto ao ActivityObjectExitRuntimeState via bridge de snapshot capture
```

### O que permanece

```text
IActivityObjectReleaseRuntimeBridge permanece para SA-7H3C.
IActivityObjectContributorUnregisterRuntimeBridge permanece para SA-7H3D.
ActivityObjectSnapshotCaptureStarted preservado.
ActivityObjectSnapshotCaptureCompleted preservado.
ActivityObjectExitRuntimeStateSnapshotPayloadStored preservado.
TryGetSnapshotPayloadForSaveOnExit continua lendo do ActivityObjectExitRuntimeState.
RouteActivitySave nÃ£o foi movido.
```

### Escopo proibido preservado

```text
RouteActivitySave nÃ£o foi movido.
Save nÃ£o Ã© executado pelo runtime state.
ObjectRelease nÃ£o foi alterado.
ContributorUnregister nÃ£o foi alterado.
ActivityContentRelease nÃ£o foi alterado.
Callback async nÃ£o foi movido.
Restart / next activity / route-exit / deactivation continuation nÃ£o foram movidos.
ActivityExitPipeline nÃ£o foi criado.
Manager/coordinator novo nÃ£o foi criado.
```

## SA-7H3C-D â€” ActivityObjectReleaseAndContributorUnregisterBridgeReduction

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

### Resultado do smoke â€” 2026-06-02

Smoke manual validado a partir de `FullLog.txt` enviado em 2026-06-02.

EvidÃªncia aceita:

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
ActivityObjectReleaseStage preservado
ActivityObjectReleaseStarted observado
ActivityObjectReleaseCompleted observado
ActivityObjectContributorUnregisterStage preservado
ActivityObjectContributorUnregisterStarted observado
ActivityObjectContributorUnregisterCompleted observado
ActivityObjectRelease Passed
ActivityObjectContributorUnregister Passed
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
```

DecisÃ£o:

```text
SA-7H3C-D fechado como PASS funcional + PASS arquitetural do corte.
ActivityObjectReleaseStage e ActivityObjectContributorUnregisterStage usam ActivityObjectExitRuntimeState diretamente e permanecem stages determinÃ­sticos; RouteActivitySave, callback async e continuation macro nÃ£o foram movidos.
```

### DecisÃ£o aplicada

As bridges transitÃ³rias restantes de object exit saÃ­ram do caminho ativo:

```text
IActivityObjectReleaseRuntimeBridge
IActivityObjectContributorUnregisterRuntimeBridge
```

`ActivityObjectReleaseStage` passa a depender diretamente de:

```text
ActivityObjectExitRuntimeState
IActivityEntryRuntimeEndpoint
```

`ActivityObjectContributorUnregisterStage` passa a depender diretamente de:

```text
ActivityObjectExitRuntimeState
IActivityEntryRuntimeEndpoint
```

### Owner preservado

```text
ActivityObjectExitRuntimeState guarda discovery/inventory/snapshot payload.
ActivityObjectReleaseStage executa somente release determinÃ­stico.
ActivityObjectContributorUnregisterStage executa somente unregister determinÃ­stico.
SessionActivityPipeline continua dono de ordering/lifecycle/continuation macro.
RouteActivitySave continua consumidor externo do payload.
```

### O que saiu

```text
IActivityObjectReleaseRuntimeBridge
IActivityObjectContributorUnregisterRuntimeBridge
implementaÃ§Ãµes explÃ­citas dessas bridges no SessionActivityPipeline
acesso indireto ao ActivityObjectExitRuntimeState via bridges de release/unregister
```

### O que permanece

```text
ActivityObjectSnapshotCaptureStage permanece como validado no SA-7H3B.
ActivityObjectReleaseStarted/Completed preservados.
ActivityObjectContributorUnregisterStarted/Unregistered/Completed preservados.
ActivityObjectExitRuntimeStateContributorDiscoveryCleared preservado via IActivityEntryRuntimeEndpoint.
TryGetSnapshotPayloadForSaveOnExit continua lendo do ActivityObjectExitRuntimeState.
RouteActivitySave nÃ£o foi movido.
```

### Escopo proibido preservado

```text
RouteActivitySave nÃ£o foi movido.
Save nÃ£o Ã© executado pelo runtime state.
SnapshotCapture nÃ£o foi alterado.
ActivityContentRelease nÃ£o foi alterado.
Callback async nÃ£o foi movido.
Restart / next activity / route-exit / deactivation continuation nÃ£o foram movidos.
ActivityExitPipeline nÃ£o foi criado.
Manager/coordinator novo nÃ£o foi criado.
```
## SA-7H4A-Big â€” ActivityActorExitRuntimeState + bridge slimming

Status: `Applied / Pending smoke`.

### DecisÃ£o aplicada

Criado `ActivityActorExitRuntimeState` e movido o state tÃ©cnico de actor exit para ele:

```text
active actor presentation states
active actor attribute states
active actor participation records
```

`ActivityExitActorTeardownStage` passa a usar `ActivityActorExitRuntimeState` diretamente para leitura/remoÃ§Ã£o desses records.

### Bridge preservada como port fino

`IActivityExitActorTeardownRuntimeBridge` permanece apenas para side-effects/adapters e resoluÃ§Ãµes que ainda nÃ£o sÃ£o state puro:

```text
ReleaseActorPresentation
ClearNonPlayerPresentationHandle
BuildActorInventoryFeedForExit
TryResolveActivePlayerParticipantBindingForExit
ExecutePlayerActorParticipationExit
```

### Escopo preservado

```text
NÃ£o move lifecycle.
NÃ£o move RouteActivitySave.
NÃ£o move ActivityContentRelease.
NÃ£o move ActivityObjectExit.
NÃ£o cria ActivityExitPipeline.
NÃ£o cria manager/coordinator.
NÃ£o reintroduz Player/NonPlayer como owner.
```

---

## Checkpoint SA-ACTOR-1C1 â€” ActorScope.SessionScoped structural lifetime

Status: `CLOSED / PASS funcional`.

### Contexto

O corte `SA-ACTOR-1C1` corrigiu uma fronteira de ownership em Actors dentro da Base 2.0: `ActorScope.SessionScoped` nÃ£o pode ser apenas um enum nem uma configuraÃ§Ã£o local do prefab. O scope precisa produzir identidade runtime, store/root session-owned, regras explÃ­citas de teardown e integraÃ§Ã£o com `ExitToMenu` sem criar trilho `Player/NonPlayer` paralelo.

### DecisÃ£o congelada

`ActorScope` decide apenas o lifetime estrutural do Actor.

```text
ActorScope.SessionScoped => o Actor estrutural sobrevive dentro da sessÃ£o.
ActorScope.RouteScoped => o Actor estrutural sobrevive dentro da rota.
ActorScope.ActivityScoped => o Actor estrutural vive sÃ³ na Activity/entry.
```

`ActorScope.SessionScoped` nÃ£o arrasta automaticamente:

```text
ActorPresentation
ActorAttributes
PowerUps
Permission receivers
Movement binding
Camera binding
PlayerInput binding
qualquer capability/component local
```

Esses componentes/capabilities tÃªm policy prÃ³pria, como `ActivityScoped`, `RouteScoped`, `SessionScoped`, `ReleaseOnActivityExit`, `ReleaseOnRouteExit` ou equivalente local. A separaÃ§Ã£o normativa Ã©:

```text
ActorScope decide a sobrevivÃªncia estrutural do Actor.
ComponentScope/CapabilityPolicy decide a sobrevivÃªncia de cada componente/capability.
```

### Owner correto

| DecisÃ£o | Owner correto |
|---|---|
| `PlayerSlot`, seleÃ§Ã£o e `SessionParticipationContext` | `SessionOperational` / `PlayerParticipation` |
| `actorScope` do player materializado | `PlayerParticipation` / `OperationalPlayerParticipationStage`, invariant `SessionScoped` |
| MaterializaÃ§Ã£o/reuso do Actor na Activity | `ActivityEntryPipeline` |
| Root/store session-owned do Actor estrutural | `SessionActorRuntimeStore` como Ã­ndice tÃ©cnico + adapter/root runtime |
| Lifetime estrutural em `ActivityExit`, `RouteExit`, `SessionReset` | `SessionActivityPipeline` / `ActivityExitActorTeardownStage` / reset stage |
| Lifetime de Presentation/Attribute/Permission/Movement/Camera | stages/policies locais das capabilities |
| Encerramento de sessÃ£o ao sair para Menu | `SessionOperationalPipeline` detecta policy de destino; `SessionActivityPipeline` executa reset estrutural |

### Regras de lifetime congeladas

| Scope | ActivityExit | RouteExit | ExitToMenu / SessionReset |
|---|---|---|---|
| `ActivityScoped` | `Release` | `Release` se ainda ativo | `Release` |
| `RouteScoped` | `Retain` | `Release` | `Release` |
| `SessionScoped` | `Retain` | `Retain` | `Release` |

`RouteExit` genÃ©rico nÃ£o libera `SessionScoped`, pois uma troca futura `GameplayRouteA -> GameplayRouteB` deve preservar actors de sessÃ£o. `ExitToMenu` encerra a sessÃ£o de gameplay e, por isso, executa `SessionReset` depois do teardown/save da rota anterior.

### Pontos implementados nos cortes H1-H7B2

```text
H1/H2 â€” Placement resolvido pela ActivityEntry usando fontes autorizadas, nÃ£o pela scene fÃ­sica do actor persistente.
H3 â€” PlayerActor runtime metadata vem do binding/materialization context, nÃ£o de campos soltos do prefab.
H4 â€” actorScope do Player sai do prefab; shape transitÃ³rio via PlayerSetDefinition foi superado por H8A.
H5 â€” restaura owner correto de placement para SessionScoped.
H6 â€” RouteExit tambÃ©m emite decisÃ£o explÃ­cita para SessionScoped retido no SessionActorRuntimeStore.
H7A â€” Observabilidade separa ActorLifetime de ComponentLifetime e reduz logs redundantes locais.
H7B â€” Primitiva SessionReset libera SessionScoped estrutural.
H7B1 â€” ExitToMenu chama SessionReset automaticamente apÃ³s RouteExit/save-on-exit.
H7B2 â€” SessionReset pÃ³s-RouteExit Ã© permitido mesmo com pipeline terminal em ClosedForRouteExit.
```

### Observabilidade congelada

Logs/facts mÃ­nimos esperados:

```text
ActorLifetimeDecisionResolved actorScope='SessionScoped' trigger='ActivityExit' decision='Retain'
ActorLifetimeDecisionResolved actorScope='SessionScoped' trigger='RouteExit' decision='Retain'
ActorLifetimeDecisionResolved actorScope='SessionScoped' trigger='SessionReset' decision='Release'
ActorLifetimeReleased actorScope='SessionScoped' trigger='SessionReset'
SessionResetCompleted sessionActorCount='0'
```

Logs de component/capability lifetime devem expor explicitamente que nÃ£o seguem automaticamente `ActorScope`:

```text
[OBS][ComponentLifetime] event='ActorPresentationRetained|Released'
actorScope='SessionScoped'
componentKind='ActorPresentation'
componentLifetimePolicy='ReleaseOnActivityExit|ReleaseOnRouteExit|...'
releaseTrigger='ActivityExit|RouteExit|SessionReset'
releaseDecision='Retain|Release'

[OBS][ComponentLifetime] event='ActorAttributeReleased'
actorScope='SessionScoped'
componentKind='ActorAttribute'
componentScope='ActivityScoped'
releaseTrigger='ActivityExit'
releaseDecision='Release'
```

### EvidÃªncia de smoke aceita

Smoke canÃ´nico usado para fechamento:

```text
Boot -> Menu -> Sandbox
Activity 01 entry
CompleteActivationWindow
RestartCurrentActivity
CompleteActivationWindow
CompleteCurrentActivity
Activity 01 -> Activity 02
BackToMenu / ExitToMenu
```

CritÃ©rios observados no fechamento:

```text
sem erro CS
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
sem invalid_required_placement
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
SessionParticipationContextPrepared com scope='SessionScoped'
ActorPresentationPlanResolved com actorInstanceRuntimeId contendo '|session|Actor|actor.player.primary|SessionScoped'
ActivityParticipantPlacementApplied
ActivityParticipantResetApplied
ActivityEntryParticipantBindingCompleted
RouteActivitySaveSaveOnExitStageCompleted antes de SessionReset
OperationalSessionResetAfterRouteExitStarted
SessionResetStarted
ActorLifetimeDecisionResolved trigger='SessionReset' decision='Release'
ActorLifetimeReleased trigger='SessionReset'
SessionResetCompleted sessionActorCount='0'
OperationalSessionResetAfterRouteExitCompleted
UnloadSceneCompleted scene='SessionActivitySandboxScene'
```

### Invariantes congeladas

```text
PlayerActor prefab nÃ£o Ã© owner de ActorId, ActorScope nem ParticipationPolicy runtime.
PlayerParticipation Ã© a fonte do actorScope estrutural do player materializado: invariant `SessionScoped`.
SessionParticipationContext carrega participaÃ§Ã£o resolvida antes do handoff.
ActivityEntryPipeline materializa/reusa Actor a partir de ActivityParticipantBinding.
SessionActorRuntimeStore Ã© Ã­ndice tÃ©cnico, nÃ£o owner de lifecycle.
ActivityPlayerActorRegistry e ActivitySceneActorRegistry nÃ£o decidem lifetime.
ActorScope nÃ£o decide lifetime de Presentation/Attribute/Permission/Movement/Camera.
RouteExit genÃ©rico retÃ©m SessionScoped.
ExitToMenu chama SessionReset apÃ³s RouteExit/save-on-exit.
SessionReset pode rodar apÃ³s ClosedForRouteExit sem reabrir Activity lifecycle.
```

### Fora do escopo deste checkpoint

```text
Runtime join real.
Multiplayer/split-screen.
Progression Save real de actors.
Policy avanÃ§ada de PowerUps/Attributes alÃ©m da observabilidade de ComponentLifetime.
Pooling real de ActorPresentation.
ReduÃ§Ã£o global de logs de InputModes/Loading/Permission.
```

### Resultado

`SA-ACTOR-1C1` fica fechado como PASS funcional para o objetivo de `ActorScope.SessionScoped` estrutural dentro da decomposiÃ§Ã£o de `SessionActivity` Base 2.0. Novos cortes de components/capabilities devem respeitar a separaÃ§Ã£o: Actor estrutural â‰  componente/capability material.



---

## Checkpoint SA-ACTOR-1C1-H8 â€” PlayerParticipation identity cleanup

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

Este checkpoint complementa o fechamento `SA-ACTOR-1C1` e corrige a fronteira entre `PlayerParticipation`, `SessionParticipationContext` e `ActivityEntryPipeline` sem reabrir o lifetime de components/capabilities.

### DecisÃµes congeladas

```text
Player estrutural vindo de PlayerParticipation Ã© sempre ActorScope.SessionScoped.
PlayerSetDefinition nÃ£o expÃµe mais actorScope editÃ¡vel.
ActorDefinitionId identifica archetype/definition.
ActorId identifica o Actor semÃ¢ntico do participante default.
SessionParticipantId Ã© derivado de PlayerSlotId.
Materialization seed resolution usa PlayerSlotId, nÃ£o ActorDefinitionId.
```

### Ownership final do corte

| Responsabilidade | Owner correto |
|---|---|
| Scope estrutural do player | `PlayerParticipation` / `OperationalPlayerParticipationStage`, sempre `SessionScoped` |
| Slot/assento | `PlayerSlotId` em `PlayerSetDefinitionEntry` / `PlayerParticipation` |
| SeleÃ§Ã£o default | `PlayerSelectionId` em `PlayerSetDefinitionEntry` |
| Definition/archetype | `ActorDefinitionId` em `ActorDefinitionAsset` |
| Actor semÃ¢ntico do participante default | `PlayerSetDefinitionEntry.actorId` |
| Participante de sessÃ£o | `SessionParticipantId`, derivado de `PlayerSlotId` |
| MaterializaÃ§Ã£o/reuso concreto | `ActivityEntryPipeline` / `ActivityParticipantBinding` / `PlayerActorRuntimeHandle` |

### Cortes fechados

```text
H8A  â€” Player scope invariant cleanup.
H8C1 â€” PlayerSlot materialization resolution.
H8C2 â€” SessionParticipantId by PlayerSlotId.
H8C3 â€” Player ActorId owner cleanup.
```

### EvidÃªncia aceita

Smoke canÃ´nico completo aceito:

```text
Boot -> Menu -> Sandbox
Activity 01 entry
CompleteActivationWindow
RestartCurrentActivity
CompleteActivationWindow
CompleteCurrentActivity
Activity 01 -> Activity 02
BackToMenu / ExitToMenu
```

CritÃ©rios observados:

```text
sem erro CS
sem [ERROR]
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
sem invalid_required_placement
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
actorIdSource='PlayerSetDefinitionEntry'
participantIdPolicy='PlayerSlotIdDerived'
resolutionKey='PlayerSlotIdToSessionParticipantId'
SessionResetCompleted sessionActorCount='0'
```

### Invariantes adicionadas

```text
ActorDefinitionAsset nÃ£o Ã© owner do ActorId do player participante.
PlayerSetDefinitionEntry Ã© owner autoral temporÃ¡rio do ActorId default do player.
ActorDefinitionId nÃ£o pode ser usado como chave runtime para reencontrar participante.
PlayerSlotId Ã© a chave de correlaÃ§Ã£o entre seed de PlayerParticipation e SessionParticipantBinding.
SessionParticipantId nÃ£o depende de Ã­ndice/ordem de lista.
```


---

## SA-13C1 â€” ActorAttribute command execution owner extraction

Status: `CLOSED / PASS funcional do command path + PASS arquitetural parcial`.

### DecisÃ£o

`TryApplyActorAttributeCommand` deixou de executar lÃ³gica interna de `ActorAttributes` dentro do `SessionActivityPipeline`.

```text
SessionActivityPipeline
-> valida o ciclo atual e normaliza o pedido
-> resolve a capability ativa por correlaÃ§Ã£o atual
-> monta ActorAttributeCommand
-> delega execuÃ§Ã£o para ActorAttributeEndpoint.TryApplyCommand(...)
```

### Owner preservado

```text
SessionActivityPipeline continua dono do momento/orquestraÃ§Ã£o do QA/runtime command.
ActorAttributeEndpoint Ã© o owner da execuÃ§Ã£o concreta do command de atributo.
ActivityActorExitRuntimeState continua correlation store tÃ©cnico para capabilities ativas.
```

### Escopo aplicado

```text
Removida do SessionActivityPipeline a aplicaÃ§Ã£o direta do command no endpoint.
Removida a resoluÃ§Ã£o auxiliar morta TryResolveActorInstanceIdForActor(...).
Reutilizado ActorAttributeEndpoint.TryApplyCommand(...).
Adicionado lookup mÃ­nimo de capability ativa por actorId no ActivityActorExitRuntimeState.
```

### Escopo explicitamente nÃ£o alterado

```text
ActorPresentation nÃ£o foi alterado.
ActorParticipation nÃ£o foi alterado.
Movement/Camera/Permission nÃ£o foram alterados.
ActivityObject nÃ£o foi alterado neste corte.
Content release, restart, next activity, route-exit e save/load nÃ£o foram alterados.
NÃ£o foi criado manager/coordinator/facade novo.
```

### EvidÃªncia aceita

Smoke manual confirmou o command path de attribute:

```text
ActorAttributeCommandRequested operation='Subtract'
ActorAttributeChanged previousValue='100' newValue='90'
QaSubtractActorAttribute outcomeKind='Applied'
ActorAttributeCommandRequested operation='Add'
ActorAttributeChanged previousValue='90' newValue='95'
QaAddActorAttribute outcomeKind='Applied'
```

Smoke macro preservado:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
```

### DÃ©bito residual controlado

```text
SessionActivityPipeline ainda resolve a capability ativa por actorId via correlation state.
Isso Ã© aceito como passo intermediÃ¡rio; execuÃ§Ã£o concreta jÃ¡ pertence ao ActorAttributeEndpoint.
```

---

## SA-13C-OBJ1-FIX â€” ActivityObject exit correlation mirror

Status: `CLOSED / PASS funcional + PASS arquitetural parcial`.

### Problema corrigido

A auditoria `SA-13C-OBJ1` classificou a causa como `EXIT_CORRELATION_MISSING`.

O estado de objeto nascia corretamente no entry:

```text
ActivityObjectContributorDiscoveryResult
ActivityCapabilityInventory preview
ActivityCapabilityInventoryValidationResult
```

Mas nÃ£o era congelado no `ActivityObjectExitRuntimeState` antes dos stages de saÃ­da. Como consequÃªncia, estes stages viam zero targets:

```text
ActivityObjectSnapshotCaptureStage
ActivityObjectReleaseStage
ActivityObjectContributorUnregisterStage
```

E o save-on-exit recebia payload ausente:

```text
RouteActivitySaveSnapshotPayload payloadResolved='false' failureReason='snapshot_payload_missing'
```

### SA-15C closure

```text
RouteActivitySaveContributorScopePolicy is normative.
CurrentActivityObjectSnapshot is the only active functional scope.
CurrentRouteSaveContributors and RouteAndActivitySaveContributors remain future contract/policy only.
activity_02 no-content now classifies as NoActivityContentContributors / no_activity_content_contributors.
SnapshotPayloadExpectedButMissing is reserved for expected contributors that failed to produce payload.
No LastUsefulActivityPayload fallback.
No scene scan for contributors.
No backend change.
```

### DecisÃ£o aplicada

ApÃ³s `ExecuteSetupAndReadiness(...)` concluir com sucesso e antes de `EnterActivationFlow(...)`, o pipeline congela a correlaÃ§Ã£o de saÃ­da para `ActivityObject`.

Pontos aplicados:

```text
EnterActivity(...)
ContinueAfterActivityContentLoadedSetReady(...)
```

### States copiados

```text
ActivityObjectContributorDiscoveryResult
ActivityCapabilityInventory preview
ActivityCapabilityInventoryValidationResult
```

`ActivitySetupInventory` nÃ£o foi copiado porque os stages de exit nÃ£o o consomem no shape atual.

### MÃ©todos usados

```text
ActivityObjectExitRuntimeState.ClearAll(...)
ActivityObjectExitRuntimeState.StoreContributorDiscoveryResult(...)
ActivityObjectExitRuntimeState.StoreInventoryPreview(...)
```

Nenhum mÃ©todo novo foi criado no exit state.

### Owner preservado

```text
ActivityEntryPipeline continua owner da produÃ§Ã£o do object setup/inventory.
ActivityObjectExitRuntimeState Ã© o correlation store tÃ©cnico para snapshot/release/unregister.
SessionActivityPipeline continua owner de ordering/lifecycle do boundary macro.
ActivityObjectSnapshotCaptureStage, ActivityObjectReleaseStage e ActivityObjectContributorUnregisterStage continuam stages determinÃ­sticos.
RouteActivitySave continua consumidor de payload capturado; nÃ£o decide discovery/release/unregister.
```

### Escopo explicitamente nÃ£o alterado

```text
NÃ£o houve reconstruÃ§Ã£o no exit.
NÃ£o houve lookup por cena no exit.
NÃ£o houve fallback silencioso.
activity_02 no-content nÃ£o foi alterada.
ActorAttribute, ActorPresentation, ActorParticipation, Movement, Camera, Permission e content release continuation nÃ£o foram alterados.
Save backend e policy de RouteActivitySave nÃ£o foram alterados.
```

### EvidÃªncia aceita

O smoke mostrou congelamento correto da correlaÃ§Ã£o:

```text
ActivityObjectExitCorrelationFrozen discoveryValid='true' discoveryCount='1' inventoryValid='true' inventoryCapabilityCount='11' inventoryValidationValid='true'
ActivityObjectExitRuntimeStateContributorDiscoveryStored discoveredCount='1'
ActivityObjectExitRuntimeStateInventoryPreviewStored inventoryCapabilityCount='11'
```

`activity_01` voltou a capturar, liberar e desregistrar `test_object_01`:

```text
ActivityObjectSnapshotCapture checkpointStatus='Passed' capturedCount='1' targetIds='test_object_01'
ActivityObjectRelease checkpointStatus='Passed' commandCount='1' appliedCount='1' targetIds='test_object_01'
ActivityObjectContributorUnregister checkpointStatus='Passed' unregisteredCount='1' targetIds='test_object_01'
```

ApÃ³s restart, o mesmo comportamento permaneceu vÃ¡lido para `entrySequence='2'`.

`activity_02` preservou o no-content explÃ­cito:

```text
ActivityObjectSnapshotCapture checkpointStatus='Passed' capturedCount='0' targetIds='<none>'
ActivityObjectRelease checkpointStatus='Passed' commandCount='0' targetIds='<none>'
ActivityObjectContributorUnregister checkpointStatus='Passed' unregisteredCount='0' skippedNoContributors='true'
```

Smoke macro preservado:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
```

### DÃ©bito residual controlado

A cÃ³pia foi implementada no `SessionActivityPipeline` como boundary macro apÃ³s `ExecuteSetupAndReadiness(...)`.

Aceito neste corte porque:

```text
SessionActivityPipeline nÃ£o executa discovery, reset, snapshot, release ou unregister.
SessionActivityPipeline apenas congela a correlaÃ§Ã£o entry->exit no boundary entre setup e activation.
Os stages de exit continuam lendo do ActivityObjectExitRuntimeState.
```

Shape final desejado para corte futuro:

```text
ActivityEntryPipeline produz e congela ActivityObject exit correlation diretamente.
SessionActivityPipeline apenas orquestra o boundary macro.
```

### Watchlist

`RouteActivitySave` ainda exige smoke prÃ³prio para payload Ãºtil quando o route-exit/salve-on-exit ocorrer apÃ³s uma activity com payload capturado ou quando a policy passar a preservar o Ãºltimo payload Ãºtil. No smoke aceito, `BackToMenu` ocorreu a partir de `activity_02`, que Ã© no-content, portanto `RouteActivitySave` nÃ£o foi fechado como payload Ãºtil final.

---

## SA-13D Ã¢â‚¬â€ Runtime surface audits

Status: `CLOSED / AUDITED`.

### Fechamento consolidado

```text
SA-13D1 — ActivityObject unregister/finalization back-reference cleanup: CLOSED.
SA-13D2 — PlayerInputBinding runtime surface audit: CLOSED / AUDITED.
SA-13D3 — ActivityCapabilityPermission runtime surface audit: CLOSED / AUDITED.
SA-13D3A — Permission dead helper cleanup: CLOSED.
SA-13D4 — Movement retained/control bridge audit: CLOSED / AUDITED.
SA-13D5 — Camera binding runtime surface audit: CLOSED / AUDITED.
SA-13D6 — ActivityContent load/release runtime surface audit: CLOSED / AUDITED.
SA-13D7 — RouteActivitySave payload/handoff audit: CLOSED / AUDITED.
```

### ConclusÃµes normativas

```text
Nao ha patch imediato recomendado para PlayerInput, Permission, Movement, Camera, ActivityContent ou RouteActivitySave.
PlayerInputBinding ainda tem debito futuro de RuntimeConfigRegistry lookup tardio no adapter, mas nao recebeu patch.
Camera explicit composition para ActivityCameraAnchorHost foi fechado em SA-16E1.
Movement retained/control e ActivityContent release/continuation permanecem por alto risco e nao devem ser reduzidos agora.
RouteActivitySave atual salva/skipa com base na rota/activity imediatamente anterior concluida.
Se o produto quiser preservar o "last useful payload" em vez da activity imediatamente anterior concluida, isso exige policy explicita nova.
Nao criar fallback silencioso para payload antigo sem policy explicita.
```

### DÃƒÂ©bitos futuros registrados

```text
ActivityCameraAnchorHost explicit scene-scope composition foi fechado em SA-16E1.
RouteActivitySave policy gap: current completed activity vs last useful snapshot payload.
Movement retained/control surface defer high risk.
ActivityContent release/continuation surface defer high risk.
```

### Regras de fechamento

```text
SA-13D foi fechado como auditoria documental, sem novo smoke funcional.
SA-14B1 foi fechado como corte funcional + arquitetural do handoff explicito.
O smoke existente continua sendo a referencia de validacao funcional anterior.
Nenhum fallback silencioso deve ser criado para payload antigo.
```

## Estado normativo atual

### Status consolidado

```text
SA-13D - Runtime surface audits: CLOSED / AUDITED
SA-13D1 - CLOSED
SA-13D2 - AUDITED
SA-13D3 - AUDITED
SA-13D3A - CLOSED / compile validation sufficient
SA-13D4 - AUDITED
SA-13D5 - AUDITED
SA-13D6 - AUDITED
SA-13D7 - AUDITED / POLICY_GAP
SA-14B1 - ActivityObject exit correlation explicit entry result: CLOSED / PASS funcional + PASS arquitetural do corte
SA-14C - residual bridge / carrier matrix: CLOSED / AUDITED
```

### Cortes fechados ou auditados sem patch

```text
SA-13C1 - CLOSED
SA-13C-OBJ1-FIX - CLOSED
PlayerInput binding - audited, no patch
ActivityCapabilityPermission - audited, no patch
Movement retained/control - audited, high risk, no patch
Camera binding - audited, no patch
ActivityContent load/release - audited, high risk, no patch
RouteActivitySave payload/handoff - audited, policy gap, no patch
SA-14B1 - CLOSED / PASS funcional + PASS arquitetural do corte
SA-14C - CLOSED / AUDITED
```

### Debitos futuros

```text
RouteActivitySave policy gap: current completed activity vs last useful snapshot payload
Movement retained/control surface defer high risk
ActivityContent release/continuation surface defer high risk
ActivityObject exit correlation observability hygiene: closed as SA-17D; technical ownership stays in ActivityObjectExitRuntimeState and SessionActivityPipeline remains only the macro ordering/freeze boundary.
`SA-17A` closed the `RunActivityContentOperation(...)` dispatch split.
`SA-17B` removed the `LoadedSet` bridge and moved store/clear to the technical runtime state.
`SA-17C` removed the `ActivityContent` aggregate bridge without introducing a substitute bridge name.
`SA-17D` closed the `ActivityObject exit correlation observability hygiene` cut with `ActivityObjectExitRuntimeState` as technical owner and `SessionActivityPipeline` only as macro ordering/freeze owner.
`SA-17D-FIX` restored `activity_02` no-content RouteActivitySave classification to `NoActivityContentContributors / no_activity_content_contributors`; `SnapshotPayloadExpectedButMissing` remains reserved for expected contributors that failed to produce payload.
IActivityEntryParticipantBindingRuntimeBridge remains a possible future split candidate.
ActivityContentReleaseRuntimeState remains defer high risk.
Movement retained/control remains defer high risk.
```

### Reabertura proibida sem regressao

```text
Nao criar fallback silencioso para payload antigo
Nao salvar last useful payload sem new explicit policy
Nao reabrir Movement ou ActivityContent sem regressao evidenciada
Nao criar ActivityExitPipeline or ActivityContentReleasePipeline only for symmetry
Nao reabrir SA-14B1 exit correlation production; the entry already owns the carrier.
```

## SA-14E - SessionActivity decomposition closure matrix

Status: CLOSED / AUDITED.

Resumo:

- A decomposicao runtime atual de `SessionActivity` fica congelada como checkpoint temporario.
- `SA-14B1` permanece como `CLOSED / PASS funcional + PASS arquitetural do corte`.
- `SA-13D`, `SA-14C` e `SA-14D` permanecem fechados como auditorias.
- Os residuos restantes ficam classificados como `DEFER_HIGH_RISK`, `POLICY_GAP`, `FUTURE_CLEANUP_MEDIUM` e `DO_NOT_REOPEN_WITHOUT_REGRESSION`.

### Matriz final

```text
CLOSED_PASS:
  SA-14B1 - ActivityObject exit correlation explicit entry result
  SA-17D - ActivityObject exit correlation observability hygiene
  SA-17D-FIX - restore activity_02 no-content RouteActivitySave classification

CLOSED_AUDITED:
  SA-13D - Runtime surface audits
  SA-14C - residual bridge / carrier matrix
  SA-14D - ActivityContent pending-operation bridge audit

DEFER_HIGH_RISK:
  Movement retained/control surface
  ActivityContent release/continuation surface
  ActivityContentReleaseRuntimeState

POLICY_GAP:
  RouteActivitySave policy gap: current completed activity vs last useful snapshot payload

FUTURE_CLEANUP_MEDIUM:
  IActivityEntryParticipantBindingRuntimeBridge possible split
DO_NOT_REOPEN_WITHOUT_REGRESSION:
  Movement
  ActivityContent
  RouteActivitySave
  SA-14B1 exit correlation production
  ActivityContent release/continuation
  ActivityContent pending-operation callback path
```

### Smoke global baseline

```text
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
RestartCurrentActivity
Activity01ToActivity02
RouteExitBackToMenu
ActivityObjectSnapshotCapture, ActivityObjectRelease, ActivityObjectContributorUnregister quando aplicavel
```

### Regras de fechamento

```text
Os itens marcados como DEFER_HIGH_RISK nao devem ser reabertos sem regressao concreta.
RouteActivitySave last useful payload e policy nova, nao bug local.
Qualquer alteracao futura no pending-operation callback path exige smoke completo.
```

## SA-14D - ActivityContent pending-operation bridge audit (histórico)

Status: CLOSED / AUDITED (historical; superseded by SA-17A).

Resumo:

- Historicamente, `IActivityEntryContentPendingOperationRuntimeBridge` cobria apenas build/set state registration antes de `SA-17A`.
- `RunActivityContentOperation(...)` is owned by `ISessionActivityPendingOperationRunner`, with `SessionActivityPipeline` as callback boundary.
- `SessionActivityPipeline` remains the callback boundary through `ISessionActivityPendingOperationCallback`.
- No fallback silencioso, no new lookup tardio, and no duplicate owner were found in the audited path.
- No immediate runtime patch is recommended.
- Any future change in this path requires full smoke validation.

### Backlog futuro

```text
IActivityEntryParticipantBindingRuntimeBridge possible split
```

## SA-14C - residual bridge / carrier matrix

Status: CLOSED / AUDITED.

Resumo:

- No new wrong owner, duplicate owner, fallback silencioso, or new lookup tardio were found inside SessionActivity.
- No bridge documented as removed was still active in the code path audited.
- No immediate runtime patch is recommended.
- `SA-17A` closed the pending-operation bridge dispatch split.
- `SA-17B` removed the `LoadedSet` bridge and moved store/clear to `ActivityContentRuntimeState`.
- `SA-17C` removed the aggregate `ActivityContent` bridge without introducing a substitute bridge name.
- `SA-17D` closed the `ActivityObject exit correlation observability hygiene` cut; `ActivityObjectExitRuntimeState` remains the technical owner and `SessionActivityPipeline` remains only the macro ordering/freeze owner.
- `SA-17D-FIX` restored the `activity_02` no-content RouteActivitySave classification to `NoActivityContentContributors / no_activity_content_contributors`.
- `SA-18A7-FIX7-DOC` recorded the validated closure note for retained PlayerActor rebind plus permission scanner guard closure; runtime ownership remains unchanged and is inherited from the `SA-18A7-FIX7` smoke baseline.
- `SA-18A8-A9-DOC` recorded the participant-binding bridge residual cleanup closure: placement marker lookup left the participant binding bridge in SA-18A8, and participation context store left the bridge in SA-18A9-H1 with separate runtime-state owners.
- `IActivityEntryParticipantBindingRuntimeBridge` remains a possible future split candidate.
- `Movement retained/control` and `ActivityContentReleaseRuntimeState` remain high risk.

## Historico / checkpoints anteriores

```text
This ADR keeps previous roadmap and checkpoint history for traceability.
Historical sections remain informative, but the consolidated status above is the source of truth for the current workfront.
```


## SA-16A - Movement / GameplayControl / Reset / Save boundary closure

Status: CLOSED.

### SA-16A1 - Initial Movement Blocked state ownership cleanup

- MovementBindingAdapter deixou de publicar gate state.
- MovementBindingAdapter passou a ser preparation/binding tecnico.
- ActivityEntryMovementBindingStage continua orquestrando a publicacao inicial de Blocked.
- ActivityCapabilityPermissionRuntime continua aplicando command/fact/snapshot e notificando receivers.
- PlayerMovementPermissionReceiver continua como reaction local.
- SessionActivityPipeline continua como macro lifecycle owner.
- Nenhuma alteracao em Save, Reset, Camera, Presentation ou Attributes.

### SA-16A2 - MovementTransient reset endpoint support

- PlayerActorDefaultResetEndpoint passou a suportar ActorResetGroup.MovementTransient.
- MovementTransient limpa apenas estado runtime/transitorio local via PlayerMovementController.ClearMovementState().
- Movement continua fora de Save/Snapshot.
- Nao houve alteracao em gate/permission/control.
- Nao houve fallback global, first player, lookup textual ou cruzamento indevido de identidades.
- ActivityEntryParticipantBindingStage continua dono do mapping/classificacao RuntimeTransient -> MovementTransient.
- ActorResetAdapter continua o executor canonico de reset.

### Invariantes registradas

- Movement e capability local de Actor.
- MovementBinding e stage de ActivityEntryPipeline.
- Gate/control nao pertence ao MovementController.
- Movement pode registrar endpoint bloqueavel/reagivel.
- Movement pode expor reset transitorio.
- Movement nao e save contributor por padrao.
- Save so consome snapshot provider explicito.
- Reset nao e Save.
- Reset nao decide lifecycle.
- Receiver local reage; nao decide policy.
- Pipeline decide macro lifecycle; nao manipula componente de Movement diretamente.

## SA-16D - PlayerInput canonical actions explicit composition

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

### Fechamento consolidado

```text
SessionActivityCompositionInstaller resolve e valida o InputActionAsset canonico.
ActivityEntryPipeline recebe o asset canonico por construtor.
ActivityEntryPipeline instancia PlayerInputBindingAdapter com dependencia explicita.
PlayerInputBindingAdapter nao consulta RuntimeConfigRegistry.
PlayerInputBindingAdapter apenas aplica/rebinda o PlayerInput usando o asset resolvido.
SessionActivityPipeline nao mantem mais instancia morta de PlayerInputBindingAdapter.
PlayerInputManager nao foi alterado.
Nao houve config duplicada no prefab.
Nao houve alteracao em lifecycle, Movement, PermissionRuntime, InputModes global, PlayerParticipation, Camera, Save, Reset, Presentation ou Attributes.
```

### Ownership final

```text
Composition root resolve e valida config obrigatoria.
ActivityEntryPipeline decide quando executar binding.
PlayerInputBindingAdapter e adapter puro de aplicacao/rebind.
InputModes continua dono de mode/action map global.
Unity PlayerInput / PlayerInputManager continuam componentes Unity-owned, usados apenas por API publica/suportada.
```

### Invariantes finais

```text
PlayerInputBindingAdapter nao consulta RuntimeConfigRegistry.
Adapter nao resolve config global.
Adapter recebe payload runtime resolvido.
Ausencia de config obrigatoria e erro na composicao, nao fallback silencioso no adapter.
Nao duplicar action asset no prefab.
Nao criar PlayerInputManager paralelo.
Nao usar Resources.Load.
Nao usar reflection.
Nao acessar internals/campos privados da Unity.
Nao alterar generated input actions.
Nao misturar PlayerInput binding com Movement gate/control.
ActivityEntryPipeline continua dono do binding timing.
InputModes continua dono do input mode global.
PlayerParticipation continua dono da participacao/slots; Activity materializa/binda.
```

### Evidencia aceita

```text
PlayerInputActionsReboundToCanonical preservado.
ActivityEntryPlayerInputBindingCompleted preservado.
MovementBindingCompleted preservado.
PlayerMovementPermissionApplied preservado com Blocked, Allowed e Unbound.
Movimento funcional em activity_01 e activity_02.
RestartCurrentActivity PASS.
Activity01ToActivity02 PASS.
RouteExitBackToMenu PASS.
RouteActivitySave preservou classificacao NoActivityContentContributors, sem regressao para SnapshotPayloadExpectedButMissing.
```
## SA-16F closure

- `SA-16F` - `Route/session save contributor inventory audit`: `CLOSED / Backlog controlado`.
  - The active `RouteActivitySave` flow remains limited to `CurrentActivityObjectSnapshot`.
  - `SessionActivityPipeline` provides the payload via `ISessionActivitySnapshotPayloadProvider`.
  - `ActivityObjectExitRuntimeState` remains the technical store for the payload.
  - `SaveRuntime` continues to execute technical storage only.
  - No real route-scoped or session-scoped save contributors exist yet.
  - Do not create generic save-contributor infrastructure until a real capability and a clear owner exist.
