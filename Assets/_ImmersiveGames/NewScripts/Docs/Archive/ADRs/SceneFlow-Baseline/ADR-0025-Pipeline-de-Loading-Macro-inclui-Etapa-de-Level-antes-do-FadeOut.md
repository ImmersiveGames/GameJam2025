> [!NOTE]
> Arquivo historico. A decisao de IntroStage/LevelFlow foi consolidada em `ADR-0027`.

# ADR-0025 - Pipeline de Loading Macro inclui Etapa de Level antes do FadeOut

## Status

- Estado: **Aceito (implementado)**
- Data (decisao): **2026-02-19**

## Decisao canonica mantida

- O completion gate macro inclui `LevelPrepare/Clear` antes do `FadeOut`.
- `SceneFlow` continua dono da ordem macro.
- O contrato final de IntroStage nao vive aqui; ele foi consolidado em `ADR-0027`.

## Relação com outros ADRs

- `ADR-0027`: ownership, hooks e presenter contract do IntroStage.
- `ADR-0037`: lista oficial de hooks e pontos de extensao.
- `ADR-0026`: swap local continua fora do trilho macro.

