# ADR-0007: Formalizar InputModes e responsabilidade do módulo

- Status: **Proposed**
- Data: 2026-02-05
- Dono: NewScripts/Core
- Tags: Input, SceneFlow, UX, Architecture

## Contexto

Existe um módulo chamado **InputModes** que aplica "modos" em runtime (normalmente alterando action maps / contexto de input).
Paralelamente existe **RuntimeMode** (Strict/Release + DegradedMode) responsável por políticas de tolerância e fallback.

O nome anterior era genérico e conflita semanticamente com RuntimeMode, sugerindo que ambos tratam do mesmo problema.

## Decisão

1) Renomear o módulo/pasta para **InputModes**.
2) Definir explicitamente o escopo:
    - **RuntimeMode**: policy do ambiente (Strict/Release, degraded, tolerância a fallback).
    - **InputModes**: estado do input (map/contexto) aplicado em PlayerInput/UI (Frontend vs Gameplay vs Pause etc.).
3) Formalizar o contrato do InputModes via `InputModeId` e um `IInputModeService` com Set/TrySet + evento/log padronizado.

## Consequências

- Reduz ambiguidade arquitetural: cada módulo tem uma “palavra única” e uma responsabilidade clara.
- Facilita auditoria baseline: troca de input vira evidência observável.
- Permite evolução incremental:
    - suportar playerId,
    - cache de PlayerInput,
    - bridges mais simples (SceneFlow/Pause).

## Alternativas Consideradas

- Manter a nomenclatura anterior e documentar: rejeitado por manter ambiguidade e dificultar comunicação.
- Fundir com RuntimeMode: rejeitado; responsabilidades são ortogonais (policy vs estado de input).

## Plano de Implementação (incremental)

1) Renomear pasta/namespace para InputModes (sem alterar comportamento).
2) Introduzir `InputModeId` e atualizar `IInputModeService`.
3) Atualizar bridges (SceneFlow/Pause) para usar `InputModeId` e reason padronizado.
4) (Opcional) Adicionar cache/registro de PlayerInput e aplicação por playerId.
5) Adicionar logs/invariantes baseline para cada transição relevante.

## Evidência Esperada (logs)

- `[InputMode] Modo alterado para 'FrontendMenu' (...)`
- `[OBS][InputMode] Applied mode='FrontendMenu' map='UI' ... reason='SceneFlow/Completed:Frontend'`
- Em Gameplay: Applied mode='Gameplay' map='Gameplay' após ScenesReady/Completed conforme profile.
