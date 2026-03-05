# ADR-0026 — Troca de Level Intra-Macro via Swap Local (sem Transição Macro)

## Status

- Estado: **Aceito (Implementado)**
- Data (decisão): 2026-02-19
- Última atualização: 2026-03-05
- Tipo: Implementação
- Escopo: NewScripts/Modules (LevelFlow, WorldLifecycle, QA)

## Resumo

A troca intra-macro está implementada via swap local sem acionar transição macro do SceneFlow.

## Decisão

- API canônica de runtime: `ILevelFlowRuntimeService.SwapLevelLocalAsync(LevelId, reason, ct)`.
- Execução local: `LevelSwapLocalService.SwapLocalAsync(...)`.
- Fluxo local publica seleção de level, executa `ResetLevelAsync` e publica `LevelSwapLocalAppliedEvent`.
- O fluxo não chama `NavigateAsync`/`SceneTransitionService` para realizar a troca.

## Implementação atual (fonte de verdade = código)

- `ILevelFlowRuntimeService` expõe `SwapLevelLocalAsync(...)`.
- `LevelFlowRuntimeService.SwapLevelLocalAsync(...)` delega para `ILevelSwapLocalService`.
- `LevelSwapLocalService.SwapLocalAsync(...)`:
  - valida macro atual pelo snapshot;
  - incrementa `SelectionVersion`;
  - publica `LevelSelectedEvent`;
  - executa `IWorldResetCommands.ResetLevelAsync(...)`;
  - publica `LevelSwapLocalAppliedEvent`.
- `LevelFlowDevContextMenu` possui provas QA:
  - `QA/LevelFlow/SwapLocal/ProofNoMacroTransition->level.2`
  - `QA/LevelFlow/NextLevel` com contador de `SceneTransitionStartedEvent` para confirmar ausência de transição macro.

## Critérios de aceite (DoD)

- [x] Existe API runtime canônica para swap local.
- [x] Implementação local dedicada (`LevelSwapLocalService`).
- [x] Fluxo usa level signature + selection version.
- [x] Há instrumentos QA explícitos para prova sem transição macro.
- [ ] Hardening: adicionar testes automatizados de regressão N→1 no CI.

## Changelog

- 2026-03-05: status alterado para **Implementado** e decisão ajustada ao shape real em código.
