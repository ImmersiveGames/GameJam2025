Perfeito. Vamos tratar isso como um mini-projeto com fases bem definidas.

Vou montar um **plano de refatoração progressiva**, onde cada etapa tem:

* Objetivo
* O que implementar
* Como validar (o que você precisa ver no Play para considerar pronto)

Depois, na próxima mensagem, já podemos começar pela Etapa 1 implementando código.

---

## Visão geral das etapas

1. **Etapa 1 – Infra de cenas organizada**
   Separar claramente o “loader baixo nível” do resto (sem mexer ainda em GameManager/fade).
2. **Etapa 2 – Núcleo `ISceneLoader` 2.0**
   Ter um serviço limpo que apenas carrega/descarrega cenas (sem fade, sem regras de jogo).
3. **Etapa 3 – Estado de cenas + contexto de transição**
   Introduzir `SceneState` e `SceneTransitionContext` para formalizar “grupo de cenas”.
4. **Etapa 4 – `SceneTransitionService` (pipeline completo)**
   Orquestrar fade + load + unload + eventos em uma classe dedicada.
5. **Etapa 5 – Integração com GameManager / testers**
   Substituir chamadas antigas pelo novo fluxo de transição.
6. **Etapa 6 – Primeira versão de `SceneSetup` (ScriptableObject)**
   Transformar os “grupos de cenas” em assets configuráveis.

Abaixo, cada etapa em detalhe, com implementação + validação.

---

## Etapa 1 – Organizar a infra de cenas

### Objetivo

Criar o “terreno” para a refatoração: tudo relacionado a scene management em um lugar claro, com a intenção explícita de separar infra de domínio.

### Implementação

1. **Namespaces e pastas**

   Criar (ou ajustar) uma estrutura parecida com:

    * `_ImmersiveGames.Scripts.SceneManagement`

        * `Core` (loader baixo nível)
        * `Transition` (futuro orquestrador, contextos)
        * `Configs` (futuros SOs)
        * `Events` (eventos de transição)

2. **Classificação dos arquivos atuais**

   Sem mudar comportamento ainda, só “mapear”:

    * O que fala diretamente com `SceneManager` (carrega/descarrega cenas) → mover para `SceneManagement/Core`.
    * O que mistura fade + load + unload → marcar para ser reescrito como parte do futuro `SceneTransitionService`.
    * Testers (`SceneLoaderTester`, etc.) → podem ficar em uma pasta `Tests` / `Debug` dentro de `SceneManagement`.

3. **Pequenos ajustes de naming (opcional agora)**

    * Manter nomes atuais, mas já anotar o plano:

        * Ex.: `SceneLoaderService` será redividido em:

            * `SceneLoader` (baixo nível)
            * `SceneTransitionService` (pipeline de transição)

   Nesta etapa, não precisamos alterar código lógico – só mover/organizar para ficar claro onde mexer nas próximas fases.

### Validação

Para considerar a Etapa 1 concluída, verifique:

* [ ] Todos os scripts que usam `SceneManager.LoadScene`, `UnloadSceneAsync`, etc. estão dentro de `SceneManagement/Core`.
* [ ] Você consegue abrir o projeto e, olhando apenas a pasta `SceneManagement`, identificar:

    * “Quem é infra de cenas” (Core)
    * “Quem será responsável por transição” (Transition – mesmo que ainda vazio)
    * “Quem são testers” (Test/Debug)
* [ ] O jogo ainda roda como antes (mesmos problemas de hoje, mas nada quebrou só por reorganizar).

---

## Etapa 2 – Definir o núcleo `ISceneLoader` 2.0

### Objetivo

Ter um serviço único, simples, responsável apenas por carregar/descarregar cenas. Sem fade, sem game flow, sem decidir o que é “menu” ou “gameplay”.

### Implementação

1. **Criar a interface de infra**

   Em `SceneManagement/Core/ISceneLoader.cs` algo nessa linha:

   ```csharp
   public interface ISceneLoader
   {
       Task LoadSceneAsync(string sceneName, LoadSceneMode mode);
       Task UnloadSceneAsync(string sceneName);
       bool IsSceneLoaded(string sceneName);
       Scene GetSceneByName(string sceneName);
   }
   ```

   (Depois, se precisar, podemos ajustar a assinatura conforme suas necessidades.)

2. **Criar a implementação `SceneLoader`**

   Em `SceneManagement/Core/SceneLoader.cs`:

    * Implementa a interface usando `SceneManager`:

        * `LoadSceneAsync` → `SceneManager.LoadSceneAsync(sceneName, mode)` + await.
        * `UnloadSceneAsync` → `SceneManager.UnloadSceneAsync(sceneName)` + await.
        * `IsSceneLoaded` → varre `SceneManager.sceneCount`.
        * `GetSceneByName` → wrapper de `SceneManager.GetSceneByName`.

    * Logs:

        * `[SceneLoader] Carregando cena X (mode=Additive/Single)`.
        * `[SceneLoader] Cena X carregada`.
        * `[SceneLoader] Descarregando cena X`.
        * `[SceneLoader] Cena X descarregada`.
        * Warnings quando:

            * Tentar carregar cena já carregada.
            * Tentar descarregar cena não carregada.

    * Nenhum conhecimento de:

        * Fade
        * GameManager
        * “cena ativa” (não decide isso; apenas carrega/descarrega).

3. **Integrar no DI**

    * Registrar `ISceneLoader` → `SceneLoader` no seu sistema de DI (provavelmente em algum bootstrap).

4. **Manter o código atual funcionando**

    * Por enquanto, você pode:

        * Deixar o antigo `SceneLoaderService` usando **internamente** o novo `ISceneLoader`.
        * Ou continuar chamando `SceneManager` diretamente, mas ir preparando para trocar.

### Validação

Para considerar a Etapa 2 concluída:

* [ ] Existe uma interface `ISceneLoader` e uma implementação concreta `SceneLoader`.

* [ ] Você consegue, em uma cena de teste, escrever um script simples:

    * Tecla `A`: `ISceneLoader.LoadSceneAsync("GameplayScene", LoadSceneMode.Additive)`
    * Tecla `D`: `ISceneLoader.UnloadSceneAsync("GameplayScene")`

* [ ] Em Play:

    * Ao apertar `A`, a cena `GameplayScene` entra em modo aditivo sem nenhum fade.
    * Ao apertar `D`, a cena `GameplayScene` é descarregada.
    * Os logs mostram claramente o fluxo (carregar/descarregar) sem mensagens relacionadas a fade ou transição.

* [ ] Nenhum conhecimento de “menu”, “UI”, “reset” existe dentro de `SceneLoader`.

---

## Etapa 3 – Estado de cenas (`SceneState`) e contexto de transição (`SceneTransitionContext`)

### Objetivo

Formalizar a ideia de “grupo de cenas carregadas” e “plano para trocar grupos”, preparando o caminho para os futuros ScriptableObjects.

### Implementação

1. **Criar `SceneState`**

   Em `SceneManagement/Transition/SceneState.cs`:

    * Mantém:

        * `HashSet<string> LoadedScenes`
        * `string ActiveSceneName`

    * Inicialmente, pode ser apenas um snapshot obtido via `SceneManager` quando necessário:

        * Helper estático: `SceneState FromSceneManager()`.

    * Depois, podemos evoluir para manter isso em sincronia com o `SceneLoader`.

2. **Criar `SceneTransitionContext`**

   Em `SceneManagement/Transition/SceneTransitionContext.cs`:

   ```csharp
   public struct SceneTransitionContext
   {
       public IReadOnlyList<string> ScenesToLoad;
       public IReadOnlyList<string> ScenesToUnload;
       public string TargetActiveScene;
       public bool UseFade;
   }
   ```

    * Isso representa **exatamente** o que você queria: grupos para carregar e descarregar, mais a cena que se tornará ativa.

3. **Criar um planner simples (opcional nesta etapa, mas recomendado)**

   Em `SceneManagement/Transition/ISceneTransitionPlanner.cs`:

   ```csharp
   public interface ISceneTransitionPlanner
   {
       SceneTransitionContext BuildContext(
           SceneState currentState,
           IReadOnlyList<string> targetScenes,
           string targetActiveScene,
           bool useFade);
   }
   ```

   Implementação inicial:

    * `ScenesToLoad` = `targetScenes` que **não** estão em `currentState.LoadedScenes`.
    * `ScenesToUnload` = `currentState.LoadedScenes` que **não** estão em `targetScenes`.
    * `TargetActiveScene`:

        * Usa o parâmetro, se não for vazio.
        * Caso contrário, pega a primeira cena de `targetScenes`.

### Validação

Para considerar a Etapa 3 concluída:

* [ ] Você tem um tipo `SceneState` que, quando chamado algo como `SceneState.FromSceneManager()`, lista corretamente:

    * Quais cenas estão carregadas.
    * Qual é a cena ativa.
* [ ] Você consegue chamar o `ISceneTransitionPlanner.BuildContext` (mesmo que num script de teste) com:

    * `currentState` contendo, por exemplo, apenas `MenuScene`.
    * `targetScenes = ["GameplayScene", "UIScene"]`.
* [ ] Verificar via log que:

    * `ScenesToLoad = ["GameplayScene", "UIScene"]`.
    * `ScenesToUnload = ["MenuScene"]`.
    * `TargetActiveScene = "GameplayScene"` (ou o que você tiver definido).
* [ ] Isso reflete exatamente a sua ideia: um grupo de cenas a carregar, um grupo a descarregar, e a cena que será ativada.

---

## Etapa 4 – `SceneTransitionService`: pipeline completo da transição

### Objetivo

Implementar uma classe dedicada a aplicar o pipeline:

> FadeIn → Load novas cenas (aditivas) → SetActiveScene → Unload antigas → FadeOut

usando `ISceneLoader`, `IFadeService` e `SceneTransitionContext`, além de disparar eventos.

### Implementação

1. **Definir interface**

   Em `SceneManagement/Transition/ISceneTransitionService.cs`:

   ```csharp
   public interface ISceneTransitionService
   {
       Task RunTransitionAsync(SceneTransitionContext context);
       bool IsTransitionInProgress { get; }
   }
   ```

2. **Implementar `SceneTransitionService`**

   Em `SceneManagement/Transition/SceneTransitionService.cs`:

    * Dependências via DI:

        * `ISceneLoader`
        * `IFadeService`
        * EventBus (ou similar)

    * Campo interno `_transitionInProgress`.

    * Pipeline de `RunTransitionAsync`:

        1. Se `_transitionInProgress`, loga warning e retorna.
        2. `_transitionInProgress = true`.
        3. Dispara `SceneTransitionStartedEvent`.
        4. Se `context.UseFade` → `FadeInAsync`.
        5. Carrega `ScenesToLoad` usando `ISceneLoader.LoadSceneAsync(..., Additive)`.
        6. Ativa `TargetActiveScene` via `SceneManager.SetActiveScene(...)`.
        7. Descarrega `ScenesToUnload` via `ISceneLoader.UnloadSceneAsync`.
        8. Atualiza `SceneState` (se tiver um storage central).
        9. Dispara `SceneTransitionScenesReadyEvent`.
        10. Se `context.UseFade` → `FadeOutAsync`.
        11. Dispara `SceneTransitionCompletedEvent`.
        12. `_transitionInProgress = false`.

3. **Criar eventos de transição**

   Em `SceneManagement/Events/SceneTransitionEvents.cs`:

    * `SceneTransitionStartedEvent` (carrega context / nomes de cena).
    * `SceneTransitionScenesReadyEvent`.
    * `SceneTransitionCompletedEvent` (inclui `ActiveSceneName`).

### Validação

Para considerar a Etapa 4 concluída:

* [ ] Você consegue, em um tester simples, montar `SceneTransitionContext` (manualmente) e chamar `SceneTransitionService.RunTransitionAsync(context)` para:

    * `MenuScene` → `GameplayScene + UIScene`.
* [ ] Em Play:

    * A tela escurece (FadeIn).
    * As novas cenas são carregadas (additive).
    * A cena ativa muda para a que você definiu (`TargetActiveScene`).
    * A(s) cena(s) antiga(s) são descarregadas, incluindo o Menu.
    * A tela clareia (FadeOut).
* [ ] Os eventos:

    * São disparados nas fases esperadas (você pode ter um listener de debug logando cada evento).
* [ ] `_transitionInProgress` impede transições concorrentes (se você apertar duas teclas rápido, só a primeira dispara de fato).

---

## Etapa 5 – Integração com GameManager e testers

### Objetivo

Parar de usar o “rascunho” atual para transições e passar a usar `SceneTransitionService` de forma consistente no fluxo real do jogo.

### Implementação

1. **Atualizar `SceneLoaderTester`**

    * Em vez de chamar diretamente o antigo `SceneLoaderService.LoadScenesWithFadeAsync`, montar contextos:

        * Tecla 1 (ir para menu):

            * `targetScenes = ["MenuScene"]`
            * `targetActiveScene = "MenuScene"`
        * Tecla 2 (ir para gameplay):

            * `targetScenes = ["GameplayScene", "UIScene"]`
            * `targetActiveScene = "GameplayScene"`

    * Usar `ISceneTransitionPlanner` → `SceneTransitionContext` → `ISceneTransitionService.RunTransitionAsync`.

2. **Atualizar `GameManager`**

    * Remover dependência direta de qualquer loader “antigo”.

    * Introduzir dependência de um serviço de alto nível, por exemplo:

      ```csharp
      public interface IGameSceneFlowService
      {
          Task GoToMenuAsync();
          Task GoToGameplayAsync();
          Task ResetGameAsync();
      }
      ```

    * Implementar `GameSceneFlowService` usando `SceneState` + `ISceneTransitionPlanner` + `ISceneTransitionService`.

3. **Deprecar (gradualmente) o código antigo de transição**

    * Deixar marcado com `[Obsolete]` ou comentários quais métodos não devem mais ser usados.
    * Remover chamadas a eles aos poucos.

### Validação

Para considerar a Etapa 5 concluída:

* [ ] Tecla 1 e 2 no `SceneLoaderTester` usam o novo pipeline e funcionam corretamente.
* [ ] `GameManager` usa um serviço de fluxo de cenas (ou diretamente `SceneTransitionService`) para:

    * Ir para o menu.
    * Ir para o gameplay.
    * Resetar o jogo.
* [ ] Não há mais nenhum código de gameplay chamando diretamente o loader “rascunho”.
* [ ] O fluxo real do jogo (do boot até o gameplay e de volta) funciona com:

    * Fade consistente.
    * Cenas corretas carregadas/descarregadas.
    * Sem cenas “fantasmas” mantendo-se carregadas sem querer.

---

## Etapa 6 – Primeira versão dos `SceneSetup` (ScriptableObjects)

### Objetivo

Transformar os “grupos de cenas” em dados configuráveis, preparando o caminho para o “criador de loadscene” que você mencionou.

### Implementação

1. **Criar `SceneSetup`**

   Em `SceneManagement/Configs/SceneSetup.cs`:

    * Campos:

        * `string Id` (ex.: "Menu", "Gameplay", "GameplayWithDebug").
        * `string[] SceneNames`.
        * `string PrimarySceneName`.

2. **Criar um pequeno repositório**

   Em `SceneManagement/Configs/ISceneSetupRepository.cs`:

    * Exposto algo como:

      ```csharp
      SceneSetup GetById(string id);
      ```

    * Implementação simples usando um array/lista de `SceneSetup` referenciado em um SO principal ou em `GameConfig`.

3. **Adaptar o fluxo de alto nível**

    * Em `GameSceneFlowService` (ou equivalente):

        * `GoToMenuAsync` → pega `SceneSetup` com Id "Menu".
        * `GoToGameplayAsync` → pega `SceneSetup` com Id "Gameplay".
        * Usa o `ISceneTransitionPlanner` para montar o `SceneTransitionContext`.
        * Chama `ISceneTransitionService.RunTransitionAsync(context)`.

4. **Criar assets no editor**

    * `MenuSetup.asset`
    * `GameplaySetup.asset`
    * (outros que desejar)

### Validação

Para considerar a Etapa 6 concluída:

* [ ] Você consegue configurar, no editor, os assets de `SceneSetup` para Menu e Gameplay.
* [ ] Se trocar a lista de cenas no `SceneSetup` (por exemplo, adicionar uma cena de Debug), o fluxo de transição passa a carregar/descarregar essa cena sem qualquer mudança de código.
* [ ] Seu fluxo de jogo real já não depende de strings hard-coded de cenas em scripts de gameplay – essas strings estão todos concentradas nos `SceneSetup`.

---

Se estiver de acordo, na próxima mensagem já começo pela **Etapa 1 + 2**, trazendo:

* A interface `ISceneLoader`.
* Uma primeira implementação `SceneLoader` com logs.
* Um pequeno script tester de baixo nível e como você testa no Editor para validar a etapa.
