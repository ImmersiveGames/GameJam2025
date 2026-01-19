# Plano 2.2 — Execução (Baseline 2.2)

> Este plano foca **execução e evidência**. A semântica e os contratos estão em ADR-0018 (ContentSwap) e ADR-0019 (Level Manager).

## Pré-condição
- Baseline 2.1 fechado via snapshot datado e `Docs/Reports/Evidence/LATEST.md` válido.

## Meta
- Evoluir para Baseline 2.2 com critérios objetivos, sem regressões em observability e pipeline.

---

## Linha A — Formalizar ContentSwap (semântica + observability)

**Objetivo**
- Separar “Phase” (ContentSwap) de “Level/Nível” e consolidar o contrato de logs/reasons.

**Entregas**
- ADR-0018 reescrito com semântica ContentSwap + contratos públicos + reasons canônicos.
- Referências atualizadas em índices/READMEs.

**QA mínimo (ContextMenu)**
- `QA/ContentSwap/InPlace/Commit (NoVisuals)`
- `QA/ContentSwap/WithTransition/Commit (Gameplay Minimal)`

**Evidência**
- Snapshot datado com logs + verificação curada dos dois modos (In-Place e WithTransition).

---

## Linha B — Implementar Level Manager (mínimo funcional)

**Objetivo**
- Orquestrar progressão de níveis, acionando ContentSwap + IntroStage.

**Entregas**
- Level Manager mínimo funcional (sem quebrar APIs atuais).
- Política default: toda mudança de nível executa IntroStage.

**QA mínimo (ContextMenu)**
- `QA/Level/Advance/IntroStage (Default)`

**Evidência**
- Logs mostrando ContentSwap + IntroStage no mesmo ciclo de mudança de nível.

---

## Linha C — Centralizar configuração (assets/definitions)

**Objetivo**
- Retirar configuração de nível/conteúdo de scripts e mover para assets/definitions.

**Entregas**
- Catálogo + resolver de definitions (assets) para níveis e conteúdo.
- Remoção de hardcode de lista de níveis em scripts de runtime.

**QA mínimo (ContextMenu)**
- `QA/Level/Resolve/Definitions`

**Evidência**
- Logs com resolução por catálogo + assinatura de conteúdo.

---

## Linha D — QA + Evidências + Gate de promoção (Baseline 2.2)

**Objetivo**
- Consolidar evidências e fechar gates de promoção.

**Entregas**
- Snapshot datado em `Docs/Reports/Evidence/<YYYY-MM-DD>/`.
- `Docs/Reports/Evidence/LATEST.md` apontando para o snapshot.
- Atualização do `Docs/CHANGELOG-docs.md` com gates fechados.

**QA mínimo (ContextMenu)**
- Reutilizar os ContextMenus das linhas A–C (apenas os necessários para evidência).

**Gate de promoção**
- Gates do ADR-0019 em PASS.
