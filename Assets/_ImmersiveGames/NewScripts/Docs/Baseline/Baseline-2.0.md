# Baseline 2.0 — Smoke Contract (NewScripts)

Última revisão: 2026-01-03

## Propósito
O Baseline 2.0 valida o “contrato mínimo” do pipeline NewScripts em produção:

- Scene Flow emite eventos e mantém ordem operacional (Started → ScenesReady → Completed).
- SimulationGate garante bloqueio determinístico durante transições, reset e pause.
- WorldLifecycle publica um evento oficial de conclusão (ou SKIP) para destravar o gate de completion.
- GameLoop sincroniza com SceneFlow (frontend → Ready, gameplay → Playing).
- Fade/Loading são integrados sem contaminar Gameplay com QA/legacy.

Este baseline tem foco em **evidência por assinatura de log**, não em “funcionou na minha máquina”.

---

## Matriz mínima (scenarios)
### A) Boot → Menu (startup)
**Expectativa:** transição de cenas com `Profile='startup'` e **Reset SKIP** (frontend).
**Critérios:**
- Gate fecha/abre com `flow.scene_transition`
- WorldLifecycle registra:
    - `Reset SKIPPED (startup/frontend)`
    - `WorldLifecycleResetCompletedEvent` (reason = Skipped_StartupOrFrontend...)
- GameLoop: `ENTER: Ready` ao final

### B) Menu → Gameplay (gameplay)
**Expectativa:** reset determinístico após ScenesReady e spawn mínimo (Player + Eater).
**Critérios:**
- `Profile='gameplay'`
- `WorldLifecycle` dispara hard reset após ScenesReady
- Spawn services = 2 (Player/Eater)
- `ActorRegistry count at 'After Spawn' >= 2`
- `WorldLifecycleResetCompletedEvent reason='ScenesReady/GameplayScene'`
- GameLoop: `ENTER: Playing`, e `GameRunStartedEvent` observado **uma vez na run**

### C) Pause/Resume
**Expectativa:** pausa fecha gate via token, resume reabre, e não republica “run started”.
**Critérios:**
- Pause: `Acquire token='state.pause'` e `ENTER: Paused`
- Resume: `Release token='state.pause'` e `ENTER: Playing`
- Não repetir “GameRunStartedEvent inicial observado” no Resume

### D) PostGame + Restart/ExitToMenu (PASS)
**Expectativa:** overlay e rotas de navegação via produção (sem QA), preservando gates e SKIPs.
**Critérios:**
- D1) Gameplay → PostGame (Victory/Defeat)
    - `GameRunEndedEvent` observado (pós-game habilitado)
- D2) PostGame → Restart (reset + rearm)
    - `GameResetRequestedEvent recebido -> RequestGameplayAsync`
    - `NavigateAsync ... routeId='to_gameplay' ... Profile='gameplay'`
    - Reset hard após `ScenesReady` + spawn (Player + Eater)
- D3) PostGame → ExitToMenu (frontend SKIP)
    - `ExitToMenu recebido -> RequestMenuAsync`
    - `NavigateAsync ... routeId='to_menu' ... Profile='frontend'`
    - `Reset SKIPPED (startup/frontend). profile='frontend'`

---

## Invariantes obrigatórias (ordem e gates)
1) `SceneTransitionStarted` deve fechar gate (`flow.scene_transition`).
2) `SceneTransitionScenesReady` ocorre antes de `SceneTransitionCompleted`.
3) `WorldLifecycleResetCompletedEvent` deve ocorrer antes do `Completed` liberar o gate de completion.
4) Em `profile startup/frontend`, o reset deve ser SKIP, mas ainda assim deve emitir `WorldLifecycleResetCompletedEvent` (para destravar o completion gate).
5) Em `profile gameplay`, reset deve ocorrer (despawn/spawn) e emitir `WorldLifecycleResetCompletedEvent`.

---

## Assinaturas de evidência (strings/trechos)
### Gate (SceneFlow)
- `Acquire token='flow.scene_transition'`
- `Release token='flow.scene_transition'`

### WorldLifecycle (SKIP frontend)
- `Reset SKIPPED (startup/frontend)`
- `Emitting WorldLifecycleResetCompletedEvent ... reason='Skipped_StartupOrFrontend:...'`

### WorldLifecycle (Gameplay)
- `Disparando hard reset após ScenesReady`
- `Spawn services registered from definition: 2`
- `ActorRegistry count at 'After Spawn': 2`
- `Emitting WorldLifecycleResetCompletedEvent ... reason='ScenesReady/GameplayScene'`

### GameLoop
- `ENTER: Ready`
- `ENTER: Playing (active=True)`
- `GameRunStartedEvent inicial observado`

### PostGame + Restart/ExitToMenu
- `GameRunEndedEvent`
- `GameResetRequestedEvent recebido -> RequestGameplayAsync`
- `NavigateAsync ... routeId='to_gameplay' ... Profile='gameplay'`
- `ExitToMenu recebido -> RequestMenuAsync`
- `NavigateAsync ... routeId='to_menu' ... Profile='frontend'`
- `Reset SKIPPED (startup/frontend). profile='frontend'`

### Pause/Resume
- `Acquire token='state.pause'`
- `Release token='state.pause'`
- `ENTER: Paused`
- `ENTER: Playing`

---

## Observações aceitas (não falha)
- Warnings de “Chamada repetida” do DebugUtility (deduplicação) durante resolves de DI.
- `GamePauseGateBridge` no shutdown pode registrar “Release ignorado … sem handle ativo” se não houver ownership naquele momento.

## Notas operacionais (baseline)
- ExitToMenu usa `Profile='frontend'` (não `startup`) e o `reason` observado para navegação é `ExitToMenu/Event`.

---

## Saídas do runner
O `Baseline2Smoke` deve escrever:
- `Docs/Reports/Baseline-2.0-Smoke-LastRun.md`
- `Docs/Reports/Baseline-2.0-Smoke-LastRun.log`

O checklist `Baseline-2.0-Checklist.md` consolida o status por cenário.
