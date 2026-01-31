# Auditoria de Invariants — Strict/Release (2026-01-31)

Escopo: `Assets/_ImmersiveGames/NewScripts/` (documental; sem alterações de código).

## ✅ Tabela de PASS/FAIL (Checklist A–F)

| Item | Resultado | Evidência principal |
|---|---|---|
| A) Fade/LoadingHUD (Strict + Release + degraded mode) | **FAIL** | Fade continua sem controller (fallback silencioso); LoadingHUD idem; sem modo STRICT explícito, sem âncora `DEGRADED_MODE`. |
| B) WorldDefinition (Strict + mínimo spawn) | **FAIL** | `worldDefinition` nulo é permitido; sem validação de mínimo spawn (Player/Eater) em gameplay. |
| C) LevelCatalog (Strict + Release) | **FAIL** | Resolver apenas loga warning e retorna `false` em ausência de catalog/definition; sem política Strict vs Release. |
| D) PostGame (Strict + Release) | **FAIL** | InputMode/Gate ausentes geram apenas warning; sem fail-fast em Strict e sem fallback definido em Release. |
| E) Ordem do fluxo (RequestStart após IntroStageComplete) | **FAIL** | `GameLoopSceneFlowCoordinator` chama `RequestStart()` após `transitionCompleted + resetCompleted`, sem esperar IntroStage. |
| F) Gates (ADR-0016 ContentSwap) | **FAIL** | `ContentSwapChangeServiceInPlaceOnly` não consulta gates `flow.scene_transition` / `sim.gameplay`. |

## Detalhamento por item

### A) Fade/LoadingHUD — Strict fail-fast, Release e modo degradado
- **Situação:** fallback silencioso quando controller/cena não existe.
- **Gap:** ausência de branch Strict/Release e ausência de âncora `DEGRADED_MODE`.

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
