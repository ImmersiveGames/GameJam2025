using System;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityUtils;
namespace _ImmersiveGames.Scripts.Tags
{
    public class FlagMarkPlanet : MonoBehaviour
    {
        private void Awake()
        {
            gameObject.DisableChildren();
            IsActive = false;
        }
        public bool IsActive { get; private set; }
        public void SetFlagActive(bool active)
        {
            IsActive = active;
            if (active)
            {
                gameObject.EnableChildren();
            }
            else
            {
                gameObject.DisableChildren();
            }
            DebugUtility.LogVerbose<FlagMarkPlanet>($"Bandeira {(active ? "ativada" : "desativada")} em {transform.parent?.name}");
        }
    }
}