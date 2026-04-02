# Fase 3.b — Generalização do contrato de `SceneComposition`

## Problema
O `SceneComposition` já entrou no trilho **local**, mas o contrato atual ainda está acoplado ao domínio de `LevelFlow`:
- `SceneCompositionRequest` depende de `LevelDefinitionAsset`
- `LevelSceneCompositionExecutor` aceita apenas `SceneCompositionScope.Local`
- o macro (`SceneFlow`) já possui o mesmo tipo de dado técnico necessário (`ScenesToLoad`, `ScenesToUnload`, `TargetActiveScene`), mas não consegue consumir o executor atual sem forçar um acoplamento indevido com `LevelFlow`

Resultado:
- o local já convergiu para o executor técnico comum
- o macro continua com execução própria no `SceneTransitionService`
- o núcleo técnico ainda não é realmente comum

---

## Causa
A Fase 2.c foi correta e conservadora: o contrato nasceu pequeno, focado no primeiro uso seguro.

Mas agora esse shape ficou estreito demais:
- **semântica local** vazou para o **contrato técnico**
- o executor não consegue receber um plano macro genérico
- `SceneTransitionService` continua executando diretamente `load/unload/active scene`

---

## Veredito
Antes de migrar o macro, é obrigatório **generalizar o contrato**.

A regra correta daqui para frente é:

- `SceneComposition` = executor técnico genérico de composição de cenas
- `LevelFlow` = dono da semântica local
- `SceneFlow/Navigation` = dono da semântica macro e do ciclo de transição
- `SceneCompositionRequest` = plano técnico, sem conhecer `LevelDefinitionAsset`, `RouteId`, `LevelId`, loading ou fade

---

## Direção arquitetural aprovada

### Executor técnico único
`Infrastructure/SceneComposition`

Responsável por:
- carregar cenas aditivas
- descarregar cenas
- definir cena ativa
- devolver resultado técnico observável

### Donos semânticos
- `LevelFlow` monta o **plano local**
- `SceneFlow/Navigation` monta o **plano macro**

### Donos de pipeline
- `SceneFlow` continua dono de:
  - loading
  - fade
  - readiness
  - dedupe/coalescing
  - completion gate
- `SceneComposition` não passa a mandar no ciclo macro; apenas executa o plano técnico quando chamado

---

## Contrato alvo

### 1. `SceneCompositionScope`
```csharp
public enum SceneCompositionScope
{
    Macro = 0,
    Local = 1
}
```

### 2. `SceneCompositionRequest`
```csharp
public readonly struct SceneCompositionRequest
{
    public SceneCompositionRequest(
        SceneCompositionScope scope,
        string reason,
        string correlationId,
        IReadOnlyList<string> scenesToLoad,
        IReadOnlyList<string> scenesToUnload,
        string activeScene)
    {
        Scope = scope;
        Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? string.Empty : correlationId.Trim();
        ScenesToLoad = scenesToLoad ?? Array.Empty<string>();
        ScenesToUnload = scenesToUnload ?? Array.Empty<string>();
        ActiveScene = string.IsNullOrWhiteSpace(activeScene) ? string.Empty : activeScene.Trim();
    }

    public SceneCompositionScope Scope { get; }
    public string Reason { get; }
    public string CorrelationId { get; }
    public IReadOnlyList<string> ScenesToLoad { get; }
    public IReadOnlyList<string> ScenesToUnload { get; }
    public string ActiveScene { get; }

    public bool HasOperations =>
        (ScenesToLoad?.Count ?? 0) > 0 ||
        (ScenesToUnload?.Count ?? 0) > 0 ||
        !string.IsNullOrWhiteSpace(ActiveScene);
}
```

### 3. `SceneCompositionResult`
```csharp
public readonly struct SceneCompositionResult
{
    public SceneCompositionResult(
        bool success,
        SceneCompositionScope scope,
        string reason,
        string correlationId,
        int scenesAdded,
        int scenesRemoved,
        string activeScene)
    {
        Success = success;
        Scope = scope;
        Reason = reason ?? string.Empty;
        CorrelationId = correlationId ?? string.Empty;
        ScenesAdded = scenesAdded;
        ScenesRemoved = scenesRemoved;
        ActiveScene = activeScene ?? string.Empty;
    }

    public bool Success { get; }
    public SceneCompositionScope Scope { get; }
    public string Reason { get; }
    public string CorrelationId { get; }
    public int ScenesAdded { get; }
    public int ScenesRemoved { get; }
    public string ActiveScene { get; }
}
```

### 4. `ISceneCompositionExecutor`
```csharp
public interface ISceneCompositionExecutor
{
    Task<SceneCompositionResult> ApplyAsync(SceneCompositionRequest request, CancellationToken ct = default);
}
```

---

## O que sai do contrato técnico

### Deve sair
- `LevelDefinitionAsset`
- `PreviousLevelRef`
- `TargetLevelRef`
- qualquer identidade de rota/level
- qualquer política de loading/fade

### Continua fora do contrato técnico
- `RouteId`
- `SceneRouteDefinition`
- `LevelRef`
- `LocalContentId`
- `SelectionVersion`

Esses dados continuam pertencendo à camada semântica.

---

## Impacto no local

### Antes
O local monta um request com:
- `previousLevelRef`
- `targetLevelRef`

### Depois
O local passa a montar um request com:
- `ScenesToLoad`
- `ScenesToUnload`
- `ActiveScene`

### Consequência
`LevelSceneCompositionExecutor` deixa de chamar diretamente `LevelAdditiveSceneRuntimeApplier` como “black box de level”.

Duas opções possíveis:

#### Opção A — preferida
Extrair do `LevelAdditiveSceneRuntimeApplier` helpers puros para:
- resolver cenas de um level
- aplicar load/unload/active scene

E então o executor usa essas partes.

#### Opção B — transitória
Criar um `LevelSceneCompositionRequestFactory` em `LevelFlow` que traduz `LevelRef -> SceneCompositionRequest`, preservando por um tempo parte do applier como helper interno.

**Recomendação:** começar pela **Opção B** para reduzir risco.

---

## Impacto no macro

### Estado atual
`SceneTransitionService` ainda executa diretamente:
- reload
- load
- set active scene
- unload

### Depois da 3.b
Ele ainda continua dono do pipeline macro, mas passa a poder delegar a execução técnica para `ISceneCompositionExecutor` porque o contrato já será genérico.

**Importante:**
A 3.b **não** faz essa migração ainda.
Ela apenas abre o caminho seguro para a 3.c.

---

## Arquivos envolvidos na implementação da 3.b

### Infra — novos/alterados
- `Infrastructure/SceneComposition/SceneCompositionRequest.cs`
- `Infrastructure/SceneComposition/SceneCompositionResult.cs`
- `Infrastructure/SceneComposition/ISceneCompositionExecutor.cs`
- `Infrastructure/SceneComposition/LevelSceneCompositionExecutor.cs`

### Local — alterados
- `Modules/LevelFlow/Runtime/LevelSwapLocalService.cs`
- `Modules/LevelFlow/Runtime/LevelMacroPrepareService.cs`
- possivelmente novo helper:
  - `Modules/LevelFlow/Runtime/LevelSceneCompositionRequestFactory.cs`

### Local — possível apoio
- `Modules/LevelFlow/Runtime/LevelAdditiveSceneRuntimeApplier.cs`

### Macro — ainda não migra, mas será consumidor depois
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionRequest.cs`
- `Modules/SceneFlow/Navigation/Runtime/SceneRouteDefinition.cs`

---

## Recomendação de implementação

### Escopo da 3.b
1. generalizar `SceneCompositionRequest`
2. generalizar `SceneCompositionResult`
3. manter `ISceneCompositionExecutor` estável
4. adaptar o local para continuar funcionando com o contrato novo
5. **não** migrar o macro ainda

### Fora do escopo da 3.b
- loading/fade
- readiness gate
- reescrever `SceneTransitionService`
- mexer no snapshot semântico além do necessário para compilar
- trazer `WorldLifecycle` para essa fase

---

## Critérios de aceite
A 3.b termina quando:
- `SceneCompositionRequest` não depende mais de `LevelDefinitionAsset`
- o local continua funcional usando o contrato novo
- `SceneCompositionScope.Local` permanece funcionando
- o macro ainda não migrou, mas já consegue ser mapeado diretamente para o contrato novo sem adaptação conceitual estranha
- nenhuma semântica de rota ou level vazou para o executor técnico

---

## Próxima fase depois da 3.b

### Fase 3.c — migração macro
Objetivo:
- manter `SceneFlow` como dono do pipeline macro
- tirar de `SceneTransitionService` a aplicação técnica de cenas
- delegar a execução técnica para `ISceneCompositionExecutor`

Isso só fica seguro depois que o contrato estiver genérico.
