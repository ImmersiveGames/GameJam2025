# ADR-0027 - IntroStage e PostLevel como Responsabilidade do Level

## Status

- Estado: **Aceito (Implementado)**
- Data (decisão): 2026-02-19
- Última atualização: 2026-03-12

## Leitura atual deste ADR

O nome do arquivo foi preservado por rastreabilidade histórica, mas o contrato vigente já foi refinado:

- `IntroStage` permanece level-owned e opcional.
- `PostGame` não é um stage arbitrário de level.
- O pós-run continua global e centralizado.
- O level atual pode apenas complementar o pós-run por hook opcional.
- `Restart` não passa por post hook.

## Decisão canônica atual

- `IntroStage` é decidido pelo level atual (`LevelDefinitionAsset`) e orquestrado por `LevelStageOrchestrator`.
- Se o level atual não expuser intro, o fluxo segue direto para gameplay sem erro.
- `PostGame` é global, com exatamente três resultados formais:
  - `Victory`
  - `Defeat`
  - `Exit`
- O hook opcional do level para pós-run reage a esses resultados sem substituir o fluxo global.
- `Restart` segue pelo trilho de reset/restart e fica fora do hook de post.

## Evidência

- `Docs/Reports/Audits/LATEST.md`
- `Docs/Reports/Audits/LATEST.md`
- `Docs/Reports/Evidence/LATEST.md`

## Consequências

- O domínio de level continua owner apenas da intro opcional e do hook opcional de reação visual.
- O owner do pós-run permanece global no eixo `GameLoop` + `PostGame`.
- Não existe `PostStage` genérico por level no contrato atual.
- A superfície documental principal deixa de promover `PostLevel` como stage de level.
