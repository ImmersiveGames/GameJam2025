# ADR-0019 — Promoção do Baseline 2.2 (declaração, snapshot e ponte LATEST)

## Status
- Estado: Proposto
- Data: 2026-01-18
- Escopo: Docs/Reports/Evidence + checklist + declaração operacional do 2.2

## Contexto

O Baseline 2.1 está fechado e evidenciado via snapshot datado. A evolução para o Baseline 2.2 introduz um conjunto de mudanças *config-driven* (WorldCycle) e correções/gates de consistência (Phases/Observability/Docs).

Para evitar regressões silenciosas, precisamos de um procedimento objetivo para:
- declarar “Baseline 2.2 promovido”; e
- manter regressão contínua via `Evidence/LATEST.md`.

## Decisão

A promoção do Baseline 2.2 ocorre quando:

1) **Gates do ADR-0018 estão PASS**
- PASS obrigatório: **G-01, G-02, G-03**.
- **G-04** somente se aplicável ao escopo do 2.2.

2) Existe **snapshot datado** em `Docs/Reports/Evidence/<YYYY-MM-DD>/` contendo:
- log bruto (Console) usado como fonte de verdade;
- verificação curada (anchors) demonstrando:
    - invariantes do pipeline SceneFlow/WorldLifecycle;
    - evidências dos gates (ex.: In-Place sem Fade/HUD, reasons canônicos, docs sem drift);
- (opcional) pacote zip de evidência, se o repositório adotar essa prática.

3) `Docs/Reports/Evidence/LATEST.md` aponta para o snapshot datado mais recente.

4) `Docs/CHANGELOG-docs.md` registra:
- quais gates foram fechados;
- links para o snapshot datado;
- notas de compatibilidade (o que mudou/foi padronizado em reasons/assinaturas).

## Fora de escopo
- Definir o conteúdo do WorldCycle (o feature set do 2.2 é definido pelo plano e ADRs específicos).
- Substituir navegação por data-driven completo.

## Consequências

### Benefícios
- Processo de promoção repetível e auditável.
- Reduz atrito em QA: a “fonte de verdade” fica explícita e versionada.

### Trade-offs / Riscos
- Requer manutenção disciplinada do snapshot e verificação curada.

## Notas de implementação

- Ao concluir o snapshot:
    - atualizar `Checklist-phase.md` (se houver itens fechados);
    - garantir que o contrato (`Observability-Contract.md`) não aponta para artefatos ausentes;
    - manter as âncoras curadas minimalistas (apenas as strings necessárias).

## Evidências
- Metodologia: `Docs/Reports/Evidence/README.md`
- Ponte canônica: `Docs/Reports/Evidence/LATEST.md`

## Referências
- ADR-0018 — Gate de Promoção do Baseline 2.2
- Plano 2.2 — Evolução do WorldLifecycle para WorldCycle Config-Driven
