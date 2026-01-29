# ADR-0017 — LevelManager: Config + Catalog (Single Source of Truth)

## Status
- Estado: Em andamento (Etapa 0 concluída; evidência pendente)
- Data: 2026-01-29
- Escopo: NewScripts → Gameplay/Levels + Docs/Reports

## Contexto
O subsistema de Levels já existe em `Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/` (ILevelManager, LevelManager, LevelPlan, LevelChangeOptions, LevelStartPipeline, LevelStartCommitBridge). O ContentSwap é InPlace-only e é o executor técnico real, enquanto o LevelManager orquestra a progressão e dispara IntroStage pós-commit. Porém, **não existe** hoje uma fonte de verdade configurável para níveis (LevelDefinition/LevelCatalog): as intenções aparecem em docs (Baseline 2.2), mas não há assets concretos nem resolver/provedor para evitar hardcode. Essa lacuna conflita com o objetivo de padronização e com o gate G-03 do ADR-0019.

Para manter consistência arquitetural (SRP, DIP) e evitar dependências diretas em listas hardcoded, precisamos de uma **configuração centralizada via ScriptableObjects**, consumida pelo LevelManager/QA/resolvedor, com observabilidade alinhada ao contrato canônico.

## Decisão
1) **Single Source of Truth via ScriptableObjects**
   - Introduzir assets configuráveis que definem níveis e seu catálogo, evitando hardcode em runtime.
   - A semântica permanece: LevelManager orquestra progressão e delega ContentSwap; IntroStage ocorre pós-commit. Referência: `LevelManager` + `LevelPlan` + `LevelStartPipeline`. 

2) **Artefatos concretos (ScriptableObject)**
   - `LevelDefinition` (ScriptableObject):
     - Campos mínimos: `levelId`, `contentId`, `contentSignature` (opcional), `defaultOptions` (LevelChangeOptions → ContentSwapOptions), `notes`.
     - Motivo: encapsular a definição de um nível e suas opções default, garantindo SRP (definição ≠ execução).
   - `LevelCatalog` (ScriptableObject):
     - Campos mínimos: `initialLevelId`, `orderedLevels` (lista de ids em ordem), `nextById` (mapa explicitamente configurável), lookup por id.
     - Motivo: centralizar progressão e resolver o próximo nível de maneira determinística.

3) **Provider/Resolver para o LevelManager (DIP)**
   - Criar interfaces mínimas e um resolver para desacoplar LevelManager de assets concretos:
     - `ILevelDefinitionProvider` (ex.: `TryGetDefinition(levelId, out LevelDefinition def)`), localizado em `Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/Providers/`.
     - `ILevelCatalogProvider` (ex.: `GetCatalog()`), localizado em `Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/Providers/`.
     - `LevelCatalogResolver` (ex.: `ResolveInitial()`, `ResolveNext(levelId)`), localizado em `Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/Resolvers/`.
   - **Por quê:** SRP (separar resolução de nível da execução), DIP (LevelManager depende de abstrações, não de assets), e testabilidade.

4) **Observabilidade (alinhada ao contrato canônico)**
   - Logs e reasons devem respeitar `Docs/Reports/Observability-Contract.md`.
   - Eventos/logs esperados (sem criar novos formatos de reason):
     - Level: `[OBS][Level] LevelChangeRequested/Started/Completed` com `reason` fornecido pelo caller (prefixos `LevelChange/<source>` ou `QA/Levels/InPlace/<...>`).
     - ContentSwap: logs canônicos já existentes em ContentSwap InPlace-only.
     - IntroStage: `IntroStage/UIConfirm` e `IntroStage/NoContent`.
   - O ADR reforça que **Reason-Map é histórico/deprecated** e a fonte de verdade é o Observability-Contract.

5) **QA mínimo e critérios de aceite (Baseline 2.2)**
   - QA mínimo (ContextMenu) permanece alinhado ao plano existente:
     - `QA/Levels/L01-GoToLevel (InPlace + IntroStage)`
     - `QA/Levels/Resolve/Definitions`
   - Critérios de aceite:
     - Evidência com snapshot datado em `Docs/Reports/Evidence/<YYYY-MM-DD>/`.
     - Atualização de `Docs/Reports/Evidence/LATEST.md` apontando para o snapshot.
     - Logs demonstram resolução por catálogo + assinatura de conteúdo (G-03 do ADR-0019).

## Fora de escopo
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
- DI: registro de providers/resolver + `ILevelManager` via `LevelManager` quando possível.
- QA: `LevelQaContextMenu` com actions `QA/Levels/L01-GoToLevel` e `QA/Levels/Resolve/Definitions`.
- Evidência: execução/QA ainda pendente (não há snapshot dedicado a Level).

## Evidências
- Snapshot datado em `Docs/Reports/Evidence/<YYYY-MM-DD>/` com:
  - Resolução por catálogo (`QA/Levels/Resolve/Definitions`).
  - Mudança de nível (LevelChange + ContentSwap + IntroStage).
- Atualização do `Docs/Reports/Evidence/LATEST.md`.

## Referências
- [README.md](../README.md)
- [ARCHITECTURE.md](../ARCHITECTURE.md)
- [Observability-Contract.md](../Reports/Observability-Contract.md)
- [ADR-0018](./ADR-0018-Gate-de-Promoção-Baseline2.2.md)
- [ADR-0019](./ADR-0019-Promocao-Baseline2.2.md)
- [LevelManager](../../Gameplay/Levels/LevelManager.cs)
- [LevelPlan](../../Gameplay/Levels/LevelPlan.cs)
- [LevelStartPipeline](../../Gameplay/Levels/LevelStartPipeline.cs)
- [ContentSwap Change Service](../../Gameplay/ContentSwap/ContentSwapChangeServiceInPlaceOnly.cs)
