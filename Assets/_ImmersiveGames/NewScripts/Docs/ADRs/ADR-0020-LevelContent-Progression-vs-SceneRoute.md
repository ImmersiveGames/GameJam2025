# ADR-0020 — Separar LevelContent/Progression de SceneRoute/Scene Data

## Status

- Estado: Aberto
- Data (decisão): 2026-02-18
- Última atualização: 2026-02-18
- Tipo: Implementação
- Escopo: LevelFlow, Navigation, ContentSwap, Gameplay/Spawn, Editor Validation
- Decisores: NewScripts Architecture + Gameplay/Tech
- Tags: LevelFlow, SceneFlow, Navigation, Content, SOLID, Observability, FailFast

> Este ADR é baseado em auditoria read-only (Assets/_ImmersiveGames/NewScripts/**).
> Evidências principais: Level é alias de Route via bijeção 1:1 + restart depende de LevelId.

## Contexto

No runtime atual, "Level" está funcionalmente acoplado à "Route":

- `LevelDefinition` resolve `routeRef/routeId` e não carrega conteúdo próprio (`ToPayload() => Empty`).
- `LevelCatalogAsset` mantém cache direto e reverso (`LevelId -> RouteId` e `RouteId -> LevelId`) e rejeita duplicidade, impondo bijeção 1:1.
- `GameNavigationService.StartGameplayRouteAsync` exige `RouteId -> LevelId`; se não mapear, falha com `[FATAL][Config]`.
- `RestartAsync` depende de `_lastStartedGameplayLevelId`; restart sem “último level” falha.

Em paralelo, conteúdo jogável está em outros eixos:
- Spawn por cena via `WorldDefinition.spawnEntries` + `SceneScopeCompositionRoot`.
- Troca de conteúdo em runtime via `ContentSwap` (contentId) sem vínculo canônico com LevelId/Progressão.

Leitura consolidada: o Level virou alias de Route para navegação, enquanto conteúdo/progressão ficou disperso.

## Decisão

### Objetivo de produção (sistema ideal)

Separar responsabilidades de forma explícita:

1) **SceneRoute/SceneFlow**: único dono de Scene Data (load/unload/active + reset policy).
2) **LevelContent/Progression**: único dono de seleção/progressão e perfil de conteúdo (quantidades/regras/spawn profiles/etc.).
3) **Navigation**: orquestra intents/transições sem depender de bijeção rígida `Level↔Route` para operar.

### Contrato de produção (mínimo)

#### A) Level = (seleção/progressão + conteúdo) e referencia uma Route

- `LevelId` permanece como chave de progressão/telemetria/logs (ID tipado).
- Um Level deve apontar para:
    - `routeRef` (Scene Data)
    - `contentRef` (Content Profile)
- Um mesmo `routeRef` pode ser compartilhado por múltiplos `LevelId` (N→1 permitido).

#### B) Evento canônico (event-driven)

Definir evento global canônico para seleção:

- `LevelSelectedEvent`
    - `levelId`
    - `routeId` (derivado do `routeRef`, para observabilidade/correlação)
    - `contentId` (derivado do `contentRef`)
    - `reason`
    - `selectionVersion` (monótono, para dedupe)
    - `contextSignature` (quando relevante para trilhas de reset/scene flow)

Publicação mínima:
- UI (MenuPlay) e/ou trilho canônico `LevelFlowRuntimeService.StartGameplayAsync(levelId, reason)`.

Consumo mínimo:
- Navigation (para iniciar gameplay com contexto completo)
- Content/Gameplay (para aplicar conteúdo e logar evidência)
- Opcional: ContentSwap (modo compat ou integração futura)

#### C) Restart não pode depender de bijeção Route→Level

Definir `IRestartContextService` com snapshot canônico:

- `GameplayStartSnapshot { levelId? , routeId, styleId, contentId?, selectionVersion, reason }`

Regras:
- Start gameplay atualiza snapshot.
- Restart usa snapshot (não exige reverse lookup `RouteId -> LevelId`).

#### D) Fail-fast (Strict) e migração Editor-only

- Editor-only: auto-migração e validações ajudam a preencher `contentRef`/`routeRef` e reserializar assets.
- Runtime strict: ausência de `routeRef` ou `contentRef` na hora de iniciar gameplay => `[FATAL][Config]` + abort.

### Não-objetivos (resumo)

- Não redesenhar SceneFlow/Route (já é source-of-truth de Scene Data).
- Não implementar sistema completo de difficulty/waves agora; este ADR define o ponto canônico de extensão.
- Não remover todo legado em um único PR (migração incremental).

## Fora de escopo

- Rebalanceamento do jogo.
- Ferramentas avançadas de authoring de conteúdo.
- Migration “big bang”.

## Consequências

### Benefícios

- Remove a ambiguidade “Level = Route”.
- Permite progressão e variação de conteúdo sem proliferar rotas.
- Isola responsabilidades (SceneFlow vs Gameplay) e reduz acoplamento.

### Custos / Riscos

- Coexistência temporária (contrato novo + compat) aumenta complexidade.
- Restart é ponto de alto risco (hoje depende de LevelId).
- Migração de assets exige disciplina (Editor-only + reserialize).

### Política de falhas e fallback (fail-fast)

- Em produção: falhar cedo para bugs de pipeline/config.
- Fallback controlado apenas durante migração via feature flag (ex.: `LevelRouteLegacyBridgeEnabled`), com logs `[OBS][Level][Compat]`.

### Critérios de pronto (DoD)

- [ ] `LevelSelectedEvent` publicado e consumido com logs âncora.
- [ ] Start Gameplay não depende de bijeção rígida Route→Level.
- [ ] Restart funciona com snapshot canônico (sem reverse lookup obrigatório).
- [ ] `LevelCatalogAsset` não impõe 1:1 Level↔Route (N→1 permitido).
- [ ] Migração Editor-only cria/associa `contentRef` default explícito.
- [ ] Baseline: menu->play, post-game restart, exit to menu continuam OK.
- [ ] Evidências/âncoras estáveis e documentadas.

## Implementação (arquivos impactados)

> Esta seção lista superfícies impactadas inferidas pela auditoria. A execução será incremental.

### P0 — Contrato mínimo + observabilidade (sem quebrar baseline)

- Criar DTO/evento: `LevelSelectedEvent`, `GameplayStartSnapshot`, `IRestartContextService`.
- Publicar logs âncora (sem alterar semântica de spawn ainda).

Superfícies típicas:
- `Modules/LevelFlow/Runtime/LevelFlowRuntimeService.cs`
- `Modules/Navigation/GameNavigationService.cs`
- `Modules/Navigation/RestartNavigationBridge.cs`
- `MenuPlayButtonBinder.cs` (origem do StartGameplayLevelId)
- `Docs/Standards` (se houver contrato de observabilidade)

### P1 — Restart e StartGameplay desacoplados de Route→Level

- `StartGameplayRouteAsync` deve aceitar contexto do snapshot (não exigir reverse lookup).
- `_lastStartedGameplayLevelId` passa a ser substituído por snapshot.

### P2 — Level passa a conter conteúdo (LevelContentDefinitionAsset)

- Introduzir `LevelContentDefinitionAsset` e `contentRef` em `LevelDefinition`.
- `LevelCatalogAsset` passa a cachear `LevelId -> LevelDefinition` (não só route).

### P3 — Remoção progressiva da bijeção e de APIs legadas

- Remover/rebaixar `TryResolveLevelId(route)` como requisito hard.
- Ajustar contratos e consumers.

## Notas de implementação (se necessário)

- Manter rollout incremental por feature flag para preservar baseline durante a transição.
- Priorizar compatibilidade de assets em migrações editor-only com reserialize controlado.
- Preservar âncoras de observabilidade existentes e introduzir as novas sem quebra de parsing em QA.

## Evidência

Âncoras atuais relevantes (existentes hoje):
- `[OBS][LevelFlow] StartGameplayRequested ...`
- `[OBS][Navigation] LastLevelIdUpdated ...`
- `[OBS][SceneFlow] RouteResolvedVia=...`
- `[OBS][ContentSwap] ContentSwapRequested ...`

Âncoras novas propostas (ADR-0020):
- `[OBS][Level] LevelSelected levelId='...' routeId='...' contentId='...' reason='...' v='...'`
- `[OBS][Level] LevelSelectionApplied ...`
- `[OBS][Navigation] RestartUsingSnapshot ...`
- `[OBS][Gameplay] LevelContentApplied ...`

## Referências

- ADR-0016 / ADR-0017 / ADR-0019
- Plano SceneFlow/Navigation/LevelFlow (Plan-v2)
- Docs/Standards/Standards.md#observability-contract
