# ADR-0019 — Promoção/fechamento do Baseline 2.2 (ContentSwap + LevelManager)

## Status
- Estado: Proposto
- Data: 2026-01-18
- Escopo: Promoção Baseline 2.2 (Docs/Reports/Evidence)

## Contexto

O Baseline 2.2 consolida a mudança de semântica **Phase => ContentSwap** e introduz o **LevelManager** como orquestrador da progressão de níveis, garantindo que IntroStage seja executada sempre após mudança de nível.

Para evitar regressões silenciosas, a promoção do Baseline 2.2 deve ser baseada em **gates verificáveis** e evidência canônica.

## Decisão

A promoção do Baseline 2.2 ocorre quando **todos os gates abaixo estiverem PASS**, com QA objetivo e evidência em snapshot datado.

### G-01 — ContentSwap formalizado (executor)
**Critério verificável**
- ADR-0018 atualizado sem conflito com ADR-0017.
- Logs canônicos de ContentSwap mantidos com alias `[OBS][ContentSwap]`.

**QA mínimo (ContextMenu)**
- `QA/ContentSwap/G01 - InPlace (NoVisuals)`
- `QA/ContentSwap/G02 - WithTransition (SingleClick)`

**Evidência**
- Snapshot datado com logs + verificação curada dos dois modos.

### G-02 — LevelManager mínimo funcional (sempre roda IntroStage)
**Critério verificável**
- Mudança de nível aciona ContentSwap + IntroStage, de acordo com política default.
- IntroStage executa exatamente uma vez por mudança de nível.

**QA mínimo (ContextMenu)**
- `QA/Levels/L01-GoToLevel (InPlace + IntroStage)`
- `QA/Levels/L02-GoToLevel (WithTransition + IntroStage)`

**Evidência**
- Snapshot datado com logs mostrando ContentSwap + IntroStage no mesmo ciclo.

### G-03 — Configuração centralizada (assets/definitions)
**Critério verificável**
- Configuração de níveis e conteúdo migra de scripts para assets/definitions.
- Nenhum script de runtime mantém “hardcode” de lista de níveis.

**QA mínimo (ContextMenu)**
- `QA/Levels/Resolve/Definitions` (apenas logs de catálogo/resolução).

**Evidência**
- Logs mostrando resolução por catálogo + assinatura de conteúdo.

### G-04 — QA + Evidências + Gate de promoção
**Critério verificável**
- Evidências consolidadas em snapshot datado (`Docs/Reports/Evidence/<YYYY-MM-DD>/`).
- `Docs/Reports/Evidence/LATEST.md` apontando para o snapshot.
- `Docs/CHANGELOG-docs.md` atualizado com os gates fechados.

## Consequências

### Benefícios
- Promoção baseada em critérios objetivos e verificáveis.
- Evidência rastreável e alinhada ao contrato de observability.

### Trade-offs / Riscos
- Exige disciplina de QA e evidência para avançar o baseline.

## Evidências
- Metodologia: `Docs/Reports/Evidence/README.md`
- Ponte canônica: `Docs/Reports/Evidence/LATEST.md`

## Referências
- ADR-0018 — Mudança de semântica: Phase => ContentSwap + introdução do LevelManager
- ADR-0017 — Tipos de troca de fase (In-Place vs SceneTransition)
- Plano 2.2 — Execução (plano2.2.md)
