# World Lifecycle (NewScripts)

> Este documento implementa operacionalmente as decisões descritas no **ADR – Ciclo de Vida do Jogo, Reset por Escopos e Fases Determinísticas**.

## Visão geral do reset determinístico
O reset do mundo segue a ordem garantida pelo `WorldLifecycleOrchestrator`: Acquire Gate → Hooks pré-despawn → Actor hooks pré-despawn → Despawn → Hooks pós-despawn → (se houver `ResetContext`) Scoped Reset Participants → Hooks pré-spawn → Spawn → Actor hooks pós-spawn → Hooks finais → Release. O fluxo realiza:
- Acquire: tenta adquirir o `ISimulationGateService` usando o token `WorldLifecycle.WorldReset` para serializar resets.
- Hooks (pré-despawn): executa hooks registrados por serviços de spawn, hooks de cena (registrados no provider da cena) e hooks explícitos no `WorldLifecycleHookRegistry`.
- Actor hooks (pré-despawn): percorre atores registrados e executa `OnBeforeActorDespawnAsync()` de cada `IActorLifecycleHook` encontrado.
- Despawn: chama `DespawnAsync()` de cada `IWorldSpawnService` registrado, mantendo logs de início/fim.
- Hooks (pós-despawn): executa `OnAfterDespawnAsync()` na mesma ordem determinística de coleções.
- (Opt-in) Scoped reset participants: quando há `ResetContext`, executa `IResetScopeParticipant.ResetAsync()` apenas para os escopos solicitados antes de seguir para spawn.
- Hooks (pré-spawn): executa `OnBeforeSpawnAsync()` após os participantes de escopo.
- Spawn: chama `SpawnAsync()` dos serviços e, em seguida, hooks de atores e de mundo para `OnAfterSpawnAsync()`.
- Release: devolve o gate adquirido e finaliza com logs de duração.
 - Nota: se não houver hooks registrados para uma fase, o sistema emite log verbose `"<PhaseName> phase skipped (hooks=0)"` para diagnóstico e para confirmar que a fase foi considerada.

## Ciclo de Vida do Jogo (Scene Flow + WorldLifecycle)
Texto de referência para Scene Flow / WorldLifecycle sobre readiness, spawn, bind e reset.

## Escopos de Reset
Define como o jogo reinicia e quais partes são recriadas em cada modo de reset.

- **Soft Reset (ex.: PlayerDeath)**:
  - Opt-in por escopo: apenas participantes que implementam `IResetScopeParticipant` e cujo `ResetScope` esteja em `ResetContext.Scopes` executam; `ResetScopesAsync` ignora listas vazias e rejeita `ResetScope.World` (para hard reset usar `ResetWorldAsync`).
  - `ResetContext.ContainsScope` retorna `false` quando `Scopes` está vazio/nulo, então um soft reset sem escopos não dispara participantes, mantendo mundo, serviços e cena ativos.
  - O ciclo passa pelo reset determinístico do WorldLifecycle apenas para os grupos marcados (ex.: `ResetScope.Players` ou um conjunto de inimigos) sem desregistrar bindings de UI, e o gate de simulação permanece adquirido até `GameplayReady` para evitar ações antecipadas.
- **Hard Reset (ex.: GameOver/Victory)**:
  - Recria o mundo inteiro: desbind de UI/canvas, teardown de registries de cena e reexecução completa do WorldLifecycle.
  - Obriga a refazer o ciclo de Scene Flow (acquire gate, readiness, spawn, bind) antes de permitir gameplay novamente.
  - Ideal para troca de mapa, reinício de rodada ou rollback completo de estado.

- **Exemplos de grupos futuros** a serem endereçados pelos resets (escopos explícitos e rastreáveis):
  - Players
  - Boss
  - Inimigos
  - Spawners
  - Sistemas de fase

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

## Fases de Readiness
Fases formais que controlam quem pode agir e quando, garantindo que spawn/bind e gameplay sigam uma ordem previsível.

- **SceneScopeReady**: a cena concluiu a configuração básica e adquiriu o gate. Providers e registries de cena estão disponíveis, porém nenhum ator ou sistema de gameplay deve executar lógica ainda. Somente serviços de bootstrap e validações estruturais podem agir.
- **WorldLoaded**: o WorldLifecycle está configurado, registries de atores/spawn estão ativos e serviços de mundo podem preparar dados. Spawners determinísticos podem registrar intenções, mas o gameplay continua bloqueado.
- **GameplayReady**: gate liberado após `SceneScopeBound`/`SceneTransitionCompleted`. Atores e sistemas de gameplay podem iniciar comportamento; nenhuma lógica de gameplay deve rodar antes deste ponto, inclusive em soft reset.

Regra explícita: gameplay, atores e sistemas de fase só iniciam após `GameplayReady`. Soft resets mantêm essa garantia porque o gate permanece adquirido até a fase ser sinalizada novamente.

## Spawn determinístico e Late Bind
Define como o spawn acontece em passes ordenados e como binds tardios evitam inconsistências de UI/canvas cross-scene.

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
- **Soft Reset**: reexecuta o reset do `WorldLifecycle` (despawn/respawn de atores e serviços voláteis) mantendo binds de UI e registries de cena. O gate permanece adquirido durante o reset e é liberado em `GameplayReady`.
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

### ResetScope as Gameplay Outcome (Not Object Hierarchy)
- **Conceito**: `ResetScope` representa o resultado esperado de gameplay (ex.: “resetar players corretamente”), e não “quais componentes do prefab/player serão tocados”.
- **Soft reset composicional**: múltiplos `IResetScopeParticipant` em sistemas/managers/serviços diferentes podem declarar `Scope=Players` (ou `Boss/Stage`), e todos devem rodar para restaurar o baseline daquele escopo. Exemplos para `Players`: UI manager que limpa HUD, roteador de input que reconfigura bindings locais, cache de stats que zera buffers temporários, gerenciador de câmera que recentraliza vista, timers/serviços de encontro que sincronizam estado do jogador.
- **Pipeline intacto**: a interpretação do escopo como resultado não muda a ordem do `WorldLifecycle`; o soft reset segue o mesmo pipeline, apenas com participantes filtrados pelo escopo solicitado.
- **Anti-pattern**: tratar `ResetScope.Players` como “apenas componentes dentro do GameObject Player”.
- **Padrão correto**: tratar `ResetScope.Players` como “tudo que afeta o estado necessário para o player reiniciar corretamente”, mesmo quando o estado vive fora do prefab.

## Onde o registry é criado e como injetar
- Guardrail de criação/ownership: o `WorldLifecycleHookRegistry` é criado e registrado **apenas** pelo `NewSceneBootstrapper` no escopo da cena, junto com `IActorRegistry` e `IWorldSpawnServiceRegistry` (sem `allowOverride`).
- Reuso em segunda chamada: se o provider já tiver o registry para a mesma cena, o bootstrapper loga erro e **não sobrescreve**; apenas reutiliza a instância existente.
- Hooks de cena QA/dev: o bootstrapper garante que os hooks `SceneLifecycleHookLoggerA/B` estejam presentes no `WorldRoot` e os registra no registry sem duplicar.
- Injeção: qualquer componente de cena pode declarar `[Inject] private WorldLifecycleHookRegistry _hookRegistry;` e chamar `DependencyManager.Provider.InjectDependencies(this);` para obter a instância da cena atual.
- Diagnóstico: há log verbose confirmando o registro (ou reuso) do registry na cena.

## Hooks disponíveis
- **`IWorldLifecycleHook`**: permite observar o ciclo de reset de mundo. Pode vir de três fontes na execução: (1) serviços que também implementam `IWorldLifecycleHook`, (2) hooks de cena registrados via `IDependencyProvider.GetAllForScene`, (3) hooks registrados explicitamente no `WorldLifecycleHookRegistry`. A ordem de execução segue exatamente o pipeline determinístico (pré-despawn → actor pré-despawn → despawn → pós-despawn/pré-spawn → spawn → actor pós-spawn → finais) e é logada por fase.
- **`IActorLifecycleHook`**: componentes `MonoBehaviour` anexados a atores. São descobertos pelo orquestrador ao percorrer `Transform` dos atores registrados e executados nas fases de ator (`OnBeforeActorDespawnAsync` e `OnAfterActorSpawnAsync`) preservando a ordem fixa do reset.

### Otimização: cache de Actor hooks por ciclo
- Durante `ResetWorldAsync`, os `IActorLifecycleHook` de cada ator são coletados e ordenados, e agora podem ser reutilizados no mesmo ciclo via cache privado por `Transform`.
- O cache é limpo no `finally` do reset, inclusive em caso de falha, mantendo o escopo estritamente por ciclo.
- A ordenação determinística continua a mesma: (`Order`, `Type.FullName`), assegurando execução estável mesmo com o cache.

### Scene Hooks (WorldLifecycleHookRegistry)
- Hooks de cena (`IWorldLifecycleHook`) podem ser registrados no `WorldLifecycleHookRegistry` criado pelo `NewSceneBootstrapper` e serão executados em todas as fases do reset.
- A ordenação continua determinística: primeiro por `IOrderedLifecycleHook.Order`, depois por `Type.FullName`.
- Exemplo (cena **NewBootstrap**): `SceneLifecycleHookLoggerA` (`Order=0`) e `SceneLifecycleHookLoggerB` (`Order=10`) são adicionados ao `WorldRoot` e registrados no registry, produzindo logs como:
  - `OnBeforeDespawn phase started (hooks=2)` seguido de `OnBeforeDespawn execution order: SceneLifecycleHookLoggerA(order=0), SceneLifecycleHookLoggerB(order=10)` e as mensagens `[QA]  -> OnBeforeDespawnAsync` de cada hook.
  - `OnAfterSpawn phase started (hooks=2)` com a mesma ordem e logs `[QA]  -> OnAfterSpawnAsync`, validando que ambos os hooks executam e preservam a ordem.

### QA / Validação de Ordenação (hooks de cena)
- Hooks de validação: `SceneLifecycleHookLoggerA` (`Order=0`) e `SceneLifecycleHookLoggerB` (`Order=10`).
- Expectativa de log em cada fase com hooks: `execution order: SceneLifecycleHookLoggerA(order=0), SceneLifecycleHookLoggerB(order=10)` seguido das mensagens `[QA]  -> OnBeforeDespawnAsync` ou `[QA]  -> OnAfterSpawnAsync` confirmando execução na ordem.
- O fluxo padrão (DummyActor + actor hooks) permanece intacto; os hooks de cena apenas instrumentam a ordem determinística.

### Ordenação determinística
- **Hooks de mundo (`IWorldLifecycleHook`)**: ordenados por `Order` quando o hook implementa `IOrderedLifecycleHook` (default = 0) e, como desempate, por `Type.FullName` com comparação ordinal.
- **Hooks de ator (`IActorLifecycleHook`)**: coletados via `GetComponentsInChildren()` em cada ator, mas a execução não depende da ordem retornada; antes de executar, a lista é ordenada por (`Order`, `Type.FullName`) com o mesmo comparador usado nos hooks de mundo.
- **Observação**: o critério (`Order`, `Type.FullName`) garante que a ordem de execução permaneça estável entre resets e ambientes.

## Do / Don't
- **Do:** criar o `WorldLifecycleHookRegistry` apenas no `NewSceneBootstrapper` e reutilizá-lo via injeção na cena.
- **Do:** manter a ordem do reset exatamente como: Acquire Gate → Hooks pré-despawn → Actor hooks pré-despawn → Despawn → Hooks pós-despawn/pré-spawn → Spawn → Actor hooks pós-spawn → Hooks finais → Release Gate.
- **Don't:** instanciar ou registrar manualmente o registry no controller ou no orquestrator; eles apenas consomem a instância de cena.
- **Don't:** alterar a sequência das fases do reset ou mover responsabilidades entre bootstrapper, controller e orquestrator.
- **Do:** se um soft reset não faz nada, verifique se os scopes foram passados e se existem participantes registrados (`IResetScopeParticipant`) para aquele escopo.

## Como registrar um hook no registry
1. Garanta que o componente tenha recebido injeção de dependências na cena e registre no `Awake` (o registry já existe porque nasceu no bootstrapper):
   ```csharp
   public sealed class MySceneHookInstaller : MonoBehaviour
   {
       [Inject] private WorldLifecycleHookRegistry _hookRegistry;

       private void Awake()
       {
           DependencyManager.Provider.InjectDependencies(this);
           _hookRegistry.Register(new AnalyticsResetHook());
       }
   }
   ```
2. Implemente o hook herdando de `IWorldLifecycleHook`:
   ```csharp
   public sealed class AnalyticsResetHook : IWorldLifecycleHook
   {
       public Task OnBeforeDespawnAsync() => Task.CompletedTask;
       public Task OnAfterDespawnAsync() => Task.CompletedTask;
       public Task OnBeforeSpawnAsync() => Task.CompletedTask;
       public Task OnAfterSpawnAsync() => Task.CompletedTask;
   }
   ```
Hooks registrados aqui serão executados em todas as fases, após os hooks de spawn services e hooks de cena.

## IOrderedLifecycleHook
- Interface opcional que adiciona a propriedade `Order` para controlar prioridade.
- Aplica-se a **hooks de mundo** e a **hooks de ator**; quem não implementa recebe o valor padrão 0 e ainda participa da ordenação determinística por (`Order`, `Type.FullName`).

## Como criar um hook em ator
Use `ActorLifecycleHookBase` para componentes por ator:
```csharp
public sealed class NotifyHUDHook : ActorLifecycleHookBase
{
    public override Task OnAfterActorSpawnAsync()
    {
        // Ex.: sinalizar HUD local que o ator spawnou.
        return Task.CompletedTask;
    }

    public override Task OnBeforeActorDespawnAsync()
    {
        // Ex.: limpar indicadores antes do despawn.
        return Task.CompletedTask;
    }
}
```
Anexe o componente ao GameObject do ator. O orquestrador irá chamá-lo automaticamente nas fases de ator durante o reset.

## QA: como reproduzir e o que esperar
1. Abra a cena **NewBootstrap** no Editor.
2. No `WorldLifecycleController`, use o menu de contexto `QA/Reset World Now` para disparar um reset determinístico manual ou
   o menu `QA/Soft Reset Players Now` para disparar apenas o escopo `Players` (equivalente a chamar `ResetPlayersAsync("QA/PlayersSoftReset")`).
3. Espere ver no Console:
   - Log verbose confirmando `WorldLifecycleHookRegistry registrado para a cena '<scene>'` vindo do `NewSceneBootstrapper`.
   - Logs de início/fim do reset e de cada fase (gate acquired/released, hooks, spawn/despawn) emitidos pelo `WorldLifecycleOrchestrator`.
4. Não deve haver logs dizendo que o registry foi criado pelo controller; ele apenas consome via DI.

## Validation Contract (Baseline)

### Hard Reset (`ResetWorldAsync`)
- `[Gate] Acquire token='WorldLifecycle.WorldReset'` (ou o token efetivamente usado para hard reset) antes de iniciar qualquer fase.
- Fases em ordem, com logs de início e fim: `OnBeforeDespawn`, `OnBeforeActorDespawn`, `Despawn`, `OnAfterDespawn`, `OnBeforeSpawn`, `Spawn`, `OnAfterActorSpawn`, `OnAfterSpawn`.
- `[Gate] Released` ao final do ciclo.
- `World Reset Completed` confirmando fechamento do pipeline.

### Soft Reset (`Players`)
- Token esperado: `SimulationGateTokens.SoftReset` adquirido antes do reset e liberado ao final.
- Apenas `IResetScopeParticipant` executa nesta fase, seguindo ordenação determinística por `(scope, order, typename)`.
- Logs do `PlayersResetParticipant` indicando start/end com o `ResetContext` completo e restauração do baseline funcional do player, mesmo quando múltiplos participantes externos são chamados (HUD, input router, câmera, caches, serviços).
- Fases e serviços podem aparecer como pulados por filtro de escopo (`phase skipped (hooks=0)`, `service skipped by scope filter`); isso é esperado quando não há participantes para o escopo solicitado.

### WorldLifecycleRuntimeDriver (`ScenesReady`)
- Recebe `SceneTransitionScenesReadyEvent` e calcula `contextSignature`.
- Garante idempotência ignorando resets com a mesma assinatura já processada.
- Adquire o gate `SimulationGateTokens.Loading` durante o reset e libera ao final.

Observação: ao migrar legado, qualquer integração deve preservar este contrato; se quebrar, rollback/ajuste do adaptador.

## Troubleshooting: QA/Testers e Boot Order
- **Sintomas (console):**
  - QA/tester não encontra `WorldLifecycleHookRegistry` / `IActorRegistry`.
  - Reset não dispara ou aborta cedo.
  - Logs iniciais “de erro” na criação do QA/Tester ao entrar em Play Mode.
  - Mensagens de “registries inexistentes” logo no `Awake` do tester.
- **Causa provável:**
  - `NewSceneBootstrapper` ausente na cena ou executando depois do QA/tester.
  - QA rodando no `Awake()` sem lazy injection/retry, antes do bootstrap.
- **Checklist de ação (3 passos):**
  1. Garantir `NewSceneBootstrapper` presente na cena e ativo.
  2. Garantir que QA/testers usem lazy injection + retry curto + timeout (padrão já descrito).
  3. Se ainda falhar, abortar com mensagem acionável apontando ausência/ordem errada do bootstrapper.
- **Nota sobre `NEWSCRIPTS_MODE`:** quando ativo, inicializadores/bootstraps podem ser ignorados por design (modo de desenvolvimento); isso pode parecer falha de QA, mas não é bug de runtime.

## Boot order & Dependency Injection timing (Scene scope)
- Os serviços de cena (`IActorRegistry`, `IWorldSpawnServiceRegistry`, `WorldLifecycleHookRegistry`) são registrados pelo `NewSceneBootstrapper` durante o bootstrap da cena. Outros componentes não devem criar registries próprios nem assumir que eles existem antes do bootstrapper executar.
- Componentes que consomem esses serviços devem evitar injeção no `Awake()` quando a ordem de execução ainda não garantiu o bootstrapper; prefira `Start()` ou injeção lazy com retry curto.
- QA testers (ex.: `WorldLifecycleQATester`) adotam injeção lazy com retry e timeout: aguardam alguns frames/tempo curto se o registry ainda não existe, abortando com mensagem clara se o bootstrapper não rodou. Isso evita falsos negativos em cenas novas.
- O `WorldLifecycleOrchestrator` assume que dependências já foram resolvidas pelo fluxo de bootstrap; ele não corrige ordem de inicialização e não registra serviços de cena.

## Migration Strategy (Legacy → NewScripts)
- Consulte a ADR [ADR-0001 — Migração incremental do Legado para o NewScripts](../../Docs/ADR/ADR-0001-NewScripts-Migracao-Legado.md) para a estratégia detalhada.
- Abordagem oficial: Portas e Adaptadores, escolhendo entre Refazer/Integrar/Adaptar cada componente legado (Player primeiro).
- Guardrails fixos: NewScripts não referencia concreto do legado fora de adaptadores; pipeline determinístico com gate sempre ativo.
- WorldLifecycleHookRegistry nasce apenas no `NewSceneBootstrapper`; controller/orchestrator apenas consomem.
- Soft reset é opt-in por escopo (`IResetScopeParticipant` + `ResetContext.Scopes`); não há participantes implícitos globais.
- `ISimulationGateService` continua sendo o gate de simulação para resets e transições.
- Plano incremental: congelar baseline de logs, definir fronteira do Player, criar adaptador mínimo, migrar capacidades uma a uma.
- Validação: hard reset mantém ordem e idempotência; soft reset `Players` roda só participantes de escopo na ordem determinística.
- Consequência esperada: isolamento do legado com rollback simples, ao custo de adaptadores temporários e disciplina de fronteiras.

## Baseline Validation Contract
- Checklist detalhado: [Docs/QA/WorldLifecycle-Baseline-Checklist](../../Docs/QA/WorldLifecycle-Baseline-Checklist.md).
- Soft reset (`Players`) executa apenas `IResetScopeParticipant` filtrado por `ResetContext.Scopes` e mantém binds/registries da cena intactos.
- Hard reset roda o ciclo completo do WorldLifecycle (gate acquire → hooks pré/pós-despawn → participantes de escopo → hooks pré/pós-spawn → gate release).
