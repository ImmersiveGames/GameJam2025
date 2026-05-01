# Module: ActorsSystem

> Status: draft structural reading. Must be audited against current source before finalizing.

## 1. One-line summary

`ActorsSystem` owns the canonical actor set semantics; operational spawn/materialization only realizes that set.

## 2. Architectural role

Semantic actor-set owner above operational execution.

## 3. What it owns

- Canonical actor definitions/specs.
- Actor set semantics.
- Actor identity, role, relevance and presence semantics.
- `ActorSpec` / `ActorSetRef` as canonical cross-axis references.
- High-level materialization spec/policy.

## 4. What it does not own

- Concrete instantiate/despawn.
- Concrete Unity input binding.
- Concrete camera binding.
- Concrete reset execution.
- Runtime-only discovery as source of legitimacy.

## 5. Main inputs

- Phase authoring data.
- Participation-derived context where applicable.
- Actor definitions/specs.
- Runtime presence observations as read-model input, not legitimacy source.

## 6. Main outputs

- Actor set references.
- Actor specs.
- Actor read/projection snapshots.
- Materialization requirements for operational execution.

## 7. Extension points

- Add a new actor source kind.
- Add a new realization mode.
- Add a new continuity/reset policy.
- Add a new read/projection derived field.

## 8. Forbidden changes

- Do not let spawn decide which actors canonically exist.
- Do not use prefab references as cross-axis semantic identity.
- Do not treat runtime discovery as actor legitimacy.
- Do not revive `WorldDefinition` as canonical actor owner.
- Do not collapse participation identity and actor identity.

## 9. Related ADRs

- ADR-0058 — ActorsSystem as canonical actor-set owner.
- ADR-0059 — Actors Operational Binding.
- ADR-0054 — Participation semantics.
- ADR-0057 — Base 1.0.

## 10. Source audit TODO

- List current `ActorsSystem` folders/files.
- Confirm `ActorSpec` fields and references.
- Confirm materialization directives and readiness rules.
- Confirm distinction between semantic actor set and runtime presence registry.
