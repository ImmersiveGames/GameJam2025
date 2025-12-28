# QA — GameplayReset Request Matrix (2025-12-29)

## Contexto
Validação da matriz de `GameplayResetRequest` via QAs do NewScripts (driver de requests + spawner de atores QA), confirmando resolução de targets e execução das fases de reset.

## Setup / Pré-condições
- Fluxo executado: **Startup → MenuScene → GameplayScene** com `SceneTransitionService` + Fade + Loading HUD.
- `WorldLifecycleRuntimeCoordinator` dispara hard reset em **GameplayScene** (evento `ScenesReady`).
- `PlayerSpawnService` spawna `Player_NewScripts` e o `ActorRegistry` fica com **count=1** após o spawn.
- QA spawner cria **3 atores QA**: `QA_Player_Kind`, `QA_Dummy_Kind`, `QA_Eater_Kind`.

## Matriz de Requests

| Request Target | Parâmetros | Targets resolvidos (count) | Atores observados | Evidência (trechos do log) |
|---|---|---:|---|---|
| EaterOnly | — | 1 | `QA_Eater_Kind` | “Resolved targets: 1 => QA_Eater_Kind:…” + “Completed … serial=1” |
| AllActorsInScene | — | 4 | `Player_NewScripts`, `QA_Player_Kind`, `QA_Dummy_Kind`, `QA_Eater_Kind` | “Resolved targets: 4 => Player_NewScripts, QA_Player_Kind, QA_Dummy_Kind, QA_Eater_Kind” |
| PlayersOnly | ActorKind=Player | 2 | `Player_NewScripts`, `QA_Player_Kind` | “Resolved targets: 2 => Player_NewScripts, QA_Player_Kind” |
| ActorIdSet | ActorIds=[1] | 1 | `QA_Eater_Kind` | “ActorIdSet (ActorIds=1)” + “Resolved targets: 1 => QA_Eater_Kind:…” |
| ByActorKind | ActorKind=Eater | 1 | `QA_Eater_Kind` | “ByActorKind (ActorKind=Eater)” + “Resolved targets: 1 => QA_Eater_Kind:…” |

## Snippets de evidência

### EaterOnly
```
Request => GameplayResetRequest(Target=EaterOnly, Reason='QA/GameplayResetRequestEaterOnly', ActorIds=0)
Resolved targets: 1 => QA_Eater_Kind:...
Completed => ... serial=1
```

### AllActorsInScene
```
Request => GameplayResetRequest(Target=AllActorsInScene, Reason='QA/GameplayResetRequestAllActors', ActorIds=0)
Resolved targets: 4 => Player_NewScripts, QA_Player_Kind, QA_Dummy_Kind, QA_Eater_Kind
Completed => ... serial=2
```

### PlayersOnly
```
Request => GameplayResetRequest(Target=PlayersOnly, Reason='QA/GameplayResetRequestPlayersOnly', ActorIds=0, ActorKind=Player)
Resolved targets: 2 => Player_NewScripts, QA_Player_Kind
Completed => ... serial=3
```

### ActorIdSet
```
Request => GameplayResetRequest(Target=ActorIdSet, Reason='QA/GameplayResetRequestActorIdSet', ActorIds=1)
Resolved targets: 1 => QA_Eater_Kind:...
Completed => ... serial=4
```

### ByActorKind (Eater)
```
Request => GameplayResetRequest(Target=ByActorKind, Reason='QA/GameplayResetRequestByActorKind', ActorIds=0, ActorKind=Eater)
Resolved targets: 1 => QA_Eater_Kind:...
Completed => ... serial=5
```

## Conclusões
- O classificador resolve corretamente por **target/kind/idset** conforme observado nos logs.
- O orchestrator executa **Cleanup/Restore/Rebind** e completa as requisições (serial 1..5).
- `AllActorsInScene` inclui `Player_NewScripts` e todos os atores QA (Player/Dummy/Eater), conforme evidência.

## Ruído conhecido (opcional)
- Warnings de “**Chamada repetida**” podem aparecer no mesmo frame durante resolução repetida; são ruído de log e não indicam falha funcional.
