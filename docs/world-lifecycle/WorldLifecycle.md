# World Lifecycle (NewScripts)
_Doc update: Reset-In-Place semantics clarified._

> Este documento implementa operacionalmente as decisões descritas no **ADR – Ciclo de Vida do Jogo, Reset por Escopos e Fases Determinísticas**.

## Visão geral do reset determinístico
O reset do mundo segue a ordem garantida pelo `WorldLifecycleOrchestrator`:

Acquire Gate → Hooks pré-despawn → Actor hooks pré-despawn → Despawn → Hooks pós-despawn → (se houver `ResetContext`) Scoped Reset Participants → Hooks pré-spawn → Spawn → Actor hooks pós-spawn → Hooks finais → Release.

O fluxo realiza:
- Acquire: tenta adquirir o `ISimulationGateService` usando o token `WorldLifecycle.WorldReset` para serializar resets.
- Hooks (pré-despawn): executa hooks registrados por serviços de spawn, hooks de cena (registrados no provider da cena) e hooks explícitos no `WorldLifecycleHookRegistry`.
- Actor hooks (pré-despawn): percorre atores registrados e executa `OnBeforeActorDespawnAsync()` de cada `IActorLifecycleHook` encontrado.
- Despawn: chama `DespawnAsync()` de cada `IWorldSpawnService` registrado, mantendo logs de início/fim.
- Hooks (pós-despawn): executa `OnAfterDespawnAsync()` na mesma ordem determinística de coleções.
- (Opt-in) Scoped reset participants: quando há `ResetContext`, executa `IResetScopeParticipant.ResetAsync()` apenas para os escopos solicitados antes de seguir para spawn.
- Hooks (pré-spawn): executa `OnBeforeSpawnAsync()` após os participantes de escopo.
- Spawn: chama `SpawnAsync()` dos serviços e, em seguida, hooks de atores e de mundo para `OnAfterSpawnAsync()`.
- Release: devolve o gate adquirido e finaliza com logs de duração.
- Nota: se não houver hooks registrados para uma fase, o sistema emite log verbose `"<PhaseName> phase skipped (hooks=0)"`.

## Ciclo de Vida do Jogo (Scene Flow + WorldLifecycle)
Fonte operacional única sobre readiness, spawn, bind e reset. As fases oficiais foram decididas no ADR `../adr/ADR-ciclo-de-vida-jogo.md`.

## Escopos de Reset
Define como o jogo reinicia e quais partes são recriadas em cada modo de reset. Decisão de escopos e fases vem do ADR `../adr/ADR-ciclo-de-vida-jogo.md`.

### Hard Reset vs Soft Reset
- **Hard Reset**: executa despawn + spawn dos `IWorldSpawnService`, recriando instâncias e `ActorId`, com hooks de cena/ator em todas as fases.
- **Soft Reset Players** (reset-in-place):
  - Mantém instâncias e `ActorId` dos atores (`ActorRegistry` não reduz).
  - Não chama `IWorldSpawnService.DespawnAsync` nem `IWorldSpawnService.SpawnAsync`.
  - Executa apenas os `IResetScopeParticipant` do escopo solicitado (ex.: `PlayersResetParticipant`) com o gate `flow.soft_reset`.
  - Logs esperados mostram services de spawn como “skipped” por filtro de escopo.

## Otimização: cache de Actor hooks por ciclo
Durante `ResetWorldAsync`, os hooks de ator (`IActorLifecycleHook`) podem ser cacheados por `Transform` dentro do ciclo para evitar varreduras duplicadas.
- O cache é limpo no `finally` do reset (inclusive em falha).
- Não há cache entre resets.

## Escopos de Reset
### Soft Reset (ex.: PlayerDeath)
- Opt-in por escopo: apenas participantes `IResetScopeParticipant` cujo escopo esteja em `ResetContext.Scopes` executam.
- Soft reset sem escopos não executa participantes.
- Não desregistra binds de UI/canvas.

### Linha do tempo oficial
```
SceneTransitionStarted
↓
SceneScopeReady (gate adquirido, registries de cena prontos)
↓
SceneTransitionScenesReady
↓
WorldLoaded (WorldLifecycle configurado; registries de actor/spawn ativos)
↓
SpawnPrewarm (Passo 0 — aquecimento de pools)
↓
SceneScopeBound (late bind liberado; HUD/overlays conectados)
↓
SceneTransitionCompleted
↓
GameplayReady (gate liberado; gameplay habilitado)
↓
[Soft Reset → WorldLifecycle reset scoped]
[Hard Reset → Desbind + WorldLifecycle full reset + reacquire gate]
```
Origem da decisão de fases: `../adr/ADR-ciclo-de-vida-jogo.md#definição-de-fases-linha-do-tempo`.

## Fases de Readiness
Fases formais que controlam quem pode agir e quando, garantindo que spawn/bind e gameplay sigam uma ordem previsível. Ver decisão em `../adr/ADR-ciclo-de-vida-jogo.md#definição-de-fases-linha-do-tempo`.

- **SceneScopeReady**: a cena concluiu a configuração básica e adquiriu o gate. Providers e registries de cena estão disponíveis, porém nenhum ator ou sistema de gameplay deve executar lógica ainda. Somente serviços de bootstrap e validações estruturais podem agir.
- **WorldLoaded**: o WorldLifecycle está configurado, registries de atores/spawn estão ativos e serviços de mundo podem preparar dados. Spawners determinísticos podem registrar intenções, mas o gameplay continua bloqueado.
- **GameplayReady**: gate liberado após `SceneScopeBound`/`SceneTransitionCompleted`. Atores e sistemas de gameplay podem iniciar comportamento; nenhuma lógica de gameplay deve rodar antes deste ponto, inclusive em soft reset.

Regra explícita: gameplay, atores e sistemas de fase só iniciam após `GameplayReady`. Soft resets mantêm essa garantia porque o gate permanece adquirido até a fase ser sinalizada novamente.

## Spawn determinístico e Late Bind
Define como o spawn acontece em passes ordenados e como binds tardios evitam inconsistências de UI/canvas cross-scene. Decisão de passes descrita em `../adr/ADR-ciclo-de-vida-jogo.md#spawn-passes`.

- **Por que spawn ocorre em passes**: o WorldLifecycle executa passos previsíveis (pré-warm, serviços de mundo, atores, late bindables) para manter determinismo em multiplayer local e permitir reset por escopo sem efeitos colaterais.
- **Problema clássico de Canvas/UI criados após atores**: se UI/canvas cruzados de cena nascem após atores, binds diretos falham ou geram referências nulas. Por isso, a criação de UI pode ocorrer antes, mas o bind real só acontece em uma fase de readiness específica.
- **Regra de binds tardios**: qualquer late bind (HUD, overlays, trackers) só é permitido após o sinal configurado de readiness (`SceneScopeBound`/`SceneTransitionCompleted`), garantindo que todos os atores e providers já existam e que o gate esteja controlando as ações.
- **Integração com readiness**: spawn em passes acontece antes do sinal de `GameplayReady`; apenas depois do bind tardio liberado e do gate ser liberado o gameplay inicia. Soft resets repetem os passes necessários e só liberam gameplay após o mesmo checkpoint de readiness.

### Quando spawn e bind acontecem
- **SpawnPrewarm (Passo 0)**: registra e aquece pools críticos (VFX, projectiles, render textures). Não faz bind de UI.
- **World Services Spawn (Passo 1)**: instancia serviços dependentes de mundo (spawners determinísticos, orchestrators de rodada) antes de atores jogáveis.
- **Actors Spawn (Passo 2)**: cria atores jogáveis e NPCs com ordenação determinística de hooks (`Order`, `Type.FullName`).
- **Late Bindables (Passo 3)**: componentes que precisam existir para UI, mas ainda sem bind (trackers, providers). O bind real ocorre apenas em `SceneScopeBound`.
- **Binds de UI**: HUD/overlays só conectam a providers após o sinal `SceneScopeBound`, evitando referências nulas e respeitando multiplayer local.

### Resets por escopo
- **Soft Reset**: reset-in-place para os escopos solicitados. Não há despawn/spawn para atores do escopo `Players`; serviços de spawn são ignorados pelo filtro de escopo. Executa somente `IResetScopeParticipant` do escopo, mantém binds/registries de cena e conserva `ActorId` e contagem do `ActorRegistry`. O gate permanece adquirido durante o reset (`flow.soft_reset`) e é liberado em `GameplayReady`.
- **Hard Reset**: realiza desbind de UI, despawn completo e rebuild de registries, reacquire do gate e reinstala Scene Flow antes de liberar `GameplayReady`. Usado para troca de mapa ou rollback de partida.
- **Escopo explícito**: todos os resets devem registrar `ResetScope` (Soft/Hard) em logs/telemetria para evitar heurísticas.

### Soft Reset por Escopo (semântica funcional)
- **Escopos são domínios de gameplay**: `Players`, `Boss`, etc. representam o resultado funcional a ser restaurado, não a localização física do código. `ResetScope.Players` é um contrato de baseline de gameplay (“player volta ao estado inicial”), não de hierarquia de GameObjects.
- **Soft reset por escopo foca no baseline, não em componentes específicos**: não é “quais componentes do prefab eu toco”, e sim “qual baseline de gameplay eu restauro”. A participação é opt-in e explícita: somente `IResetScopeParticipant` declarando `Scope=Players` (ou `Boss/Stage`) e presente em `ResetContext.Scopes` executa; qualquer ausência de escopo significa não rodar.
- **Participantes podem tocar sistemas externos**: um `IResetScopeParticipant` de `Scope=Players` pode (e deve) resetar managers, roteadores de input, serviços de gameplay, caches temporários, timers ou UI/HUD que influenciem o player, mesmo que morem fora do prefab. Escopo é resultado esperado, não endereço físico.
- **Restaurar baseline completo**: o critério é “o player volta ao estado inicial consistente”. Se isso exigir limpar cooldowns globais, estado de câmera, buffers de input, progressão temporária ou caches de encontro compartilhados, os participantes declarados no escopo devem fazê-lo.
- **Exemplos práticos**: `Players` pode englobar limpar buffers de input, reconfigurar HUD/overlays, resetar caches de atributos/estado de gameplay, reenquadrar câmera, invalidar timers globais dependentes do player ou sincronizar roteadores de câmera/input — tudo via participantes de `Scope=Players`, mesmo fora do prefab.
- **Anti-pattern explícito**: interpretar `ResetScope.Players` como “reset apenas dos componentes dentro do GameObject Player” é incorreto; o contrato é restaurar baseline funcional do domínio.
- **Determinismo preservado**: o pipeline continua o mesmo (Gate → Hooks → Scoped Participants → Hooks → Gate), apenas filtrando quem participa pelo escopo solicitado; o impacto pode atravessar fronteiras de sistemas para garantir o baseline do jogador.

### Fluxo textual do Soft Reset Players (reset-in-place)
Gate (`flow.soft_reset`) → hooks aplicáveis (se houver) → `IResetScopeParticipant` do escopo `Players` → hooks finais (se houver) → release do gate. Não há despawn/spawn nem recriação de atores/IDs.

### ResetScope as Gameplay Outcome (Not Object Hierarchy)
- **Conceito**: `ResetScope` representa o resultado esperado de gameplay (ex.: “resetar players corretamente”), e não “quais componentes do prefab/player serão tocados”.
- **Soft reset composicional**: múltiplos `IResetScopeParticipant` em sistemas/managers/serviços diferentes podem declarar `Scope=Players` (ou `Boss/Stage`), e todos devem rodar para restaurar o baseline daquele escopo. Exemplos para `Players`: UI manager que limpa HUD, roteador de input que reconfigura bindings locais, cache de stats que zera buffers temporários, gerenciador de câmera que recentraliza vista, timers/serviços de encontro que sincronizam estado do jogador.
- **Pipeline intacto**: a interpretação do escopo como resultado não muda a ordem do `WorldLifecycle`; o soft reset segue o mesmo pipeline, apenas com participantes filtrados pelo escopo solicitado.
- **Anti-pattern**: tratar `ResetScope.Players` como “apenas componentes dentro do GameObject Player”.
- **Padrão correto**: tratar `ResetScope.Players` como “tudo que afeta o estado necessário para o player reiniciar corretamente”, mesmo quando o estado vive fora do prefab.

## Onde o registry é criado e como injetar
- `WorldLifecycleHookRegistry` é criado e registrado apenas pelo `NewSceneBootstrapper`.
- Consumidores obtêm via DI e devem tolerar boot order (preferir `Start()` ou lazy injection + retry).

## Troubleshooting: QA/Testers e Boot Order
- Sintomas típicos: QA/tester não encontra registries, falha em `Awake`, logs iniciais “de erro”.
- Causa provável: bootstrapper ausente ou ordem de execução.
- Ação:
    1. Garantir `NewSceneBootstrapper` presente e ativo.
    2. Usar lazy injection + retry curto + timeout.
    3. Falhar com mensagem acionável se bootstrapper não rodou.

## Migration Strategy (Legacy → NewScripts)
- Consulte: **ADR-0001 — Migração incremental do Legado para o NewScripts**
- Guardrails: NewScripts não referencia concreto do legado fora de adaptadores; pipeline determinístico com gate sempre ativo.

## Baseline Validation Contract
- Checklist detalhado: `Docs/QA/WorldLifecycle-Baseline-Checklist.md`.
