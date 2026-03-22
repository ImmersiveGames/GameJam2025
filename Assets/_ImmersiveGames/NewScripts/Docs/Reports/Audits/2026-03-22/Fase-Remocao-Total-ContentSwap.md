# Fase — Remoção total do `ContentSwap`

## Resumo
O `ContentSwap` deve ser removido integralmente do projeto.

Motivo:
- o trilho local já está canônico com `LevelFlow -> WorldLifecycle -> SceneComposition`;
- `ContentSwap` ficou como trilho paralelo, sem ownership semântico nem execução técnica canônica;
- manter o módulo só prolonga redundância de DI, eventos, reason/contentId e documentação.

---

## Problema
O projeto ainda mantém um módulo `ContentSwap` que:
- é registrado globalmente no boot;
- possui contratos próprios (`IContentSwapChangeService`, `IContentSwapContextService`);
- publica eventos próprios (`ContentSwapPending*`, `ContentSwapCommittedEvent`);
- é referenciado por `WorldResetCommands` no reset local;
- mas **não** é mais o executor técnico real da composição local.

Hoje a composição local já acontece via `ISceneCompositionExecutor` / `LevelSceneCompositionExecutor`.

---

## Causa
O módulo foi criado em uma fase anterior como trilho in-place de conteúdo.

Com a entrada de `SceneComposition`, o papel técnico canônico mudou, mas o `ContentSwap` permaneceu:
- no DI global;
- no `WorldResetCommands.ResetLevelAsync`;
- no EventBus;
- em ADRs e índices documentais.

---

## Decisão
Remover o `ContentSwap` por completo.

### Fonte de verdade depois da remoção
- **Semântica local:** `LevelFlow`
- **Snapshot semântico:** `GameplayStartSnapshot` + `RestartContextService`
- **Reset:** `WorldLifecycle`
- **Execução técnica local:** `SceneComposition`

### O que NÃO permanece
- nenhum contrato `IContentSwap*`;
- nenhum evento `ContentSwap*`;
- nenhum registro global do módulo;
- nenhum `contentId='level-ref:...'` paralelo fora do snapshot semântico.

---

## Escopo

### Arquivos a editar

#### 1. Cortar dependência funcional
- `NewScripts/Modules/WorldLifecycle/Runtime/WorldResetCommands.cs`
  - remover `using _ImmersiveGames.NewScripts.Modules.ContentSwap.Runtime;`
  - remover resolução de `IContentSwapChangeService`
  - remover `canonicalContentToken = $"level-ref:{levelRef.name}"`
  - remover chamada `RequestContentSwapInPlaceAsync(...)`
  - manter `PublishRequested(...)` / `PublishCompleted(...)`
  - `ResetLevelAsync(...)` deve virar um comando de reset puro, sem swap paralelo

#### 2. Cortar bootstrap / DI
- `NewScripts/Infrastructure/Composition/GlobalCompositionRoot.ContentLevels.cs`
  - remover `RegisterContentSwapChangeService()`
  - remover `using` do namespace de `ContentSwap`
  - remover `InstallContentSwapServices()`

- `NewScripts/Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs`
  - remover `using _ImmersiveGames.NewScripts.Modules.ContentSwap.Runtime;`
  - remover `CompositionInstallStage.ContentSwap`
  - remover `_compositionInstallStage = CompositionInstallStage.ContentSwap;`
  - remover `InstallContentSwapServices();`

- `NewScripts/Infrastructure/Composition/GlobalCompositionRoot.Events.cs`
  - remover `using _ImmersiveGames.NewScripts.Modules.ContentSwap.Runtime;`
  - remover:
    - `EventBus<ContentSwapCommittedEvent>.Clear();`
    - `EventBus<ContentSwapPendingSetEvent>.Clear();`
    - `EventBus<ContentSwapPendingClearedEvent>.Clear();`

#### 3. Limpar tokens órfãos
- `NewScripts/Modules/Gates/SimulationGateTokens.cs`
  - remover:
    - `ContentSwapTransition`
    - `ContentSwapInPlace`
  - validar se nenhum outro código depende desses tokens antes de apagar

#### 4. Limpeza documental mínima
- `NewScripts/Docs/ADRs/ADR-0016-ContentSwap-WorldLifecycle.md`
  - marcar como **Superseded** ou remover do trilho canônico
- `NewScripts/Docs/ADRs/README.md`
  - remover a linha do ADR-0016 ou marcar como obsoleto

### Arquivos a remover

#### Módulo inteiro
- `NewScripts/Modules/ContentSwap/Runtime/ContentSwapContextService.cs`
- `NewScripts/Modules/ContentSwap/Runtime/ContentSwapEvents.cs`
- `NewScripts/Modules/ContentSwap/Runtime/ContentSwapMode.cs`
- `NewScripts/Modules/ContentSwap/Runtime/ContentSwapOptions.cs`
- `NewScripts/Modules/ContentSwap/Runtime/ContentSwapPlan.cs`
- `NewScripts/Modules/ContentSwap/Runtime/IContentSwapChangeService.cs`
- `NewScripts/Modules/ContentSwap/Runtime/IContentSwapContextService.cs`
- `NewScripts/Modules/ContentSwap/Runtime/InPlaceContentSwapService.cs`
- `NewScripts/Modules/ContentSwap/CONTENTSWAP_ANALYSIS_REPORT.md`

#### Opcional na mesma mudança se existir no seu estado atual
- `.meta` correspondentes
- diretórios vazios de `Modules/ContentSwap/**`

---

## Ordem segura de execução

### Etapa 1 — cortar referência funcional
Aplicar primeiro em `WorldResetCommands.cs`.

Objetivo:
- fazer o reset local deixar de depender do `ContentSwap` antes de apagar o módulo.

### Etapa 2 — cortar boot e EventBus
Aplicar em:
- `GlobalCompositionRoot.ContentLevels.cs`
- `GlobalCompositionRoot.Pipeline.cs`
- `GlobalCompositionRoot.Events.cs`

Objetivo:
- impedir registro e limpeza de tipos que vão sumir.

### Etapa 3 — limpar tokens
Aplicar em `SimulationGateTokens.cs`.

Objetivo:
- evitar infra morta por trás de um módulo já removido.

### Etapa 4 — apagar o módulo físico
Remover `Modules/ContentSwap/**`.

### Etapa 5 — limpar docs
Remover/superseder ADR-0016 e qualquer índice canônico que trate `ContentSwap` como módulo ativo.

---

## Critérios de aceite
A remoção só termina quando estas condições forem verdadeiras:

- não existe mais `IContentSwapChangeService` no projeto;
- não existe mais `IContentSwapContextService` no projeto;
- não existe mais `ContentSwapCommittedEvent`, `ContentSwapPendingSetEvent` ou `ContentSwapPendingClearedEvent` no projeto;
- o boot não registra mais `ContentSwap`;
- o EventBus não limpa mais tipos do `ContentSwap`;
- o log não mostra mais anchors `[ContentSwap]`;
- o fluxo continua íntegro em:
  - boot → menu → gameplay
  - restart
  - exit to menu
- a composição local continua passando por `SceneComposition`.

---

## Riscos de regressão

### Baixo
- limpeza de DI/eventos/documentação.

### Médio
- `WorldResetCommands.ResetLevelAsync(...)` se ainda existir alguma expectativa implícita de side effect do `ContentSwap`.

### Observação
Pelo estado atual validado em runtime, esse risco é aceitável, porque a composição local já está no trilho de `SceneComposition`.

---

## Observabilidade esperada após remoção

### Deve permanecer
- logs de `LevelSelectedEventConsumed`
- logs de `GameplayStartSnapshotUpdated`
- logs de `ResetRequestedV2` / `ResetCompletedV2`
- logs de `SceneComposition`:
  - `LocalCompositionApplied`
  - `LocalCompositionCleared`

### Deve desaparecer
- qualquer anchor `[OBS][ContentSwap]`
- qualquer referência a `content_swap_inplace`
- qualquer log com `IContentSwapChangeService`

---

## Próxima fase depois da remoção
Depois que o `ContentSwap` sair, a discussão passa a ser só:
- convergência do trilho macro para a mesma capability técnica;
- e limpeza residual de documentação/índices antigos.
