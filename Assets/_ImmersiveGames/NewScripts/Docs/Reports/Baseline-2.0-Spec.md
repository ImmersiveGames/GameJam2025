# Baseline 2.0 — Spec (Frozen)

> ⚠️ **Histórico / pode estar desatualizado:** o baseline é log-driven e a fonte de verdade é o `Reports/Baseline-2.0-Smoke-LastRun.log`.

Este documento é **histórico** e serve como referência do Baseline 2.0 para o projeto NewScripts.

> Nota explícita: **soft não afeta FAIL** e **hard afeta FAIL**.

## Propósito e escopo
O Baseline 2.0 valida o “contrato mínimo” do pipeline NewScripts em produção:
- Scene Flow emite eventos e mantém ordem operacional (Started → ScenesReady → Completed).
- SimulationGate garante bloqueio determinístico durante transições, reset e pause.
- WorldLifecycle publica um evento oficial de conclusão (ou SKIP) para destravar o gate de completion.
- GameLoop sincroniza com SceneFlow (frontend → Ready, gameplay → Playing).
- Fade/Loading são integrados sem contaminar Gameplay com QA/legacy.

---

## Baseline Matrix A–E

| ID | Cenário | Objetivo | Perfil esperado |
|---:|---|---|---|
| A | Boot → Menu | Garantir boot + transição inicial com reset SKIP e gate fechado/aberto corretamente | `startup` |
| B | Menu → Gameplay | Garantir reset hard + spawn mínimo e chegada em gameplay | `gameplay` |
| C | IntroStage (fora do baseline — Opção B) | Manter como etapa opcional, validada em smoke separado quando promovido | n/a |
| D | Pause → Resume | Garantir coerência do token `state.pause` | n/a |
| E | PostGame (Victory/Defeat) + Restart + ExitToMenu | Garantir pós-game + restart e saída ao menu com perfis esperados | `gameplay` / `frontend` |

---

## Invariantes Globais (HARD)

**Regras obrigatórias (todas hard):**
- `SceneTransitionStarted` fecha o gate do token `flow.scene_transition`.
- `ScenesReady` acontece antes de `Completed`.
- `WorldLifecycleResetCompletedEvent` (ou SKIP) ocorre **antes** do completion gate liberar o FadeOut.
- Tokens devem fechar balanceados (`Acquire == Release` por token).

### Evidências HARD (patterns/regex)
- `I.SceneTransitionStarted` :: `\[Readiness\].*SceneTransitionStarted`
- `I.SceneTransitionCompleted` :: `\[Readiness\].*SceneTransitionCompleted`
- `I.SceneTransitionGateAcquire` :: `Acquire token='flow\.scene_transition'`
- `I.SceneTransitionGateRelease` :: `Release token='flow\.scene_transition'\. Active=0\. IsOpen=True`

### Regras de Ordem (patterns/regex)
- `I.SceneTransitionStartedBeforeCompleted` :: `\[Readiness\].*SceneTransitionStarted` => `\[Readiness\].*SceneTransitionCompleted`
- `I.ScenesReadyBeforeCompleted` :: `SceneTransitionScenesReady` => `\[Readiness\].*SceneTransitionCompleted`
- `I.ResetCompletedBeforeFadeOut` :: `WorldLifecycleResetCompletedEvent|Reset SKIPPED \(startup/frontend\)` => `Completion gate conclu[ií]do\. Prosseguindo para FadeOut`

---

## Assinaturas-chave (patterns/regex)

> Estas assinaturas são usadas como chaves estáveis no baseline. Elas refletem as mensagens reais presentes no log de referência.

- `SIG.Startup.Menu` :: `signature='p:startup\|a:MenuScene\|f:1\|l:MenuScene\|UIGlobalScene\|u:NewBootstrap'`
- `SIG.Gameplay.FromMenu` :: `signature='p:gameplay\|a:GameplayScene\|f:1\|l:GameplayScene\|UIGlobalScene\|u:MenuScene'`
- `SIG.Frontend.FromGameplay` :: `signature='p:frontend\|a:MenuScene\|f:1\|l:MenuScene\|UIGlobalScene\|u:GameplayScene'`

---

## Cenários A–E (evidências por log)

### Cenário A — Boot → Menu (profile=startup, SKIP reset)

**Objetivo:** transição inicial com reset SKIP, gate de transição fechado/aberto e Ready no frontend.

#### Evidências HARD (PASS/FAIL)
- `A.StartTransition` :: `Iniciando transi[cç][aã]o: Load=\[MenuScene, UIGlobalScene\], Unload=\[NewBootstrap\], Active='MenuScene', UseFade=True, Profile='startup'`
- `A.ScenesReady` :: `SceneTransitionScenesReady recebido` 
- `A.ResetSkippedStartup` :: `Reset SKIPPED \(startup/frontend\).*profile='startup'`
- `A.ResetCompletedStartup` :: `Emitting WorldLifecycleResetCompletedEvent.*profile='startup'.*Skipped_StartupOrFrontend:profile=startup;scene=MenuScene`
- `A.FlowGateRelease` :: `Release token='flow\.scene_transition'\. Active=0\. IsOpen=True`

#### Evidências SOFT (diagnóstico)
- `A.LoadingHudShow` :: `FadeInCompleted → Show\. signature='p:startup\|a:MenuScene\|f:1\|l:MenuScene\|UIGlobalScene\|u:NewBootstrap'`
- `A.GameLoopReady` :: `ENTER: Ready \(active=False\)`

#### Regras de Ordem (diagnóstico)
- `A.Order.ResetCompletedBeforeFadeOut` :: `Emitting WorldLifecycleResetCompletedEvent.*profile='startup'` => `Completion gate conclu[ií]do\. Prosseguindo para FadeOut`

---

### Cenário B — Menu → Gameplay (profile=gameplay, reset + spawn)

**Objetivo:** reset hard + spawn mínimo (Player + Eater) com perfil gameplay.

#### Evidências HARD (PASS/FAIL)
- `B.NavigateToGameplay` :: `NavigateAsync -> routeId='to-gameplay'.*Profile='gameplay'`
- `B.ScenesReady` :: `\[WorldLifecycleRuntimeCoordinator\].*SceneTransitionScenesReady recebido` 
- `B.ResetCompletedGameplay` :: `Emitting WorldLifecycleResetCompletedEvent.*profile='gameplay'.*ScenesReady/GameplayScene`
- `B.SpawnCount` :: `ActorRegistry count at 'After Spawn': 2`

#### Evidências SOFT (diagnóstico)
- `B.PlayerSpawned` :: `Actor spawned: .*Player_NewScriptsClone`
- `B.EaterSpawned` :: `Actor spawned: .*Eater_NewScriptsClone`
- `B.GameLoopPlaying` :: `ENTER: Playing \(active=True\)`

#### Regras de Ordem (diagnóstico)
- `B.Order.ScenesReadyBeforeResetCompleted` :: `SceneTransitionScenesReady` => `WorldLifecycleResetCompletedEvent`

---

### Cenário C — IntroStage (fora do baseline na Opção B)

**Objetivo (histórico):** manter a IntroStage como etapa opcional e validar em smoke separado quando o fluxo for promovido.

- **Status:** fora do baseline (Opção B).
- **Evidências:** n/a (sem evidência no smoke log atual).

---

### Cenário D — Pause → Resume (token state.pause)

**Objetivo:** token `state.pause` fecha no pause e libera no resume.

#### Evidências HARD (PASS/FAIL)
- `D.PauseAcquire` :: `Acquire token='state\.pause'`
- `D.PauseRelease` :: `Release token='state\.pause'`

#### Evidências SOFT (diagnóstico)
- `D.GameLoopPaused` :: `ENTER: Paused` 
- `D.GameLoopPlaying` :: `ENTER: Playing`

#### Regras de Ordem (diagnóstico)
- `D.Order.AcquireBeforeRelease` :: `Acquire token='state\.pause'` => `Release token='state\.pause'`

---

### Cenário E — PostGame (Victory/Defeat) + Restart + ExitToMenu

**Objetivo:** pós-game com outcome, restart e saída ao menu com perfis esperados.

#### Evidências HARD (PASS/FAIL)
- `E.VictoryDetected` :: `Outcome=Victory`
- `E.DefeatDetected` :: `Outcome=Defeat`
- `E.RestartToGameplay` :: `NavigateAsync -> routeId='to-gameplay'.*reason='PostGame/Restart'.*Profile='gameplay'`
- `E.ExitToMenuRequest` :: `ExitToMenu recebido -> RequestMenuAsync`
- `E.NavigateToMenuFrontend` :: `NavigateAsync -> routeId='to-menu'.*Profile='frontend'`
- `E.ResetSkippedFrontend` :: `Reset SKIPPED \(startup/frontend\).*profile='frontend'`

#### Evidências SOFT (diagnóstico)
- `E.PostGameGateAcquire` :: `Acquire token='state\.postgame'`
- `E.PostGameGateRelease` :: `Release token='state\.postgame'`
- `E.GameLoopReady` :: `ENTER: Ready \(active=False\)`

---

## Template mínimo de evidência (por cenário)
Use este bloco para registrar o resultado do cenário A–E:

```
Scenario: <A|B|C|D|E>
Profile: <startup|gameplay|frontend|n/a>
ContextSignature: <signature observada>
Outcome: <PASS|FAIL>
Evidence:
- <linha de log ou regex>
- <linha de log ou regex>
Notes:
- <observações relevantes>
```

## Referência histórica (runner)
Historicamente, o `Baseline2Smoke` escrevia:
- `Docs/Reports/Baseline-2.0-Smoke-LastRun.md`
- `Docs/Reports/Baseline-2.0-Smoke-LastRun.log`

O checklist `Reports/Baseline-2.0-Checklist.md` consolida o status por cenário.

---

## Changelog

- 2026-01-05: **Baseline 2.0 spec frozen**.
- 2026-01-05: **Tool now aligned to spec**.
- 2026-01-05: **Hard/soft separation**.
- 2026-01-05: **Single menu entry**.
- 2026-01-05: **Tightened regex for SceneTransitionStarted/Completed to avoid false positives; order validation now supports multiple transitions**.
