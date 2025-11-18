# 🧭 Sistema de Bússola — Guia de Referência

> Inspirado na estrutura do `Eater System` docs, este guia lista responsabilidades, fluxo de runtime, padrões de integração e troubleshooting em formato curto e direto.

## Visão Geral

A bússola conecta a cena de gameplay à HUD carregada de forma aditiva sem dependências diretas pelo inspector. O fluxo utiliza `CompassRuntimeService` para expor `PlayerTransform` e os `ICompassTrackable` ativos, permitindo que a `CompassHUD` instancie ícones, calcule ângulos e atualize distâncias seguindo o pipeline de canvas do projeto.【F:Assets/_ImmersiveGames/Scripts/World/Compass/CompassRuntimeService.cs†L6-L72】【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassHUD.cs†L13-L296】

## Componentes Registrados

| Componente | Papel | Links rápidos |
| --- | --- | --- |
| `CompassRuntimeService` | Serviço estático com o `PlayerTransform` e lista somente leitura de alvos registrados, tratando nulos e duplicatas ao registrar/desregistrar. | 【F:Assets/_ImmersiveGames/Scripts/World/Compass/CompassRuntimeService.cs†L6-L72】 |
| `CompassPlayerBinder` | Colocado no jogador; publica o `transform` ao habilitar e limpa ao desabilitar, mantendo a referência ao trocar de personagem. | 【F:Assets/_ImmersiveGames/Scripts/World/Compass/CompassPlayerBinder.cs†L5-L20】 |
| `ICompassTrackable` / `CompassTarget` | Contrato base de alvos (Transform, tipo, estado). `CompassTarget` registra-se automaticamente no serviço. | 【F:Assets/_ImmersiveGames/Scripts/World/Compass/ICompassTrackable.cs†L6-L26】【F:Assets/_ImmersiveGames/Scripts/World/Compass/CompassTarget.cs†L6-L29】 |
| `CompassSettings` | Define campo angular, distâncias e clamp de bordas para posicionamento dos ícones. | 【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassSettings.cs†L5-L25】 |
| `CompassTargetVisualConfig` + `CompassVisualDatabase` | Configuram ícones/cores/tamanhos por `CompassTargetType` e expõem busca simples por tipo. | 【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassTargetVisualConfig.cs†L5-L28】【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassVisualDatabase.cs†L6-L36】 |
| `CompassIcon` | Prefab de UI que recebe `ICompassTrackable` + config visual, atualiza sprite, cor, tamanho, distância e highlight/estilos de recurso para planetas. | 【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassIcon.cs†L8-L210】 |
| `CompassHUD` | HUD registrada no pipeline de canvas; sincroniza ícones com o runtime service e posiciona-os conforme ângulo relativo ao jogador. | 【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassHUD.cs†L53-L259】 |
| `PlanetResourceCompassStyleDatabase` | Opcional; fornece cor por `PlanetResources` para planetas descobertos sem alterar tamanho. | 【F:Assets/_ImmersiveGames/Scripts/UI/Compass/PlanetResourceCompassStyleDatabase.cs†L6-L26】 |
| `CompassPlanetHighlightController` | Observa o planeta marcado (PlanetsMaster) e aplica `SetMarked(true/false)` nos ícones correspondentes. | 【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassPlanetHighlightController.cs†L7-L88】 |

## Setup Essencial

1. **Assets de configuração**
   - Crie `CompassSettings` em `ImmersiveGames/UI/Compass/Settings` e ajuste `compassHalfAngleDegrees`, distâncias e `clampIconsAtEdges` conforme o FOV desejado.【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassSettings.cs†L8-L24】
   - Crie `CompassTargetVisualConfig` para cada `CompassTargetType` usado e agrupe em um `CompassVisualDatabase` (menu `ImmersiveGames/UI/Compass/Visual Database`).【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassTargetVisualConfig.cs†L5-L28】【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassVisualDatabase.cs†L6-L36】

2. **Cena de gameplay**
   - Adicione `CompassPlayerBinder` ao GameObject do player para publicar o transform atual.【F:Assets/_ImmersiveGames/Scripts/World/Compass/CompassPlayerBinder.cs†L5-L20】
   - Marque alvos com `CompassTarget` (ou implemente `ICompassTrackable`) e selecione o `targetType`. Para planetas, use `Planet` e deixe `PlanetsMaster` no pai para habilitar ícone dinâmico.【F:Assets/_ImmersiveGames/Scripts/World/Compass/CompassTarget.cs†L11-L29】【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassIcon.cs†L97-L210】

3. **Cena de HUD**
   - No Canvas carregado via pipeline, adicione `CompassHUD`, referencie `compassRectTransform`, `settings`, `visualDatabase` e o prefab `CompassIcon`.
   - A HUD segue o padrão de bind (`ICanvasBinder`) e se registra no `CanvasPipelineManager`, mantendo IDs únicos via `IUniqueIdFactory` quando `autoGenerateCanvasId` está ativo.【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassHUD.cs†L36-L87】

## Fluxo em Runtime

1. `CompassPlayerBinder` publica o player no `CompassRuntimeService` ao habilitar.【F:Assets/_ImmersiveGames/Scripts/World/Compass/CompassPlayerBinder.cs†L11-L19】
2. Cada `CompassTarget` registra-se no serviço durante seu ciclo de vida, e a HUD sincroniza o dicionário de ícones a cada frame.【F:Assets/_ImmersiveGames/Scripts/World/Compass/CompassTarget.cs†L15-L23】【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassHUD.cs†L94-L175】
3. Para cada alvo ativo, a HUD calcula o ângulo relativo ao forward do jogador, aplica clamp conforme `CompassSettings`, converte em posição X no `RectTransform` e atualiza distância.【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassHUD.cs†L175-L240】
4. Planetas em `dynamicMode = PlanetResourceIcon` trocam o sprite de genérico → `ResourceIcon` ao serem descobertos e podem ter cor ajustada por `PlanetResourceCompassStyleDatabase`; o tamanho permanece definido pelo `baseSize` do tipo `Planet`. O highlight altera apenas o `localScale` do ícone selecionado.【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassIcon.cs†L97-L210】【F:Assets/_ImmersiveGames/Scripts/UI/Compass/PlanetResourceCompassStyleDatabase.cs†L6-L26】
5. `CompassPlanetHighlightController` reage à marcação de planetas e chama `SetMarked` nos ícones correspondentes para ampliar o destaque sem alterar posicionamento.【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassPlanetHighlightController.cs†L35-L88】

## Exemplos Rápidos

### Registro condicional de inimigo

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

### Estilo de planeta por recurso

```csharp
// Configuração (ScriptableObject)
// - Crie PlanetResourceCompassStyleDatabase e defina cores por PlanetResources.
// - Em CompassTargetVisualConfig (tipo Planet), atribua planetResourceStyleDatabase.
```

### Destaque de planeta marcado

```csharp
// Em runtime, chame highlightController.SetMarkedPlanet(planetsMaster);
// O ícone correspondente recebe SetMarked(true) e escala 30% maior.
```

## Boas Práticas

- **Separação de cenas**: mantenha HUD e gameplay desacoplados via `CompassRuntimeService`; evite referências diretas pelo inspector.
- **Tamanho por tipo**: ajuste `baseSize` em `CompassTargetVisualConfig` por tipo de alvo. Estilos de recurso afetam apenas cor, não tamanho.【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassTargetVisualConfig.cs†L5-L28】【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassIcon.cs†L153-L210】
- **Clamp vs. ocultação**: use `clampIconsAtEdges` para decidir se ícones fora do FOV colam na borda ou somem.【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassSettings.cs†L22-L24】【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassHUD.cs†L187-L238】
- **Prefabs completos**: garanta `RectTransform`, `Image` e (opcional) `TextMeshProUGUI` no prefab de ícone para evitar sprites nulos.【F:Assets/_ImmersiveGames/Scripts/UI/Compass/CompassIcon.cs†L13-L72】
- **Multiplayer local**: como o serviço é estático, trocas de player (respawn ou split-screen local) devem substituir o transform via `CompassPlayerBinder` ativo no personagem correto.
- **Highlight não invasivo**: `SetMarked` altera apenas `localScale`, preservando tamanho base e cálculo de posição.

## Solução de Problemas

| Sintoma | Verificações | Correções sugeridas |
| --- | --- | --- |
| Ícones não aparecem | `PlayerTransform` nulo? `CompassHUD` possui `compassRectTransform`, `settings`, `visualDatabase` e `iconPrefab` preenchidos? | Adicione `CompassPlayerBinder` ao player e configure a HUD. |
| Ícone some fora do FOV | Campo angular menor que 180° com clamp desativado. | Ajuste `compassHalfAngleDegrees` ou habilite `clampIconsAtEdges`. |
| Ícones ficam presos após destruir objetos | Implementação customizada de `ICompassTrackable` não remove no ciclo de vida. | Chame `UnregisterTarget` em `OnDisable`/`OnDestroy` ou use `CompassTarget`. |
| Highlight não responde | `CompassPlanetHighlightController` não conhece o planeta marcado ou HUD não expõe os ícones. | Certifique-se de chamar `SetMarkedPlanet` com o `PlanetsMaster` correto e que `CompassHUD` está ativo. |
| Cor errada para planeta descoberto | Database de estilo não configurada ou tipo de recurso não mapeado. | Preencha `PlanetResourceCompassStyleDatabase` ou verifique o `PlanetResources` recebido de `PlanetsMaster`. |

Mantenha a bússola alinhada ao pipeline de canvas e aos princípios SOLID, preservando responsabilidades claras entre gameplay, serviço de runtime e HUD.
