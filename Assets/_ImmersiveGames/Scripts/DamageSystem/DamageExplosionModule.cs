using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    /// <summary>
    /// Responsável por reproduzir efeitos visuais de explosão quando o ator morre.
    /// </summary>
    public class DamageExplosionModule
    {
        private readonly Transform _ownerTransform;
        private readonly PoolData _explosionPoolData;
        private readonly Vector3 _offset;

        private ObjectPool _pool;
        private bool _poolInitialized;
        private bool _missingConfigLogged;

        public DamageExplosionModule(Transform ownerTransform, PoolData explosionPoolData, Vector3 offset)
        {
            _ownerTransform = ownerTransform;
            _explosionPoolData = explosionPoolData;
            _offset = offset;
        }

        public bool HasConfiguration => _explosionPoolData != null;

        public void Initialize()
        {
            _poolInitialized = false;
            _pool = null;

            if (!HasConfiguration)
            {
                if (_missingConfigLogged) return;
                DebugUtility.LogVerbose<DamageExplosionModule>(
                    "Pool de explosão não configurado. Nenhuma explosão será reproduzida.");
                _missingConfigLogged = true;
                return;
            }

            _missingConfigLogged = false;

            var poolManager = PoolManager.Instance;
            if (poolManager == null)
            {
                DebugUtility.LogWarning<DamageExplosionModule>("PoolManager não encontrado. Explosões não serão reproduzidas.");
                return;
            }

            _pool = poolManager.RegisterPool(_explosionPoolData);
            _poolInitialized = _pool != null;

            if (!_poolInitialized)
            {
                DebugUtility.LogError<DamageExplosionModule>($"Não foi possível obter o pool '{_explosionPoolData.ObjectName}'.");
            }
        }

        public void PlayExplosion(DamageContext context)
        {
            if (context == null)
            {
                return;
            }

            if (!_poolInitialized)
            {
                Initialize();
            }

            if (!_poolInitialized)
            {
                return;
            }

            var position = ResolveExplosionPosition(context);
            _pool.GetObject(position);
        }

        private Vector3 ResolveExplosionPosition(DamageContext context)
        {
            if (context.hasHitPosition)
            {
                return context.hitPosition + _offset;
            }

            if (_ownerTransform != null)
            {
                return _ownerTransform.position + _offset;
            }

            return _offset;
        }
    }
}
