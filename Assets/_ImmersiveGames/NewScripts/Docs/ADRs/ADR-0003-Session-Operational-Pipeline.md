<!--
STATUS: HISTÓRICO PARA CONSULTA.
Este ADR foi reclassificado pelo ADR-2.0-0001 — Capability Discovery e Activity Capability Inventory.
Use como evidência, histórico e intenção funcional. Em conflito, ADR-2.0-0001 prevalece.
-->

# ADR-0003 - Session Operational Pipeline e Session Transition Envelope

## Status

- Estado: Accepted
- Data: 2026-05-12
- Última atualização: 2026-05-21
- Tipo: Direction / Canonical architecture
- Fonte de verdade canônica deste contrato: este ADR.

---

## Contexto

O conceito histórico de `local` misturou decisão semântica da sessão, ativação de conteúdo e comportamento scene-local.

Na Base 1.0, parte do setup de sessão ficou espalhada como efeito implícito de gates, handoffs, rotas, serviços de cena e decisões locais sem identidade explícita.

A Base 1.1 exige:

1. Um rail próprio e determinístico para a sessão: `Session Operational Pipeline`.
2. Um envelope temporal explícito para a transição de sessão: `Session Transition Envelope`.
3. Separação clara entre:
    - pipeline que decide ordem, lifecycle, policies e handoffs;
    - adapters que executam side-effects;
    - módulos/stages que produzem `Pipeline Facts`, `Pipeline Commands`, `Pipeline Snapshots` ou dados de handoff.
4. Proteção por `Pipeline Identity`, para impedir que eventos `foreign/stale` alterem o pipeline ativo.

---

## Decisão

Adota-se o `SessionOperationalPipeline` como owner semântico do ciclo operacional de sessão/rota na Base 1.1.

O `Session Transition Envelope` é o contrato temporal que define a janela segura da transição com cortina/loading fechado.

O `SessionOperationalPipeline` decide:

- ordem da rota operacional;
- início e conclusão da transição;
- lifecycle operacional da sessão;
- policies de rota;
- `Pipeline Handoffs`;
- quando executar save/load operacional;
- quando preparar input operacional;
- qual `SessionOperationalInputPolicy` da rota deve ser aplicada e quando emitir `SessionOperationalInputModeCommand`;
- quando executar `PlayerPreparation`;
- quando emitir handoff para `SessionActivityPipeline`.

Adapters executam side-effects comandados pelo pipeline:

- scene composition;
- fade;
- loading;
- audio;
- save runtime;
- input runtime;
- preparacao de intencao/payload de player para handoff (sem materializacao Unity de PlayerActor).

---

## 1. Session Operational Pipeline

### 1.1 Princípios

- `SessionOperationalPipeline` substitui o uso histórico de `local` como owner implícito de sessão.
- O pipeline concentra lifecycle de sessão, políticas operacionais e handoffs locais.
- `IntroStage`, `RunResult`, `GameLoop`, `InputMode`, gates e scene services não decidem lifecycle da sessão.
- Scene/route/navigation executam side-effects físicos por adapters; não decidem o ciclo semântico.
- O host local resolve a instância concreta somente no momento canônico do pipeline.
- Toda rota operacional relevante possui identidade explícita:
    - `routeIdentity`;
    - `routeOperationId`;
    - `transitionId`;
    - `routeSequence`;
    - `source`;
    - `reason`.

### 1.2 Invariantes

- Toda sessão relevante possui identidade explícita.
- Eventos `foreign/stale` não podem alterar o `SessionOperationalPipeline` ativo.
- A resolução local concreta só ocorre no momento canônico do pipeline.
- Ausência válida de conteúdo gera `skip/no-content`, `observed_noop` ou skip explícito equivalente.
- Não há fallback silencioso para configuração obrigatória.
- Pipelines decidem.
- Adapters executam side-effects.
- Config fornece dados, mas não decide lifecycle.

---

## 2. Session Transition Envelope

Adota-se o conceito canônico de `SessionTransitionEnvelope`.

O envelope representa a janela temporal da transição operacional, normalmente com cortina/loading fechados.

### 2.1 Regras do Envelope

1. O envelope roda durante a transição operacional da rota.
2. A cortina/loading deve proteger visualmente operações de setup e composição.
3. `SessionOperationalTeardown` acontece depois que a nova rota começa e antes de desmontar recursos relevantes da rota anterior.
4. `RoutePhysicalApply` é a aplicação física da rota via scene composition.
5. `SessionOperationalSetup` acontece depois de cenas prontas e antes do reveal.
6. `PlayerPreparation` acontece dentro da janela operacional, antes do handoff.
7. `SessionActivityEntryHandoff` deve ocorrer apenas depois do setup operacional necessário.
8. `SessionActivityPipeline` começa somente depois do handoff preparado.
9. `SessionActivityHost` atua como bridge/composition surface e nao decide entrada de Activity.
10. `autoStart` e comandos de debug locais nao substituem o handoff canonico de producao.
11. `SceneFlow`/`Navigation` não decidem lifecycle; permanecem como executores/adapters físicos.
12. `InputMode`, `SimulationGate`, `GameLoop`, save, audio, loading, fade e scene composition entram como adapters/stages comandados pelo pipeline; PlayerPreparation permanece planned_only/intencao/handoff payload.
13. `PlayerPreparationStage` no `SessionOperational` é restrito a requisitos de players.
14. Actors não-player — enemies, NPCs, props, objetos e actors de activity — ficam fora do ownership ativo de `SessionOperational` e pertencem ao futuro `ActivitySetup`/`SessionActivity`.

### 2.2 Fases Canônicas do Envelope

1. `TransitionStarted`
2. `CurtainClosed`
3. `SessionOperationalTeardown`
4. `RoutePhysicalApply`
5. `SessionOperationalSetup`
6. `PlayerPreparation`
7. `SessionActivityEntryHandoffPrepared`
8. `BeforeFadeOut`
9. `TransitionCompleted`
10. `ActivityActivation`

### 2.3 Invariantes do Envelope

- Não transformar `SceneTransitionService` em owner semântico.
- Não usar `SessionActivityMiniFlowHost` como composition root da transição.
- Não usar `SessionActivityPipeline` como owner de lifecycle de cena.
- Não introduzir fallback legado para suprir ausência de envelope.
- Não permitir que SceneFlow/Navigation decida lifecycle.
- Não permitir que events `foreign/stale` executem setup, materialização ou handoff no pipeline ativo.

---

## 3. Ordem Canônica Atual do SessionOperational

A ordem atual validada para rota operacional com handoff para activity é:

```text
RouteRequested
-> RoutePlanReady
-> LoadingStarted
-> FadeIn
-> RouteActivitySave save-on-exit da rota anterior, se aplicável
-> SceneComposition
-> SceneCompositionCompleted
-> RouteActivitySave load-on-enter da rota atual, se aplicável
-> InputCapability
-> PlayerPreparationStarted
-> PlayerPreparationCompleted
-> PlayerPreparationIntentPrepared
-> LoadingCompleted
-> LoadingHidden
-> RouteRevealAudio
-> FadeOut
-> OperationalRouteCompleted
-> SessionActivityEntryHandoff

Nota normativa curta (checkpoint de áudio operacional de rota):
- Durante setup/reveal operacional, `SessionOperationalPipeline` emite e executa o comando de `RouteAudio` (`RouteAudioPlanReady` -> `RouteRevealAudioStarted` -> `RouteRevealAudioSubmitted`) com `AudioAdapter` como executor de side-effect.

Nota normativa curta (checkpoint RuntimeConfig / wiring obrigatório):
- `SessionOperationalRuntime` é composto via `RuntimeConfigRegistry`/`RuntimeConfigSetAsset` (profile `Base11Sandbox`) e registra apenas adapters canônicos.

Regra de fronteira aplicada:
- Apos `SessionActivityEntryHandoff`, a entrada na Activity ocorre por `SessionActivityPipeline.StartFromPreparedHandoff`.
- `SessionActivityHost` nao inicia Activity automaticamente e nao substitui o handoff em runtime normal.

Nota curta (RouteActivitySave boundary):
- `SessionOperationalPipeline` permanece owner canônico de timing/policy de `RouteActivitySave` (`save-on-exit`/`load-on-enter`); adapter e `SaveRuntime` executam, sem decidir lifecycle.

---

## Checkpoint - SessionActivity Route Exit Teardown Pre-Unload (2026-05-17)

- SessionOperationalPipeline valida teardown canonico de SessionActivity **antes** de executar SceneComposition quando a rota anterior possui SessionActivity ativa e a cena sera descarregada.
- Observabilidade operacional obrigatoria:
  - SessionActivityRouteExitTeardownStarted
  - SessionActivityRouteExitTeardownCompleted
  - SessionActivityRouteExitBlocked
- Se o fechamento local nao atingir ActivityDeactivated antes do unload, a rota e bloqueada no SessionOperationalPipeline com erro fatal; SceneComposition nao deve iniciar unload.
- Boundary de ownership:
  - SessionOperational: owner da ordem da rota/unload.
  - SessionActivity: owner do lifecycle local de fechamento.
  - SceneComposition: executor fisico de unload/load, sem decisao de lifecycle.

## Checkpoint - Route-Exit Close sem Handoff de Catalogo (2026-05-17)

- SessionOperationalPipeline usa o boundary de RouteExitTeardown para exigir fechamento de SessionActivity sem continucao de catalogo.
- O caminho usado no teardown de saida de rota e CloseForRouteExit (nao CompleteCurrentActivity).
- Se o resultado voltar com handoff pendente, a rota e bloqueada antes de SceneComposition (SessionActivityRouteExitBlocked).

## Checkpoint - SessionActivityEntry RouteCamera skip por prioridade da ActivityCamera (2026-05-18)

- Para rotas com completionHandoff=SessionActivityEntry, RouteCameraPresentationStage aplica skip explicito quando a policy indicar prioridade da ActivityCamera.
- Skip canonico: reason='activity_camera_has_priority'.
- RouteCamera stage nao resolve SurfaceCameraAnchorHost nem executa side-effect de camera nesse caso.
- A rota segue para PlayerPreparation/ActivityCameraPreparationStage e conclui SessionActivityEntryHandoff sem transferir ownership de camera para RouteCamera.


## Checkpoint - Fronteira PlayerSelection / PlayerActor (2026-05-18)

Decisao congelada para a fronteira entre `SessionOperationalPipeline` e `SessionActivityPipeline`:

```text
SessionOperationalPipeline
-> valida PlayerSlot / PlayerInputManager / input operacional
-> transporta PlayerSelectionSnapshot ou payload equivalente no handoff
-> nao materializa PlayerActor jogavel final
```

```text
SessionActivityPipeline / ActivitySetup
-> consome PlayerSelectionSnapshot
-> resolve PlayerActorEntryPlan
-> materializa PlayerActor v0
```

Slots podem ser ocupados antes da rota de gameplay.

A origem da intencao pode ser:

```text
CharacterSelection Activity futura
```

ou, no MVP:

```text
Menu -> botao "1 Player" / "2 Players" -> PlayerSelectionSnapshot default
```

O `SessionOperationalPipeline` nao deve transformar essa intencao em runtime jogavel. Ele apenas valida capacidade, conserva identidade e prepara o handoff.

Regra de ownership:

```text
PlayerPreparation nao e PlayerActorSetup.
PlayerActorSetup pertence ao ActivitySetup da SessionActivity.
```

`PlayerSelectionSnapshot` nao deve carregar referencias Unity runtime como `GameObject`, `Transform`, `PlayerInput`, `Camera` ou componentes de movimento.

### Checkpoint - Transporte de PlayerSelectionSnapshot no Handoff (2026-05-18)

Para rotas com `completionHandoff=SessionActivityEntry` que exigem player, o `SessionOperationalPipeline` pode transportar `PlayerSelectionSnapshot` ou payload equivalente no `SessionActivityEntryHandoff`.

Responsabilidades do `SessionOperationalPipeline` neste ponto:

```text
validar playerCount contra maxPlayerSlots
validar slots ocupados quando aplicavel
validar presenca do snapshot quando obrigatorio
preservar Pipeline Identity no payload/handoff
nao materializar PlayerActor jogavel final
```

O payload deve ser explicito, imutavel para o ciclo e sem referencias Unity runtime.

O consumo materializante pertence ao `SessionActivityPipeline / ActivitySetup`:

```text
SessionActivityEntryHandoff
-> ActivitySetup
-> PlayerActorSetupStage
-> PlayerActorEntryPlan
-> PlayerActor v0
```

Ficam fora do contrato canonico:

```text
static/global mutable player selection
DontDestroyOnLoad selection object como fonte de verdade
instancia de player criada no menu
PlayerInputManager criando PlayerActor final antes da Activity
```

### Checkpoint - Validacao de PlayerSelectionSnapshot antes do Handoff materializante (2026-05-18)

Para rotas com `completionHandoff=SessionActivityEntry`, o `SessionOperationalPipeline` valida a consistencia minima do `PlayerSelectionSnapshot` antes de transporta-lo no handoff.

Responsabilidades operacionais:

```text
validar snapshot obrigatorio quando a rota/activity exige player
validar playerCount contra maxPlayerSlots
validar slots duplicados
validar payload sem referencias Unity runtime
preservar Pipeline Identity
nao materializar PlayerActor jogavel final
```

O `SessionOperationalPipeline` nao valida detalhes de materializacao concreta, como prefab final, spawn point ou reset tecnico da instancia. Esses requisitos pertencem ao `SessionActivityPipeline / ActivitySetup`.

Responsabilidades de `ActivitySetup`:

```text
validar PlayerDefinitionId
validar SpawnPointId
validar PlayerActorEntryPlan
validar PlayerActorResetPlan
materializar PlayerActor v0 somente depois da validacao
```

Regra congelada:

```text
Ausencia obrigatoria e fail-fast.
Ausencia aceitavel e skip explicito.
Fallback silencioso para primeiro prefab/slot/spawn/player encontrado e proibido.
```

### Checkpoint - Fronteira com PlayerActorSetupStage (2026-05-18)

Para rotas com `completionHandoff=SessionActivityEntry`, o `SessionOperationalPipeline` transporta e valida a intencao de player, mas nao executa `PlayerActorSetupStage`.

Fronteira canonica:

```text
SessionOperationalPipeline
-> valida capacidade/consistencia do PlayerSelectionSnapshot
-> emite SessionActivityEntryHandoff com payload valido
-> nao materializa PlayerActor jogavel final
```

```text
SessionActivityPipeline / ActivitySetup
-> PlayerActorSetupStage
-> PlayerActorEntryPlan
-> PlayerActorResetPlan
-> PlayerActorMaterializationCommand
-> PlayerActorReadyFact
```

O `PlayerActorSetupStage` pertence ao `ActivitySetup`, nao ao `SessionOperationalPipeline`.

Regra congelada:

```text
SessionOperationalPipeline valida e transporta intencao.
SessionActivityPipeline materializa PlayerActor v0 dentro do ActivitySetup.
```

### Checkpoint - Fronteira com PlayerDefinition e ActivityPlayerSpawnPoint (2026-05-18)

`SessionOperationalPipeline` nao resolve prefab final nem spawn point de `PlayerActor`.

Para rotas com `completionHandoff=SessionActivityEntry`, o pipeline operacional pode transportar `PlayerSelectionSnapshot` validado, mas a resolucao concreta de materializacao pertence ao `SessionActivityPipeline / ActivitySetup`.

Fronteira canonica:

```text
SessionOperationalPipeline
-> valida capacidade/consistencia do PlayerSelectionSnapshot
-> emite SessionActivityEntryHandoff com payload valido
-> nao resolve PlayerDefinition
-> nao resolve ActivityPlayerSpawnPoint
-> nao materializa PlayerActor jogavel final
```

```text
SessionActivityPipeline / ActivitySetup
-> PlayerActorSetupStage
-> resolve PlayerDefinition
-> resolve ActivityPlayerSpawnPoint
-> PlayerActorEntryPlan
-> PlayerActorResetPlan
-> PlayerActorMaterializationCommand
-> PlayerActorReadyFact
```

Regras congeladas:

```text
Prefab final de player nao e policy do SessionOperationalPipeline.
Spawn point de player nao e policy do SessionOperationalPipeline.
Nao ha fallback operacional para primeiro prefab/spawn/player encontrado.
```

### Checkpoint - Fronteira com PlayerActorIdentity e ActivityPlayerActorRegistry (2026-05-18)

`SessionOperationalPipeline` nao registra `PlayerActor` e nao resolve instancia runtime de player.

Para rotas com `completionHandoff=SessionActivityEntry`, a fronteira permanece:

```text
SessionOperationalPipeline
-> valida capacidade/consistencia do PlayerSelectionSnapshot
-> transporta payload valido no SessionActivityEntryHandoff
-> nao materializa PlayerActor
-> nao aplica PlayerActorIdentity
-> nao registra PlayerActor em registry runtime
```

```text
SessionActivityPipeline / ActivitySetup
-> PlayerActorSetupStage
-> PlayerActorMaterializationCommand
-> PlayerActorIdentity
-> ActivityPlayerActorRegistry
-> PlayerActorReadyFact
```

Regra congelada:

```text
PlayerActorIdentity e ActivityPlayerActorRegistry pertencem ao nascimento do PlayerActor no ActivitySetup.
SessionOperationalPipeline nao usa registry de PlayerActor como fonte de lifecycle, readiness ou materializacao.
```

O registry da Activity e recurso de localizacao por identidade para stages/adapters futuros, nao owner de pipeline.

### Checkpoint - Fronteira com PlayerActorRelease (2026-05-18)

`SessionOperationalPipeline` nao libera `PlayerActor` diretamente.

Para rotas com `completionHandoff=SessionActivityEntry` ou saida de rota com Activity ativa, a fronteira permanece:

```text
SessionOperationalPipeline
-> decide ordem da rota/unload
-> solicita/aguarda fechamento canonico da SessionActivity quando necessario
-> nao executa PlayerActorReleaseCommand diretamente
```

```text
SessionActivityPipeline / ActivityRelease
-> resolve PlayerActorReleasePlan
-> emite PlayerActorReleaseCommand
-> aguarda PlayerActorReleasedFact
-> remove registro do ActivityPlayerActorRegistry
-> informa fechamento canonico ao SessionOperationalPipeline
```

Regra congelada:

```text
PlayerActorRelease pertence ao lifecycle local da SessionActivity.
SessionOperationalPipeline pode bloquear unload/route-exit ate a SessionActivity concluir release obrigatorio.
SessionOperationalPipeline nao destroi PlayerActor, nao limpa registry e nao executa fallback de release.
```

Isso preserva a fronteira: `SessionOperationalPipeline` decide ordem de rota; `SessionActivityPipeline` decide lifecycle/release local do PlayerActor ActivityOwned.


### Checkpoint - Fronteira com PlayerActorReset (2026-05-18)

`SessionOperationalPipeline` nao reseta `PlayerActor` diretamente.

Para restart/reset local de Activity ou saida de rota com Activity ativa, a fronteira permanece:

```text
SessionOperationalPipeline
-> decide ordem da rota/unload quando aplicavel
-> solicita/aguarda fechamento canonico da SessionActivity quando necessario
-> nao executa PlayerActorResetCommand diretamente
```

```text
SessionActivityPipeline / ActivitySetup
-> resolve PlayerActorResetPlan
-> emite PlayerActorResetCommand
-> aguarda PlayerActorResetCompletedFact
-> garante PlayerActorReadyFact obrigatorio antes de concluir restart/setup
```

Regra congelada:

```text
PlayerActorReset pertence ao lifecycle local da SessionActivity.
SessionOperationalPipeline pode bloquear unload/route-exit ate a SessionActivity concluir reset/release obrigatorio quando aplicavel.
SessionOperationalPipeline nao reposiciona PlayerActor, nao limpa estado runtime de player e nao executa fallback de reset.
```

Isso preserva a fronteira: `SessionOperationalPipeline` decide ordem de rota; `SessionActivityPipeline` decide lifecycle/reset local do `PlayerActor ActivityOwned`.

### Checkpoint - Fronteira com componentes minimos do PlayerActor v0 (2026-05-18)

`SessionOperationalPipeline` nao define nem injeta componentes internos do `PlayerActor v0`.

Para rotas com `completionHandoff=SessionActivityEntry`, a fronteira permanece:

```text
SessionOperationalPipeline
-> valida capacidade/consistencia do PlayerSelectionSnapshot
-> transporta payload valido no SessionActivityEntryHandoff
-> nao adiciona PlayerInput
-> nao adiciona movimento
-> nao adiciona camera/Cinemachine
-> nao adiciona save/progression
-> nao configura componentes internos do PlayerActor
```

```text
SessionActivityPipeline / ActivitySetup
-> PlayerActorSetupStage
-> PlayerDefinition resolve prefab/config autoral
-> PlayerActorMaterializationAdapter instancia prefab
-> aplica PlayerActorIdentityComponent / LifecycleMarker minimo
-> registra PlayerActor v0
-> emite PlayerActorReadyFact
```

Regra congelada:

```text
Os componentes minimos do PlayerActor v0 pertencem ao nascimento do PlayerActor no ActivitySetup.
SessionOperationalPipeline nao transforma PlayerActor v0 em player jogavel final por side-effect operacional.
```

### Checkpoint - Fronteira com PlayerActorReadyFact v0 (2026-05-18)

`SessionOperationalPipeline` nao define readiness jogavel do `PlayerActor`.

Para rotas com `completionHandoff=SessionActivityEntry`, a fronteira permanece:

```text
SessionOperationalPipeline
-> valida/transporta PlayerSelectionSnapshot
-> emite SessionActivityEntryHandoff
-> nao emite PlayerActorReadyFact
-> nao decide se input/movimento/camera/save do PlayerActor estao prontos
```

```text
SessionActivityPipeline / ActivitySetup
-> PlayerActorSetupStage
-> PlayerActorMaterializationCommand
-> PlayerActorMaterializedFact
-> PlayerActorReadyFact
```

Regra congelada:

```text
PlayerActorReadyFact pertence ao ActivitySetup.
No MVP, PlayerActorReadyFact significa MaterializedOnly.
SessionOperationalPipeline pode bloquear/aguardar fechamento canonico da SessionActivity em route-exit, mas nao transforma PlayerActorReadyFact em readiness operacional de rota.
```


### Checkpoint - PlayerActor v0 RouteOwned/RouteScoped (2026-05-19)

Contrato congelado de lifetime:

```text
PlayerActor v0 nasce no ActivitySetup.
Lifetime padrao do PlayerActor v0 pertence a rota (RouteOwned/RouteScoped), nao a Activity.
Activity exit encerra participacao local do PlayerActor na Activity, sem destruir por padrao.
Deactivation nao implica Release.
```

Policy explicita de route-exit:

```text
ReleasePlayersOnRouteExit
PersistPlayersAcrossRoutes
```

Fronteira com SessionOperational:

```text
SessionOperationalPipeline decide ordem de rota e aplica policy de route-exit.
SessionOperationalPipeline nao destroi PlayerActor diretamente.
SessionOperationalPipeline transporta intencao/handoff e rejeita foreign/stale.
```

Proximo corte tecnico:

```text
PlayerActorParticipationExit v0
```

### Checkpoint - Fronteira SessionOperational x PlayerActorParticipationExit (2026-05-19)

Contrato congelado:

```text
SessionOperationalPipeline nao decide participation exit local de PlayerActor na Activity.
SessionOperationalPipeline permanece owner de ordem/policy de rota, handoff e route-exit.
SessionActivityPipeline decide PlayerActorParticipationExit v0 no lifecycle local da Activity.
```

Boundary de policy:

```text
Participation exit local da Activity != route exit policy.
Route exit policy explicita permanece:
ReleasePlayersOnRouteExit
PersistPlayersAcrossRoutes
```

Reforco de ownership:

```text
SessionOperational nao destroi PlayerActor diretamente.
SessionOperational nao transforma Gate/InputMode em owner de participation lifecycle.
```

### Checkpoint - Fronteira SessionOperational x Participation Enter/Reenter + Catalog Loop (2026-05-19)

Contrato fechado:

```text
SessionOperationalPipeline permanece owner de ordem/policy de rota e handoff.
SessionOperationalPipeline nao materializa PlayerActor.
SessionOperationalPipeline nao decide ParticipationEnter/Reenter.
SessionOperationalPipeline transporta apenas intencao/payload de player (planned_only) para SessionActivity.
```

Boundary com SessionActivity:

```text
SessionActivityPipeline decide:
- ParticipationExit antes da DeactivationWindow
- ParticipationEnter/Reenter durante ActivitySetup
- continuidade Activity -> Activity por nextActivityId explicito ou policy de catalogo LoopToFirst
```

Regra de ownership preservada:

```text
Looping de catalogo e decisao da SessionActivityPipeline.
QA/Host nao decide looping.
SessionOperational apenas consome o fechamento canonico da SessionActivity quando necessario para route-exit/unload.
```

### Checkpoint - Route-exit handshake observavel com SessionActivity (2026-05-19)

Contrato congelado:

```text
Route-exit com SessionActivity ativa e um handshake observavel, nao single-shot cego.
SessionOperationalPipeline nao pode avancar para SceneComposition unload enquanto SessionActivity tiver rail/pending ativo.
```

Regras obrigatorias:

```text
1) SessionOperational solicita close/teardown local por boundary explicito.
2) SessionActivity responde estado observavel do rail (request -> in-progress -> completed/failed).
3) SessionOperational aguarda estado canonicamente fechado antes de liberar unload.
4) DeactivationWindow transitoria nao libera unload por si.
5) Handoff pendente bloqueia route-exit pre-unload.
```

Boundary de ownership mantido:

```text
SessionOperationalPipeline decide ordem da rota e autorizacao de unload.
SessionActivityPipeline decide lifecycle local de fechamento.
SceneComposition executa side-effect fisico, sem decidir lifecycle.
```

### Checkpoint CLOSED - ActivityRouteExitRail / BackToMenu ordering (2026-05-19)

Status formal:

```text
CLOSED
```

Contrato congelado:

```text
Se existe SessionActivity ativa, BackToMenu/route-exit deve deferir a troca de rota
antes de qualquer side-effect operacional da rota.
```

Sequencia obrigatoria:

```text
1) OperationalRouteRequestDeferredForSessionActivityTeardown
2) SessionActivityRouteExitTeardownStarted
3) ActivityRouteExitRail local:
   ActivityRouteExitRequested
   -> ActivityCompleting
   -> DeactivationWindowStarted
   -> DeactivationWindowReady
4) DeactivationWindowReady aguarda comando explicito (sem auto-complete)
5) CompleteDeactivationWindow (QA no sandbox; botao real no futuro)
6) DeactivationWindowCompleted
   -> DeactivationWindowAdditiveSceneUnloadStarted
   -> DeactivationWindowAdditiveSceneUnloaded
   -> ActivityDeactivated
   -> ActivityRouteExitCompleted
   -> ClosedForRouteExit
7) SessionActivityRouteExitTeardownCompleted kind=Completed stage=ClosedForRouteExit hasPendingHandoff=false
8) So depois iniciar side-effects operacionais da rota:
   - ActivityCameraReleasePreviousStage
   - RouteActivitySave save-on-exit
   - TransitionPlanReady
   - loading/fade
   - ApplyOperationalRoute
   - SceneComposition unload/load
   - route reveal/fadeOut
```

Regras de fronteira:

```text
Planejamento puro antes do defer e aceitavel.
Adapter/side-effect operacional antes de ClosedForRouteExit nao e aceitavel.
```

Smoke congelado:

```text
OperationalRouteRequestDeferredForSessionActivityTeardown ocorre antes de:
- ActivityCameraReleasePreviousStageStarted
- RouteActivitySaveSaveStarted/Skipped
- TransitionPlanReady
- fadeInStarted
- ApplyOperationalRoute

SessionActivityRouteExitTeardownCompleted kind=Completed stage=ClosedForRouteExit hasPendingHandoff=false
ocorre antes dos mesmos side-effects.

BackToMenu nao abre activity_02.
ActivationWindow/DeactivationWindow continuam dependentes de comando explicito.
```

### 2026-05-21 - Checkpoint CLOSED - Route Request Submission / Preflight Safety

Status formal:

```text
Route Request Submission / Preflight Safety - CLOSED / PASS
```

Contrato congelado:

```text
Route Button / UI
-> submete intencao imediata
-> SessionOperationalPipeline faz preflight antes de qualquer plano/comando operacional
-> se rejeitado por policy, nenhum lifecycle operacional nasce
-> se aceito, SessionOperationalPipeline assume a operacao de rota
-> completion operacional e reportada por sinal explicito
```

Ownership congelado:

```text
SessionOperationalPipeline decide policy, aceite, rejeicao, lifecycle e completion da rota.
Frontend/UI/Binder apenas submete intencao e observa resultado de submissao/completion.
Adapters executam side-effects somente depois que o pipeline aceitar a rota.
```

Regras obrigatorias:

```text
1) UI/Binder nao decide lifecycle de rota.
2) UI/Binder nao aguarda lifecycle por async/await, ContinueWith ou inspecao posterior de Task.
3) SubmitRouteRequest representa submissao/preflight, nao conclusao da rota.
4) RejectedByPolicy e resultado valido de submissao e nao e falha fatal.
5) IgnoredAlreadyInFlight e resultado valido quando ja existe operacao ativa.
6) FailedInvalidConfig permanece falha explicita de contrato/config.
7) Accepted inicia operacao controlada pelo SessionOperationalPipeline.
8) Operacao aceita reabilita UI apenas por completion signal explicito.
```

Preflight obrigatorio antes de qualquer plano/comando operacional:

```text
TryPreflightRouteRequest
-> RouteRequestSubmissionResult
```

Quando a request e rejeitada por policy, o pipeline deve emitir observabilidade explicita e **nao** deve criar:

```text
LoadingPlanReady
RouteAudioPlanReady
OperationalRouteCommand
RouteActivitySavePlanReady
TransitionPlanReady
OperationalRouteRequestDeferredForSessionActivityTeardown
```

Bloqueios canonicos de policy:

```text
ActivationWindowReady
-> route request rejeitada com activation_window_not_completed

ActivityCompletionRail / DeactivationWindowReady nao route-exit-owned
-> route request rejeitada com activity_transition_in_progress

Operacao de rota ja ativa
-> route request ignorada como already_in_flight, sem criar trilho paralelo
```

Route-exit permitido:

```text
ActivityRunning
-> route request aceita
-> OperationalRouteRequestDeferredForSessionActivityTeardown
-> SessionActivityRouteExitTeardownStarted
-> ActivityRouteExitRail
-> ClosedForRouteExit
-> SessionActivityRouteExitTeardownCompleted
-> side-effects operacionais da nova rota
```

Invariantes:

```text
1) Rejeicao de policy ocorre antes de qualquer side-effect operacional.
2) Nao existe route transition parcialmente planejada quando a SessionActivity bloqueia preflight.
3) BackToMenu em ActivationWindowReady nao deve iniciar teardown, fade, loading, save, audio ou SceneComposition.
4) BackToMenu durante Activity -> Activity local nao deve sequestrar o rail local nem descarregar a route scene.
5) SceneComposition unload da route scene anterior so pode ocorrer depois de ClosedForRouteExit.
6) Eventos/completions foreign/stale nao podem alterar a operacao de rota ativa.
```

Smoke congelado:

```text
Caso A:
ActivationWindowReady
-> BackToMenu
-> RouteRequestBlockedBySessionActivity reason='activation_window_not_completed'
-> RouteRequestRejectedByPolicy
-> sem planos/comandos operacionais

Caso B:
DeactivationWindowReady em ActivityCompletionRail
-> BackToMenu
-> RouteRequestBlockedBySessionActivity reason='activity_transition_in_progress'
-> RouteRequestRejectedByPolicy
-> ActivityTransition local continua
-> SessionActivitySandboxScene nao e descarregada

Caso C:
ActivityRunning
-> BackToMenu
-> OperationalRouteRequestDeferredForSessionActivityTeardown
-> SessionActivityRouteExitTeardownCompleted stage='ClosedForRouteExit'
-> ActivityCamera release
-> RouteActivitySave save-on-exit
-> ApplyOperationalRoute
-> OperationalRouteCompleted routeIdentity='route-boot-menu'
```

Resultado aceito:

```text
Sem RouteButtonResultInspectionFailed.
Sem erro de main thread por manipulacao Unity fora do contexto principal.
Sem route_transition_failed para rejeicao de policy.
Sem [FATAL] em bloqueio operacional esperado.
Sem SessionActivityRouteExitWithoutCanonicalDeactivation.
```
