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
- Camera explicit composition para ActivityCameraAnchorHost foi fechado em SA-16E1.
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
- `SA-17A` closed the pending-operation bridge dispatch split; `RunActivityContentOperation(...)` now lives in the runner boundary, not in the entry bridge.
- `SA-17B` closed the `LoadedSet` bridge cleanup; the runtime state now owns direct store/clear.
- `SA-17C` closed the aggregate `ActivityContent` bridge cleanup; no substitute bridge was introduced.
- `SA-17D` closed the activity-object exit correlation observability hygiene cut; `ActivityObjectExitRuntimeState` is the technical owner and `SessionActivityPipeline` stays only the macro ordering/freeze owner.
- `SA-17D-FIX` preserved `NoActivityContentContributors / no_activity_content_contributors` for `activity_02`, and `SnapshotPayloadExpectedButMissing` remained reserved for the expected-contributor-failed case.
- `SA-18A7-FIX7-DOC` recorded the validated closure note for retained PlayerActor rebind plus permission scanner guard closure, without changing runtime ownership or the no-content route save path.
- `SA-18A8-A9-DOC` recorded the participant-binding bridge residual cleanup closure; placement marker lookup and participation context store now have separate runtime owners outside the bridge.
- `IActivityEntryParticipantBindingRuntimeBridge` remains a possible future split candidate.
- `Movement retained/control` and `ActivityContentReleaseRuntimeState` remain high risk.

## SA-14D - ActivityContent pending-operation bridge audit (histórico)

- Status: CLOSED / AUDITED (historical; superseded by SA-17A).
- Historicamente, `IActivityEntryContentPendingOperationRuntimeBridge` cobria apenas build/set state registration antes de `SA-17A`.
- `RunActivityContentOperation(...)` is owned by `ISessionActivityPendingOperationRunner` with `SessionActivityPipeline` as callback boundary.
- `SessionActivityPipeline` remains the callback boundary through `ISessionActivityPendingOperationCallback`.
- No fallback silencioso, no new lookup tardio, and no duplicate owner were found in the audited path.
- No immediate runtime patch is recommended.
- Any future change in this path requires full smoke validation.

### Backlog futuro

```text
RouteActivitySave policy gap: current completed activity vs last useful snapshot payload
```

## Status canônico

O resumo atual de `SessionActivity` fica em:

- `Docs/Reports/SessionActivity-2.0-Current-Status.md`

Este README mantém apenas a documentação operacional do pipeline.
O histórico de corte continua no ADR e nos relatórios específicos.

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
