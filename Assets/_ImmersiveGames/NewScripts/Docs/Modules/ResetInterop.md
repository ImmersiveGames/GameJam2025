# ResetInterop

## Precedencia canonica
- Fonte de verdade operacional: `ADR-0030`, `ADR-0031`, `ADR-0032`, `ADR-0033`.
- Em conflito, esta linha prevalece sobre historico obsoleto.

## Estado atual
- `ResetInterop` concentra a ponte entre `SceneFlow` e `WorldReset` no trilho macro.
- O modulo reune driver, gate e tokens publicos de correlacao para a transicao macro.
- `SceneResetFacade` continua em `SceneReset`, como compat historica separada.

## Ownership
- `SceneFlowWorldResetDriver`: handoff `SceneFlow/ScenesReady -> WorldResetService` com guard/dedupe de assinatura.
- `WorldResetCompletionGate`: gate de completion consumido pelo pipeline macro do `SceneFlow`.
- `WorldResetTokens`: tokens publicos de reset.
- Eventos de contrato consumidos/publicados no trilho: `WorldResetStartedEvent` e `WorldResetCompletedEvent`.

## Regras praticas
- `ResetInterop` nao e owner do reset macro (owner: `WorldReset`).
- `ResetInterop` nao e owner do reset local de cena (owner: `SceneReset`).
- `ResetInterop` nao e owner de semantica local de level (owner: `LevelLifecycle`).
- `ResetInterop` nao define policy macro de reset (policy permanece em `WorldReset`).
- O gate existe para correlacao e liberacao do fim macro; nao redefine ownership da timeline do `SceneFlow`.
- `Orchestration/LevelFlow/Runtime` ainda existe por compat de transicao, mas nao e owner do reset macro.

## Boundary com completion gate
- `WorldResetCompletionGate` aguarda `WorldResetCompletedEvent` correlacionado por `ContextSignature`.
- O gate e parte do contrato entre `ScenesReady` e `BeforeFadeOut`.
- Caminhos de fallback/degraded no driver devem permanecer explicitos em log e restritos a desbloqueio do trilho macro quando necessario.

## Leitura cruzada
- `Docs/Modules/WorldReset.md`
- `Docs/Modules/SceneReset.md`
- `Docs/Modules/SceneFlow.md`