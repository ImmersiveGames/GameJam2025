# LATEST

## Macro baseline consolidation
- o nucleo `GameLoop / PostGame / LevelFlow / Navigation` esta fechado e estabilizado no canon atual
- `Restart`, `RestartFromFirstLevel` e `ExitToMenu` permanecem semanticamente distintos e no owner correto
- `Audio / BGM context` foi consolidado com precedencia final audio-owned
- `Frontend/UI` ficou funcionalmente estavel e passou por reshape tecnico leve guiado pelo Slice 6
- `SceneFlow` segue como monitor-only tecnico, sem correcao local nesta rodada
- o proximo trabalho nao e nova arquitetura central; e housekeeping controlado e residual
- o housekeeping residual prioritario foi concluido e o baseline entra em manutencao controlada
- o backlog restante fica em estado passivo, sem caracter de frente ativa

## EntityAudioSemanticMapAsset audit
- `EntityAudioSemanticMapAsset` permanece scoped a semantica de entidade
- o asset contem apenas `purpose -> cue` com overrides de emissao/execucao/voz e ajuste de follow target/volume/reason
- nao foi encontrada mistura real de contexto de rota, level ou macro ownership
- nao houve necessidade de split ou reducao estrutural nesta fatia da Phase 3

## Phase 4 SceneFlow audit
- `SceneFlow` permanece tecnico e o trilho de transicao continua canonicamente ordenado
- readiness gating continua consistente entre `SceneFlow`, `GameLoop` e `LevelFlow`
- nao foi identificado drift semantico claro que justificasse correcao local nesta rodada
- a Phase 4 aparenta pedir, por ora, apenas auditoria e monitoramento do runtime tecnico

## Phase 5 UI / Presenter audit
- `Frontend/UI` permanece como camada visual local e emissora de intents
- `PostRunMenu` e `PauseMenu` continuam como contextos visuais locais, sem ownership de dominio
- binders e controllers auditores nao carregam ownership de flow, resultado ou readiness
- nao foi identificado drift semantico claro que justificasse correcao local nesta rodada
- a frente esta funcionalmente estavel; o proximo corte e apenas reshape tecnico guiado pelo Slice 6
- o Slice 6 e a referencia ativa para esta frente

## Fase vigente
### Baseline 4.0 - Phase 2 - Core Boundary Cleanup

## Entrega
- fechamento documental do `Slice 8` concluido
- implementacao minima estrutural de `Checkpoint` dentro de `Save` concluida
- `Save/Checkpoint` estacionado no estado atual, sem aprofundamento imediato
- proximo passo recomendado: `Phase 2 - Core Boundary Cleanup`
- modulos foco: `GameLoop`, `PostGame`, `LevelFlow`, `Navigation`

## Phase 2 stance
- a Phase 2 esta operando em modo canon-first, com rewrite/refactor canonico permitido
- a primeira fatia `GameLoop` x `PostGame` ja seguiu essa regra de forma deliberada
- isso nao e regressao de processo; e a postura estrutural esperada para a fase

## Restart contract
- o contrato de restart foi estabilizado e validado em runtime
- `Restart` = current level/context
- `RestartFromFirstLevel` = first canonical level from catalog
- a distinção pertence ao trilho de `LevelFlow` e nao deve colapsar de novo no mesmo caminho

## ExitToMenu closure
- a frente `RunResult / Restart / ExitToMenu` esta fechada nesta Phase 2
- `ExitToMenu` segue o trilho canonico `PostGame -> LevelFlow -> Navigation`
- o proximo foco da limpeza de fronteira passa a ser `LevelFlow x Navigation`

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

## Residual housekeeping inventory
- arquivos fisicos inertes em `Modules/GameLoop/Interop` foram removidos
- alguns docs historicos ainda carregam nomes legados por registro, nao por contrato ativo
- remanesce housekeeping leve de naming/namespace e eventual doc cleanup superado
- nenhum desses itens reabre ownership ou fronteira central

## Regra
Nesta fase, o foco atual do Baseline 4.0 e a limpeza de fronteiras centrais; `Save/Checkpoint` permanece estacionado e nao e o foco imediato.
