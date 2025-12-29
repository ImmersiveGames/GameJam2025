# WorldLifecycle Reset — Status e Pendências (macro-estruturas)

Data: 2025-12-26  
Escopo: NewScripts — WorldLifecycle + Gameplay/Reset

## O que está funcional (confirmado por logs)
### 1) Macro-fluxo do WorldLifecycle Reset
O pipeline “grande” de reset está operacional e resiliente mesmo quando **não há spawn services**:
- Gate fecha/abre durante o reset (`flow.soft_reset`).
- Ordem de fases do orchestrator executa sem falhas:
  - `OnBeforeDespawn` (hooks) → `Despawn` → `Scoped reset participants` → `OnBeforeSpawn` → `Spawn` → `OnAfterSpawn`
- Quando não existem spawn services, `Despawn/Spawn` são **skip** com warning, mas o reset segue (hooks + scoped participants).

### 2) Macro “WorldLifecycle → Gameplay Reset (targets/grupos)”
O bridge está estável e validado para **PlayersOnly**:
- `IResetScopeParticipant` (WorldLifecycle) → `PlayersResetParticipant` → `IGameplayResetOrchestrator`
- QA dedicado validou execução real por fases:
  - `GameplayResetQaSpawner` spawnou players QA
  - `GameplayResetOrchestrator` resolveu `targets=2`
  - `GameplayResetQaProbe` recebeu `Cleanup/Restore/Rebind` por target

## Status macro (escala 0–100, aproximado)
Estimativa **qualitativa** baseada nos logs e nos docs atuais (não é métrica de performance).

- **Boot → Menu:** ~80  
  Pipeline observado e estável, com SKIP no startup/menu.
- **SceneFlow:** ~85  
  Fluxo `Started → ScenesReady → gate → FadeOut → Completed` confirmado.
- **Fade:** ~85  
  Cena aditiva com ordenação e integração com SceneFlow.
- **LoadingHud:** ~80  
  HUD integrado ao SceneFlow e respeitando o gate.
- **Gate/Readiness:** ~85  
  Tokens de transição e pausa aparecem nos logs.
- **WorldLifecycle:** ~80  
  Reset por escopos com hooks/participants e emissão de `ResetCompleted`.
- **GameplayScene:** ~70  
  Reset e spawn funcional no gameplay, mas cobertura de targets ainda parcial.
- **Addressables (planejado):** ~0  
  Ainda não implementado; apenas diretrizes em documentação.

## O que ainda falta (provável parcial / não confirmado)
A pendência principal não é “o pipeline”, e sim **cobertura de feature** para todos os targets/grupos.

### 1) Targets/Grupos ainda sem evidência de QA / validação
- `GameplayResetTarget.EaterOnly`  
  Classificação agora é **Kind-first** (`ActorKind.Eater`) com fallback string-based (`EaterActor`),  
  mas ainda falta evidência de QA (não afirmar testado) e validação de componentes `IGameplayResettable`.
- `GameplayResetTarget.AllActorsInScene`  
  Falta evidência de: critérios claros de “ator”/alvo (ActorRegistry? scene scan?) + QA.
- `GameplayResetTarget.ActorIdSet`  
  Falta evidência de: resolução por ActorIds com comportamento determinístico + QA.

### 2) Implementação concreta do classifier/orchestrator
Para cravar “100% funcional” para todos os targets, ainda precisamos validar:
- Implementação concreta do `IGameplayResetTargetClassifier` (como ele resolve cada target).
- Implementação do `GameplayResetOrchestrator` (execução, filtros, ordenação, fallbacks).

### 3) Spawn ainda incompleto (efeito colateral na percepção do reset)
Mesmo com o reset por grupos funcional, a percepção de “mundo resetou” fica limitada enquanto:
- Spawn services não estiverem disponíveis/registrados (ou QA não usar spawn e sim spawner dedicado, como já foi feito).

## Próximos passos recomendados (para tornar “grupos” testável e completo)
1) **Adicionar QA por target** (um por vez):
   - QA `EaterOnly`: spawner + probe + request reset target EaterOnly.
   - QA `AllActorsInScene`: spawner (players + eaters + outros) + probe e validação de contagem.
   - QA `ActorIdSet`: spawner múltiplo + seleção de subset por ids + validação de subset.
2) **Revisar e padronizar** critérios do `IGameplayResetTargetClassifier`:
   - Fonte de verdade: `IActorRegistry` vs scene scan.
   - Regras de inclusão: o que conta como “ator resetável”? (ex.: presença de `IGameplayResettable`).
3) **Documentar invariantes** (determinismo):
   - Ordenação dos targets (por ActorId) + ordenação interna por `IGameplayResetOrder`.
4) Só depois: integrar com spawn “real” quando os spawn services estiverem estáveis.

## Artefatos relacionados (onde olhar primeiro)
- Logs confirmando:
  - `WorldLifecycleController` soft reset (Players) com gate + fases.
  - `GameplayResetQaSpawner` + `GameplayResetOrchestrator` + `GameplayResetQaProbe` para PlayersOnly.
- Código-chave a revisar para “fechar 100%”:
  - `IGameplayResetTargetClassifier` (implementação concreta)
  - `GameplayResetOrchestrator` (implementação concreta)
