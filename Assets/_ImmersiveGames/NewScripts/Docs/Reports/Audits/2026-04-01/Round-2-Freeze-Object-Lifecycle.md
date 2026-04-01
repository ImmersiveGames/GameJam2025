# Round 2 Freeze - Object Lifecycle

## 1. Objetivo

Este arquivo congela o estado final da rodada 2.
Ele resume a leitura consolidada do lifecycle de objetos e nao reabre decisoes ja tomadas.

## 2. Status da rodada

| Corte | Nome | Status final |
|---|---|---|
| 1 | `Ownership Taxonomy` | concluido |
| 2 | `Actor Consumption Contract` | concluido |
| 3 | `Runtime Ownership + Reset Participation` | concluido |
| 4 | `Pooling Future-Ready Seam` | concluido |

## 3. Leitura consolidada da rodada 2

- `Spawn` materializa e atribui identidade.
- `ActorRegistry` e diretorio runtime dos vivos.
- `ActorRegistry` nao significa readiness.
- `ActorSpawnCompletedEvent` e o marco canonico de observabilidade segura.
- `SceneReset` executa reset local.
- `GameplayReset` faz `cleanup / restore / rebind`.
- `LevelLifecycle` ancora restart e reconstituicao de nivel.
- pooling fica abaixo de `Spawn` como backend possivel, sem semantica de gameplay.

## 4. Principais decisoes consolidadas

- taxonomia de ownership explicitada.
- contrato de consumo seguro explicitado.
- reset e restart distribuidos sem confundir ownership.
- pooling tratado como seam futuro abaixo de `Spawn`.

## 5. O que ficou fora da rodada 2

- migracao ampla para pooling.
- redesign amplo de spawn.
- redesign amplo de reset.
- cercas tecnicas mais fortes contra uso direto de `IPoolService`.
- renomeacoes fisicas e cleanup de namespace.

## 6. Proxima frente

A rodada 2 foi concluida.
Novas frentes devem partir deste freeze.
Decisoes futuras nao devem reabrir backbone ou rodada 2 sem motivo real.

