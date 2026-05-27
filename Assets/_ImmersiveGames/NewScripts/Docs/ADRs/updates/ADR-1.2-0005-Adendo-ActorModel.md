# Adendo para ADR-1.2-0005 — Decomposition alinhada ao ADR-1.2-0008

Adicionar ao final do ADR-1.2-0005.

---

## Adendo normativo — Decomposition sem ActorKind switches

Após o ADR-1.2-0008, a decomposição do `SessionActivityPipeline` deve obedecer à regra adicional:

```text
não substituir trilhos paralelos por switches centrais de ActorKind.
```

A decomposição correta é por:

```text
Actor
ActorCapability
ActorEndpoint
runtime reference tipada
pipeline stage/boundary quando houver lifecycle/setup/readiness/release
relação local quando for gameplay moment-to-moment
```

`PlayerActor`, `NpcActor`, `EnemyActor`, `ObjectActor` e `InteractiveActor` podem ser tipos concretos, mas o pipeline não deve criar lifecycles paralelos por tipo.

A matriz de escopo fica refinada:

| Escopo | Responsabilidade |
|---|---|
| `SessionActivityPipeline core` | lifecycle macro, handoff, route-exit, restart, transition, foreign/stale guard |
| `ActivityEntryPipeline / stages` | ordem determinística, setup/release/readiness/reset/snapshot timing |
| `Actor` | identidade, participation e exposição de capabilities/endpoints |
| `ActorCapabilitySurface` | catálogo local de endpoints/capabilities, sem lifecycle global |
| `ActorEndpoint` | execução/reação local |
| `Adapter` | side-effect Unity comandado |
| Local gameplay | relação moment-to-moment entre endpoints/capabilities |

Componentes de binding transitório não devem proliferar como componentes Unity permanentes quando puderem ser estado de stage, inventory, adapter ou endpoint local.
