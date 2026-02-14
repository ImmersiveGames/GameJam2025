# ADR-0014 — GameplayReset: Targets por Grupos

## Status

- Estado: Implementado
- Data (decisão): 2026-02-01
- Última atualização: 2026-02-04
- Tipo: Implementação
- Escopo:
  - `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Runtime/RunRearm/Core/*`
  - `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Runtime/RunRearm/Core/*`

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

Introduzir um reset de gameplay **orientado por alvos**, com três peças explícitas:

1) **Classificação de targets** (actor registry → lista de atores)
2) **Participantes de reset** (componentes `IRunRearmable` por ator)
3) **Orquestração determinística** (ordenação e execução por etapas)

A seleção de targets passa a ser:

- **Explicitamente resolvida** a partir de fontes existentes (principalmente `IActorRegistry`),
- **Classificada** em `RunRearmTarget` (ex.: `PlayersOnly`, `EaterOnly`, `ByActorKind`),
- **Executada** por componentes `IRunRearmable` encontrados no root de cada ator.

### Não-objetivos (resumo)

- Definir novos alvos fora de `RunRearmTarget` sem ADR.
- Substituir o WorldLifecycle reset pipeline.

## Contratos

Os contratos ficam em `RunRearmContracts.cs`:

- `RunRearmStep` (enum)
- `RunRearmTarget` (enum)
- `RunRearmRequest` (target + reason + actorIds/kind)
- `RunRearmContext` (contexto do reset)
- `IRunRearmable` / `IRunRearmableSync`
- `IRunRearmOrder` / `IRunRearmTargetFilter`
- `IRunRearmTargetClassifier` (registry → lista de atores)
- `IRunRearmOrchestrator` (coordena o reset)

## Regras e invariantes

### 1) Somente fatos do projeto

- A implementação **não deve inventar** propriedades/funções/assinaturas.
- Quando faltar um tipo ou serviço, o comportamento deve ser **degradar** (log/skip) em vez de “criar contrato novo”.

### 2) Ordem determinística

No `RunRearmOrchestrator`:

- Os targets são ordenados por `ActorId` (ordenação estável).
- Para cada target, os componentes são ordenados por `IRunRearmOrder` e nome do tipo.
- As etapas são executadas em ordem fixa: `Cleanup → Restore → Rebind`.

### 3) Fail-soft e auditabilidade

- Se não houver targets resolvidos: Strict pode falhar; Release degrada com log explícito.
- Se um ator está inválido/nulo: **ignorar**.
- Se um target não tiver componentes resetáveis: **log + skip** daquele target.

### 4) Integração com “ResetWorld”

- O reset de gameplay é executado **apenas quando o profile exige gameplay** (ex.: `Menu → Gameplay`).
- Em “frontend/profile=frontend” o reset pode ser **SKIP** (conforme baseline atual).

## Implementação

### Implementação default (sem assets)

A implementação atual é intencionalmente “mínima e concreta”:

- `DefaultRunRearmTargetClassifier.cs`
  - Resolve alvos via `IActorRegistry` conforme `RunRearmTarget`.
  - Suporta fallback string-based para `EaterOnly` (compatibilidade).
  - Não depende de enums externos além de `ActorKind`.

- `PlayersRunRearmWorldParticipant.cs`
  - Ponte do WorldLifecycle (ResetScope.Players) para RunRearm (`PlayersOnly`).

- `RunRearmOrchestrator.cs`
  - Resolve registry/classifier/policy via DI.
  - Usa registry-first com fallback de scan apenas quando policy permitir.
  - Resolve componentes `IRunRearmable` por target e executa etapas.

### Registro no DI

O binding fica no bootstrap de cena:

- `SceneScopeCompositionRoot.cs`
  - Registra `IRunRearmOrchestrator` e `IRunRearmTargetClassifier`.

### QA Driver (opcional)

- `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Editor/RunRearm/RunRearmRequestDevDriver.cs`
  - Dispara reset via `ContextMenu`.
  - Loga targets e report.

## Fora de escopo

- Criar novo sistema de discovery fora de registry/scan opt-in.
- Alterar políticas Strict/Release fora do reset.

## Consequências

- **Pró:** reset previsível, auditável e extensível (novo grupo = novo participant).
- **Pró:** reduz dependência de “scan de cena” e de regras implícitas.
- **Contra:** exige disciplina para registrar participantes e manter o classifier alinhado com os tipos reais.

## Evidências / validação

- **Última evidência (log bruto):** `Docs/Reports/lastlog.log`

- Compilação: todas as referências usadas por ADR-0014 existem no `output14`.
- Integração: `SceneScopeCompositionRoot` registra os serviços; QA driver permite validar manualmente.
- Auditoria: o `ADR-Sync-Audit-NewScripts.md` aponta os touchpoints.

## Touchpoints (para evitar regressões)

Quando editar o reset de gameplay, revisar também:

- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/SceneScopeCompositionRoot.cs` (DI)
- `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Runtime/RunRearm/Core/RunRearmOrchestrator.cs` (orquestração)
- `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Runtime/RunRearm/Interop/PlayersRunRearmWorldParticipant.cs` (bridge WorldLifecycle)
- `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Editor/RunRearm/RunRearmRequestDevDriver.cs` (QA)
- `NewScripts/Docs/Reports/Audits/*/ADR-Sync-Audit-NewScripts.md` (auditoria)

## Implementação (arquivos impactados)

### Runtime / Editor (código e assets)

- **NewScripts**
  - `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Runtime/RunRearm/Core/RunRearmContracts.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Runtime/RunRearm/Core/DefaultRunRearmTargetClassifier.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Runtime/RunRearm/Core/RunRearmOrchestrator.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Runtime/RunRearm/Core/ActorKindMatching.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Runtime/RunRearm/Interop/PlayersRunRearmWorldParticipant.cs`
  - `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/SceneScopeCompositionRoot.cs`
  - `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Editor/RunRearm/RunRearmRequestDevDriver.cs`

### Docs / evidências relacionadas

- `NewScripts/Docs/Reports/Audits/*/ADR-Sync-Audit-NewScripts.md`
