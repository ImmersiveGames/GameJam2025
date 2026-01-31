# Auditoria de Invariants — Strict/Release (2026-01-31)

Escopo: `Assets/_ImmersiveGames/NewScripts/` (documental; refletindo estado do código desta iteração).

## ✅ Tabela de PASS/FAIL (Checklist A–F)

| Item | Resultado | Evidência principal |
|---|---|---|
| A) Fade/LoadingHUD (Strict + Release + degraded mode) | **PARCIAL** | **Fade (PASS runtime):** `Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md` (âncoras `[OBS][Fade]`). **LoadingHUD:** ver ADR-0010. |
| B) WorldDefinition (Strict + mínimo spawn) | **FAIL** | `worldDefinition` nulo é permitido; sem validação de mínimo spawn (Player/Eater) em gameplay. |
| C) LevelCatalog (Strict + Release) | **FAIL** | Resolver apenas loga warning e retorna `false` em ausência de catalog/definition; sem política Strict vs Release. |
| D) PostGame (Strict + Release) | **FAIL** | InputMode/Gate ausentes geram apenas warning; sem fail-fast em Strict e sem fallback definido em Release. |
| E) Ordem do fluxo (RequestStart após IntroStageComplete) | **FAIL** | `GameLoopSceneFlowCoordinator` chama `RequestStart()` após `transitionCompleted + resetCompleted`, sem esperar IntroStage. |
| F) Gates (ADR-0016 ContentSwap) | **FAIL** | `ContentSwapChangeServiceInPlaceOnly` não consulta gates `flow.scene_transition` / `sim.gameplay`. |

## Detalhamento por item

### A) Fade/LoadingHUD — Strict fail-fast, Release e modo degradado

**Fade (ADR-0009):** **PASS (código)**

- Strict: falha cedo quando profile/DI/scene/controller ausentes.
- Release: fallback somente com `DEGRADED_MODE feature='fade' ...` (no-op).
- Observabilidade: âncoras canônicas `[OBS][Fade]` no envelope (Start/Complete por fase).

**LoadingHUD (ADR-0010):** **FAIL (pendente)**

- Situação: fallback silencioso quando scene/controller faltam.
- Gap: ausência de branch Strict/Release e ausência de âncora `DEGRADED_MODE` (feature='loadinghud').

### B) WorldDefinition — Strict em gameplay + mínimo de spawn
- **Situação:** gameplay pode iniciar sem `WorldDefinition`.
- **Gap:** validação do mínimo de atores (Player/Eater) e fail-fast em Strict.

### C) LevelCatalog — Strict vs Release
- **Situação:** faltas viram warning/false.
- **Gap:** falha controlada em Strict e comportamento Release definido (abort).

### D) PostGame — Strict para InputMode/Gate
- **Situação:** ausência de serviços críticos não derruba em Strict.
- **Gap:** tratamento por política e logs canônicos.

### E) Ordem do fluxo (ADR-0013)
- **Requisito:** `RequestStart()` após concluir IntroStage (token canônico `sim.gameplay`).
- **Gap:** sincronização do coordinator com IntroStage.

### F) Gates (ADR-0016 ContentSwap)
- **Requisito:** respeitar gates `flow.scene_transition` e `sim.gameplay`.
- **Gap:** checagem antes de commit; política de bloquear/reintentar.

## Comandos usados (auditoria)
- `rg -n "sim.gameplay|SimulationGateTokens|state.pause|state.postgame|flow.scene_transition" Assets/_ImmersiveGames/NewScripts`
- `rg -n "STRICT|Strict|fail-fast|FailFast|DEGRADED|degraded|assert|throw|InvalidOperationException" Assets/_ImmersiveGames/NewScripts`
