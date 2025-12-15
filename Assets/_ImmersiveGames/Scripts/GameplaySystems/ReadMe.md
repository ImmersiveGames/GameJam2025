# Gameplay Systems – Documentação

## 📋 Visão Geral

O **Gameplay Systems** concentra os serviços centrais usados pelas cenas de gameplay em multiplayer local no Unity 6 (C#):

- **Domínios de atores/jogadores** com registro por cena e auto-registro de instâncias (`ActorRegistry`, `PlayerDomain`, `EaterDomain`).
- **Coordenação de execução** desacoplada do `Time.timeScale`, permitindo pausar/desbloquear lógica por tokens (`SimulationGateService` + `GameplayExecutionCoordinator`).
- **Pipeline de reset in-place** assíncrono e ordenável para restaurar atores sem recarregar a cena (`ResetOrchestratorBehaviour`).
- Integração com **DependencyManager** para respeitar SOLID (Inversão de Dependência) e facilitar testes de integração.

Estrutura principal:

```text
GameplaySystems
├─ Bootstrap/GameplayDomainBootstrapper (registra serviços por cena)
├─ Domain/ (registries e auto-registrars de atores)
├─ Execution/ (gate/tokens + coordenação de execução)
├─ Reset/ (orquestração de reset in-place)
└─ GameplayManager.cs (acesso global ao Eater)
```

---

## 🏗️ Arquitetura Geral

### Ciclo de vida por cena

1. **`GameplayDomainBootstrapper`** instancia e registra no `DependencyManager` os serviços de domínio (`IActorRegistry`, `IPlayerDomain`, `IEaterDomain`).
2. **Auto-registrars** (`ActorAutoRegistrar`, `PlayerAutoRegistrar`, `EaterAutoRegistrar`) resolvem os domínios da cena e registram os `IActor` com `DefaultExecutionOrder` negativo para acontecer antes da lógica de gameplay.
3. **`SimulationGateService`** (global) expõe um gate de simulação controlado por tokens (padrões em `SimulationGateTokens`).
4. **`GameplayExecutionCoordinator`** (scene-scoped) consome o gate global e propaga `IsExecutionAllowed` para todos os `GameplayExecutionParticipantBehaviour` da cena.
5. **`ResetOrchestratorBehaviour`** coordena resets in-place por escopo (todos os atores, apenas players, apenas eater ou lista de ActorIds), com fases `Cleanup → Restore → Rebind`.

### Fluxo de execução/pause

```text
UI/Estado/QA     SimulationGateTokens.*       GameplayExecutionCoordinator
      │                 │                               │
      ├── Acquire(token)┤                               │
      │                 └─> Gate fechado (IsOpen=false) │
      │                                                   │
      └──────────────────────────────────────────> Participants
                                        SetExecutionAllowed(false/true)
```

- Qualquer sistema pode pausar a simulação adquirindo um token (`using gate.Acquire(SimulationGateTokens.Pause)`), sem mexer em `timeScale`.
- Participantes podem ser coletados manualmente (listas `behavioursToToggle` / `gameObjectsToToggle`) ou via auto-coleta filtrada (ignora UI/registradores/infra).

### Fluxo de reset in-place

```text
ResetOrchestratorBehaviour (scene)
    ↳ Resolve serviços (gate + domínios)
    ↳ Define alvos conforme ResetScope
    ↳ Fases assíncronas em todos os participantes:
        Cleanup → Restore → Rebind
```

- Participantes implementam `IResetInterfaces` (assíncrono recomendado) ou `IResetParticipantSync` (fallback).
- Ordem opcional por `IResetOrder` e filtro por `IResetScopeFilter`.
- Quando `includeSceneLevelParticipants=true`, GameObjects root da cena que não são atores também entram no reset (útil para timers e sistemas globais da cena).

---

## 🎯 Componentes Principais

### Bootstrap
- **`GameplayDomainBootstrapper`**: registra `IActorRegistry`, `IPlayerDomain` e `IEaterDomain` no escopo da cena. Use `allowOverride` em testes para substituir implementações.

### Domínio (registro de atores)
- **`ActorRegistry` (`IActorRegistry`)**: dicionário `ActorId → IActor` com eventos de registro/desregistro e consultas (`TryGetActor`). Rejeita ActorId vazio ou duplicado.
- **`ActorAutoRegistrar`**: registra automaticamente qualquer `IActor` encontrado no `Awake`/`OnEnable` (adiando para `Start` se o `ActorId` ainda não existir). Requer `GameplayDomainBootstrapper` na cena.
- **`PlayerDomain` (`IPlayerDomain`)**: mantém a lista de players (ordem de registro), captura `Pose` inicial por `ActorId` e permite recuperar ou atualizar spawn poses.
- **`PlayerAutoRegistrar`**: registra players assim que o `ActorId` fica disponível (loop em `Update` para esperar ID gerado por outros sistemas).
- **`EaterDomain` (`IEaterDomain`)**: armazena um único `IActor` “Eater” por cena (evento de register/unregister, rejeita duplicatas).
- **`EaterAutoRegistrar`**: registra o ator local como Eater assim que o `ActorId` existe e o domínio está disponível.

### Execução (pause/retomada sem mexer em timeScale)
- **`SimulationGateService` (`ISimulationGateService`)**: gate thread-safe baseado em tokens (HashSet). `IsOpen` é verdadeiro quando não há tokens ativos. Eventos `GateChanged` notificam coordenadores.
- **`SimulationGateTokens`**: constantes para tokens mais comuns (`state.pause`, `state.gameover`, `flow.soft_reset`, etc.). Centraliza strings para evitar divergência.
- **`GameplayExecutionCoordinator`**: resolve o `ISimulationGateService` global, registra-se como `IGameplayExecutionCoordinator` da cena e coordena participantes. Pode auto-descobrir `GameplayExecutionParticipantBehaviour` na cena e reaplica o estado atual do gate.
- **`GameplayExecutionParticipantBehaviour`**: aplica `SetExecutionAllowed` aos componentes/GameObjects configurados. Suporta:
  - Auto-coleta opcional de `Behaviour` (com filtros por namespace UI, nomes excluídos ou marker `IExecutionToggleIgnored`).
  - Mesclar ou substituir listas manuais, sanitização de nulos/duplicatas, bloqueio inicial (`startBlocked`).

### Reset in-place
- **`IResetInterfaces` / `IResetParticipantSync`**: contratos para participantes de reset (assíncrono preferencial). Devem ser idempotentes por fase.
- **`ResetOrchestratorBehaviour` (`IResetOrchestrator`)**: scene-scoped, opcionalmente escuta `GameResetRequestedEvent` (fluxo macro). Executa reset por escopo (`ResetScope`) usando fases `Cleanup/Restore/Rebind`. Usa `SimulationGateTokens.SoftReset` para bloquear simulação durante o reset e publica eventos (`GameResetStartedEvent`, `GameResetCompletedEvent`).
- **`ResetStructs` / `ResetScope` / `ResetRequest` / `ResetContext`**: modelos de dados para descrever fases, escopos e contexto corrente do reset.

### Manager
- **`GameplayManager` (`IGameplayManager`)**: singleton global que fornece acesso ao `WorldEater` via domínio (`IEaterDomain`) quando disponível, com fallback manual em `worldEater`. Registra-se como serviço global.

### QA utilitário
- **`QaOverlayE2ETester`**: ferramenta de QA para validar fluxo E2E (EventSystem único, reset in-place vs. reset macro, overlays). Pode ser acionada via `ContextMenu` ou `autoRunOnStart`.

---

## 🧭 Como usar nas cenas

1. Adicione **`GameplayDomainBootstrapper`** em um GameObject raiz da cena de gameplay.
2. Garanta que cada ator implementa **`IActor`** e possui **`ActorAutoRegistrar`** (e, se for player/Eater, os auto-registrars específicos).
3. Coloque **`SimulationGateService`** no contêiner global do `DependencyManager` (ou registre manualmente antes de carregar a cena). Use `SimulationGateTokens` para pausar/retomar.
4. Para controlar execução local, adicione **`GameplayExecutionCoordinator`** na cena e configure `autoDiscoverParticipants` se quiser registro automático.
5. Em cada ator ou subsistema que deva ser pausado, adicione **`GameplayExecutionParticipantBehaviour`** e configure auto-coleta ou listas manuais.
6. Para resets locais, adicione **`ResetOrchestratorBehaviour`** na cena, habilite `includeSceneLevelParticipants` se necessário e implemente `IResetInterfaces`/`IResetParticipantSync` nos componentes relevantes.

---

## 🧪 Boas Práticas

- **SOLID / DI**: registre e consuma serviços sempre via `DependencyManager` (evite `FindObjectOfType`). Mantenha interfaces finas (`IActorRegistry`, `IGameplayExecutionCoordinator`, etc.).
- **IDs estáveis**: garanta que `ActorId` é gerado antes do `OnEnable` quando possível; caso contrário, use os auto-registrars que esperam o ID.
- **Tokens bem nomeados**: centralize novos tokens em `SimulationGateTokens` para evitar colisões e facilitar QA.
- **Listas de toggle**: prefira auto-coleta com exclusão de infra/registradores (marker `IExecutionToggleIgnored`) para reduzir manutenção manual.
- **Reset idempotente**: cada fase (`Cleanup`, `Restore`, `Rebind`) deve ser segura para chamadas repetidas. Use `IResetOrder` para dependências e `IResetScopeFilter` para limitar escopos.
- **Logs**: mantenha `DebugLevel.Verbose` apenas em desenvolvimento; em produção, ajuste para evitar ruído.

---

## 📚 Referências Cruzadas

- `_ImmersiveGames/Scripts/GameplaySystems/Bootstrap/GameplayDomainBootstrapper.cs`
- `_ImmersiveGames/Scripts/GameplaySystems/Domain/ActorRegistry.cs`
- `_ImmersiveGames/Scripts/GameplaySystems/Domain/ActorAutoRegistrar.cs`
- `_ImmersiveGames/Scripts/GameplaySystems/Domain/PlayerDomain.cs`
- `_ImmersiveGames/Scripts/GameplaySystems/Domain/PlayerAutoRegistrar.cs`
- `_ImmersiveGames/Scripts/GameplaySystems/Domain/EaterDomain.cs`
- `_ImmersiveGames/Scripts/GameplaySystems/Domain/EaterAutoRegistrar.cs`
- `_ImmersiveGames/Scripts/GameplaySystems/Execution/SimulationGateService.cs`
- `_ImmersiveGames/Scripts/GameplaySystems/Execution/SimulationGateTokens.cs`
- `_ImmersiveGames/Scripts/GameplaySystems/Execution/GameplayExecutionCoordinator.cs`
- `_ImmersiveGames/Scripts/GameplaySystems/Execution/GameplayExecutionParticipantBehaviour.cs`
- `_ImmersiveGames/Scripts/GameplaySystems/Reset/ResetOrchestratorBehaviour.cs`
- `_ImmersiveGames/Scripts/GameplaySystems/Reset/IResetInterfaces.cs`
- `_ImmersiveGames/Scripts/GameplaySystems/Reset/ResetStructs.cs`
- `_ImmersiveGames/Scripts/GameplaySystems/GameplayManager.cs`
- `_ImmersiveGames/Scripts/GameplaySystems/QaOverlayE2ETester.cs`

---

*Documento criado para padronizar o uso dos serviços de Gameplay Systems em cenas de multiplayer local.*
