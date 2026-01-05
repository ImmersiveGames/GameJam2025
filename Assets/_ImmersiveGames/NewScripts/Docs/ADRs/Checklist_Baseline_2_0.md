## Checklist Baseline 2.0 — Evidências (log 2026-01-05)

### Invariantes globais

* [x] **SceneTransitionStarted fecha gate** (`flow.scene_transition` Acquire) — evidenciado.
* [x] **ScenesReady ocorre antes de Completed** — evidenciado.
* [x] **Completion gate é aguardado antes do FadeOut** — evidenciado (`Aguardando completion gate…`).
* [x] **WorldLifecycleResetCompletedEvent é emitido** em **skip** (startup/frontend) e em **hard reset** (gameplay) — evidenciado.
* [x] **Cleanup de SceneServiceRegistry** no unload de cenas — evidenciado (remoção de serviços por cena; pool de dicionários).

---

## Matrix de cenários (A–E)

### A) Boot → Menu (startup) **sem reset**

* [x] Transition startup: Load `[MenuScene, UIGlobalScene]`, Unload `[NewBootstrap]`, Active `MenuScene`, UseFade `True`, Profile `startup` — evidenciado.
* [x] **WorldLifecycle SKIP** (startup/frontend) + **ResetCompletedEvent** com reason `Skipped_StartupOrFrontend…` — evidenciado.
* [x] Gate `flow.scene_transition` Acquire/Release — evidenciado.
* [x] GameLoop sincroniza para **Ready** em profile não-gameplay — evidenciado.

**Status A:** PASS

---

### B) Menu → Gameplay (profile=gameplay) **com reset + spawn**

* [x] NavigateAsync route `to-gameplay` (Menu/PlayButton) — evidenciado.
* [x] ScenesReady → **hard reset** reason `ScenesReady/GameplayScene` — evidenciado.
* [x] Spawn services registrados a partir de WorldDefinition: **Player + Eater** — evidenciado.
* [x] Orquestração completa: OnBeforeDespawn/Despawn/OnAfterDespawn/OnBeforeSpawn/Spawn/OnAfterSpawn + actor hooks — evidenciado.
* [x] InputMode muda para **Gameplay** no Completed — evidenciado.
* [x] GameLoop entra em **Playing** após sincronização — evidenciado.

**Status B:** PASS

---

### C) Pause / Resume (tokens coerentes)

* [x] Pause: Acquire `state.pause`, Gate fecha, InputMode `PauseOverlay`, GameLoop `Paused` — evidenciado.
* [x] Resume: Release `state.pause`, Gate abre, InputMode volta `Gameplay`, GameLoop `Playing` — evidenciado.
* [x] StateDependent bloqueia/libera movimento conforme gate/estado — evidenciado.

**Status C:** PASS

---

### D) Gameplay → PostGame (Victory/Defeat) **idempotente**

* [x] `GameRunEndedEvent` publicado (Victory e Defeat) — evidenciado.

* [x] PostGame overlay exibe, InputMode `FrontendMenu` — evidenciado.

* [x] Gate: Acquire/Release `state.postgame` — evidenciado.

* [x] Pausa suprimida no PostGame — evidenciado (`PostGame sem PauseOverlay`).

* [ ] **Idempotência de PostGame** (ex.: receber `GameRunEndedEvent` repetido / clicar múltiplas vezes sem duplicar gate/bindings) — **não há evidência direta no log**.

**Status D:** PASS (funcional) / PENDÊNCIA: evidência de idempotência

---

### E) PostGame → Restart e PostGame → ExitToMenu

* [x] Restart: `GameResetRequestedEvent` → route `to-gameplay` → transição + ScenesReady → hard reset novamente — evidenciado.
* [x] Reinício limpa estado de run: `GameRunStatusService.Clear()` observado — evidenciado.
* [x] ExitToMenu: ExitToMenu recebido → route `to-menu`, Profile `frontend` — evidenciado.
* [x] Frontend: WorldLifecycle **SKIP** + ResetCompletedEvent — evidenciado.
* [x] Unload GameplayScene: limpeza de serviços da cena + CameraResolver unregister — evidenciado.

**Status E:** PASS
