# 🎯 Plano de Refatoração - Runtime Attribute System (Domain/Application/Presentation/UI)

Documento alinhado com a nomenclatura padronizada e a árvore atual de pastas. Foco em manter SOLID, arquitetura limpa e fluxo totalmente event-driven para o multiplayer local.

## 📋 Status Atual
- **Última Atualização:** 2025-02-22
- **Próxima Etapa:** Validar binds dinâmicos no pipeline novo e revisar feedbacks visuais por camada.

## 🏗️ Arquitetura do Sistema

### Diagrama de Camadas
```
Domain                → Application                         → Presentation                            → UI
Configs / Values         Serviços + Eventos                    Bridges / Binders                          Slots / Animações
RuntimeAttribute*        RuntimeAttribute*Service              RuntimeAttribute*Bridge/CanvasBinder        RuntimeAttributeUISlot
```

### Caminhos Reais
- **Domain**: `Assets/_ImmersiveGames/Scripts/RuntimeAttributeSystems/Domain` (Configs, Values)
- **Application**: `Assets/_ImmersiveGames/Scripts/RuntimeAttributeSystems/Application/Services` + `RuntimeAttributeUpdateEvent.cs`
- **Presentation**: `Assets/_ImmersiveGames/Scripts/RuntimeAttributeSystems/Presentation/Bridges` e `Presentation/Bind`
- **UI**: `Assets/_ImmersiveGames/Scripts/RuntimeAttributeSystems/UI` e `UI/AnimationStrategies`
- **Suporte**: `Assets/_ImmersiveGames/Scripts/RuntimeAttributeSystems/Utils/RuntimeAttributeEventHub.cs`

## ✅ Componentes Estáveis
- `RuntimeAttributeContext` (Domain/Application boundary) — núcleo de dados por entidade.
- Serviços: `RuntimeAttributeCoordinator`, `RuntimeAttributeCanvasManager`, `RuntimeAttributeLinkService`, `RuntimeAttributeAutoFlowService`, `RuntimeAttributeThresholdService`.
- Bridges: `RuntimeAttributeBridgeBase`, `RuntimeAttributeAutoFlowBridge`, `RuntimeAttributeLinkBridge`, `RuntimeAttributeThresholdBridge`, `WorldSpaceBillboard`.
- Binders: `RuntimeAttributeSceneCanvasBinder`, `RuntimeAttributeDynamicCanvasBinder`, `RuntimeAttributeActorCanvas`.
- UI: `RuntimeAttributeUISlot`, estratégias de animação (`InstantFill`, `BasicReactiveFill`, `SmoothReactiveFill`), `FillAnimationStrategyFactory`.

## 🚧 Componentes em Revisão
- **`RuntimeAttributeBootstrapper`**: garantir ordem determinística de injeção entre bridges e binders.
- **`RuntimeAttributeEventHub`**: avaliar política de retenção de pendências para canvases tardios.
- **Perfis de Animação**: revisar `FillAnimationProfile` para suportar novos temas de HUD.

## 🔄 Fluxo de Execução Atual
1. **Bootstrap**: `RuntimeAttributeBootstrapper` resolve dependências globais e injeta em bridges/binders.
2. **Registro de Canvas**: `RuntimeAttributeActorCanvas` gera `CanvasId` e registra no `RuntimeAttributeCoordinator` e `RuntimeAttributeCanvasManager`.
3. **Bind**: coordenador publica `CanvasBindRequest` → pipeline executa `ScheduleBind` → `RuntimeAttributeUISlot` é criado e animado.
4. **Execução Reativa**: `RuntimeAttributeContext` emite `RuntimeAttributeUpdateEvent`; serviços de AutoFlow/Link/Threshold emitem eventos dedicados; UI reage via `RuntimeAttributeEventHub`.
5. **Cleanup**: canvases e bridges se desregistram, liberando slots (pool) e links.

## 🎨 Diagrama Simplificado
```
Actor
 ├─ RuntimeAttributeBridgeBase (Presentation)
 │    ├─ AutoFlow / Link / Threshold Bridges
 │    └─ WorldSpaceCanvasBillboard
 └─ RuntimeAttributeContext (Application)
       ├─ Link / AutoFlow / Threshold Services
       └─ RuntimeAttributeEventHub

Canvas
 ├─ RuntimeAttributeActorCanvas (Presentation)
 └─ RuntimeAttributeUISlot + AnimationStrategies (UI)
```

## 🧭 Tabela de Migração (Legado → Novo)

| Nome antigo | Nome novo | Pasta nova |
| ----------- | --------- | ---------- |
| `ResourceSystem` | `RuntimeAttributeContext` | `Application/Services` |
| `ActorResourceOrchestratorService` | `RuntimeAttributeCoordinator` | `Application/Services` |
| `CanvasPipelineManager` | `RuntimeAttributeCanvasManager` | `Application/Services` |
| `ResourceLinkService` | `RuntimeAttributeLinkService` | `Application/Services` |
| `ResourceAutoFlowService` | `RuntimeAttributeAutoFlowService` | `Application/Services` |
| `ResourceThresholdService` | `RuntimeAttributeThresholdService` | `Application/Services` |
| `ResourceEventHub` | `RuntimeAttributeEventHub` | `Utils` |
| `InjectableEntityResourceBridge` / `ResourceBridgeBase` | `RuntimeAttributeBridgeBase` | `Presentation/Bridges` |
| `ResourceAutoFlowBridge` | `RuntimeAttributeAutoFlowBridge` | `Presentation/Bridges` |
| `ResourceLinkBridge` | `RuntimeAttributeLinkBridge` | `Presentation/Bridges` |
| `ResourceThresholdBridge` | `RuntimeAttributeThresholdBridge` | `Presentation/Bridges` |
| `InjectableCanvasResourceBinder` / `DynamicCanvasBinder` | `RuntimeAttributeSceneCanvasBinder` / `RuntimeAttributeDynamicCanvasBinder` | `Presentation/Bind` |
| `ResourceUISlot` | `RuntimeAttributeUISlot` | `UI` |

## 🎯 Próximas Etapas
1. Consolidar logging estruturado por camada (Domain/Application/Presentation/UI) usando `DebugUtility` com níveis configuráveis.
2. Adicionar testes de integração para `RuntimeAttributeCanvasManager` (binds atrasados e rebind após reset).
3. Otimizar `RuntimeAttributeLinkService` para reduzir alocações no multiplayer local.
4. Documentar exemplos de uso por camada (Domain configs → Application services → Presentation bridges → UI slots) mantendo nomes padronizados.

## 🐛 Problemas Conhecidos
- Canvas dinâmico pode perder o primeiro bind se o `CanvasRegisteredEvent` chegar antes do bootstrap (mitigado pelo EventHub, mas precisa de teste dedicado).
- Perfis de animação não têm fallback para HUD com bar invertido; depende de atualização no `FillAnimationStrategyFactory`.

## 📊 Métricas de Sucesso
- Bind deve ocorrer em < 3 frames após `CanvasRegisteredEvent` (multiplayer local).
- Zero vazamento de slots no pool após destruição de ator/canvas.
- Eventos de link e threshold sem duplicidade por frame.

## 🔍 Troubleshooting Rápido
- **Bind não ocorre**: verificar `RuntimeAttributeEventHub` (pendências) e `RuntimeAttributeCanvasManager.ScheduleBind`.
- **UI não atualiza**: conferir se o ator tem `RuntimeAttributeContext` registrado e se o slot usa a animação correta.
- **Links não respeitados**: revisar `RuntimeAttributeLinkConfig` do ator e logs do `RuntimeAttributeLinkService`.
