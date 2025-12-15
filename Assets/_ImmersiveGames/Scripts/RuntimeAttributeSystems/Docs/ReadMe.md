# 💠 Sistema de Atributos em Tempo de Execução (v3.1)

Documentação alinhada com a nova nomenclatura **Domain / Application / Presentation / UI** e com a árvore real de pastas do repositório. Todo o fluxo continua orientado a eventos e a injeção de dependências para manter o jogo multiplayer local desacoplado e fácil de debugar.

## 📋 Índice
1. [Visão Geral](#visão-geral)
2. [Arquitetura em Camadas](#arquitetura-em-camadas)
3. [Componentes Principais](#componentes-principais)
4. [Configurações (ScriptableObjects)](#configurações-scriptableobjects)
5. [Serviços e Bridges](#serviços-e-bridges)
6. [Eventos e Fluxo Reativo](#eventos-e-fluxo-reativo)
7. [UI e Animação](#ui-e-animação)
8. [Fluxo de Inicialização](#fluxo-de-inicialização)
9. [Tabela de Migração](#tabela-de-migração)

---

## 🎯 Visão Geral

O **Runtime Attribute System** controla atributos como vida, energia e escudos com UI dinâmica e bridges leves. Tudo é dirigido por eventos (`EventBus`/`FilteredEventBus`) e serviços injetados (`DependencyManager`), garantindo separação clara entre regras de domínio, orquestração e interface.

---

## 🏛️ Arquitetura em Camadas

```text
Domain (regras e dados puros)
├─ Assets/_ImmersiveGames/Scripts/RuntimeAttributeSystems/Domain/Configs
└─ Assets/_ImmersiveGames/Scripts/RuntimeAttributeSystems/Domain/Values

Application (serviços orquestradores)
├─ Assets/_ImmersiveGames/Scripts/RuntimeAttributeSystems/Application/Services
└─ Assets/_ImmersiveGames/Scripts/RuntimeAttributeSystems/RuntimeAttributeUpdateEvent.cs

Presentation (bridges e binders)
├─ Assets/_ImmersiveGames/Scripts/RuntimeAttributeSystems/Presentation/Bridges
└─ Assets/_ImmersiveGames/Scripts/RuntimeAttributeSystems/Presentation/Bind

UI (renderização e animações)
├─ Assets/_ImmersiveGames/Scripts/RuntimeAttributeSystems/UI
└─ Assets/_ImmersiveGames/Scripts/RuntimeAttributeSystems/UI/AnimationStrategies
```

Fluxo simples:
```
Domain → Application → Presentation → UI
Configs    Serviços      Bridges       Slots/Animações
```

---

## 🧩 Componentes Principais

### 🧠 Domínio
- **`RuntimeAttributeDefinition`**: define tipo, valor inicial e máximo.
- **`RuntimeAttributeInstanceConfig`**: instancia configurações por ator.
- **`RuntimeAttributeLinkConfig` / `RuntimeAttributeAutoFlowConfig` / `RuntimeAttributeThresholdConfig`**: governam links, regen/dreno e thresholds.
- **`BasicRuntimeAttributeValue`**: implementação básica de valor com limites.

### ⚙️ Aplicação
- **`RuntimeAttributeContext`**: núcleo de dados por entidade (equivale ao antigo *ResourceSystem*).
- **`RuntimeAttributeCoordinator`**: coordena binds pendentes e registra canvases.
- **`RuntimeAttributeCanvasManager`**: executa `ScheduleBind` quando a UI está pronta.
- **`RuntimeAttributeLinkService`**, **`RuntimeAttributeAutoFlowService`**, **`RuntimeAttributeThresholdService`**: serviços reativos especializados.
- **`RuntimeAttributeBootstrapper`**: injeta dependências em bridges/binders no ciclo de cena.

### 🎭 Apresentação
- **Binders** (`RuntimeAttributeSceneCanvasBinder`, `RuntimeAttributeDynamicCanvasBinder`, `RuntimeAttributeActorCanvas`): criam `CanvasId`, registram no orquestrador e notificam o pipeline.
- **Bridges** (`RuntimeAttributeBridgeBase`, `RuntimeAttributeAutoFlowBridge`, `RuntimeAttributeLinkBridge`, `RuntimeAttributeThresholdBridge`, `WorldSpaceBillboard`): conectam atores aos serviços e ao HUD.
- **Contratos** (`RuntimeAttributeBindingContracts`): interfaces para padronizar binds e canvas routing.

### 🎨 UI
- **`RuntimeAttributeUISlot`**: slot visual que recebe updates e animações.
- **Animações**: `IFillAnimationStrategy` + fábrica (`FillAnimationStrategyFactory`) com estratégias `InstantFill`, `BasicReactiveFill`, `SmoothReactiveFill` (todas em `UI/AnimationStrategies`).

---

## 🧩 Configurações (ScriptableObjects)

| Config                        | Pasta | Função |
| ----------------------------- | ----- | ------ |
| `RuntimeAttributeDefinition`  | `Domain/Configs` | Define o tipo e limites base do atributo |
| `RuntimeAttributeInstanceConfig` | `Domain/Configs` | Liga uma definição a um ator específico |
| `RuntimeAttributeAutoFlowConfig` | `Domain/Configs` | Parâmetros de regen/dreno automática |
| `RuntimeAttributeLinkConfig`  | `Domain/Configs` | Links de transferência/overflow entre atributos |
| `RuntimeAttributeThresholdConfig` | `Domain/Configs` | Thresholds percentuais para eventos e VFX |
| `RuntimeAttributeUIStyle`     | `Domain/Configs` | Estilo visual usado pelos slots |
| `FillAnimationProfile`        | `UI/Animation` | Perfil de animação para slots |

---

## 🎛️ Serviços e Bridges

- **Bootstrap**: `RuntimeAttributeBootstrapper` prepara o contexto do ator e registra serviços globais.
- **Orquestração**: `RuntimeAttributeCoordinator` + `RuntimeAttributeCanvasManager` publicam/consomem `CanvasBindRequest` via `RuntimeAttributeEventHub`.
- **AutoFlow**: `RuntimeAttributeAutoFlowService` aplica regen/dreno reativo; `RuntimeAttributeAutoFlowBridge` conecta configs por ator.
- **Links**: `RuntimeAttributeLinkService` + `RuntimeAttributeLinkBridge` garantem drenagens combinadas/overflow.
- **Thresholds**: `RuntimeAttributeThresholdService` + `RuntimeAttributeThresholdBridge` disparam `RuntimeAttributeVisualFeedbackEvent`.

---

## 📡 Eventos e Fluxo Reativo

| Evento                                 | Origem                                         | Função |
| -------------------------------------- | ---------------------------------------------- | ------ |
| `RuntimeAttributeUpdateEvent`          | `RuntimeAttributeContext`                      | Notifica qualquer alteração de atributo |
| `CanvasBindRequest`                    | `RuntimeAttributeCoordinator`                  | Solicita bind de ator ↔ canvas |
| `CanvasRegisteredEvent`                | `RuntimeAttributeActorCanvas`                  | Informa pipeline de que o canvas está pronto |
| `RuntimeAttributeThresholdEvent`       | `RuntimeAttributeThresholdService`             | Threshold cruzado (percentual) |
| `RuntimeAttributeVisualFeedbackEvent`  | `RuntimeAttributeThresholdBridge`              | Efeito visual disparado pela ponte |
| `RuntimeAttributeLinkChangeEvent`      | `RuntimeAttributeLinkService`                  | Propaga efeitos de links entre atributos |
| `RuntimeAttributeAutoFlowEvent`        | `RuntimeAttributeAutoFlowService`              | Atualiza regen/dreno automática |

---

## 🎨 UI e Animação

- **Binds**: `RuntimeAttributeSceneCanvasBinder` e `RuntimeAttributeDynamicCanvasBinder` criam slots via pipeline e pooling.
- **Slots**: `RuntimeAttributeUISlot` aplica animação recebida da fábrica (`FillAnimationStrategyFactory`).
- **Estratégias**: `InstantFill` (sem animação), `BasicReactiveFill` (lerp rápido), `SmoothReactiveFill` (transição contínua). Todas vivem em `UI/AnimationStrategies`.

---

## 🚀 Fluxo de Inicialização

1. **Bootstrap**: `RuntimeAttributeBootstrapper` injeta dependências (contexto, serviços globais e binders).
2. **Registro**: `RuntimeAttributeActorCanvas` registra `CanvasId`; bridges resolvem `RuntimeAttributeContext` via `DependencyManager`.
3. **Bind**: `RuntimeAttributeCoordinator` emite `CanvasBindRequest`; `RuntimeAttributeCanvasManager` executa `ScheduleBind` criando slots na UI.
4. **Execução**: Serviços de AutoFlow/Link/Thresholds publicam eventos; UI reage via `RuntimeAttributeEventHub` e animações.

---

## 🔄 Tabela de Migração

| Nome antigo | Nome novo | Nova pasta |
| ----------- | --------- | ---------- |
| `ResourceSystem` | `RuntimeAttributeContext` | `Application/Services` |
| `ActorResourceOrchestratorService` | `RuntimeAttributeCoordinator` | `Application/Services` |
| `CanvasPipelineManager` | `RuntimeAttributeCanvasManager` | `Application/Services` |
| `ResourceLinkService` | `RuntimeAttributeLinkService` | `Application/Services` |
| `ResourceAutoFlowService` | `RuntimeAttributeAutoFlowService` | `Application/Services` |
| `ResourceThresholdService` | `RuntimeAttributeThresholdService` | `Application/Services` |
| `ResourceEventHub` | `RuntimeAttributeEventHub` | `Utils` |
| `InjectableCanvasResourceBinder` / `DynamicCanvasBinder` | `RuntimeAttributeSceneCanvasBinder` / `RuntimeAttributeDynamicCanvasBinder` | `Presentation/Bind` |
| `ResourceUISlot` | `RuntimeAttributeUISlot` | `UI` |
| `ResourceBridgeBase` / `ResourceAutoFlowBridge` / `ResourceLinkBridge` / `ResourceThresholdBridge` | `RuntimeAttributeBridgeBase` / `RuntimeAttributeAutoFlowBridge` / `RuntimeAttributeLinkBridge` / `RuntimeAttributeThresholdBridge` | `Presentation/Bridges` |

Use esta tabela para localizar classes legadas durante a migração para a estrutura atual.
