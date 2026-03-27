> [!NOTE]
> Arquivo historico. A decisao de IntroStage/LevelFlow foi consolidada em `ADR-0027`.

# ADR-0026 - Troca de Level Intra-Macro via Swap Local (sem Transicao Macro)

## Status

- Estado: **Aceito (implementado)**
- Data (decisao): **2026-02-19**

## Decisao canonica mantida

- Swap local continua no dominio de `LevelFlow` e `SceneComposition` local.
- A macro route delimita o universo valido de levels.
- Restart local do mesmo level continua sendo local.
- A intro do level nao e owner deste ADR; consulte `ADR-0027`.

## Relação com outros ADRs

- `ADR-0027`: ownership, hooks e presenter contract do IntroStage.
- `ADR-0037`: lista oficial de hooks e pontos de extensao.
- `ADR-0025`: ordenacao macro de prepare/clear antes do fade out.

