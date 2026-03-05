# ADR-0026 - Troca de Level Intra-Macro via Swap Local (sem Transicao Macro)

## Status

- Estado: **Aceito (Implementado)**
- Data (decisao): 2026-02-19
- Ultima atualizacao: 2026-03-05
- Tipo: Implementacao
- Escopo: NewScripts/Modules (LevelFlow, WorldLifecycle, QA)

## Resumo

Trocar level dentro do mesmo macro sem chamar transicao macro do SceneFlow.

## Decisao

1. API canonica em runtime:
   - `ILevelFlowRuntimeService.SwapLevelLocalAsync(LevelId levelId, string reason, CancellationToken ct)`
2. Execucao de swap local em `LevelSwapLocalService.SwapLocalAsync(...)`:
   - resolve target level/macro;
   - valida snapshot e macro route atual;
   - incrementa `selectionVersion`;
   - publica `LevelSelectedEvent`;
   - executa `IWorldResetCommands.ResetLevelAsync(...)`;
   - publica `LevelSwapLocalAppliedEvent`.
3. QA proof sem transicao macro via `LevelFlowDevContextMenu`.

## Implementacao atual (fonte de verdade: codigo)

- `ILevelFlowRuntimeService` expoe `SwapLevelLocalAsync(...)`.
- `LevelFlowRuntimeService.SwapLevelLocalAsync(...)` delega para `ILevelSwapLocalService`.
- `LevelSwapLocalService` executa o fluxo end-to-end e nao chama navegacao macro.
- `LevelFlowDevContextMenu` possui comandos de QA:
  - NextInMacro
  - ToTargetLevelId
  - ProofNoMacroTransition (`SceneTransitionStartedEvent` com `transitionStartedCount == 0`).

## Criterios de aceite (DoD)

- [x] API canonica de swap local existe no runtime.
- [x] Servico dedicado de swap local existe e usa reset de level.
- [x] `selectionVersion` e incrementado no swap.
- [x] Existe prova de QA sem transicao macro (`ProofNoMacroTransition`).
- [ ] Hardening: teste automatizado para o cenario ProofNoMacroTransition.

## Changelog

- 2026-03-05: revisado com base nas auditorias de 2026-03-04/2026-03-05 e no codigo atual.
- 2026-03-04: status atualizado para Implementado com base no codigo atual (`SwapLevelLocalAsync` + `LevelSwapLocalService` + QA proof).
