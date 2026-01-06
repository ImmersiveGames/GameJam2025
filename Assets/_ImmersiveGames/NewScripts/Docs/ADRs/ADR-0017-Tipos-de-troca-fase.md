# ADR-0017 — Tipos de troca de fase: In-Place Reset vs Scene Transition

## Status
**Aceito / Ativo**

## Data
2026-01-06

## Contexto

No NewScripts, “trocar de fase” foi identificado como um requisito que cobre **dois comportamentos distintos**:

1. Troca de fase durante o gameplay **sem troca de cenas** (mesma cena/escopo), geralmente para progressão (fase 1 → fase 2).
2. Troca de fase que exige **transição formal de cenas** (load/unload + fade + loading), típica de navegação e mudanças estruturais.

Sem uma taxonomia explícita, o termo “nova fase” vira ambíguo e mistura:
- reset in-place,
- transição de cena,
- efeitos visuais (curtain/fade),
- e até “PreGame”.

Este ADR define **nomes oficiais** e o contrato semântico para cada tipo.

> Referência de contrato de Phase/Pontos seguros: **ADR-0016**.

---

## Decisão

### A) PhaseChange.InPlaceReset

**Definição**
Mudança de fase aplicada no mesmo conjunto de cenas (sem SceneFlow). O mundo é reconstruído “no lugar” via reset/spawn, sem load/unload de cenas.

**Contrato**
- Não envolve `SceneTransitionService` (nenhum `SceneTransitionContext` é criado).
- Pode usar efeito visual local (curtain/fade) se desejado, mas isso é UX e não implica SceneFlow.
- O reset/spawn ocorre no mesmo scene scope.
- A fase efetiva deve ser aplicada em ponto seguro pós-reset (alinhado ao contrato do PhaseContext no ADR-0016).

**Uso típico**
- Progressão contínua dentro do gameplay, mantendo a mesma cena.

---

### B) PhaseChange.SceneTransition

**Definição**
Mudança de fase acoplada a uma transição formal de cenas via SceneFlow (load/unload + fade + loading). O reset do mundo ocorre após `ScenesReady`, correlacionado por `ContextSignature`.

**Contrato**
- Executada via SceneFlow, com `SceneTransitionContext.ContextSignature` como chave canônica.
- Deve preservar invariantes:
    - `SceneTransitionStarted` fecha `flow.scene_transition`.
    - `ScenesReady` antes de `Completed`.
    - `WorldLifecycleResetCompletedEvent(contextSignature, reason)` correlacionado ocorre antes de `Completed` (completion gate).
- A fase (se staged em Pending) só é efetivamente aplicada em ponto seguro compatível (ADR-0016), tipicamente após o reset correlacionado.

**Uso típico**
- Menu → Gameplay, Gameplay → Menu, mudança de capítulo/bioma/mapa que depende de cenas diferentes.

---

## PreGame / PreReveal (clarificação)

**Decisão**
PreGame/PreReveal é **opcional** e **não é Phase**.

- Pode existir como etapa de UX (ex.: “Fase 2”, cutscene, splash) antes do gameplay, mas:
    - não pode bloquear o fluxo indefinidamente,
    - não deve ser requisito para entrar em `Playing` quando não aplicável.

---

## Nomenclatura padronizada (obrigatória)

- **InPlaceReset**: `PhaseChange.InPlaceReset`
- **SceneTransition**: `PhaseChange.SceneTransition`

Esses nomes devem ser usados em logs, razões (`reason`) e documentação, para evitar ambiguidade.

---

## Consequências

### Positivas
- Remove ambiguidade: “trocar fase” passa a ter semântica definida.
- Permite UX (fade/curtain) sem forçar load/unload quando não necessário.
- Mantém auditoria determinística por assinatura (`ContextSignature`) no modo SceneTransition.

### Trade-offs
- Requer disciplina de nomenclatura e de logs (`reason`, `contextSignature` quando aplicável).
- Exige que decisões de UX (fade local vs SceneFlow) não vazem para o contrato de Phase.
