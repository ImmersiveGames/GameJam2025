# ADR-0045 - Gameplay Runtime Composition como centro semantico do gameplay

> STATUS NORMATIVO: HISTORICO - ANTECEDENTE DA BASE 1.0, NAO FONTE NORMATIVA PRIMARIA.
> Em conflito, prevalecem ADR-0057, ADR-0056, ADR-0055, ADR-0058, ADR-0054 e ADR-0052.

## Status
- Estado: Aceito
- Data: 2026-04-03
- Tipo: Direction / Canonical architecture

## Contexto

O backbone atual segue necessario e forte para:
- boot
- SceneFlow
- Fade/Loading
- gates
- reset e materializacao operacional
- estado macro transversal

Mas ele nao deve continuar carregando a semantica principal do gameplay.
O jogo ainda aparece fragmentado em WorldDefinition, LevelManager, reset/retry/restart e handoffs espalhados; restart, porem, nasce como intencao downstream e nao como ownership do backbone.
A linguagem final do projeto nao deve girar em torno de swap de conteudo.

## Decisao

O centro semantico do gameplay passa a ser o **Gameplay Runtime Composition**.

Nota de escopo: a camada acima do baseline para transformacao composta de sessao/runtime e documentada em `ADR-0052`; este ADR permanece valido para o centro semantico do gameplay, mas nao governa a composicao acima do baseline.

Esse subsistema e o ponto de leitura canonica para o V1 ja consolidado:
- `SessionContext`
- `PhaseRuntime`
- `Players`
- `Prepare`
- `Intro`
- `Playing`
- `Outcome`
- `RunEndIntent`
- `RunResultStage`
- `RunDecision`
- `Continuity` como sinal downstream de fechamento da run, nao como ownership da ordem entre phases

## Relacao com os ADRs seguintes

Este ADR fecha a direcao macro e congela o V1 ja exercitado no runtime.

- `ADR-0046` fecha o primeiro bloco interno do centro semantico.
- `ADR-0047` fecha o pipeline minimo de construcao de fase dentro desse bloco.

## O que fica no backbone

O backbone continua dono de:
- boot
- SceneFlow
- Fade/Loading
- gates
- GameLoop macro
- reset e materializacao operacional
- estado macro transversal

O backbone continua existindo e continua necessario.
Ele deixa de ser o lugar onde mora a semantica principal do gameplay.

## O que sobe para o Gameplay Runtime Composition

Sobem para o novo centro semantico ja consolidado no V1:
- `SessionContext`
- `PhaseRuntime`
- `Players`
- `Prepare`
- `Intro`
- `Playing`
- `Outcome`
- `RunEndIntent`
- `RunResultStage`
- `RunDecision`
- `Continuity` como linguagem de fechamento downstream, nao como ownership de progressao entre phases

## O que passa a ser linguagem historica

Passam a ser lidos como historicos, transitivos ou de menor peso semantico:
- `WorldDefinition` como fundacao final do jogo
- `LevelManager` como eixo separado de mesmo peso
- linguagem central baseada em swap de conteudo
- reset como capacidade de execucao do backbone/orchestration; restart como intencao downstream do gameplay runtime
- `Rules/Objectives` e `InitialState` nao fazem mais parte do canônico de `Phase`.

## Consequencias praticas

- `WorldDefinition` nao deve virar base final do jogo.
- `LevelManager` nao deve permanecer como eixo semanticamente autonomo.
- retry / restart / respawn / advance passam a nascer da semantica do gameplay.
- o backbone permanece como executor tecnico, nao como dono do significado do gameplay.
- a leitura dos modulos deve seguir a composicao de gameplay, nao a forma historica da pasta.
- a ordem entre phases e a progressao editorial ficam no catalogo de `PhaseDefinition`, enquanto `GameplaySessionFlow` apenas consome a phase ja resolvida.

O owner documental do fim de run dentro desse centro e `ADR-0049`; este ADR define apenas a direcao macro.

## Proximos passos arquiteturais imediatos

1. Tratar o runtime V1 como base consolidada, nao mais como corte em definicao.
2. Evoluir para o modelo de authoring/configuration da fase.
3. Revisar `Modules/GameLoop`, `Modules/PostRun` e `Modules/LevelFlow` apenas sob essa leitura ja congelada.
