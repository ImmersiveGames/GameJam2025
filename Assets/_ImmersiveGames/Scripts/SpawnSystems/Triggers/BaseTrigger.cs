﻿using _ImmersiveGames.Scripts.SpawnSystems.Data;
using _ImmersiveGames.Scripts.SpawnSystems.DynamicPropertiesSystem;
using _ImmersiveGames.Scripts.SpawnSystems.Interfaces;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems.Triggers
{
    [DebugLevel(DebugLevel.Logs)]
    public abstract class BaseTrigger : ISpawnTrigger
    {
        protected readonly EnhancedTriggerData data;
        protected SpawnPoint spawnPoint;
        protected bool isActive;

        protected BaseTrigger(EnhancedTriggerData data)
        {
            this.data = data;
            isActive = true;
        }

        public virtual void Initialize(SpawnPoint spawnPointRef)
        {
            spawnPoint = spawnPointRef;
            isActive = true;
        }

        public abstract bool CheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject);

        public virtual void SetActive(bool active)
        {
            isActive = active;
            DebugUtility.LogVerbose<BaseTrigger>($"Trigger {(active ? "ativado" : "desativado")} para '{spawnPoint?.name}'.", "yellow", spawnPoint);
        }

        public virtual void Reset()
        {
            SetActive(true);
        }

        public virtual void OnDisable()
        {
            isActive = false;
            DebugUtility.LogVerbose<BaseTrigger>($"OnDisable chamado para '{spawnPoint?.name}'.", "yellow", spawnPoint);
        }

        public bool IsActive => isActive;
    }
}