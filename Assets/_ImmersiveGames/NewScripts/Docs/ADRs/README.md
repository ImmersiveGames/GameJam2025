# ADRs (Architecture Decision Records)

Este diretório contém decisões arquiteturais do **NewScripts**.

## Convenções de status

Cada ADR possui 3 eixos:

- **Decisão:** Proposta | Aceita | Rejeitada | Substituída | Obsoleta
- **Implementação:** Não iniciada | Em andamento | Parcial | Implementada | Obsoleta
- **Manutenção:** Ativa | Fechada | Obsoleta

> Regra prática: uma decisão pode estar **Aceita** mesmo com implementação **Parcial**; isso evita confundir "aprovação do design" com "trabalho concluído".

## Índice

| ADR | Título | Decisão | Implementação | Manutenção | Última atualização |
|---|---|---:|---:|---:|---:|
| [`ADR-0009-FadeSceneFlow.md`](ADR-0009-FadeSceneFlow.md) | ADR-0009 - Fade + SceneFlow (NewScripts) | Aceita | Implementada | Ativa | 2026-02-04 |
| [`ADR-0010-LoadingHud-SceneFlow.md`](ADR-0010-LoadingHud-SceneFlow.md) | ADR-0010 - Loading HUD + SceneFlow (NewScripts) | Aceita | Implementada | Ativa | 2026-02-04 |
| [`ADR-0011-WorldDefinition-MultiActor-GameplayScene.md`](ADR-0011-WorldDefinition-MultiActor-GameplayScene.md) | ADR-0011 - WorldDefinition multi-actor para GameplayScene (NewScripts) | Aceita | Implementada | Ativa | 2026-02-04 |
| [`ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md`](ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md) | ADR-0012 - Fluxo Pós-Gameplay (GameOver, Vitória, Restart, ExitToMenu) | Aceita | Implementada | Fechada | 2026-02-04 |
| [`ADR-0013-Ciclo-de-Vida-Jogo.md`](ADR-0013-Ciclo-de-Vida-Jogo.md) | ADR-0013 - Ciclo de Vida do Jogo (NewScripts) | Aceita | Implementada | Ativa | 2026-02-04 |
| [`ADR-0014-GameplayReset-Targets-Grupos.md`](ADR-0014-GameplayReset-Targets-Grupos.md) | ADR-0014 - GameplayReset: Targets por Grupos | Aceita | Implementada | Ativa | 2026-02-04 |
| [`ADR-0015-Baseline-2.0-Fechamento.md`](ADR-0015-Baseline-2.0-Fechamento.md) | ADR-0015 - Baseline 2.0: Fechamento | Aceita | Implementada | Ativa | 2026-02-04 |
| [`ADR-0016-ContentSwap-WorldLifecycle.md`](ADR-0016-ContentSwap-WorldLifecycle.md) | ADR-0016 - ContentSwap InPlace-only (NewScripts) | Aceita | Implementada | Ativa | 2026-02-04 |
| [`ADR-0017-LevelManager-Config-Catalog.md`](ADR-0017-LevelManager-Config-Catalog.md) | ADR-0017 - LevelManager: Config + Catalog (Single Source of Truth) | Aceita | Implementada | Ativa | 2026-02-04 |
| [`ADR-0020-LevelContent-Progression-vs-SceneRoute.md`](ADR-0020-LevelContent-Progression-vs-SceneRoute.md) | ADR-0020 - Separar LevelContent/Progression de SceneRoute/Scene Data | Aberto | Não iniciada | Ativa | 2026-02-18 |

## Templates

- **Implementação:** [`ADR-TEMPLATE.md`](ADR-TEMPLATE.md)
- **Completude / Governança:** [`ADR-TEMPLATE-COMPLETENESS.md`](ADR-TEMPLATE-COMPLETENESS.md)

> Regra: ADRs de implementação seguem o template de implementação; ADRs de fechamento/baseline seguem o template de completude (ex.: ADR-0015).