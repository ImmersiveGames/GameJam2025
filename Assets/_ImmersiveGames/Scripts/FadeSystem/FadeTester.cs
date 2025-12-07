using UnityEngine;
using _ImmersiveGames.Scripts.LoaderSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;

namespace _ImmersiveGames.Scripts.FadeSystem
{
    public class FadeTester : MonoBehaviour
    {
        private IFadeService _fade;

        private void Start()
        {
            if (DependencyManager.Provider.TryGetGlobal(out IFadeService service))
            {
                _fade = service;
                Debug.Log("[FadeTester] FadeService resolvido com sucesso.");
            }
            else
            {
                Debug.LogError("[FadeTester] Não foi possível resolver IFadeService.");
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                Debug.Log("[FadeTester] F pressionado - RequestFadeIn()");
                _fade?.RequestFadeIn();
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                Debug.Log("[FadeTester] G pressionado - RequestFadeOut()");
                _fade?.RequestFadeOut();
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                Debug.Log("[FadeTester] H pressionado - Disparando FadeInRequestedEvent");
                EventBus<FadeInRequestedEvent>.Raise(new FadeInRequestedEvent());
            }

            if (Input.GetKeyDown(KeyCode.J))
            {
                Debug.Log("[FadeTester] J pressionado - Disparando FadeOutRequestedEvent");
                EventBus<FadeOutRequestedEvent>.Raise(new FadeOutRequestedEvent());
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log("[FadeTester] R pressionado - ReloadCurrentScene via SceneLoader");
                SceneLoader.Instance.ReloadCurrentScene();
            }
        }
    }
}