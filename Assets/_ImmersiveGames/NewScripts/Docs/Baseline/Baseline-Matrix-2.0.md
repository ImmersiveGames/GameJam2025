# Baseline Matrix 2.0 (NewScripts)

Objetivo: transformar o baseline em um **contrato verificável** (logs + invariantes), reduzindo falsos positivos e evitando “funciona na minha máquina”.

Escopo: **SceneFlow + Fade/LoadingHUD + WorldLifecycle + GameLoop + Pause/Resume + PostGame (Victory/Defeat) + Restart/ExitToMenu**.

## Referências

- Template para coleta de evidência: [Baseline-Evidence-Template](Baseline-Evidence-Template.md)
- Invariantes que devem ser verdadeiras: [Baseline-Invariants](Baseline-Invariants.md)
- Evidência histórica (master): [Reports/SceneFlow-Production-EndToEnd-Validation](../Reports/SceneFlow-Production-EndToEnd-Validation.md)
- Evidência de pipeline (snapshot): [Reports/SceneFlow-Production-Evidence-2025-12-31](../Reports/SceneFlow-Production-Evidence-2025-12-31.md)

## Matriz mínima (obrigatória)

| ID | Cenário | Perfil esperado | Resultado esperado |
|---:|---|---|---|
| B2.0-01 | Boot → Menu | `startup` | Reset **SKIP** + `WorldLifecycleResetCompletedEvent` emitido |
| B2.0-02 | Menu → Gameplay | `gameplay` | Reset **HARD** + spawn mínimo (Player + Eater, quando aplicável) |
| B2.0-03 | Gameplay → PostGame (Victory/Defeat) | n/a | `GameRunEndedEvent` emitido **1x** por run (idempotência) |
| B2.0-04 | PostGame → Restart | `gameplay` | Nova transição com reset (rearm) e retorno ao `Playing` |
| B2.0-05 | PostGame → ExitToMenu | `frontend`/menu | Skip no menu (sem reset hard) e volta para Frontend |
| B2.0-06 | Pause → Resume | n/a | Gate token `state.pause` coerente (adquire e libera) |

## Cenários detalhados

### B2.0-01 — Boot → Menu (startup, SKIP esperado)

**Passos**
1. Inicie o jogo a partir do `NewBootstrap` (startup padrão).

**Evidência mínima (procure no log por)**
- `SceneTransitionStartedEvent` com `TransitionProfileName='startup'` (ou `TransitionProfileId=startup`).
- `SceneTransitionScenesReadyEvent` antes de `SceneTransitionCompletedEvent`.
- `WorldLifecycleResetCompletedEvent` com:
  - `ContextSignature` compatível com a transição (mesmo signature do `SceneTransitionContext.ContextSignature`).
  - `Reason` indicando SKIP (por exemplo, prefixo `Skipped_...`).
- Presença de `SceneTransitionBeforeFadeOutEvent` (o baseline exige que o reset-completed aconteça antes deste evento).

**Falha alta**
- `WorldLifecycleResetCompletedEvent` ausente (mesmo em SKIP).
- `ScenesReady` ausente ou aparecendo após `Completed`.
- `BeforeFadeOut` acontecendo antes do `ResetCompleted`.

---

### B2.0-02 — Menu → Gameplay (profile=gameplay, reset obrigatório)

**Passos**
1. No Menu, dispare navegação para gameplay (produção: `IGameNavigationService.RequestToGameplay(reason)`).

**Evidência mínima (procure no log por)**
- `SceneTransitionStartedEvent` com `TransitionProfileName='gameplay'`.
- Ordem: `SceneTransitionScenesReadyEvent` → `WorldLifecycleResetCompletedEvent` → `SceneTransitionBeforeFadeOutEvent` → `SceneTransitionCompletedEvent`.
- `WorldLifecycleResetCompletedEvent.Reason` indicando reset hard (por exemplo, prefixo `ScenesReady/...` ou `Ok`/equivalente do runtime coordinator).
- `GameRunStartedEvent` com `StateId=Playing` (quando o Coordinator libera o start).
- Se o baseline de spawn estiver ativo no projeto:
  - Player registrado/spawnado (e Eater quando o mundo multi-actor estiver habilitado).

**Falha alta**
- `ResetCompleted` não aparece ou aparece com `ContextSignature` diferente do `SceneTransitionContext.ContextSignature` da transição.
- `GameRunStartedEvent` não ocorre (ou ocorre antes do reset-completed).
- Exceptions na `GameplayScene` durante/apos `ScenesReady` (hard blockers).

---

### B2.0-03 — Gameplay → PostGame (Victory/Defeat)

**Passos (QA)**
1. Em gameplay, dispare fim de run:
   - Se `PostGameQaHotkeys` estiver ativo: `F7` (Victory) e `F6` (Defeat).

**Evidência mínima (procure no log por)**
- `GameRunEndRequestedEvent` (quando usando o caminho recomendado) seguido de:
- `GameRunEndedEvent` com `Outcome=Victory` **ou** `Outcome=Defeat`.

**Invariante crítica**
- `GameRunEndedEvent` ocorre **no máximo 1x por run**.

**Falha alta**
- `GameRunEndedEvent` repetido na mesma run.
- `GameRunEndedEvent` ausente após o request.

---

### B2.0-04 — PostGame → Restart (reset + rearm)

**Passos**
1. No PostGame, dispare “Restart” (produção: request → bridge de navegação → `IGameNavigationService.RequestToGameplay(...)`).

**Evidência mínima (procure no log por)**
- Nova transição SceneFlow com profile `gameplay`.
- Mesmas invariantes do cenário B2.0-02 (ordem de eventos + reset completed antes do fade out).
- Novo `GameRunStartedEvent` após concluir a transição.

**Falha alta**
- `GameRunStartedEvent` não reinicia ou o `GameRunEndedEvent` “vaza” para a próxima run.

---

### B2.0-05 — PostGame → ExitToMenu (frontend skip)

**Passos**
1. No PostGame, dispare “ExitToMenu” (produção: `IGameNavigationService.RequestToMenu(reason)`).

**Evidência mínima (procure no log por)**
- Transição com profile de frontend/menu (ex.: `startup`/`frontend` conforme o projeto).
- `WorldLifecycleResetCompletedEvent` com `Reason` de SKIP.
- Gate de transição fecha em `Started` e libera em `Completed` (ver invariantes).

**Falha alta**
- Reset hard disparando em Menu (regressão de SKIP).
- Falta de `ResetCompleted`.

---

### B2.0-06 — Pause → Resume (gate tokens coerentes)

**Passos**
1. Em gameplay, pause (overlay) e depois resume.

**Evidência mínima (procure no log por)**
- `GamePauseCommandEvent(IsPaused=true)` e token `SimulationGateTokens.Pause` ativo.
- `GameResumeRequestedEvent` (ou `GamePauseCommandEvent(IsPaused=false)`) e token `SimulationGateTokens.Pause` liberado.

**Falha alta**
- Token de pause fica preso (gate não abre após resume) ou o token nunca é adquirido.

## Saída esperada do Baseline 2.0

- Para cada cenário: um “bloco de evidência” com signature correlacionável + verdict (PASS/FAIL) preenchido via template.
- Falhas devem ser “altas”: erro claro e rastreável para o evento/invariante que foi quebrado.
