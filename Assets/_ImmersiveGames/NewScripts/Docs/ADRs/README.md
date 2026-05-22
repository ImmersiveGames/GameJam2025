# ADRs — Base 1.1

Este diretório mantém a fonte normativa viva da Base 1.1.

## Fonte normativa atual

ADRs vivos:

1. **ADR-0001** — Base 1.1: Pipeline Convergence, Identidade Explícita e Isolamento contra Foreign Events
2. **ADR-0002** — Run Pipeline Canonical e Deactivation/Continuity
3. **ADR-0003** — Session Operational Pipeline e Session Transition Envelope
4. **ADR-0004** — Session Activity Pipeline
5. **ADR-0005** — Modules Produzem Facts/Commands, Adapters Executam Side-Effects
6. **ADR-0006** — Route, Scene Composition, Fade, Loading e Audio Adapters
7. **ADR-0007** — Gates, InputModes e Simulation Executors
8. **ADR-0008** — SaveSystem Canonical
9. **ADR-0009** — Session Player Slots e Operational Input Runtime
10. **ADR-0010** — Player Preparation Flow, Player Slots e Unity PlayerInput
11. **ADR-0011** — Runtime Configuration Registry e Config Sets
12. **ADR-0012** — Operational Camera Runtime e Future Activity Camera Binding
13. **ADR-0013** — Camera Presentation Runtime e Activity Camera Director
14. **ADR-0014** — ActivityContent, WindowTemplateLibrary e ActivityEntryPipeline

ADRs anteriores são histórico apenas. Em conflito, prevalece a Base 1.1.

---

## Precedência normativa

- ADR-0001 a ADR-0008: fundamentos estruturais da Base 1.1.
- ADR-0009 a ADR-0014: checkpoints normativos aceitos/congelados/implementados.
- Ownership não é decidido por conveniência operacional.
- `foreign/stale events` não podem alterar pipeline ativo.
- Config obrigatória quebrada deve falhar explicitamente.
- Não criar fallback silencioso, compat paralelo ou trilho fantasma.

---

## Conceitos chave

### Pipelines

- **Run Pipeline**: direção macro normativa para run, deactivation e continuity. No checkpoint atual do Base11Sandbox, não está materializado e não é pendência ativa. Reabrir somente quando houver run concreta com `RunResult`, `RunDecision`, `PostRun` ou decisão própria de continuidade/retry/exit/save de run.
- **Session Operational Pipeline**: orquestra rota, transição operacional, setup operacional, save/load de rota/activity e handoff inicial de sessão.
- **Session Activity Pipeline**: orquestra lifecycle local de Activity, activation, running, deactivation, restart, transition e route-exit.

### Separação de responsabilidades

- **Módulos** produzem `Pipeline Facts` ou `Pipeline Commands`.
- **Pipelines** decidem ordem, lifecycle, policy e handoffs.
- **Adapters** executam side-effects comandados.

### Identidade explícita

Todo ciclo relevante carrega identidade canônica. Identidade não é inferida por nome de cena, timing, singleton ou classe.

---

## Estado consolidado dos checkpoints

### Operational / Route

- `SessionOperationalPipeline` é owner de rota/transição operacional/handoff.
- `SceneComposition` executa load/unload/set-active; não decide lifecycle.
- Fade/loading/audio são adapters comandados pelo pipeline.
- Route camera e Activity camera têm ownership separado.

### Input / Player

- `InputRuntimeRoot` em `UIGlobalScene` é runtime operacional persistente de input.
- `PlayerInputManager`, `EventSystem` e `InputSystemUIInputModule` são executores técnicos, não owners de lifecycle.
- `SessionOperationalPipeline` prepara intenção/payload de player para handoff.
- `PlayerActor` jogável nasce no `SessionActivityPipeline/ActivitySetup`.
- `PlayerInputBindingStage` usa o asset canônico validado em InputModes.
- `MovementBindingStage` prepara/binda; `MovementControl` só libera em `ActivityRunning`.

### Camera

- `OperationalCameraRuntime` é infraestrutura de composition/bootstrap.
- `CameraPresentation` fornece directors/rigs/adapters de câmera.
- `PlayerCameraEndpoint` expõe `CameraFollowTarget` e `CameraLookAtTarget` no `PlayerActor`.
- `SessionActivityPipeline` decide o binding da `ActivityCamera` ao endpoint.
- `PlayerInput.camera` fica reservado para split-screen futuro.

### Activity / ADR-0014

Estado atual congelado:

- `ActivityEntryPipeline` único.
- `ActivityContentProfile` como fonte de conteúdo da Activity.
- `ActivitySetupInventory` como contrato de requisitos.
- `PlayerActor readiness` — PASS.
- `PlayerInputBindingStage` — PASS.
- `MovementBindingStage + MovementControl lifecycle` — PASS.
- `PlacementSetupStage v0` — fechado como implícito por `PlayerActorSetup + PlayerActorReset(Placement)`.
- `Window AdditiveScene v0` — aceito para `ActivationWindow`/`DeactivationWindow`; `WindowTemplateLibrary route-scoped` fica como futuro explícito.
- `CameraBindingSetupStage` mínimo — PASS.
- `RouteActivitySave + ActivityObjectSnapshotRestore` para `test_object_01` — PASS funcional e semântico.
- `RouteExitBackToMenu` checkpoint — PASS.
- `RestartCurrentActivity` com content e no-content — PASS.

Não são pendências ativas agora:

- integração de pooling sem objeto concreto;
- Progression Save real completo sem progressão real de jogo;
- materialização do Run Pipeline sem ciclo de run concreto;
- `CameraBindingRetained` explícito para Activity skip/no-content;
- `PlacementSetupStage` nominal separado enquanto o v0 implícito for suficiente;
- `WindowTemplateLibrary route-scoped` enquanto o v0 de `AdditiveScene` for suficiente e não houver necessidade de templates compartilhados/standby/payload bind-unbind.

---

## Gatilhos de reabertura

Reabrir temas somente com necessidade concreta:

- **Pooling**: quando objeto/prefab/policy exigir `Rent`, `Prewarm` ou `ReturnToPool`.
- **Progression Save real**: quando houver inventário, objetivos persistentes, actors/world state, run continuity ou UI de slots.
- **Run Pipeline**: quando houver run real com resultado, decisão, post-run, retry/continue/exit ou save de run.
- **PlacementSetupStage nominal**: quando placement exigir stage independente de `PlayerActorSetup/Reset`.
- **WindowTemplateLibrary route-scoped**: quando windows deixarem de ser QA simples/additive e precisarem de templates compartilhados, standby, payload bind/unbind ou reaproveitamento visual real entre Activities.
- **ObjectEntry/RuntimeSpawn real**: quando houver objeto/NPC/prop materializado dinamicamente pelo pipeline.

---

## Regra para próximos trabalhos

Antes de criar componente/config/adapter novo:

1. localizar o que já existe;
2. confirmar ownership atual;
3. classificar como déficit real, atraso documental, documentação sem congelamento ou fora de escopo ativo;
4. só então implementar ou atualizar documentação.

Prompts para Codex devem ser curtos, começar por auditoria quando houver risco de reinventar infraestrutura e nunca pedir build, compile, tests, playmode, batchmode ou validação executável.
