using _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems.Interfaces
{
    public class ContinuousTargetedShootStrategy : ISpawnStrategy
    {
        private float _lastShotTime;
        private readonly float _shotInterval = 1f; // Intervalo configurável

        public void Spawn(IPoolable[] objects, SpawnData data, Vector3 origin, Vector3 forward)
        {
            if (Time.time < _lastShotTime + _shotInterval) return;

            // Processa apenas o primeiro objeto
            var obj = objects != null && objects.Length > 0 ? objects[0] : null;
            if (obj == null) return;

            if (forward.magnitude < 0.1f)
            {
                DebugUtility.Log<ContinuousTargetedShootStrategy>($"Direção inválida: {forward}", "red", null);
                return;
            }

            var go = obj.GetGameObject();
            go.transform.position = origin;
            go.transform.rotation = Quaternion.identity;
            obj.Activate(origin);

            var movement = go.GetComponent<ProjectileMovement>();
            if (movement != null)
            {
                var poolableData = data.PoolableData as ProjectilesData;
                if (poolableData != null)
                {
                    movement.InitializeMovement(forward.normalized, poolableData.speed);
                    _lastShotTime = Time.time;
                    DebugUtility.Log<ContinuousTargetedShootStrategy>($"[{go.name}] Disparou na direção {forward.normalized}, Velocidade: {poolableData.speed}, Posição: {go.transform.position}", "green", go);
                }
            }
        }
    }
}