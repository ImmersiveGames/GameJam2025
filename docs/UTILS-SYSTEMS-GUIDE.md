# Guia Técnico da Pasta Uteis

## 1. Visão Geral da Pasta Uteis
A pasta **Uteis** concentra utilidades arquiteturais transversais (Event Bus, Injeção de Dependências, Depuração, Predicados, Pooling, geração de IDs), usadas para manter o núcleo do jogo desacoplado de gameplay e UI. Ela resolve problemas de infraestrutura compartilhada (mensageria, escopos de serviço, logging, reuso de objetos, validação) e não deve abrigar regras de gameplay ou lógica de UI específica; esses módulos apenas fornecem serviços de base consumidos por gameplay e interface. Relações:
- **Gameplay**: consome serviços (ex.: `IEventBus<T>`, `IUniqueIdFactory`, pools) sem conhecer implementações concretas.
- **UI**: utiliza bindings reativos e utilitários (ex.: `IBindableUI`, extensões) mantendo a UI desacoplada de domínios.
- **Infra**: centraliza bootstraps e singletons persistentes; mantém ordem de inicialização e limpeza de escopos.

### Política de Uso do Legado
- Regras completas em `docs/DECISIONS.md` (seção “Política de Uso do Legado”); NewScripts é a fonte de verdade e o legado só serve como referência comportamental.
- Checklist rápido para PR/Commit:
  - Existe alguma referência ao legado?
  - Foi explicitamente autorizado?
  - Foi documentado o motivo e a alternativa considerada?
  - A migração mantém NewScripts como source of truth?

## 2. Mapa de Sistemas
- **Event Bus**
  - Arquivos: `BusEventSystems/*` (incluindo `EventBus.cs`, `FilteredEventBus.cs`, `InjectableEventBus.cs`, `EventBusUtil.cs`, bindings e interfaces).
  - Papel: mensageria tipada, com opção global e filtrada por escopo; integra com DI.
  - Escopo: global por tipo de evento; filtrado por chave (ex.: ActorId) para isolamento por ator/cena.
- **Dependency Injection / Service Registries**
  - Arquivos: `DependencySystems/*` (`DependencyManager`, `DependencyBootstrapper`, registries, `DependencyInjector`, `IDependencyProvider`).
  - Papel: registrar e resolver serviços por escopo (global/cena/objeto) e injetar campos anotados.
  - Escopo: global (singletons), por cena, por objeto; limpeza automática em unload/destruição.
- **Debug / Logging**
  - Arquivos: `DebugSystems/*` (`DebugUtility`, `DebugManager`, `DebugLevelAttribute`, `FrameRateLimiter`).
  - Papel: configurar níveis de log globais e por tipo, aplicar políticas no início da execução; utilitário para limitar FPS em runtime.
  - Escopo: global, com ajustes por instância/tipo.
- **Pooling**
  - Arquivos: `PoolSystems/*` (`PoolManager`, `ObjectPool`, `PoolData`, `LifetimeManager`, `PooledObject`, `IPoolable`).
  - Papel: reuso de objetos com controle de ativação/retorno e eventos; centralizado por `PoolManager` persistente.
  - Escopo: global (manager) com pools específicos por tipo de objeto.
- **Identidade / UniqueId**
  - Arquivos: `UniqueIdFactory.cs`.
  - Papel: gerar IDs consistentes para atores/objetos, com contagem incremental por nome e integração com `IActor`.
  - Escopo: global, mas retorna IDs por ator/objeto.
- **Predicados / Gate**
  - Arquivos: `Predicates/*` (`IPredicate`, `And/Or/Not`, `Preconditions`, extensões).
  - Papel: compor regras booleanas e validar pré-condições antes de executar lógica.
  - Escopo: usado em qualquer camada para evitar ifs acoplados.
- **Helpers de Cálculo/Extensão**
  - Arquivos: `CalculateRealLength.cs`, `Extensions/*` (assíncrono, componente, transform, UI fill amount).
  - Papel: utilidades pontuais de cálculo de bounds e extensões de API Unity.
  - Escopo: local ao uso; sem estado.

## 3. Guia de Uso por Sistema
### Event Bus
#### 3.1 Propósito
Sistema de publicação/assinatura tipado que evita acoplamento direto entre produtores e consumidores de eventos, inclusive entre múltiplos jogadores no multiplayer local.
#### 3.2 Quando usar
- Notificar UI ou outros serviços sobre mudanças de estado sem referência direta ao emissor.
- Propagar eventos por jogador/ator usando `FilteredEventBus<T>` com chave de escopo (ex.: `actorId`).
- Expor eventos de infraestrutura (ex.: carregamento de cena, atributos runtime) para listeners opt-in.
#### 3.3 Quando NÃO usar
- Evitar uso para chamadas síncronas de comando (não garante ordem de execução nem retorno).
- Não publicar eventos sem registrar/unregistrar bindings adequadamente (risco de vazamento entre cenas).
- Não misturar escopos (ex.: usar ActorId errado) para evitar listeners cruzados.
#### 3.4 Ciclo de vida
- Nascimento: `EventBusUtil.Initialize()` cria os buses antes da primeira cena (`BeforeSceneLoad`) e registra limpeza no Editor ao sair do Play Mode.【F:Assets/_ImmersiveGames/Scripts/Utils/BusEventSystems/EventBusUtil.cs†L38-L74】
- Uso: bindings registrados/desregistrados por componente (`EventBus<T>.Register/Unregister`, `FilteredEventBus<T>.Register/Unregister`).
- Limpeza: `EventBus<T>.Clear()` limpa um tipo; `FilteredEventBus<T>.Unregister(scope)` remove bindings de um escopo; `EventBusUtil.ClearAllBuses()` limpa todos (chamado no Editor ao sair do Play Mode).【F:Assets/_ImmersiveGames/Scripts/Utils/BusEventSystems/EventBusUtil.cs†L25-L35】【F:Assets/_ImmersiveGames/Scripts/Utils/BusEventSystems/FilteredEventBus.cs†L16-L58】
#### 3.5 Regras implícitas detectadas
- Assume que `EventBusUtil.EventTypes` foi populado antes de `DependencyBootstrapper.RegisterEventBuses()`; caso contrário, buses injetáveis não são registrados.【F:Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/DependencyBootstrapper.cs†L140-L175】
- Handlers que lançam exceção não interrompem o bus, mas são logados; não confiar em ordem determinística de execução.【F:Assets/_ImmersiveGames/Scripts/Utils/BusEventSystems/InjectableEventBus.cs†L22-L43】
- Bindings precisam ser desregistrados manualmente em `OnDisable/OnDestroy` para evitar notificações de objetos destruídos (não há coleta automática fora dos `Clear`).

### Dependency Injection / Registries
#### 3.1 Propósito
Fornecer resolução de serviços por escopo (global/cena/objeto) e injeção em campos anotados, evitando singletons rígidos e permitindo substituições em testes/cenas.
#### 3.2 Quando usar
- Registrar serviços de infraestrutura (carregamento de cena, áudio, gates) que várias cenas compartilham.
- Registrar serviços específicos de uma cena antes de sua lógica começar, para injeção em componentes daquela cena.
- Associar serviços a instâncias de atores/objetos via `objectId` (ex.: HUD de um jogador).
#### 3.3 Quando NÃO usar
- Não registrar serviços de gameplay efêmeros no escopo global (risco de vazamento entre cenas e persistência indesejada).
- Não chamar `InjectDependencies` repetidamente no mesmo frame (o injetor deduplica e silencia chamadas redundantes).【F:Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/DependencyInjector.cs†L19-L63】
- Evitar `TryGet` com `objectId` nulo para serviços que deveriam ser por objeto; use o escopo correto para evitar colisões.
#### 3.4 Ciclo de vida
- Nascimento: `DependencyBootstrapper.Initialize()` (BeforeSceneLoad) força criação do `DependencyManager` e registra serviços essenciais (loader de cena, fade, gates, buses, atributos runtime).【F:Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/DependencyBootstrapper.cs†L26-L118】
- Uso: `DependencyManager` registra/resolve em três registries (`ObjectServiceRegistry`, `SceneServiceRegistry`, `GlobalServiceRegistry`) e injeta dependências via `InjectAttribute`.【F:Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/DependencyManager.cs†L24-L99】【F:Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/DependencyInjector.cs†L19-L109】
- Limpeza: serviços por cena são limpos no unload (`SceneServiceCleaner` assina `sceneUnloaded`), e o manager limpa todos os escopos em `OnDestroy`/`OnApplicationQuit`.【F:Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/SceneServiceCleaner.cs†L10-L23】【F:Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/DependencyManager.cs†L101-L124】
#### 3.5 Regras implícitas detectadas
- `DependencyBootstrapper` assume que `DependencyManager.Provider` existe antes de registrar serviços; não desabilitar o singleton de regulador.
- `SceneServiceRegistry` valida nomes de cena contra build; registrar com nome incorreto gera warning e impede injeção.【F:Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/SceneServiceRegistry.cs†L17-L74】
- Limite de serviços por cena (`maxSceneServices`) pode rejeitar registros adicionais silenciosamente com warning; ajustar se necessário.【F:Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/SceneServiceRegistry.cs†L13-L38】
- Serviços registrados com `allowOverride` descartam implementações anteriores via `Dispose` se implementarem `IDisposable`.【F:Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/GlobalServiceRegistry.cs†L12-L38】

### Debug / Logging
#### 3.1 Propósito
Centralizar políticas de log, níveis e verbosidade para todo o projeto, evitando logs ruidosos e garantindo sinais claros em ambiente Editor/Player.
#### 3.2 Quando usar
- Configurar níveis globais na cena inicial via `DebugManager` (executa cedo com `DefaultExecutionOrder -200`).【F:Assets/_ImmersiveGames/Scripts/Utils/DebugSystems/DebugManager.cs†L5-L41】
- Usar `DebugUtility.Log/LogWarning/LogError/LogVerbose` em serviços para mensagens consistentes, com cores e deduplicação.
- Habilitar `FrameRateLimiter` apenas em sessões de diagnóstico com teclado (Shift+F1..F5) para testar estabilidade de performance.【F:Assets/_ImmersiveGames/Scripts/Utils/DebugSystems/FrameRateLimiter.cs†L1-L16】
#### 3.3 Quando NÃO usar
- Evitar `DebugUtility` em construtores/estáticos que rodem antes de `Initialize` (SubystemRegistration) se depender de níveis configurados.【F:Assets/_ImmersiveGames/Scripts/Utils/DebugSystems/DebugUtility.cs†L17-L50】
- Não deixar `verboseInPlayer` ativado por padrão em builds públicas; é controlado por `DebugManager`.
#### 3.4 Ciclo de vida
- Nascimento: `DebugUtility.Initialize()` roda em `SubsystemRegistration`, resetando níveis/pools de mensagem antes de qualquer sistema.【F:Assets/_ImmersiveGames/Scripts/Utils/DebugSystems/DebugUtility.cs†L17-L50】
- Configuração: `DebugManager.Awake` aplica flags e níveis globais, opcionalmente considerando `GameConfig` para modo debug.【F:Assets/_ImmersiveGames/Scripts/Utils/DebugSystems/DebugManager.cs†L20-L64】
- Uso: logs respeitam níveis por tipo/instância/atributo e suportam deduplicação por frame para chamadas repetidas.【F:Assets/_ImmersiveGames/Scripts/Utils/DebugSystems/DebugUtility.cs†L51-L141】
- Limpeza: não possui teardown especial; estados estáticos persistem até reinicialização do domínio.
#### 3.5 Regras implícitas detectadas
- Verbose só é emitido se `_verboseLoggingEnabled` verdadeiro; atributos `DebugLevelAttribute` definem nível por classe e são cacheados.【F:Assets/_ImmersiveGames/Scripts/Utils/DebugSystems/DebugUtility.cs†L81-L118】
- Chamadas repetidas no mesmo frame podem ser silenciadas ou logadas em cor específica; usar `deduplicate` quando risco de spam.【F:Assets/_ImmersiveGames/Scripts/Utils/DebugSystems/DebugUtility.cs†L119-L156】

### Pooling
#### 3.1 Propósito
Reutilizar objetos caros (projéteis, efeitos, inimigos) evitando alocação/GC, com orquestração centralizada por nome de pool.
#### 3.2 Quando usar
- Objetos com ciclo rápido de spawn/remoção (tiros, partículas) que exigem instâncias padronizadas (`PoolData`).
- Serviços que precisam notificar quando objetos são ativados/retornados via `UnityEvent` exposto no `ObjectPool`.
#### 3.3 Quando NÃO usar
- Não registrar `PoolData` com `Prefab` sem `IPoolable` (falha de inicialização); o pool aborta e loga erro.【F:Assets/_ImmersiveGames/Scripts/Utils/PoolSystems/ObjectPool.cs†L68-L106】
- Evitar pools globais para objetos exclusivos de cena se não forem limpos em unload; podem reter referências.
#### 3.4 Ciclo de vida
- Nascimento: `PoolManager` (PersistentSingleton) inicializa no `Awake` e registra pools sob objetos filhos nomeados `Pool_*`.【F:Assets/_ImmersiveGames/Scripts/Utils/PoolSystems/PoolManager.cs†L9-L52】
- Uso: `RegisterPool` cria `ObjectPool` configurado; `GetObject`/`GetMultipleObjects` retornam e ativam instâncias, respeitando expansão e warnings de exaustão.【F:Assets/_ImmersiveGames/Scripts/Utils/PoolSystems/ObjectPool.cs†L31-L94】【F:Assets/_ImmersiveGames/Scripts/Utils/PoolSystems/ObjectPool.cs†L96-L144】
- Limpeza: `ReturnObject` devolve ao pool e reconfigura se necessário; `ClearAllPools` destrói objetos ativos/inativos e limpa dicionário.【F:Assets/_ImmersiveGames/Scripts/Utils/PoolSystems/ObjectPool.cs†L146-L185】【F:Assets/_ImmersiveGames/Scripts/Utils/PoolSystems/PoolManager.cs†L69-L84】
#### 3.5 Regras implícitas detectadas
- `ObjectPool` bloqueia `GetObject` se `SetData/Initialize` não forem chamados; garantir criação via `PoolManager.RegisterPool`.
- Requisições múltiplas no mesmo frame podem gerar warning controlado por `_allowMultipleGetsInFrame`; planejar bursts.
- `PoolData.CanExpand` controla se `GetObject` cria novas instâncias ou retorna null com warning quando esgotado.【F:Assets/_ImmersiveGames/Scripts/Utils/PoolSystems/ObjectPool.cs†L117-L139】

### Identidade / UniqueId
#### 3.1 Propósito
Gerar identificadores estáveis para atores e objetos, garantindo distinção por jogador (via `PlayerInput`) ou NPC com contador incremental.
#### 3.2 Quando usar
- Ao inicializar atores/player roots para definir `ActorId` consistente.
- Para objetos não-atores (props temporários) que ainda precisam de ID para logs/eventos.
#### 3.3 Quando NÃO usar
- Não reutilizar IDs manuais em objetos filhos que já herdam `ActorId` do pai; fábrica reaproveita automaticamente para children sem `IActor` próprio.【F:Assets/_ImmersiveGames/Scripts/Utils/UniqueIdFactory.cs†L21-L50】
- Evitar chamar antes de `IActor` estar disponível no objeto/ancestors (gera prefixos genéricos `Obj_*`).【F:Assets/_ImmersiveGames/Scripts/Utils/UniqueIdFactory.cs†L57-L65】
#### 3.4 Ciclo de vida
- Nascimento: instanciado e registrado globalmente pelo `DependencyBootstrapper` (serviço global).【F:Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/DependencyBootstrapper.cs†L46-L50】
- Uso: `GenerateId` calcula ID com contadores por baseActorName; `GetInstanceCount` expõe número atual.
- Limpeza: sem reset automático de contadores; persiste enquanto o serviço existir (global).
#### 3.5 Regras implícitas detectadas
- Para atores com `PlayerInput`, o ID segue formato `Player_{playerIndex}`; demais atores recebem `NPC_{ActorName}_{n}`.【F:Assets/_ImmersiveGames/Scripts/Utils/UniqueIdFactory.cs†L32-L44】
- Filhos sem `IActor` herdam `ActorId` do pai; atores filhos com `IActor` forçam criação de novo ID (evita circularidade).【F:Assets/_ImmersiveGames/Scripts/Utils/UniqueIdFactory.cs†L30-L50】

### Predicados / Gate
#### 3.1 Propósito
Compor regras booleanas reutilizáveis e validar pré-condições, reduzindo complexidade de condicionais espalhadas.
#### 3.2 Quando usar
- Criar regras combináveis (`And/Or/Not`) para checar estados de jogo ou permissões antes de executar ações.
- Validar argumentos/estado com `Preconditions.CheckNotNull/CheckState` em serviços compartilhados.
#### 3.3 Quando NÃO usar
- Não usar predicados para lógica que depende de efeitos colaterais; eles devem ser puros (apenas avaliação de booleanos).
- Evitar `Preconditions` com mensagens genéricas em áreas sensíveis; personalize para diagnósticos úteis.
#### 3.4 Ciclo de vida
- Nascimento: classes estáticas/imutáveis; sem bootstrap.
- Uso: instanciadas conforme necessário e combinadas via extensões `And/Or/Not`.【F:Assets/_ImmersiveGames/Scripts/Utils/Predicates/IPredicate.cs†L5-L43】【F:Assets/_ImmersiveGames/Scripts/Utils/Predicates/PredicateExtensions.cs†L8-L29】
- Limpeza: não se aplica; objetos são descartados pelo GC.
#### 3.5 Regras implícitas detectadas
- Construtores de `And/Or` exigem ao menos um predicado e lançam exceção se receberem array vazio; não criar sem regras.【F:Assets/_ImmersiveGames/Scripts/Utils/Predicates/IPredicate.cs†L9-L33】
- `Preconditions` lançam exceção imediatamente; devem ser usados para erros de programação, não fluxo de jogo.【F:Assets/_ImmersiveGames/Scripts/Utils/Predicates/Preconditions.cs†L8-L37】

### Helpers de Cálculo/Extensão
#### 3.1 Propósito
Encapsular cálculos recorrentes (ex.: bounds reais de objetos ignorando marcados com `IgnoreBoundsFlag`) e extensões utilitárias de Unity.
#### 3.2 Quando usar
- Calcular dimensões efetivas de objetos com hierarquia complexa antes de spawn/colisão usando `CalculateRealLength.GetBounds`.
- Aplicar extensões de componentes/async/transform em scripts sem duplicar código.
#### 3.3 Quando NÃO usar
- Não usar `GetBounds` se renderizadores estiverem desativados/ausentes intencionalmente; pode retornar bounds zerados e recursar desnecessariamente.【F:Assets/_ImmersiveGames/Scripts/Utils/CalculateRealLength.cs†L7-L25】
#### 3.4 Ciclo de vida
- Nascimento: classes estáticas; sem estado.
- Uso: chamado sob demanda.
- Limpeza: não se aplica.
#### 3.5 Regras implícitas detectadas
- Filhos com `IgnoreBoundsFlag` são ignorados no cálculo; todos os demais filhos são encapsulados recursivamente.【F:Assets/_ImmersiveGames/Scripts/Utils/CalculateRealLength.cs†L11-L25】

## 4. Dependências entre Sistemas
- `DependencyBootstrapper` → registra `IUniqueIdFactory`, serviços de cena/transição, `ISimulationGateService` e **EventBus** injetáveis; depende de `EventBusUtil.EventTypes` já carregado.【F:Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/DependencyBootstrapper.cs†L46-L175】
- `DependencyManager` → depende de registries (`Object/Scene/Global`) e `DependencyInjector`; `SceneServiceRegistry` depende do `SceneServiceCleaner` para limpar no unload.【F:Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/DependencyManager.cs†L24-L99】【F:Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/SceneServiceCleaner.cs†L10-L23】
- `PoolManager` → usa `DebugUtility` para logs e `PersistentSingleton` para persistir; não integra com DI diretamente.
- `Event Bus` → pode operar isolado, mas é registrado como serviço global pelo bootstrap para permitir injeção.
- `Predicates`/`CalculateRealLength` → utilitários autônomos, sem dependências fortes.
Dependências aceitáveis: infraestrutura chamando utilidades (ex.: DI registrando EventBus). Dependências perigosas: gameplay depender diretamente de `DependencyBootstrapper` ou `PoolManager` global para lógica crítica de rodada (acoplamento ao escopo global).

## 5. Relação com Reset / Spawn / Lifecycle (infra)
- Participam ativamente do reset: `SceneServiceRegistry` limpa serviços ao descarregar cena via `SceneServiceCleaner`, evitando vazamento de instâncias entre rounds.【F:Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/SceneServiceCleaner.cs†L10-L23】
- Reagem a reset/descarte: `DependencyManager` remove serviços em `OnDestroy`/`OnApplicationQuit`; `EventBusUtil` limpa buses ao sair do Play Mode (Editor), mas não há limpeza automática ao trocar cena em runtime além do que os registries fizerem.【F:Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/DependencyManager.cs†L101-L124】【F:Assets/_ImmersiveGames/Scripts/Utils/BusEventSystems/EventBusUtil.cs†L25-L35】
- `PoolManager` e pools não são limpos automaticamente por cena; se usados para objetos de gameplay por rodada, precisam de limpeza manual ou segregação por cena (ponto de fragilidade).
- `UniqueIdFactory` mantém contadores enquanto o serviço global existir; em resets de partida, IDs podem continuar incrementando, o que pode afetar lógica que espera contagem reiniciada.
- Contratos operacionais de pipeline/fases/escopos estão em `docs/world-lifecycle/WorldLifecycle.md`; aqui mantemos apenas a visão infra e impactos.

## 6. Pontos Fortes do Design Atual
- Ordem de inicialização explícita (RuntimeInitializeOnLoad + DefaultExecutionOrder) para DI, debug e buses, garantindo infraestrutura antes das cenas.【F:Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/DependencyBootstrapper.cs†L26-L118】【F:Assets/_ImmersiveGames/Scripts/Utils/DebugSystems/DebugManager.cs†L5-L41】
- Separação clara de escopos no `DependencyManager` (global/cena/objeto) com limpeza automatizada por unload e encerramento.【F:Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/DependencyManager.cs†L59-L124】【F:Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/SceneServiceCleaner.cs†L10-L23】
- Event Bus tipado e injetável, com suporte a filtragem por escopo e limpeza integrada ao ciclo do Editor.【F:Assets/_ImmersiveGames/Scripts/Utils/BusEventSystems/EventBusUtil.cs†L38-L74】【F:Assets/_ImmersiveGames/Scripts/Utils/BusEventSystems/FilteredEventBus.cs†L10-L58】

## 7. Riscos e Armadilhas Conhecidas
- Dependência de inicialização refletiva (`EventBusUtil.EventTypes`) para registrar buses; se assemblies mudarem ou tipos não carregarem, DI de buses falha silenciosamente.
- Pools persistentes não vinculados a cena podem manter referências a objetos destruídos se `ClearAllPools` não for chamado entre partidas (vazamento de estado).
- `UniqueIdFactory` não reseta contadores; reinícios de partida podem gerar IDs crescentes e quebrar suposições de lógica por índice.
- Registro de serviços em `SceneServiceRegistry` com nome inválido passa com warning, mas injeções futuras falham silenciosamente, dificultando diagnóstico.【F:Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/SceneServiceRegistry.cs†L17-L74】
- `DependencyInjector` ignora injeções repetidas no mesmo frame; scripts que esperam reconfiguração imediata após troca de escopo podem ficar sem dependências atualizadas.【F:Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/DependencyInjector.cs†L31-L63】

## 8. Glossário de Conceitos
- **EventBus**: Fachada estática para publicar/assinar eventos `IEvent`, com implementação injetável e opção de filtro por escopo.
- **Escopo Global / Scene / Object**: Níveis de registro de serviço no `DependencyManager` — global persiste entre cenas, scene vive enquanto a cena está carregada, object vincula um ID específico e deve ser limpo manualmente.
- **ActorId**: Identificador lógico de ator (player/NPC), derivado de `IActor` ou `PlayerInput`, usado para escopos de evento e UI.【F:Assets/_ImmersiveGames/Scripts/Utils/UniqueIdFactory.cs†L21-L50】
- **Gate**: Controle de execução/simulação registrado como serviço global (`ISimulationGateService`) para habilitar/pausar lógicas sem alterar `timeScale`.【F:Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/DependencyBootstrapper.cs†L46-L52】
- **Bootstrap**: Classe com `RuntimeInitializeOnLoadMethod`/singleton persistente que registra serviços essenciais antes das cenas (`DependencyBootstrapper`).
- **Reset**: Limpeza de serviços/buses ao trocar de cena ou sair do Play Mode; inclui `Clear` em registries e `EventBusUtil.ClearAllBuses` no Editor.
