using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.AudioSystem.Core;
using _ImmersiveGames.Scripts.SkinSystems;
using _ImmersiveGames.Scripts.SkinSystems.Configurable;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using _ImmersiveGames.Scripts.SkinSystems.Runtime;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem.Skins
{
    /// <summary>
    /// Integra o sistema de áudio com o sistema de skins.
    ///
    /// - Reage às alterações de skin via SkinConfigurable.
    /// - Garante que as instâncias de skin possuam EntityAudioEmitter (quando configurado).
    /// - Ajusta parâmetros de spatial audio (maxDistance) com base no SkinRuntimeState.
    /// - Cria um grupo lógico de áudio por ator no AudioRuntimeRoot (para uso futuro).
    ///
    /// Importante:
    /// - Trabalha "por skin inteira" (targetModelType), não por subpartes.
    /// - Não modifica o Skin System, apenas consome seus eventos e estados.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/Audio/Skin Audio Configurable")]
    public class SkinAudioConfigurable : SkinConfigurable
    {
        [Header("Actor Audio Group")]
        [Tooltip("Se verdadeiro, cria um grupo lógico de áudio por ator no AudioRuntimeRoot.")]
        [SerializeField] private bool createActorAudioGroup = true;

        [Tooltip("Prefixo usado para nomear o grupo de áudio do ator.")]
        [SerializeField] private string audioGroupPrefix = "ActorAudio_";

        [Header("Emitters em Instâncias de Skin")]
        [Tooltip("Se verdadeiro, garante que cada instância de skin possua um EntityAudioEmitter.")]
        [SerializeField] private bool autoAddEmittersToSkinInstances = true;

        [Header("Spatial Audio baseado no Runtime State")]
        [Tooltip("Se verdadeiro, ajusta o maxDistance dos AudioSources com base em SkinRuntimeState.")]
        [SerializeField] private bool useRuntimeStateForSpatialAudio = true;

        [Tooltip("Multiplicador usado para converter o raio aproximado da skin em maxDistance.")]
        [SerializeField] private float radiusToMaxDistanceMultiplier = 2f;

        [Tooltip("Valor mínimo de maxDistance ao ajustar a partir do tamanho da skin.")]
        [SerializeField] private float minMaxDistance = 10f;

        [Tooltip("Valor máximo de maxDistance ao ajustar a partir do tamanho da skin.")]
        [SerializeField] private float maxMaxDistance = 200f;

        private ActorSkinController _actorSkinController;
        private SkinRuntimeStateTracker _runtimeStateTracker;
        private IActor _ownerActor;
        private Transform _actorAudioGroup;

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            CacheDependencies();
            CreateActorAudioGroupIfNeeded();
        }

        #endregion

        #region Dependency Cache

        private void CacheDependencies()
        {
            _actorSkinController = GetComponentInParent<ActorSkinController>();
            _runtimeStateTracker = GetComponent<SkinRuntimeStateTracker>();
            _ownerActor = GetComponentInParent<IActor>();

            if (_actorSkinController == null)
            {
                DebugUtility.LogWarning<SkinAudioConfigurable>(
                    $"[{name}] SkinAudioConfigurable não encontrou ActorSkinController no parent.");
            }

            if (_ownerActor == null)
            {
                DebugUtility.LogWarning<SkinAudioConfigurable>(
                    $"[{name}] SkinAudioConfigurable não encontrou IActor no parent.");
            }

            if (_runtimeStateTracker == null && useRuntimeStateForSpatialAudio)
            {
                DebugUtility.LogWarning<SkinAudioConfigurable>(
                    $"[{name}] SkinAudioConfigurable não encontrou SkinRuntimeStateTracker no mesmo GameObject. " +
                    "Ajuste de spatial audio por tamanho da skin ficará limitado.");
            }
        }

        private void CreateActorAudioGroupIfNeeded()
        {
            if (!createActorAudioGroup)
                return;

            if (_ownerActor == null || string.IsNullOrEmpty(_ownerActor.ActorId))
                return;

            _actorAudioGroup = AudioRuntimeRoot.GetOrCreateGroup($"{audioGroupPrefix}{_ownerActor.ActorId}");
        }

        #endregion

        #region SkinConfigurable Overrides

        /// <summary>
        /// Chamado quando uma SkinConfig específica é aplicada.
        /// Aqui poderemos, no futuro, conectar dados de SFX específicos da skin.
        /// Por ora, apenas registra o evento para debug.
        /// </summary>
        protected override void ConfigureSkin(ISkinConfig skinConfig)
        {
            if (skinConfig == null)
                return;

            // Neste momento não há dados específicos de áudio por skin.
            // Mantemos este hook preparado para associar coleções de SFX futuramente.
            DebugUtility.LogVerbose<SkinAudioConfigurable>(
                $"[{name}] ConfigureSkin chamado para ModelType={skinConfig.ModelType} (Skin='{skinConfig.ConfigName}').");
        }

        /// <summary>
        /// Chamado quando as instâncias da skin para o targetModelType são criadas.
        /// Aqui garantimos emitters e ajustamos spatial audio com base no runtime state.
        /// </summary>
        protected override void ConfigureSkinInstances(List<GameObject> instances)
        {
            if (instances == null || instances.Count == 0)
                return;

            if (autoAddEmittersToSkinInstances)
            {
                EnsureEmittersOnInstances(instances);
            }

            if (useRuntimeStateForSpatialAudio)
            {
                AdjustSpatialAudioFromRuntimeState(instances);
            }
        }

        /// <summary>
        /// Chamado quando for necessário aplicar modificações dinâmicas na skin atual.
        /// Por ora, reaplicamos o ajuste de spatial audio (caso o tamanho tenha mudado).
        /// </summary>
        protected override void ApplyDynamicModifications()
        {
            var instances = GetSkinInstances();
            if (instances is { Count: > 0 } && useRuntimeStateForSpatialAudio)
            {
                AdjustSpatialAudioFromRuntimeState(instances);
            }
        }

        #endregion

        #region Emitters

        private void EnsureEmittersOnInstances(List<GameObject> instances)
        {
            foreach (var instance in instances)
            {
                if (instance == null)
                    continue;

                // Garante um EntityAudioEmitter na raiz da instância da skin.
                if (!instance.TryGetComponent(out EntityAudioEmitter emitter))
                {
                    emitter = instance.AddComponent<EntityAudioEmitter>();

                    DebugUtility.LogVerbose<SkinAudioConfigurable>(
                        $"[{name}] EntityAudioEmitter adicionado automaticamente em '{instance.name}' (skin).");
                }

                // Se desejarmos, no futuro, poderemos configurar um AudioConfig padrão
                // via inspector neste SkinAudioConfigurable e aplicá-lo aos emitters.
                // Atualmente, respeitamos o que estiver configurado no prefab / componente.
            }
        }

        #endregion

        #region Spatial Audio via SkinRuntimeState

        private void AdjustSpatialAudioFromRuntimeState(List<GameObject> instances)
        {
            if (_actorSkinController == null || _runtimeStateTracker == null)
                return;

            if (!_actorSkinController.TryGetRuntimeState(targetModelType, out var state) || !state.HasValidBounds)
            {
                DebugUtility.LogVerbose<SkinAudioConfigurable>(
                    $"[{name}] Não foi possível obter SkinRuntimeState válido para ModelType={targetModelType}. " +
                    "Ajuste de spatial audio ignorado.");
                return;
            }

            float suggestedMaxDistance = Mathf.Clamp(
                state.ApproxRadius * radiusToMaxDistanceMultiplier,
                minMaxDistance,
                maxMaxDistance);

            foreach (var instance in instances)
            {
                if (instance == null)
                    continue;

                var audioSources = instance.GetComponentsInChildren<AudioSource>(true);
                foreach (var source in audioSources)
                {
                    if (source == null)
                        continue;

                    // Ajusta apenas fontes com spatialBlend > 0 (sons 3D).
                    if (source.spatialBlend <= 0f)
                        continue;

                    source.maxDistance = suggestedMaxDistance;
                }
            }

            DebugUtility.LogVerbose<SkinAudioConfigurable>(
                $"[{name}] Spatial audio ajustado para ModelType={targetModelType}: " +
                $"ApproxRadius={state.ApproxRadius:F2}, maxDistance≈{suggestedMaxDistance:F2}");
        }

        #endregion
    }
}
