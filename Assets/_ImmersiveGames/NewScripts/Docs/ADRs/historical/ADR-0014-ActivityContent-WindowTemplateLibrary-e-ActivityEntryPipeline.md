# ADR-0014 — ActivityContent, WindowTemplateLibrary e ActivityEntryPipeline

## Status

- Estado: ACEITO / checkpoint normativo vivo da Base 1.1
- Data: 2026-05-19
- Última atualização: 2026-05-22
- Tipo: Direction / Canonical architecture / Base 1.1 checkpoint
- Fonte de verdade canônica deste contrato: este ADR, após aceite.

---

## 1. Decisão central

A Base 1.1 — Pipeline Convergence / Convergência para Pipelines Determinísticos — adota um único pipeline de entrada de Activity:

```text
ActivityEntryPipeline
```

Toda Activity passa pelo mesmo lifecycle. O que varia entre Activities é o `ActivitySetupInventory` resolvido, não a ordem global do pipeline.

Separação normativa:

```text
Activity = lifecycle determinístico local.
ActivityContent = conteúdo jogável/material usado pela Activity.
ActivationWindow = janela autoral de entrada.
DeactivationWindow = janela autoral de saída.
Route Scene = superfície/base da rota.
WindowTemplateLibrary = capacidade visual compartilhada route-scoped para janelas.
```

Ownership:

- `SessionActivityPipeline` decide lifecycle local da Activity, `ActivityEntryPipeline`, `ActivityContent`, windows autorais, setup, reset, release, restart e route-exit local.
- `SessionOperationalPipeline` decide rota, transição operacional, `SceneComposition` de rota, loading/fade de rota, save/load operacional de rota/activity e handoff inicial para `SessionActivityPipeline`.
- Adapters executam side-effects comandados.
- Objetos/contributors produzem requisitos, facts, commands ou capacidades; não decidem lifecycle.

---

## 2. Conceitos normativos

### Activity

`Activity` é lifecycle local:

```text
setup -> activation -> running -> deactivation -> transition/release
```

A Activity não é uma cena, não é seu conteúdo e não é sua window.

### ActivityContent

`ActivityContent` é conteúdo jogável/material específico da Activity. Pode conter cenas additive, markers, props, objetos interativos, contributors, contracts, pontos de placement, rigs ou metadados de câmera.

`ActivityContent` não é `WindowTemplateLibrary` e não substitui a `Route Scene`.

### ActivationWindow / DeactivationWindow

Windows são janelas autorais visíveis, não setup técnico e não release técnico.

Regras:

- não auto-completam;
- exigem comando explícito quando presentes;
- podem usar payload da Activity, Route Scene e ActivityContent;
- podem usar camera rig/profile próprio;
- não são fallback para resolver setup quebrado.

### Route Scene

A `Route Scene` pertence ao domínio operacional da rota. A `SessionActivityPipeline` não descarrega a `Route Scene`.

### WindowTemplateLibrary

`WindowTemplateLibrary route-scoped` permanece o shape final desejado para janelas compartilhadas por rota, mas **não é pendência ativa do checkpoint atual**.

O runtime v0 ativo usa windows additive simples:

```text
ActivationWindow / DeactivationWindow
-> ActivityWindowMode.None ou AdditiveScene
-> load
-> ready
-> complete explícito
-> unload
```

Esse trilho additive v0 está aceito enquanto não houver necessidade concreta de templates compartilhados, standby, payload bind/unbind ou reaproveitamento visual entre Activities.

Regra final futura para `WindowTemplateLibrary`:

```text
window close != UnloadScene da template
window close = unbind payload + reset presenter + standby
route-exit = release/descarregamento da WindowTemplateLibrary
```

Reabrir `WindowTemplateLibrary route-scoped` somente quando houver necessidade concreta de:

- templates compartilhados por rota;
- window scenes em `Standby`;
- payload bind/unbind;
- reaproveitamento visual real entre Activities;
- release da biblioteca somente no route-exit.

---

## 3. ActivityContentProfile e scenes

`ActivityContent` é declarado por `ActivityContentProfile`, não apenas por `SceneKeyAsset[]`.

Campos conceituais:

```text
contentIdentity
contentSceneKeys[]
activityCameraProfile / camera requirement
scene contract metadata
contributor discovery policy
loading/preparation metadata
```

Regras de scenes:

```text
LoadSceneMode.Additive
não SetActiveScene
não substituir Route Scene
não usar WindowSceneAdapter semanticamente
não usar fallback silencioso para Route Scene
```

Toda Activity passa por `Load/PrepareActivityContent`.

- sem conteúdo: `ActivityContentLoadSkippedNoContent`;
- com conteúdo: `LoadActivityContentScene -> ActivityContentSceneLoaded`.

Toda operação de conteúdo carrega `Pipeline Identity`, incluindo:

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

Completion stale/foreign não pode alterar o pipeline ativo.

---

## 4. ActivityEntryPipeline e ActivitySetupInventory

O `ActivityEntryPipeline` é único para qualquer Activity.

Ordem conceitual:

```text
ResolveActivityEntry
ResolveActivityContentProfile
Load/PrepareActivityContent
DiscoverActivityContributors
BuildActivitySetupInventory
ValidateActivitySetupInventory
ParticipantSetupStage
ObjectEntrySetupStage
PlacementSetupStage
CameraBindingSetupStage
InteractionBindingSetupStage
HudBindingSetupStage
WarmupSetupStage
ActivitySetupReadinessValidation
ActivitySetupCompleted
ActivationWindowPresentation ou ActivationWindowSkippedNoContent
CompleteActivationWindow, se houver window
ActivityRunning
```

Subplanos possíveis no `ActivitySetupInventory`:

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

- requisito obrigatório ausente = fail-fast;
- requisito opcional ausente = skip explícito;
- subplano vazio = skip explícito;
- stage produz `Pipeline Fact`, `Pipeline Command`, `Skipped` ou `Failed`;
- adapters executam side-effects comandados;
- `Completed` avança;
- `Skipped` avança quando vazio/opcional;
- `Failed` bloqueia/fail-fast.

---

## 4.1 Escopo normativo — Activity, Actor, ActivityObject e capability local

Este ADR não autoriza tratar `Activity`, `Actor`, `ActivityObject` e gameplay local como o mesmo owner.

Separação obrigatória:

| Item | Owner de decisão | Executor / reação local |
|---|---|---|
| Entrada da Activity | `SessionActivityPipeline` / `ActivityEntryPipeline` | stages e adapters comandados |
| Ordem de setup/readiness | `ActivityEntryPipeline` | stages específicos |
| Descoberta de capability | scanners/contributors autorizados | `ActivityCapabilityInventory` |
| Reset timing | pipeline/stage | endpoint local de objeto/actor |
| Snapshot timing | pipeline dono do ciclo/save boundary | provider local |
| Restore timing | pipeline/stage | endpoint local |
| Release timing | pipeline/stage | endpoint local / adapter |
| Mutação local de gameplay | capability local | endpoint/runtime local |

`ActivityObject` e `Actor` são conceitos separados:

```text
Um objeto pode ser ActivityObject sem ser Actor.
Um Actor pode expor ActivityObject-like capabilities, mas isso deve ser explícito.
Um Actor deve ter identidade, participation e lifecycle de ator.
Um ActivityObject deve ter participação/capability local, não identity de ator implícita.
```

A `Activity` decide lifecycle, policies, readiness e timing. A instância local decide como executar sua própria capability. Dano, heal, stamina, interação local e variações internas de atributo não entram no pipeline como commands obrigatórios quando podem ser resolvidos por relação local segura.

## 5. Participante controlável v0

`Player` não é propriedade semântica da Activity.

```text
Player = participante/capacidade da rota/sessão.
Activity entry = pode exigir, posicionar, bindar ou resetar esse participante.
```

A Activity não deve ter branches de lifecycle como “com player”/“sem player”. Ela resolve requisitos no inventário.

### PlayerActor readiness — PASS

Checkpoint congelado:

```text
ActivityParticipantBindingCompleted
-> PlayerActorReadinessStarted
-> PlayerActorReadyMaterializedOnly, quando obrigatório e válido
-> PlayerActorReadinessCompleted
-> ActivitySetupCompleted
```

Sem participante obrigatório, o stage emite skip explícito e completa readiness.

### PlayerInputBindingStage — PASS

O `PlayerInputBindingStage` liga o `PlayerInput` do `PlayerActor` materializado ao asset canônico já validado pelo runtime de input.

Regras:

- usa asset canônico do `RuntimeConfigRegistry`/InputModes;
- não provisiona `PlayerInput` operacional paralelo;
- pode rebinder explicitamente `PlayerInput.actions` para o asset canônico;
- não usa reflection/UnityEditor runtime;
- não decide lifecycle.

### MovementBindingStage + MovementControl — PASS / transição controlada

Separação obrigatória:

```text
binding/preparation != permissão de execução local
```

`MovementBindingStage` prepara reader/controller e deixa controle bloqueado.

`MovementControlEnabled` ocorre somente em `ActivityRunning`.

`MovementControlDisabled` ocorre em complete/deactivation/route-exit.

Binding retido em Activity skip/no-content é permitido quando validado por identity/registry.

Decisão complementar Base 1.2:

```text
MovementControlStage é checkpoint transitório validado, não padrão final para novas capabilities.
```

A direção normativa futura é que o pipeline publique `ActivityCapabilityPermission` semântica, como `activity.gameplay.control`, e que `PlayerMovementController` ou receiver local reaja a essa permission.

O `ActivityEntryPipeline` continua owner de setup/binding/readiness. Ele não deve virar owner de controle individual de attack, interaction, inventory, NPC brain ou outras funções internas.

---

## 6. PlacementSetupStage v0 — FECHADO COMO IMPLÍCITO

Classificação: `B) placement mínimo já coberto implicitamente`.

No checkpoint atual, não existe `PlacementSetupStage` nominal separado no código ativo. O placement mínimo de `PlayerActor` está materializado no caminho ativo por:

```text
PlayerActorSetupStage
-> PlayerActor materialization
-> PlayerActorResetGroup.Placement
-> PlayerActorDefaultResetEndpoint
```

Fonte de dados:

```text
SessionActivityPlayerTechnicalPlanEntry
<- PlayerSetDefinition
<- ActorDefinition
```

Modos aceitos no v0 atual:

```text
SceneMarker + placementKey
FixedTransform + local transform
```

Ownership:

- `SessionActivityPipeline / ActivitySetup` é owner semântico do placement v0.
- `SessionOperationalPipeline` apenas transporta plano técnico no handoff; não posiciona `PlayerActor`.
- `PlayerActorMaterializationAdapter` e `PlayerActorResetAdapter` executam side-effects.
- Required placement inválido deve falhar explicitamente.

Este fechamento remove da lista ativa a pendência “implementar `PlacementSetupStage` explícito”.

Reabrir somente se surgir requirement próprio que não caiba no `PlayerActorSetup/Reset` atual, como:

- múltiplos targets de placement por Activity;
- placement de objetos não-player com policy própria;
- placement dependente de contributors de `ActivityContent`;
- troca dinâmica de marker por Activity;
- necessidade de ordenar placement separadamente de materialização/reset.

---

## 7. Window AdditiveScene v0 — ACEITO / WindowTemplateLibrary futura

Classificação: `D) parcialmente coberto pelo sistema atual de window additive scenes`.

Não existe `WindowTemplateLibrary route-scoped` runtime no caminho ativo atual. Isso não é déficit funcional do sandbox v0.

O caminho ativo de windows é:

```text
ActivityWindowMode.None
ou
ActivityWindowMode.AdditiveScene
```

No modo `AdditiveScene`, o `SessionActivityPipeline` executa:

```text
ActivationWindowSceneLoad
-> ActivationWindowReady
-> CompleteActivationWindow
-> ActivationWindowSceneUnload
-> ActivityRunning
```

E, na saída:

```text
DeactivationWindowSceneLoad
-> DeactivationWindowReady
-> CompleteDeactivationWindow
-> DeactivationWindowSceneUnload
-> ActivityDeactivated
```

Esse load/unload da window scene faz parte do contrato v0 atual. Ele não deve ser tratado como bug nem como blocker arquitetural enquanto as windows forem simples/QA.

Permanece normativo:

- `SessionActivityPipeline` é owner do lifecycle das windows;
- `CompleteActivationWindow` e `CompleteDeactivationWindow` permanecem comandos explícitos;
- pending operations e identity continuam obrigatórios;
- ausência/configuração inválida de window obrigatória continua fail-fast;
- não há fallback silencioso para completar window sem contrato.

Fica fora da pendência ativa:

```text
Implementar WindowTemplateLibrary route-scoped agora.
```

`WindowTemplateLibrary route-scoped` permanece futuro explícito e só deve ser reaberto com necessidade real de template compartilhado, standby, payload bind/unbind ou reuse visual entre Activities.

---

## 8. CameraBindingSetupStage v0 — PASS

Para câmera solo atual, o `PlayerActor` não carrega `Camera` nem `CinemachineBrain` como filhos obrigatórios.

O `PlayerActor` expõe:

```text
PlayerCameraEndpoint
-> CameraFollowTarget
-> CameraLookAtTarget
```

Regras:

- `OperationalCameraRuntime` fornece output camera / `CinemachineBrain`.
- `CameraPresentation` fornece rigs/directors/virtual cameras.
- `SessionActivityPipeline` decide binding da ActivityCamera ao `PlayerCameraEndpoint`.
- `CinemachineActivityCameraDirector` / adapter executam o rebind técnico.
- `PlayerActor` expõe anchors/endpoints; não decide lifecycle de câmera.
- `PlayerInput.camera` fica reservado para split-screen futuro.

Checkpoint congelado:

```text
ActivityCameraPrepared
PlayerCameraEndpointResolved
ActivityCameraTargetsRebound
ActivityCameraTargetBound
CameraBindingCompleted
```

A identidade correta para rebind é a identidade da sessão de Activity (`SessionActivitySandboxSession`), não o `activityId` local.

Activity sem `CameraBindingRequirements` pode emitir `CameraBindingSkippedNoRequiredCamera`. Isso não implica release da câmera ativa já preparada.

---

## 9. Activity Reset / StateReset

Reset pertence ao `ActivityEntryPipeline / ActivitySetup`.

Reset não é:

```text
Release
Restore de Save
Activation
```

Regras:

- pipeline comanda reset;
- adapters/endpoints executam;
- objetos/participants aplicam seus próprios campos resetáveis;
- objetos não decidem quando resetar;
- reset obrigatório ausente/falho bloqueia `ActivitySetupCompleted`;
- reset opcional ausente gera skip explícito;
- `ResetAll` cego é proibido.

ResetGroups v0:

```text
Placement
ActivityParticipation
TransformState
RuntimeTransient
InteractionState
ObjectiveState
```

Grupos avançados ficam fora do v0 até componentes reais exigirem:

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

---

## 10. ActivityContent Retention / Release

`Deactivation` não implica `Release`.

```text
ActivityContentRetention = policy de manter/liberar conteúdo deactivated.
ActivityContentRelease = processo determinístico de liberação comandado pelo SessionActivityPipeline.
```

Release pode incluir:

- object release;
- contributor unregister;
- binding cleanup;
- HUD cleanup;
- camera release;
- interaction release;
- unload de `ActivityContentScenes`;
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

v0 runtime suporta:

```text
ReleasePreviousActivityContent
```

Futuro/unsupported v0:

```text
KeepRecentActivityContent(count)
RetainUntilRouteExit
ResetContentOnRestart
```

Route-exit só pode declarar `ClosedForRouteExit` depois do release obrigatório de `ActivityContent`.

---

## 11. Pooling canônico — SEM AÇÃO ATIVA

O projeto já possui pooling canônico transversal:

```text
IPoolService
PoolService
PoolDefinitionAsset
GameObjectPool
PoolRuntimeHost
PoolRuntimeInstance
PoolAutoReturnTracker
IPoolableObject
PooledBehaviour
PoolingQaContextMenuDriver
```

Este ADR não exige criar pooling nem integrar pooling agora por antecipação.

Pooling só entra em `ActivityEntryPipeline`, `ObjectEntry`, `RuntimeSpawn`, `ActivityContentRelease` ou `ObjectRelease` quando houver:

- objeto concreto;
- policy explícita;
- requisito real de reuse/pool.

Regras:

- não criar pooling paralelo em `SessionActivity`;
- não criar pool ad hoc dentro de `ActivityContentRelease`;
- não destruir por padrão objeto que deveria retornar ao pool;
- quando policy/ownerScope indicar pool, usar `IPoolService` canônico.

---

## 12. Save / Progression boundary

`SessionActivityPipeline` não salva progression diretamente.

Boundary ativa:

- `SessionActivityPipeline` decide timing de capture/restore local da Activity.
- `SessionOperationalPipeline` decide load/save operacional por rota/activity.
- `SaveRuntime` persiste por comando; não decide lifecycle.
- provider/endpoint lê/aplica estado local comandado.

Progression Save real completo está fora do escopo ativo atual da Activity. Reabrir somente quando houver progressão real de jogo para salvar, como inventário, objetivos persistentes, estado de actors, mundo, run continuity ou UI de slots.

MVP validado:

```text
RouteActivitySave + ActivityObjectSnapshotRestore para test_object_01
```

Ordem congelada:

```text
ActivityObjectSnapshotContractValidation
-> ActivityObjectReset
-> ActivityObjectSnapshotRestore
```

---

## 13. RestartCurrentActivity — PASS

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

Checkpoint validado em 2026-05-22:

- restart com `ActivityContent` real passou por release/unload/reload e nova `entrySequence`;
- restart em Activity sem conteúdo passou por conclusão semântica `no-content`;
- ambos retornaram ao mesmo `ActivityEntryPipeline`;
- bindings de input/movement/camera e object reset foram reexecutados conforme aplicável.

---

## 14. RouteExitBackToMenu QA checkpoint — PASS

O retorno ao menu foi validado como lifecycle correto e observabilidade QA corrigida.

Critério de PASS:

```text
releaseCompleted=true
closedForRouteExit=true
routeExitTeardownCompleted=true
menuRouteApplied=true
checkpointStatus=Passed
```

Isto remove da lista ativa a pendência de `RouteExitBackToMenu checkpoint Passed explícito`.

---

## 15. Unsupported v0 / Scope guard

v0 suporta:

```text
ActivityContentProfile
Load/PrepareActivityContent obrigatório
ActivityEntryPipeline único
ActivitySetupInventory
ActivationWindow/DeactivationWindow em AdditiveScene simples
CompleteActivationWindow / CompleteDeactivationWindow explícitos
PlayerActor readiness
PlayerInputBindingStage
MovementBindingStage + MovementControl lifecycle transitório; direção futura via ActivityCapabilityPermission
Placement v0 implícito por PlayerActorSetup + Reset(Placement)
CameraBindingSetupStage mínimo
StateResetRequirements
ResetGroups v0
ReleasePreviousActivityContent
ReloadContentOnRestart
Pooling canônico existente como capacidade transversal
RouteActivitySave + ActivityObjectSnapshotRestore MVP para test_object_01
```

Futuro explícito / não pendência ativa agora:

```text
WindowTemplateLibrary route-scoped completa em runtime final
standby de templates
payload bind/unbind de window
release da WindowTemplateLibrary somente no route-exit
```

v0 não suporta ainda:

```text
KeepRecentActivityContent real
RetainUntilRouteExit real
ResetContentOnRestart real
ref-count de content scenes compartilhadas
seamless real
restore genérico de objetos além do MVP test_object_01
runtime spawn completo
object release completo com todos os tipos
pool-backed Activity/ObjectRelease sem objeto concreto/policy explícita
budgeted retention/preload
manual release por gameplay
Progression real completa
HUD binding real
NPC/materialization real
InteractionBinding real
split-screen
camera priority/cutscene policy completa
```

Unsupported deve ser explícito. Não criar fallback silencioso.

---

## 16. Invariantes obrigatórios

- `SessionActivityPipeline` decide lifecycle local.
- `SessionOperationalPipeline` decide ordem de rota/unload/handoff operacional.
- `SceneComposition` executa scene changes, não lifecycle.
- `ActivityAsset`/`ActivityCatalogAsset` definem dados autorais, não lifecycle.
- Objetos/domínios contribuem requirements, não avançam pipeline.
- Policies decidem estratégia/bloqueio, não conteúdo concreto de gameplay.
- Adapters executam side-effects comandados.
- Facts confirmam readiness/completion/failure.
- Toda etapa relevante carrega `Pipeline Identity`.
- `foreign/stale events` não podem alterar Activity ativa.
- Nenhuma ausência obrigatória vira fallback silencioso.
- Deactivation não implica Release.
- Conteúdo declarado e conteúdo descoberto convergem para o mesmo `ActivitySetup/ObjectEntry` pipeline.

---

## 17. Relação com ADRs existentes

- **ADR-0001**: preserva Base 1.1 como Pipeline Convergence com identidade explícita e isolamento contra `foreign/stale events`.
- **ADR-0003**: `SessionOperationalPipeline` permanece owner da rota, transição operacional e handoff para `SessionActivityPipeline`.
- **ADR-0004**: `SessionActivityPipeline` permanece owner do lifecycle local da Activity.
- **ADR-0005**: módulos produzem facts/commands; adapters executam side-effects.
- **ADR-0007**: gates/input modes executam estado/efeito, não lifecycle.
- **ADR-0008/ADR-0011**: save/runtime config executam persistência/config validada, não lifecycle de Activity.
- **ADR-0009/ADR-0010**: slots/player preparation operacional são intenção/handoff; `PlayerActor` jogável nasce no `SessionActivityPipeline/ActivitySetup`.
- **ADR-0012/ADR-0013**: `OperationalCameraRuntime` e `CameraPresentation` são base técnica; Activity camera binding mínimo é decidido pelo `SessionActivityPipeline`.

---

## 18. Critérios de aceite futuros

Implementações futuras devem provar por smoke/log:

- identity correta por `activityId + entrySequence`;
- skip explícito para subplano vazio/opcional;
- fail-fast para requisito obrigatório quebrado;
- ausência de fallback por tag/nome/singleton/`Camera.main`;
- command -> adapter -> fact/completion;
- route-exit sem side-effect operacional antes de teardown local;
- nenhum owner duplo ativo após migração de ownership.
