# Fase 2.b — Contrato alvo para SceneComposition

## 1. Resumo executivo

A intencao arquitetural aprovada e:

- `SceneFlow/Navigation` continuam donos da semantica macro
- `LevelFlow` continua dono da semantica local
- `RestartContext` continua dono do snapshot restauravel
- uma capability tecnica unica executa a composicao de cenas

O nome recomendado para essa capability e `SceneComposition`.

A recomendacao e **nao** expandir o `ContentSwap` atual como base do desenho novo. O estado atual dele e de contexto/evento in-place; nao de executor tecnico canonico de composicao de cenas.

## 2. Problema

Hoje ha trilhos conceitualmente sobrepostos:

- `SceneFlow/Navigation` trocam composicao macro de cenas
- `LevelFlow` troca conteudo local in-place
- `ContentSwap` existe como contexto/telemetria de swap, mas nao executa a troca tecnica real

Isso gera:
- duplicacao de responsabilidade
- snapshot semantico incompleto
- contrato inconsistente entre macro e local
- risco de cada modulo manter o proprio mecanismo de troca

## 3. Causa

Faltam duas coisas:

1. uma capability tecnica unica para aplicar composicao de cenas
2. uma separacao explicita entre:
   - semantica (`Route`, `Level`, `LocalContent`)
   - execucao tecnica (load/unload/active scene)
   - restart (estado restauravel)

## 4. Principio arquitetural

### Regra central
**Um executor tecnico unico, multiplos donos semanticos.**

### Executor tecnico
`SceneComposition`

### Donos semanticos
- `SceneFlow/Navigation` -> macro
- `LevelFlow` -> local
- `RestartContext` -> snapshot restauravel
- `WorldLifecycle` -> reset, sem executar composicao de cenas

## 5. Contrato alvo — Scope

```csharp
public enum SceneCompositionScope
{
    Macro = 0,
    Local = 1
}
```

## 6. Contrato alvo — Request

```csharp
public readonly struct SceneCompositionRequest
{
    public SceneCompositionScope Scope { get; }
    public string Reason { get; }
    public string CorrelationId { get; }
    public IReadOnlyList<SceneRef> ScenesToLoad { get; }
    public IReadOnlyList<SceneRef> ScenesToUnload { get; }
    public SceneRef ActiveScene { get; }
}
```

### Regra
Esse request e **tecnico**.

Ele nao deve carregar:
- `RouteId`
- `LevelId`
- `LevelRef`
- `LocalContentId` semantico
- estado de progressao

## 7. Contrato alvo — Plan

```csharp
public sealed class SceneCompositionPlan
{
    public SceneCompositionRequest Request { get; }
    public string SemanticContentId { get; }
    public string SemanticSignature { get; }
}
```

### Regra
`Plan` separa:
- identidade semantica
- de execucao tecnica

Isso permite mudar cenas tecnicas sem invalidar o snapshot semantico.

## 8. Contrato alvo — Executor

```csharp
public interface ISceneCompositionExecutor
{
    UniTask<SceneCompositionResult> ApplyAsync(SceneCompositionRequest request, CancellationToken ct);
}
```

## 9. Contrato alvo — Result

```csharp
public readonly struct SceneCompositionResult
{
    public bool Success { get; }
    public SceneCompositionScope Scope { get; }
    public string Reason { get; }
    public string CorrelationId { get; }
}
```

### Regra
O executor tecnico:
- nao conhece `RouteId`
- nao conhece `LevelRef`
- nao conhece restart
- nao decide loading/fade
- nao decide semantica de progresso

Ele apenas aplica a composicao.

## 10. Snapshot semantico alvo

`GameplayStartSnapshot` precisa evoluir para representar o estado restauravel semantico, nao o plano tecnico.

### Shape recomendado

```csharp
public sealed class GameplayStartSnapshot
{
    public SceneRouteId MacroRouteId { get; }
    public LevelDefinitionAsset LevelRef { get; }
    public string LocalContentId { get; }
    public string Reason { get; }
    public int SelectionVersion { get; }
    public string Signature { get; }
}
```

### Regra
O snapshot guarda:
- o que restaurar

O snapshot nao guarda:
- lista de cenas a carregar
- lista de cenas a descarregar
- `SceneCompositionRequest`

## 11. Ownership

### Macro
`SceneFlow/Navigation`
- resolve rota
- decide loading/fade
- produz identidade semantica macro
- monta ou resolve plano tecnico macro
- chama `ISceneCompositionExecutor`

### Local
`LevelFlow`
- resolve `LevelRef`
- decide `LocalContentId` quando existir
- atualiza `RestartContext`
- monta ou resolve plano tecnico local
- chama `ISceneCompositionExecutor`

### Restart
`RestartContextService`
- persiste/restaura snapshot semantico
- nao executa composicao tecnica

## 12. Papel do ContentSwap atual

### Veredito
Nesta fase, `ContentSwap` nao entra como contrato base do desenho novo.

### Motivo
O modulo atual esta ancorado em:
- contexto in-place
- pending/commit
- eventos/telemetria

Nao e o executor tecnico canonico que o desenho novo precisa.

### Direcao recomendada
1. criar `SceneComposition`
2. migrar o uso real do trilho local
3. depois decidir se `ContentSwap`:
   - morre
   - vira facade temporaria
   - ou tem observabilidade reaproveitada

## 13. Arquivos impactados na fase seguinte

### Novo modulo tecnico
- `Infrastructure/SceneComposition/*`

### Trilho local
- `Modules/LevelFlow/Runtime/LevelAdditiveSceneRuntimeApplier.cs`
- `Modules/LevelFlow/Runtime/LevelSwapLocalService.cs`
- `Modules/LevelFlow/Runtime/GameplayStartSnapshot.cs`
- `Modules/LevelFlow/Runtime/RestartContextService.cs`

### Trilho macro (depois)
- `Modules/Navigation/*`
- `Modules/SceneFlow/*`

### Legado/convergencia futura
- `Modules/ContentSwap/*`

## 14. Ordem segura apos a 2.b

### Fase 2.c
Migrar primeiro o trilho local:
- `LevelAdditiveSceneRuntimeApplier` vira implementacao de `ISceneCompositionExecutor`
- `LevelFlow` para de aplicar cena diretamente

### Fase 2.d
Evoluir `GameplayStartSnapshot` e `RestartContext`

### Fase 2.e
Trazer o trilho macro para o mesmo executor tecnico, sem tirar de `SceneFlow` o ownership de loading/fade

### Fase 2.f
Desidratar ou remover `ContentSwap`

## 15. Critérios de aceite da fase

A Fase 2.b termina quando estiver congelado:

- qual interface executa composicao de cenas
- qual estrutura representa o pedido tecnico
- qual estrutura representa o estado restauravel
- quem decide macro
- quem decide local
- qual o destino do `ContentSwap` atual na migracao

## 16. Recomendacao objetiva

A melhor direcao para o projeto e:

- criar `SceneComposition` como capability tecnica nova
- migrar primeiro o trilho local
- deixar a semantica nos modulos donos
- evitar transformar `ContentSwap` num novo monolito central

## Fechamento 2026-03-25

- A direcao desta fase foi absorvida pelo fechamento do plano macro e pelo trilho `SceneComposition` consolidado.
- O que era pendencia de consolidacao macro agora deve ser lido como historico de caminho, nao como backlog ativo.
- `SceneFlow`, `LevelFlow`, `Navigation`, `ResetInterop` e `GameLoop` ficaram estabilizados em boundaries mais claros do que o texto original descrevia.
