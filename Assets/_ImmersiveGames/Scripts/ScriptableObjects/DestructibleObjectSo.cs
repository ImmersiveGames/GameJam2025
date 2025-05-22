using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ScriptableObjects
{
    public abstract class DestructibleObjectSo : ScriptableObject
    {
        public float planetDefense = 0f;
        public bool planetCanDestroy = true;
        public float planetMaxHealth = 100f;
        public float planetDeathDelay = 2f;
        [SerializeField, Tooltip("Prefab do inimigo a ser instanciado")]
        public GameObject enemyModel;
    }
    public enum PlanetResources
    {
        Metal,
        Gas,
        Water,
        Rocks,
        Life
    }
}
