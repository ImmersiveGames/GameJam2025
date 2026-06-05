# SA-13C-OBJ1-FIX — ActivityObject exit correlation mirror

Status: `CLOSED / PASS funcional + PASS arquitetural parcial`.

## Causa

`EXIT_CORRELATION_MISSING` entre `ActivityEntryPipeline` e os stages de exit de `ActivityObject`.

A entry descobria e validava `test_object_01`, mas o `ActivityObjectExitRuntimeState` não recebia o mirror técnico necessário antes de `ActivityObjectSnapshotCaptureStage`, `ActivityObjectReleaseStage` e `ActivityObjectContributorUnregisterStage`.

## Correção

Após `ExecuteSetupAndReadiness(...)` concluir com sucesso e antes de `EnterActivationFlow(...)`, a correlação de saída é congelada em `ActivityObjectExitRuntimeState`.

Caminhos alterados:

```text
EnterActivity(...)
ContinueAfterActivityContentLoadedSetReady(...)
```

States congelados:

```text
ActivityObjectContributorDiscoveryResult
ActivityCapabilityInventory preview
ActivityCapabilityInventoryValidationResult
```

Métodos usados:

```text
ClearAll(...)
StoreContributorDiscoveryResult(...)
StoreInventoryPreview(...)
```

## Owner

```text
ActivityEntryPipeline produz object setup/inventory.
ActivityObjectExitRuntimeState armazena correlation state técnico.
ActivityObjectSnapshotCaptureStage, ActivityObjectReleaseStage e ActivityObjectContributorUnregisterStage consomem o mirror.
SessionActivityPipeline preserva ordering/lifecycle do boundary macro.
```

## Evidência

```text
ActivityObjectExitCorrelationFrozen discoveryCount='1' inventoryCapabilityCount='11'
ActivityObjectSnapshotCapture capturedCount='1' targetIds='test_object_01'
ActivityObjectRelease commandCount='1' appliedCount='1' targetIds='test_object_01'
ActivityObjectContributorUnregister unregisteredCount='1' targetIds='test_object_01'
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
```

`activity_02` preservou o no-content explícito:

```text
ActivityObjectSnapshotCapture capturedCount='0' targetIds='<none>'
ActivityObjectRelease commandCount='0' targetIds='<none>'
ActivityObjectContributorUnregister unregisteredCount='0' skippedNoContributors='true'
```

## Débitos

- Localização final ideal: mover o freeze de correlation para `ActivityEntryPipeline`, mantendo `SessionActivityPipeline` só como macro boundary.
- `RouteActivitySave` precisa de smoke/auditoria própria para confirmar payload útil quando o exit ocorrer após activity com snapshot capturado.
