# ADR-0017 — LevelManager: Config + Catalog (Single Source of Truth)

> **Status atualizado (runtime):** o fluxo de Levels (LevelManager/LevelCatalog) está **descontinuado** em favor de `LevelFlow` (`LevelCatalogAsset` + `ILevelFlowService`).  
> O bootstrap atual bloqueia a coexistência de dois catálogos para evitar ambiguidade.

## Status

- Estado: Implementado
- Data (decisão): 2026-01-31
- Última atualização: 2026-03-23
- Tipo: Implementação
- Escopo: NewScripts → Modules/Levels + Docs/Reports

## Contexto

O subsistema legado de Levels já existiu em `Assets/_ImmersiveGames/NewScripts/Modules/Levels/` (ILevelManager, LevelManager, LevelPlan, LevelChangeOptions), mas o trilho canônico atual está em `LevelFlow`. A composição técnica local agora passa por `SceneComposition`, enquanto `LevelFlow` orquestra a semântica da mudança de conteúdo. A etapa de "start" (IntroStage/GameLoop) é tratada por módulos próprios (fora do LevelFlow). O problema estrutural original deste ADR permanece válido: é necessária uma fonte de verdade configurável para níveis (LevelDefinition/LevelCatalog) para evitar hardcode.

Para manter consistência arquitetural (SRP, DIP) e evitar dependências diretas em listas hardcoded, precisamos de uma **configuração centralizada via ScriptableObjects**, consumida pelo LevelManager/QA/resolvedor, com observabilidade alinhada ao contrato canônico.

## Decisão

### Objetivo de produção (sistema ideal)

Centralizar definição e descoberta de levels/fases via um catálogo de configs (LevelManager), evitando hardcode e permitindo evolução (fases, campanhas, playlists) sem quebrar o fluxo de produção.

### Contrato de produção (mínimo)

- Catálogo é a fonte de verdade para enumerar conteúdos jogáveis (ids, metadata, configs).
- Seleção de level/fase não depende de assets soltos; deve ser resolvida via id/config.
- Mudança de level pode usar composição local (`SceneComposition`) ou transição macro (`SceneFlow`), explicitando o modo.
- Falhas de id/config ausente são fail-fast (não gerar level default silencioso).

### Não-objetivos (resumo)

Ver seção **Fora de escopo**.

## Fora de escopo

- UI/UX completa de seleção de level (apenas contrato e plumbing).

- Refactor total de nomenclaturas legadas no runtime.
- Remoção imediata de bridges legadas históricas, quando ainda existirem fora do trilho canônico.
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

- **Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow**
  - `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Config/LevelDefinitionAsset.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Config/LevelCollectionAsset.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Runtime/GameplayStartSnapshot.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Runtime/RestartContextService.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Runtime/LevelMacroPrepareService.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Runtime/LevelSwapLocalService.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Runtime/LevelSceneCompositionRequestFactory.cs`
- **Infrastructure**
  - `Assets/_ImmersiveGames/NewScripts/Infrastructure/SceneComposition/**`
- **Histórico**
  - `Assets/_ImmersiveGames/NewScripts/Modules/Levels/**` e `LevelManager/LevelPlan` permanecem apenas como referência histórica, fora do trilho canônico atual.

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
- `LevelDefinitionAsset` e `LevelCollectionAsset` como fonte de verdade configurável.
- `GameplayStartSnapshot` e `RestartContextService` para o snapshot semântico.
- `LevelSceneCompositionRequestFactory` como ponte entre semântica de level e composição técnica local.

### Etapa 1 — concluída
- `LevelMacroPrepareService` e `LevelSwapLocalService` integrados ao trilho canônico atual.
- `LevelSelectedRestartSnapshotBridge` atualiza o snapshot a partir da seleção de level.
- Evidência: `Docs/Reports/Evidence/LATEST.md` e `Docs/Reports/Audits/LATEST.md`.

## Evidência

- **Última evidência (log bruto):** `Docs/Reports/Evidence/LATEST.md`

- **Fonte canônica atual:** [LATEST.md](../Reports/Evidence/LATEST.md)
- **Snapshot dedicado (ADR-0017):** [ADR-0017-LevelCatalog-Evidence-2026-02-03.md](../Reports/Evidence/2026-02-03/ADR-0017-LevelCatalog-Evidence-2026-02-03.md)
- **Contrato de observabilidade:** [Observability-Contract.md](../Standards/Standards.md#observability-contract)


- Snapshot datado em `Docs/Reports/Evidence/<YYYY-MM-DD>/` com:
  - Resolução por catálogo (`QA/Levels/Resolve/Definitions`).
  - Mudança de nível (LevelFlow + SceneComposition + IntroStage).
- Atualização do `Docs/Reports/Evidence/LATEST.md`.

## Referências

- [README.md](../README.md)
- [Docs/Overview/Overview.md](../Overview/Overview.md)
- [Observability-Contract.md](../Standards/Standards.md#observability-contract)
- LevelManager (historical): `../../Modules/Levels/LevelManager.cs` (removed/deprecated)
- LevelPlan (historical): `../../Modules/Levels/LevelPlan.cs` (removed/deprecated)
- Referência operacional atual: `../Modules/LevelFlow.md`, `../Modules/SceneFlow.md` e `../Modules/ResetInterop.md`.
- [LATEST.md](../Reports/Evidence/LATEST.md)

