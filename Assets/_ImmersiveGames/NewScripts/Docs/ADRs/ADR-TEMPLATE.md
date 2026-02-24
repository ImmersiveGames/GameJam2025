# ADR-XXXX — Title

## Status

- Estado: Aberto | Em andamento | Concluído | Parcial | Cancelado
- Data (decisão): YYYY-MM-DD
- Última atualização: YYYY-MM-DD
- Tipo: Implementação
- Escopo: (módulos/áreas afetadas)
- Decisores: (nomes/roles)
- Tags: (ex.: SceneFlow, WorldLifecycle, Observability)

> Para ADRs de completude/governança, use `ADR-TEMPLATE-COMPLETENESS.md`.

## Contexto

Descreva o problema, restrições (produção/QA) e por que isto é necessário.

## Decisão

### Objetivo de produção (sistema ideal)

Descreva o estado ideal "production complete".

### Contrato de produção (mínimo)

Regras objetivas e verificáveis (ordem de eventos, invariantes, ownership, dependências).

### Não-objetivos (resumo)

O que não está sendo resolvido aqui (detalhe completo em "Fora de escopo").

## Fora de escopo

Lista explícita do que fica fora (para reduzir escopo e evitar promessas implícitas).

## Consequências

### Benefícios

### Custos / Riscos

### Política de falhas e fallback (fail-fast)

Em Unity, preferir falhar cedo para sinalizar bugs de pipeline/config, evitando criação silenciosa em runtime.

### Critérios de pronto (DoD)

Checklist objetiva do que precisa estar verdadeiro para considerar "feito".

## Implementação (arquivos impactados)

> TBD: este ADR não contém caminhos de implementação explicitados no documento atual.

## Notas de implementação (se necessário)

Detalhes de arquitetura, pontos de integração, exemplos de API, observabilidade, etc.

## Evidência

- Última evidência (log bruto): `Docs/Reports/lastlog.log`
- Fonte canônica atual: `Docs/Reports/Evidence/LATEST.md`
- Âncoras/assinaturas relevantes: (strings ou padrões de log)
- Contrato de observabilidade: `Docs/Standards/Standards.md#observability-contract`

## Referências

Links para ADRs relacionados, documentos e contratos (Observability, Evidence, etc).