# Evidências — metodologia e estrutura

Este diretório contém **evidências arquivadas** (snapshots) usadas para:

- Sustentar ADRs com prova observável.
- Detectar regressões ao longo do tempo.
- Reduzir ruído em `Docs/Reports` mantendo um conjunto mínimo e organizado de artefatos.

## Princípios

1. **Log como evidência primária**
   - A evidência primária é o **log bruto** produzido por uma execução real.
   - Relatórios derivados (.md) devem ser curtos e apontar para o log.

2. **Contrato como fonte de tokens canônicos**
   - `Docs/Reports/Observability-Contract.md` define o contrato de evidências (tokens/strings).
   - O verificador deve validar o log contra o contrato, mas o log permanece a fonte de verdade.

3. **Snapshot datado e imutável**
   - Um snapshot é identificado por data (`YYYY-MM-DD`) e não deve ser reescrito.
   - Cada snapshot representa o estado do sistema em um momento, e serve como baseline para regressão.

## Estrutura

- `Evidence/YYYY-MM-DD/`
  - `Baseline-2.1-Evidence-YYYY-MM-DD.md` (resumo canônico do snapshot)
  - artefatos mínimos usados pelo resumo (ex.: log e/ou relatório de verificação), quando necessário.

- `Evidence/LATEST.md`
  - Aponta para o snapshot mais recente considerado canônico.

## Regras para ADR

- **ADR em status “Aceito”**
  - Deve referenciar pelo menos **um** snapshot datado (`Evidence/YYYY-MM-DD/...`).
  - Deve também referenciar `Evidence/LATEST.md` como ponte para a verificação de regressão contínua.

- **ADR em status “Proposto/Em andamento”**
  - Pode referenciar apenas `Evidence/LATEST.md` até o fechamento.

## Como arquivar uma nova evidência (quando um ADR for fechado)

1. Rodar o fluxo/cenário relevante (ex.: Baseline 2.1).
2. Gerar um resumo canônico (arquivo `...-Evidence-YYYY-MM-DD.md`).
3. Copiar o resumo e os artefatos mínimos para `Evidence/YYYY-MM-DD/`.
4. Atualizar `Evidence/LATEST.md` para apontar para a nova data.
5. Atualizar o ADR com os links de evidência.
6. Registrar no `Docs/CHANGELOG-docs.md`.

## Retenção e limpeza

- É aceitável remover relatórios antigos em `Docs/Reports` desde que:
  - `Observability-Contract.md` seja mantido; e
  - `Evidence/` contenha os snapshots datados necessários.
