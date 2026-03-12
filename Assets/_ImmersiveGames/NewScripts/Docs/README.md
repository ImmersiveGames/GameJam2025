# NewScripts - Docs

Este diretorio e o ponto de entrada para a documentacao do modulo **NewScripts**.

## Navegacao principal

Use esta cadeia como trilha canonica de consulta:

1. `README.md`
2. `Canon/Canon-Index.md`
3. `Plans/Plan-Continuous.md`
4. `Reports/Audits/LATEST.md`
5. `Reports/Evidence/LATEST.md`
6. `ADRs/README.md`
7. `CHANGELOG.md`

## Acesso rapido

- `Canon/Canon-Index.md` - estado canonico consolidado e baseline congelada vigente.
- `Plans/Plan-Continuous.md` - trilho continuo de planejamento sem duplicar contrato.
- `Reports/Audits/LATEST.md` - auditoria estatica canonica mais recente.
- `Reports/Evidence/LATEST.md` - baseline/evidencia canonica vigente.
- `ADRs/README.md` - indice das decisoes arquiteturais aceitas e ativas.
- `CHANGELOG.md` - registro de alteracoes documentais.

## Onde fica cada coisa

- ADRs: `ADRs/README.md` e arquivos `ADRs/ADR-*.md`.
- Reports, audits e evidence: `Reports/Audits/`, `Reports/Evidence/` e `Reports/Baseline/`.
- Contratos e politicas de producao: `Standards/Standards.md`.
- Material de apoio live, mas fora da trilha principal: `Overview/Overview.md`, `Guides.md`, `Modules/`, `Shared/` e `Plans/README.md`.
- Historico e snapshots arquivados: pastas datadas em `Reports/Audits/`, `Reports/Evidence/`, `Reports/Baseline/` e `Reports/Audits/2026-03-06/Archive/`.

## Regra operacional

- **CODEX e usado apenas para auditorias** (varredura/diagnostico). Veja `Standards/Standards.md#politica-de-uso-do-codex`.
- Implementacoes e correcoes devem referenciar ADR(s) + politica Strict/Release + contrato de observabilidade + evidencia datada.
- Em caso de duvida entre promover e preservar historico, prefira manter o arquivo acessivel fora da navegacao principal.
