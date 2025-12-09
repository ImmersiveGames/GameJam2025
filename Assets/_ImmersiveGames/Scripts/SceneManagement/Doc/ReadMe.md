# **Scene Flow System (SFS)**

### *Pipeline Moderno de Transição de Cenas — Fade, HUD, Scene Groups & Context Planning*

---

# **1. Visão Geral**

O **Scene Flow System (SFS)** é o pipeline oficial de gerenciamento e transição de cenas para projetos Unity dentro do ecossistema *ImmersiveGames*.

Ele substitui completamente sistemas antigos como:

* `SceneSetup` (LEGADO – removido)
* Strings de cenas no `GameConfig` (LEGADO – removido)
* Fluxos manuais e scene loaders ad hoc

O SFS é:

* **100% orientado a dados (ScriptableObjects)**
* **Determinístico e previsível**
* **Extensível e isolado por camadas**
* Integrado com:

    * Fade animado por **AnimationCurve**
    * HUD global de carregamento
    * Eventos de transição
    * SceneLoader async padrão do projeto

---

# **2. Arquitetura Geral**

A arquitetura é composta de 3 camadas:

```
GAME LAYER (Top)
 └── GameManager.SceneFlow
       ├── SceneFlowMap (SO)
       └── SceneGroupProfile (SO)
PIPELINE LAYER (Core)
 ├── SceneTransitionPlanner
 └── SceneTransitionService
SYSTEM LAYER (Runtime Executors)
 ├── FadeService → FadeController
 ├── SceneLoader
 └── SceneLoadingHudService → SceneLoadingHudView
```

---

## **2.1 Fluxo Resumido**

```
GameManager                 Usuário solicita transição (GoToMenu / Gameplay)
   │
   ├── SceneFlowMap         SFS escolhe grupo destino via ScriptableObject
   │
   ├── SceneTransitionPlanner
   │        Compara estado atual vs grupo destino → gera SceneTransitionContext
   │
   └── SceneTransitionService
            │
            ├── FadeService.ApplyProfile()
            ├── HUD.ShowLoading()
            ├── SceneLoader.LoadSceneAsync()
            ├── SceneLoader.UnloadSceneAsync()
            ├── HUD.MarkScenesReady()
            ├── HUD.HideLoading()
            └── FadeService.FadeOut()
```

---

# **3. ScriptableObjects Fundamentais**

O SFS segue a regra central:

> **Toda configuração deve estar em ScriptableObjects.
> Código não carrega strings de cena.**

---

## **3.1 SceneFlowMap**

Mapa mestre de todos os fluxos do jogo.

```cs
public class SceneFlowMap : ScriptableObject
{
    public SceneGroupProfile MenuGroup;
    public SceneGroupProfile GameplayGroup;

    [Serializable]
    public struct NamedGroupEntry
    {
        public string key;
        public SceneGroupProfile group;
    }

    [SerializeField] private List<NamedGroupEntry> namedGroups;
}
```

### Função

Ele garante:

* Quais são os grupos principais do jogo (Menu, Gameplay, Lobby, Boss, etc.)
* Um dicionário expandível de grupos extras via chave (`namedGroups`)

### Exemplo no Inspector:

```
SceneFlowMap
  - MenuGroup       → MenuGroupProfile
  - GameplayGroup   → GameplayGroupProfile
  - namedGroups:
        "Boss01" → BossGroupProfile
        "Shop"   → ShopGroupProfile
```

---

## **3.2 SceneGroupProfile**

Define um “grupo de cenas” que devem existir juntas.

```cs
sceneNames: ["GameplayScene", "UIScene"]
activeSceneName: "GameplayScene"
transitionProfile: GameplayTransitionProfile
```

### Função

* Lista de cenas a carregar
* Indica qual será a cena ativa
* Limpa erros do usuário (cenas vazias/nulas)
* Centraliza tempos mínimos e perfil visual

### Exemplo

```
SceneGroupProfile: Gameplay

Scenes:
  - GameplayScene
  - UIScene
Active Scene: GameplayScene
Transition Profile: GameplayTransitionProfile
Use Fade: Yes
```

---

## **3.3 SceneTransitionProfile**

Fonte de verdade para:

* FadeInDuration
* FadeOutDuration
* FadeInCurve
* FadeOutCurve
* Títulos e descrições do HUD
* Tempo mínimo visível do HUD

O **FadeController NÃO tem configuração via inspector** — ele só executa as curvas/durações deste SO.

### Exemplo:

```
UseFade = true
FadeInDuration = 0.5
FadeOutDuration = 0.7
FadeInCurve = EaseOut
FadeOutCurve = EaseIn
MinHudVisibleSeconds = 0.8

LoadingTitle = "Iniciando..."
LoadingDescriptionTemplate = "Carregando: {Scenes}"
FinishingTitle = "Pronto!"
FinishingDescription = "Carregamento concluído."
```

---

# **4. Pipeline Interno do Scene Flow System**

## **4.1 SceneTransitionPlanner (SimpleSceneTransitionPlanner)**

Responsável por comparar:

* Estado atual (`SceneState.Capture()`)
* Estado desejado (`SceneGroupProfile`)

E gerar:

* Cenas a carregar
* Cenas a descarregar
* Cena ativa final
* Perfil de transição
* Flags de fade

### Exemplo de contexto gerado:

```cs
SceneTransitionContext:
{
    scenesToLoad: ["GameplayScene", "UIScene"],
    scenesToUnload: ["MenuScene"],
    targetActiveScene: "GameplayScene",
    useFade: true,
    transitionProfile: GameplayTransitionProfile
}
```

---

## **4.2 SceneTransitionService**

Executor do fluxo.

### Ordem exata (sem CancellationToken):

```
FadeIn
HUD.ShowLoading
Load (additive)
SetActiveScene
Unload (antigos)
HUD.MarkScenesReady
Garantir tempo mínimo de HUD
HUD.Hide
FadeOut
```

### Integração FadeService

Antes do primeiro fade:

```cs
fadeService.ConfigureFromProfile(context.transitionProfile);
```

### Eventos emitidos:

* `SceneTransitionStartedEvent`
* `SceneTransitionScenesReadyEvent`
* `SceneTransitionCompletedEvent`

---

## **4.3 FadeService + FadeController**

### Princípios:

* FadeController não guarda valores em inspector.
* FadeService aplica valores do perfil:

```cs
fadeController.Configure(
    profile.FadeInDuration,
    profile.FadeOutDuration,
    profile.FadeInCurve,
    profile.FadeOutCurve
);
```

### Fluxo:

```
FadeService
   └── Locate FadeController
   └── ApplyProfile(SceneTransitionProfile)
   └── FadeIn or FadeOut
```

---

## **4.4 SceneLoadingHudController**

Centraliza HUD de loading.

### Em ShowLoadingAsync:

* Lê perfil do contexto
* Aplica:

    * LoadingTitle
    * LoadingDescriptionTemplate ("{Scenes}")

### Em MarkScenesReadyAsync:

* Aplica:

    * FinishingTitle
    * FinishingDescription

---

# **5. Configuração Inicial — Passo a Passo (IMPORTANTE)**

## **Passo 1 — Criar grupos**

1. Crie 2 `SceneGroupProfile`:

    * MenuGroup
    * GameplayGroup

2. Preencha:

    * Lista de cenas
    * Cena ativa

3. Defina `TransitionProfile`.

---

## **Passo 2 — Criar SceneFlowMap**

```
SceneFlowMap
  MenuGroup → MenuGroupProfile
  GameplayGroup → GameplayGroupProfile
```

---

## **Passo 3 — Configurar Fade**

Crie uma cena **FadeScene** contendo:

* Canvas fullscreen
* CanvasGroup
* FadeController

---

## **Passo 4 — Configurar HUD Global**

Crie uma cena **UIGlobalScene**:

* Canvas
* SceneLoadingHudView
* SceneLoadingHudController (registrado global via DI)

---

## **Passo 5 — Registrar no GameManager**

O fluxo moderno já está implementado em:

```
GameManager.SceneFlow.cs
```

Chamadas:

```cs
await GoToMenuFromCurrentSceneAsync();
await StartGameplayFromCurrentSceneAsync();
```

---

# **6. Exemplos Práticos**

## **6.1 Ir para o menu**

```cs
await GameManager.Instance.GoToMenuFromCurrentSceneAsync();
```

---

## **6.2 Ir para o gameplay**

```cs
await GameManager.Instance.StartGameplayFromCurrentSceneAsync();
```

---

## **6.3 Criar tecla de debug**

```cs
void Update()
{
    if (Input.GetKeyDown(KeyCode.F1))
        _ = GameManager.Instance.GoToMenuFromCurrentSceneAsync();

    if (Input.GetKeyDown(KeyCode.F2))
        _ = GameManager.Instance.StartGameplayFromCurrentSceneAsync();
}
```

---

## **6.4 Criar novo grupo (Boss Battle)**

1. Duplique um `SceneGroupProfile`
2. Preencha cenas:

    * GameplayScene
    * UIScene
    * BossArenaScene
3. Adicione no SceneFlowMap:

```
namedGroups:
    "Boss" → BossGroupProfile
```

4. Chame via planner:

```cs
var bossGroup = sceneFlowMap.GetGroup("Boss");
var ctx = planner.BuildContext(SceneState.Capture(), bossGroup);
await transitionService.RunTransitionAsync(ctx);
```

---

# **7. Diagramas**

## **7.1 Pipeline do Fade**

```
SceneTransitionService
   └── FadeService.ConfigureFromProfile()
         └── FadeController.Configure()
               └── FadeToAsync()
```

---

## **7.2 Pipeline da Transição**

```
[Start]
   ↓
FadeIn
   ↓
HUD.Show
   ↓
Load Additive Scenes
   ↓
Set Active Scene
   ↓
Unload Old Scenes
   ↓
HUD.MarkReady
   ↓
Wait Min HUD Time
   ↓
HUD.Hide
   ↓
FadeOut
   ↓
[Complete]
```

---

# **8. Boas Práticas**

### ✔ Fonte de verdade sempre nos ScriptableObjects

Nada de strings de cena no código.

### ✔ Nunca referenciar diretamente nomes de cenas

Sempre via SceneGroupProfile.

### ✔ Cada grupo deve ter UMA cena ativa

Evita comportamentos inconsistentes.

### ✔ Durations/Curves só no TransitionProfile

Evita duplicações e erros de configuração.

### ✔ HUD sempre veio antes do FadeOut

Para evitar flickers.

### ✔ Evitar remover cenas da lista manualmente em runtime

Sempre deixar o Planner decidir.

---

# **9. Debug Checklist**

### HUD não aparece?

* Verificar se `SceneLoadingHudController` está registrado global.
* Verificar se UIGlobalScene está carregada no bootstrap.

### Fade não aparece?

* Verificar TransitionProfile.UseFade.
* Verificar FadeScene carregada.

### Cena ativa errada?

* ActiveSceneName do SceneGroupProfile incorreto.

### Cena não descarrega?

* Verificar SceneFlowMap → grupos conflitantes.

### Perfil não aplicado?

* Verificar TransitionProfile associado no SceneGroupProfile.

---

# **10. Estrutura Recomendada de Pastas**

```
/Assets/_Game
    /SceneFlow
        SceneFlowMap.asset
        MenuGroupProfile.asset
        GameplayGroupProfile.asset
        Profiles/
            MenuTransitionProfile.asset
            GameplayTransitionProfile.asset
    /Fade
        FadeScene.unity
        FadeController.cs
        FadeService.cs
    /HUD
        UIGlobalScene.unity
        SceneLoadingHudView.cs
        SceneLoadingHudController.cs
    /Core
        SceneTransitionService.cs
        SceneTransitionPlanner.cs
        SceneState.cs
```

---

# **Conclusão**

O **Scene Flow System (SFS)** entrega:

* Pipeline robusto
* Configurável via ScriptableObjects
* Integrado com Fade & HUD
* Determinístico e seguro
* Simples de estender
* Sem legado

Você agora possui:

* A documentação oficial
* Um framework sólido para qualquer fluxo de cena
* Uma base para futuros módulos (cutscenes, checkpoints, loading avançado, etc.)

---
