# ADR-2.0-0002 — SessionActivity Ownership Decomposition e ActivityEntryPipeline

## Status

Aceito / congelado incrementalmente. Último checkpoint: `SA-ACTOR-1C1-H8C3 — PASS funcional + PASS arquitetural do corte`, fechando `ActorScope.SessionScoped` estrutural, `ExitToMenu -> SessionReset` canônico e a limpeza de ownership de `PlayerScope`, `SessionParticipantId`, materialization resolution e `ActorId` do player default.

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


### Corte de normalização aplicado — SA-5A0-H1

Status: **CLOSED / PASS funcional + PASS arquitetural do corte**.

Objetivo:

```text
Normalizar a base lexical/arquitetural antes da auditoria SA-5A para não iniciar ActorDiscovery/ActorReadiness com a leitura torta de rails PlayerActor/NonPlayerActor.
```

Decisões aplicadas:

```text
- `NonPlayerActorDiscoveryStage` foi renomeado para `ActorSceneDiscoveryStage`.
- O método de emissão passou de `EmitNonPlayerActorDiscoveryStage` para `EmitActorSceneDiscoveryStage`.
- Os facts/stages de discovery de cena passaram de `NonPlayerActorDiscovery*` para `ActorSceneDiscovery*`, preservando os valores numéricos dos enums.
- `NonPlayerActor` permanece como componente/fonte concreta scene-authored, mas não como nome do stage/corte/owner arquitetural.
- `PlayerActorReadinessStage` foi renomeado para `ActivityParticipantReadinessStage`.
- Os facts/stages de readiness passaram de `PlayerActorReadiness*` para `ActivityParticipantReadiness*`, preservando os valores numéricos dos enums.
- Entries antigas e não usadas `NonPlayerActorPresentation*` foram removidas dos contratos para não sugerir rail paralelo de presentation.
- `NonPlayerActorDiscoveryRecord` não usado foi removido dos contratos concretos.
```

Fronteira preservada:

```text
- Não move ActorPresentation.
- Não move ActorAttributes.
- Não move ActorParticipation.
- Não altera Camera, Permission, Movement, Reset, Release, Deactivation ou RouteExit.
- Não altera a semântica de activity_01/activity_02.
- Não cria stage final para PlayerActor ou NonPlayerActor.
```

Critério de aceite:

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

Smoke manual validado após aplicação do pacote.

Evidência aceita:

```text
sem erros CS observáveis pelo smoke no Editor
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
activity_01 preserva cenário com conteúdo/contributors
activity_02 preserva cenário negativo/no-content com skip explícito e PassedNoCommands
```

Nota de observabilidade:

```text
O smoke não emite facts literais chamados ActorSceneDiscovery ou ActivityParticipantReadiness.
Neste corte, o aceite arquitetural é restrito à normalização lexical/contratual confirmada por compile/smoke e pela ausência dos nomes antigos de stage no log.
Adicionar facts explícitos para ActorSceneDiscovery/ActivityParticipantReadiness pode ser tratado como hygiene futura, sem bloquear este PASS.
```


Após esse smoke, a auditoria `SA-5A — ActorDiscovery / ActorReadiness ownership audit` pode começar sobre uma base menos contaminada por nomes de rails concretos.

---

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

Objetivo: mover `ActorPresentation` setup para stage real de entry, sem transformar `ActivityEntryPipeline.cs` em novo monólito.

Formulação correta:

```text
ActivityEntryPipeline ordena/chama o stage.
ActivityEntryActorPresentationStage executa o setup determinístico.
Policies classificam retain/materialize/skip/fail.
Adapters/endpoints executam side-effects.
SessionActivityPipeline perde o bloco concreto de presentation setup.
```

Critério:

```text
Retention policy explícita.
Materialization em adapter.
Stage não decide next activity.
Presentation obrigatória ausente falha explicitamente.
Sem fallback silencioso.
ActivityEntryPipeline não recebe loop grande de Presentation.
SessionActivityPipeline perde mais lógica concreta do que ganha.
Release ActivityExit/RouteExit fica fora deste corte.
```

#### `SA-5C — ActorAttributes setup stage`

Objetivo: mover setup de attributes para stage real de entry.

Critério:

```text
Attributes são capability local.
Pipeline/stage prepara/descobre.
Reação local não vira command global quando for ação local.
```


##### Status pós-smoke — SA-5C

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

Evidência validada no smoke:

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

Conclusão arquitetural:

```text
ActorAttributes setup saiu do caminho concreto do SessionActivityPipeline.
ActivityEntryPipeline ficou como owner de ordem/lifecycle da entry.
ActivityEntryActorAttributeStage executa o setup determinístico de attributes.
SessionActivityPipeline ainda mantém lifecycle macro e ainda possui débitos posteriores em ActorParticipation/Input/Movement/Camera.
IActivityEntryActorAttributeRuntimeBridge permanece transitória e não pode crescer como manager/coordinator.
```

Débitos remanescentes não bloqueantes deste corte:

```text
ActorParticipationEnter ainda executa no SessionActivityPipeline.
PlayerInput, Movement e Camera ainda serão avaliados em cortes próprios.
ActorAttribute release ainda fica fora do escopo do SA-5C e será tratado em exit/release decomposition.
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

##### Checkpoint SA-12-AUDIT — Commands/contracts hygiene

Status: `AUDITED / NEEDS SMALL COMMAND HYGIENE PATCH`.

A auditoria estática de `SA-12` confirmou que não havia blocker de executor/delegate nos commands auditados:

```text
sem Action
sem Func<T>
sem adapters embutidos nos commands
sem delegates de execução
sem SessionActivityRuntimeState embutido nos commands
```

O débito restante foi classificado como higiene de contrato:

```text
commands carregando authoring asset inteiro;
wrappers internos carregando Stage/Boundary;
commands duplicando PipelineId/SessionStateId/ActivityId/ActivityOrdinal/EntrySequence quando SessionActivityIdentity já era a fonte do ciclo;
ActorAttributeCommand ainda usando strings livres para identidades runtime.
```

Conclusão:

```text
SA-12 não exige pipeline novo.
SA-12 não exige redesenhar lifecycle macro.
SA-12 deve ser resolvido por cortes pequenos de command hygiene.
```

##### Checkpoint SA-12B/C — Command boundary + identity duplication cleanup

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

Escopo fechado:

```text
ActivityObjectContributorUnregisterStageCommand
ActivityObjectResetCommand
ActivityObjectReleaseCommand
ActivityObjectSnapshotRestoreCommand
ActivityContentSceneUnloadCommand
```

Correções aplicadas:

```text
ActivityObjectContributorUnregisterStageCommand não carrega mais SessionActivityStage Stage.
ActivityObjectContributorUnregisterStage constrói suas identities locais internamente.
Não há fallback do wrapper para ActivityContentReleaseCompleted.
ActivityObjectResetCommand não duplica PipelineId/SessionStateId/ActivityId/ActivityOrdinal/EntrySequence.
ActivityObjectReleaseCommand não duplica PipelineId/SessionStateId/ActivityId/ActivityOrdinal/EntrySequence.
ActivityObjectSnapshotRestoreCommand não duplica PipelineId/SessionStateId/ActivityId/ActivityOrdinal/EntrySequence.
ActivityContentSceneUnloadCommand não duplica PipelineId/SessionStateId/ActivityId/ActivityOrdinal/EntrySequence.
Consumers passaram a usar command.Identity como fonte única do ciclo.
```

##### Checkpoint SA-12D — ActorAttributeCommand typed identity

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

Escopo fechado:

```text
ActorAttributeCommand
produtores de ActorAttributeCommand
consumidores de ActorAttributeCommand
logs/facts de setup/release de ActorAttribute
```

Correções aplicadas:

```text
ActorAttributeCommand não carrega mais string PipelineIdentity.
ActorAttributeCommand não carrega mais string ActivityIdentity.
ActorAttributeCommand não carrega mais string ActorInstanceId.
ActorAttributeCommand carrega SessionActivityIdentity como identidade tipada do ciclo.
ActorAttributeCommand carrega ActorInstanceRuntimeId como identidade funcional runtime do actor.
Call sites foram migrados para o shape tipado.
Logs podem imprimir ToString()/Value apenas como observabilidade, não como lookup funcional.
```

##### Checkpoint SA-12E — ActivityContent runtime scene reference

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

Correções aplicadas:

```text
ActivityEntryContentLoadCommand não carrega mais SessionActivityDefinition.
ActivityEntryContentLoadCommand passou a carregar ActivityContentLoadPlan como payload runtime resolvido.
ActivityContentLoadPlan contém Identity, ActivityId, ActivityOrdinal, ActivityContentMode, ActivityContentProfileId, Scenes, Source e Reason.
ActivityContentLoadPlanScene carrega runtime scene reference mínima para load.
ActivityContentLoadedSceneRecord não carrega mais SceneKeyAsset autoral como fonte de unload.
ActivityContentSceneUnloadDispatchStage passou a operar por ActivityContentSceneRuntimeReference.
activity_01 preserva content load com loadedScenes='1'.
activity_02 preserva no-content/skip explícito sem fallback para Route Scene.
```

Observação de escopo:

```text
O corte excedeu o mínimo inicialmente previsto para SA-12F2 porque também removeu SceneKeyAsset de ActivityContentLoadedSceneRecord e ajustou unload/object setup para runtime reference.
A expansão foi aceita porque permaneceu dentro da mesma fronteira arquitetural: ActivityContent runtime payload.
```

##### Checkpoint SA-12F — Reduce SessionActivityDefinition from ActivityEntry commands

Status: `PARTIAL / IN PROGRESS`.

Subcortes validados até este checkpoint:

```text
SA-12F1A/B — CLOSED / PASS funcional + PASS arquitetural do corte
SA-12F2    — CLOSED / PASS funcional + PASS arquitetural do corte
SA-12F3A   — CLOSED / PASS funcional + PASS arquitetural do corte
SA-12F3B   — CLOSED / PASS funcional + PASS arquitetural do corte
SA-12F3C   — CLOSED / PASS funcional + PASS arquitetural do command boundary
SA-12F4A   — CLOSED / PASS funcional + PASS arquitetural do corte
SA-12F4B   — CLOSED / PASS funcional + PASS arquitetural do corte
SA-12F4C   — CLOSED / PASS funcional + PASS arquitetural do corte
```

Escopo fechado:

```text
ActivityEntryContentLoadCommand foi reduzido para ActivityContentLoadPlan.
PlayerInput/Permission/Movement/Camera binding commands deixaram de carregar SessionActivityDefinition quando já possuíam payload resolvido.
ActorPresentation/ActorAttribute setup commands deixaram de carregar SessionActivityDefinition.
ActivityEntryParticipantBindingCommand passou a carregar ActivityParticipantBindingPlan.
ActivityEntryObjectSetupCommand deixou de carregar SessionActivityDefinition após separação de ActivityObjectSetupInventoryPlan e ActivityObjectResetRestorePlan.
ActivitySetupInventoryBuilder passou a consumir ActivityObjectSetupInventoryPlan.
Reset/restore do object setup passaram a consumir ActivityObjectResetRestorePlan.
```

Notas de arquitetura:

```text
ActivityParticipantBindingPlan ficou intencionalmente estreito e fecha o command boundary, mas não representa decomposição completa de participant requirements/materialization/placement.
ActivityObjectSetupInventoryPlan é payload de setup inventory.
ActivityObjectResetRestorePlan é payload de reset/snapshot restore.
ActivityEntryObjectSetupCommand não usa mais SessionActivityDefinition como carrier runtime.
```

Evidência funcional aceita para os subcortes:

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

##### Checkpoint SA-12F-BLOCKER-MOVEMENT-ACTIVITY02 — retained PlayerActor movement in no-content activity

Status: `CLOSED / PASS funcional + PASS arquitetural parcial`.

Problema fechado:

```text
Ao transicionar de activity_01 para activity_02, o PlayerActor SessionScoped permanecia visível/materializado, mas movement não funcionava.
activity_02 é no-content, mas ActivityContent ausente não implica PlayerActor ausente nem perda automática de movement/control.
```

Causas confirmadas durante os cortes:

```text
ActivityParticipationContext de activity_02 era gravado vazio quando não havia participant requirements próprios.
O retained player binding existia no ActivityActorExitRuntimeState, mas era rejeitado por validação de scope incorreta para ActorScope.SessionScoped.
O capability inventory de activity_02 projetava apenas PresentationEndpoint e não projetava PermissionTarget/movement receiver do PlayerActor retido.
```

Correções aceitas:

```text
ActivityEntryParticipantBindingStage passou a promover retained player binding para ActivityParticipationContext current-entry quando a activity não possui participant requirements próprios, mas há PlayerActor SessionScoped válido.
A validação passou a aceitar ActorInstanceRuntimeId SessionScoped atravessando activities sem rebadgear o runtime id para activity scope.
ActivityEntryPipeline passou a ter ActivityParticipationContext com activityParticipants='1' em activity_02.
PlayerInputBinding passou a bindar o player em activity_02.
MovementBinding passou a encontrar target em activity_02.
ActivityCapabilityInventory passou a receber a capability surface funcional do PlayerActor SessionScoped retido antes da PermissionTargetPreparation.
PermissionTargetPreparation passou a registrar receiver para activity_02.
ActivityGameplayControl Allowed passou a ser aplicado ao PlayerMovementPermissionReceiver de activity_02.
MovementControlEnabled voltou a ocorrer em activity_02.
```

Evidência aceita:

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

Débito aceito:

```text
SA-12F-MOV-H1 — Retained PlayerActor target projection ownership hygiene.
Status: OPEN / MEDIUM DEBT.

Parte da projeção de ActorTargets para capability inventory ficou em SessionActivityPipeline como bridge técnica:
- ResolvePlayerActorCapabilityTargetsForCurrentEntry(...)
- AddPlayerActorCapabilityTargetsFromParticipationContext(...)
- TryResolvePlayerActorHandleForCapabilityInventory(...)

A auditoria classificou o shape como PASS funcional / PASS arquitetural parcial porque não há writer duplicado de inventory, fallback por string, first-player fallback, branch player/nonplayer novo ou lifecycle/policy sendo decidido fora do owner.
Mesmo assim, o owner conceitual final da projeção deve ser ActivityEntryPipeline / ActivityEntryActorInventoryStage / helper específico de entry.
```

Critério futuro para fechar o débito:

```text
Mover a projeção de PlayerActor SessionScoped retido para helper/bridge do ActivityEntryPipeline ou ActivityEntryActorInventoryStage.
Preservar o mesmo smoke funcional de activity_02.
Não reconstruir inventory em consumidor posterior.
Não criar fallback por string, first actor, first player ou registry tardio.
Manter ActivityCapabilityInventory como snapshot/índice runtime passivo com writer único por lifecycle.
```

##### Pendências restantes de SA-12

```text
SA-12F5 — auditoria/correção final dos resíduos de SessionActivityDefinition em ActivityEntryCommand, content-load completion/failure, ActorParticipationEnterCommand e ActivityContentReleaseFinalizationStageCommand.
SA-12F-MOV-H1 — hygiene futuro: mover retained PlayerActor target projection bridge para ActivityEntryPipeline / ActivityEntryActorInventoryStage.
```

Critério para os próximos cortes:

```text
Não reabrir SA-12E salvo regressão explícita.
Não reabrir o blocker funcional de movement em activity_02 salvo regressão de smoke.
Resolver SA-12F5 por cortes pequenos de residual command hygiene.
Tratar SA-12F-MOV-H1 como hygiene futuro, não blocker funcional.
Não criar compat/fallback paralelo.
Não criar pipeline novo.
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
      SA-4A1  ActivityObjectEntryStage/API cleanup, se auditoria pós-smoke ainda encontrar wrapper/debt
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

PEND  SA-12   Command/contract hygiene — partial
DONE  SA-12B/C Command boundary + identity duplication cleanup
DONE  SA-12D  ActorAttributeCommand typed identity
DONE  SA-12E  ActivityContent SceneKeyAsset/runtime scene reference
PART  SA-12F  Reduce SessionActivityDefinition from ActivityEntry*Command
DONE  SA-12F-BLOCKER-MOVEMENT-ACTIVITY02 — PASS funcional / PASS arquitetural parcial
PEND  SA-12F5 residual SessionActivityDefinition command hygiene
DEBT  SA-12F-MOV-H1 Retained PlayerActor target projection ownership hygiene
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


---

## Corte aplicado — SA-5A1 ActorInventoryFeed / ActorScanTarget ownership normalization

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

### Objetivo

Mover o ownership efetivo de `ActorSceneDiscovery`, `ActorInventoryFeed` e `ActorScanTarget` para o escopo de `ActivityEntryPipeline`, sem migrar ainda `ActorPresentation`, `ActorAttributes`, `ActorParticipation`, Input, Movement, Camera, Permission, Release, Deactivation ou RouteExit.

### Alterações aplicadas

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
PlayerActor e NonPlayerActor continuam apenas como fontes concretas para o feed genérico de Actor.
ActivityEntryPipeline é o owner do feed/targets da entry.
SessionActivityPipeline permanece owner do lifecycle macro.
ActivityNonPlayerActorRegistry e ActivityPlayerActorRegistry continuam índices técnicos, não owners de lifecycle.
ActorPresentation, ActorAttributes e ActorParticipation ainda não foram movidos neste corte.
```

### Débito aceito do corte

```text
IActivityEntryActorInventoryRuntimeBridge ainda é bridge transitória para expor registries e targets já existentes ao ActivityEntryPipeline.
Esse bridge não pode virar owner permanente nem crescer para lifecycle/policy.
O próximo corte deve continuar reduzindo o SessionActivityPipeline sem criar ActorManager/Coordinator.
```

### Critério de aceite

```text
compilar sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
ActivityEntryActorSceneDiscoveryStarted/Completed visível com owner ActivityEntryPipeline
ActivityEntryActorInventoryFeedStarted/Completed visível com owner ActivityEntryPipeline
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

### Smoke / evidência aceita

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
activity_02 negativa/no-content preservada com skip explícito e PassedNoCommands
```

Decisão: `SA-5A1` está fechado como PASS do corte. O débito restante é mover os consumidores de Actor setup (`ActorPresentation`, `ActorAttributes`, `ActorParticipation`) para stages reais, sem reabrir Feed/ScanTarget.

---

## Corte documental — SA-5B0 ActorPresentation Ownership Contradiction Cleanup / Extraction Audit

Status: CLOSED / AUDIT + DOCUMENTATION ONLY.

### Objetivo

Limpar a contradição antes do `SA-5B`: mover `ActorPresentation` para o owner correto não significa adicionar lógica concreta em `ActivityEntryPipeline.cs`.

Decisão normativa:

```text
ActivityEntryPipeline é owner de ordem/lifecycle da ActivityEntry.
ActivityEntryActorPresentationStage deve ser o executor determinístico do setup.
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

### Decisão

`SA-5B` só fica autorizado se for extração real, não redistribuição de monólito.

Permitido:

```text
Criar ActivityEntryActorPresentationStage ou evoluir ActorPresentationSetupStage para stage real.
Mover setup from-inventory para stage dedicado.
Mover helpers de resolve references/retention/store state necessários ao setup.
Manter facts/logs equivalentes.
Preservar ActorPresentationSetupCompleted/Materialized/Retained/Ready.
```

Proibido:

```text
Não colocar loop/materialization/retention diretamente em ActivityEntryPipeline.cs.
Não adicionar lógica concreta nova ao SessionActivityPipeline.
Não mover ActorAttributes.
Não mover ActorParticipationEnter.
Não mover release/deactivation/route-exit.
Não mexer em PlayerInput, Movement, Camera ou Permission.
Não criar ActorManager/ActorCoordinator.
Não criar fallback para caminho antigo.
Não criar branch global Player/NonPlayer.
```

### Critério de aceite para SA-5B

```text
SessionActivityPipeline deve perder mais lógica concreta do que ganhar.
ActivityEntryPipeline deve continuar pequeno: ordem, lifecycle e chamada de stage.
ActivityEntryActorPresentationStage executa setup determinístico.
Caminho antigo de setup no SessionActivityPipeline sai ou fica inacessível.
Sem fallback silencioso.
Smoke preservado.
```

### Artefato

Relatório detalhado criado em:

```text
NewScripts/Docs/Reports/SA-5B0-ActorPresentation-Ownership-Audit.md
```

---

## SA-5B — ActorPresentation setup stage extraction

**Status:** `CLOSED / PASS funcional + PASS arquitetural do corte`  
**Data:** 2026-05-31

### Decisão aplicada

`ActorPresentation` setup deixou de ser executado diretamente pelo `SessionActivityPipeline` e passou a ser executado por stage dedicado da entry:

```text
ActivityEntryPipeline.ExecuteActorPresentationSetup
-> ActivityEntryActorPresentationStage.Execute
```

O `ActivityEntryPipeline` permanece como owner de ordem/lifecycle da entry, mas não recebeu o loop concreto de presentation. A execução determinística foi extraída para `ActivityEntryActorPresentationStage`.

### Mudança de ownership

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
-> store/sync via bridge transitória
```

### Bridge transitória

Foi criada `IActivityEntryActorPresentationRuntimeBridge` para expor ao stage o mínimo necessário enquanto o estado de presentation ainda não saiu totalmente do `SessionActivityPipeline`:

```text
CurrentActivityCapabilityInventoryPreview
TryGetActiveActorPresentationHandle
StoreActiveActorPresentationHandle
SyncActiveActorPresentationHandle
ReleaseActorPresentationBeforeRematerialization
```

Essa bridge é transitória. Ela não deve virar manager/coordinator e não deve crescer para Attributes, Participation, Movement ou Camera.

### Evidência de PASS

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

## SA-5C — ActorAttributes setup stage extraction

**Status:** `CLOSED / PASS funcional + PASS arquitetural do corte`  
**Data:** 2026-05-31

### Decisão aplicada

`ActorAttributes` setup deixa de ser executado diretamente pelo `SessionActivityPipeline` e passa a ser executado por stage dedicado da entry:

```text
ActivityEntryPipeline.ExecuteActorAttributeSetup
-> ActivityEntryActorAttributeStage.Execute
```

O `ActivityEntryPipeline` permanece owner de ordem/lifecycle da entry. Ele apenas chama o stage e valida o resultado. O loop concreto de attributes fica em `ActivityEntryActorAttributeStage`.

### Mudança de ownership

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
-> store active attribute capability via bridge transitória
```

### Bridge transitória

Foi criada `IActivityEntryActorAttributeRuntimeBridge` para expor ao stage o mínimo necessário enquanto o state de actor attributes ainda não saiu totalmente do `SessionActivityPipeline`:

```text
CurrentActivityCapabilityInventoryPreview
StoreActiveActorAttributeCapability
RemoveActiveActorAttributeCapability
```

Essa bridge é transitória. Ela não deve virar manager/coordinator e não deve crescer para Participation, Movement ou Camera.

### Evidência de PASS

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

## SA-5D — ActorParticipation enter stage extraction

**Status:** `CLOSED / PASS funcional + PASS arquitetural do corte`  
**Data:** 2026-05-31

### Decisão aplicada

`ActorParticipationEnter` deixa de ser executado diretamente pelo `SessionActivityPipeline` e passa a ser executado por stage dedicado da entry:

```text
ActivityEntryPipeline.ExecuteActorParticipationEnter
-> ActivityEntryActorParticipationStage.ExecuteEnter
```

O `ActivityEntryPipeline` permanece owner de ordem/lifecycle da entry. Ele apenas chama o stage e valida o resultado. A execução concreta de participation enter fica em `ActivityEntryActorParticipationStage`.

### Mudança de ownership

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
-> aplica readiness policy via bridge transitória
-> classifica entered/skipped/failed
-> registra active participations via bridge transitória
-> emite ActorReady
```

### Bridge transitória

Foi criada `IActivityEntryActorParticipationRuntimeBridge` para expor ao stage o mínimo necessário enquanto o state de participation/readiness ainda não saiu totalmente do `SessionActivityPipeline`:

```text
EvaluateActorParticipationReadiness
StoreActiveActorParticipation
```

Essa bridge é transitória. Ela não deve virar manager/coordinator e não deve crescer para PlayerInput, Movement ou Camera.

### Evidência de PASS

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
ActorParticipationEnter deixou de ser execução concreta do SessionActivityPipeline.
ActivityEntryPipeline manteve ownership de ordem/lifecycle da entry.
ActivityEntryActorParticipationStage passou a executar enter/ready determinístico.
SessionActivityPipeline preserva lifecycle macro e ainda mantém exits/releases para cortes futuros.
```

Débitos restantes controlados:

```text
IActivityEntryActorParticipationRuntimeBridge ainda é transitória.
ActorParticipationExit ainda pertence ao fluxo de Exit/Release.
PlayerInput, Movement e Camera ainda serão cortes próprios da entry.
ActorPresentation/ActorAttribute release ainda pertence ao bloco futuro de Exit/Release decomposition.
```

---

## SA-6A — PlayerInput binding stage extraction

**Status:** `CLOSED / PASS funcional + PASS arquitetural do corte`  
**Data:** 2026-05-31

### Decisão aplicada

`PlayerInputBinding` deixa de ser executado diretamente pelo `SessionActivityPipeline` e passa a ser executado por stage dedicado da entry:

```text
ActivityEntryPipeline.ExecutePlayerInputBinding
-> ActivityEntryPlayerInputBindingStage.Execute
```

O `ActivityEntryPipeline` permanece owner de ordem/lifecycle da entry. Ele apenas chama o stage e valida o resultado. A execução concreta do binding de `PlayerInput` fica em `ActivityEntryPlayerInputBindingStage`.

### Mudança de ownership

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
-> passa referências passivas dos participant bindings da entry
-> chama ActivityEntryPipeline.ExecutePlayerInputBinding

ActivityEntryPipeline
-> chama ActivityEntryPlayerInputBindingStage

ActivityEntryPlayerInputBindingStage
-> monta requisitos de PlayerInput
-> chama PlayerInputBindingAdapter
-> classifica skip/fail/completed
-> emite PlayerInputBindingCommandIssued/PlayerInputBound/PlayerInputBindingCompleted
```

### Regra anti-monólito preservada

O corte não move lógica concreta para dentro do `ActivityEntryPipeline.cs`. O pipeline de entry só ordena e valida resultado; o trabalho concreto fica no stage dedicado.

O antigo `PlayerInputBindingStage` foi esvaziado como trilho ativo. O caminho canônico passa a ser `ActivityEntryPlayerInputBindingStage`.

### Escopo preservado

```text
Movement não foi alterado.
Camera não foi alterada.
Permission não foi alterada.
Release/Deactivation/RouteExit não foram alterados.
OperationalInputRuntime não foi alterado.
```

### Critério de smoke validado

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

### Evidência do smoke

O smoke manual após o compile hotfix confirmou o novo owner do binding de input na entry:

```text
ActivityEntryPlayerInputBindingStarted owner='ActivityEntryPipeline'
ActivityEntryPlayerInputBindingCompleted owner='ActivityEntryPipeline'
```

Também confirmou que o rebinding canônico continuou ativo:

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

Observação: `PlayerInputBindingCommandIssued` e `PlayerInputBound` permanecem emitidos como `SessionActivityFactKind` pelo stage, mas não aparecem como linhas `OBS` individuais no log manual. Como `ActivityEntryPlayerInputBindingCompleted`, `PlayerInputActionsReboundToCanonical`, `MovementBindingCompleted` e `CameraBindingCompleted` validam o caminho ativo, isso foi classificado como observabilidade interna não bloqueante para este corte. Se a exigência futura for log `OBS` explícito para esses facts, tratar como hygiene local de observabilidade, não como regressão funcional do SA-6A.

### Leitura arquitetural

```text
PlayerInputBinding deixou de ser execução concreta do SessionActivityPipeline.
ActivityEntryPipeline manteve ownership de ordem/lifecycle da entry.
ActivityEntryPlayerInputBindingStage executa o trabalho concreto de binding.
Movement, Camera, Permission, Release, Deactivation e RouteExit permaneceram fora do corte.
```

Débitos restantes controlados:

```text
Movement binding ainda está no SessionActivityPipeline.
Camera binding ainda está no SessionActivityPipeline.
Permission target preparation ainda será corte próprio.
ActivityEntryPlayerInputBindingStage ainda usa bridge/endpoint de entry já existente para emitir facts/snapshots.
```


---

## Checkpoint — SA-6B Permission target preparation stage / CLOSED / PASS

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

### Objetivo

Extrair a preparação de `PermissionTarget` para stage real da `ActivityEntry`, sem redesenhar toda identity de permission e sem alterar a reação local dos receivers.

### Mudança de ownership

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
-> continua responsável apenas pelo MovementBinding até SA-6C
```

### Regra anti-monólito preservada

`ActivityEntryPipeline.cs` só ordena e valida resultado. A execução concreta fica em `ActivityEntryPermissionTargetPreparationStage`.

### Escopo preservado

```text
Movement binding não foi movido.
Camera binding não foi movido.
Permission reaction local não foi alterada.
ActivityCapabilityPermissionRuntime não foi redesenhado.
Release/Deactivation/RouteExit não foram alterados.
```

### Critério de smoke esperado

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

### Débito controlado

`MovementBinding` ainda está no `SessionActivityPipeline` e será tratado no `SA-6C`. O `SA-6B` apenas separa a preparação dos permission targets para que `MovementBinding` não continue sendo o owner indireto de receiver discovery/registration.


---

## Checkpoint — SA-6C Movement binding stage / CLOSED / PASS

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

### Objetivo

Extrair o binding concreto de Movement para um stage real da `ActivityEntry`, sem alterar `PermissionRuntime`, reaction local, Camera, Release, Deactivation ou RouteExit.

### Mudança de ownership

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
      -> grava movement control targets via bridge mínima

SessionActivityPipeline
-> continua apenas lifecycle macro e chama CameraBinding após o resultado da entry
```

### Correções de compile aplicadas antes do smoke

O primeiro pacote `SA-6C` exigiu dois hotfixes de compilação antes do smoke:

```text
SA-6C-compilefix-movement-binding-stage
- restaurou SessionActivityPipeline.cs completo com TryGetSnapshotPayloadForSaveOnExit preservado.

SA-6C-compilefix2-movement-binding-references
- restaurou BuildMovementBindingReferences(...) como helper passivo para montar ActivityEntryMovementBindingReference.
```

Esses hotfixes não reintroduziram execução concreta de Movement no `SessionActivityPipeline`.

### Evidência do smoke

O smoke manual confirmou o novo owner do binding de Movement na entry:

```text
ActivityEntryMovementBindingStarted owner='ActivityEntryPipeline'
MovementBindingStarted owner='ActivityEntryPipeline'
PlayerMovementBound owner='ActivityEntryPipeline'
MovementBindingCompleted owner='ActivityEntryPipeline'
ActivityEntryMovementBindingCompleted owner='ActivityEntryPipeline'
```

Na `activity_02`, que é cenário negativo/no-content, o binding preservou retenção explícita:

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
MovementBinding deixou de ser execução concreta do SessionActivityPipeline.
ActivityEntryPipeline manteve ownership de ordem/lifecycle da entry.
ActivityEntryMovementBindingStage executa o trabalho concreto de binding.
PermissionRuntime continua command/fact/snapshot e não decide lifecycle.
PlayerMovementPermissionReceiver continua reaction local.
Camera, Release, Deactivation e RouteExit permaneceram fora do corte.
```

### Regra anti-monólito preservada

`ActivityEntryPipeline.cs` só ordena e valida resultado. A execução concreta fica em `ActivityEntryMovementBindingStage`. O stage legado `PlayerMovementBindingStage` foi esvaziado para não manter trilho paralelo ativo.

### Escopo preservado

```text
PermissionRuntime não foi redesenhado.
Permission target preparation permanece no SA-6B.
Camera binding não foi movido.
MovementControl enable/disable não foi movido.
Release/Deactivation/RouteExit não foram alterados.
```

### Débito controlado

`IActivityEntryMovementBindingRuntimeBridge` é transitória e expõe apenas registry, adapter e targets de movement control enquanto o state de MovementControl ainda está no `SessionActivityPipeline`. Camera binding permanece como próximo corte.

## SA-6D — Camera binding stage — CLOSED / PASS funcional + PASS arquitetural do corte

### Objetivo

Extrair o binding concreto de Camera para um stage real da `ActivityEntry`, sem alterar `CameraPresentation` operacional, ActivityCamera preparation/release, Release, Deactivation ou RouteExit.

### Mudança de ownership validada

Antes:

```text
SessionActivityPipeline.EmitCameraBindingStage
-> lê ActivitySetupInventory
-> lê ActivityCapabilityInventory
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

### Evidência funcional do smoke

O smoke manual pós-compilefix confirmou:

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

Na `activity_02` negativa/no-content, o skip explícito foi preservado:

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
CameraBinding deixou de ser execução concreta do SessionActivityPipeline.
ActivityEntryPipeline manteve ownership de ordem/lifecycle da entry.
ActivityEntryCameraBindingStage executa o trabalho concreto de binding.
CameraPresentation operacional continua no owner atual.
ActivityCamera preparation/release não foi movido.
Release, Deactivation e RouteExit permaneceram fora do corte.
```

### Regra anti-monólito preservada

`ActivityEntryPipeline.cs` só ordena e valida resultado. A execução concreta fica em `ActivityEntryCameraBindingStage`. Não foi criado trilho paralelo ativo para camera binding.

### Débito controlado

`IActivityEntryCameraBindingRuntimeBridge` permanece transitória e expõe apenas inventory, participantes, resolução de handle e `IActivityCameraPreparationExecutor` enquanto `CameraPresentation` e o state de ActivityCamera continuam nos owners atuais.

## SA-7A — Exit / Release Decomposition Audit — CLOSED / AUDIT ONLY

### Contexto

Após os cortes SA-5A1..SA-6D, a entrada da Activity já possui stages explícitos para ActorInventoryFeed, ActorPresentation, ActorAttributes, ActorParticipation, PlayerInput, PermissionTargetPreparation, Movement e Camera.

A saída ainda concentra release, teardown, snapshot e unload dentro do `SessionActivityPipeline`.

### Decisão

```text
Não criar ActivityExitPipeline agora.
Manter SessionActivityPipeline como owner macro de saída por enquanto.
Extrair primeiro stages determinísticos de exit/release chamados pelo SessionActivityPipeline.
```

### Razão

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

Criar `ActivityExitPipeline` agora produziria owner duplicado de lifecycle. A extração deve começar por stages sem alterar ordering.

### Resultado da auditoria

O próximo corte autorizado é:

```text
SA-7B — ActivityExitActorTeardownStage
```

Objetivo:

```text
Extrair o teardown de actors para stage dedicado:
- ActorPresentation release/retain por rail;
- ActorAttribute release;
- ActorParticipation exit;
- player participation exit quando aplicável.
```

### Escopo proibido no SA-7B

```text
Não criar ActivityExitPipeline.
Não mover DeactivationWindow.
Não mover ActivityContentRelease async.
Não mover ActivityObject snapshot/release.
Não mover CompleteRouteExitClosure.
Não alterar RouteActivitySave.
Não alterar CameraPresentation operacional release.
Não alterar PermissionRuntime/reaction local.
Não criar branch global player/nonplayer.
```

### Critério de aceite do SA-7B

```text
SessionActivityPipeline decide quando o teardown roda.
ActivityExitActorTeardownStage executa o teardown determinístico.
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

### Relatório

Relatório detalhado:

```text
NewScripts/Docs/Reports/SA-7A-Exit-Release-Decomposition-Audit.md
```

## SA-7B — ActivityExitActorTeardownStage — CLOSED / PASS funcional + PASS arquitetural do corte

### Objetivo

Extrair o teardown concreto de actors da saída da Activity para um stage dedicado, sem criar `ActivityExitPipeline` e sem alterar `DeactivationWindow`, `ActivityContentRelease`, `ActivityObject snapshot/release`, `RouteActivitySave`, `CameraPresentation` operacional ou `RouteExit` macro.

### Mudança de ownership aplicada

Antes:

```text
SessionActivityPipeline
-> EmitActorPresentationReleaseGenericStage
-> EmitActorAttributeReleaseFromInventoryStage
-> EmitActorParticipationExitFromInventoryStage
-> PlayerActorParticipationExit quando aplicável
```

Depois:

```text
SessionActivityPipeline
-> decide quando o teardown roda conforme rail atual
-> ActivityExitActorTeardownStage
   -> ActorPresentation release/retain por rail
   -> ActorAttribute release
   -> ActorParticipation exit
   -> PlayerActorParticipation exit quando aplicável
```

### Regra preservada

```text
SessionActivityPipeline continua owner do lifecycle macro de saída.
ActivityExitActorTeardownStage executa apenas o teardown determinístico de actors.
Não foi criado ActivityExitPipeline.
```

### Escopo preservado

```text
DeactivationWindow não foi movida.
ActivityObjectSnapshotCapture não foi movido.
ActivityObjectRelease não foi movido.
ActivityContent scene unload async não foi movido.
CompleteRouteExitClosure não foi movido.
RouteActivitySave não foi alterado.
CameraPresentation operacional release não foi alterado.
PermissionRuntime/reaction local não foi alterado.
```

### Bridge transitória

`IActivityExitActorTeardownRuntimeBridge` foi criada como ponte mínima para o stage acessar state runtime ainda preso no `SessionActivityPipeline`:

```text
active ActorPresentation handles
active ActorAttribute capabilities
active ActorParticipation records
player participant binding resolution
player actor participation adapter/registry
```

Essa bridge é transitória e não deve virar manager/coordinator.

### Critério de smoke

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
ActorPresentationReleaseStarted/Released/Skipped/Completed preservado
ActorAttributeReleaseStarted/Released/Completed preservado
ActorParticipationExitStarted/Exited/Completed preservado
PlayerActorParticipationExit preservado quando aplicável
ActivityObjectSnapshotCapture PASS
ActivityObjectRelease PASS
ActivityObjectContributorUnregister PASS
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```

### Smoke / evidência

Smoke manual validado após compile fix de `ActorParticipationExitCommand`.

O log confirmou:

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'

ActivityExitActorTeardownStage como owner visível do teardown de actors
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

Evidência funcional relevante:

```text
ActivityExitActorTeardownStage executa ActorPresentation release no rail ActivityExit e RouteExit.
ActivityExitActorTeardownStage executa ActorAttribute release sem failures.
ActivityExitActorTeardownStage executa ActorParticipation exit sem failures.
ActivityObject snapshot/release/unregister continuam no caminho existente e passam.
ActivityContent scene unload async continua preservado fora do corte.
RouteExitBackToMenu aplica Menu route após ClosedForRouteExit.
```

### Status

`SA-7B` está `CLOSED / PASS funcional + PASS arquitetural do corte`.

### Débito controlado

```text
IActivityExitActorTeardownRuntimeBridge permanece transitória.
ActivityObject snapshot/release ainda pertence a corte futuro.
ActivityContent unload async ainda pertence a corte futuro.
DeactivationWindow e RouteExit macro continuam ownership do SessionActivityPipeline.
```



## SA-7C — ActivityObject Snapshot / Release / Unregister / Content Release Audit

Status: `CLOSED / AUDIT ONLY`

### Decisão

O SA-7C confirmou que o bloco de objetos da saída não deve ser movido em um único patch.

A ordem segura passa a ser:

```text
SA-7D — ActivityObjectSnapshotCaptureStage
SA-7E — ActivityObjectReleaseStage
SA-7F — ActivityObjectContributorUnregisterStage
SA-7G — ActivityContentReleaseAsync audit/extraction
```

### Motivo

O fluxo atual mistura responsabilidades diferentes:

```text
Snapshot capture produz payload para RouteActivitySave.
Object release executa side-effects em endpoints locais.
Contributor unregister limpa discovery/runtime state.
ActivityContentRelease controla pending operation e scene unload async.
```

Mover tudo junto aumentaria risco de regressão em restart, activity transition e route-exit.

### Owner preservado

```text
SessionActivityPipeline continua dono do macro lifecycle de saída.
Stages dedicados executam passos determinísticos.
RouteActivitySave continua consumidor de payload, não owner de snapshot capture.
SaveRuntime continua persistência, não decide snapshot.
```

### Bridge identificada

A classe interna `ActivityObjectExitStage` foi classificada como bridge transitória, não stage Base 2.0 real:

```text
CaptureSnapshot -> EmitObjectSnapshotCaptureStageCore
Release -> EmitObjectReleaseStageCore
UnregisterContributors -> EmitObjectContributorUnregisterStageCore
```

Ela deve ser substituída gradualmente por stages reais em arquivos próprios.

### Próximo corte aceito

`SA-7D — ActivityObjectSnapshotCaptureStage`.

Escopo permitido:

```text
Extrair snapshot capture para stage dedicado.
Manter TryGetSnapshotPayloadForSaveOnExit como API pública.
Registrar payload/failure flags por bridge mínima.
Preservar facts/checkpoints de ActivityObjectSnapshotCapture.
```

Escopo proibido:

```text
Não mover ObjectRelease.
Não mover ContributorUnregister.
Não mover ActivityContentRelease async.
Não alterar RouteActivitySave.
Não criar ActivityExitPipeline.
Não executar save dentro do stage.
```

### Status

`SA-7C` está `CLOSED / AUDIT ONLY`.


## SA-7D — ActivityObjectSnapshotCaptureStage

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`

### Decisão

O snapshot capture de objetos da Activity foi extraído do bloco concreto do `SessionActivityPipeline` para um stage dedicado:

```text
SessionActivityPipeline
-> decide quando a saída/dematerialization exige snapshot
-> ActivityObjectSnapshotCaptureStage
   -> valida discovery/result atual
   -> resolve SnapshotProvider pelo ActivityCapabilityInventory
   -> executa ActivityObjectSnapshotCaptureCommand
   -> registra payload/failure flags por bridge mínima
   -> preserva facts/checkpoints de ActivityObjectSnapshotCapture
```

### Owner preservado

```text
SessionActivityPipeline continua dono do macro lifecycle de saída.
ActivityObjectSnapshotCaptureStage executa apenas o passo determinístico de captura.
RouteActivitySave continua consumidor do payload; não decide capture.
SaveRuntime continua backend/executor de persistência; não participa deste stage.
```

### Escopo aplicado

```text
Criado ActivityObjectSnapshotCaptureStage.
Criada bridge transitória IActivityObjectSnapshotCaptureRuntimeBridge.
EmitObjectSnapshotCaptureStage agora delega ao stage dedicado.
Removido o caminho ativo EmitObjectSnapshotCaptureStageCore do macro pipeline.
ActivityObjectExitStage deixou de possuir CaptureSnapshot e permanece só como bridge transitória para Release/Unregister.
```

### Escopo explicitamente não alterado

```text
ObjectRelease não foi movido.
ContributorUnregister não foi movido.
ActivityContentRelease async não foi movido.
RouteActivitySave não foi alterado.
TryGetSnapshotPayloadForSaveOnExit foi preservado como API pública.
ActivityExitPipeline não foi criado.
```

### Critério de smoke

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
RouteActivitySave payload continua consumível quando existir snapshot
```

### Status

`SA-7D` está `CLOSED / PASS funcional + PASS arquitetural do corte`.


### Smoke / evidência SA-7D

Smoke manual validado após compile. O log confirmou:

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

Evidência observada:

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

Conclusão arquitetural:

```text
SessionActivityPipeline continua dono do macro lifecycle de saída/dematerialization.
ActivityObjectSnapshotCaptureStage executa apenas o passo determinístico de capture.
RouteActivitySave continua consumidor do payload; não virou owner de snapshot capture.
ObjectRelease, ContributorUnregister, ActivityContentRelease async, DeactivationWindow e RouteExit não foram movidos.
Não foi criado ActivityExitPipeline.
```

Débito controlado:

```text
IActivityObjectSnapshotCaptureRuntimeBridge permanece transitória.
ActivityObjectReleaseStage fechado no SA-7E.
ActivityObjectContributorUnregisterStage fechado no SA-7F.
ActivityContentRelease async ainda exige auditoria/extraction própria no SA-7G.
```

## SA-7E — ActivityObjectReleaseStage

Status: `Applied / Pending smoke`

### Decisão

O release de objetos da Activity foi extraído do caminho ativo do `SessionActivityPipeline` para um stage dedicado:

```text
SessionActivityPipeline
-> decide quando a saída/dematerialization exige release de objetos
-> ActivityObjectReleaseStage
   -> valida discovery/result atual
   -> resolve ReleaseEndpoint pelo ActivityCapabilityInventory
   -> executa ActivityObjectReleaseCommand
   -> preserva facts/checkpoints de ObjectRelease
```

### Owner preservado

```text
SessionActivityPipeline continua dono do macro lifecycle de saída.
ActivityObjectReleaseStage executa apenas o passo determinístico de release.
ActivityContentRelease async continua dono do unload de scenes.
RouteActivitySave continua consumidor do payload já capturado; não decide release.
```

### Escopo aplicado

```text
Criado ActivityObjectReleaseStage.
Criada bridge transitória IActivityObjectReleaseRuntimeBridge.
EmitObjectReleaseStage agora delega ao stage dedicado.
ActivityObjectExitStage deixa de possuir Release e permanece apenas como bridge transitória para ContributorUnregister.
```

### Escopo explicitamente não alterado

```text
ContributorUnregister não foi movido.
ActivityContentRelease async não foi movido.
RouteActivitySave não foi alterado.
ActivityObjectSnapshotCaptureStage não foi alterado.
DeactivationWindow e RouteExit não foram movidos.
ActivityExitPipeline não foi criado.
```

### Critério de smoke

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

`SA-7E` está `CLOSED / PASS funcional + PASS arquitetural do corte`.

### Evidência de smoke

Smoke manual validado após compile.

Resultado observado:

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
```

Evidência funcional relevante:

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

Observação de observabilidade:

```text
Não há linha OBS literal ActivityObjectReleaseApplied no smoke.
A aplicação está confirmada por ActivityObjectReleaseCompleted appliedCount='1'
e pelo checkpoint ActivityObjectRelease checkpointStatus='Passed' appliedCount='1'.
Se necessário, emitir ActivityObjectReleaseApplied como OBS explícito deve ser hygiene local futuro,
não bloqueio funcional deste corte.
```

Conclusão arquitetural:

```text
SessionActivityPipeline continua dono do macro lifecycle de saída/dematerialization.
ActivityObjectReleaseStage executa apenas o passo determinístico de release.
ActivityObjectSnapshotCaptureStage continua separado e executa antes do release.
ContributorUnregister, ActivityContentRelease async, RouteActivitySave, DeactivationWindow e RouteExit não foram movidos.
Não foi criado ActivityExitPipeline.
```

Débito controlado:

```text
IActivityObjectReleaseRuntimeBridge permanece transitória.
ActivityObjectContributorUnregisterStage fechado no SA-7F.
ActivityContentRelease async ainda exige auditoria/extraction própria no SA-7G.
```


## SA-7F — ActivityObjectContributorUnregisterStage

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

### Decisão

O unregister de contributors de ActivityObject foi extraído do caminho ativo do `SessionActivityPipeline` para um stage dedicado:

```text
SessionActivityPipeline
-> decide quando a saída/dematerialization exige unregister de contributors
-> ActivityObjectContributorUnregisterStage
   -> valida discovery result da entry
   -> emite ActivityObjectContributorUnregisterStarted
   -> emite ActivityObjectContributorUnregistered por contributor quando houver
   -> limpa CurrentActivityObjectContributorDiscoveryResult
   -> emite ActivityObjectContributorUnregisterCompleted
```

### Owner preservado

```text
SessionActivityPipeline continua dono do macro lifecycle de saída/dematerialization.
ActivityObjectContributorUnregisterStage executa apenas o passo determinístico de unregister.
ActivityObjectSnapshotCaptureStage continua separado e executa antes do release.
ActivityObjectReleaseStage continua separado e executa antes do unregister.
ActivityContentRelease async continua responsável pelo unload de scenes.
RouteActivitySave continua consumidor do payload capturado; não decide unregister.
```

### Escopo aplicado

```text
Criado ActivityObjectContributorUnregisterStage.
Criada bridge transitória IActivityObjectContributorUnregisterRuntimeBridge.
Removido ActivityObjectExitStage do caminho ativo.
EmitObjectContributorUnregisterStage agora delega ao stage dedicado.
CurrentActivityObjectContributorDiscoveryResult passa a ser limpo pelo stage dedicado.
```

### Escopo explicitamente não alterado

```text
ActivityObjectSnapshotCaptureStage não foi alterado.
ActivityObjectReleaseStage não foi alterado.
ActivityContentRelease async não foi movido.
RouteActivitySave não foi alterado.
SaveRuntime não foi alterado.
DeactivationWindow e RouteExit não foram movidos.
ActivityExitPipeline não foi criado.
```

### Evidência de smoke

Smoke manual validado após compile.

Resultado observado:

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'
```

Evidência funcional relevante:

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

Conclusão arquitetural:

```text
SessionActivityPipeline continua dono do macro lifecycle de saída/dematerialization.
ActivityObjectContributorUnregisterStage executa apenas o passo determinístico de unregister.
Snapshot capture, object release e contributor unregister agora estão separados em stages próprios.
ActivityContentRelease async, RouteActivitySave, DeactivationWindow e RouteExit não foram movidos.
Não foi criado ActivityExitPipeline.
```

Débito controlado:

```text
IActivityObjectContributorUnregisterRuntimeBridge permanece transitória.
ActivityContentRelease async ainda exige auditoria/extraction própria no SA-7G.
```


## SA-7G — ActivityContentReleaseAsync audit

Status: `CLOSED / AUDIT ONLY`.

### Decisão

Não mover `ActivityContentRelease async` inteiro agora.

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
SessionActivityPipeline continua dono do macro lifecycle de saída/dematerialization.
ActivityContent scene unload dispatch pode virar stage determinístico.
UnityActivityContentSceneReleaseAdapter continua adapter de side-effect Unity.
UnitySessionActivityPendingOperationRunner continua bridge async técnica.
RouteActivitySave continua consumidor do payload; não decide unload/release.
```

### Próximo corte recomendado

```text
SA-7G1 — ActivityContentSceneUnloadDispatchStage
```

Escopo do próximo corte:

```text
Extrair apenas:
- validação do próximo loaded scene record;
- criação de ActivityContentSceneUnloadCommand;
- criação de SessionActivityPendingOperation;
- SetPendingOperation;
- ActivityContentSceneUnloadCommandIssued;
- chamada a RunActivityContentReleaseOperation.
```

Fica proibido no `SA-7G1`:

```text
Não mover CompleteActivityContentSceneUnloadOperation.
Não mover FinalizeActivityContentReleaseCompleted.
Não mover FailPendingOperation.
Não mover PendingActivityContentReleaseContext.
Não alterar RouteExit closure.
Não alterar restart/activity transition/deactivation continuation.
Não criar ActivityContentReleasePipeline.
Não criar ActivityExitPipeline.
```

### Critério de aceite futuro

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

### Débito documentado

```text
ExecuteNextActivityContentSceneRelease ainda é bridge transitória dentro do SessionActivityPipeline.
CompleteActivityContentSceneUnloadOperation permanece macro continuation owner.
PendingActivityContentReleaseContext permanece state técnico do macro pipeline.
```

## SA-7G1 — ActivityContentSceneUnloadDispatchStage

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

### Decisão aplicada

O dispatch do unload async de uma scene de `ActivityContent` foi extraído para um stage dedicado:

```text
SessionActivityPipeline
-> mantém PendingActivityContentReleaseContext
-> decide que precisa descarregar a próxima scene
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
SessionActivityPipeline continua dono do macro lifecycle de saída/dematerialization.
ActivityContentSceneUnloadDispatchStage executa apenas o dispatch determinístico do unload de uma scene.
CompleteActivityContentSceneUnloadOperation continua no SessionActivityPipeline como callback/continuation macro.
FinalizeActivityContentReleaseCompleted continua no SessionActivityPipeline.
FailPendingOperation continua no SessionActivityPipeline.
PendingActivityContentReleaseContext continua state técnico do macro pipeline.
```

### Escopo aplicado

```text
Criado ActivityContentSceneUnloadDispatchStage.
Criada bridge transitória IActivityContentSceneUnloadDispatchRuntimeBridge.
ExecuteNextActivityContentSceneRelease deixou de montar diretamente command/pending operation/runner call.
ActivityContentSceneUnloadCommandIssued foi preservado pelo stage dedicado.
Pending operation ActivityContentSceneUnload continua sendo criada antes do runner async.
```

### Escopo explicitamente não alterado

```text
Não moveu CompleteActivityContentSceneUnloadOperation.
Não moveu FinalizeActivityContentReleaseCompleted.
Não moveu FailPendingOperation.
Não moveu PendingActivityContentReleaseContext.
Não alterou UnityActivityContentSceneReleaseAdapter.
Não alterou UnitySessionActivityPendingOperationRunner.
Não alterou restart/activity transition/deactivation continuation.
Não alterou RouteExit closure.
Não alterou RouteActivitySave.
Não criou ActivityContentReleasePipeline.
Não criou ActivityExitPipeline.
```

### Critério de smoke

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
activity_02 no-content preserva skip explícito
```

### Débito controlado

```text
IActivityContentSceneUnloadDispatchRuntimeBridge é transitória.
CompleteActivityContentSceneUnloadOperation ainda concentra continuation macro.
ActivityContentRelease finalization ainda deve ser auditada antes de qualquer extração futura.
```

## SA-7G2A — ActivityContentReleaseFinalizationStage

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

### Decisão aplicada

A finalização determinística de `ActivityContentRelease` foi extraída para stage dedicado:

```text
SessionActivityPipeline
-> decide que o content release chegou ao ponto de finalização
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
ActivityContentReleaseFinalizationStage fecha apenas o release determinístico.
StartPendingRestartEntry permanece no SessionActivityPipeline.
CompleteRouteExitClosure permanece no SessionActivityPipeline.
ContinueAfterDeactivationAsync permanece no SessionActivityPipeline.
NextActivity continuation permanece no SessionActivityPipeline.
```

### Observabilidade obrigatória aplicada

O corte preserva os nomes canônicos:

```text
ActivityContentReleaseCompleted
SessionActivityDematerializationCompleted
ActivityObjectContributorUnregisterStarted
ActivityObjectContributorUnregistered
ActivityObjectContributorUnregisterCompleted
```

E adiciona os eventos explícitos:

```text
ActivityContentReleaseFinalizationStarted
ActivityContentReleaseFinalizationCleanupStarted
ActivityContentReleaseFinalizationCleanupCompleted
ActivityContentReleaseFinalizationCompleted
```

Campos observáveis adicionados:

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

### Escopo explicitamente não alterado

```text
CompleteActivityContentSceneUnloadOperation não foi movido.
FailPendingOperation não foi movido.
StartPendingRestartEntry não foi movido.
CompleteRouteExitClosure não foi movido.
ContinueAfterDeactivationAsync não foi movido.
NextActivity continuation não foi movido.
UnityActivityContentSceneReleaseAdapter não foi alterado.
UnitySessionActivityPendingOperationRunner não foi alterado.
RouteActivitySave não foi alterado.
Não foi criado ActivityContentReleasePipeline.
Não foi criado ActivityExitPipeline.
```

### Critério de smoke

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
activity_02 no-content preserva skip explícito
```


## SA-7G2A-H1 — ActivityContentReleaseCompleted observability alias



### Status

`SA-7G2A-H1` está `CLOSED / PASS funcional + PASS arquitetural do hygiene`.

### Evidência de smoke

Smoke manual validado após aplicação do H1.

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

O evento aparece nos três caminhos relevantes:

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

Conclusão arquitetural:

```text
O H1 corrigiu somente a observabilidade literal exigida pelo SA-7G2.
Não houve alteração de lifecycle.
Não houve alteração de cleanup.
Não houve alteração de continuation macro.
Não houve ActivityContentReleasePipeline.
Não houve ActivityExitPipeline.
SessionActivityPipeline segue dono da continuação macro.
ActivityContentReleaseFinalizationStage segue responsável apenas pela finalização determinística.
```

### Motivo

O smoke de `SA-7G2A` validou funcionalmente o fluxo de finalization, cleanup e continuation, mas a observabilidade literal `event='ActivityContentReleaseCompleted'` não apareceu no log. O nome `ActivityContentReleaseCompleted` aparecia como `stage` e como fact interno, mas não como evento OBS explícito do stage.

Como `SA-7G2` exigiu observabilidade canônica preservada, este hygiene adiciona um alias/fact OBS explícito sem alterar lifecycle.

### Alteração

`ActivityContentReleaseFinalizationStage` passa a emitir:

```text
event='ActivityContentReleaseCompleted'
owner='ActivityContentReleaseFinalizationStage'
```

logo após `SessionActivityFactKind.ActivityContentReleaseCompleted` ser registrado e antes de `SessionActivityDematerializationCompleted`.

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
Sem alteração de lifecycle.
Sem alteração de cleanup.
Sem alteração de continuation macro.
Sem alteração de RouteExit/Restart/NextActivity.
Sem novo pipeline.
Sem fallback.
```

### Smoke necessário

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


## SA-7G2B — ActivityContentReleaseContinuation audit

Status: `CLOSED / AUDIT ONLY`.

### Decisão

Não criar `ActivityContentReleaseContinuationStage` ainda.

A continuação pós-release ainda é macro lifecycle do `SessionActivityPipeline`.

### Owner correto

```text
SessionActivityPipeline continua dono de:
- restart continuation;
- next activity continuation;
- route-exit closure;
- deactivation/complete continuation.
```

### Próximo corte recomendado

```text
SA-7G2B-H1 — ActivityContentReleaseContinuationObservability
```

Escopo:

```text
Adicionar fact/OBS explícito para:
- ActivityContentReleaseContinuationResolved;
- ActivityContentReleaseContinuationStarted;
- ActivityContentReleaseContinuationCompleted.
```

Owner obrigatório:

```text
owner='SessionActivityPipeline'
```

Campos obrigatórios:

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
Não criar ActivityContentReleaseContinuationStage.
Não criar ActivityContentReleasePipeline.
Não criar ActivityExitPipeline.
Não mover StartPendingRestartEntry.
Não mover CompleteRouteExitClosure.
Não mover ContinueAfterDeactivationAsync.
Não alterar route-exit handoff.
Não alterar restart lifecycle.
Não alterar next activity lifecycle.
```

### Critério de aceite futuro

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
ActivityContentReleaseContinuationResolved aparece
ActivityContentReleaseContinuationStarted aparece
ActivityContentReleaseContinuationCompleted aparece quando aplicável
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
owner='SessionActivityPipeline'
```

## SA-7G2B-H1 — ActivityContentReleaseContinuationObservability

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

### Decisão aplicada

Observabilidade explícita foi adicionada para a continuação macro pós-`ActivityContentReleaseFinalizationStage`.

Eventos novos:

```text
ActivityContentReleaseContinuationResolved
ActivityContentReleaseContinuationStarted
ActivityContentReleaseContinuationCompleted
```

Owner obrigatório preservado:

```text
owner='SessionActivityPipeline'
```

### Regra arquitetural

```text
Pipeline decide a continuação.
Logs tornam a decisão verificável.
```

O patch não cria `ActivityContentReleaseContinuationStage`, não cria `ActivityContentReleasePipeline` e não cria `ActivityExitPipeline`.

### Escopo aplicado

```text
Adicionada telemetry interna transitória ActivityContentReleaseContinuationTelemetry.
ActivityContentReleaseContinuationResolved é emitido ao final da finalization.
ActivityContentReleaseContinuationStarted é emitido imediatamente antes da chamada de continuação macro.
ActivityContentReleaseContinuationCompleted é emitido após a chamada síncrona/delegação aplicável.
RestartCurrentActivity, NextActivity, RouteExit e CompleteActivity são classificados explicitamente.
```

### Campos observáveis

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

### Escopo explicitamente não alterado

```text
StartPendingRestartEntry continua no SessionActivityPipeline.
CompleteRouteExitClosure continua no SessionActivityPipeline.
ContinueAfterDeactivationAsync continua no SessionActivityPipeline.
Route-exit handoff não foi alterado.
Restart lifecycle não foi alterado.
Next activity lifecycle não foi alterado.
ActivityContentReleaseFinalizationStage não foi alterado.
Unload adapter/runner não foram alterados.
```

### Critério de smoke

```text
sem erros CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem checkpointStatus='Failed'

ActivityContentReleaseContinuationResolved aparece
ActivityContentReleaseContinuationStarted aparece
ActivityContentReleaseContinuationCompleted aparece quando aplicável

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


## SA-7H — Release/Exit Bridge Debt audit

Status: `CLOSED / AUDIT ONLY`.

### Decisão

As bridges transitórias criadas nos cortes SA-7B até SA-7G2B-H1 são aceitáveis temporariamente, mas não são contratos finais.

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
SessionActivityPipeline continua dono do macro lifecycle de saída/dematerialization/continuation.
Stages dedicados executam passos determinísticos.
Bridges apenas expõem state temporário ainda preso no pipeline.
```

### Regra

```text
Não expandir bridges.
Não transformar bridge em manager/coordinator.
Não expor continuation macro por bridge.
Não criar ActivityExitPipeline.
Não criar ActivityContentReleasePipeline.
```

### Próximo corte recomendado

```text
SA-7H1 — SessionActivityExitRuntimeState audit/design
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
Não mover código runtime.
Não remover bridge ainda.
Não alterar lifecycle.
Não criar manager/coordinator.
Não mover continuation macro.
```


## SA-7H1 — SessionActivityExitRuntimeState audit/design

Status: `CLOSED / DESIGN ONLY`.

### Decisão

Não criar um único `SessionActivityExitRuntimeState` gigante.

Separação aprovada para próximos cortes:

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

Runtime states armazenam state técnico.  
Runtime states não decidem lifecycle.

### Checkpoint SA-8C-DOC

Status: CLOSED / DOCUMENTED.

- `SA-ACTOR-1B1*` está fechado no trilho Actors.
- `ActorScope` ficou congelado como fonte canônica de `lifetime/retention/release`.
- `PlayerActor` e `NonPlayerActor` não são owners de lifetime.
- `PlayerParticipation` ficou restrito a `slot/selection/participant`.
- `ActivityActorExitRuntimeState` ficou classificado como `correlation store` técnico.
- `ActivityPlayerActorRegistry` ficou classificado como índice técnico puro, sem `Destroy` local.
- O próximo passo volta para decomposição macro de `SessionActivity`; não abrir `SessionScoped` ainda.

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

### Próximo corte recomendado

```text
SA-7H2 — ActivityContentReleaseRuntimeState implementation
```

Motivo:

```text
É o menor state coeso.
Cobre loaded set, pending release context e awaiting flag.
Reduz duas bridges relacionadas.
Não toca actor stores.
Não toca snapshot payload/save.
Não move continuation macro.
```

### Proibido no SA-7H2

```text
Não mover CompleteActivityContentSceneUnloadOperation.
Não mover StartPendingRestartEntry.
Não mover CompleteRouteExitClosure.
Não mover ContinueAfterDeactivationAsync.
Não criar manager/coordinator.
Não criar ActivityExitPipeline.
Não criar ActivityContentReleasePipeline.
Não alterar RouteActivitySave.
```

### Critério de aceite futuro

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

## SA-7H2 — ActivityContentReleaseRuntimeState implementation

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

### Decisão aplicada

Criado `ActivityContentReleaseRuntimeState` para concentrar o state técnico de release async de ActivityContent:

```text
CurrentLoadedSet
PendingActivityContentReleaseContext
IsAwaitingContinuation
```

### Owner preservado

```text
SessionActivityPipeline continua dono da continuation macro.
ActivityContentReleaseRuntimeState guarda apenas state técnico.
ActivityContentSceneUnloadDispatchStage continua stage de dispatch.
ActivityContentReleaseFinalizationStage continua stage de finalization.
```

### Escopo aplicado

```text
Criado NewScripts/SessionActivity/Pipeline/Runtime/ActivityContentReleaseRuntimeState.cs.
SessionActivityPipeline passa a delegar pending release context e awaiting flag ao runtime state.
Set/Clear de CurrentActivityContentLoadedSet passa a espelhar o state técnico no runtime state.
Bridges existentes continuam como camada transitória, mas agora leem/limpam o runtime state em vez de fields soltos do pipeline.
```

### Escopo explicitamente não alterado

```text
CompleteActivityContentSceneUnloadOperation não foi movido.
StartPendingRestartEntry não foi movido.
CompleteRouteExitClosure não foi movido.
ContinueAfterDeactivationAsync não foi movido.
ActivityContentReleaseContinuation* permanece owner='SessionActivityPipeline'.
RouteActivitySave não foi alterado.
ActivityExitPipeline não foi criado.
ActivityContentReleasePipeline não foi criado.
```

### Observabilidade nova esperada

```text
ActivityContentReleaseRuntimeStateLoadedSetStored
ActivityContentReleaseRuntimeStateLoadedSetCleared
ActivityContentReleaseRuntimeStatePendingContextStored
ActivityContentReleaseRuntimeStatePendingContextCleared
ActivityContentReleaseRuntimeStateAwaitingContinuationChanged
```

### Critério de smoke

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


## SA-7H2-H1 — ContentRelease bridge retirement audit

Status: `CLOSED / AUDIT ONLY`.

### Decisão

Não remover as duas bridges de `ActivityContentRelease` de uma vez.

Bridges auditadas:

```text
IActivityContentSceneUnloadDispatchRuntimeBridge
IActivityContentReleaseFinalizationRuntimeBridge
```

### Resultado

```text
IActivityContentSceneUnloadDispatchRuntimeBridge:
  pode ser reduzida/removida primeiro, desde que ActivityContentSceneUnloadDispatchStage dependa de ActivityContentReleaseRuntimeState e ports explícitos de pending operation/runner.

IActivityContentReleaseFinalizationRuntimeBridge:
  pode ser reduzida/removida depois, desde que ActivityContentReleaseFinalizationStage dependa de ActivityContentReleaseRuntimeState e de ActivityObjectContributorUnregisterStage ou executor explícito.
```

### Próximos cortes recomendados

```text
SA-7H2-H2 — ActivityContentSceneUnloadDispatchBridgeReduction
SA-7H2-H3 — ActivityContentReleaseFinalizationBridgeReduction
```

### Owner preservado

```text
SessionActivityPipeline continua dono do macro lifecycle e da continuation.
ActivityContentReleaseRuntimeState guarda state técnico.
Stages executam passos determinísticos.
Bridges são transitórias.
```

### Proibido

```text
Não mover CompleteActivityContentSceneUnloadOperation.
Não mover FailPendingOperation.
Não mover StartPendingRestartEntry.
Não mover CompleteRouteExitClosure.
Não mover ContinueAfterDeactivationAsync.
Não alterar RouteActivitySave.
Não criar ActivityContentReleasePipeline.
Não criar ActivityExitPipeline.
```

## SA-7H2-H2 — ActivityContentSceneUnloadDispatchBridgeReduction

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

### Resultado do smoke — 2026-06-02

Smoke manual validado a partir de `FullLog.txt` enviado em 2026-06-02.

Evidência aceita:

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

Decisão:

```text
SA-7H2-H2 fechado como PASS funcional + PASS arquitetural do corte.
A redução da bridge de dispatch de unload não regrediu restart, transition, route-exit, snapshot/release/unregister nem continuation macro.
```

### Decisão aplicada

A bridge transitória de dispatch de unload de `ActivityContent` foi removida do caminho ativo:

```text
IActivityContentSceneUnloadDispatchRuntimeBridge
```

O stage passou a depender de contratos explícitos:

```text
ActivityContentReleaseRuntimeState
IActivityEntryRuntimeEndpoint
ISessionActivityPendingOperationRunner
ISessionActivityPendingOperationCallback
```

### Owner preservado

```text
SessionActivityPipeline continua dono do callback e da continuation macro.
ActivityContentReleaseRuntimeState guarda state técnico.
ActivityContentSceneUnloadDispatchStage executa apenas o dispatch determinístico de unload.
PendingOperationRunner continua bridge técnica async.
UnityActivityContentSceneReleaseAdapter continua adapter de side-effect Unity.
```

### Escopo aplicado

```text
ActivityContentSceneUnloadDispatchStage lê PendingActivityContentReleaseContext via ActivityContentReleaseRuntimeState.
ActivityContentSceneUnloadDispatchStage monta SessionActivityPendingOperation localmente a partir de identity canônica.
ActivityContentSceneUnloadDispatchStage chama ISessionActivityPendingOperationRunner.RunActivityContentReleaseOperation diretamente.
SessionActivityPipeline deixou de implementar IActivityContentSceneUnloadDispatchRuntimeBridge.
Métodos explícitos da bridge de dispatch foram removidos.
```

### Escopo explicitamente não alterado

```text
CompleteActivityContentSceneUnloadOperation não foi movido.
FailPendingOperation não foi movido.
StartPendingRestartEntry não foi movido.
CompleteRouteExitClosure não foi movido.
ContinueAfterDeactivationAsync não foi movido.
ActivityContentReleaseFinalizationStage não foi alterado.
ActivityObjectContributorUnregisterStage não foi alterado.
RouteActivitySave não foi alterado.
Não foi criado ActivityContentReleasePipeline.
Não foi criado ActivityExitPipeline.
```

### Critério de smoke

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

## SA-7H2-H3 — ActivityContentReleaseFinalizationBridgeReduction

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

### Resultado do smoke — 2026-06-02

Smoke manual validado a partir de `FullLog.txt` enviado em 2026-06-02.

Evidência aceita:

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

Decisão:

```text
SA-7H2-H3 fechado como PASS funcional + PASS arquitetural do corte.
A remoção da finalization bridge não transformou ActivityContentReleaseFinalizationStage em owner de lifecycle macro; a continuation permanece no SessionActivityPipeline.
```

### Decisão aplicada

`IActivityContentReleaseFinalizationRuntimeBridge` foi removida do caminho ativo de finalization de `ActivityContentRelease`.

`ActivityContentReleaseFinalizationStage` passa a depender diretamente de:

```text
ActivityContentReleaseRuntimeState
IActivityObjectContributorUnregisterRuntimeBridge
IActivityEntryRuntimeEndpoint
```

### Owner preservado

```text
ActivityContentReleaseRuntimeState guarda state técnico de release async.
ActivityContentReleaseFinalizationStage executa somente finalization determinística.
ActivityObjectContributorUnregisterStage continua stage explícito.
SessionActivityPipeline continua dono de continuation macro.
```

### O que saiu

```text
IActivityContentReleaseFinalizationRuntimeBridge
implementações explícitas dessa bridge no SessionActivityPipeline
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
CompleteActivityContentSceneUnloadOperation não foi movido.
FailPendingOperation não foi movido.
StartPendingRestartEntry não foi movido.
CompleteRouteExitClosure não foi movido.
ContinueAfterDeactivationAsync não foi movido.
RouteActivitySave não foi alterado.
ActivityContentReleasePipeline não foi criado.
ActivityExitPipeline não foi criado.
Manager/coordinator novo não foi criado.
```

### Observação

`ActivityContentReleaseFinalizationStage` ainda usa `IActivityEntryRuntimeEndpoint.ClearCurrentActivityContentLoadedSet()` para limpar o loaded set espelhado em `SessionActivityRuntimeState`, enquanto `ActivityContentReleaseRuntimeState` permanece dono do state técnico de release async.

Isso não move lifecycle e não reintroduz a bridge de finalization.
## SA-7H3A — ActivityObjectExitRuntimeState

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

### Resultado do smoke — 2026-06-02

Smoke manual validado a partir de `FullLog.txt` enviado em 2026-06-02.

Evidência aceita:

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

Decisão:

```text
SA-7H3A fechado como PASS funcional + PASS arquitetural do corte.
ActivityObjectExitRuntimeState ficou validado como owner técnico de state de object exit, sem assumir lifecycle, save ou continuation macro.
```

### Decisão aplicada

`ActivityObjectExitRuntimeState` foi criado como owner técnico do state de object exit:

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

### Compatibilidade técnica transitória

```text
As bridges de object exit continuam existindo como facade fina.
SessionActivityRuntimeState ainda mantém espelho para consumidores de entry que ainda não foram migrados.
Esse espelho não é owner final e deve ser removido em cortes SA-7H3B/C/D.
```

### Owner preservado

```text
SessionActivityPipeline continua dono do macro lifecycle.
ActivityObjectExitRuntimeState guarda state técnico.
Stages continuam executando passos determinísticos.
RouteActivitySave continua consumidor do payload.
```

### Proibido preservado

```text
RouteActivitySave não foi movido.
Save não é executado pelo runtime state.
ActivityContentRelease não foi movido.
Callback async não foi movido.
Continuation macro não foi movida.
ActivityExitPipeline não foi criado.
Manager/coordinator novo não foi criado.
```

### Smoke necessário

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

## SA-7H3B — ActivityObjectSnapshotCaptureBridgeReduction

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

### Resultado do smoke — 2026-06-02

Smoke manual validado a partir de `FullLog.txt` enviado em 2026-06-02.

Evidência aceita:

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

Decisão:

```text
SA-7H3B fechado como PASS funcional + PASS arquitetural do corte.
ActivityObjectSnapshotCaptureStage passou a usar ActivityObjectExitRuntimeState como state técnico direto sem reintroduzir bridge ativa ou mover RouteActivitySave.
```

### Decisão aplicada

`IActivityObjectSnapshotCaptureRuntimeBridge` saiu do caminho ativo.

`ActivityObjectSnapshotCaptureStage` passa a depender diretamente de:

```text
ActivityObjectExitRuntimeState
IActivityEntryRuntimeEndpoint
```

### Owner preservado

```text
ActivityObjectExitRuntimeState guarda discovery/inventory/snapshot payload.
ActivityObjectSnapshotCaptureStage executa apenas snapshot capture determinístico.
SessionActivityPipeline continua dono de ordering/lifecycle/continuation macro.
RouteActivitySave continua consumidor externo do payload.
```

### O que saiu

```text
IActivityObjectSnapshotCaptureRuntimeBridge
implementações explícitas dessa bridge no SessionActivityPipeline
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
RouteActivitySave não foi movido.
```

### Escopo proibido preservado

```text
RouteActivitySave não foi movido.
Save não é executado pelo runtime state.
ObjectRelease não foi alterado.
ContributorUnregister não foi alterado.
ActivityContentRelease não foi alterado.
Callback async não foi movido.
Restart / next activity / route-exit / deactivation continuation não foram movidos.
ActivityExitPipeline não foi criado.
Manager/coordinator novo não foi criado.
```

## SA-7H3C-D — ActivityObjectReleaseAndContributorUnregisterBridgeReduction

Status: `CLOSED / PASS funcional + PASS arquitetural do corte`.

### Resultado do smoke — 2026-06-02

Smoke manual validado a partir de `FullLog.txt` enviado em 2026-06-02.

Evidência aceita:

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

Decisão:

```text
SA-7H3C-D fechado como PASS funcional + PASS arquitetural do corte.
ActivityObjectReleaseStage e ActivityObjectContributorUnregisterStage usam ActivityObjectExitRuntimeState diretamente e permanecem stages determinísticos; RouteActivitySave, callback async e continuation macro não foram movidos.
```

### Decisão aplicada

As bridges transitórias restantes de object exit saíram do caminho ativo:

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
ActivityObjectReleaseStage executa somente release determinístico.
ActivityObjectContributorUnregisterStage executa somente unregister determinístico.
SessionActivityPipeline continua dono de ordering/lifecycle/continuation macro.
RouteActivitySave continua consumidor externo do payload.
```

### O que saiu

```text
IActivityObjectReleaseRuntimeBridge
IActivityObjectContributorUnregisterRuntimeBridge
implementações explícitas dessas bridges no SessionActivityPipeline
acesso indireto ao ActivityObjectExitRuntimeState via bridges de release/unregister
```

### O que permanece

```text
ActivityObjectSnapshotCaptureStage permanece como validado no SA-7H3B.
ActivityObjectReleaseStarted/Completed preservados.
ActivityObjectContributorUnregisterStarted/Unregistered/Completed preservados.
ActivityObjectExitRuntimeStateContributorDiscoveryCleared preservado via IActivityEntryRuntimeEndpoint.
TryGetSnapshotPayloadForSaveOnExit continua lendo do ActivityObjectExitRuntimeState.
RouteActivitySave não foi movido.
```

### Escopo proibido preservado

```text
RouteActivitySave não foi movido.
Save não é executado pelo runtime state.
SnapshotCapture não foi alterado.
ActivityContentRelease não foi alterado.
Callback async não foi movido.
Restart / next activity / route-exit / deactivation continuation não foram movidos.
ActivityExitPipeline não foi criado.
Manager/coordinator novo não foi criado.
```
## SA-7H4A-Big — ActivityActorExitRuntimeState + bridge slimming

Status: `Applied / Pending smoke`.

### Decisão aplicada

Criado `ActivityActorExitRuntimeState` e movido o state técnico de actor exit para ele:

```text
active actor presentation states
active actor attribute states
active actor participation records
```

`ActivityExitActorTeardownStage` passa a usar `ActivityActorExitRuntimeState` diretamente para leitura/remoção desses records.

### Bridge preservada como port fino

`IActivityExitActorTeardownRuntimeBridge` permanece apenas para side-effects/adapters e resoluções que ainda não são state puro:

```text
ReleaseActorPresentation
ClearNonPlayerPresentationHandle
BuildActorInventoryFeedForExit
TryResolveActivePlayerParticipantBindingForExit
ExecutePlayerActorParticipationExit
```

### Escopo preservado

```text
Não move lifecycle.
Não move RouteActivitySave.
Não move ActivityContentRelease.
Não move ActivityObjectExit.
Não cria ActivityExitPipeline.
Não cria manager/coordinator.
Não reintroduz Player/NonPlayer como owner.
```

---

## Checkpoint SA-ACTOR-1C1 — ActorScope.SessionScoped structural lifetime

Status: `CLOSED / PASS funcional`.

### Contexto

O corte `SA-ACTOR-1C1` corrigiu uma fronteira de ownership em Actors dentro da Base 2.0: `ActorScope.SessionScoped` não pode ser apenas um enum nem uma configuração local do prefab. O scope precisa produzir identidade runtime, store/root session-owned, regras explícitas de teardown e integração com `ExitToMenu` sem criar trilho `Player/NonPlayer` paralelo.

### Decisão congelada

`ActorScope` decide apenas o lifetime estrutural do Actor.

```text
ActorScope.SessionScoped => o Actor estrutural sobrevive dentro da sessão.
ActorScope.RouteScoped => o Actor estrutural sobrevive dentro da rota.
ActorScope.ActivityScoped => o Actor estrutural vive só na Activity/entry.
```

`ActorScope.SessionScoped` não arrasta automaticamente:

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

Esses componentes/capabilities têm policy própria, como `ActivityScoped`, `RouteScoped`, `SessionScoped`, `ReleaseOnActivityExit`, `ReleaseOnRouteExit` ou equivalente local. A separação normativa é:

```text
ActorScope decide a sobrevivência estrutural do Actor.
ComponentScope/CapabilityPolicy decide a sobrevivência de cada componente/capability.
```

### Owner correto

| Decisão | Owner correto |
|---|---|
| `PlayerSlot`, seleção e `SessionParticipationContext` | `SessionOperational` / `PlayerParticipation` |
| `actorScope` do player materializado | `PlayerParticipation` / `OperationalPlayerParticipationStage`, invariant `SessionScoped` |
| Materialização/reuso do Actor na Activity | `ActivityEntryPipeline` |
| Root/store session-owned do Actor estrutural | `SessionActorRuntimeStore` como índice técnico + adapter/root runtime |
| Lifetime estrutural em `ActivityExit`, `RouteExit`, `SessionReset` | `SessionActivityPipeline` / `ActivityExitActorTeardownStage` / reset stage |
| Lifetime de Presentation/Attribute/Permission/Movement/Camera | stages/policies locais das capabilities |
| Encerramento de sessão ao sair para Menu | `SessionOperationalPipeline` detecta policy de destino; `SessionActivityPipeline` executa reset estrutural |

### Regras de lifetime congeladas

| Scope | ActivityExit | RouteExit | ExitToMenu / SessionReset |
|---|---|---|---|
| `ActivityScoped` | `Release` | `Release` se ainda ativo | `Release` |
| `RouteScoped` | `Retain` | `Release` | `Release` |
| `SessionScoped` | `Retain` | `Retain` | `Release` |

`RouteExit` genérico não libera `SessionScoped`, pois uma troca futura `GameplayRouteA -> GameplayRouteB` deve preservar actors de sessão. `ExitToMenu` encerra a sessão de gameplay e, por isso, executa `SessionReset` depois do teardown/save da rota anterior.

### Pontos implementados nos cortes H1-H7B2

```text
H1/H2 — Placement resolvido pela ActivityEntry usando fontes autorizadas, não pela scene física do actor persistente.
H3 — PlayerActor runtime metadata vem do binding/materialization context, não de campos soltos do prefab.
H4 — actorScope do Player sai do prefab; shape transitório via PlayerSetDefinition foi superado por H8A.
H5 — restaura owner correto de placement para SessionScoped.
H6 — RouteExit também emite decisão explícita para SessionScoped retido no SessionActorRuntimeStore.
H7A — Observabilidade separa ActorLifetime de ComponentLifetime e reduz logs redundantes locais.
H7B — Primitiva SessionReset libera SessionScoped estrutural.
H7B1 — ExitToMenu chama SessionReset automaticamente após RouteExit/save-on-exit.
H7B2 — SessionReset pós-RouteExit é permitido mesmo com pipeline terminal em ClosedForRouteExit.
```

### Observabilidade congelada

Logs/facts mínimos esperados:

```text
ActorLifetimeDecisionResolved actorScope='SessionScoped' trigger='ActivityExit' decision='Retain'
ActorLifetimeDecisionResolved actorScope='SessionScoped' trigger='RouteExit' decision='Retain'
ActorLifetimeDecisionResolved actorScope='SessionScoped' trigger='SessionReset' decision='Release'
ActorLifetimeReleased actorScope='SessionScoped' trigger='SessionReset'
SessionResetCompleted sessionActorCount='0'
```

Logs de component/capability lifetime devem expor explicitamente que não seguem automaticamente `ActorScope`:

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

### Evidência de smoke aceita

Smoke canônico usado para fechamento:

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

Critérios observados no fechamento:

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
PlayerActor prefab não é owner de ActorId, ActorScope nem ParticipationPolicy runtime.
PlayerParticipation é a fonte do actorScope estrutural do player materializado: invariant `SessionScoped`.
SessionParticipationContext carrega participação resolvida antes do handoff.
ActivityEntryPipeline materializa/reusa Actor a partir de ActivityParticipantBinding.
SessionActorRuntimeStore é índice técnico, não owner de lifecycle.
ActivityPlayerActorRegistry e ActivitySceneActorRegistry não decidem lifetime.
ActorScope não decide lifetime de Presentation/Attribute/Permission/Movement/Camera.
RouteExit genérico retém SessionScoped.
ExitToMenu chama SessionReset após RouteExit/save-on-exit.
SessionReset pode rodar após ClosedForRouteExit sem reabrir Activity lifecycle.
```

### Fora do escopo deste checkpoint

```text
Runtime join real.
Multiplayer/split-screen.
Progression Save real de actors.
Policy avançada de PowerUps/Attributes além da observabilidade de ComponentLifetime.
Pooling real de ActorPresentation.
Redução global de logs de InputModes/Loading/Permission.
```

### Resultado

`SA-ACTOR-1C1` fica fechado como PASS funcional para o objetivo de `ActorScope.SessionScoped` estrutural dentro da decomposição de `SessionActivity` Base 2.0. Novos cortes de components/capabilities devem respeitar a separação: Actor estrutural ≠ componente/capability material.



---

## Checkpoint SA-ACTOR-1C1-H8 — PlayerParticipation identity cleanup

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

Este checkpoint complementa o fechamento `SA-ACTOR-1C1` e corrige a fronteira entre `PlayerParticipation`, `SessionParticipationContext` e `ActivityEntryPipeline` sem reabrir o lifetime de components/capabilities.

### Decisões congeladas

```text
Player estrutural vindo de PlayerParticipation é sempre ActorScope.SessionScoped.
PlayerSetDefinition não expõe mais actorScope editável.
ActorDefinitionId identifica archetype/definition.
ActorId identifica o Actor semântico do participante default.
SessionParticipantId é derivado de PlayerSlotId.
Materialization seed resolution usa PlayerSlotId, não ActorDefinitionId.
```

### Ownership final do corte

| Responsabilidade | Owner correto |
|---|---|
| Scope estrutural do player | `PlayerParticipation` / `OperationalPlayerParticipationStage`, sempre `SessionScoped` |
| Slot/assento | `PlayerSlotId` em `PlayerSetDefinitionEntry` / `PlayerParticipation` |
| Seleção default | `PlayerSelectionId` em `PlayerSetDefinitionEntry` |
| Definition/archetype | `ActorDefinitionId` em `ActorDefinitionAsset` |
| Actor semântico do participante default | `PlayerSetDefinitionEntry.actorId` |
| Participante de sessão | `SessionParticipantId`, derivado de `PlayerSlotId` |
| Materialização/reuso concreto | `ActivityEntryPipeline` / `ActivityParticipantBinding` / `PlayerActorRuntimeHandle` |

### Cortes fechados

```text
H8A  — Player scope invariant cleanup.
H8C1 — PlayerSlot materialization resolution.
H8C2 — SessionParticipantId by PlayerSlotId.
H8C3 — Player ActorId owner cleanup.
```

### Evidência aceita

Smoke canônico completo aceito:

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

Critérios observados:

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
ActorDefinitionAsset não é owner do ActorId do player participante.
PlayerSetDefinitionEntry é owner autoral temporário do ActorId default do player.
ActorDefinitionId não pode ser usado como chave runtime para reencontrar participante.
PlayerSlotId é a chave de correlação entre seed de PlayerParticipation e SessionParticipantBinding.
SessionParticipantId não depende de índice/ordem de lista.
```
