# ADR-0014 — ActivityContent, WindowTemplateLibrary e ActivityEntryPipeline

## Status

- Estado: ACEITO / checkpoint normativo vivo da Base 1.1
- Data: 2026-05-19
- Tipo: Direction / Canonical architecture / Base 1.1 checkpoint
- Fonte de verdade canônica deste contrato: este ADR, após aceite.

---

## Contexto

A Base 1.1 — Pipeline Convergence / Convergência para Pipelines Determinísticos — já materializou o `SessionOperationalPipeline` e o `SessionActivityPipeline` como owners separados de rota e ciclo local de activity.

Até este ponto, o projeto estabilizou:

- entrada de `SessionActivity` por `SessionActivityEntryHandoff`;
- `ActivationWindow` e `DeactivationWindow` com comando explícito;
- `ActivityTransition` local Activity -> Activity;
- `ActivityRouteExitRail` antes de qualquer side-effect operacional de troca de rota;
- nascimento mínimo de `PlayerActor` no `ActivitySetup`;
- `PlayerActorReset` v0;
- fronteira entre `SessionOperationalPipeline`, `SessionActivityPipeline`, adapters e executores técnicos.

O próximo problema arquitetural é separar definitivamente:

```text
Activity
ActivityContent
ActivationWindow
DeactivationWindow
Route Scene
WindowTemplateLibrary
ActivityEntryPipeline
ActivitySetupInventory
```

Sem essa separação, há risco de regressão para:

- tratar Activity como cena;
- misturar window scenes com conteúdo jogável;
- carregar todo o catálogo de activities da rota;
- fazer setup especial para a primeira Activity;
- duplicar templates de janela;
- destruir objetos que deveriam voltar ao pool;
- criar branches paralelos para activities “com player”, “sem player”, “com NPC”, “sem NPC”;
- aplicar reset/release/save como se fossem o mesmo conceito.

Este ADR consolida o shape canônico para `ActivityContent`, `WindowTemplateLibrary`, `ActivityEntryPipeline`, `ActivitySetupInventory`, reset, retention/release, pooling e restart de Activity.

---

## Decisão

A Base 1.1 adota um pipeline único de entrada de Activity:

```text
ActivityEntryPipeline
```

Toda Activity passa pelo mesmo pipeline. O que varia entre Activities é o `ActivitySetupInventory` resolvido, não o lifecycle.

A Base 1.1 separa:

```text
Activity = lifecycle.
ActivityContent = conteúdo jogável/material usado pela Activity.
ActivationWindow = janela autoral de entrada.
DeactivationWindow = janela autoral de saída.
Route Scene = superfície/base da rota.
WindowTemplateLibrary = capacidade visual compartilhada route-scoped para janelas.
```

O `SessionActivityPipeline` é owner do lifecycle local da Activity, do `ActivityEntryPipeline`, do `ActivityContent`, das windows autorais e do reset/release de conteúdo Activity-owned.

O `SessionOperationalPipeline` continua owner da rota, da `Route Scene`, da transição operacional de rota, do fade/loading de rota, da `SceneComposition` de rota e da disponibilidade route-scoped da `WindowTemplateLibrary` quando a rota declarar essa capacidade.

Adapters executam side-effects comandados. Objetos, módulos e contributors produzem requisitos, facts, commands ou capacidades; não decidem lifecycle.

---

## 1. Separação conceitual

### 1.1 Activity

`Activity` é lifecycle determinístico local:

```text
setup
-> activation
-> running
-> deactivation
-> transition/release
```

A Activity não é uma cena.
A Activity não é seu conteúdo.
A Activity não é sua window de ativação/desativação.

### 1.2 ActivityContent

`ActivityContent` é o conteúdo jogável/material usado pela Activity.

Pode incluir:

- cenas additive de conteúdo;
- markers;
- props;
- objetos interativos;
- actors não-player;
- object entries;
- contributors;
- pontos de placement;
- rig/camera profile da Activity;
- contratos locais de cena;
- dados de discovery/setup.

`ActivityContent` é específico por Activity.

### 1.3 ActivationWindow

`ActivationWindow` é janela autoral de entrada.

Pode representar:

- introdução;
- tutorial local;
- disclaimer;
- prompt “aperte para começar”;
- apresentação narrativa;
- confirmação antes de liberar gameplay.

Ela não é setup técnico e não auto-completa.

### 1.4 DeactivationWindow

`DeactivationWindow` é janela autoral de saída.

Pode representar:

- resultado local;
- recompensa;
- resumo;
- cutscene de saída;
- confirmação;
- post-run local da Activity.

Ela não é release técnico e não auto-completa.

### 1.5 Route Scene

`Route Scene` é a superfície/base da rota.

Ela pertence ao domínio operacional da rota e é carregada pelo `SessionOperationalPipeline` via adapters/SceneComposition.

A `SessionActivityPipeline` não descarrega a `Route Scene`.

---

## 2. ActivityContentProfile

`ActivityContent` não deve ser modelado apenas como `SceneKeyAsset[]`.

A Base 1.1 adota o conceito:

```text
ActivityContentProfile
```

Campos conceituais:

```text
contentIdentity
contentSceneKeys[]
activityCameraProfile / camera requirement
scene contract metadata
contributor discovery policy
loading/preparation metadata
```

Exemplo:

```text
Activity 1-1
  contentScenes = [World1_1_Ground, World1_1_Props, World1_1_Markers]

Activity 1-2
  contentScenes = [World1_2_Caves, World1_2_Enemies, World1_2_Markers]
```

As cenas de `ActivityContent` são específicas da Activity. Elas não são templates comuns da rota.

---

## 3. ActivityContentScenes

### 3.1 Regras

`ActivityContentScenes` são sempre additive sobre a `Route Scene`.

Regras obrigatórias:

```text
LoadSceneMode.Additive
não SetActiveScene
não substituir Route Scene
não usar WindowSceneAdapter semanticamente
não usar fallback silencioso para Route Scene
```

### 3.2 Stage obrigatório

Toda Activity passa por:

```text
Load/PrepareActivityContent
```

Se a Activity não tiver conteúdo próprio:

```text
ActivityContentLoadSkippedNoContent
```

Se tiver content scenes:

```text
LoadActivityContentScene Command
-> ActivityContentSceneAdapter
-> LoadSceneAsync Additive
-> ActivityContentSceneLoaded
```

### 3.3 Identidade

Toda operação de conteúdo deve carregar identidade suficiente:

```text
pipelineId
sessionStateId
activityId
activityOrdinal
entrySequence
contentProfileId
sceneKey
sceneName
operationId
```

Chave crítica:

```text
activityId + entrySequence
```

Completion stale/foreign não pode alterar o pipeline ativo.

### 3.4 Ordem

`ActivityContent` precisa estar pronto antes de:

- contributor discovery;
- `ActivitySetupInventory`;
- placement;
- camera binding;
- activation window presentation;
- `ActivityRunning`.

---

## 4. ActivityCatalog e carregamento

Uma rota pode apontar para um `ActivityCatalog`.

Isso não significa carregar todas as Activities do catálogo.

```text
ActivityCatalog = índice autoral de Activities.
Não é pacote de cenas a carregar integralmente.
```

Exemplo:

```text
Route World_1_X
  ActivityCatalog:
    1-1
    1-2
    1-3
```

Na entrada da rota:

```text
carrega/prepara Activity 1-1
não carrega Activity 1-2
não carrega Activity 1-3
```

Activities seguintes são resolvidas e preparadas pelo `SessionActivityPipeline` durante `ActivityTransition`.

---

## 5. WindowTemplateLibrary route-scoped

A rota pode fornecer uma biblioteca de templates de janela:

```text
WindowTemplateLibrary
  -> ActivationWindowTemplateScene
  -> DeactivationWindowTemplateScene
  -> layouts
  -> animações
  -> camera rigs
  -> variantes visuais
  -> presenters
```

Essas templates são `route-scoped` e podem ser pré-carregadas com a rota.

A Activity não precisa declarar cena exclusiva de window. Ela pode declarar:

```text
templateId
variantId
payload/data
presentation intent
```

Exemplo:

```text
Activity 1-1
  activationTemplate = "IntroShort"
  deactivationTemplate = "ResultCollectibles"

Activity 1-2
  activationTemplate = "IntroDanger"
  deactivationTemplate = "ResultTimeScore"
```

Regra de ownership:

```text
A rota fornece capacidade visual compartilhada para janelas.
A Activity fornece intenção, payload e conteúdo jogável.
O SessionActivityPipeline decide o lifecycle.
```

---

## 6. WindowTemplateLibrary não é descarregada por window close

Encerrar uma `ActivationWindow` ou `DeactivationWindow` não descarrega a template scene da rota.

Fechar uma window significa:

```text
unbind payload
parar animações locais
resetar presenter
resetar câmera local da presentation
desativar layout/view usado
limpar estado local de apresentação
retornar para Standby
```

Fechar uma window não significa:

```text
UnloadScene
Destroy template scene
duplicar template scene
recarregar template scene
```

Estados conceituais:

```text
NotLoaded
Loaded
Prepared
Standby
Presenting
ReadyForExplicitCompletion
Closing
Resetting
Standby
ReleasedOnRouteExit
```

Regra v0:

```text
Uma WindowTemplateLibrary por rota ativa.
Uma ActivationWindow presentation ativa por vez.
Uma DeactivationWindow presentation ativa por vez.
Duplicação de template scene não é permitida.
Comando stale/foreign deve ser rejeitado por identity/presentationId.
```

A template scene só é descarregada no route-exit/liberação da rota.

---

## 7. ActivationWindow e DeactivationWindow

Activation e Deactivation Windows são janelas autorais separadas do `ActivityContent`.

Ambas:

```text
não auto-completam
aguardam comando explícito
podem consumir dados da Route Scene e do ActivityContent
podem usar camera rig/profile próprio
podem usar templates fornecidos pela rota
```

No sandbox, QA simula futuro botão real:

```text
CompleteActivationWindow
CompleteDeactivationWindow
```

A apresentação da window não deve depender de load async tardio. A template/window capability deve estar preparada/standby quando o pipeline precisar apresentá-la.

---

## 8. Prioridade de câmera

Ordem de suplantação, da menor para a maior prioridade:

```text
Camera default
-> Camera da rota
-> Camera da Activity
-> Camera da ActivationWindow
-> Camera da DeactivationWindow
```

Interpretação:

- `Route camera`: câmera base da rota.
- `Activity camera`: suplanta route camera durante a Activity.
- `ActivationWindow camera`: suplanta Activity camera durante a janela de ativação.
- `DeactivationWindow camera`: suplanta Activity camera durante a janela de desativação.

---

## 9. Load/preparation vs apresentação

Separação obrigatória:

```text
load/preparation != apresentação/ativação autoral
```

As cenas/templates de janela podem estar:

```text
Loaded / Prepared / Standby
```

antes de serem apresentadas.

Quando chega a hora da janela:

```text
ActivationWindowStarted
-> ActivationWindowPresented/Ready
```

ou:

```text
DeactivationWindowStarted
-> DeactivationWindowPresented/Ready
```

O load técnico Unity pode ser async, mas deve estar encapsulado:

```text
Pipeline Command
-> PendingOperation
-> Completion validada
-> Pipeline Fact
-> próximo stage
```

A apresentação/ativação autoral deve ser determinística e comandada pelo pipeline.

---

## 10. ActivityEntryPipeline único

Toda Activity passa pelo mesmo `ActivityEntryPipeline`.

A primeira Activity e as seguintes não têm pipelines diferentes.

A diferença é apenas o contexto visual em volta do setup:

```text
Primeira Activity:
  setup ocorre com cortina/loading da rota ainda fechado.

Activities seguintes:
  setup ocorre com cortina/transição local da Activity fechada.
```

Regra:

```text
Toda Activity passa pelo mesmo ActivityEntryPipeline.
O que varia é o ActivitySetupInventory resolvido, não o lifecycle.
```

Não devem existir branches de lifecycle como:

```text
Activity com PlayerActor
Activity sem PlayerActor
Activity com NPC
Activity sem NPC
```

O pipeline deve emitir skip explícito para subplanos vazios.

---

## 11. Player não é propriedade semântica da Activity

`Player` não é atributo estrutural da Activity.

```text
Player = participante/capacidade da rota/sessão.
Activity entry = pode exigir, posicionar, bindar ou resetar esse participante.
```

A Activity não deve depender de um booleano semântico como:

```text
requiresPlayer = true
```

O shape correto é inventário:

```text
ParticipantRequirements
PlacementRequirements
InteractionBindingRequirements
```

Exemplo correto:

```text
Esta Activity precisa de um participante controlável no placementId='start_A'.
```

Não:

```text
Esta Activity tem PlayerActor = true.
```

---

## 12. ActivitySetupInventory

`ActivitySetupInventory` é o contrato que transforma:

```text
ActivityContent
ActivitySceneContract
contributors descobertos
```

em requisitos determinísticos de setup.

Subplanos possíveis:

```text
ParticipantRequirements
ObjectEntryRequirements
SceneContributorRequirements
PlacementRequirements
CameraBindingRequirements
InteractionBindingRequirements
HudBindingRequirements
WarmupRequirements
ReleaseRequirements
StateResetRequirements
```

Regras:

```text
Requisito obrigatório ausente -> fail-fast.
Requisito opcional ausente -> skip explícito.
Subplano vazio -> skip explícito.
```

O inventário é resolvido por entry, não por classe de Activity.

---

## 13. Execução do ActivitySetupInventory

O `ActivityEntryPipeline` é dono da ordem e do lifecycle.

Ele não implementa diretamente regras específicas de player, NPC, HUD, câmera, interaction ou objetos.

Cada `Pipeline Stage` consome o `ActivitySetupInventory` e produz:

```text
Pipeline Fact
Pipeline Command
Skipped
Failed
```

Adapters executam side-effects comandados pelos stages.

Regra de avanço:

```text
Completed -> avança.
Skipped -> avança quando subplano é vazio/opcional.
Failed -> bloqueia/fail-fast.
```

Ordem inicial aceita:

```text
1. ResolveActivityEntry
2. ResolveActivityContentProfile
3. Load/PrepareActivityContent
4. DiscoverActivityContributors
5. BuildActivitySetupInventory
6. ValidateActivitySetupInventory
7. ParticipantSetupStage
   - PlayerActorReadinessValidation
   - PlayerInputBindingStage
   - MovementBindingStage, quando houver requisito de controle/movimento ou binding retido válido
8. ObjectEntrySetupStage
9. PlacementSetupStage
10. CameraBindingSetupStage
11. InteractionBindingSetupStage
12. HudBindingSetupStage
13. WarmupSetupStage
14. ActivitySetupReadinessValidation
15. ActivitySetupCompleted
16. ActivationWindowPresentation ou ActivationWindowSkippedNoContent
17. CompleteActivationWindow, se houver window
18. ActivityRunning
```

Participant/ObjectEntry vêm antes de Placement. Camera vem depois de Placement. Interaction/HUD vêm depois da existência dos objetos. Warmup é sequencial em v0.

`PlayerInputBindingStage` e `MovementBindingStage` fazem parte do setup/preparation do participante controlável. Eles não liberam gameplay control durante `ActivitySetup` ou `ActivationWindow`. A liberação de controle pertence ao lifecycle da Activity e ocorre somente quando o pipeline entra em `ActivityRunning`.

---

## 14. ActivitySceneContract

`ActivitySceneContract` observa o escopo da entry atual:

```text
Route Scene
+ ActivityContentScenes carregadas da entry atual
```

Prioridade semântica:

```text
Activity-specific markers/contributors vêm das ActivityContentScenes.
Route Scene fornece base/superfície/infra da rota.
```

Se a Activity declarou conteúdo obrigatório, não pode haver fallback silencioso para Route Scene.

---

## 15. Activity Reset / Object StateReset

Reset pertence ao `ActivityEntryPipeline` / `ActivitySetup`.

Reset não é:

```text
Release
Restore de Save
Activation
```

Reset é o processo determinístico que retorna participants, objetos e estado transitório da Activity para uma condição válida da entry atual.

Objetos/participants relevantes que mantêm estado runtime devem expor capacidade de reset por contrato explícito.

O `ActivitySetupInventory` resolve:

```text
StateResetRequirements
```

A partir de:

```text
ActivityContent
ActivitySceneContract
contributors descobertos
```

Regras:

```text
SessionActivityPipeline comanda reset.
Stages/adapters/endpoints executam.
Pipeline Facts confirmam completion/failure.
Objetos aplicam seus próprios campos resetáveis.
Objetos não decidem quando resetar.
ResetAll cego é proibido.
Reset obrigatório ausente/falho bloqueia ActivitySetupCompleted.
Reset opcional ausente gera skip explícito.
```

---

## 16. ResetGroups v0

ResetGroups v0:

```text
Placement
ActivityParticipation
TransformState
RuntimeTransient
InteractionState
ObjectiveState
```

`ResetGroup` define o tipo de estado a resetar, não o componente específico.

### Placement

Reseta posicionamento semântico por marker/anchor/spawn.

### ActivityParticipation

Reseta/atualiza participação na Activity atual, `activityId`, `entrySequence` e estado de participação.

### TransformState

Reseta transform autoral/técnico, como posição/rotação/escala local inicial.

### RuntimeTransient

Limpa estado momentâneo da execução atual, como velocity, timers, cooldowns, buffers e flags efêmeras.

### InteractionState

Reseta estado de interação local, como interactable enabled/disabled, trigger armado/desarmado, porta aberta/fechada quando autoralmente resetável.

### ObjectiveState

Reseta estado local de objetivo da Activity, como contador, objetivo concluído/falhado, score local e coleta local.

Grupos avançados ficam fora do v0:

```text
AIState
PhysicsState
AnimationState
HudState
CameraTransient
SaveRestoredState
InventoryState
AudioState
```

Eles serão adicionados quando componentes reais forem migrados/adaptados ao novo shape.

---

## 17. ActivityContent Retention / Release

`Deactivation` não implica `Release`.

```text
ActivityContentRetention
= policy que decide se conteúdo deactivated será liberado, mantido ou retido.
```

```text
ActivityContentRelease
= processo determinístico comandado pelo SessionActivityPipeline para liberar conteúdo ActivityOwned.
```

Release pode incluir:

- object release;
- contributor unregister;
- binding cleanup;
- HUD cleanup;
- camera release;
- interaction release;
- unload de ActivityContentScenes;
- limpeza de `ActivityContentLoadedSet`.

Release não inclui:

- Route Scene;
- WindowTemplateLibrary route-scoped;
- Operational Camera Runtime;
- FadeScene;
- LoadingHudScene;
- InputMode global;
- SaveRuntime backend;
- PlayerSlot.

Policies canônicas:

```text
ReleasePreviousActivityContent
KeepRecentActivityContent(count)
RetainUntilRouteExit
```

v0 runtime suporta:

```text
ReleasePreviousActivityContent
```

Ficam previstos, mas unsupported em v0:

```text
KeepRecentActivityContent(count)
RetainUntilRouteExit
```

---

## 18. ActivityContentRelease no Activity -> Activity

Com `ReleasePreviousActivityContent`, ordem conceitual:

```text
ActivityRunning
-> CompleteCurrentActivity
-> DeactivationWindow
-> CompleteDeactivationWindow, se houver window
-> ActivityDeactivated
-> ActivityTransition curtain/fade in
-> ActivityContentRetentionPolicyResolved
-> PreviousActivityContentReleaseStarted
-> ObjectRelease / BindingRelease / CameraRelease / HudRelease
-> ActivityContentSceneUnloadStarted
-> ActivityContentSceneUnloaded
-> PreviousActivityContentReleased
-> Load/PrepareActivityContent da próxima Activity
-> ActivitySetupInventory
-> ActivitySetupCompleted
-> ActivationWindow da próxima
-> ActivityRunning
```

Release do conteúdo anterior ocorre com a cortina/transição local fechada.

---

## 19. Route-exit e release de ActivityContent

No route-exit, conteúdo ActivityOwned/retained precisa ser liberado antes de declarar fechamento canônico da `SessionActivity`.

Ordem:

```text
BackToMenu / RouteExit
-> ActivityRouteExitRail
-> DeactivationWindow
-> CompleteDeactivationWindow, se houver window
-> ActivityDeactivated
-> ActivityContentReleaseStarted
-> libera conteúdo ActivityOwned/retained
-> ActivityContentReleased
-> ActivityRouteExitCompleted
-> ClosedForRouteExit
-> SessionOperational continua route transition
```

Regra:

```text
ClosedForRouteExit só pode ocorrer depois do release obrigatório de ActivityContent.
```

A `WindowTemplateLibrary` route-scoped não é liberada por `ActivityContentRelease`.

ActivityRouteExitRail fecha presentations e devolve templates para Standby. A liberação/descarregamento da `WindowTemplateLibrary` pertence à rota, quando a rota for liberada.

---

## 20. Pooling canônico como capacidade transversal

O projeto possui um sistema de pooling canônico.

Pooling não é assunto apenas de Release. É capacidade técnica transversal.

Pode aparecer em:

```text
PoolWarmup
ObjectEntry / Materialization
RuntimeSpawn futuro
Reset / reuse
ActivityContentRetention
ActivityContentRelease / ObjectRelease
```

Regras:

```text
Não criar pooling paralelo em SessionActivity.
Não criar pool ad hoc dentro de ActivityContentRelease.
Não destruir por padrão objeto que deveria retornar ao pool.
Quando releasePolicy/ownerScope indicar pool, usar o sistema de pool canônico existente.
```

Policies/ownership conceituais:

```text
ReturnToPool
Destroy
DisableAndRetain
UnregisterOnly
SharedPoolOwned
```

Antes de implementar integração real com pool, deve ser feita auditoria do pooling existente para mapear:

- contratos atuais;
- ownership;
- lifecycle;
- adapters;
- policies corretas;
- como `Pipeline Commands` entram no sistema de pool.

---

## 21. RestartCurrentActivity e ActivityContent

`RestartCurrentActivity` é rail local do `SessionActivityPipeline`.

Regras:

```text
Restart sempre cria nova entrySequence.
Restart não usa pipeline especial.
A nova execução passa pelo mesmo ActivityEntryPipeline.
Restart não é Save/Restore.
```

v0 usa:

```text
ReloadContentOnRestart
```

Fluxo v0:

```text
RestartCurrentActivityRequested
-> RestartCurrentActivityAccepted
-> ActivityRestartTeardownStarted
-> DeactivationWindowStarted ou DeactivationWindowSkippedNoContent
-> CompleteDeactivationWindow, se houver window
-> ActivityDeactivated
-> ActivityContentReleaseStarted da entry antiga
-> ObjectRelease / BindingRelease / CameraRelease
-> ActivityContentSceneUnloadStarted
-> ActivityContentSceneUnloaded
-> ActivityContentReleased
-> ActivityRestartSetupStarted
-> nova entrySequence
-> ResolveActivityEntry
-> ResolveActivityContentProfile
-> Load/PrepareActivityContent
-> DiscoverActivityContributors
-> BuildActivitySetupInventory
-> ValidateActivitySetupInventory
-> execute setup stages
-> ActivitySetupCompleted
-> ActivityActivationStarted
-> ActivationWindowPresented/Ready ou ActivationWindowSkippedNoContent
-> CompleteActivationWindow, se houver window
-> ActivityRunning
-> RestartCurrentActivityCompleted
```

`WindowTemplateLibrary` route-scoped não é descarregada nem duplicada durante restart. Apenas presentations são limpas/resetadas para Standby.

`ResetContentOnRestart` fica previsto como policy futura, mas só pode ser suportado quando `StateResetRequirements` obrigatórios estiverem completos e confiáveis.

---

## 22. Unsupported v0 / Scope guard

v0 suporta conceitualmente:

```text
ActivityContentProfile
Load/PrepareActivityContent obrigatório
ActivityEntryPipeline único
ActivitySetupInventory
StateResetRequirements
ResetGroups v0
ReleasePreviousActivityContent
ReloadContentOnRestart
WindowTemplateLibrary route-scoped
Window presentations em Standby/reset
Pooling canônico como capacidade a auditar antes de integração real
```

v0 não suporta ainda:

```text
KeepRecentActivityContent real
RetainUntilRouteExit real
ResetContentOnRestart real
ref-count de content scenes compartilhadas
content scenes compartilhadas entre entries retidas
seamless real
restore genérico de objetos além do MVP `ActivityObjectSnapshotRestore` validado para `test_object_01`
runtime spawn completo
object release completo com todos os tipos
pool integration real sem auditoria prévia
budgeted retention/preload
manual release por gameplay
Progression restore genérico por objeto além do MVP `test_object_01`
```

Unsupported deve ser explícito. Não criar fallback silencioso.

---

## 22.1 Checkpoint congelado — ActivityObject Snapshot Save/Load/Restore — PASS funcional e semântico

- Estado: CONGELADO / PASS funcional e semântico.
- Data: 2026-05-22.
- Escopo: `SessionActivityPipeline`, `SessionOperationalPipeline`, `RouteActivitySave`, `ActivityObjectSnapshot`, validação de contrato de snapshot e restore mínimo de `Transform`.
- Fonte de evidência: smoke final com `test_object_01` confirmando ordem canônica, save-on-exit, load-on-enter e restore verificado.

### 22.1.1 Decisão congelada

A Base 1.1 congela o seguinte shape para snapshot mínimo de objeto de Activity:

```text
SessionActivityPipeline
-> valida contrato de snapshot no ActivityEntryPipeline
-> executa ObjectReset somente depois do contrato de snapshot validado
-> aplica restore no setup da Activity depois de ObjectReset e antes de ActivityRunning
-> captura snapshot no route-exit antes de ObjectRelease/ActivityContentSceneUnload

SessionOperationalPipeline
-> decide load-on-enter/save-on-exit pelo RouteActivitySave
-> resolve ProgressionSlotContext
-> mantém payload carregado como pending/read-only
-> persiste payload capturado via SaveRuntime

SaveRuntime
-> executa persistência por SaveAddress/SaveRequest
-> não decide lifecycle
-> não decide slot/snapshot
-> não aplica estado em objetos

ActivityObjectTransformSnapshotProvider
-> lê estado do Transform alvo explicitamente configurado
-> não chama SaveRuntime
-> não decide quando salvar

ActivityObjectTransformSnapshotRestoreEndpoint
-> aplica side-effect local no Transform alvo explicitamente configurado
-> não chama SaveRuntime
-> não decide lifecycle
```

### 22.1.2 Ordem canônica do ActivityEntryPipeline para snapshot

A ordem observável e normativa do setup da Activity inclui validação de contrato de snapshot antes de qualquer side-effect de reset:

```text
ResolveActivityEntry
-> ResolveActivityContentProfile
-> Load/PrepareActivityContent
-> DiscoverActivityContributors
-> BuildActivitySetupInventory
-> ValidateActivitySetupInventory
-> ActivityObjectSnapshotContractValidation
-> ObjectReset
-> ActivityObjectSnapshotRestore
-> ParticipantBinding
-> ActivitySetupCompleted
-> ActivationWindow
-> ActivityRunning
```

`ActivityObjectSnapshotContractValidation` é `Pipeline Stage`, não adapter. Ele produz `Pipeline Facts` e bloqueia o pipeline se contrato obrigatório estiver quebrado.

### 22.1.3 Regras de contrato de snapshot

Para cada contributor descoberto na entry atual, o contrato de snapshot considera:

```text
targetId
requiredness
snapshot provider capability
snapshot restore endpoint capability
targetTransform binding
entry identity
```

Regras congeladas:

- `targetTransform` é obrigatório quando um provider ou restore endpoint de snapshot existe.
- Não existe fallback silencioso para `this.transform`.
- Não existe busca por nome, tag ou singleton.
- Se provider e restore endpoint existem para o mesmo `targetId`, ambos devem apontar para o mesmo `targetTransform`.
- Capability quebrada não pode virar `SkippedOptional`.
- `SkippedOptional` só é válido quando o contributor é opcional e nenhuma capability de snapshot foi declarada.
- Contributor required sem capability obrigatória de snapshot, quando a policy exigir snapshot, falha explicitamente.
- Contrato quebrado gera `ActivityObjectSnapshotContractFailed` e bloqueia `ObjectReset`, `ActivationWindowReady` e `ActivityRunning`.

### 22.1.4 Regras de restore

O restore é comandado pelo `SessionActivityPipeline`.

```text
ActivityObjectSnapshotContractValidation
-> ObjectReset
-> ActivityObjectSnapshotRestore
```

`ObjectReset` retorna a entry para uma condição determinística base.  
`ActivityObjectSnapshotRestore` aplica o payload salvo por cima dessa base, quando houver payload carregado.

Regras:

- Sem payload carregado: `ActivityObjectSnapshotRestore` completa como `Skipped`, não como `Passed`.
- Payload carregado sem target compatível para a entry atual: skip explícito, salvo quando policy futura exigir restore obrigatório.
- Payload foreign/stale: rejeição/falha explícita conforme policy.
- Target obrigatório ausente: falha explícita.
- Endpoint obrigatório ausente ou inválido: falha explícita.
- Restore só passa se houver restore aplicado e `restoreVerified=true`.
- O endpoint deve evidenciar `beforePosition`, `payloadPosition`, `afterPosition` e `restoreVerified`.

### 22.1.5 Regras de capture/save/load

O capture ocorre no rail de saída da Activity, antes de `ObjectRelease` e antes de unload da ActivityContent scene.

```text
RouteExit / Activity deactivation
-> ActivityObjectSnapshotCapture
-> ObjectRelease
-> ActivityContentSceneUnload
-> SessionOperational RouteActivitySave save-on-exit
```

O `SessionActivityPipeline` produz payload read-only. Ele não salva.

O `SessionOperationalPipeline` decide save-on-exit e load-on-enter via policy de rota:

```text
loadActivitySaveOnEnter
saveActivityOnExit
```

O `RouteActivitySave` usa `ProgressionSlotContext` resolvido e persiste via `ISaveService`/`SaveRuntime`.

### 22.1.6 Checkpoint funcional e semântico validado

Smoke final confirmou a ordem canônica e o ciclo completo:

```text
ActivityObjectContributorDiscovery checkpointStatus='Passed'
ActivityObjectSnapshotContractValidation checkpointStatus='Passed'
ActivityObjectReset checkpointStatus='Passed'
ActivityObjectSnapshotRestore checkpointStatus='Skipped' // primeira entrada sem payload
ActivityObjectSnapshotCapture checkpointStatus='Passed'
RouteActivitySaveSnapshotPayload checkpointStatus='Passed'
RouteActivitySaveSaveCompleted
RouteActivitySnapshotPayloadLoaded
RouteActivitySaveSnapshotLoad checkpointStatus='Passed'
ActivityObjectSnapshotContractValidation checkpointStatus='Passed'
ActivityObjectReset checkpointStatus='Passed'
ActivityObjectSnapshotRestore checkpointStatus='Passed'
restoreVerified='true'
```

Payload de restore validado no smoke final:

```text
targetId='test_object_01'
payloadAvailable='true'
payloadObjectCount='1'
matchedTargetCount='1'
restoredCount='1'
beforePosition='(960,540,0)'
payloadPosition='(2,3,0)'
afterPosition='(2,3,0)'
restoreVerified='true'
restoreFailed='false'
```

### 22.1.7 Ownership congelado

```text
SessionOperationalPipeline = owner de route-level load/save policy.
SessionActivityPipeline = owner de Activity setup, snapshot contract validation, capture timing e restore timing.
SaveRuntime = executor de persistência.
Object provider/endpoint = executor local/leitor local de estado do objeto.
```

Objetos não decidem quando salvar, carregar, restaurar ou liberar.  
Adapters/endpoints executam side-effects comandados por `Pipeline Commands`.  
Eventos foreign/stale não podem alterar a Activity ativa nem o payload ativo.

### 22.1.8 Dívida não bloqueante

O checkpoint atual restaurou corretamente, mas a observabilidade ainda pode melhorar:

```text
captureTargetTransformPath
```

deve ser propagado no payload carregado ou marcado explicitamente como indisponível quando o dado não existir no schema salvo.

Essa dívida não bloqueia o PASS funcional e semântico porque `restoreVerified='true'` confirmou o resultado final e a ordem `ActivityObjectSnapshotContractValidation -> ObjectReset -> ActivityObjectSnapshotRestore` foi validada.


## 22.2 Checkpoint congelado — PlayerActor Readiness, PlayerInputBinding e MovementControl — PASS funcional

- Estado: CONGELADO / PASS funcional.
- Data: 2026-05-22.
- Escopo: `SessionActivityPipeline`, `ActivitySetup`, `PlayerActor` materializado, `PlayerInputBindingStage`, `MovementBindingStage`, `MovementControl` e binding retido em Activity sem novo participant command plan.
- Fonte de evidência: smoke `Boot -> Menu -> Sandbox -> activity_01 -> activity_02 -> BackToMenu`, confirmando binding de input, binding de movimento, bloqueio em windows, liberação em running, retained binding em skip/no-content e retorno ao menu.

### 22.2.1 Decisão congelada

A Base 1.1 congela o seguinte shape para participante controlável mínimo dentro do `ActivityEntryPipeline`:

```text
SessionActivityPipeline / ActivitySetup
-> materializa PlayerActor pela entry atual
-> valida PlayerActor readiness
-> executa PlayerInputBindingStage
-> executa MovementBindingStage
-> conclui ActivitySetup somente após os bindings obrigatórios aplicáveis
```

`PlayerActor` é materializado pelo `SessionActivityPipeline` / `ActivitySetup`.

`PlayerInput` vem do `PlayerActorPrefab` materializado. Não existe `PlayerInput` operacional paralelo para gameplay.

`MovementBindingStage` prepara/binda capacidade de movimento, mas não libera controle durante setup.

```text
MovementBindingStage = preparation/binding.
MovementControl = lifecycle enable/disable comandado pelo pipeline.
```

### 22.2.2 Regras congeladas para PlayerInputBindingStage

`PlayerInputBindingStage` usa o `PlayerInput` existente no `PlayerActor` materializado.

Regras:

- não criar `operationalPlayerInputPrefab`;
- não criar `__OperationalPlayerInputsRuntimeRoot`;
- não provisionar `PlayerInput` de gameplay no `SessionOperationalPipeline`;
- não usar `UnityEditor` em runtime;
- não usar reflection para escrever binding;
- não buscar `PlayerInput` globalmente por nome, tag, singleton ou primeiro componente da cena;
- resolver o `PlayerInput` a partir do `PlayerActor` materializado e da identidade da entry atual;
- permitir rebind explícito de `PlayerInput.actions` para o asset canônico já validado pelo `RuntimeConfigRegistry` / `InputModesRuntime`, emitindo observabilidade;
- falhar explicitamente se `PlayerInput`, action map `Player`, action `Move` ou asset canônico obrigatório estiverem ausentes.

O rebind ao asset canônico é permitido porque:

```text
OperationalInputRuntime valida o InputActionAsset canônico.
PlayerInputBindingStage aplica esse asset no endpoint técnico materializado.
```

Isso não é fallback silencioso. O rebind precisa ser explícito, observável e baseado no asset já validado, não em busca por nome.

Evidência congelada:

```text
PlayerInputActionsReboundToCanonical
PlayerInputBound / binding equivalente
```

### 22.2.3 Regras congeladas para MovementBindingStage

`MovementBindingStage` roda depois de `PlayerInputBindingStage` e antes de `ActivitySetupCompleted`.

Responsabilidades:

```text
resolver PlayerActor materializado
resolver PlayerInput já bound
resolver PlayerMoveInputReader / PlayerMovementController no PlayerActor
bindar PlayerInput ao reader/controller
emitir PlayerMovementBound
emitir MovementBindingCompleted
manter controlEnabled=false
```

Regras:

- não usar `Input.GetAxis`;
- não usar `Input.GetAxisRaw`;
- não buscar `PlayerInput` globalmente;
- não criar reader/controller automaticamente por fallback silencioso;
- não liberar movimento em `ActivitySetup`;
- não liberar movimento em `ActivationWindow`;
- não mover lifecycle para `PlayerMovementController` ou `PlayerMoveInputReader`;
- controller/reader executam side-effects técnicos, mas não decidem lifecycle da Activity.

Evidência congelada:

```text
MovementBindingStarted
PlayerMovementBound
MovementBindingCompleted controlEnabled=false
```

### 22.2.4 Regras congeladas para MovementControl lifecycle

A liberação/bloqueio de movimento pertence ao lifecycle comandado pelo `SessionActivityPipeline`.

Fluxo congelado:

```text
ActivitySetup
-> MovementBindingStage prepara/binda
-> controlEnabled=false

ActivationWindow
-> movimento bloqueado

CompleteActivationWindow
-> transição para ActivityRunning
-> MovementControlEnabled

CompleteCurrentActivity / Deactivation / RouteExit
-> MovementControlDisabled
```

Regras:

- `ActivityRunning` habilita movimento somente quando houver alvo válido novo ou retido;
- `DeactivationWindow`, `ActivityTransition` e `RouteExit` bloqueiam movimento;
- disable deve limpar input e estado transitório de movimento;
- disable duplicado no mesmo `activityId|entrySequence|source|reason` é idempotente e deve emitir `MovementControlDisableSkippedDuplicate`, não um segundo `MovementControlDisabled` real.

Evidência congelada:

```text
MovementControlEnabled
MovementControlDisabled
MovementControlDisableSkippedDuplicate // quando houver segunda tentativa idempotente no mesmo contexto
```

### 22.2.5 Skip real não é retained binding

A Base 1.1 congela a distinção semântica:

```text
MovementBindingSkippedNoRequiredMovement
= não há requisito/capacidade de movimento a liberar para a entry.

MovementBindingRetained
= não há novo bind, mas existe binding válido retido na sessão para a entry atual.
```

`Activity` sem novo participant command plan pode reutilizar binding de player/control retido quando a identidade continuar válida.

Regras:

- skip real não habilita controle;
- retained binding pode habilitar controle ao entrar em `ActivityRunning`;
- binding retido precisa validar identidade e estado, incluindo `sessionStateId`, `activityId`/entry aplicável, `entrySequence`, `playerSlotId`, `playerActorId`, binding de input, binding de movimento e instance ainda válida;
- foreign/stale binding não pode habilitar controle.

Evidência congelada para `activity_02` skip/no-content:

```text
MovementBindingStarted
MovementBindingRetained
MovementBindingCompleted status='RetainedExistingBinding' controlEnabled=false
MovementControlEnabled
ActivityRunning
```

### 22.2.6 Route-exit e release no-content

No route-exit, `ClosedForRouteExit` continua bloqueado até a conclusão semântica do release obrigatório de ActivityContent.

Para Activity sem content scenes carregadas, o branch `SkippedNoContent` também precisa emitir conclusão semântica de release:

```text
ActivityContentReleaseSkippedNoContent
ActivityContentReleaseCompleted status='SkippedNoContent'
```

Isso preserva a invariante:

```text
ClosedForRouteExit só pode ocorrer depois do release obrigatório de ActivityContent.
```

No caso `no-content`, `releaseStarted=false` com `releaseCompleted=true` significa:

```text
não houve release físico de content scene;
houve conclusão semântica do stage de release.
```

Essa diferença deve permanecer observável para evitar falsos diagnósticos.

### 22.2.7 Aprendizados congelados

A implementação deste checkpoint consolida os seguintes aprendizados como guias para próximos stages do `ActivityEntryPipeline`:

- preparação/binding não é liberação de gameplay;
- skip real não é retained binding;
- Activity sem novo participant command plan pode reutilizar binding válido retido;
- ausência de log/fact de stage deve levar primeiro a auditoria de conexão/observabilidade do pipeline, não a criação de config nova;
- prefab/config/adapter novo só deve ser criado depois de auditar capacidades existentes;
- contratos existentes devem ser reaproveitados/adaptados quando já estiverem alinhados à Base 1.1;
- não criar trilho paralelo para resolver problema de authoring ou de stage não conectado;
- facts internos relevantes precisam ter observabilidade suficiente no smoke para evitar falso diagnóstico de configuração.

### 22.2.8 Fora do checkpoint

Este checkpoint não implementa nem congela:

```text
CameraBindingStage final
InteractionBindingStage final
HudBindingStage final
split-screen real
runtime rebind de input
Player camera rig final
ObjectEntry real completo
Progression restore genérico
```

Esses pontos permanecem futuros e devem seguir a mesma regra: auditar o existente antes de criar contrato/config/prefab novo.


## 22.3 Dívidas não bloqueantes pós-checkpoint PlayerInput/Movement

As seguintes dívidas não bloqueiam o PASS funcional do checkpoint `PlayerActor Readiness + PlayerInputBinding + MovementControl`:

- `RouteExitBackToMenu` deve emitir `checkpointStatus='Passed'` explicitamente depois de `menuRouteApplied='true'`.
- Clarificar nos checkpoints a semântica `releaseStarted=false` + `releaseCompleted=true` quando o release físico é `SkippedNoContent`, mas o stage concluiu semanticamente.
- Futuro: consolidar a distribuição de estado entre pipeline, registry e binding state components quando houver mais bindings.
- Futuro: revisar criação runtime de `PlayerActorMovementBindingState` e preferir componente autorado no prefab se isso reduzir mutação implícita sem criar rigidez desnecessária.
- Futuro: padronizar os vários usos de `PlayerId`, separando semanticamente `PlayerSlotId`, `PlayerActorId`, `PlayerDefinitionId`, `PlayerSelectionId` ou equivalentes.
- Futuro: quando runtime rebind de input for implementado, manter readers lendo actions semânticas, não teclas/botões concretos.

Essas dívidas são de hardening, observabilidade ou refinamento semântico. Elas não reabrem o ownership congelado:

```text
SessionActivityPipeline decide lifecycle.
Adapters/componentes executam side-effects.
InputModes valida/aplica modos, mas não decide lifecycle de Activity.
PlayerInput/Movement endpoints não decidem ActivityRunning.
```


## 23. Invariantes obrigatórios

- `SessionActivityPipeline` decide lifecycle local de Activity.
- `SessionOperationalPipeline` decide lifecycle de rota.
- `ActivityEntryPipeline` é único para todas as Activities.
- O que varia é `ActivitySetupInventory`, não lifecycle.
- `ActivityContent` é específico da Activity.
- `WindowTemplateLibrary` é route-scoped.
- Fechar window não descarrega template scene.
- Activity sem conteúdo gera skip explícito.
- Subplano vazio gera skip explícito.
- Requisito obrigatório ausente falha explicitamente.
- Reset não é Release.
- Restart não é Save/Restore.
- Release não é Destroy por default.
- Pooling canônico deve ser preferido quando policy/ownership indicar pool.
- Todo command relevante carrega `Pipeline Identity`.
- Foreign/stale events não alteram pipeline ativo.
- Nenhum adapter decide lifecycle.
- Nenhum objeto/contributor decide quando o pipeline avança.
- ActivityObject snapshot/restore mínimo validado não transforma objeto em owner de Save.
- `RouteActivitySave` não aplica estado em objeto; restore pertence ao `SessionActivityPipeline`.
- `PlayerInputBindingStage` usa o `PlayerInput` do `PlayerActor` materializado, sem `PlayerInput` operacional paralelo.
- `PlayerInputBindingStage` pode rebinder explicitamente para o asset canônico validado pelo `RuntimeConfigRegistry` / `InputModesRuntime`, mas não pode buscar asset por nome nem usar fallback silencioso.
- `MovementBindingStage` prepara/binda movimento com `controlEnabled=false`.
- `MovementControlEnabled` só ocorre quando o pipeline entra em `ActivityRunning` e há alvo válido.
- `MovementControlDisabled` ocorre em deactivation, transition e route-exit quando houver alvo ativo/retido.
- Skip real não é retained binding; `MovementBindingRetained` pode liberar controle em `ActivityRunning`, skip real não.
- Disable duplicado deve ser idempotente e observável como duplicate/skip, não como segundo side-effect real.
- Branch `SkippedNoContent` de release deve emitir conclusão semântica antes de `ClosedForRouteExit`.
- Não criar Base 2.0 agora.
- Não criar core genérico universal agora.
- Não reorganizar fisicamente arquitetura em Core/Concrete/UnityAdapter.

---

## 24. Ordem conceitual de entrada da Activity

```text
ResolveActivityEntry
-> ResolveActivityContentProfile
-> Load/PrepareActivityContent ou ActivityContentLoadSkippedNoContent
-> DiscoverActivityContributors
-> BuildActivitySetupInventory
-> ValidateActivitySetupInventory
-> ParticipantSetupStage
   -> PlayerActorReadinessValidation
   -> PlayerInputBindingStage
   -> MovementBindingStage, quando aplicável
-> ObjectEntrySetupStage
-> PlacementSetupStage
-> CameraBindingSetupStage
-> InteractionBindingSetupStage
-> HudBindingSetupStage
-> WarmupSetupStage
-> ActivitySetupReadinessValidation
-> ActivitySetupCompleted
-> ActivationWindowPresentation ou ActivationWindowSkippedNoContent
-> CompleteActivationWindow, se houver window
-> ActivityRunning
   -> MovementControlEnabled, quando houver alvo válido
```

A primeira Activity usa a cortina/loading da rota como contexto visual.

Activities seguintes usam cortina/transição local da Activity como contexto visual.

O pipeline interno é o mesmo.

---

## 25. Auditorias obrigatórias antes da implementação

Antes de implementar este ADR no runtime, fazer auditoria sobre:

```text
ActivityAsset / ActivityCatalogAsset atuais
ActivationWindow / DeactivationWindow atuais
Window AdditiveScene atual
ActivityTransition atual
PlayerActorSetup/Reset atual
ActivitySceneContract atual
SceneKeyAsset e scene loading atual
CameraPresentation / ActivityCamera atual
Pooling canônico existente
ObjectEntry / contributor contracts existentes
Save/RouteActivitySave boundaries atuais
```

Objetivos da auditoria:

- mapear o que existe;
- mapear o que deve ser removido;
- mapear o que deve ser reaproveitado;
- identificar conflitos com este ADR;
- evitar compat paralelo;
- evitar trilho fantasma;
- evitar implementação ansiosa antes de ownership correto.

---

## 26. Relação com ADRs existentes

### ADR-0001

Este ADR preserva Pipeline Convergence, identidade explícita e isolamento contra foreign/stale events.

### ADR-0003

`SessionOperationalPipeline` continua owner de rota, transição operacional, SceneComposition operacional e handoff inicial para `SessionActivityPipeline`.

### ADR-0004

Este ADR detalha e atualiza o shape de `ActivityContent`, `ActivityEntryPipeline`, windows, reset, retention/release e ObjectEntry dentro do domínio do `SessionActivityPipeline`.

### ADR-0005

Mantém a regra: pipeline decide, facts/commands registram, adapters executam side-effects.

### ADR-0006

SceneComposition, fade e loading de rota permanecem adapters/execução do domínio operacional.

### ADR-0007

Gates/InputModes/GameLoop seguem como executores técnicos, não owners de lifecycle.

### ADR-0008

Restart/reset deste ADR não são Save/Restore. `ActivityObjectSnapshotCapture` produz dado local para Progression Save MVP; `RouteActivitySave`/`SaveRuntime` permanecem no ADR-0008. O MVP funcional `RouteActivitySave + ActivityObjectSnapshotRestore` validou restore mínimo de `Transform` para `test_object_01`, mantendo a regra de ownership: `SessionOperationalPipeline` decide load/save de rota, `SessionActivityPipeline` decide timing de capture/restore, e endpoints apenas executam side-effects comandados. Restore genérico de Progression permanece futuro e não deve sobrescrever/ser sobrescrito por Placement/ObjectReset sem policy explícita.

### ADR-0009 / ADR-0010

PlayerSlot é capacidade operacional. PlayerActor nasce no `ActivitySetup`. Player não é propriedade semântica da Activity.

O checkpoint `PlayerActor Readiness + PlayerInputBinding + MovementControl` preserva a fronteira:

```text
SessionOperationalPipeline / InputModesRuntime
-> valida capacidade operacional e asset canônico de input.

SessionActivityPipeline / ActivitySetup
-> materializa PlayerActor.
-> binda PlayerInput do PlayerActor ao slot/ator da entry.
-> binda Movement ao PlayerInput já validado.
-> libera/bloqueia controle conforme lifecycle local da Activity.
```

Não há provisioning operacional paralelo de `PlayerInput` de gameplay.

### ADR-0012 / ADR-0013

Operational Camera, Route Camera, Activity Camera e Window Camera permanecem camadas separadas. Este ADR registra prioridade e uso dentro do ActivityEntry/Window lifecycle.

---

## 27. Consequências

- Activity deixa de ser confundida com cena.
- Windows deixam de ser confundidas com conteúdo jogável.
- A rota pode fornecer templates de janela sem virar owner de lifecycle da janela.
- `ActivityContent` passa a ser específico, carregável, rastreável e liberável por entry.
- O catálogo de activities não vira pacote de scenes a carregar integralmente.
- O setup da Activity passa a convergir por inventário e stages, não branches especiais.
- Reset, release, restart e pooling deixam de ser efeitos soltos.
- O sistema fica preparado para migrar componentes reais gradualmente, adicionando ResetGroups, Save providers e ObjectEntry adapters quando houver necessidade concreta.

---

## 28. Critérios de aceite futuro

Este ADR poderá ser considerado implementado quando houver evidência de runtime para:

```text
ActivityContentProfile resolvido por Activity.
Load/PrepareActivityContent emitindo facts e commands com identity.
Activity sem content emitindo skip explícito.
Content scenes additive sem SetActiveScene.
Contributor discovery após content load.
ActivitySetupInventory construído por entry.
Subplanos vazios emitindo skip explícito.
ResetGroups v0 comandados por pipeline.
ActivityContentRelease antes de ClosedForRouteExit em route-exit.
Branch SkippedNoContent de release emitindo conclusão semântica antes de ClosedForRouteExit.
PlayerActor readiness validado antes de ActivitySetupCompleted quando houver participante obrigatório.
PlayerInputBindingStage usando PlayerInput do PlayerActor materializado, sem runtime paralelo.
MovementBindingStage preparando controle com controlEnabled=false.
MovementControlEnabled somente em ActivityRunning.
MovementControlDisabled em deactivation/route-exit.
MovementBindingRetained habilitando controle em Activity skip/no-content quando houver binding válido.
RestartCurrentActivity criando nova entrySequence e recarregando content no v0.
WindowTemplateLibrary não duplicada nem descarregada por window close.
Comandos stale/foreign rejeitados.
Nenhum fallback silencioso para Route Scene quando content obrigatório faltar.
```

