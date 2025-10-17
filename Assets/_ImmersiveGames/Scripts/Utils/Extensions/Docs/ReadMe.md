# 🧩 Extensões Utilitárias — Guia de Uso (v1.0)

## 📚 Índice
1. [Visão Geral](#visão-geral)
2. [ComponentExtensions](#componentextensions)
3. [TransformExtensions](#transformextensions)
4. [TweenExtensions (DOTween)](#tweenextensions-dotween)
5. [Casos de Uso Recomendados](#casos-de-uso-recomendados)
6. [Boas Práticas](#boas-práticas)

---

## 🎯 Visão Geral

Este módulo agrega extensões leves para componentes Unity, orientadas a reduzir boilerplate em prefabs compartilhados e fluxos multiplayer. Todas as extensões seguem SRP e podem ser combinadas com o sistema de dependências sem acoplamento.

---

## 🧱 `ComponentExtensions`

Conjunto de métodos focados em descoberta ou criação dinâmica de componentes.

### `TryGetComponentInParent<T>`
* Percorre a hierarquia ascendente até encontrar um componente do tipo `T`.
* Útil para localizar `IActor` ou serviços registrados na raiz do player.

### `TryGetComponentInChildren<T>`
* Vasculha filhos (opcionalmente inativos) recursivamente.
* Mantém o comportamento do `TryGetComponent` base, retornando `bool` e `out T`.

### `GetOrCreateComponentInChild<T>` (Component / Transform)
* Busca por um componente em filhos; se ausente, cria um `GameObject` filho, reseta transform e adiciona o componente.
* Ideal para configurar slots dinâmicos (ex.: indicadores de recursos) sem poluir o prefab base.

---

## 🌀 `TransformExtensions`

### `IsChildOrSelf`
* Disponível em duas assinaturas (`Transform` e `GameObject`).
* Verifica se o alvo é o próprio transform ou um descendente. Útil para validação de interações locais (ex.: limitar efeitos a um jogador).

---

## 🎚️ `TweenExtensions (DOTween)`

### `DoFillAmount`
* Extensão para `UnityEngine.UI.Image` que recria `DOFillAmount` da versão Pro do DOTween usando apenas a versão gratuita.
* Mantém target vinculado ao tween (`SetTarget`).
* Atualiza `fillAmount` via `DOTween.To`, garantindo compatibilidade com a pipeline de UI reativa.

Uso típico:
```csharp
healthImage.DoFillAmount(targetPercentage, duration: 0.25f)
           .SetEase(Ease.OutCubic);
```

---

## 🧪 Casos de Uso Recomendados

| Cenário | Extensão | Benefícios |
| --- | --- | --- |
| UI dinâmica por jogador | `GetOrCreateComponentInChild<T>` | Garante que cada jogador receba seus elementos sem duplicar prefabs. |
| Checagem de hierarquia para inputs | `IsChildOrSelf` | Evita controlar objetos fora do escopo do jogador. |
| Interpolação de barras de recurso | `DoFillAmount` | Simula animação suave sem necessidade da versão Pro do DOTween. |

---

## ✅ Boas Práticas

* Prefira `TryGet...` ao invés de `GetComponent` direto para evitar exceções em runtime.
* Ao criar filhos dinamicamente, defina nomes explícitos (parâmetro `childName`) para facilitar debugging.
* Combine com `DependencyManager` quando precisar injetar serviços nos componentes recém-criados.
* Mantenha DOTween inicializado (`DOTween.Init()`) no bootstrap do projeto para evitar alocações inesperadas durante o primeiro tween.

Estas extensões foram criadas para manter o código limpo e reutilizável, seguindo o princípio **Open/Closed** — adicione novos métodos sem modificar os existentes.
