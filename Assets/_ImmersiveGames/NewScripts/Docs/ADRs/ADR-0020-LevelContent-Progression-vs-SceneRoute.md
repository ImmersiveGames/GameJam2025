# ADR-0020 — Level/Content Progression vs SceneRoute

## Status
- **State:** Implemented
- **Date:** 2026-02-19
- **Owner:** Innocenti
- **Tags:** LevelFlow, LevelCatalog, ContentSwap, SceneFlow, Navigation, WorldLifecycle
- **Scope:**
    - Runtime: `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/**`, `.../Modules/Navigation/**`, `.../Modules/SceneFlow/**`
    - Assets: `Assets/Resources/Navigation/LevelCatalog.asset`
- **Related ADRs:** ADR-0017 (LevelManager/Config/Catalog), Plan-v2 (F4 LevelFlow)

## Summary
**Decision:** manter *SceneRoute* responsável apenas por **carregamento/aplicação de cenas** (SceneFlow) e mover a noção de **progresso de gameplay** (LevelId → ContentId/ContentRef) para o trilho de **LevelFlow/LevelCatalog + RestartSnapshot**.

Na prática:
- Um `LevelId` seleciona um *level entry* no `LevelCatalog`.
- O entry referencia a rota (preferencialmente `routeRef`) e o conteúdo (ex.: `contentId`/`contentRef`).
- `StartGameplay(levelId)` dispara o SceneFlow para a mesma rota de gameplay, mas a *identidade do conteúdo* fica no snapshot (e pode mudar sem mudar a rota).

## Problem statement
O sistema precisava suportar:
1) **N→1**: múltiplos `LevelId` diferentes apontando para **a mesma rota** (ex.: `level.1`), mas com **conteúdos distintos**, para evidenciar progressão/variação sem multiplicar rotas.
2) **ContentSwap in-place**: trocar conteúdo dentro da mesma gameplay (sem transição de cena) e garantir que o snapshot de “start/restart” reflita o conteúdo atual.

O risco original era “vazar” progressão de conteúdo para dentro do `SceneRoute` (ex.: criar rotas por conteúdo), criando:
- explosão de rotas;
- mais wiring por string;
- dedupe de transição confundindo o cenário;
- dificuldade de observabilidade do “conteúdo atual” versus “rota atual”.

## Constraints and requirements
- **Direct-ref-first**: quando possível, preferir referências diretas (SO) em vez de strings.
- **Fail-fast** para assets obrigatórios; sem fallback silencioso em runtime.
- **Observabilidade**: logs [OBS] e [QA] com âncoras estáveis para gerar evidências.
- **Compatibilidade**: aceitar, quando necessário, campos legados (ex.: `routeId`) sem divergir de `routeRef`.

## Options considered
### Option A — Colocar progressão/conteúdo em SceneRoute
- Ex.: route por content (`level.1.content.2`), ou metadata de content dentro do route.
- **Prós:** “um único lugar”.
- **Contras:** acoplamento errado (cena ≠ conteúdo), explosão de rotas e churn no SceneFlow.

### Option B — LevelFlow/LevelCatalog como fonte de verdade de progressão
- `SceneRoute` fica “sobre cenas”, `LevelId` fica “sobre gameplay/progressão”.
- **Prós:** separa responsabilidades; N→1 vira uma configuração de catálogo; ContentSwap não precisa de cena.
- **Contras:** exige trilho claro (StartGameplay(levelId)) e snapshot/telemetria bem definidos.

### Option C — Híbrido (route + level)
- Parte no route, parte no level.
- **Contras:** invariantes ambíguas e maior chance de divergência.

## Decision
Escolher **Option B**.

### Decision details
- `SceneRouteDefinition`/`SceneRouteCatalog` definem **apenas**: cenas a carregar/descarregar, cena ativa, perfil/estilo de transição, e policy (ex.: requiresWorldReset).
- `LevelCatalog` define: `levelId` + `routeRef` (source of truth) + `contentId`/`contentRef` (identidade do conteúdo).
- `StartGameplay(levelId)`:
    1) resolve o entry do catálogo;
    2) publica/atualiza o snapshot (`RestartContext` / “GameplayStartSnapshot”) com `levelId/routeId/styleId/contentId`;
    3) despacha o intent canônico de gameplay para aplicar a rota.
- `ContentSwap in-place`:
    - atualiza o “content atual” e também o snapshot de restart, **sem exigir** transition de rota.

## Consequences
- **N→1** é suportado naturalmente com múltiplos entries no `LevelCatalog` apontando para a mesma `routeRef`.
- O SceneFlow pode **dedupar** transições repetidas (mesma signature) sem quebrar o conceito de “conteúdo atual” — porque o conteúdo não depende de uma transição de cena.
- O catálogo pode conter rotas duplicadas (por design). Mantemos telemetria (ex.: `duplicatedRoutes`) como **[OBS]**, não fatal.

## Implementation notes
- Foram adicionadas entradas DEV/QA no `LevelCatalog`:
    - `qa.level.nto1.a` → `routeRef=level.1` + `contentId=content.1`
    - `qa.level.nto1.b` → `routeRef=level.1` + `contentId=content.2`
    - `routeId` legado intencionalmente vazio para evitar divergência com `routeRef`.
- Existe menu QA/Dev para acionar N→1:
    - `QA/LevelFlow/NTo1/Start A`
    - `QA/LevelFlow/NTo1/Start B`
    - `QA/LevelFlow/NTo1/Run Sequence A->B`

## Observability and evidence
### Evidence 1 — ContentSwap in-place atualiza snapshot de restart
Arquivo de evidência:
- `ADR-0020-Evidence-ContentSwap-2026-02-18.log`

Âncoras esperadas:
- `[QA][ContentSwap] ... start contentId='content.2'`
- `[OBS][ContentSwap] ContentSwapRequested ... contentId='content.2'`
- `[OBS][Navigation] RestartSnapshotContentUpdated ... contentId='content.2'`

### Evidence 2 — N→1 (A e B) com mesma rota e conteúdos diferentes
Arquivo de evidência:
- `ADR-0020-Evidence-LevelFlow-NTo1-2026-02-18.log`

Âncoras esperadas:
- `[QA][LevelFlow] NTo1 start levelId='qa.level.nto1.a' routeRef='level.1'`
- `[OBS][SceneFlow] RouteResolvedVia=AssetRef levelId='qa.level.nto1.a' routeId='level.1'`
- `[OBS][Level] LevelSelected ... levelId='qa.level.nto1.a' ... contentId='content.1'`
- Repetir para `qa.level.nto1.b` com `contentId='content.2'`.

### Evidence 3 — Sequência A→B pode dedupar transição (expected)
Âncora:
- `[SceneFlow] Dedupe: TransitionAsync ignorado (signature repetida...)`

Interpretação:
- **Aceitável** para N→1 quando a rota é idêntica; o conteúdo/snapshot muda independentemente da transição.

### Commands (auditoria rápida)
- `rg -n "\[QA\]\[ContentSwap\]|\[OBS\]\[ContentSwap\]|RestartSnapshotContentUpdated" Assets/_ImmersiveGames/NewScripts/`
- `rg -n "\[QA\]\[LevelFlow\] NTo1 start|RouteResolvedVia=AssetRef|LevelSelected" Assets/_ImmersiveGames/NewScripts/`

## Follow-ups
- Se o produto exigir “aplicar conteúdo” como efeito colateral do `LevelSelected` (além de snapshot), formalizar um *ContentApply* explícito (event-driven) separado do SceneFlow.
- Evolução futura (fora do escopo deste ADR): migração de cenas para Addressables.

## Sources
- Logs de evidência anexados e logs [OBS]/[QA] do runtime.
