# NewScripts — Documentação

Este conjunto de documentos descreve a arquitetura **NewScripts** (Unity) e o estado atual do pipeline de **Scene Flow** + **Fade** e do **World Lifecycle** (reset determinístico por escopos).

## Onde está a documentação
Arquivos canônicos (este pacote):
- `README.md` — índice e orientação rápida.
- `ARCHITECTURE.md` — visão arquitetural de alto nível.
- `ARCHITECTURE_TECHNICAL.md` — detalhes técnicos, módulos e responsabilidades.
- `WORLD_LIFECYCLE.md` — semântica operacional do reset determinístico do mundo.
- `DECISIONS.md` — decisões/ADRs resumidos (o “porquê”).
- `EXAMPLES_BEST_PRACTICES.md` — exemplos e práticas recomendadas.
- `GLOSSARY.md` — glossário de termos.
- `CHANGELOG-docs.md` — histórico de alterações desta documentação.
- `ADR-0009-FadeSceneFlow.md` — ADR específico do Fade + SceneFlow (NewScripts).

## Status atual (resumo)
- Added: **Gameplay Reset module** (`Gameplay/Reset/`) com contratos e semântica estável:
    - `GameplayResetPhase` (Cleanup/Restore/Rebind) e `GameplayResetTarget` (AllActorsInScene/PlayersOnly/EaterOnly/ActorIdSet).
    - `GameplayResetRequest` + `GameplayResetContext`.
    - `IGameplayResettable` (+ `IGameplayResettableSync`), `IGameplayResetOrder`, `IGameplayResetTargetFilter`.
    - `IGameplayResetOrchestrator` + `IGameplayResetTargetClassifier` (serviços por cena).
- Added: **QA isolado para validar reset por grupos** (sem depender de Spawn 100%):
    - `GameplayResetQaSpawner` cria atores de teste (ex.: Players) e registra `IGameplayResettable` de prova.
    - `GameplayResetQaProbe` confirma execução das fases via logs (Cleanup/Restore/Rebind).
- Updated: integração **WorldLifecycle → Gameplay Reset** via `PlayersResetParticipant` (gameplay) plugado como `IResetScopeParticipant` no soft reset por escopos.
- Updated: padronização de nomes para reduzir ambiguidade com o reset por escopos do WorldLifecycle (ex.: “Target” no gameplay vs “Scope” no WorldLifecycle).

Em 2025-12-25:
- Pipeline **GameLoop → SceneTransitionService → Fade (FadeScene)** está funcional no perfil `startup` para carregar:
    - `MenuScene` + `UIGlobalScene` (Additive), definindo `MenuScene` como cena ativa.
- `NewScriptsSceneTransitionProfile` é resolvido via **Resources** em:
    - `Resources/SceneFlow/Profiles/<profileName>`
- `WorldLifecycleRuntimeCoordinator` recebe `SceneTransitionScenesReadyEvent` e:
    - **SKIP** de reset quando `profile='startup'` ou `activeScene='MenuScene'`
    - Emite `WorldLifecycleResetCompletedEvent` mesmo no SKIP (mantém o Coordinator destravando).

Observação: o `WorldLifecycleController` continua existente na cena de bootstrap (quando aplicável), com `AutoInitializeOnStart` desabilitado para evitar contaminação de testes. A execução real do reset em Gameplay será tratada na etapa de integração da cena de gameplay.

## Como ler (ordem sugerida)
1. `ARCHITECTURE.md`
2. `WORLD_LIFECYCLE.md`
3. `ADR-0009-FadeSceneFlow.md`
4. `ARCHITECTURE_TECHNICAL.md`
5. `DECISIONS.md`
6. `EXAMPLES_BEST_PRACTICES.md`
7. `GLOSSARY.md`
8. `CHANGELOG-docs.md`

## Convenções usadas nesta documentação
- Não presumimos assinaturas inexistentes. Onde necessário, exemplos são explicitamente marcados como **PSEUDOCÓDIGO**.
- `SceneTransitionContext` é um `readonly struct` (sem `null`, sem object-initializer).
- “NewScripts” e “Legado” coexistem: bridges podem existir, mas o **Fade** do NewScripts não possui fallback para fade legado.
