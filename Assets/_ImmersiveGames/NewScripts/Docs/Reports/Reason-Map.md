# Reason Map (NewScripts) — `reason` e rastreabilidade

> **Fonte de verdade:** para o contrato completo de observabilidade (eventos, campos mínimos, invariantes e reasons canônicos), ver:
>
> - [Observability-Contract.md](./Observability-Contract.md)
>
> Este documento mantém um mapa prático para busca e navegação; quando houver divergência, prevalece o contrato.

## Objetivo
Consolidar, em um único ponto, os **prefixos e formatos canônicos** de `reason` usados nos logs/eventos de produção.

Este mapa existe para:
- reduzir ambiguidade em auditorias,
- padronizar diagnósticos (grep/asserts),
- e evitar drift entre SceneFlow, WorldLifecycle, GameLoop e PhaseChange.

> Importante: esta tabela descreve **convenções já adotadas** nos docs e reports atuais. Não cria novos contratos.

## Escopo
- SceneFlow (transição de cena)
- WorldLifecycle (reset/skip e evento `WorldLifecycleResetCompletedEvent`)
- Navigation (Menu/Restart/Exit)
- GameLoop (fim de run)
- PhaseChange (in-place vs with-transition)
- IntroStage (pós-revelação; opcional)

## Convenções
- `reason` deve ser **machine-readable**, sem localização.
- Preferir **prefixos** estáveis (`Domain/Action`), e extensões por **key-values** (`;k=v`).
- Onde existir assinatura (`contextSignature`), `reason` complementa a correlação — não substitui.

## Mapa de reasons canônicos

| Domínio | Reason (formato) | Origem típica | Quando ocorre | Evidência (docs/reports) |
|---|---|---|---|---|
| WorldLifecycle | `ScenesReady/<ActiveScene>` | `WorldLifecycleRuntimeCoordinator` | Reset de gameplay após `SceneTransitionScenesReadyEvent` | `Reports/Baseline-2.0-Checklist.md`, `Reports/Baseline-2.0-Smoke-LastRun.log` |
| WorldLifecycle | `Skipped_StartupOrFrontend:profile=<profile>;scene=<activeScene>` | `WorldLifecycleRuntimeCoordinator` | SKIP de reset em startup/frontend (ainda emite ResetCompleted) | `Reports/Baseline-2.0-Checklist.md`, `Reports/Baseline-2.0-Smoke-LastRun.log` |
| WorldLifecycle | `ProductionTrigger/<source>` | `WorldLifecycleRuntimeCoordinator` (via `IWorldResetRequestService`) | Reset manual fora de transição (sem SceneFlow ativo) | `Reports/Audit-ResetTrigger-Production.md` |
| Navigation | `Menu/PlayButton` | UI (binder) → `IGameNavigationService` | Menu → Gameplay (produção) | `Reports/Baseline-2.0-Checklist.md`, `Reports/Baseline-2.0-Smoke-LastRun.log` |
| Navigation | `PostGame/Restart` | Pós-game → navigation | Restart → Gameplay (produção) | `Reports/Baseline-2.0-Checklist.md` |
| Navigation | `ExitToMenu/Event` | Pós-game / evento | Gameplay/PostGame → Menu (produção) | `Reports/Baseline-2.0-Checklist.md` |
| Navigation | `Frontend/QuitButton` | UI frontend | Ação de saída/quit (dev/prod conforme build) | `Reports/Baseline-2.0-Smoke-LastRun.log` |
| SceneFlow | `SceneFlow/Completed:<Profile>` | Bridges globais | Pós `SceneTransitionCompletedEvent` (ex.: sincronização de InputMode/GameLoop) | `Reports/Baseline-2.0-Smoke-LastRun.log` |
| PhaseChange | `PhaseChange/In-Place/...` | Caller → `PhaseChangeService` | Troca de fase sem SceneFlow (reset in-place) | `ADRs/ADR-0017-Tipos-de-troca-fase.md`, `Reports/QA-PhaseChange-Smoke.md` |
| PhaseChange | `PhaseChange/SceneTransition/...` | Caller → `PhaseChangeService` | Troca de fase com SceneFlow (intent + ScenesReady + reset + commit) | `ADRs/ADR-0017-Tipos-de-troca-fase.md`, `Reports/QA-PhaseChange-Smoke.md` |
| IntroStage | `IntroStage/UIConfirm` | Debug GUI / produção | Confirmação de UI para concluir IntroStage | `ADRs/ADR-0016-Phases-WorldLifecycle.md`, `Reports/QA-IntroStage-Smoke.md` |
| IntroStage | `IntroStage/NoContent` | IntroStageCoordinator | Auto-skip quando não há conteúdo | `ADRs/ADR-0016-Phases-WorldLifecycle.md`, `Reports/QA-IntroStage-Smoke.md` |
| IntroStage | `QA/IntroStage/Complete` / `QA/IntroStage/Skip` | Context Menu / Tools | Mitigação determinística em dev/QA | `ADRs/ADR-0016-Phases-WorldLifecycle.md`, `Reports/QA-IntroStage-Smoke.md` |
| GameLoop | `RunEnded/<Outcome>` *(ex.: `RunEnded/Victory`)* | GameLoop / pós-game | Encerramento da run (resultado) | `ADRs/ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md` |

## Notas
- Reasons de infraestrutura (ex.: readiness snapshots como `gate_closed`, `scene_transition_started`) são **internos** e não devem virar contrato externo, a menos que promovidos explicitamente em um baseline.
- Para novos reasons: preferir **prefixo canônico** + documentação aqui + evidência em report/checklist.
