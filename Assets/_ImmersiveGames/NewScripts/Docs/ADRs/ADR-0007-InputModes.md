# ADR-0007: Formalizar InputModes e responsabilidade do mÃ³dulo

## Status

- Estado: **Implementado**
- Data (decisÃ£o): **2026-02-05**
- Ãšltima atualizaÃ§Ã£o: **2026-02-18**
- Dono: NewScripts/Core
- Tags: Input, SceneFlow, UX, Architecture

## EvidÃªncias canÃ´nicas (atualizado em 2026-02-18)

- `Docs/Reports/Evidence/LATEST.md`
- `Docs/Reports/Evidence/LATEST.md`
- `Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md`
- `Docs/Reports/Audits/2026-02-18/Audit-SceneFlow-RouteResetPolicy.md`

## Contexto

Existe um mÃ³dulo chamado **InputModes** que aplica "modos" em runtime (normalmente alternando action maps / contexto de input).
Paralelamente existe **RuntimeMode** (Strict/Release + DegradedMode) responsÃ¡vel por polÃ­ticas de tolerÃ¢ncia e fallback.

A fonte de verdade de configuraÃ§Ã£o do runtime Ã© o **RuntimeModeConfig** (asset global), que deve controlar a ativaÃ§Ã£o
do InputModes e seus nomes de action map padrÃ£o, mantendo fallback seguro quando o asset nÃ£o estiver disponÃ­vel.

## DecisÃ£o

1) Renomear o mÃ³dulo/pasta para **InputModes**.
2) Definir explicitamente o escopo:
    - **RuntimeMode**: policy do ambiente (Strict/Release, degraded, tolerÃ¢ncia a fallback) e fonte de configuraÃ§Ã£o global.
    - **InputModes**: estado do input (map/contexto) aplicado em PlayerInput/UI (Frontend vs Gameplay vs Pause etc.).
3) Consolidar a fonte Ãºnica de verdade via **RuntimeModeConfig** (asset global), com fallback para defaults (Player/UI)
   quando o asset estiver ausente ou com valores vazios.
4) Formalizar o contrato do InputModes via `InputModeId` e um `IInputModeService` com Set/TrySet + evento/log padronizado.

## ConsequÃªncias

- Reduz ambiguidade arquitetural: cada mÃ³dulo tem uma â€œpalavra Ãºnicaâ€ e uma responsabilidade clara.
- Facilita auditoria baseline: troca de input vira evidÃªncia observÃ¡vel.
- Permite evoluÃ§Ã£o incremental:
    - suportar playerId,
    - cache de PlayerInput,
    - bridges mais simples (SceneFlow/Pause).

## Alternativas Consideradas

- Manter a nomenclatura anterior e documentar: rejeitado por manter ambiguidade e dificultar comunicaÃ§Ã£o.
- Fundir com RuntimeMode: rejeitado; responsabilidades sÃ£o ortogonais (policy vs estado de input).

## Plano de ImplementaÃ§Ã£o (incremental)

1) Renomear pasta/namespace para InputModes (sem alterar comportamento).
2) Expor configuraÃ§Ã£o no RuntimeModeConfig (enable + nomes de action maps) como fonte Ãºnica.
3) Registrar `IInputModeService` via GlobalCompositionRoot (produÃ§Ã£o) com fallback seguro e logs verbosos.
4) Atualizar bridges (SceneFlow/Pause) para usar `InputModeId` e reason padronizado.
5) (Opcional) Adicionar cache/registro de PlayerInput e aplicaÃ§Ã£o por playerId.
6) Adicionar logs/invariantes baseline para cada transiÃ§Ã£o relevante.

## EvidÃªncia Esperada (logs)

- `[InputMode] Modo alterado para 'FrontendMenu' (...)`
- `[OBS][InputMode] Applied mode='FrontendMenu' map='UI' ... reason='SceneFlow/Completed:Frontend'`
- Em Gameplay: Applied mode='Gameplay' map='Gameplay' apÃ³s ScenesReady/Completed conforme profile.
- Se desabilitado por config: DEGRADED_MODE feature='InputModes' reason='disabled_by_config'.

## Como validar (checklist rÃ¡pido)

1) No boot, validar log de registro do IInputModeService com playerMap/menuMap conforme RuntimeModeConfig.
2) Em SceneFlow TransitionCompleted Startup/Frontend, validar:
   - `[OBS][InputMode] Applied mode='FrontendMenu' map='UI' ... reason='SceneFlow/Completed:Frontend'`
3) Em SceneFlow TransitionCompleted Gameplay, validar:
   - `[OBS][InputMode] Applied mode='Gameplay' map='Gameplay' ... reason='SceneFlow/Completed:Gameplay'`
4) Se RuntimeModeConfig.inputModes.enableInputModes=false, validar:
   - DEGRADED_MODE feature='InputModes' reason='disabled_by_config'
   - IInputModeService nÃ£o registrado no DI global.

