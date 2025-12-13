# Sistema de Bússola — Guia de Referência

> Documentação oficial atualizada para a versão otimizada e totalmente funcional em cenas aditivas + multiplayer local.

## Visão Geral

Bússola 100% desacoplada entre gameplay e HUD (cena aditiva).  
O fluxo usa apenas o `CompassRuntimeService` como ponto de contato global.  
A HUD (`CompassHUD`) cria, posiciona e atualiza ícones com performance excelente (zero GC em runtime) e responsividade configurável.

## Componentes Principais

| Componente                            | Papel                                                                                           |
|---------------------------------------|-------------------------------------------------------------------------------------------------|
| `CompassRuntimeService`               | Serviço global (DependencyManager + fallback). Mantém `PlayerTransform` e lista de `ICompassTrackable`. |
| `CompassPlayerBinder`                 | Colocado no player ativo → publica e limpa o `PlayerTransform` automaticamente.                |
| `ICompassTrackable` / `CompassTarget` | Contrato de alvos. `CompassTarget` registra/desregistra automaticamente.                      |
| `CompassSettings`                     | Ângulo FOV, distâncias e clamp nas bordas.                                                     |
| `CompassTargetVisualConfig` + `CompassVisualDatabase` | Config visual por `CompassTargetType` (sprite, cor, tamanho, modo dinâmico).          |
| `CompassIcon`                         | Prefab UI que recebe alvo + config, atualiza sprite, cor, distância e highlight.               |
| `CompassHUD` (v2.1)                    | HUD otimizada – cria ícones, sincroniza em cenas aditivas, atualização configurável (default 60 FPS). |
| `PlanetResourceCompassStyleDatabase`  | Cor por tipo de recurso (apenas cor – tamanho continua definido pelo tipo de alvo).            |
| `CompassPlanetHighlightController`   | Escuta planeta marcado → aplica `SetMarked` com escala + tint opcional (zero alocação).        |

## Novidades da Versão 2.1 (Performance & Usabilidade)

- Atualização da bússola agora **configurável no Inspector**  
  ```csharp
  [Range(0.008f, 0.2f)]
  public float updateInterval = 0.016f; // 60 FPS (recomendado)
  ```
Valores comuns:
- **0.016** → 60 FPS (fluido máximo)
- **0.033** → 30 FPS (excelente equilíbrio)
- **0.066** → 15 FPS (muito leve, ainda aceitável)

- Zero alocação em runtime:
   - `ForEachIcon(Action<…>)` substitui `IEnumerable` com yield
   - Sem LINQ nem `GetComponentInParent` em loop
   - `SetMarked` evita set de scale/color quando valor já é o mesmo

- Criação de ícones 100% funcional em cenas aditivas (ícones aparecem mesmo se o player/trackables carregarem depois da HUD).

- `CompassHUD` agora implementa corretamente `GetObjectId()` para o pipeline de injeção.

## Setup Essencial (2025)

### 1. Assets de Configuração
- `CompassSettings` → ajuste ângulo, distâncias e `clampIconsAtEdges`.
- Crie um `CompassTargetVisualConfig` para cada tipo (Planet, Objective, etc.) → agrupe em um `CompassVisualDatabase`.

### 2. Cena de Gameplay
- Player → adicione `CompassPlayerBinder`.
- Alvos → `CompassTarget` (ou implemente `ICompassTrackable` com registro manual).

### 3. Cena de HUD (aditiva)
- Canvas → adicione `CompassHUD`
   - Preencha: `compassRectTransform`, `settings`, `visualDatabase`, `iconPrefab`
   - Ajuste `Update Interval` no Inspector (recomendado **0.016**)

### 4. Highlight de Planeta Marcado
```csharp
// Qualquer sistema (ex: UI de seleção de planeta)
compassPlanetHighlightController.SetMarkedPlanet(planetsMasterInstance);
```

### 5. Estilo Dinâmico de Planetas
- `CompassTargetVisualConfig` (tipo **Planet**):
   - `dynamicMode = PlanetResourceIcon`
   - `planetResourceStyleDatabase` → opcional (define cor por recurso)
   - `hideUntilDiscovered` → true/false conforme desejado

## Boas Práticas (Atualizadas)

| Tema                         | Recomendação                                                                                   |
|------------------------------|------------------------------------------------------------------------------------------------|
| Responsividade               | Deixe `updateInterval = 0.016f` (60 FPS) para sensação premium. Use 0.033f em mobile se precisar. |
| Cenas aditivas               | Não há mais ordem crítica – `CompassHUD` cria ícones assim que os trackables aparecerem.       |
| Multiplayer local            | Apenas o `CompassPlayerBinder` ativo define o player. Troca de personagem funciona instantaneamente. |
| Performance                  | Zero GC em runtime. Testado com 80+ planetas simultâneos sem impacto perceptível.              |
| Highlight                    | `SetMarked` é zero-alloc e evita redefinição de scale/color quando já está no estado desejado. |
| Organização                  | Scripts de teste removidos. Apenas código de runtime e configuração na pasta `Scripts/UI/Compass`. |

## Solução de Problemas (Atualizada)

| Sintoma                              | Verificação                                                   | Correção sugerida                                                     |
|--------------------------------------|---------------------------------------------------------------|-----------------------------------------------------------------------|
| Ícones não aparecem                  | `CompassPlayerBinder` no player ativo? HUD tem prefab/config? | Adicione binder + preencha todos os campos da HUD                     |
| Ícones travam / atualização lenta   | `updateInterval` muito alto?                                  | Ajuste para **0.016** (60 FPS) no Inspector da `CompassHUD`           |
| Ícones não acompanham movimento      | `updateInterval` muito baixo?                                 | Aumente para 0.016–0.033                                              |
| Highlight não funciona               | `CompassPlanetHighlightController` tem referência da HUD?    | Use `[RequireComponent(typeof(CompassHUD))]` – já incluso na versão atual |
| Cor de planeta descoberto errada     | `PlanetResourceCompassStyleDatabase` atribuído na config?    | Preencha o database ou deixe nulo para cor padrão                     |
| Ícones somem fora do FOV             | `clampIconsAtEdges` desativado + ângulo pequeno               | Ative clamp ou aumente `compassHalfAngleDegrees`                      |

---