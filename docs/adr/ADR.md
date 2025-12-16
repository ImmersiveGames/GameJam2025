# GameJam2025

## Atualizações 02/2026 — Defesa Planetária v2.2

### Consolidação final
- Interfaces de defesa centralizadas em `PlanetDefenseInterfaces.cs`, incluindo `IPlanetDefenseSetupOrchestrator`, `IPlanetDefensePoolRunner` e `IPlanetDefenseWaveRunner`, para manter contratos únicos e alinhados ao DI do projeto.
- Implementações concretas ativas: `PlanetDefenseOrchestrationService`, `RealPlanetDefensePoolRunner` e `RealPlanetDefenseWaveRunner` substituem stubs antigos, usando `WavePresetSo` + `DefenseEntryConfigSO` para preparar contexto, aquecer pools e disparar waves com `CountdownTimer`.
- Eventos em `PlanetDefenseEvents.cs` carregam apenas dados de runtime (planeta, detector, tipo de detecção, papel/contador e contexto de spawn) — nenhum ScriptableObject de configuração é propagado pelos eventos.
- Sobrecargas/métodos de debug não utilizados foram removidos, mantendo somente APIs chamadas pelo fluxo de runtime e simplificando a documentação.

### Divisão por Responsabilidade Única
- O antigo `PlanetDefenseSpawnService` foi dividido em dois serviços explícitos: `PlanetDefenseOrchestrationService` (prepara contexto/pool/waves) e `PlanetDefenseEventService` (processa eventos e delega ao orquestrador). Ambos são instanciados pelo `PlanetDefenseController` e registrados por `ActorId` via `DependencyManager`.
- O `PlanetDefenseEventHandler` agora resolve o `PlanetDefenseEventService` para encaminhar eventos `Engaged/Disengaged/Disabled/MinionSpawned`, mantendo o orquestrador puro e reutilizável.

### Correção Passo 1: Removida struct obsoleta – SO como fonte única de config
- Eliminada a struct/classe `PlanetDefenseSpawnConfig`; o `PlanetDefenseController` não cria nem aplica mais este config e passa somente o `DefenseWaveProfileSO` atribuído no Inspector para o serviço de spawn.
- Removidas referências residuais ao caminho legado (`BuildPlanetConfig`) no controlador, garantindo que apenas o ScriptableObject do Inspector e o `PoolData` sejam usados para configurar o serviço.
- O `PlanetDefenseOrchestrationService` recebe o profile via `SetWaveProfile` (comentado em português para lembrar que SO não é injetado via DI) e registra bindings do `EventBus` após a injeção, garantindo que eventos `Engaged/Disengaged/Disabled` acionem warm-up e start/stop de ondas (via `PlanetDefenseEventService`).
- `RealPlanetDefenseWaveRunner` usa os valores do `DefenseWaveProfileSO` presente no `PlanetDefenseSetupContext` para intervalo e quantidade de spawns, mantendo timers alinhados ao asset configurado no Inspector e evitando valores duplicados em structs.
- Removidos resíduos de referências ao antigo config e corrigido o uso de `PlanetDefenseSetupContext` no runner de waves para evitar variáveis inexistentes em `SpawnWave`.

### Correção Passo 1: Removida injeção DI em SO – configs via Inspector
- ScriptableObjects (`DefenseWaveProfileSO`) deixam de ser injetados via DI e agora são atribuídos diretamente pelo `PlanetDefenseController` usando `SetWaveProfile`, mantendo o Inspector como fonte única de configuração.
- `PlanetDefenseOrchestrationService` recebe o profile via método público (com aviso explícito em português) e repassa ao `PlanetDefenseSetupContext`, evitando criação dinâmica de assets e mantendo o fluxo controlado por cena.
- Logs verbosos foram adicionados para confirmar o profile atribuído e para alertar quando nenhum profile for fornecido, facilitando depuração de cenas multiplayer locais.

### Correção Passo 1: PoolData não injetado
- Movido o aviso de PoolData ausente para ocorrer somente após a injeção de dependências e configuração do PoolData, garantindo que assets atribuídos no Inspector (ex.: `PoolDataDefenses.asset`) sejam respeitados antes de qualquer log de alerta.
- Adicionados logs verbosos no `PlanetDefenseOrchestrationService` para registrar o PoolData padrão configurado e o flag `WarmUpPools`, facilitando depuração de cenas onde o serviço é instanciado via código e o PoolData é definido em `PlanetDefenseController`.
- Centralizado o uso do `DefenseWaveProfileSO` como fonte única de configuração das ondas (intervalo, minions, raio/altura) por planeta, compartilhando a mesma instância via DI sem criar ScriptableObjects em runtime.
- Removido o caminho legado `BuildPlanetConfig`/`PlanetDefenseSpawnConfig` no `PlanetDefenseController`: o serviço recebe apenas o `DefenseWaveProfileSO` e o `PoolData` atribuídos no Inspector, evitando config duplicado ignorado em runtime.

### Passo 1 — Avaliação e Preparação (Pré-Refatoração)
- **Escopo analisado:** `PlanetDefenseController`, `PlanetDefenseDetectable`, `PlanetDefenseEventService`, `PlanetDefenseOrchestrationService`, `RealPlanetDefensePoolRunner`, `RealPlanetDefenseWaveRunner` e o fluxo de ScriptableObjects (`DefenseEntryConfigSO`, `WavePresetSo`). Referências externas observadas: `PlanetsMaster`, `PlanetsManager`, `EventBus`, `DetectionSystems`, `PoolSystem`.
- **Fluxo atual mapeado:**
  1. Sensores (`IDetector`) entram em alcance do planeta via `PlanetDefenseDetectable`, que registra a entrada para evitar chamadas duplicadas e aciona `PlanetDefenseController.EngageDefense` com o `DetectionType`.【F:Assets/_ImmersiveGames/Scripts/PlanetSystems/Detectable/PlanetDefenseDetectable.cs†L32-L78】
  2. `PlanetDefenseController` resolve o papel (`DefenseRole`) do detector, mantém contagem local de detectores ativos e publica `PlanetDefenseEngagedEvent` com flag de primeira ativação e total de detectores. Desengates removem da tabela e publicam `PlanetDefenseDisengagedEvent`; ao desabilitar o objeto, emite `PlanetDefenseDisabledEvent` e limpa estado.【F:Assets/_ImmersiveGames/Scripts/PlanetSystems/Detectable/PlanetDefenseController.cs†L13-L115】
  3. `PlanetDefenseEventService` escuta os três eventos para rastrear estado de defesa por planeta, delegando aquecimento de pool/waves para o orquestrador e registrando contadores. Logs verbosos auxiliam na depuração e o cache de contexto é limpo no disable.
  4. `RealPlanetDefensePoolRunner` registra pools reais no `PoolManager` a partir do `WavePresetSo`, e `RealPlanetDefenseWaveRunner` roda o loop de waves com `CountdownTimer`, fazendo spawn e publicando `PlanetDefenseMinionSpawnedEvent` por planeta.【F:Assets/_ImmersiveGames/Scripts/PlanetSystems/Defense/RealPlanetDefensePoolRunner.cs†L1-L94】【F:Assets/_ImmersiveGames/Scripts/PlanetSystems/Defense/RealPlanetDefenseWaveRunner.cs†L1-L120】【F:Assets/_ImmersiveGames/Scripts/PlanetSystems/Defense/PlanetDefenseEvents.cs†L51-L71】
  5. Configuração de minions agora ocorre apenas via `WavePresetSo` (pool e padrão de spawn) e `DefenseMinionBehaviorProfileSO` (comportamento por role/wave); o antigo `DefenseMinionConfigSO` foi removido.

### Dependências identificadas
- **PlanetsMaster**: usado como chave de estado em controlador/spawn service; fornece `ActorName` para logs. Integração com `PlanetsManager` via eventos de morte (potencial ponto de integração futuro).【F:Assets/_ImmersiveGames/Scripts/PlanetSystems/Detectable/PlanetDefenseController.cs†L13-L115】【F:Assets/_ImmersiveGames/Scripts/PlanetSystems/PlanetsMaster.cs†L1-L74】
- **EventBus**: transporte principal entre detecção e serviços de defesa; bindings registrados no `OnEnable`/`OnDisable` do spawn service. Sem verificação de duplicidade de registro além de nulidade.
- **DetectionSystems**: `IDetector`, `DetectionType`, `AbstractDetectable` definem contratos de entrada/saída. `PlanetDefenseDetectable` assume um `myDetectionType` herdado.
- **PoolSystem e Wave Runners**: implementações concretas (`RealPlanetDefensePoolRunner`, `RealPlanetDefenseWaveRunner`) usam `PoolManager`, `WavePresetSo` e `CountdownTimer` para rodar em runtime sem corrotinas, mantendo compatibilidade via contratos de `PlanetDefenseInterfaces.cs`.
- **Multiplayer local**: nenhum código atual trata identificação de jogadores múltiplos além de heurísticas de nome em `ResolveDefenseRole` (busca "Player" no nome). Pode gerar ambiguidades em sessões locais com vários players.

### Pontos fracos e riscos
- **Dependência de nomes para papel**: `ResolveDefenseRole` inspeciona strings em `ActorName`, quebrando princípio de fonte única de verdade e tornando o comportamento frágil para multiplayer local (nomes duplicados). Necessário Strategy/Provider formal para papel de defesa.【F:Assets/_ImmersiveGames/Scripts/PlanetSystems/Detectable/PlanetDefenseController.cs†L62-L108】
- **Acoplamento a logs e Update**: serviços atuais removem dependência de `Update`, delegando logs periódicos ao `DefenseDebugLogger` e acionando runners apenas em eventos.
- **Lacuna de sincronização**: `OnDisable` de `PlanetDefenseDetectable` força `DisengageDefense` para cada detector registrado, mas o controlador apenas limpa contagem e publica evento em `OnDisable` se ainda houver detectores. Em pipelines rápidos, pode gerar eventos redundantes. Revisar responsabilidade de limpeza única.
- **Ausência de telemetria central**: não há agregador para múltiplos serviços de defesa (pool, ondas, efeitos). EventBus cobre transporte, mas falta um orchestrator aplicando Strategy/DI para cada planeta (ex.: `IPlanetDefenseOrchestrator`).
- **Teste/Debug**: Sem testes automatizados. Interpretação de `DetectionType` como `TypeName` para logs; não há validação de nulos na origem do evento (detector/planeta nulo apenas retorna silenciosamente). Pode mascarar falhas.

### Recomendações para próximos passos
- Introduzir **interfaces claras de papel** (`IDefenseRoleProvider` concreto ou Strategy) e eliminar heurística por nome; permitir binding por jogador local (ex.: inject `IPlayerIdentity`).
- Criar **serviço orquestrador** que reaja aos eventos e coordene runners reais (pool/wave); etapa concluída com `PlanetDefenseOrchestrationService` + `PlanetDefenseEventService`.
- Substituir uso de `Update` por **timers/Tasks** específicos ou corrotinas encapsuladas por planeta para evitar conflito em multiplayer local e reduzir carga de logs.
- Formalizar **contratos de DI** para `IPlanetDefensePoolRunner`/`IPlanetDefenseWaveRunner`, com configuração por ScriptableObject para cenas locais múltiplas.
- Adicionar **testes de integração** para fluxo de eventos (engage → start waves; disengage → stop) e cenários de desabilitação, garantindo idempotência e contagem correta.

### Passo 2 — Refatorar Interfaces e Abstrações (D/I do SOLID)
- **Interfaces segmentadas de listener**: `IDefenseEngagedListener`, `IDefenseDisengagedListener` e `IDefenseDisabledListener` permanecem separadas; o listener agregado foi removido para reforçar Interface Segregation e evitar dependência em contratos genéricos.
- **Contratos de configuração por planeta**: `IPlanetDefensePoolRunner` e `IPlanetDefenseWaveRunner` expõem métodos para configurar minions, recursos e estratégias por planeta (`ConfigureForPlanet`, `TryGetConfiguration`, `WarmUp(PlanetDefenseSetupContext)`, `ConfigureStrategy`, `TryGetStrategy`, `StartWaves` com estratégia). As interfaces vivem em `PlanetDefenseInterfaces.cs`, e as implementações concretas (`RealPlanetDefensePoolRunner`, `RealPlanetDefenseWaveRunner`) executam de fato o aquecimento de pool e o loop de waves usando `WavePresetSo` + `CountdownTimer`.
- **Strategy Pattern preparado**: a interface `IDefenseStrategy` em `PlanetDefenseInterfaces.cs` trabalha com `PlanetDefenseSetupContext`, permitindo injeção de comportamentos diferentes (agressivo/defensivo) por planeta ou recurso, reforçando Dependency Inversion ao isolar decisões de spawn de implementações concretas. O contrato é neutro para multiplayer local e pode ser usado pelos runners ou orchestrators futuros.【F:Assets/_ImmersiveGames/Scripts/PlanetSystems/Defense/PlanetDefenseInterfaces.cs†L33-L62】

### Passo 3 — Sistema de Roles Explícitos
- **Provedores explícitos de role**: a heurística por string foi encapsulada atrás do contrato `IDefenseRoleProvider.GetDefenseRole()`. O `ActorMaster` é a fonte primária do papel defensivo, garantindo configuração por prefab/GameObject e reduzindo fragilidade em sessões multiplayer locais.【F:Assets/_ImmersiveGames/Scripts/DetectionsSystems/Core/DefenseRole.cs†L1-L19】【F:Assets/_ImmersiveGames/Scripts/PlayerControllerSystem/Detections/PlayerDetectionController.cs†L19-L98】【F:Assets/_ImmersiveGames/Scripts/EaterSystem/Detections/EaterDetectionController.cs†L11-L93】【F:Assets/_ImmersiveGames/Scripts/ActorSystems/ActorMaster.cs†L9-L114】
- **Resolução ordenada no controlador**: `PlanetDefenseController` agora confia apenas nos providers (`IDefenseRoleProvider`) presentes no detector ou em seu Owner; o fallback antigo baseado em configuração foi removido, e casos sem provider retornam `Unknown` para forçar configuração explícita em prefabs/atores.【F:Assets/_ImmersiveGames/Scripts/PlanetSystems/Detectable/PlanetDefenseController.cs†L17-L180】

### 26/11/2025 — Integração de DefenseRole no ActorMaster
- **Papel configurável por ator**: `ActorMaster` agora expõe `DefenseRole` serializado e implementa `IDefenseRoleProvider`, permitindo definir o papel diretamente no prefab ou GameObject e tornando-o acessível para qualquer sistema que consuma `IActor` (sensores, controladores e estratégias).【F:Assets/_ImmersiveGames/Scripts/ActorSystems/ActorMaster.cs†L9-L114】【F:Assets/_ImmersiveGames/Scripts/ActorSystems/IActor.cs†L1-L25】
- **Detectores delegando ao ator**: `PlayerDetectionController` e `EaterDetectionController` deixam de implementar o contrato de role e delegam à hierarquia do ator, reduzindo duplicação e garantindo consistência entre sensores e outros componentes que derivam do mesmo `ActorMaster`. Isso mantém compatibilidade com o fallback legado de `PlanetDefenseController` sem dependência de heurísticas por nome no detector.【F:Assets/_ImmersiveGames/Scripts/PlayerControllerSystem/Detections/PlayerDetectionController.cs†L1-L178】【F:Assets/_ImmersiveGames/Scripts/EaterSystem/Detections/EaterDetectionController.cs†L1-L105】

### 26/11/2025 — Debug para Fontes de Resolução de Role
- **Telemetria de resolução**: `PlanetDefenseController.ResolveDefenseRole` registra logs verbosos diferenciando se o papel veio do provider do detector ou do Owner; como o fallback por configuração foi removido, mensagens `Unknown` indicam falta de provider e precisam de ajuste no prefab/ator.【F:Assets/_ImmersiveGames/Scripts/PlanetSystems/Detectable/PlanetDefenseController.cs†L97-L176】
- **Dicas de teste**: habilite o nível Verbose no `DebugUtility` para ver mensagens de origem de role ao engajar/desengajar defesas. Prefabs que ainda retornam `Unknown` devem receber implementação de `IDefenseRoleProvider` (ex.: no `ActorMaster`).

### 26/11/2025 — Remoção de Fallback Legacy em ResolveDefenseRole
- **Fonte única e determinística**: `ResolveDefenseRole` agora confia apenas em providers explícitos (detector, Owner); o fallback `DefenseRoleConfig` foi retirado. Casos sem provider retornam `Unknown`, exigindo configuração no prefab/ator e eliminando heurísticas por string sujeitas a conflito em multiplayer local.【F:Assets/_ImmersiveGames/Scripts/PlanetSystems/Detectable/PlanetDefenseController.cs†L97-L176】
- **Monitoramento orientado a ação**: ao retornar `Unknown`, o controlador registra log verboso e recomenda adicionar providers. Isso acelera a migração completa para roles explícitos, preservando clareza de diagnóstico durante testes locais.

### 26/11/2025 — Passo 4: Loop de Spawn com CountdownTimer
- **Separação de responsabilidades**: o fluxo de spawn usa `DefenseStateManager` para rastrear detectores/planetas e delega timers para `RealPlanetDefenseWaveRunner`, removendo `Update`/corrotinas e reduzindo alocação de GC.【F:Assets/_ImmersiveGames/Scripts/PlanetSystems/Defense/DefenseStateManager.cs†L1-L78】【F:Assets/_ImmersiveGames/Scripts/PlanetSystems/Defense/RealPlanetDefenseWaveRunner.cs†L1-L120】
- **Pools reais e ondas com timers**: `RealPlanetDefensePoolRunner` registra/aquece pools no `PoolManager` com `PoolData` configurado em `WavePresetSo`, e `RealPlanetDefenseWaveRunner` usa `CountdownTimer` por planeta para disparar waves, fazer spawn via `ObjectPool` e emitir `PlanetDefenseMinionSpawnedEvent`.【F:Assets/_ImmersiveGames/Scripts/PlanetSystems/Defense/RealPlanetDefensePoolRunner.cs†L1-L94】【F:Assets/_ImmersiveGames/Scripts/PlanetSystems/Defense/RealPlanetDefenseWaveRunner.cs†L1-L120】【F:Assets/_ImmersiveGames/Scripts/PlanetSystems/Defense/PlanetDefenseEvents.cs†L51-L71】
- **Serviço orquestrador**: `PlanetDefenseOrchestrationService` injeta os runners reais, cria `PlanetDefenseSetupContext` com recurso do planeta/pool configurado, aquece pools, inicia/paralisa ondas conforme engajamento/disengajamento e limpa pools em disable, preservando logs verbosos e compatibilidade de eventos.
- **Bootstrap e DI**: `DependencyBootstrapper` registra o `DefenseStateManager`, runners reais e o serviço de spawn como singletons de cena, garantindo injeção e ciclo de vida correto para multiplayer local.【F:Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/DependencyBootstrapper.cs†L1-L143】

### 26/11/2025 — Registro de serviços
- **Registro manual e explícito**: `PlanetDefenseOrchestrationService` e `PlanetDefenseEventService` permanecem desacoplados de inicialização automática; adicione-os à cena e resolva suas dependências via `DependencyManager` conforme necessidade, utilizando apenas os contratos segmentados (`IDefenseEngagedListener`, `IDefenseDisengagedListener`, `IDefenseDisabledListener`) em registradores ou event handlers.

### 26/11/2025 — Estado do DefenseRoleConfig
- O `DefenseRoleConfig` permanece no projeto apenas para compatibilidade com cenas antigas, mas o `PlanetDefenseController` não consulta mais esse asset; use providers (`IDefenseRoleProvider`) nos prefabs para definir o papel de defesa de forma explícita.【F:Assets/_ImmersiveGames/Scripts/PlanetSystems/Detectable/PlanetDefenseController.cs†L17-L180】

## ADR: World Lifecycle Hooks Architecture

### Context
- Necessidade de reset determinístico para manter consistência de mundo, atores e serviços de spawn.
- Evitar acoplamento entre sistemas de UI, áudio, analytics e componentes de atores durante o ciclo de reset.

### Decision
- Introdução de hooks opt-in em múltiplos níveis, permitindo que serviços de spawn, serviços de cena e componentes de atores participem do ciclo.
- Separação clara entre responsabilidades de world, scene e actor, cada uma com registro explícito e resolução sem reflection.
- Registry é criado no bootstrapper (escopo de cena) e consumido por DI; proibido criar/registrar fora do bootstrapper.

### Consequences
- Mais flexibilidade para instrumentar resets com telemetria, debug e limpeza direcionada.
- Custo mínimo de complexidade ao manter contratos simples e determinísticos.
- Arquitetura extensível para multiplayer local, replay e testes automatizados.
- Previne duplo-registro, evita divergência de instância e garante previsibilidade em testes/QA.

### Boot order & Dependency Injection timing (Scene scope)
- Serviços de cena (`IActorRegistry`, `IWorldSpawnServiceRegistry`, `WorldLifecycleHookRegistry`) nascem no `NewSceneBootstrapper`; nenhum outro componente deve criá-los ou registrá-los novamente.
- Consumidores de cena evitam injetar no `Awake()` sem garantir ordem. Preferir `Start()` ou injeção lazy com retry curto/timeout para não rodar antes do bootstrap.
- Testes/QA adotam o padrão lazy + retry: aguardam alguns frames por registro do bootstrapper e abortam com mensagem acionável se a cena ainda não registrou os serviços. Isso reduz falsos negativos em cenas novas sem alterar o `WorldLifecycleOrchestrator`.

### Deterministic Ordering for World and Actor Lifecycle Hooks

#### Context
- O sistema de reset do mundo executa múltiplos tipos de hooks:
  - hooks de serviços e de cena (`IWorldLifecycleHook`)
  - hooks de componentes de ator (`IActorLifecycleHook`)
- A ordem de enumeração dessas coleções não é garantida por:
  - DI (`GetAllForScene`)
  - `GetComponentsInChildren`
  - ordem de registro acidental
- Falta de determinismo gera bugs difíceis de reproduzir durante reset, QA e multiplayer local.

#### Decision
- Todos os hooks de lifecycle, independentemente da origem, são executados em ordem determinística.
- A ordenação segue o critério:
  - `Order` (quando o hook implementa `IOrderedLifecycleHook`, default = 0)
  - `Type.FullName` (ordem ordinal) como desempate estável
- A regra aplica-se igualmente a:
  - hooks de mundo (`IWorldLifecycleHook`)
  - hooks de ator (`IActorLifecycleHook`)
- A ordenação é responsabilidade do `WorldLifecycleOrchestrator`.
- Não é usado reflection nem heurísticas por nome de GameObject.

#### Consequences
- A execução do reset torna-se estável entre:
  - múltiplos resets
  - diferentes cenas
  - Editor vs Build
- Hooks passam a ter um mecanismo explícito de prioridade sem acoplamento.
- Alterações de hierarquia ou ordem de componentes não afetam a execução.
- Desenvolvedores devem definir `Order` apenas quando necessário; a maioria dos hooks pode permanecer com o valor padrão.

### Lazy Injection and Boot Order Tolerance for Scene Consumers

#### Context
- Serviços de cena (`IActorRegistry`, `IWorldSpawnServiceRegistry`, `WorldLifecycleHookRegistry`) são criados exclusivamente pelo `NewSceneBootstrapper`.
- Componentes de cena (QA, debug, ferramentas, controladores auxiliares) podem executar antes do bootstrap dependendo da ordem de execução do Unity.
- Injeção direta no `Awake()` pode falhar legitimamente em cenas novas ou em cenários de desenvolvimento (`NEWSCRIPTS_MODE`).
- Falhas precoces geram falsos negativos, confundindo erros de ordem de boot com falhas reais de runtime.

#### Decision
- Componentes consumidores de serviços de scene-scope:
  - não devem assumir disponibilidade no `Awake()`;
  - devem usar lazy injection com retry curto e timeout controlado.
- Padrão recomendado:
  - tentar injeção em `Start()` ou via rotina assíncrona curta;
  - abortar com mensagem acionável se o bootstrapper não rodou.
- O `WorldLifecycleOrchestrator` não corrige nem compensa ordem de boot; ele assume dependências resolvidas corretamente pelo fluxo de bootstrap.
- Aplica-se especialmente a QA/Testers, ferramentas de debug e instaladores auxiliares de hooks.

#### Consequences
- Redução de falsos negativos em QA e testes locais.
- Separação clara entre erro de boot/ordem de execução e erro real de lógica de reset.
- Fluxo de inicialização mais resiliente sem introduzir acoplamento temporal no core do sistema.
- Boot order torna-se um contrato explícito, não uma suposição implícita.

### Explicit Separation of World, Scene, and Actor Lifecycle Responsibilities

#### Context
- O ciclo de reset envolve múltiplos níveis:
  - mundo (spawn/despawn e serviços)
  - cena (serviços e ferramentas registradas por escopo)
  - ator (componentes `MonoBehaviour`)
- Misturar responsabilidades (ex.: ator criando registry, controller criando hooks de cena) gera acoplamento indevido, duplicação de instâncias e comportamento imprevisível em resets.
- Projetos Unity tendem a colapsar essas camadas sem um contrato explícito.

#### Decision
- Responsabilidades são explicitamente separadas:
  - **World**: orquestra o reset e executa hooks; não cria serviços de cena.
  - **Scene**: cria e registra serviços/registries no bootstrap (`NewSceneBootstrapper`).
  - **Actor**: reage ao reset via componentes (`IActorLifecycleHook`); não registra hooks globais.
- `WorldLifecycleHookRegistry` nasce apenas no bootstrapper da cena e é consumido via DI por QA, debug e ferramentas.
- Hooks são opt-in e não implícitos; nenhum nível cria hooks para outro automaticamente.

#### Consequences
- Arquitetura previsível e escalável.
- Evita efeitos colaterais entre cenas, atores e serviços globais.
- Facilita testes, QA e evolução do sistema (ex.: multiplayer local, replay).
- Permite extensão controlada sem violar princípios SOLID ou DI.
