# ADR-0017 — LevelCatalog Evidência (2026-02-03)

Este snapshot valida a **resolução do LevelCatalog** e a **aplicação de nível** via LevelSession/LevelManager, usando o log bruto da pasta.

## Fonte

- Log bruto: `Baseline-2.2-Smoke-LastRun.log`
- Contexto QA: ações `QA/Levels/Select/Initial` e `QA/Levels/ApplySelected`.

## Âncoras observáveis

- Catalog carregado via Resources (`NewScripts/Config/LevelCatalog`).
- Definições resolvidas (`level.1`, `level.2`).
- Plano inicial resolvido (`level.1`).
- Seleção inicial e aplicação in-place via LevelManager.

## Trecho mínimo (log)

```text
[INFO] [ResourcesLevelCatalogProvider] [OBS][LevelCatalog] CatalogLoadAttempt path='NewScripts/Config/LevelCatalog' result='hit' fallback='Levels/LevelCatalog'.
[INFO] [ResourcesLevelCatalogProvider] [OBS][LevelCatalog] CatalogLoaded name='LevelCatalog' initial='level.1' orderedCount='2' definitionsCount='2' source='new'.
[INFO] [LevelCatalogResolver] [OBS][LevelCatalog] DefinitionResolved levelId='level.1' contentId='content.1'.
[INFO] [LevelCatalogResolver] [OBS][LevelCatalog] DefinitionResolved levelId='level.2' contentId='content.2'.
[INFO] [LevelCatalogResolver] [OBS][LevelCatalog] InitialPlanResolved levelId='level.1' contentId='content.1' source='FirstEligible'.
<color=#A8DEED>[INFO] [LevelSessionService] [OBS][Level] InitialSelected levelId='level.1' contentId='content.1' reason='QA/Levels/Select/Initial' source='CatalogInitial'.</color>
<color=#A8DEED>[INFO] [LevelSessionService] [OBS][Level] ApplyRequested levelId='level.1' contentId='content.1' selectedLevelId='level.1' selectedContentId='content.1' appliedLevelId='' appliedContentId='' reason='QA/Levels/ApplySelected'.</color>
<color=#A8DEED>[INFO] [LevelManager] [OBS][Level] LevelChangeRequested levelId='level.1' contentId='content.1' mode='InPlace' reason='QA/Levels/ApplySelected' contentSig='sig.content.1'.</color>
<color=#A8DEED>[INFO] [LevelManager] [OBS][Level] LevelChangeStarted levelId='level.1' contentId='content.1' mode='InPlace' reason='QA/Levels/ApplySelected'.</color>
<color=#A8DEED>[INFO] [LevelManager] [OBS][Level] LevelChangeCompleted levelId='level.1' contentId='content.1' mode='InPlace' reason='QA/Levels/ApplySelected'.</color>
<color=#4CAF50>[INFO] [LevelSessionService] [OBS][Level] Applied levelId='level.1' contentId='content.1' reason='QA/Levels/ApplySelected'.</color>
```

## Interpretação

- O catálogo foi encontrado no path canônico e contém `initial='level.1'` com duas definições.
- As definições `level.1` e `level.2` foram resolvidas pelo resolver.
- O plano inicial foi resolvido e aplicado via LevelSession/LevelManager, com ContentSwap in-place.
