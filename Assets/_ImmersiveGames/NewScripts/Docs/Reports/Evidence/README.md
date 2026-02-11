# Evidence (Reports)

Este diretório guarda snapshots de evidência (logs + interpretação) usados para validar o baseline.

## Como usar

1. Leia o ponteiro em [`LATEST.md`](LATEST.md).
2. Abra o arquivo de evidência do snapshot (ex.: `Baseline-2.2-Evidence-2026-01-31.md`).
3. Valide as **assinaturas-chave** e **invariantes** indicadas no documento.
4. Use o `*.log` correspondente como fonte bruta quando houver dúvida.
5. Para evidência rápida, use `Docs/Reports/lastlog.log`.

## Convenções

- **Snapshot:** pasta `YYYY-MM-DD/` contendo:
  - `Baseline-<versão>-Evidence-YYYY-MM-DD.md`
  - `Baseline-<versão>-Smoke-LastRun.log`
- **Omissões de log:** usar `[...trecho omitido...]` quando necessário (evitar “...” solto).
- **Promoção:** ao promover um snapshot para LATEST, atualizar `LATEST.md` e registrar o motivo em um ADR (quando aplicável).


## Política de retenção

- Manter **LATEST** + **as 2 evidências anteriores** em `Reports/Evidence/`.
- Manter **as 3 auditorias mais recentes** em `Reports/Audits/`.
- Material antigo deve ser removido (o histórico segue no Git).
