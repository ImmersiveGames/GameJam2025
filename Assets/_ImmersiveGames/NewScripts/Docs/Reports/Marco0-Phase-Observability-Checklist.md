# Marco 0 — Baseline de Observabilidade (Checklist)

> Evidências esperadas apenas via logs (grep). Ordem aproximada: início do runner, transição, reset solicitado, conclusão do reset, fim do runner.

## Checklist (8–15 itens)

1. Verificar início do QA runner (opt-in): `MARCO0_START`.
2. Confirmar solicitação de navegação do QA (se IGameNavigationService disponível).
3. Verificar log de início de transição de cena (ex.: `[SceneFlow] Iniciando transição:`).
4. Verificar evento de cenas prontas (ex.: `[WorldLifecycle] SceneTransitionScenesReady recebido`).
5. Verificar assinatura-base de reset solicitado:
   - `[OBS][Phase] ResetRequested`
6. Verificar linha canônica de reset solicitado (já existente):
   - `[WorldLifecycle] Reset REQUESTED. reason=`
7. Verificar que o reset foi executado ou explicitamente pulado:
   - `[WorldLifecycle] Disparando hard reset` **ou** `[WorldLifecycle] Reset SKIPPED`
8. Verificar evento de reset concluído:
   - `WorldLifecycleResetCompletedEvent`
9. Verificar conclusão da transição de cena:
   - `[SceneFlow] Transição concluída com sucesso.`
10. Verificar log do coordenador GameLoop após reset/transition:
   - `[GameLoopSceneFlow] WorldLifecycle reset concluído (ou skip)`
11. Verificar log de sincronização do GameLoop:
   - `RequestStart()` **ou** `RequestReady()`
12. Verificar fim do QA runner: `MARCO0_END`.

## Assinaturas-base (const string)

- `[OBS][Phase] PhaseRequested`
- `[OBS][Phase] ResetRequested`
- `[OBS][Phase] PreRevealStarted`
- `[OBS][Phase] PreRevealCompleted`
- `[OBS][Phase] PreRevealSkipped`
- `[OBS][Phase] PreRevealTimeout`
