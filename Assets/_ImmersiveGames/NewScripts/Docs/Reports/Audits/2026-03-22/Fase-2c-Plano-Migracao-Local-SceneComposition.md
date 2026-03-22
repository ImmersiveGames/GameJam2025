# Fase 2.c — Migração do trilho local para `SceneComposition`

## Resumo
Nesta fase, o objetivo **não** é migrar o macro nem redesenhar o restart.

O corte seguro é:
- introduzir o núcleo técnico `SceneComposition`
- manter `LevelFlow` como dono semântico do swap local
- fazer `LevelFlow` parar de chamar `LevelAdditiveSceneRuntimeApplier` diretamente
- usar um executor técnico único por interface
- deixar `GameplayStartSnapshot` e `RestartContext` **sem mudança estrutural** nesta fase
- deixar `ContentSwap` **intocado** nesta fase

---

## Problema
Hoje o swap local de conteúdo continua acoplado diretamente ao `LevelFlow`.

`LevelSwapLocalService` e `LevelMacroPrepareService` fazem:
1. decidir semanticamente qual level deve estar ativo
2. publicar `LevelSelectedEvent`
3. pedir `ResetLevelAsync`
4. aplicar a troca técnica das cenas via `LevelAdditiveSceneRuntimeApplier`

Isso deixa a composição local presa ao módulo semântico, e impede a criação de um executor técnico único reutilizável.

---

## Causa
Ainda não existe uma capability canônica para:
- aplicar composição local de cenas
- receber um request técnico estável
- devolver um resultado técnico padronizado

O código atual usa um helper estático (`LevelAdditiveSceneRuntimeApplier`) como executor concreto embutido no `LevelFlow`.

---

## Arquivos envolvidos

### Novos arquivos
- `NewScripts/Infrastructure/SceneComposition/SceneCompositionScope.cs`
- `NewScripts/Infrastructure/SceneComposition/SceneCompositionRequest.cs`
- `NewScripts/Infrastructure/SceneComposition/SceneCompositionResult.cs`
- `NewScripts/Infrastructure/SceneComposition/ISceneCompositionExecutor.cs`
- `NewScripts/Infrastructure/SceneComposition/LevelSceneCompositionExecutor.cs`
- `NewScripts/Infrastructure/Composition/GlobalCompositionRoot.SceneComposition.cs`

### Arquivos alterados
- `NewScripts/Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs`
- `NewScripts/Modules/LevelFlow/Runtime/LevelSwapLocalService.cs`
- `NewScripts/Modules/LevelFlow/Runtime/LevelMacroPrepareService.cs`
- `NewScripts/Docs/Reports/Audits/LATEST.md`

### Arquivos preservados sem alteração funcional nesta fase
- `NewScripts/Modules/LevelFlow/Runtime/LevelAdditiveSceneRuntimeApplier.cs`
- `NewScripts/Modules/LevelFlow/Runtime/GameplayStartSnapshot.cs`
- `NewScripts/Modules/LevelFlow/Runtime/RestartContextService.cs`
- `NewScripts/Modules/Navigation/LevelSelectedRestartSnapshotBridge.cs`
- `NewScripts/Modules/ContentSwap/Runtime/*`

---

## Ação recomendada

### 1. Criar o núcleo técnico novo
Criar `Infrastructure/SceneComposition` com o contrato mínimo:

```csharp
public enum SceneCompositionScope
{
    Macro = 0,
    Local = 1
}
```

```csharp
public readonly struct SceneCompositionRequest
{
    public SceneCompositionScope Scope { get; }
    public string Reason { get; }
    public string CorrelationId { get; }
    public LevelDefinitionAsset PreviousLevelRef { get; }
    public LevelDefinitionAsset TargetLevelRef { get; }
}
```

```csharp
public readonly struct SceneCompositionResult
{
    public bool Success { get; }
    public SceneCompositionScope Scope { get; }
    public string Reason { get; }
    public string CorrelationId { get; }
    public int ScenesAdded { get; }
    public int ScenesRemoved { get; }
}
```

```csharp
public interface ISceneCompositionExecutor
{
    Task<SceneCompositionResult> ApplyAsync(SceneCompositionRequest request, CancellationToken ct = default);
}
```

### Observação
Nesta fase, o request ainda pode carregar `LevelDefinitionAsset previous/target`.
Isso **não é o shape final de longo prazo**, mas é o menor corte seguro para desacoplar o `LevelFlow` do applier estático sem abrir o macro agora.

---

### 2. Criar o primeiro executor concreto
Criar `LevelSceneCompositionExecutor` em `Infrastructure/SceneComposition`.

Responsabilidade:
- receber `SceneCompositionRequest`
- validar `Scope == Local`
- delegar internamente para `LevelAdditiveSceneRuntimeApplier.ApplyAsync(previous, target, ct)`
- devolver `SceneCompositionResult`

### Regra importante
O executor novo **não** decide qual level usar.
Ele apenas aplica o pedido técnico recebido.

---

### 3. Trocar o consumo no `LevelSwapLocalService`
Remover o acoplamento direto com:
- `LevelAdditiveSceneRuntimeApplier.ApplyAsync(...)`

Passar a depender de:
- `ISceneCompositionExecutor`

Fluxo continua:
1. validar snapshot atual
2. validar level alvo
3. publicar `LevelSelectedEvent`
4. chamar `ResetLevelAsync`
5. montar `SceneCompositionRequest`
6. chamar `_sceneCompositionExecutor.ApplyAsync(...)`
7. publicar `LevelSwapLocalAppliedEvent`

### O que não muda
- ownership do swap local continua em `LevelFlow`
- snapshot continua sendo atualizado por `LevelSelectedRestartSnapshotBridge`
- reset continua vindo de `IWorldResetCommands`

---

### 4. Trocar o consumo no `LevelMacroPrepareService`
Remover o acoplamento direto com:
- `LevelAdditiveSceneRuntimeApplier.ApplyAsync(...)`
- `LevelAdditiveSceneRuntimeApplier.ClearAsync(...)`

Nesta fase, o executor novo deve expor **também** um método técnico para limpar composição local **ou** aceitar request com `TargetLevelRef = null` e `Scope = Local`.

### Recomendação de corte seguro
Para manter a interface limpa, adicionar um segundo método apenas nesta fase pode ser pior.

Então a forma mais segura é:
- `SceneCompositionRequest` permitir `TargetLevelRef = null` para `clear`
- o executor tratar:
  - `previous != null && target != null` → apply
  - `target == null` → clear

Isso mantém um único trilho técnico.

---

## Não objetivos da Fase 2.c
- não migrar o macro para `SceneComposition`
- não alterar `GameplayStartSnapshot`
- não introduzir `LocalContentId` ainda
- não redesenhar `RestartContext`
- não integrar `ContentSwap`
- não mover `LevelAdditiveSceneRuntimeApplier` para fora do `LevelFlow` ainda

---

## Critérios de aceite
A Fase 2.c termina quando estas condições forem verdadeiras:

- `LevelSwapLocalService` não chama mais `LevelAdditiveSceneRuntimeApplier` diretamente
- `LevelMacroPrepareService` não chama mais `LevelAdditiveSceneRuntimeApplier` diretamente
- existe `ISceneCompositionExecutor` registrado no composition root
- o primeiro executor concreto reutiliza o applier atual internamente
- o comportamento funcional do trilho local continua o mesmo
- `GameplayStartSnapshot` e `RestartContext` continuam sem regressão

---

## Risco de regressão

### Baixo
- introduzir interface nova
- adaptar `LevelSwapLocalService`
- adaptar `LevelMacroPrepareService`
- registrar executor no composition root

### Médio
- modelar clear no mesmo request
- manter logs/assinaturas sem perder rastreabilidade

### Alto
- tentar já migrar macro na mesma fase
- mexer em restart/snapshot agora
- reaproveitar `ContentSwap` já nesta etapa

---

## Ordem exata recomendada
1. criar contratos de `SceneComposition`
2. criar `LevelSceneCompositionExecutor`
3. registrar no composition root
4. migrar `LevelSwapLocalService`
5. migrar `LevelMacroPrepareService`
6. rodar validação funcional

---

## Validação recomendada

### Cenários mínimos
- boot -> menu -> gameplay
- gameplay -> swap local de level
- gameplay -> restart
- gameplay -> exit to menu
- entrada em gameplay por macro route gameplay sem snapshot atual
- entrada em gameplay por macro route gameplay com snapshot existente

### Anchors esperados
Os logs do `LevelFlow` devem continuar mostrando:
- `LevelSelectedEventPublished`
- `ResetLevel` canônico
- `LevelApplied` / `LevelSwapLocalApplied`

O importante é que o comportamento fique igual, mas o executor técnico passe a ser resolvido por interface.

---

## Próxima fase depois da 2.c
### Fase 2.d
Evoluir o snapshot semântico:
- `GameplayStartSnapshot`
- `RestartContext`
- eventual `LocalContentId`

### Fase 2.e
Trazer o trilho macro para o mesmo executor técnico, sem tirar de `SceneFlow` o ownership de loading/fade.
