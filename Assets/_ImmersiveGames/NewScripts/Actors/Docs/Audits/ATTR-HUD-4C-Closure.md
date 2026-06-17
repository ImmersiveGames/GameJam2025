# ATTR-HUD-4C — Closure Docs + Smoke Criteria Update

## Status

```text
ATTR-HUD-4C — CLOSED / DOCUMENTAL
ATTR-HUD-4B — PASS funcional do provider scene-local + live HUD binding
```

## Caso validado

```text
PrimaryPlayer
-> actor.attribute.health
-> ActorAttributeImageFillSink
-> UnityEngine.UI.Image.fillAmount
```

## Resultado funcional

O HUD screen-space do player foi ligado por request scene-local, resolvido pelo `ActivityEntryPipeline`, assinado no stream tipado de atributos e limpo no route-exit/deactivation.

O smoke validou:

```text
SceneActorAttributeUiBindingRequestProvider
-> entry accepted
-> requestCount=1
-> PrimaryPlayer resolved
-> initial value applied
-> stream subscribed
-> ImageFill applied
-> QA Subtract updates fillAmount to 0.9
-> handle disposed on route exit/deactivation
-> UI binding released count = 1
```

## Status dos cortes

| Corte | Status | Resultado |
|---|---:|---|
| `ATTR-HUD-0` | CLOSED | Auditoria/documentação inicial. |
| `ATTR-HUD-1` | PASS | `ActorAttributeChangedEvent` + `IActorAttributeEventStream`. |
| `ATTR-HUD-1B` | PASS | Observabilidade de publish validada. |
| `ATTR-HUD-2` | PASS | `ActorAttributeImageFillSink` passivo. |
| `ATTR-HUD-3A` | PASS | Selectors/requests tipados. |
| `ATTR-HUD-3B` | PASS | Runtime de binding + handle descartável. |
| `ATTR-HUD-3C` | PASS | Resolver `PrimaryPlayer` -> `ActorInstanceRuntimeId + ActorAttributeId`. |
| `ATTR-HUD-4A` | PASS | Stage de binding no-op com provider vazio. |
| `ATTR-HUD-4B` | PASS | Provider scene-local + binding real do HUD. |
| `ATTR-HUD-4C` | CLOSED | Fechamento documental. |

## Shape final

```text
UIScene
└── HudBindingRoot
    └── SceneActorAttributeUiBindingRequestProvider
        └── Binding Requests[0]
            ├── Request Enabled = true
            ├── Selector Kind = PrimaryPlayer
            ├── Attribute Id = actor.attribute.health
            └── Image Fill Sink = ActorAttributeImageFillSink
```

```text
Canvas
└── PlayerHud
    └── HealthBar
        └── Fill
            ├── Image(type=Filled)
            └── ActorAttributeImageFillSink
```

## Fronteira arquitetural final

- `UIScene` declara intenção de binding.
- `SceneActorAttributeUiBindingRequestProvider` não resolve ator.
- `LoadedSceneActorAttributeUiBindingRequestProvider` coleta providers das cenas carregadas.
- `ActivityEntryActorAttributeUiBindingStage` é o owner de binding dentro da entrada.
- `ActivityEntryActorAttributeUiTargetResolver` transforma selector em target runtime.
- `ActorAttributeUiBindingRuntime` aplica estado inicial e assina stream.
- `ActorAttributeImageFillSink` aplica side-effect visual local.
- `ActivityEntryPipeline` libera handles no teardown/restart/route-exit.

## Decisões preservadas

- Binding final usa `ActorInstanceRuntimeId + ActorAttributeId`.
- `PrimaryPlayer` é selector autoral, não chave runtime final.
- HUD não consulta pipeline, registry ou service locator.
- UI sink não resolve ator.
- Provider não assina evento.
- EventBus/FilteredEventBus continuam encapsulados por `IActorAttributeEventStream`.
- Cleanup é explícito por handle/store.

## Não fazer

```text
HUD -> FindObjectOfType<PlayerActor>
HUD -> DependencyManager.Provider
HUD -> registry
HUD -> pipeline
HUD -> EventBus cru
HUD -> actorId string "player1"
PlayerSlotId como chave final de HUD
```

## Critério de aceite final

PASS exige:

```text
FATAL: 0
Exception: 0
error CS: 0
route_transition_failed: 0
checkpointStatus='Failed': 0

ActorAttributeUiSceneRequestProviderEntryAccepted >= 1
ActivityEntryActorAttributeUiBindingStarted requestCount='1'
ActorAttributeUiTargetResolved >= 1
ActorAttributeUiInitialValueApplied >= 1
ActorAttributeUiBindingSubscribed >= 1
ActivityEntryActorAttributeUiBindingApplied >= 1
ActorAttributeImageFillApplied >= 2
ActorAttributeUiBindingEventApplied >= 1
ActorAttributeUiBindingDisposed >= 1
ActivityEntryActorAttributeUiBindingsReleased releasedCount='1'
```

## Observação pendente não bloqueante

O comando QA de atributo ainda pode precisar de uma policy futura de aceitação por fase:

```text
ATTR-QA-GATE-1 — Attribute command QA gating
```

Esse débito não bloqueia HUD porque o HUD apenas observa estado já mutado.
