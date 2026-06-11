# ACTIVITY-OBJECT-CAPACITY-1A-FIX1 — Compile Fix

## Erro corrigido

```text
Assets\_ImmersiveGames\NewScripts\SessionActivity\Pipeline\Stages\ActivityEntryObjectSetupStages.cs(1610,24): error CS0103: The name 'ActivityEntryObjectSetupStages' does not exist in the current context
```

## Causa

O arquivo `ActivityEntryObjectSetupStages.cs` contém múltiplos tipos internos, mas não existe um tipo chamado `ActivityEntryObjectSetupStages`. O novo helper de lookup de lifecycle contributions usava:

```csharp
nameof(ActivityEntryObjectSetupStages)
```

## Correção

A referência foi trocada para o tipo real onde o helper vive:

```csharp
nameof(ActivityEntryObjectSetupStageUtility)
```

## Impacto

- Correção de compile apenas.
- Sem mudança de arquitetura.
- Sem mudança de runtime flow.
- Sem alteração em Save/Snapshot/ActivityObject além do arquivo já envolvido no corte 1A.
