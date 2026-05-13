# ADR-0009 - SessionModeProfile e Session Mode Resolution

## Status
- Estado: Proposed
- Data: 2026-05-12
- Tipo: Direction / Canonical architecture
- Fonte de verdade canônica deste contrato: este ADR, após aceite.

---

## Contexto

A Base 1.1 precisa separar claramente:

- decisão de **modo de sessão** (quem participa e sob quais políticas);
- decisão de **preparação de actors/players** (como o set resolvido será preparado);
- execução técnica de input/runtime Unity.

Sem esse corte, decisões de participação acabam diluídas entre rota, input runtime e preparação de actors, gerando drift de ownership.

---

## Decisão

Adota-se `SessionModeProfile` como contrato canônico para declarar o modo esperado da sessão.

Regra central:

```text
OperationalRouteAsset pode declarar defaults de modo.
SessionOperationalPipeline resolve o modo concreto da sessão.
OperationalRouteAsset não é owner definitivo do modo concreto.
```

O modo concreto resolvido guia políticas de participação e readiness do ciclo operacional antes do handoff para activity.

---

## 1. SessionModeProfile

`SessionModeProfile` deve declarar, no mínimo:

- tipo macro de sessão: `SinglePlayer`, `LocalMultiplayer`, `OnlineMultiplayer`;
- forma de interação: `Solo`, `Coop`, `Versus`, `PvP`, `PvE`;
- limites de participação:
  - `minPlayers`;
  - `maxPlayers`;
- política de preenchimento:
  - `allowBots`;
- necessidade de seleção prévia:
  - `requiresPlayerSelection`;
- política futura de entrada:
  - `joinPolicy` (futuro, contrato explícito).

`SessionModeProfile` não executa side-effects; ele declara política/capacidade esperada.

---

## 2. Responsabilidades

### 2.1 OperationalRouteAsset

- Pode carregar configuração default de `SessionModeProfile` para casos simples/sandbox.
- Não decide sozinho o modo concreto final quando houver fontes adicionais válidas.
- Não deve ser tratado como owner definitivo da participação concreta.

### 2.2 SessionOperationalPipeline

- Resolve o modo concreto da sessão (`ResolvedSessionMode`) em tempo operacional.
- Aplica precedência de fontes canônicas definidas pelo contrato.
- Falha explicitamente quando precondições obrigatórias de modo não forem atendidas.
- Publica resultado resolvido para consumo por estágios/capabilities subsequentes.

---

## 3. Invariantes

- `SessionModeProfile` é contrato de política, não executor.
- `SessionOperationalPipeline` é owner da resolução concreta de modo.
- Resolução de modo não pode depender de inferência implícita por cena/nome de objeto.
- Não usar fallback silencioso para mascarar ausência de configuração obrigatória.
- Input/runtime Unity não define sozinho semântica de modo de sessão.

---

## 4. Relação com Actor Preparation

Este ADR **não** define a preparação de actors.

A preparação de actors, participação em activity, integração com `PlayerInput`/`PlayerInputManager` e política de input legado/teste ficam no ADR dependente:

- `ADR-0010 - Actor Preparation Flow, Player Participation e Unity PlayerInput`.

---

## 5. Consequências

- O domínio ganha uma fronteira explícita para decidir modo de sessão antes de detalhes de materialização/input.
- Reduz acoplamento entre rota, preparação de actor e runtime Unity input.
- Permite evoluir join policy sem transferir ownership para adapters Unity.

---

## 6. Roadmap Futuro

- Formalizar `ResolvedSessionMode` como resultado explícito de resolução operacional.
- Definir matriz de precedência entre defaults de rota, seleção de players e políticas de sessão.
- Evoluir `joinPolicy` de contrato futuro para contrato operacional ativo.
- Integrar consumo de `ResolvedSessionMode` no fluxo de participação/preparação definido no ADR-0010.
