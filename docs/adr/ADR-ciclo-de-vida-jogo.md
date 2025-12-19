# ADR – Ciclo de Vida do Jogo, Reset por Escopos e Fases Determinísticas
_Doc update: Reset-In-Place semantics clarified._

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
 - **Detalhamento operacional**: o pipeline completo, ordenação determinística, contratos de escopo e troubleshooting vivem em `docs/world-lifecycle/WorldLifecycle.md` como fonte operacional única.

## Definição de Fases (linha do tempo)
`SceneScopeReady → WorldServicesReady → SpawnPrewarm → SceneScopeBound → GameplayReady`
- **Owner das fases**: detalhamento e logs esperados estão em `../world-lifecycle/WorldLifecycle.md#fases-de-readiness`.
- **Resumo**: Scene Flow prepara e adquire o gate em `SceneScopeReady`, configura serviços em `WorldServicesReady`, realiza prewarm em `SpawnPrewarm`, libera binds em `SceneScopeBound` e autoriza gameplay apenas em `GameplayReady`.

## Reset Scopes
- **Owner das semânticas**: contrato completo em `../world-lifecycle/WorldLifecycle.md#escopos-de-reset` e `#resets-por-escopo`.
- **Resumo**: soft reset é opt-in por escopo (`ResetContext.Scopes`), mantendo binds/registries de cena; hard reset recompõe mundo, bindings e registries com novo acquire do gate.

### Soft Reset Semantics — Reset-In-Place
- **Definição**: Soft Reset `Players` é reset-in-place. Nenhuma instância de ator é destruída ou recriada; o pipeline atua sobre os participantes do escopo.
- **Preservado**:
  - GameObject/instância do ator permanece ativo (não é despawnado).
  - Identidade/`ActorId` permanece a mesma (não é regenerada).
  - Presença no `ActorRegistry` é mantida (count não reduz).
- **Não acontece**:
  - `IWorldSpawnService.DespawnAsync` não é chamado para esse escopo.
  - `IWorldSpawnService.SpawnAsync` não é chamado para esse escopo.
- **O que acontece**:
  - Execução dos `IResetScopeParticipant` do escopo (ex.: `PlayersResetParticipant`) na ordem determinística padrão.
  - Uso do gate `flow.soft_reset` (logs existentes no runner/controller).
- **Justificativa**:
  - Reduz churn de instância e custo de alocação.
  - Preserva referências externas já bindadas (HUD, listeners, roteadores).
  - Mantém determinismo do estado via reset explícito dos participantes.
  - Evita recriação de IDs e respawn desnecessário em multiplayer local.

## Spawn Passes
- **Owner do pipeline de passes**: `../world-lifecycle/WorldLifecycle.md#spawn-determinístico-e-late-bind`.
- **Resumo**: pipeline determinístico em passes (pré-warm, serviços de mundo, atores, late bindables) para garantir multiplayer local previsível.

## Late Bind (UI cross-scene)
- **Owner das regras de bind**: `../world-lifecycle/WorldLifecycle.md#spawn-determinístico-e-late-bind`.
- **Resumo**: binds de HUD/overlay são liberados apenas após `SceneScopeBound`; UI deve se registrar como late bindable e aguardar o sinal.

## Uso do SimulationGateService
- `SimulationGateService` é requerido para serializar resets e readiness de cena.
- Scene Flow adquire o gate em `SceneScopeReady`, mantém durante Spawn/Bind e libera em `GameplayReady`.
- Resets hard reabrem o gate; resets soft reutilizam o gate existente, bloqueando apenas enquanto o WorldLifecycle roda.

## Linha do tempo oficial
````
SceneTransitionStarted
↓
SceneScopeReady (Gate Acquired, registries prontos)
↓
SceneTransitionScenesReady
↓
WorldLoaded (registries e serviços de mundo configurados)
↓
SpawnPrewarm (Passo 0)
↓
SceneScopeBound (Late Bind liberado; HUD/overlays conectados)
↓
SceneTransitionCompleted
↓
GameplayReady (Gate liberado; gameplay habilitado)
↓
[Soft Reset? → WorldLifecycle reset scoped]
[Hard Reset? → Desbind + WorldLifecycle full reset + reacquire gate]
```
- **Owner**: linha do tempo, ordenação e logs pertencem a `../world-lifecycle/WorldLifecycle.md#linha-do-tempo-oficial`.

## Consequências
- **Determinismo**: fases claras evitam corridas de spawn/bind em multiplayer local.
- **Observabilidade**: cada fase/pass dispara logs e telemetria, facilitando QA.
- **Resiliência de UI**: binds tardios evitam referências nulas em HUDs compartilhados.
- **Escopos explícitos**: operações de reset documentadas; controladores não precisam heurísticas.
- **Detalhes operacionais**: o pipeline de reset determinístico e os hooks correspondentes estão descritos em `docs/world-lifecycle/WorldLifecycle.md` (fonte única).

## Não-objetivos
- Alterar APIs públicas atuais de WorldLifecycle ou Scene Flow.
- Implementar novos sistemas de rede ou multiplayer online (escopo permanece local).
- Introduzir QA automatizado neste ADR.

## Plano de Implementação (fases)
1. **Fase 1 — Contratos e sinais**: definir enums/identificadores de `ResetScope`, fases (`SceneScopeReady`, `GameplayReady`) e eventos de Scene Flow; adicionar logs/gates sem mudar APIs.
2. **Fase 2 — Scene Flow**: orquestrar aquisição/liberação do `SimulationGateService`, ordenar spawn passes e emitir sinal de `SceneScopeBound` para late bind.
3. **Fase 3 — WorldLifecycle**: adaptar reset para aceitar `ResetScope` (soft/hard) sem quebrar contratos; manter ordenação determinística e cache por ciclo.
4. **Fase 4 — UI Late Bind**: alinhar HUD/overlays para escutar `SceneScopeBound`, mover binds críticos para esse ponto, remover binds antecipados.
5. **Fase 5 — Telemetria e QA manual**: instrumentar logs por fase/pass, criar checklist manual para validar ordem e binds; sem testes automatizados neste estágio.
