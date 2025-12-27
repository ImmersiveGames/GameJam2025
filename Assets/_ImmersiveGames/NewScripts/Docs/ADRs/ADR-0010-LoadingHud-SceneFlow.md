# ADR-0010 — Loading HUD + SceneFlow (NewScripts)

**Data:** 2025-12-25
**Status:** Implementado

## Contexto
O Scene Flow do NewScripts já possui Fade (ADR-0009), porém o HUD de loading precisa ser um módulo separado,
sem depender de legados e sem acoplar ao Fade. Além disso, a transição pode aguardar o completion gate
(WorldLifecycle reset), o que exige manter o HUD visível até o momento correto, mesmo quando o HUD nasce tarde.

## Decisão
Implementar um módulo de Loading HUD separado do Fade, com as regras:

1) **Show no Started**
- Ao receber `SceneTransitionStartedEvent`, o HUD é exibido com texto "Carregando...".

2) **Manter durante o gate**
- O HUD permanece visível durante `SceneTransitionScenesReadyEvent` e durante o completion gate.
- Texto atualizado para "Preparando..." como fase intermediária.

3) **Hide antes do FadeOut**
- Um novo evento `SceneTransitionBeforeFadeOutEvent` é emitido antes do FadeOut.
- O HUD é ocultado nesse evento para evitar piscar no momento de revelar a cena.

4) **HUD pronto + ordenação acima do Fade**
- O `SceneLoadingHudController` registra `ISceneLoadingHud` no DI global e se anexa explicitamente ao serviço.
- O serviço aplica o estado pendente imediatamente após o attach.
- O Canvas do HUD deve usar `overrideSorting=true` e `sortingOrder` maior que o Fade (ex.: 12050).

5) **Pending quando HUD nasce tarde**
- Se o HUD ainda não existir no `Started`/`ScenesReady`, o serviço guarda pendências por assinatura da transição.
- Ao anexar o HUD ou receber `SceneLoadingHudRegisteredEvent`, aplica o último estado pendente imediatamente.

6) **Host no UIGlobalScene**
- O `SceneLoadingHudController` vive no `UIGlobalScene`.
- Caso o HUD nasça tarde, o serviço aplica o estado atual imediatamente.

## Consequências
- Fade e Loading permanecem responsabilidades separadas.
- O HUD pode existir sem exigir QA ou legados.
- A experiência mantém consistência mesmo quando o `UIGlobalScene` é carregado após o `Started`.

## Referências
- ADR-0009 — Fade + SceneFlow (NewScripts)

## Atualização (2025-12-27)
- O pipeline atual mantém o gate de completion antes do `FadeOut`, garantindo janela segura para o HUD
  permanecer visível até a conclusão do reset (quando aplicável).

## Evidências (log)
- `SceneTransitionService` executa `Started → FadeIn → Load/Unload → ScenesReady → gate → FadeOut → Completed`.
- `SceneFlowLoadingService` aparece registrado no bootstrap global.
