# ADR-0016 — Phases no WorldLifecycle (In-Place, SceneFlow e PreGame)

## Status

**Aceito / Ativo**

## Contexto

Com o fechamento do **Baseline 2.0**, o projeto passou a ter um **WorldLifecycle determinístico**, com reset canônico disparado em pontos bem definidos do fluxo (em especial em `SceneTransitionScenesReadyEvent`), além de um **sistema de Phase** já integrado ao reset do mundo.

Durante a evolução do gameplay, surgiram requisitos adicionais:

1. Suporte a **múltiplas fases (Phase 1, Phase 2, …)** com:

   - spawns distintos,
   - dados distintos,
   - comportamento distinto.

2. Necessidade de **dois modos diferentes de troca de fase**:

   - troca direta no mesmo gameplay (sem trocar cenas),
   - troca com transição completa (fade, loading, novas cenas).

3. Necessidade de uma etapa **antes da revelação da cena** (após FadeIn e reset), para:

   - splash screen,
   - cutscene,
   - preparação de UI,
   - prewarm de sistemas,

   sem bloquear indefinidamente o fluxo.

Além disso, existe um requisito de produto importante:

- **O sistema não deve impor um “gatilho fixo” de avanço de fase.**
  - Alguns jogos terão apenas uma fase.
  - Em jogos com múltiplas fases, o avanço pode ocorrer por múltiplos critérios (objetivos, UI, hub, narrativa, etc.).

Este ADR formaliza essas decisões **sem alterar o Baseline 2.0**, apenas estendendo o modelo de forma compatível.

---

## Decisões

### 1. Phase é um input do Reset do Mundo (não do SceneFlow)

Uma **Phase** representa um **estado lógico do mundo** que influencia:

- quais spawns são criados,
- quais dados são aplicados,
- como o mundo é reconstruído após um reset.

**Princípio-chave (já existente e reafirmado):**

> Uma Phase só é efetivamente aplicada no início de um World Reset.

Isso garante:

- determinismo,
- atomicidade,
- ausência de estados intermediários inválidos.

A Phase **não controla**:

- fade,
- loading,
- troca de cenas,
- apresentação visual.

**Implementação atual (base):**

- `PhasePlan` representa o “plano” (ex.: `id` + `contentSignature`).
- `PhaseContextService` mantém `Current` e `Pending`.
- `PhaseChangeService` é o façador (façade) de gameplay para:
  - armar `Pending`,
  - pedir reset in-place,
  - limpar `Pending`.

---

### 2. Dois tipos explícitos de troca de fase

Para evitar ambiguidade arquitetural, a troca de fase passa a ter **dois nomes oficiais**, com responsabilidades distintas.

> Nota: o *quando* avançar fase é responsabilidade do jogo (ver Decisão 2.3). Este ADR define *como* executar a troca de fase de forma canônica.

---

#### 2.1 InPlacePhaseChange (Troca de Fase In-Place)

**Definição**

Troca de fase **no mesmo conjunto de cenas**, reconstruindo o mundo no local atual.

**Fluxo canônico (produção)**

1. `IPhaseChangeService.SetPending(PhasePlan plan, string reason)`
2. `IPhaseChangeService.RequestInPlaceReset(string reason)`
3. WorldLifecycle executa:
   - Despawn
   - Spawn conforme a Phase *pendente*
4. No final do reset, a Phase *pendente* é **commitada** (vira `Current`) e o gameplay continua.

**Características**

- ❌ Não envolve `SceneFlow`
- ❌ Não troca cenas
- ✅ Reset completo do mundo
- ✅ Ideal para progressão contínua (ex.: fase 1 → fase 2)

**Uso típico**

- Fases sequenciais dentro da mesma arena/mapa
- Progressão de dificuldade
- Ondas de inimigos

---

#### 2.2 SceneFlowPhaseTransition (Troca de Fase com Transição)

**Definição**

Troca de fase **associada a uma transição de cenas**, com fade, loading e possível troca de layout/bioma.

**Fluxo canônico (conceitual)**

1. Definir “fase alvo” (por regra de jogo / UI / narrativa).
2. `IGameNavigationService.NavigateAsync(...)` dispara `SceneTransitionService`.
3. `SceneTransitionService` executa:
   - FadeIn
   - Load/Unload de cenas
4. `SceneTransitionScenesReadyEvent`
5. WorldLifecycle executa reset **canônico**
6. Cena é revelada (FadeOut)

**Ponto crítico (comportamento atual):**

- `PhaseContextSceneFlowBridge` **limpa `Pending`** ao receber `SceneTransitionStartedEvent`.
- Portanto, uma `Pending` armada **antes** do início da transição **não atravessa** a transição.

**Consequência:**

- Para suportar “phase + scene transition” de forma robusta, a fase alvo deve ser:
  - **carregada como dado de navegação** (route/intent), e
  - aplicada **depois** do `SceneTransitionStartedEvent` (após o auto-clear) e **antes** do `SceneTransitionScenesReadyEvent` (quando o reset é disparado), via um bridge dedicado.

**Status:**

- O fluxo de reset em transição de cenas já existe e é o caminho canônico do Baseline.
- O “carry” de Phase (phase-alvo acoplada à navegação) **ainda não tem call-site de produção**; hoje o uso é essencialmente via QA / chamadas explícitas.

**Características**

- ✅ Usa `SceneFlow`
- ✅ Usa Fade / Loading
- ✅ Reset ocorre em `SceneTransitionScenesReadyEvent`
- ✅ Ideal para mudanças estruturais de gameplay

**Uso típico**

- Troca de mapas
- Mudança de bioma
- Gameplay → Boss Arena
- Gameplay → Challenge Room

---

#### 2.3 Gatilhos de avanço são responsabilidade do jogo (não da infra)

Para evitar acoplamento indevido entre “fases” e “progressão”, fica definido:

- O sistema de Phase fornece **um procedimento único e documentado** para **aplicar** uma fase (SetPending → Reset → Commit).
- O sistema **não define**:
  - quando avançar,
  - qual critério decide,
  - se existe “fase 2”, “fase 3”, etc.

Isso permite que cada jogo (ou cada modo) implemente sua regra (objetivo concluído, UI de hub, seleção do jogador, narrativa, etc.) sem forçar um fluxo fixo.

---

### 3. PreGame / PreReveal não é Phase do Mundo

Para evitar confusão conceitual, fica definido:

> **PreGame / PreReveal NÃO é Phase.**

Ele não altera spawn, dados ou estado lógico do mundo.

---

#### 3.1 Definição de PreReveal

**PreReveal** é uma **etapa opcional de apresentação**, executada:

- **após**:
  - FadeIn (cortina fechada),
  - `SceneTransitionScenesReadyEvent`,
  - `WorldLifecycleResetCompletedEvent` (ou Skip),
- **antes**:
  - FadeOut (revelar a cena ao jogador).

Exemplos:

- Splash screen
- Cutscene
- Tela “Fase X”
- Preparação visual de UI
- Pequena narrativa contextual

---

#### 3.2 PreReveal não pode bloquear o fluxo

Regras obrigatórias:

- PreReveal é **opcional**
- Deve ter **timeout**
- Deve sempre **completar automaticamente** se não configurado
- Nunca pode impedir o FadeOut definitivo

---

#### 3.3 Encaixe técnico no pipeline existente

O PreReveal se encaixa **sem alterar o Baseline 2.0**, usando o ponto já existente:

```text
SceneFlow:
  SceneTransitionScenesReady
    → WorldLifecycleResetCompleted
      → Completion Gate
        → FadeOut
```

Decisão:

- O **Completion Gate** passa a poder ser **composto**, incluindo:
  - `WorldLifecycleResetCompletionGate` (existente)
  - `PreRevealGate` (novo, opcional)

Se não houver PreReveal configurado:

- o gate completa imediatamente,
- o comportamento atual é preservado.

---

### 4. Escopo do sistema de Phase

- O **sistema de Phase do mundo** é considerado **gameplay-centric**:
  - só produz efeito real quando há reset/spawn.
- Em `startup` e `frontend`:
  - Phase pode existir como dado,
  - mas o WorldLifecycle continua aplicando **Skip**, conforme Baseline 2.0.

Já o **PreReveal / Presentation Flow**:

- pode existir tanto em gameplay quanto em frontend,
- pois não depende de spawn nem reset.

---

### 5. Assets de Phase (ScriptableObjects)

Este ADR **não força** o uso de `Resources`.

Regra explícita:

- Se o resolver usa `Resources.Load`, o asset **deve** estar em `Resources/`.
- Se não se deseja usar `Resources`, o resolver **deve** usar:
  - referência serializada,
  - Addressables,
  - ou outro registry explícito.

Isso é uma decisão de infraestrutura, não de Phase.

---

## Consequências

### Benefícios

- Clareza absoluta entre:
  - Phase (estado do mundo),
  - Transição (SceneFlow),
  - Apresentação (PreReveal).
- Elimina regressões conceituais ao mudar de chat/IA.
- Permite evolução futura (cutscenes, narrativa, loading rico) sem quebrar baseline.

### Custos

- Introdução de mais um conceito (PreRevealGate).
- Necessidade de documentar bem as rotas de navegação que usam Phase + SceneFlow.

---

## Relação com outros ADRs

- **Baseline 2.0**: totalmente preservado.
- **ADR de Fade/Loading**: apenas complementado (não substituído).
- **ADR de WorldLifecycle Hooks**: inalterado.
- **ADR de SceneFlow**: inalterado.
- **ADR-0017 (Tipos de troca de fase)**: complementa este ADR ao nomear e separar `InPlacePhaseChange` vs `SceneFlowPhaseTransition`.

Este ADR **não reescreve**, apenas **especializa e nomeia** comportamentos já emergentes no sistema.

---

## Status Final

Este ADR encerra a discussão conceitual sobre:

- fases,
- resets,
- transições,
- pregame.

A partir deste ponto, qualquer implementação futura **deve referenciar este documento** como fonte de verdade.

---

## Referências de implementação

### ResetWorld (produção) — caminho real do fluxo

O gatilho de produção para **ResetWorld em transição de cenas** já existe e está ancorado em `SceneTransitionScenesReadyEvent`:

- `SceneTransitionService` emite `SceneTransitionStartedEvent` / `SceneTransitionScenesReadyEvent` / `SceneTransitionCompletedEvent`
- `WorldLifecycleRuntimeCoordinator` recebe `SceneTransitionScenesReadyEvent` e chama `IWorldResetRequestService.RequestReset(...)` quando `profile='gameplay'`
- `WorldLifecycleController` + `WorldLifecycleOrchestrator` executam o reset (despawn/spawn + hooks)
- Ao final do reset, o runtime emite `WorldLifecycleResetCompletedEvent(signature, reason)`
- `GameLoopSceneFlowCoordinator` aguarda `SceneTransitionScenesReadyEvent` + `WorldLifecycleResetCompletedEvent` e então chama `IGameLoopService.RequestStart()`

**Arquivos-chave (implementação atual):**

- `NewScripts/Infrastructure/SceneFlow/SceneTransitionService.cs` (eventos Started/ScenesReady/Completed)
- `NewScripts/Infrastructure/WorldLifecycle/Runtime/WorldLifecycleRuntimeCoordinator.cs` (gatilho do reset pós-ScenesReady)
- `NewScripts/Infrastructure/WorldLifecycle/Runtime/WorldLifecycleController.cs` + `NewScripts/Infrastructure/WorldLifecycle/WorldLifecycleOrchestrator.cs` (execução do reset)
- `NewScripts/Gameplay/GameLoop/GameLoopSceneFlowCoordinator.cs` (start do GameLoop após reset)
- `NewScripts/Infrastructure/GlobalBootstrap.cs` (registro global do runtime/coordenadores)

### Troca de fase (produção) — status

O **serviço** de troca de fase existe (`IPhaseChangeService` / `PhaseChangeService`), porém **não há um call-site “produção”** conectado ao gameplay que decida “quando” avançar fase (ex.: fim de fase, fim de run, seleção em hub, etc.). Hoje, o uso é essencialmente via QA / chamadas explícitas.

**Arquivos-chave (implementação atual):**

- `NewScripts/Gameplay/Phases/IPhaseChangeService.cs`
- `NewScripts/Gameplay/Phases/PhaseChangeService.cs`
- `NewScripts/Gameplay/Phases/PhaseContextService.cs`
- `NewScripts/Infrastructure/Gameplay/PhaseContextSceneFlowBridge.cs` (auto-clear de Pending em `SceneTransitionStartedEvent`)

