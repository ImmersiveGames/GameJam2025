# ADR-2.0-0002 — SessionActivity Ownership Decomposition e ActivityEntryPipeline

## Status

Proposto para congelamento antes de implementação.

## Área

`SessionActivity` / `ActivityEntryPipeline` / `RouteExit teardown` / `ActivityCapability` / `Actor participation`

## Contexto

A auditoria de `SessionActivity` identificou que o fluxo ainda não atende ao objetivo da Base 2.0. O problema principal não é apenas tamanho de arquivo: o `SessionActivityPipeline` concentra decisões e execução de lifecycle, transition, content load/release, snapshot, route-exit teardown, visual readiness, actor setup, capability setup e permission handling.

A auditoria também identificou que `ActivityEntryPipeline` existe apenas como contrato/boundary, sem pipeline concreto, e que o lifecycle de `RouteExit teardown` possui owner duplicado entre `SessionActivityHost` e `SessionActivityPipeline`.

Base 2.0 não deve transformar um pipeline grande em outro componente grande. A regra anti-deslocamento do `SessionOperational` passa a valer também para `SessionActivity`: nenhuma extração é aceita apenas para reduzir tamanho; toda extração precisa ter owner, categoria e critério de aceite.

## Problema

O estado atual gera estes riscos:

1. `SessionActivityPipeline` permanece como god object.
2. `ActivityEntryPipeline` ainda não é owner real do entry lifecycle.
3. `RouteExit teardown` tem decisão/estado duplicados entre Host e Pipeline.
4. `SessionActivityHost` mistura boundary externo, decisão de flow e registro global via service locator.
5. Duplicação de listas/policies de stages pode gerar branch drift.
6. Permission target mistura domínios de identidade (`PlayerActorId`, `PlayerSlotId`, receiver técnico) em um mesmo campo.
7. QA/hardcodes podem continuar mascarando contrato real.

## Decisão

### 1. `SessionActivityPipeline` permanece como owner macro

`SessionActivityPipeline` é owner de:

```text
session activity lifecycle macro
activity-to-activity transition policy
restart current activity lifecycle
route-exit handoff recebido do SessionOperational
ordem macro Entry -> ActivationWindow -> ActivityRunning -> Completion -> DeactivationWindow -> Exit/Next
proteção foreign/stale da sessão ativa
```

Ele pode chamar pipelines/stages concretos explicitamente.

Ele não deve executar diretamente:

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

`ActivityEntryPipeline` é owner do lifecycle determinístico de uma `ActivityEntry`.

Ele recebe um comando de entrada com payload runtime já resolvido:

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
- Route-scoped context necessário
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

Ele é owner de:

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

Ele não decide:

```text
qual é a próxima Activity
quando a Activity termina
quando a ActivationWindow é completada pelo usuário/QA
quando a DeactivationWindow é completada
quando a rota troca
save/progression global
route operation lifecycle
```

### 3. `RouteExit teardown` deve ter owner único

`SessionActivityPipeline` decide o lifecycle de teardown de activity para `RouteExit`.

`SessionActivityHost` deve ser endpoint/delegador externo, não owner de decisão.

Permitido ao Host:

```text
expor boundary para SessionOperational
encaminhar request para SessionActivityPipeline
aguardar resultado publicado pelo pipeline
validar ausência/presença mínima de pipeline ativo
retornar result externo
```

Proibido ao Host:

```text
classificar stage de teardown como policy final
manter lista própria divergente de stages
avançar lifecycle de route-exit por conta própria
registrar estado como fonte de verdade de teardown
executar side-effects de teardown
```

### 4. Policies devem ser únicas e explícitas

Duplicações como `IsRouteExitTransitStage` e `IsDeactivationTransitionStage` devem convergir para policy única quando fizerem parte do mesmo domínio de decisão.

Policy classifica:

```text
skip
failure
route-exit transit
stale/foreign
required/optional capability
stage allowed/blocked
```

Policy não executa side-effect.

### 5. Commands não carregam infraestrutura

Commands de Base 2.0 não podem carregar:

```text
Stage
Boundary
Adapter
Func<T>
Action
MonoBehaviour executor genérico
state mutável compartilhado
ScriptableObject autoral inteiro quando só é necessário payload resolvido
```

Commands carregam payload runtime resolvido.

### 6. Facts não executam side-effects

Facts registram o que aconteceu. Não podem:

```text
chamar EventBus
chamar adapter
alterar lifecycle
criar command operacional
resolver próxima stage
```

### 7. Adapters executam side-effects, não lifecycle

Adapters podem executar side-effects Unity comandados por pipeline/stage:

```text
load/unload scene
materialize/release presentation
bind input/camera/movement
apply/reset endpoint local
capture/restore snapshot local
```

Adapters não decidem:

```text
next activity
route exit
required vs optional
fallback de configuração obrigatória
entry lifecycle
policy de stage order
```

### 8. Permission identity precisa separar domínios

O débito de `targetId` deve ser tratado como identidade ambígua.

Separação alvo:

```text
ActorInstanceRuntimeId / ActorId: identidade do actor runtime
PlayerActorId: identidade semântica de player actor
PlayerSlotId: slot/entrada do jogador
ReceiverId: identidade técnica do receiver local
PermissionTargetId: identidade do alvo de permission, sem misturar slot/actor/receiver
```

Enquanto o receiver atual for player-specific, o contrato pode continuar carregando `PlayerActorId` e `PlayerSlotId`, mas não deve comparar domínios diferentes como fallback.

### 9. Actor convergence continua normativa

`PlayerActor` e `NonPlayerActor` não devem voltar a virar rails paralelos permanentes.

`ActivityEntryPipeline` deve consumir o shape já aceito de:

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

Variação concreta de actor deve aparecer como typed policy/capability/endpoint, não como branch global `player/nonplayer` no pipeline.

#### Guarda corretiva pós-auditoria SA-5

A tentativa de criar um corte específico de `NonPlayerActorDiscovery` como owner de entry foi classificada como premissa arquitetural errada.

Regra normativa:

```text
Actor é a única entrada arquitetural para discovery/readiness/setup de actors.
PlayerActor, NonPlayerActor e outros tipos concretos podem existir como especializações, metadata, endpoint, authoring ou fonte transitória.
Essas especializações não podem definir cortes, stages ou lifecycle rails próprios no ActivityEntryPipeline.
```

Nomes transitórios existentes no código, como `NonPlayerActorDiscovery`, só podem permanecer enquanto forem fontes/adapters para um contrato canônico de `ActorDiscovery`/`ActorInventoryFeed`. Eles não podem ser promovidos a owner final nem usados como precedente para novos cortes.

### 10. Sem compatibility rails novos

Não criar:

```text
ActivityEntryPipeline paralelo opcional
manager/coordinator genérico para esconder pipeline novo
fallback para caminho antigo quando o novo falhar
alias/compat permanente para stages ou results antigos
bridge stage-to-stage como owner final
```

Extração transitória só é aceita quando:

```text
for curta
for documentada
não tiver dois owners ativos
não criar fallback silencioso
remover ou substituir o caminho antigo no mesmo corte ou em corte imediatamente seguinte
```


### 11. Observabilidade não pode antecipar lifecycle

Logs, facts, snapshots e traces precisam representar o lifecycle real, não apenas a etapa recém-extraída.

É proibido emitir evento com semântica de conclusão total quando apenas um subpasso terminou.

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
O nome do fact/log deve corresponder ao escopo realmente concluído.
Completed de pipeline inteiro só pode ser emitido quando o pipeline inteiro terminou.
Completed de stage/subpasso deve carregar o nome do stage/subpasso, não do pipeline pai.
```

Essa regra vale mesmo quando o smoke funcional passa. Smoke sem erro não valida semântica de ownership.

### 12. Snapshots/índices runtime têm writer canônico único

Snapshots e índices runtime passivos, como `ActivityCapabilityInventoryPreview`, não podem ter múltiplos writers tardios.

O owner correto do inventory de entry é o `ActivityEntryPipeline` ou o stage canônico chamado por ele.

Stages posteriores devem consumir o inventory resolvido. Eles não podem reconstruir e sobrescrever o mesmo state canônico para satisfazer uma necessidade local.

Proibido:

```text
MovementBinding reconstruir ActivityCapabilityInventoryPreview e gravar CurrentActivityCapabilityInventoryPreview.
CameraBinding reconstruir ActivityCapabilityInventoryPreview e gravar CurrentActivityCapabilityInventoryPreview.
Actor/Object setup reconstruir inventory canônico para esconder ausência de capability.
```

Permitido:

```text
MovementBinding consultar o inventory canônico da entry.
CameraBinding consultar o inventory canônico da entry.
Actor/Object setup consultar o inventory canônico da entry.
Stage falhar explicitamente quando o inventory esperado estiver ausente, stale, foreign ou incompleto.
```

Se um stage posterior precisa de capability ausente no inventory canônico, a correção deve ocorrer no owner do inventory, não por rebuild local.

Regra normativa:

```text
Um snapshot runtime canônico tem um writer ativo por lifecycle.
Consumidores não podem virar writers para corrigir falta local.
Rebuild local só é permitido como diagnóstico temporário, documentado e removido no mesmo corte ou no corte imediatamente seguinte.
```

### 13. Correção funcional não basta quando a fronteira continua ambígua

Um corte pode passar no smoke e ainda assim não ser aceito como PASS arquitetural final se:

```text
o owner correto não estiver visível;
o log/fact declarar conclusão mais ampla do que ocorreu;
um state canônico tiver múltiplos writers;
um consumidor posterior reconstruir dados que deveriam vir do owner anterior;
a correção esconder falta de contrato com fallback local.
```

Nesses casos, o corte pode ser classificado apenas como:

```text
PASS funcional
PASS arquitetural parcial
PENDING hygiene/ownership normalization
```

A normalização deve ser feita antes de migrar o próximo bloco dependente.

## Ownership final por categoria

| Categoria | Owner correto | Observação |
|---|---|---|
| Session activity macro lifecycle | `SessionActivityPipeline` | Ordem macro, handoff, next/restart/route-exit |
| Activity entry lifecycle | `ActivityEntryPipeline` | Content/setup/readiness/bindings por entry |
| Activity transition policy | `SessionActivityPipeline` + policy dedicada | Decide next/restart/complete, não side-effect |
| RouteExit teardown lifecycle | `SessionActivityPipeline` | Host delega, não decide |
| ActivityContent side-effects | Adapter/stage de content | Pipeline comanda, adapter executa |
| Actor/Object setup | Entry stages | Sem rails player/nonplayer paralelos permanentes |
| Actor capability behavior local | Endpoint local | Endpoint reage, não decide lifecycle global |
| Permission reaction concreta | Receiver local | Pipeline publica state; receiver aplica localmente |
| Facts/traces | Recorder/fact emitter | Registro apenas |
| Composition/global registry | Composition root/installer | Não no Host como lifecycle owner |
| QA probes | Endpoints QA isolados | Nunca owner final de lifecycle |

## Plano normativo consolidado de refatoração

Este plano substitui a sequência inicial genérica. Ele é parte normativa deste ADR e deve guiar a implementação de `SessionActivity` Base 2.0.

A regra principal é:

```text
SessionActivityPipeline mantém lifecycle macro, transition, restart, route-exit e handoffs.
ActivityEntryPipeline vira owner real do lifecycle determinístico da entry.
Stages executam passos determinísticos.
Policies classificam skip/failure/required/optional/stale/foreign.
Commands carregam payload runtime resolvido.
Facts registram o que ocorreu.
Adapters executam side-effects.
Endpoints reagem localmente.
```

Nenhum corte deve ser aceito apenas por reduzir tamanho de arquivo. Um corte só é válido se remover responsabilidade concreta do owner errado, atribuir owner correto, remover ou tornar inacessível o caminho antigo equivalente, não criar fallback e preservar smoke/log.

### Estado já fechado

| Corte | Status normativo | Resultado |
|---|---|---|
| `SA-0` | Fechado | ADR/plano inicial criados. |
| `SA-1` | Fechado | `RouteExit teardown` com owner único no `SessionActivityPipeline`; Host delega. |
| `SA-2` | Fechado | `ActivityEntryPipeline` concreto criado. |
| `SA-2B` | Fechado | Owner `ActivityEntryPipeline` visível nos logs. |
| `SA-3A` | Fechado | `ActivityContent load/prepare/readiness` movido para `ActivityEntryPipeline`. |
| `SA-3A-H1` | Fechado | Corrigida observabilidade prematura de `ActivityEntryPipelineCompleted`; `ActivityEntryPreparationAccepted` substitui conclusão falsa. |

### Estado ainda problemático

Mesmo após `SA-3A-H1`, o código ainda não atende ao desenho final do ADR porque:

```text
ActivityEntryPipeline ainda não é owner real de setup/readiness completo.
EmitNominalActivitySetup ainda concentra setup real no SessionActivityPipeline.
ObjectReset/ObjectRestore ainda pertencem ao miolo de entry e dependem de ordem correta com Inventory.
ActivityObjectEntryStage interno ainda é wrapper/fachada se apenas chamar métodos Core do SessionActivityPipeline.
IActivityEntryRuntimeEndpoint ainda é bridge transitória e não pode crescer como fachada permanente.
```

### Regra de replanejamento

O plano original `SA-3 = ActivityContent + Inventory` foi refinado pela auditoria consolidada. O próximo passo não é mover apenas `ActivityCapabilityInventory` isoladamente. Antes, deve-se corrigir a ordem e o ownership do subfluxo mínimo que torna o inventory canônico útil para os consumidores.

---



### Corte corretivo aplicado localmente — SA-5A0 + SA-5A1

Status: implementado neste pacote, **pendente de smoke/log**.

Objetivo:

```text
Interromper regressão de Actor rails e isolar mistura de identidade antes de continuar ActorDiscovery genérico.
```

Decisões aplicadas:

```text
SA-5A0 — Actor rail regression stopper
- Actor é a única entrada arquitetural para discovery/readiness/setup.
- PlayerActor/NonPlayerActor permanecem tipos concretos, mas não podem definir stage/corte/rail final.
- Fontes transitórias podem alimentar ActorInventoryFeed/ActorScanTarget.
- README de SessionActivity recebeu guarda anti-regressão explícita.

SA-5A1 — Identity quarantine
- PlayerActorMaterializationAdapter não pode mais definir ActorId a partir de PlayerSlotId.
- ActorId do PlayerActor materializado passa a ser o PlayerActorId semântico.
- Permission command expõe TargetActorId em vez de TargetId ambíguo.
- Permission binding/reference exige TargetActorId quando scope=Actor.
- PlayerMovementPermissionReceiver só aceita TargetActorId == PlayerActorId.
- PlayerSlotId e ReceiverId deixam de ser fallback de matching de alvo.
- Camera requirement matching deixa de aceitar PlayerSlotId como alias de TargetId.
```

Não congelar como PASS sem smoke contendo:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
MovementControlEnabled em ActivityRunning
MovementControlDisabled em completion/route-exit
sem PermissionTargetIdentityUnresolved em cenário válido
sem fallback TargetActorId == PlayerSlotId
sem fallback TargetActorId == ReceiverId
ActorId, PlayerActorId e PlayerSlotId observáveis como domínios separados
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```

## Roadmap normativo por fases

### Fase A — Consolidar entry lifecycle real

#### `SA-3B0 — Entry Setup Pre-Inventory Ownership / Ordering Correction`

Objetivo: mover para `ActivityEntryPipeline` o primeiro bloco real de setup que hoje impede o inventory canônico de nascer na ordem correta.

Escopo permitido:

```text
ActivitySetupInventory
ObjectSnapshotContractValidation
ObjectReset
ObjectRestore
ActivityCapabilityInventoryPreview
QA reset ligado a esse caminho canônico
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

Critério arquitetural:

```text
ActivityEntryPipeline owna o subfluxo.
SessionActivityPipeline não executa diretamente esse bloco.
ObjectReset não reconstrói inventory local.
ObjectRestore não depende de preview vazio/antigo.
QA reset chama caminho canônico, não trilho paralelo.
Inventory canônico nasce antes dos consumidores desse bloco.
ActivityObjectEntryStage interno não é expandido como fachada.
IActivityEntryRuntimeEndpoint não cresce como owner remoto do god pipeline.
```

#### `SA-3B1 — ActivityCapabilityInventory ownership final`

Objetivo: completar a migração do `ActivityCapabilityInventory` para owner real no `ActivityEntryPipeline` ou em stage canônico chamado por ele.

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
ActivityCapabilityInventory é snapshot/índice runtime passivo.
Ele não decide lifecycle.
Ele tem writer único por lifecycle.
Consumidores não podem reconstruí-lo para corrigir falta local.
Não confundir ActivityCapabilityInventory com ActivitySetupInventory.
```

---

### Fase B — Transformar setup de objetos em entry stages reais

#### `SA-4A — ActivityObjectEntryStage real / auditoria`

Resultado da auditoria pós `SA-3B0` + `SA-3B1`:

```text
SA-3B0 já transferiu para ActivityEntryPipeline o subfluxo:
- ActivitySetupInventory;
- ObjectSnapshotContractValidation;
- ActivityCapabilityInventoryPreview;
- ObjectReset;
- ObjectRestore.

Portanto, SA-4A não deve recriar ActivityObjectEntryStage do zero.
O débito real restante é ActivityObjectContributorDiscovery ainda nascer no SessionActivityPipeline.
```

Decisão normativa:

```text
SA-4A deve ser reinterpretado como sequência pequena de cleanup, começando por SA-4A0.
Não reabrir reset/restore/inventory sem evidência de regressão.
Não criar stage paralelo para o que SA-3B0 já moveu.
```

#### `SA-4A0 — ActivityObjectContributorDiscoveryStage real`

Objetivo: mover `ActivityObjectContributorDiscovery` para stage real chamado pelo `ActivityEntryPipeline`, removendo execução concreta do `SessionActivityPipeline`.

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
SessionActivityPipeline não chama DiscoverActivityObjectContributorsOrSkipCore.
ActivityEntryPipeline chama ActivityEntryObjectContributorDiscoveryStage antes de ActivitySetupInventory.
Discovery result tem writer único por entry.
ActivitySetupInventory e SnapshotContractValidation consomem discovery result produzido no mesmo owner.
Sem fallback para discovery antigo.
Sem bridge grande nova.
A bridge transitória só pode expor setter técnico de CurrentActivityObjectContributorDiscoveryResult.
```

Critério de aceite:

```text
ActivityObjectContributorDiscovery facts preservados.
ActivityObjectContributorDiscovery checkpoint preservado.
SessionActivityPipeline perde execução direta de discovery.
ActivityEntryPipeline é owner do stage.
Sem alteração de ActorPresentation, ActorAttributes, ActorParticipation, PlayerInput, Movement, Camera, Release, Deactivation ou RouteExit.
```

---

#### `SA-4B — ActivityObjectSnapshot/Reset/Restore cleanup`

Objetivo: separar snapshot/reset/restore em commands/facts/adapters claros.

Critério:

```text
Reset obrigatório ausente = fail-fast.
Reset opcional ausente = skip explícito.
ResetAll cego proibido.
Snapshot restore não decide lifecycle.
Facts não executam side-effects.
Adapters/endpoints executam aplicação local.
```

---

### Fase C — Actor setup por entry

#### `SA-5A — ActorDiscovery / ActorReadiness ownership audit`

Objetivo: auditar e redesenhar o setup de actors para garantir que `Actor` seja a única entrada arquitetural de lifecycle/readiness no `ActivityEntryPipeline`.

Escopo canônico:

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
stage de lifecycle nomeado por especialização concreta de Actor
```

Critério:

```text
Actor é a única raiz de entrada para discovery/readiness.
PlayerActor, NonPlayerActor e especializações futuras são tipos/metadata/endpoints locais, não owners de lifecycle.
Fontes transitórias com nomes antigos podem alimentar ActorInventoryFeed, mas não definir stage/corte/owner canônico.
Sem comparar ActorId, PlayerActorId, ActorInstanceRuntimeId e PlayerSlotId como equivalentes.
Variação concreta de actor aparece como typed policy/capability/endpoint, nunca como branch global player/nonplayer no pipeline.
```

Decisão corretiva:

```text
Qualquer corte chamado NonPlayerActorDiscovery, PlayerActorReadiness ou equivalente deve ser rejeitado antes de implementação.
O próximo corte autorizado nesta área é auditoria/correção de ActorDiscovery genérico.
```

#### `SA-5B — ActorPresentation setup stage`

Objetivo: mover `ActorPresentation` setup para stage real de entry.

Critério:

```text
Retention policy explícita.
Materialization em adapter.
Stage não decide next activity.
Presentation obrigatória ausente falha explicitamente.
Sem fallback silencioso.
```

#### `SA-5C — ActorAttributes setup stage`

Objetivo: mover setup de attributes para stage real de entry.

Critério:

```text
Attributes são capability local.
Pipeline/stage prepara/descobre.
Reação local não vira command global quando for ação local.
```

#### `SA-5D — ActorParticipation enter stage`

Objetivo: mover participation enter/readiness para stage real.

Critério:

```text
Participation context explícito.
Ausência obrigatória fail-fast.
Ausência opcional skip explícito.
Sem branch global player/nonplayer.
```

---

### Fase D — Input, Permission, Movement e Camera

#### `SA-6A — PlayerInput binding stage`

Objetivo: mover `PlayerInputBinding` para stage real de entry.

Critério:

```text
Usa asset canônico já resolvido.
Não cria configuração duplicada no prefab.
Não mexe no OperationalInputRuntime.
Binding é preparation; enable/disable pertence a permission/lifecycle apropriado.
```

#### `SA-6B — Permission target preparation stage`

Objetivo: preparar permission targets como entry stage, sem redesenhar toda identity no mesmo corte.

Critério:

```text
PermissionTarget discovery explícito.
Receiver registration explícito.
Initial state Blocked/Unbound explícito.
Não misturar PlayerActorId, PlayerSlotId, ReceiverId e PermissionTargetId.
Não mudar reaction local.
```

#### `SA-6C — Movement binding stage`

Objetivo: mover movement binding para stage real.

Critério:

```text
Binding prepara.
Permission/runtime habilita ou bloqueia.
Receiver aplica localmente.
Pipeline não chama controller diretamente para lifecycle fino.
```

#### `SA-6D — Camera binding stage`

Objetivo: mover camera target binding para stage real.

Critério:

```text
Camera consome capability inventory canônico.
Camera não reconstrói inventory.
Activity camera identity correta.
Skip/no-content preservado em activity_02.
```

---

### Fase E — Entry readiness boundary final

#### `SA-7 — EntryReadinessResult e handoff limpo para macro pipeline`

Objetivo: fazer `ActivityEntryPipeline` retornar resultado final de readiness completo para `SessionActivityPipeline`.

Resultados esperados:

```text
ActivityEntryResult.Completed
ActivityEntryResult.SkippedNoContent
ActivityEntryResult.Failed
ActivityEntryResult.RejectedStaleOrForeign
ActivityEntryResult.BlockedByRequiredCapability
```

Critério:

```text
ActivityEntryPipeline decide readiness da entry.
SessionActivityPipeline decide apenas o próximo macro passo: ActivationWindow ou fail/abort.
SessionActivityPipeline não executa setup residual.
ActivityEntryPipeline tem início/fim semanticamente corretos.
ActivityEntryPipelineCompleted só aparece quando a entry realmente terminou.
ActivationWindow continua fora do ActivityEntryPipeline.
```

---

### Fase F — Exit, release e dematerialization

#### `SA-8A — Exit/Release ownership audit`

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

Decisão possível A:

```text
SessionActivityPipeline mantém macro exit lifecycle.
Exit stages executam release/dematerialization.
```

Decisão possível B:

```text
Criar ActivityExitPipeline somente se houver lifecycle determinístico próprio suficientemente grande.
Não criar pipeline por simetria estética.
```

#### `SA-8B — ActivityObjectRelease / SnapshotCapture stage cleanup`

Critério:

```text
Snapshot capture antes de release.
Release command explícito.
Unregister depois do release aplicável.
No-content = skip explícito.
```

#### `SA-8C — Actor release/participation exit cleanup`

Critério:

```text
RouteScoped pode reter por policy.
ActivityScoped libera por ActivityExit.
RouteExit libera o que é route-scoped quando aplicável.
Sem rail player/nonplayer paralelo.
```

---

### Fase G — Host, composition e boundaries

#### `SA-9A — SessionActivityHost boundary cleanup`

Objetivo: reduzir `SessionActivityHost` para boundary/endpoint externo, não lifecycle owner.

Critério:

```text
Host não classifica lifecycle.
Host não executa side-effects de teardown.
Host não vira registry tardio.
QA chama comandos/stages canônicos.
```

#### `SA-9B — Composition / service locator cleanup`

Critério:

```text
Composition root registra dependências.
Pipeline não usa DependencyManager.Provider para lifecycle ativo.
Host não registra lifecycle como fonte de verdade.
```

---

### Fase H — Permission identity final

#### `SA-10 — Permission identity separation`

Objetivo: resolver o débito de identidade sem misturar domínios.

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

Critério:

```text
Nenhum fallback comparando domínios diferentes.
Receiver técnico não vira ActorId.
PlayerSlotId não vira PlayerActorId.
PermissionTarget tem identidade própria.
Logs expõem os domínios separados.
```

---

### Fase I — State/fact hygiene

#### `SA-11A — ActivityEntry state/context extraction`

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

Critério:

```text
State da entry não vaza como global mutável sem owner.
Foreign/stale continua protegido.
Restart cria novo entry context.
```

#### `SA-11B — Fact recorder hygiene`

Critério:

```text
Fact recorder não decide policy.
Fact recorder não executa side-effect.
Logs mantêm owner correto.
Facts não alteram lifecycle.
```

---

### Fase J — Contract/command hygiene

#### `SA-12 — Commands e contracts finais`

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
Commands não carregam Stage, Boundary, Adapter, Func<T>, Action, executor genérico, state mutável compartilhado ou ScriptableObject autoral inteiro quando só é necessário payload resolvido.
Commands carregam payload runtime resolvido e identity tipada.
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

NEXT  SA-4A0  ActivityObjectContributorDiscoveryStage real
      SA-4A1  ActivityObjectEntryStage/API cleanup, se auditoria pós-smoke ainda encontrar wrapper/debt
      SA-4B   Object snapshot/reset/restore cleanup

      SA-5A   Actor discovery/readiness ownership
      SA-5B   ActorPresentation setup stage
      SA-5C   ActorAttributes setup stage
      SA-5D   ActorParticipation enter stage

      SA-6A   PlayerInput binding stage
      SA-6B   Permission target preparation stage
      SA-6C   Movement binding stage
      SA-6D   Camera binding stage

      SA-7    EntryReadinessResult final

      SA-8A   Exit/Release ownership audit
      SA-8B   ObjectRelease/SnapshotCapture cleanup
      SA-8C   Actor release/participation exit cleanup

      SA-9A   Host boundary cleanup
      SA-9B   Composition/service locator cleanup

      SA-10   Permission identity separation

      SA-11A  Entry state/context extraction
      SA-11B  Fact recorder hygiene

      SA-12   Command/contract hygiene
```

## Critério global de viabilidade Base 2.0

A refatoração de `SessionActivity` só pode ser considerada viável para Base 2.0 quando:

```text
SessionActivityPipeline mantém apenas lifecycle macro, transition, restart, route-exit e handoffs.
ActivityEntryPipeline é owner real de entry lifecycle.
ActivityEntryPipeline não é fachada do SessionActivityPipeline.
ActivityContent, Inventory, Object setup, Actor setup, Input, Movement e Camera têm stages/owners explícitos.
Release/Exit têm owner claro, com ou sem ActivityExitPipeline.
Host é boundary/delegador, não owner de lifecycle.
QA chama caminhos canônicos.
Commands não carregam infraestrutura.
Facts não executam side-effects.
Adapters não decidem lifecycle/policy.
Snapshots/índices runtime têm writer único por lifecycle.
Identidades de domínios diferentes não são comparadas como equivalentes.
Sem fallback silencioso.
Sem trilho paralelo novo.
Sem compat desnecessária.
Smoke completo PASS.
```

## Smoke global mínimo

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
ActivityObjectSnapshotCapture PASS quando aplicável
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

## Critérios de aceite arquitetural

Um corte de `SessionActivity` só pode ser aceito como PASS arquitetural quando:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem fallback silencioso
sem trilho paralelo novo
sem owner duplicado para o mesmo lifecycle
Host não decide lifecycle de route-exit
ActivityEntryPipeline é owner real dos steps migrados
SessionActivityPipeline mantém lifecycle macro
stages não viram mini-pipeline
boundaries não chamam sub-stages
commands não carregam infraestrutura
facts não executam side-effects
adapters não decidem lifecycle/policy
identidades de domínios diferentes não são comparadas como equivalentes
logs mostram owner correto do passo executado
logs/facts não antecipam completed de pipeline antes do lifecycle real
snapshots/índices runtime canônicos possuem writer único por lifecycle
consumidores não reconstruem state canônico para corrigir falta local
```

## Critérios de smoke mínimos

Após cada corte funcional, exigir log/smoke manual com pelo menos:

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
ActivityEntry owner visível nos logs quando aplicável
RouteExit teardown com owner único
ActivityEntryPipelineCompleted não aparece antes do fim real do entry lifecycle
ActivityContent/Inventory têm owner visível e sem writers duplicados
ActivityContent/Actor/Input/Movement/Camera sem regressão nos checkpoints existentes
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

## Decisão final proposta

Aceitar este ADR como contrato de decomposição de `SessionActivity` para Base 2.0.

Implementação só deve começar pelo corte de menor risco:

```text
SA-1 — RouteExit teardown owner unification
```

Nenhum corte deve ser aceito como PASS sem smoke/log.
