# ADR-0017 — LevelManager: Config + Catalog (Single Source of Truth)

## Status

- Estado: Em andamento
- Data (decisão): 2026-01-31
- Última atualização: 2026-01-31
- Escopo: NewScripts → Gameplay/Levels + Docs/Reports

## Contexto

O subsistema de Levels já existe em `Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/` (ILevelManager, LevelManager, LevelPlan, LevelChangeOptions, LevelStartPipeline, LevelStartCommitBridge). O ContentSwap é InPlace-only e é o executor técnico real, enquanto o LevelManager orquestra a progressão e dispara IntroStage pós-commit. Porém, **não existe** hoje uma fonte de verdade configurável para níveis (LevelDefinition/LevelCatalog): as intenções aparecem em docs (Baseline 2.2), mas não há assets concretos nem resolver/provedor para evitar hardcode. Essa lacuna conflita com o objetivo de padronização e com o gate G-03 do ADR-0019.

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

## Notas de implementação

- Assets sugeridos (paths):
  - `Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/Definitions/LevelDefinition.cs`
  - `Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/Catalogs/LevelCatalog.cs`
  - `Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/Providers/ILevelDefinitionProvider.cs`
  - `Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/Providers/ILevelCatalogProvider.cs`
  - `Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/Resolvers/LevelCatalogResolver.cs`
- LevelManager deve consumir **apenas** abstrações (providers/resolvers), mantendo DIP.
- Observabilidade deve reutilizar strings existentes no contrato (não criar reasons novos).

### Etapa 0 — Artefatos entregues (concluída)
- ScriptableObjects `LevelDefinition` e `LevelCatalog` com campos mínimos.
- Providers: `ResourcesLevelCatalogProvider` + `LevelDefinitionProviderFromCatalog`.
- Resolver: `ILevelCatalogResolver` + `LevelCatalogResolver`.

### Etapa 1 (planejada)
- DI: registro de providers/resolver + `ILevelManager` via `LevelManager` no `GlobalBootstrap`.
- QA: `LevelQaContextMenu` e `LevelQaInstaller`.
- Evidência: atualização de `Docs/Reports/Evidence/LATEST.md` e snapshot dedicado com QA de Level.

## Evidência

- **Fonte canônica atual:** [`LATEST.md`](../Reports/Evidence/LATEST.md)
- **Âncoras/assinaturas relevantes:**
  - TODO: adicionar evidência/log âncora do catálogo de levels (não aparece na evidência canônica atual).
- **Contrato de observabilidade:** [`Observability-Contract.md`](../Standards/Standards.md#observability-contract)

## Evidências

- Snapshot datado em `Docs/Reports/Evidence/<YYYY-MM-DD>/` com:
  - Resolução por catálogo (`QA/Levels/Resolve/Definitions`).
  - Mudança de nível (LevelChange + ContentSwap + IntroStage).
- Atualização do `Docs/Reports/Evidence/LATEST.md`.

## Referências

- [README.md](../README.md)
- [Overview/Overview.md](../Overview/Overview.md)
- [Observability-Contract.md](../Standards/Standards.md#observability-contract)
- [ADR-0018](./ADR-0018-Gate-de-Promocao-Baseline2.2.md)
- [ADR-0019](./ADR-0019-Promocao-Baseline2.2.md)
- [LevelManager](../../Gameplay/Levels/LevelManager.cs)
- [LevelPlan](../../Gameplay/Levels/LevelPlan.cs)
- [LevelStartPipeline](../../Gameplay/Levels/LevelStartPipeline.cs)
- [ContentSwap Change Service](../../Gameplay/ContentSwap/ContentSwapChangeServiceInPlaceOnly.cs)
- [`Observability-Contract.md`](../Standards/Standards.md#observability-contract)
- [`Evidence/LATEST.md`](../Reports/Evidence/LATEST.md)
