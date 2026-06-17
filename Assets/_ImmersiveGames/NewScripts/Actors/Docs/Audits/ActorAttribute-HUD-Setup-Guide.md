# ActorAttribute HUD Setup Guide

Guia prático para configurar uma barra de HP screen-space usando `ActorAttributeImageFillSink`.

## Objetivo

Ligar o atributo `actor.attribute.health` do `PrimaryPlayer` a um `UnityEngine.UI.Image.fillAmount` dentro da `UIScene`.

## Pré-requisitos

O código deve conter:

```text
SceneActorAttributeUiBindingRequestProvider
LoadedSceneActorAttributeUiBindingRequestProvider
ActivityEntryActorAttributeUiBindingStage
ActivityEntryActorAttributeUiTargetResolver
ActorAttributeUiBindingRuntime
ActorAttributeImageFillSink
ActorAttributeEventStream
```

## Configuração na `UIScene`

### 1. Criar/usar a barra visual

Estrutura recomendada:

```text
Canvas
└── PlayerHud
    └── HealthBar
        ├── Background
        └── Fill
```

No objeto `Fill`, configure o componente `Image`:

```text
Image Type = Filled
Fill Method = Horizontal
Fill Origin = Left
Fill Amount = 1
```

### 2. Adicionar o sink

No mesmo objeto `Fill`, adicione:

```text
ActorAttributeImageFillSink
```

Configuração recomendada:

```text
Fill Image = Image do próprio objeto Fill
Clear Mode = SetZero ou KeepCurrent
```

Para primeiro teste, `KeepCurrent` reduz ambiguidade. Para route-exit visual explícito, `SetZero` é aceitável.

### 3. Criar o provider scene-local

Na `UIScene`, crie:

```text
HudBindingRoot
└── SceneActorAttributeUiBindingRequestProvider
```

No provider, configure:

```text
Binding Requests
  Size = 1

  Element 0
    Request Enabled = true
    Selector Kind = PrimaryPlayer
    Explicit Actor Id = vazio
    Explicit Actor Instance Runtime Id = vazio
    Attribute Id = actor.attribute.health
    Image Fill Sink = Fill/ActorAttributeImageFillSink
```

## Resultado esperado em runtime

Ao entrar na activity:

```text
ActorAttributeUiSceneRequestProviderEntryAccepted
ActorAttributeUiSceneRequestProviderCollected
ActorAttributeUiSceneRequestProvidersCollected requestCount='1'
ActivityEntryActorAttributeUiBindingStarted requestCount='1'
ActorAttributeUiTargetResolved selectorKind='PrimaryPlayer'
ActorAttributeUiInitialValueApplied
ActorAttributeImageFillApplied fillAmount=1
ActivityEntryActorAttributeUiBindingApplied activeHandleCount='1'
```

Ao reduzir HP pelo QA:

```text
ActorAttributeChangedEventPublished currentValue=90 normalizedValue=0.9
ActorAttributeUiBindingEventApplied
ActorAttributeImageFillApplied fillAmount=0.9
```

Ao sair da rota/activity:

```text
ActorAttributeImageFillCleared
ActorAttributeUiBindingDisposed
ActivityEntryActorAttributeUiBindingsReleased releasedCount='1'
```

## Se `requestCount=0`

Verifique:

```text
UIScene está carregada
HudBindingRoot está ativo
SceneActorAttributeUiBindingRequestProvider está ativo
Binding Requests Size = 1
Request Enabled = true
Image Fill Sink preenchido
ActorAttributeImageFillSink.IsReady = true
```

Filtrar Console:

```text
ActorAttributeUiSceneRequestProvider
```

## Se o provider rejeitar entry

Procure:

```text
ActorAttributeUiSceneRequestProviderEntryRejected
```

Reasons esperados:

| Reason | Causa provável |
|---|---|
| `attribute_id_missing` | `Attribute Id` não preenchido ou inválido. |
| `sink_reference_missing` | `Image Fill Sink` vazio. |
| `sink_not_ready` | Sink sem `Image` pronto. |
| `explicit_actor_id_missing` | Selector `ExplicitActorId` sem id. |
| `explicit_actor_instance_runtime_id_missing` | Selector `ExplicitActorInstanceRuntimeId` sem runtime id. |
| `unsupported_selector_kind` | Selector kind não suportado. |

## Se binding resolve, mas barra não muda

Verifique:

```text
ActorAttributeUiBindingEventApplied
ActorAttributeImageFillApplied
```

Se `ActorAttributeUiBindingEventApplied` aparece, mas `ActorAttributeImageFillApplied` não aparece, o problema está no sink.

Se `ActorAttributeImageFillApplied` aparece com `fillAmount=0.9`, mas a UI visual não muda, o problema está no setup visual do `Image` ou na referência para o `Image` errado.

## Fronteiras proibidas

Não usar:

```text
FindObjectOfType
DependencyManager.Provider
UnityEvent
EventBus cru no HUD
registry direto no HUD
PlayerSlotId como chave final
string "player1"
```
