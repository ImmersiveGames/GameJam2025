# 🧭 Sistema de Bússola — Guia de Referência

> Inspirado na estrutura do `Eater System` docs, este guia lista responsabilidades, fluxo de runtime, padrões de integração e troubleshooting em formato curto e direto.

## Visão Geral

A bússola conecta a cena de gameplay à HUD carregada de forma aditiva sem dependências diretas pelo inspector. O fluxo utiliza `CompassRuntimeService` para expor `PlayerTransform` e os `ICompassTrackable` ativos, permitindo que a `CompassHUD` instancie ícones, calcule ângulos e atualize distâncias seguindo o pipeline de canvas do projeto.

## Componentes Registrados

| Componente | Papel |
| --- | --- |
| `CompassRuntimeService` | Serviço estático com o `PlayerTransform` e lista somente leitura de alvos registrados, tratando nulos e duplicatas ao registrar/desregistrar. |
| `CompassPlayerBinder` | Colocado no jogador; publica o `transform` ao habilitar e limpa ao desabilitar, mantendo a referência ao trocar de personagem. |
| `ICompassTrackable` / `CompassTarget` | Contrato base de alvos (Transform, tipo, estado). `CompassTarget` registra-se automaticamente no serviço. |
| `CompassSettings` | Define campo angular, distâncias e clamp de bordas para posicionamento dos ícones. |
| `CompassTargetVisualConfig` + `CompassVisualDatabase` | Configuram ícones/cores/tamanhos por `CompassTargetType` e expõem busca simples por tipo. |
| `CompassIcon` | Prefab de UI que recebe `ICompassTrackable` + config visual, atualiza sprite, cor, tamanho, distância e highlight/estilos de recurso para planetas. |
| `CompassHUD` | HUD registrada no pipeline de canvas; sincroniza ícones com o runtime service e posiciona-os conforme ângulo relativo ao jogador. |
| `PlanetResourceCompassStyleDatabase` | Opcional; fornece cor por `PlanetResources` para planetas descobertos sem alterar tamanho. |
| `CompassPlanetHighlightController` | Observa o planeta marcado (PlanetsMaster) e aplica `SetMarked(true/false)` nos ícones correspondentes. |

## Setup Essencial

1. **Assets de configuração**
   - Crie `CompassSettings` em `ImmersiveGames/UI/Compass/Settings` e ajuste `compassHalfAngleDegrees`, distâncias e `clampIconsAtEdges` conforme o FOV desejado.
   - Crie `CompassTargetVisualConfig` para cada `CompassTargetType` usado e agrupe em um `CompassVisualDatabase` (menu `ImmersiveGames/UI/Compass/Visual Database`).

2. **Cena de gameplay**
   - Adicione `CompassPlayerBinder` ao GameObject do player para publicar o transform atual.
   - Marque alvos com `CompassTarget` (ou implemente `ICompassTrackable`) e selecione o `targetType`. Para planetas, use `Planet` e deixe `PlanetsMaster` no pai para habilitar ícone dinâmico.

3. **Cena de HUD**
   - No Canvas carregado via pipeline, adicione `CompassHUD`, referencie `compassRectTransform`, `settings`, `visualDatabase` e o prefab `CompassIcon`.
   - A HUD segue o padrão de bind (`ICanvasBinder`) e se registra no `CanvasPipelineManager`, mantendo IDs únicos via `IUniqueIdFactory` quando `autoGenerateCanvasId` está ativo.

## Fluxo em Runtime

1. `CompassPlayerBinder` publica o player no `CompassRuntimeService` ao habilitar.
2. Cada `CompassTarget` registra-se no serviço durante seu ciclo de vida, e a HUD sincroniza o dicionário de ícones a cada frame.
3. Para cada alvo ativo, a HUD calcula o ângulo relativo ao forward do jogador, aplica clamp conforme `CompassSettings`, converte em posição X no `RectTransform` e atualiza distância.
4. Planetas em `dynamicMode = PlanetResourceIcon` trocam o sprite de genérico → `ResourceIcon` ao serem descobertos e podem ter cor ajustada por `PlanetResourceCompassStyleDatabase`; o tamanho permanece definido pelo `baseSize` do tipo `Planet`. O highlight altera apenas o `localScale` do ícone selecionado.
5. `CompassPlanetHighlightController` reage à marcação de planetas e chama `SetMarked` nos ícones correspondentes para ampliar o destaque sem alterar posicionamento.

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
- **Tamanho por tipo**: ajuste `baseSize` em `CompassTargetVisualConfig` por tipo de alvo. Estilos de recurso afetam apenas cor, não tamanho.
- **Clamp vs. ocultação**: use `clampIconsAtEdges` para decidir se ícones fora do FOV colam na borda ou somem.
- **Prefabs completos**: garanta `RectTransform`, `Image` e (opcional) `TextMeshProUGUI` no prefab de ícone para evitar sprites nulos.
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
