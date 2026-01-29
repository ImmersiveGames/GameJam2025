# Changelog (Docs)

## 2026-01-29

- ADR-0017 Etapa 0 concluída (assets + providers/resolver + DI + QA); execução/evidência de Level ainda pendente.
- Atualizado `Docs/Reports/Evidence/LATEST.md` para apontar para o snapshot **2026-01-29**.
- Arquivado novo snapshot: `Docs/Reports/Evidence/2026-01-29/Baseline-2.2-Evidence-2026-01-29.md`.
- **ADR-0016** marcado como **Implementado** e vinculado à evidência 2026-01-29 (ContentSwap in-place / G01).
- Atualizado `Docs/Reports/Observability-Contract.md` com os tokens/razões canônicas atuais (Pause/PostGame/ContentSwap).

## 2026-01-28
- Archived Baseline 2.2 evidence snapshot (Boot→Menu skip, Menu→Gameplay reset+spawn+IntroStage, Level L01 InPlace pipeline).
- ADR-0012: removida referência obsoleta a `WorldLifecycleRuntimeCoordinator` (substituído pelo driver canônico `WorldLifecycleSceneFlowResetDriver`).
- ADR-0018: normalizada seção de evidências para exigir snapshot datado (Aceito 2026-01-18) + ponte canônica LATEST.
- Evidence/LATEST: adicionado link do snapshot de aceitação do ADR-0018.
- Runtime/Observability: alinhado contrato mínimo de observabilidade para WorldLifecycle (ResetRequested/ResetCompleted) e InputMode em `SceneFlow/Completed`.

## 2026-01-27
- Docs: Baseline 2.0 → fontes vigentes (ADR-0015 + Evidence/LATEST + Observability-Contract).
- ADR-0012: PostGame canônico + idempotência do overlay (double click + evento duplicado).
- Arquivos alterados: `Docs/ARCHITECTURE.md`, `Docs/CHANGELOG-docs.md`,
  `Docs/ADRs/ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md`,
  `Docs/Reports/Observability-Contract.md`.

## 2026-01-21
- ADR-0018 reescrito para formalizar a mudança semântica para ContentSwap + LevelManager e delimitar o LevelManager.
- ADR-0019 atualizado para descrever promoção do Baseline 2.2 com escopo, gates e metodologia de evidência por data.
- ARCHITECTURE.md e READMEs ajustados para terminologia consistente (ContentSwap vs LevelManager).
- Arquivos alterados: `Docs/ADRs/ADR-0018-Gate-de-Promoção-Baseline2.2.md`, `Docs/ADRs/ADR-0019-Promocao-Baseline2.2.md`, `Docs/plano2.2.md`, `Docs/ARCHITECTURE.md`, `Docs/README.md`, `Docs/ADRs/README.md`, `README.md`.

## 2026-01-20
- ADR-0018/ADR-0019 reescritos para formalizar ContentSwap + LevelManager.
- Plano 2.2 reordenado com QA separado para ContentSwap (QA_ContentSwap) e Level (QA_Level).
- Observability-Contract atualizado para ContentSwap + Level (reasons e anchors).

## 2026-01-19
- ADR-0018 reestruturado para definir ContentSwap e observability, separando de Level/Nível.
- ADR-0019 reescrito para Level Manager (progressão) e gates verificáveis do Baseline 2.2.
- Plano 2.2 reordenado (ContentSwap → Level Manager → Configuração → QA/Evidências/Gate).
- Índice de ADRs atualizado para refletir os novos escopos.

## 2026-01-18
- Reports/Evidence: novo snapshot 2026-01-18 (Baseline 2.1) com logs mesclados (Restart e ExitToMenu).
- ADR-0012: referência de evidência atualizada para o snapshot 2026-01-18.
- ADR-0015: referência de evidência atualizada para o snapshot 2026-01-18.

## [2026-01-16]

### Alterado

- Consolidado snapshot datado de evidências em `Docs/Reports/Evidence/2026-01-16/` e atualizado `Docs/Reports/Evidence/LATEST.md`.
- Restaurado `Docs/Reports/Observability-Contract.md` como fonte de verdade.
- Atualizados links de evidência em ADRs e READMEs para apontar para `Docs/Reports/Evidence/`.

## [2026-01-15]
### Changed
- Baseline 2.0 checklist ajustado para refletir a cobertura do log atual (A, B, D, E; **IntroStage pendente**) e a ordem Fade/Loading detalhada.
- ADR-0016 refinado para explicitar contrato operacional da IntroStage (token `sim.gameplay`, InputMode UI/Gameplay, `UIConfirm`/`NoContent`, RuntimeDebugGui/QA).
- ADR-0010 alinhado à ordem real do Fade/Loading HUD e ao posicionamento da IntroStage (post-reveal).
- IntroStage consolidada como termo canônico (sem compatibilidade legada).

## [2026-01-14]
### Changed
- ADR-0016 atualizado para consolidar **IntroStage (PostReveal)** como nomenclatura canônica e explicitando que ocorre após `FadeOut` e `SceneTransitionCompleted` (fora do Completion Gate).

## [2026-01-13]
### Added
- Registro incremental de evidências do **Baseline 2.0** (cenários 1 e 2) a partir do log fornecido nesta conversa.

### Evidência (log) usada como fonte de verdade
- **Teste 1 — Startup → Menu (profile=`startup`)**
    - `WorldLifecycleRuntimeCoordinator` solicitou reset e **SKIPOU** por perfil não-gameplay: `Reset SKIPPED (startup/frontend). why='profile'` e emitiu `WorldLifecycleResetCompletedEvent(signature, reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene')`.
    - `WorldLifecycleResetCompletionGate` recebeu o `WorldLifecycleResetCompletedEvent(...)` e liberou o `SceneTransitionService` **antes do FadeOut** (gate cached + “Completion gate concluído. Prosseguindo para FadeOut.”).

- **Teste 2 — Menu → Gameplay (profile=`gameplay`)**
    - `SceneTransitionScenesReady` observado antes de `SceneTransitionCompleted` (ordem preservada).
    - `WorldLifecycleRuntimeCoordinator` executou **hard reset após ScenesReady**: `Disparando hard reset após ScenesReady. reason='ScenesReady/GameplayScene'`.
    - `WorldLifecycleController/Orchestrator` completou o pipeline determinístico:
        - Hooks: `OnBeforeDespawn` → `Despawn` → `OnAfterDespawn` → `OnBeforeSpawn` → `Spawn` → `OnAfterActorSpawn` → `OnAfterSpawn`.
        - Spawns OK: `Spawn services registered from definition: 2` (Player + Eater) e `ActorRegistry count at 'After Spawn': 2`.
    - `WorldLifecycleRuntimeCoordinator` emitiu `WorldLifecycleResetCompletedEvent(signature, reason='ScenesReady/GameplayScene')` e o `WorldLifecycleResetCompletionGate` liberou a continuação do SceneFlow.
