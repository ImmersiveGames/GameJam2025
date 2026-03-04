# ADR-0022..0027 — Compliance vs Code (P0, read-only audit)

_Data da auditoria:_ 2026-03-04  
_Escopo auditado:_ `Modules/SceneFlow`, `Modules/LevelFlow`, `Modules/Navigation`, `Modules/Gameplay`.

## 1) Decisões dos ADRs (resumo objetivo)

- **ADR-0022 (assinaturas e dedupe por domínio):** separar assinatura macro (`macroSignature`) da assinatura de level (`levelSignature`) e evitar que dedupe macro bloqueie operações locais de level.
- **ADR-0023 (dois resets):** manter `MacroReset` e `LevelReset` como fluxos distintos, com observabilidade explícita por `kind`.
- **ADR-0024 (catálogo por macro + 1 level ativo):** macro route é dono do catálogo; seleção de level é determinística (default/snapshot) e não deve depender de reverse lookup ambíguo.
- **ADR-0025 (loading macro inclui etapa de level):** macro só deve concluir visualmente (FadeOut/Completed) depois de preparação de level.
- **ADR-0026 (troca intra-macro local):** troca de level dentro do mesmo macro deve ocorrer por swap/reset local, sem transição macro.
- **ADR-0027 (Intro/Post como responsabilidade de level):** IntroStage e PostLevel devem ser orquestrados no domínio de level (SceneFlow segue apenas macro).

## 2) Divergências identificadas (com impacto)

## 2.1 Compat legacy ainda acoplando Level↔Macro via fallback global

- **Onde diverge:** `GameNavigationService.StartGameplayRouteAsync` ainda resolve level por fallback em estado global (`snapshot` ou `_lastStartedGameplayLevelId`) para permitir navegação por `routeId` sem seleção explícita do level.  
  - `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/GameNavigationService.cs:292-331`
- **Por que diverge do trilho ADR:** ADR-0024 pede seleção explícita/determinística por macro e hardening; esse fallback mantém um caminho implícito de acoplamento `macroRouteId -> levelId` fora do contrato de seleção ativa.
- **Impacto:**
  - **Runtime:** risco de replay/swap reusar level anterior indevidamente em cenários de estado residual.
  - **Editor/QA:** pode mascarar falhas de configuração de seleção ativa (parece “funcionar” por compat).

## 2.2 API reverse lookup ambígua mantida no contrato principal

- **Onde diverge:** `ILevelFlowService.TryResolveLevelId(SceneRouteId routeId, out LevelId levelId)` permanece no contrato base; implementação marca casos ambíguos e mantém best-effort para compat.
  - Interface: `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Runtime/ILevelFlowService.cs:10-12`
  - Implementação/ambiguidade: `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Bindings/LevelCatalogAsset.cs:87-110`, `:374`
- **Por que diverge do trilho ADR:** ADR-0024 define ownership macro→catálogo e “1 level ativo”; reverse lookup macro→level como API pública principal reforça acoplamento antigo e ambíguo.
- **Impacto:**
  - **Runtime:** possibilidade de resolução não determinística quando múltiplos levels compartilham o mesmo macro.
  - **Editor:** warnings de ambiguidade viram “normalidade” e atrasam remoção do legado.

## 2.3 Superfície pública de navegação legacy ainda exposta

- **Onde diverge:** `IGameNavigationService` mantém métodos obsoletos que operam no eixo route/intent legado (`StartGameplayAsync(LevelId)`, `NavigateAsync(string)`, `RequestGameplayAsync`, `RequestMenuAsync`).
  - `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/IGameNavigationService.cs:31-63`
  - Implementações compat/fail-fast: `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/GameNavigationService.cs:193-210`, `:356-360`
- **Por que diverge do trilho ADR:** ADR-0022/0024/0026 convergem para trilho canônico em `LevelFlowRuntime` + `StartGameplayRouteAsync` (já resolvido), com menos strings/rotas implícitas.
- **Impacto:**
  - **Runtime:** risco de novos chamadores usarem APIs de compat por engano.
  - **Editor/manutenção:** dívida técnica e custo de suporte em testes.

## 2.4 ADR-0026 incompleto por dependência opcional de swap local

- **Onde diverge:** `SwapLevelLocalAsync` depende de `ILevelSwapLocalService`; quando ausente, apenas loga e retorna (sem fallback canônico).
  - `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Runtime/LevelFlowRuntimeService.cs:111-128`
- **Por que diverge do trilho ADR:** ADR-0026 pede trilho runtime robusto para troca intra-macro local; hoje o comportamento é opcional por composição/DI.
- **Impacto:**
  - **Runtime:** feature de troca local pode ficar inoperante por binding faltante.
  - **Editor/QA:** falsos positivos em smoke test se só houver navegação macro.

## 2.5 ADR-0027 parcial: domínio de level cobre IntroStage, mas não fecha PostLevel dedicado

- **Onde diverge:** `LevelStageOrchestrator` orquestra IntroStage (transição completa + swap local), porém o fim de run ainda é conduzido por bridge de PostGame no GameLoop, sem trilha explícita `PostLevel`/`NextLevel` no LevelFlow.
  - Intro no LevelFlow: `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Runtime/LevelStageOrchestrator.cs:15-93`, `:95-137`
  - Fim de run/PostGame no GameLoop: `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/Bindings/Bridges/GameLoopRunEndEventBridge.cs:63-84`
- **Por que diverge do trilho ADR:** ADR-0027 explicita PostLevel como responsabilidade de level; hoje o acoplamento operacional ainda está no fluxo PostGame macro.
- **Impacto:**
  - **Runtime:** ausência de trilho claro para `NextLevel` intra-macro após término de fase.
  - **Editor/QA:** cobertura de casos de progressão por level permanece indireta.

## 3) Pontos já alinhados (sem ação de reversão nesta fase)

- Separação de assinatura macro no SceneFlow (`ContextSignature` com elementos macro de transição).
  - `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Runtime/SceneTransitionEvents.cs:25-57`, `:76-95`
- Preparação de level no pipeline macro antes da conclusão (serviço dedicado de prepare).
  - `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Runtime/LevelMacroPrepareService.cs:42-147`
- Distinção prática de resets macro/level no LevelFlow runtime.
  - `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Runtime/LevelFlowRuntimeService.cs:131-172`

## 4) APIs “a deprecar” (acoplamento indevido Level↔Macro)

1. `ILevelFlowService.TryResolveLevelId(SceneRouteId routeId, out LevelId levelId)`  
   - Motivo: reverse lookup ambíguo macro→level perpetua contrato legado.
2. `IGameNavigationService.StartGameplayAsync(LevelId levelId, string reason = null)`  
   - Motivo: bypass do trilho canônico `ILevelFlowRuntimeService.StartGameplayAsync(string, ...)`.
3. `IGameNavigationService.NavigateAsync(string routeId, string reason = null)`  
   - Motivo: stringly-typed intent/route facilita acoplamento implícito.
4. `IGameNavigationService.RequestGameplayAsync(string reason = null)`  
   - Motivo: gameplay sem seleção explícita de level.
5. `IGameNavigationService.RequestMenuAsync(string reason = null)`  
   - Motivo: alias legado sem valor arquitetural após GoToMenu canônico.
6. Fallback implícito em `GameNavigationService.StartGameplayRouteAsync(...)` por `_lastStartedGameplayLevelId`  
   - Motivo: “corrige” ausência de seleção ativa e oculta problemas de contrato.

## 5) Proposta de correção mínima por fases (sem implementação)

### Fase P1 — Hardening de contratos (baixo risco)

- Marcar reverse lookup macro→level como **compat-only** e remover do caminho canônico (chamadores novos proibidos).
- Adicionar telemetria explícita de uso das APIs obsoletas de `IGameNavigationService` para mapear callsites vivos.
- Subir severidade de logs quando `StartGameplayRouteAsync` precisar de fallback de `_lastStartedGameplayLevelId`.

### Fase P2 — Corte de compat e dedupe estrito por domínio

- Remover fallback implícito em `StartGameplayRouteAsync` (exigir snapshot/seleção válida sempre).
- Encapsular todos os inícios de gameplay no `ILevelFlowRuntimeService.StartGameplayAsync`.
- Garantir que dedupe de macro nunca dependa de estado de level fora de `levelSignature`.

### Fase P3 — Fechamento ADR-0026/0027

- Tornar `ILevelSwapLocalService` obrigatório no composition root de gameplay (ou criar fallback canônico de reset local com falha explícita).
- Introduzir trilha `PostLevel` dedicada no LevelFlow (incluindo ação `NextLevel` intra-macro).
- Promover QA de progressão `N->1` e `NextLevel` sem `SceneTransitionStarted` como gate de release.

---

## Resultado P0

- Relatório de compliance entregue.
- Lista de APIs a deprecar entregue.
- Nenhuma alteração de gameplay implementada nesta fase.
