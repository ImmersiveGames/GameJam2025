# ADR-0021 - Baseline 3.0 (Completeness)

## Status

- Estado: **Aceito (Fechado)**
- Data (decisao): 2026-02-19
- Ultima atualizacao: 2026-03-11
- Tipo: Baseline / Completeness
- Escopo: NewScripts (SceneFlow, Navigation, LevelFlow, WorldLifecycle, LoadingHud, GameLoop)

## Objetivo

Definir e fechar um baseline 3.0 de completude: invariantes minimos para garantir que o trilho Macro-Level esta solido para evolucao sem regressao.

## Fonte de verdade atual

- Codigo atual do repositorio.
- Auditoria ADR-0022..ADR-0027: `Docs/Reports/Audits/2026-03-04/ADR-0022-0027-Code-Audit.md`.
- Hardening H1: `Docs/Reports/Audits/2026-03-05/H1-Hardening-Changes.md`.
- Sincronizacao final do eixo canon-only: `Docs/Reports/Audits/2026-03-11/CANON-ONLY-AXIS-SYNC.md`.

## Escopo do baseline (cobertura atual)

Cobre:

- Menu/frontend sem reset indevido.
- Gameplay com reset e selecao de level deterministicos.
- Restart/Exit-to-menu sem trilho paralelo em Strict/Production.
- Reset em dois niveis (Macro e Level).
- Swap local intra-macro sem transicao macro.
- IntroStage e acoes pos-level no dominio de Level.

## Checklist de completude (Baseline 3.0)

### A) SceneFlow / Macro transitions

- [x] `RouteApplied` + politica de reset por rota (`requiresWorldReset`).
- [x] `TransitionStarted/ScenesReady/TransitionCompleted` com assinatura macro consistente.
- [x] Fade + Loading HUD em ordem correta de pipeline.
- [x] `LevelPrepare` no completion gate antes do FadeOut (`MacroLevelPrepareCompletionGate`).

### B) WorldLifecycle / Resets

- [x] MacroReset executa pipeline completo.
- [x] Completion gate do SceneFlow e liberado corretamente.
- [x] Two-level reset (`Macro` vs `Level`) via `WorldResetCommands`.
- [x] Reset required: Strict/Production = fail-fast H1; DEV = degraded fallback com completion.

### C) LevelFlow / selecao e snapshot

- [x] Trilho canonico: `StartGameplayDefaultAsync(...)` + `SwapLevelLocalAsync(LevelDefinitionAsset, ...)` via LevelFlow runtime.
- [x] Selecao observavel com `selectionVersion` e `levelSignature`.
- [x] Restart prioriza snapshot canonico.
- [x] Swap local intra-macro sem transicao macro (ADR-0026).

### D) GameLoop / gates / InputMode

- [x] `flow.scene_transition` fecha/abre corretamente.
- [x] IntroStage bloqueia/libera `sim.gameplay` e alterna InputMode.
- [x] Pause/Resume e PostGame usam tokens dedicados.
- [x] Pos-level (Restart/NextLevel/ExitToMenu) via servico de dominio (ADR-0027).

## Criterios de saida

Baseline 3.0 e considerado fechado quando:

1. ADR-0022..ADR-0027 estao implementados e auditados contra codigo.
2. O eixo principal nao promove mais superficies paralelas de compat/legacy no runtime.
3. Documentacao ADR e auditorias estao sincronizadas com o comportamento real.

**Situacao atual:** criterios atendidos.

## Escopo e limites do fechamento

- Este ADR deve ser lido como **fechamento do eixo principal**:
  - `LevelFlow`
  - `LevelDefinition`
  - `Navigation`
  - `WorldLifecycle V2`
  - tooling/editor/QA associado
- Este ADR **nao** declara `NewScripts/**` inteiro como 100% canon-only.
- Excecoes remanescentes fora/borda do eixo fechado:
  - `Gameplay ActorGroupRearm` com fallback legado de actor-kind/string
  - pequeno residuo editor/serializado em `GameNavigationIntentCatalogAsset`

## Changelog

- 2026-03-11: baseline re-sincronizado para o estado pos-H1..H7; eixo principal registrado como canon-only com excecoes remanescentes explicitadas.
- 2026-03-05: baseline marcado como fechado; alinhado as auditorias de 2026-03-04 e 2026-03-05.
- 2026-03-01: atualizacao de status e checklist baseada no estado daquele momento.
- 2026-02-25: checklist inicial alinhado as ADRs 0022-0027.

