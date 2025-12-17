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

## Definição de Fases (linha do tempo)
`SceneScopeReady → WorldServicesReady → SpawnPrewarm → SceneScopeBound → GameplayReady`
- **SceneScopeReady**: cena carregada, serviços de cena registrados (registries, providers, EventBus de cena) e SimulationGate adquirido.
- **WorldServicesReady**: world-level services configurados e registrados (WorldLifecycle, ActorRegistry, Spawn registries), sem spawn ainda.
- **SpawnPrewarm**: passes determinísticos para pools/actors leves; sem binding de UI ainda.
- **SceneScopeBound**: binds cross-scene concluídos (HUD/overlays), permissões de input liberadas.
- **GameplayReady**: autorização para lógica de gameplay, AI e timers; WorldLifecycle habilitado para reset por escopo.

## Reset Scopes
- **Soft Reset (SceneScopeSoftReset)**: reexecuta WorldLifecycle com focus em despawn/respawn de atores e serviços voláteis, preservando binds e estado de UI. Não fecha SimulationGate de cena.
- **Hard Reset (SceneScopeHardReset)**: executa pipeline completo (desbind, despawn, rebuild de registries), reacquire do SimulationGate e reinstala Scene Flow. Usado para troca de mapa ou rollback.
- **Escopo declarado**: todos os resets recebem o `ResetScope` (Soft/Hard) para telemetria e logs, evitando heurísticas.

## Spawn Passes
1. **Passo 0 — Prewarm Pools**: registra pools necessários e aquece recursos críticos (VFX, projectiles, HUD render textures).
2. **Passo 1 — World Services Spawn**: instancia serviços dependentes de mundo (spawners determinísticos, orquestradores de rodada).
3. **Passo 2 — Actors Spawn**: cria atores jogáveis e NPCs com ordem determinística de hooks (`Order`, `Type.FullName`).
4. **Passo 3 — Late Bindables**: objetos que precisam existir para UI (trackers, score providers) mas ainda sem UI vinculada.

## Late Bind (UI cross-scene)
- Binds de HUD/overlay ocorrem somente após `SceneScopeBound`, garantindo que spawners, registries e providers estejam vivos.
- UI listeners devem registrar-se como **Late Bindables** e aguardar sinal explícito de `SceneScopeBound` antes de consumir providers.
- Nenhum bind crítico ocorre durante `SpawnPrewarm`; apenas preparação de dados e providers.

## Uso do SimulationGateService
- `SimulationGateService` é requerido para serializar resets e readiness de cena.
- Scene Flow adquire o gate em `SceneScopeReady`, mantém durante Spawn/Bind e libera em `GameplayReady`.
- Resets hard reabrem o gate; resets soft reutilizam o gate existente, bloqueando apenas enquanto o WorldLifecycle roda.

## Linha do tempo oficial
```
Load Scene
↓
SceneScopeReady (Gate Acquired, Registries prontos)
↓
WorldServicesReady (WorldLifecycle configurado, registries de actors/spawn ativos)
↓
SpawnPrewarm (Passo 0)
↓
SceneScopeBound (Late Bind liberado; HUD/overlays conectados)
↓
GameplayReady (Gate liberado; gameplay habilitado)
↓
[Soft Reset? → WorldLifecycle reset scoped]
[Hard Reset? → Desbind + WorldLifecycle full reset + reacquire gate]
```

## Consequências
- **Determinismo**: fases claras evitam corridas de spawn/bind em multiplayer local.
- **Observabilidade**: cada fase/pass dispara logs e telemetria, facilitando QA.
- **Resiliência de UI**: binds tardios evitam referências nulas em HUDs compartilhados.
- **Escopos explícitos**: operações de reset documentadas; controladores não precisam heurísticas.
- **Detalhes operacionais**: o pipeline de reset determinístico e os hooks correspondentes estão descritos em `docs/world-lifecycle/WorldLifecycle.md`.

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
