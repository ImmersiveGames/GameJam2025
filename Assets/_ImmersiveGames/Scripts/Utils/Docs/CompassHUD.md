# 🧭 Sistema de Bússola — Guia de Uso

## Visão Geral

A bússola conecta a cena de gameplay à HUD carregada de forma aditiva sem depender de referências arrastadas pelo inspector. O fluxo usa o serviço estático `CompassRuntimeService` para expor o `PlayerTransform` e todos os `ICompassTrackable` ativos, permitindo que a `CompassHUD` instancie ícones, calcule ângulos e atualize distâncias seguindo o pipeline de canvas do projeto.

## Componentes Principais

- **CompassRuntimeService** — Mantém o `PlayerTransform` e uma lista somente leitura de alvos registrados, permitindo registro/desregistro seguro (ignora nulos e duplicatas) para consumo da HUD.【F:Assets/_ImmersiveGames/Scripts/World/Compass/CompassRuntimeService.cs†L6-L72】
- **CompassPlayerBinder** — Colocado no GameObject do jogador; publica o `transform` no serviço ao habilitar e limpa ao desabilitar, mantendo a referência correta mesmo em trocas de cena.【F:Assets/_ImmersiveGames/Scripts/World/Compass/CompassPlayerBinder.cs†L5-L20】
- **ICompassTrackable / CompassTarget** — Contrato para objetos rastreáveis (Transform, tipo, estado). `CompassTarget` implementa o básico, registrando-se automaticamente no serviço quando habilitado.【F:Assets/_ImmersiveGames/Scripts/World/Compass/ICompassTrackable.cs†L6-L26】【F:Assets/_ImmersiveGames/Scripts/World/Compass/CompassTarget.cs†L6-L29】
- **Configurações e Visual** — `CompassSettings` define ângulo, distâncias e comportamento de clamp; `CompassTargetVisualConfig` e `CompassVisualDatabase` mapeiam ícones/cores/tamanhos por tipo de alvo.【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassSettings.cs†L5-L25】【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassTargetVisualConfig.cs†L5-L24】【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassVisualDatabase.cs†L6-L33】
- **CompassHUD + CompassIcon** — A HUD consulta o serviço, cria ícones conforme o banco visual, posiciona-os com base no ângulo relativo ao jogador e registra-se no `CanvasPipelineManager` seguindo o padrão de injeção do projeto.【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassHUD.cs†L13-L296】【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassIcon.cs†L8-L55】

## Setup Rápido

1. **Assets de configuração**
   - Crie um `CompassSettings` via menu `ImmersiveGames/UI/Compass/Settings` e ajuste `compassHalfAngleDegrees`, distâncias e `clampIconsAtEdges` conforme o campo de visão desejado.【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassSettings.cs†L8-L24】
   - Crie um `CompassVisualDatabase` e adicione entradas de `CompassTargetVisualConfig` (menu `ImmersiveGames/UI/Compass/Target Visual Config`) para cada `CompassTargetType` usado.【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassTargetVisualConfig.cs†L5-L24】【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassVisualDatabase.cs†L6-L33】

2. **Cena de gameplay**
   - No GameObject do player, adicione `CompassPlayerBinder` para publicar o transform atual ao serviço.【F:Assets/_ImmersiveGames/Scripts/World/Compass/CompassPlayerBinder.cs†L5-L20】
   - Nos objetos rastreáveis, use `CompassTarget` e escolha o `targetType` apropriado; para comportamentos customizados, implemente `ICompassTrackable` diretamente.【F:Assets/_ImmersiveGames/Scripts/World/Compass/CompassTarget.cs†L11-L29】【F:Assets/_ImmersiveGames/Scripts/World/Compass/ICompassTrackable.cs†L6-L26】

3. **Cena de HUD (carregada via pipeline)**
   - No Canvas da HUD, adicione `CompassHUD`, referencie `compassRectTransform`, `settings`, `visualDatabase` e o prefab `CompassIcon`.
   - O componente registra-se automaticamente para injeção (`ResourceInitializationManager`) e no `CanvasPipelineManager`, alinhando-se às outras HUDs sem buscas globais.【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassHUD.cs†L53-L87】

## Fluxo em Runtime

1. `CompassPlayerBinder` publica o player no `CompassRuntimeService` ao habilitar.【F:Assets/_ImmersiveGames/Scripts/World/Compass/CompassPlayerBinder.cs†L11-L19】
2. Cada `CompassTarget` registra-se no serviço quando habilitado e é removido ao desabilitar.【F:Assets/_ImmersiveGames/Scripts/World/Compass/CompassTarget.cs†L15-L23】
3. `CompassHUD` sincroniza ícones com `Trackables`, aplicando a configuração visual correta e removendo alvos inexistentes.【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassHUD.cs†L94-L259】
4. Para cada alvo ativo, a HUD calcula o ângulo relativo ao forward do jogador, aplica clamp conforme `CompassSettings`, converte para posição X no `RectTransform` e atualiza a distância exibida.【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassHUD.cs†L175-L240】

## Exemplos de Uso

### Marcar um inimigo com ativação condicional

```csharp
using _ImmersiveGames.Scripts.World.Compass;
using UnityEngine;

public class EnemyCompassTracker : MonoBehaviour, ICompassTrackable
{
    [SerializeField] private bool showOnCompass = true;
    [SerializeField] private CompassTargetType type = CompassTargetType.Enemy;

    private void OnEnable() => CompassRuntimeService.RegisterTarget(this);
    private void OnDisable() => CompassRuntimeService.UnregisterTarget(this);

    public Transform Transform => transform;
    public CompassTargetType TargetType => type;
    public bool IsActive => showOnCompass && gameObject.activeInHierarchy;
}
```

### Atualizar rótulo de distância extra

```csharp
using _ImmersiveGames.Scripts.UI.Compass;
using UnityEngine;

public class CompassIconExtra : CompassIcon
{
    public void SetCustomText(string text)
    {
        if (distanceLabel != null)
        {
            distanceLabel.text = text;
        }
    }
}
```

## Boas Práticas

- **Clamp ou ocultação** — Use `clampIconsAtEdges` para decidir se alvos fora do campo angular aparecem nas extremidades ou são ocultados.【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassSettings.cs†L22-L24】【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassHUD.cs†L187-L238】
- **Prefabs coesos** — Garanta que `iconPrefab` possua `RectTransform`, `Image` e (opcionalmente) `TextMeshProUGUI` atribuídos para evitar ícones sem visual.【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassIcon.cs†L13-L55】
- **Multiplayer local** — Como o serviço é estático, trocas de player (ex.: respawn) devem substituir o transform via `CompassPlayerBinder` ativo no novo personagem.【F:Assets/_ImmersiveGames/Scripts/World/Compass/CompassPlayerBinder.cs†L5-L20】
- **Integrado ao pipeline** — Deixe `autoGenerateCanvasId` ativo para que a HUD gere IDs únicos via `IUniqueIdFactory` e registre-se no `CanvasPipelineManager` sem colisões.【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassHUD.cs†L36-L79】

## Solução de Problemas

| Sintoma | Verificações | Correções sugeridas |
| --- | --- | --- |
| Ícones não aparecem | `CompassRuntimeService.PlayerTransform` está nulo? `CompassHUD` tem `compassRectTransform` e `iconPrefab` atribuídos? | Adicione `CompassPlayerBinder` ao player e preencha referências da HUD. |
| Ícone desaparece fora do FOV | Campo angular (`compassHalfAngleDegrees`) menor que 180° com clamp desativado? | Ajuste o ângulo ou habilite `clampIconsAtEdges` para fixar nas extremidades. |
| Alvos persistem após serem destruídos | Implementação customizada de `ICompassTrackable` não chama `UnregisterTarget` em `OnDisable`/`OnDestroy`. | Adicione a remoção no ciclo de vida ou use `CompassTarget`. |
| IDs de canvas colidindo | `canvasId` vazio com `autoGenerateCanvasId` desabilitado. | Mantenha a geração automática ou defina IDs únicos manualmente. |

Aplique estes passos para manter a bússola coerente com o pipeline de HUD, respeitando separação de responsabilidades e a arquitetura modular do projeto.
