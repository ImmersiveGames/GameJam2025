using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems.Defense;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Testes de fluxo do orquestrador, cobrindo cache sequencial, raio dinâmico e validações básicas.
/// Comentários em português para a equipe de game design.
/// </summary>
public class TestPlanetDefenseFlow
{
    private PlanetDefenseOrchestrationService _service;
    private PlanetsMaster _planet;

    [SetUp]
    public void SetUp()
    {
        _service = new PlanetDefenseOrchestrationService();
        _planet = new GameObject("PlanetUnderTest").AddComponent<PlanetsMaster>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_planet != null)
        {
            Object.DestroyImmediate(_planet.gameObject);
        }
    }

    [Test]
    public void SequentialEntryIsCachedPerIndex()
    {
        // Configura duas entradas com presets distintos.
        var presets = new[] { CreatePreset("PoolA"), CreatePreset("PoolB") };
        var entries = new List<PlanetDefenseEntrySo>
        {
            CreateEntry(presets[0], 0f),
            CreateEntry(presets[1], 0f)
        };

        _service.ConfigureDefenseEntries(_planet, entries, DefenseChoiceMode.Sequential);

        var firstContext = _service.ResolveEffectiveConfig(_planet, DetectionType.Player);
        ClearCachedDetectionContext(_service, _planet, DetectionType.Player);
        var secondContext = _service.ResolveEffectiveConfig(_planet, DetectionType.Player);
        ClearCachedDetectionContext(_service, _planet, DetectionType.Player);
        var thirdContext = _service.ResolveEffectiveConfig(_planet, DetectionType.Player);

        Assert.AreSame(presets[0], firstContext.WavePreset, "O primeiro preset deve ser reutilizado para o primeiro índice sequencial.");
        Assert.AreSame(presets[1], secondContext.WavePreset, "O segundo preset deve ser escolhido ao avançar a sequência.");
        Assert.AreSame(presets[0], thirdContext.WavePreset, "Após completar a sequência, o cache por índice deve retornar ao primeiro preset.");
    }

    [Test]
    public void RespectsCachedRadiusWithOffset()
    {
        var preset = CreatePreset("PoolRadius");
        var entry = CreateEntry(preset, 2f);

        CacheApproxRadius(_service, _planet, 5f);

        _service.ConfigureDefenseEntries(_planet, new List<PlanetDefenseEntrySo> { entry }, DefenseChoiceMode.Sequential);
        var context = _service.ResolveEffectiveConfig(_planet, DetectionType.Player);

        Assert.AreEqual(7f, context.SpawnRadius, 0.001f, "SpawnRadius deve somar o raio cacheado com o offset da entrada.");
        Assert.AreEqual(2f, context.SpawnOffset, 0.001f, "SpawnOffset deve refletir o valor configurado na entrada.");
    }

    [Test]
    public void FlagsInvalidWavePresetAtRuntime()
    {
        var preset = ScriptableObject.CreateInstance<WavePresetSo>();
        SetPrivateField(preset, "intervalBetweenWaves", 0f);
        SetPrivateField(preset, "numberOfEnemiesPerWave", 0);
        var entry = CreateEntry(preset, 0f);

        _service.ConfigureDefenseEntries(_planet, new List<PlanetDefenseEntrySo> { entry }, DefenseChoiceMode.Sequential);

        LogAssert.Expect(LogType.Error, new Regex("NumberOfEnemiesPerWave inválido"));
        LogAssert.Expect(LogType.Error, new Regex("IntervalBetweenWaves inválido"));

        var context = _service.ResolveEffectiveConfig(_planet, DetectionType.Player);

        Assert.IsNotNull(context, "Contexto deve ser retornado mesmo para validação fail-fast.");
        Assert.AreSame(preset, context.WavePreset, "Preset inválido ainda deve ser encaminhado para depuração.");
    }

    private static WavePresetSo CreatePreset(string poolName)
    {
        var preset = ScriptableObject.CreateInstance<WavePresetSo>();
        var pool = ScriptableObject.CreateInstance<PoolData>();
        SetPrivateField(pool, "objectName", poolName);
        SetPrivateField(pool, "initialPoolSize", 1);
        SetPrivateField(pool, "objectConfigs", new PoolableObjectData[1] { null });

        SetPrivateField(preset, "poolData", pool);
        SetPrivateField(preset, "numberOfEnemiesPerWave", 3);
        SetPrivateField(preset, "intervalBetweenWaves", 1f);
        return preset;
    }

    private static PlanetDefenseEntrySo CreateEntry(WavePresetSo preset, float offset)
    {
        var entry = ScriptableObject.CreateInstance<PlanetDefenseEntrySo>();
        SetPrivateField(entry, "entryDefaultWavePreset", preset);
        SetPrivateField(entry, "spawnOffset", offset);
        return entry;
    }

    private static void CacheApproxRadius(PlanetDefenseOrchestrationService service, PlanetsMaster planet, float radius)
    {
        var cacheField = typeof(PlanetDefenseOrchestrationService)
            .GetField("_cachedApproxRadii", BindingFlags.Instance | BindingFlags.NonPublic);
        var cache = cacheField?.GetValue(service) as Dictionary<PlanetsMaster, float>;
        cache?.Add(planet, radius);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field?.SetValue(target, value);
    }

    private static void ClearCachedDetectionContext(
        PlanetDefenseOrchestrationService service,
        PlanetsMaster planet,
        DetectionType detectionType)
    {
        var cacheField = typeof(PlanetDefenseOrchestrationService)
            .GetField("_resolvedContexts", BindingFlags.Instance | BindingFlags.NonPublic);
        var cache = cacheField?.GetValue(service) as Dictionary<PlanetsMaster, Dictionary<DetectionType, PlanetDefenseSetupContext>>;

        if (cache == null || !cache.TryGetValue(planet, out var byDetection) || byDetection == null)
        {
            return;
        }

        byDetection.Remove(detectionType);
    }
}
