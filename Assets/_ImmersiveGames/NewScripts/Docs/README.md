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

- `Canon/Canon-Index.md` - estado canonico consolidado: eixo principal canon-only e excecoes remanescentes.
- `Plans/Plan-Continuous.md` - trilho continuo de planejamento e status de fechamento do eixo principal.
- `Reports/Audits/LATEST.md` - auditoria estatica canonica mais recente do estado pos-H1..H7.
- `Reports/Evidence/LATEST.md` - baseline/evidencia canonica vigente + leitura do estado atual.
- `ADRs/README.md` - indice das decisoes arquiteturais aceitas e ativas.
- `CHANGELOG.md` - registro de alteracoes documentais.

## Onde fica cada coisa

- ADRs: `ADRs/README.md` e arquivos `ADRs/ADR-*.md`.
- Reports, audits e evidence: `Reports/Audits/`, `Reports/Evidence/` e `Reports/Baseline/`.
- Contratos e politicas de producao: `Standards/Standards.md`.
- Material de apoio live, mas fora da trilha principal: `Overview/Overview.md`, `Guides.md`, `Modules/`, `Shared/` e `Plans/README.md`.
- Historico e snapshots arquivados: pastas datadas em `Reports/Audits/`, `Reports/Evidence/`, `Reports/Baseline/` e `Reports/Audits/2026-03-06/Archive/`.

## Estado atual (oficial)

- O eixo principal de `NewScripts` esta canon-only em `LevelFlow`, `LevelDefinition`, `Navigation`, `WorldLifecycle V2` e tooling/editor/QA associado.
- O runtime principal de start gameplay nao depende mais principalmente de string hardcoded `to-gameplay`; a resolucao canonica usa catalogo/slot core de Navigation.
- Ainda nao se considera `NewScripts/**` 100% canon-only em sentido absoluto: permanece uma excecao localizada em `Gameplay RunRearm` (fallback legado de actor-kind/string) e um residuo menor editor/serializado em `GameNavigationIntentCatalogAsset`.

## Regra operacional

- **CODEX e usado apenas para auditorias** (varredura/diagnostico). Veja `Standards/Standards.md#politica-de-uso-do-codex`.
- Implementacoes e correcoes devem referenciar ADR(s) + politica Strict/Release + contrato de observabilidade + evidencia datada.
- Em caso de duvida entre promover e preservar historico, prefira manter o arquivo acessivel fora da navegacao principal.
