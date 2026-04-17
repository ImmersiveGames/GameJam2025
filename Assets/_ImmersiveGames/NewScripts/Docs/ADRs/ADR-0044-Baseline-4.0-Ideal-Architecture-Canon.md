# ADR-0044 - Baseline 4.0 Ideal Architecture Canon

## Status
- Estado: Aceito
- Data: 2026-03-28
- Tipo: Direction / Canonical architecture

## Contexto

O ADR-0001 definiu o vocabulario fundamental do dominio.
O ADR-0043 estabeleceu o Baseline 4.0 como realinhamento conceitual + adequacao estrutural sem regressao.

O documento `Docs/Plans/Blueprint-Baseline-4.0-Ideal-Architecture.md` consolida a arquitetura ideal do Baseline 4.0 a partir dessas bases e passa a ser a referencia principal de arquitetura-alvo.

Este ADR funciona como canon guarda-chuva. Os ADRs `ADR-0045`, `ADR-0046` e `ADR-0047` refinam depois a semantica de composicao da gameplay sem rebaixar o papel estrutural deste baseline.

O codigo atual continua valioso como inventario de comportamento e reaproveitamento, mas nao define o contrato final da arquitetura.

## Decisao

O Baseline 4.0 passa a adotar como referencia canonica de arquitetura:

- a espinha conceitual do ADR-0001;
- a direcao estrutural do ADR-0043;
- a arquitetura ideal consolidada no blueprint do Baseline 4.0;
- a leitura de composicao da gameplay refinada pelos ADRs `ADR-0045`, `ADR-0046` e `ADR-0047`.

## Coluna dorsal conceitual

O dominio deve ser lido a partir dos seguintes conceitos canonicos:

- Contexto Macro
- Contexto Local de Conteudo
- Contexto Local Visual
- Estagio Local
- Estado de Fluxo
- Resultado da Run
- Intencao Derivada
- Estado Transversal

### Leitura canonica

- `Gameplay` e o `Contexto Macro`.
- `Level` e o `Contexto Local de Conteudo`.
- `PhaseDefinitionAsset` responde por "o que a phase e".
- `PhaseDefinitionCatalogAsset` responde por "como as phases se encadeiam".
- `EnterStage` e `ExitStage` sao `Estagios Locais`.
- `Playing` e o `Estado de Fluxo`.
- `Victory` / `Defeat` sao `Resultado da Run`.
- `Overlay` / visual de `RunDecision` e `Contexto Local Visual`.
- `Restart` / `ExitToMenu` sao `Intencoes Derivadas`.
- `Pause` e `Estado Transversal`.

A leitura canonica do runtime de gameplay abaixo e intencionalmente de alto nivel. A composicao semantica fina da sessao jogavel e detalhada depois por `ADR-0045`, `ADR-0046` e `ADR-0047`, sem reatribuir ao backbone o centro do significado do gameplay. A progressao entre phases fica no catalogo; a phase individual nao assume ownership de ordem, initial, next ou previous. Para o fim de run, `ADR-0049` e o owner documental canonico.

## Coluna dorsal do runtime

Sequencia canonica do runtime:

`Gameplay -> Level -> EnterStage -> Playing -> ExitStage -> RunEndIntent -> RunResultStage -> RunDecision -> Overlay -> Restart / ExitToMenu -> Navigation primary dispatch -> Audio contextual reactions`

## Dominios-alvo

### GameLoop
- Estado de fluxo, run e pausa.
- Nao deve possuir ownership de pos-run visual, route dispatch ou audio precedence.
- Nao e eixo semantico primario concorrente ao `Gameplay Runtime Composition`.

### RunResultStage / RunDecision
- `RunResultStage` e phase-owned e simetrico ao `IntroStage`.
- `IntroStage` e o espelho de entrada; `RunResultStage` e o espelho de saida da phase.
- `RunDecision` e macro-route-owned / macro-stage-owned e representa a decisao downstream.
- `RunEndIntent` e a intencao com `reason` que inicia a trilha de fim de run.
- `RunResultStage` pode variar o conteudo conforme a `reason`.
- `RunResultStage` sai apenas por acao explicita de encerramento.
- `RunResultStage` nao depende de `Task` como semantica de negocio.
- `Overlay` e apenas projecao visual de `RunDecision`.
- `PostRun` permanece apenas como alias historico de compatibilidade.

### LevelFlow
- Conteudo local do gameplay, restart context e acoes pos-level.
- `Continuity`, quando lida no gameplay runtime, e downstream semantico de fechamento; a ordem entre phases nao nasce aqui.
- Nao deve possuir resultado terminal, ownership pos-run ou dispatch global.
- Nao e eixo primario concorrente ao `Gameplay Runtime Composition`.

### Navigation
- Resolucao de intent para route/style e dispatch primario.
- Nao deve possuir semantica de resultado, pos-run ou pause.

### Audio
- Playback global e entity-bound com precedencia contextual propria.
- Nao deve ser dono de navigation, resultado ou pos-run.

### SceneFlow
- Pipeline tecnico de transicao e readiness.
- Nao deve carregar semantica de gameplay, pos-run ou audio.

### Frontend/UI
- Contextos visuais locais e emissores de intents.
- Nao deve ser dono de dominio, resultado ou politica de navegacao.

## Regras de reaproveitamento

- Reaproveitar quando a peça ja expressa o papel canonico sem ambiguidade.
- Reaproveitar com ajuste quando a peça e util, mas ainda carrega ruido semantico.
- Substituir quando a peça mistura ownership ou preserva um contrato conceitualmente errado.
- Proibir adapters quando eles apenas esconderiam uma fronteira incorreta.

## Papel do codigo atual

O codigo atual e fonte de evidencia, inventario de comportamento e material de reaproveitamento.
Ele nao e contrato.

O nome da implementacao local atual nao redefine o canon. Quando um boundary macro conversa com uma execucao local, o contrato deve ser lido primeiro pela funcao canonica e so depois pela implementacao concreta observada.

## Consequencias

- O blueprint passa a ser lido sob a cobertura do ADR-0044.
- O plano operacional e os cortes por fase devem seguir a arquitetura ideal, nao a forma atual dos modulos.
- A compatibilidade com o legado so e mantida quando ela nao distorce a espinha conceitual.

## Fechamento

Este ADR consolida o Baseline 4.0 como arquitetura ideal primeiro, legado depois.

