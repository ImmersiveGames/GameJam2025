# Phase Catalog Navigation Hooks

## Objetivo

- Explicar os hooks canônicos mínimos de `Phase Catalog Navigation`.
- Estes hooks servem para reagir a navegação e estado do catálogo, nao para inferir ownership de content/runtime da phase.

## Eventos

| Evento | Publicador | Momento |
| --- | --- | --- |
| `PhaseCatalogNavigationRequestedEvent` | `PhaseNextPhaseSelectionService` | Quando o rail canônico entra na selecao da navegaçao. |
| `PhaseCatalogPendingTargetChangedEvent` | `PhaseCatalogRuntimeStateService` | Quando o pending target e setado ou limpo. |
| `PhaseCatalogCurrentCommittedChangedEvent` | `PhaseCatalogRuntimeStateService` | Quando o committed muda de fato. |
| `PhaseCatalogLoopCountChangedEvent` | `PhaseCatalogRuntimeStateService` | Quando ocorre wrap real com incremento efetivo. |
| `PhaseCatalogNavigationCompletedEvent` | `PhaseNextPhaseService` | Ao final do rail real, apos composicao e handoff concluídos. |

## Quando usar

- Use hooks de catalogo para reagir a ordenacao, navegacao ordinal, pending target, committed e loop count.
- Use hooks de `GameplaySessionFlow` para reagir a content/runtime ativo da phase.
- Nao use hooks de catalogo para substituir o completion canonico de content/runtime quando esse ainda nao ocorreu.

## Campos principais

- `requestKind`
- `fromPhaseId`
- `toPhaseId`
- `reason`
- `routeId`
- `loopCount`
- `wasWrapped`
- `outcome`

## Boas praticas

- Assine os hooks em vez de fazer polling quando o evento canonico ja existe.
- Reaja ao publicador canonico, nao ao painel de QA.
- Trate `CurrentCommittedChanged` como cambio de estado do catalogo, nao como "phase totalmente pronta" por si so.
- Considere `PhaseCatalogNavigationCompletedEvent` como o ponto de confirmacao final do rail.

## Anti-patterns

- Reagir ao QA em vez do evento canonico.
- Ler o estado por polling quando um hook ja foi publicado.
- Misturar hook de catalogo com ownership de runtime de content.
- Assumir que `CurrentCommittedChanged` sozinho significa phase totalmente pronta.

