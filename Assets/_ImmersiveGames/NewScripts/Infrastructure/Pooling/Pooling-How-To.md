# Pooling How-To

## Objetivo do modulo

O modulo canonico de pooling fornece reutilizacao de `GameObject` em infraestrutura compartilhada, com identidade por `PoolDefinitionAsset` e lifecycle previsivel.

Ele existe para:
- reduzir custo de instantiate/destroy em runtime
- padronizar ensure/prewarm/rent/return
- manter ownership tecnico fora de dominio

Ele nao existe para:
- conter regra de gameplay
- decidir fluxo de audio, UI, VFX ou actor systems
- substituir composition root

## Onde vive

- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Pooling/Contracts/**`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Pooling/Config/**`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Pooling/Runtime/**`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Pooling/QA/**`

## Ownership e bootstrap

- ownership global: `GlobalCompositionRoot`
- bootstrap global registra apenas `IPoolService`
- nao existe lista global de pools no boot
- sem scan automatico de assets

## Contratos principais

### `IPoolService`

Superficie publica de operacao:
- `EnsureRegistered(PoolDefinitionAsset definition)`
- `Prewarm(PoolDefinitionAsset definition)`
- `Rent(PoolDefinitionAsset definition, Transform parent = null)`
- `Return(PoolDefinitionAsset definition, GameObject instance)`
- `Shutdown()`

### `IPoolableObject`

Hooks canonicos opcionais em objetos pooled:
- `OnPoolCreated()`
- `OnPoolRent()`
- `OnPoolReturn()`
- `OnPoolDestroyed()`

### `PooledBehaviour`

Base opcional com no-op para facilitar implementacao de lifecycle.

### `PoolDefinitionAsset`

Define identidade estrutural do pool e sua configuracao.

Campos principais:
- `prefab`
- `initialSize`
- `canExpand`
- `maxSize`
- `autoReturnSeconds`
- `poolLabel`
- `prewarm`

Regra importante:
- `prewarm` declara intencao de prewarm, mas o asset nao executa nada sozinho.

## Runtime principal

### `PoolService`

- owner dos pools por `PoolDefinitionAsset`
- cache interno por referencia de asset
- `EnsureRegistered` e idempotente e faz apenas ensure/register
- `Prewarm` e explicito

### `GameObjectPool`

- implementa prewarm/rent/return/cleanup
- controla contagem ativa/inativa/total
- aplica expansao com teto maximo
- falha explicitamente ao atingir limite

### `PoolRuntimeHost`

- owner de hierarchy runtime do pool
- separa objetos disponiveis vs rented

### `PoolRuntimeInstance`

- estrutura interna em memoria
- vincula instancia ao pool de origem
- nao e componente obrigatorio visivel na instancia

### `PoolAutoReturnTracker`

- agenda auto-return quando `autoReturnSeconds > 0`
- cancela timer em return manual, cleanup ou shutdown
- evita double-return

## Fluxo canonico

### 1) Ensure

`EnsureRegistered(definition)` cria/registra pool se necessario.

### 2) Prewarm

`Prewarm(definition)` preenche capacidade inicial conforme configuracao.

### 3) Rent

`Rent(definition, parent)` aluga instancia do pool.

### 4) Return

`Return(definition, instance)` devolve instancia ao pool.

### 5) Cleanup

`Shutdown()` limpa pools, cancela timers e encerra runtime host.

### 6) Auto-return

Se `autoReturnSeconds > 0`, instancia rented recebe retorno automatico canonicamente.

## Como configurar um `PoolDefinitionAsset`

1. Criar asset `PoolDefinitionAsset`.
2. Preencher `prefab`.
3. Definir `initialSize`, `canExpand` e `maxSize`.
4. Definir `autoReturnSeconds` (0 para desativado).
5. Definir `poolLabel` para observabilidade.
6. Marcar `prewarm=true` quando quiser aquecimento automatico no consumer.

## Como um consumer usa pools

Shape final aprovado:
- o consumer usa base reutilizavel (`PoolConsumerBehaviourBase`, namespace `Infrastructure.Pooling.Interop`)
- o consumer declara lista explicita de `PoolDefinitionAsset`
- a base resolve `IPoolService`
- para cada definicao:
  - sempre executa `EnsureRegistered`
  - executa `Prewarm` somente quando `definition.prewarm == true`

Sem script manual por feature para chamar ensure/prewarm.

## Como o QA usa pools hoje

- consumer QA canonico: `PoolingQaContextMenuDriver`
- o driver ja herda da base reutilizavel de consumer
- nao precisa de `MonoBehaviour` complementar para dependencias de pool

## Validacao manual com `PoolingQaContextMenuDriver`

Acoes principais de Play Mode via ContextMenu:
- `QA/Ensure Pool`
- `QA/Prewarm`
- `QA/Rent One`
- `QA/Return Last`
- `QA/Return All`
- `QA/Rent Burst`
- `QA/Rent Until Max`
- `QA/Rent Past Max (Expect Fail)`
- `QA/Cleanup Pool`
- `QA/Run Basic Scenario`
- `QA/Run AutoReturn Scenario`

## Guardrails arquiteturais

- sem `Resources.Load`
- sem `PersistentSingleton`
- sem `RuntimeInitializeOnLoadMethod` como owner
- sem identidade estrutural por string
- sem acoplamento com Audio/Gameplay/UI/VFX/Actor/spawner
- sem regra hardcoded de posicao
- sem bootstrap global por lista de pools

## O que NAO fazer

- nao colocar regra de dominio dentro do pool
- nao usar o mock QA como parte estrutural de runtime
- nao mover ownership para consumer de dominio
- nao criar runtime paralelo de pooling
- nao mascarar erro de limite maximo

## Observacoes de memoria e GC

- pooling reduz churn de `Instantiate/Destroy`, mas ainda precisa sizing correto
- `initialSize` e `maxSize` muito baixos podem aumentar expansao frequente
- `maxSize` muito alto sem necessidade pode segurar memoria
- use logs de observabilidade para calibrar perfil real

## Observacoes de auto-return

- `autoReturnSeconds <= 0`: sem auto-return
- `autoReturnSeconds > 0`: timer por instancia rented
- return manual cancela timer
- cleanup/shutdown cancela timers pendentes

## Proximos consumidores naturais

Com pooling fechado no escopo do ADR-0029, o proximo consumo natural e Audio (ADR-0028 / F3), sem necessidade de bootstrap alternativo.