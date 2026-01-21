# ADR-0019 — Promoção do Baseline 2.2 (ContentSwap + Level/Phase Manager + Config)

## Status
- Estado: Proposto
- Data: 2026-01-18
- Escopo: Promoção Baseline 2.2 (Docs/Reports/Evidence)

## Contexto

O Baseline 2.2 consolida a mudança de semântica **Phase => ContentSwap** (ADR-0018) e introduz um **Level/Phase Manager** configurável para progressão de níveis. Além disso, o baseline precisa centralizar a configuração de gameplay (cenas, níveis, spawns, conteúdo e transições) para reduzir hardcode e garantir consistência de evidências.

Para evitar regressões silenciosas, a promoção deve ser baseada em **gates verificáveis** e evidência canônica, respeitando o contrato de observability.

## Escopo do Baseline 2.2

### Entra
- Semântica oficial: **Phase == ContentSwap** (executor técnico).
- **Level/Phase Manager** como orquestrador de progressão (usa ContentSwap + IntroStage).
- Centralização/configuração de gameplay (ex.: cenas, níveis, spawns, conteúdo, transições).
- QA mínimo com ContextMenus e logs canônicos para ContentSwap e Level.

### Fica fora (neste ciclo)
- Refactor total de nomenclatura nos códigos existentes.
- Tornar IntroStage opcional (ficará configurável no futuro, mas **sempre executa** neste ciclo).
- Substituição completa de assets/definitions antigos (apenas migração necessária para cumprir o baseline).

## Decisão

A promoção do Baseline 2.2 ocorre quando **todos os gates abaixo estiverem PASS**, com QA objetivo e evidência em snapshot datado.

### G-01 — Semântica e contrato de ContentSwap
**Critério verificável**
- ADR-0018 atualizado e coerente com ADR-0017.
- Observability mantém o contrato de ContentSwap (logs e reasons canônicos).

**QA mínimo (ContextMenu)**
- `QA/ContentSwap/G01 - InPlace (NoVisuals)`
- `QA/ContentSwap/G02 - WithTransition (SingleClick)`

**Evidência**
- Snapshot datado com logs demonstrando os dois modos.

### G-02 — Level/Phase Manager integrado
**Critério verificável**
- Mudança de nível aciona ContentSwap + IntroStage no mesmo ciclo.
- IntroStage ocorre **uma vez por mudança de nível** (política default).

**QA mínimo (ContextMenu)**
- `QA/Levels/L01-GoToLevel (InPlace + IntroStage)`
- `QA/Levels/L02-GoToLevel (WithTransition + IntroStage)`

**Evidência**
- Snapshot datado com logs mostrando ContentSwap + IntroStage no mesmo fluxo de Level.

### G-03 — Configuração centralizada
**Critério verificável**
- Configuração de gameplay (cenas, níveis, spawns, conteúdo, transições) migrada para assets/definitions.
- Nenhum script de runtime mantém listas hardcoded de níveis/cenas.

**QA mínimo (ContextMenu)**
- `QA/Levels/Resolve/Definitions` (apenas logs de catálogo/resolução).

**Evidência**
- Logs mostrando resolução por catálogo + assinatura de conteúdo.

### G-04 — QA + Evidências + Gate final
**Critério verificável**
- Evidências consolidadas em snapshot datado (`Docs/Reports/Evidence/<YYYY-MM-DD>/`).
- `Docs/Reports/Evidence/LATEST.md` apontando para o snapshot do baseline fechado.
- `Docs/CHANGELOG-docs.md` atualizado com gates fechados.

## Metodologia de evidência (por data)

- **ADR aberto**: referencia `Docs/Reports/Evidence/LATEST.md`.
- **ADR concluído**: deve referenciar evidência com data **<= data de conclusão** do ADR.

## Consequências

### Benefícios
- Promoção baseada em critérios objetivos e verificáveis.
- Evidência rastreável e alinhada ao contrato de observability.

### Trade-offs / Riscos
- Exige disciplina de QA e curadoria de evidências para avançar o baseline.

## Evidências
- Metodologia: `Docs/Reports/Evidence/README.md`
- Ponte canônica (ADR aberto): `Docs/Reports/Evidence/LATEST.md`

## Referências
- ADR-0018 — Mudança de semântica: Phase => ContentSwap + Level/Phase Manager
- ADR-0017 — Tipos de troca de fase (ContentSwap: In-Place vs SceneTransition)
- Plano 2.2 — Execução (plano2.2.md)
