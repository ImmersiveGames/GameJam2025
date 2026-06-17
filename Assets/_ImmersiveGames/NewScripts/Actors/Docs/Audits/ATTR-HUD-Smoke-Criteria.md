# ATTR-HUD — Smoke Criteria

## Smoke mínimo de compilação/runtime

```text
FATAL: 0
Exception: 0
error CS: 0
route_transition_failed: 0
checkpointStatus='Failed': 0
```

## Smoke de provider ausente

Quando não há provider na `UIScene`, o comportamento correto é no-op explícito:

```text
ActivityEntryActorAttributeUiBindingStarted requestCount='0'
ActivityEntryActorAttributeUiBindingSkipped reason='no_binding_requests'
```

Aceite:

```text
ActivityEntryActorAttributeUiBindingStarted >= 1
ActivityEntryActorAttributeUiBindingSkipped >= 1
ActivityEntryActorAttributeUiBindingRejected = 0
```

## Smoke de provider real

Com `SceneActorAttributeUiBindingRequestProvider` configurado:

```text
ActorAttributeUiSceneRequestProviderEntryAccepted >= 1
ActorAttributeUiSceneRequestProviderCollected acceptedCount='1' rejectedCount='0'
ActorAttributeUiSceneRequestProvidersCollected requestCount='1'
ActivityEntryActorAttributeUiBindingStarted requestCount='1'
```

## Smoke de target resolution

```text
ActorAttributeUiTargetResolveStarted selectorKind='PrimaryPlayer'
ActorAttributeUiTargetResolved selectorKind='PrimaryPlayer' actorId='actor.player.primary'
```

Não deve aparecer:

```text
ActorAttributeUiTargetResolveRejected
```

## Smoke de initial apply

```text
ActorAttributeUiBindingStarted
ActorAttributeImageFillApplied currentValue=100 fillAmount=1
ActorAttributeUiInitialValueApplied
ActorAttributeUiBindingSubscribed
ActivityEntryActorAttributeUiBindingApplied activeHandleCount='1'
```

## Smoke de update via QA

Executar QA de subtract no player:

```text
QaSubtractActorAttribute
actorId='actor.player.primary'
attributeId='actor.attribute.health'
amount='10'
```

Esperado:

```text
ActorAttributeCommandRequested
ActorAttributeImageFillApplied currentValue=90 fillAmount=0.9
ActorAttributeUiBindingEventApplied
ActorAttributeChangedEventPublished currentValue=90 normalizedValue=0.9
ActorAttributeChanged previousValue='100' newValue='90'
```

Não deve aparecer:

```text
ActorAttributeCommandRejected
ActorAttributeImageFillApplyRejected
ActorAttributeUiBindingRejected
```

## Smoke de route exit / cleanup

Executar `BackToMenu` / route-exit.

Esperado:

```text
ActorAttributeImageFillCleared
ActorAttributeUiBindingDisposed
ActivityEntryActorAttributeUiBindingsReleased releasedCount='1'
```

## PASS final para `ATTR-HUD-4B`

```text
FATAL: 0
Exception: 0
error CS: 0
route_transition_failed: 0
checkpointStatus='Failed': 0

ActorAttributeUiSceneRequestProviderEntryAccepted >= 1
ActorAttributeUiSceneRequestProviderEntryRejected = 0
ActorAttributeUiSceneRequestProvidersCollected requestCount='1'
ActivityEntryActorAttributeUiBindingStarted requestCount='1'
ActorAttributeUiTargetResolved >= 1
ActorAttributeUiBindingStarted >= 1
ActorAttributeUiInitialValueApplied >= 1
ActorAttributeUiBindingSubscribed >= 1
ActivityEntryActorAttributeUiBindingApplied >= 1
ActorAttributeImageFillApplied >= 2
ActorAttributeUiBindingEventApplied >= 1
ActorAttributeUiBindingDisposed >= 1
ActivityEntryActorAttributeUiBindingsReleased releasedCount='1'
```

## Filtros úteis no Console

```text
ActorAttributeUi
ActivityEntryActorAttributeUiBinding
ActorAttributeImageFill
ActorAttributeChanged
```
