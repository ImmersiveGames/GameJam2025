# ADR-0021 — Baseline 3.0 (Completeness)

## Status

- Estado: **Ativo (baseline de completude)**
- Data (decisão): 2026-02-19
- Última atualização: 2026-02-25
- Tipo: Baseline / Contrato verificável por log
- Escopo: NewScripts (SceneFlow, Navigation, LevelFlow, WorldLifecycle, LoadingHud, GameLoop)

## Objetivo

Definir um baseline “3.0” de completude: um conjunto de invariantes e evidências mínimas para considerar o trilho Macro↔Level está sólido o suficiente para avançar em refatorações maiores.

## Evidência canônica atual (2026-02-25)

Fonte: log “Commit 1 minimal” enviado no chat (boot → menu → start gameplay level.1 → intro → playing → postgame → restart → exit-to-menu + resets QA).

Anchors principais observados:

- SceneFlow completo: `TransitionStarted` → `ScenesReady` → `TransitionCompleted` (Menu e Gameplay).
- Política de reset por rota: `RouteAppliedPolicy requiresWorldReset=False (Frontend)` e `True (Gameplay)`.
- Níveis de reset: `WorldResetCommands ResetRequested kind='Level'` e `kind='Macro'` + `ResetCompleted`.
- LevelFlow trilho canônico: `StartGameplayAsync(levelId)` + `LevelSelectedEventPublished` + `levelSignature`.
- IntroStage: bloqueio/liberação de `sim.gameplay` e sincronização com GameLoop.

## Escopo do baseline

Cobre o mínimo para:

- Entrar em Menu (frontend) sem reset.
- Entrar em Gameplay (macro com levels) com reset determinístico.
- Reiniciar e sair para menu sem regressões de gate/input.
- Executar reset macro e reset level via QA.

Não cobre ainda (explicitamente):

- Swap local de level (ADR-0026).
- PostLevel completo (ADR-0027).

## Checklist de completude (Baseline 3.0)

### A) SceneFlow / Macro transitions

- [x] `RouteApplied` + `RouteAppliedPolicy` com `requiresWorldReset` (fonte/razão).
- [x] `TransitionStarted/ScenesReady/TransitionCompleted` com assinatura macro consistente.
- [x] Fade + Loading HUD seguem ordem: Show após FadeIn; Hide antes do FadeOut.

### B) WorldLifecycle / Resets

- [x] MacroReset executa pipeline completo (hooks + despawn + spawn) e registra tempos/ordem.
- [x] Reset completion gate libera a transição (SceneFlowGate).
- [x] Two-level reset via `WorldResetCommands` (Macro vs Level) com `ResetCompleted success=True`.

### C) LevelFlow / seleção e snapshot

- [x] Trilho canônico: `StartGameplayAsync(levelId, reason)`.
- [x] Seleção observável: `LevelSelectedEventPublished` + `levelSignature` + `v` monotônico.
- [x] Restart usa snapshot (não depende de re-seleção manual).

### D) GameLoop / gates / InputMode

- [x] `flow.scene_transition` fecha/abre corretamente.
- [x] IntroStage bloqueia `sim.gameplay`, troca InputMode para UI e depois retorna a Gameplay.
- [x] Pause/Resume usa token dedicado (`state.pause`).
- [x] PostGame usa token dedicado (`state.postgame`) e ações restart/exit.

### E) Pendências que impedem “Baseline fechado”

- [ ] Swap local de level sem transição macro (ADR-0026) com evidência N→1.
- [ ] PostLevel (ADR-0027) com NextLevel/Exit/Restart e logs [OBS] dedicados.
- [ ] Vínculo macroRoute → catálogo de levels consolidado (ADR-0024).

## Critérios de saída

Baseline 3.0 pode ser marcado como “Fechado” quando:

1) ADR-0026 e ADR-0027 estiverem fechados (evidências em log).
2) ADR-0024 estiver fechado (macroRouteId separado e vínculo explícito macro→catálogo).
3) Existir um “log canônico” único para o baseline (A–E) com anchors estáveis.

## Changelog

- 2026-02-25: Atualizado com log canônico mais recente e checklist com status (PASS/pendências) alinhado às ADRs 0022–0027.
