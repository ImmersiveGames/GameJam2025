﻿using UnityEngine;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public enum FactoryType
    {
        Default,
        Bullet,
        Enemy
    }

    public abstract class PoolableObjectData : ScriptableObject
    {
        [System.Serializable]
        public struct BulletConfig
        {
            public float speed;
            public float damage;
            public Sprite sprite;
        }

        [SerializeField] private string objectName;
        [SerializeField] private GameObject prefab;
        [SerializeField, Min(0)] private float lifetime = 5f;
        [SerializeField] private int initialPoolSize = 5;
        [SerializeField] private bool canExpand;
        [SerializeField] private FactoryType factoryType = FactoryType.Default;
        [SerializeField] private BulletConfig[] bulletConfigs; // Configurações para variações de bullets

        public string ObjectName => objectName;
        public GameObject Prefab => prefab;
        public float Lifetime => lifetime;
        public int InitialPoolSize => initialPoolSize;
        public bool CanExpand => canExpand;
        public FactoryType FactoryType => factoryType;
        public BulletConfig[] BulletConfigs => bulletConfigs;

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (lifetime < 0)
            {
                Debug.LogWarning($"Lifetime não pode ser negativo em {name}. Definindo como 0.", this);
                lifetime = 0;
            }

            if (factoryType == FactoryType.Bullet && (bulletConfigs == null || bulletConfigs.Length == 0))
            {
                Debug.LogWarning($"BulletConfigs não configurado em {name} com FactoryType.Bullet.", this);
            }
        }
#endif
    }
}