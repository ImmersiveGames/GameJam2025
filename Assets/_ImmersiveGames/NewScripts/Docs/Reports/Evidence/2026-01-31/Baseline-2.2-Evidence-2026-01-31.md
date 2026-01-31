# Baseline 2.2 — Evidência canônica (2026-01-31)

> **Fonte de verdade:** log do smoke/QA (ContextMenu) executado em 2026-01-31. Este arquivo consolida as **assinaturas-chave** e **trechos mínimos** necessários para auditar regressões (Baseline 2.x) sem depender de tooling automatizado.

## Resultado

- **Status:** PASS (cenários A–E)
- **Perfis exercitados:** `startup` → `frontend` → `gameplay` → `postgame`
- **Pontos validados:** SceneFlow, WorldLifecycle reset determinístico, spawn multi-actor, gating (`flow.scene_transition`, `sim.gameplay`, `state.pause`), ContentSwap, Pause/Resume, PostGame (Victory/Defeat), Restart e ExitToMenu.

---

## A) Boot → Menu (startup) — sem reset (SKIP esperado)

### Assinaturas

- `SceneFlow` inicia em `startup`.
- `WorldLifecycle` emite `ResetCompleted SKIP` em frontend.

### Trechos

```
[SceneFlow] Transition START :: from='<none>' -> to='MenuScene' :: id=1 :: profile=startup
[WorldLifecycle] ResetCompleted SKIP :: profile=startup :: reason='Frontend/NoGameplay'
```

---

## B) Menu → Gameplay (profile=gameplay) — reset + spawn (Player + Eater)

### WorldDefinition / SceneBootstrapper

**MenuScene (WorldDefinition ausente e permitida):**

```
[SceneBootstrapper] Setup OK :: hasWorldDefinition=False :: scene='MenuScene'
[SceneBootstrapper] WorldDefinition is null (allowed in scenes that do not spawn actors). :: scene='MenuScene'
```

**GameplayScene (WorldDefinition presente, entries=2, spawn services registrados):**

```
[SceneBootstrapper] Setup OK :: hasWorldDefinition=True :: scene='GameplayScene'
[SceneBootstrapper] WorldDefinition loaded :: entries=2 :: scene='GameplayScene'
[SceneBootstrapper] Registered IWorldSpawnContext.
[SceneBootstrapper] Registered ISpawnDefinitionService.
[SceneBootstrapper] Registered ISpawnRegistry.
```

### ResetWorld + spawn determinístico

```
[SceneFlow] Transition START :: from='MenuScene' -> to='GameplayScene' :: id=2 :: profile=gameplay
[WorldLifecycle] ResetWorld START :: profile=gameplay :: id=2 :: reason='SceneFlow/ScenesReady'
[WorldLifecycle] Spawns COMPLETE :: spawnCount=2 :: registryCount=2 :: actors=[Player,Eater]
[WorldLifecycle] ResetWorld COMPLETE :: id=2
```

---

## C) PreGame / IntroStage — gate de simulação (`sim.gameplay`)

### Assinaturas

- `sim.gameplay` bloqueia durante a intro.
- `IntroStage/UIConfirm` desbloqueia e entra em `Playing`.

### Trechos

```
[SimulationGate] Acquire :: token='sim.gameplay' :: reason='IntroStage'
[IntroStage] COMPLETE :: reason='IntroStage/UIConfirm'
[SimulationGate] Release :: token='sim.gameplay' :: reason='IntroStage/UIConfirm'
[GameLoop] ENTER :: state='Playing'
```

---

## D) Gameplay — ContentSwap in-place (QA)

### Assinaturas

- ContentSwap sem transição visual (in-place) com reason padronizado.

### Trechos

```
[ContentSwap] APPLY :: mode='InPlace/NoVisuals' :: contentId='content.2' :: reason='QA/ContentSwap/InPlace/NoVisuals'
```

---

## E) Pause/Resume — gate de estado (`state.pause`) + InputMode

### Assinaturas

- Pause adquire `state.pause` e aplica InputMode de overlay.
- Resume libera `state.pause` e restaura o InputMode.

### Trechos

```
[SimulationGate] Acquire :: token='state.pause' :: reason='PauseOverlay'
[InputMode] APPLY :: mode='PauseOverlay'
...
[SimulationGate] Release :: token='state.pause' :: reason='PauseOverlay'
[InputMode] APPLY :: mode='Gameplay'
```

---

## F) PostGame — Victory/Defeat (idempotência) + Restart + ExitToMenu

### Assinaturas

- PostGame é acionado por `Victory` e `Defeat`.
- Restart gera novo ciclo determinístico (reset + intro + playing).
- ExitToMenu retorna a profile `frontend` e mantém `SKIP reset`.

### Trechos

```
[PostGame] ENTER :: reason='Victory'
[PostGame] ENTER :: reason='Defeat'

[PostGame] Restart :: reason='PostGame/Restart'
[SceneFlow] Transition START :: from='PostGameScene' -> to='Boot' :: reason='PostGame/Restart'

[PostGame] ExitToMenu :: reason='PostGame/ExitToMenu'
[SceneFlow] Transition START :: from='PostGameScene' -> to='MenuScene' :: profile=frontend :: reason='PostGame/ExitToMenu'
[WorldLifecycle] ResetCompleted SKIP :: profile=frontend :: reason='Frontend/NoGameplay'
```

---

## Invariantes globais observados (amostras)

### `flow.scene_transition`

```
[SimulationGate] Acquire :: token='flow.scene_transition' :: reason='SceneTransitionStarted'
...
[SimulationGate] Release :: token='flow.scene_transition' :: reason='SceneTransitionCompleted'
```

### `WorldLifecycle.WorldReset`

```
[SimulationGate] Acquire :: token='WorldLifecycle.WorldReset' :: reason='ResetWorld'
...
[SimulationGate] Release :: token='WorldLifecycle.WorldReset' :: reason='ResetWorld/Complete'
```

---

## Referências

- Auditoria Strict/Release: `Docs/Reports/Audits/2026-01-31/Invariants-StrictRelease-Audit.md`
- ADR relacionado: `Docs/ADRs/ADR-0011-WorldDefinition-MultiActor-GameplayScene.md`
