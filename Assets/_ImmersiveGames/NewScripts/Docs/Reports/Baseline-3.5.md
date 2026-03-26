# Baseline 3.5 - Estabilizacao Arquitetural

## Status

- Estado: **Fechada**
- Tipo: baseline de estabilizacao arquitetural
- Data: **2026-03-26**

## Objetivo

A Baseline 3.5 nao introduz nova feature. Ela registra o ponto em que a base de `NewScripts` passou a ser considerada estavel o suficiente para servir de partida para a camada acima, sem reabrir a definicao da base.

## Fonte de Verdade

Esta baseline se apoia, como referencia canonica, em:

- `Docs/Plans/Plan-SceneFlow-Refoundation.md`
- `Docs/Plans/Plan-MacroFlow-Stack-Consolidation.md`
- `Docs/Plans/Plan-Reset-Stack-Consolidation.md`
- `Docs/ADRs/ADR-0034-Actor-Presentation-Domain-Intent-and-Boundaries.md`
- `Docs/ADRs/ADR-0035-Ownership-Canonico-dos-Clusters-de-Modules-em-NewScripts.md`

## O Que Esta Baseline Afirma

A Baseline 3.5 afirma que:

1. a base arquitetural principal ja esta estabilizada;
2. `SceneFlow`, o stack macro e o stack de reset tiveram seus limites consolidados;
3. os ADRs de `Actor Presentation` e ownership global de modulos podem ser tratados como referencias congeladas para a proxima camada;
4. a evolucao acima da base pode comecar sem redefinir a semantica central da plataforma.

## Escopo

Entram nesta baseline apenas os pontos ja consolidados:

- refoundation de `SceneFlow`
- consolidacao do stack macro
- consolidacao do stack de reset
- ADR-0034
- ADR-0035

Ficam fora desta baseline:

- implementacao de `Audio`
- implementacao de `Actor Presentation`
- reorganizacao de `Content`
- qualquer expansao de escopo de codigo
- novos ADRs

## Contratos Preservados

- `SceneFlow` continua owner da timeline macro.
- `Navigation` continua owner de intent e dispatch.
- `LevelFlow` continua owner do lifecycle local e do contexto semantico de level/start/restart.
- `GameLoop` continua owner da state machine da run.
- `WorldReset` continua owner do reset macro.
- `SceneReset` continua owner do pipeline local de reset.
- `ResetInterop` continua bridge fina.
- `SimulationGate` continua owner da trava.
- `Gameplay` e `Readiness` continuam consumidores, nao owners de reset.

## Consolidado Nesta Baseline

- `SceneFlow` ficou estabilizado como owner da timeline macro.
- O stack macro ficou com ownership e boundaries mais claros entre `SceneFlow`, `Navigation`, `LevelFlow`, `GameLoop` e `ResetInterop`.
- O stack de reset ficou com contrato mais explicito entre `WorldReset`, `SceneReset`, `ResetInterop`, `SceneFlow`, `SimulationGate` e `Gameplay`.
- ADR-0034 formalizou intenção e boundary de `Actor Presentation`.
- ADR-0035 formalizou a leitura canonica dos clusters de modulos em `NewScripts`.

## O Que Ficou Conscientemente Fora

- nova feature
- refactor amplo de modulos ja estabilizados
- reorganizacao fisica de pastas
- expansao de escopo para camadas ainda nao iniciadas
- nova regra documental fora dos contratos ja aprovados

## Residuos Aceitos Conscientemente

| Residuo | Motivo | Bloqueia a proxima fase? |
|---|---|---:|
| Ajustes finos de naming historico em docs secundarios | nao alteram o contrato canonico | nao |
| Pequenos residuos de observabilidade ou texto legado | valor baixo nesta fase | nao |
| Estrutura fisica ainda nao reorganizada por completo | a baseline prioriza ownership, nao layout | nao |

## Criterio de Entrada da Proxima Fase

A proxima fase pode comecar quando puder assumir, sem revisitar a base:

1. `SceneFlow` ja esta estabilizado;
2. o stack macro nao precisa ser redesenhado para suportar a camada acima;
3. o stack de reset nao precisa de nova redefinicao de ownership;
4. `Actor Presentation`, `Audio` e `Content` podem ser trabalhados como camadas acima da base.

## Checklist Manual

- [ ] a leitura canonica aponta para esta baseline como ponto de partida atual
- [ ] `SceneFlow` permanece estabilizado
- [ ] o stack macro permanece coerente
- [ ] o stack de reset permanece coerente
- [ ] ADR-0034 e ADR-0035 permanecem congelados
- [ ] nao ha indicio de que a base precise ser redesenhada para iniciar a proxima camada

## Exit Condition

A Baseline 3.5 pode ser considerada fechada quando este documento, os planos concluidos e os ADRs referidos forem tratados como fonte canonica para a camada acima, sem reabrir a base.

## Decisao Final

A Baseline 3.5 e uma baseline de estabilizacao arquitetural, nao de feature nova. Ela existe para permitir que a proxima camada seja construida sobre uma base ja consolidada, com menos ambiguidade de ownership e menos risco de retrabalho.
