# ADR-0011 – WorldDefinition multi-ator na GameplayScene (sem regressão)

**Status:** Proposto
**Escopo:** `WorldLifecycle` (NewScripts), `GameplayScene`, `WorldDefinition`, serviços de spawn
**Data:** 2025-12-28

## 1. Contexto

O pipeline de produção atual está estável:

* Startup → Menu (`profile='startup'`) com:

    * `SceneTransitionService` + Fade + LoadingHUD.
    * `WorldLifecycleRuntimeCoordinator` fazendo **skip** do reset em perfis `startup/frontend`.
    * `GameReadinessService` + `SimulationGateService` controlando gate + `GameplayReady`.
    * `InputModeService` setando `FrontendMenu`.
    * `GameLoop` entrando em `Ready`.

* Menu → Gameplay (`profile='gameplay'`) com:

    * `GameNavigationService` disparando `SceneTransitionService.TransitionAsync`.
    * `WorldLifecycleRuntimeCoordinator` disparando hard reset na `GameplayScene`.
    * `WorldLifecycleOrchestrator` rodando fases determinísticas (`OnBeforeDespawn/Despawn/OnAfterDespawn/OnBeforeSpawn/Spawn/OnAfterSpawn + OnAfterActorSpawn`).
    * `PlayerSpawnService` único serviço de spawn configurado no `WorldDefinition` da `GameplayScene`.
    * `InputModeService` setando `Gameplay` ao final da transição.
    * `InputModeSceneFlowBridge` chamando `GameLoop.RequestStart()` ao receber `SceneTransitionCompletedEvent(profile='gameplay')`.
    * `GameLoop` indo de `Ready` para `Playing`, liberando ações via `NewScriptsStateDependentService`.

Objetivo agora: **evoluir o `WorldDefinition` da GameplayScene para suportar múltiplos atores (Player + Planet + Eater/NPC/etc.)**, **sem quebrar o comportamento já validado** de:

* SceneFlow (startup/frontend/gameplay).
* WorldLifecycle reset determinístico.
* GameLoop (Boot → Ready → Playing ↔ Paused).
* Gating (`SimulationGateService` + `GameReadinessService`).
* InputMode (FrontendMenu/Gameplay/PauseOverlay).

## 2. Problema

Atualmente:

* A `GameplayScene` tem apenas **um** serviço de spawn configurado (Player).
* O reset da cena funciona, mas está validando apenas o caso trivial: spawn e despawn de um único ator.
* Para o jogo real, precisamos spawnar vários tipos de atores (Planets, Players, Eaters, NPCs, etc.) com:

    * Ordem determinística.
    * Reset consistente (mesmo resultado em toda transição Menu → Gameplay, Retry, etc.).
    * Sem alterar o contrato já estabelecido do WorldLifecycle, SceneFlow e GameLoop.

Qualquer alteração em spawn/reset que mexa na infraestrutura central pode introduzir regressões difíceis de detectar se não forem limitadas a **camadas de configuração (WorldDefinition)** e **novos serviços isolados**.

## 3. Decisão

1. **WorldDefinition multi-ator apenas via configuração e novos serviços**

    * A evolução para múltiplos atores será feita:

        * Alterando **apenas** o `WorldDefinition` da `GameplayScene`.
        * Criando **novos** serviços de spawn (ex.: `PlanetSpawnService`, `EaterSpawnService`, etc.).
    * O `WorldLifecycleOrchestrator`, `WorldLifecycleController` e os contratos de `IWorldSpawnService` **não** serão alterados nesta fase.

2. **Ordem determinística explícita de spawn por serviço**

    * A ordem de spawn será garantida pela ordem de registro dos serviços (já suportada pelo `WorldSpawnServiceRegistry`).
    * O `WorldDefinition` da `GameplayScene` deve documentar e refletir a ordem pretendida, por exemplo:

        1. Planets
        2. Player
        3. NPCs/Eaters/etc.

3. **Invariantes de regressão** (não podem mudar nesta fase):

    * SceneFlow:

        * Perfis `startup/frontend/gameplay` mantêm a mesma semântica.
        * `WorldLifecycleRuntimeCoordinator` continua:

            * Fazendo **skip** do reset em `startup/frontend`.
            * Disparando hard reset em `gameplay` após `ScenesReady`.
    * WorldLifecycle:

        * Fases do `WorldLifecycleOrchestrator` permanecem iguais (mesmas fases, mesma assinatura dos métodos).
        * `WorldLifecycleController` continua a:

            * Coletar serviços de spawn da cena.
            * Invocar o orquestrador com os serviços registrados.
    * GameLoop:

        * Estados e transições `Boot → Ready → Playing ↔ Paused` permanecem inalterados.
        * `InputModeSceneFlowBridge` continua usando `SceneTransitionCompletedEvent(profile='gameplay')` para chamar `GameLoop.RequestStart()`.
    * InputMode e Gating:

        * `InputModeService` e `NewScriptsStateDependentService` continuam com as mesmas regras.
    * Logging e debug:

        * Mensagens de log atuais não serão removidas, apenas complementadas se necessário.

4. **Sem mudança estrutural em PlayerSpawnService nesta etapa**

    * `PlayerSpawnService` permanece como está (contrato, lógica principal).
    * Quaisquer ajustes necessários para convivência com outros serviços devem ser:

        * Incrementais.
        * Documentados neste ADR (na seção de “Impactos”).
        * Validados com log equivalente ao atual (compare antes/depois).

5. **Evolução incremental, orientada por WorldDefinition**

    * A inclusão de novos atores no reset será guiada por **edições incrementais** do `WorldDefinition` da `GameplayScene`.
    * Cada nova entrada (e serviço correspondente) será introduzida, testada e logada **antes** de adicionar a próxima.

## 4. Detalhamento técnico

### 4.1. WorldDefinition – GameplayScene

* Continuar usando o mesmo asset `WorldDefinition` já referenciado pelo `NewSceneBootstrapper` na `GameplayScene`.

* Estrutura (conceitual):

    * Lista de entradas de spawn (já existente) com campos do tipo:

        * `Enabled` (bool)
        * `Kind` (enum ou similar: `Player`, `Planet`, `Eater`, `NPC`, etc.)
        * `Prefab`
        * `Order` ou equivalente (caso exista; se não existir, a ordem da lista define a prioridade).

* Regras para GameplayScene:

    * Deve haver **no mínimo um Player** por definição de jogo.
    * Cada novo tipo de ator adicionado deve possuir um serviço de spawn associado (registrado no `WorldSpawnServiceRegistry` da cena).

### 4.2. Serviços de spawn

* Serviços seguem o contrato existente (ex.: `IWorldSpawnService` ou interface equivalente já usada pelo `PlayerSpawnService`):

    * `Task SpawnAsync(IWorldSpawnContext context, IActorRegistry registry, CancellationToken token)`
    * `Task DespawnAsync(IWorldSpawnContext context, IActorRegistry registry, CancellationToken token)`
    * Qualquer outro membro já padronizado.

* Novos serviços (exemplos):

    * `PlanetSpawnService`
    * `EaterSpawnService`
    * `NpcSpawnService`

* Responsabilidades por serviço:

    * Saber:

        * Para qual `Kind` responde.
        * Como localizar instâncias/prefabs no `WorldDefinition`.
    * Registrar os atores criados no `IActorRegistry` com IDs determinísticos (mesmo padrão do Player).

### 4.3. Orquestração e Reset

* `WorldLifecycleOrchestrator` já está responsável por:

    * Ordenar serviços de spawn conforme `WorldSpawnServiceRegistry`.
    * Chamar `DespawnAsync` e `SpawnAsync` numa ordem determinística.
    * Executar hooks `OnBeforeDespawn/OnAfterDespawn/OnBeforeSpawn/OnAfterSpawn/OnAfterActorSpawn`.

* Para múltiplos atores:

    * Nenhum ajuste estrutural é feito no orquestrador nesta ADR.
    * A ordem do reset passa a depender da **ordem de registro dos serviços**, que é configurada via `WorldDefinition` e fábrica de serviços.

## 5. Não-decisões (fora de escopo desta ADR)

* Novos estados do GameLoop (`GameOver`, `Victory`, `Restart`) – serão tratados em ADR separado.
* Qualquer alteração no SceneFlow (novos perfis, mudança de semântica de `startup/frontend/gameplay`).
* Qualquer mudança na semântica do `SimulationGateService` ou do `GameReadinessService`.
* Qualquer alteração no sistema de InputMode além da troca de mapas já existente (`FrontendMenu`, `Gameplay`, `PauseOverlay`).

## 6. Plano de implementação em micro-etapas (anti-regressão)

1. **Baseline QA (já feito, mas documentar):**

    * Registrar snapshot de log de:

        * Startup → Menu (profile `startup`).
        * Menu → Gameplay (profile `gameplay`).
        * Gameplay: Pause, Resume, ExitToMenu → Menu.
    * Guardar como “Baseline Single-Actor”.

2. **Etapa 1 – PlanetSpawnService isolado (desligado no WorldDefinition):**

    * Criar `PlanetSpawnService` com contrato correto.
    * Integrar com `WorldSpawnServiceRegistry` e `WorldDefinition`, mas manter entries `Enabled=false` (ou não referenciadas).
    * Validar:

        * Projeto compila.
        * Nenhum log novo de erro em runtime.
    * Nenhuma mudança de comportamento esperada (como se o serviço não existisse).

3. **Etapa 2 – Ativar Planet no WorldDefinition (ordem antes do Player):**

    * Habilitar entrada `Planet` no `WorldDefinition` da `GameplayScene`.
    * Garantir ordem:

        1. `PlanetSpawnService`
        2. `PlayerSpawnService`
    * Validar:

        * Menu → Gameplay:

            * Planets instanciados antes do Player.
            * `ActorRegistry` com contagem correta.
        * Reset subsequente (por nova transição para Gameplay) respawna Planets e Player de forma determinística.
    * Comparar logs:

        * Verificar que as mensagens antigas aparecem iguais.
        * Novos logs restringem-se a lines de Planet/ActorRegistry adicionais.

4. **Etapa 3 – Próximo ator (ex.: EaterSpawnService):**

    * Repetir o padrão:

        * Criar serviço novo.
        * Ativar entry no `WorldDefinition` com ordem declarada (ex.: Planet → Player → Eater).
        * Validar fluxo completo e comparar logs com baseline + diffs esperados.

5. **Etapa 4 – Auditoria Codex (em massa):**

    * Usar Codex apenas para:

        * Garantir que todos os serviços de spawn:

            * Registram corretamente no `WorldSpawnServiceRegistry`.
            * Respeitam o contrato de reset.
            * Mantêm logs consistentes com o padrão já usado.
        * Ajustar de forma bulk eventuais inconsistências de nomenclatura / namespace.

## 7. Riscos e mitigação

* **Risco:** alteração acidental em `WorldLifecycleOrchestrator` ou `WorldLifecycleController`.

    * **Mitigação:**

        * Este ADR proíbe mudanças estruturais nessas classes nesta fase.
        * Qualquer alteração nelas deve ser acompanhada de novo ADR.

* **Risco:** ordem de spawn instável entre builds/branches.

    * **Mitigação:**

        * Fixar ordem explicitamente no `WorldDefinition` (lista ordenada).
        * Garantir que a fábrica de `WorldSpawnServiceRegistry` preserve essa ordem.

* **Risco:** regressão silenciosa no GameLoop (não iniciar/pausar corretamente).

    * **Mitigação:**

        * Reexecutar o mesmo fluxo de QA (Startup → Menu → Gameplay → Pause → Resume → ExitToMenu → Menu → Gameplay) após cada nova entrada no `WorldDefinition`.
        * Comparar logs com baseline + diffs esperados (apenas logs dos novos serviços/atores).

## 8. Impacto em QA e documentação

* **QA:**

    * Criar/atualizar um doc de QA rápido (ex.: `QA/WorldLifecycle-Gameplay-MultiActor.md`) com:

        * Cenários mínimos para validar:

            * Spawn determinístico de todos os atores.
            * Reset após nova transição Gameplay.
            * Interação com Pause/ExitToMenu.

* **Docs:**

    * Atualizar:

        * `WorldLifecycle.md`:

            * Seção de “WorldDefinition/GameplayScene” com exemplo de multiple entries (Planet + Player + outro).
        * `CHANGELOG-docs.md`:

            * Registrar a introdução de `WorldDefinition` multi-ator para GameplayScene e os novos serviços de spawn.

---
## Escopo do Eater (NewScripts) e política de legado

- A cena **canônica de gameplay do NewScripts** é `Assets/_ImmersiveGames/Scenes/GameplayScene.unity`.
- A cena `Assets/_ImmersiveGames/Scenes/GameplayScene 1.unity` é um **backup legado** e deve ser usada apenas como referência/consulta.
    - Nenhum fluxo de produção ou sistema do NewScripts pode depender desta cena.
    - Nenhum prefab ou campo serializado em assets do NewScripts deve apontar explicitamente para objetos desta cena.

### Eater no NewScripts

- O ator **oficial** do Eater no NewScripts será um prefab próprio (ex.: `Assets/_ImmersiveGames/NewScripts/Infrastructure/Prefabs/Eater_NewScripts.prefab`) contendo:
    - `EaterActor` (implementação NewScripts de `IActor`/`IActorKindProvider`).
    - Componentes de comportamento/AI/recursos específicos da nova linha (quando forem criados).
- O pipeline de spawn do Eater no NewScripts deve usar **apenas**:
    - `WorldDefinition` + `WorldSpawnServiceKind.Eater`.
    - `EaterSpawnService` + `EaterActor` + `ActorRegistry` + `IUniqueIdFactory`.
- É **proibido** usar diretamente os tipos de domínio legado como parte do fluxo de spawn/vida do Eater no NewScripts:
    - `EaterDomain`, `GameplayManager.WorldEater`, `EaterMaster`, `EaterBehavior` e seus partials.
    - Esses tipos existem apenas como **referência de design** para futura reimplementação.

### Política de migração

- A migração do comportamento completo do Eater (desejos, FSM, detecção, animação, UI) será feita como **reimplementação** na pasta `NewScripts`, tomando os scripts de `EaterSystem` como guia conceitual, e não via adapters ou bridges diretos.
- Enquanto a migração não for feita:
    - O Eater do NewScripts pode ter um comportamento mínimo (ex.: movimento simples, placeholder), mas sempre respeitando o pipeline de `WorldLifecycle` (spawn/despawn/reset) e o `IStateDependentService`.
