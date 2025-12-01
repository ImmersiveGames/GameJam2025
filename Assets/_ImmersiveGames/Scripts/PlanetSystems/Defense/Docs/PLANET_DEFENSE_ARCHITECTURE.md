# Arquitetura do Sistema de Defesa Planetária

## Visão Geral Rápida
Este documento explica como o sistema de defesa é montado, como conectar os componentes e como criar loadouts completos em menos de 5 minutos. Todo o código está em C# para Unity 6, com foco em SOLID e extensibilidade.

### Diagrama ASCII
```
PlanetsManager
  └── Instantiate planetPrefab
      └── PlanetsMaster (Awake)
          └── PlanetDefenseController
              ├── PlanetDefenseOrchestrationService
              |   └── ConfigureLoadout(planet, loadout)
              |   └── Build context (pool, waveProfile, strategy)
              |   └── RealPlanetDefenseWaveRunner
              |       └── Loop de waves → spawn via pool
              |           └── DefenseMinionController (coordenador)
              |               ├── MinionEntryHandler
              |               ├── MinionOrbitWaitHandler
              |               └── MinionChaseHandler
              └── PlanetDefenseEventService
```

### Fluxo Completo (runtime)
1) **PlanetsManager** instancia `planetPrefab` e escolhe um `PlanetDefenseLoadoutSO` a partir de `possibleLoadouts` (por índice ou aleatório) para cada planeta.
2) **PlanetsMaster.Awake** recebe o loadout e chama `PlanetDefenseOrchestrationService.ConfigureLoadout(this, loadout)`, preservando SRP (spawn + defesa) e DIP (loadout injetado).
3) **PlanetDefenseController** inicializa o `PlanetDefenseOrchestrationService` (setup/caches) e o `PlanetDefenseEventService` (eventos). O orquestrador constrói o contexto com pool data, wave profile e estratégia (`IDefenseStrategy`). Logs verbosos mostram qual loadout foi aplicado.
4) **RealPlanetDefenseWaveRunner** pega o alvo primário (label + role), consulta a estratégia para resolver o profile do minion com base no role e aplica o profile antes de ativar cada poolable, evitando piscadas.
5) **DefenseMinionController** coordena `MinionEntryHandler`, `MinionOrbitWaitHandler` e `MinionChaseHandler` para executar entrada, espera em órbita e perseguição conforme o profile recebido.
6) Qualquer handler pode ser desabilitado individualmente no prefab (por exemplo, pausar perseguição) sem afetar os demais estágios.

### Guia de 5 minutos – Criar uma defesa customizada
1. **Perfis de minion**: crie `DefenseMinionBehaviorProfileSO` via `Create → ImmersiveGames → PlanetSystems → Defense → Minions → Behavior Profile V2`.
   - Defina `variantId`, `entryDurationSeconds`, `initialScaleFactor`, `orbitIdleSeconds`, `chaseSpeed`, e estratégias (`MinionEntryStrategySO`, `MinionChaseStrategySO`).
2. **Wave profile**: crie um `DefenseWaveProfileSO` e preencha `defaultMinionProfile` com o profile desejado para a onda.
3. **Estratégia (opcional)**: crie um `DefenseStrategySO` concreto (ex.: `AggressiveEaterStrategySO`, `DefensivePlayerStrategySO`) para escolher profiles diferentes por `DefenseRole`.
4. **Loadout completo**: crie `PlanetDefenseLoadoutSO` com pool data, wave profile e estratégia (quando necessário).
5. **Planetas**: no `PlanetsManager`, preencha `possibleLoadouts` com os loadouts criados. O manager atribui automaticamente um loadout por planeta ao instanciar o prefab.
6. Pressione Play e verifique os logs verbosos (`[Loadout]` e aplicação de profile) para confirmar que cada planeta carregou sua configuração.

### Exemplos de Loadout
- **Peaceful**: minions lentos, `entryDurationSeconds` maior, `chaseSpeed` baixo, estratégia defensiva (arco). Ideal para planetas seguros ou tutoriais.
- **Balanced**: valores médios de entrada/orbita/perseguição; estratégia padrão usando `defaultMinionProfile` do wave profile sem overrides por role.
- **Berserk**: entrada rápida, `initialScaleFactor` menor (spawn discreto), `chaseSpeed` alto e estratégia agressiva (zigzag para Eater, rápido arco curto para Player).

## Histórico de testes temporários
- **Etapa 3 (aplicação de profile no spawn)**: teste de validação visual concluído com sucesso. O script temporário `MinionSpawnTest` foi removido após confirmar que o profile é aplicado no instante do spawn, eliminando piscadas visuais.
- **Etapa 4 (remoção de campos duplicados em MonoBehaviours)**: campos de configuração visíveis em prefabs foram removidos do `DefenseMinionController`, mantendo o comportamento 100% guiado por profiles para evitar discrepâncias entre dados do prefab e do profile.
- **Etapa 5 (SRP aplicado aos minions)**: `DefenseMinionController` agora coordena três handlers especializados (entrada, espera em órbita e perseguição), permitindo desabilitar etapas individualmente sem alterar a lógica central.
- **Etapa 6 (WaveProfile como pacote completo)**: `DefenseWaveProfileSO` passou a definir também o `defaultMinionProfile`, permitindo criar ondas com comportamentos distintos (ex.: rápida + zigzag vs. lenta + arco) apenas trocando o asset de wave sem tocar em código.
- **Etapa 7 (Loadouts por planeta)**: cada planeta agora recebe um `PlanetDefenseLoadoutSO` completo (pool, wave profile e estratégia), garantindo que loadouts distintos gerem defesas totalmente diferentes sem depender de configurações globais.
- **Etapa 8 (Estratégias ativas)**: `IDefenseStrategy` passou a decidir o profile aplicado por alvo, permitindo respostas distintas (ex.: minions rápidos e zigzag para Eater; lentos e em arco para Player) apenas trocando o loadout do planeta.

## Atualização 01/12/2025 – Loadouts por Planeta
- Atribuição runtime no `PlanetsManager` via array de loadouts para variações em instâncias de um único prefab. Exemplo: para 3 planetas, use `loadouts[0]` no primeiro e sorteie para os demais, garantindo defesas distintas sem alterar prefabs.

## Atualização 01/12/2025 – Estratégias de Defesa Vivas
- `IDefenseStrategy` agora é usada para resolver profiles de minion por alvo. Exemplo de configuração: atribua `AggressiveEaterStrategy` no loadout de um planeta com profile zigzag rápido e `DefensivePlayerStrategy` com profile lento de arco para Players. Ao detectar cada role, o runner aplica o profile correspondente antes do spawn, sem tocar em código.

## Atualização 02/12/2025 – Limpeza Final
- Remoção de aliases obsoletos e referências mortas para eliminar warnings do console e manter o sistema de defesa sem código legado. O controle de alvo agora ocorre apenas via `SetTarget`, alinhando o fluxo de roles centralizado no runner.
