# ADR-0014 — GameplayReset: Targets por Grupos

## Status

- Estado: Implementado
- Data (decisão): 2026-02-01
- Última atualização: 2026-02-01
- Escopo: `Assets/_ImmersiveGames/NewScripts/Gameplay/CoreGameplay/Reset/*`

## Contexto

O fluxo de `ResetWorld` (WorldLifecycle) precisa executar um **reset de gameplay** de forma:

- **Determinística** (mesmas regras e ordem de execução entre execuções)
- **Auditável** (é possível rastrear, via logs e evidências, o que foi resetado e por quê)
- **Com fail-soft** (quando algo esperado não está presente, o reset não deve quebrar o ciclo inteiro)

O problema recorrente era o reset depender de “descobertas” dinâmicas (scan de cena, `FindObjectsOfType`, etc.), o que:

- introduz **não-determinismo** e custo;
- dificulta auditoria (não fica claro *quem* foi resetado);
- leva a “invenções” de contratos (nomes/propriedades inexistentes) quando o código tenta adivinhar o que existe.

## Decisão

Introduzir um reset de gameplay **orientado por grupos**, com três peças explícitas:

1) **Classificação de targets** (ator → grupo)
2) **Participantes de reset** (um handler por grupo)
3) **Orquestração determinística** (ordenação e execução)

A seleção de targets passa a ser:

- **Explicitamente resolvida** a partir de fontes existentes (principalmente `IActorRegistry`),
- **Classificada** em `GameplayResetGroup` (ex.: `Players`, `Eaters`),
- **Executada** por participantes registrados no DI.

## Contratos

Os contratos ficam em `GameplayResetContracts.cs`:

- `GameplayResetGroup` (enum)
- `GameplayResetReason` (campos: `contextSignature`, `reason`)
- `GameplayResetRequest` (targets opcionais, `reason`)
- `IGameplayResetTargetClassifier` (ator → grupo)
- `IGameplayResetParticipant` (handler por grupo)
- `IGameplayResetOrchestrator` (coordena o reset)

## Regras e invariantes

### 1) Somente fatos do projeto

- A implementação **não deve inventar** propriedades/funções/assinaturas.
- Quando faltar um tipo ou serviço, o comportamento deve ser **degradar** (log/skip) em vez de “criar contrato novo”.

### 2) Ordem determinística

No `GameplayResetOrchestrator`:

- Os targets são agrupados por `GameplayResetGroup`.
- Dentro de cada grupo, a lista é ordenada por `ActorId` (ordenação estável).
- Os grupos são executados em ordem previsível (por enum e depois por chave).

### 3) Fail-soft e auditabilidade

- Se um grupo não tem participante registrado: **log + skip** daquele grupo.
- Se um ator está inválido/nulo: **ignorar**.
- O reset retorna um `GameplayResetReport` com status por grupo.

### 4) Integração com “ResetWorld”

- O reset de gameplay é executado **apenas quando o profile exige gameplay** (ex.: `Menu → Gameplay`).
- Em “frontend/profile=frontend” o reset pode ser **SKIP** (conforme baseline atual).

## Implementação

### Implementação default (sem assets)

A implementação atual é intencionalmente “mínima e concreta”:

- `DefaultGameplayResetTargetClassifier.cs`
  - Classifica `PlayerActor` como `GameplayResetGroup.Players`.
  - Classifica `EaterActor` como `GameplayResetGroup.Eaters`.
  - Não depende de enums externos (`Mode`, `StrictRelease`, etc.).

- `PlayersResetParticipant.cs`
  - Reseta o player via o contrato existente em `PlayerActor`.

- `GameplayResetOrchestrator.cs`
  - Resolve classifier + participants via DI.
  - Gera batches determinísticos.
  - Emite logs de execução e preenche `GameplayResetReport`.

### Registro no DI

O binding fica no bootstrap de cena:

- `SceneBootstrapper.cs`
  - Registra `IGameplayResetOrchestrator`, `IGameplayResetTargetClassifier` e participantes.

### QA Driver (opcional)

- `QA/GameplayReset/GameplayResetRequestQaDriver.cs`
  - Dispara reset via `ContextMenu`.
  - Loga targets e report.

## Consequências

- **Pró:** reset previsível, auditável e extensível (novo grupo = novo participant).
- **Pró:** reduz dependência de “scan de cena” e de regras implícitas.
- **Contra:** exige disciplina para registrar participantes e manter o classifier alinhado com os tipos reais.

## Evidências / validação

- Compilação: todas as referências usadas por ADR-0014 existem no `output14`.
- Integração: `SceneBootstrapper` registra os serviços; QA driver permite validar manualmente.
- Auditoria: o `ADR-Sync-Audit-NewScripts.md` aponta os touchpoints.

## Touchpoints (para evitar regressões)

Quando editar o reset de gameplay, revisar também:

- `NewScripts/Runtime/Bootstrap/SceneBootstrapper.cs` (DI)
- `NewScripts/QA/GameplayReset/GameplayResetRequestQaDriver.cs` (QA)
- `NewScripts/Docs/Reports/Audits/*/ADR-Sync-Audit-NewScripts.md` (auditoria)

## Implementação (arquivos impactados)

### Runtime / Editor (código e assets)

- **NewScripts**
  - `NewScripts/Runtime/Bootstrap/SceneBootstrapper.cs`
  - `NewScripts/QA/GameplayReset/GameplayResetRequestQaDriver.cs`
- **QA**
  - `QA/GameplayReset/GameplayResetRequestQaDriver.cs`

### Docs / evidências relacionadas

- `NewScripts/Docs/Reports/Audits/*/ADR-Sync-Audit-NewScripts.md`
