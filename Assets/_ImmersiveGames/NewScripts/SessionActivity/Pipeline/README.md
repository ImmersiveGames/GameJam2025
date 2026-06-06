# SessionActivityPipeline - Estado Real Congelado (Base 1.1 Sandbox)

## Status
- Este modulo esta congelado como **sandbox funcional minimo Base 1.1**.
- O objetivo atual e validar trilho canonico de entrada e ciclo local minimo de activity.
- Este documento descreve o que **existe hoje** e o que **ainda nao e contrato final**.
- `Run Pipeline / Deactivation / Continuity` permanece direção macro futura da Base 1.1, mas não é pendência ativa deste sandbox enquanto não houver run concreta.

## Base 2.0 checkpoints
- `SA-9B` foi fechado como cleanup local de composition/service locator.
- `SessionActivityHost`, `SessionActivityPipeline` e `ActivityEntryObjectSetupStages` nao usam mais `DependencyManager.Provider` no caminho ativo de `SessionActivity`.
- `IActivityCameraPreparationExecutor` nao passa mais por `SessionActivityPipeline` nem por `TryGet` de bridge.
- `ActivityEntryPipeline` recebe `IActivityCameraPreparationExecutor` por construtor.
- O seam restante `BindEntryPipeline(...)` nao pertence mais ao escopo de `SA-9B`; ele fica registrado como debito proprio de `SA-13`.
- `SA-13` cobre a decomposicao do runtime surface de `Entry` para permitir construcao sem ciclo entre `SessionActivityPipeline` e `ActivityEntryPipeline`.

## Entrada canonica
1. `SessionOperationalPipeline` prepara o handoff.
2. `SessionOperationalPipeline` emite `SessionActivityEntryHandoff`.
3. `SessionActivityPipeline` entra por `StartFromPreparedHandoff`.

Regras de fronteira:
- `DebugStartActivity` e apenas QA/tooling.
- `autoStart` nao e contrato de producao.
- `SessionActivityHost` nao e owner de lifecycle.
- `foreign/stale events` nao podem alterar a activity ativa.

## Ownership atual (implementado)
1. `SessionActivityPipeline` decide ciclo local de:
   - activation;
   - running;
   - pause/resume;
   - completion local (incluindo continue/handoff interno entre activities do catalogo).
2. `SimulationGate` executa block/release de simulacao.
3. `InputModeAdapter` e `PauseOverlayAdapter` executam efeitos observaveis.
4. Adapters/gates nao decidem lifecycle semantico; pipeline decide.

## Estado implementado hoje
1. Activation local existe, com skip/no-content explícito quando aplicável.
2. Running local existe.
3. Pause/resume local existe com gate de simulacao.
4. Completion local existe, inclusive transição local Activity -> Activity.
5. Deactivation local, restart e route-exit estão cobertos pelos rails validados do `SessionActivityPipeline`.
6. `ActivitySetup` v0 já materializa/valida PlayerActor, binda PlayerInput, Movement e ActivityCamera quando houver requisitos aplicáveis.
7. `ActivityObjectSnapshot` MVP existe para contributor de Activity, com contract validation, reset, capture e restore mínimo.

## Fora do contrato final / não ativo neste checkpoint
1. `WindowTemplateLibrary` route-scoped final ainda não substitui o trilho atual de additive window scenes. Classificação congelada: futuro/shape final, não pendência ativa enquanto o v0 additive for suficiente.
2. `PlacementSetupStage` nominal separado não é pendência ativa: o v0 está coberto por `PlayerActorSetup + PlayerActorReset(Placement)`.
3. Progression Save real/genérico permanece fora do escopo ativo até existir progressão concreta de jogo.
4. `Run Pipeline / Deactivation / Continuity` materializado permanece direção macro futura, não backlog curto do sandbox atual.

## Fronteiras normativas
- Run-level deactivation/continuity **nao pertence** a `SessionActivity`; pertence ao `RunPipeline` futuro quando houver run concreta.
- A ausência de `RunPipeline` materializado não é déficit funcional do sandbox atual.
- `SessionActivity` nao salva progression diretamente; capture/restore local é comandado por `SessionActivityPipeline`, enquanto persistência de rota/activity pertence ao `SessionOperationalPipeline`/`SaveRuntime`.
- Enquanto não existir progressão real/genérica, `RouteActivitySave` permanece no MVP validado por ActivityObjectSnapshot.

## Resumo
`SessionActivity` permanece congelada como sandbox funcional Base 1.1: entrada canonica por handoff, ciclo local controlado pelo pipeline e fronteiras explicitas para itens futuros que ainda nao sao contrato final. `Run Pipeline / Deactivation / Continuity` nao e pendencia ativa deste sandbox; e direcao macro futura condicionada a uma run concreta.

## Window AdditiveScene v0

O caminho ativo atual para `ActivationWindow` e `DeactivationWindow` é `ActivityWindowMode.None` ou `ActivityWindowMode.AdditiveScene`.

No modo additive, o pipeline comanda load, aguarda ready, exige complete explícito e descarrega a window scene antes de avançar para `ActivityRunning` ou `ActivityDeactivated`.

Isso é o contrato v0 aceito do sandbox. Não implementar `WindowTemplateLibrary route-scoped` agora sem necessidade concreta de templates compartilhados, standby, payload bind/unbind ou reaproveitamento visual entre Activities.

## SA-13D - fechamento documental

- Status: CLOSED / AUDITED.
- D1 a D7 foram encerrados em auditoria documental, sem patch imediato para PlayerInput, Permission, Movement, Camera, ActivityContent ou RouteActivitySave.
- PlayerInput ainda tem debito futuro de explicit injection de canonical actions.
- Camera ainda tem debito futuro de explicit composition para ActivityCameraAnchorHost.
- Movement retained/control e ActivityContent release/continuation permanecem por alto risco.
- RouteActivitySave atual salva/skipa com base na activity/rota imediatamente anterior concluida, nao em "last useful payload" global.
- Nao criar fallback silencioso para payload antigo sem policy explicita.

## SA-14B1 - fechamento documental

- Status: CLOSED / PASS funcional + PASS arquitetural do corte.
- ActivityObject exit correlation agora é carrier explícito produzido pela entry e commitado pelo SessionActivityPipeline no boundary macro.
- SessionActivityPipeline permanece apenas como commit boundary; ActivityObjectExitRuntimeState continua sink técnico.
- Sem fallback ou reconstruction no exit.
- Stages de exit continuam determinísticos e sem mudança de comportamento.

## SA-14C - fechamento documental

- Status: CLOSED / AUDITED.
- Residual bridge/carrier matrix audited after SA-14B1; no new wrong owner, duplicate owner, fallback silencioso, or new lookup tardio found inside SessionActivity.
- No immediate runtime patch is recommended.
- Main future bridge candidate remains `IActivityEntryContentPendingOperationRuntimeBridge` and `RunActivityContentOperation(..., this)`.
- `IActivityEntryParticipantBindingRuntimeBridge` remains a possible future split candidate.
- `IActivityEntryContentLoadedSetRuntimeBridge` remains a future cleanup candidate.
- `Movement retained/control` and `ActivityContentReleaseRuntimeState` remain high risk.

## SA-14D - ActivityContent pending-operation bridge audit

- Status: CLOSED / AUDITED.
- `IActivityEntryContentPendingOperationRuntimeBridge` remains a technical residual and is deferred.
- `RunActivityContentOperation(..., this)` is a technical callback, not a wrong owner.
- `SessionActivityPipeline` remains the callback boundary through `ISessionActivityPendingOperationCallback`.
- No fallback silencioso, no new lookup tardio, and no duplicate owner were found in the audited path.
- No immediate runtime patch is recommended.
- Any future change in this path requires full smoke validation.

### Backlog futuro

```text
IActivityEntryContentPendingOperationRuntimeBridge split/reduction
IActivityEntryContentLoadedSetRuntimeBridge cleanup
IActivityEntryContentRuntimeBridge aggregate cleanup
```

## SA-14A - status normalization

- Current normalized source of truth: `SA-14E - SessionActivity decomposition closure matrix CLOSED / AUDITED`.
- Keep the earlier roadmap/history for traceability only.
- Keep current future debts limited to: PlayerInput explicit injection, ActivityCameraAnchorHost explicit composition, RouteActivitySave policy gap, Movement high risk, ActivityContent high risk, and optional ActivityObject exit correlation observability hygiene only.
- Do not reopen Movement, ActivityContent, or RouteActivitySave without regression evidence.
- Do not add fallback for old payloads or create `ActivityExitPipeline` / `ActivityContentReleasePipeline` just for symmetry.

## SA-14E - SessionActivity decomposition closure matrix

- Status: CLOSED / AUDITED.
- The current runtime decomposition is frozen as a temporary checkpoint.
- `SA-14B1` remains `CLOSED / PASS funcional + PASS arquitetural do corte`.
- `SA-13D`, `SA-14C` and `SA-14D` remain closed as audits.
- Remaining debts are classified as `DEFER_HIGH_RISK`, `POLICY_GAP`, `FUTURE_CLEANUP_LOW`, `FUTURE_CLEANUP_MEDIUM` and `DO_NOT_REOPEN_WITHOUT_REGRESSION`.

### Final matrix

```text
CLOSED_PASS:
  SA-14B1 - ActivityObject exit correlation explicit entry result

CLOSED_AUDITED:
  SA-13D - Runtime surface audits
  SA-14C - residual bridge / carrier matrix
  SA-14D - ActivityContent pending-operation bridge audit

DEFER_HIGH_RISK:
  Movement retained/control surface
  ActivityContent release/continuation surface
  ActivityContentReleaseRuntimeState

POLICY_GAP:
  RouteActivitySave policy gap: current completed activity vs last useful snapshot payload

FUTURE_CLEANUP_LOW:
  IActivityEntryContentLoadedSetRuntimeBridge cleanup
  IActivityEntryContentRuntimeBridge aggregate cleanup
  ActivityObject exit correlation observability hygiene

FUTURE_CLEANUP_MEDIUM:
  IActivityEntryContentPendingOperationRuntimeBridge split/reduction
  IActivityEntryParticipantBindingRuntimeBridge possible split
  PlayerInput canonical actions explicit injection
  ActivityCameraAnchorHost explicit composition

DO_NOT_REOPEN_WITHOUT_REGRESSION:
  Movement
  ActivityContent
  RouteActivitySave
  SA-14B1 exit correlation production
  ActivityContent release/continuation
  ActivityContent pending-operation callback path
```

### Smoke baseline

```text
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
RestartCurrentActivity
Activity01ToActivity02
RouteExitBackToMenu
ActivityObjectSnapshotCapture, ActivityObjectRelease, ActivityObjectContributorUnregister quando aplicavel
```

### Closure rules

```text
DEFER_HIGH_RISK items must not be reopened without concrete regression evidence.
RouteActivitySave last useful payload is a new policy, not a local bug.
Any future change in the pending-operation callback path requires full smoke validation.
```
