using System;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.New
{
    public class KeyPressTrigger : MonoBehaviour, ITrigger
    {
        [SerializeField] private KeyCode triggerKey = KeyCode.Space; // Configurável no Inspector

        public event Action OnTriggered;

        private void Update()
        {
            if (Input.GetKeyDown(triggerKey))
            {
                OnTriggered?.Invoke();
            }
        }
    }
}