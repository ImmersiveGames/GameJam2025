# Fase 2.a — Auditoria de consumidores de RuntimeMode / Degraded

## 1. Resumo executivo

`Infrastructure/RuntimeMode` e uma infraestrutura legitima e permanece no lugar certo.

O problema real nao esta no modulo em si, e sim em consumidores que ainda burlam o trilho canonico de `RuntimePolicy` / `IRuntimeModeProvider` / `IDegradedModeReporter`.

O caso mais critico e `ContentSwap`, que ainda instancia `UnityRuntimeModeProvider` e `DegradedModeReporter` diretamente fora do composition root. Ha um segundo cheiro menor em `Gameplay/ActorGroupRearm`, que recompõe `ProductionWorldResetPolicy` localmente quando o contrato nao vem do DI.

## 2. Veredito

- `RuntimeMode` permanece em `Infrastructure`.
- `DegradedModeReporter` continua funcional, mas conceitualmente esta mais proximo de `Observability` do que de `RuntimeMode`.
- O primeiro alvo de correcao e `Modules/ContentSwap/Runtime/InPlaceContentSwapService.cs`.
- O segundo alvo de correcao e `Modules/Gameplay/Runtime/ActorGroupRearm/Core/ActorGroupRearmOrchestrator.cs`.

## 3. Matriz da auditoria

| Caso | Status | Observacao |
|---|---|---|
| `Modules/ContentSwap/Runtime/InPlaceContentSwapService.cs` | ERRADO | Instancia `UnityRuntimeModeProvider` e `DegradedModeReporter` direto. |
| `Modules/Gameplay/Runtime/ActorGroupRearm/Core/ActorGroupRearmOrchestrator.cs` | PARCIAL | Recompõe `ProductionWorldResetPolicy` localmente se DI falhar. |
| `Infrastructure/Composition/GlobalCompositionRoot.RuntimePolicy.cs` | ACEITAVEL | Fallback interno redundante, mas ainda dentro do composition root. |
| `Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs` | CERTO | Consome contratos por construtor. |
| `Modules/WorldLifecycle/Runtime/ProductionWorldResetPolicy.cs` | CERTO | Consome contratos por construtor. |
| `Modules/Audio/**` e `Infrastructure/InputModes/**` | CERTO | Uso por DI/composition, sem instancia local de reporter/provider. |

## 4. Problema

Ha consumidores fora do trilho canonico de `RuntimePolicy` que recriam ou recompõem comportamento de runtime/degraded localmente.

## 5. Causa

Os contratos canonicos existem e estao registrados no boot, mas alguns modulos ainda mantem fallback local defensivo:

- `new UnityRuntimeModeProvider()`
- `new DegradedModeReporter()`
- `new ProductionWorldResetPolicy(...)`

Isso abre caminhos paralelos e enfraquece o ownership do boot/config.

## 6. Arquivos envolvidos

### Prioridade 1
- `Modules/ContentSwap/Runtime/InPlaceContentSwapService.cs`

### Prioridade 2
- `Modules/Gameplay/Runtime/ActorGroupRearm/Core/ActorGroupRearmOrchestrator.cs`

### Prioridade 3
- `Infrastructure/Composition/GlobalCompositionRoot.RuntimePolicy.cs`
- `Infrastructure/RuntimeMode/DegradedModeReporter.cs`

## 7. Acao recomendada

### 7.1. ContentSwap
Remover a instancia local de `UnityRuntimeModeProvider` e `DegradedModeReporter`.

Regra alvo:
- modulo consumidor tenta resolver o contrato canonico
- se faltar contrato obrigatorio em runtime, falha explicitamente ou entra em degraded controlado
- modulo consumidor nao cria a propria infraestrutura de runtime/degraded

### 7.2. Gameplay / ActorGroupRearm
Parar de recompôr `ProductionWorldResetPolicy` localmente no consumidor.

Regra alvo:
- `ActorGroupRearmOrchestrator` depende do contrato canonico
- policy nao e remontada em fallback dentro do modulo consumidor

### 7.3. RuntimePolicy interno
Depois de corrigir consumidores, enxugar redundancia dentro do composition root.

Regra alvo:
- composition root continua sendo o unico lugar aceitavel para fallback de bootstrap
- consumers nao repetem esse comportamento

## 8. Risco de regressao

### Alto
- manter `ContentSwap` criando reporter/provider proprio e divergir silenciosamente da policy global.

### Medio
- corrigir `ActorGroupRearmOrchestrator` sem revisar quem garante `IWorldResetPolicy` no boot.

### Baixo
- enxugar fallback interno do `RuntimePolicy` depois que os consumers estiverem limpos.

## 9. Resultado esperado da fase

Ao fechar a Fase 2.a:

- nenhum modulo de dominio/aplicacao instancia `UnityRuntimeModeProvider` diretamente
- nenhum modulo de dominio/aplicacao instancia `DegradedModeReporter` diretamente
- `RuntimeMode` permanece em `Infrastructure`
- `ContentSwap` deixa de burlar o `RuntimePolicy` canonico

## 10. Proximo passo natural

Usar esta auditoria como pre-condicao para a Fase 2.b de desenho de contrato de composicao de cenas, sem carregar bypass de runtime/degraded para a arquitetura nova.
