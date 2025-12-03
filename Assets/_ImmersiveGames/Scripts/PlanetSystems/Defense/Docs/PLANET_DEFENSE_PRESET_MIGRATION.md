# Migração para PlanetDefensePresetSo (02/12/2025)

## Visão geral
- **Objetivo:** unificar configuração de defesa planetária em um único `PlanetDefensePresetSo`, reduzindo SOs paralelos e o risco de divergência entre ondas, estratégia e alvo.
- **Escopo:** validação em Unity 6 (multiplayer local), mantendo SRP (planeta não decide comportamento de minion) e compatibilidade com perfis existentes.
- **Fluxo-alvo:** `PlanetDefenseLoadoutSo` referencia apenas o preset; `PlanetDefenseOrchestrationService` resolve contexto via `PlanetDefensePresetAdapter`, caindo no legado somente quando o preset estiver ausente.

## Plano de migração detalhado
1. **Auditar assets antigos**
   - Rode um script de inventário (abaixo) para listar `PlanetDefenseLoadoutSo` que ainda usam `waveProfileOverride`, `defenseStrategy` ou `defensePoolData`.
   - Filtre por planetas usados em cenas ativas para priorizar migração.
2. **Gerar presets automaticamente**
   - Use o script de Editor abaixo para criar um `PlanetDefensePresetSo` por loadout antigo, preservando o `DefenseWaveProfileSo` e o `DefenseStrategySo` quando existirem.
   - O pool permanece no `PlanetDefenseLoadoutSo` (migração gradual) e será lido como fallback pelo orquestrador.
3. **Revisar manualmente no Inspector**
   - Abra cada preset gerado e valide:
     - `Target Mode`: escolha entre `PlayerOnly`, `EaterOnly`, `PlayerOrEater`, `PreferPlayer`, `PreferEater`.
     - `Wave Profile`: confirme `WaveEnemiesCount`, `SecondsBetweenWaves`, `SpawnRadius`, `SpawnHeightOffset` e `SpawnPattern`.
     - `Minion Data`: selecione `DefensesMinionData` correspondente ao tipo de minion.
     - `Use Custom Strategy`: marque apenas se quiser substituir a `SimplePlanetDefenseStrategy` padrão.
4. **Salvar e versionar**
   - Salve os novos assets (`Ctrl+S`) e faça commit dos presets criados. Mantenha os campos legados ocultos até completar a migração de todas as cenas.
5. **Limpeza futura**
   - Após validar as cenas, remova gradualmente o uso dos campos ocultos e delete perfis legados não referenciados.

### Script de Editor para migrar loadouts
Coloque este arquivo em `Assets/_ImmersiveGames/Scripts/PlanetSystems/Defense/Editor/PlanetDefensePresetMigration.cs` e execute via menu `Tools/Defense/Migrate Loadouts To Preset`.

```csharp
using System.IO;
using UnityEditor;
using UnityEngine;
using _ImmersiveGames.Scripts.PlanetSystems.Defense;

public static class PlanetDefensePresetMigration
{
    [MenuItem("Tools/Defense/Migrate Loadouts To Preset")]
    public static void Migrate()
    {
        string[] guids = AssetDatabase.FindAssets("t:PlanetDefenseLoadoutSo");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var loadout = AssetDatabase.LoadAssetAtPath<PlanetDefenseLoadoutSo>(path);
            if (loadout == null || loadout.DefensePreset != null)
            {
                continue;
            }

            var preset = ScriptableObject.CreateInstance<PlanetDefensePresetSo>();
            preset.name = $"{loadout.name}_Preset";
            preset.SetFromLegacy(loadout.WaveProfileOverride, loadout.DefenseStrategy, loadout.DefensePoolData);

            string presetPath = Path.Combine(Path.GetDirectoryName(path) ?? "Assets", preset.name + ".asset");
            AssetDatabase.CreateAsset(preset, presetPath);
            AssetDatabase.SaveAssets();

            loadout.SetPresetForMigration(preset);
            EditorUtility.SetDirty(loadout);
            Debug.Log($"[PresetMigration] Converted '{loadout.name}' → '{preset.name}'");
        }
    }
}
```

> Observação: `SetFromLegacy` e `SetPresetForMigration` devem ser adicionados apenas em um branch de migração temporário para manter o runtime limpo; remova-os após concluir a migração.

## Exemplos de uso no Editor
- **Criar preset manual**: `Create → ImmersiveGames → PlanetSystems → Defense → Planet Defense Preset`. Preencha `Wave Profile`, `Minion Data` e `Target Mode`. Use nomes padronizados como `Preset_Balanced_PlayerFirst`.
- **Aplicar em loadout**: no `PlanetDefenseLoadoutSo`, arraste o preset para `Defense Preset`. Os campos `Wave Profile Override` e `Defense Strategy` ficam ocultos e servem apenas para fallback durante a migração.
- **Cena com múltiplos planetas**: configure cada planeta com um loadout que aponte para um preset distinto (`Preset_Tutorial`, `Preset_HardMode`). O orquestrador cacheia o contexto por planeta para evitar alocações extras em multiplayer local.

## Boas práticas
- **SRP**: mantenha o comportamento do minion apenas nos profiles (`DefenseMinionBehaviorProfileSO`). O planeta não deve alterar lógica do minion.
- **Nomenclatura**: use sufixo `So` para ScriptableObjects e nomes descritivos (`WaveEnemiesCount`, `SpawnRadius`). Evite abreviações ambíguas.
- **Otimização**: preferir presets compartilhados entre planetas que usam o mesmo padrão de onda para reduzir GC. Confirme no Profiler que não há alocações em loops de waves/minions (cache de `PlanetDefenseSetupContext` e `DefenseTargetMode` já cobre os casos comuns).

## Testes finais recomendados
- **SRP**: valide que minions aplicam o profile vindo do wave runner, sem overrides no planeta (inspecione `DefenseMinionController` em runtime).
- **Nomenclatura**: revise assets novos para garantir sufixos `So` e campos claros (ex.: `WaveEnemiesCount`).
- **Funcionalidade**: em Play Mode, force detecções Player/Eater e confirme targeting via enum (logs de `SimplePlanetDefenseStrategy`).
- **Performance**: rode a cena em multiplayer local e monitore o Profiler; não devem ocorrer GC spikes durante loops de waves/minions graças ao cache de contexto no `PlanetDefenseOrchestrationService`.

## Atualizações
- **02/12/2025**: adicionado fluxo de preset único, cache de contexto e estratégia simples baseada em `DefenseTargetMode`, com plano de migração guiado por Editor.
