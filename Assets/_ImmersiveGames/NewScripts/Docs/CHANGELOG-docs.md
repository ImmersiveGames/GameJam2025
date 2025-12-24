# CHANGELOG-docs.md — NewScripts Docs

> Escopo: mudanças de documentação e consolidações de semântica/contratos do NewScripts.
> Implementações (código) podem ser referenciadas aqui, mas o “source of truth” de código continua sendo o histórico do Git.

## [2025-12-24]
### Consolidated (Semântica / nomenclatura)
- Clarified: **dois domínios** distintos e não-intercambiáveis:
    - **App FrontEnd (AppFlow / macro SceneFlow):** Bootstrap → FrontEnd → Gameplay → FrontEnd.
    - **Simulação (SimulationFlow dentro de GameplayScene):** Warmup/Preparation → GameplayReady → Simulating → PausedSim → ExitSim.
- Clarified: o rótulo **“Menu”** é ambíguo:
    - “MenuScene / FrontEnd” (App) ≠ “Menu” como etapa interna de simulação (GameplayScene).
    - A documentação deve tratar “Menu” no GameLoop como **estado/etapa de simulação** (ex.: splash/prep), e nunca como FrontEnd.

### Validated (por evidência de log)
- Validated: **Opção B (Start sincronizado)** funcionando:
    - `GameStartEvent` é coordenado pelo `GameLoopSceneFlowCoordinator`,
    - `SceneTransitionScenesReadyEvent` dispara reset determinístico via `WorldLifecycleRuntimeDriver`,
    - e `RequestStart()` ocorre **exatamente 1x** após **TransitionCompleted + WorldLifecycleResetCompleted** (correlacionados pela mesma assinatura) — evita start duplo.
- Validated: uso coerente de gates:
    - `flow.scene_transition` durante a transição,
    - `flow.loading` durante o reset,
    - `WorldLifecycle.WorldReset` no reset hard,
    - `state.pause` no pause (bloqueio de ação sem timeScale).


### Validated (WorldLifecycle Baseline — por evidência de log)
- Validated: `WorldLifecycleBaselineRunner` **Full Baseline** (Hard Reset + Soft Reset Players) executando com sucesso na cena de boot (`NewBootstrap`):
    - Hard Reset: pipeline completo com gate `WorldLifecycle.WorldReset`, hooks de cena, spawn/despawn e hooks de ator.
    - Soft Reset Players: **reset-in-place** com `Despawn/Spawn` explicitamente **skipped by scope filter** e execução do `PlayersResetParticipant`.

### Clarified (Soft Reset Players — escopo intencionalmente mínimo)
- Clarified: durante a fase atual (foco em ciclo global), o `ResetScope.Players` pode permanecer como **smoke test** com um conjunto mínimo de resetáveis,
  até a migração dos controllers legados para o novo formato de reset-in-place.


### QA / Smoke (governança)
- Noted: risco prático de “QA paralelo” quando existirem scripts com `RuntimeInitializeOnLoadMethod`.
- Identified: scripts com maior potencial de auto-execução em Editor/DevBuild:
    - `BaselineDebugBootstrap.cs`
    - `PlayerMovementLeakSmokeBootstrap.cs`
- Action: documentação de QA deve ter **mapa rápido** e instruções claras de ativação/desativação.

### Docs process
- Decision: não substituir arquivos originais por versões “updated.md” simplificadas; revisão passa a ser **arquivo por arquivo**, preservando conteúdo e ajustando termos/links/owners.
- Planned: incluir **Glossário** e **Validação Cruzada** (páginas apontando explicitamente para contrato operacional vs decisão vs QA).

---

## [2025-12-24] (doc updates já registrados)
- Added: `QA/GameLoop-StateFlow-QA.md` com execução do GameLoop/StateDependent QA e resumo de QAs removidos.
- Updated: `GameLoop/GameLoop.md` alinhado ao fluxo real (GameStartEvent + coordinator + ScenesReady → RequestStart).
- Updated: `README.md` listando QA do GameLoop na ordem recomendada e na tabela de owners.

## [2025-12-23] (normalização adicional)
- Merged: conteúdo de `Migrations/LegacyBridges.md` incorporado em `ADR/ADR-0001-NewScripts-Migracao-Legado.md#bridges-temporários-legacysceneflowbridge` (arquivo removido).
- Updated: README/ARCHITECTURE/ADR/ADR-ciclo-de-vida-jogo.md/ADR.md alinhados aos owners (decisão vs operação vs QA) com links explícitos para `WorldLifecycle/WorldLifecycle.md`.
- Updated: documentos de planejamento/relato de normalização movidos para `Reports/` como evidência.
- Updated: `HowTo-PlayerMovement.md` movido para `Reports/` junto das evidências de smoke.
- Updated: referências a critérios de remoção do bridge apontam para o ADR de migração.

## [2025-12-23]
- Added: SceneTransitionService nativo (NewScripts) com adapters para Fade/SceneLoader e proteção contra transições concorrentes.
- Added: SceneTransitionServiceSmokeQATester integrado ao NewScriptsInfraSmokeRunner para validar ordem de eventos e readiness/gate.
- Updated: GlobalBootstrap registra SceneTransitionService nativo com flag NEWSCRIPTS_SCENEFLOW_NATIVE e fallback para bridge legado.
- Updated: LegacyBridges (bridge de Scene Flow legado) marcado explicitamente como temporário até a ativação do Scene Flow nativo.
- Updated: LegacySceneFlow adapters refinados para resolver SceneTransitionProfile por nome e QA reforçado para exercitar loader/fade nativos.

## [2025-12-22]
- Updated: Eventos do GameLoop migrados para NewScripts (doc `GameLoop/GameLoop.md` menciona bootstrap global e localização dos eventos).
- Updated: Auditoria de dependências legadas recalculada após remover referências a `GameManagerSystems.Events`; contagem ajustada para os arquivos restantes.

## [2025-12-20]
- Added: Seção de pause em `WorldLifecycle/WorldLifecycle.md` descrevendo bloqueio de ações via gate sem congelar física/timeScale, agora explícito que o fluxo é GamePauseGateBridge → SimulationGateTokens.Pause → NewScriptsStateDependentService (serviço oficial) bloqueando Move.

## [2025-12-19]
- Added: Baseline Audit for `ResetScope.Players` documenting As-Is state, identified subsystems, and gaps prior to gameplay integration.

## Documentação — Changelog de Normalização
- Moved: referência de pipeline/ordenção de hooks de `ARCHITECTURE.md` (resumo) → `WorldLifecycle/WorldLifecycle.md` (owner operacional já existente; arquitetura mantém o link de resumo)
- Moved: semântica detalhada de fases/passos/reset de `ADR/ADR-ciclo-de-vida-jogo.md` → `WorldLifecycle/WorldLifecycle.md` (referenciado com links explícitos)
- Moved: explicações do pipeline na checklist de QA `QA/WorldLifecycle-Baseline-Checklist.md` → referências diretas para `WorldLifecycle/WorldLifecycle.md`
- Removed (duplicate): detalhes operacionais em ADR-0001 substituídos por referência ao contrato em `WorldLifecycle/WorldLifecycle.md`
- Updated links: `DECISIONS.md`, `ARCHITECTURE.md`, `Guides/UTILS-SYSTEMS-GUIDE.md`, `ADR/ADR.md`, `README.md`, `ADR/ADR-0001-NewScripts-Migracao-Legado.md`, `QA/WorldLifecycle-Baseline-Checklist.md`, `ADR/ADR-ciclo-de-vida-jogo.md`
- No functional change (documentação apenas)
