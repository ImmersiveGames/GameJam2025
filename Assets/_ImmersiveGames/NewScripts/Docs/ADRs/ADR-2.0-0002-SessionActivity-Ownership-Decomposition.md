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
PlayerActor readiness dentro da Activity
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

## Sequência normativa de refatoração

1. Congelar este ADR.
2. Unificar owner de `RouteExit teardown`.
3. Criar `ActivityEntryPipeline` concreto sem trilho paralelo.
4. Migrar blocos de entry em cortes pequenos, removendo o caminho antigo a cada corte.
5. Só depois reduzir state mutável e limpar Host/composition.
6. Tratar identity de permission como débito próprio, sem misturar com o corte inicial de entry.

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
