# Baseline 4.0 Residual Housekeeping Audit

## Objective
Catalogar apenas o backlog tecnico residual de baixo risco depois do fechamento das frentes centrais do Baseline 4.0, sem reabrir ownership, arquitetura central ou trilhos canonicos ja estabilizados.

## Closed Fronts
- `GameLoop / PostGame / LevelFlow / Navigation` estabilizados
- `Restart`, `RestartFromFirstLevel` e `ExitToMenu` fechados no canon atual
- `Audio / BGM context` consolidado com precedencia final audio-owned
- `Frontend/UI` funcionalmente estavel e reshape tecnico leve concluido

## Monitor-Only Fronts
- `SceneFlow` permanece monitor-only tecnico
- sem correcao local nesta rodada para readiness gating ou sync runtime

## Residual Technical Housekeeping

| Item | Priority | Reason |
|---|---|---|
| `Modules/GameLoop/Interop/ExitToMenuCoordinator.cs` | Done | placeholder fisico removido sem impacto funcional |
| `Modules/GameLoop/Interop/MacroRestartCoordinator.cs` | Done | placeholder fisico removido sem impacto funcional |
| docs historicos com nomes legados | Can wait | sao registros historicos, nao contrato ativo |
| nomes/namespace residuais fora do `Frontend/UI` ja reshaped | Archive only | nao alteram ownership e nao bloqueiam runtime |
| `AudioBgmService` TODO de defaults/preview | Do not touch now | pertence a refinamento de audio, nao a housekeeping leve |

## Items Explicitly Not To Reopen
- `Save`
- `Checkpoint`
- `Audio`
- `SceneFlow`
- `GameLoop / PostGame / LevelFlow / Navigation`
- `NavigationLevelRouteBgmBridge` como semantica de ownership
- qualquer nova fronteira arquitetural ampla

## Recommended Next Small Cuts
1. Normalizar somente os pontos de doc historica que ainda confundem leitura, sem reescrever historico.
2. Deixar o restante em archive only ate haver outro conjunto de mudancas locais no mesmo modulo.
