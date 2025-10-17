# 🔗 Sistema de Dependências — Guia de Uso (v2.0)

## 📚 Índice
1. [Visão Geral](#visão-geral)
2. [Camadas e Escopos](#camadas-e-escopos)
3. [Componentes](#componentes)
4. [Fluxo de Bootstrap](#fluxo-de-bootstrap)
5. [Injeção em Componentes](#injeção-em-componentes)
6. [Monitoramento e Limpeza](#monitoramento-e-limpeza)
7. [Boas Práticas](#boas-práticas)

---

## 🎯 Visão Geral

O **DependencySystem** fornece uma camada de **Inversion of Control** otimizada para Unity. Ele integra `DependencyManager`, registries especializados e um `DependencyBootstrapper` persistente, permitindo desacoplamento entre serviços globais, de cena e de objeto — essencial para o multiplayer local e testes automatizados.

---

## 🗂️ Camadas e Escopos

```
DependencyManager (RegulatorSingleton)
├── GlobalServiceRegistry     → Serviços singleton (ex.: UniqueIdFactory)
├── SceneServiceRegistry      → Serviços por cena (ex.: Spawn tables)
└── ObjectServiceRegistry     → Serviços por objeto/Actor (ex.: ResourceSystem por entidade)
```

* **Global** — válido em todo o jogo. Persistem entre cenas.
* **Cena** — válido somente enquanto a cena estiver carregada.
* **Objeto** — vinculado a um identificador (`objectId`) e limpo manualmente.

A resolução de dependências segue a ordem **Objeto → Cena → Global**, garantindo que instâncias específicas sobreponham serviços compartilhados.

---

## 🧩 Componentes

### `DependencyManager`
* Singleton (`RegulatorSingleton`) com instâncias de todos os registries.
* Métodos públicos: `RegisterGlobal`, `RegisterForScene`, `RegisterForObject`, `TryGet`, `GetAll`, `InjectDependencies`.
* Expõe flags como `IsInTestMode` para flexibilizar validações (ex.: cenas não presentes em build durante testes).

### `DependencyBootstrapper`
* `PersistentSingleton` inicializado antes da primeira cena.
* Registra serviços essenciais (`UniqueIdFactory`, `ResourceInitializationManager`, `CanvasPipelineManager`, `ActorResourceOrchestrator`, `IStateDependentService`).
* Usa `EnsureGlobal<T>` para evitar duplicidade.
* Dispara `RegisterEventBuses()` via reflexão, garantindo que todos os `IEventBus<T>` estejam registrados no `DependencyManager`.

### Registries
* `GlobalServiceRegistry` — dicionário simples `Type → service`.
* `SceneServiceRegistry` — mantém `sceneName → (Type → service)` e respeita limite opcional de tipos por cena (`maxSceneServices`). Aciona `SceneServiceCleaner` para limpar ao descarregar a cena.
* `ObjectServiceRegistry` — mapeia `objectId → (Type → service)`, permitindo override por objeto.
* `ServiceRegistry` — classe base com pooling de dicionários, validações e utilitários de log.

### `DependencyInjector`
* Responsável por refletir campos marcados com `[Inject]`.
* Evita injeções duplicadas no mesmo frame (`_injectedObjectsThisFrame`).
* Resolve serviços usando os registries mencionados.
* Permite extensões via métodos `TryGet` dinâmicos (reflection helpers).

### `SceneServiceCleaner`
* Observa `SceneManager.sceneUnloaded` e aciona `SceneServiceRegistry.Clear(scene)`.

---

## ⚙️ Fluxo de Bootstrap

1. `DependencyBootstrapper.Initialize()` roda antes da primeira cena.
2. Garante a criação do `DependencyManager.Instance`.
3. Registra serviços globais essenciais.
4. Busca serviços que precisam de injeção e chama `RegisterForInjection` no `ResourceInitializationManager`.
5. Registra todos os `IEventBus<T>` como serviços globais para permitir injeção explícita.
6. Loga resultado via `DebugUtility` no nível Verbose.

---

## 🧪 Injeção em Componentes

```csharp
public class ResourceHud : MonoBehaviour
{
    [Inject] private ResourceSystem _resourceSystem;
    [Inject] private IEventBus<ResourceUpdateEvent> _eventBus;

    private void Awake()
    {
        DependencyManager.Instance.InjectDependencies(this, objectId: _actor.ActorId);
    }
}
```

* Campos privados marcados com `[Inject]` são preenchidos automaticamente.
* Informe `objectId` para consumir serviços de escopo de objeto.
* `DependencyInjector` percorre a hierarquia de tipos, permitindo injeção em classes base.

---

## 🧼 Monitoramento e Limpeza

* `DependencyManager.OnDestroy` limpa todos os registries, garantindo que singletons não vazem referências.
* Métodos auxiliares: `ClearSceneServices`, `ClearObjectServices`, `ClearGlobalServices` e variantes `ClearAll`.
* `SceneServiceRegistry.OnSceneServicesCleared` pode ser usado para disparar feedback (ex.: rebind de UI).

---

## ✅ Boas Práticas

| Cenário | Estratégia |
| --- | --- |
| Testes unitários | Ative `DependencyManager.Instance.IsInTestMode = true` para flexibilizar validação de cenas e injete stubs manualmente. |
| Registros duplicados | Utilize `allowOverride` apenas quando realmente precisar substituir implementações. Preferir logs Verbose para diagnosticar. |
| Serviços temporários | Registre com `objectId` e chame `ClearObjectServices(id)` no `OnDestroy` do ator. |
| Bootstrap customizado | Estenda `DependencyBootstrapper` com novos serviços, mantendo chamadas para `EnsureGlobal`. |
| Dead references | Combine com `DebugUtility.LogVerbose` para identificar serviços não encontrados em `DependencyInjector`. |

Este sistema adere a SOLID ao separar responsabilidade de registro, injeção e limpeza, facilitando evolução da arquitetura sem gerar acoplamento rígido.
