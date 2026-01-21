# Plano 2.2 — Execução (Baseline 2.2)

> Este plano foca **execução e evidência**. A semântica e os contratos estão em ADR-0018 (ContentSwap + LevelManager) e ADR-0019 (Promoção Baseline 2.2).

## Pré-condições
- Baseline 2.1 fechado via snapshot datado e `Docs/Reports/Evidence/LATEST.md` válido.
- ADR-0017 já implementado (modos canônicos de ContentSwap).

## Meta
- Evoluir para Baseline 2.2 com critérios objetivos, sem regressões em observability e pipeline.

---

## Linha 0 — Sequência de marcos (dependências)
1. **ADR-0017** (modos de ContentSwap) — já implementado.
2. **ADR-0018** (semântica: Phase => ContentSwap + LevelManager).
3. **ADR-0019** (promoção: config centralizada + gates + evidências).
4. **Execução Baseline 2.2** (QA + evidências + gate final).

---

## Linha A — Documentação e semântica (ADR-0018)

**Objetivo**
- Formalizar ContentSwap como executor técnico e separar LevelManager (progressão).

**Entregáveis (docs)**
- ADR-0018 atualizado com termos formais, boundaries e relação com ADR-0017.
- Terminologia consistente em docs de topo (ARCHITECTURE/README).

**Critérios de aceite**
- Não há uso ambíguo de “Phase” como nível sem explicação.
- ADR-0018 referenciado por ADR-0019 e pelo plano.

---

## Linha B — Promoção Baseline 2.2 (ADR-0019)

**Objetivo**
- Definir o baseline com configuração centralizada + LevelManager.

**Entregáveis (docs/roadmap)**
- ADR-0019 com escopo, gates verificáveis e metodologia de evidência por data.

**Critérios de aceite**
- Gates de promoção descritos com QA mínimo e logs/contrato.
- Evidência de ADR aberto aponta para `Evidence/LATEST`.

---

## Linha C — Arquitetura/configuração (baseline 2.2)

**Objetivo**
- Centralizar configuração (cenas, níveis, spawns, conteúdo, transições).

**Entregáveis (arquitetura/config)**
- Catálogo + resolver de definitions (assets) para níveis e conteúdo.
- Remoção de hardcode de listas de níveis/cenas em scripts de runtime.

**Critérios de aceite**
- Logs mostrando resolução por catálogo + assinatura de conteúdo.
- QA mínimo `QA/Levels/Resolve/Definitions` produzido.

---

## Linha D — Execução de QA e evidências (baseline 2.2)

**Objetivo**
- Consolidar evidências e fechar gates de promoção.

**Entregáveis**
- Snapshot datado em `Docs/Reports/Evidence/<YYYY-MM-DD>/`.
- `Docs/Reports/Evidence/LATEST.md` apontando para o snapshot.
- Atualização do `Docs/CHANGELOG-docs.md` com gates fechados.

**QA mínimo (ContextMenu)**
- ContentSwap:
  - `QA/ContentSwap/G01 - InPlace (NoVisuals)`
  - `QA/ContentSwap/G02 - WithTransition (SingleClick)`
- LevelManager:
  - `QA/Levels/L01-GoToLevel (InPlace + IntroStage)`
  - `QA/Levels/L02-GoToLevel (WithTransition + IntroStage)`
- Configuração:
  - `QA/Levels/Resolve/Definitions`

**Gate de promoção**
- Gates do ADR-0019 em PASS.
