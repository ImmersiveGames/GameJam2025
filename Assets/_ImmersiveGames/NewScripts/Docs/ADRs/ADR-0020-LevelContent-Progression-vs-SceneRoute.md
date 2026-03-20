# ADR-0020 â€” Level/Content Progression vs SceneRoute

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
**Decision:** manter *SceneRoute* responsÃ¡vel apenas por **carregamento/aplicaÃ§Ã£o de cenas** (SceneFlow) e mover a noÃ§Ã£o de **progresso de gameplay** (LevelId â†’ ContentId/ContentRef) para o trilho de **LevelFlow/LevelCatalog + RestartSnapshot**.

Na prÃ¡tica:
- Um `LevelId` seleciona um *level entry* no `LevelCatalog`.
- O entry referencia a rota (preferencialmente `routeRef`) e o conteÃºdo (ex.: `contentId`/`contentRef`).
- `StartGameplay(levelId)` dispara o SceneFlow para a mesma rota de gameplay, mas a *identidade do conteÃºdo* fica no snapshot (e pode mudar sem mudar a rota).

## Problem statement
O sistema precisava suportar:
1) **Nâ†’1**: mÃºltiplos `LevelId` diferentes apontando para **a mesma rota** (ex.: `level.1`), mas com **conteÃºdos distintos**, para evidenciar progressÃ£o/variaÃ§Ã£o sem multiplicar rotas.
2) **ContentSwap in-place**: trocar conteÃºdo dentro da mesma gameplay (sem transiÃ§Ã£o de cena) e garantir que o snapshot de â€œstart/restartâ€ reflita o conteÃºdo atual.

O risco original era â€œvazarâ€ progressÃ£o de conteÃºdo para dentro do `SceneRoute` (ex.: criar rotas por conteÃºdo), criando:
- explosÃ£o de rotas;
- mais wiring por string;
- dedupe de transiÃ§Ã£o confundindo o cenÃ¡rio;
- dificuldade de observabilidade do â€œconteÃºdo atualâ€ versus â€œrota atualâ€.

## Constraints and requirements
- **Direct-ref-first**: quando possÃ­vel, preferir referÃªncias diretas (SO) em vez de strings.
- **Fail-fast** para assets obrigatÃ³rios; sem fallback silencioso em runtime.
- **Observabilidade**: logs [OBS] e [QA] com Ã¢ncoras estÃ¡veis para gerar evidÃªncias.
- **Compatibilidade**: aceitar, quando necessÃ¡rio, campos legados (ex.: `routeId`) sem divergir de `routeRef`.

## Options considered
### Option A â€” Colocar progressÃ£o/conteÃºdo em SceneRoute
- Ex.: route por content (`level.1.content.2`), ou metadata de content dentro do route.
- **PrÃ³s:** â€œum Ãºnico lugarâ€.
- **Contras:** acoplamento errado (cena â‰  conteÃºdo), explosÃ£o de rotas e churn no SceneFlow.

### Option B â€” LevelFlow/LevelCatalog como fonte de verdade de progressÃ£o
- `SceneRoute` fica â€œsobre cenasâ€, `LevelId` fica â€œsobre gameplay/progressÃ£oâ€.
- **PrÃ³s:** separa responsabilidades; Nâ†’1 vira uma configuraÃ§Ã£o de catÃ¡logo; ContentSwap nÃ£o precisa de cena.
- **Contras:** exige trilho claro (StartGameplay(levelId)) e snapshot/telemetria bem definidos.

### Option C â€” HÃ­brido (route + level)
- Parte no route, parte no level.
- **Contras:** invariantes ambÃ­guas e maior chance de divergÃªncia.

## Decision
Escolher **Option B**.

### Decision details
- `SceneRouteDefinition`/`SceneRouteCatalog` definem **apenas**: cenas a carregar/descarregar, cena ativa, perfil/estilo de transiÃ§Ã£o, e policy (ex.: requiresWorldReset).
- `LevelCatalog` define: `levelId` + `routeRef` (source of truth) + `contentId`/`contentRef` (identidade do conteÃºdo).
- `StartGameplay(levelId)`:
    1) resolve o entry do catÃ¡logo;
    2) publica/atualiza o snapshot (`RestartContext` / â€œGameplayStartSnapshotâ€) com `levelId/routeId/styleId/contentId`;
    3) despacha o intent canÃ´nico de gameplay para aplicar a rota.
- `ContentSwap in-place`:
    - atualiza o â€œcontent atualâ€ e tambÃ©m o snapshot de restart, **sem exigir** transition de rota.

## Consequences
- **Nâ†’1** Ã© suportado naturalmente com mÃºltiplos entries no `LevelCatalog` apontando para a mesma `routeRef`.
- O SceneFlow pode **dedupar** transiÃ§Ãµes repetidas (mesma signature) sem quebrar o conceito de â€œconteÃºdo atualâ€ â€” porque o conteÃºdo nÃ£o depende de uma transiÃ§Ã£o de cena.
- O catÃ¡logo pode conter rotas duplicadas (por design). Mantemos telemetria (ex.: `duplicatedRoutes`) como **[OBS]**, nÃ£o fatal.

## Implementation notes
- Foram adicionadas entradas DEV/QA no `LevelCatalog`:
    - `qa.level.nto1.a` â†’ `routeRef=level.1` + `contentId=content.1`
    - `qa.level.nto1.b` â†’ `routeRef=level.1` + `contentId=content.2`
    - `routeId` legado intencionalmente vazio para evitar divergÃªncia com `routeRef`.
- Existe menu QA/Dev para acionar Nâ†’1:
    - `QA/LevelFlow/NTo1/Start A`
    - `QA/LevelFlow/NTo1/Start B`
    - `QA/LevelFlow/NTo1/Run Sequence A->B`

## Observability and evidence
### Evidence 1 â€” ContentSwap in-place atualiza snapshot de restart
Arquivo de evidÃªncia:
- `ADR-0020-Evidence-ContentSwap-2026-02-18.log`

Ã‚ncoras esperadas:
- `[QA][ContentSwap] ... start contentId='content.2'`
- `[OBS][ContentSwap] ContentSwapRequested ... contentId='content.2'`
- `[OBS][Navigation] RestartSnapshotContentUpdated ... contentId='content.2'`

### Evidence 2 â€” Nâ†’1 (A e B) com mesma rota e conteÃºdos diferentes
Arquivo de evidÃªncia:
- `ADR-0020-Evidence-LevelFlow-NTo1-2026-02-18.log`

Ã‚ncoras esperadas:
- `[QA][LevelFlow] NTo1 start levelId='qa.level.nto1.a' routeRef='level.1'`
- `[OBS][SceneFlow] RouteResolvedVia=AssetRef levelId='qa.level.nto1.a' routeId='level.1'`
- `[OBS][Level] LevelSelected ... levelId='qa.level.nto1.a' ... contentId='content.1'`
- Repetir para `qa.level.nto1.b` com `contentId='content.2'`.

### Evidence 3 â€” SequÃªncia Aâ†’B pode dedupar transiÃ§Ã£o (expected)
Ã‚ncora:
- `[SceneFlow] Dedupe: TransitionAsync ignorado (signature repetida...)`

InterpretaÃ§Ã£o:
- **AceitÃ¡vel** para Nâ†’1 quando a rota Ã© idÃªntica; o conteÃºdo/snapshot muda independentemente da transiÃ§Ã£o.

### Commands (auditoria rÃ¡pida)
- `rg -n "\[QA\]\[ContentSwap\]|\[OBS\]\[ContentSwap\]|RestartSnapshotContentUpdated" Assets/_ImmersiveGames/NewScripts/`
- `rg -n "\[QA\]\[LevelFlow\] NTo1 start|RouteResolvedVia=AssetRef|LevelSelected" Assets/_ImmersiveGames/NewScripts/`

## Follow-ups
- Se o produto exigir â€œaplicar conteÃºdoâ€ como efeito colateral do `LevelSelected` (alÃ©m de snapshot), formalizar um *ContentApply* explÃ­cito (event-driven) separado do SceneFlow.
- EvoluÃ§Ã£o futura (fora do escopo deste ADR): migraÃ§Ã£o de cenas para Addressables.

## Sources
- Logs de evidÃªncia anexados e logs [OBS]/[QA] do runtime.

