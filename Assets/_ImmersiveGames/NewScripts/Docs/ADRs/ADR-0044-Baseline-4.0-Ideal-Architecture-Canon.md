# ADR-0044 - Baseline 4.0 Ideal Architecture Canon

## Status
- Estado: Aceito
- Data: 2026-03-28
- Tipo: Direction / Canonical architecture

## Contexto

O ADR-0001 definiu o vocabulario fundamental do dominio.
O ADR-0043 estabeleceu o Baseline 4.0 como realinhamento conceitual + adequacao estrutural sem regressao.

O documento `Docs/Plans/Blueprint-Baseline-4.0-Ideal-Architecture.md` consolida a arquitetura ideal do Baseline 4.0 a partir dessas bases e passa a ser a referencia principal de arquitetura-alvo.

O codigo atual continua valioso como inventario de comportamento e reaproveitamento, mas nao define o contrato final da arquitetura.

## Decisao

O Baseline 4.0 passa a adotar como referencia canonica de arquitetura:

- a espinha conceitual do ADR-0001;
- a direcao estrutural do ADR-0043;
- a arquitetura ideal consolidada no blueprint do Baseline 4.0.

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
- `EnterStage` e `ExitStage` sao `Estagios Locais`.
- `Playing` e o `Estado de Fluxo`.
- `Victory` / `Defeat` sao `Resultado da Run`.
- `PostRunMenu` e `Contexto Local Visual`.
- `Restart` / `ExitToMenu` sao `Intencoes Derivadas`.
- `Pause` e `Estado Transversal`.

## Coluna dorsal do runtime

Sequencia canonica do runtime:

`Gameplay -> Level -> EnterStage -> Playing -> ExitStage -> RunResult -> PostRunMenu -> Restart / ExitToMenu -> Navigation primary dispatch -> Audio contextual reactions`

## Dominios-alvo

### GameLoop
- Estado de fluxo, run e pausa.
- Nao deve possuir ownership de pos-run visual, route dispatch ou audio precedence.

### PostGame
- Ownership do pos-run, projecao do resultado e contexto visual local.
- Nao deve possuir a maquina de estados do gameplay nem a politica primara de navegacao.

### LevelFlow
- Conteudo local do gameplay, restart context e acoes pos-level.
- Nao deve possuir resultado terminal, ownership pos-run ou dispatch global.

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

## Consequencias

- O blueprint passa a ser lido sob a cobertura do ADR-0044.
- O plano operacional e os cortes por fase devem seguir a arquitetura ideal, nao a forma atual dos modulos.
- A compatibilidade com o legado so e mantida quando ela nao distorce a espinha conceitual.

## Fechamento

Este ADR consolida o Baseline 4.0 como arquitetura ideal primeiro, legado depois.
