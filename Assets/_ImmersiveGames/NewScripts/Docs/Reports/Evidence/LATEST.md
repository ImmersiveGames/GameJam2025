# Evidencia canonica (LATEST)

Esta pagina aponta para o conjunto de evidencias mais recente aceito como referencia para auditoria.

## Snapshot atual

- **Data:** 2026-03-06
- **Baseline:** 3.1 Freeze
- **Arquivo de evidencia:** [../Baseline/2026-03-06/Baseline-3.1-Freeze.md](../Baseline/2026-03-06/Baseline-3.1-Freeze.md)
- **Log bruto congelado do baseline:** [../Baseline/2026-03-06/lastlog.log](../Baseline/2026-03-06/lastlog.log)
- **Evidencia local adicional:** [../Baseline/2026-03-06/doisResets-na-sequencia.txt](../Baseline/2026-03-06/doisResets-na-sequencia.txt)
- **Auditoria estatica vigente:** [../Audits/LATEST.md](../Audits/LATEST.md)
- **Log bruto corrente do repositorio:** [../lastlog.log](../lastlog.log)

## Regras

- Quando um snapshot e promovido para LATEST, ele vira fonte de verdade ate nova promocao.
- O baseline 3.1 congelado em 2026-03-06 substitui os ponteiros anteriores para Baseline 2.2 / 2026-02-03 como evidencia canonica vigente.
- Alteracoes de comportamento devem atualizar o snapshot e/ou justificar divergencias via ADR + evidencia.