# Plan - Round 5 - Gameplay Population Model

## Resumo

Esta rodada inicia a modelagem da futura populacao de gameplay sobre a baseline ja fechada.
O boundary canonico de entrada ja existe; agora a modelagem define como o conteudo futuro deve ser lido, sem tratar placeholders e mocks como forma final.

## Objetivo da Rodada

- definir a leitura arquitetural da populacao futura de gameplay;
- manter o boundary de entrada separado do runtime operacional;
- separar conteudo, definicao/configuracao, instancia runtime e comportamento operacional;
- preservar backbone, round 2, round 3 e round 4 congelados.

## Leitura Canonica da Populacao de Gameplay

A populacao de gameplay e o conteudo que a baseline recebe para compor a experiencia jogavel futura.
Ela nao e o boundary de entrada, nao e o spawn operacional e nao e o registro runtime.

Leitura canonica:

- `Level` continua sendo o contexto onde essa populacao entra e opera;
- o `Entry Boundary` declara o que pode entrar;
- a baseline materializa e observa;
- o conteudo de gameplay fornece identidade, presenca e comportamento dentro do level.

## Classificacao Inicial

Classificacao inicial e enxuta:

1. `Core gameplay content`: conteudo principal da experiencia futura, como `player` e inimigos.
2. `Support gameplay content`: conteudo auxiliar de gameplay que participa da experiencia, sem virar taxonomia final.
3. `Prototype content`: `Dummy`, placeholders e mocks usados apenas como evidencia do estado atual.

Separacao funcional:

- `Conteudo de gameplay` = o que sera populado no jogo futuro;
- `Definicao/Configuracao` = dado declarativo que descreve o conteudo;
- `Instancia runtime` = forma viva materializada pela baseline;
- `Comportamento operacional` = regras de spawn, reset, registro e observabilidade ja congeladas.

## Relacao com os Trilhos da Baseline

- `Entry Boundary` recebe a declaracao da populacao futura.
- `Definition` descreve o conteudo que pode entrar.
- `Materialization` converte a declaracao em instancia runtime.
- `Registry` reconhece e acompanha a instancia viva.
- `Reset/Reconstitution` retira e restaura a instancia no ciclo da run.
- `Observability` publica a entrada, o ciclo e a saida sem assumir ownership do conteudo.

Relacao com `player` e inimigos:

- entram como conteudo de gameplay, nao como modelagem final de objeto;
- dependem do level para contexto e do boundary para permissao de entrada;
- sao materializados operacionalmente pela baseline;
- devem ser registrados como instancias vivas;
- participam de reset/reconstituicao conforme o ciclo do level e da run;
- sao observados por eventos e sinais do runtime, nao por inferencia solta.

## O que Fica Fora

- reabrir backbone, round 2, round 3 ou round 4;
- redesenhar `GameLoop`, `SceneFlow`, `Navigation`, `PostRun` ou `Save`;
- migrar legado de `Scripts`;
- definir taxonomia final de objetos, entidades ou componentes;
- transformar `Dummy`, `Mock` ou placeholder em modelagem final;
- detalhar comportamento completo de cada futuro tipo de gameplay.

## Criterios de Aceite

1. A populacao futura de gameplay passa a ser lida como conteudo, nao como infraestrutura.
2. O `Entry Boundary` continua separado de `Materialization`, `Registry`, `Reset/Reconstitution` e `Observability`.
3. `player` e inimigos entram como conteudo principal, sem fechar taxonomia excessiva.
4. `Dummy` e mocks permanecem como suporte de prototipo, nao como forma final.
5. O nivel continua sendo o contexto de operacao da populacao, sem reabrir a arquitetura de backbone.

## Decisao Final

A modelagem inicial da populacao de gameplay fica aprovada como conteudo futuro declarado pelo boundary canonico, materializado pela baseline e ainda aberto apenas no nivel necessario para prototipo, sem taxonomia final nem mudanca de trilhos congelados.
