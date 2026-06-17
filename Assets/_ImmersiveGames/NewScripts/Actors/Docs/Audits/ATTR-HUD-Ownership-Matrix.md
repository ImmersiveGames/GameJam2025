# ATTR-HUD — Ownership Matrix

| Arquivo / Classe | Responsabilidade atual | Owner correto | Tipo | Status |
|---|---|---|---|---|
| `UIScene` | Hospeda Canvas, sink visual e provider scene-local. | `SessionOperationalPipeline` carrega a cena; cena só hospeda objetos. | Scene/UI host | Final |
| `SceneActorAttributeUiBindingRequestProvider` | Declara requests de binding no Inspector. | Scene-local provider. | Provider / authoring scene-local | Final |
| `LoadedSceneActorAttributeUiBindingRequestProvider` | Coleta providers em cenas carregadas. | Composition/entry dependency. | Collector técnico bounded | Final |
| `ActivityEntryActorAttributeUiBindingStage` | Orquestra binding no entry após `ActorAttributeSetupCompleted`. | `ActivityEntryPipeline` | Stage | Final |
| `ActivityEntryActorAttributeUiTargetResolver` | Resolve selector autoral para target runtime. | `ActivityEntryPipeline` / resolver injetado | Resolver | Final |
| `ActorAttributeUiBindingRuntime` | Aplica valor inicial, assina stream, cria handle. | Binding runtime | Runtime helper | Final |
| `ActorAttributeUiBindingHandle` | Mantém subscription e faz dispose/clear idempotente. | Store/handle owner | Runtime handle | Final |
| `ActivityActorAttributeUiBindingRuntimeState` | Guarda handles ativos do entry. | `ActivityEntryPipeline` | Runtime state/store | Final |
| `ActorAttributeEventStream` | Publica/assina eventos tipados de atributo. | Actor attribute event stream | Event stream adapter | Final |
| `ActorAttributeEndpoint` | Muta/commita atributo. | Actor local endpoint | Endpoint | Final |
| `ActorAttributeImageFillSink` | Aplica `Image.fillAmount`. | UI sink local | Sink/adapter visual | Final |
| `SessionActivityPipeline.TryApplyActorAttributeCommand` | Command surface atual de QA/host. | Macro pipeline/QA bridge transitório | Command surface | Transitório aceitável |

## Perguntas obrigatórias respondidas

### Qual pipeline é dono desta decisão?

`ActivityEntryPipeline` é dono do binding de UI de atributo na entrada da atividade. `SessionActivityPipeline` só mantém macro lifecycle e chama cleanup no teardown/route-exit/restart.

### Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data?

- `SceneActorAttributeUiBindingRequestProvider`: provider/authoring scene-local.
- `ActivityEntryActorAttributeUiBindingStage`: stage.
- `ActivityEntryActorAttributeUiTargetResolver`: resolver.
- `ActorAttributeUiBindingRuntime`: runtime helper.
- `ActorAttributeUiBindingHandle`: runtime handle.
- `ActorAttributeImageFillSink`: UI sink / adapter visual.
- `ActorAttributeEndpoint`: endpoint.
- `ActorAttributeChangedEvent`: event/fact-like runtime notification.

### Isso é comportamento final ou bridge transitória?

O fluxo HUD é comportamento final para screen-space HUD simples.

A command surface QA de atributo ainda é transitória como ferramenta de teste e pode receber policy futura.

### Essa compatibilidade ainda é necessária?

Não há compat/alias novo. Provider vazio permanece como fallback explícito para ausência de scene-local providers, não como compat silenciosa.

### O erro estava no sintoma ou na fronteira arquitetural errada?

O erro inicial era fronteira: HUD não tinha provider scene-local real. Corrigido com request provider que declara intenção e deixa resolution para o stage.

### Existe owner duplicado para o mesmo lifecycle?

Não. Provider declara requests; stage cria binding; runtime/handle mantém subscription; pipeline limpa handles.

## Regras preservadas

```text
Pipeline decide lifecycle/order.
Stage executa binding determinístico.
Provider não resolve ator.
Sink não resolve ator.
Endpoint muta atributo.
Event stream encapsula EventBus.
Cleanup é explícito.
```

## Riscos fechados

| Risco | Status |
|---|---|
| HUD consultar pipeline/registry | Eliminado. |
| Binding por string `player1` | Eliminado. |
| Provider com `requestCount=0` permanente | Resolvido com provider scene-local. |
| Sink genérico sem tipo | Corrigido para `ActorAttributeImageFillSink`. |
| Entry genérico ilegível no Inspector | Corrigido com authoring entry + drawer. |
| Subscription vazando após route-exit | Smoke validou dispose/release. |

## Débitos remanescentes

| Débito | Bloqueia HUD? | Observação |
|---|---:|---|
| `ATTR-QA-GATE-1` | Não | Policy futura para comando QA de atributo por fase. |
| HUD world-space | Não | Reusar o mesmo sink/request, mas selector pode ser `Self` futuro. |
| Múltiplos players | Não para single-player atual | Necessário validar selectors/roles para multiplayer. |
