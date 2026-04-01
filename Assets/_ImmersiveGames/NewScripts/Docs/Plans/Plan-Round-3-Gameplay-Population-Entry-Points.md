# Plan - Round 3 - Gameplay Population Entry Points

## Base canonica

Esta rodada parte do freeze do backbone e do freeze da rodada 2.
Nao reabre arquitetura ja congelada.

Fontes obrigatorias:

- `Docs/ADRs/ADR-0001-Glossario-Fundamental-Contextos-e-Rotas-v2.md`
- `Docs/ADRs/ADR-0043-Ancora-de-Decisao-para-o-Baseline-4.0.md`
- `Docs/ADRs/ADR-0044-Baseline-4.0-Ideal-Architecture-Canon.md`
- `Docs/Plans/Blueprint-Baseline-4.0-Ideal-Architecture.md`
- `Docs/Plans/Plan-Baseline-4.0-Execution-Guardrails.md`
- `Docs/Plans/Plan-Round-2-Object-Lifecycle.md`
- `Docs/Reports/Audits/2026-04-01/Round-2-Freeze-Object-Lifecycle.md`

## Reposicionamento da rodada

O projeto ainda deve ser lido como prototipo, com placeholders e mocks.
A rodada 3 nao trata de um sistema de gameplay ja existente.
Ela define a arquitetura ideal de entrada da baseline para futura populacao do jogo.

## Fronteira

### Baseline

- define contratos de entrada;
- exibe entry points para receber conteudo futuro;
- preserva ownership de runtime, reset, registro e observabilidade;
- valida o que pode entrar sem assumir populacao real.

### Conteudo futuro

- e o que ainda vai ser injetado na baseline;
- nao e owner do backbone;
- nao redefine lifecycle, reset ou navegacao;
- entra apenas pelos contratos expostos pela baseline.

## O que a baseline precisa expor

1. `Definition` ou equivalente de conteudo.
2. `Materialization` ou equivalente de instancia inicial.
3. `Registry` ou equivalente de reconhecimento runtime.
4. `Reset/Reconstitution` ou equivalente de retorno ao estado previsto.
5. `Observability` ou equivalente de evento, estado ou sinal seguro.

## Ownership minimo

- `Baseline` e owner dos entry points, contratos e guardrails.
- `Gameplay content` e owner do que preenche esses entry points.
- `Mocks` e `placeholders` continuam como evidencia de prototipo, nao como contrato final.
- Nenhuma taxonomia fina de `objects` deve ser assumida antes de maturidade suficiente.

## Entra

- leitura dos entry points que a baseline precisa oferecer para futura populacao;
- delimitacao entre contrato de entrada e conteudo que ainda nao existe;
- separacao entre definicao, materializacao, registro, reset/reconstituicao e observabilidade;
- revisao de nomes ativos apenas quando eles confundem ownership de entrada;
- uso de `player`, `enemies` e demais conteudos apenas como exemplos de futura populacao, nao como sistema pronto.

## Fora

- reabrir backbone ou rodada 2;
- propor codigo, workaround ou redesenho amplo;
- tratar a rodada como inventario de objetos ja implementados;
- criar nova taxonomia de entidades ou objects sem base madura;
- mover ownership para modules de conteudo;
- assumir jogo real implementado quando o estado atual ainda e prototipo.

## Conflitos vivos nos docs ativos

- `Gameplay` ainda aparece amplo demais e mistura setup, spawn, state e reset.
- `LevelFlow` ainda carrega nomes historicos enquanto o owner operacional e `LevelLifecycle`.
- `GameLoop` e `PostRun` ainda usam bordas e termos diferentes para handoff pos-run.
- `PostPlay` e `WorldLifecycle` seguem como nomenclatura residual em docs ativos.

## Plano curto

1. Fixar a baseline como owner dos contratos de entrada para futuro conteudo.
2. Separar o que e definicao de entrada do que e populacao futura.
3. Validar os cinco contratos minimos: definicao, materializacao, registro, reset/reconstituicao e observabilidade.
4. Manter `player`, `enemies` e demais conteudos apenas como destino futuro dos entry points.
5. Fechar a rodada sem abrir taxonomia de objects, sem codigo e sem reabrir frozen decisions.

## Aceite

- a rodada 3 fica definida como preparacao de entry points da baseline;
- o prototipo atual continua reconhecido como incompleto;
- backbone e rodada 2 permanecem congelados;
- a saida fica curta, objetiva e focada em entrada de conteudo futuro.
