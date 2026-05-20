# Mini ADR — ActivityContent, WindowTemplateLibrary e ActivityEntryPipeline

**Base 1.1 — Pipeline Convergence / Convergência para Pipelines Determinísticos**  
**Status:** Documento de intenção / pré-ADR  
**Versão:** v0.2  
**Escopo:** `SessionActivityPipeline`, `ActivityContent`, windows autorais, setup de Activity e inventário de requirements.

---

## 1. Separação conceitual

Na Base 1.1, `Activity` não deve ser confundida com o conteúdo que ela usa.

```text
Activity = lifecycle.
ActivityContent = conteúdo jogável/material usado pela Activity.
ActivationWindow = janela autoral de entrada.
DeactivationWindow = janela autoral de saída.
Route Scene = superfície/base da rota.
```

A `Activity` representa o ciclo determinístico:

```text
setup -> activation -> running -> deactivation -> transition/release
```

O `ActivityContent` representa o material jogável da Activity:

```text
cenas additive de conteúdo
markers
props
objetos interativos
actors não-player
object entries
contributors
pontos de placement
rig/camera profile da Activity
```

---

## 2. ActivityContent pode ser grupo de cenas

`ActivityContent` não precisa ser uma cena única.

Ele deve ser modelado como um perfil composto:

```text
ActivityContentProfile
  -> contentIdentity
  -> contentSceneKeys[]
  -> activityCameraProfile / camera requirement
  -> scene contract metadata
  -> contributor discovery policy
  -> loading/preparation metadata
```

Exemplo:

```text
Activity 1-1
  contentScenes = [World1_1_Ground, World1_1_Props, World1_1_Markers]

Activity 1-2
  contentScenes = [World1_2_Caves, World1_2_Enemies, World1_2_Markers]
```

Essas cenas são específicas da Activity. Elas não devem ser presumidas como templates comuns da rota.

---

## 3. Diferença entre Route Scene e ActivityContent

A `Route Scene` é carregada pelo `SessionOperationalPipeline`.

Ela é a superfície/base da rota:

```text
Route Scene = base da rota / mundo / superfície operacional
```

O `ActivityContent` é carregado/preparado pelo `SessionActivityPipeline`.

Ele é o conteúdo jogável/material da Activity atual:

```text
ActivityContent = conteúdo específico da Activity
```

Regras:

```text
ActivityContentScenes são additive sobre a Route Scene.
ActivityContentScenes não substituem a Route Scene.
ActivityContentScenes não chamam SetActiveScene.
ActivityContentScenes não usam WindowSceneAdapter semanticamente.
```

---

## 4. ActivationWindow e DeactivationWindow

`ActivationWindow` e `DeactivationWindow` são janelas autorais.

Elas não são `ActivityContent`.

```text
ActivationWindow = entrada/apresentação/tutorial/confirmação.
DeactivationWindow = saída/resultado/recompensa/encerramento.
```

Ambas:

```text
não auto-completam
aguardam comando explícito
podem consumir dados da Route Scene e do ActivityContent
podem usar camera rig/profile próprio
podem usar templates fornecidos pela rota
```

No sandbox, o QA simula o futuro botão real:

```text
CompleteActivationWindow
CompleteDeactivationWindow
```

---

## 5. WindowTemplateLibrary route-scoped

A rota pode fornecer uma biblioteca de templates de janela.

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

A Activity não precisa declarar uma cena exclusiva de janela. Ela pode declarar intenção e payload:

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

Encerrar uma `ActivationWindow` ou `DeactivationWindow` não deve descarregar a template scene da rota.

Fechar uma window significa:

```text
unbind payload
parar animações locais
resetar presenter
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

---

## 7. Prioridade de câmera

A ordem de suplantação de câmera, da menor para a maior prioridade:

```text
Camera default
-> Camera da rota
-> Camera da Activity
-> Camera da ActivationWindow
-> Camera da DeactivationWindow
```

Interpretação:

```text
Route camera
  é a câmera base da rota.

Activity camera
  suplanta a Route camera durante a Activity.

ActivationWindow camera
  suplanta a Activity camera durante a janela de ativação.

DeactivationWindow camera
  suplanta a Activity camera durante a janela de desativação.
```

---

## 8. Load/preparation vs apresentação

Foi separado:

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

O load técnico Unity pode ser async, mas deve estar encapsulado como:

```text
Pipeline Command
-> PendingOperation
-> Completion validada
-> Pipeline Fact
-> próximo stage
```

A apresentação da window não deve depender de load async tardio.

---

## 9. ActivityCatalog e carregamento

Uma rota pode apontar para um `ActivityCatalog`.

Isso não significa carregar todas as Activities do catálogo.

```text
ActivityCatalog é índice autoral de Activities.
Não é pacote de cenas a carregar integralmente.
```

A rota carrega a `Route Scene` e fornece handoff para `SessionActivity`.

O `SessionActivityPipeline` resolve a Activity atual e carrega/prepara apenas o necessário para a entry atual.

Exemplo:

```text
Route World_1_X
  ActivityCatalog:
    1-1
    1-2
    1-3
```

No início:

```text
carrega/prepara Activity 1-1
não carrega Activity 1-2
não carrega Activity 1-3
```

Templates de janela da rota podem estar pré-carregados porque são compartilhados e contidos no escopo da rota.

`ActivityContent` continua específico por Activity.

---

## 10. ActivityEntryPipeline único

Toda Activity deve passar pelo mesmo `ActivityEntryPipeline`.

A primeira Activity e as seguintes não devem ter pipelines diferentes.

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

Não devem existir branches como:

```text
Activity com PlayerActor
Activity sem PlayerActor
Activity com NPC
Activity sem NPC
```

O pipeline deve emitir skip explícito para subplanos vazios.

---

## 11. Player não é propriedade semântica da Activity

`Player` não deve ser tratado como atributo estrutural da Activity.

```text
Player = participante/capacidade da rota/sessão.
Activity entry = pode exigir, posicionar, bindar ou resetar esse participante.
```

A Activity não deveria depender de um booleano semântico como:

```text
requiresPlayer = true
```

O shape final deve ser inventário:

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

Ele pode conter subplanos como:

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
8. ObjectEntrySetupStage
9. PlacementSetupStage
10. CameraBindingSetupStage
11. InteractionBindingSetupStage
12. HudBindingSetupStage
13. WarmupSetupStage
14. ActivitySetupReadinessValidation
15. ActivitySetupCompleted
16. ActivationWindowPresentation ou ActivationWindowSkippedNoContent
17. ActivityRunning
```

---

## 14. Load/PrepareActivityContent

`Load/PrepareActivityContent` é stage obrigatório do `ActivityEntryPipeline`.

Toda Activity passa por ele.

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

Regras:

```text
LoadSceneMode.Additive
não SetActiveScene
não trocar Route Scene
não fallback silencioso para Route Scene
completion stale/foreign é rejeitada
```

Identity obrigatória:

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

`ActivityContent` precisa estar pronto antes de:

```text
Contributor discovery
ActivitySetupInventory
Placement
CameraBinding
ActivationWindow
```

---

## 15. Relação com ActivitySceneContract

`ActivitySceneContract` deve observar o escopo da entry atual:

```text
Route Scene
+ ActivityContentScenes carregadas da entry atual
```

Mas com prioridade semântica:

```text
Activity-specific markers/contributors vêm das ActivityContentScenes.
Route Scene fornece base/superfície/infra da rota.
```

Se a Activity declarou conteúdo obrigatório, não pode haver fallback silencioso para Route Scene.

---

## 16. Ordem conceitual de entrada

```text
ResolveActivityEntry
-> ResolveActivityContentProfile
-> Load/PrepareActivityContent
-> DiscoverActivityContributors
-> BuildActivitySetupInventory
-> ValidateActivitySetupInventory
-> Execute setup stages
-> ActivitySetupCompleted
-> ActivationWindowPresented/Ready ou ActivationWindowSkippedNoContent
-> CompleteActivationWindow, se houver window
-> ActivityRunning
```

A primeira Activity usa a cortina/loading da rota como contexto visual.

Activities seguintes usam a cortina/transição local da Activity como contexto visual.

O pipeline interno é o mesmo.

---

## 17. Decisões congeladas até aqui

```text
ActivityContent é conteúdo jogável/material específico da Activity.

ActivityContent é declarado por Activity via ActivityContentProfile, não apenas SceneKeyAsset[].

ActivityContent pode ser composto por múltiplas cenas additive e possuir rig/camera profile próprio.

ActivationWindow e DeactivationWindow são janelas autorais separadas do ActivityContent.

As cenas/templates das janelas podem ser fornecidas pela rota em uma WindowTemplateLibrary route-scoped e pré-carregável.

Encerrar uma window não descarrega nem duplica a template scene; apenas limpa/unbind/reseta e devolve para Standby.

As Activities escolhem templates/variantes e injetam payload/dados.

As Activities continuam declarando seu próprio ActivityContent, porque conteúdo jogável/material dificilmente será padronizado por rota.

Toda Activity passa pelo mesmo ActivityEntryPipeline.

O que varia entre Activities é o ActivitySetupInventory resolvido, não o lifecycle.

Player não é propriedade semântica da Activity; é participante/capacidade da rota/sessão.

O SessionActivityPipeline continua dono do lifecycle da Activity, das janelas e do conteúdo da Activity.

O SessionOperationalPipeline continua dono da rota, do fade/loading de rota e da SceneComposition da rota.
```

---

## 18. Pendências para os próximos tópicos

```text
1. ActivityContent Retention / Release.
2. Restart de Activity e recarga/reuso de conteúdo.
3. Route-exit e release de ActivityContent antes de ClosedForRouteExit.
4. Quando preparar WindowTemplateLibrary na rota.
5. Shape v0 de implementation sem tentar Base 2.0.
6. Auditoria dos contratos atuais antes da implementação.
```
