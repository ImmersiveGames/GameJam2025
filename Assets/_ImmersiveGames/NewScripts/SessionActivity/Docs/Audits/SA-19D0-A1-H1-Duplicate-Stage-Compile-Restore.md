# SA-19D0-A1-H1 — Duplicate Stage Compile Restore

Status: Applied / Pending compile

## Motivo

O corte `SA-19D0-A1` foi aplicado sobre uma base que já continha os stages extraídos de object setup. Isso reintroduziu definições antigas dentro de `ActivityEntryObjectSetupStages.cs` e duplicou tipos já existentes em arquivos canônicos separados.

Também reintroduziu `ActivityCapabilityInventoryBuildResult` dentro de `ActivityCapabilityInventoryCoordinator.cs`, enquanto o contrato canônico já existe em `ActivityCapabilityInventoryBuildResult.cs`.

## Correção

- `ActivityEntryObjectSetupStages.cs` volta a conter apenas `ActivityEntryObjectSetupStageUtility`.
- `ActivityEntryCapabilityInventoryPreviewStage.cs` recebe `ActivityEntryCapabilityInventoryBuildStage` como build boundary concreto.
- `ActivityEntryPipeline.cs` mantém a dependência em `ActivityEntryCapabilityInventoryBuildStage`.
- `ActivityCapabilityInventoryCoordinator.cs` deixa de declarar tipos runtime ativos.

## Ownership preservado

- `ActivityEntryPipeline` permanece owner de ordem.
- `ActivityEntryCapabilityInventoryBuildStage` é build boundary determinístico.
- `ActivityEntryCapabilityInventoryPreviewStage` continua owner do preview/fact/snapshot.
- Inventory permanece snapshot/index passivo.

## Fora do corte

- Não mexe em PendingOperationRunner.
- Não mexe em teardown.
- Não reabre B2/B3/C2.
- Não altera reset policy.
- Não altera Actor/Command/Projectile.
