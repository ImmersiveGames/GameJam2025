# ADR-0026 — Troca de Level Intra-Macro via Swap Local (sem Transição Macro)

## Status

- Estado: **Aceito (Implementado)**
- Data (decisão): 2026-02-19
- Última atualização: 2026-03-04
- Tipo: Implementação
- Escopo: NewScripts/Modules (LevelFlow, WorldLifecycle, QA)

## Resumo

Trocar level dentro do mesmo macro sem chamar transição macro do SceneFlow.

## Decisão

1. API canônica em runtime:
   - `ILevelFlowRuntimeService.SwapLevelLocalAsync(LevelId levelId, string reason, CancellationToken ct)`
2. Execução de swap local em `LevelSwapLocalService.SwapLocalAsync(...)`:
   - resolve target level/macro;
   - valida snapshot e macro route atual;
   - incrementa `selectionVersion`;
   - publica `LevelSelectedEvent`;
   - executa `IWorldResetCommands.ResetLevelAsync(...)`;
   - publica `LevelSwapLocalAppliedEvent`.
3. QA proof sem transição macro via `LevelFlowDevContextMenu`.

## Implementação atual (fonte de verdade: código)

- `ILevelFlowRuntimeService` expõe `SwapLevelLocalAsync(...)`.
- `LevelFlowRuntimeService.SwapLevelLocalAsync(...)` delega para `ILevelSwapLocalService`.
- `LevelSwapLocalService` executa o fluxo end-to-end e não chama navegação macro.
- `LevelFlowDevContextMenu` possui comandos de QA:
  - NextInMacro
  - ToTargetLevelId
  - ProofNoMacroTransition (conta `SceneTransitionStartedEvent` e comprova `transitionStartedCount == 0`).

## Critérios de aceite (DoD)

- [x] API canônica de swap local existe no runtime.
- [x] Serviço dedicado de swap local existe e usa reset de level.
- [x] `selectionVersion` é incrementado no swap.
- [x] Existe prova de QA sem transição macro (`ProofNoMacroTransition`).
- [ ] Hardening: teste automatizado (não manual) para o cenário ProofNoMacroTransition.

## Changelog

- 2026-03-04: status atualizado para Implementado com base no código atual (`SwapLevelLocalAsync` + `LevelSwapLocalService` + QA proof).
