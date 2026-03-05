# ADR-0023 — Dois níveis de reset: MacroReset vs LevelReset

## Status

- Estado: **Aceito (Implementado)**
- Data (decisão): 2026-02-19
- Última atualização: 2026-03-05
- Tipo: Implementação
- Escopo: NewScripts/Modules (WorldLifecycle, LevelFlow)

## Resumo

Existem dois resets com responsabilidades distintas:

- **MacroReset**: reset de mundo no domínio macro (SceneFlow/WorldLifecycle).
- **LevelReset**: reset local de level/conteúdo sem exigir transição macro.

## Decisão

- O contrato público é `IWorldResetCommands` com:
  - `ResetMacroAsync(SceneRouteId macroRouteId, ...)`
  - `ResetLevelAsync(LevelId levelId, ...)`
- A implementação canônica é `WorldResetCommands`.
- Observabilidade canônica por eventos V2:
  - `WorldLifecycleResetRequestedV2Event`
  - `WorldLifecycleResetCompletedV2Event`

## Implementação atual (fonte de verdade = código)

- `WorldResetCommands.ResetMacroAsync(...)` chama `IWorldResetService/WorldResetService` e publica Requested/Completed V2 com `kind=Macro`.
- `WorldResetCommands.ResetLevelAsync(...)` resolve snapshot atual, reaplica conteúdo via `IContentSwapChangeService.RequestContentSwapInPlaceAsync(...)` e publica Requested/Completed V2 com `kind=Level`.
- Eventos V2 carregam os dois contextos:
  - macro: `MacroRouteId`, `MacroSignature`
  - level: `LevelId`, `ContentId`, `LevelSignature`

## Critérios de aceite (DoD)

- [x] Dois comandos explícitos separados (Macro e Level).
- [x] Implementação em `WorldResetCommands` com contratos claros.
- [x] Eventos V2 de requested/completed implementados e publicados.
- [x] LevelReset executa swap de conteúdo local sem depender de transição macro.
- [ ] Hardening: cobertura de testes para cenários de falha de DI/fail-fast.

## Changelog

- 2026-03-05: ADR alinhado com contratos e implementação atuais (`IWorldResetCommands`, `WorldResetCommands`, eventos V2).
