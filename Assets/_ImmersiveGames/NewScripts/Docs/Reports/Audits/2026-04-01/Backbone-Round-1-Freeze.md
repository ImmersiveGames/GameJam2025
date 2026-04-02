# Backbone Round 1 Freeze

## 1. Objetivo

Este arquivo congela o estado da rodada 1 do backbone.
Ele resume o backbone resultante depois dos cortes executados e serve como referencia canonica para as proximas frentes.
Nao reabre decisoes ja tomadas.

## 2. Status da rodada

| Corte | Nome | Status final |
|---|---|---|
| 1A | `Spawn + Identity` | concluido |
| 1B | `Spawn Completion Contract` | concluido |
| 2 | `SceneReset` executor local | concluido |
| 3 | `ResetInterop` seam fino | concluido |
| 4 | `LevelLifecycle` vs `SceneComposition` | concluido |
| 5 | `GameLoop` puro | concluido |
| 6 | `Experience` edge reativo | concluido |

## 3. Leitura consolidada do backbone

- `Navigation` resolve intencao.
- `SceneFlow` executa transicao.
- `SceneComposition` aplica composicao de cenas.
- `LevelLifecycle` governa selecao, snapshot e entrada local.
- `WorldReset` decide reset macro.
- `SceneReset` executa reset local.
- `Spawn` materializa e atribui identidade.
- `ActorRegistry` e diretorio runtime dos vivos.
- `GameplayReset` faz `cleanup / restore / rebind`.
- `GameLoop` e owner puro da run.
- `Experience` reage ao backbone como edge.

## 4. Principais decisoes consolidadas

- `Spawn` e owner de identidade e materializacao.
- `ActorRegistry` nao significa `ready`.
- `ActorSpawnCompletedEvent` e o marco canonico de observabilidade segura.
- `SceneReset` e executor local, nao owner de spawn.
- `ResetInterop` e seam fino.
- `LevelLifecycle` escolhe e `SceneComposition` aplica.
- `GameLoop` e owner da run.
- `Experience` reage ao backbone.

## 5. O que ficou fora da rodada 1

- renomeacoes fisicas
- cleanup de namespace
- redesign amplo de docs
- pooling completo
- refactor global de restart
- outras frentes ainda nao iniciadas

## 6. Proxima frente

A rodada 1 do backbone foi concluida.
As proximas frentes devem partir deste freeze e respeitar esta base consolidada.
Novas decisoes nao devem reabrir o que ja ficou canonico aqui.
