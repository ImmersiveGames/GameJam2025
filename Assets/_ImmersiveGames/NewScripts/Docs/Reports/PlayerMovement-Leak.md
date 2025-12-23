# Player Movement Leak Smoke Result
- Timestamp (UTC): 2025-12-23T20:52:00.6136885Z
- Cena ativa: DontDestroyOnLoad
- Player encontrado via: WorldRoot:WorldRoot
- Reset API: SceneTransitionScenesReadyEvent
- Gate inicial aberto: True
- Logs marcados capturados: 9
- Resultado final: PASS

## Métricas
- Teste A (Gate fecha): status=PASS velInicial=0,000 velApósGate=0,000 driftApósGate=0,000 detalhe=
- Teste B (Reset limpa estado): status=PASS velApósReset=0,000 driftApósReset=0,000 detalhe=
- Teste C (Reabrir gate): status=PASS velApósReabertura=0,000 driftApósReabertura=0,000 detalhe=

## Logs (até 50 entradas)
- [INFO] [PlayerMovementLeakSmokeBootstrap] [PlayerMoveTest][Leak] Runner iniciado (scene='DontDestroyOnLoad').
- [INFO] [PlayerMovementLeakSmokeBootstrap] [PlayerMoveTest][Leak] Teste A iniciado - gate deve bloquear movimento.
- [INFO] [PlayerMovementLeakSmokeBootstrap] [PlayerMoveTest][Leak] Teste A PASS - speed=0,000, drift=0,000
- [INFO] [PlayerMovementLeakSmokeBootstrap] [PlayerMoveTest][Leak] Teste B iniciado - reset deve limpar física.
- [INFO] [PlayerMovementLeakSmokeBootstrap] [PlayerMoveTest][Leak] Reset disparado via EventBus (SceneTransitionScenesReadyEvent). Context=SceneTransitionContext(Load=[NewBootstrap], Unload=[], TargetActive='NewBootstrap', UseFade=False, Profile='qa.player_move_leak_reset')
- [INFO] [PlayerMovementLeakSmokeBootstrap] [PlayerMoveTest][Leak] Player reapareceu após reset (path='WorldRoot:WorldRoot').
- [INFO] [PlayerMovementLeakSmokeBootstrap] [PlayerMoveTest][Leak] Teste B PASS - speed=0,000, drift=0,000, preResetSpeed=0,000, preResetDelta=0,000
- [INFO] [PlayerMovementLeakSmokeBootstrap] [PlayerMoveTest][Leak] Teste C iniciado - reabertura do gate não deve gerar input fantasma.
- [INFO] [PlayerMovementLeakSmokeBootstrap] [PlayerMoveTest][Leak] Teste C PASS - speed=0,000, drift=0,000
