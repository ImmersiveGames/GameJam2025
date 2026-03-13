# Latest Evidence

Fonte operacional vigente de evidencia runtime: `Docs/Reports/lastlog.log`.

## Leitura vigente

- O log mais recente sustenta o estado atual ja consolidado.
- A leitura operacional parte de:
  - `Docs/Reports/lastlog.log`
  - `Docs/Reports/Audits/LATEST.md`
  - docs modulares e guias oficiais atuais
- A leitura vigente inclui o loading de producao ja validado em runtime:
  - `LoadingHudScene` como HUD canonica do macro flow
  - progresso hibrido por carga real de cena + marcos ponderados
  - HUD com barra, porcentagem, etapa e spinner
- Snapshots antigos de evidence permanecem apenas como historico rastreavel, fora da superficie operacional principal.

Estados anteriores nao devem mais ser usados para interpretar o contrato atual.
