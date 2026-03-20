# ADR-0017 â€” LevelManager: Config + Catalog (Single Source of Truth)

> **Status atualizado (runtime):** o fluxo de Levels (LevelManager/LevelCatalog) estÃ¡ **descontinuado** em favor de `LevelFlow` (`LevelCatalogAsset` + `ILevelFlowService`).  
> O bootstrap atual bloqueia a coexistÃªncia de dois catÃ¡logos para evitar ambiguidade.

## Status

- Estado: Implementado
- Data (decisÃ£o): 2026-01-31
- Ãšltima atualizaÃ§Ã£o: 2026-02-04
- Tipo: ImplementaÃ§Ã£o
- Escopo: NewScripts â†’ Modules/Levels + Docs/Reports

## Contexto

O subsistema de Levels jÃ¡ existe em `Assets/_ImmersiveGames/NewScripts/Modules/Levels/` (ILevelManager, LevelManager, LevelPlan, LevelChangeOptions). O ContentSwap Ã© InPlace-only e Ã© o executor tÃ©cnico real; o LevelManager orquestra a mudanÃ§a de conteÃºdo. A etapa de "start" (IntroStage/GameLoop) Ã© tratada por mÃ³dulos prÃ³prios (fora do LevelManager). PorÃ©m, **nÃ£o existe** hoje uma fonte de verdade configurÃ¡vel para nÃ­veis (LevelDefinition/LevelCatalog): as intenÃ§Ãµes aparecem em docs (Baseline 2.2), mas nÃ£o hÃ¡ assets concretos nem resolver/provedor para evitar hardcode. Essa lacuna conflita com o objetivo de padronizaÃ§Ã£o.

Para manter consistÃªncia arquitetural (SRP, DIP) e evitar dependÃªncias diretas em listas hardcoded, precisamos de uma **configuraÃ§Ã£o centralizada via ScriptableObjects**, consumida pelo LevelManager/QA/resolvedor, com observabilidade alinhada ao contrato canÃ´nico.

## DecisÃ£o

### Objetivo de produÃ§Ã£o (sistema ideal)

Centralizar definiÃ§Ã£o e descoberta de levels/fases via um catÃ¡logo de configs (LevelManager), evitando hardcode e permitindo evoluÃ§Ã£o (fases, campanhas, playlists) sem quebrar o fluxo de produÃ§Ã£o.

### Contrato de produÃ§Ã£o (mÃ­nimo)

- CatÃ¡logo Ã© a fonte de verdade para enumerar conteÃºdos jogÃ¡veis (ids, metadata, configs).
- SeleÃ§Ã£o de level/fase nÃ£o depende de assets soltos; deve ser resolvida via id/config.
- MudanÃ§a de level pode ser in-place (ContentSwap) ou via transiÃ§Ã£o (SceneFlow), explicitando o modo.
- Falhas de id/config ausente sÃ£o fail-fast (nÃ£o gerar level default silencioso).

### NÃ£o-objetivos (resumo)

Ver seÃ§Ã£o **Fora de escopo**.

## Fora de escopo

- UI/UX completa de seleÃ§Ã£o de level (apenas contrato e plumbing).

- Refactor total de nomenclaturas legadas no runtime.
- RemoÃ§Ã£o imediata de bridges legadas (ContentSwapStart*), que permanecem atÃ© migraÃ§Ã£o completa.
- Implementar um sistema de campanha completo (Campaign/Progression) alÃ©m de LevelCatalog.

## ConsequÃªncias

### BenefÃ­cios
- Fonte Ãºnica de verdade para nÃ­veis, eliminando hardcode em runtime.
- Melhora o alinhamento com SRP/DIP e facilita QA/evidÃªncias.
- Padroniza progressÃ£o de nÃ­veis no multiplayer local (determinismo).

### Trade-offs / Riscos
- Requer criaÃ§Ã£o e manutenÃ§Ã£o de assets (ScriptableObjects) e seus providers/resolvers.
- MigraÃ§Ã£o incremental: enquanto assets nÃ£o existirem, o LevelManager ainda dependerÃ¡ de fallback/QA.

### PolÃ­tica de falhas e fallback (fail-fast)

- Em Unity, ausÃªncia de referÃªncias/configs crÃ­ticas deve **falhar cedo** (erro claro) para evitar estados invÃ¡lidos.
- Evitar "auto-criaÃ§Ã£o em voo" (instanciar prefabs/serviÃ§os silenciosamente) em produÃ§Ã£o.
- ExceÃ§Ãµes: apenas quando houver **config explÃ­cita** de modo degradado (ex.: HUD desabilitado) e com log Ã¢ncora indicando modo degradado.


### CritÃ©rios de pronto (DoD)

- Existe API estÃ¡vel para: listar levels, resolver idâ†’config, aplicar seleÃ§Ã£o.
- EvidÃªncia/logs para pelo menos um caminho (QA ou produÃ§Ã£o) demonstrando resoluÃ§Ã£o do catÃ¡logo.

## ImplementaÃ§Ã£o (arquivos impactados)

### Runtime / Editor (cÃ³digo e assets)

- **Assets/_ImmersiveGames/NewScripts/Modules/Levels**
  - `Assets/_ImmersiveGames/NewScripts/Modules/Levels/Catalogs/LevelCatalog.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/Levels/Definitions/LevelDefinition.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/Levels/Providers/ILevelCatalogProvider.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/Levels/Providers/ILevelDefinitionProvider.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/Levels/Resolvers/LevelCatalogResolver.cs`
- **Gameplay**
  - `Assets/_ImmersiveGames/NewScripts/Modules/ContentSwap/Runtime/InPlaceContentSwapService.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/Levels/LevelManager.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/Levels/LevelPlan.cs`

### Docs / evidÃªncias relacionadas

- `Docs/Reports/Evidence/LATEST.md`
- `Docs/Overview/Overview.md`
- `Docs/Standards/Standards.md`

## Notas de implementaÃ§Ã£o

- Assets sugeridos (paths):
  - `Assets/_ImmersiveGames/NewScripts/Modules/Levels/Definitions/LevelDefinition.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/Levels/Catalogs/LevelCatalog.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/Levels/Providers/ILevelDefinitionProvider.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/Levels/Providers/ILevelCatalogProvider.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/Levels/Resolvers/LevelCatalogResolver.cs`
- LevelManager deve consumir **apenas** abstraÃ§Ãµes (providers/resolvers), mantendo DIP.
- Observabilidade deve reutilizar strings existentes no contrato (nÃ£o criar reasons novos).

### Etapa 0 â€” Artefatos entregues (concluÃ­da)
- ScriptableObjects `LevelDefinition` e `LevelCatalog` com campos mÃ­nimos.
- Providers: `ResourcesLevelCatalogProvider` + `LevelDefinitionProviderFromCatalog`.
- Resolver: `ILevelCatalogResolver` + `LevelCatalogResolver`.

### Etapa 1 â€” concluÃ­da
- DI: registro de providers/resolver + `ILevelManager` via `LevelManager` no `GlobalCompositionRoot`.
- QA: `LevelDevContextMenu` e `LevelDevInstaller`.
- EvidÃªncia: atualizaÃ§Ã£o de Docs/Reports/Evidence/LATEST.md e snapshot dedicado com QA de Level.

## EvidÃªncia

- **Ãšltima evidÃªncia (log bruto):** `Docs/Reports/Evidence/LATEST.md`

- **Fonte canÃ´nica atual:** [LATEST.md](../Reports/Evidence/LATEST.md)
- **Snapshot dedicado (ADR-0017):** [ADR-0017-LevelCatalog-Evidence-2026-02-03.md](../Reports/Evidence/2026-02-03/ADR-0017-LevelCatalog-Evidence-2026-02-03.md)
- **Contrato de observabilidade:** [Observability-Contract.md](../Standards/Standards.md#observability-contract)


- Snapshot datado em `Docs/Reports/Evidence/<YYYY-MM-DD>/` com:
  - ResoluÃ§Ã£o por catÃ¡logo (`QA/Levels/Resolve/Definitions`).
  - MudanÃ§a de nÃ­vel (LevelChange + ContentSwap + IntroStage).
- AtualizaÃ§Ã£o do `Docs/Reports/Evidence/LATEST.md`.

## ReferÃªncias

- [README.md](../README.md)
- [Docs/Overview/Overview.md](../Overview/Overview.md)
- [Observability-Contract.md](../Standards/Standards.md#observability-contract)
- LevelManager (historical): `../../Modules/Levels/LevelManager.cs` (removed/deprecated)
- LevelPlan (historical): `../../Modules/Levels/LevelPlan.cs` (removed/deprecated)
- [ContentSwap Change Service](../../Modules/ContentSwap/Runtime/InPlaceContentSwapService.cs)
- [LATEST.md](../Reports/Evidence/LATEST.md)

