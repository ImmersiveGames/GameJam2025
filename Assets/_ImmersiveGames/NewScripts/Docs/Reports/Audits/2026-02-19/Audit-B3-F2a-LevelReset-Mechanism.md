# B3-F2a — Decisão de mecanismo para LevelReset (mais local)

## Decisão
Para `ResetLevelAsync`, foi escolhido **ContentSwap InPlace** reaplicando o `contentId` atual do level.

## Por que este mecanismo é o mais local hoje
- `InPlaceContentSwapService` já representa troca/reaplicação local no domínio de level/conteúdo (sem transição macro).
- O caminho já existe em produção, com gates e observabilidade próprios.
- Evita acoplar LevelReset ao pipeline macro de SceneFlow, preservando a separação MacroReset vs LevelReset.
- Permite evolução futura para combinar com RunRearm sem quebrar o contrato novo (`IWorldResetCommands`).

## Fontes de estado usadas
- `IRestartContextService` para snapshot atual (level/route/content).
- Fallback de `contentId` via `IContentSwapContextService.Current` quando necessário.

## Compatibilidade
- O evento legado `WorldLifecycleResetCompletedEvent` (usado pelo gate do SceneFlow) **não foi alterado**.
- Os eventos V2 são adicionais para observabilidade/contrato explícito.
