# ADR-0022 — Assinaturas e Dedupe por Domínio (MacroRoute vs Level)

## Status

- Estado: **Aceito (Implementado)**
- Data (decisão): 2026-02-19
- Última atualização: 2026-03-05
- Tipo: Implementação
- Escopo: NewScripts/Modules (SceneFlow, LevelFlow, WorldLifecycle)

## Resumo

Separação canônica entre:

- **MacroSignature (SceneFlow)** para transições macro de cena.
- **LevelSignature (LevelFlow)** para seleção/aplicação de level/conteúdo dentro do macro.

Também há dedupe por domínio para evitar reentrância duplicada sem bloquear fluxos locais de level.

## Decisão

1) **Assinatura macro**
- Fonte canônica: `SceneTransitionContext.ContextSignature`.
- Construção padrão: `SceneTransitionContext.ComputeSignature(...)` com `route/style/profile/profileAsset/active/fade/load/unload`.
- Consumo canônico: `SceneTransitionSignature.Compute(context)`.

2) **Assinatura level**
- Tipo canônico: `LevelContextSignature`.
- Construção canônica: `LevelContextSignature.Create(levelId, routeId, reason, contentId)`.

3) **Dedupe macro**
- `SceneTransitionService.ShouldDedupe(signature)` dedupa start/start e completed/start em janela curta.
- `WorldLifecycleSceneFlowResetDriver.ShouldSkipDuplicate(signature)` dedupa reset por assinatura em _in-flight_ + janela recente.

4) **Dedupe level**
- Fluxos de seleção/troca usam `SelectionVersion` monotônico.
- `LevelStageOrchestrator` ignora eventos já processados por `_lastProcessedSelectionVersion`.

## Implementação atual (fonte de verdade = código)

- MacroSignature em `SceneTransitionContext.ContextSignature` e `ComputeSignature(...)`.
- SceneFlow consome a assinatura via `SceneTransitionSignature.Compute(...)`.
- Dedupe macro no `SceneTransitionService.ShouldDedupe(...)`.
- Dedupe de reset macro no `WorldLifecycleSceneFlowResetDriver.ShouldSkipDuplicate(...)`.
- LevelSignature em `LevelContextSignature` + `Create(...)`.
- Versionamento de seleção em `LevelSelectedEvent.SelectionVersion`.
- Orquestração IntroStage com dedupe por versão em `LevelStageOrchestrator`.

## Critérios de aceite (DoD)

- [x] MacroSignature e LevelSignature são separadas por contrato e implementação.
- [x] Dedupe macro implementado em SceneFlow e no driver de reset do WorldLifecycle.
- [x] Dedupe level por `SelectionVersion` implementado no orquestrador de stages.
- [ ] Hardening: testes automatizados cobrindo colisão (mesma macroSignature com levelSignature diferente).

## Changelog

- 2026-03-05: ADR reescrito para refletir exclusivamente a implementação atual em código (sem depender de logs).
