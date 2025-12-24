## ADRs (referência)

Decisões fundacionais e diretrizes derivadas:
- Índice: `ADR.md`
- ADR-001: `ADR-001-world-reset-por-respawn.md`
- ADR-002: `ADR-002-spawn-pipeline.md`
- ADR-003: `ADR-003-escopos-servico.md`
- ADR-005: `ADR-005-gate-nao-e-reset.md`
- ADR-006: `ADR-006-ui-reage-ao-mundo.md`
- ADR-007: `ADR-007-testes-estado-final.md`

## Implemented (As-Is)

### Princípios Fundamentais

* **World-Driven**
  Cenas representam **mundos autocontidos** que dirigem o ciclo de vida da simulação.
  Uma cena é responsável por montar, executar e descartar completamente seu estado de mundo.

* **Actor-Centric**
  Atores são a **unidade primária de comportamento e interação**.
  Toda lógica de gameplay deve estar ancorada em atores e seus contratos explícitos com serviços e eventos.

* **Reset por Despawn/Respawn**
  Reset significa **descartar instâncias vivas** (atores e estado acoplado) e recriar a partir de definições/configurações.
  Isso evita:

    * mutação global persistente
    * estados “fantasma”
    * dependência implícita de ordem de execução

* **Multiplayer local**
  Fluxos devem suportar múltiplos jogadores no mesmo dispositivo, com:

    * identidade explícita
    * ausência de singletons de gameplay
    * serviços preparados para múltiplos atores do mesmo tipo

* **SOLID e baixo acoplamento**

    * Contratos em inglês
    * Comentários e documentação em português
    * Responsabilidade única
    * Inversão de dependência como regra, não exceção

---

### Escopos

A arquitetura se organiza por **escopos de vida bem definidos**:

* **Global**
  Serviços de infraestrutura:

    * logging
    * configuração
    * pooling
    * gates
    * event bus

  ➜ **Nunca carregam estado de gameplay**
  ➜ Vivem apenas enquanto necessários

* **Scene**
  Cada cena monta **seu próprio grafo de serviços e registries**, incluindo (exemplos):

    * `WorldLifecycleHookRegistry`
    * `IActorRegistry`
    * `IWorldSpawnServiceRegistry`

  ➜ Não pressupõe persistência entre cenas
  ➜ Unload da cena descarta todo o escopo

* **Actor**
  Componentes e serviços específicos do ator:

    * resetados via despawn/respawn
    * nunca sobrevivem ao reset do mundo

---

### Fluxo de Vida Atual

*(Resumo — detalhes operacionais em `WorldLifecycle/WorldLifecycle.md`)*

* **Bootstrap de Cena**
  `NewSceneBootstrapper`:

    * registra serviços e registries de cena
    * inclui `WorldLifecycleHookRegistry`, `IActorRegistry`, `IWorldSpawnServiceRegistry`
    * **não utiliza `allowOverride`**
    * falhas de duplo registro são consideradas erro arquitetural

* **WorldLifecycle**
  `WorldLifecycleController`:

    * orquestra o reset determinístico
    * controla gates
    * executa hooks
    * suporta hard reset ou reset-in-place por escopo

  ➜ Pipeline completo descrito no documento operacional

* **Gameplay**
  O gameplay emerge exclusivamente:

    * de eventos
    * de contratos entre atores
    * de serviços registrados no escopo da cena

  ➜ Não há lógica centralizadora de gameplay

* **Unload**
  O unload da cena:

    * descarta completamente o grafo de serviços
    * invalida registries
    * força reconstrução total na próxima cena

* **QA / Testers**

    * São **consumidores de scene-scope**
    * Devem usar **lazy injection** ou `Start()`
    * Devem tolerar ordem variável de boot

  ➜ Detalhes e armadilhas documentados em
  `WorldLifecycle/WorldLifecycle.md#troubleshooting-qatesters-e-boot-order`

---

### World Lifecycle — Reset & Hooks

*(Referência cruzada)*

* **Owner operacional**
  `WorldLifecycle/WorldLifecycle.md`

* **Guardrails arquiteturais**

    * `WorldLifecycleHookRegistry` nasce **exclusivamente** no `NewSceneBootstrapper`
    * Segundo registro:

        * reutiliza a instância existente
        * loga erro explícito
    * Hooks são:

        * opt-in
        * ordenados por `(Order, Type.FullName)`
        * sem heurísticas implícitas

* **Fontes de hooks**

    * Spawn services
    * Serviços de cena via DI
    * Hooks registrados explicitamente no registry
    * `IActorLifecycleHook` em atores

  ➜ Todos executados pelo `WorldLifecycleOrchestrator` conforme o pipeline oficial

* **Otimização**

    * Cache de hooks de ator:

        * válido apenas por ciclo de reset
        * limpo no `finally`
    * Evita múltiplas varreduras de hierarquia

* **Determinismo**

    * Falhas interrompem o reset (*fail-fast*)
    * Ordem de execução:

        * estável
        * reprodutível
        * idêntica entre cenas e resets

---

## Planned (To-Be / Roadmap)

* Bootstrap global adicional para serviços compartilhados entre cenas, **sem violar separação de estado**
* Ampliação dos contratos de eventos e telemetria:

    * suporte completo a multiplayer local
    * rastreamento explícito de identidade
* Expansão dos testes automatizados e utilitários:

    * documentados em `Guides/UTILS-SYSTEMS-GUIDE.md`
* Evolução dos guias de DI e EventBus:

    * cenários mais complexos
    * mantendo registro explícito e previsível

---

## Nota de Consolidação Semântica (2025)

Este documento descreve **a arquitetura de simulação e world lifecycle**.

Fluxos de:

* menus visuais
* navegação de UI
* App Frontend

são **domínios distintos** e não devem ser inferidos a partir deste arquivo.

A separação formal entre:

* **App / Frontend**
* **Simulation Runtime**

é intencional e será detalhada em documentos complementares, sem invalidar este.
