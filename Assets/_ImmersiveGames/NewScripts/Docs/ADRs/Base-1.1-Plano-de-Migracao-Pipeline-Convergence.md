# Base 1.1 - Plano de migracao: Pipeline Convergence

## Status
- Checkpoint concluido: `Base11Sandbox Minimal Route + Session Activity Cycle - PASS`

## O que ja foi congelado

- rota operacional minima por asset direto;
- handoff operacional -> activity com identidade separada;
- executor fisico isolado do owner de pipeline;
- `InputMode` real adiado;
- `ActorsSystem`, `GameLoop`, `IntroStage`, `RunEndRail`, `WorldReset` e `Save` fora do profile minimo.

## Proximos temas possiveis

1. Auditar gates e safe executors.
2. Manter `InputMode` deferred ate o target real ser decidido.
3. Evoluir `SessionActivityPipeline`.
4. Documentar `transitions` / `fade` / `loading` como fora do checkpoint.
5. Auditar residuos de UI/legacy apenas se entrarem no trilho ativo.

## Regra de sequenciamento

- nao implementar novos rails enquanto o checkpoint congelado nao exigir.
- nao criar Base 2.0 aqui.
- nao generalizar `SessionOperationalRouteAsset` para sistema universal.
