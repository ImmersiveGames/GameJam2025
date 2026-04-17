# Session Integration

## Status

- Runtime seam explicit between the technical baseline and the semantic layers above it.
- Thin by design: this area composes session-side context, translators and request publishers without taking semantic ownership.

## Current scope

- Session-side bridges.
- Snapshot/state translators.
- Canonical session-side request publishers for adjacent seams such as `InputModes`.
- Thin coordinators only when the seam really needs them.

## Boundaries

- Does not own `GameplaySessionFlow`.
- Does not own `Session Transition`.
- Does not execute spawn or reset.
- Does not replace bootstrap ownership.
