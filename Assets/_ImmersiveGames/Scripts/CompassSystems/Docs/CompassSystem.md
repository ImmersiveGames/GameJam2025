# 🧭 Compass System — Doc Único (Runtime + UI)

> Documento condensado do sistema de bússola unindo lógica de runtime e HUD. Segue o formato dos guias existentes para facilitar leitura rápida por designers e programadores.

## Visão Geral

O Compass System conecta o gameplay ao Canvas por meio de um serviço global (`CompassRuntimeService`) e de uma HUD desacoplada (`CompassHUD`). O serviço mantém o `PlayerTransform` e uma lista de `ICompassTrackable`; a HUD consulta esse estado para instanciar ícones, calcular ângulos, aplicar clamp de bordas e atualizar distâncias/estilos. Todo o fluxo é pensado para multiplayer local, permitindo trocar o jogador ativo via `CompassPlayerBinder`.

## Componentes Principais

| Componente | Papel |
| --- | --- |
| `CompassRuntimeService` | Serviço global (DependencyManager + fallback estático) criado em `BeforeSceneLoad`. Expõe player e trackables, evita duplicatas e limpa referências nulas. |
| `CompassPlayerBinder` | Componente no jogador que publica o transform ao habilitar e limpa ao desabilitar, suportando respawn/troca de personagem. |
| `ICompassTrackable` / `CompassTarget` | Contrato e implementação padrão de alvos (Transform, tipo, estado). `CompassTarget` registra e remove automaticamente no serviço. |
| `CompassDamageLifecycleAdapter` | Bridge opcional que sincroniza `ActorMaster` + `DamageReceiver` com a bússola (remove em morte, reinsere em revive/reset) filtrando por `ActorId`. |
| `CompassHUD` | HUD no Canvas que sincroniza ícones com o runtime service, calcula posição X pelo ângulo relativo ao forward do player e atualiza distâncias. |
| `CompassIcon` | Prefab de UI que recebe `ICompassTrackable` + config visual para ajustar sprite, cor, tamanho, destaque e dinâmica de planetas. |
| `CompassSettings` | ScriptableObject com meio-ângulo da bússola, distâncias e flag `clampIconsAtEdges` para colar ou ocultar ícones fora do FOV. |
| `CompassTargetVisualConfig` + `CompassVisualDatabase` | Catálogo de sprites/cores/tamanhos por `CompassTargetType` com busca simples na HUD. |
| `PlanetResourceCompassStyleDatabase` | Opcional; mapeia `PlanetResources` para cores sem alterar tamanho base dos ícones de planeta. |
| `CompassPlanetHighlightController` | Observa o planeta marcado e chama `SetMarked` nos ícones correspondentes para aplicar escala de destaque. |

## Setup Rápido

1. **Assets de configuração**
   - Crie `CompassSettings` (menu `ImmersiveGames/UI/Compass/Settings`) e ajuste `compassHalfAngleDegrees`, distâncias e `clampIconsAtEdges` conforme o FOV desejado.
   - Gere `CompassTargetVisualConfig` para cada `CompassTargetType` utilizado e agrupe-os em um `CompassVisualDatabase`.
   - Se usar planetas com recurso, crie `PlanetResourceCompassStyleDatabase` e associe no config do tipo `Planet`.

2. **Cena de gameplay**
   - Adicione `CompassPlayerBinder` ao GameObject do jogador ativo (um por player local) para publicar o transform.
   - Marque alvos com `CompassTarget` (ou implemente `ICompassTrackable` custom) e selecione o `targetType`. Para atores danificáveis, adicione `CompassDamageLifecycleAdapter` no mesmo root que contém `ActorMaster` e `DamageReceiver`.

3. **Cena de HUD**
   - No Canvas carregado pelo pipeline, adicione `CompassHUD` e preencha `compassRectTransform`, `settings`, `visualDatabase` e o prefab `CompassIcon`.
   - A HUD segue o padrão de bind (`ICanvasBinder`) e registra-se no `CanvasPipelineManager`, mantendo IDs únicos via `IUniqueIdFactory` quando `autoGenerateCanvasId` estiver ativo.

## Fluxo em Runtime

1. `CompassRuntimeService` é instanciado antes das cenas e registrado no DependencyManager.
2. `CompassPlayerBinder` publica o transform do jogador atual no serviço. Trocas de personagem substituem a referência.
3. Cada `CompassTarget` (ou implementações de `ICompassTrackable`) registra-se no serviço durante o ciclo de vida; o adaptador de dano remove/recadastra conforme eventos de morte/renascimento/reset.
4. `CompassHUD` consulta o serviço a cada frame, sincroniza o dicionário de ícones, calcula ângulos relativos ao forward do player e aplica clamp/ocultação conforme `CompassSettings`.
5. Ícones de planeta em modo dinâmico trocam sprite ao serem descobertos e podem receber cor do `PlanetResourceCompassStyleDatabase`; `SetMarked` apenas ajusta `localScale` para destacar sem alterar posição.

## Boas Práticas

- **Desacoplamento de cenas**: mantenha gameplay e UI ligados apenas pelo `ICompassRuntimeService`; evite arrastar referências diretas entre cenas.
- **Idempotência**: `RegisterTarget/UnregisterTarget` são seguros contra duplicatas; ainda assim, chame-os nos eventos de ciclo de vida (`OnEnable/OnDisable`) dos trackables customizados.
- **Multiplayer local**: garanta que apenas o player ativo possua `CompassPlayerBinder` habilitado para evitar disputa de `PlayerTransform` global.
- **Clamp consciente**: use `clampIconsAtEdges` quando quiser feedback de direção mesmo fora do FOV; desative para ocultar ícones não visíveis.
- **Prefabs completos**: assegure `RectTransform`, `Image` (e opcional `TextMeshProUGUI`) no prefab de ícone para evitar sprites ou distâncias nulas.
- **Debug seguro**: mensagens verbosas do serviço ajudam a rastrear registro de alvos; mantenha `DebugUtility` configurado apenas em ambientes adequados.

## Solução de Problemas

| Sintoma | Verificações | Correções |
| --- | --- | --- |
| Nenhum ícone na HUD | `PlayerTransform` nulo? HUD sem referências de `settings`, `visualDatabase` ou prefab? | Adicione `CompassPlayerBinder` ao player ativo e configure campos na `CompassHUD`. |
| Ícone some fora do ângulo | `compassHalfAngleDegrees` pequeno ou clamp desativado. | Ajuste o ângulo ou habilite `clampIconsAtEdges`. |
| Alvos persistem após destruição | Implementação custom de `ICompassTrackable` não remove no ciclo de vida. | Chame `UnregisterTarget` em `OnDisable`/`OnDestroy` ou use `CompassTarget`. |
| Adaptador de dano não reage | `ActorId` vazio ou `CompassDamageLifecycleAdapter` sem `ActorMaster`/`DamageReceiver`. | Verifique dependências e certifique-se de registrar o ator correto no `FilteredEventBus`. |
| Planeta não destaca | `SetMarkedPlanet` não foi chamado ou HUD não expôs ícones. | Confirme integração com `CompassPlanetHighlightController` e que a `CompassHUD` está ativa. |

Mantenha o Compass System aderente aos princípios SOLID: serviço único para estado compartilhado, HUD como consumidor, componentes pequenos e especializados para registro, ciclo de vida e visual.
