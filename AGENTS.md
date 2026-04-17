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
- **Historical terms** (`LevelFlow`, `LevelLifecycle`, `ContentSwap`, `PostRun`) are deprecated. Use canonical module names and boundaries.
- **Ownership**: Each module is owner only of its boundary. E.g., `Gameplay` does not own `SceneFlow` or `Navigation`.
- **Event hooks**: Public events are documented in `Docs/Guides/Event-Hooks-Reference.md`. Use only canonical hooks for cross-module signaling.
- **Phase/session composition**: `GameplaySessionFlow` is the canonical entry for session/phase logic. See ADRs 0045–0050 for rationale.
- **Reset**: Macro reset is handled by `WorldReset`; local reset by `SceneReset`; bridge logic in `ResetInterop`.
- **Input modes**: Managed by `InputModeService` and `InputModeCoordinator`. Only use canonical requests (`FrontendMenu`, `Gameplay`, `PauseOverlay`).
- **Save/Progression**: `Experience/Save` is the official hook surface; `Progression` and `Checkpoint` are placeholders, not final features.
- **Audio**: Now a pure playback module; no domain arbitration. See `Docs/Modules/Audio.md` for current shape.

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

