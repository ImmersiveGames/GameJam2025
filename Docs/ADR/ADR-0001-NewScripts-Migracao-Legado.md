# ADR-0001 — Migração incremental do Legado para o NewScripts

## Contexto
- O NewScripts é a camada de produto nova e já provê pipeline determinístico de reset, gate e registries por cena.
- Componentes legados precisam migrar de forma progressiva (começando pelo Player) sem quebrar fluxo atual e sem contaminar a arquitetura nova.
- Precisamos de rollback simples caso um passo da migração introduza regressões.

## Decisão
- Adotar estratégia de **Portas e Adaptadores** para incorporar partes do legado sob controle do NewScripts, mantendo determinismo.
- Para cada componente legado, escolher explicitamente uma opção:
  - **Refazer (New)**: reimplementar no NewScripts quando custo de adaptação for alto ou quando o legado conflitar com as novas regras.
  - **Integrar (Direct)**: consumir diretamente um serviço legado somente se ele já respeitar as regras de determinismo e não exigir dependências cruzadas.
  - **Adaptar (Adapter/Bridge)**: encapsular o legado em adaptadores isolados para controlar side effects e alinhar com o pipeline.
  - Durante a migração (ex.: Player legado), mapear quais managers/caches/UI/serviços precisam participar de `ResetScope.Players` e decidir se cada um será refatorado, adaptado ou integrado.
  - Usar soft reset por escopo como rede de segurança: o baseline do player deve voltar ao estado correto mesmo quando parte do estado vive fora do prefab, então participantes externos precisam declarar `Scope=Players` até a migração completa.
  - Avaliar responsabilidades funcionais, não só componentes: um reset de `Players` pode atravessar fronteiras de sistemas legados (managers, caches, serviços compartilhados) para garantir o baseline do player, desde que cada participação seja explícita.
  - Guardrail conceitual: `ResetScope.Players` (e futuros `Boss/Stage`) é um contrato funcional de baseline (experiência/estado do player), não estrutural de prefab. Participações externas são válidas via adaptadores ou `IResetScopeParticipant`, mantendo o pipeline determinístico e o gate. Escopo é resultado de gameplay, não hierarquia de GameObject.

## Guardrails (não negociáveis)
- Código em `_ImmersiveGames.NewScripts.*` não referencia diretamente classes concretas do legado, exceto dentro de adaptadores dedicados.
- Reset e transições seguem sempre o pipeline determinístico: `Gate Acquire → Hooks/ActorHooks → Despawn → Hooks → (Scoped Participants no soft reset) → Spawn → Hooks → Gate Release`.
- `WorldLifecycleHookRegistry` é criado somente no `NewSceneBootstrapper`; controller/orchestrator apenas consomem via DI.
- Soft reset executa apenas `IResetScopeParticipant` para escopos solicitados; não existe execução implícita global.
- Escopos de reset são contratos funcionais (resultado de gameplay), não contratos estruturais de prefab/objeto; participação de legado deve ser declarada via adaptadores ou participantes de escopo, mantendo o pipeline intacto.
- `ISimulationGateService` é o gate oficial para bloquear a simulação durante resets/transições.
- Detalhes operacionais do pipeline, escopos e troubleshooting estão em `../../docs/world-lifecycle/WorldLifecycle.md`; este ADR mantém a decisão e guardrails de migração.

## Arquitetura atual (resumo)
- **Global**: `GlobalBootstrap` registra `IUniqueIdFactory`, `ISimulationGateService`, `WorldLifecycleRuntimeDriver` e readiness (`GameReadinessService`).
- **Escopo de cena**: `NewSceneBootstrapper` cria `WorldRoot`, `IWorldSpawnContext`, `IActorRegistry`, `IWorldSpawnServiceRegistry`, `WorldLifecycleHookRegistry` e participantes de reset de escopo (ex.: `PlayersResetParticipant`).
- **Runtime**: `WorldLifecycleRuntimeDriver` escuta `SceneTransitionScenesReadyEvent`, garante idempotência por `contextSignature`, aciona reset e gerencia acquire/release do gate.
- **World lifecycle**: `WorldLifecycleController` injeta dependências, coleta spawn services e instancia `WorldLifecycleOrchestrator`, que executa fases, hooks e participantes de escopo.

```
SceneBootstrapper -> registries/context -> Controller -> Orchestrator -> SpawnServices/Hooks/Participants -> Gate
```

## Plano incremental
- **Congelar baseline**: validar/logar pipeline atual (gate acquire/release, ordem das fases, participantes por escopo) para detectar regressões.
- **Definir fronteira do Player**: publicar contratos/interfaces no NewScripts que o Player deve implementar ou expor via adaptador.
- **Implementar Adapter mínimo do Player**: criar adaptador apenas após fronteira definida, mantendo o Player legado sob controle do gate e do pipeline.
- **Migrar capacidades**: mover módulos de input, movimento, vida, etc., para o NewScripts uma capacidade por vez, removendo dependências diretas do legado.
- **Mapear baseline funcional**: identificar tudo o que precisa ser resetado para restaurar o baseline do player (incluindo managers, caches, serviços e UI fora do prefab) e decidir quem participa via `Scope=Players`.
- **Atravessar fronteiras com adaptadores**: quando o baseline exigir tocar sistemas legados externos ao prefab (UI, roteadores de input, caches de gameplay), inserir participantes/adaptadores explícitos de `Scope=Players` sem quebrar o gate ou o pipeline determinístico.

## Critérios de validação
- Hard reset: logs de `Gate Acquire/Release` e fases em ordem, sem concorrência ou duplicação por `contextSignature`.
- Soft reset `Players`: executa somente participantes do escopo `Players`, ordem determinística por `(scope, order, typename)`, sem hooks globais indevidos e restaurando baseline funcional e consistente do player mesmo quando envolve participantes externos via adaptadores (escopo é resultado de gameplay, não hierarquia).
- Nenhum arquivo do NewScripts referencia namespaces do legado diretamente (exceto adaptadores em pasta dedicada).

## Consequências
- **Positivas**: isolamento entre legado e NewScripts, previsibilidade do ciclo, rollback simples caso adaptação falhe.
- **Custos**: necessidade de adaptadores temporários e disciplina rígida de boundaries até a substituição completa.

## Validation / Exit Criteria
- O contrato de validação do ciclo está descrito em `docs/world-lifecycle/WorldLifecycle.md` (seção **Validation Contract (Baseline)**).
- Cada passo incremental de migração do legado deve passar por **Hard Reset** e **Soft Reset (Players)** sem regressão de ordem ou logs.
