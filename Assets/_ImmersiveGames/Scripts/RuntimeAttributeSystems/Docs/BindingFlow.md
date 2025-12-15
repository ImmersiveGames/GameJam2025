# 📌 Fluxos de Binding e Responsabilidades

Este resumo deixa claro quem cria, quem notifica e quem consome eventos no binding UI ↔ atributo.

## Criação e registro de Canvas
- **`RuntimeAttributeActorCanvas`**: gera `CanvasId`, inicializa pool de slots e registra-se no `RuntimeAttributeOrchestratorService`.
- **`RuntimeAttributeSceneCanvasBinder` / `RuntimeAttributeDynamicCanvasBinder`**: estendem a base e, opcionalmente, registram-se no `RuntimeAttributeCanvasPipelineManager` (Dynamic também notifica o `RuntimeAttributeEventHub`).
- **`CompassHUD`**: segue o mesmo contrato (`IAttributeCanvasBinder`) quando a HUD da bússola precisa de binds.

## Orquestração e notificações
- **`RuntimeAttributeOrchestratorService`**: cria binds iniciais para cada ator, cacheia pendências e publica `CanvasBindRequest` via `RuntimeAttributeEventHub`.
- **`RuntimeAttributeCanvasPipelineManager`**: registra canvases, consome `CanvasBindRequest` e executa `ScheduleBind` imediato quando o canvas está pronto.
- **`RuntimeAttributeEventHub`**: mantém pendências para canvases ainda não registrados e reenvia quando recebe `CanvasRegisteredEvent`.

## Bridges e assinatura de eventos
- **`RuntimeAttributeBridgeBase`**: resolve `IActor` e `RuntimeAttributeContext`, expondo `IRuntimeAttributeBridge` para serviços que dependem do contexto.
- **`RuntimeAttributeThresholdBridge`**: assina `FilteredEventBus<RuntimeAttributeThresholdEvent>` e dispara `RuntimeAttributeVisualFeedbackEvent`.
- **`RuntimeAttributeAutoFlowBridge`**: observa `ResourceChanging/Changed` no contexto e controla o serviço de `AutoFlow`.
- **`RuntimeAttributeLinkBridge`**: registra links no `RuntimeAttributeLinkService` global e remove no dispose.

## Linha do tempo simplificada
1. **Bootstrap**: `RuntimeAttributeBootstrapper` injeta dependências em binders/bridges.
2. **Registro**: canvases chamam `RegisterCanvas` (orquestrador + pipeline) e notificam `RuntimeAttributeEventHub` quando necessário.
3. **Bind**: orquestrador publica `CanvasBindRequest` e o pipeline executa `ScheduleBind` (criando slots de UI).
4. **Eventos**: serviços e bridges assinam os buses relevantes (thresholds, autflow, links) e propagam efeitos para a UI.
