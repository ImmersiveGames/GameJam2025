# AGENTS.md — GameJam2025 AI Coding Agent Guide

## Big Picture Architecture
- **Unity 6 project** with a modular, ADR-driven architecture. Core logic is in `Assets/_ImmersiveGames/NewScripts/`.
- **Major modules**: `Gameplay`, `SceneFlow`, `GameLoop`, `Navigation`, `WorldReset`, `SceneReset`, `ResetInterop`, `InputModes`, `Save`, `Audio`.
- **Service boundaries** are defined by module ownership (see `Docs/Modules/README.md`). Each module has clear responsibilities and boundaries; cross-module communication is explicit via services, events, or bridges.
- **Canonical documentation**: All operational docs live in `Assets/_ImmersiveGames/NewScripts/Docs`. ADRs in `Docs/ADRs/` define the "why" for major decisions.

## Developer Workflows
- **Build/Run**: Standard Unity workflows apply. No custom build scripts found.
- **Validation**: Use the Unity Editor MenuItem `Validate SceneFlow Config (DataCleanup v1)`; see report in `Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md`.
- **Input System**: Uses Unity Input System (`InputSystem_Actions.cs` auto-generated from `.inputactions`).

## Project-Specific Conventions
- **Historical terms** (`LevelFlow`, `LevelLifecycle`, `ContentSwap`, `PostRun`, `macro`, `local`) are deprecated. Use canonical Base 1.1 architecture.
- **Ownership**: Defined by Base 1.1 pipelines, not by module convenience. See `Docs/ADRs/ADR-0001.md` through `ADR-0008.md`.
- **Event hooks**: Public events are documented in `Docs/Guides/Event-Hooks-Reference.md`. Use only canonical hooks for cross-module signaling.
- **Session/Activity composition**: `SessionOperationalPipeline` owns routs/transitions; `SessionActivityPipeline` owns activity lifecycle. See `Docs/ADRs/ADR-0003.md` and `ADR-0004.md`.
- **Reset**: Macro reset is handled by `WorldReset`; local reset by `SceneReset`; bridge logic in `ResetInterop`.
- **Input modes**: Managed by `InputModeService` and `InputModeCoordinator`. Only use canonical requests (`FrontendMenu`, `Gameplay`, `PauseOverlay`). See `Docs/ADRs/ADR-0007.md`.
- **Save/Progression**: `SaveSystem` is adapter driven by pipelines. `Progression` and `Checkpoint` are placeholders. See `Docs/ADRs/ADR-0008.md`.
- **Audio**: Pure playback adapter driven by pipelines; no domain arbitration. See `Docs/Modules/Audio.md` and `Docs/ADRs/ADR-0006.md`.

## Integration Points & Patterns
- **Cross-module communication**: Always via explicit services, events, or bridges. Never assume implicit ownership.
- **Adding new features**: Follow the module/ownership pattern. Reference `Docs/Guides/How-To-Add-A-New-Module-To-Composition.md`.
- **Canonical event flow**: `SceneTransitionCompletedEvent` → `GameplaySessionFlow` → `PhaseDefinition` → `IntroStage`/`RunResultStage`/`RunDecision`.
- **Input**: Extend only via `.inputactions` and regenerate `InputSystem_Actions.cs`.

## Key Files & Directories
- `Assets/_ImmersiveGames/NewScripts/Docs/Modules/` — Module docs and boundaries
- `Assets/_ImmersiveGames/NewScripts/Docs/ADRs/` — Architectural decisions
- `Assets/_ImmersiveGames/NewScripts/Docs/Guides/` — How-tos and event references
- `Assets/InputSystem_Actions.cs` — Input system bindings (auto-generated)
- `Assets/_ImmersiveGames/NewScripts/Docs/Reports/` — Validation and operational reports

## Quick Reference
- **Never** use historical terms as canonical owners.
- **Always** check module docs and ADRs before changing boundaries.
- **For new integrations**, prefer explicit bridges/services and document in the appropriate module doc.
- **For event-driven logic**, use only hooks listed in `Event-Hooks-Reference.md`.

