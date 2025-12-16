# 📐 Utilitário `CalculateRealLength` — Guia de Uso

## Visão Geral

`CalculateRealLength` é um helper estático responsável por obter `Bounds` reais de um `GameObject`, respeitando hierarquia e objetos que devem ser ignorados. É útil para cálculos de colisão, posicionamento automático de HUD e alinhamento de áreas interativas no multiplayer local.

## Como Funciona

1. **Entrada** — Recebe um `GameObject` raiz.
2. **Render Bounds Direto** — Tenta capturar `Renderer.bounds` diretamente do objeto.
3. **Fallback Recursivo** — Se o objeto não possui renderer próprio (extents zero), percorre os filhos recursivamente.
4. **Ignorar Filhos** — Filhos com o componente `IgnoreBoundsFlag` (em `_ImmersiveGames.Scripts.Tags`) são desconsiderados.
5. **Agregação** — Usa `Bounds.Encapsulate` para combinar bounds dos filhos válidos.

## Exemplo de Uso

```csharp
var meshBounds = CalculateRealLength.GetBounds(actorRoot);
var radius = meshBounds.extents.magnitude;
```

## Boas Práticas

* Marque objetos decorativos com `IgnoreBoundsFlag` para evitar distorção do cálculo.
* Reutilize o resultado quando possível; chamadas profundas percorrem toda a hierarquia.
* Combine com `UniqueIdFactory` para registrar métricas por ator (`actor.ActorId`).

## Casos de Uso

| Cenário | Benefício |
| --- | --- |
| Ajuste automático de câmeras | Determine distância ideal baseada no tamanho real do ator. |
| Spawns seguros | Garante espaço livre mínimo antes de instanciar novos objetos. |
| UI world-space responsiva | Dimensiona elementos com base no volume real do personagem. |

O método segue o princípio SRP: apenas calcula bounds, deixando decisões de gameplay para camadas superiores.
