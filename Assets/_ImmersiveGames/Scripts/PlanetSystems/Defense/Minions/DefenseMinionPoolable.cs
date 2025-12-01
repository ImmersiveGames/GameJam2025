using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ProjectilesSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Versão especializada do BulletPoolable para minions de defesa.
    ///
    /// Reaproveita:
    /// - Rigidbody / velocidade
    /// - DamageDealer / colisão / retorno ao pool
    /// - LifetimeManager
    ///
    /// E adiciona:
    /// - leitura do DefensesMinionData (PoolableObjectData específico)
    /// - leitura do DefenseMinionBehaviorProfile
    /// - aplica o profile em um DefenseMinionController, se existir.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class DefenseMinionPoolable : BulletPoolable
    {
        private DefensesMinionData _minionData;
        private DefenseMinionBehaviorProfileSO _profileV2;
        private DefenseMinionBehaviorProfile _profile;
        private DefenseMinionController _controller;

        private void Awake()
        {
            // Se o controller existir no mesmo GameObject, guardamos a referência.
            _controller = GetComponent<DefenseMinionController>();
        }

        protected override void OnConfigured(PoolableObjectData config, IActor spawner)
        {
            // Mantém comportamento base (Rigidbody, DamageDealer, BulletObjectData, etc.)
            base.OnConfigured(config, spawner);

            ApplyMinionConfig(config, "OnConfigured");
        }

        protected override void OnReconfigured(PoolableObjectData config)
        {
            base.OnReconfigured(config);

            ApplyMinionConfig(config, "OnReconfigured");
        }

        protected override void OnActivated(Vector3 pos, Vector3? direction, IActor spawner)
        {
            // Para minions, normalmente vamos controlar o movimento via
            // Entry/Chase (DOTween ou lógica própria), então:
            // - chamamos base.OnActivated(pos, null, spawner) para NÃO dar velocidade de bullet.
            base.OnActivated(pos, null, spawner);

            DebugUtility.LogVerbose<DefenseMinionPoolable>(
                $"[Poolable] OnActivated em '{name}' | pos={pos} | spawner={(spawner != null ? spawner.ActorName : "null")}.",
                null,this);
        }

        /// <summary>
        /// Lê o DefensesMinionData e o BehaviorProfile associados a este objeto.
        /// </summary>
        private void ApplyMinionConfig(PoolableObjectData config, string source)
        {
            _minionData = config as DefensesMinionData;
            if (_minionData == null)
            {
                // Não é um minion de defesa – nada a fazer.
                return;
            }

            _profileV2 = _minionData.BehaviorProfileV2;
            _profile   = _minionData.DefaultProfile;

            // Preferimos o profile v2; se estiver ausente, caímos no legado.
            if (_profileV2 != null)
            {
                DebugUtility.LogVerbose<DefenseMinionPoolable>(
                    $"[{source}] '{name}' recebeu profile v2 '{_profileV2.VariantId}' do data '{_minionData.name}'.",
                    null,this);

                if (_controller != null)
                {
                    _controller.ApplyProfile(_profileV2, _profile);
                }
                else
                {
                    DebugUtility.LogWarning<DefenseMinionPoolable>(
                        $"[{source}] '{name}' possui profile v2 '{_profileV2.VariantId}', mas não encontrou DefenseMinionController no mesmo GameObject.",
                        this);
                }

                return;
            }

            if (_profile == null)
            {
                DebugUtility.LogWarning<DefenseMinionPoolable>(
                    $"[{source}] DefensesMinionData '{_minionData.name}' não possui BehaviorProfile definido (v2 nem legado).",
                    this);
                return;
            }

            DebugUtility.LogVerbose<DefenseMinionPoolable>(
                $"[{source}] '{name}' recebeu profile legado '{_profile.VariantId}' do data '{_minionData.name}'.",
                null,this);

            if (_controller != null)
            {
                _controller.ApplyProfile(_profile);
            }
            else
            {
                DebugUtility.LogWarning<DefenseMinionPoolable>(
                    $"[{source}] '{name}' possui profile legado '{_profile.VariantId}', mas não encontrou DefenseMinionController no mesmo GameObject.",
                    this);
            }
        }
    }
}
