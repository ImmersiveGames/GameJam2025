# Base 1.1 - Operational Surface and Input Capability Checkpoint

**Data**: 2026-05-12  
**Status**: Checkpoint consolidado  
**Escopo**: `OperationalSurfaceKind` + `SessionOperationalInputCapability`  
**Fonte operacional**: Log manual do fluxo `Boot -> Menu -> SessionActivitySandboxScene`.

---

## 1. Objetivo

Consolidar a separação entre:

```text
CompletionHandoff
```

e:

```text
OperationalSurfaceKind
```

e registrar a primeira validação operacional de UI/Input para rotas de menu/frontend.

---

## 2. Decisão consolidada

A Base 1.1 separa duas perguntas:

### 2.1 Para quem o pipeline entrega depois?

Representado por:

```text
CompletionHandoff
```

Valores atuais:

```text
NoHandoff
SessionActivityEntry
```

### 2.2 Que tipo de superfície operacional a rota representa?

Representado por:

```text
OperationalSurfaceKind
```

Valores atuais:

```text
None
FrontendMenu
SessionActivity
Overlay
LoadingOnly
```

Regra:

```text
NoHandoff não significa rota sem propósito.
NoHandoff significa apenas que não há transferência para outro pipeline.
```

---

## 3. Resultado implementado

Foi adicionado `OperationalSurfaceKind` em:

```text
Assets/_ImmersiveGames/NewScripts/SessionOperational/Pipeline/OperationalRouteAsset.cs
```

Com campo serializado:

```text
operationalSurfaceKind
```

E propriedade pública:

```text
OperationalSurfaceKind
```

---

## 4. Regras de validação da rota

### 4.1 SessionActivityEntry

```text
CompletionHandoff = SessionActivityEntry
-> OperationalSurfaceKind deve ser SessionActivity
```

### 4.2 NoHandoff

```text
CompletionHandoff = NoHandoff
-> OperationalSurfaceKind pode ser:
   - None
   - FrontendMenu
   - Overlay
   - LoadingOnly
```

Regra negativa:

```text
CompletionHandoff = NoHandoff
-> OperationalSurfaceKind não pode ser SessionActivity
```

---

## 5. SessionOperationalInputCapability

Foi adicionada preparação/validação mínima de UI/Input no `SessionOperationalPipeline`.

### 5.1 FrontendMenu

Para rotas:

```text
OperationalSurfaceKind = FrontendMenu
```

o pipeline valida:

```text
InitialInputMode = FrontendMenu
EventSystem.current existe
```

Se faltar `EventSystem`, a falha deve ser explícita:

```text
[FATAL][Config][SessionOperationalInputCapability]
```

Não há criação automática de `EventSystem`.

### 5.2 SessionActivity

Para rotas:

```text
OperationalSurfaceKind = SessionActivity
```

o pipeline não aplica regra de menu.

Resultado esperado:

```text
InputCapabilityPrepared
eventSystem='<not_required>'
outcome='observed_noop'
```

---

## 6. Evidência validada por log

### 6.1 Boot -> Menu

O log validou:

```text
routeIdentity='route-boot-menu'
completionHandoff='NoHandoff'
operationalSurfaceKind='FrontendMenu'
initialInputMode='FrontendMenu'
eventSystem='EventSystem'
```

Linha observada:

```text
[OBS][SessionOperationalPipeline][InputCapability] InputCapabilityPrepared ... operationalSurfaceKind='FrontendMenu' initialInputMode='FrontendMenu' eventSystem='EventSystem'
```

Leitura:

```text
Menu não tem handoff próprio.
Menu é uma superfície operacional FrontendMenu.
Menu exige UI/Input capability.
```

### 6.2 Menu -> SessionActivitySandboxScene

O log validou:

```text
routeIdentity='route-menu-gameplay'
completionHandoff='SessionActivityEntry'
operationalSurfaceKind='SessionActivity'
```

E a ordem operacional relevante:

```text
SceneCompositionCompleted
-> ActorPreparationStage
-> InputCapabilityPrepared observed_noop
-> MaterializationCompleted
-> LoadingCompleted / LoadingHidden
-> fadeOut
-> OperationalRouteCompleted
-> SessionActivityEntryHandoffEmitted
```

A `ActorPreparationStage` registrou:

```text
participationKind='ActorSetExpected'
plannedActors='1'
outcome='observed_noop'
```

O `InputCapability` registrou:

```text
operationalSurfaceKind='SessionActivity'
initialInputMode='ActivityDefault'
eventSystem='<not_required>'
outcome='observed_noop'
```

---

## 7. Invariantes preservadas

- Menu não ganhou handoff artificial.
- `CompletionHandoff` continua representando transferência de ownership.
- `OperationalSurfaceKind` representa a superfície operacional da rota.
- `FrontendMenu` exige `InitialInputMode=FrontendMenu`.
- `FrontendMenu` exige `EventSystem`.
- `EventSystem` não é actor.
- `EventSystem` não é criado por fallback silencioso.
- `SessionActivity` não é tratada como menu.
- `ActorPreparationStage` continua condicionada a `SessionActivityEntry`.
- `SessionActivityPipeline` permanece inalterado.
- Não houve materialização real de actors.
- Não houve uso do legado.

---

## 8. Arquivos alterados nesta frente

```text
Assets/_ImmersiveGames/NewScripts/SessionOperational/Pipeline/OperationalRouteAsset.cs
Assets/_ImmersiveGames/NewScripts/SessionOperational/Pipeline/SessionOperationalPipeline.cs
```

---

## 9. Validações manuais pendentes

### 9.1 Validações positivas

```text
FrontendMenu + InitialInputMode FrontendMenu + EventSystem
-> InputCapabilityPrepared OK

SessionActivity + InitialInputMode ActivityDefault
-> InputCapabilityPrepared observed_noop
-> eventSystem='<not_required>'
```

### 9.2 Validações negativas

```text
FrontendMenu + InitialInputMode != FrontendMenu
-> falha explícita

FrontendMenu sem EventSystem
-> falha explícita

SessionActivityEntry + OperationalSurfaceKind != SessionActivity
-> TryValidate falha

NoHandoff + OperationalSurfaceKind = SessionActivity
-> TryValidate falha
```

---

## 10. Resultado do checkpoint

A frente está fechada em nível de contrato inicial.

Estado final aceito:

```text
Boot -> Menu
CompletionHandoff = NoHandoff
OperationalSurfaceKind = FrontendMenu
InputCapabilityPrepared exige UI/EventSystem

Menu -> Activity
CompletionHandoff = SessionActivityEntry
OperationalSurfaceKind = SessionActivity
ActorPreparationStage roda
InputCapabilityPrepared observed_noop
SessionActivityEntryHandoff é emitido depois
```

---

## 11. Próxima frente recomendada

Próximo passo:

```text
ActorSetDefinitionAsset
```

Objetivo:

```text
Substituir actorSetIds: List<string> na rota por um asset canônico de authoring.
```

Ainda não implementar:

```text
- spawn
- materialização
- placement
- input binding de actor
- HUD binding
- camera binding
- readiness real
```
