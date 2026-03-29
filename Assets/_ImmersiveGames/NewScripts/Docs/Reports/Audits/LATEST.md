# LATEST

## Fase vigente
### Baseline 4.0 - Phase 2 - Core Boundary Cleanup

## Entrega
- fechamento documental do `Slice 8` concluído
- implementação mínima estrutural de `Checkpoint` dentro de `Save` concluída
- `Save/Checkpoint` estacionado no estado atual, sem aprofundamento imediato
- próximo passo recomendado: `Phase 2 - Core Boundary Cleanup`
- módulos foco: `GameLoop`, `PostGame`, `LevelFlow`, `Navigation`

## Phase 2 stance
- a Phase 2 está operando em modo canon-first, com rewrite/refactor canônico permitido
- a primeira fatia `GameLoop` x `PostGame` já seguiu essa regra de forma deliberada
- isso não é regressão de processo; é a postura estrutural esperada para a fase

## Restart contract
- o contrato de restart foi estabilizado e validado em runtime
- `Restart` = current level/context
- `RestartFromFirstLevel` = first canonical level from catalog
- a distinção pertence ao trilho de `LevelFlow` e não deve colapsar de novo no mesmo caminho

## ExitToMenu closure
- a frente `RunResult / Restart / ExitToMenu` está fechada nesta Phase 2
- `ExitToMenu` segue o trilho canônico `PostGame -> LevelFlow -> Navigation`
- o próximo foco da limpeza de fronteira passa a ser `LevelFlow x Navigation`

## LevelFlow x Navigation
- `Navigation` despacha a macro route de gameplay e nao escolhe level
- `LevelFlow` e owner da selecao explicita e da politica default no `LevelPrepare`
- o log de entrada em gameplay foi ajustado para refletir essa separacao sem ambiguidade

## Central frontier closure
- a frente central da `Phase 2` foi fechada: `GameLoop / PostGame / LevelFlow / Navigation`
- `Restart`, `RestartFromFirstLevel` e `ExitToMenu` permanecem estabilizados no canon atual
- a proxima frente relevante passa a ser `Audio / BGM context ownership cleanup`

## BGM audit
- o contexto inicial de BGM nasce no dispatch da macro route, via `Navigation` + `SceneFlow`
- o contexto final de BGM e confirmado quando `LevelFlow` publica a selecao do level e a aplicacao local
- `NavigationLevelRouteBgmBridge` atua como seam de reconciliacao canonica, nao como owner de audio
- a correcao tardia existe por necessidade de confirmacao do level, e nao como smell de fronteira neste momento

## Regra
Nesta fase, o foco atual do Baseline 4.0 é a limpeza de fronteiras centrais; `Save/Checkpoint` permanece estacionado e não é o foco imediato.
