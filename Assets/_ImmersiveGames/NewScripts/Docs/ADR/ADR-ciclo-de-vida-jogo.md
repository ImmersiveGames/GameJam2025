Doc update: Reset-In-Place semantics clarified

# ADR – Ciclo de Vida do Jogo, Reset por Escopos e Fases Determinísticas

## Contexto
- **Ordem**: a ausência de fases formais para readiness, spawn e bind gera corridas entre cenas, serviços e UI, dificultando resets determinísticos.
- **Spawn**: pipelines de spawn variam por cena, com pouca previsibilidade sobre pools, atores e dependências de UI.
- **Bind**: bindings de UI entre cenas (HUD, overlays compartilhados) acontecem cedo demais ou tardiamente, causando referências nulas.
- **Reset**: não há contrato único para resets parciais (soft) ou completos (hard), forçando duplicação de lógica em controladores.

## Objetivos
- Estabelecer fases oficiais de readiness do jogo (da cena até gameplay) com ordem determinística.
- Padronizar pipeline de spawn em passes explícitos, permitindo inspeção e telemetria.
- Permitir late bind de UI cross-scene sem acoplamento temporal.
- Introduzir resets por escopo (soft/hard) com contratos claros e reutilizáveis.
- Integrar o ciclo de vida com **Scene Flow** e **WorldLifecycle** sem alterar APIs existentes.

## Decisões Arquiteturais
- **FSM (Game Flow Controller)**: permanece simples, limitado a sinais de alto nível (`EnterLobby`, `StartMatch`, `EndMatch`) e delega execução detalhada para Scene Flow + WorldLifecycle.
- **Scene Flow**: responsável por readiness de cena e binds cross-scene. Fornece estados `SceneScopeReady` e `SceneScopeBound` antes de liberar gameplay.
- **WorldLifecycle**: permanece encarregado de reset determinístico de atores/serviços, agora acionado por escopo (soft/hard) e alinhado às fases de Scene Flow.
- **Coordenação**: Scene Flow coordena gates de readiness; WorldLifecycle executa despawn/spawn/reset; FSM apenas navega entre cenas/partidas.
- **Detalhamento operacional**: o pipeline completo, ordenação determinística, contratos de escopo e troubleshooting vivem em `../WorldLifecycle/WorldLifecycle.md` como fonte operacional única.

## Definição de Fases (linha do tempo)
`SceneScopeReady → WorldServicesReady → SpawnPrewarm → SceneScopeBound → GameplayReady`
- **Owner das fases**: detalhamento e logs esperados estão em `../WorldLifecycle/WorldLifecycle.md#fases-de-readiness`.
- **Resumo**: Scene Flow prepara e adquire o gate em `SceneScopeReady`, configura serviços em `WorldServicesReady`, realiza prewarm em `SpawnPrewarm`, libera binds em `SceneScopeBound` e autoriza gameplay apenas em `GameplayReady`.

## Reset Scopes
- **Owner das semânticas**: contrato completo em `../WorldLifecycle/WorldLifecycle.md#escopos-de-reset` e `#resets-por-escopo`.
- **Resumo**: soft reset é opt-in por escopo (`ResetContext.Scopes`), mantendo binds e registries de cena; hard reset recompõe mundo, bindings e registries com novo acquire do gate.

### Soft Reset Semantics — Reset-In-Place
- **Decisão arquitetural**: `Soft Reset Players = reset-in-place`.
- **Contrato explícito**: não existe pipeline de despawn/spawn nesse fluxo.

#### Preservado
- `GameObject` / instância do ator permanece viva (não é despawnada).
- Identidade (`ActorId`) permanece a mesma (não é recriada).
- Registro no `ActorRegistry` permanece ativo (contagem não diminui).

#### Não acontece no Soft Reset Players
- `IWorldSpawnService.DespawnAsync` **não é chamado** (skip por filtro de escopo).
- `IWorldSpawnService.SpawnAsync` **não é chamado** (skip por filtro de escopo).
- Nenhuma instância é destruída ou recriada.
- Nenhum `ActorId` é regenerado.

#### O que acontece
- Execução exclusiva de `IResetScopeParticipant` pertencentes ao escopo solicitado
  (ex.: `PlayersResetParticipant`).
- Ordem determinística conforme registro.
- Uso explícito do gate `flow.soft_reset`, conforme logs validados.

#### Justificativa
- Reduz churn de instanciamento e custo de GC.
- Mantém referências externas estáveis (UI, câmera, input, listeners).
- Preserva determinismo via reset explícito de estado lógico.
- Mantém continuidade de telemetria e QA ao preservar `ActorId`.

#### Escopo como domínio funcional
- `ResetScope.Players` representa o **baseline funcional de gameplay**
  (input, câmera, HUD/UI, managers, caches, timers globais),
  não a hierarquia do prefab.
- Participantes podem atuar fora do GameObject para restaurar o estado esperado.

#### Contrato estável
- Reset-in-place é parte **formal** do contrato do WorldLifecycle.
- Não é workaround, nem comportamento temporário.

### Hard Reset vs Soft Reset Players

| Aspecto        | Hard Reset                  | Soft Reset Players (Reset-In-Place) |
|---------------|-----------------------------|-------------------------------------|
| Despawn       | Sim                         | Não (skip por scope filter)          |
| Spawn         | Sim                         | Não (skip por scope filter)          |
| ActorId       | Recriado                    | Preservado                           |
| GameObject    | Reinstanciado               | Mantido                              |
| Registry      | Recomposto                  | Mantido (contagem estável)           |
| Gate          | `WorldLifecycle.WorldReset` | `flow.soft_reset`                    |
| Semântica     | Reconstrução total          | Reset lógico in-place                |

- **Racional**: decisão intencional para garantir determinismo, testabilidade (baseline de QA) e evolução de gameplay sem acoplamento estrutural.

## Spawn Passes
- **Owner do pipeline de passes**: `../WorldLifecycle/WorldLifecycle.md#spawn-determinístico-e-late-bind`.
- **Resumo**: pipeline determinístico em passes (prewarm, serviços de mundo, atores, late bindables).

## Late Bind (UI cross-scene)
- **Owner das regras**: `../WorldLifecycle/WorldLifecycle.md#spawn-determinístico-e-late-bind`.
- **Resumo**: binds de HUD/overlay são liberados apenas após `SceneScopeBound`.

## Uso do SimulationGateService
- `SimulationGateService` é obrigatório para serializar readiness e resets.
- Scene Flow adquire o gate em `SceneScopeReady` e libera em `GameplayReady`.
- Hard resets reabrem o gate.
- Soft resets reutilizam o gate existente, bloqueando apenas durante o reset.

## Linha do tempo oficial
````
SceneTransitionStarted
↓
SceneScopeReady (Gate Acquired, registries prontos)
↓
SceneTransitionScenesReady
↓
WorldLoaded
↓
SpawnPrewarm
↓
SceneScopeBound (Late Bind liberado)
↓
SceneTransitionCompleted
↓
GameplayReady (Gate liberado)
↓
[Soft Reset → WorldLifecycle reset scoped]
[Hard Reset → Desbind + WorldLifecycle full reset + reacquire gate]
````

## Consequências
- Determinismo forte entre cenas.
- Observabilidade clara por logs.
- UI resiliente a reloads e resets.
- Escopos explícitos eliminam heurísticas em controladores.

## Não-objetivos
- Alterar APIs públicas existentes.
- Introduzir multiplayer online.
- Implementar QA automatizado neste ADR.

## Plano de Implementação (fases)
1. **Contratos e sinais**
2. **Scene Flow**
3. **WorldLifecycle**
4. **UI Late Bind**
5. **Telemetria e QA manual**
