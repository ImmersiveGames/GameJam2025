# ADR-0017 — LevelManager: Config + Catalog (Single Source of Truth)

> **Status atualizado (runtime):** o fluxo de Levels (LevelManager/LevelCatalog) está **descontinuado** em favor de `LevelFlow` (`LevelCatalogAsset` + `ILevelFlowService`).  
> O bootstrap atual bloqueia a coexistência de dois catálogos para evitar ambiguidade.

## Status

- Estado: Implementado
- Data (decisão): 2026-01-31
- Última atualização: 2026-02-04
- Tipo: Implementação
- Escopo: NewScripts → Modules/Levels + Docs/Reports

## Contexto

O subsistema de Levels já existe em `Assets/_ImmersiveGames/NewScripts/Modules/Levels/` (ILevelManager, LevelManager, LevelPlan, LevelChangeOptions). O ContentSwap é InPlace-only e é o executor técnico real; o LevelManager orquestra a mudança de conteúdo. A etapa de "start" (IntroStage/GameLoop) é tratada por módulos próprios (fora do LevelManager). Porém, **não existe** hoje uma fonte de verdade configurável para níveis (LevelDefinition/LevelCatalog): as intenções aparecem em docs (Baseline 2.2), mas não há assets concretos nem resolver/provedor para evitar hardcode. Essa lacuna conflita com o objetivo de padronização.

Para manter consistência arquitetural (SRP, DIP) e evitar dependências diretas em listas hardcoded, precisamos de uma **configuração centralizada via ScriptableObjects**, consumida pelo LevelManager/QA/resolvedor, com observabilidade alinhada ao contrato canônico.

## Decisão

### Objetivo de produção (sistema ideal)

Centralizar definição e descoberta de levels/fases via um catálogo de configs (LevelManager), evitando hardcode e permitindo evolução (fases, campanhas, playlists) sem quebrar o fluxo de produção.

### Contrato de produção (mínimo)

- Catálogo é a fonte de verdade para enumerar conteúdos jogáveis (ids, metadata, configs).
- Seleção de level/fase não depende de assets soltos; deve ser resolvida via id/config.
- Mudança de level pode ser in-place (ContentSwap) ou via transição (SceneFlow), explicitando o modo.
- Falhas de id/config ausente são fail-fast (não gerar level default silencioso).

### Não-objetivos (resumo)

Ver seção **Fora de escopo**.

## Fora de escopo

- UI/UX completa de seleção de level (apenas contrato e plumbing).

- Refactor total de nomenclaturas legadas no runtime.
- Remoção imediata de bridges legadas (ContentSwapStart*), que permanecem até migração completa.
- Implementar um sistema de campanha completo (Campaign/Progression) além de LevelCatalog.

## Consequências

### Benefícios
- Fonte única de verdade para níveis, eliminando hardcode em runtime.
- Melhora o alinhamento com SRP/DIP e facilita QA/evidências.
- Padroniza progressão de níveis no multiplayer local (determinismo).

### Trade-offs / Riscos
- Requer criação e manutenção de assets (ScriptableObjects) e seus providers/resolvers.
- Migração incremental: enquanto assets não existirem, o LevelManager ainda dependerá de fallback/QA.

### Política de falhas e fallback (fail-fast)

- Em Unity, ausência de referências/configs críticas deve **falhar cedo** (erro claro) para evitar estados inválidos.
- Evitar "auto-criação em voo" (instanciar prefabs/serviços silenciosamente) em produção.
- Exceções: apenas quando houver **config explícita** de modo degradado (ex.: HUD desabilitado) e com log âncora indicando modo degradado.


### Critérios de pronto (DoD)

- Existe API estável para: listar levels, resolver id→config, aplicar seleção.
- Evidência/logs para pelo menos um caminho (QA ou produção) demonstrando resolução do catálogo.

## Implementação (arquivos impactados)

### Runtime / Editor (código e assets)

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

### Docs / evidências relacionadas

- `Docs/Reports/Evidence/LATEST.md`
- `Docs/Overview/Overview.md`
- `Docs/Standards/Standards.md`

## Notas de implementação

- Assets sugeridos (paths):
  - `Assets/_ImmersiveGames/NewScripts/Modules/Levels/Definitions/LevelDefinition.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/Levels/Catalogs/LevelCatalog.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/Levels/Providers/ILevelDefinitionProvider.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/Levels/Providers/ILevelCatalogProvider.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/Levels/Resolvers/LevelCatalogResolver.cs`
- LevelManager deve consumir **apenas** abstrações (providers/resolvers), mantendo DIP.
- Observabilidade deve reutilizar strings existentes no contrato (não criar reasons novos).

### Etapa 0 — Artefatos entregues (concluída)
- ScriptableObjects `LevelDefinition` e `LevelCatalog` com campos mínimos.
- Providers: `ResourcesLevelCatalogProvider` + `LevelDefinitionProviderFromCatalog`.
- Resolver: `ILevelCatalogResolver` + `LevelCatalogResolver`.

### Etapa 1 — concluída
- DI: registro de providers/resolver + `ILevelManager` via `LevelManager` no `GlobalCompositionRoot`.
- QA: `LevelDevContextMenu` e `LevelDevInstaller`.
- Evidência: atualização de Docs/Reports/Evidence/LATEST.md e snapshot dedicado com QA de Level.

## Evidência

- **Última evidência (log bruto):** `Docs/Reports/lastlog.log`

- **Fonte canônica atual:** [LATEST.md](../Reports/Evidence/LATEST.md)
- **Snapshot dedicado (ADR-0017):** [ADR-0017-LevelCatalog-Evidence-2026-02-03.md](../Reports/Evidence/2026-02-03/ADR-0017-LevelCatalog-Evidence-2026-02-03.md)
- **Contrato de observabilidade:** [Observability-Contract.md](../Standards/Standards.md#observability-contract)


- Snapshot datado em `Docs/Reports/Evidence/<YYYY-MM-DD>/` com:
  - Resolução por catálogo (`QA/Levels/Resolve/Definitions`).
  - Mudança de nível (LevelChange + ContentSwap + IntroStage).
- Atualização do `Docs/Reports/Evidence/LATEST.md`.

## Referências

- [README.md](../README.md)
- [Docs/Overview/Overview.md](../Docs/Overview/Overview.md)
- [Observability-Contract.md](../Standards/Standards.md#observability-contract)
- [LevelManager](../../Modules/Levels/LevelManager.cs)
- [LevelPlan](../../Modules/Levels/LevelPlan.cs)
- [ContentSwap Change Service](../../Modules/ContentSwap/Runtime/InPlaceContentSwapService.cs)
- [LATEST.md](../Reports/Evidence/LATEST.md)
