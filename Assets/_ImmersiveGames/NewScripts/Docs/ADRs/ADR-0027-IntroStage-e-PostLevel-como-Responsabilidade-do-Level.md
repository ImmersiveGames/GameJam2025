# ADR-0027 - IntroStage e PostLevel como Responsabilidade do Level

## Status

- Estado: **Aceito (Implementado)**
- Data (decisao): 2026-02-19
- Ultima atualizacao: 2026-03-12

## Leitura atual deste ADR

O nome do arquivo foi preservado por rastreabilidade historica, mas o contrato vigente ja foi refinado:

- `IntroStage` permanece level-owned e opcional.
- `PostGame` nao e um stage arbitrario de level.
- O pos-run continua global e centralizado.
- O level atual pode apenas complementar o pos-run por hook opcional.
- `Restart` nao passa por post hook.

## Decisao canonica atual

- `IntroStage` e decidido pelo level atual (`LevelDefinitionAsset`) e orquestrado por `LevelStageOrchestrator`.
- Se o level atual nao expuser intro, o fluxo segue direto para gameplay sem erro.
- `PostGame` e global, com exatamente tres resultados formais:
  - `Victory`
  - `Defeat`
  - `Exit`
- O hook opcional do level para pos-run reage a esses resultados sem substituir o fluxo global.
- `Restart` segue pelo trilho de reset/restart e fica fora do hook de post.

## Evidencia

- `Docs/Reports/Audits/2026-03-12/INTRO-LEVEL-AND-POSTGAME-GLOBAL.md`
- `Docs/Reports/Audits/2026-03-12/DOCS-FINAL-CLOSEOUT.md`
- `Docs/Reports/lastlog.log`

## Consequencias

- O dominio de level continua owner apenas da intro opcional e do hook opcional de reacao visual.
- O owner do pos-run permanece global no eixo `GameLoop` + `PostGame`.
- Nao existe `PostStage` generico por level no contrato atual.
- A superficie documental principal deixa de promover `PostLevel` como stage de level.