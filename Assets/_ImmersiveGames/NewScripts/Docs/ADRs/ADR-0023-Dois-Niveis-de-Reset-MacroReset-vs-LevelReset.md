# ADR-0023 - Dois níveis de reset: MacroReset vs LevelReset

## Status atual (2026-03-11)
- Status: **DONE**
- Implementado no código:
  - `MacroRestartCoordinator` é owner único de `GameResetRequestedEvent` com coalescing/debounce.
  - `GameLoopCommandEventBridge` sem reset listener.
  - `RestartNavigationBridge` não participa do runtime canônico atual.
  - `ResetMacroAsync(...)` permanece no domínio macro.
  - `ResetLevelAsync(...)` canônico recebe `LevelDefinitionAsset levelRef`.
  - `WorldReset` + `ResetInterop` já não promovem `levelId/contentId` como shape principal de telemetria.
  - `Gameplay ActorGroupRearm` foi consolidado como soft reset local canônico por grupo de atores.
- Evidência:
  - `Docs/Reports/Audits/LATEST.md`
  - `Docs/Reports/Evidence/LATEST.md`

## Status

- Estado: **Aceito (Implementado)**
- Data (decisão): 2026-02-19
- Última atualização: 2026-03-12

## Decisão canônica atual

- `ResetMacroAsync(...)` permanece no domínio macro.
- `ResetLevelAsync(...)` canônico recebe `LevelDefinitionAsset levelRef`.
- `ResetLevelAsync(...)` não depende mais de `contentId` operacional; a identidade local vive no snapshot semântico (`LocalContentId`).
- `ResetInterop` continua sendo gate/correlação do `SceneFlow`.
- `WorldReset` + `ResetInterop` continuam sendo a superfície canônica de telemetria/observabilidade do reset.
- O reset local/gameplay passa por `ActorGroupRearm` canônico por grupo de atores, centrado em `ByActorKind`.

## Evidência (log)

- `lastlog:737` `StartGameplayRouteAsync without level selection; default will be selected in LevelPrepare.`
- `lastlog:1145` `LevelDefaultSelected ... levelRef='Level1'`
- `lastlog:1685` `[OBS][Navigation] MacroRestartStart runId='1' effectiveReason='PostGame/Restart#r1'`
- `lastlog:2033` `[OBS][LevelFlow] LevelDefaultSelected ... levelRef='Level1' reason='PostGame/Restart#r1'`

## Escopo e exceções remanescentes

- O fechamento deste ADR vale para a separação canônica entre MacroReset e LevelReset no eixo principal.
- `Gameplay ActorGroupRearm` deixa de ser exceção arquitetural nessa borda: o subsistema agora usa contrato canônico por grupo (`ByActorKind`) e manteve `ActorIdSet` apenas como seleção técnica explícita.
- Não há mais resíduo estrutural de navigation fora do trilho canônico atual.
