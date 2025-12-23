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

### Fluxo de Vida Atual (resumo — detalhes operacionais em `WorldLifecycle/WorldLifecycle.md`)
- **Bootstrap de Cena**: `NewSceneBootstrapper` registra serviços/registries de cena (incluindo `WorldLifecycleHookRegistry`, `IActorRegistry`, `IWorldSpawnServiceRegistry`) sem `allowOverride`.
- **WorldLifecycle**: `WorldLifecycleController` orquestra o reset determinístico (gate → hooks/hard reset ou reset-in-place por escopo) seguindo o contrato descrito no documento operacional.
- **Gameplay**: ocorre via eventos/contratos entre atores e serviços registrados na cena.
- **Unload**: unload da cena descarta o grafo/registries; próxima cena recompõe serviços.
- **QA/Testers**: consumidores de scene-scope; devem usar lazy injection/`Start()` para tolerar ordem de boot (detalhes em `WorldLifecycle/WorldLifecycle.md#troubleshooting-qatesters-e-boot-order`).

### World Lifecycle Reset & Hooks (referência cruzada)
- **Owner operacional**: `WorldLifecycle/WorldLifecycle.md`.
- **Guardrails**: `WorldLifecycleHookRegistry` nasce apenas no `NewSceneBootstrapper` (segundo registro reusa a instância e loga erro); hooks são opt-in e ordenados por (`Order`, `Type.FullName`) sem heurísticas.
- **Fontes de hooks**: spawn services, serviços de cena via DI, hooks registrados no registry e `IActorLifecycleHook` em atores — todos executados pelo `WorldLifecycleOrchestrator` conforme pipeline oficial.
- **Otimização**: cache de hooks de ator é válido apenas por ciclo de reset (limpo no `finally`), evitando custo de varredura múltipla.
- **Determinismo**: falhas interrompem o reset (fail-fast) e a ordem é estável entre cenas/resets.

## Planned (To-Be / Roadmap)
- Bootstrap global adicional para serviços compartilhados entre cenas, mantendo separação clara de estado.
- Ampliação de contratos de eventos/telemetria para multiplayer local com rastreamento explícito de identidade.
- Testes automatizados e utilitários extras documentados em `Guides/UTILS-SYSTEMS-GUIDE.md`.
- Evolução dos guias de DI/EventBus para cenários mais complexos sem abrir mão de registro explícito.
