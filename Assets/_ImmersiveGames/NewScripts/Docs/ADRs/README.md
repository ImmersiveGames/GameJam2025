# ADRs (Architecture Decision Records)

Este diretÃ³rio contÃ©m decisÃµes arquiteturais do **NewScripts**.

## ConvenÃ§Ãµes de status

Cada ADR possui 3 eixos:

- **DecisÃ£o:** Proposta | Aceita | Rejeitada | SubstituÃ­da | Obsoleta  
- **ImplementaÃ§Ã£o:** NÃ£o iniciada | Em andamento | Parcial | Implementada | Obsoleta  
- **ManutenÃ§Ã£o:** Ativa | Fechada | Obsoleta  

> Regra prÃ¡tica: uma decisÃ£o pode estar **Aceita** mesmo com implementaÃ§Ã£o **Parcial**; isso evita confundir â€œaprovaÃ§Ã£o do designâ€ com â€œtrabalho concluÃ­doâ€.

## Ãndice

| ADR | TÃ­tulo | DecisÃ£o | ImplementaÃ§Ã£o | ManutenÃ§Ã£o | Ãšltima atualizaÃ§Ã£o |
|---|---|---:|---:|---:|---:|
| [`ADR-0009-FadeSceneFlow.md`](ADR-0009-FadeSceneFlow.md) | ADR-0009 â€” Fade + SceneFlow (NewScripts) | Aceita | Implementada | Ativa | 2026-01-31 |
| [`ADR-0010-LoadingHud-SceneFlow.md`](ADR-0010-LoadingHud-SceneFlow.md) | ADR-0010 â€” Loading HUD + SceneFlow (NewScripts) | Aceita | Implementada | Ativa | 2026-02-01 |
| [`ADR-0011-WorldDefinition-MultiActor-GameplayScene.md`](ADR-0011-WorldDefinition-MultiActor-GameplayScene.md) | ADR-0011 â€” WorldDefinition multi-actor para GameplayScene (NewScripts) | Aceita | Implementada | Ativa | 2026-01-31 |
| [`ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md`](ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md) | ADR-0012 â€” Fluxo PÃ³s-Gameplay (GameOver, VitÃ³ria, Restart, ExitToMenu) | Aceita | Implementada | Fechada | 2026-01-31 |
| [`ADR-0013-Ciclo-de-Vida-Jogo.md`](ADR-0013-Ciclo-de-Vida-Jogo.md) | ADR-0013 — Ciclo de Vida do Jogo (NewScripts) | Aceita | Implementada | Ativa | 2026-02-03 |
| [`ADR-0014-GameplayReset-Targets-Grupos.md`](ADR-0014-GameplayReset-Targets-Grupos.md) | ADR-0014 â€” GameplayReset: Targets por Grupos | Aceita | Implementada | Ativa | 2026-02-01 |
| [`ADR-0015-Baseline-2.0-Fechamento.md`](ADR-0015-Baseline-2.0-Fechamento.md) | ADR-0015 - Baseline 2.0: Fechamento | Aceita | Implementada | Ativa | 2026-02-03 |
| [`ADR-0016-ContentSwap-WorldLifecycle.md`](ADR-0016-ContentSwap-WorldLifecycle.md) | ADR-0016 — ContentSwap InPlace-only (NewScripts) | Aceita | Implementada | Ativa | 2026-02-03 |
| [`ADR-0017-LevelManager-Config-Catalog.md`](ADR-0017-LevelManager-Config-Catalog.md) | ADR-0017 — LevelManager: Config + Catalog (Single Source of Truth) | Aceita | Implementada | Ativa | 2026-02-03 |
| [`ADR-0018-Gate-de-Promocao-Baseline2.2.md`](ADR-0018-Gate-de-Promocao-Baseline2.2.md) | ADR-0018 - Gate de Promocao (Baseline 2.2) | Aceita | Parcial | Ativa | 2026-01-31 |
| [`ADR-0019-Promocao-Baseline2.2.md`](ADR-0019-Promocao-Baseline2.2.md) | ADR-0019 - Promocao do Baseline 2.2 | Aceita | Parcial | Ativa | 2026-02-01 |


## Template

Para novas decisÃµes, use: [`ADR-TEMPLATE.md`](ADR-TEMPLATE.md)
