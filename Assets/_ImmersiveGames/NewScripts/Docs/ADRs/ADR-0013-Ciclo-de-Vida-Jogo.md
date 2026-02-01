# ADR-0013 — Ciclo de vida do jogo (NewScripts)

## Status

- Estado: Implementado
- Data: 2025-12-24
- Escopo: GameLoop + SceneFlow + WorldLifecycle (NewScripts)

## Contexto

O NewScripts precisava de um ciclo de vida de jogo consistente, que:

- Separe “transição de cenas” (SceneFlow) de “reset de mundo” (WorldLifecycle).
- Garanta ordenação determinística e rastreável (logs + signatures).
- Centralize a evolução de estados do jogo (GameLoop) sem depender de scripts QA para disparar fluxo real.

## Decisão

### Objetivo de produção (sistema ideal)

Padronizar o ciclo de vida (Boot → Menu → Gameplay → PostGame → Restart/ExitToMenu) via SceneFlow + WorldLifecycle + GameLoop, com contratos claros de reset, gating e observabilidade.

### Contrato de produção (mínimo)

- SceneFlow define perfis (startup/frontend/gameplay) e dispara reset **apenas** quando apropriado (gameplay).
- WorldLifecycle executa reset determinístico e publica `ResetCompleted` com reason/contextSignature padronizados.
- GameLoop inicia simulação apenas após gates liberados (ex.: IntroStage/UIConfirm).

### Não-objetivos (resumo)

Ver seção **Fora de escopo**.

## Fora de escopo

- Detalhar implementação de loading/fade (ver ADR-0009/0010).

- (não informado)

## Consequências

### Benefícios

- O fluxo de produção fica determinístico:
    - Start → Menu (`startup`) → Play → Gameplay (`gameplay`) → pós-gameplay → Restart/Exit.
- O WorldLifecycle executa reset somente quando apropriado (ex.: gameplay) e sempre sinaliza conclusão.
- O SceneFlow não conclui a transição antes do reset completar (completion gate).

### Trade-offs / Riscos

- (não informado)

### Política de falhas e fallback (fail-fast)

- Em Unity, ausência de referências/configs críticas deve **falhar cedo** (erro claro) para evitar estados inválidos.
- Evitar "auto-criação em voo" (instanciar prefabs/serviços silenciosamente) em produção.
- Exceções: apenas quando houver **config explícita** de modo degradado (ex.: HUD desabilitado) e com log âncora indicando modo degradado.


### Critérios de pronto (DoD)

- Evidência cobre Boot→Menu (SKIP), Menu→Gameplay (RESET), PostGame, Restart e ExitToMenu.
- Tokens de gate e InputMode coerentes ao longo do fluxo.

## Notas de implementação (Strict/Release)

- O **SceneFlow** (via `GameLoopSceneFlowCoordinator`) deve apenas sincronizar o `GameLoop` até **Ready** quando a transição de cena e o reset (quando aplicável) tiverem concluído.
- A transição para **Playing** (**`RequestStart`**) é responsabilidade do pipeline de início de gameplay (ex.: `LevelStartPipeline` / `IntroStageCoordinator`), **após** `IntroStageCompleted`.
- Motivo: garantir que a simulação não seja iniciada antes do jogador concluir o *Intro Stage* (invariante *Strict/Release*).

## Evidência

- **Fonte canônica atual:** [`LATEST.md`](../Reports/Evidence/LATEST.md)
- **Âncoras/assinaturas relevantes:**
  - `[SceneFlow] TransitionStarted ... profile='startup'` + `ResetCompleted ... Skipped_StartupOrFrontend`
  - `[SceneFlow] TransitionStarted ... profile='gameplay'` + `ResetRequested ... reason='SceneFlow/ScenesReady'`
  - `[IntroStage] ... reason='IntroStage/UIConfirm'` + `GameLoop ENTER Playing`
- **Contrato de observabilidade:** [`Observability-Contract.md`](../Standards/Observability-Contract.md)

## Referências

- [`Overview.md`](../Overview/Overview.md)
- [`Observability-Contract.md`](../Standards/Observability-Contract.md) — contrato canônico de reasons, campos mínimos e invariantes
- [`Reports/Evidence/LATEST.md`](../Reports/Evidence/LATEST.md)
