# ADR — Ciclo de Vida do Jogo (Reset por Escopos + Gate)

**Data:** 2025-12-25
**Status:** Ativo / Aplicado em produção

## Contexto
O NewScripts precisa de um fluxo de reset determinístico para garantir que o mundo de gameplay
seja reconstruído de forma previsível durante transições de cena e reinícios de partida.
Esse fluxo deve ser independente de UI e se integrar ao SceneFlow sem acoplamento indevido.

## Decisão
- O reset do mundo é feito por **escopos** (`ResetScope` + `IResetScopeParticipant`).
- O reset é executado em fases determinísticas pelo `WorldLifecycleOrchestrator`.
- O pipeline utiliza **gate de simulação** via `ISimulationGateService` para bloquear ações durante reset.

## Consequências
- A conclusão do reset é sinalizada por `WorldLifecycleResetCompletedEvent`, que destrava o SceneFlow.
- O gate de simulação garante que gameplay não execute ações enquanto o reset ainda está em andamento.
- O GameLoop e o InputMode dependem do reset concluído para avançar para gameplay jogável.

## Confirmação com o fluxo atual
- O reset por escopos com gate (`SimulationGateService`) está coerente com o log de produção:
  o reset ocorre antes do `FadeOut` e só então a transição é concluída.

## Atualização (2025-12-29)
- **Perfis frontend** (startup/menu) podem sinalizar `WorldLifecycleResetCompletedEvent` via **skip**
  quando não há mundo de gameplay a ser resetado.
- **Perfis gameplay** disparam o reset completo **antes** da conclusão da transição,
  garantindo estado determinístico antes de liberar o fade-out e o `InputMode`.
