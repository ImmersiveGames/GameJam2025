# ADR-0019 — Promoção do Baseline 2.2 (ContentSwap + LevelManager + Config)

## Status

- Estado: Proposto
- Data: 2026-01-18
- Escopo: Promoção Baseline 2.2 (Docs/Reports/Evidence)

## Contexto

O Baseline 2.2 consolida a mudança de semântica para **ContentSwap + LevelManager** (ADR-0018) e introduz um **LevelManager** configurável para progressão de níveis. Além disso, o baseline precisa centralizar a configuração de gameplay (cenas, níveis, spawns, conteúdo e transições) para reduzir hardcode e garantir consistência de evidências.

Para evitar regressões silenciosas, a promoção deve ser baseada em **gates verificáveis** e evidência canônica, respeitando o contrato de observability.

## Escopo do Baseline 2.2

### Entra
- Semântica oficial: **ContentSwap** como executor técnico.
- **LevelManager** como orquestrador de progressão (usa ContentSwap + IntroStage).
- Centralização/configuração de gameplay (ex.: cenas, níveis, spawns, conteúdo, transições).
- QA mínimo com ContextMenus e logs canônicos para ContentSwap e Level.

### Fica fora (neste ciclo)
- Refactor total de nomenclatura nos códigos existentes.
- Tornar IntroStage opcional (ficará configurável no futuro, mas **sempre executa** neste ciclo).
- Substituição completa de assets/definitions antigos (apenas migração necessária para cumprir o baseline).

## Decisão

### Objetivo de produção (sistema ideal)

Formalizar o ato de 'promover' Baseline 2.2 (congelar contrato + evidência canônica) e como evoluí-lo sem quebrar a rastreabilidade (datas, changelog, regressões).

### Contrato de produção (mínimo)

- Promoção gera/atualiza uma evidência datada e atualiza `LATEST.md`.
- Changelog registra o que foi promovido e por quê, com link para evidência.
- Se a promoção falhar, não atualizar `LATEST.md` (evitar falso positivo).

### Não-objetivos (resumo)

Ver seção **Fora de escopo**.

### G-01 — Semântica e contrato de ContentSwap
**Critério verificável**
- ADR-0018 atualizado e coerente com ADR-0016.
- Observability mantém o contrato de ContentSwap InPlace-only (logs e reasons canônicos).

**QA mínimo (ContextMenu)**
- `QA/ContentSwap/G01 - InPlace (NoVisuals)`

**Evidência**
- Snapshot datado com logs demonstrando o modo InPlace.

### G-02 — LevelManager integrado
**Critério verificável**
- Mudança de nível aciona ContentSwap + IntroStage no mesmo ciclo.
- IntroStage ocorre **uma vez por mudança de nível** (política default).

**QA mínimo (ContextMenu)**
- `QA/Levels/L01-GoToLevel (InPlace + IntroStage)`

**Evidência**
- Snapshot datado com logs mostrando ContentSwap + IntroStage no fluxo de Level.

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

## Fora de escopo

- Resolver todas as causas de regressão; apenas definir processo/contrato de promoção.

## Metodologia de evidência (por data)

- **ADR aberto**: referencia `Docs/Reports/Evidence/LATEST.md`.
- **ADR concluído**: deve referenciar evidência com data **<= data de conclusão** do ADR.

## Consequências

### Benefícios
- Promoção baseada em critérios objetivos e verificáveis.
- Evidência rastreável e alinhada ao contrato de observability.

### Trade-offs / Riscos
- Exige disciplina de QA e curadoria de evidências para avançar o baseline.

### Política de falhas e fallback (fail-fast)

- Em Unity, ausência de referências/configs críticas deve **falhar cedo** (erro claro) para evitar estados inválidos.
- Evitar "auto-criação em voo" (instanciar prefabs/serviços silenciosamente) em produção.
- Exceções: apenas quando houver **config explícita** de modo degradado (ex.: HUD desabilitado) e com log âncora indicando modo degradado.


### Critérios de pronto (DoD)

- Existe um snapshot de evidência datado + LATEST atualizado para Baseline 2.2.

## Evidência

- **Fonte canônica atual:** [`LATEST.md`](../Reports/Evidence/LATEST.md)
- **Âncoras/assinaturas relevantes:**
  - Ver evidência Baseline 2.2 em `Docs/Reports/Evidence/LATEST.md`.
- **Contrato de observabilidade:** [`Observability-Contract.md`](../Standards/Observability-Contract.md)

## Evidências

- Metodologia: `Docs/Reports/Evidence/README.md`
- Ponte canônica (ADR aberto): `Docs/Reports/Evidence/LATEST.md`

## Referências

- ADR-0018 — Mudança de semântica: ContentSwap + LevelManager
- ADR-0016 — ContentSwap InPlace-only
- Plano 2.2 — Execução (plano2.2.md)
- [`Observability-Contract.md`](../Standards/Observability-Contract.md)
- [`Evidence/LATEST.md`](../Reports/Evidence/LATEST.md)
