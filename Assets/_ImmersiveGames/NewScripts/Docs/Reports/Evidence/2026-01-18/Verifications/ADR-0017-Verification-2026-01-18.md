# ADR-0017 — Verification (2026-01-18)

## Objetivo
Validar por log (Console) que existem **dois tipos explícitos** de troca de fase:
- **In-Place**: sem SceneFlow; reset determinístico direto.
- **SceneTransition**: com SceneFlow; intent registry; commit após reset.

## Fonte
- `../Logs/ADR-0017-ConsoleLog-2026-01-18.log`

## Checklist de aceite (por contrato)

### In-Place (PASS)
- PhaseChangeRequested `mode=InPlace`.
- Gate `flow.phase_inplace` adquirido/liberado.
- Pending set → Reset solicitado (`source='phase.inplace:<phaseId>'`) → Commit observado.

### SceneTransition (PASS)
- Intent registrada com `signature`.
- TransitionStarted (SceneFlow) com a mesma `signature`.
- Reset disparado em `SceneFlow/ScenesReady`.
- Intent consumida + commit observado após ResetCompleted.
- Gate `flow.phase_transition` adquirido/liberado.

## Pontos de documentação
- Ajustar o texto do ADR-0017 para descrever corretamente:
  - Reset disparado em `ScenesReady`.
  - Consumo do intent e commit em `ResetCompleted` via bridge.
