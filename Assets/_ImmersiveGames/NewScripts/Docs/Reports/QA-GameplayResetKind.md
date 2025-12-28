# QA — GameplayReset por ActorKind

## Objetivo
Validar que o `GameplayReset` por `ActorKind` executa as fases apenas nos atores esperados (Dummy e Player), incluindo o Player real.

## Pré-requisitos
- Cena com `GameplayResetKindQaSpawner` disponível.
- Build em **Editor** ou **Development Build** (o `GameplayResetPhaseLogger` é instrumentação de QA e não existe em build final).
- `GameplayResetKindQaProbe` só existe em Editor/Dev/NEWSCRIPTS_QA, evitando vazamento em build final.

## Como executar (passo a passo)
1) Menu → **Gameplay** → **Spawn QA actors** (com `spawnEaterProbe=true` e prefab do Eater configurado).
2) Menu → **Gameplay** → **Reset By Kind Dummy**.
3) Menu → **Gameplay** → **Reset By Kind Player**.
4) No driver `GameplayResetRequestQaDriver`, execute **Run EaterOnly**.

## Critérios de sucesso (logs)
### Reset por Dummy
- `Resolved targets for kind=Dummy: 1`
- `Dummy Probe -> Cleanup`
- `Dummy Probe -> Restore`
- `Dummy Probe -> Rebind`

### Reset por Player
- `Resolved targets for kind=Player: 2` (Player real + QA)
- `GameplayResetPhaseLogger -> Cleanup` (no Player real, inclui `id=` quando disponível)
- `GameplayResetPhaseLogger -> Restore` (no Player real, inclui `id=` quando disponível)
- `GameplayResetPhaseLogger -> Rebind` (no Player real, inclui `id=` quando disponível)

### Reset por EaterOnly (QA)
- `Resolved targets: 1 => QA_Eater_Kind:...`
- `Eater Probe -> Cleanup`
- `Eater Probe -> Restore`
- `Eater Probe -> Rebind`

## Observações
- `PlayersResetParticipant` **não** é afetado por este QA: ele continua sendo a ponte de `ResetScope.Players`.
- O `GameplayResetPhaseLogger` é apenas instrumentação e não altera input/movimento do Player.
- Se `spawnEaterProbe=false`, o `EaterOnly` volta a resolver `0` targets (comportamento esperado).
