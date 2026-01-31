# Checklist de completude ideal por ADR (produção)

Este documento resume **o mínimo necessário** para considerar cada ADR (0009–0019) “completo para produção” sob a ótica:

- **Strict vs Release** (falha controlada em Dev/QA; degradação explícita em Release).
- **Invariants verificáveis** (ordem, gates, eventos).
- **Observabilidade canônica** (logs âncora do contrato).

> Uso: base para auditorias (CODEX read-only) e para normalização do sistema.

## Tabela (resumo)

| ADR | Tema | Para ficar “ideal de produção” (mínimos) | Evidência esperada |
|---|---|---|---|
| ADR-0009 | Fade + SceneFlow | (1) **Fail-fast em Strict** quando `FadeScene/Controller` não existe; (2) **Degraded mode explícito** em Release (config + log âncora); (3) Ordem: FadeIn → operação → ScenesReady → BeforeFadeOut → FadeOut → Completed; (4) Logs conforme Observability Contract | Logs/anchors `[OBS][Fade]` ou equivalente + trecho de código com branch Strict/Release |
| ADR-0010 | Loading HUD + SceneFlow | (1) Fail-fast em Strict para HUD/controller ausente; (2) Degraded mode explícito em Release; (3) Orquestração por eventos SceneFlow; (4) Logs canônicos | Logs `[OBS][LoadingHUD]` + branch Strict/Release |
| ADR-0011 | WorldDefinition + multi-actor | (1) Em gameplay: **worldDefinition obrigatório** em Strict; (2) validação de mínimo spawn (Player + Eater); (3) deterministic spawn pipeline | Logs `[OBS][WorldDefinition]`/`[OBS][Spawn]` + validações explícitas |
| ADR-0012 | PostGame | (1) Dependências críticas (Gate/InputMode) falham em Strict; (2) fallback explícito em Release; (3) idempotência do overlay; (4) reason/contextSignature canônicos | Logs `[OBS][PostGame]` + evidências de idempotência |
| ADR-0013 | Ciclo de vida | (1) `RequestStart()` somente após **IntroStageComplete** (ou equivalente); (2) tokens `flow.scene_transition`, `sim.gameplay` coerentes; (3) reset determinístico disparado no ponto “produção” definido | Logs `[OBS][SceneFlow]` + `[OBS][WorldLifecycle]` + ordem comprovada |
| ADR-0014 | Reset targets/grupos | (1) Classificação determinística; (2) inconsistências falham em Strict (ou policy formal); (3) ausência de target/config não vira scan silencioso sem política | Logs `[OBS][GameplayReset]` + validações |
| ADR-0015 | Baseline 2.0 | (1) Evidência canônica arquivada; (2) invariants A–E verificáveis via log; (3) método de atualização de evidências | `Docs/Reports/LATEST.md` + logs arquivados |
| ADR-0016 | ContentSwap in-place | (1) Respeitar gates `flow.scene_transition` e `sim.gameplay`; (2) policy de bloqueio/retry/abort documentada; (3) logs canônicos e reason | Logs `[OBS][ContentSwap]` + checagens de gate |
| ADR-0017 | LevelCatalog/LevelManager | (1) Resolver por ID falha em Strict se catálogo/definição ausente; (2) comportamento Release definido; (3) logs canônicos | Logs `[OBS][LevelCatalog]` + validações e policy |
| ADR-0018 | Gate de promoção | (1) Gate carrega **config real** (ou policy explícita “always on”); (2) enforcement real no fluxo; (3) logs de decisão do gate | Logs `[OBS][PromotionGate]` + fonte de config |
| ADR-0019 | Promoção Baseline 2.2 | (1) Processo documental consistente com ADR-0018; (2) quando “promovido”, evidência arquivada e linkada; (3) se não há runtime, explicitar limites | Doc de promoção + evidência |

## Checklist transversal (A–F)

- **A)** Fade/LoadingHUD: Strict + Release + degraded mode explícito
- **B)** WorldDefinition: Strict + mínimo spawn
- **C)** LevelCatalog: Strict + Release
- **D)** PostGame: Strict + Release
- **E)** Ordem do fluxo: RequestStart após IntroStageComplete
- **F)** Gates: ContentSwap respeita `flow.scene_transition` e `sim.gameplay`
