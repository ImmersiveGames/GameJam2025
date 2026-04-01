# Plan - Round 2 Object Lifecycle

## 1. Objetivo do plano

Este plano congela a rodada 2 do projeto.
Ele parte do freeze da rodada 1 do backbone e organiza a evolucao do lifecycle de objetos de jogo com ownership claro.
Nao reabre decisoes do backbone nem serve como brainstorming.

## 2. Base congelada

A rodada 1 do backbone ja esta concluida e congelada.
Este plano assume como canon:

- `Spawn` materializa e atribui identidade.
- `ActorRegistry` e diretorio runtime dos vivos.
- `ActorSpawnCompletedEvent` e o marco canonico de observabilidade segura.
- `SceneReset` executa reset local.
- `WorldReset` decide reset macro.
- `GameplayReset` fica restrito a `cleanup / restore / rebind`.
- binders, UI e consumidores externos reagem ao lifecycle sem governar o backbone.

## 3. Escopo da rodada 2

| Eixo | Entra agora? | Objetivo | O que fica fora |
|---|---|---|---|
| Object Lifecycle | sim | explicitar materializacao, identidade, registro, observabilidade, cleanup e reconstituicao | redesign amplo do backbone |
| Spawn Rail | sim | consolidar o rail canônico de spawn em torno de ownership e observabilidade | reabrir a rodada 1 |
| Actor Consumption | sim | definir consumo seguro por binders, UI e observadores externos | ownership de run ou reset |
| Gameplay Object Ownership | sim | deixar categorias e donos de objetos vividos explicitos | criar novos mods de runtime sem necessidade |
| Pooling Future-Ready | sim | preparar o desenho para reuse sem contaminar a semantica | integrar pooling completo agora |

## 4. Leitura arquitetural alvo da rodada 2

- `Spawn` e o owner da materializacao e da identidade.
- `ActorRegistry` representa existencia e consulta runtime, nao readiness.
- `ActorSpawnCompletedEvent` e o marco para observabilidade segura.
- `SceneReset` continua sendo executor local de reset, nao owner do nascimento.
- `WorldReset` segue como owner macro da decisao.
- `GameplayReset` continua limitado a cleanup, restore e rebind de comportamento.
- binders e consumidores externos observam objetos apos o spawn concluido, nao por inferencia de `Awake` ou `OnEnable`.
- pooling entra apenas como backend futuro de reuse, sem alterar a semantica do gameplay.

## 5. Roadmap da rodada 2

| Ordem | Nome do corte | Objetivo curto | Risco | Dependencias | Status inicial |
|---|---|---|---|---|---|
| 1 | `Ownership Taxonomy` | explicitar categorias de objeto e dono runtime | baixo | freeze da rodada 1 | concluido |
| 2 | `Actor Consumption Contract` | definir observacao segura por binders/UI/consumidores | baixo / medio | corte 1 | concluido |
| 3 | `Runtime Ownership + Reset Participation` | ligar objetos vivos ao reset/restart sem confundir ownership | medio | cortes 1 e 2 | concluido |
| 4 | `Pooling Future-Ready Seam` | deixar o desenho pronto para reuse sem acoplar pooling ao gameplay | medio | cortes 1 a 3 | concluido |

## 6. Detalhamento por corte

### 6.1 `Ownership Taxonomy`

Objetivo: deixar explicitas as categorias de objeto e quem e owner de cada uma.

Problema que resolve: mistura entre objeto vivo, objeto de apresentacao, objeto local e objeto global.

Entra:

- categorias de objeto
- owner runtime
- fronteira entre presentation-only e gameplay-owned

Fica fora:

- pooling
- redesign de backbone
- novas camadas de compatibilidade

Criterio de aceite:

- cada categoria tem owner claro
- a leitura de runtime fica objetiva

Dependencias:

- freeze da rodada 1

Risco:

- baixo

### 6.2 `Actor Consumption Contract`

Objetivo: definir quando binders, UI e observadores externos podem consumir actors com seguranca.

Problema que resolve: dependencia de timing implícito e inferencia de readiness.

Entra:

- contrato de consumo seguro
- observabilidade apos `ActorSpawnCompletedEvent`
- fronteira clara entre existe e pode ser observado

Fica fora:

- mudanca de ownership do registry
- redesign do spawn rail
- polling ou fallback silencioso

Criterio de aceite:

- consumidores reagem a contrato explicito
- nao dependem de `Awake`/`OnEnable` para encontrar actor por id

Dependencias:

- corte 1 da rodada 1

Risco:

- baixo / medio

### 6.3 `Runtime Ownership + Reset Participation`

Objetivo: ligar objetos vivos ao reset e ao restart sem embaralhar ownership.

Problema que resolve: objetos existem, mas o papel deles no reset fica implícito ou disperso.

Entra:

- participacao de objeto vivo em reset/restart
- comportamento de cleanup e reconstituicao
- relaçao com `SceneReset`, `WorldReset` e `GameplayReset`

Fica fora:

- ownership macro do backbone
- pooling completo
- rename estrutural

Criterio de aceite:

- e claro quem reconstitui, quem limpa e quem observa
- o runtime de objetos continua consultavel e previsivel

Status: concluido.

Dependencias:

- cortes 1 e 2

Risco:

- medio

### 6.4 `Pooling Future-Ready Seam`

Objetivo: preparar o desenho para reuse sem contaminar a semantica atual.

Problema que resolve: pooling aparece cedo demais como solucao estrutural ou fica isolado demais.

Entra:

- seam futuro para reuse
- criterios de compatibilidade com lifecycle
- limites do backend de pool

Fica fora:

- integracao ampla de pooling
- ownership novo para gameplay
- mudanca de contrato de spawn concluido

Criterio de aceite:

- o desenho aceita pooling depois, sem obrigar mudanca de semantica agora

Dependencias:

- cortes 1 a 3

Risco:

- medio

Status: concluido.

## 7. Regra de execucao

- Nao pular corte sem fechar o anterior.
- Nao abrir frentes paralelas desnecessarias.
- Nao criar adapters temporarios sem necessidade.
- Quando surgir duvida local, voltar ao plano central da rodada 2.
- Nao reabrir o backbone congelado da rodada 1 sem motivo real.

## 8. O que fica fora da rodada 2

- renomeacoes fisicas
- cleanup de namespace
- redesign amplo de docs
- pooling completo se ainda nao for o momento
- outras frentes que desviem do lifecycle de objetos

## 9. Proximo passo oficial

O corte oficial `Runtime Ownership + Reset Participation` esta concluido.
O corte oficial `Pooling Future-Ready Seam` esta concluido.
O pooling permanece como backend de infraestrutura abaixo de `Spawn`, sem assumir identidade, readiness, reset ou gameplay state.
O ganho principal foi deixar a arquitetura pronta para reuse futuro sem contaminar a semantica de gameplay.
O escopo minimo consolidado deste corte foi:

- seam arquitetural de pooling sob `Spawn`
- limite claro entre backend de pool e lifecycle de gameplay object

## 10. Freeze final

A rodada 2 esta concluida.
Este plano fica como referencia canonica da rodada 2 e nao reabre as decisoes ja fechadas.
