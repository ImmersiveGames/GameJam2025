# ADR-0007: Formalizar InputModes e responsabilidade do módulo

## Status

- Estado: **Implementado**
- Data (decisão): **2026-02-05**
- Última atualização: **2026-02-18**
- Dono: NewScripts/Core
- Tags: Input, SceneFlow, UX, Architecture

## Evidências canônicas (atualizado em 2026-02-18)

- `Docs/Reports/Evidence/LATEST.md`
- `Docs/Reports/Evidence/LATEST.md`
- `Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md`
- `Docs/Reports/Audits/2026-02-18/Audit-SceneFlow-RouteResetPolicy.md`

## Contexto

Existe um módulo chamado **InputModes** que aplica "modos" em runtime (normalmente alternando action maps / contexto de input).
Paralelamente existe **RuntimeMode** (Strict/Release + DegradedMode) responsável por políticas de tolerância e fallback.

A fonte de verdade de configuração do runtime é o **RuntimeModeConfig** (asset global), que deve controlar a ativação
do InputModes e seus nomes de action map padrão, mantendo fallback seguro quando o asset não estiver disponível.

## Decisão

1) Renomear o módulo/pasta para **InputModes**.
2) Definir explicitamente o escopo:
    - **RuntimeMode**: policy do ambiente (Strict/Release, degraded, tolerância a fallback) e fonte de configuração global.
    - **InputModes**: estado do input (map/contexto) aplicado em PlayerInput/UI (Frontend vs Gameplay vs Pause etc.).
3) Consolidar a fonte única de verdade via **RuntimeModeConfig** (asset global), com fallback para defaults (Player/UI)
   quando o asset estiver ausente ou com valores vazios.
4) Formalizar o contrato do InputModes via `InputModeId` e um `IInputModeService` com Set/TrySet + evento/log padronizado.

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
2) Expor configuração no RuntimeModeConfig (enable + nomes de action maps) como fonte única.
3) Registrar `IInputModeService` via GlobalCompositionRoot (produção) com fallback seguro e logs verbosos.
4) Atualizar bridges (SceneFlow/Pause) para usar `InputModeId` e reason padronizado.
5) (Opcional) Adicionar cache/registro de PlayerInput e aplicação por playerId.
6) Adicionar logs/invariantes baseline para cada transição relevante.

## Evidência Esperada (logs)

- `[InputMode] Modo alterado para 'FrontendMenu' (...)`
- `[OBS][InputMode] Applied mode='FrontendMenu' map='UI' ... reason='SceneFlow/Completed:Frontend'`
- Em Gameplay: Applied mode='Gameplay' map='Gameplay' após ScenesReady/Completed conforme profile.
- Se desabilitado por config: DEGRADED_MODE feature='InputModes' reason='disabled_by_config'.

## Como validar (checklist rápido)

1) No boot, validar log de registro do IInputModeService com playerMap/menuMap conforme RuntimeModeConfig.
2) Em SceneFlow TransitionCompleted Startup/Frontend, validar:
   - `[OBS][InputMode] Applied mode='FrontendMenu' map='UI' ... reason='SceneFlow/Completed:Frontend'`
3) Em SceneFlow TransitionCompleted Gameplay, validar:
   - `[OBS][InputMode] Applied mode='Gameplay' map='Gameplay' ... reason='SceneFlow/Completed:Gameplay'`
4) Se RuntimeModeConfig.inputModes.enableInputModes=false, validar:
   - DEGRADED_MODE feature='InputModes' reason='disabled_by_config'
   - IInputModeService não registrado no DI global.

