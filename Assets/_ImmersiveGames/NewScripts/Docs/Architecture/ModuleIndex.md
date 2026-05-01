# Module Index

> Versão: v1 para revisão  
> Fonte de leitura: snapshot do projeto em 2026-04-29.  
> Escopo: inventário inicial dos módulos reais em `NewScripts`, com classificação prática para orientar os próximos docs.

## 1. Objetivo

Este documento é o mapa rápido dos módulos de `NewScripts`.

Ele responde:

- quais módulos existem no pacote atual;
- onde ficam no projeto;
- qual papel arquitetural exercem;
- qual tipo de documentação combina com cada módulo;
- qual prioridade faz sentido para as próximas fichas.

Este índice não substitui ADRs, não valida implementação e não tenta explicar cada arquivo. Ele é uma porta de navegação.

## 2. Como ler este índice

A ordem deste documento é uma **ordem de leitura e priorização documental**, não uma ordem exata de execução runtime.

- itens mais acima tendem a ser mais estruturais para entender a base;
- itens mais à esquerda nas tabelas são o foco principal da linha;
- classificação por papel serve para orientar onde documentar e onde alterar;
- sequência real de eventos deve ser descrita em `Flows/*.md`.



## 3. Papéis usados neste índice

| Papel | Significado prático |
|---|---|
| Foundation | Utilitários e infraestrutura técnica reutilizável. |
| Baseline | Trilho técnico/macro: boot, scene transition, loading/fade, gates, lifecycle macro. |
| Semantic | Define significado, política, ordem ou composição de sessão/runtime. |
| Seam | Traduz verdade semântica em intenção operacional para outros domínios. |
| Operational | Executa efeito concreto ou mantém estado/runtime operacional. |
| Presentation / UI | Interface, painéis, overlays, QA visual ou presenters. |
| Persistence / Settings | Persistência, preferências e backend de dados. |
| Authoring / Config | Assets, catálogos, configurações e dados de autoria. |
| Docs | Documentação, ADRs, relatórios e evidências. |

## 3. Índice principal dos módulos

| Módulo / Área | Caminho principal | Papel | Para que serve | Tipo de doc recomendado | Prioridade |
|---|---|---|---|---|---|
| Foundation | `NewScripts/Foundation` | Foundation | Base técnica comum: eventos, FSM, IDs, logging, validação, composição, config, pooling e utilidades de runtime. | Guia prático dividido por bloco. | Feito |
| ActorsSystem | `NewScripts/ActorsSystem` | Semantic | Define e governa o conjunto canônico de actors, specs, presença, ensemble, materialization plan e binding boundary. | Ficha arquitetural pesada. | Alta |
| AudioRuntime | `NewScripts/AudioRuntime` | Operational | Runtime de áudio: BGM, SFX global, cues, defaults, routing, pooling e host de listener. | Ficha média/prática. | Média |
| FrontendRuntime | `NewScripts/FrontendRuntime` | Presentation / UI | UI, painéis e ferramentas frontend/QA. | Guia prático por painel/área. | Baixa |
| GameplayRuntime | `NewScripts/GameplayRuntime` | Operational | Execução concreta do gameplay: spawn, ActorRegistry, GameplayReset, StateGate, integração operacional de actors. | Ficha média/pesada por subárea. | Alta |
| InputModes | `NewScripts/InputModes` | Operational | Rail operacional de input mode: request, coordinator, service, changed event e aplicação de mapa/contexto. | Guia curto + boundary. | Média |
| PreferencesRuntime | `NewScripts/PreferencesRuntime` | Persistence / Settings | Estado e backend de preferências, especialmente áudio/vídeo. | Guia prático. | Baixa |
| ResetFlow | `NewScripts/ResetFlow` | Operational / Baseline-adjacent | Reset macro/local: WorldReset, SceneReset e interop de reset. | Ficha média/pesada. | Alta |
| Resources | `NewScripts/Resources` | Authoring / Config | Assets carregáveis e configurações globais, como catalogs/configs usados no boot/runtime. | Índice de assets/configs. | Média |
| SaveRuntime | `NewScripts/SaveRuntime` | Persistence / Settings | Orquestração de save, contracts, models e persistence backend. | Guia prático + boundary. | Baixa |
| SceneFlow | `NewScripts/SceneFlow` | Baseline | Transição macro de cenas, navigation dispatch, loading/fade, readiness e scene composition. | Ficha arquitetural média/pesada. | Alta |
| SessionFlow | `NewScripts/SessionFlow` | Mixed: Semantic / Seam / Baseline | Guarda vários blocos centrais: GameLoop, Host, Integration, Semantic GameplaySession, PhaseCatalog, PostRun e SessionTransition. | Quebrar em módulos internos. | Alta |
| Docs | `NewScripts/Docs` | Docs | ADRs, relatórios, evidências e documentação arquitetural. | Índice documental. | Baixa |

## 5. Módulos com CompositionDescriptor explícito

Estes módulos aparecem com descriptor de composição no pacote atual.

| ModuleId | Descriptor | Pasta | Leitura inicial |
|---|---|---|---|
| `ActorsSystem` | `ActorsSystemCompositionDescriptor` | `ActorsSystem/Integration/Bootstrap` | Semantic owner do eixo de actors, com runtime composition próprio. |
| `Audio` | `AudioCompositionDescriptor` | `AudioRuntime/Playback/Bootstrap` | Runtime operacional de áudio. |
| `Gameplay` | `GameplayCompositionDescriptor` | `GameplayRuntime/Integration/Bootstrap` | Runtime operacional de gameplay, incluindo gates/camera e bridges de actors. |
| `InputModes` | `InputModesCompositionDescriptor` | `InputModes/Bootstrap` | Rail operacional de modos de input. |
| `Preferences` | `PreferencesCompositionDescriptor` | `PreferencesRuntime/Bootstrap` | Estado e backend de preferências. |
| `WorldReset` | `WorldResetCompositionDescriptor` | `ResetFlow/WorldReset/Installers` | Boundary de reset macro/world reset. |
| `Save` | `SaveCompositionDescriptor` | `SaveRuntime/Persistence/Bootstrap` | Orquestração canônica de save. |
| `SceneFlow` | `SceneFlowCompositionDescriptor` | `SceneFlow/Installers` | Baseline de transição macro de cena. |
| `GameLoop` | `GameLoopCompositionDescriptor` | `SessionFlow/GameLoop/Installers` | Lifecycle macro de jogo: play, pause, run-start, run-end. |
| `SessionIntegration` | `SessionIntegrationCompositionDescriptor` | `SessionFlow/Integration/Installers/Bootstrap` | Seam entre semântica de sessão e intenção operacional. |
| `Navigation` | `NavigationCompositionDescriptor` | `SessionFlow/Integration/Installers/Navigation` | Boundary de intent -> route/style/dispatch. |
| `PhaseDefinition` | `PhaseDefinitionCompositionDescriptor` | `SessionFlow/Semantic/GameplaySession/RuntimeComposition/Installers/PhaseDefinition` | Catálogo/resolução autoral de phases. |
| `RunEndRail` | `RunEndRailCompositionDescriptor` | `SessionFlow/Semantic/PostRun/Installers` | Rail interno de fim de run / run result / decision. |

## 6. Quebra recomendada de `SessionFlow`

`SessionFlow` não deve virar um único documento. Ele contém vários módulos lógicos com responsabilidades diferentes.

| Subárea | Caminho | Papel | Doc recomendado | Prioridade |
|---|---|---|---|---|
| GameLoop | `SessionFlow/GameLoop` | Baseline / Macro lifecycle | Ficha média. | Alta |
| Host / IntroStage | `SessionFlow/Host/IntroStage` | Presentation / Phase-local host | Ficha média. | Média |
| Host / PostRun | `SessionFlow/Host/PostRun` | Presentation / Macro decision UI | Ficha média. | Média |
| SessionIntegration | `SessionFlow/Integration` | Seam | Ficha arquitetural pesada. | Alta |
| Navigation | `SessionFlow/Integration/Installers/Navigation` + adapters | Baseline-adjacent / Dispatch | Ficha média. | Alta |
| GameplaySession | `SessionFlow/Semantic/GameplaySession` | Semantic | Ficha arquitetural pesada. | Alta |
| Participation | `SessionFlow/Semantic/Participation` | Semantic | Ficha pesada/média. | Alta |
| PhaseCatalog | `SessionFlow/Semantic/PhaseCatalog` | Semantic / Order | Ficha média/pesada. | Alta |
| IntroStage Semantic | `SessionFlow/Semantic/IntroStage` | Semantic / Phase-local lifecycle | Ficha média. | Média |
| PostRun / RunEndRail | `SessionFlow/Semantic/PostRun` | Semantic / Run end | Ficha pesada. | Alta |
| SessionTransition | `SessionFlow/Semantic/SessionTransition` | Semantic / Runtime transformation | Ficha arquitetural pesada. | Alta |

## 7. Quebra recomendada de `GameplayRuntime`

| Subárea | Caminho | Papel | Leitura inicial | Prioridade |
|---|---|---|---|---|
| ActorRegistry | `GameplayRuntime/ActorRegistry` | Operational | Fonte operacional de actors vivos/runtime. | Alta |
| Spawn | `GameplayRuntime/Spawn` | Operational | Materialização concreta de actors/objects. | Alta |
| GameplayReset | `GameplayRuntime/GameplayReset` | Operational | Reset escopado de gameplay. | Alta |
| StateGate | `GameplayRuntime/StateGate` | Operational / Gate | Gate técnico de estado/simulação. | Média |
| Integration / ActorsExecution | `GameplayRuntime/Integration/ActorsExecution` | Operational integration | Executor/handoff de materialização operacional. | Alta |
| Authoring | `GameplayRuntime/Authoring` | Authoring / Config | Assets/configs runtime de gameplay. | Média |

## 8. Quebra recomendada de `SceneFlow`

| Subárea | Caminho | Papel | Leitura inicial | Prioridade |
|---|---|---|---|---|
| Transition | `SceneFlow/Transition` | Baseline | Execução de transição macro e scene composition. | Alta |
| NavigationDispatch | `SceneFlow/NavigationDispatch` | Baseline / Dispatch | Dispatch macro para requests de navegação. | Alta |
| LoadingFade | `SceneFlow/LoadingFade` | Baseline / Presentation support | Loading/fade como subcapability de SceneFlow. | Média |
| Readiness | `SceneFlow/Readiness` | Baseline / Gate | Readiness técnica ligada à transição. | Alta |
| Contracts | `SceneFlow/Contracts` | Contracts | Superfície pública do módulo. | Média |
| Authoring | `SceneFlow/Authoring` | Authoring / Config | Route definitions, catalogs, transition assets. | Média |
| Installers | `SceneFlow/Installers` | Composition | Installer/bootstrap/descriptor. | Média |

## 9. Quebra recomendada de `ActorsSystem`

| Subárea | Caminho | Papel | Leitura inicial | Prioridade |
|---|---|---|---|---|
| Authoring | `ActorsSystem/Authoring` | Authoring / Config | Catálogos de actor sets/specs. | Alta |
| Models | `ActorsSystem/Models` | Semantic models | Contratos de axis, specs, ensemble, presence, plan e binding. | Alta |
| Semantic | `ActorsSystem/Semantic` | Semantic | Serviços de ensemble, specs, presence e materialization plan. | Alta |
| Contracts | `ActorsSystem/Contracts` | Contracts | Portas inbound para outros módulos. | Alta |
| Integration | `ActorsSystem/Integration` | Seam/Adapters | Adapters com SessionFlow, GameplayRuntime e OperationalBinding. | Alta |

## 10. Ordem recomendada para os próximos documentos

A ordem abaixo prioriza módulos com maior risco de regressão arquitetural.

1. `BaseOverview.md` — visão geral atualizada contra o pacote real.
2. `SceneFlow.md` — baseline macro de transição.
3. `SessionIntegration.md` — seam central.
4. `SessionTransition.md` — transformação de sessão/runtime.
5. `GameplaySessionFlow.md` — composição semântica da sessão jogável.
6. `ActorsSystem.md` — owner semântico do conjunto de actors.
7. `GameplayRuntime.md` — execução operacional de spawn/registry/reset/gates.
8. `ResetFlow.md` — reset macro/local.
9. `GameLoop.md` — lifecycle macro e pause/run events.
10. `PhaseCatalog.md` — ordem e navegação ordinal.
11. `RunEndRail.md` — RunResultStage / RunDecision / continuidade.
12. `AudioRuntime.md` — runtime de áudio.
13. `SaveRuntime.md` + `PreferencesRuntime.md` — persistência/configs de usuário.
14. `FrontendRuntime.md` — UI/QA/painéis.

## 11. Regras de leitura rápida

- Se define significado, política, ordem ou identidade: provavelmente é **Semantic**.
- Se traduz semântica para pedido operacional: provavelmente é **Seam**.
- Se executa boot, scene transition, loading, fade, gates ou lifecycle macro: provavelmente é **Baseline**.
- Se instancia, aplica, registra, reseta, toca áudio, salva ou altera input: provavelmente é **Operational**.
- Se é asset, catalog, config ou authoring data: provavelmente é **Authoring / Config**.
- Se é painel, presenter, overlay ou QA visual: provavelmente é **Presentation / UI**.

## 12. Observações para revisão

- `Foundation` já foi tratado como guia prático separado.
- `SessionFlow` precisa ser quebrado; não documentar como um módulo único pesado.
- `GameplayRuntime` e `ActorsSystem` devem ser documentados juntos em algum momento, mas não no mesmo arquivo.
- `Resources` deve virar índice de assets/configs, não ficha arquitetural pesada.
- `Docs` deve ser mantido como mapa documental, não como módulo runtime.
- Este índice deve ser revisado antes de gerar as fichas pesadas.
