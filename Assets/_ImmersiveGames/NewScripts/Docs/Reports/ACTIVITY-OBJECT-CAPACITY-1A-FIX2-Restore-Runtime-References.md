# ACTIVITY-OBJECT-CAPACITY-1A-FIX2 — Restore Runtime References

## Status

`IMPLEMENTED / PENDING COMPILE + SMOKE`

## Correção

O corte `ACTIVITY-OBJECT-CAPACITY-1A` migrou o discovery de `ActivityObject` para `IActivityObjectLifecycleContributionProvider`, mas passou a derivar o `capabilityId` do scanner a partir de `ContributionId`.

O `ActivityCapabilityInventoryBuilder`, porém, rederiva o `capabilityId` canônico a partir de:

```text
inventoryId + ownerId + capabilityKind + moduleId + componentPath
```

Isso fazia os descriptors finais e as runtime references ficarem com chaves diferentes. O resultado no smoke foi:

```text
ActivityCapabilityInventoryValidationFailedPassive
issueCodes='runtime_reference_missing:4'
```

## Mudança aplicada

`ActivityObjectCapabilityScanner.TryAppendLifecycleContribution(...)` voltou a derivar `capabilityId` por `componentPath`, preservando a chave canônica esperada pelo builder.

Antes:

```text
capabilityId <- ContributionId quando presente
```

Depois:

```text
capabilityId <- componentPath
```

## Boundary arquitetural

A correção não retorna ao scan nominal direto de endpoints. O caminho permanece:

```text
ActivityObjectCapabilityScanner
-> IActivityObjectLifecycleContributionProvider
-> IActivityObjectLifecycleContribution
-> runtime reference concreta
```

Apenas a chave de correlação descriptor/runtime reference foi corrigida para o shape canônico do inventory.

## O que não foi mexido

```text
SaveRuntime
ActivityObject stages
SessionActivityPipeline macro lifecycle
ActivityObject endpoint contracts
Actor capability contracts
Pooling
```

## Validação esperada

```text
sem erro CS
ActivityCapabilityInventoryValidationPassed
sem runtime_reference_missing
ActivityObjectReset PassedApplied em activity_01
ActivityObjectSnapshotCapture capturedCount='1' em activity_01
ActivityObjectRelease appliedCount='1' em activity_01
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
```
