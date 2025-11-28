# Sistema de Skins – Documentação (Versão Atualizada)

## 📋 Visão Geral

O **Sistema de Skins** permite compor, trocar, medir e sincronizar modelos visuais de atores do jogo.
Ele foi projetado para:

* Garantir **consistência visual** mesmo com objetos complexos (ex.: planetas compostos por várias partes).
* Oferecer um **ponto único de orquestração de skins** (`ActorSkinController`).
* Integrar-se ao **DependencyManager** (escopos: global, cena e por objeto).
* Expor um **estado de runtime** reutilizável (`SkinRuntimeState`) para outros sistemas (HUD, detectores, IA, etc.).
* Minimizar acoplamento entre **prefabs**, **gameplay** e **UI**.

Principais peças:

* `ActorSkinController` – orquestra aplicação de skins, eventos e integração com DI.
* `ISkinService` / `DefaultSkinService` – faz a aplicação real das skins e gerência das instâncias.
* `SkinRuntimeStateTracker` – mede o tamanho real das skins (bounds/raio) usando `CalculateRealLength`.
* `SkinConfigurable` + “features” (GroupedMaterial, RandomTransform, Ring, etc.) – modificações dinâmicas na skin.
* `CalculateRealLength` – utilitário central para medir bounds reais de objetos compostos.

---

## 🏗️ Arquitetura Geral

```text
IActor / IHasSkin
    ↑
ActorSkinController (MonoBehaviour)
    ↳ ISkinService (DefaultSkinService)
         ↳ SkinContainerService
         ↳ SkinModelFactory
         ↳ ISkinInstancePostProcessor[]
               (DynamicCanvasBinderPostProcessor por padrão)
    ↳ SkinRuntimeStateTracker (opcional, recomendado)
    ↳ DependencyManager (registro por objeto via ActorId)
```

### Fluxo de Alto Nível

1. O `ActorSkinController` encontra um `IActor` e um `IHasSkin` na hierarquia (dono visual).
2. Durante `Initialize()` ele:

    * Configura o `ISkinService` (normalmente `DefaultSkinService`);
    * Cria contêineres de modelos e aplica a **coleção default** (se configurada).
3. O `DefaultSkinService` instancia prefabs, aplica transform inicial e executa pós-processadores (UI, binds, etc.).
4. O `ActorSkinController` dispara:

    * Eventos **locais** (`OnSkinApplied`, `OnSkinCollectionApplied`, `OnSkinInstancesCreated`);
    * Eventos **globais** no `EventBus` (opcional);
    * Eventos **filtrados por ator** (`FilteredEventBus`) usando o `ActorId`.
5. O `SkinRuntimeStateTracker` escuta `OnSkinInstancesCreated` e:

    * Calcula bounds reais usando `CalculateRealLength`;
    * Salva o resultado num `SkinRuntimeState` por `ModelType`;
    * Se não houver skins criadas, pode usar o **fallback pelo root do ator** (planetas).
6. `ActorSkinController` e `SkinRuntimeStateTracker` se registram no `DependencyManager`:

    * `RegisterForObject(ActorId, service)` → resolução por ator;
    * Opcionalmente, registrar global (`RegisterGlobal`) para casos especiais.

---

## 🎯 Componentes Principais

### ActorSkinController

**Responsabilidades:**

* Gerenciar a skin visual de um ator (via `IActor` + `IHasSkin`).
* Delegar a criação de instâncias para um `ISkinService`.
* Integrar com o `DependencyManager` via registro **por objeto** (ActorId).
* Propagar eventos locais, globais e filtrados.
* Expor helpers para acessar instâncias e estados de runtime.

**Campos principais (resumido):**

* `SkinCollectionData defaultSkinCollection;`
* `bool autoInitialize;`
* `bool enableGlobalEvents;`
* `ISkinService _skinService;`
* `IActor _ownerActor;`
* `IHasSkin _skinOwner;`
* `bool IsInitialized { get; private set; }`

**Eventos:**

* `event Action<ISkinConfig> OnSkinApplied;`
* `event Action<SkinCollectionData> OnSkinCollectionApplied;`
* `event Action<ModelType, List<GameObject>> OnSkinInstancesCreated;`

**Principais métodos públicos:**

```csharp
public void Initialize();
public void ApplySkin(ISkinConfig config);
public void ApplySkinCollection(SkinCollectionData collection);
public void SetSkinActive(bool active);

public List<GameObject> GetSkinInstances(ModelType type);
public Transform GetSkinContainer(ModelType type);
public bool HasSkinApplied(ModelType type);

// Acesso a componentes nas instâncias de skin
public List<T> GetComponentsFromSkinInstances<T>(ModelType type) where T : Component;
public T GetComponentFromSkinInstances<T>(ModelType type) where T : Component;

// Integração com SkinRuntimeStateTracker
public bool TryGetRuntimeState(ModelType type, out SkinRuntimeState state);
```

**Integração com DependencyManager:**

No `Start()`, o controller registra-se como serviço de objeto usando o `ActorId`:

```csharp
_objectId = _ownerActor.ActorId;
DependencyManager.Provider.RegisterForObject(_objectId, this);
```

Isso permite fazer:

```csharp
if (DependencyManager.Provider.TryGet<ActorSkinController>(out var controller, actorId))
{
    // usar controller
}
```

**ContextMenu de Debug:**

O controller possui um contexto de debug no Inspector:

```csharp
[ContextMenu("Log Skin Runtime States")]
private void Editor_LogSkinRuntimeStates()
{
    var tracker = GetComponent<SkinRuntimeStateTracker>();
    if (tracker != null)
        tracker.LogAllStatesToConsole();
}
```

---

### ISkinService / DefaultSkinService

**ISkinService** define o contrato:

```csharp
public interface ISkinService
{
    void Initialize(SkinCollectionData collection, Transform parent, IActor owner);
    IReadOnlyDictionary<ModelType, IReadOnlyList<GameObject>> ApplyCollection(SkinCollectionData collection, IActor owner);
    IReadOnlyList<GameObject> ApplyConfig(ISkinConfig config, IActor owner);
    IReadOnlyList<GameObject> GetInstancesOfType(ModelType type);
    bool HasInstancesOfType(ModelType type);
    Transform GetContainer(ModelType type);
}
```

**DefaultSkinService** é a implementação padrão:

* Usa:

    * `SkinContainerService` para criar/reaproveitar contêineres por `ModelType`.
    * `SkinModelFactory` para instanciar prefabs e aplicar transform inicial.
    * `ISkinInstancePostProcessor[]` para executar lógica adicional por instância (por exemplo: `DynamicCanvasBinderPostProcessor`).
* Mantém um dicionário interno de instâncias:

```csharp
Dictionary<ModelType, List<GameObject>> _instances;
```

* Permite limpezas e reaplicações de coleções sem gerar lixo desnecessário.

---

### SkinRuntimeStateTracker

O componente que **mede** o tamanho real da skin.

**Objetivos:**

* Centralizar o cálculo de bounds/raio/centro das skins.
* Evitar duplicação de lógica em `PlanetsManager`, detectores, HUD, etc.
* Expor um estado estável e fácil de consultar para outros sistemas.

**Dados expostos:**

```csharp
// Por ModelType
SkinRuntimeState
{
    public ModelType ModelType;
    public Bounds WorldBounds;
    public Vector3 Center;
    public Vector3 Size;
    public float MaxDimension;
    public float ApproxRadius;
    public bool HasValidBounds;
}
```

**Integração com ActorSkinController:**

* Escuta o evento `OnSkinInstancesCreated(ModelType type, List<GameObject> instances)`.
* Para cada tipo, faz:

```csharp
Bounds bounds = CalculateWorldBoundsForInstances(instances);
// dentro: CalculateRealLength.GetBounds(instance);
_states[type] = new SkinRuntimeState(type, bounds);
```

**Integração com DependencyManager:**

No `Awake()`, o tracker tenta registrar-se:

```csharp
_objectId = skinController.OwnerActor.ActorId;
DependencyManager.Provider.RegisterForObject(_objectId, this);
```

Opcionalmente, pode registrar também como global (`registerAsGlobalService`).

**API pública:**

```csharp
public bool TryGetState(ModelType type, out SkinRuntimeState state);
public SkinRuntimeState GetStateOrEmpty(ModelType type);
public void RecalculateState(ModelType type);
public void RecalculateAllStates();
public void LogAllStatesToConsole();
```

---

### SkinRuntimeState

Estrutura serializável que representa o estado geométrico de uma skin:

```csharp
[Serializable]
public struct SkinRuntimeState
{
    public ModelType ModelType;
    public Bounds WorldBounds;

    public Vector3 Center => WorldBounds.center;
    public Vector3 Size   => WorldBounds.size;
    public float MaxDimension => Mathf.Max(Size.x, Size.y, Size.z);
    public float ApproxRadius => MaxDimension * 0.5f;

    public static SkinRuntimeState Empty(ModelType modelType);
    public bool HasValidBounds { get; }
}
```

---

### CalculateRealLength (utilitário de bounds)

Este utilitário é usado tanto pelo `SkinRuntimeStateTracker` quanto por outros sistemas (ex.: `PlanetsManager`) para calcular o tamanho real de objetos compostos:

* Varre hierarquia de filhos.
* Considera todos os renderizadores válidos.
* Permite ignorar elementos com `IgnoreBoundsFlag` ou similar.
* Retorna um `Bounds` em espaço de mundo representando o conjunto.

---

### SkinConfigurable e “features”

`SkinConfigurable` é uma base para comportamentos que querem reagir a mudanças de skin:

Exemplos de features:

* `GroupedMaterialSkin` – troca materiais por grupos, sorteio, progressão de material etc.
* `RandomTransformSkin` – aplica escala/rotação aleatória e guarda o estado.
* `RingActivationSkin` – controla o “anel” do planeta (presença, rotação, visibilidade).

Cada feature:

* Se registra nos eventos do `ActorSkinController` (local/globais).
* Aplica suas modificações sobre as instâncias de skin relevantes.
* Pode expor seu próprio estado (ex.: `TransformState`, `RingState`, `GroupedMaterialState`).

---

## 🌍 Fallback de Medição para Objetos Complexos (Planetas)

Em muitos casos, certos atores **não utilizam o sistema de skin** para gerar suas partes visuais – por exemplo:

* Planetas compostos por múltiplos filhos (`PlanetsMaster`, `PlanetsManager` etc.).
* Prefabs já montados, onde o sistema de skin não está criando instâncias adicionais.

Nesses casos:

* Nenhuma skin é aplicada via `ApplySkin` / `ApplySkinCollection`.
* O `ActorSkinController` **não dispara** `OnSkinInstancesCreated`.
* Consequência: o `SkinRuntimeStateTracker` não teria estados calculados por padrão.

Para isso, o tracker possui um **fallback automático** baseado no **root do ator**.

### Como funciona o fallback

Se **não existir nenhum estado calculado** e o fallback estiver habilitado:

1. O tracker pega o `Transform` do `OwnerActor` (raiz lógica do ator).
2. Chama `CalculateRealLength.GetBounds(rootGameObject)`.
3. Cria um `SkinRuntimeState` usando um `ModelType` configurável (ex.: `ModelRoot` ou `Body`).
4. Armazena isso como estado inicial e marca `_initialStateComputedFromRoot = true`.
5. Esse estado passa a ser usado em todas as consultas (`TryGetState`, `LogAllStatesToConsole`, etc.).

### Configuração no Inspector

No `SkinRuntimeStateTracker`, configure:

```text
[✔] computeInitialStateFromActorRoot
initialStateModelType = ModelRoot  (ou outro ModelType que faça sentido no seu enum)
```

Assim, ao chamar o ContextMenu do `ActorSkinController` ou acessar via código:

```csharp
if (controller.TryGetRuntimeState(ModelType.ModelRoot, out var state))
{
    float radius = state.ApproxRadius;
    Vector3 center = state.Center;
}
```

Você terá o **tamanho real do planeta**, mesmo sem usar skins ativas.

### Exemplo real de log

Algo como:

```text
[VERBOSE] [SkinRuntimeStateTracker] [Planet01_1] 
Estado inicial calculado a partir do root do ator. 
ModelType=ModelRoot, Center=(0.00, -0.75, 10.00), 
Size=(4.03, 8.50, 20.00), Radius≈10.00

[VERBOSE] [SkinRuntimeStateTracker] [Planet01_1] 
ModelType=ModelRoot | Center=(0.00, -0.75, 10.00) | 
Size=(4.03, 8.50, 20.00) | Radius≈10.00 | HasValidBounds=True
```

---

## 🔁 Ciclo de Vida & Eventos

| Fase                            | Componente                               | Ação                                                                |
| ------------------------------- | ---------------------------------------- | ------------------------------------------------------------------- |
| **Awake**                       | ActorSkinController                      | Encontra `IActor` / `IHasSkin`, configura `ISkinService` default    |
| **Awake**                       | SkinRuntimeStateTracker                  | Encontra controller, registra no `DependencyManager`                |
| **Start**                       | ActorSkinController                      | Registra-se no `DependencyManager` (por `ActorId`)                  |
| **OnEnable**                    | ActorSkinController                      | Registra em `FilteredEventBus` / `EventBus` globais (se habilitado) |
| **OnEnable**                    | SkinRuntimeStateTracker                  | Se inscreve em `OnSkinInstancesCreated`                             |
| **Initialize**                  | ActorSkinController                      | Chama `ISkinService.Initialize` e aplica `defaultSkinCollection`    |
| **ApplySkin / ApplyCollection** | ActorSkinController / DefaultSkinService | Instancia prefabs, aplica transform, dispara eventos                |
| **OnSkinInstancesCreated**      | SkinRuntimeStateTracker                  | Calcula `SkinRuntimeState` por `ModelType`                          |
| **Start** (Tracker)             | SkinRuntimeStateTracker                  | Se não houver estados, aplica fallback pelo root do ator            |
| **OnDisable**                   | ActorSkinController                      | Remove bindings globais                                             |
| **OnDisable**                   | SkinRuntimeStateTracker                  | Remove inscrição dos eventos do controller                          |
| **OnDestroy**                   | ActorSkinController                      | Limpa serviços de objeto no `DependencyManager`                     |

---

## 🚀 Guia de Uso

### 1. Configuração Básica

No prefab do seu ator (ex.: planeta, player):

1. Adicione `ActorSkinController`.
2. Adicione `SkinRuntimeStateTracker`.
3. Certifique-se de que o ator implementa:

    * `IActor` com `ActorId` único;
    * `IHasSkin` com um `ModelTransform` apontando para o ponto base visual.

No `ActorSkinController`:

* `defaultSkinCollection` (opcional).
* `autoInitialize` – se verdadeiro, chama `Initialize()` no Awake.
* `enableGlobalEvents` – para integração com `EventBus`/`FilteredEventBus`.

No `SkinRuntimeStateTracker`:

* `computeInitialStateFromActorRoot = true` para planetas ou objetos fixos.
* `initialStateModelType = ModelRoot` (ou outro do seu enum).

---

### 2. Trocando Skin em Runtime

```csharp
public class SkinSwitcher : MonoBehaviour
{
    [SerializeField] private ActorSkinController controller;
    [SerializeField] private SkinCollectionData alternateCollection;

    public void Swap()
    {
        controller.ApplySkinCollection(alternateCollection);
    }
}
```

---

### 3. Consultando o tamanho real da skin (via controller)

```csharp
if (controller.TryGetRuntimeState(ModelType.ModelRoot, out var state) && state.HasValidBounds)
{
    Debug.Log($"Center={state.Center}, Size={state.Size}, Radius≈{state.ApproxRadius}");
}
```

> Para planetas que não usam skins, basta garantir o fallback configurado no tracker.

---

### 4. Consultando via DependencyManager

```csharp
string actorId = myActor.ActorId;

if (DependencyManager.Provider.TryGet<SkinRuntimeStateTracker>(out var tracker, actorId) &&
    tracker.TryGetState(ModelType.ModelRoot, out var state))
{
    float radius = state.ApproxRadius;
    Vector3 center = state.Center;
}
```

---

### 5. Injetando um serviço de skin customizado

```csharp
void Awake()
{
    var controller = GetComponent<ActorSkinController>();

    var service = new DefaultSkinService(
        new SkinContainerService(),
        new SkinModelFactory(),
        new ISkinInstancePostProcessor[]
        {
            new DynamicCanvasBinderPostProcessor(),
            new MyCustomPostProcessor()
        });

    controller.SetSkinService(service);
}
```

---

## 🧪 Boas Práticas

* **Para planetas e objetos complexos**:

    * Deixe `computeInitialStateFromActorRoot = true` no `SkinRuntimeStateTracker`.
    * Use `SkinRuntimeState` como a **fonte única da verdade** para raio/tamanho/centro.
    * Evite chamar `CalculateRealLength` diretamente em múltiplos sistemas.

* **Para sistemas de detecção / HUD**:

    * Consulte sempre `SkinRuntimeState` pelo `DependencyManager` ou `ActorSkinController`.
    * Assim, qualquer mudança futura de escala/skin fica automaticamente refletida.

* **Para testes/unit tests**:

    * Injete uma implementação própria de `ISkinService` (mock ou fake).
    * Evite depender de cenas/prefabs carregados, focando na lógica de composição.

---

## 🛠️ Solução de Problemas

| Sintoma                                      | Possível causa                                            | Ação sugerida                                                                                                      |
| -------------------------------------------- | --------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------ |
| `SkinRuntimeStateTracker` não possui estados | Nenhuma skin aplicada / fallback desabilitado             | Habilite `computeInitialStateFromActorRoot` e configure `initialStateModelType`.                                   |
| Bounds muito pequenos ou zero                | Renderers ignorados ou `IgnoreBoundsFlag` mal configurado | Revise a hierarquia visual e flags usadas pelo `CalculateRealLength`.                                              |
| Serviço não encontrado no DI                 | `ActorId` nulo ou duplicado                               | Verifique a implementação de `IActor.ActorId`. Cada ator deve ter um ID único e não vazio.                         |
| Eventos globais disparando múltiplas vezes   | Registro duplicado em `OnEnable`                          | `ActorSkinController` já trata `_globalEventsRegistered`; verifique se não há scripts externos registrando a mais. |
| UI dinâmica não atualiza                     | Pós-processador default removido                          | `DefaultSkinService` injeta `DynamicCanvasBinderPostProcessor`; revise customizações de pós-processadores.         |
| Pico de GC ao trocar skins                   | Coleções temporárias alocadas em loops externos           | O sistema de skin evita LINQ; monitore outros scripts que manipulam coleções ao redor das chamadas de skin.        |

---

## 📚 Referências Cruzadas

Principais arquivos relacionados ao sistema de skins:

* `_ImmersiveGames/Scripts/SkinSystems/ActorSkinController.cs`
* `_ImmersiveGames/Scripts/SkinSystems/Core/DefaultSkinService.cs`
* `_ImmersiveGames/Scripts/SkinSystems/Core/SkinContainerService.cs`
* `_ImmersiveGames/Scripts/SkinSystems/Core/SkinModelFactory.cs`
* `_ImmersiveGames/Scripts/SkinSystems/Runtime/SkinRuntimeStateTracker.cs`
* `_ImmersiveGames/Scripts/SkinSystems/Runtime/SkinRuntimeState.cs`
* `_ImmersiveGames/Scripts/SkinSystems/Data/SkinCollectionData.cs`
* `_ImmersiveGames/Scripts/SkinSystems/Data/SkinConfigData.cs`
* `_ImmersiveGames/Scripts/SkinSystems/Behaviours/SkinConfigurable.cs`
* `_ImmersiveGames/Scripts/SkinSystems/Behaviours/GroupedMaterialSkin.cs`
* `_ImmersiveGames/Scripts/SkinSystems/Behaviours/RandomTransformSkin.cs`
* `_ImmersiveGames/Scripts/SkinSystems/Behaviours/RingActivationSkin.cs`
* `_ImmersiveGames/Scripts/Utils/CalculateRealLength.cs`
* `_ImmersiveGames/Scripts/Utils/DependencySystems/DependencyManager.cs`

---

*Documento atualizado para refletir a nova arquitetura com `ActorSkinController`, integração com o `DependencyManager` por objeto e o fallback de medição via `SkinRuntimeStateTracker` para objetos complexos (como planetas).*
