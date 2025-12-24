# GameJam2025

## Atualizações 02/12/2025 — Defesa Planetária v2.2

- Política global: ver `../DECISIONS.md` (seção “Política de Uso do Legado”); NewScripts é fonte de verdade e qualquer reaproveitamento do legado exige autorização explícita.

### ADR – Ciclo de Vida do Jogo, Reset por Escopos e Fases Determinísticas
- Documenta fases formais (`SceneScopeReady → GameplayReady`), reset por escopo (soft/hard), passes de spawn e late bind de UI cross-scene.
- Integra Scene Flow, WorldLifecycle e `SimulationGateService` sem alterar APIs atuais, priorizando determinismo e telemetria.
- Inclui linha do tempo oficial e plano de implementação por fases para adoção incremental.
- Fonte operacional do pipeline/hook ordering/QA: `../WorldLifecycle/WorldLifecycle.md`.

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
- **Bootstrap e DI**: no NewScripts, o `DependencyManager` provê registries por escopo (global/cena/objeto) e o `NewSceneBootstrapper` registra serviços de cena de forma determinística, garantindo injeção e ciclo de vida correto para multiplayer local.【F:Assets/_ImmersiveGames/NewScripts/Infrastructure/DI/DependencyManager.cs†L24-L139】【F:Assets/_ImmersiveGames/NewScripts/Infrastructure/Scene/NewSceneBootstrapper.cs†L25-L118】

### 26/11/2025 — Registro de serviços
- **Registro manual e explícito**: `PlanetDefenseOrchestrationService` e `PlanetDefenseEventService` permanecem desacoplados de inicialização automática; adicione-os à cena e resolva suas dependências via `DependencyManager` conforme necessidade, utilizando apenas os contratos segmentados (`IDefenseEngagedListener`, `IDefenseDisengagedListener`, `IDefenseDisabledListener`) em registradores ou event handlers.

### 26/11/2025 — Estado do DefenseRoleConfig
- O `DefenseRoleConfig` permanece no projeto apenas para compatibilidade com cenas antigas, mas o `PlanetDefenseController` não consulta mais esse asset; use providers (`IDefenseRoleProvider`) nos prefabs para definir o papel de defesa de forma explícita.【F:Assets/_ImmersiveGames/Scripts/PlanetSystems/Detectable/PlanetDefenseController.cs†L17-L180】

## ADR: World Lifecycle Hooks Architecture (índice)

Owner operacional e detalhes: `../WorldLifecycle/WorldLifecycle.md`. Este resumo mantém as decisões de arquitetura:
- **Propriedade do registry**: `WorldLifecycleHookRegistry` nasce apenas no `NewSceneBootstrapper`; tentativas de recriação reutilizam a instância existente e logam erro.
- **Ordenação determinística**: todos os hooks de mundo/ator seguem (`Order`, `Type.FullName`) e executam em ordem estável entre cenas/resets; responsabilidade do `WorldLifecycleOrchestrator`.
- **Lazy injection / tolerância ao boot**: consumidores de serviços de cena devem usar `Start()` ou retry curto; falhas em `Awake` por ordem de boot são tratadas como diagnóstico, não como bug do orquestrador.
- **Separação de responsabilidades**: world orquestra, scene registra serviços/registries, actors apenas reagem; nenhum nível cria hooks globais para outro.
- **Cache por ciclo**: lista de hooks de ator é cacheada apenas dentro de um `ResetWorldAsync` e limpa no `finally` para reduzir custo sem perder determinismo.

Referências cruzadas:
- Pipeline e troubleshooting: `../WorldLifecycle/WorldLifecycle.md`.
- Decisão de fases/escopos: `ADR-ciclo-de-vida-jogo.md`.
- Guardrails globais: `../DECISIONS.md`.

## ADRs (núcleo NewScripts)

| ADR | Tema | Arquivo |
| --- | --- | --- |
| 001 | World Reset por Despawn + Respawn | `ADR-001-world-reset-por-respawn.md` |
| 002 | Spawn como Pipeline Explícito e Orquestrado | `ADR-002-spawn-pipeline.md` |
| 003 | Escopos de Serviço e Ciclo de Vida | `ADR-003-escopos-servico.md` |
| 004 | Domínios não controlam ciclo de vida | `ADR-004-dominios-nao-controlam-ciclo-de-vida.md` |
| 005 | Gate de simulação ≠ Reset | `ADR-005-gate-nao-e-reset.md` |
| 006 | UI reage ao mundo | `ADR-006-ui-reage-ao-mundo.md` |
| 007 | Testes validam estado final | `ADR-007-testes-estado-final.md` |

