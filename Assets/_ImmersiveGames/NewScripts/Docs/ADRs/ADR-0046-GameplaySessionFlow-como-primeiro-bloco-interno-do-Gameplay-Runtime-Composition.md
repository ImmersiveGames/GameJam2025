# GameplaySessionFlow como primeiro bloco interno do Gameplay Runtime Composition

## 1. Resumo executivo

`GameplaySessionFlow` e o primeiro bloco semantico interno acima do backbone dentro de `Gameplay Runtime Composition`.
Ele fecha a fronteira da sessao jogavel ja consolidada no runtime: entrada, preparacao do level, outcome, fim de run e operacoes de retry/restart/advance ficam lidos a partir deste bloco, sem reatribuir ao backbone a semantica principal do gameplay.
O owner documental do fim de run dentro desse centro e `ADR-0049`.

## 2. Papel do bloco

O papel de `GameplaySessionFlow` e concentrar a orquestracao da sessao ativa do jogo.
Ele existe para tornar explicita a leitura canonica da experiencia jogavel, separando:
- infraestrutura operacional do backbone
- composicao semantica da sessao jogavel
- pontes temporarias que ainda conectam os dois lados

## 3. Fronteira

A fronteira do bloco e a faixa da sessao jogavel que vai da entrada na experiencia ativa ate o fechamento do fim de run e das intencoes de continuidade da run.

Entra no bloco o que define a sessao jogavel.
Fica fora o que apenas executa suporte tecnico do runtime, transporte de cena, navegacao ou reset operacional.

## 4. O que entra no primeiro corte

### Ownership semantico do bloco

`GameplaySessionFlow` e o dono da orquestracao da sessao ativa do jogo.
Ele concentra a leitura canonica da experiencia jogavel e separa:
- infraestrutura operacional do backbone
- composicao semantica da sessao jogavel
- pontes temporarias que ainda conectam os dois lados

### Pecas atuais e transitórias reaproveitadas

As pecas abaixo continuam sendo reaproveitadas como suporte operacional abaixo do ownership semantico:
- `LevelMacroPrepareService`
- `LevelStageOrchestrator` como ponte de sequencing
- `GameRunOutcomeService`
- `PostRunHandoffService`
- `PostRunOwnershipService`
- `PostRunResultService`
- `RestartContextService`
- `LevelFlowRuntimeService` como leitura interna de runtime de level dentro da sessao

O modelo canonico alvo do fim de run e `RunEndIntent -> RunResultStage -> RunDecision`, documentado em `ADR-0049`.
Os nomes `PostRun*` acima permanecem apenas como bridges historicas da ponte atual, nao como centro semantico do rail final.

## 4.1 Contrato semantico minimo da V1

O V1 ja consolidado fecha estes eixos semanticos obrigatorios:
- `SessionContext`
- `PhaseRuntime`
- `Players`
- `Rules/Objectives`
- `InitialState`

Esses eixos sao o minimo necessario para dizer que a fase foi montada em termos de conjunto, e nao apenas preparada em termos tecnicos.

## 5. O que fica fora

Ficam fora deste corte:
- `SceneFlow`
- `WorldReset`
- `Navigation`
- `Loading/Fade`
- `GameLoopService`
- `GameLoopStateMachine`

Tambem ficam fora como responsabilidade semantica:
- transporte de cena
- politica de navegacao
- executor tecnico de reset
- transicoes visuais e readiness de plataforma

## 6. Relacao com backbone e eixos futuros

O backbone continua como executor tecnico e dono do fluxo macro, mas nao como dono do significado central do gameplay.

Relacao futura com os eixos internos:
- `level runtime`: passa a ser parte interna do `GameplaySessionFlow`, nao eixo separado de mesmo peso
- `players`: entram como participantes da sessao ativa, com ownership semantico do bloco
- `objetivos/estado de fase`: ficam sob a composicao da sessao, incluindo resultado, progresso local e continuidade entre fases

As pecas listadas acima sao meios de execucao e transicao do corte atual, nao a definicao canonica do bloco.
O cleanup explicito de `Rules/Objectives` e `InitialState` no fim da run tambem faz parte do bloco consolidado.

## 7. Bridges transitorias

Continuam como bridges transitorias por enquanto:
- `GameLoopSceneFlowSyncCoordinator`
- `GameLoopInputCommandBridge`
- `GameRunEndedEventBridge`
- `GameLoopCommands`
- `GameRunOutcomeRequestBridge`
- `GameLoopStartRequestEmitter`

Essas pecas seguem existindo apenas ate a fronteira semantica ser absorvida pelo novo bloco.
Elas nao devem virar o novo centro de ownership.

## 8. Fechamento

Com este documento, `GameplaySessionFlow` fica formalmente definido como o primeiro bloco interno do `Gameplay Runtime Composition`.
O proximo passo arquitetural e evoluir para o modelo de authoring/configuration da fase sem reabrir a decisao macro nem a fronteira agora fechada.
