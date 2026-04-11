# Checklist Canonico: Phase Content Unload em Gameplay -> Menu

## Status

Frozen.

## Objetivo

Validar o rail canonicamente correto de unload de `Phase Content` no `Gameplay -> Menu`, sem reabrir a arquitetura.

## Checklist

- [ ] `PhaseContentSceneTransitionUnloadSupplementProvider` registrado no boot.
- [ ] `PhaseContentSceneTransitionCompletionCleaner` registrado no boot.
- [ ] `RunDecision/ExitToMenu` entra como rota macro `to-menu`.
- [ ] O rail consulta e contribui unload suplementar de `Phase Content`.
- [ ] O unload real remove a cena local da phase.
- [ ] O cleaner limpa o read model apos `SceneTransitionCompleted`.
- [ ] O log final mostra `PhaseContentClearedOnSceneTransitionCompleted`.
- [ ] O caso nao depende de hardcode de `SceneTest2`.
- [ ] A base nao conhece `PhaseContentSceneRuntimeApplier` diretamente.
- [ ] A phase nao executa unload macro por conta propria.

## Criterio de interpretacao

- Se os itens acima passam, o boundary Base/Phase esta correto e o rail canonicamente observavel esta funcionando.
- Se algum item falha, a regressao deve ser investigada no boundary, nao por workaround local.

## Evidencia canonica resumida

- Provider registra contribuicao suplementar quando ha `Phase Content` ativo.
- `SceneTransitionService` inclui a lista suplementar no `RouteExecutionPlan`.
- `SceneCompositionExecutor` remove a cena local da phase junto do unload macro.
- `PhaseContentSceneTransitionCompletionCleaner` limpa o read model apos `SceneTransitionCompleted`.
