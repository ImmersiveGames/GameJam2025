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

### Fluxo de Vida Atual
1. **Bootstrap de Cena**: `NewSceneBootstrapper.Awake` registra serviços de cena e registries (incluindo `WorldLifecycleHookRegistry`, `IActorRegistry`, `IWorldSpawnServiceRegistry`) sem `allowOverride` e com logs de diagnóstico.
2. **World Lifecycle**: `WorldLifecycleController` aciona `WorldLifecycleOrchestrator`, que coordena reset determinístico (Acquire Gate → hooks pré-despawn → actor hooks pré-despawn → despawn → hooks pós-despawn/pré-spawn → spawn → actor hooks pós-spawn → hooks finais → release), interrompendo em falhas (fail-fast) e registrando a ordem.
3. **Gameplay**: ocorre por eventos/contratos entre atores/serviços configurados na cena.
4. **Unload da Cena**: serviços e registries de cena são descartados; próxima cena cria um novo grafo.
5. **Notas para QA/Testers**:
   - São consumidores de scene-scope; podem falhar no `Awake` se o boot ainda não ocorreu.
   - Padrão recomendado: `Start()` ou lazy injection + retry/timeout para evitar falso negativo.
   - Erros no início do Play Mode normalmente indicam ordem de inicialização/ausência do bootstrapper, não falha do reset.

### World Lifecycle Reset & Hooks (As-Is)
- **Guardrail do Registry**: `WorldLifecycleHookRegistry` nasce apenas no `NewSceneBootstrapper`; controller/orchestrator apenas consomem via DI. Qualquer tentativa de segundo registro é erro.
- **Tipos de hooks**:
  1. **Spawn Service Hooks (`IWorldLifecycleHook`)**: implementados por serviços de spawn; usados para limpar caches/preparar pools.
  2. **Scene Hooks via DI**: serviços registrados no escopo de cena e resolvidos via `IDependencyProvider.GetAllForScene`.
  3. **Scene Hooks via Registry**: instância criada no bootstrapper e injetada; usada para QA, debug, ferramentas, testes.
  4. **Actor Component Hooks (`IActorLifecycleHook`)**: `MonoBehaviour` executados via `ActorRegistry` nas fases de ator.
- **Garantias**: hooks são opt-in; falha interrompe o reset (fail-fast); ordem é determinística e sem reflection.

## Planned (To-Be / Roadmap)
- Bootstrap global adicional para serviços compartilhados entre cenas, mantendo separação clara de estado.
- Ampliação de contratos de eventos/telemetria para multiplayer local com rastreamento explícito de identidade.
- Testes automatizados e utilitários extras documentados em `docs/UTILS-SYSTEMS-GUIDE.md`.
- Evolução dos guias de DI/EventBus para cenários mais complexos sem abrir mão de registro explícito.
