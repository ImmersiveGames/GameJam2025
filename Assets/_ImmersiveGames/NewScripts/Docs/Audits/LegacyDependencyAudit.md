# Auditoria de Dependências Legadas — NewScripts

## Sumário
- Total de referências ao legado `_ImmersiveGames.Scripts.*`: **21**.
- Distribuição por tipo: **Código: 20**, **Asmdef: 0**, **Docs: 1**.
- Arquivos de código afetados: **8** em `Assets/_ImmersiveGames/NewScripts`.

## Tabela 1 — Código
| Caminho | Linhas/Trechos com referência | Tipos legados usados | Categoria |
| --- | --- | --- | --- |
| Gameplay/Player/Movement/NewPlayerMovementController.cs | 13-17 | ActorSystems, GameplaySystems.Domain/Reset, PlayerControllerSystem.Movement, StateMachineSystems | Reset |
| Infrastructure/GlobalBootstrap.cs | 18 | IStateDependentService (StateMachineSystems) | DI |
| Infrastructure/State/NewScriptsStateDependentService.cs | 5 | ActionType (StateMachineSystems) | DI |
| Infrastructure/QA/WorldLifecycleBaselineRunner.cs | 13 | ActionType (StateMachineSystems) | QA |
| Infrastructure/Scene/GameReadinessService.cs | 2 | SceneTransitionStarted/ScenesReady/Completed (scene flow legado) | Outro |
| Infrastructure/World/WorldLifecycleRuntimeDriver.cs | 5 | SceneTransitionStarted/ScenesReady/Completed (scene flow legado) | Outro |
| Infrastructure/World/Scopes/Players/PlayersResetParticipant.cs | 6-17 | GameplaySystems.Domain/Reset, ActorSystems aliases | Reset |
| Infrastructure/Actors/PlayerActorAdapter.cs | 2 | LegacyActor alias (_ImmersiveGames.Scripts.ActorSystems.IActor_) | Outro |

## Tabela 2 — Asmdef
Nenhuma `.asmdef` em `Assets/_ImmersiveGames/NewScripts` referencia assemblies do legado `_ImmersiveGames.Scripts.*`.

## Tabela 3 — Docs
| Caminho | Referência | Motivo |
| --- | --- | --- |
| Docs/Audits/LegacyDependencyAudit.md | Lista consolidada das dependências acima. | Auditoria |

## Parte B — Propostas de Ação por Categoria (não executadas)
Cada categoria abaixo segue as opções solicitadas e uma recomendação preliminar.

- **Debug**
  - A) Adaptar via Bridge: criar um wrapper `DebugUtility` em `NewScripts/Gameplay/Bridges` para mapear chamadas para um logger interno.
  - B) Extrair Infra: duplicar utilitário de logging equivalente em `NewScripts/Infrastructure` com API compatível e redirecionar usos.
  - C) Recriar: implementar logger mínimo nativo do NewScripts, reduzindo superfície de API.
  - **Recomendação:** B — facilita substituição gradual preservando assinaturas atuais e reduz acoplamento ao legado.

- **DI**
  - A) Adaptar via Bridge: criar adaptadores em `NewScripts/Gameplay/Bridges` que encapsulem `DependencyManager`/`Inject` e exponham interfaces próprias.
  - B) Extrair Infra: trazer um contêiner leve para `NewScripts/Infrastructure` (ex.: provedor de serviços simples) mantendo contratos esperados pelo código atual.
  - C) Recriar: definir um novo mecanismo de resolução nativo (factory/service locator mínimo) e migrar pontos de uso.
  - **Recomendação:** B — extrair um provedor compatível dentro do NewScripts para eliminar dependência direta sem reescrever todas as entradas de DI de imediato.

- **Reset**
  - A) Adaptar via Bridge: criar interfaces/bridges em `NewScripts/Gameplay/Bridges` que traduzam `IResetInterfaces`/`ResetScope`/`ResetContext` para contratos nativos.
  - B) Extrair Infra: copiar contratos essenciais de reset para `NewScripts/Infrastructure` garantindo compatibilidade binária com chamadas existentes.
  - C) Recriar: definir modelo de reset próprio (escopos/contextos) e migrar participantes gradualmente.
  - **Recomendação:** A — manter contratos legados através de bridges enquanto se modela um ciclo de reset próprio, reduzindo risco para gameplay já integrado.

- **Outro (Predicates/EventBus/ActorSystems/SceneTransition)**
  - A) Adaptar via Bridge: criar adaptadores específicos (ex.: `EventBusBridge`, `PredicateAdapter`, `LegacyActorAdapter` estendido) em `NewScripts/Gameplay/Bridges`.
  - B) Extrair Infra: portar utilitários mínimos de predicates/event bus/scene flow para `NewScripts/Infrastructure` preservando assinaturas usadas.
  - C) Recriar: implementar versões nativas simplificadas e atualizar chamadas para os novos contratos.
  - **Recomendação:** A — iniciar com bridges pontuais para desatrelar rapidamente pontos sensíveis (event bus/scene flow/actors) antes de decidir por extração completa.

## Doc Placement Issues
Nenhum problema de localização ou duplicação de docs relacionado a NewScripts foi identificado fora de `Assets/_ImmersiveGames/NewScripts/Docs`.
