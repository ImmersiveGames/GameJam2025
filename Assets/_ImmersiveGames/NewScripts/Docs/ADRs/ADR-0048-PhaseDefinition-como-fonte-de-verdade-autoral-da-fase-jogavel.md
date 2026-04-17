# ADR-0048 - PhaseDefinition como fonte de verdade autoral da fase jogavel

## Status
- Estado: Aceito
- Data: 2026-04-03
- Tipo: Direction / Canonical architecture

## 1. Objetivo

`PhaseDefinition` e a fonte de verdade autoral da fase jogavel.

Este ADR congela a camada de authoring da fase como contrato positivo, tipado e autocontido, separada da leitura runtime que `GameplaySessionFlow` deriva em memoria.

Nota de escopo: `PhaseDefinition` continua sendo a fonte autoral da phase, mas nao e owner da transformacao composta da sessao/runtime acima do baseline; essa leitura pertence a `ADR-0052`.

## 2. Escopo

Este ADR cobre:

- a identidade autoral da fase
- a estrutura conceitual minima do `PhaseDefinition`
- os blocos de primeiro nivel do V1
- as referencias internas entre blocos
- o catalogo explicito de `PhaseDefinition`
- a resolucao deterministica da phase
- o consumo dessa definicao por `GameplaySessionFlow`

## 3. Papel de `PhaseDefinition`

`PhaseDefinition` e a definicao canonicamente autoral da fase jogavel.

Ele alimenta `GameplaySessionFlow` e separa a definicao da fase da leitura runtime.
Ele nao define, por si so, a ordem entre phases nem a politica de encadeamento macro da experiencia.

Leitura canonica:

- `PhaseDefinition` representa a fase como contrato autoral
- `PhaseDefinition` responde pelo que a phase e
- `GameplaySessionFlow` consome a definicao ja resolvida
- o runtime deriva uma versao operacional em memoria
- a definicao autoral permanece estavel enquanto o runtime executa

## 4. Estrutura conceitual minima no V1

O minimo autoral de uma phase e simples, embutido no proprio asset, com estes blocos obrigatorios:

1. identidade / metadados
2. conteudo da phase
3. ao menos uma cena de conteudo carregavel em additive

Esse e o menor contrato necessario para dizer que a phase existe.
Os demais eixos de lifecycle, incluindo `IntroStage` e `RunResultStage`, podem aparecer acima desse minimo, mas nao devem ser confundidos com o recorte basal da phase.

## 5. Blocos do `PhaseDefinition`

### 5.1 Identidade / metadados

Esse bloco identifica a phase no ecossistema.

Ele concentra:

- id da phase
- nome autoral
- classificacao editorial
- sinais simples de uso

### 5.2 Conteudo da fase

Esse bloco declara a composicao de conteudo da phase.

Leitura canonica:

- o payload principal do bloco e cena, preferencialmente via Addressables
- uma phase pode compor uma ou mais cenas locais
- o arranjo additive e permitido como forma de composicao
- cada entrada referencia uma cena auto-declarativa
- a phase compoe o conteudo sem reescrever a identidade da cena

Estrutura minima de cada entrada:

- id local da entrada
- referencia da cena
- papel / tipo da cena
- tags / classificacao local simples

### 5.3 Players

Esse eixo declara quem participa semanticamente da phase.

Leitura canonica:

- o eixo e uma lista explicita de participantes
- cada participante possui id local semantico
- cada participante possui papel / tipo de participacao forte
- o eixo e declarativo e nao operacional

`Rules/Objectives` e `InitialState` foram removidos do canônico atual de `Phase` e nao compoem mais este asset.

### 5.6 Fechamento da fase

Esse bloco declara como a phase consolida semanticamente seu encerramento.

Leitura canonica:

- o bloco de fechamento e declarativo e unico
- ele declara o resultado da run e sinais semanticos locais de fechamento
- o shape tecnico final desse bloco permanece separado do contrato autoral

## 6. Tipagem, registro e cardinalidade

Os contratos internos do `PhaseDefinition` usam ids estaveis, tipagem por dominio e registro explicito.

Regras canonicas:

- a identidade principal pertence a propria `PhaseDefinition`
- o catalogo organiza, ordena, encadeia e expõe as phases, mas nao cria uma identidade paralela
- o id interno e a chave principal de resolucao da phase
- o label externo, quando existir, e apoio editorial
- cada bloco pode ter sua propria tipagem forte e seu proprio contrato
- a cardinalidade do contrato permanece controlada por bloco e por dominio

## 7. Referencias internas

As referencias entre blocos usam ids internos estaveis.

Leitura canonica:

- as referencias internas apontam para elementos da propria phase
- o `PhaseDefinition` permanece autocontido
- o id interno e a chave de referencia entre blocos
- a referencia so aparece quando ha necessidade explicita
- a tipagem por dominio preserva clareza e evita ambiguidade

## 8. Catalogo e resolucao

`PhaseDefinition` entra no sistema por registro explicito em catalogo.

Leitura canonica:

- o catalogo organiza, ordena e encadeia as phases
- o catalogo define initial, next, previous e progressao editorial entre phases
- o catalogo orienta a selecao da phase
- a identidade principal continua sendo da propria `PhaseDefinition`
- o catalogo nao cria um id paralelo para a phase
- a resolucao acontece principalmente por id interno da phase
- multiplos catalogos podem referenciar a mesma phase
- catalogo e resolucao permanecem separados conceitualmente
- qualquer relacao entre phases deve ser lida pelo catalogo, nao por um asset individual isolado

`NextPhase` e um contrato de lifecycle da phase e nao navegacao macro.
A politica concreta de troca local permanece pluggable e pode ser resolvida por um owner semantico explicito do lifecycle, por exemplo `PhaseFlowService`, sem reatribuir ao backbone a ownership da phase.

## 9. Consumo por `GameplaySessionFlow`

`GameplaySessionFlow` consome a `PhaseDefinition` inteira como input principal.

Leitura canonica:

- a entrada principal do `GameplaySessionFlow` e a propria `PhaseDefinition` ja resolvida
- o runtime nao muta o asset autoral
- `SessionContext` nasce em `PhaseSelected`
- `PhaseRuntime` e `Players` nascem em `ContentApplied`
- `IntroStage`, quando presente, acontece antes de `Playing`
- `RunResultStage`, quando presente, acontece depois de `Playing`
- `RunResult` acontece depois de `Playing`
- a sequencia de derivacao permanece alinhada ao lifecycle canonico da phase

`RestartCurrent` e um contrato de lifecycle da phase.
`RestartFromFirst` e um restart macro do gameplay com reseed da first phase.

## 10. Consequencias

Este contrato consolida a fase jogavel como um conjunto autoral autocontido.

Consequencias principais:

- o V1 ganha um centro autoral unico e legivel
- a separacao entre definicao e runtime fica clara
- o projeto evita identidade paralela desnecessaria no catalogo
- os blocos internos podem evoluir sem reabrir o owner da fase
- a progressao entre phases fica no catalogo, preservando o `PhaseDefinitionAsset` como owner apenas do que a phase e
- `GameplaySessionFlow` continua sendo consumidor da definicao, nao o owner da semantica autoral

## 11. Contexto historico curto

`Level` permanece como nome historico do estado atual visivel no runtime.
O contrato canÃ´nico do futuro fica centrado em `PhaseDefinition`.

