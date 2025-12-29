# Decisões e ADRs (resumo)

Este documento consolida as decisões arquiteturais relevantes ao NewScripts.

## Convenções
- Cada decisão possui: **ID**, **Status**, **Decisão**, **Motivação** e **Consequências**.
- ADRs completos podem existir na pasta [ADRs](ADRs/) do repositório.

---

## ADR-0001 — Migração do legado (bridges temporários)
**Status:** Ativo (migração incremental)

**Decisão:** manter “bridges” para conectar sistemas legados ao pipeline NewScripts durante a transição, minimizando reescrita e permitindo validar infraestrutura antes do gameplay completo.

**Motivação:** reduzir risco e permitir evolução por commits pequenos, mantendo o jogo executável.

**Consequências:**
- haverá código de adaptação (ex.: loader fallback),
- logs devem deixar explícito quando o fluxo está em “fallback”,
- bridges são temporárias e devem ser removidas quando serviços nativos NewScripts estiverem prontos.

---

## ADR-000X — Ciclo de Vida do Jogo e Reset por Escopos

Referência: [ADR-0013-Ciclo-de-Vida-Jogo.md](ADRs/ADR-0013-Ciclo-de-Vida-Jogo.md).

## ADR-0014 — Gameplay Reset Targets/Grupos (fases Cleanup/Restore/Rebind)

Referência: [ADR-0014-GameplayReset-Targets-Grupos.md](ADRs/ADR-0014-GameplayReset-Targets-Grupos.md).

**Motivação:** Enquanto o spawn ainda está sendo consolidado, precisamos de um mecanismo **testável** e local ao escopo da cena para validar “reset de gameplay” (ex.: players) sem depender do pipeline completo de spawn.

Decisões:
- O reset de gameplay fica em **Gameplay** (`Gameplay/Reset/`), não em infra, para evitar que o WorldLifecycle assuma regras de componentes de gameplay.
- Usamos **`GameplayResetTarget`** (e não “Scope”) para evitar conflito semântico com `ResetScope` do WorldLifecycle.
- O pipeline de gameplay tem 3 fases fixas: **`Cleanup → Restore → Rebind`** (`GameplayResetPhase`), com ordenação opcional via `IGameplayResetOrder`.
- A integração com WorldLifecycle é feita por **bridges** via `IResetScopeParticipant` (ex.: `PlayersResetParticipant` → `IGameplayResetOrchestrator`).

Renames (consolidação de nomes no reset de gameplay):
| Antes | Depois | Motivo |
|---|---|---|
| `IGameplayResetParticipant` | `IGameplayResettable` | “Participant” conflita com `IResetScopeParticipant` do WorldLifecycle; “Resettable” é mais direto para componentes. |
| `GameplayResetScope` | `GameplayResetTarget` | Evitar colisão conceitual com `ResetScope` (WorldLifecycle). |
| `Reset_CleanupAsync / Reset_RestoreAsync / Reset_RebindAsync` (em componentes) | `ResetCleanupAsync / ResetRestoreAsync / ResetRebindAsync` | Normalização para remover underscores e alinhar com o contrato de interface. |

**Status:** Ativo (base operacional)

**Decisão:** reset determinístico por escopos e fases, com hooks e participantes registrados por cena.

**Motivação:** permitir resets previsíveis e testáveis, evitando “estado invisível” residual.

**Consequências:**
- cenas NewScripts devem registrar registries (actor/spawn/hooks/participants),
- reset deve ser acionado de forma centralizada (driver/orchestrator),
- gameplay deve respeitar gate/readiness.

Referência operacional: [WORLD_LIFECYCLE.md](WORLD_LIFECYCLE.md).

---

## ADR-0009 — Fade + SceneFlow (NewScripts)
**Status:** Implementado e validado

**Decisão:** implementar Fade no pipeline de SceneFlow usando:
- `INewScriptsFadeService` com `FadeScene` (Additive)
- `NewScriptsSceneTransitionProfile` resolvido via Resources
- sem fallback para fade legado

**Motivação:** garantir transições visuais padronizadas sem depender de UI/DI legados.

**Consequências:**
- o asset de profile deve estar em `Resources/SceneFlow/Profiles/`
- o adapter de fade deve aplicar profile (durations/curves) antes do FadeIn/FadeOut
- falha de resolução de profile deve degradar para defaults (não travar o fluxo)

Referência: [ADR-0009-FadeSceneFlow.md](ADRs/ADR-0009-FadeSceneFlow.md).

---

## ADR-0010 — Loading HUD + SceneFlow (NewScripts)
**Status:** Implementado e validado

**Decisão:** implementar Loading HUD no pipeline de SceneFlow usando:
- `INewScriptsLoadingHudService` com `LoadingHudScene` (Additive)
- `NewScriptsLoadingHudController` como UI root (CanvasGroup + overlay simples)
- `SceneFlowLoadingService` para reagir a `SceneTransitionStarted/ScenesReady/Completed` e aplicar `Show/Hide` correlacionando por `signature`

**Motivação:** garantir feedback visual consistente em transições (startup/menu/gameplay) sem depender de HUD legada e sem acoplamento com cenas específicas (ex.: `UIGlobalScene`).

**Consequências:**
- `LoadingHudScene` é carregada aditivamente sob demanda e pode ser reutilizada em qualquer transição.
- O serviço deve aplicar “Started → Show”, “ScenesReady → Update”, “BeforeFadeOut → Hide”, “Completed → Safety Hide”.
- A correlação é por `signature` do `SceneTransitionContext` (não por eventos do GameLoop).

Referência: [ADR-0010-LoadingHud-SceneFlow.md](ADRs/ADR-0010-LoadingHud-SceneFlow.md).

---

## ADR-0013 — Readiness/Gate como fonte de verdade para “pronto para jogar”
**Status:** Ativo

Resumo:
- Prontidão é derivada do `GameReadinessService` + `ISimulationGateService` (tokens).
- Ações de gameplay devem consultar `IStateDependentService` (gate-aware).
- Transições de cena adquirem/liberam tokens para manter determinismo.

Referência: [ADR-0013-Ciclo-de-Vida-Jogo.md](ADRs/ADR-0013-Ciclo-de-Vida-Jogo.md).

---

## ADR-0013 — SKIP de WorldLifecycle em startup/menu
**Status:** Temporário (para estabilização e testes)

Resumo:
- O `WorldLifecycleRuntimeCoordinator` faz **SKIP** em `startup/menu`.
- Mesmo no SKIP, emite `WorldLifecycleResetCompletedEvent` para destravar o fluxo.
- Evita dependência da GameplayScene durante bootstrap/frontend.

Referência: [ADR-0013-Ciclo-de-Vida-Jogo.md](ADRs/ADR-0013-Ciclo-de-Vida-Jogo.md).

## Decisão — Eventos do GameLoop permanecem context-free
- O GameLoop não transporta `ContextSignature` em eventos de start/pause/resume/reset.
- A correlação por assinatura é feita pelo Coordinator via eventos do Scene Flow (`SceneTransitionContext`) e World Lifecycle (`WorldLifecycleResetCompletedEvent`).
- Se transições concorrentes se tornarem um requisito, introduzir `GameStartCommandEvent` com `ContextSignature` como extensão (não padrão).

## ADR-0011 — `CanPerform` não autoriza gameplay; enforcement é gate-aware via IStateDependentService
**Status:** Ativo

**Decisão:** `CanPerform(...)` no GameLoop expressa apenas “capacidade por estado macro” e não consulta gate/readiness. A autorização final de ações é feita por `IStateDependentService` (gate-aware).

**Motivação:** separar “estado do loop” de “estado de infraestrutura” (gate/readiness), evitando acoplamento e mantendo determinismo do pipeline.

**Consequências:**
- Código de gameplay não deve usar `CanPerform` como única condição de execução.
- Logs e bloqueios devem sempre refletir `IStateDependentService` como fonte de verdade.
