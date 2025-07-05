using UnityEngine;
namespace _ImmersiveGames.Scripts.SkinSystems.Handlers
{
    public interface IRootHandler
    {
        Transform GetOrCreateRoot(Transform parent);
        void Clear();
        void SetActive(bool active);
    }
}