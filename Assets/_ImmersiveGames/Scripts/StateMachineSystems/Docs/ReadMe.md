# 🔁 State Machine Systems — Documentação Oficial (v2.0)

## 📚 Índice
1. [Visão Geral](#visão-geral)
2. [Arquitetura Dirigida a Eventos](#arquitetura-dirigida-a-eventos)
3. [Estados do Ciclo de Jogo](#estados-do-ciclo-de-jogo)
4. [Fluxo de Eventos](#fluxo-de-eventos)
5. [Integração com UI e Controles](#integração-com-ui-e-controles)
    1. [Usando `GameLoopRequestButton`](#usando-gamelooprequestbutton)
    2. [Criando Botões Personalizados](#criando-botões-personalizados)
6. [Ferramentas de Teste no Editor](#ferramentas-de-teste-no-editor)
7. [Boas Práticas](#boas-práticas)

---

## Visão Geral
A infraestrutura de FSM (`_ImmersiveGames.NewScripts.Infrastructure.Fsm`) controla o ciclo completo do jogo multiplayer local. Ela segue princípios SOLID, desacoplados via `EventBus`, permitindo que UI, controles físicos ou automações de QA disparem o fluxo sem depender de `Input` direto.

Componentes principais:
- `GameLoopStateMachine`: orquestra transições entre estados.
- Estados concretos (`MenuState`, `PlayingState`, `PausedState`, `GameOverState`, `VictoryState`), cada um responsável apenas por suas ações.
- `StateMachine` genérica e `StateMachineBuilder`: infraestrutura reutilizável para qualquer FSM do projeto.
- `GameManager`: ponto de entrada que escuta pedidos e publica eventos efetivos (start/pause/resume/reset/game over/victory).

---

## Arquitetura Dirigida a Eventos

```
UI / Controles → GameLoopRequestEvent (EventBus)
                             ↓
                GameLoopStateMachine
                             ↓
                     GameManager valida
                             ↓
        Eventos efetivos (Start, Pause, Victory, ...)
                             ↓
                    Estados reagem e propagam
```

1. UI ou outros sistemas disparam *pedidos* (`GameStartRequestedEvent`, `GameResetRequestedEvent`, etc.).
2. `GameLoopStateMachine` converte pedidos em transições internas usando `EventTriggeredPredicate`.
3. `GameManager` verifica se o pedido é válido para o estado atual antes de publicar o evento definitivo (`GamePauseEvent`, `GameStartEvent`, `GameOverEvent`, `GameVictoryEvent`).
4. Estados concretos executam lógica de entrada/saída (UI, gates, notificações) e notificam demais serviços.

Essa separação garante que qualquer camada (UI, IA, automação) possa pilotar o loop sem conhecer implementações internas.

---

## Estados do Ciclo de Jogo

| Estado | Ativo? | Eventos recebidos | Eventos emitidos |
| ------ | ------ | ----------------- | ---------------- |
| `MenuState` | ❌ | `GameStartRequestedEvent`, `GameResetRequestedEvent` | `StateChangedEvent(false)` |
| `PlayingState` | ✅ | `GamePauseRequestedEvent`, `GameOverEvent`, `GameVictoryEvent`, `GameResetRequestedEvent` | `GamePauseEvent(false)`, `StateChangedEvent(true)` |
| `PausedState` | ❌ | `GameResumeRequestedEvent`, `GameResetRequestedEvent` | `GamePauseEvent(true)` |
| `GameOverState` | ❌ | `GameResetRequestedEvent` | `StateChangedEvent(false)` |
| `VictoryState` | ❌ | `GameResetRequestedEvent` | `StateChangedEvent(false)` |

Todos os estados partilham a base `GameStateBase`, responsável por avisar UI e serviços dependentes quando entram ou saem.

### Ações permitidas por estado (`ActionType`)

- `Navigate`: navegação de UI (menus, overlays de pausa/pós-jogo).
- `UiSubmit` / `UiCancel`: confirma ou cancela interações de UI.
- `RequestReset` / `RequestQuit`: comandos genéricos para reiniciar ou sair (permanecem desacoplados da implementação da tela).

| Estado | Ações liberadas |
| ------ | --------------- |
| `MenuState` | Navegação, submit/cancel, pedidos de reset/quit. |
| `PlayingState` | `Move`, `Shoot`, `Spawn`, `Interact`. |
| `PausedState` | Navegação, submit/cancel, pedidos de reset/quit (sem ações de gameplay). |
| `GameOverState` | Navegação, submit/cancel, pedidos de reset/quit. |
| `VictoryState` | Navegação, submit/cancel, pedidos de reset/quit. |

---

## Fluxo de Eventos

Eventos disponíveis no `GameEventsBus`:

- **Pedidos** (usados para UI/controles):
  - `GameStartRequestedEvent`
  - `GamePauseRequestedEvent`
  - `GameResumeRequestedEvent`
  - `GameResetRequestedEvent`

- **Eventos efetivos** (disparados pelo `GameManager` depois de validar o pedido):
  - `GameStartEvent`
  - `GamePauseEvent`
  - `GameOverEvent`
  - `GameVictoryEvent`

- **Sinalizadores de reset** (pipeline de reinicialização):
  - `GameResetStartedEvent` (permite que sistemas persistentes limpem/flush antes do reload)
  - `GameResetCompletedEvent` (recarregamento concluído e FSM reconstruída)

- **Notificações auxiliares**:
  - `StateChangedEvent` (indica se o jogo está ativo).
  - Eventos de atores (`ActorDeathEvent`, `ActorReviveEvent`, `ActorStateChangedEvent`).

### Ordem típica para iniciar uma partida
1. UI dispara `GameStartRequestedEvent`.
2. `GameLoopStateMachine` muda de `MenuState` → `PlayingState`.
3. `GameManager` publica `GameStartEvent` para iniciar gameplay.
4. Subscritores (`SpawnSystems`, `TimerSystem`, etc.) respondem ao `GameStartEvent`.

### Ordem típica para resetar
1. UI ou QA dispara `GameResetRequestedEvent` (ou via menu de contexto).
2. `GameManager` publica `GameResetStartedEvent` e aguarda a FSM voltar ao `MenuState` para garantir que `OnExit` de cada estado seja aplicado (timeScale, eventos, etc.).
3. A FSM é reconstruída e a cena ativa é recarregada (UI é reanexada se for uma cena separada).
4. `GameResetCompletedEvent` confirma que a nova sessão está pronta.

---

## Integração com UI e Controles

Você pode conectar qualquer fonte de input (botões, atalhos, rede) apenas disparando os eventos de pedido. Abaixo, duas abordagens.

### Usando `GameLoopRequestButton`
1. Adicione o componente `GameLoopRequestButton` (menu *Immersive Games/Game Loop/Game Loop Request Button*) ao mesmo objeto que já possui um `Button` da UI.
2. No inspetor, escolha o `requestType` desejado:
   - `Start`, `Pause`, `Resume`, `Reset`, `GameOver`, `Victory`.
3. No `Button.onClick`, referencie o próprio componente e a função `GameLoopRequestButton.RaiseRequest`.

```csharp
// Internamente, o componente dispara o evento correspondente:
EventBus<GameStartRequestedEvent>.Raise(new GameStartRequestedEvent());
```

Esse componente é ideal para prototipagem e telas de debug, evitando boilerplate.

### Criando Botões Personalizados
Se preferir um script dedicado, basta publicar os eventos manualmente:

```csharp
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;

public sealed class CustomStartButton : MonoBehaviour
{
    // Chamado por UI Button → OnClick
    public void RequestStart()
    {
        // Comentário em português: dispara pedido de início seguindo o padrão event-driven.
        EventBus<GameStartRequestedEvent>.Raise(new GameStartRequestedEvent());
    }
}
```

O mesmo padrão vale para reset (`GameResetRequestedEvent`), pausa/retomada (`GamePauseRequestedEvent`, `GameResumeRequestedEvent`), ou testes de fim de jogo (`GameOverEvent`, `GameVictoryEvent`).

---

## Ferramentas de Teste no Editor

O `GameManager` expõe menus de contexto para acelerar QA e depuração:

- **Game Loop/Request Start** — envia `GameStartRequestedEvent`.
- **Game Loop/Request Pause** — envia `GamePauseRequestedEvent`.
- **Game Loop/Request Resume** — envia `GameResumeRequestedEvent`.
- **Game Loop/Request Reset** — envia `GameResetRequestedEvent` (executa rebuild + recarrega cena).
- **Game Loop/Force Game Over** — publica `GameOverEvent` diretamente.
- **Game Loop/Force Victory** — publica `GameVictoryEvent` diretamente.

Acesse clicando no ícone ☰ do componente `GameManager` no inspetor e selecione a ação desejada. Todos os comandos reutilizam o pipeline de eventos existente, garantindo que os mesmos guardrails sejam exercitados.

---

## Boas Práticas

- **Dispare pedidos, não estados**: UI deve enviar `*RequestedEvent` para permitir que `GameManager` valide a transição.
- **Evite dependência direta de `GameLoopStateMachine`**: sempre use o `EventBus`.
- **Desinscreva bindings**: quando adicionar novos serviços que escutam eventos, use `EventBinding` e limpe-os em `OnDestroy`.
- **Testes automatizados**: para simular fluxo no editor, use `ContextMenu` ou scripts de editor que chamem `RaiseRequest`.
- **Extensão de estados**: ao adicionar novos estados, registre-os no `StateMachineBuilder` e exponha novos eventos de pedido/efetivação conforme necessário.

---

> Qualquer dúvida ou nova feature no ciclo de jogo, atualizar este documento mantendo o padrão de exemplos em inglês e comentários em português.
> o Reset não esta funcionando corretamente no jogo, revisar.
