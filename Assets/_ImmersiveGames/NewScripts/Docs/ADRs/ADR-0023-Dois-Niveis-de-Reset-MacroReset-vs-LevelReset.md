# ADR-0023 - Dois niveis de reset: MacroReset vs LevelReset

## Status atual (2026-03-11)
- Status: **DONE**
- Implementado no codigo:
  - `MacroRestartCoordinator` e owner unico de `GameResetRequestedEvent` com coalescing/debounce.
  - `GameLoopCommandEventBridge` sem reset listener.
  - `RestartNavigationBridge` nao participa do runtime canonico atual.
  - `ResetMacroAsync(...)` permanece no dominio macro.
  - `ResetLevelAsync(...)` canonico recebe `LevelDefinitionAsset levelRef`.
  - `WorldLifecycle V2` ja nao promove `levelId/contentId` como shape principal de telemetria.
- Evidencia:
  - `Docs/Reports/Baseline/2026-03-06/Baseline-3.1-Freeze.md`
  - `Docs/Reports/Baseline/2026-03-06/lastlog.log`

## Status

- Estado: **Aceito (Implementado)**
- Data (decisao): 2026-02-19
- Ultima atualizacao: 2026-03-11

## Decisao canonica atual

- `ResetMacroAsync(...)` permanece no dominio macro.
- `ResetLevelAsync(...)` canonico recebe `LevelDefinitionAsset levelRef`.
- `contentId='level-ref:...'` no reset e token de operacao, nao identidade de negocio.
- `WorldLifecycle V1` continua sendo gate/correlacao do SceneFlow.
- `WorldLifecycle V2` continua sendo telemetria/observabilidade.

## Evidencia (log)

- `lastlog:737` `StartGameplayRouteAsync without level selection; default will be selected in LevelPrepare.`
- `lastlog:1145` `LevelDefaultSelected ... levelRef='Level1'`
- `lastlog:1155` `ResetRequested kind='Level' ... contentId='level-ref:Level1'`
- `lastlog:1685` `[OBS][Navigation] MacroRestartStart runId='1' effectiveReason='PostGame/Restart#r1'`
- `lastlog:2033` `[OBS][LevelFlow] LevelDefaultSelected ... levelRef='Level1' reason='PostGame/Restart#r1'`

## Escopo e excecoes remanescentes

- O fechamento deste ADR vale para a separacao canonica entre MacroReset e LevelReset no eixo principal.
- O que permanece fora deste fechamento e o bloco de `Gameplay RunRearm`, que ainda possui fallback legado de actor-kind/string e nao altera esta separacao arquitetural.
