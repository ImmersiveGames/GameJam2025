# Arquitetura Base

## Implemented (As-Is)
### Princípios Fundamentais
- **World-Driven**: cenas representam mundos autocontidos que dirigem o ciclo de jogo.
- **Actor-Centric**: atores são a unidade principal de comportamento e interação, com contratos claros para serviços e eventos.
- **Reset por Despawn/Respawn**: a limpeza de estado descarta instâncias de atores e recria a partir de perfis/configurações, evitando mutação global persistente.
- **Multiplayer local**: fluxos devem suportar múltiplos jogadores no mesmo dispositivo, com fontes únicas de identidade.
- **SOLID e baixo acoplamento**: contratos em inglês, comentários/explicações em português; responsabilidade única e inversão de dependência.

### Escopos
- **Global**: serviços de infraestrutura (logging, configuração, pooling) vivem apenas quando necessários e não carregam estado de gameplay.
- **Scene**: cada cena monta seu próprio grafo de serviços e registries (ex.: `WorldLifecycleHookRegistry`), sem pressupor persistência entre cenas.
- **Actor**: componentes e serviços específicos do ator; resetados via despawn/respawn.

### Fluxo de Vida Atual (resumo; detalhes operacionais em `docs/world-lifecycle/WorldLifecycle.md`)
1. **Bootstrap de Cena**: `NewSceneBootstrapper.Awake` registra serviços de cena e registries (incluindo `WorldLifecycleHookRegistry`, `IActorRegistry`, `IWorldSpawnServiceRegistry`) sem `allowOverride` e com logs de diagnóstico.
2. **World Lifecycle**: `WorldLifecycleController` aciona `WorldLifecycleOrchestrator`, que coordena reset determinístico (Acquire Gate → hooks pré-despawn → actor hooks pré-despawn → despawn → hooks pós-despawn/pré-spawn → spawn → actor hooks pós-spawn → hooks finais → release), interrompendo em falhas (fail-fast) e registrando a ordem. Detalhes de pipeline, escopos e troubleshooting estão em `docs/world-lifecycle/WorldLifecycle.md` (owner operacional).
3. **Gameplay**: ocorre por eventos/contratos entre atores/serviços configurados na cena.
4. **Unload da Cena**: serviços e registries de cena são descartados; próxima cena cria um novo grafo.
5. **Notas para QA/Testers**:
    - São consumidores de scene-scope; podem falhar no `Awake` se o boot ainda não ocorreu.
    - Padrão recomendado: `Start()` ou lazy injection + retry/timeout para evitar falso negativo.
    - Erros no início do Play Mode normalmente indicam ordem de inicialização/ausência do bootstrapper, não falha do reset.

### World Lifecycle Reset & Hooks (As-Is — owner operacional em `docs/world-lifecycle/WorldLifecycle.md`)
- **Guardrails do Registry**: `WorldLifecycleHookRegistry` nasce apenas no `NewSceneBootstrapper`; controller/orchestrator apenas consomem via DI. Segunda tentativa de registro é logada como erro e reusa a instância existente (ownership é do bootstrapper da cena).
- **Fontes de hooks executados em cada fase**:
  1. **Spawn Service Hooks (`IWorldLifecycleHook`)**: implementados por serviços de spawn; usados para limpar caches/preparar pools.
  2. **Scene Hooks via DI**: serviços registrados no escopo de cena e resolvidos via `IDependencyProvider.GetAllForScene`.
  3. **Scene Hooks via Registry**: criados/registrados pelo bootstrapper (ex.: QA `SceneLifecycleHookLoggerA/B`) sem duplicar e reutilizados entre resets da mesma cena.
  4. **Actor Component Hooks (`IActorLifecycleHook`)**: `MonoBehaviour` executados via `ActorRegistry` nas fases de ator.
- **Ordenação determinística**: todos os hooks (mundo e ator) seguem (`Order`, `Type.FullName`) com comparador ordinal, sem reflection, garantindo logs e execução estáveis entre resets.
- **Otimização (cache por ciclo)**: o orquestrador reusa por ciclo a lista ordenada de hooks de ator para reduzir chamadas a `GetComponentsInChildren` em resets com muitos atores. Usa sentinela `EmptyActorHookList` para evitar alocações e não cacheia a sentinela; invalidação permanece por `root` e o cache é limpo no `finally` do reset.
- **Garantias**: hooks são opt-in; falha interrompe o reset (fail-fast); ordem permanece determinística em todas as fases. Consultar `docs/world-lifecycle/WorldLifecycle.md` para troubleshooting e contratos de reset por escopo.

## Planned (To-Be / Roadmap)
- Bootstrap global adicional para serviços compartilhados entre cenas, mantendo separação clara de estado.
- Ampliação de contratos de eventos/telemetria para multiplayer local com rastreamento explícito de identidade.
- Testes automatizados e utilitários extras documentados em `docs/UTILS-SYSTEMS-GUIDE.md`.
- Evolução dos guias de DI/EventBus para cenários mais complexos sem abrir mão de registro explícito.
