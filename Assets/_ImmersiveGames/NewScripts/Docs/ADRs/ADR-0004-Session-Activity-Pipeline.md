# ADR-0004 - Session Activity Pipeline

## Status
- Estado: Accepted / Living Canonical Checkpoint
- Data inicial: 2026-05-12
- Última atualização: 2026-05-17
- Tipo: Direction / Canonical architecture / Base 1.1 checkpoint
- Fonte de verdade canônica deste contrato: este ADR.

## Contexto

A Base 1.0 tratou o engagement com activities como um fluxo localmente operacional, com phases, stages, `IntroStage`, result presentation, shortcuts de QA e closure distribuídos entre módulos diferentes.

Na Base 1.1, dentro da estratégia de **Pipeline Convergence / Convergência para Pipelines Determinísticos**, o ciclo interno de uma activity passa a pertencer ao `SessionActivityPipeline`.

Este ADR consolida o contrato atual e o shape alvo para:

- lifecycle local da `SessionActivity`;
- `ActivationWindow` e `DeactivationWindow`;
- troca Activity -> Activity;
- route-exit teardown;
- ActivitySetup / ObjectEntry;
- ActivitySceneContract;
- retention/release;
- fronteiras com `SessionOperationalPipeline`, `SceneComposition`, `SaveRuntime`, QA e runtime spawn futuro.

## Decisão

### 1. Session Activity Pipeline

Adota-se o `SessionActivityPipeline` como owner do lifecycle local de activity.

#### Responsabilidades do `SessionActivityPipeline`

- Receber entrada canônica via `SessionActivityEntryHandoff`.
- Resolver a activity inicial a partir do catálogo.
- Controlar `ActivitySetup`, `ActivationWindow`, `ActivityRunning`, `DeactivationWindow`, `ActivityTransition`, `ActivityRetention` e `ActivityRelease`.
- Produzir `Pipeline Fact`, `Pipeline Snapshot`, `Pipeline Command` e `Pipeline Handoff` com `Pipeline Identity` explícita.
- Rejeitar comandos/eventos `foreign/stale`.
- Preparar ou rejeitar handoff para próxima activity.
- Fechar activity ativa por route-exit sem preparar próxima activity.

#### Não responsabilidades

O `SessionActivityPipeline` não é owner de:

- rota operacional;
- scene composition física de rota;
- loading/fade de rota;
- save progression direto;
- Run-level deactivation/continuity;
- gameplay interno da activity;
- decisão concreta de objetivos/recompensas/branch narrativa;
- side-effects Unity diretos.

Esses pontos pertencem respectivamente a `SessionOperationalPipeline`, adapters, `RunPipeline`, domínios de gameplay ou `SaveRuntime`.

### 2. Fronteira com SessionOperationalPipeline

O `SessionOperationalPipeline` é owner da ordem de rota, transição operacional, scene composition operacional, loading/fade de rota e handoff inicial para a `SessionActivity`.

O `SessionActivityPipeline` é owner do lifecycle local da activity após o handoff.

A entrada normal em runtime ocorre apenas por:

```text
SessionOperationalPipeline
-> SessionActivityEntryHandoff
-> SessionActivityPipeline.StartFromPreparedHandoff
```

`SessionActivityHost`, debug panels e binders locais não são owners de lifecycle.

### 3. Fronteira com SceneComposition

`SceneComposition` executa load/unload/set-active de cenas comandados por pipelines. Ele não decide lifecycle de activity.

Antes de descarregar uma rota/cena que contém `SessionActivity` ativa, o `SessionOperationalPipeline` deve consultar/acionar o boundary explícito de route-exit teardown da `SessionActivity`.

A ordem obrigatória para route-exit é:

```text
RouteExitRequested
-> SessionActivityRouteExitTeardownStarted
-> CloseForRouteExit
-> ActivityRouteExitCompleted / ClosedForRouteExit
-> SessionActivityRouteExitTeardownCompleted
-> ApplyOperationalRouteAsync
-> SceneComposition unload
```

Nunca é válido iniciar `UnloadSceneStarted` de cena com activity ativa sem fechamento canônico prévio.

### 4. Lifecycle local canônico

O ciclo local mínimo da activity é:

```text
SessionActivityEntryHandoffAccepted
-> ActivitySetup
-> ActivityActivationStarted
-> ActivationWindow
-> ActivityRunning
-> ActivityCompletionRequested / ActivityNavigationExitRequested / ActivityRouteExitRequested
-> ActivityCompleting
-> DeactivationWindow
-> ActivityDeactivated
-> ActivityTransition / ClosedForRouteExit / Completed
```

#### Entrada nominal atual

```text
SessionActivityEntryHandoffAccepted
-> ActivitySetupStarted
-> ActivitySetupSkippedNoContent
-> ActivitySetupCompleted
-> ActivityActivationStarted
-> ActivationWindowStarted
-> ActivationWindowSkippedNoContent ou ActivationWindowReady/Completed
-> ActivityRunning
```

#### Saída nominal atual

```text
ActivityRunning
-> ActivityCompletionRequested ou ActivityNavigationExitRequested ou ActivityRouteExitRequested
-> ActivityCompleting
-> DeactivationWindowStarted
-> DeactivationWindowSkippedNoContent ou DeactivationWindowReady/Completed
-> ActivityDeactivated
```

### 5. ActivationWindow

`ActivationWindow` substitui o papel antigo de `IntroStage` como janela autoral de entrada.

Ela serve para conteúdo visível/autorável antes de liberar a activity, como:

- disclaimer;
- splash;
- cutscene;
- prompt “aperte para começar”;
- introdução local da activity.

`ActivationWindow` não é setup técnico.

#### Modos

```text
ActivityWindowMode.None
ActivityWindowMode.AdditiveScene
```

#### None

```text
ActivationWindowStarted
-> ActivationWindowSkippedNoContent
-> ActivityRunning
```

#### AdditiveScene

```text
ActivationWindowStarted
-> ActivationWindowAdditiveSceneLoadStarted
-> ActivationWindowAdditiveSceneLoaded
-> ActivationWindowReady
-> CompleteActivationWindow
-> ActivationWindowCompleted
-> ActivationWindowAdditiveSceneUnloadStarted
-> ActivationWindowAdditiveSceneUnloaded
-> ActivityRunning
```

Regras:

- Cena da window deve ser referenciada por `SceneKeyAsset`, não string livre.
- `CompleteActivationWindow` é comando explícito.
- `ActivityRunning` só pode ocorrer depois de `ActivationWindowCompleted` e unload da cena quando `AdditiveScene`.
- `AdditiveScene` sem `SceneKeyAsset` válido é fail-fast.
- Não há fallback silencioso para skip/no-content.

### 6. DeactivationWindow

`DeactivationWindow` substitui o papel antigo de result/presentation/post-run local da activity.

Ela serve para conteúdo visível/autorável antes de sair da activity, como:

- resultado local;
- tela de conclusão;
- cutscene de saída;
- post-run local;
- confirmação antes de avançar.

`DeactivationWindow` não é release técnico.

#### None

```text
DeactivationWindowStarted
-> DeactivationWindowSkippedNoContent
-> ActivityDeactivated
```

#### AdditiveScene

```text
DeactivationWindowStarted
-> DeactivationWindowAdditiveSceneLoadStarted
-> DeactivationWindowAdditiveSceneLoaded
-> DeactivationWindowReady
-> CompleteDeactivationWindow
-> DeactivationWindowCompleted
-> DeactivationWindowAdditiveSceneUnloadStarted
-> DeactivationWindowAdditiveSceneUnloaded
-> ActivityDeactivated
```

Regras:

- Cena da window deve ser referenciada por `SceneKeyAsset`.
- `CompleteDeactivationWindow` é comando explícito.
- `ActivityDeactivated` só pode ocorrer depois de `DeactivationWindowCompleted` e unload da cena quando `AdditiveScene`.
- `AdditiveScene` sem `SceneKeyAsset` válido é fail-fast.
- Não há fallback silencioso para skip/no-content.

### 7. Removal de trilhos antigos

O contrato ativo remove ou proíbe retorno de:

- `HasActivation`;
- `HasActivityResult`;
- `ActivationEntered`;
- `ActivationSkippedNoContent` antigo;
- `ActivationExecuting` antigo;
- `ActivityResultPresentation*`;
- `IntroStage` como owner de lifecycle;
- `DebugStartActivity` como entrada de produção;
- `autoStart` como entrada de produção.

`IntroStage`, se citado historicamente, deve ser lido apenas como antecedente conceitual de `ActivationWindow`/`Pipeline Policy`, nunca como owner ativo.

### 8. CompleteCurrentActivity vs CloseForRouteExit

Há dois caminhos semanticamente distintos.

#### CompleteCurrentActivity

Fecha a activity dentro do catálogo local e pode preparar próxima activity.

```text
ActivityCompletionRequested
-> ActivityCompleting
-> DeactivationWindow
-> ActivityDeactivated
-> ActivityTransitionPolicy/Profile resolved
-> ActivityHandoffPrepared, se houver next activity
```

#### CloseForRouteExit

Fecha a activity porque a rota está saindo. Não prepara próxima activity.

```text
ActivityRouteExitRequested
-> ActivityCompleting
-> DeactivationWindow
-> ActivityDeactivated
-> ActivityRouteExitCompleted
-> ClosedForRouteExit
```

Regras:

- `CloseForRouteExit` não emite `ActivityTransitionPolicySelected`.
- `CloseForRouteExit` não emite `ActivityHandoffPrepared`.
- `CloseForRouteExit` não chama `ContinueToNextActivity`.
- `CloseForRouteExit` deve terminar sem `CurrentHandoff` pendente.
- `SessionOperationalPipeline` bloqueia route-exit se houver handoff pendente antes do unload.

### 9. QA canônico

O QA principal deve validar o lifecycle, não criar atalhos.

Botões/caminhos válidos:

- `DumpState` / `Trace`;
- `CompleteActivationWindow`;
- `CompleteCurrentActivity`;
- `CompleteDeactivationWindow`;
- `ContinueToNextActivity`, apenas quando houver handoff preparado após `ActivityDeactivated`;
- `RequestPause` / `RequestResume`, se não criarem bypass.

Devem permanecer removidos/bloqueados:

- `DebugStartActivity` em produção;
- `GoToNextActivity` direto;
- `GoToPreviousActivity` direto;
- `GoToActivity` direto;
- `RestartCurrentActivity` direto;
- qualquer atalho que faça `ActivityRunning -> ActivityDeactivated -> NextActivity` sem `DeactivationWindow`.

### 10. ActivityTransition

`ActivityTransition` é o bloco de troca Activity -> Activity dentro da mesma `SessionActivity`.

Ele pertence ao `SessionActivityPipeline`, não ao `SessionOperationalPipeline`.

#### Separação

```text
Route Transition
owner: SessionOperationalPipeline
uso: troca de rota/cena operacional
```

```text
Activity Transition
owner: SessionActivityPipeline
uso: troca Activity -> Activity dentro da SessionActivity
```

A `ActivityTransition` pode reutilizar perfis técnicos de rota, como fade/loading profiles, mas a decisão de uso pertence à `SessionActivity`.

#### Regras de profile

- Override específico da Activity vence herança da rota.
- Herança de profile da rota deve ser explícita e observável.
- Ausência de profile obrigatório para `CutWithCurtain` é fail-fast.
- `Seamless` permanece futuro/unsupported até contrato próprio.
- `None` representa troca seca intencional, não ausência de configuração.

#### Sequência conceitual para CutWithCurtain

```text
ActivityDeactivated
-> ActivityTransitionProfileSelected
-> ActivityTransitionProfileResolved
-> ActivityTransitionStarted
-> ActivityTransitionFadeInStarted
-> ActivityTransitionFadeInCompleted
-> NextActivitySetupStarted
-> NextActivitySetupSkippedNoContent ou NextActivitySetupCompleted
-> ActivityHandoffPrepared
-> ContinueToNextActivity
-> next ActivitySetup
-> next ActivationWindow
-> reveal-safe point
-> ActivityTransitionFadeOutStarted
-> ActivityTransitionFadeOutCompleted
-> ActivityTransitionCompleted
```

Reveal-safe point:

- se `ActivationWindowMode=None`: `ActivityRunning`;
- se `ActivationWindowMode=AdditiveScene`: `ActivationWindowReady`, antes de `CompleteActivationWindow`.

### 11. ActivitySetup

`ActivitySetup` é o estágio que prepara a activity antes de ela ser revelada ou antes de rodar.

Ele não é `ActivationWindow`.

#### Responsabilidades

- Coletar contributors declarados pela Activity.
- Coletar contributors descobertos em cenas carregadas.
- Montar `ActivitySetupPlan`.
- Emitir `Pipeline Commands`.
- Aguardar `Pipeline Facts`.
- Validar readiness.
- Bloquear reveal/running quando houver requirements obrigatórios.

#### MVP Base 1.1

O MVP cria o slot determinístico, mas pode ser nominal:

```text
ActivitySetupStarted
-> ActivitySetupSkippedNoContent
-> ActivitySetupCompleted
```

A ausência de conteúdo real deve gerar fact explícito de `skip/no-content`, nunca omissão silenciosa.

### 12. ActivitySceneContract

Cenas de activity podem conter objetos/contributors que só são conhecidos depois do load.

Para manter determinismo, cenas devem poder declarar um contrato explícito:

```text
ActivitySceneContract
- sceneKey
- contractId
- discoveryMode
- safeForSeamlessReveal
- expectedRequiredContributors
- allowOptionalExtras
- requiredBeforeRevealTags
- requiredBeforeRunningTags
```

#### Discovery modes

```text
Strict
AllowOptionalExtras
Open
```

Regras:

- A cena pode esconder complexidade autoral, mas não pode esconder contrato.
- Só participa do pipeline quem tiver marker/contributor explícito.
- O pipeline compara inventário declarado vs contributors descobertos.
- Missing required contributor é fail-fast.
- Extras só são aceitos conforme `discoveryMode`.

### 13. ActivitySetupContributor

Objetos, assets e domínios podem contribuir requirements para setup.

Fontes possíveis:

- `ActivityAsset` / `ActivityCatalogAsset`;
- `ActivitySceneContract`;
- objetos presentes na cena;
- definitions de actors/NPCs/props;
- spawners;
- HUD local;
- runtime objects;
- save/progression futuramente.

O contributor não executa lifecycle. Ele declara requirements.

### 14. ActivitySetupRequirement

Um requirement descreve uma necessidade de preparação.

Campos conceituais:

```text
requirementId
ownerObjectId
kind
requiredness
deadline
dependencies
command
```

Deadlines iniciais:

```text
RequiredBeforeReveal
RequiredBeforeRunning
RequiredBeforeActivation
WarmupOnly
Optional
Lazy
```

Kinds conceituais:

```text
ResourceWarmup
PoolWarmup
Materialization
StateReset
Placement
HudBinding
InteractionBinding
CameraBinding
Activation
Release
```

### 15. ObjectEntry

Todo objeto que entra no jogo deve passar por um trilho homogêneo.

Isso vale para:

- objeto inicial da activity;
- objeto presente em cena carregada;
- objeto vindo de spawn durante gameplay;
- objeto restaurado de save futuramente.

Sequência conceitual:

```text
ObjectEntryRequested
-> ObjectMaterializationStarted
-> ObjectMaterialized
-> ObjectSetupStarted
-> ObjectSetupCompleted
-> ObjectReady
-> ObjectActivated
```

A diferença entre ActivitySetup e runtime spawn não é o contrato do objeto, mas o que fica bloqueado:

```text
ActivitySetup bloqueia reveal/running da Activity.
RuntimeSpawn bloqueia ativação do objeto.
```

### 16. Preload / Warmup / Setup / Activation

Preload não significa instanciar tudo.

Separação canônica:

```text
Preload/Warmup
-> prepara capacidade: assets, pools, HUD local, cenas auxiliares
```

```text
Setup
-> prepara instância: materializar, resetar, posicionar, bindar
```

```text
Activation
-> coloca no gameplay: habilita visual/interação/IA/input
```

Regra curta:

```text
Preload prepara capacidade.
Setup prepara instância.
Activation coloca no gameplay.
```

### 17. ActivityRetentionPolicy

`Deactivation` não implica `Release`.

Uma activity pode deixar de ser a activity atual e ainda permanecer retida/suspensa por policy.

Estados conceituais:

```text
Active
Deactivated
Retained
Suspended
ReleasePending
Released
```

Policies conceituais:

```text
ReleaseImmediately
RetainPreviousCount
ReleaseAfterDistance
RetainUntilRouteExit
ManualRelease
Budgeted
```

MVP Base 1.1 pode implementar apenas comportamento nominal ou `ReleaseImmediately`, mas o contrato precisa reconhecer que deactivation e release são conceitos diferentes.

### 18. ActivityRelease / ObjectRelease

Tudo que entra precisa de saída.

Activity release conceitual:

```text
ActivityReleaseStarted
-> ActivityReleasePlanResolved
-> ObjectReleaseStarted
-> ObjectReleased
-> ActivityReleased
```

Object release conceitual:

```text
ObjectDeactivation
-> UnbindHud
-> UnregisterInteraction
-> StopAI
-> ClearRuntimeSubscriptions
-> ReturnToPool ou Destroy
-> ObjectReleased
```

Regras:

- `ActivityRelease` limpa recursos/objetos/binds/cenas da activity.
- `ActivityDeactivated` apenas tira a activity do foco atual.
- Activities retidas devem estar suspensas ou protegidas por identity.
- Route-exit força release/teardown de tudo que não for explicitamente session-owned, route-owned ou persistente por outro contrato.

### 19. Ownership de objetos

Todo objeto preparado precisa ter ownership explícito.

Scopes conceituais:

```text
ActivityOwned
SessionOwned
RouteOwned
SharedPoolOwned
RuntimeSpawnOwned
```

Cada `ObjectEntryPlan` deve carregar:

```text
ownerScope
ownerId
releasePolicy
objectEntryId
objectDefinitionId
activityId
sourceKind
sourceId
```

Isso evita liberar HUD/pool/recurso compartilhado que ainda pertence à sessão ou a outra activity.

### 20. RuntimeSpawn futuro

Runtime spawn deve usar o mesmo `ObjectEntryPlan` de ActivitySetup.

Não deve existir um caminho paralelo para criar objetos durante gameplay.

Diferença de policy:

```text
ActivitySetupPolicy
-> bloqueia reveal/running da Activity
```

```text
RuntimeSpawnPolicy
-> bloqueia ativação do objeto spawnado
```

### 21. Conteúdo random/procedural

Conteúdo random/procedural é permitido, mas deve ser determinístico quando necessário.

Regras:

- Contributor procedural recebe seed/contexto.
- Contributor procedural emite plano resolvido.
- O plano resolvido é observável.
- Instanciação direta fora do pipeline é proibida para conteúdo relevante.

### 22. Seamless futuro

`Seamless` não é uma troca frouxa. É mais rígida.

Para uma Activity/cena ser usada sem transição visual, ela precisa provar:

```text
safeForSeamlessReveal = true
RequiredBeforeReveal resolvido
nenhum objeto visualmente incompleto será exposto
objetos incompletos nascem hidden/inactive até ObjectReady
```

Se isso não for provado, `Seamless` deve falhar explicitamente como unsupported/fail-fast.

### 23. Save / Progression boundary

`SessionActivityPipeline` não salva progression diretamente.

Snapshot de activity para `RouteActivitySave` deve vir de provider explícito futuro, como `IProgressionSnapshotProvider` ou contrato equivalente.

`SessionOperationalPipeline` continua owner do timing operacional de load/save de rota/activity.

`SaveRuntime` executa persistência comandada por pipelines/adapters; não decide activity lifecycle.

### 24. MVP Base 1.1

O MVP Base 1.1 deste ADR é deliberadamente menor que a arquitetura-alvo.

#### Inclui

- Lifecycle local fechado com `ActivationWindow` e `DeactivationWindow`.
- `None` e `AdditiveScene` para windows.
- Comandos explícitos `CompleteActivationWindow` e `CompleteDeactivationWindow`.
- `CloseForRouteExit` sem handoff para próxima activity.
- QA canônico sem atalhos de lifecycle.
- Route-exit teardown antes de unload operacional.
- `ActivityTransitionPolicy/Profile` como contrato de Activity -> Activity, com `CutWithCurtain` nominal/visual conforme implementação validada e `Seamless` unsupported explícito.
- `ActivitySetup` nominal com `skip/no-content`.
- `NextActivitySetup` nominal durante ActivityTransition.
- `ActivitySceneContract` como contrato/shape.
- `ObjectEntry` como contrato/shape.
- `ActivityRetentionPolicy` como contrato/shape.
- `ActivityRelease/ObjectRelease` como contrato/shape.

#### Fora do MVP

- Budgeted preload.
- Seamless real.
- Random/procedural setup completo.
- RuntimeSpawn real usando ObjectEntry.
- Save restore de objetos.
- ObjectRelease real completo.
- Retenção real de múltiplas activities.
- Dependências complexas entre requirements.
- HUD binding real.
- NPC materialization real.
- Pool warmup real.
- ActivitySetup com contributors reais.

### 25. Invariantes obrigatórios

- `SessionActivityPipeline` decide lifecycle local.
- `SessionOperationalPipeline` decide ordem de rota/unload/handoff operacional.
- `SceneComposition` executa scene changes, não lifecycle.
- `ActivityAsset`/`ActivityCatalogAsset` definem dados autorais, não lifecycle.
- Objetos/domínios contribuem requirements, não avançam pipeline.
- Policies decidem estratégia/bloqueio, não conteúdo concreto de gameplay.
- Adapters executam side-effects comandados.
- Facts confirmam readiness.
- Toda etapa relevante carrega `Pipeline Identity`.
- `foreign/stale events` não podem alterar activity ativa.
- Nenhuma ausência obrigatória vira fallback silencioso.
- Deactivation não implica Release.
- Conteúdo declarado e conteúdo descoberto convergem para o mesmo ActivitySetup/ObjectEntry pipeline.

## Consequências

- `SessionActivity` deixa de ser apenas uma troca local de catálogo e passa a ser o owner determinístico do ciclo interno de activities.
- Janelas autorais visíveis são separadas de setup técnico.
- Route-exit deixa de matar activity por unload de cena e passa por fechamento canônico.
- O futuro sistema de objects/spawn/save pode convergir em `ObjectEntryPlan` sem criar trilhos paralelos.
- Retention/release ficam previstos sem obrigar implementação pesada no MVP.
- `Seamless` futuro exige authoring e contrato mais rigorosos, não menos.

## Relação com Base 1.0 e Base 2.0

- Base 1.0 é histórico de leitura phase-owned de `IntroStage`, engagement disperso, result presentation local e ownership fragmentado.
- Base 1.1 converte essas peças em lifecycle explícito do `SessionActivityPipeline`.
- Base 1.1 não cria um core genérico universal agora; ela dá shape final aos fluxos concretos atuais.
- Base 2.0 futura pode extrair `ObjectEntry`, `ActivitySetup`, retention e release como abstrações mais gerais se a Base 1.1 provar o fluxo.
