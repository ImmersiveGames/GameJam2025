# 🧠 Sistema de Predicados — Guia de Uso (v1.0)

## 📚 Índice
1. [Visão Geral](#visão-geral)
2. [Contratos e Implementações](#contratos-e-implementações)
3. [Composição Funcional](#composição-funcional)
4. [Integração com Gameplay](#integração-com-gameplay)
5. [Validações e Erros](#validações-e-erros)
6. [Boas Práticas](#boas-práticas)

---

## 🎯 Visão Geral

O **PredicateSystem** oferece um DSL leve para regras booleanas reutilizáveis. Ideal para gates de habilidade, validações de UI e lógica de AI, mantendo o código alinhado a SOLID e permitindo testes unitários simples.

---

## 📄 Contratos e Implementações

### `IPredicate`
* Contrato mínimo com `bool Evaluate()`.
* Pode ser implementado por qualquer classe que represente uma condição.

### Predicados Compostos
* `And` — Recebe uma lista de `IPredicate` e retorna verdadeiro se todos forem verdadeiros.
* `Or` — Retorna verdadeiro se pelo menos um predicado for verdadeiro.
* `Not` — Inverte o resultado do predicado encapsulado.

Todos os construtores usam `Preconditions.CheckNotNull` e validam que haja pelo menos um predicado (para `And`/`Or`).

### `Preconditions`
* `CheckNotNull` — Lança `ArgumentNullException` com mensagem customizada.
* `CheckState` — Lança `InvalidOperationException` quando a expressão é falsa.

---

## 🧩 Composição Funcional

Além dos construtores diretos, utilize as extensões para compor regras fluentemente:

```csharp
IPredicate canShoot = new HasAmmoPredicate()
    .And(new IsNotReloadingPredicate())
    .And(new HasAuthorityPredicate(localPlayerId));

IPredicate shouldDisplayWarning = canShoot.Not().Or(new IsOutOfBoundsPredicate());
```

As extensões `And`, `Or`, `Not` retornam novas instâncias, mantendo a imutabilidade das regras originais.

---

## 🎮 Integração com Gameplay

* **Habilidades** — Verifique condições antes de enviar comandos para o `DamageSystem`.
* **UI Dinâmica** — Mostre tooltips ou disable buttons com base em `predicate.Evaluate()`.
* **AI Local Multiplayer** — Combine condições de proximidade, cooldown e estado do alvo.

Recomendação: injete dependências necessárias dentro dos predicados concretos (ex.: `ResourceSystem`, `EventBus`) para manter coesão.

---

## 🧪 Validações e Erros

| Situação | Resultado | Ação |
| --- | --- | --- |
| Criar `And()` sem argumentos | `ArgumentException` | Garanta pelo menos um predicado na composição |
| Passar predicado nulo | `ArgumentNullException` via `Preconditions` | Construa predicados concretos antes de compor |
| Predicado com dependência nula | `InvalidOperationException` via `CheckState` (quando usado) | Configure dependências via DI ou construtores |

---

## ✅ Boas Práticas

* Mantenha predicados **puros**: evite efeitos colaterais em `Evaluate()`.
* Para debug, encapsule predicados com logs usando `DebugUtility.LogVerbose` quando necessário.
* Organize predicados concretos em namespaces específicos do sistema (ex.: `DamageSystem.Predicates`).
* Reaproveite instâncias imutáveis quando possível para evitar alocações por frame.

Esse módulo simplifica a composição de regras complexas, respeitando OCP e LSP ao permitir que novos predicados se encaixem sem alterar a infraestrutura existente.
