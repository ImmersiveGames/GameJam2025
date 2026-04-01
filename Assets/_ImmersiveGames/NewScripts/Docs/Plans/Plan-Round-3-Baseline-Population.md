# Plan - Round 3 - Baseline Population

## Base canonica

Esta rodada parte do freeze do backbone e do freeze da rodada 2.  
Fonte de leitura obrigatoria:

- `Docs/ADRs/ADR-0001-Glossario-Fundamental-Contextos-e-Rotas-v2.md`
- `Docs/ADRs/ADR-0043-Ancora-de-Decisao-para-o-Baseline-4.0.md`
- `Docs/ADRs/ADR-0044-Baseline-4.0-Ideal-Architecture-Canon.md`
- `Docs/Plans/Blueprint-Baseline-4.0-Ideal-Architecture.md`
- `Docs/Plans/Plan-Baseline-4.0-Execution-Guardrails.md`
- `Docs/Plans/Plan-Round-2-Object-Lifecycle.md`
- `Docs/Reports/Audits/2026-04-01/Round-2-Freeze-Object-Lifecycle.md`

## Fronteira

### Baseline / fundacao

- espinha conceitual e ownership canonico;
- trilhos de lifecycle, reset, spawn e observabilidade ja congelados;
- contratos que definem como o runtime aceita e reconstitui objetos;
- fronteiras entre `GameLoop`, `PostRun`, `LevelFlow`, `SceneFlow`, `Navigation` e `ResetInterop`.

### Populacao do jogo

- conteudo concreto que ocupa o baseline;
- `player`, `enemies` e demais objetos programaticos de gameplay;
- configuracao e composicao de entidade em nivel/local de jogo;
- instancias que entram em cena via contratos ja congelados.

## Eixos da rodada 3

1. Populacao por tipo: `player`, `enemies`, objetos programaticos.
2. Fronteira de ownership: o que e baseline e o que e instancia de gameplay.
3. Leitura canonica de nomes: evitar reutilizar termos de backbone para conteudo.
4. Mapa de consumo: quem registra, quem reconstitui, quem observa.

## Entra

- classificacao de objetos de gameplay por papel canonico;
- delimitacao de `player` como populacao gameplay-owned;
- delimitacao de `enemies` como populacao gameplay-owned;
- delimitacao de objetos programaticos de gameplay como conteudo instanciado;
- relacao desses objetos com reset, spawn, registry e observabilidade ja congelados;
- revisao de nomes onde o doc ainda mistura owner, runtime e conteudo.

## Fora

- reabrir backbone, round 1 ou round 2;
- propor codigo, adapters ou workarounds;
- mexer em navigation, post-run, scene flow ou audio como alvo principal;
- redesenhar pooling, reset macro, loop de jogo ou pipeline de transicao;
- criar nova taxonomia fora do glossario canonico;
- usar `Gameplay` como owner amplo de tudo que e content-driven.

## Conflitos vivos nos docs ativos

- `Gameplay` ainda aparece amplo demais e entra em choque com `Orchestration/LevelLifecycle` e `Game/Content/Definitions/Levels`.
- `Navigation` ainda descreve `Restart`/`ExitToMenu` em termos que encostam em `LevelLifecycle`.
- `GameLoop` e `PostRun` ainda dividem a borda do pos-run com nomes diferentes para o mesmo handoff.
- `PostPlay` e `WorldLifecycle` seguem como nomenclatura residual em docs, enquanto o runtime ativo usa `PostRun`, `WorldReset` e `SceneReset`.

## Plano enxuto

1. Fechar a definicao de baseline vs. populacao para esta rodada.
2. Inventariar apenas os grupos de gameplay da rodada: `player`, `enemies` e objetos programaticos.
3. Classificar cada grupo por ownership, entrada no runtime e participacao em reset/spawn.
4. Resolver conflitos de nomenclatura ativos apenas onde eles atrapalham essa classificacao.
5. Fechar a rodada com uma leitura unica e curta de populacao de gameplay sobre o baseline congelado.

## Aceite

- a fronteira baseline/populacao fica inequivoca;
- nada do backbone ou da rodada 2 e reaberto;
- o plano permanece limitado a conteudo de gameplay e seus nomes.
