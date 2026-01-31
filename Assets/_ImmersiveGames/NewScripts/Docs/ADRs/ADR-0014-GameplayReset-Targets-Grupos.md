# ADR-0014 - GameplayReset: Targets e Grupos

## Status

- Estado: **Aceito**
- Data: **2026-01-31** (revalidado no snapshot canonico)

## Contexto

Durante transicoes para gameplay (profile=gameplay), o projeto precisa resetar de forma deterministica um conjunto bem definido de alvos (targets) e grupos (grupos de servicos/registries) para garantir:

- ausencia de state leak entre runs
- spawns coerentes (Player/Eater, etc.)
- invariantes de gating e InputMode

## Decisao

- Consolidar o conceito de "targets" e "grupos" do GameplayReset.
- O reset de gameplay e disparado via driver de producao na fase ScenesReady do SceneFlow.
- A validacao do comportamento e feita via Baseline (logs) e auditoria Strict/Release.

## Evidencia

- Snapshot canonico: `../Reports/Evidence/LATEST.md`
- Evidencia datada (2026-01-31): `../Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md`

## Notas

Detalhes finos de implementacao (targets/grupos especificos) devem permanecer no codigo e no contrato de observabilidade, evitando duplicacao extensa em docs.
