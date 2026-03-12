# DOCS HYGIENE PHASE 2 - 2026-03-11

## Escopo auditado

- `Docs/README.md`
- `Docs/CHANGELOG.md`
- `Docs/Canon/Canon-Index.md`
- `Docs/Plans/Plan-Continuous.md`
- `Docs/Reports/Audits/LATEST.md`
- `Docs/Reports/Evidence/LATEST.md`
- `Docs/ADRs/README.md`
- arvore real sob `Docs/**`, com foco em navegacao principal vs historico preservado

## Criterios usados

- **Manter live e facilmente navegavel:** documentos canonicos atuais, ponteiros `LATEST`, indice de ADRs, changelog e baseline freeze vigente.
- **Despromover da navegacao principal:** documentos de apoio, guias operacionais, overview consolidado, readmes secundarios e snapshots datados que continuam validos, mas nao sao a trilha principal.
- **Arquivar/mover:** somente se houvesse duplicata acidental obvia ou se a navegacao nao pudesse ser limpa sem movimento. Nesta fase isso nao foi necessario.

## Arquivos alterados

- `Docs/README.md`
- `Docs/CHANGELOG.md`
- `Docs/Reports/Baseline/2026-03-06/Baseline-3.1-Freeze.md`
- `Docs/Reports/Audits/2026-03-11/DOCS-HYGIENE-PHASE-2.md`

## Arquivos movidos ou arquivados

- Nenhum.

## Redundancias eliminadas da navegacao principal

- `Docs/Overview/Overview.md` deixou de aparecer como entrada de primeira camada.
- `Docs/Guides.md` deixou de aparecer como entrada de primeira camada.
- `Docs/Plans/README.md` deixou de aparecer como trilha principal.
- Arvores auxiliares `Docs/Modules/`, `Docs/Shared/` e snapshots datados em `Docs/Reports/**` permaneceram acessiveis, mas despromovidos para contexto/historico.

## Pendencias restantes

- `Docs/CHANGELOG.md` ainda preserva historico legado com ordem cronologica irregular em trechos antigos; mantido assim para preservar rastreabilidade.
- Alguns arquivos auxiliares live continuam densos e com encoding historico heterogeneo, mas isso nao bloqueia a navegacao principal.
- Se houver uma fase futura de retencao, ela pode revisar arquivos auxiliares que hoje estao corretos como historico/live de apoio, sem tocar na cadeia canonica.
