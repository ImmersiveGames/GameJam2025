# Baseline 2.0 — Checklist (Smoke)

Última validação: 2026-01-03
Timezone: America/Sao_Paulo
Runner: Baseline2Smoke (mode=MANUAL_ONLY)
Artifacts gerados pelo runner:
- Reports/Baseline-2.0-Smoke-LastRun.md
- Reports/Baseline-2.0-Smoke-LastRun.log

## Objetivo
Transformar o baseline em contrato verificável (logs + invariantes), reduzindo ambiguidade e regressões silenciosas.

---

## Status geral
- [x] A) Boot/Infra OK (serviços globais + registries + bridges)
- [x] A2/B0) Menu estável (startup → Menu, SKIP reset, gate coerente)
- [x] B1/B3) Gameplay entra e estabiliza (reset + spawn + Playing)
- [x] C1) Pause/Resume (tokens coerentes + sem GameRunStarted em Resume)
- [ ] Próximo: D) PostGame (Victory/Defeat) + Restart/ExitToMenu (produção) — ainda não executado neste log
- [ ] Próximo: E) Idempotência (reiniciar duas vezes seguidas / repetição de comandos) — ainda não executado neste log

---

## A) Boot / Infra ready
### A1) Infra ready (DI + serviços críticos)
**PASS evidências:**
- Resolve services: `IGameCommands=OK, IGameNavigationService=OK`
- Serviços globais registrados (exemplos do log):
    - `ISimulationGateService`
    - `INewScriptsFadeService` (ADR-0009)
    - `INewScriptsLoadingHudService`
    - `IGameLoopService`
    - `ISceneTransitionService`
    - `IGameNavigationService`

**Observações (não-bloqueante):**
- Warning de deduplicação “Chamada repetida no frame …” em resolves de DI.

---

## B) Menu stable (startup profile)
### B0) Startup → Menu (SKIP reset)
**PASS evidências:**
- SceneFlow iniciou com `Profile='startup'`
- Gate fecha na transição e abre no fim:
    - `Acquire token='flow.scene_transition'`
    - `Release token='flow.scene_transition'`
- Reset SKIP com evento oficial:
    - `Reset SKIPPED (startup/frontend)`
    - `Emitting WorldLifecycleResetCompletedEvent ... reason='Skipped_StartupOrFrontend:...'`
- Coordinator sincroniza GameLoop para frontend:
    - `Profile não-gameplay (profileId='startup'). Chamando RequestReady() no GameLoop.`
    - `ENTER: Ready`

**Invariantes verificados:**
- `SceneTransitionStarted` fecha gate (`flow.scene_transition`) antes de prosseguir.
- `WorldLifecycleResetCompletedEvent` ocorre antes de `SceneFlow Completed`.

---

## B) Gameplay enter + stable (gameplay profile)
### B1) Menu → Gameplay (Reset + Spawn)
**PASS evidências:**
- `Profile='gameplay'`
- `WorldLifecycle` disparou reset após `ScenesReady`:
    - `Disparando hard reset após ScenesReady. reason='ScenesReady/GameplayScene'`
- Spawn services:
    - `Spawn services registered from definition: 2` (Player + Eater)
    - `ActorRegistry count at 'After Spawn': 2`
- Evento oficial de conclusão:
    - `Emitting WorldLifecycleResetCompletedEvent ... reason='ScenesReady/GameplayScene'`
- Completion gate conclui antes do FadeOut:
    - `Aguardando completion gate antes do FadeOut`
    - `Completion gate concluído. Prosseguindo para FadeOut.`

### B3) Gameplay stable (Playing + input)
**PASS evidências:**
- GameLoop entra em Playing:
    - `EXIT: Ready`
    - `ENTER: Playing (active=True)`
- Run started observado no primeiro Playing:
    - `GameRunStartedEvent inicial observado`
- StateDependent libera ação após condições:
    - `Action 'Move' liberada ... gameLoopState='Playing'`

---

## C) Pause/Resume (tokens + sem duplicar run start)
### C1) Pause
**PASS evidências:**
- Comando:
    - `RequestPause reason='baseline2'`
- Gate token:
    - `Acquire token='state.pause'`
- GameLoop:
    - `ENTER: Paused (active=False)`
- InputMode:
    - `Modo alterado para 'PauseOverlay'`

### C1) Resume
**PASS evidências:**
- Comando:
    - `RequestResume reason='baseline2'`
- Gate token:
    - `Release token='state.pause'`
- GameLoop:
    - `ENTER: Playing (active=True)`
- Regra de “run start once” preservada:
    - Não reaparece “GameRunStartedEvent inicial observado” no Resume.

---

## Registro de riscos/ruídos aceitos
- Dedup warnings de DI: aceitável no baseline (não é falha funcional).
- `GamePauseGateBridge` no shutdown: “Release ignorado (Dispose) — sem handle ativo” (aceito se o bridge só libera quando possui ownership/handle).

---

## Próximos passos (prioridade por valor/risco)
1) Baseline D: PostGame (Victory/Defeat) + overlays.
2) Baseline D/E: Restart e ExitToMenu via rotas de produção (NavigationService) com gates e SKIP correto em frontend.
3) Hardening: Idempotência (cliques repetidos, spam de navegação, repetição de pause/resume).
