# 🎬 Sistema de Fade + Gerenciamento de Transição de Cenas

### Guia de Uso e Arquitetura (v2.0 — Task-Based, Zero Corrotinas)

---

## 📚 **Índice**

1. [Visão Geral](#visão-geral)
2. [Arquitetura do Sistema](#arquitetura-do-sistema)
3. [Fluxo Completo de Transição](#fluxo-completo-de-transição)
4. [Componentes Essenciais](#componentes-essenciais)
5. [Ciclo de Vida e Pré-Carregamento](#ciclo-de-vida-e-pré-carregamento)
6. [Integração Passo a Passo](#integração-passo-a-passo)
7. [Configurações no Editor](#configurações-no-editor)
8. [Boas Práticas e Troubleshooting](#boas-práticas-e-troubleshooting)
9. [Extensões Futuras Sugeridas](#extensões-futuras-sugeridas)

---

## 🎯 **Visão Geral**

O sistema de **Scene Flow + Fade** provê uma infraestrutura robusta, assíncrona e totalmente desacoplada para:

* Troca de cenas **aditivas**
* Controle de fade-in/fade-out
* HUD de loading com transição animada
* Pipeline ordenado e previsível
* 0% de corrotinas – tudo movido para `Task` + `async/await`

O objetivo principal é garantir:

1. Experiência visual consistente
2. Transições estável independente da carga real
3. Modularidade e extensão por DI (DependencyManager)
4. Possibilidade de instrumentação via EventBus

---

## 🧠 **Arquitetura do Sistema**

```
SceneTransitionPlanner (define o contexto)
        │
        ▼
SceneTransitionService (orquestrador principal)
        │
        ├── IFadeService
        │       └── FadeService
        │               └── FadeController (AnimationCurve)
        │
        ├── ISceneLoader
        │       └── SceneLoaderCore
        │
        └── ISceneLoadingHudTaskService
                └── SceneLoadingHudController
                        └── SceneLoadingHudView (CanvasGroup fade)
```

Além disso, o fluxo emite eventos:

```
SceneTransitionStartedEvent
SceneTransitionScenesReadyEvent
SceneTransitionCompletedEvent
```

Para permitir integração com UI, IA, áudio, analytics, etc.

---

## 🔄 **Fluxo Completo de Transição**

A transição segue **sempre** está ordem:

1. **FadeIn** (escurecer a tela)
2. **Exibir HUD** (fade-in do painel de loading)
3. **Carregar cenas alvo (Additive)**
4. **Definir cena ativa**
5. **Descarregar cenas antigas**
6. **HUD → “Finalizando...”**
7. **Garantir tempo mínimo de HUD visível**
8. **Ocultar HUD** (fade-out)
9. **FadeOut** (revelar cena final)
10. **Evento: Transição Concluída**

O sistema garante:

* Nenhum frame de “flash branco”
* HUD nunca pisca ou some instantaneamente
* Fade é sempre suave graças ao `AnimationCurve`
* Ordem sempre determinística

---

## 🧩 **Componentes Essenciais**

---

### **1. IFadeService**

Contratos principais:

```csharp
Task FadeInAsync();
Task FadeOutAsync();
void RequestFadeIn();
void RequestFadeOut();
Task PreloadAsync();
```

Fornecido pela implementação:

### **FadeService**

Responsável por:

* Carregar a FadeScene (somente uma vez)
* Instanciar `FadeController` persistente
* Sincronizar chamadas concorrentes via `SemaphoreSlim`
* Pré-carregar no bootstrap (`DependencyBootstrapper`)

---

### **2. FadeController**

*(Task-based, sem corrotinas)*

Funções:

* Animar `CanvasGroup.alpha` usando `Time.unscaledDeltaTime`
* Utilizar curvas de easing via `AnimationCurve`:

```csharp
[SerializeField] AnimationCurve fadeInCurve;
[SerializeField] AnimationCurve fadeOutCurve;
```

Em vez de `lerp` linear.

---

### **3. SceneTransitionPlanner**

Gera o `SceneTransitionContext`:

```csharp
Load = ["GameplayScene", "UIScene"]
Unload = ["MenuScene"]
TargetActive = "GameplayScene"
UseFade = true
```

É intercambiável; você pode criar planners para cutscenes, arenas, etc.

---

### **4. SceneTransitionService**

*(Orquestrador principal)*

O único responsável por “coreografar”:

* fade → hud → load → unload → hud → fade
* resiliência a cenas que carregam rápido
* sincronização com HUD
* emissão de eventos
* sem corrotinas, tudo `await`

---

### **5. SceneLoadingHudController + SceneLoadingHudView**

Responsáveis por:

* Fade-in/out do HUD
* Atualização de textos
* Mostrar/esconder sem jamais desativar objetos críticos
* Compatíveis com canvases ativados/desativados em runtime

---

### **6. SceneLoaderCore**

Wrapper para `LoadSceneAsync` / `UnloadSceneAsync` convertidos para `Task`.

---

## 🔥 **Ciclo de Vida e Pré-Carregamento**

O FadeService, dentro do `DependencyBootstrapper`, agora executa:

```csharp
_ = fadeService.PreloadAsync();
```

Isso evita:

* Travamento na primeira transição
* Aquele “soluço” no primeiro fade-in

Com pré-load, a transição inicial já é suave e imediata.

---

## 🚀 **Integração Passo a Passo**

### **1) Criar o contexto**

```csharp
var context = new SceneTransitionContext(
    scenesToLoad: new[] { "GameplayScene", "UIScene" },
    scenesToUnload: new[] { "MenuScene" },
    targetActiveScene: "GameplayScene",
    useFade: true
);
```

---

### **2) Solicitar a transição via ISceneTransitionService**

```csharp
DependencyManager.Provider
    .GetGlobal<ISceneTransitionService>()
    .RunTransitionAsync(context);
```

---

### **3) Ou usar a camada superior (GameManager)**

```csharp
await GameManager.SceneFlow.LoadGameplayAsync();
```

---

### **4) Ou disparar fade manual**

```csharp
var fade = DependencyManager.Provider.Resolve<IFadeService>();
await fade.FadeInAsync();
await fade.FadeOutAsync();
```

---

## 🛠️ **Configurações no Editor**

### **FadeScene**

* Deve conter **apenas** um GameObject com `FadeController`.
* `CanvasGroup.alpha` deve começar em `0`.
* Curvas sugeridas:

    * FadeIn: Ease-In-Out suave
    * FadeOut: Ease-Out mais curto

### **HUD (UIGlobalScene)**

* `SceneLoadingHudView` deve estar com:

    * `startHidden = true`
    * `fadeInDuration = 0.35`
    * `fadeOutDuration = 0.35`

### **Canvas**

* Sorting Order recomendado: **5000** ou maior que todos os outros canvases.
* `overrideSorting = true`

---

## 🧪 **Boas Práticas e Troubleshooting**

| Problema                   | Causa Provável                           | Solução                                                         |
| -------------------------- | ---------------------------------------- | --------------------------------------------------------------- |
| Fade demora muito ou pouco | Curvas diferentes ou durations distintos | Ajustar `fadeInDuration`, `fadeOutDuration`, AnimationCurve     |
| HUD “pisca”                | Cena carrega muito rápido                | Ajustar `MinHudVisibleSeconds`                                  |
| HUD não aparece            | Canvas desativado por terceiros          | `EnsureCanvasAndViewActive()` reativa automaticamente           |
| Fade piscando              | FadeScene não pré-carregada              | Confirmar bootstrap executando `PreloadAsync()`                 |
| EventSystem duplicado      | Cenas UI carregadas sem controle         | Garanta somente 1 EventSystem ou destrua o duplicado via script |

---

## 🧱 **Extensões Futuras Sugeridas**

1. **Fade por cor ou textura**
   Permitindo blecaute, branco, ou texturas personalizadas.

2. **Perfis de transição (ScriptableObject)**
   Tempo de fade e HUD variáveis por tipo de cena (Menu, Gameplay, Cutscene, BossFight).

3. **Progress real baseado em AsyncOperation.progress**
   Para HUD de carregamento detalhado.

4. **Transições em cadeia (queue)**
   Sistemas que empilham múltiplos contextos e executam em sequência.

5. **Modo Instantâneo (sem fade/HUD)**
   Para mudanças internas invisíveis.

---

# ✅ **Conclusão**

O sistema atual é:

* **100% assíncrono**
* **Totalmente livre de corrotinas**
* **Determinístico e previsível**
* **Extensível por DI**
* **Com fade cinematográfico usando curvas**
* **HUD suave (fade-in/out), com tempo mínimo garantido**
* **Capaz de lidar com múltiplas transições de forma estável**

Se quiser, posso:

* Gerar uma versão **Markdown com formatação avançada**,
* Criar **diagramas gráficos**,
* Criar **README separado para FadeSystem e SceneFlow**,
* Criar **diagramas PlantUML** para documentação interna.
