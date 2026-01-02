# Baseline Invariants (NewScripts)

Este documento lista os invariantes que o **Baseline Matrix 2.0** exige.

> “Invariante” aqui significa: se isso quebrar, o pipeline perdeu previsibilidade e deve falhar alto.

## I1) SceneTransitionStarted fecha o gate de transição

- Evento: `SceneTransitionStartedEvent`
- Token esperado: `SimulationGateTokens.SceneTransition` (`flow.scene_transition`)

**Regra**
- Ao observar `SceneTransitionStartedEvent`, o token `flow.scene_transition` deve ficar ativo durante a transição.
- Ao observar `SceneTransitionCompletedEvent`, o token deve estar liberado.

**Por quê**
- Garante que lógica de gameplay não roda durante load/unload/ativação.

## I2) ScenesReady sempre acontece antes de Completed

- Eventos: `SceneTransitionScenesReadyEvent` e `SceneTransitionCompletedEvent`

**Regra**
- Para uma mesma `ContextSignature`, `ScenesReady` deve ocorrer antes de `Completed`.

## I3) ResetCompleted sempre chega antes do FadeOut

- Eventos: `WorldLifecycleResetCompletedEvent` e `SceneTransitionBeforeFadeOutEvent`

**Regra**
- Para uma mesma `ContextSignature`, `WorldLifecycleResetCompletedEvent` deve ocorrer antes de `SceneTransitionBeforeFadeOutEvent`.

**Observação**
- Em `startup`/frontend, o reset pode ser SKIP, mas o evento **ainda deve** ser emitido.

## I4) GameLoop só libera gameplay quando Playing

- Evento: `GameRunStartedEvent`

**Regra**
- `GameRunStartedEvent` deve ser emitido apenas quando `StateId == GameLoopStateId.Playing`.

## I5) GameRunEndedEvent no máximo 1x por run

- Evento: `GameRunEndedEvent`

**Regra**
- Entre um `GameRunStartedEvent` e o próximo, `GameRunEndedEvent` pode ocorrer no máximo uma vez.

## I6) Pause/Resume mantém token state.pause coerente

- Eventos: `GamePauseCommandEvent`, `GameResumeRequestedEvent`
- Token esperado: `SimulationGateTokens.Pause` (`state.pause`)

**Regra**
- Ao pausar: token `state.pause` deve se tornar ativo.
- Ao resumir: token deve ser liberado.

---

## Instrumentação recomendada (opcional)

Para tornar essas invariantes “auto-fail”, use um asserter opt-in (dev/QA) que escuta os eventos e valida a ordem/tokens.

- Implementação sugerida: `BaselineInvariantAsserter` (ver `NewScripts/Infrastructure/Baseline/`).
