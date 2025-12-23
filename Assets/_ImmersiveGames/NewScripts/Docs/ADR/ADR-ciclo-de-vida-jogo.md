Doc update: Reset-In-Place semantics clarified

# ADR – Ciclo de Vida do Jogo, Reset por Escopos e Fases Determinísticas

> Owner deste ADR: decisão arquitetural sobre **fases, escopos e reset-in-place**.  
> Owner operacional (pipeline, ordem e troubleshooting): `../WorldLifecycle/WorldLifecycle.md`.

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
- **Detalhamento operacional**: pipeline, ordenação determinística, contratos de escopo e troubleshooting estão em `../WorldLifecycle/WorldLifecycle.md` (owner operacional).

## Definição de Fases (linha do tempo)
`SceneScopeReady → WorldServicesReady → SpawnPrewarm → SceneScopeBound → GameplayReady`  
Owner do detalhamento e âncoras: `../WorldLifecycle/WorldLifecycle.md#fases-de-readiness`.
- Scene Flow prepara e adquire o gate em `SceneScopeReady`, configura serviços em `WorldServicesReady`, realiza prewarm em `SpawnPrewarm`, libera binds em `SceneScopeBound` e autoriza gameplay apenas em `GameplayReady`.

## Reset Scopes
- **Owner das semânticas**: `../WorldLifecycle/WorldLifecycle.md#resets-por-escopo` e `../WorldLifecycle/WorldLifecycle.md#reset-por-escopo--soft-reset-players-reset-in-place`.
- **Resumo da decisão**: soft reset é opt-in por escopo (`ResetContext.Scopes`), preserva instâncias/identidades (reset-in-place) e mantém binds/registries de cena; hard reset recompõe mundo, bindings e registries com novo acquire do gate.

### Soft Reset Semantics — Reset-In-Place (decisão)
- **Soft Reset Players = reset-in-place** (sem despawn/spawn, instâncias e `ActorId` preservados).
- Gate utilizado: `flow.soft_reset`.
- Participam apenas `IResetScopeParticipant` do escopo solicitado (ex.: `Players`).
- Justificativa: reduzir churn/GC, manter referências externas estáveis e preservar baseline funcional.  
Detalhe operacional: `../WorldLifecycle/WorldLifecycle.md#reset-por-escopo--soft-reset-players-reset-in-place`.

## Spawn Passes
- Owner do pipeline: `../WorldLifecycle/WorldLifecycle.md#spawn-determinístico-e-late-bind`.
- Decisão: pipeline determinístico em passes (prewarm, serviços de mundo, atores, late bindables) com binds liberados apenas após `SceneScopeBound`.

## Late Bind (UI cross-scene)
- Owner das regras: `../WorldLifecycle/WorldLifecycle.md#spawn-determinístico-e-late-bind`.
- Decisão: binds de HUD/overlay são liberados somente após `SceneScopeBound`.

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
