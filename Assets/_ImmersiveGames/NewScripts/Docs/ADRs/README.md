# ADRs — Índice

Este índice lista as decisões arquiteturais registradas como ADR no projeto. Use-o como ponto de entrada para auditoria e navegação.

## Convenções

- **ADRs descrevem decisões**, não implementação detalhada. Evidências e execução ficam em `../Reports/` (quando aplicável).
- O campo **Estado** indica o estágio do ADR (Proposto, Aceito, Implementado, Obsoleto, Substituído).

## Lista de ADRs

| ADR | Título | Estado | Escopo |
|---|---|---|---|
| [ADR-0009](./ADR-0009-FadeSceneFlow.md) | Fade + SceneFlow (NewScripts) | Implementado | SceneFlow + Fade + Loading HUD (NewScripts) |
| [ADR-0010](./ADR-0010-LoadingHud-SceneFlow.md) | Loading HUD + SceneFlow (NewScripts) | Implementado | SceneFlow + Loading HUD (NewScripts) |
| [ADR-0011](./ADR-0011-WorldDefinition-MultiActor-GameplayScene.md) | WorldDefinition multi-actor para GameplayScene (NewScripts) | Implementado | `GameplayScene`, `NewSceneBootstrapper`, spawn services (Player/Eater), WorldLifecycle |
| [ADR-0012](./ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md) | Fluxo pós-gameplay: GameOver, Vitória e Restart | Implementado | `GameLoop` (NewScripts), `WorldLifecycle`, SceneFlow, `UIGlobalScene` (overlays de UI) |
| [ADR-0013](./ADR-0013-Ciclo-de-Vida-Jogo.md) | Ciclo de vida do jogo (NewScripts) | Implementado | GameLoop + SceneFlow + WorldLifecycle (NewScripts) |
| [ADR-0014](./ADR-0014-GameplayReset-Targets-Grupos.md) | Gameplay Reset: Targets e Grupos | Implementado | `GameplayReset` (NewScripts), WorldLifecycle, spawn services (Player/Eater) |
| [ADR-0015](./ADR-0015-Baseline-2.0-Fechamento.md) | Baseline 2.0: Fechamento Operacional | Implementado | NewScripts / Baseline 2.0 |
| [ADR-0016](./ADR-0016-ContentSwap-WorldLifecycle.md) | ContentSwap + modos de avanço + IntroStage opcional (WorldLifecycle/SceneFlow) | Implementado | WorldLifecycle + SceneFlow + GameLoop (NewScripts) |
| [ADR-0017](./ADR-0017-Tipos-de-troca-conteudo.md) | Tipos de troca de conteúdo (ContentSwap: In-Place vs SceneTransition) | Implementado | ContentSwap + SceneFlow (NewScripts) |
| [ADR-0018](./ADR-0018-Gate-de-Promoção-Baseline2.2.md) | Mudança de semântica: ContentSwap + LevelManager | Aceito | ContentSwap + LevelManager |
| [ADR-0019](./ADR-0019-Promocao-Baseline2.2.md) | Promoção do Baseline 2.2 (ContentSwap + LevelManager + Config) | Proposto | Baseline 2.2 |


## Atalhos

- Contrato canônico: [Observability-Contract.md](../Reports/Observability-Contract.md)
- Evidência vigente (ponte): [LATEST](../Reports/Evidence/LATEST.md)
- Snapshot (2026-01-18): [Evidência consolidada](../Reports/Evidence/2026-01-18/Baseline-2.1-Evidence-2026-01-18.md)
