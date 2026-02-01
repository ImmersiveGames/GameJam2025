# Auditoria de Invariants — Strict/Release (2026-01-31)

Escopo: `Assets/_ImmersiveGames/NewScripts/` (documental; refletindo estado do código desta iteração).

## ✅ Tabela de PASS/FAIL (Checklist A–F)

| Item | Resultado | Evidência principal |
|---|---|---|
| A) Fade/LoadingHUD (Strict + Release + degraded mode) | **PASS** | **Fade (PASS runtime):** `Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md` (âncoras `[OBS][Fade]`). **LoadingHUD:** ver ADR-0010. |
| B) WorldDefinition (Strict + mínimo spawn) | **PASS** | Baseline-2.2 evidencia `WorldDefinition` ativo em gameplay + spawn pipeline consistente (Player + Eater / `ActorRegistry=2`). |
| C) LevelCatalog (Strict + Release) | **FAIL** | Resolver apenas loga warning e retorna `false` em ausência de catalog/definition; sem política Strict vs Release. |
| D) PostGame (Strict + Release) | **PASS** | `PostGameOverlayController` assume ownership de `state.postgame` (gate) + InputMode (frontend) com release garantido; fluxo PostGame evidenciado no Baseline-2.2. |
| E) Ordem do fluxo (RequestStart após IntroStageComplete) | **PASS** | `GameLoopSceneFlowCoordinator` não chama mais `RequestStart()`; apenas `RequestReady()` após reset/transição. `RequestStart()` é emitido por `IntroStageCoordinator` após `IntroStageCompleted`. |
| F) Gates (ADR-0016 ContentSwap) | **FAIL** | `ContentSwapChangeServiceInPlaceOnly` não consulta gates `flow.scene_transition` / `sim.gameplay`. |

## Detalhamento por item

### A) Fade/LoadingHUD — Strict fail-fast, Release e modo degradado

**Fade (ADR-0009):** **PASS (código)**

- Strict: falha cedo quando profile/DI/scene/controller ausentes.
- Release: fallback somente com `DEGRADED_MODE feature='fade' ...` (no-op).
- Observabilidade: âncoras canônicas `[OBS][Fade]` no envelope (Start/Complete por fase).

**LoadingHUD (ADR-0010):** **PASS (código)**

- Strict: validação síncrona de cena em Build Settings (`Application.CanStreamedLevelBeLoaded`) + erro evidente (log + `Debug.Break()`).
- Release: fallback explícito com `DEGRADED_MODE feature='loadinghud' ...` e HUD desabilitado para evitar spam.
- Observabilidade: âncoras com `signature` + `phase` (ver `Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md`).


### B) WorldDefinition — Strict em gameplay + mínimo de spawn
- **Status:** **PASS (Baseline 2.2 / evidência)**.
- **Evidência canônica:** `Docs/Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md` (spawn de Player+Eater e `ActorRegistry=2`).
- **Referência:** `Docs/ADRs/ADR-0011-WorldDefinition-MultiActor-GameplayScene.md`.
- **Observação (hardening futuro, não bloqueante):** manter a possibilidade de adicionar fail-fast opcional em Strict para `WorldDefinition` nulo e/ou mínimo de atores.

### C) LevelCatalog — Strict vs Release
- **Situação:** faltas viram warning/false.
- **Gap:** falha controlada em Strict e comportamento Release definido (abort).

### D) PostGame — Strict para InputMode/Gate
- **Status:** **PASS (Baseline 2.2 / evidência)**.
- **Evidência canônica:** `Docs/Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md` (fluxos PostGame: Victory/Defeat, Restart, ExitToMenu).
- **Código (ownership):** `NewScripts/Gameplay/PostGame/PostGameOverlayController.cs` adquire `state.postgame` e aplica `InputMode=Frontend` quando opera em modo de ownership local, liberando em `OnDisable/Hide`.
- **Referência:** `Docs/ADRs/ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md`.

### E) Ordem do fluxo (ADR-0013)
- **Requisito:** `RequestStart()` apenas após concluir IntroStage (token canônico `sim.gameplay`).
- **Status:** **PASS** — `GameLoopSceneFlowCoordinator` sincroniza apenas até **Ready**; o `IntroStageCoordinator` emite `RequestStart()` somente depois de `IntroStageCompleted`. |

### F) Gates (ADR-0016 ContentSwap)
- **Requisito:** respeitar gates `flow.scene_transition` e `sim.gameplay`.
- **Gap:** checagem antes de commit; política de bloquear/reintentar.

## Comandos usados (auditoria)
- `rg -n "sim.gameplay|SimulationGateTokens|state.pause|state.postgame|flow.scene_transition" Assets/_ImmersiveGames/NewScripts`
- `rg -n "STRICT|Strict|fail-fast|FailFast|DEGRADED|degraded|assert|throw|InvalidOperationException" Assets/_ImmersiveGames/NewScripts`
