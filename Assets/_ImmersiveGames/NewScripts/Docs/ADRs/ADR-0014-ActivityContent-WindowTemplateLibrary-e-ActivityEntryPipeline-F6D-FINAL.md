# ADR-0014 — ActivityContent, WindowTemplateLibrary e ActivityEntryPipeline

## Status

- Estado: ACEITO / checkpoint normativo vivo da Base 1.1
- Data original: 2026-05-19
- Última atualização: 2026-05-21
- Tipo: Direction / Canonical architecture / Base 1.1 checkpoint
- Fonte de verdade canônica deste contrato: este ADR.

Checkpoints congelados:

```text
F4D3 -> F4D6d = PASS funcional
F4D7 = PASS funcional
F4D8 = PASS funcional
F5B -> F5C.2 = PASS estrutural
F5D = PASS funcional
F5QA = PASS funcional
F5E1 = PASS funcional
F5E2 = PASS funcional
F6A = boundary congelado / auditoria aceita
F6B = PASS funcional
F6C = PASS funcional
F6D = PASS funcional
```

Frente atual após F6D:

```text
F6E — Contributor unregister / cleanup de contributors descobertos por ActivityContent, se necessário
```

---

## 1. Contexto

A Base 1.1 — Pipeline Convergence / Convergência para Pipelines Determinísticos — materializou `SessionOperationalPipeline` e `SessionActivityPipeline` como owners separados:

```text
SessionOperationalPipeline = owner de rota, transição operacional, SceneComposition, fade/loading de rota e handoff operacional.
SessionActivityPipeline = owner do lifecycle local de Activity, ActivityEntryPipeline, ActivityContent, windows e teardown local.
```

Até este ADR, o projeto estabilizou:

- entrada de `SessionActivity` por `SessionActivityEntryHandoff`;
- `ActivationWindow` e `DeactivationWindow` com comando explícito;
- `ActivityTransition` local Activity -> Activity;
- `ActivityRouteExitRail` antes de side-effects operacionais de troca de rota;
- participant/player preparation pertencente à rota/sessão;
- `ActivityParticipantBinding` como rail canônico de setup local;
- release mínimo de `ActivityContent` nos fluxos Activity -> Activity, Restart e Route-exit;
- discovery observacional de `ActivityObjectContributor` por `entrySequence`;
- `ObjectReset` mínimo por objeto antes de `ActivitySetupCompleted`;
- `ObjectRelease` mínimo por objeto antes de `ActivityContentSceneUnload` nos rails Activity -> Activity, Restart e Route-exit.

O problema arquitetural tratado por este ADR é separar definitivamente:

```text
Activity
ActivityContent
ActivationWindow
DeactivationWindow
Route Scene
WindowTemplateLibrary
ActivityEntryPipeline
ActivitySetupInventory
ObjectReset
ObjectRelease
```

Sem essa separação, o sistema tende a regredir para:

- tratar Activity como cena;
- misturar window scenes com conteúdo jogável;
- carregar todo o catálogo de activities da rota;
- fazer setup especial para a primeira Activity;
- duplicar templates de janela;
- criar branches paralelos para activities “com player”, “sem player”, “com NPC”, “sem NPC”;
- aplicar reset, release e save como se fossem o mesmo conceito;
- transformar `ActivityContentProfile` em árvore manual de requirements por objeto.

---

## 2. Decisão canônica

A Base 1.1 adota um pipeline único de entrada de Activity:

```text
ActivityEntryPipeline
```

Toda Activity passa pelo mesmo pipeline. O que varia é o `ActivitySetupInventory` resolvido, não o lifecycle.

Separação canônica:

```text
Activity = lifecycle.
ActivityContent = conteúdo jogável/material usado pela Activity.
ActivationWindow = janela autoral de entrada.
DeactivationWindow = janela autoral de saída.
Route Scene = superfície/base da rota.
WindowTemplateLibrary = capacidade visual compartilhada route-scoped para janelas.
```

Ownership:

```text
SessionActivityPipeline decide lifecycle local da Activity.
SessionOperationalPipeline decide lifecycle de rota.
Pipeline Stages emitem Pipeline Facts e Pipeline Commands.
Pipeline Adapters executam side-effects comandados.
Objetos/contributors/endpoints executam sua própria estratégia técnica quando comandados.
```

Nenhum adapter decide lifecycle. Nenhum objeto decide quando o pipeline avança. Todo ciclo relevante carrega `Pipeline Identity`. Eventos `foreign/stale` não podem alterar o pipeline ativo.

---

## 3. Separação conceitual

### 3.1 Activity

`Activity` é lifecycle determinístico local:

```text
setup
-> activation
-> running
-> deactivation
-> transition/release
```

A Activity não é uma cena, não é seu conteúdo e não é sua window de ativação/desativação.

### 3.2 ActivityContent

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

### 3.3 ActivationWindow

`ActivationWindow` é janela autoral de entrada.

Pode representar introdução, tutorial local, disclaimer, prompt de início, apresentação narrativa ou confirmação antes de liberar gameplay.

Ela não é setup técnico e não auto-completa.

### 3.4 DeactivationWindow

`DeactivationWindow` é janela autoral de saída.

Pode representar resultado local, recompensa, resumo, cutscene de saída, confirmação ou post-run local da Activity.

Ela não é release técnico e não auto-completa.

### 3.5 Route Scene

`Route Scene` é a superfície/base da rota.

Ela pertence ao domínio operacional da rota e é carregada pelo `SessionOperationalPipeline` via adapters/SceneComposition.

O `SessionActivityPipeline` não descarrega a `Route Scene`.

---

## 4. ActivityContentProfile e ActivityContentScenes

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
required roles mínimos
required targets mínimos quando necessário
overrides raros da Activity
```

Regras de `ActivityContentScenes`:

```text
LoadSceneMode.Additive
não SetActiveScene
não substituir Route Scene
não usar WindowSceneAdapter semanticamente
não usar fallback silencioso para Route Scene
```

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

Identidade obrigatória:

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

Completion `foreign/stale` não pode alterar o pipeline ativo.

Ordem obrigatória:

```text
ActivityContent pronto
-> contributor discovery
-> ActivitySetupInventory
-> placement
-> camera binding
-> activation window
-> ActivityRunning
```

---

## 5. ActivityCatalog e carregamento

Uma rota pode apontar para um `ActivityCatalog`.

Isso não significa carregar todas as Activities do catálogo.

```text
ActivityCatalog = índice autoral de Activities.
Não é pacote de cenas a carregar integralmente.
```

Na entrada da rota:

```text
carrega/prepara a Activity inicial
não carrega automaticamente as próximas Activities
```

Activities seguintes são resolvidas e preparadas pelo `SessionActivityPipeline` durante `ActivityTransition`.

---

## 6. WindowTemplateLibrary route-scoped

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

A Activity fornece intenção, payload e conteúdo jogável. A rota fornece capacidade visual compartilhada. O `SessionActivityPipeline` decide o lifecycle da apresentação.

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

## 7. ActivationWindow, DeactivationWindow e câmera

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

Prioridade de câmera, da menor para a maior:

```text
Camera default
-> Camera da rota
-> Camera da Activity
-> Camera da ActivationWindow
-> Camera da DeactivationWindow
```

Separação obrigatória:

```text
load/preparation != apresentação/ativação autoral
```

A apresentação/ativação autoral deve ser determinística e comandada pelo pipeline.

---

## 8. ActivityEntryPipeline único

Toda Activity passa pelo mesmo `ActivityEntryPipeline`.

A primeira Activity e as seguintes não têm pipelines diferentes.

A diferença é apenas o contexto visual:

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

## 9. Player e participantes

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

Fonte canônica:

```text
OperationalRouteAsset.RouteParticipantSetDefinition
-> SessionOperationalPipeline PlayerPreparation
-> SessionActivityEntryHandoff.TechnicalPlanEntries
-> SessionActivityPipeline ActivityParticipantBinding / ActivityParticipant* commands
```

---

## 10. ActivitySetupInventory

`ActivitySetupInventory` transforma:

```text
ActivityContentProfile mínimo
ActivitySceneContract
contributors descobertos
endpoints/capabilities locais
overrides raros da Activity
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

## 11. Execução do ActivitySetupInventory

O `ActivityEntryPipeline` é dono da ordem e do lifecycle.

Ele não implementa diretamente regras específicas de player, NPC, HUD, câmera, interaction ou objetos.

Cada `Pipeline Stage` consome o `ActivitySetupInventory` e produz:

```text
Pipeline Fact
Pipeline Command
Skipped
Failed
```

Adapters/endpoints executam side-effects comandados pelos stages.

Regra de avanço:

```text
Completed -> avança.
Skipped -> avança quando subplano é vazio/opcional.
Failed -> bloqueia/fail-fast.
```

Ordem conceitual de entrada:

```text
ResolveActivityEntry
-> ResolveActivityContentProfile
-> Load/PrepareActivityContent ou ActivityContentLoadSkippedNoContent
-> DiscoverActivityContributors
-> BuildActivitySetupInventory
-> ValidateActivitySetupInventory
-> ParticipantSetupStage
-> ObjectEntrySetupStage
-> PlacementSetupStage
-> ObjectResetStage
-> CameraBindingSetupStage
-> InteractionBindingSetupStage
-> HudBindingSetupStage
-> WarmupSetupStage
-> ActivitySetupReadinessValidation
-> ActivitySetupCompleted
-> ActivationWindowPresentation ou ActivationWindowSkippedNoContent
-> CompleteActivationWindow, se houver window
-> ActivityRunning
```

Participant/ObjectEntry vêm antes de Placement. Camera vem depois de Placement. Interaction/HUD vêm depois da existência dos objetos. Warmup é sequencial em v0.

---

## 12. ActivitySceneContract

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

## 13. ObjectReset / StateReset

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
endpoints/capabilities locais
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

## 14. ActivityContent Retention / Release

`Deactivation` não implica `Release`.

```text
ActivityContentRetention
= policy que decide se conteúdo deactivated será liberado, mantido ou retido.
```

```text
ActivityContentRelease
= processo determinístico comandado pelo SessionActivityPipeline para liberar conteúdo Activity-owned.
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

## 15. ActivityContentRelease no Activity -> Activity

Com `ReleasePreviousActivityContent`, ordem conceitual:

```text
ActivityRunning
-> CompleteCurrentActivity
-> DeactivationWindow
-> CompleteDeactivationWindow, se houver window
-> ActivityDeactivated
-> ActivityTransition curtain/fade in
-> ActivityContentRetentionPolicyResolved
-> ActivityContentReleaseStarted
-> ObjectRelease / BindingRelease / CameraRelease / HudRelease
-> ActivityContentSceneUnloadStarted
-> ActivityContentSceneUnloaded
-> ActivityContentReleaseCompleted
-> Load/PrepareActivityContent da próxima Activity
-> ActivitySetupInventory
-> ActivitySetupCompleted
-> ActivationWindow da próxima
-> ActivityRunning
```

Release do conteúdo anterior ocorre com a cortina/transição local fechada.

---

## 16. Route-exit e release de ActivityContent

No route-exit, conteúdo Activity-owned/retained precisa ser liberado antes de declarar fechamento canônico da `SessionActivity`.

Ordem:

```text
BackToMenu / RouteExit
-> ActivityRouteExitRail
-> DeactivationWindow
-> CompleteDeactivationWindow, se houver window
-> ActivityDeactivated
-> ActivityContentReleaseStarted
-> libera conteúdo Activity-owned/retained
-> ActivityContentReleaseCompleted
-> ClosedForRouteExit
-> SessionActivityRouteExitTeardownCompleted
-> SessionOperational continua route transition
```

Regra:

```text
ClosedForRouteExit só pode ocorrer depois do release obrigatório de ActivityContent.
```

A `WindowTemplateLibrary` route-scoped não é liberada por `ActivityContentRelease`.

ActivityRouteExitRail fecha presentations e devolve templates para Standby. A liberação/descarregamento da `WindowTemplateLibrary` pertence à rota, quando a rota for liberada.

---

## 17. RestartCurrentActivity e ActivityContent

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
-> ActivityContentReleaseCompleted
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

## 18. Boundary F6A — ObjectReset / ObjectRelease por objeto

Status:

```text
Boundary congelado / auditoria aceita.
Sem implementação runtime neste checkpoint.
```

Objetivo da F6A:

```text
Definir o modelo replicável para objetos/contributors dentro de ActivityContentScenes,
sem transformar ActivityContentProfile em uma árvore manual de requirements.
```

Diagnóstico da auditoria F6A:

1. O projeto estava mais perto de `ActivityContentProfile` carregado por listas manuais do que do modelo de contributors ricos.
2. `ActivityContentProfileAsset` já possui `setupRequirements` extensos e pode crescer demais se continuar acumulando detalhes por objeto.
3. `ActivityContentSceneEntry` está bem delimitado como declaração de cena + requiredness e não deve receber semântica detalhada de objeto.
4. `ActivitySceneContractAuthoring` atua no nível de contrato de cena, não como contributor por objeto.
5. O discovery anterior observava `ActivitySceneContractAuthoring` nas scenes carregadas, mas ainda não descobria contributors por objeto.
6. A referência correta já existia: restringir observação à entry atual e ao `ActivityContentLoadedSet` válido.
7. `ActivitySetupInventoryBuilder` nascia principalmente de `ActivityContentProfile.SetupRequirements`.
8. `ActivitySetupInventory` ainda não era resolvido a partir de contributors descobertos + endpoints locais.
9. `StateResetRequirements` e `ReleaseRequirements` existiam como shape, mas ainda não viravam stages executáveis por objeto.
10. Não existia componente equivalente a `ActivityObjectContributor`.
11. Não existiam endpoints genéricos por objeto para `ObjectReset` / `ObjectRelease`.
12. O padrão de endpoint + adapter específico de participant/player serve apenas como referência arquitetural, sem migração nesta frente.

Modelo alvo congelado:

```text
ActivityContentProfile pequeno
+ ActivityObjectContributor rico na scene
+ endpoints especializados por responsabilidade
+ ActivitySetupInventory como plano resolvido por entry
```

### 18.1 ActivityContentProfile pequeno

`ActivityContentProfile` não deve ser catálogo detalhado de todos os requirements dos objetos.

Ele deve declarar apenas:

```text
contentScenes
required roles mínimos
required targets mínimos quando necessário
overrides raros da Activity
policies autorais de entry quando realmente específicas da Activity
```

Ele não deve concentrar:

```text
lista exaustiva de reset por objeto
lista exaustiva de release por objeto
detalhes internos de componentes concretos
árvore manual de capabilities por GameObject
```

Regra:

```text
O Profile declara o que a Activity espera.
O objeto declara o que ele é capaz de oferecer.
O Inventory resolve o plano final.
```

### 18.2 ActivityObjectContributor

O modelo alvo introduz um contributor por objeto de cena, ou componente equivalente, com papel autoral e de descoberta.

Responsabilidades conceituais:

```text
targetId
role/kind
requiredness default
capabilities locais
referências/agregação de endpoints
metadados mínimos de debug/autoria
```

O contributor não decide lifecycle.
O contributor não avança pipeline.
O contributor não executa side-effect.

Ele responde:

```text
Eu existo nesta ActivityContentScene.
Este é meu targetId.
Este é meu role/kind.
Estas são minhas capacidades locais.
Estes endpoints existem neste objeto/escopo.
```

### 18.3 Endpoints especializados

Endpoints executam comportamento concreto e devem ser especializados por responsabilidade.

Exemplos conceituais:

```text
Transform reset endpoint
Interaction reset endpoint
Objective reset endpoint
Runtime transient cleanup endpoint
Local binding release endpoint
Contributor unregister endpoint
```

Endpoints não decidem quando rodar.
Endpoints não decidem lifecycle.
Endpoints executam comandos emitidos pelo pipeline/stage.

### 18.4 ActivitySetupInventory como plano resolvido

`ActivitySetupInventory` é o plano normalizado da entry atual.

Ele deve ser resolvido a partir de:

```text
ActivityContentProfile mínimo
+ ActivitySceneContract
+ contributors descobertos
+ endpoints/capabilities locais
+ overrides raros da Activity
```

O Inventory deve produzir requirements executáveis:

```text
StateResetRequirement
ReleaseRequirement
ObjectEntryRequirement
SceneContributorRequirement
PlacementRequirement
CameraBindingRequirement
InteractionBindingRequirement
HudBindingRequirement
WarmupRequirement
```

Ele não deve ser apenas uma cópia direta de listas autorais do profile.

### 18.5 Discovery controlado

Discovery deve ser controlado pelo pipeline/adapters e restrito a:

```text
ActivityContentScenes carregadas da entry atual
activityId atual
entrySequence atual
ActivityContentLoadedSet válido
```

Discovery produz reports, não executa setup.

```text
Contributor discovery = quem está presente e quais capacidades existem.
ActivitySetupInventory = o que esta entry precisa fazer com isso.
```

Foreign/stale discovery não pode alterar o pipeline ativo.

### 18.6 Decisões de ObjectReset / ObjectRelease

1. `SessionActivityPipeline` decide quando os stages de `ObjectReset` e `ObjectRelease` rodam.
2. `ObjectReset` e `ObjectRelease` consomem `ActivitySetupInventory`.
3. O pipeline emite `Pipeline Commands` por requirement.
4. Objetos/endpoints/adapters executam a estratégia técnica.
5. Activity não escolhe a estratégia técnica.
6. Activity não executa side-effect diretamente.
7. `ObjectReset` roda após `ActivitySetupInventoryValidated` e antes de `ActivitySetupCompleted`.
8. `ObjectRelease` roda após `ActivityDeactivated` e antes do unload da `ActivityContentScene`.
9. Required ausente/falho gera fail-fast.
10. Optional ausente gera skip explícito.
11. Completion `foreign/stale` deve ser rejeitada por `Pipeline Identity + requirementId + targetId`.

Ordem futura para reset:

```text
ActivitySetupInventoryValidated
-> ObjectResetStarted
-> ObjectResetCommandIssued
-> ObjectResetApplied / ObjectResetSkippedOptional / ObjectResetFailed
-> ObjectResetCompleted
-> ActivitySetupCompleted
```

Ordem futura para release:

```text
ActivityDeactivated
-> ObjectReleaseStarted
-> ObjectReleaseCommandIssued
-> ObjectReleaseApplied / ObjectReleaseSkippedOptional / ObjectReleaseFailed
-> ObjectReleaseCompleted
-> ActivityContentSceneUnloadCommandIssued
-> ActivityContentSceneUnloaded
-> ActivityContentReleaseCompleted
```

Facts futuros esperados:

```text
ObjectResetStarted
ObjectResetCommandIssued
ObjectResetApplied
ObjectResetSkippedOptional
ObjectResetFailed
ObjectResetCompleted

ObjectReleaseStarted
ObjectReleaseCommandIssued
ObjectReleaseApplied
ObjectReleaseSkippedOptional
ObjectReleaseFailed
ObjectReleaseCompleted
```

### 18.7 Gaps aceitos pela auditoria F6A

Gaps aceitos antes de F6B:

```text
Nao existia ActivityObjectContributor.
Nao existia scanner de contributors por objeto.
ActivitySetupInventory ainda nascia majoritariamente de SetupRequirements autorais.
StateResetRequirements ainda nao viravam ObjectReset commands.
ReleaseRequirements ainda nao viravam ObjectRelease commands.
Nao existiam endpoints genericos de ObjectReset/ObjectRelease.
Nao existiam facts agregados de ObjectReset/ObjectRelease por objeto.
```

Maior risco:

```text
ActivityContentProfile virar uma arvore gigante de requirements manuais.
```

Direção congelada:

```text
Mover detalhe repetível para contributors/endpoints.
Manter ActivityContentProfile pequeno.
Fazer ActivitySetupInventory ser o plano resolvido, nao a fonte manual unica.
```

### 18.8 Fora do escopo da F6A

```text
Implementação runtime.
Mudança de lifecycle já congelado.
Mudança de ActivityParticipantBinding.
Mudança de ActivityContent scene release.
Migração de PlayerActor.
Save/progression por objeto.
Regras concretas de gameplay por tipo de objeto.
```

---

## 19. Checkpoint F6B — ActivityObjectContributor Discovery PASS funcional

Status:

```text
F6B = PASS funcional (2026-05-21)
```

Objetivo:

```text
Criar o primeiro corte estrutural para objetos/contributors de ActivityContent:
ActivityObjectContributor mínimo + discovery controlado por entry.
```

Superfície validada:

```text
ActivityObjectContributor
ActivityObjectContributorDiscoveryContracts
ActivityObjectContributionReport
ActivityObjectContributorDiscoveryResult
ActivityObjectContributorDiscoveryStarted
ActivityObjectContributorDiscovered
ActivityObjectContributorDiscoverySkippedNoContent
ActivityObjectContributorDiscoveryCompleted
ActivityObjectContributorDiscoveryFailed
QACheckpoint ActivityObjectContributorDiscovery
```

Campos mínimos congelados do `ActivityObjectContributor`:

```text
targetId
roleId
contributorKind
defaultRequiredness
supportedResetGroups
supportedReleaseKinds
includeChildrenForEndpointDiscovery
debugLabel
```

Decisões congeladas:

1. `ActivityObjectContributor` é o marcador mínimo de objetos pertencentes à `ActivityContentScene`.
2. O contributor identifica presença, identidade, role/kind, requiredness default e capabilities locais.
3. O contributor não decide lifecycle.
4. O contributor não avança pipeline.
5. O contributor não executa side-effect.
6. Discovery é limitado às `ActivityContentScenes` carregadas da entry atual.
7. Discovery usa o `CurrentActivityContentLoadedSet` válido da entry atual como fronteira.
8. Discovery não usa `Route Scene` como fallback.
9. Discovery não é global.
10. Discovery é observacional nesta fase.
11. Activity sem `ActivityContent` emite skip explícito.
12. Discovery é refeito por `entrySequence`.
13. O resultado de discovery ainda não altera funcionalmente o `ActivitySetupInventory`.
14. `ObjectReset` e `ObjectRelease` continuam fora da F6B.
15. Completion/observação `foreign/stale` não pode alterar o pipeline ativo.

Evidência funcional congelada para `activity_01`:

```text
checkpoint='ActivityObjectContributorDiscovery'
checkpointStatus='Passed'
activityId='activity_01'
entrySequence='1'
discoveryStarted='true'
discoveredCount='1'
targetIds='test_object_01'
roleIds='test_interactable'
contributorKinds='SceneObject'
discoveryCompleted='true'
discoveryFailed='false'
skippedNoContent='false'
```

Evidência de reexecução por nova entry após restart:

```text
checkpoint='ActivityObjectContributorDiscovery'
checkpointStatus='Passed'
activityId='activity_01'
entrySequence='2'
discoveryStarted='true'
discoveredCount='1'
targetIds='test_object_01'
roleIds='test_interactable'
contributorKinds='SceneObject'
discoveryCompleted='true'
discoveryFailed='false'
skippedNoContent='false'
```

Evidência de Activity sem `ActivityContent`:

```text
checkpoint='ActivityObjectContributorDiscovery'
checkpointStatus='Passed'
activityId='activity_02'
entrySequence='3'
discoveryStarted='true'
discoveredCount='0'
targetIds='<none>'
roleIds='<none>'
contributorKinds='<none>'
discoveryCompleted='false'
discoveryFailed='false'
skippedNoContent='true'
```

A mesma regra foi observada novamente para `activity_02` em nova `entrySequence` após restart local:

```text
checkpoint='ActivityObjectContributorDiscovery'
checkpointStatus='Passed'
activityId='activity_02'
entrySequence='4'
discoveryStarted='true'
discoveredCount='0'
skippedNoContent='true'
```

Critérios negativos confirmados:

```text
F6B não executa ObjectReset.
F6B não executa ObjectRelease.
F6B não cria endpoint real de gameplay.
F6B não altera ActivityParticipantBinding.
F6B não altera PlayerActor.
F6B não altera ActivityContent scene release.
F6B não altera RestartCurrentActivity.
F6B não altera Route-exit.
F6B não altera QA para dirigir lifecycle.
```

Smoke correlacionado preservado:

```text
RestartCurrentActivity checkpointStatus='Passed'
Activity01ToActivity02 checkpointStatus='Passed'
```

Conclusão:

```text
F6B fecha a identificação observacional de objetos da ActivityContentScene por entry,
sem transformar o ActivityContentProfile em árvore manual de requirements
e sem iniciar execução de reset/release por objeto.
```

---

## 20. Checkpoint F6C — ObjectReset mínimo por objeto PASS funcional

Status:

```text
F6C = PASS funcional (2026-05-21)
```

Objetivo:

```text
Executar o primeiro ObjectReset mínimo por objeto descoberto na ActivityContentScene,
sem criar regras específicas de gameplay e sem implementar ObjectRelease.
```

Superfície validada:

```text
ActivityObjectResetCommand
ActivityObjectResetResult
ActivityObjectResetResultKind
IActivityObjectResetEndpoint
ActivityObjectDefaultResetEndpoint
ObjectResetStarted
ObjectResetCommandIssued
ObjectResetApplied
ObjectResetSkippedOptional
ObjectResetFailed
ObjectResetCompleted
QACheckpoint ActivityObjectReset
```

Ponto de lifecycle congelado:

```text
ActivityObjectContributorDiscoveryCompleted
-> ActivitySetupInventoryValidated
-> ObjectResetStarted
-> ObjectResetCommandIssued
-> ObjectResetApplied / ObjectResetSkippedOptional / ObjectResetFailed
-> ObjectResetCompleted
-> ActivitySetupCompleted
-> ActivationWindowPresentation / ActivationWindowReady
```

Decisões congeladas:

1. `ObjectReset` é stage do `ActivityEntryPipeline` e roda antes de `ActivitySetupCompleted`.
2. `SessionActivityPipeline` decide quando `ObjectReset` roda.
3. O contributor/objeto não decide lifecycle e não avança pipeline.
4. O stage consome contributors descobertos na entry atual e seus `supportedResetGroups`.
5. Cada reset comandado carrega `Pipeline Identity`, `targetId` e `resetGroup`.
6. Endpoint/adapter executa o reset concreto; pipeline apenas comanda e valida resultado.
7. `ActivityObjectDefaultResetEndpoint` é endpoint explícito de teste/authoring mínimo; não é fallback silencioso global.
8. Contributor sem `supportedResetGroups` gera skip explícito e não bloqueia a Activity.
9. Optional sem endpoint compatível gera skip explícito.
10. Required sem endpoint compatível ou failure válido da entry atual gera falha explícita.
11. Resultado `foreign/stale` não pode avançar nem alterar o pipeline ativo.
12. `ObjectReset` não implementa `ObjectRelease`.
13. `ObjectReset` não implementa save/progression.
14. `ObjectReset` não contém regra específica de porta, pickup, trigger, objetivo ou outro gameplay concreto.
15. `ObjectReset` não altera `ActivityParticipantBinding`, `PlayerActor`, release de `ActivityContentScene`, restart ou route-exit.

Evidência funcional congelada para entrada inicial de `activity_01`:

```text
checkpoint='ActivityObjectReset'
checkpointStatus='Passed'
activityId='activity_01'
entrySequence='1'
resetStarted='true'
commandCount='1'
appliedCount='1'
skippedCount='0'
failedCount='0'
targetIds='test_object_01'
resetGroups='TransformState'
resetCompleted='true'
```

Evidência de reexecução após restart local de `activity_01`:

```text
checkpoint='ActivityObjectReset'
checkpointStatus='Passed'
activityId='activity_01'
entrySequence='2'
resetStarted='true'
commandCount='1'
appliedCount='1'
skippedCount='0'
failedCount='0'
targetIds='test_object_01'
resetGroups='TransformState'
resetCompleted='true'
```

Evidência de Activity sem `ActivityContent`:

```text
checkpoint='ActivityObjectReset'
checkpointStatus='Passed'
activityId='activity_02'
entrySequence='3'
resetStarted='true'
commandCount='0'
appliedCount='0'
skippedCount='0'
failedCount='0'
targetIds='<none>'
resetGroups='<none>'
resetCompleted='true'
```

A mesma regra foi observada novamente para `activity_02` em nova `entrySequence` após restart local:

```text
checkpoint='ActivityObjectReset'
checkpointStatus='Passed'
activityId='activity_02'
entrySequence='4'
commandCount='0'
appliedCount='0'
failedCount='0'
resetCompleted='true'
```

Evidência de retorno posterior para `activity_01` em nova entry:

```text
checkpoint='ActivityObjectReset'
checkpointStatus='Passed'
activityId='activity_01'
entrySequence='5'
resetStarted='true'
commandCount='1'
appliedCount='1'
failedCount='0'
targetIds='test_object_01'
resetGroups='TransformState'
resetCompleted='true'
```

Smoke correlacionado preservado:

```text
RestartCurrentActivity checkpointStatus='Passed'
Activity01ToActivity02 checkpointStatus='Passed'
ActivityObjectContributorDiscovery checkpointStatus='Passed'
```

Critérios negativos confirmados:

```text
F6C não executa ObjectRelease.
F6C não cria regra específica de gameplay.
F6C não altera ActivityParticipantBinding.
F6C não altera PlayerActor.
F6C não altera ActivityContent scene release.
F6C não altera RestartCurrentActivity.
F6C não altera Route-exit.
F6C não altera QA para dirigir lifecycle.
```

Observação:

```text
O smoke validou o caminho funcional normal.
Rejeição foreign/stale de ObjectReset permanece garantia estrutural do contrato,
mas não foi exercitada como evidência funcional neste checkpoint.
```

Conclusão:

```text
F6C fecha o primeiro reset mínimo por objeto como Pipeline Stage comandado,
executado por endpoint explícito e validado por Pipeline Facts/QACheckpoint,
sem transformar Activity em owner de estratégia técnica do objeto.
```

## 21. Checkpoint F6D — ObjectRelease mínimo por objeto PASS funcional

Status:

```text
F6D = PASS funcional (2026-05-21)
```

Objetivo:

```text
Executar o primeiro ObjectRelease mínimo por objeto descoberto na ActivityContentScene,
antes do unload da ActivityContentScene, sem criar regras específicas de gameplay
e sem transferir ownership de release para o objeto, endpoint ou UI.
```

Superfície validada:

```text
ActivityObjectReleaseCommand
ActivityObjectReleaseResult
ActivityObjectReleaseResultKind
IActivityObjectReleaseEndpoint
ActivityObjectDefaultReleaseEndpoint
ObjectReleaseStarted
ObjectReleaseCommandIssued
ObjectReleaseApplied
ObjectReleaseSkippedOptional
ObjectReleaseFailed
ObjectReleaseRejectedForeignOrStale
ObjectReleaseCompleted
QACheckpoint ActivityObjectRelease
```

Ponto de lifecycle congelado:

```text
ActivityDeactivated
-> ObjectReleaseStarted
-> ObjectReleaseCommandIssued
-> ObjectReleaseApplied / ObjectReleaseSkippedOptional / ObjectReleaseFailed
-> ObjectReleaseCompleted
-> ActivityContentReleaseStarted
-> ActivityContentSceneUnloadCommandIssued
-> ActivityContentSceneUnloaded
-> ActivityContentReleaseCompleted
```

Decisões congeladas:

1. `ObjectRelease` é stage de saída do lifecycle local da `SessionActivity`.
2. `SessionActivityPipeline` decide quando `ObjectRelease` roda.
3. O contributor/objeto não decide lifecycle, não inicia unload e não avança pipeline.
4. O stage consome contributors descobertos na entry atual e seus `supportedReleaseKinds`.
5. Cada release comandado carrega `Pipeline Identity`, `targetId` e `releaseKind`.
6. Endpoint/adapter executa o release concreto; pipeline comanda e valida resultado.
7. `ActivityObjectDefaultReleaseEndpoint` é endpoint explícito de teste/authoring mínimo; não é fallback silencioso global.
8. Contributor sem `supportedReleaseKinds` gera skip explícito e não bloqueia a Activity.
9. Optional sem endpoint compatível gera skip explícito.
10. Required sem endpoint compatível ou failure válido da entry atual gera falha explícita e bloqueia `ActivityContentSceneUnload`.
11. Resultado `foreign/stale` gera `ObjectReleaseRejectedForeignOrStale`, não incrementa failure, não avança pipeline e não altera a entry ativa.
12. `ObjectReleaseFailed` fica reservado para failure válido da entry atual.
13. `ObjectRelease` roda antes de `ActivityContentSceneUnload` em Activity -> Activity, Restart e Route-exit.
14. `ObjectRelease` não implementa save/progression.
15. `ObjectRelease` não contém regra específica de porta, pickup, trigger, objetivo ou outro gameplay concreto.
16. `ObjectRelease` não altera `ActivityParticipantBinding`, `PlayerActor`, route/session ownership ou `ActivityContentRelease` além da ordenação necessária.

Evidência funcional congelada para route button durante `ActivationWindow`:

```text
stage='ActivationWindowReady'
RouteButtonRejectedNotRouteExitSafe
blockedReason='session_activity_not_route_exit_safe'
activityId='activity_01'
entrySequence='1'
pendingOperation='<none>'
```

Decisão associada:

```text
Route button não aborta ActivationWindow.
Route button só pode emitir route request quando SessionActivity está route-exit-safe.
AbortActivation, se existir no futuro, deve ser comando/policy próprio do SessionActivityPipeline.
```

Evidência funcional congelada para route-exit após `ActivityRunning`:

```text
OperationalRouteRequestDeferredForSessionActivityTeardown
SessionActivityRouteExitTeardownStarted
SessionActivityRouteExitTeardownInProgress stage='DeactivationWindowAdditiveSceneLoadStarted'
DeactivationWindowReady
CompleteDeactivationWindow
```

Evidência de `ObjectRelease` em route-exit:

```text
checkpoint='ActivityObjectRelease'
checkpointStatus='Passed'
activityId='activity_01'
entrySequence='1'
releaseStarted='true'
commandCount='1'
appliedCount='1'
skippedCount='0'
failedCount='0'
targetIds='test_object_01'
releaseKinds='UnloadActivityContentScene'
releaseCompleted='true'
```

Evidência de `ActivityContentRelease` e fechamento de route-exit:

```text
checkpoint='RouteExitBackToMenu'
checkpointStatus='Passed'
activityId='activity_01'
entrySequence='1'
releaseStarted='true'
releaseCompleted='true'
releaseSceneName='ActivityScene01'
releaseStatus='Unloaded'
releaseSceneIsLoadedAfterRelease='false'
closedForRouteExit='true'
routeExitTeardownCompleted='true'
```

Evidência operacional correlacionada:

```text
SessionActivityRouteExitTeardownCompleted
teardownResult.kind='Completed'
stage='ClosedForRouteExit'
hasPendingHandoff='False'
reason='route_exit_completed'
```

Sequência validada no smoke F6D:

```text
ActivityRunning activity_01
-> BackToMenu permitido
-> SessionActivityRouteExitTeardownStarted
-> DeactivationWindowReady
-> CompleteDeactivationWindow
-> ObjectRelease Passed
-> ActivityContentSceneUnload
-> RouteExitBackToMenu Passed
-> ClosedForRouteExit
-> SessionActivityRouteExitTeardownCompleted
-> ActivityCamera release
-> RouteActivitySave skip no_snapshot_provider
-> SceneComposition MenuScene
-> OperationalRouteCompleted
```

Critérios negativos confirmados:

```text
F6D não executa Save/Progression.
F6D não cria regra específica de gameplay.
F6D não altera ActivityParticipantBinding.
F6D não altera PlayerActor.
F6D não altera ownership de route/session.
F6D não cria bypass de ActivationWindow.
F6D não transforma route button em owner de lifecycle.
```

Observação:

```text
O smoke validou o caminho funcional normal e a rejeição de route button não-safe durante ActivationWindow.
A rejeição foreign/stale de ObjectRelease foi corrigida estruturalmente por ObjectReleaseRejectedForeignOrStale,
mas não foi exercitada como evidência funcional dedicada neste checkpoint.
```

Conclusão:

```text
F6D fecha o primeiro release mínimo por objeto como Pipeline Stage comandado,
executado por endpoint explícito e validado por Pipeline Facts/QACheckpoint,
antes do unload de ActivityContentScene e antes de ClosedForRouteExit.
```

## 22. Unsupported v0 / Scope guard

v0 suporta conceitualmente:

```text
ActivityContentProfile
Load/PrepareActivityContent obrigatório
ActivityEntryPipeline único
ActivitySetupInventory
ActivityObjectContributor discovery observacional por entry
ObjectReset mínimo por objeto
ObjectRelease mínimo por objeto
StateResetRequirements
ResetGroups v0
ReleasePreviousActivityContent
ReloadContentOnRestart
WindowTemplateLibrary route-scoped
Window presentations em Standby/reset
ObjectReset/ObjectRelease boundary por objeto
```

v0 não suporta ainda:

```text
KeepRecentActivityContent real
RetainUntilRouteExit real
ResetContentOnRestart real
ref-count de content scenes compartilhadas
content scenes compartilhadas entre entries retidas
seamless real
save/restore real de objetos
runtime spawn completo
object release completo com todos os tipos
budgeted retention/preload
manual release por gameplay
Progression snapshot real por objeto
```

Unsupported deve ser explícito. Não criar fallback silencioso.

---

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
- Todo command relevante carrega `Pipeline Identity`.
- Foreign/stale events não alteram pipeline ativo.
- Nenhum adapter decide lifecycle.
- Nenhum objeto/contributor decide quando o pipeline avança.
- Não criar Base 2.0 agora.
- Não criar core genérico universal agora.
- Não reorganizar fisicamente arquitetura em Core/Concrete/UnityAdapter.

---

## 24. Auditorias obrigatórias antes de implementação pesada

Antes de implementar novas partes deste ADR no runtime, auditar:

```text
ActivityAsset / ActivityCatalogAsset atuais
ActivationWindow / DeactivationWindow atuais
Window AdditiveScene atual
ActivityTransition atual
PlayerActorSetup/Reset atual
ActivitySceneContract atual
SceneKeyAsset e scene loading atual
CameraPresentation / ActivityCamera atual
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

## 25. Relação com ADRs existentes

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

Restart/reset deste ADR não são Save/Restore. Progression Save permanece por slots/snapshots e providers futuros.

### ADR-0009 / ADR-0010

PlayerSlot é capacidade operacional. PlayerActor nasce no setup local quando comandado por requirement. Player não é propriedade semântica da Activity.

### ADR-0012 / ADR-0013

Operational Camera, Route Camera, Activity Camera e Window Camera permanecem camadas separadas. Este ADR registra prioridade e uso dentro do ActivityEntry/Window lifecycle.

---

## 26. Consequências

- Activity deixa de ser confundida com cena.
- Windows deixam de ser confundidas com conteúdo jogável.
- A rota pode fornecer templates de janela sem virar owner de lifecycle da janela.
- `ActivityContent` passa a ser específico, carregável, rastreável e liberável por entry.
- O catálogo de activities não vira pacote de scenes a carregar integralmente.
- O setup da Activity passa a convergir por inventário e stages, não branches especiais.
- Reset, release e restart deixam de ser efeitos soltos.
- O sistema fica preparado para migrar componentes reais gradualmente, adicionando ResetGroups, Save providers e ObjectEntry adapters quando houver necessidade concreta.

---

## 27. Critérios de aceite futuro

Este ADR poderá ser considerado implementado quando houver evidência de runtime para:

```text
ActivityContentProfile resolvido por Activity.
Load/PrepareActivityContent emitindo facts e commands com identity.
Activity sem content emitindo skip explícito.
Content scenes additive sem SetActiveScene.
Contributor discovery após content load. [Encerrado pela F6B]
ActivitySetupInventory construído por entry.
Subplanos vazios emitindo skip explícito.
ResetGroups v0 comandados por pipeline.
ObjectReset por objeto comandado por pipeline. [Encerrado pela F6C]
ObjectRelease por objeto comandado por pipeline. [Encerrado pela F6D]
ActivityContentRelease antes de ClosedForRouteExit em route-exit. [Encerrado pela F5E2]
RestartCurrentActivity criando nova entrySequence e recarregando content no v0. [Encerrado pela F5E1]
WindowTemplateLibrary não duplicada nem descarregada por window close.
Comandos stale/foreign rejeitados.
Nenhum fallback silencioso para Route Scene quando content obrigatório faltar.
```

---

## 28. Checkpoint F4D3-F4D6d — PASS funcional

Objetivo:

```text
Remover ownership antigo de setup de player no domínio Activity
e consolidar ActivityParticipantBinding como rail canônico local.
```

Decisões congeladas:

1. `SessionActivityPipeline` não mantém snapshot legado de seleção de player.
2. `PlayerActorSetupStage` e contratos legados de seleção foram removidos do rail ativo.
3. Activity não declara mais fonte técnica de player.
4. Player continua `RouteSession-owned`.
5. `SessionActivityEntryHandoff.TechnicalPlanEntries` e `PlayerPreparation` continuam preservados como dados vindos da rota/sessão.
6. `ActivityParticipantBinding` e `ActivityParticipant*CommandIssued/*Applied` são o rail canônico de setup local.

---

## 29. Checkpoint F4D7 — PASS funcional

Objetivo:

```text
Convergir a linguagem de participantes de rota/sessão no SessionOperationalPipeline.
```

Decisão congelada:

```text
OperationalRouteAsset.playerSetDefinition foi renomeado para routeParticipantSetDefinition.
```

A fonte técnica de participantes permanece na rota/sessão, não na Activity.

Garantias:

1. Activity não volta a declarar `playerSetDefinition` como fonte semântica.
2. Activity não volta a declarar `RequiresPlayerActor`.
3. `ParticipantRequirement` segue como fonte semântica da Activity.
4. Route/Session segue como fonte técnica de participant/player preparation.
5. Player continua `RouteSession-owned`.
6. `ActivityParticipantBinding` continua como rail canônico de setup local.

---

## 30. Checkpoint F4D8 — PASS funcional

Objetivo:

```text
Fechar o branch de Activity sem ParticipantRequirements com skip explícito e observabilidade suficiente.
```

Shape congelado:

```text
ActivitySetupInventoryBuilt totalRequirements='0'
ActivitySetupInventoryValidated totalRequirements='0'
ActivityParticipantBindingStarted
ActivityParticipantBindingSkippedNoRequirements totalRequirements='0' routeSessionParticipantPreparationConsumed='false'
ActivityParticipantBindingCompleted resolved='0' skipped='0' totalRequirements='0' status='SkippedNoRequirements'
ActivitySetupCompleted
ActivityActivationStarted
ActivityRunning
```

Critérios negativos:

```text
ActivityParticipantCommandPlanReady não aparece para Activity sem requirements.
ActivityParticipant*CommandIssued não aparece para Activity sem requirements.
ActivityParticipant*Applied não aparece para Activity sem requirements.
PlayerPreparation/TechnicalPlanEntries não são consumidos quando totalRequirements='0'.
```

---

## 31. Checkpoint F5B-F5D — PASS estrutural + PASS funcional

Status:

```text
F5B -> F5C.2 = PASS estrutural (2026-05-21)
F5D = PASS funcional (2026-05-21)
```

Objetivo:

```text
Completar o lifecycle mínimo de ActivityContent no fluxo Activity -> Activity.
```

Superfície estrutural preparada:

```text
SessionActivityPendingOperationKind.ActivityContentSceneUnload
ActivityContentSceneUnloadCommand
ActivityContentSceneUnloadResult
ActivityContentUnloadResultKind
IActivityContentSceneReleaseAdapter
UnityActivityContentSceneReleaseAdapter
CompleteActivityContentSceneUnloadOperation(..., ActivityContentSceneUnloadResult)
ActivityContentSceneUnloadRejected
```

Decisões congeladas:

1. `ActivityContentSceneUnloadCommandIssued` pertence ao dispatch do command, não ao callback de completion.
2. `ActivityContentSceneUnloaded` pertence à completion válida do adapter/runner.
3. `releaseStatus` vem de `ActivityContentSceneUnloadResult.Kind`.
4. Completion stale/foreign de unload deve ser rejeitada e observável por `ActivityContentSceneUnloadRejected`.
5. Failure válida de unload emite `ActivityContentReleaseFailed` e não deve avançar o pipeline.
6. Adapter executa unload; pipeline decide quando descarregar.
7. Runner executa pending operation; runner não decide retention policy.

Fluxo funcional congelado Activity -> Activity:

```text
ActivityDeactivated
-> ActivityContentReleaseStarted
-> ActivityContentRetentionPlanResolved policy='ReleaseByDefault'
-> ActivityContentSceneUnloadCommandIssued sceneName='ActivityScene01'
-> ActivityContentSceneUnloaded sceneName='ActivityScene01' releaseStatus='Unloaded'
-> ActivityContentReleaseCompleted
-> ActivityTransition / ContinueAccepted
-> ActivityContentLoadSkippedNoContent activity_02
-> ActivitySetupCompleted activity_02
-> ActivityRunning activity_02
```

Evidência funcional congelada:

```text
QACheckpoint checkpoint='Activity01ToActivity02'
checkpointStatus='Passed'
fromActivity='activity_01'
toActivity='activity_02'
releaseStarted='true'
releaseCompleted='true'
releaseSceneName='ActivityScene01'
releaseStatus='Unloaded'
releaseSceneIsLoadedAfterRelease='false'
activity02ReachedRunning='true'
activity02SkipNoRequirementsObserved='true'
participantCommandsForActivity02Observed='false'
```

---

## 32. Checkpoint F5QA — PASS funcional

Objetivo:

```text
Manter QA manual, objetivo e alinhado ao caminho canônico do SessionActivityPipeline.
```

Decisão congelada:

```text
Não há botão de smoke que dirige o pipeline.
O smoke é manual.
O QA apenas chama ações canônicas e imprime evidências compactas automaticamente.
```

Botões principais mantidos no painel QA:

```text
CompleteActivationWindow
CompleteCurrentActivity
CompleteDeactivationWindow
RestartCurrentActivity
```

Regras:

1. QA não decide lifecycle.
2. QA não altera stage diretamente.
3. QA não limpa pending operation.
4. QA não cria handoff.
5. QA não executa adapter diretamente.
6. QA não tenta dirigir trilhos async por botão de smoke.
7. QA chama apenas APIs canônicas do `SessionActivityHost`.
8. Smokes são manuais e usam os botões reais do ciclo local.
9. Evidência de checkpoint vem por `QACheckpoint` automático compacto.
10. `QACheckpoint` deve ter escopo por transição e por `entrySequence`.
11. Checkpoint aprovado deve ser congelado e não pode ser invalidado por ciclos posteriores.

---

## 33. Checkpoint F5E1 — PASS funcional

Objetivo:

```text
RestartCurrentActivity deve liberar ActivityContent da entry antiga antes de iniciar a nova entry.
```

Fluxo congelado para `activity_01` com `ActivityContent`:

```text
ActivityRunning activity_01 entrySequence=N
-> RestartCurrentActivity
-> DeactivationWindowReady
-> CompleteDeactivationWindow
-> ActivityContentReleaseStarted entrySequence=N
-> ActivityContentSceneUnloadCommandIssued sceneName='ActivityScene01'
-> ActivityContentSceneUnloaded releaseStatus='Unloaded'
-> ActivityContentReleaseCompleted entrySequence=N
-> nova entrySequence=N+1
-> ActivityContentSceneLoading / ActivationWindowReady
```

Evidência congelada:

```text
checkpoint='RestartCurrentActivity'
checkpointStatus='Passed'
fromActivity='activity_01'
toActivity='activity_01'
releaseStarted='true'
releaseCompleted='true'
releaseSceneName='ActivityScene01'
releaseStatus='Unloaded'
releaseSceneIsLoadedAfterRelease='false'
newEntryStarted='true'
newEntryReachedActivationWindowReady='true'
```

Fluxo congelado para `activity_02` sem `ActivityContent`:

```text
ActivityRunning activity_02 entrySequence=N
-> RestartCurrentActivity
-> ActivityRunning activity_02 entrySequence=N+1
```

Evidência congelada:

```text
checkpoint='RestartCurrentActivity'
checkpointStatus='Passed'
fromActivity='activity_02'
toActivity='activity_02'
releaseStarted='false'
releaseCompleted='false'
newEntryStarted='true'
newEntryReachedActivityRunning='true'
```

Decisões:

1. Restart não usa ActivityTransition.
2. Restart não troca de Activity.
3. Restart não usa rota/navigation.
4. Restart cria nova `entrySequence` da mesma Activity.
5. Se houver `ActivityContent`, release é obrigatório antes da nova entry.
6. Se não houver `ActivityContent`, restart pode seguir direto para nova entry com skip/no-content explícito ou ausência aceita de release obrigatório.
7. `CurrentActivityContentLoadedSet` não pode ser limpo antes de `ActivityContentReleaseCompleted`.
8. `ActivityContentSceneUnloadCommandIssued` continua pertencendo ao dispatch, não à completion.
9. Route-exit permanece fluxo separado.

---

## 34. Checkpoint F5E2 — PASS funcional

Objetivo:

```text
Route-exit / BackToMenu deve liberar ActivityContent antes de ClosedForRouteExit e antes do SessionOperationalPipeline continuar a troca de rota.
```

Fluxo funcional congelado:

```text
BackToMenu
-> OperationalRouteRequestDeferredForSessionActivityTeardown
-> SessionActivityRouteExitTeardownStarted
-> DeactivationWindowReady
-> CompleteDeactivationWindow
-> ActivityContentReleaseStarted
-> ActivityContentRetentionPlanResolved policy='ReleaseByDefault'
-> ActivityContentSceneUnloadCommandIssued sceneName='ActivityScene01'
-> ActivityContentSceneUnloaded releaseStatus='Unloaded'
-> ActivityContentReleaseCompleted
-> ClosedForRouteExit
-> SessionActivityRouteExitTeardownCompleted kind='Completed'
-> ActivityCamera release
-> RouteActivitySave save/skip
-> TransitionPlanReady
-> fadeIn
-> ApplyOperationalRoute MenuScene
-> UnloadSceneCompleted SessionActivitySandboxScene
-> OperationalRouteCompleted
```

Evidência congelada do `QACheckpoint RouteExitBackToMenu`:

```text
checkpoint='RouteExitBackToMenu'
checkpointStatus='Passed'
activityId='activity_01'
entrySequence='1'
releaseStarted='true'
releaseCompleted='true'
releaseSceneName='ActivityScene01'
releaseStatus='Unloaded'
releaseSceneIsLoadedAfterRelease='false'
closedForRouteExit='true'
routeExitTeardownCompleted='true'
menuRouteApplied='false'
```

Observação:

```text
menuRouteApplied é campo observacional, não critério obrigatório do SessionActivity QACheckpoint.
A aplicação de MenuScene pertence ao escopo do SessionOperationalPipeline.
```

Decisões:

1. `SessionOperationalPipeline` deve deferir route change quando há `SessionActivity` ativa.
2. `SessionActivityPipeline` é owner do teardown local da Activity.
3. `ActivityContent` carregado deve ser liberado antes de `ClosedForRouteExit`.
4. `ClosedForRouteExit` não pode ocorrer antes de `ActivityContentReleaseCompleted` quando há content carregado.
5. `SessionActivityRouteExitTeardownCompleted kind='Completed'` não pode ocorrer antes de `ClosedForRouteExit`.
6. `SessionOperationalPipeline` só pode continuar side-effects operacionais depois do teardown completed.
7. Failure válida de unload não pode emitir `ClosedForRouteExit`.
8. Completion stale/foreign de unload não pode alterar o pipeline ativo.
9. `Route Scene`, `WindowTemplateLibrary` route-scoped, players/participants route-session-owned, save, camera e rota operacional permanecem fora do ownership de `ActivityContentRelease`.

Critérios negativos confirmados:

```text
ClosedForRouteExit não ocorre antes de releaseCompleted='true'.
SessionActivityRouteExitTeardownCompleted não ocorre antes de ClosedForRouteExit.
ActivityCamera release, RouteActivitySave e TransitionPlanReady ocorrem depois do teardown completed.
ApplyOperationalRoute MenuScene ocorre depois do teardown completed.
```

Higiene futura:

```text
Suprimir ruído residual de QA Activity01ToActivity02 Waiting após RouteExitBackToMenu aprovado.
```

---

## 35. Próximas frentes após F6D

Com F5D, F5E1, F5E2, F6A, F6B, F6C e F6D congeladas, o lifecycle mínimo de `ActivityContent` está fechado para cenas, o discovery observacional por entry já foi validado, `ObjectReset` roda na entrada e `ObjectRelease` roda na saída antes do unload da `ActivityContentScene`:

```text
Activity -> Activity
RestartCurrentActivity
Route-exit / BackToMenu
ActivityObjectContributor discovery por entry
ObjectReset mínimo por objeto antes de ActivationWindow
ObjectRelease mínimo por objeto antes de ActivityContentSceneUnload
Route button guard para impedir route request durante ActivationWindow
Route-exit / BackToMenu validado com ObjectRelease + ActivityContentRelease
```

Próximas frentes recomendadas:

1. `F6E` — Contributor unregister / cleanup de contributors descobertos por ActivityContent, se necessário.
2. `F6F` — ActivityContent Retention policies avançadas, mantendo `KeepRecentActivityContent` e `RetainUntilRouteExit` explicitamente unsupported até haver caso concreto.
3. `F7` — Progression snapshot providers reais de Activity/Object, separado de release/reset.
4. Guard estrutural futuro no `SessionOperationalPipeline` para rejeição limpa de route request não-safe, sem depender apenas do binder/UI.
