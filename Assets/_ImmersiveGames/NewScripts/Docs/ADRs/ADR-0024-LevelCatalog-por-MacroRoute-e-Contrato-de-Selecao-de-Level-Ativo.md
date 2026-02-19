# ADR-0024 — LevelCatalog por MacroRoute e Contrato de Seleção de Level Ativo

## Status

- Estado: Proposto
- Data (decisão): 2026-02-19
- Última atualização: 2026-02-19
- Tipo: Implementação
- Escopo: NewScripts/Modules (SceneFlow, Navigation, LevelFlow, WorldLifecycle)


## Contexto

A intenção é que **MacroRoutes** representem “espaços macros” do jogo (Menu, Gameplay, Tutorial, Hub...).  
Apenas alguns macros possuem *levels* (Gameplay, Tutorial). Menu normalmente não.

Para o macro “com levels”, ao entrar nele o sistema deve:
- identificar o catálogo de levels do macro;
- selecionar um único level ativo;
- preparar o conteúdo antes da conclusão do loading macro (FadeOut macro).

Além disso, precisamos suportar:
- progressão por ordem (next/prev);
- seleção direta por id/index;
- cadeia de levels (ex.: mundo 1-1, 1-2...).

## Decisão

Definir um contrato explícito:

- Cada **MacroRouteDefinition** (ou RouteKind=Gameplay/Tutorial) pode opcionalmente ter:
  - `LevelCollectionRef` (catálogo/coleção de LevelDefinitions)
  - `DefaultLevelId` (ou “primeiro do catálogo”)

- Existe **exatamente 1 level ativo por macro**:
  - `ILevelSelectionState` guarda `{ macroRouteId, levelId, contentId, version }`
  - Mudança de macro invalida seleção anterior e força seleção do default.

- `LevelCatalog` continua sendo *source-of-truth* para produção, mas QA/Dev pode inserir entradas auxiliares (qa.*) para evidência (como N→1 A/B).

### Regras

- LevelDefinitions **não** carregam/unloadam “cenas macro”; isso é responsabilidade da rota macro.
- LevelDefinitions descrevem apenas:
  - conteúdo aditivo do level (cenas a adicionar / addressables futuramente);
  - conteúdo/variantes (contentId/contentRef);
  - flags: `hasIntroStage`, `allowCurtainIn`, `allowCurtainOut`, etc.

## Implicações

- Clarifica separação:
  - Macro: carrega base (GameplayScene + UIGlobal etc.) e política de reset.
  - Level: carrega *conteúdo do level* e stages do level (intro/post).
- Permite múltiplos macros com catálogos distintos (Tutorial vs Gameplay).

## Alternativas consideradas

1) **Um catálogo global de levels sem vínculo com macro**  
Rejeitado: perde encapsulamento; aumenta risco de usar level “errado” no macro errado.

2) **Levels como rotas “micro”**  
Rejeitado: volta a confundir e obriga unloads manuais; queremos “swap local”.

## Critérios de aceite (DoD)

- Ao entrar em uma macro com catálogo, o sistema seleciona automaticamente o default level.
- Logs [OBS] mostram:
  - Macro: route aplicada / policy / started/ready/completed
  - Level: level selecionado + conteúdo aplicado
- QA/Dev: ações de N→1 (A/B/Sequence) demonstram:
  - mesmo routeRef macro;
  - levelId distintos;
  - contentId distintos;
  - sem exigir nova transição macro.

## Referências

- ADR-0017 — LevelManager/Config/Catalog (provável reabertura)
- ADR-0019 — Navigation IntentCatalog
- ADR-0020 — LevelContent/Progression vs SceneRoute
- ADR-0021 — Baseline 3.0 (Completeness)
