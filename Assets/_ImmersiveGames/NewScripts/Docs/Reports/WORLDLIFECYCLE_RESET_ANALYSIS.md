> Moved from `Infrastructure/WorldLifecycle/WORLDLIFECYCLE_RESET_ANALYSIS.md` on 2025-12-29.

# WorldLifecycle + Gameplay Reset — Análise consolidada (do chat)

Data: 2025-12-26  
Escopo: NewScripts — `WorldLifecycle/Reset` + `Gameplay/Reset`

## 1) Status atual (macro) — o que está funcional

### 1.1 WorldLifecycle Reset (macro-fases)
Confirmado por logs recentes:
- O `WorldLifecycleController` executa **soft reset** por escopo (ex.: Players) sem quebrar o pipeline.
- O `WorldLifecycleOrchestrator` mantém o macro-fluxo:
  - Gate Acquire/Release (`flow.soft_reset`)
  - Hooks: `OnBeforeDespawn` → `OnAfterDespawn` → `OnBeforeSpawn` → `OnAfterSpawn` (quando há hooks)
  - Fases de `Despawn` e `Spawn` executam (ou **SKIP** com warning se não houver spawn services).
- Mesmo quando o spawn ainda não está completo, o reset “grande” permanece resiliente.

### 1.2 Gameplay Reset (targets) — testável sem spawn real
Foi criado um QA dedicado que prova a execução completa das fases de reset no alvo `PlayersOnly`:
- `GameplayResetQaSpawner` cria Players de QA (ex.: 2).
- `GameplayResetOrchestrator` resolve targets e executa:
  - `Cleanup` → `Restore` → `Rebind`
- `GameplayResetQaProbe` confirma que cada fase é chamada por target.

Isso resolve o problema observado anteriormente: “spawn não 100%” não impede validar a feature de reset.

## 2) Renomes / Normalização (tabela)

Abaixo, um mapa prático do que foi normalizado (visão do chat; usar como referência para evitar reintroduzir aliases):

| Antes (conceito antigo) | Agora (NewScripts) | Observação |
|---|---|---|
| `IGameplayResetParticipant` | `IGameplayResettable` | Contrato assíncrono por fases (`ResetCleanupAsync/ResetRestoreAsync/ResetRebindAsync`). |
| `IGameplayResetParticipant` (sync) | `IGameplayResettableSync` | Fallback síncrono, adaptável para Task pelo orchestrator. |
| `IGameplayResetScopeFilter` | `IGameplayResetTargetFilter` | O filtro passou a operar sobre *Target* (não “scope”). |
| `GameplayResetScope` | `GameplayResetTarget` | Targets: `AllActorsInScene`, `PlayersOnly`, `EaterOnly`, `ActorIdSet`. |
| `Reset_CleanupAsync/Reset_RestoreAsync/Reset_RebindAsync` | `ResetCleanupAsync/ResetRestoreAsync/ResetRebindAsync` | Padronização de nomenclatura. |
| Aliases para interfaces legadas | Removidos | “Tudo foi portado para os novos nomes”. |

## 3) O que ainda falta (principalmente “grupos”)

Pelo que foi acordado no chat, o soft reset por escopo está “razoavelmente bem”, mas a parte de **targets/grupos** precisa ficar tão funcional e testável quanto o escopo soft.

### 3.1 Lacunas típicas para “grupos”
1. **Definição formal do que é “grupo”**
   - É tag? componente? enum? string?
2. **Classificação determinística**
   - `IGameplayResetTargetClassifier` precisa definir regras claras:
     - fonte de targets (`IActorRegistry` vs scan de cena)
     - inclusão (o que conta como resetável)
     - ordenação (por `ActorId`, nome, instância, etc.)
3. **Request/contrato**
   - `GameplayResetRequest` hoje suporta `ActorIds` e `Target`.
   - Falta um caminho canônico para “grupo(s)” (ex.: `GroupIds`).

## 4) Plano para tornar “grupos” testável e funcional (mesmo nível de PlayersOnly)

### Passo 1 — Definir o contrato mínimo de “grupo”
Escolha incremental (baixa fricção):
- Adicionar um *marker* opcional em gameplay:
  - `IGameplayResetGroupProvider { string GroupId { get; } }`  
  ou
  - `GameplayResetGroupTag : MonoBehaviour` com `GroupId` serializado

E estender o request com:
- `IReadOnlyList<string> GroupIds` (nulo/empty quando não aplicável)
- Novo enum: `GameplayResetTarget.GroupIdSet` (ou similar)

### Passo 2 — Implementar resolução de targets no classifier
Responsabilidade única do `IGameplayResetTargetClassifier`:
- Para `PlayersOnly`: usar `IActorRegistry` e filtrar por tipo de ator (ex.: `PlayerActor`) ou por marker.
- Para `ActorIdSet`: resolver por `actorIds` e ignorar o resto.
- Para `GroupIdSet`: coletar apenas objetos/atores com `GroupId` em `GroupIds`.

**Determinismo obrigatório:**
- Ordenar targets por:
  1) `ActorId` (quando houver)
  2) nome do tipo + nome do GameObject
  3) InstanceID como tie-break final

### Passo 3 — QA dedicado para grupos (não depende de spawn real)
Novo QA (similar ao PlayersOnly QA):
- Spawn de QA cria:
  - 4 targets, distribuídos em 2 grupos (ex.: “G_A”, “G_B”)
  - adiciona `GameplayResetQaProbe` em todos
- Testes:
  - Request `GroupIdSet=[G_A]` → apenas 2 probes devem registrar chamadas
  - Request `GroupIdSet=[G_B]` → apenas as outras 2
  - Request `GroupIdSet=[G_A,G_B]` → todas
  - Request de grupo inexistente → zero targets, sem erro

### Passo 4 — Bridge com WorldLifecycle (opcional e posterior)
Quando o conceito de grupos estiver validado em gameplay:
- Criar um `IResetScopeParticipant` adicional (ex.: `GroupResetParticipant`) que constrói `GameplayResetRequest(Target=GroupIdSet, GroupIds=...)`.
- Mantém a mesma filosofia do `PlayersResetParticipant`: WorldLifecycle delega, gameplay executa.

## 5) Observações rápidas sobre o log
- O pipeline de Start Request (GameLoopSceneFlowCoordinator → SceneTransition → ScenesReady → RequestStart) está consistente.
- `WorldLifecycleRuntimeCoordinator` está fazendo SKIP no profile `startup` (esperado para frontend).
- `GameplayResetQaSpawner` confirmou execução das 3 fases em 2 players.

## 6) Arquivo relacionado (status anterior)
- O arquivo gerado anteriormente no projeto: `WORLDLIFECYCLE_RESET_STATUS.md` (contém um snapshot macro e pendências).
