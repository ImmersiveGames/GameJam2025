# Round 2 Cut 4 - Pooling Future-Ready Seam

## 1. Objetivo

Este snapshot registra o corte 4 da rodada 2 e congela o seam de pooling futuro.
Ele documenta que pooling permanece como backend de infraestrutura abaixo de `Spawn`, sem assumir ownership de lifecycle, identidade, readiness ou reset.

## 2. Auditoria curta

- O pooling canonicamente vive em `Infrastructure/Pooling/**`.
- `Game` e `Orchestration` nao possuem consumo direto de `IPoolService`.
- O trilho de `Spawn` continua materializando, atribuindo identidade, registrando no `ActorRegistry` e publicando `ActorSpawnCompletedEvent`.
- O pooling atual nao toca `ActorRegistry`, `SceneReset`, `GameplayReset` ou `LevelLifecycle` como owner semantico.
- O seam ja existe como infraestrutura compartilhada; o corte aqui e de contrato e documentacao, nao de migracao funcional.

## 3. Seams confirmados

- `Spawn` continua dono do lifecycle de entrada e saida de gameplay objects.
- pooling pode servir como backend de materializacao/despawn de `GameObject`.
- pooling nao pode assumir identidade, readiness, reset semantics ou gameplay state.
- `ActorRegistry` continua significando apenas vivos/consultaveis.
- `ActorSpawnCompletedEvent` continua sendo o marco de observabilidade segura.

## 4. Resultado

O corte 4 fica concluido sem mudanca de runtime:

- sem migracao ampla para pooling
- sem refactor estrutural de `Spawn`
- sem alteracao de `SceneReset`
- sem alteracao de `GameplayReset`

O ajuste necessario foi tornar o boundary explicito em docs e no guia de pooling.

